#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
static class SweptSlashReview
{
    const string Folder="C:/Users/User/Documents/Codex/2026-11-05/knight-only-continuation/work/nov10_swept_slash";
    static SweptSlashReview(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Folder+"/request.txt"))return;
        string id=File.ReadAllText(Folder+"/request.txt");if(SessionState.GetString("SweptSlashReview","")==id)return;
        SessionState.SetString("SweptSlashReview",id);
        try{Run();}catch(Exception e){File.WriteAllText(Folder+"/result.txt",e.ToString());}
    }
    static void Run()
    {
        var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Stage1_Enemy/Stage1_MainBoss.prefab");
        MainBoss1AttackSlash slash=null;var report=new StringBuilder();
        try
        {
            var fx=root.GetComponent<BossEffects>();var animator=fx.motionAnimator;
            var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/1/MainBoss 1/MainBoss1_Attack.anim");
            foreach(float frame in new[]{6f,7f,8f,9f,18f,18.5f,19f,19.5f,20f,21f})
            {
                clip.SampleAnimation(animator.gameObject,frame/12f);
                foreach(var c in root.GetComponentsInChildren<BossRearWeaponSorting>())
                    typeof(BossRearWeaponSorting).GetMethod("LateUpdate",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)?.Invoke(c,null);
                var blade=fx.weaponAnchor.GetComponent<SpriteRenderer>();
                var grip=blade.transform.position;var tip=BossWeaponSlashAttachment.WorldTip(blade);
                report.AppendLine($"f{frame}: grip={grip.ToString("F3")} tip={tip.ToString("F3")} radius={Vector3.Distance(grip,tip)} angle={Mathf.Atan2(tip.y-grip.y,tip.x-grip.x)*Mathf.Rad2Deg} parent={blade.transform.parent.name} parentPos={blade.transform.parent.position} size={blade.sprite.bounds.size} pivot={blade.sprite.pivot} scale={blade.transform.lossyScale}");
                MainBoss1AttackSlash.Sample(fx,clip,frame/12f,1,ref slash);
                Capture(root,"frame_"+frame);
            }
            report.AppendLine("PASS sampled / no authored assets saved");
        }
        finally{MainBoss1AttackSlash.Clear(ref slash);PrefabUtility.UnloadPrefabContents(root);}
        File.WriteAllText(Folder+"/result.txt",report.ToString());
    }
    static void Capture(GameObject root,string name)
    {
        var obj=new GameObject("Swept slash review camera"){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(obj,root.scene);
        var camera=obj.AddComponent<Camera>();camera.scene=root.scene;camera.orthographic=true;camera.orthographicSize=9;
        camera.transform.position=root.transform.position+new Vector3(2,2,-20);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.15f,.15f,1);
        var target=new RenderTexture(800,800,24);Texture2D image=null;var old=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;image=new Texture2D(800,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,800,800),0,0);image.Apply();File.WriteAllBytes(Folder+"/"+name+".png",image.EncodeToPNG());}
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(obj);target.Release();UnityEngine.Object.DestroyImmediate(target);if(image!=null)UnityEngine.Object.DestroyImmediate(image);}
    }
}
#endif
