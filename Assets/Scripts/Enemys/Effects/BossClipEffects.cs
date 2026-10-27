using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Deterministic clip sampling: the same frame is rendered when playing or scrubbing backwards.
[ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(1000)]
public sealed class BossClipEffects : MonoBehaviour
{
    public enum Style { Down, Up, Thrust, Spin, Punch, Dash, Dust, Hit, Death, Buff }
    public enum Anchor { Body, Weapon, Feet, Hitbox }
    [Serializable] public class Cue
    {
        public float time, duration = .28f, angle, scale = 1;
        public Style style;
        public Anchor anchor = Anchor.Weapon;
        public Vector2 offset;
        public bool alignBlade, loop, weaponSide;
    }
    [Serializable] public class Track
    {
        public AnimationClip clip;
        public bool runtime;
        public Cue[] cues = Array.Empty<Cue>();
    }
    public Animator animator;
    public Transform body, weapon, feet;
    public BossEffects source;
    public Track[] tracks = Array.Empty<Track>();
    public bool runtimeEnabled;
    public MobEffects.Effect down, up, thrust, spin, punch, dash, dust, hit, death, buff;
    public float pixelsPerUnit = 64;
    public int sortingOrder = 100;
    readonly Dictionary<MobEffects.Effect, Sprite[]> frames = new Dictionary<MobEffects.Effect, Sprite[]>();
    readonly HashSet<Sprite> owned = new HashSet<Sprite>();
    readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
    public int VisibleCount => renderers.FindAll(r => r != null && r.enabled && r.sprite != null).Count;
    public bool OwnsCurrentAttack
    {
        get
        {
            if (!runtimeEnabled || !isActiveAndEnabled || animator == null || !animator.isActiveAndEnabled) return false;
            var info = animator.GetCurrentAnimatorClipInfo(0);
            foreach (var c in info) foreach (var track in tracks)
                if (track.clip == c.clip && track.runtime && c.weight > .5f) return true;
            return false;
        }
    }
    void LateUpdate()
    {
        if (!Application.isPlaying || !runtimeEnabled) return;
        if (animator == null || !animator.isActiveAndEnabled) { ClearVisuals(); return; }
        var info = animator.GetCurrentAnimatorClipInfo(0);
        AnimationClip clip = null; float weight = 0;
        foreach (var item in info) if (item.weight > weight) { clip = item.clip; weight = item.weight; }
        if (clip == null) { ClearVisuals(); return; }
        var state = animator.GetCurrentAnimatorStateInfo(0);
        float normalized = state.normalizedTime;
        float time = (clip.isLooping ? Mathf.Repeat(normalized, 1) : normalized) * clip.length;
        Sample(clip, time, true);
    }
    public void Configure(BossEffects fx)
    {
        source = fx;
        down = fx.attack; up = fx.upward; thrust = fx.horizontal; spin = fx.spin;
        punch = fx.punch; dash = fx.dash; dust = fx.chase; hit = fx.hit; death = fx.death;
        pixelsPerUnit = fx.pixelsPerUnit;
    }
    public Track Find(AnimationClip clip)
    { foreach (var t in tracks) if (t.clip == clip) return t; return null; }
    public void Sample(AnimationClip clip, float time, bool runtime = false)
    {
        Track track = Find(clip);
        if (track == null || (runtime && !track.runtime)) { ClearVisuals(); return; }

        int count = 0;
        float facing = transform.lossyScale.x < 0 ? -1 : 1;
        if (runtime && source != null)
        {
            var boss = source.GetComponent<MidBossController>();
            if (boss != null) facing = boss.FXDirection;
        }
        foreach (var cue in track.cues)
        {
            float age = time - cue.time;
            if (age < -.0001f || age >= cue.duration) continue;
            MobEffects.Effect effect = GetEffect(cue.style);
            Sprite[] sheet = Frames(effect);
            if (sheet.Length == 0) continue;
            bool combat = cue.style <= Style.Punch;
            float fps = Mathf.Max(1, effect.fps) * (combat ? 1.5f : 1);
            int frame = Mathf.FloorToInt(Mathf.Max(0, age) * fps);
            frame = cue.loop ? frame % sheet.Length : Mathf.Min(frame, sheet.Length - 1);
            float cueFacing = cue.weaponSide && weapon != null && body != null
                ? (weapon.position.x >= body.position.x ? 1 : -1) : facing;
            SpriteRenderer sr = Renderer(count++);
            sr.enabled = true; sr.sprite = sheet[frame];
            sr.flipX = effect.artFacesRight ? cueFacing < 0 : cueFacing > 0; sr.flipY = effect.flipY;
            sr.sortingLayerID = SortingLayer.NameToID("Front"); sr.sortingOrder = sortingOrder;
            Transform anchor = cue.anchor == Anchor.Weapon ? weapon : cue.anchor == Anchor.Feet ? feet : body;
            Vector3 position = anchor != null ? anchor.position : transform.position;
            if (cue.anchor == Anchor.Hitbox) position = source != null ? source.transform.position : transform.position;
            if (cue.anchor == Anchor.Weapon && weapon != null)
            {
                var blade = weapon.GetComponent<SpriteRenderer>();
                if (blade != null && blade.sprite != null) position = blade.bounds.center;
            }
            Vector2 offset = cue.offset + effect.offset;
            position += new Vector3(offset.x * facing, offset.y, 0);
            sr.transform.position = position;
            float angle = cue.angle * facing;
            if (cue.alignBlade && weapon != null)
            {
                Vector3 axis = weapon.TransformVector(Vector3.up);
                angle = Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg - (facing < 0 ? 180 : 0);
            }
            sr.transform.rotation = Quaternion.Euler(0, 0, angle);
            float multiplier = cue.style == Style.Death ? 1 : 1.5f;
            sr.transform.localScale = Vector3.one * Mathf.Max(.01f, effect.scale * cue.scale * multiplier);
            Color color = cue.style == Style.Buff ? new Color(.55f, .85f, 1, .55f) : Color.white;
            color.a *= Mathf.Clamp01((cue.duration - age) / Mathf.Min(.12f, cue.duration * .35f));
            sr.color = color;
        }
        for (int i = count; i < renderers.Count; i++) if (renderers[i] != null) renderers[i].enabled = false;
    }
    MobEffects.Effect GetEffect(Style style)
    {
        switch (style)
        {
            case Style.Up: return up; case Style.Thrust: return thrust; case Style.Spin: return spin;
            case Style.Punch: return punch; case Style.Dash: return dash; case Style.Dust: return dust;
            case Style.Hit: return hit; case Style.Death: return death; case Style.Buff: return buff;
            default: return down;
        }
    }
    Sprite[] Frames(MobEffects.Effect effect)
    {
        if (effect == null) return Array.Empty<Sprite>();
        if (effect.frames != null && effect.frames.Length > 0) return effect.frames;
        if (frames.TryGetValue(effect, out var cached)) return cached;
        if (effect.sheet == null || effect.columns < 1) return Array.Empty<Sprite>();
        float width = effect.sheet.width / (float)effect.columns;
        var result = new Sprite[effect.columns];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = Sprite.Create(effect.sheet, new Rect(i * width, 0, width, effect.sheet.height),
                new Vector2(.5f, .5f), Mathf.Max(1, pixelsPerUnit), 0, SpriteMeshType.FullRect);
            result[i].hideFlags = HideFlags.HideAndDontSave; owned.Add(result[i]);
        }
        frames.Add(effect, result); return result;
    }
    SpriteRenderer Renderer(int index)
    {
        while (renderers.Count <= index)
        {
            var obj = new GameObject("Boss Clip FX (Temporary)") { hideFlags = HideFlags.HideAndDontSave };
            if (gameObject.scene.IsValid() && gameObject.scene.isLoaded) SceneManager.MoveGameObjectToScene(obj, gameObject.scene);
            renderers.Add(obj.AddComponent<SpriteRenderer>());
        }
        return renderers[index];
    }
    public void ClearVisuals()
    { foreach (var sr in renderers) if (sr != null) sr.enabled = false; }
    public void Release()
    {
        foreach (var sr in renderers) if (sr != null) Dispose(sr.gameObject);
        renderers.Clear(); foreach (var sprite in owned) if (sprite != null) Dispose(sprite);
        owned.Clear(); frames.Clear();
    }
    void OnDisable() { Release(); }
    void OnDestroy() { Release(); }
    static void Dispose(UnityEngine.Object obj)
    { if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
}
