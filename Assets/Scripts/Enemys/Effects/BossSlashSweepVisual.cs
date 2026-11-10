using System.Collections.Generic;
using UnityEngine;

// One continuous, textured sweep around the grip. Sparse 3/4-view poses do not
// describe a literal trail, so the space between them is deliberately inferred.
public sealed class BossSlashSweepVisual : MonoBehaviour
{
    public struct SweepPose { public Vector3 center;public float radius,aspect,start,end; }
    public enum Plane { Down, Up, Horizontal }
    const int Segments=64, Rings=12;
    Mesh mesh;
    float[] widths;
    MeshRenderer view;
    Material material;
    SpriteRenderer owner;
    bool rendering;
    Animator character;
    SpriteRenderer[] characterParts;
    Vector3[] vertices=new Vector3[(Segments+1)*(Rings+1)];
    Color[] colors=new Color[(Segments+1)*(Rings+1)];
    public Vector3 LeadingTip { get; private set; }
    public Bounds WorldBounds => view!=null ? view.bounds : default;
    public Renderer View => view;
    static readonly Dictionary<Texture2D,float[]> contours=new Dictionary<Texture2D,float[]>();
    static readonly Dictionary<Texture2D,Vector2[]> textureCoordinates=new Dictionary<Texture2D,Vector2[]>();

