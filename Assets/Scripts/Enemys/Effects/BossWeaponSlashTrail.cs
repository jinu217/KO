using System;
using UnityEngine;

// A complete animated slash with one faint pose-sampled echo behind the blade.
// Both preview and gameplay use the same authored attack windows.
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
    readonly SpriteRenderer[] echoes = new SpriteRenderer[2];
    public bool Covers(AnimationClip clip, float time)
    {
        if (MainBoss1SlashComparison.UseOriginal(source)) return false;
        foreach(var track in tracks)if(track.clip==clip)
            foreach(var window in track.windows)if(time>=window.x && time<=window.y)return true;
        return false;
    }
    // A sheet started just before a rotation would otherwise overlap its first echoes.
    public bool CoversImpact(AnimationClip clip, float time)
    {
        if (MainBoss1SlashComparison.UseOriginal(source)) return false;
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
        if(source==null || weapon==null || source.GetComponent<Stage5_MainBoss>()?.OrbitSwordActive==true)return;
        Track track=Array.Find(tracks,t=>t.clip==clip);
        if(track==null || !Covers(clip,time))return;
        if(frames==null)BuildFrames();
        if(frames==null || frames.Length==0)return;
        var blade=weapon.GetComponent<SpriteRenderer>();
        if(blade==null || blade.sprite==null || track.centers.Length==0 || track.tips.Length!=track.centers.Length)return;
        Vector2 window=Array.Find(track.windows,w=>time>=w.x && time<=w.y);
        Vector3 lastCenter=Vector3.zero, lastAxis=Vector3.zero;
        bool hasPrevious=false;
        for(int i=0;i<echoes.Length;i++)
        {
            float past=time-i*.06f;if(past<window.x)continue;
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
            float phase=Mathf.Clamp01((past-window.x)/Mathf.Max(.001f,window.y-window.x));
            sr.sprite=frames[Mathf.Min(frames.Length-1,Mathf.FloorToInt(phase*frames.Length))];
            sr.sortingLayerID=blade.sortingLayerID;sr.sortingOrder=blade.sortingOrder-1-i;
            sr.flipX=transform.lossyScale.x<0;
            sr.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(tip.y-center.y,tip.x-center.x)*Mathf.Rad2Deg-90);
            sr.transform.localScale=Vector3.one*Mathf.Max(.01f,source.attack.scale*1.5f);
            BossEffects.FitWeaponEffect(sr,weapon,.92f);
            sr.transform.position=center;
            sr.transform.position+=center-BossEffects.VisibleBounds(sr).center;
            BossWeaponSlashAttachment.Apply(sr,blade,.04f+i*.025f,null,axis,1+i);
            float fade=Mathf.Clamp01((window.y-time)/Mathf.Min(.06f,(window.y-window.x)*.25f));
            sr.color=new Color(1,1,1,(i==0?.7f:.1f)*fade);
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
    public void Clear(){foreach(var sr in echoes)if(sr!=null)sr.enabled=false;}
    void OnDisable(){Clear();}
    void OnDestroy()
    {
        foreach(var sr in echoes)if(sr!=null){if(Application.isPlaying)Destroy(sr.gameObject);else DestroyImmediate(sr.gameObject);}
        if(frames!=null && source!=null && (source.attack.frames==null || source.attack.frames.Length==0))
            foreach(var sprite in frames)if(sprite!=null){if(Application.isPlaying)Destroy(sprite);else DestroyImmediate(sprite);}
    }
}
