#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

// Opt-in review of the actual stage instance, using its normal skill coroutine.
// Commands only affect Play Mode instances. Original scenes/data are never saved.
[InitializeOnLoad]
public static class BossLiveReview
{
    const string Folder = "C:/Users/User/Documents/Codex/2026-10-26/new-chat-3/work/boss_live_review";
    const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    [Serializable] class Command { public string id, op, scene; public int index=-1, phase=1, facing=1; public float distance=3; }
    [Serializable] class State { public string commandId, scene, boss, clip, error, run; public bool playing, attacking; public float time, hp, normalized; public Vector3 bossPosition, playerPosition; public int skills, captures, hits; }
    [Serializable] class Frame { public string file, clip; public float elapsed, normalized; public Vector3 bossPosition, playerPosition; public Hit[] hits; }
    [Serializable] class Hit { public string step, action, collider; public Vector2 center, size, minViewport, maxViewport; public float time; }
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
            Directory.CreateDirectory(Folder);
            try
            {
                string file=Path.Combine(Folder,"command.json");
                if(File.Exists(file))
                {
                    var c=JsonUtility.FromJson<Command>(File.ReadAllText(file));
                    if(c!=null && c.id!=SessionState.GetString("BossLiveReview.command",""))
                    { SessionState.SetString("BossLiveReview.command",c.id); error=null; Execute(c); }
                }
                Status();
            }
            catch(Exception e) { error=e.ToString(); File.WriteAllText(Path.Combine(Folder,"error.txt"),error); }
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
        if(c.op=="scene")
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop play before opening another stage.");
            if(!c.scene.StartsWith("Assets/Scenes/Stage") || !c.scene.EndsWith(".unity"))throw new Exception("Stage path required.");
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
            if(boss==null || player==null)throw new Exception("Stage boss/player missing.");
            animator=boss.GetComponentInChildren<Animator>(); home=boss.transform.position;
            boss.StopAllCoroutines(); Set(boss,"isAttacking",false);
            boss.GetComponent<BossEffects>()?.EndSkill();
            var copy=UnityEngine.Object.Instantiate(boss.data); copy.hideFlags=HideFlags.DontSave; boss.data=copy;
            copy.basicAttack.cooldown=100000; foreach(var s in copy.skills)s.cooldown=100000;
            Set(boss,"lastBasicTime",Time.time); Set(boss,"lastSkillTimes",Enumerable.Repeat(Time.time,copy.skills.Count).ToArray());
            Set(boss,"initialCooldownsApplied",true); Set(boss,"playEncounterDialogue",false); Set(boss,"dialoguePaused",false);
            player.PauseKeyboardControl(); Time.timeScale=1;
            var status=player.GetComponent<PlayerStatus>(); if(status!=null)status.currentHp=100000;
            foreach(var component in boss.GetComponents<MonoBehaviour>())
                if(component!=null && component.GetType().Name.Contains("Desire"))
                { component.enabled=false; Set(component,"lastUseTime",1000000f);Set(component,"nextUseTime",1000000f);Set(component,"nextUse",1000000f); }
            Position(c);
            Selection.activeGameObject=boss.gameObject;
            return;
        }
        if(boss==null)throw new Exception("Prepare this stage first.");
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
        player.transform.position=p;var pr=player.GetComponent<Rigidbody2D>();if(pr!=null){pr.position=p;pr.linearVelocity=Vector2.zero;}
        Physics2D.SyncTransforms();
        // Keep the normal camera and lighting, framing both combatants during the review.
        var camera=Camera.main;
        if(camera!=null){var follow=camera.GetComponent<CameraFollow2D>();if(follow!=null)follow.enabled=false;camera.transform.position=new Vector3(home.x+(c.facing<0?-1:1)*1.5f,home.y+1,-10);if(boss is Stage4_MainBoss)camera.orthographicSize=3.5f;}
    }
    static string Clip() => animator!=null?string.Join("+",animator.GetCurrentAnimatorClipInfo(0).Select(c=>c.clip.name)):"";
    static void HitObserved(IBossSkillContext context,BossAttackStepData step,Vector2 center,Collider2D collider)
    {
        if(run==null || boss==null || context.BossTransform!=boss.transform)return;
        var camera=Camera.main;
        Vector2 actualSize=BossSkillExecutor.LastObservedBounds.size;
        pending.Add(new Hit{step=step.stepName,action=step.actionType.ToString(),center=center,size=actualSize,collider=collider!=null?collider.name:"none",time=Time.time-started,
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
                    var frame=new Frame{file=file,clip=Clip(),elapsed=Time.time-started,normalized=animator.GetCurrentAnimatorStateInfo(0).normalizedTime,bossPosition=boss.transform.position,playerPosition=player.transform.position,hits=pending.ToArray()};
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
        if(boss!=null){s.boss=boss.name;s.attacking=Attacking;s.bossPosition=boss.transform.position;s.skills=boss.data.skills.Count;s.clip=Clip();s.hp=(float)Get(boss,"currentHp");s.normalized=animator!=null?animator.GetCurrentAnimatorStateInfo(0).normalizedTime:0;}
        if(player!=null)s.playerPosition=player.transform.position;
        File.WriteAllText(Path.Combine(Folder,"status.json"),JsonUtility.ToJson(s,true));
    }
}
#endif
