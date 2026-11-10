#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
static class SweepContinuityReview
{
    const string Folder="C:/Users/User/Documents/Codex/2026-11-05/knight-only-continuation/work/nov10_continuity";
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static SweepContinuityReview(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Folder+"/request.txt"))return;
        string id=File.ReadAllText(Folder+"/request.txt").Trim();if(SessionState.GetString("SweepContinuityReview","")==id)return;
        SessionState.SetString("SweepContinuityReview",id);
        try{Run(id);}catch(Exception e){File.WriteAllText(Folder+"/error.txt",e.ToString());}
    }
    sealed class Previous{public Vector3[] points;public Vector3 tip,grip;public float alpha;}
    static Dictionary<int,Previous> previous=new Dictionary<int,Previous>();
    static StringBuilder anomalies,details,tracks;
    static int samples,jumps,windows;
    static void Pose(GameObject root,Animator animator,AnimationClip clip,float t)
    {
        clip.SampleAnimation(animator.gameObject,t);
        foreach(var c in root.GetComponentsInChildren<BossRearWeaponSorting>())typeof(BossRearWeaponSorting).GetMethod("LateUpdate",Private)?.Invoke(c,null);
    }
    static void Check(BossSlashSweepVisual visual,SpriteRenderer blade,string label,float time)
    {
        if(visual==null || visual.View==null || !visual.View.enabled)return;
        var mesh=visual.GetComponentInChildren<MeshFilter>().sharedMesh;var v=mesh.vertices;var colors=mesh.colors;
        float alpha=colors.Length>0?colors[0].a:1;
        var trans=visual.GetComponentInChildren<MeshFilter>().transform;
        var points=new Vector3[9];for(int i=0;i<points.Length;i++)points[i]=trans.TransformPoint(v[(i*8)*13+12]);
        Vector3 tip=BossWeaponSlashAttachment.WorldTip(blade),grip=blade.transform.position;
        float radius=Mathf.Max(.1f,Vector3.Distance(tip,grip));
        float max=0,tipStep=0,gripStep=0;
        if(previous.TryGetValue(visual.GetInstanceID(),out var old))
        {
            for(int i=0;i<points.Length;i++)max=Mathf.Max(max,Vector3.Distance(points[i],old.points[i])/radius);
            tipStep=Vector3.Distance(tip,old.tip)/radius;gripStep=Vector3.Distance(grip,old.grip)/radius;
            if(alpha>.12f && old.alpha>.12f && max>.22f && max>tipStep+gripStep+.18f)
            {jumps++;anomalies.AppendLine($"{label},{time:F5},jump={max:F3},blade={tipStep:F3},grip={gripStep:F3},alpha={alpha:F3}");}
        }
        previous[visual.GetInstanceID()]=new Previous{points=points,tip=tip,grip=grip,alpha=alpha};samples++;
        if(label.StartsWith("Main1"))details.AppendLine($"{label},{time*12:F3},{radius:F4},{max:F4},{tipStep:F4},{gripStep:F4},{alpha:F4},{visual.LeadingTip.x:F3},{visual.LeadingTip.y:F3}");
    }
    static void Run(string id)
    {
        samples=jumps=windows=0;anomalies=new StringBuilder();details=new StringBuilder("clip,frame,radius,meshStep,bladeStep,gripStep,alpha,tipX,tipY\n");tracks=new StringBuilder();
        string run=Folder+"/"+id;Directory.CreateDirectory(run);
        for(int stage=1;stage<=5;stage++)foreach(bool main in new[]{false,true})
        {
            if(main && stage==4)continue; if(id=="poses" && !(main && stage==1))continue;
            var root=PrefabUtility.LoadPrefabContents($"Assets/Prefabs/Stage{stage}_Enemy/Stage{stage}_{(main?"Main":"Mid")}Boss.prefab");
            try
            {
                var source=root.GetComponent<BossEffects>();var animator=source.motionAnimator;var blade=source.weaponAnchor.GetComponent<SpriteRenderer>();
                string label=(main?"Main":"Mid")+stage;
                var trail=animator.GetComponent<BossWeaponSlashTrail>();var driver=animator.GetComponent<BossClipEffects>();
                if(main && stage==1)
                {
                    foreach(var clip in animator.runtimeAnimatorController.animationClips.Concat(new[]{AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Animations/1/MainBoss 1/MainBoss1_Attack.anim")}).Distinct().Where(c=>MainBoss1AttackSlash.Applies(source,c)))
                    {
                        MainBoss1AttackSlash slash=null;previous.Clear(); foreach(float f in new[]{6f,8f,18f,19f,20f}){Pose(root,animator,clip,f/12); var tip=BossWeaponSlashAttachment.WorldTip(blade); tracks.AppendLine(clip.name+" KEY "+f+" grip="+animator.transform.InverseTransformPoint(blade.transform.position).ToString("F6")+" tip="+animator.transform.InverseTransformPoint(tip).ToString("F6")+" pivot="+blade.sprite.pivot+" rect="+blade.sprite.rect); }
                        for(int step=50;step<=220;step++)
                        {
                            float frame=step/10f;Pose(root,animator,clip,frame/12);
                            var instance=MainBoss1AttackSlash.Sample(source,clip,frame/12,1,ref slash);
                            if(instance!=null)Check(instance.GetComponent<BossSlashSweepVisual>(),blade,label+" "+clip.name,frame/12);
                            if(clip.name=="MainBoss1_Attack" && step%2==0)Capture(root,run+"/main1_"+((step-50)/2).ToString("D3")+".png");
                        }
                        MainBoss1AttackSlash.Clear(ref slash);
                    }
                }
                if(id!="poses" && trail!=null)foreach(var track in trail.tracks)foreach(var window in track.windows)
                {
                    previous.Clear();windows++;
                    float angle=0,last=0;bool first=true;
                    for(int n=0;n<=Mathf.CeilToInt((window.y-window.x)*120);n++)
                    {
                        float t=Mathf.Min(window.y,window.x+n/120f);Pose(root,animator,track.clip,t);trail.Sample(track.clip,t);
                        var echoes=(SpriteRenderer[])typeof(BossWeaponSlashTrail).GetField("echoes",Private).GetValue(trail);
                        foreach(var sr in echoes)if(sr!=null && sr.enabled)Check(sr.GetComponent<BossSlashSweepVisual>(),blade,label+" trail "+track.clip.name,t);
                        float current=blade.transform.eulerAngles.z;if(!first)angle+=Mathf.DeltaAngle(last,current);last=current;first=false;
                    }
                    tracks.AppendLine($"{label},{track.clip.name},{window.x:F3},{window.y:F3},poseTravel={angle:F1}");trail.Clear();
                }
                if(id!="poses" && driver!=null)foreach(var track in driver.tracks)
                foreach(var cue in track.cues.Where(c=>c.style==BossClipEffects.Style.Down || c.style==BossClipEffects.Style.Up || c.style==BossClipEffects.Style.Spin))
                {
                    previous.Clear();windows++;
                    for(int n=0;n<=Mathf.CeilToInt(cue.duration*120);n++)
                    {
                        float t=cue.time+Mathf.Min(cue.duration,n/120f);Pose(root,animator,track.clip,t);driver.Sample(track.clip,t,false,false);
                        var renderers=(List<SpriteRenderer>)typeof(BossClipEffects).GetField("renderers",Private).GetValue(driver);
                        foreach(var sr in renderers)if(sr!=null && sr.enabled)Check(sr.GetComponent<BossSlashSweepVisual>(),blade,label+" cue "+track.clip.name,t);
                    }
                    driver.ClearVisuals();trail?.Clear();
                }
                driver?.Release();trail?.Clear();
            }
            finally{CombatFXScenePreview.EndReview();PrefabUtility.UnloadPrefabContents(root);}
        }
        File.WriteAllText(run+"/summary.txt",$"samples={samples}; windows={windows}; discontinuities={jumps}\n"+anomalies);
        File.WriteAllText(run+"/main1.csv",details.ToString());File.WriteAllText(run+"/tracks.csv",tracks.ToString());
    }
    static void Capture(GameObject root,string filename)
    {
        var obj=new GameObject("Sweep continuity camera"){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(obj,root.scene);
        var camera=obj.AddComponent<Camera>();camera.scene=root.scene;camera.orthographic=true;camera.orthographicSize=10;
        camera.transform.position=root.transform.position+new Vector3(.5f,2,-20);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.15f,.15f,1);
        var target=new RenderTexture(480,480,24);Texture2D image=null;var old=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;image=new Texture2D(480,480,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,480,480),0,0);image.Apply();File.WriteAllBytes(filename,image.EncodeToPNG());}
        finally{RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(obj);target.Release();UnityEngine.Object.DestroyImmediate(target);if(image!=null)UnityEngine.Object.DestroyImmediate(image);}
    }
}
#endif

