#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEditor;

// Explicit opt-in review commands; no automatic scene writes or gameplay/save mutations.
[InitializeOnLoad]
public static class HeavySandFXReview
{
    const string Folder = "C:/Users/User/Documents/Codex/2026-10-26/new-chat-3/work/sand_pulse_review";
    static bool running;
    static HeavySandFXReview() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if(EditorApplication.isCompiling || running || !File.Exists(Folder + "/request.txt"))return;
        string request;
        try { request=File.ReadAllText(Folder + "/request.txt"); }
        catch(IOException) { return; }
        if(SessionState.GetString("HeavySandFXReview", "")==request)return;
        if(request.StartsWith("apply") && Application.isPlaying)return;
        if(!request.StartsWith("apply") && !Application.isPlaying)return;
        SessionState.SetString("HeavySandFXReview", request);
        try
        {
            if(request.StartsWith("apply"))Apply();
            else if(request.StartsWith("variants"))
            {
                var fx=UnityEngine.Object.FindFirstObjectByType<PlayerStressScreenFX>();
                running=true;fx.StartCoroutine(ReviewVariants(fx));
            }
            else if(request.StartsWith("noise"))
            {
                var fx=UnityEngine.Object.FindFirstObjectByType<PlayerStressScreenFX>();
                running=true;fx.StartCoroutine(ReviewNoise(fx));
            }
            else if(request.StartsWith("room"))
            {
                var fx=UnityEngine.Object.FindFirstObjectByType<PlayerStressScreenFX>();
                running=true;fx.StartCoroutine(ReviewRoom(fx));
            }
            else if(request.StartsWith("sand"))
            {
                var fx=UnityEngine.Object.FindFirstObjectByType<BossClipEffects>();
                running=true;fx.StartCoroutine(ProbeSand(fx,request));
            }
        }
        catch(Exception e){running=false;File.WriteAllText(Folder+"/error.txt",e.ToString());}
    }
    [MenuItem("Tools/Effects/Bosses/Refresh Heavy Weapon Ground Sand")]
    public static void Apply()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Edit mode required");
        Directory.CreateDirectory(Folder);
        var report=new StringBuilder();
        for(int stage=1;stage<=5;stage++)foreach(bool main in new[]{false,true})
        {
            string path=$"Assets/Prefabs/Stage{stage}_Enemy/Stage{stage}_{(main?"Main":"Mid")}Boss.prefab";
            var template=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<BossEffects>();
            report.AppendLine(path+" weapon="+(template.weaponAnchor!=null?template.weaponAnchor.name:"none")+" heavy="+template.UsesHeavyWeapon());
            if(!template.UsesHeavyWeapon())continue;
            foreach(string target in new[]{path,$"Assets/Prefab/{stage}/{(main?"Main":"Mid")}Boss {stage}.prefab"})
            {
                string backup=Folder+"/before/"+target;
                Directory.CreateDirectory(Path.GetDirectoryName(backup));
                if(!File.Exists(backup))File.Copy(target,backup);
                var root=PrefabUtility.LoadPrefabContents(target);
                try
                {
                    var driver=root.GetComponentInChildren<BossClipEffects>(true);
                    var source=root.GetComponent<BossEffects>() ?? template;
                    BossClipEffectsSetup.ConfigureHeavySand(driver,source);
                    foreach(var track in driver.tracks)
                        foreach(var cue in track.cues.Where(c=>c.style==BossClipEffects.Style.Sand))
                            report.AppendLine(target+" | "+track.clip.name+" | "+cue.time+"s ground="+cue.groundContact);
                    PrefabUtility.SaveAsPrefabAsset(root,target);
                }
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
        }
        AssetDatabase.SaveAssets();File.WriteAllText(Folder+"/apply.txt",report+"DONE\n");
    }
    static IEnumerator ProbeSand(BossClipEffects fx,string request)
    {
        var report=new StringBuilder();float end=Time.time+7;
        while(Time.time<end)
        {
            yield return new WaitForEndOfFrame();
            foreach(var sr in Resources.FindObjectsOfTypeAll<SpriteRenderer>())
                if(sr.enabled && sr.sprite!=null && sr.gameObject.scene.IsValid() && sr.sprite.texture!=null && sr.sprite.texture.name=="sand_03")
                {
                    var state=fx.animator.GetCurrentAnimatorStateInfo(0);
                    var clips=fx.animator.GetCurrentAnimatorClipInfo(0);
                    report.AppendLine((clips.Length>0?clips[0].clip.name:"")+" t="+(clips.Length>0?state.normalizedTime*clips[0].clip.length:0)
                        +" x="+sr.transform.position.x+" bottom="+BossEffects.VisibleBounds(sr).min.y+" alpha="+sr.color.a);
                }
        }
        File.WriteAllText(Folder+"/"+request+".txt",report.ToString());running=false;
    }
    static IEnumerator ReviewRoom(PlayerStressScreenFX fx)
    {
        var room=UnityEngine.Object.FindFirstObjectByType<EquipmentTestRoom>();
        var report=new StringBuilder();
        var equip=typeof(EquipmentTestRoom).GetMethod("Equip",BindingFlags.NonPublic|BindingFlags.Instance);
        var machine=room.items.First(i=>i!=null && i.equipType==EquipmentType.Weapon && i.rarity==ItemRarity.Mechanization);
        var normal=room.items.First(i=>i!=null && i.equipType==EquipmentType.Weapon && i.rarity!=ItemRarity.Mechanization);
        try
        {
            fx.preview=false;room.previewScreenEffects=true;
            typeof(EquipmentTestRoom).GetField("tab",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(room,4);
            typeof(EquipmentTestRoom).GetField("weaponCategory",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(room,1);
            typeof(EquipmentTestRoom).GetField("showScreenEffects",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(room,true);
            equip.Invoke(room,new object[]{machine});room.previewBpm=91;
            yield return new WaitForSecondsRealtime(1);
            report.AppendLine("machine="+machine.itemName+" count="+room.MachineEquipmentCount);
            foreach(float bpm in new[]{91f,150f,195f})
            {
                room.previewBpm=bpm;yield return new WaitForSecondsRealtime(1.5f);
                float end=Time.time+1.4f,max=0,min=1;float originalSize=room.TestCamera.orthographicSize;
                while(Time.time<end)
                {
                    yield return new WaitForEndOfFrame();
                    max=Mathf.Max(max,fx.CurrentPulseZoom);min=Mathf.Min(min,fx.CurrentPulseZoom);
                }
                report.AppendLine("bpm="+bpm+" zoom range="+min+".."+max+" ortho unchanged="+Mathf.Approximately(originalSize,room.TestCamera.orthographicSize));
            }
            yield return Capture("room-machine-bpm");
            room.previewScreenEffects=false;yield return null;
            report.AppendLine("disabled zoom="+fx.CurrentPulseZoom+" canvas="+fx.GetComponentInChildren<Canvas>().enabled);
            room.previewScreenEffects=true;room.previewBpm=91;equip.Invoke(room,new object[]{normal});
            yield return new WaitForSecondsRealtime(3);
            report.AppendLine("normal count="+room.MachineEquipmentCount+" zoom="+fx.CurrentPulseZoom+" canvas="+fx.GetComponentInChildren<Canvas>().enabled);
            yield return Capture("room-normal");
            File.WriteAllText(Folder+"/room.txt",report+"DONE\n");
        }
        finally{room.previewBpm=91;room.previewScreenEffects=true;running=false;}
    }
    static IEnumerator Capture(string name)
    {
        yield return new WaitForEndOfFrame();
        var texture=ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(Folder+"/"+name+".png",texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);
    }
    static IEnumerator ReviewNoise(PlayerStressScreenFX fx)
    {
        var room=UnityEngine.Object.FindFirstObjectByType<EquipmentTestRoom>();
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var equip=typeof(EquipmentTestRoom).GetMethod("Equip",flags);
        var current=(System.Collections.Generic.Dictionary<EquipmentType,ItemData>)typeof(EquipmentTestRoom).GetField("equipped",flags).GetValue(room);
        var original=current.Values.ToArray();
        var types=new[]{EquipmentType.Weapon,EquipmentType.Hand,EquipmentType.Head,EquipmentType.Body,EquipmentType.Foot};
        var report=new StringBuilder();bool preview=fx.preview;float bpm=room.previewBpm;
        try
        {
            fx.preview=false;room.previewBpm=91;
            foreach(var type in types)
                equip.Invoke(room,new object[]{room.items.First(i=>i!=null && i.equipType==type && i.rarity!=ItemRarity.None && i.rarity!=ItemRarity.Mechanization)});
            for(int n=0;n<=types.Length;n++)
            {
                if(n>0)equip.Invoke(room,new object[]{room.items.First(i=>i!=null && i.equipType==types[n-1] && i.rarity==ItemRarity.Mechanization)});
                yield return new WaitForSecondsRealtime(2);
                var noise=fx.GetComponentsInChildren<UnityEngine.UI.RawImage>().FirstOrDefault(i=>i.name=="Machine static noise");
                report.AppendLine("equipped="+room.MachineEquipmentCount+" noiseAlpha="+(noise!=null?noise.color.a:0)+" heartbeat="+fx.CurrentPulseZoom);
                if(n==1 || n==3 || n==5)yield return Capture("noise-"+n);
            }
            File.WriteAllText(Folder+"/noise.txt",report+"DONE\n");
        }
        finally
        {
            foreach(var item in original)equip.Invoke(room,new object[]{item});
            fx.preview=preview;room.previewBpm=bpm;running=false;
        }
    }
    static IEnumerator ReviewVariants(PlayerStressScreenFX fx)
    {
        var room=UnityEngine.Object.FindFirstObjectByType<EquipmentTestRoom>();
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var equip=typeof(EquipmentTestRoom).GetMethod("Equip",flags);
        var current=(System.Collections.Generic.Dictionary<EquipmentType,ItemData>)typeof(EquipmentTestRoom).GetField("equipped",flags).GetValue(room);
        var original=current.Values.ToArray();
        var report=new StringBuilder();bool preview=fx.preview;float bpm=room.previewBpm;
        var style=room.previewNoiseStyle;bool effects=room.previewScreenEffects;
        try
        {
            fx.preview=false;room.previewBpm=91;room.previewScreenEffects=true;
            foreach(var type in new[]{EquipmentType.Weapon,EquipmentType.Hand,EquipmentType.Head,EquipmentType.Body,EquipmentType.Foot})
                equip.Invoke(room,new object[]{room.items.First(i=>i!=null && i.equipType==type && i.rarity==ItemRarity.Mechanization)});
            typeof(EquipmentTestRoom).GetField("showScreenEffects",flags).SetValue(room,true);
            typeof(EquipmentTestRoom).GetField("scroll",flags).SetValue(room,new Vector2(0,460));
            foreach(var variant in new[]{MachineNoiseStyle.Static,MachineNoiseStyle.HorizontalInterference,MachineNoiseStyle.DigitalBlocks})
            {
                room.previewNoiseStyle=variant;yield return new WaitForSecondsRealtime(2);
                var shader=Resources.Load<Shader>("MachineNoiseVariants");
                var distortion=room.TestCamera.GetComponent<MachineNoisePostProcess>();
                report.AppendLine(variant+" count="+room.MachineEquipmentCount+" shaderSupported="+(shader!=null && shader.isSupported)
                    +" extraPass="+(distortion!=null && distortion.enabled)+" heartbeat="+fx.CurrentPulseZoom);
                for(int i=0;i<3;i++)
                {yield return Capture("variant-"+(int)variant+"-"+i);yield return new WaitForSecondsRealtime(.15f);}
            }
            room.previewScreenEffects=false;yield return null;
            report.AppendLine("disabled extraPass="+room.TestCamera.GetComponent<MachineNoisePostProcess>().enabled);
            room.previewScreenEffects=true;room.previewNoiseStyle=MachineNoiseStyle.Static;yield return null;
            report.AppendLine("original extraPass="+room.TestCamera.GetComponent<MachineNoisePostProcess>().enabled);
            File.WriteAllText(Folder+"/variants.txt",report+"DONE\n");
        }
        finally
        {
            foreach(var item in original)equip.Invoke(room,new object[]{item});
            fx.preview=preview;room.previewBpm=bpm;room.previewNoiseStyle=style;room.previewScreenEffects=effects;running=false;
        }
    }
}
#endif
