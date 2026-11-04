#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
public static class BossWeaponSlashTrailSetup
{
    public static void Apply()
    {
        var report=new StringBuilder();
        foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/Prefabs"}))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(asset.GetComponent<MidBossController>()==null)continue;
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var fx=root.GetComponent<BossEffects>();var animator=root.GetComponentInChildren<Animator>(true);
                if(fx==null || fx.weaponAnchor==null || fx.mainBoss && fx.stage==4)continue;
                var transforms=root.GetComponentsInChildren<Transform>(true);
                var components=root.GetComponentsInChildren<Component>(true).Where(c=>c!=null && !(c is Transform) && !(c is BossWeaponSlashTrail)).ToArray();
                var snapshots=components.Select(c=>EditorJsonUtility.ToJson(c)).ToArray();
                var positions=transforms.Select(t=>t.localPosition).ToArray();var rotations=transforms.Select(t=>t.localRotation).ToArray();var scales=transforms.Select(t=>t.localScale).ToArray();
                var driver=animator.GetComponent<BossWeaponSlashTrail>()??animator.gameObject.AddComponent<BossWeaponSlashTrail>();
                driver.source=fx;driver.animator=animator;driver.weapon=fx.weaponAnchor;
                var tracks=new List<BossWeaponSlashTrail.Track>();
                var blade=fx.weaponAnchor.GetComponent<SpriteRenderer>();
                foreach(var clip in animator.runtimeAnimatorController.animationClips.Distinct())
                {
                    string n=clip.name.ToLowerInvariant();
                    if(!n.Contains("attack") && !n.Contains("skill") && !n.Contains("combo"))continue;
                    if(n.Contains("miss") || n.Contains("buff") || n.Contains("dskill") || n.Contains("groggy") || fx.mainBoss && fx.stage==5 && n.Contains("skill1"))continue;
                    int count=Mathf.CeilToInt(clip.length*48)+1;
                    var centers=new Vector3[count];var tips=new Vector3[count];var angles=new float[count];
                    for(int i=0;i<count;i++)
                    {
                        clip.SampleAnimation(animator.gameObject,Mathf.Min(i/48f,clip.length));
                        centers[i]=animator.transform.InverseTransformPoint(blade.bounds.center);
                        tips[i]=animator.transform.InverseTransformPoint(blade.bounds.center+fx.weaponAnchor.TransformVector(Vector3.up));
                        var axis=tips[i]-centers[i];angles[i]=Mathf.Atan2(axis.y,axis.x)*Mathf.Rad2Deg;
                    }
                    var windows=new List<Vector2>();int start=-1,sign=0;float total=0;
                    for(int i=1;i<=count;i++)
                    {
                        float delta=i<count?Mathf.DeltaAngle(angles[i-1],angles[i]):0;
                        int direction=Mathf.Abs(delta)>=2 && Mathf.Abs(delta)<100?Math.Sign(delta):0;
                        if(direction==0 || direction!=sign)
                        {
                            if(start>=0 && total>=165)windows.Add(new Vector2(start/48f,Mathf.Min((i-1)/48f,clip.length)));
                            start=direction!=0?i-1:-1;total=0;sign=direction;
                        }
                        if(direction!=0)total+=Mathf.Abs(delta);
                    }
                    // Authored spin windows also cover 2.5D turns whose projected blade angle reverses.
                    if(fx.mainBoss && fx.stage==5 && n=="mainboss5_attack")windows=new List<Vector2>{new Vector2(8/12f,20/12f)};
                    if(fx.mainBoss && fx.stage==5 && n.Contains("skill3_follow"))windows=new List<Vector2>{new Vector2(0,11/12f)};
                    if(windows.Count==0)continue;
                    tracks.Add(new BossWeaponSlashTrail.Track{clip=clip,centers=centers,tips=tips,windows=windows.ToArray()});
                    report.AppendLine(path+" | "+clip.name+" | "+string.Join(", ",windows.Select(w=>$"{w.x:0.###}–{w.y:0.###}s")));
                }
                driver.tracks=tracks.ToArray();driver.Clear();
                for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=positions[i];transforms[i].localRotation=rotations[i];transforms[i].localScale=scales[i];}
                
                string backup="C:/Users/User/Documents/Codex/2026-10-26/new-chat-3/work/slash_before/"+path;
                Directory.CreateDirectory(Path.GetDirectoryName(backup));if(!File.Exists(backup))File.Copy(path,backup);
                // Save a fresh, unsampled prefab so local references and animation flags remain intact.
                var clean=PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var cleanFx=clean.GetComponent<BossEffects>();var cleanAnimator=clean.GetComponentInChildren<Animator>(true);
                    var saved=cleanAnimator.GetComponent<BossWeaponSlashTrail>()??cleanAnimator.gameObject.AddComponent<BossWeaponSlashTrail>();
                    saved.source=cleanFx;saved.animator=cleanAnimator;saved.weapon=cleanFx.weaponAnchor;saved.tracks=tracks.ToArray();
                    if(cleanFx.weaponAnchor==null || cleanFx.motionAnimator==null)throw new Exception("Missing source references: "+path);
                    PrefabUtility.SaveAsPrefabAsset(clean,path);
                }
                finally{PrefabUtility.UnloadPrefabContents(clean);}
            }
            finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        AssetDatabase.SaveAssets();File.WriteAllText("Library/BossSlashTrails.txt",report.ToString());
    }
}
#endif