    public static BossSlashSweepVisual Draw(SpriteRenderer owner,SpriteRenderer blade,
        Plane plane,float facing,float opacity=1,float? progress=null,float overlap=0,SweepPose? pose=null,float? sweepDegrees=null)
    {
        if(owner==null || owner.sprite==null || blade==null || blade.sprite==null)return null;
        var visual=owner.GetComponent<BossSlashSweepVisual>() ?? owner.gameObject.AddComponent<BossSlashSweepVisual>();
        visual.Render(owner,blade,plane,facing,opacity,progress,overlap,pose,sweepDegrees);
        return visual;
    }
    void Render(SpriteRenderer target,SpriteRenderer blade,Plane plane,float facing,float opacity,float? progress,float overlap,SweepPose? pose,float? sweepDegrees)
    {
        owner=target;rendering=true;
        // The original big slash supplies the crescent's ragged inner edge.
        var settings=MainBoss1SlashComparison.Settings;
        var texture=settings!=null && settings.verticalSlash!=null ? settings.verticalSlash.sheet : null;
        if(texture==null)return;
        if(mesh==null)Build(texture);
        if(material.mainTexture!=texture){material.mainTexture=texture;mesh.uv=UVs(texture);widths=contours[texture];}
        owner.forceRenderingOff=true;
        view.enabled=owner.enabled && owner.gameObject.activeInHierarchy;
        SortBehindCharacter(blade);

        Vector3 grip=blade.transform.position;
        Vector3 bladeTip=BossWeaponSlashAttachment.WorldTip(blade);
        Vector3 delta=bladeTip-grip;
        float sign=facing<0?-1:1;
        float aspect=plane==Plane.Horizontal?.28f:1f;
        if(plane==Plane.Horizontal && !progress.HasValue)
            aspect=Mathf.Max(aspect,Mathf.Abs(delta.y)/Mathf.Max(.001f,delta.magnitude));
        Vector2 projected=new Vector2(delta.x*sign,delta.y/aspect);
        float radius=projected.magnitude;
        if(radius<.001f){view.enabled=false;return;}
        float end=Mathf.Atan2(projected.y,projected.x)*Mathf.Rad2Deg;
        float start;
        if(pose.HasValue)
        {
            var authored=pose.Value;grip=authored.center;radius=authored.radius;aspect=authored.aspect;
            start=authored.start;end=authored.end;
        }
        else
        {
            // Keep the arc relative to its head: crossing +/-180 degrees cannot
            // flip the tail to another hemisphere. Clip paths supply their own
            // signed travel; one-shot effects use their configured attack plane.
            float span=sweepDegrees ?? (plane==Plane.Down?-165:plane==Plane.Up?150:160);
            if(progress.HasValue)
            {
                if(!sweepDegrees.HasValue)span*=Mathf.Clamp01(progress.Value*3);
                opacity*=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,progress.Value));
            }
            start=end-Mathf.Clamp(span,-240,240);
        }
        float inset=blade.transform.TransformVector(Vector3.up*blade.sprite.bounds.size.y).magnitude*Mathf.Clamp01(overlap);
        radius*=Mathf.Max(0,1-inset/Mathf.Max(.001f,delta.magnitude));
        var color=owner.color;color.a*=Mathf.Clamp01(opacity);
        int index=0;
        for(int i=0;i<=Segments;i++)
        {
            float angle=Mathf.Lerp(start,end,i/(float)Segments)*Mathf.Deg2Rad;
            Vector3 radial=new Vector3(Mathf.Cos(angle)*sign,Mathf.Sin(angle)*aspect,0)*radius;
            for(int j=0;j<=Rings;j++)
            {
                Vector3 point=grip+radial*(1-widths[i]*(1-j/(float)Rings));
                vertices[index]=transform.InverseTransformPoint(point);colors[index++]=color;
                if(i==Segments && j==Rings)LeadingTip=point;
            }
        }
        mesh.vertices=vertices;mesh.colors=colors;mesh.RecalculateBounds();
    }
    void Build(Texture2D texture)
    {
        var child=new GameObject("Continuous slash sweep"){hideFlags=HideFlags.DontSave};
        child.transform.SetParent(transform,false);
        mesh=new Mesh{name="Textured slash sweep",hideFlags=HideFlags.DontSave};mesh.MarkDynamic();
        child.AddComponent<MeshFilter>().sharedMesh=mesh;
        view=child.AddComponent<MeshRenderer>();
        material=new Material(Shader.Find("Sprites/Default")){mainTexture=texture,hideFlags=HideFlags.DontSave};
        view.sharedMaterial=material;
        mesh.vertices=vertices;mesh.colors=colors;mesh.uv=UVs(texture);widths=contours[texture];
        var triangles=new int[Segments*Rings*6];int k=0;
        for(int i=0;i<Segments;i++)for(int j=0;j<Rings;j++)
        {
            int a=i*(Rings+1)+j,b=a+Rings+1;
            triangles[k++]=a;triangles[k++]=b;triangles[k++]=a+1;
            triangles[k++]=a+1;triangles[k++]=b;triangles[k++]=b+1;
        }
        mesh.triangles=triangles;
    }
    static Vector2[] UVs(Texture2D texture)
    {
        if(textureCoordinates.TryGetValue(texture,out var cached))return cached;
        int width=texture.width/10,height=texture.height;
        var previous=RenderTexture.active;var rt=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32);
        var read=new Texture2D(width,height,TextureFormat.RGBA32,false);
        Color32[] pixels;
        try{Graphics.Blit(texture,rt);RenderTexture.active=rt;read.ReadPixels(new Rect(width*4,0,width,height),0,0);read.Apply();pixels=read.GetPixels32();}
        finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);if(Application.isPlaying)Destroy(read);else DestroyImmediate(read);}
        Vector2 center=new Vector2(width*(20.5f/55),height*(34.5f/79));
        Vector2 upper=new Vector2(width*(46.5f/55),height*(63.5f/79))-center;
        Vector2 lower=new Vector2(width*(32.5f/55),height*(4.5f/79))-center;
        float from=Mathf.Atan2(upper.y,upper.x),to=Mathf.Atan2(lower.y,lower.x);
        var uv=new Vector2[(Segments+1)*(Rings+1)];var profile=new float[Segments+1];int index=0;
        // Read a solid texel; the original silhouette drives the strip width.
        // Sampling disconnected inner flecks through polar UVs creates radial spokes.
        int solid=System.Array.FindIndex(pixels,p=>p.a==255);
        if(solid<0)solid=0;
        Vector2 ink=new Vector2((width*4+solid%width+.5f)/texture.width,(solid/width+.5f)/texture.height);
        for(int i=0;i<=Segments;i++)
        {
            float angle=Mathf.Lerp(from,to,i/(float)Segments);Vector2 axis=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            float outer=1;
            for(float r=.25f;r<Mathf.Max(width,height);r+=.25f)
            {
                Vector2 p=center+axis*r;int x=Mathf.FloorToInt(p.x),y=Mathf.FloorToInt(p.y);
                if(x>=0 && x<width && y>=0 && y<height && pixels[y*width+x].a>32)outer=r;
            }
            float inner=outer;
            for(float r=outer;r>0;r-=.25f)
            {
                Vector2 p=center+axis*r;int x=Mathf.FloorToInt(p.x),y=Mathf.FloorToInt(p.y);
                if(x<0 || x>=width || y<0 || y>=height || pixels[y*width+x].a<=32)break;
                inner=r;
            }
            float taper=Mathf.Pow(Mathf.Max(0,Mathf.Sin(Mathf.PI*i/Segments)),.45f);
            profile[i]=Mathf.Clamp((outer-inner)/outer,.16f,.52f)*taper;
            for(int j=0;j<=Rings;j++)uv[index++]=ink;
        }
        contours[texture]=profile;
        textureCoordinates[texture]=uv;return uv;
    }
    void SortBehindCharacter(SpriteRenderer blade)
    {
        int layer=blade.sortingLayerID,order=blade.sortingOrder-1;
        var animator=blade.GetComponentInParent<Animator>();
        if(animator!=character || characterParts==null){character=animator;characterParts=animator!=null?animator.GetComponentsInChildren<SpriteRenderer>():new SpriteRenderer[0];}
        foreach(var part in characterParts)
        {
            if(part==null || !part.enabled || part.sprite==null)continue;
            int value=SortingLayer.GetLayerValueFromID(part.sortingLayerID),current=SortingLayer.GetLayerValueFromID(layer);
            if(value<current){layer=part.sortingLayerID;order=part.sortingOrder-1;}
            else if(value==current)order=Mathf.Min(order,part.sortingOrder-1);
        }
        view.sortingLayerID=layer;view.sortingOrder=order;
    }
    public static void Hide(SpriteRenderer renderer)
    {
        if(renderer==null)return;
        renderer.forceRenderingOff=false;
        var visual=renderer.GetComponent<BossSlashSweepVisual>();
        if(visual==null)return;
        visual.rendering=false;if(visual.view!=null)visual.view.enabled=false;
    }
    void LateUpdate(){if(view!=null && owner!=null)view.enabled=rendering && owner.enabled && owner.sprite!=null;}
    void OnDisable(){if(view!=null)view.enabled=false;}
    void OnDestroy()
    {
        if(owner!=null)owner.forceRenderingOff=false;
        if(Application.isPlaying){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
        else{if(mesh!=null)DestroyImmediate(mesh);if(material!=null)DestroyImmediate(material);}
    }
}

