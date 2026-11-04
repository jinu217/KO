using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerController))]
public sealed class PlayerMovementEffects : MonoBehaviour
{
    public Texture2D walkSheet;
    public int walkColumns = 8;
    public Sprite[] jumpFrames, dashFrames;
    public float pixelsPerUnit = 64, walkFPS = 12, jumpFPS = 16, dashFPS = 20;
    public float walkScale = 2, jumpScale = 2, dashScale = 2;
    public Vector2 walkOffset = new Vector2(-.7f, -.1f);
    public Vector2 jumpOffset = Vector2.zero;
    public Vector2 dashOffset = new Vector2(0, -.1f);
    public bool walkArtFacesRight = false;
    public bool jumpArtFacesRight = false;
    // Preserve the existing serialized dash toggle; reversing the source image is independent.
    public bool reverseDashImage = true;
    public bool dashArtFacesRight = true;
    public string sortingLayer = "Front";
    public int sortingOrder = 20;
    public Transform groundAnchor;
    [Min(0)] public float walkStartDelay = .15f;
    [Min(0)] public float additionalWalkStartDelay = .15f;
    float movingTime;
    PlayerController player;
    Sprite[] walkFrames;
    readonly List<Particle> particles = new List<Particle>();
    Particle walking;
    sealed class Particle
    {
        public GameObject obj;
        public SpriteRenderer renderer;
        public Sprite[] frames;
        public float age, fps;
        public bool active, fading;
        public float fadeAge;
    }
    void Awake()
    {
        player = GetComponent<PlayerController>();
        EnsureWalkFrames();
        walking = NewParticle("Walk FX");
    }
    void EnsureWalkFrames()
    {
        if (walkFrames != null) return;
        if (walkSheet != null && walkColumns > 0 && walkSheet.width % walkColumns == 0)
        {
            walkFrames = new Sprite[walkColumns];
            int width = walkSheet.width / walkColumns;
            for (int i = 0; i < walkColumns; i++)
                walkFrames[i] = Sprite.Create(walkSheet, new Rect(i * width, 0, width, walkSheet.height), new Vector2(.5f, 0), Mathf.Max(1, pixelsPerUnit));
        }
    }
    void OnEnable()
    {
        if (player == null) player = GetComponent<PlayerController>();
        player.MovementFXJump += Jump;
        player.MovementFXDash += Dash;
    }
    void OnDisable()
    {
        movingTime = 0f;
        if (player != null) { player.MovementFXJump -= Jump; player.MovementFXDash -= Dash; }
        foreach (Particle p in particles) { if (p.obj != null) p.obj.SetActive(false); p.active = false; }
    }
    Vector3 Origin()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && previewOrigin.HasValue) return previewOrigin.Value;
