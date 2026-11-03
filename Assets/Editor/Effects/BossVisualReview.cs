#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;

// Read-only pose capture. Runtime collision/conditional execution is a separate test.
public static class BossVisualReview
{
    const string Folder = "C:/Users/User/Documents/Codex/2026-10-26/new-chat-3/work/boss_visual_review";
    [InitializeOnLoadMethod] static void Once()
    {
        if (SessionState.GetBool("BossVisualReview.1", false)) return;
        SessionState.SetBool("BossVisualReview.1", true);
        EditorApplication.delayCall += Run;
    }
    [MenuItem("Tools/Effects/Bosses/Capture All Motion Frames")]
    public static void Run() => Capture(null);
    public static void Capture(string[] only)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Directory.CreateDirectory(Folder);
        var report = new System.Text.StringBuilder();
        var material = new Material(Shader.Find("Sprites/Default"));
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[]{"Assets/Prefabs"}))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || asset.GetComponent<MidBossController>() == null) continue;
            GameObject root = null, cameraObject = null;
            RenderTexture rt = null; Texture2D tile = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(path);
                var animator = root.GetComponentInChildren<Animator>(true);
                var fx = root.GetComponent<BossEffects>();
                var boss = root.GetComponent<MidBossController>();
                if (animator == null || fx == null) continue;
                fx.motionAnimator = animator;
                var sprites = animator.GetComponentsInChildren<SpriteRenderer>(true);
                // Unlit capture preserves sprite/pose/sorting; lighting is deliberately excluded.
                foreach (var sr in sprites) sr.sharedMaterial = material;
                var clips = animator.runtimeAnimatorController.animationClips.Distinct().OrderBy(c=>c.name).ToArray();
                cameraObject = new GameObject("Review Camera");
                SceneManager.MoveGameObjectToScene(cameraObject, root.scene);
                var camera = cameraObject.AddComponent<Camera>(); camera.scene = root.scene;
                camera.orthographic = true; camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.16f,.16f,.18f); camera.nearClipPlane=.01f; camera.farClipPlane=200;
                rt = new RenderTexture(256,256,24); camera.targetTexture=rt;
                tile = new Texture2D(256,256,TextureFormat.RGB24,false);
                int row = 0;
                foreach (var clip in clips)
                {
                    if(only!=null && !only.Contains(clip.name))continue;
                    var times = new SortedSet<float>(Enumerable.Range(0,8).Select(i=>clip.length*i/7f));
                    for(int i=0;i<40;i++) { if(!fx.TryMotionImpact(clip,i,out float t)) break; times.Add(t); }
                    var chosen = times.ToArray();
                    var sheet = new Texture2D(256*chosen.Length,256,TextureFormat.RGB24,false);
                    Bounds all = new Bounds(); bool first=true;
                    foreach(float t in chosen)
                    {
                        clip.SampleAnimation(animator.gameObject,t);
                        foreach(var sr in sprites) if(sr.sprite!=null && sr.enabled)
                        { var b=BossEffects.VisibleBounds(sr); if(first){all=b;first=false;}else all.Encapsulate(b); }
                    }
                    camera.orthographicSize=Mathf.Max(2,Mathf.Max(all.size.x,all.size.y)*.58f);
                    camera.transform.position=new Vector3(all.center.x,all.center.y,-50);
                    for(int i=0;i<chosen.Length;i++)
                    {
                        clip.SampleAnimation(animator.gameObject,chosen[i]);
                        var clipFx=animator.GetComponent<BossClipEffects>();if(clipFx!=null)clipFx.Sample(clip,chosen[i],false,!(fx.mainBoss && fx.stage==5));
                        CombatFXScenePreview.SampleForReview(animator,clip,chosen[i]);
                        var trail=animator.GetComponent<BossWeaponSlashTrail>();if(trail!=null)trail.Sample(clip,chosen[i]);
                        camera.Render(); var old=RenderTexture.active; RenderTexture.active=rt;
                        tile.ReadPixels(new Rect(0,0,256,256),0,0); tile.Apply(); RenderTexture.active=old;
                        sheet.SetPixels(i*256,0,256,256,tile.GetPixels());
                    }
                    sheet.Apply();
                    string file=root.name.Replace("(Clone)","")+"__"+clip.name;
                    foreach(char c in Path.GetInvalidFileNameChars())file=file.Replace(c,'_');
                    File.WriteAllBytes(Path.Combine(Folder,file+".png"),sheet.EncodeToPNG());
                    report.AppendLine(file+" | "+string.Join(",",chosen.Select(t=>t.ToString("0.###"))));
                    UnityEngine.Object.DestroyImmediate(sheet); row++;
                }
                report.AppendLine("COUNT "+root.name+" "+row);
            }
            catch(Exception e){report.AppendLine("ERROR "+path+" "+e);}
            finally
            {
                CombatFXScenePreview.EndReview();
                if(cameraObject!=null)UnityEngine.Object.DestroyImmediate(cameraObject);
                if(rt!=null)UnityEngine.Object.DestroyImmediate(rt);
                if(tile!=null)UnityEngine.Object.DestroyImmediate(tile);
                if(root!=null)PrefabUtility.UnloadPrefabContents(root);
            }
        }
        UnityEngine.Object.DestroyImmediate(material);
        File.WriteAllText(Path.Combine(Folder,"index.txt"),report.ToString());
        Debug.Log("Boss visual capture complete: "+Folder);
    }
}
// Targeted authoring operations. Existing unrelated clips and scene overrides are preserved.
public static class BossMotionPolish
{
    const string BackupRoot="C:/Users/User/Documents/Codex/2026-10-26/new-chat-3/work/post_panel_before/";
    static void Backup(string path)
    { string p=BackupRoot+path;Directory.CreateDirectory(Path.GetDirectoryName(p));if(!File.Exists(p))File.Copy(path,p); }
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        ConfigureTown();
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/2/MainBoss 2/MainBoss2_Buff.anim");
        Backup(AssetDatabase.GetAssetPath(clip));GroundBuff(clip);EditorUtility.SetDirty(clip);
        foreach(string path in new[]{"Assets/Prefabs/Stage5_Enemy/Stage5_MainBoss.prefab","Assets/Prefab/5/MainBoss 5.prefab"})
        {
            Backup(path);var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var fx=root.GetComponentInChildren<BossClipEffects>(true);
                foreach(var track in fx.tracks)
                    if(track.clip!=null && track.clip.name.ToLowerInvariant().StartsWith("mainboss5_skill3"))
                        track.cues=track.cues.Where(c=>c.style!=BossClipEffects.Style.Dash && c.style!=BossClipEffects.Style.Dust).ToArray();
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        foreach(var guid in AssetDatabase.FindAssets("MainBoss4_skill2 t:AnimationClip"))Backup(AssetDatabase.GUIDToAssetPath(guid));
        Backup("Assets/Animations/4/MainBoss 4/MainBoss4_AC.controller");
        MainBoss4_AnimGenerators.Skill2Menu();
        AssetDatabase.SaveAssets();
        Debug.Log("[Motion Polish] Town idles, grounded Main2 buff, Main4 air punch, Main5 flight trail updated.");
    }
    static UnityEditor.Animations.AnimatorController Controller(string name)
    { string p=$"Assets/Animations/NPC/{name}/{name}_AC.controller";Backup(p);return AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(p); }
    static void ConfigureTown()
    {
        var smith=Controller("Smith");var sm=smith.layers[0].stateMachine;
        UnityEditor.Animations.AnimatorState Find(string n)=>sm.states.Single(s=>s.state.name==n).state;
        var idle=Find("Smith_Idle");var laugh=Find("Smith_Laugh");var stretch=Find("Smith_Stretch");
        var pause=sm.states.FirstOrDefault(s=>s.state.name=="Smith_IdleBeforeStretch").state;
        if(pause==null)pause=sm.AddState("Smith_IdleBeforeStretch",new Vector3(250,350));
        pause.motion=idle.motion;pause.writeDefaultValues=idle.writeDefaultValues;
        void Transition(UnityEditor.Animations.AnimatorState from,UnityEditor.Animations.AnimatorState to,float loops)
        {
            foreach(var t in from.transitions)from.RemoveTransition(t);
            var tr=from.AddTransition(to);tr.hasExitTime=true;tr.exitTime=loops;tr.hasFixedDuration=true;tr.duration=.18f;tr.canTransitionToSelf=false;
        }
        Transition(idle,laugh,3);Transition(laugh,pause,1);Transition(pause,stretch,2);Transition(stretch,idle,1);
        sm.defaultState=idle;EditorUtility.SetDirty(smith);
        var buffer=Controller("Buffer");buffer.layers[0].stateMachine.defaultState=buffer.layers[0].stateMachine.states.Single(s=>s.state.name=="Buffer_RaiseBodySway").state;
        EditorUtility.SetDirty(buffer);
        var merchant=Controller("Merchant");var state=merchant.layers[0].stateMachine.states.Single(s=>s.state.name=="Merchant_IdleCoinPose").state;
        merchant.layers[0].stateMachine.defaultState=state;
        foreach(var t in state.transitions)state.RemoveTransition(t);
        var repeat=state.AddTransition(state);repeat.hasExitTime=true;repeat.exitTime=1;repeat.hasFixedDuration=true;repeat.duration=.20f;repeat.canTransitionToSelf=true;
        EditorUtility.SetDirty(merchant);
    }
    public static void ValidateTown()
    {
        var report=new System.Text.StringBuilder();
        foreach(string name in new[]{"Smith","Buffer","Merchant"})
        {
            var root=PrefabUtility.LoadPrefabContents("Assets/Prefab/NPC/"+name+".prefab");
            var graph=PlayableGraph.Create("TownMotionValidation");
            try
            {
                var a=root.GetComponentInChildren<Animator>(true);
                var playable=UnityEngine.Animations.AnimatorControllerPlayable.Create(graph,a.runtimeAnimatorController);
                var output=UnityEngine.Animations.AnimationPlayableOutput.Create(graph,"NPC",a);output.SetSourcePlayable(playable);
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);graph.Play();string previous="";
                for(int i=0;i<2400;i++)
                {
                    graph.Evaluate(1f/30);var info=playable.GetCurrentAnimatorClipInfo(0);
                    string current=string.Join("+",info.Select(c=>c.clip.name));
                    if(current!=previous){report.AppendLine(name+" "+(i/30f).ToString("0.00")+" "+current);previous=current;}
                }
            }
            finally{graph.Destroy();PrefabUtility.UnloadPrefabContents(root);}
        }
        File.WriteAllText("C:/Users/User/Documents/Codex/2026-10-26/new-chat-3/work/town_npc_motion_validation.txt",report.ToString());
    }
    public static void GroundBuff(AnimationClip clip)
    {
        var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Stage2_Enemy/Stage2_MainBoss.prefab");
        var source=UnityEngine.Object.Instantiate(clip);
        try
        {
            var animator=root.GetComponentInChildren<Animator>(true);var visual=animator.transform;
            var body=visual.Find("Body");var feet=new[]{visual.Find("Body/Bottom/Thigh_F/Calf_F/Foot_F"),visual.Find("Body/Bottom/Thigh_B/Calf_B/Foot_B")};
            source.SampleAnimation(animator.gameObject,0);
            var targets=feet.Select(t=>(Vector2)t.position).ToArray();var rotations=feet.Select(t=>t.eulerAngles.z).ToArray();
            var soles=feet.Select(t=>BossEffects.VisibleBounds(t.GetComponent<SpriteRenderer>()).min.y).ToArray();
            var signs=feet.Select(t=>{
                Vector2 h=t.parent.parent.position,k=t.parent.position,a=t.position,d=a-h;
                return Mathf.Sign(d.x*(k.y-h.y)-d.y*(k.x-h.x));
            }).ToArray();
            var curves=new Dictionary<string,List<Keyframe>>();
            void Record(string path,string prop,float time,float value,bool angle=false)
            {
                string key=path+"|"+prop;if(!curves.TryGetValue(key,out var keys))curves[key]=keys=new List<Keyframe>();
                if(angle && keys.Count>0)value=keys[keys.Count-1].value+Mathf.DeltaAngle(keys[keys.Count-1].value,value);
                keys.Add(new Keyframe(time,value));
            }
            int samples=Mathf.CeilToInt(clip.length*60);
            for(int i=0;i<=samples;i++)
            {
                float time=clip.length*i/samples;source.SampleAnimation(animator.gameObject,time);
                float lift=0;for(int j=0;j<2;j++)lift+=feet[j].position.y-targets[j].y;
                body.position+=Vector3.down*(lift*.5f);
                // Lower the hips only as far as necessary to reach the planted ankles.
                for(int tries=0;tries<80;tries++)
                {
                    bool reachable=true;
                    for(int j=0;j<2;j++)
                    {
                        var ankle=feet[j];var knee=ankle.parent;var hip=knee.parent;
                        float reach=Vector2.Distance(hip.position,knee.position)+Vector2.Distance(knee.position,ankle.position);
                        if(Vector2.Distance(hip.position,targets[j])>reach-.0001f)reachable=false;
                    }
                    if(reachable)break;body.position+=Vector3.down*.01f;
                }
                Record("Body","m_LocalPosition.y",time,body.localPosition.y);
                for(int j=0;j<2;j++)
                {
                    var ankle=feet[j];var knee=ankle.parent;var hip=knee.parent;
                    Vector2 h=hip.position,k=knee.position,a=ankle.position,target=targets[j];
                    float l1=Vector2.Distance(h,k),l2=Vector2.Distance(k,a),d=Vector2.Distance(h,target);
                    var axis=(target-h).normalized;float along=(l1*l1-l2*l2+d*d)/(2*Mathf.Max(.0001f,d));
                    float height=Mathf.Sqrt(Mathf.Max(0,l1*l1-along*along));
                    Vector2 wantedKnee=h+axis*along+new Vector2(-axis.y,axis.x)*height*signs[j];
                    float delta=Vector2.SignedAngle(k-h,wantedKnee-h);hip.rotation=Quaternion.Euler(0,0,hip.eulerAngles.z+delta);
                    delta=Vector2.SignedAngle((Vector2)ankle.position-(Vector2)knee.position,target-(Vector2)knee.position);
                    knee.rotation=Quaternion.Euler(0,0,knee.eulerAngles.z+delta);ankle.rotation=Quaternion.Euler(0,0,rotations[j]);
                    foreach(var t in new[]{hip,knee,ankle})Record(AnimationUtility.CalculateTransformPath(t,visual),"localEulerAnglesRaw.z",time,t.localEulerAngles.z,true);
                }
            }
            foreach(var pair in curves)
            {
                var parts=pair.Key.Split('|');var curve=new AnimationCurve(pair.Value.ToArray());
                for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
                AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(parts[0],typeof(Transform),parts[1]),curve);
            }
            float error=0;
            for(int i=0;i<=samples*2;i++)
            {
                clip.SampleAnimation(animator.gameObject,clip.length*i/(samples*2));
                for(int j=0;j<2;j++)error=Mathf.Max(error,Mathf.Abs(BossEffects.VisibleBounds(feet[j].GetComponent<SpriteRenderer>()).min.y-soles[j]));
            }
            File.WriteAllText("C:/Users/User/Documents/Codex/2026-10-26/new-chat-3/work/main2_buff_ground_error.txt",error.ToString("R"));
            if(error>.02f)throw new Exception("Grounded buff foot error: "+error);
        }
        finally{UnityEngine.Object.DestroyImmediate(source);PrefabUtility.UnloadPrefabContents(root);}
    }
}

#endif
