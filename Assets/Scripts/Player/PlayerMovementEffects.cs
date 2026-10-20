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
        public bool active;
    }
    void Awake()
    {
        player = GetComponent<PlayerController>();
        if (walkSheet != null && walkColumns > 0 && walkSheet.width % walkColumns == 0)
        {
            walkFrames = new Sprite[walkColumns];
            int width = walkSheet.width / walkColumns;
            for (int i = 0; i < walkColumns; i++)
                walkFrames[i] = Sprite.Create(walkSheet, new Rect(i * width, 0, width, walkSheet.height), new Vector2(.5f, 0), Mathf.Max(1, pixelsPerUnit));
        }
        walking = NewParticle("Walk FX");
    }
    void OnEnable()
    {
        if (player == null) player = GetComponent<PlayerController>();
        player.MovementFXJump += Jump;
        player.MovementFXDash += Dash;
    }
    void OnDisable()
    {
        if (player != null) { player.MovementFXJump -= Jump; player.MovementFXDash -= Dash; }
        foreach (Particle p in particles) { if (p.obj != null) p.obj.SetActive(false); p.active = false; }
    }
    Vector3 Origin()
    {
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
        p.frames = frames; p.fps = Mathf.Max(1, fps); p.age = 0; p.active = true;
        p.obj.transform.position = Origin() + new Vector3(offset.x * direction, offset.y, 0);
        p.obj.transform.localScale = Vector3.one * Mathf.Max(.01f, scale);
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
    void LateUpdate()
    {
        bool move = player.enabled && player.MovementFXWalking;
        float direction = player.MovementFXDirection;
        if (move && !walking.active) StartParticle(walking, walkFrames, walkFPS, walkScale, walkOffset, direction, walkArtFacesRight ? direction < 0 : direction > 0);
        if (!move) { walking.active = false; walking.obj.SetActive(false); }
        foreach (Particle p in particles)
        {
            if (!p.active) continue;
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
    void OnDestroy()
    {
        foreach (Particle p in particles) if (p.obj != null) Destroy(p.obj);
        if (walkFrames != null) foreach (Sprite sprite in walkFrames) if (sprite != null) Destroy(sprite);
    }
}
