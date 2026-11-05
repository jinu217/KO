using System;
using UnityEngine;

// Pose-sampled slash echoes: the same clip time gives the same arc in preview and play.
[ExecuteAlways, DefaultExecutionOrder(1200)]
public sealed class BossWeaponSlashTrail : MonoBehaviour
{
    [Serializable] public class Track
    {
        public AnimationClip clip;
        public Vector3[] centers, tips;
        public Vector2[] windows;
    }
    public BossEffects source;
    public Animator animator;
    public Transform weapon;
    public Track[] tracks = Array.Empty<Track>();
    Sprite[] frames;
    Sprite[] motionFrames;
    readonly SpriteRenderer[] echoes = new SpriteRenderer[3];
    public bool Covers(AnimationClip clip, float time)
    {
        if (MainBoss1SlashComparison.UseOriginal(source)) return false;
        if (BossMotionFXSettings.Applies(source))
            return Array.Exists(tracks, t => t.clip == clip) && BossMotionFXSettings.SpinWindow(clip, out var motionWindow) && time >= motionWindow.x && time <= motionWindow.y;
        foreach(var track in tracks)if(track.clip==clip)
            foreach(var window in track.windows)if(time>=window.x && time<=window.y)return true;
        return false;
    }
    // A sheet started just before a rotation would otherwise overlap its first echoes.
    public bool CoversImpact(AnimationClip clip, float time)
    {
        if (MainBoss1SlashComparison.UseOriginal(source)) return false;
        if (BossMotionFXSettings.Applies(source)) return Covers(clip, time);
        foreach(var track in tracks)if(track.clip==clip)
            foreach(var window in track.windows)if(time<=window.y && time+.16f>=window.x)return true;
        return false;
    }
    void LateUpdate()
    {
        if(!Application.isPlaying)
        {
#if UNITY_EDITOR
            if(!UnityEditor.AnimationMode.InAnimationMode())Clear();
#endif
            return;
        }
        if(animator==null || !animator.isActiveAndEnabled){Clear();return;}
        var clips=animator.GetCurrentAnimatorClipInfo(0);
        if(clips.Length==0){Clear();return;}
        Sample(clips[0].clip,animator.GetCurrentAnimatorStateInfo(0).normalizedTime*clips[0].clip.length);
    }
    public void Sample(AnimationClip clip,float time)
    {
        Clear();
        if (MainBoss1SlashComparison.UseOriginal(source)) return;
        if (BossMotionFXSettings.Applies(source)) { SampleMotion(clip, time); return; }
        if(source==null || weapon==null || source.GetComponent<Stage5_MainBoss>()?.OrbitSwordActive==true)return;
        Track track=Array.Find(tracks,t=>t.clip==clip);
        if(track==null || (!Covers(clip,time) && !Covers(clip,time-.145f)))return;
        if(frames==null)BuildFrames();
        if(frames==null || frames.Length==0)return;
        var blade=weapon.GetComponent<SpriteRenderer>();
        Vector3 lastCenter=Vector3.zero, lastAxis=Vector3.zero;
        bool hasPrevious=false;
        for(int i=0;i<echoes.Length;i++)
        {
            float past=time-.025f-i*.06f;if(!Covers(clip,past))continue;
            float sample=Mathf.Clamp(past*48,0,track.centers.Length-1);
            int a=Mathf.FloorToInt(sample),b=Mathf.Min(a+1,track.centers.Length-1);float u=sample-a;
            Vector3 center=transform.TransformPoint(Vector3.Lerp(track.centers[a],track.centers[b],u));
            Vector3 tip=transform.TransformPoint(Vector3.Lerp(track.tips[a],track.tips[b],u));
            Vector3 axis=tip-center;
            // Slow/stationary sections should not stack several almost identical sheets.
            if(hasPrevious && Vector3.Angle(lastAxis,axis)<12 && Vector3.Distance(lastCenter,center)<axis.magnitude*.15f)continue;
            lastCenter=center;lastAxis=axis;hasPrevious=true;
            center+=axis*.1f;
            if(echoes[i]==null)
            {
                var go=new GameObject("Weapon Slash Arc "+i){hideFlags=HideFlags.HideAndDontSave};
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,gameObject.scene);
                echoes[i]=go.AddComponent<SpriteRenderer>();
            }
            var sr=echoes[i];sr.enabled=true;
            sr.sprite=frames[Mathf.Clamp(frames.Length/3+i,0,frames.Length-1)];
            sr.sortingLayerID=blade.sortingLayerID;sr.sortingOrder=blade.sortingOrder-1-i;
            sr.flipX=transform.lossyScale.x<0;
            sr.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(tip.y-center.y,tip.x-center.x)*Mathf.Rad2Deg-90);
            BossEffects.FitWeaponEffect(sr,weapon,.92f);
            var scale=sr.transform.localScale;scale.x*=.5f;sr.transform.localScale=scale;
            sr.transform.position=center;
            sr.transform.position+=center-BossEffects.VisibleBounds(sr).center;
            BossWeaponSlashAttachment.Apply(sr,blade,.04f+i*.025f,null,axis,1+i);
            sr.color=new Color(1,1,1,i==0?.2f:i==1?.06f:.015f);
        }
    }
    void BuildFrames()
    {
        var effect=source.attack;
        if(effect.frames!=null && effect.frames.Length>0){frames=effect.frames;return;}
        if(effect.sheet==null || effect.columns<=0)return;
        frames=new Sprite[effect.columns];int width=effect.sheet.width/effect.columns;
        for(int i=0;i<frames.Length;i++)frames[i]=Sprite.Create(effect.sheet,new Rect(i*width,0,width,effect.sheet.height),new Vector2(.5f,.5f),source.pixelsPerUnit);
    }
    void SampleMotion(AnimationClip clip, float time)
    {
        if (source == null || weapon == null || !Covers(clip, time)) return;
        if (source.GetComponent<Stage5_MainBoss>()?.OrbitSwordActive == true) return;
        var track = Array.Find(tracks, t => t.clip == clip);
        var config = BossMotionFXSettings.Settings;
        if (config == null || config.spin.sheet == null || track == null) return;
        if (motionFrames == null)
        {
            var effect = config.spin;
            motionFrames = new Sprite[effect.columns];
            float width = effect.sheet.width / (float)effect.columns;
            for(int i=0;i<motionFrames.Length;i++) motionFrames[i] = Sprite.Create(effect.sheet,
                new Rect(i*width,0,width,effect.sheet.height),new Vector2(.5f,.5f),source.pixelsPerUnit);
        }
        if(echoes[0] == null)
        {
            var go = new GameObject("Weapon Slash Arc 0") { hideFlags = HideFlags.HideAndDontSave };
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, gameObject.scene);
            echoes[0] = go.AddComponent<SpriteRenderer>();
        }
        var sr = echoes[0]; var blade = weapon.GetComponent<SpriteRenderer>();
        if(blade == null || blade.sprite == null) return;
        BossMotionFXSettings.SpinWindow(clip, out var window);
        sr.enabled = true;
        sr.sprite = motionFrames[Mathf.FloorToInt((time-window.x)*36) % motionFrames.Length];
        sr.flipX = transform.lossyScale.x < 0; sr.flipY = false;
        sr.sortingLayerID = blade.sortingLayerID; sr.sortingOrder = blade.sortingOrder-1;
        float sample = Mathf.Clamp((time-.025f)*48,0,track.centers.Length-1);
        int a=Mathf.FloorToInt(sample), b=Mathf.Min(a+1,track.centers.Length-1);
        Vector3 center=transform.TransformPoint(Vector3.Lerp(track.centers[a],track.centers[b],sample-a));
        Vector3 tip=transform.TransformPoint(Vector3.Lerp(track.tips[a],track.tips[b],sample-a));
        Vector3 axis=tip-center;
        sr.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(axis.y,axis.x)*Mathf.Rad2Deg-90);
        BossEffects.FitWeaponEffect(sr,weapon,1.05f);
        sr.transform.position=center;
        sr.transform.position+=center-BossEffects.VisibleBounds(sr).center;
        BossWeaponSlashAttachment.Apply(sr,blade,.04f,null,axis);
        sr.color=Color.white;
    }
    public void Clear(){foreach(var sr in echoes)if(sr!=null)sr.enabled=false;}
    void OnDisable(){Clear();}
    void OnDestroy()
    {
        if(motionFrames!=null) foreach(var sprite in motionFrames) if(sprite!=null) { if(Application.isPlaying)Destroy(sprite);else DestroyImmediate(sprite); }
        foreach(var sr in echoes)if(sr!=null){if(Application.isPlaying)Destroy(sr.gameObject);else DestroyImmediate(sr.gameObject);}
        if(frames!=null && source!=null && (source.attack.frames==null || source.attack.frames.Length==0))
            foreach(var sprite in frames)if(sprite!=null){if(Application.isPlaying)Destroy(sprite);else DestroyImmediate(sprite);}
    }
}
