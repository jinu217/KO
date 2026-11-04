#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Read-only measurements of authored weapon poses against a fixed rest floor.
public static class BossAttackHeightReview
{
    const string Folder="C:/Users/User/Documents/Codex/2026-10-26/new-chat-3/work/";
    static void Backup(string path)
    {
        string destination=Folder+"height_before/"+path;
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        if(!File.Exists(destination))File.Copy(path,destination);
    }
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        foreach(var name in new[]{"MainBoss3_Skill2","MainBoss3_Skill3"})
            Backup("Assets/Animations/3/MainBoss 3/"+name+".anim");
        MainBoss3_AnimGenerators.RebuildThrustAim();
        RefreshTimings();Report();
    }
    public static void RefreshTimings()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        foreach(int stage in new[]{1,2,3}) foreach(string path in new[]{
            $"Assets/Prefabs/Stage{stage}_Enemy/Stage{stage}_MainBoss.prefab",$"Assets/Prefab/{stage}/MainBoss {stage}.prefab"})
        {
            Backup(path);var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var fx=root.GetComponentInChildren<BossClipEffects>(true);
                foreach(var track in fx.tracks)
                {
                    float oldTime,newTime;
                    if(stage==1 && track.clip.name.Contains("Attack")){oldTime=19f/12;newTime=24f/12;}
                    else if(track.clip.name=="MainBoss2_Attack"){oldTime=20f/12;newTime=16f/12;}
                    else if(track.clip.name=="MainBoss2_Skill2"){oldTime=15f/12;newTime=14f/12;}
                    else if(track.clip.name=="MainBoss3_Attack"){oldTime=.34f;newTime=.50f;}
                    else continue;
                    foreach(var cue in track.cues)if(cue.style<=BossClipEffects.Style.Punch && Mathf.Abs(cue.time-oldTime)<.025f)cue.time=newTime;
                }
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
    }
    public static void LowerFirstFinisher()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        foreach(var guid in AssetDatabase.FindAssets("MainBoss1 t:AnimationClip"))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool finisher=clip.name=="MainBoss1_skill1" || clip.name=="MainBoss1_Prepared_skill1" || clip.name=="MainBoss1_Prepared_skill1_Phase3";
            if(!finisher)continue;
            if(File.Exists(Folder+"height_before/"+path))continue; // Never accumulate a second correction.
            Backup(path);
            var binding=AnimationUtility.GetCurveBindings(clip).Single(b=>b.path=="Body/Shoulder_B" && b.propertyName.Contains("Euler") && b.propertyName.EndsWith(".z"));
            var original=AnimationUtility.GetEditorCurve(clip,binding);var curve=new AnimationCurve();
            for(int i=0;i<=Mathf.CeilToInt(clip.length*60);i++)
            {
                float t=Mathf.Min(clip.length,i/60f);
                float weight=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.83f,2f,t))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(2.35f,2.80f,t)));
                curve.AddKey(t,original.Evaluate(t)-18f*weight);
            }
            for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Linear);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Linear);}
            AnimationUtility.SetEditorCurve(clip,binding,curve);EditorUtility.SetDirty(clip);
        }
        AssetDatabase.SaveAssets();Report();
    }
    public static void Report()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Edit Mode required");
        var report=new StringBuilder("stage,clip,time,floor,minX,maxX,minY,maxY,tipX,tipY,shoulder,bladeAngle\n");
        foreach(int stage in new[]{1,2,3,5})
        {
            var scene=EditorSceneManager.OpenPreviewScene($"Assets/Scenes/Stage{stage}/Stage{stage}-6.unity");
            var root=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<MainBossController>(true)).Single().gameObject;
            try
            {
                var a=root.GetComponentInChildren<Animator>(true);
                var fx=root.GetComponent<BossEffects>();
                var blade=fx.weaponAnchor.GetComponent<SpriteRenderer>();
                var shoulder=a.GetComponentsInChildren<Transform>().Single(t=>t.name=="Shoulder_B");
                var clips=a.runtimeAnimatorController.animationClips.Distinct();
                var idle=clips.First(c=>c.name.EndsWith("_Idle"));
                idle.SampleAnimation(a.gameObject,0);
                float floor=a.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.name=="Foot_F"||s.name=="Foot_B").Min(s=>BossEffects.VisibleBounds(s).min.y);
                foreach(var clip in clips.Where(c=>c.name.Contains("Attack")||c.name.ToLowerInvariant().Contains("skill")))
                {
                    for(int i=0;i<=Mathf.CeilToInt(clip.length*60);i++)
                    {
                        float time=Mathf.Min(i/60f,clip.length);clip.SampleAnimation(a.gameObject,time);
                        var b=BossEffects.VisibleBounds(blade);
                        var tip=blade.sprite.vertices.Select(v=>blade.transform.TransformPoint(v)).OrderByDescending(v=>(v-blade.transform.position).sqrMagnitude).First();
                        report.AppendLine(string.Join(",",new[]{stage.ToString(),clip.name,time.ToString("R"),floor.ToString("R"),(b.min.x-root.transform.position.x).ToString("R"),(b.max.x-root.transform.position.x).ToString("R"),(b.min.y-floor).ToString("R"),(b.max.y-floor).ToString("R"),(tip.x-root.transform.position.x).ToString("R"),(tip.y-floor).ToString("R"),shoulder.localEulerAngles.z.ToString("R"),blade.transform.eulerAngles.z.ToString("R")}));
                    }
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }
        File.WriteAllText(Folder+"boss_attack_height_samples.csv",report.ToString());
    }
}
#endif
