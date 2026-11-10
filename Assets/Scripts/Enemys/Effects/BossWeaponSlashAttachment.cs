using System.Collections.Generic;
using UnityEngine;

// Legacy attachment entry point; slash visuals now surround an inferred sweep from the grip.
[DefaultExecutionOrder(1300)]
public sealed class BossWeaponSlashAttachment : MonoBehaviour
{
    SpriteRenderer effect, blade;
    BossSlashSweepVisual.Plane plane;
    static readonly Dictionary<(Texture2D, Rect), Vector2[]> tips = new Dictionary<(Texture2D, Rect), Vector2[]>();
    public void Configure(SpriteRenderer target, Transform weapon, BossSlashSweepVisual.Plane sweepPlane=BossSlashSweepVisual.Plane.Down)
    {
        plane=sweepPlane;effect=target;blade=weapon!=null?weapon.GetComponent<SpriteRenderer>():null;Align();
    }
    void LateUpdate() { Align(); }
    public void Align() { var lifetime=GetComponent<MobEffectInstance>();Apply(effect,blade,plane:plane,progress:lifetime!=null?lifetime.SlashProgress:(float?)null); }
    public static void Apply(SpriteRenderer effect, SpriteRenderer blade, float overlap=.04f,
        Vector3? sampledTip=null, Vector3? sampledAxis=null, int orderOffset=1, BossSlashSweepVisual.Plane plane=BossSlashSweepVisual.Plane.Down,float? sweepDegrees=null,float? progress=null)
    {
        if(effect==null || effect.sprite==null || blade==null || blade.sprite==null)return;
        effect.sortingLayerID=blade.sortingLayerID;effect.sortingOrder=blade.sortingOrder-Mathf.Max(1,orderOffset);
        var animator=blade.GetComponentInParent<Animator>();
        float facing=animator!=null ? Mathf.Sign(animator.transform.lossyScale.x) : 1;
        effect.transform.position=blade.transform.position;effect.transform.rotation=Quaternion.identity;effect.transform.localScale=Vector3.one;
        BossSlashSweepVisual.Draw(effect,blade,plane,facing,.85f,progress,overlap,sweepDegrees:sweepDegrees);
    }
    public static Vector3 WorldTip(SpriteRenderer renderer)
    {
        var sweep=renderer.GetComponent<BossSlashSweepVisual>();
        if(sweep!=null && sweep.View!=null)return sweep.LeadingTip;
        var s=renderer.sprite; var key=(s.texture,s.rect);
        if(!tips.TryGetValue(key,out var ends))
        {
            var previous=RenderTexture.active; RenderTexture rt=null; Texture2D read=null;
            try
            {
                rt=RenderTexture.GetTemporary(s.texture.width,s.texture.height,0,RenderTextureFormat.ARGB32);
                Graphics.Blit(s.texture,rt); RenderTexture.active=rt;
                int w=Mathf.RoundToInt(s.rect.width),h=Mathf.RoundToInt(s.rect.height);
                read=new Texture2D(w,h,TextureFormat.RGBA32,false);read.ReadPixels(s.rect,0,0);read.Apply();
                var pixels=read.GetPixels32();
                bool vertical=s.rect.height>=s.rect.width;
                int along=vertical?h:w,across=vertical?w:h;
                ends=new Vector2[2];
                for(int side=0;side<2;side++)
                for(int row=side==0?0:along-1;row>=0 && row<along;row+=side==0?1:-1)
                {
                    int min=across,max=-1;
                    for(int column=0;column<across;column++)
                    {
                        int x=vertical?column:row,y=vertical?row:column;
                        if(pixels[y*w+x].a>32){min=Mathf.Min(min,column);max=Mathf.Max(max,column);}
                    }
                    if(max>=0)
                    {
                        float mid=(min+max+1)*.5f,edge=row+(side==0?0:1);
                        ends[side]=vertical?new Vector2(mid,edge):new Vector2(edge,mid);break;
                    }
                }
                tips[key]=ends;
            }
            finally
            {
                RenderTexture.active=previous;if(rt!=null)RenderTexture.ReleaseTemporary(rt);
                if(read!=null) { if(Application.isPlaying)Destroy(read);else DestroyImmediate(read); }
            }
        }
        // Some imported swords point downward in their source sprite. Select
        // the opaque end farther from the grip, rather than always taking +Y.
        Vector3 best=renderer.transform.position;float distance=-1;
        foreach(var pixel in ends)
        {
            Vector2 local=(pixel-s.pivot)/s.pixelsPerUnit;
            if(renderer.flipX)local.x=-local.x;if(renderer.flipY)local.y=-local.y;
            Vector3 point=renderer.transform.TransformPoint(local);
            // Fix the source end before animated squash/stretch; do not swap ends mid-swing.
            float d=s.rect.height>=s.rect.width ? Mathf.Abs(local.y) : Mathf.Abs(local.x);
            if(d>distance){distance=d;best=point;}
        }
        return best;
    }
}
