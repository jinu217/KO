using UnityEngine;
using UnityEngine.SceneManagement;

// Clip-driven, so the same typhoon is visible in Animation preview and in Play mode.
[ExecuteAlways, DisallowMultipleComponent]
public sealed class Stage5MainBossSpinEffect : MonoBehaviour
{
    public Texture2D sheet;
    public Transform body;
    public SpriteRenderer sword;
    [HideInInspector] public float active, progress;
    Sprite[] frames;
    Texture2D cachedSheet;
    SpriteRenderer effect;
    float radius;
    const int Columns = 24;

    void OnEnable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update += Preview;
#endif
    }
    void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.update -= Preview;
#endif
        Cleanup();
    }
    void OnDestroy() { Cleanup(); }
    void OnDidApplyAnimationProperties() { Render(); }
    void LateUpdate() { if(Application.isPlaying) Render(); }
#if UNITY_EDITOR
    void Preview()
    {
        if(this==null || Application.isPlaying) return;
        if(UnityEditor.AnimationMode.InAnimationMode()) Render();
        else if(effect!=null) Cleanup();
    }
#endif
    void Render()
    {
        if(active < .5f || sheet==null || body==null || sword==null || sword.sprite==null)
        { if(effect!=null) effect.enabled=false; radius=0; return; }
        if(cachedSheet!=sheet || frames==null)
        {
            Cleanup();
            if(sheet.width%Columns!=0) return;
            cachedSheet=sheet;frames=new Sprite[Columns];
            int width=sheet.width/Columns;
            for(int i=0;i<Columns;i++)frames[i]=Sprite.Create(sheet,new Rect(i*width,0,width,sheet.height),
                new Vector2(.5f,.5f),64,0,SpriteMeshType.FullRect);
        }
        if(effect==null)
        {
            var go=new GameObject("MainBoss5 Typhoon 03 (Preview FX)");
            go.hideFlags=HideFlags.HideAndDontSave;
            if(gameObject.scene.IsValid()&&gameObject.scene.isLoaded)SceneManager.MoveGameObjectToScene(go,gameObject.scene);
            effect=go.AddComponent<SpriteRenderer>();
        }
        effect.enabled=true;
        effect.sprite=frames[Mathf.FloorToInt(Mathf.Max(0,progress)*36)%Columns];
        effect.sortingLayerID=SortingLayer.NameToID("Front");effect.sortingOrder=100;
        effect.transform.position=body.position;
        // Radius measured against actual transformed blade vertices, including parent scale and flipping.
        foreach(Vector2 vertex in sword.sprite.vertices)
            radius=Mathf.Max(radius,Vector2.Distance(body.position,sword.transform.TransformPoint(vertex)));
        float frameDiameter=Mathf.Min(effect.sprite.bounds.size.x,effect.sprite.bounds.size.y);
        effect.transform.localScale=Vector3.one*(2*radius/Mathf.Max(.01f,frameDiameter)*1.08f);
    }
    void Cleanup()
    {
        if(effect!=null) Dispose(effect.gameObject);
        effect=null;
        if(frames!=null) foreach(var frame in frames) if(frame!=null) Dispose(frame);
        frames=null;cachedSheet=null;radius=0;
    }
    static void Dispose(Object obj)
    { if(Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
}
