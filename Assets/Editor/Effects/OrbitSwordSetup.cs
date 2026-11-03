#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
public static class OrbitSwordSetup
{
    [MenuItem("Tools/5/MainBoss5/Create Orbit Sword Spare")]
    public static void Apply()
    {
        const string path="Assets/Prefabs/Stage5_Enemy/Stage5_MainBoss_OrbitSpare.prefab";
        if(AssetDatabase.LoadAssetAtPath<GameObject>(path)!=null){Layers();return;}
        var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Stage5_Enemy/Stage5_MainBoss.prefab");
        try
        {
            root.name="Stage5_MainBoss_OrbitSpare";
            root.GetComponent<Stage5_MainBoss>().useOrbitSword=true;
            if(root.GetComponent<Stage5OrbitSword>()==null)root.AddComponent<Stage5OrbitSword>();
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
        Layers();
        AssetDatabase.SaveAssets();
    }
    [MenuItem("Tools/2/MainBoss2/Refresh Weapon Layers")]
    public static void Layers()
    {
        foreach(string path in new[]{"Assets/Prefabs/Stage2_Enemy/Stage2_MainBoss.prefab","Assets/Prefab/2/MainBoss 2.prefab"})
        {
            string backup="C:/Users/User/Documents/Codex/2026-10-26/new-chat-3/work/orbit_weapon_before/"+path;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(backup));if(!System.IO.File.Exists(backup))System.IO.File.Copy(path,backup);
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var animator=root.GetComponentInChildren<Animator>(true);
                var sprites=root.GetComponentsInChildren<SpriteRenderer>(true);
                var body=sprites.First(x=>x.name=="Body");var hand=sprites.First(x=>x.name=="Hand_B");var frontHand=sprites.First(x=>x.name=="Hand_F");var weapon=sprites.First(x=>x.name=="Axe");
                var layer=animator.GetComponent<MainBoss2WeaponLayer>() ?? animator.gameObject.AddComponent<MainBoss2WeaponLayer>();
                layer.weapon=weapon;layer.hand=hand;
                foreach(var clip in animator.runtimeAnimatorController.animationClips.Distinct())
                {
                    string cp=AssetDatabase.GetAssetPath(clip);string cb="C:/Users/User/Documents/Codex/2026-10-26/new-chat-3/work/orbit_weapon_before/"+cp;
                    System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(cb));if(!System.IO.File.Exists(cb))System.IO.File.Copy(cp,cb);
                    BakeLayerCurve(clip,animator);
                }
                // Reload the original rest transforms after sampling; save only layer setup.
                var original=AssetDatabase.LoadAssetAtPath<GameObject>(path);var originals=original.GetComponentsInChildren<Transform>(true);
                foreach(var t in root.GetComponentsInChildren<Transform>(true)){string key=AnimationUtility.CalculateTransformPath(t,root.transform);var src=originals.FirstOrDefault(x=>AnimationUtility.CalculateTransformPath(x,original.transform)==key);if(src!=null){t.localPosition=src.localPosition;t.localRotation=src.localRotation;t.localScale=src.localScale;}}
                layer.front=0;weapon.sortingLayerName=hand.sortingLayerName="Back";weapon.sortingOrder=-1;hand.sortingOrder=1;
                PrefabUtility.SaveAsPrefabAsset(root,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();
    }

    // Also used by the motion generator so rebuilding a clip keeps its draw order.
    public static void BakeLayerCurve(AnimationClip clip,Animator rig=null)
    {
        GameObject temporary=null;
        try
        {
            if(rig==null)
            {
                temporary=PrefabUtility.LoadPrefabContents("Assets/Prefab/2/MainBoss 2.prefab");
                rig=temporary.GetComponentInChildren<Animator>(true);
            }
            var sprites=rig.GetComponentsInChildren<SpriteRenderer>(true);
            var body=sprites.First(x=>x.name=="Body");var hand=sprites.First(x=>x.name=="Hand_B");
            var frontHand=sprites.First(x=>x.name=="Hand_F");var weapon=sprites.First(x=>x.name=="Axe");
                    var keys=new List<Keyframe>();string name=clip.name.ToLowerInvariant();bool action=name.Contains("attack")||name.Contains("skill")||name.Contains("buff");
                    for(int i=0;i<=Mathf.CeilToInt(clip.length*24);i++)
                    {
                        float time=Mathf.Min(clip.length,i/24f);clip.SampleAnimation(rig.gameObject,time);
                        Bounds torso=body.bounds;Vector3 grip=weapon.transform.position;
                        bool twoHands=Vector2.Distance(hand.bounds.center,frontHand.bounds.center)<(hand.bounds.size.magnitude+frontHand.bounds.size.magnitude)*.40f;
                        bool crossing=false;Vector3 tip=weapon.transform.TransformPoint(new Vector3(weapon.sprite.bounds.center.x,weapon.sprite.bounds.max.y));
                        for(int n=0;n<=12;n++){Vector3 pt=Vector3.Lerp(grip,tip,n/12f);pt.z=torso.center.z;if(torso.Contains(pt)){crossing=true;break;}}
                        keys.Add(new Keyframe(time,action && time>.08f && time<clip.length-.12f && (twoHands||crossing)?1:0));
                    }
                    var curve=new AnimationCurve(keys.ToArray());for(int i=0;i<curve.length;i++){AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.Constant);AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.Constant);}
                    AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(MainBoss2WeaponLayer),"front"),curve);
        }
        finally{if(temporary!=null)PrefabUtility.UnloadPrefabContents(temporary);}
    }

}
#endif
