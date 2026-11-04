#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using System.Text;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Explicit, one-shot retime preserves all current authored poses and references.
[InitializeOnLoad]
public static class BossPolishNovemberReview
{
    const string Folder="C:/Users/User/Documents/Codex/2026-10-26/new-chat-3/work/boss_polish_1103";
    static BossPolishNovemberReview()
    {
        EditorApplication.update+=()=>
        {
            if(Application.isPlaying && File.Exists(Folder+"/fxprobe.txt"))
            {
                string request=File.ReadAllText(Folder+"/fxprobe.txt");
                var boss=UnityEngine.Object.FindFirstObjectByType<MidBossController>();
                if(boss!=null && SessionState.GetString("FXProbe","")!=request)
                {SessionState.SetString("FXProbe",request);boss.StartCoroutine(ProbeFX(boss,request));}
            }
            if(Application.isPlaying && File.Exists(Folder+"/observe.txt"))
            {
                string request=File.ReadAllText(Folder+"/observe.txt");
                var sword=UnityEngine.Object.FindFirstObjectByType<Stage5OrbitSword>();
                if(sword!=null && sword.Active && SessionState.GetString("OrbitObserve","")!=request)
                {SessionState.SetString("OrbitObserve",request);sword.StartCoroutine(Observe(sword,request));}
            }
            if(Application.isPlaying || EditorApplication.isCompiling || !File.Exists(Folder+"/apply.txt"))return;
            string id=File.ReadAllText(Folder+"/apply.txt");
            if(SessionState.GetString("BossPolishNovember","")==id)return;
            SessionState.SetString("BossPolishNovember",id);
            try{Apply();File.WriteAllText(Folder+"/result.txt","DONE");}
            catch(Exception e){File.WriteAllText(Folder+"/result.txt",e.ToString());}
        };
    }
    static IEnumerator ProbeFX(MidBossController boss,string id)
    {
        var animator=boss.GetComponentInChildren<Animator>();
        var report=new StringBuilder();float end=Time.time+8,next=0;
        while(Time.time<end)
        {
            yield return new WaitForEndOfFrame();
            var clips=animator.GetCurrentAnimatorClipInfo(0);
            if(clips.Length==0 || !clips[0].clip.name.Contains("Attack"))continue;
            float t=animator.GetCurrentAnimatorStateInfo(0).normalizedTime*clips[0].clip.length;
            if(t<next)continue;next=t+.04f;
            report.AppendLine("TIME "+t);
            foreach(var sr in Resources.FindObjectsOfTypeAll<SpriteRenderer>())
                if(sr.enabled && sr.sprite!=null && (sr.name.Contains("FX") || sr.name.Contains("Slash")))
                    report.AppendLine(sr.name+" alpha="+sr.color.a+" shader="+sr.sharedMaterial.shader.name+" sprite="+sr.sprite.name+" scale="+sr.transform.lossyScale);
        }
        File.WriteAllText(Folder+"/fxprobe_"+id+".txt",report.ToString());
    }
    static IEnumerator Observe(Stage5OrbitSword sword,string id)
    {
        var fx=sword.GetComponent<BossEffects>();
        var report=new StringBuilder("time,x,y,angle\n");
        float deadline=Time.time+3;
        while(sword!=null && sword.Active && Time.time<deadline)
        {
            yield return new WaitForEndOfFrame();
            var clips=fx.motionAnimator.GetCurrentAnimatorClipInfo(0);
            if(clips.Length==0)continue;
            float t=fx.motionAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime*clips[0].clip.length;
            Vector3 p=sword.transform.InverseTransformPoint(fx.weaponAnchor.GetComponent<SpriteRenderer>().bounds.center);
            report.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,"{0},{1},{2},{3}\n",t,p.x,p.y,fx.weaponAnchor.eulerAngles.z);
            if(t>=1.5f)break;
        }
        File.WriteAllText(Folder+"/orbit_"+id+".csv",report.ToString());
    }
    static float Map(float t)
    {
        float start=22f/12f,end=30f/12f;
        return t<=start?t:t<end?start+(t-start)*7f/8f:t-1f/12f;
    }
    static void Apply()
    {
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/5/MainBoss 5/MainBoss5_Attack.anim");
        // Guard both duplicate application and an unexpected user edit.
        if(Mathf.Abs(clip.length-35f/12f)>.005f)throw new Exception("Unexpected Attack length: "+clip.length);
        foreach(var binding in AnimationUtility.GetCurveBindings(clip))
        {
            var curve=AnimationUtility.GetEditorCurve(clip,binding);
            var keys=curve.keys;
            for(int i=0;i<keys.Length;i++)
            {
                float old=keys[i].time;
                keys[i].time=Map(old);
                if(binding.type==typeof(Stage5MainBossPathPreview) && binding.propertyName=="frame")keys[i].value=Map(keys[i].value/12f)*12f;
                // Authored clips use sampled linear keys; preserve their interpolation after compression.
                if(old>22f/12f && old<30f/12f){keys[i].inTangent*=8f/7f;keys[i].outTangent*=8f/7f;}
            }
            curve.keys=keys;AnimationUtility.SetEditorCurve(clip,binding,curve);
        }
        foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            var keys=AnimationUtility.GetObjectReferenceCurve(clip,binding);
            for(int i=0;i<keys.Length;i++)keys[i].time=Map(keys[i].time);
            AnimationUtility.SetObjectReferenceCurve(clip,binding,keys);
        }
        var events=AnimationUtility.GetAnimationEvents(clip);
        foreach(var e in events)e.time=Map(e.time);
        AnimationUtility.SetAnimationEvents(clip,events);
        var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.stopTime=34f/12f;
        AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);
        foreach(string path in new[]{"Assets/Prefabs/Stage5_Enemy/Stage5_MainBoss.prefab","Assets/Prefabs/Stage5_Enemy/Stage5_MainBoss_OrbitSpare.prefab"})
        {
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var fx=root.GetComponentInChildren<BossClipEffects>(true);
                var track=fx!=null?fx.Find(clip):null;
                if(track!=null)foreach(var cue in track.cues)
                {float end=Map(cue.time+cue.duration);cue.time=Map(cue.time);cue.duration=end-cue.time;}
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
    }
}
#endif
