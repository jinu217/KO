#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
[InitializeOnLoad]
public static class Boss4SizingReview
{
    const string Output="C:/Users/User/Documents/Codex/2026-10-26/new-chat-3/work/boss4_geometry.txt";
    static Boss4SizingReview() { EditorApplication.delayCall += Once; }
    static void Once() { if(SessionState.GetBool("Boss4SizingReview.measure1",false)||EditorApplication.isPlayingOrWillChangePlaymode)return;SessionState.SetBool("Boss4SizingReview.measure1",true);Measure(); }
    [MenuItem("Tools/Effects/Measure MainBoss4 Geometry")]
    public static void Measure()
    {
        var scene=EditorSceneManager.NewPreviewScene();var log=new StringBuilder();
        try
        {
            foreach(var path in new[]{"Assets/Prefab/Player.prefab","Assets/Prefabs/Stage4_Enemy/Stage4_MainBoss.prefab"})
            {
                var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),scene);
                root.transform.position=Vector3.zero;
                foreach(var a in root.GetComponentsInChildren<Animator>())a.enabled=false;
                log.AppendLine(path);Dump(root,log,"Rest");
                if(path.Contains("MainBoss"))
                {
                    var anim=root.GetComponentInChildren<Animator>();
                    var transforms=root.GetComponentsInChildren<Transform>();var poses=transforms.Select(t=>(t.localPosition,t.localRotation,t.localScale)).ToArray();
                    foreach(var clip in anim.runtimeAnimatorController.animationClips.Distinct().Where(c=>c.name.Contains("Attack")||c.name.Contains("skill")||c.name.Contains("Idle")))
                    {
                        for(float t=0;t<clip.length;t+=.1f)
                        {
                            for(int i=0;i<transforms.Length;i++){transforms[i].localPosition=poses[i].Item1;transforms[i].localRotation=poses[i].Item2;transforms[i].localScale=poses[i].Item3;}
                            clip.SampleAnimation(anim.gameObject,t);Dump(root,log,clip.name+" "+t.ToString("F2"));
                        }
                    }
                }
                UnityEngine.Object.DestroyImmediate(root);
            }
            File.WriteAllText(Output,log.ToString());
        }
        catch(Exception e){File.WriteAllText(Output,e.ToString());}
        finally{EditorSceneManager.ClosePreviewScene(scene);}
    }
    static void Dump(GameObject root,StringBuilder log,string label)
    {
        var sprites=root.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.sprite!=null && s.name!="Sword").ToArray();
        if(sprites.Length==0)return;
        Bounds all=BossEffects.VisibleBounds(sprites[0]);foreach(var s in sprites.Skip(1))all.Encapsulate(BossEffects.VisibleBounds(s));
        log.Append(label+" full="+all.min.ToString("F3")+".."+all.max.ToString("F3"));
        foreach(string name in new[]{"Foot_F","Foot_B","Hand_F","Hand_B","Head","Body"})
        {var s=sprites.FirstOrDefault(r=>r.name==name);if(s==null)continue;var b=BossEffects.VisibleBounds(s);log.Append(" "+name+"="+b.center.ToString("F3")+" size="+b.size.ToString("F3"));}
        log.AppendLine();
    }
}
#endif