#endif
        Transform anchor = groundAnchor != null ? groundAnchor : player.groundCheck;
        if (anchor != null) return anchor.position;
        Collider2D col = GetComponent<Collider2D>();
        return col != null ? new Vector3(col.bounds.center.x, col.bounds.min.y, transform.position.z) : transform.position;
    }
    Particle NewParticle(string label)
    {
        var p = new Particle();
        p.obj = new GameObject(label); p.renderer = p.obj.AddComponent<SpriteRenderer>();
        p.renderer.sortingLayerName = sortingLayer; p.renderer.sortingOrder = sortingOrder;
        p.obj.SetActive(false); particles.Add(p); return p;
    }
    void StartParticle(Particle p, Sprite[] frames, float fps, float scale, Vector2 offset, float direction, bool flip)
    {
        if (frames == null || frames.Length == 0) return;
        p.frames = frames; p.fps = Mathf.Max(1, fps); p.age = 0; p.active = true; p.fading = false; p.fadeAge = 0;
        p.renderer.color = Color.white;
        p.obj.transform.position = Origin() + new Vector3(offset.x * direction, offset.y, 0);
        p.obj.transform.localScale = Vector3.one * Mathf.Max(.01f, scale) * 1.5f;
        p.renderer.flipX = flip; p.renderer.sprite = frames[0]; p.obj.SetActive(true);
    }
    void Burst(Sprite[] frames, float fps, float scale, Vector2 offset, float direction, bool flip)
    {
        if (frames == null || frames.Length == 0) return;
        Particle free = particles.Find(p => p != walking && !p.active);
        if (free == null) { if (particles.Count >= 17) return; free = NewParticle("Movement Burst FX"); }
        StartParticle(free, frames, fps, scale, offset, direction, flip);
    }
    void Jump(float direction) { Burst(jumpFrames, jumpFPS, jumpScale, jumpOffset, direction, jumpArtFacesRight ? direction < 0 : direction > 0); }
    void Dash(float direction) { Burst(dashFrames, dashFPS, dashScale, dashOffset, direction, (dashArtFacesRight ? direction < 0 : direction > 0) ^ reverseDashImage); }
    void StopWalking()
    {
        if (walking == null || !walking.active) return;
        Particle tail = particles.Find(p => p != walking && !p.active);
        if (tail == null && particles.Count < 17) tail = NewParticle("Walk Stop Fade FX");
        if (tail != null)
        {
            tail.frames = walking.frames;
            tail.active = true; tail.fading = true; tail.fadeAge = 0;
            tail.renderer.sprite = walking.renderer.sprite;
            tail.renderer.flipX = walking.renderer.flipX;
            tail.renderer.flipY = walking.renderer.flipY;
            tail.renderer.color = Color.white;
            tail.obj.transform.position = walking.obj.transform.position;
            tail.obj.transform.localScale = walking.obj.transform.localScale;
            tail.obj.SetActive(true);
        }
        walking.active = false; walking.obj.SetActive(false);
    }
    void LateUpdate()
    {
        bool move = player.enabled && player.MovementFXWalking;
        float direction = player.MovementFXDirection;
        movingTime = move ? movingTime + Time.deltaTime : 0f;
        if (move && movingTime >= walkStartDelay + additionalWalkStartDelay && !walking.active) StartParticle(walking, walkFrames, walkFPS, walkScale, walkOffset, direction, walkArtFacesRight ? direction < 0 : direction > 0);
        if (!move) StopWalking();
        foreach (Particle p in particles)
        {
            if (!p.active) continue;
            if (p.fading)
            {
                p.fadeAge += Time.deltaTime;
                Color color = p.renderer.color;
                color.a = 1f - Mathf.Clamp01((p.fadeAge - .06f) / .14f);
                p.renderer.color = color;
                if (p.fadeAge >= .20f) { p.active = false; p.obj.SetActive(false); }
                continue;
            }
            p.age += Time.deltaTime;
            int frame = Mathf.FloorToInt(p.age * p.fps);
            if (p == walking)
            {
                frame %= p.frames.Length;
                p.obj.transform.position = Origin() + new Vector3(walkOffset.x * direction, walkOffset.y, 0);
                p.renderer.flipX = walkArtFacesRight ? direction < 0 : direction > 0;
            }
            else if (frame >= p.frames.Length) { p.active = false; p.obj.SetActive(false); continue; }
            p.renderer.sprite = p.frames[frame];
        }
    }
#if UNITY_EDITOR
    Particle previewParticle;
    Vector3? previewOrigin;
    public void PreviewMovement(string kind, Vector3 origin, float direction, float age)
    {
        if (Application.isPlaying) return;
        EnsureWalkFrames();
        if (previewParticle == null) previewParticle = NewParticle("Player Movement FX Preview");
        previewParticle.obj.hideFlags = HideFlags.HideAndDontSave;
        var scene = gameObject.scene;
        if (scene.IsValid() && scene.isLoaded && previewParticle.obj.scene != scene)
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(previewParticle.obj, scene);
        previewParticle.obj.SetActive(false);
        if (age < 0) return;
        bool run = kind == "run", dash = kind == "dash";
        var frames = run ? walkFrames : dash ? dashFrames : jumpFrames;
        if (frames == null || frames.Length == 0) return;
        float fps = run ? walkFPS : dash ? dashFPS : jumpFPS;
        float scale = run ? walkScale : dash ? dashScale : jumpScale;
        Vector2 offset = run ? walkOffset : dash ? dashOffset : jumpOffset;
        bool facesRight = run ? walkArtFacesRight : dash ? dashArtFacesRight : jumpArtFacesRight;
        bool flip = (facesRight ? direction < 0 : direction > 0) ^ (dash && reverseDashImage);
        previewOrigin = origin;
        try { StartParticle(previewParticle, frames, fps, scale, offset, direction, flip); }
        finally { previewOrigin = null; }
        int frame = Mathf.FloorToInt(age * Mathf.Max(1, fps));
        if (run) frame %= frames.Length;
        previewParticle.obj.SetActive(frame < frames.Length);
        if (frame < frames.Length) previewParticle.renderer.sprite = frames[frame];

    }
    public void ClearPreview()
    {
        if (Application.isPlaying) return;
        if (previewParticle != null)
        {
            particles.Remove(previewParticle);
            if (previewParticle.obj != null) DestroyImmediate(previewParticle.obj);
            previewParticle = null;
        }
        if (walkFrames != null) foreach (var frame in walkFrames) if (frame != null) DestroyImmediate(frame);
        walkFrames = null;
    }
#endif
    void OnDestroy()
    {
        foreach (Particle p in particles) if (p.obj != null) Destroy(p.obj);
        if (walkFrames != null) foreach (Sprite sprite in walkFrames) if (sprite != null) Destroy(sprite);
    }
}
