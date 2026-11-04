#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BossMotionSortingSetup
{
    const string BackupRoot="C:/Users/User/Documents/Codex/2026-10-26/new-chat-3/work/boss_sorting_before/";
    public static void Backup(string path)
    {
        if(string.IsNullOrEmpty(path)||!File.Exists(path)) return;
        string destination=BackupRoot+path;
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        if(!File.Exists(destination)) File.Copy(path,destination);
    }
    public static SpriteRenderer Weapon(Animator rig)
    {
        var fx=rig.GetComponentInParent<BossEffects>();
        if(fx!=null && fx.weaponAnchor!=null) return fx.weaponAnchor.GetComponent<SpriteRenderer>();
        var hand=rig.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r.name=="Hand_B");
        return hand==null?null:hand.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r!=hand);
    }
    public static bool Configure(Animator rig,int stage,bool main)
    {
        if(stage==5 || (main && (stage==2 || stage==4))) return false;
        if(rig.GetComponent<BossRearWeaponSorting>()!=null) return false;
        var hand=rig.GetComponentsInChildren<SpriteRenderer>(true).First(r=>r.name=="Hand_B");
        rig.gameObject.AddComponent<BossRearWeaponSorting>().Configure(hand,Weapon(rig),!main && (stage==3 || stage==4));
        return true;
    }
    [MenuItem("Tools/Effects/Bosses/뒷손 무기 모션별 레이어 갱신")]
    public static void ApplyAll()
    {
        var baked=new HashSet<AnimationClip>();
        var motionBefore=new Dictionary<string,string>();
        for(int stage=1;stage<=5;stage++) foreach(bool main in new[]{false,true})
        {
            string kind=main?"Main":"Mid";
            var paths=new List<string>{ $"Assets/Prefab/{stage}/{kind}Boss {stage}.prefab",$"Assets/Prefabs/Stage{stage}_Enemy/Stage{stage}_{kind}Boss.prefab" };
            if(stage==5 && main) paths.Add("Assets/Prefabs/Stage5_Enemy/Stage5_MainBoss_OrbitSpare.prefab");
            foreach(string path in paths)
            {
                if(AssetDatabase.LoadAssetAtPath<GameObject>(path)==null) continue;
                var root=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var rig=root.GetComponentInChildren<Animator>(true);
                    if(Configure(rig,stage,main)) { Backup(path); PrefabUtility.SaveAsPrefabAsset(root,path); }
                    foreach(var clip in rig.runtimeAnimatorController.animationClips.Distinct())
                        if(baked.Add(clip))
                        {
                            string cp=AssetDatabase.GetAssetPath(clip); Backup(cp);
                            motionBefore[cp]=File.ReadAllText(cp);
                            BakeClip(clip,rig,stage,main); EditorUtility.SetDirty(clip);
                        }
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }
        AssetDatabase.SaveAssets();
        // Some imported project clips have runtime transform curves edited independently
        // of their editor curves. Adding a flag makes Unity rebuild those runtime curves.
        // A sorting-only operation must keep the original motion/timing byte-for-byte.
        foreach(var entry in motionBefore)
        {
            string after=File.ReadAllText(entry.Key),restored=PreserveMotion(entry.Value,after);
            if(after==restored)continue;
            File.WriteAllText(entry.Key,restored,new System.Text.UTF8Encoding(false));
            AssetDatabase.ImportAsset(entry.Key,ImportAssetOptions.ForceUpdate);
        }
    }
    public static string PreserveMotion(string before,string after)
    {
        foreach(string field in new[]{"m_RotationCurves","m_CompressedRotationCurves","m_EulerCurves","m_PositionCurves","m_ScaleCurves","m_PPtrCurves","m_AnimationClipSettings","m_Events"})
        {
            string pattern=@"(?ms)^  "+field+@":.*?(?=^  \w+:|\z)";
            var original=System.Text.RegularExpressions.Regex.Match(before,pattern);
            if(original.Success) after=System.Text.RegularExpressions.Regex.Replace(after,pattern,m=>original.Value);
        }
        return after;
    }
    static bool Action(string name)
    {
        name=name.ToLowerInvariant();
        return name.Contains("attack")||name.Contains("combo")||name.Contains("skill")||name.Contains("buff")||name.Contains("dash");
    }
    public static void BakeGeneratedClip(AnimationClip clip,int stage,bool main)
    {
        var root=PrefabUtility.LoadPrefabContents($"Assets/Prefab/{stage}/{(main?"Main":"Mid")}Boss {stage}.prefab");
        try { BakeClip(clip,root.GetComponentInChildren<Animator>(true),stage,main); }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    // Use the same body-crossing / two-handed-grip convention as MainBoss2.
    static bool CrossesBody(SpriteRenderer body,SpriteRenderer hand,SpriteRenderer frontHand,SpriteRenderer weapon)
    {
        Bounds torso=body.bounds; torso.Expand(new Vector3(-torso.size.x*.12f,-torso.size.y*.10f,0));
        Vector3 point=hand.bounds.center; point.z=torso.center.z;
        bool crossing=torso.Contains(point);
        bool twoHands=frontHand!=null && Vector2.Distance(hand.bounds.center,frontHand.bounds.center)<(hand.bounds.size.magnitude+frontHand.bounds.size.magnitude)*.40f;
        if(weapon!=null && weapon.sprite!=null)
        {
            Vector3 grip=weapon.transform.position;
            Vector3 tip=weapon.transform.TransformPoint(new Vector3(weapon.sprite.bounds.center.x,weapon.sprite.bounds.max.y));
            for(int n=0;n<=16;n++) { point=Vector3.Lerp(grip,tip,n/16f); point.z=torso.center.z; if(torso.Contains(point)) { crossing=true; break; } }
        }
        return crossing||twoHands;
    }
    public static void SetFlag(AnimationClip clip,string path,Type type,string name,IEnumerable<Keyframe> keys)
    {
        var reduced=new List<Keyframe>(); var source=keys.ToArray();
        for(int i=0;i<source.Length;i++) if(i==0||i==source.Length-1||source[i].value!=source[i-1].value) reduced.Add(source[i]);
        var curve=new AnimationCurve(reduced.ToArray());
        for(int i=0;i<curve.length;i++) { AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Constant); AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Constant); }
        AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve(path,type,name),curve);
    }
    public static void BakeClip(AnimationClip clip,Animator source,int stage,bool main)
    {
        if(main && stage==2) return; // Already authored and verified.
        // Sample a disposable clone: never bake a sampled pose back into the prefab.
        var clone=UnityEngine.Object.Instantiate(source.gameObject);
        clone.hideFlags=HideFlags.HideAndDontSave;
        try
        {
            var rig=clone.GetComponent<Animator>();
            var sprites=clone.GetComponentsInChildren<SpriteRenderer>(true);
            var body=sprites.First(r=>r.name=="Body"); var hand=sprites.First(r=>r.name=="Hand_B");
            var frontHand=sprites.FirstOrDefault(r=>r.name=="Hand_F"); var weapon=Weapon(rig);
            bool action=Action(clip.name);
            var keys=new List<Keyframe>();
            // Preserve authored front intervals. Fill missing intervals, not the existing custom rules.
            string field=stage==5?"frontWeapon":"forceFront";
            string handPath=AnimationUtility.CalculateTransformPath(hand.transform,rig.transform);
            var type=stage==5?typeof(Stage5BossAnimationRelay):typeof(MainBoss4ArmLayerPreview);
            var existing=AnimationUtility.GetEditorCurve(clip,EditorCurveBinding.FloatCurve(stage==5?"":handPath,type,field));
            var weaponLayerBinding=AnimationUtility.GetCurveBindings(clip).FirstOrDefault(b=>b.type==typeof(SpriteRenderer)&&b.propertyName=="m_SortingLayerID"&&b.path.EndsWith("/Scythe"));
            var oldLayer=!main && stage==4 && weaponLayerBinding.type!=null?AnimationUtility.GetEditorCurve(clip,weaponLayerBinding):null;
            for(int i=0;i<=Mathf.CeilToInt(clip.length*60);i++)
            {
                float t=Mathf.Min(clip.length,i/60f);
                clip.SampleAnimation(rig.gameObject,t);
                bool front=action && t>=.08f && t<clip.length-.08f && CrossesBody(body,hand,frontHand,weapon);
                if(existing!=null && existing.Evaluate(t)>.5f) front=true;
                keys.Add(new Keyframe(t,front?1:0));
            }
            // A tangential graze at the body boundary must not cause a one-frame pop.
            // Keep genuine passes; close only short interior gaps in the sampled flag.
            for(int start=1;start<keys.Count-1;)
            {
                int end=start+1;while(end<keys.Count && keys[end].value==keys[start].value)end++;
                if(end<keys.Count && keys[start-1].value==keys[end].value && keys[end].time-keys[start].time<.075f)
                    for(int k=start;k<end;k++)keys[k]=new Keyframe(keys[k].time,keys[start-1].value);
                start=end;
            }
            if(stage==5)
            {
                bool flying=clip.name.IndexOf("skill1",StringComparison.OrdinalIgnoreCase)>=0;
                if(!flying) SetFlag(clip,"",type,field,keys);
                if(main && flying)
                {
                    // Each cross-body transfer alternates far/near; change depth at the side,
                    // before the blade crosses the torso, rather than halfway through overlap.
                    var orbitKeys=new List<Keyframe>{new Keyframe(0,0)};
                    for(int group=0;group<8;group++) orbitKeys.Add(new Keyframe((12+group*9)/12f,group%2==0?0:1));
                    orbitKeys.Add(new Keyframe(84/12f,0)); orbitKeys.Add(new Keyframe(clip.length,0));
                    SetFlag(clip,"",type,"frontBlade",orbitKeys);
                }
            }
            else if(main && stage==4)
            {
                foreach(string path in new[]{handPath,AnimationUtility.CalculateTransformPath(hand.transform.parent,rig.transform)}) SetFlag(clip,path,type,field,keys);
            }
            else
            {
                SetFlag(clip,"",typeof(BossRearWeaponSorting),"active",action
                    ? new[]{new Keyframe(0,0),new Keyframe(.08f,1),new Keyframe(Mathf.Max(.081f,clip.length-.08f),0),new Keyframe(clip.length,0)}
                    : new[]{new Keyframe(0,0),new Keyframe(clip.length,0)});
                SetFlag(clip,"",typeof(BossRearWeaponSorting),"front",keys);
                if(!main && stage==3 && weapon!=null && !clip.name.EndsWith("_Skill3",StringComparison.OrdinalIgnoreCase))
                    SetFlag(clip,AnimationUtility.CalculateTransformPath(weapon.transform,rig.transform),typeof(MidBoss3WeaponSortingPreview),"backLayer",new[]{new Keyframe(0,0),new Keyframe(clip.length,0)});
                if(oldLayer!=null && weapon!=null)
                {
                    // Legacy clips animated billion-sized IDs as floats. Retain their timings
                    // but route through the exact-ID helper, including an explicit rest reset.
                    string path=AnimationUtility.CalculateTransformPath(weapon.transform,rig.transform);
                    SetFlag(clip,path,typeof(MidBoss4WeaponLayerPreview),"front",oldLayer.keys.Select(k=>new Keyframe(k.time,k.value>0?1:0)));
                    AnimationUtility.SetEditorCurve(clip,weaponLayerBinding,null);
                }
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(clone); }
    }
}

// Generators that replace a whole clip via CopySerialized must retain the layer flags.
// Only fill missing bindings on the six rigs owned by the new helper.
sealed class BossMotionSortingSaveHook : AssetModificationProcessor
{
    static string[] OnWillSaveAssets(string[] paths)
    {
        foreach(string path in paths)
        {
            if(!path.EndsWith(".anim",StringComparison.OrdinalIgnoreCase)) continue;
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if(clip==null || AnimationUtility.GetCurveBindings(clip).Any(b=>b.type==typeof(BossRearWeaponSorting))) continue;
            var match=System.Text.RegularExpressions.Regex.Match(clip.name,@"^(Mid|Main)Boss\s*([1-5])_");
            if(!match.Success) continue;
            bool main=match.Groups[1].Value=="Main"; int stage=int.Parse(match.Groups[2].Value);
            if(stage==5 || main && (stage==2 || stage==4)) continue;
            BossMotionSortingSetup.BakeGeneratedClip(clip,stage,main);
        }
        return paths;
    }
}
#endif
