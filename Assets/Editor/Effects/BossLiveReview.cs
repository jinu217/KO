#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

// Opt-in review of the actual stage instance, using its normal skill coroutine.
// Play commands affect instances only. Explicit editor commands rebuild/save reviewed assets.
[InitializeOnLoad]
public static class BossLiveReview
{
    // (수정) 권한 문제가 없는 프로젝트 폴더 옆(Assets 밖)에 저장
    static readonly string Folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../boss_live_review")).Replace("\\", "/");
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    [Serializable] class Command { public string id, op, scene; public int index=-1, phase=1, facing=1; public float distance=3, height; }
    [Serializable] class State { public string commandId, scene, boss, clip, error, run, interpolation, bossId; public bool playing, attacking; public float time, hp, normalized; public Vector3 bossPosition, playerPosition; public int skills, captures, hits; }
    [Serializable] class Frame { public string file, clip; public float elapsed, normalized; public Vector3 bossPosition, playerPosition; public Hit[] hits; public int fxVisible; public string weaponLayer; public float weaponFront; }
    [Serializable] class Hit { public string step, action, collider, artName; public Vector2 artMin, artMax, center, size, minViewport, maxViewport, playerMin, playerMax; public float time; }
    static MidBossController boss;
    static PlayerController player;
    static Animator animator;
    static readonly List<Hit> pending = new List<Hit>();
    static string run, error;
    static float started, nextCapture, finishedAt=-1;
    static int captures, hits;
    static bool captureBusy;
    static bool specialRunning;
    static double nextPoll;
    static Vector3 home;
    static float playerGravity;
    static RigidbodyConstraints2D playerConstraints;
    static BossLiveReview()
    {
        EditorApplication.update += Update; BossSkillExecutor.HitboxObserved += HitObserved;
        EditorApplication.playModeStateChanged += state =>
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("BossLiveReview.play",false))
            { SessionState.SetBool("BossLiveReview.background",Application.runInBackground); Application.runInBackground=true; }
            if(state==PlayModeStateChange.ExitingPlayMode && SessionState.GetBool("BossLiveReview.play",false))
            { Application.runInBackground=SessionState.GetBool("BossLiveReview.background",false);SessionState.SetBool("BossLiveReview.play",false); }
        };
    }
    static FieldInfo Field(object o,string name) { for(var t=o.GetType();t!=null;t=t.BaseType){var f=t.GetField(name,Flags|BindingFlags.DeclaredOnly);if(f!=null)return f;}return null; }
    static object Get(object o,string name) => o==null?null:Field(o,name)?.GetValue(o);
    static void Set(object o,string name,object value) { if(o!=null)Field(o,name)?.SetValue(o,value); }
    static MethodInfo Method(object o,string name) { for(var t=o.GetType();t!=null;t=t.BaseType){var m=t.GetMethod(name,Flags|BindingFlags.DeclaredOnly);if(m!=null)return m;}return null; }
    static object Call(object o,string name,params object[] args) => Method(o,name)?.Invoke(o,args);
    static bool Attacking => specialRunning || (boss!=null && (bool)Get(boss,"isAttacking"));
    static void Update()
    {
        if(EditorApplication.isCompiling) return;
        if(EditorApplication.timeSinceStartup>=nextPoll)
        {
            nextPoll=EditorApplication.timeSinceStartup+.2;
            try
            {
                Directory.CreateDirectory(Folder);   // (수정) try 안으로 이동
                string file=Path.Combine(Folder,"command.json");
                if(File.Exists(file))
                {
                    var c=JsonUtility.FromJson<Command>(File.ReadAllText(file));
                    if(c!=null && c.id!=SessionState.GetString("BossLiveReview.command",""))
                    { SessionState.SetString("BossLiveReview.command",c.id); error=null; Execute(c); }
                }
                Status();
            }
            catch(Exception e)
            {
                error=e.ToString();
                // (수정) 에러 파일 저장까지 실패해도 에디터 오류가 쌓이지 않게 보호
                try { File.WriteAllText(Path.Combine(Folder,"error.txt"),error); } catch { }
            }
        }
        if(!Application.isPlaying || boss==null) return;
        // Suppress autonomous casts even when a desire component swaps skill data.
        Set(boss,"lastBasicTime",Time.time+100000f);
        Set(boss,"lastSkillTimes",Enumerable.Repeat(Time.time+100000f,boss.data.skills.Count).ToArray());
        // Health is replenished after recording actual hits; damage is not disabled.
        if(player!=null) { var status=player.GetComponent<PlayerStatus>(); if(status!=null)status.currentHp=100000; }
        if(run==null) return;
        if(!Attacking && finishedAt<0) finishedAt=Time.time;
        if(!captureBusy && (Time.time>=nextCapture || pending.Count>0))
        { nextCapture=Time.time+.18f; boss.StartCoroutine(Capture()); }
        if((finishedAt>=0 && Time.time-finishedAt>1.2f) || Time.time-started>35)
        {
            File.AppendAllText(Path.Combine(Folder,"runs.txt"),run+" duration="+(Time.time-started).ToString("0.###")+" attacking="+Attacking+" captures="+captures+" hitQueries="+hits+" final="+Clip()+"\n");
            run=null;
        }
    }
    static void Execute(Command c)
    {
        if(c.op=="stop") { run=null; EditorApplication.isPlaying=false; return; }
        if(c.op=="rebuild4")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop play before rebuilding clips.");
            if(AnimationMode.InAnimationMode())AnimationMode.StopAnimationMode();
            MainBoss4_AnimGenerators.RebuildPunchAiming();return;
        }
        if(c.op=="save")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop play before saving the scene.");
            if(AnimationMode.InAnimationMode())AnimationMode.StopAnimationMode();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());return;
        }
        if(c.op=="apply4")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop play before applying reviewed sizing.");
            Boss4SizingReview.ApplyReviewedSize();return;
        }
        if(c.op=="fx5") { BossClipEffectsSetup.RefreshStageFive();return; }
        if(c.op=="airpunch") { MainBoss4_AnimGenerators.Skill2Menu();AssetDatabase.SaveAssets();return; }
        if(c.op=="townverify") { BossMotionPolish.ValidateTown();return; }
        if(c.op=="polishcapture") { BossVisualReview.Capture(new[]{"MainBoss2_Buff","MainBoss4_skill2_Uppercut_Desire"});return; }
        if(c.op=="polish") { BossMotionPolish.Apply();return; }
        if(c.op=="bufffx") { BossClipEffectsSetup.RefreshBuffs();return; }
        if(c.op=="heightreport") { BossAttackHeightReview.Report();return; }
        if(c.op=="heightapply") { BossAttackHeightReview.Apply();return; }
        if(c.op=="heightfirst") { BossAttackHeightReview.LowerFirstFinisher();return; }
        if(c.op=="polishverify") { BossClipEffectsSetup.VerifyCombatPolish();return; }
        if(c.op=="combatpolish") { Stage5BossAnimationBuilder.RebuildMainCombatPolish();BossClipEffectsSetup.RefreshCombatPolish();return; }
        if(c.op=="midcapture") {
            var clips=AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets/Animations"}).Select(g=>AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g))).Where(a=>a!=null && a.name.StartsWith("MidBoss")).Select(a=>a.name).Distinct().ToArray();
            BossVisualReview.Capture(clips);return;
        }
        if(c.op=="motionaudit") { BossMotionFXAudit.Run();return; }
        if(c.op=="slashsetup") { BossWeaponSlashTrailSetup.Apply();return; }
        if(c.op=="orbitaudit") { OrbitSwordSetup.Layers();Stage5BossAnimationBuilder.RebuildMainFlightAim();BossMotionFXAudit.Run();BossWeaponFXAudit.Run();return; }
        if(c.op=="layercapture") { BossVisualReview.Capture(new[]{"MainBoss2_Attack","MainBoss2_Skill1","MainBoss2_Skill2","MainBoss2_Skill3","MainBoss2_Buff"});return; }
        if(c.op=="flightaim") { Stage5BossAnimationBuilder.RebuildMainFlightAim();return; }
        if(c.op=="heightretime") { BossAttackHeightReview.RefreshTimings();return; }
        if(c.op=="scenecopy") { if(Application.isPlaying)throw new Exception("Edit Mode required");EditorSceneManager.SaveScene(SceneManager.GetActiveScene(),Folder+"/pending_scene.unity",true);return; }
        if(c.op=="reloadcopied")
        {
            if(Application.isPlaying || !File.Exists(Folder+"/pending_scene.unity"))throw new Exception("Edit Mode and preserved scene copy required");
            EditorSceneManager.OpenScene(SceneManager.GetActiveScene().path);return;
        }
        if(c.op=="scene")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop play before opening another stage.");
            if((!c.scene.StartsWith("Assets/Scenes/Stage") && c.scene!="Assets/Scenes/Equipment_TestRoom.unity") || !c.scene.EndsWith(".unity"))throw new Exception("Stage or equipment test room path required.");
            if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new Exception("Unsaved scene: leave it untouched.");
            if(AnimationMode.InAnimationMode())AnimationMode.StopAnimationMode();
            EditorSceneManager.OpenScene(c.scene); return;
        }
        if(c.op=="play") { SessionState.SetBool("BossLiveReview.play",true);EditorApplication.isPlaying=true; return; }
        if(c.op=="status") return;
        if(!Application.isPlaying)throw new Exception("Play Mode required.");
        if(c.op=="prepare")
        {
            boss=UnityEngine.Object.FindFirstObjectByType<MidBossController>();
            player=UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            // Stage5-3 currently has no boss placement. Review the real prefab in
            // this stage without adding/saving a scene instance.
            if(boss==null && player!=null && SceneManager.GetActiveScene().name=="Stage5-3")
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Stage5_Enemy/Stage5_MidBoss.prefab");
                var instance=UnityEngine.Object.Instantiate(prefab,player.transform.position+Vector3.right*12,Quaternion.identity);
                instance.name="Stage5_MidBoss_ReviewOnly";boss=instance.GetComponent<MidBossController>();
                var col=instance.GetComponent<Collider2D>();
                var floor=Physics2D.Raycast(instance.transform.position+Vector3.up,Vector2.down,10,boss.wallLayer);
                if(floor.collider!=null)instance.transform.position+=Vector3.up*(floor.point.y-col.bounds.min.y+.01f);
                Physics2D.SyncTransforms();
            }
            if(boss==null || player==null)throw new Exception("Stage boss/player missing.");
            animator=boss.GetComponentInChildren<Animator>(); home=boss.transform.position;
            playerGravity=player.GetComponent<Rigidbody2D>().gravityScale;
            playerConstraints=player.GetComponent<Rigidbody2D>().constraints;
            boss.StopAllCoroutines(); Set(boss,"isAttacking",false);
            boss.GetComponent<BossEffects>()?.EndSkill();
            var copy=UnityEngine.Object.Instantiate(boss.data); copy.hideFlags=HideFlags.DontSave; boss.data=copy;
            copy.basicAttack.cooldown=100000; foreach(var s in copy.skills)s.cooldown=100000;
            Set(boss,"lastBasicTime",Time.time); Set(boss,"lastSkillTimes",Enumerable.Repeat(Time.time,copy.skills.Count).ToArray());
            Set(boss,"initialCooldownsApplied",true); Set(boss,"playEncounterDialogue",false); Set(boss,"dialoguePaused",false);
            foreach(var dialogue in UnityEngine.Object.FindObjectsByType<MainBossDialogueController>(FindObjectsSortMode.None))
            {
                dialogue.StopAllCoroutines();
                (Get(dialogue,"dialoguePanel") as GameObject)?.SetActive(false);
                dialogue.enabled=false;
            }
            player.PauseKeyboardControl(); Time.timeScale=1;
            foreach(var background in UnityEngine.Object.FindObjectsByType<BackgroundLoop>(FindObjectsSortMode.None))
                if(background.backgrounds==null || background.backgrounds.Length<4 || background.backgrounds.Any(x=>x==null))background.enabled=false;
            var status=player.GetComponent<PlayerStatus>(); if(status!=null)status.currentHp=100000;
            foreach(var component in boss.GetComponents<MonoBehaviour>())
                if(component!=null && component.GetType().Name.Contains("Desire"))
                { component.enabled=false; Set(component,"lastUseTime",1000000f);Set(component,"nextUseTime",1000000f);Set(component,"nextUse",1000000f); }
            Position(c);
            Selection.activeGameObject=boss.gameObject;
            return;
        }
        if(boss==null)throw new Exception("Prepare this stage first.");
        if(c.op=="playerattack") { player.StartCoroutine(ReviewPlayerAttackMovement());return; }
        if(c.op=="playerrun") { player.StartCoroutine(ReviewPlayerRunInterpolation());return; }
        if(c.op=="size")
        {
            if(!(boss is Stage4_MainBoss) || Attacking || run!=null)throw new Exception("Idle MainBoss4 required.");
            float scale=Mathf.Clamp(c.distance,.3f,1f);
            var visual=boss.transform.Find("MainBoss 4");var capsule=boss.GetComponent<CapsuleCollider2D>();
            float floor=home.y+capsule.offset.y-capsule.size.y*.5f;
            visual.localScale=new Vector3(Mathf.Sign(visual.localScale.x)*scale,scale,1);Set(boss,"visualScaleX",scale);
            float bottom=-2.856f*scale+.13f,top=2*scale;
            capsule.offset=new Vector2(-.2f*scale,(bottom+top)*.5f);capsule.size=new Vector2(1.65f*scale,top-bottom);
            home.y=floor-bottom;
            c.distance=scale/.46f*1.65f;Position(c);return;
        }
        if(c.op=="position") { Position(c); return; }
        if(c.op=="desire")
        {
            if(Attacking || run!=null)throw new Exception("Previous action still running.");
            Position(c); Set(boss,"currentPhase",3);Set(boss,"shownPhase",3);Set(boss,"currentHp",boss.data.maxHp*.2f);
            var desire=boss.GetComponents<MonoBehaviour>().FirstOrDefault(x=>x!=null && x.GetType().Name.Contains("Desire"));
            if(desire==null)throw new Exception("Desire component missing.");
            desire.enabled=true;
            run=boss.name+"_Desire"+c.index+"_"+c.id;captures=hits=0;pending.Clear();finishedAt=-1;started=Time.time;nextCapture=Time.time;
            Directory.CreateDirectory(Path.Combine(Folder,run));
            specialRunning=true;boss.StartCoroutine(DesireRoutine(desire,c.index));return;
        }
        if(c.op=="chase")
        {
            Position(c); run=boss.name+"_chase_"+c.id; captures=hits=0;pending.Clear();finishedAt=-1;started=Time.time;nextCapture=Time.time;
            Directory.CreateDirectory(Path.Combine(Folder,run)); return;
        }
        if(c.op=="orbiton") { ((Stage5_MainBoss)boss).useOrbitSword=true;return; }
        if(c.op=="orbitoff") { ((Stage5_MainBoss)boss).useOrbitSword=false;return; }
        if(c.op=="skill")
        {
            if(Attacking || run!=null)throw new Exception("Previous action still running.");
            Position(c); Set(boss,"currentPhase",c.phase); Set(boss,"shownPhase",c.phase);
            Set(boss,"currentHp",boss.data.maxHp*(c.phase==1?1:c.phase==2?.5f:.2f));
            var skill=(BossSkillData)(c.index<0?Call(boss,"GetBasicAttackData"):Call(boss,"GetSkillData",c.index));
            if(skill==null)throw new Exception("Skill missing.");
            run=boss.name+"_P"+c.phase+"_S"+c.index+"_"+c.id; captures=hits=0;pending.Clear();finishedAt=-1;started=Time.time;nextCapture=Time.time;
            Directory.CreateDirectory(Path.Combine(Folder,run));
            var routine=(IEnumerator)typeof(MidBossController).GetMethod("ExecuteSkill",Flags).Invoke(boss,new object[]{skill,c.index});
            Set(boss,"runningSkill",boss.StartCoroutine(routine)); return;
        }
    }
    static IEnumerator DesireRoutine(MonoBehaviour desire,int index)
    {
        try
        {
            object result=null;
            if(index==0) result=desire is Stage1_DesireSkill ? Call(desire,"Activate",new object[]{null}) : Call(desire,"Activate");
            else if(index==1) result=Call(desire,"Succeed");
            else if(index==2)
            {
                if(desire is Stage3_MainBossDesireSkill)result=Call(desire,"FailByTimeout");
                else if(desire is Stage4_MainBossDesireSkill)Set(desire,"end",Time.time);
                else result=Call(desire,"Fail");
            }
            else if(index==3)result=Call(desire,"UseFinisher");
            else if(index==4)result=Call(desire,"UseReturnAttack");
            if(result is IEnumerator routine)yield return routine;
            yield return null;
        }
        finally {specialRunning=false;Set(desire,"lastUseTime",1000000f);Set(desire,"nextUseTime",1000000f);Set(desire,"nextUse",1000000f);}
    }
    static void Position(Command c)
    {
        boss.transform.position=home;var rb=boss.GetComponent<Rigidbody2D>();if(rb!=null){rb.position=home;rb.linearVelocity=Vector2.zero;}
        Set(boss,"lookDirection",c.facing<0?-1:1);Set(boss,"isChasing",true);Call(boss,"ApplyFacingDirection");
        var p=home+new Vector3((c.facing<0?-1:1)*c.distance,0,0);
        var bc=boss.GetComponent<Collider2D>();var pc=player.GetComponent<Collider2D>();
        if(bc!=null && pc!=null)p.y=bc.bounds.min.y+(player.transform.position.y-pc.bounds.min.y)+.03f;
        p.y+=c.height;
        player.transform.position=p;var pr=player.GetComponent<Rigidbody2D>();if(pr!=null){pr.position=p;pr.linearVelocity=Vector2.zero;pr.gravityScale=c.height!=0?0:playerGravity;pr.constraints=c.height!=0?playerConstraints|RigidbodyConstraints2D.FreezePositionY:playerConstraints;}
        Physics2D.SyncTransforms();
        // Keep the normal camera and lighting, framing both combatants during the review.
        var camera=Camera.main;
        if(camera!=null){var follow=camera.GetComponent<CameraFollow2D>();if(follow!=null)follow.enabled=false;camera.transform.position=new Vector3(home.x+(c.facing<0?-1:1)*1.5f,home.y+1,-10);}
    }
    static IEnumerator ReviewPlayerAttackMovement()
    {
        var lines=new List<string>();var rb=player.GetComponent<Rigidbody2D>();
        var pad=InputSystem.AddDevice<Gamepad>("MovementReviewPad");
        var action=new InputAction("ReviewAttack",InputActionType.Button,pad.buttonNorth.path);
        var oldWeapon=player.currentWeapon;bool wasPaused=(bool)Get(player,"isKeyboardControlPaused");
        var start=player.transform.position;
        boss.enabled=false;boss.StopAllCoroutines();boss.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
        Set(player,"isKeyboardControlPaused",false);
        action.started+=player.OnAttack;action.Enable();
        try
        {
            foreach(var weapon in new[]{WeaponType.Sword,WeaponType.Dagger,WeaponType.Blunt,WeaponType.Axe,WeaponType.Scythe})
            foreach(int facing in new[]{-1,1})
            {
                rb.position=start;rb.linearVelocity=Vector2.zero;
                player.currentWeapon=weapon;Set(player,"moveInput",new Vector2(facing,0));Set(player,"lastAttackTime",-999f);
                yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();
                InputSystem.QueueStateEvent(pad,new GamepadState().WithButton(GamepadButton.North));
                yield return null;
                InputSystem.QueueStateEvent(pad,new GamepadState());
                yield return null;
                bool accepted=player.AttackMovementLocked;float origin=rb.position.x,maxSlide=0,until=Time.time+5f;
                while(player.AttackMovementLocked && Time.time<until)
                {
                    maxSlide=Mathf.Max(maxSlide,Mathf.Abs(rb.position.x-origin));yield return new WaitForFixedUpdate();
                }
                bool released=!player.AttackMovementLocked;float stoppedX=rb.position.x;
                for(int i=0;i<4;i++)yield return new WaitForFixedUpdate();
                float resumed=(rb.position.x-stoppedX)*facing;
                lines.Add(weapon+" facing="+facing+" accepted="+accepted+" slide="+maxSlide.ToString("0.00000")+" released="+released+" resumed="+resumed.ToString("0.000")+" PASS="+(accepted&&released&&maxSlide<.025f&&resumed>.01f));
                Set(player,"moveInput",Vector2.zero);yield return new WaitForSeconds(.15f);
            }
        }
        finally
        {
            action.Dispose();InputSystem.RemoveDevice(pad);player.currentWeapon=oldWeapon;
            Set(player,"moveInput",Vector2.zero);Set(player,"isKeyboardControlPaused",wasPaused);
            File.WriteAllLines(Path.Combine(Folder,"player_attack_movement_review.txt"),lines);
        }
    }
    static IEnumerator ReviewPlayerRunInterpolation()
    {
        var rb=player.GetComponent<Rigidbody2D>();var start=rb.position;
        var original=rb.interpolation;var lines=new List<string>{"mode,time,playerX,cameraX"};
        boss.enabled=false;boss.StopAllCoroutines();boss.GetComponent<Rigidbody2D>().simulated=false;
        var camera=Camera.main;var follow=camera!=null?camera.GetComponent<CameraFollow2D>():null;
        if(follow!=null)follow.enabled=true;
        bool paused=(bool)Get(player,"isKeyboardControlPaused");Set(player,"isKeyboardControlPaused",false);
        try
        {
            foreach(var mode in new[]{RigidbodyInterpolation2D.None,RigidbodyInterpolation2D.Interpolate})
            {
                rb.position=start;rb.linearVelocity=Vector2.zero;rb.interpolation=mode;
                Set(player,"moveInput",Vector2.right);float until=Time.time+1.15f,warmup=Time.time+.2f;
                while(Time.time<until)
                {
                    yield return new WaitForEndOfFrame();
                    if(Time.time>=warmup)lines.Add(mode+","+Time.time.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+player.transform.position.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture)+","+camera.transform.position.x.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
                }
            }
        }
        finally
        {
            rb.interpolation=original;Set(player,"moveInput",Vector2.zero);Set(player,"isKeyboardControlPaused",paused);
            File.WriteAllLines(Path.Combine(Folder,"player_run_interpolation.csv"),lines);
        }
    }
    static string Clip() => animator!=null?string.Join("+",animator.GetCurrentAnimatorClipInfo(0).Select(c=>c.clip.name)):"";
    static void HitObserved(IBossSkillContext context,BossAttackStepData step,Vector2 center,Collider2D collider)
    {
        if(run==null || boss==null || context.BossTransform!=boss.transform)return;
        var camera=Camera.main;
        Vector2 actualSize=BossSkillExecutor.LastObservedBounds.size;
        var visual=boss.GetComponentInChildren<BossClipEffects>();
        var art=visual!=null && visual.weapon!=null ? visual.weapon.GetComponent<SpriteRenderer>() : null;
        if(boss is Stage4_MainBoss)art=boss.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.name=="Hand_F" || s.name=="Hand_B").OrderBy(s=>Vector2.Distance(BossEffects.VisibleBounds(s).center,center)).FirstOrDefault();
        Bounds artBounds=art!=null?BossEffects.VisibleBounds(art):new Bounds();
        pending.Add(new Hit{artName=art!=null?art.name:"",artMin=artBounds.min,artMax=artBounds.max,step=step.stepName,action=step.actionType.ToString(),center=center,size=actualSize,playerMin=player!=null?(Vector2)player.GetComponent<Collider2D>().bounds.min:Vector2.zero,playerMax=player!=null?(Vector2)player.GetComponent<Collider2D>().bounds.max:Vector2.zero,collider=collider!=null?collider.name:"none",time=Time.time-started,
            minViewport=camera!=null?(Vector2)camera.WorldToViewportPoint(center-actualSize*.5f):Vector2.zero,
            maxViewport=camera!=null?(Vector2)camera.WorldToViewportPoint(center+actualSize*.5f):Vector2.zero});hits++;
    }
    static IEnumerator Capture()
    {
        captureBusy=true;string current=run;
        yield return new WaitForEndOfFrame();
        if(current!=null && current==run)
        {
            Texture2D screen=null,small=null;RenderTexture rt=null;var old=RenderTexture.active;
            try
            {
                screen=ScreenCapture.CaptureScreenshotAsTexture();
                if(screen!=null)
                {
                    int w=960,h=Mathf.RoundToInt(w*(float)screen.height/screen.width);rt=RenderTexture.GetTemporary(w,h,0);
                    Graphics.Blit(screen,rt);RenderTexture.active=rt;small=new Texture2D(w,h,TextureFormat.RGB24,false);small.ReadPixels(new Rect(0,0,w,h),0,0);small.Apply();
                    string file=captures.ToString("D4")+".jpg";File.WriteAllBytes(Path.Combine(Folder,current,file),small.EncodeToJPG(85));
                    var frame=new Frame{file=file,clip=Clip(),elapsed=Time.time-started,normalized=animator.GetCurrentAnimatorStateInfo(0).normalizedTime,bossPosition=boss.transform.position,playerPosition=player.transform.position,hits=pending.ToArray(),fxVisible=boss.GetComponentInChildren<BossClipEffects>()?.VisibleCount??0};
                    var layer=animator.GetComponent<MainBoss2WeaponLayer>();if(layer!=null){frame.weaponLayer=layer.weapon.sortingLayerName;frame.weaponFront=layer.front;}
                    File.AppendAllText(Path.Combine(Folder,current,"frames.jsonl"),JsonUtility.ToJson(frame)+"\n");pending.Clear();captures++;
                }
            }
            finally {RenderTexture.active=old;if(rt!=null)RenderTexture.ReleaseTemporary(rt);if(screen!=null)UnityEngine.Object.Destroy(screen);if(small!=null)UnityEngine.Object.Destroy(small);}
        }
        captureBusy=false;
    }
    static void Status()
    {
        var s=new State{commandId=SessionState.GetString("BossLiveReview.command",""),scene=SceneManager.GetActiveScene().path,playing=Application.isPlaying,error=error,run=run,time=Time.time,captures=captures,hits=hits};
        if(boss!=null){s.interpolation=boss.GetComponent<Rigidbody2D>().interpolation.ToString();s.bossId=Get(boss,"bossId") as string;s.boss=boss.name;s.attacking=Attacking;s.bossPosition=boss.transform.position;s.skills=boss.data.skills.Count;s.clip=Clip();s.hp=(float)Get(boss,"currentHp");s.normalized=animator!=null?animator.GetCurrentAnimatorStateInfo(0).normalizedTime:0;}
        if(player!=null)s.playerPosition=player.transform.position;
        File.WriteAllText(Path.Combine(Folder,"status.json"),JsonUtility.ToJson(s,true));
    }
}
#endif
