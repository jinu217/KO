using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Deterministic clip sampling: the same frame is rendered when playing or scrubbing backwards.
[ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(1300)]
public sealed class BossClipEffects : MonoBehaviour
{
    public enum Style { Down, Up, Thrust, Spin, Punch, Dash, Dust, Hit, Death, Buff, Sand }
    public enum Anchor { Body, Weapon, Feet, Hitbox, WeaponTip }
    public enum BuffKind { Legacy, Offense, Defense, Speed, Recovery }
    [Serializable] public class Cue
    {
        public float time, duration = .28f, angle, scale = 1;
        public Style style;
        public Anchor anchor = Anchor.Weapon;
        public Vector2 offset;
        public bool alignBlade, loop, weaponSide, backwards, groundContact, motionOnly;
        public BuffKind buffKind;
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
    public MobEffects.Effect down, up, thrust, spin, punch, dash, dust, hit, death, buff, sand;
    public MobEffects.Effect buffOffense, buffDefense, buffSpeed, buffRecovery;
    public float pixelsPerUnit = 64;
    public int sortingOrder = 100;
    readonly Dictionary<MobEffects.Effect, Sprite[]> frames = new Dictionary<MobEffects.Effect, Sprite[]>();
    readonly HashSet<Sprite> owned = new HashSet<Sprite>();
    readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
    readonly Dictionary<Cue, Vector3> contactPositions = new Dictionary<Cue, Vector3>();
    readonly Dictionary<Track, Track> motionTracks = new Dictionary<Track, Track>();
    AnimationClip contactClip;
    float contactTime;
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
    public void Sample(AnimationClip clip, float time, bool runtime = false, bool supplementalOnly = false)
    {
        var slashTrail=GetComponent<BossWeaponSlashTrail>();
        if(!runtime && slashTrail!=null)slashTrail.Sample(clip,time);
        if (clip != contactClip || time < contactTime) contactPositions.Clear();
        contactClip = clip; contactTime = time;
        if(runtime && clip.name.StartsWith("MainBoss5_skill1") && source!=null && source.GetComponent<Stage5_MainBoss>()?.OrbitSwordActive==true)
        { ClearVisuals(); return; }
        Track track = Find(clip);
        if (track == null) { ClearVisuals(); return; }
        bool motionMatched = BossMotionFXSettings.Applies(source, clip);
        if (motionMatched) track = MotionTrack(track);
        // Supplemental dust must never take ownership of the existing attack FX.
        if(runtime && !track.runtime) supplementalOnly = true;

        int count = 0;
        float facing = transform.lossyScale.x < 0 ? -1 : 1;
        if (runtime && source != null)
        {
            var boss = source.GetComponent<MidBossController>();
            if (boss != null) facing = boss.FXDirection;
        }
        foreach (var cue in track.cues)
        {
            if (supplementalOnly && cue.style != Style.Buff && cue.style != Style.Sand && !(motionMatched && cue.motionOnly)) continue;
            if (runtime && clip.name.StartsWith("MidBoss 1") && cue.style != Style.Buff && cue.style != Style.Sand) continue;
            if((cue.style==Style.Spin || cue.style==Style.Down || cue.style==Style.Up) && slashTrail!=null && slashTrail.CoversImpact(clip,cue.time))continue;
            float age = time - cue.time;
            if (age < -.0001f || age >= cue.duration) continue;
            MobEffects.Effect effect = cue.style == Style.Buff && cue.buffKind != BuffKind.Legacy
                ? BuffEffect(cue.buffKind) : GetEffect(cue.style);
            BossEffects.Kind motionKind = cue.style == Style.Up ? BossEffects.Kind.Up : cue.style == Style.Thrust ? BossEffects.Kind.Thrust
                : cue.style == Style.Spin ? BossEffects.Kind.Spin : cue.style == Style.Punch ? BossEffects.Kind.Punch : BossEffects.Kind.Down;
            // The cross passes swing the blade; their old thrust cue must not emit Sting.
            if(motionMatched && clip.name.ToLowerInvariant().Contains("mainboss5_skill5") && cue.style == Style.Thrust)
                motionKind = BossEffects.Kind.Horizontal;
            if(motionMatched && cue.style <= Style.Punch && BossMotionFXSettings.Settings != null)
                effect = BossMotionFXSettings.Settings.Effect(motionKind, effect);
            Sprite[] sheet = Frames(effect);
            if (sheet.Length == 0) continue;
            bool combat = cue.style <= Style.Punch;
            float fps = Mathf.Max(1, effect.fps) * (combat ? 1.5f : 1);
            int frame = Mathf.FloorToInt(Mathf.Max(0, age) * fps);
            frame = cue.loop ? frame % sheet.Length : Mathf.Min(frame, sheet.Length - 1);
            float cueFacing = cue.weaponSide && weapon != null && body != null
                ? (weapon.position.x >= body.position.x ? 1 : -1) : facing * (cue.backwards ? -1 : 1);
            SpriteRenderer sr = Renderer(count++);
            sr.enabled = true; sr.sprite = sheet[frame];
            sr.flipX = effect.artFacesRight ? cueFacing < 0 : cueFacing > 0; sr.flipY = effect.flipY;
            sr.sortingLayerID = SortingLayer.NameToID("Front"); sr.sortingOrder = sortingOrder;
            Transform anchor = cue.anchor == Anchor.Weapon ? weapon : cue.anchor == Anchor.Feet ? feet : body;
            Vector3 position = anchor != null ? anchor.position : transform.position;
            if (cue.anchor == Anchor.Hitbox) position = source != null ? source.transform.position : transform.position;
            if(motionMatched && cue.style == Style.Spin && weapon != null)
            {
                var blade = weapon.GetComponent<SpriteRenderer>();
                position = blade != null ? blade.bounds.center : weapon.position;
            }
            if (cue.anchor == Anchor.Weapon && weapon != null)
            {
                var blade = weapon.GetComponent<SpriteRenderer>();
                if (blade != null && blade.sprite != null) position = blade.bounds.center;
            }
            if (cue.anchor == Anchor.Feet && (cue.style == Style.Dash || cue.style == Style.Dust) && source != null)
                position = source.MovementContact(cueFacing, cue.backwards);
            if (cue.anchor == Anchor.WeaponTip && weapon != null)
            {
                var blade = weapon.GetComponent<SpriteRenderer>();
                if (blade != null && blade.sprite != null)
                {
                    var bounds = blade.sprite.bounds;
                    Vector3 a = weapon.TransformPoint(new Vector3(bounds.center.x, bounds.min.y, 0));
                    Vector3 b = weapon.TransformPoint(new Vector3(bounds.center.x, bounds.max.y, 0));
                    position = a.y < b.y ? a : b;
                }
            }
            Vector2 offset = cue.offset + effect.offset;
            position += new Vector3(offset.x * facing, offset.y, 0);
            if (cue.anchor == Anchor.Feet && (cue.style == Style.Dash || cue.style == Style.Dust) && source != null)
                position = source.MovementContact(cueFacing, cue.backwards);
            if (cue.style == Style.Sand)
            {
#if UNITY_EDITOR
                if (!runtime) position = PreviewContact(clip, cue.time, position);
#endif
                if (clip.name.StartsWith("MidBoss 1")) position.x -= .8f;
                if(cue.groundContact) position.y = GroundHeight(position);
                if (runtime && contactPositions.TryGetValue(cue, out var contact)) position = contact;
                else contactPositions[cue] = position;
            }
            if(combat && source!=null && source.mainBoss && source.stage==2)position.y+=.15f;
            sr.transform.position = position;
            float angle = cue.angle * facing;
            if(motionMatched && motionKind == BossEffects.Kind.Horizontal) angle = -90 * facing;
            if (cue.alignBlade && weapon != null)
            {
                Vector3 axis = weapon.TransformVector(Vector3.up);
                angle = Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg - (facing < 0 ? 180 : 0);
            }
            sr.transform.rotation = Quaternion.Euler(0, 0, angle);
            float multiplier = cue.style == Style.Death ? 1 : 1.5f;
            sr.transform.localScale = Vector3.one * Mathf.Max(.01f, effect.scale * cue.scale * multiplier);
            if(combat && weapon!=null && (source==null || !source.mainBoss || source.stage!=4))
                BossEffects.FitWeaponEffect(sr,weapon);
            if(weapon!=null && (cue.style==Style.Down || cue.style==Style.Up || cue.style==Style.Spin
                || motionMatched && motionKind==BossEffects.Kind.Horizontal))
                BossWeaponSlashAttachment.Apply(sr,weapon.GetComponent<SpriteRenderer>());
            if (cue.style == Style.Buff && cue.buffKind != BuffKind.Legacy)
            {
                Bounds bounds = BuffBounds();
                // Fit the full sheet canvas once, avoiding size pumping as particles converge.
                float height = Mathf.Max(.2f, sheet[0].rect.height / sheet[0].pixelsPerUnit);
                sr.transform.localScale = Vector3.one * Mathf.Max(.1f, bounds.size.y * .95f / height * cue.scale);
                sr.transform.position = bounds.center + new Vector3(cue.offset.x * facing, cue.offset.y, 0);
            }
            if (cue.style == Style.Dash || cue.style == Style.Dust)
                BossEffects.AlignTrail(sr, position, cueFacing);
            if (cue.style == Style.Sand)
                sr.transform.position += Vector3.up * (position.y - BossEffects.VisibleBounds(sr).min.y);
            Color color = cue.style == Style.Buff && cue.buffKind != BuffKind.Legacy ? new Color(1,1,1,.8f) :
                cue.style == Style.Buff && !clip.name.StartsWith("MidBoss 1") ? new Color(.55f, .85f, 1, .55f) : Color.white;
            color.a *= Mathf.Clamp01((cue.duration - age) / Mathf.Min(.12f, cue.duration * .35f));
            sr.color = color;
        }
        for (int i = count; i < renderers.Count; i++) if (renderers[i] != null) renderers[i].enabled = false;
    }
    Track MotionTrack(Track track)
    {
        if(motionTracks.TryGetValue(track, out var cached)) return cached;
        var cues = new List<Cue>(track.cues);
        if(track.clip.name == "MainBoss3_Skill2" || track.clip.name == "MainBoss3_Skill3")
            cues.Add(new Cue { time=track.clip.name == "MainBoss3_Skill2" ? 1.38f : .42f,
                duration=.24f, style=Style.Thrust, anchor=Anchor.Weapon, alignBlade=true, motionOnly=true });
        if(track.clip.name.StartsWith("MainBoss5_"))
            foreach(var e in track.clip.events)
                if(e.stringParameter == "Attack.DashStart" || e.stringParameter == "Skill3.DashStart")
                    cues.Add(new Cue { time=e.time, duration=.24f, style=Style.Thrust, anchor=Anchor.Weapon, alignBlade=true });
        cached = new Track { clip=track.clip, runtime=track.runtime, cues=cues.ToArray() };
        motionTracks.Add(track, cached);
        return cached;
    }
    float GroundHeight(Vector3 contact)
    {
        var controller = source != null ? source.GetComponent<MidBossController>() : GetComponentInParent<MidBossController>();
        var collider = controller != null ? controller.GetComponent<Collider2D>() : null;
        float floor = collider != null ? collider.bounds.min.y : feet != null ? feet.position.y : contact.y;
        if(controller != null)
        {
            var hit = Physics2D.Raycast(new Vector2(contact.x, floor + .75f), Vector2.down, 1.5f, controller.wallLayer);
            if(hit.collider != null) return hit.point.y;
        }
        if(feet != null)
        {
            var foot = feet.GetComponent<SpriteRenderer>();
            if(foot != null && foot.sprite != null) floor = BossEffects.VisibleBounds(foot).min.y;
        }
        return floor;
    }
#if UNITY_EDITOR
    // Evaluate the contact pose without changing the scene's currently sampled pose.
    Vector3 PreviewContact(AnimationClip clip, float time, Vector3 fallback)
    {
        if (weapon == null || animator == null) return fallback;
        var blade = weapon.GetComponent<SpriteRenderer>();
        if (blade == null || blade.sprite == null) return fallback;
        var chain = new List<Transform>();
        Transform root = animator.transform;
        for (Transform t = weapon; t != null && t != root; t = t.parent) chain.Add(t);
        Matrix4x4 matrix = root.localToWorldMatrix;
        var bindings = UnityEditor.AnimationUtility.GetCurveBindings(clip);
        for (int i = chain.Count - 1; i >= 0; i--)
        {
            Transform t = chain[i];
            string path = UnityEditor.AnimationUtility.CalculateTransformPath(t, root);
            Vector3 p = t.localPosition, e = t.localEulerAngles, scale = t.localScale;
            foreach (var binding in bindings)
            {
                if (binding.path != path || binding.type != typeof(Transform)) continue;
                string n = binding.propertyName;
                int axis = n.EndsWith(".x") ? 0 : n.EndsWith(".y") ? 1 : n.EndsWith(".z") ? 2 : -1;
                if (axis < 0) continue;
                float value = UnityEditor.AnimationUtility.GetEditorCurve(clip, binding).Evaluate(time);
                if (n.StartsWith("m_LocalPosition")) p[axis] = value;
                else if (n.StartsWith("m_LocalScale")) scale[axis] = value;
                else if (n.StartsWith("localEulerAngles")) e[axis] = value;
            }
            matrix *= Matrix4x4.TRS(p, Quaternion.Euler(e), scale);
        }
        var bounds = blade.sprite.bounds;
        Vector3 a = matrix.MultiplyPoint3x4(new Vector3(bounds.center.x, bounds.min.y, 0));
        Vector3 b = matrix.MultiplyPoint3x4(new Vector3(bounds.center.x, bounds.max.y, 0));
        return a.y < b.y ? a : b;
    }
#endif
    MobEffects.Effect BuffEffect(BuffKind kind)
    {
        switch(kind)
        {
            case BuffKind.Offense: return buffOffense;
            case BuffKind.Defense: return buffDefense;
            case BuffKind.Speed: return buffSpeed;
            case BuffKind.Recovery: return buffRecovery;
            default: return buff;
        }
    }
    Bounds BuffBounds()
    {
        // Same visual bounds in scene preview and play, independent of physics collider size.
        bool found=false;Bounds result=new Bounds(body != null ? body.position : transform.position,Vector3.one);
        foreach(var sr in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if(sr.sprite==null || (sr.name!="Body" && sr.name!="Head" && sr.name!="Foot_F" && sr.name!="Foot_B"))continue;
            var bounds=BossEffects.VisibleBounds(sr);
            if(!found){result=bounds;found=true;}else result.Encapsulate(bounds);
        }
        return result;
    }
    MobEffects.Effect GetEffect(Style style)
    {
        switch (style)
        {
            case Style.Up: return up; case Style.Thrust: return thrust; case Style.Spin: return spin;
            case Style.Punch: return punch; case Style.Dash: return dash; case Style.Dust: return dust;
            case Style.Hit: return hit; case Style.Death: return death; case Style.Buff: return buff; case Style.Sand: return sand;
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
        contactPositions.Clear();
        motionTracks.Clear();
        renderers.Clear(); foreach (var sprite in owned) if (sprite != null) Dispose(sprite);
        owned.Clear(); frames.Clear();
    }
    void OnDisable() { Release(); }
    void OnDestroy() { Release(); }
    static void Dispose(UnityEngine.Object obj)
    { if (Application.isPlaying) Destroy(obj); else DestroyImmediate(obj); }
}
