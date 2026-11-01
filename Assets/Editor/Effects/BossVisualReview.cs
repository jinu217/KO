#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

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
    public static void Run()
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
#endif
