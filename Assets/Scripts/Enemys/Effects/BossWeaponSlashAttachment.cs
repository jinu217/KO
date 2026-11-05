using System.Collections.Generic;
using UnityEngine;

// A small overlap joins the leading slash apex to the blade while the arc trails it.
[DefaultExecutionOrder(1300)]
public sealed class BossWeaponSlashAttachment : MonoBehaviour
{
    SpriteRenderer effect, blade;
    static readonly Dictionary<(Texture2D, Rect), Vector2> tips = new Dictionary<(Texture2D, Rect), Vector2>();
    public void Configure(SpriteRenderer target, Transform weapon)
    {
        effect=target;blade=weapon!=null?weapon.GetComponent<SpriteRenderer>():null;Align();
    }
    void LateUpdate() { Align(); }
    public void Align() { Apply(effect,blade); }
    public static void Apply(SpriteRenderer effect, SpriteRenderer blade, float overlap=.04f,
        Vector3? sampledTip=null, Vector3? sampledAxis=null, int orderOffset=1)
    {
        if(effect==null || effect.sprite==null || blade==null || blade.sprite==null)return;
        Vector3 axis=sampledAxis ?? blade.transform.TransformVector(blade.flipY?Vector3.down:Vector3.up);
        if(axis.sqrMagnitude<.000001f)return;
        axis.Normalize();
        effect.sortingLayerID=blade.sortingLayerID;effect.sortingOrder=blade.sortingOrder-Mathf.Max(1,orderOffset);
        effect.flipY=false;
        effect.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(axis.y,axis.x)*Mathf.Rad2Deg-90);
        float length=blade.transform.TransformVector(Vector3.up*blade.sprite.bounds.size.y).magnitude;
        Vector3 target=(sampledTip ?? WorldTip(blade))-axis*length*Mathf.Clamp01(overlap);
        effect.transform.position+=target-WorldTip(effect);
    }
    public static Vector3 WorldTip(SpriteRenderer renderer)
    {
        var s=renderer.sprite; var key=(s.texture,s.rect);
        if(!tips.TryGetValue(key,out var pixel))
        {
            var previous=RenderTexture.active; RenderTexture rt=null; Texture2D read=null;
            try
            {
                rt=RenderTexture.GetTemporary(s.texture.width,s.texture.height,0,RenderTextureFormat.ARGB32);
                Graphics.Blit(s.texture,rt); RenderTexture.active=rt;
                int w=Mathf.RoundToInt(s.rect.width),h=Mathf.RoundToInt(s.rect.height);
                read=new Texture2D(w,h,TextureFormat.RGBA32,false);read.ReadPixels(s.rect,0,0);read.Apply();
                var pixels=read.GetPixels32();pixel=new Vector2(w*.5f,h);
                for(int y=h-1;y>=0;y--)
                {
                    int min=w,max=-1;
                    for(int x=0;x<w;x++) if(pixels[y*w+x].a>32) { min=Mathf.Min(min,x);max=Mathf.Max(max,x); }
                    if(max>=0) { pixel=new Vector2((min+max+1)*.5f,y+1);break; }
                }
                tips[key]=pixel;
            }
            finally
            {
                RenderTexture.active=previous;if(rt!=null)RenderTexture.ReleaseTemporary(rt);
                if(read!=null) { if(Application.isPlaying)Destroy(read);else DestroyImmediate(read); }
            }
        }
        Vector2 local=(pixel-s.pivot)/s.pixelsPerUnit;
        if(renderer.flipX)local.x=-local.x;if(renderer.flipY)local.y=-local.y;
        return renderer.transform.TransformPoint(local);
    }
}
