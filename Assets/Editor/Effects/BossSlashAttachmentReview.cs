#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
static class BossSlashAttachmentReview
{
    const string Folder="C:/Users/User/Documents/Codex/2026-11-05/knight-only-continuation/work/boss_slash_attachment";
    const BindingFlags Private=BindingFlags.NonPublic|BindingFlags.Instance;
    static int factoryCount,cueCount,trailCount;static float maxError;
    static BossSlashAttachmentReview() { EditorApplication.update+=Poll; }
    static void Poll()
    {
        if(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Folder+"/request.txt"))return;
        string id=File.ReadAllText(Folder+"/request.txt");if(SessionState.GetString("BossSlashAttachmentReview","")==id)return;
        SessionState.SetString("BossSlashAttachmentReview",id);
        try { Run(); }catch(Exception e){File.WriteAllText(Folder+"/result.txt",e.ToString());}
    }
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static bool Slash(BossEffects.Kind kind)=>kind==BossEffects.Kind.Down || kind==BossEffects.Kind.Up || kind==BossEffects.Kind.Horizontal || kind==BossEffects.Kind.Spin;
    static void Check(SpriteRenderer sr,SpriteRenderer blade,float overlap,string context)
    {
        Require(sr.sortingLayerID==blade.sortingLayerID && sr.sortingOrder<blade.sortingOrder,context+" wrong layer/order");
        float length=blade.transform.TransformVector(Vector3.up*blade.sprite.bounds.size.y).magnitude;
        var expected=BossWeaponSlashAttachment.WorldTip(blade)-sr.transform.up*length*overlap;
        float error=Vector3.Distance(BossWeaponSlashAttachment.WorldTip(sr),expected);
        maxError=Mathf.Max(maxError,error);Require(error<.001f,context+" attachment error="+error);
    }
    static void Pose(GameObject root,Animator animator,AnimationClip clip,float time)
    {
        clip.SampleAnimation(animator.gameObject,time);
        foreach(var c in root.GetComponentsInChildren<MonoBehaviour>(true))
            if(c!=null && (c is BossRearWeaponSorting || c.GetType().Name.Contains("LayerPreview") || c.GetType().Name.Contains("AnimationRelay")))
                c.GetType().GetMethod("LateUpdate",Private)?.Invoke(c,null);
    }
    static void Run()
    {
        factoryCount=cueCount=trailCount=0;maxError=0;
        var settings=MainBoss1SlashComparison.Settings;var oldMode=settings.mode;var report=new StringBuilder();
        try
        {
            foreach(var mode in new[]{MainBoss1SlashComparison.DisplayMode.OriginalSlash,MainBoss1SlashComparison.DisplayMode.MotionMatched})
            for(int stage=1;stage<=5;stage++)foreach(bool main in new[]{false,true})
            {
                settings.mode=mode;
                string label=(main?"Main":"Mid")+stage;
                var root=PrefabUtility.LoadPrefabContents($"Assets/Prefabs/Stage{stage}_Enemy/Stage{stage}_{(main?"Main":"Mid")}Boss.prefab");
                try
                {
                    var fx=root.GetComponent<BossEffects>();var controller=root.GetComponent<MidBossController>();var animator=fx.motionAnimator;
                    string dataBefore=EditorJsonUtility.ToJson(controller.data);
                    if(stage==4 && main){report.AppendLine(label+" fists: excluded from weapon attachment");continue;}
                    var blade=fx.weaponAnchor.GetComponent<SpriteRenderer>();
                    var clip=animator.runtimeAnimatorController.animationClips.First(c=>c.name.ToLowerInvariant().Contains("attack") || c.name.ToLowerInvariant().Contains("combo"));
                    var driver=animator.GetComponent<BossClipEffects>();var trail=animator.GetComponent<BossWeaponSlashTrail>();
                    int before=factoryCount+cueCount+trailCount;bool captured=false;
                    foreach(int facing in new[]{1,-1})
                    {
                        root.transform.localScale=new Vector3(facing,1,1);
                        var skills=new[]{controller.data.basicAttack}.Concat(controller.data.skills).ToArray();
                        for(int si=0;si<skills.Length;si++)
                        {
                            var skill=skills[si];if(skill?.active?.attackSteps==null)continue;
                            foreach(var step in skill.active.attackSteps)
                            {
                                if(step.damage<=0)continue;
                                var kind=fx.ResolveForPreview(skill,si-1,step,0,out _);if(!Slash(kind))continue;
                                float time=fx.TryMotionImpact(clip,0,out float impact)?impact:clip.length*.35f;
                                Pose(root,animator,clip,time);
                                var instance=fx.PreviewStep(skill,si-1,step,0,root.transform.position,facing,.13f);
                                if(instance==null)continue;
                                try
                                {
                                    var sr=instance.GetComponent<SpriteRenderer>();if(!sr.enabled)continue;
                                    Require(instance.GetComponent<BossWeaponSlashAttachment>()!=null,label+" missing follower");
                                    Check(sr,blade,.04f,label+" factory");factoryCount++;
                                    string layer=blade.sortingLayerName;int order=blade.sortingOrder;
                                    blade.sortingLayerName="Back";blade.sortingOrder=47;instance.GetComponent<BossWeaponSlashAttachment>().Align();Check(sr,blade,.04f,label+" switched depth");
                                    blade.sortingLayerName=layer;blade.sortingOrder=order;instance.GetComponent<BossWeaponSlashAttachment>().Align();
                                    if(!captured && facing==1 && mode==MainBoss1SlashComparison.DisplayMode.OriginalSlash)
                                    {Capture(root,sr,label);captured=true;}
                                }
                                finally{UnityEngine.Object.DestroyImmediate(instance.gameObject);}
                            }
                        }
                        if(driver!=null)foreach(var track in driver.tracks)
                        {
                            if(track.clip==null)continue;
                            foreach(var cue in track.cues.Where(c=>c.style==BossClipEffects.Style.Down || c.style==BossClipEffects.Style.Up || c.style==BossClipEffects.Style.Spin))
                            {
                                float time=cue.time+Mathf.Min(.08f,cue.duration*.5f);Pose(root,animator,track.clip,time);driver.Sample(track.clip,time,false,false);
                                var renderers=(List<SpriteRenderer>)typeof(BossClipEffects).GetField("renderers",Private).GetValue(driver);
                                foreach(var sr in renderers.Where(r=>r!=null && r.enabled && r.sprite!=null && r.sprite.texture.name.Contains("slash")))
                                {Check(sr,blade,.04f,label+" clip "+track.clip.name);cueCount++;}
                            }
                        }
                        if(trail!=null)foreach(var track in trail.tracks)
                        foreach(var window in track.windows)
                        {
                            float time=(window.x+window.y)*.5f;Pose(root,animator,track.clip,time);trail.Sample(track.clip,time);
                            var echoes=(SpriteRenderer[])typeof(BossWeaponSlashTrail).GetField("echoes",Private).GetValue(trail);
                            for(int i=0;i<echoes.Length;i++)if(echoes[i]!=null && echoes[i].enabled && echoes[i].sprite!=null)
                            {Check(echoes[i],blade,mode==MainBoss1SlashComparison.DisplayMode.MotionMatched && main?.04f:.04f+i*.025f,label+" trail "+track.clip.name);trailCount++;}
                        }
                        driver?.Release();trail?.Clear();
                    }
                    Require(EditorJsonUtility.ToJson(controller.data)==dataBefore,label+" attack data changed");
                    report.AppendLine(mode+" "+label+" both facings: "+(factoryCount+cueCount+trailCount-before)+" attached samples PASS");
                }
                finally{CombatFXScenePreview.EndReview();PrefabUtility.UnloadPrefabContents(root);}
            }
        }
        finally{settings.mode=oldMode;}
        report.AppendLine($"PASS factory={factoryCount} clipCues={cueCount} trails={trailCount} maxTipError={maxError}; original mode restored; no assets saved.");
        File.WriteAllText(Folder+"/result.txt",report.ToString());
    }
    static void Capture(GameObject root,SpriteRenderer effect,string name)
    {
        var renderers=root.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.enabled && r.sprite!=null).ToArray();
        Bounds bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);bounds.Encapsulate(effect.bounds);
        var obj=new GameObject("Attachment review camera"){hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(obj,root.scene);
        var camera=obj.AddComponent<Camera>();camera.scene=root.scene;camera.orthographic=true;camera.orthographicSize=Mathf.Max(bounds.extents.x,bounds.extents.y)*1.1f;
        camera.transform.position=bounds.center+Vector3.back*20;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.16f,.18f,1);
        var target=new RenderTexture(640,640,24);Texture2D image=null;var previous=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;image=new Texture2D(640,640,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,640,640),0,0);image.Apply();File.WriteAllBytes(Folder+"/"+name+".png",image.EncodeToPNG());}
        finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(obj);target.Release();UnityEngine.Object.DestroyImmediate(target);if(image!=null)UnityEngine.Object.DestroyImmediate(image);}
    }
}
#endif
