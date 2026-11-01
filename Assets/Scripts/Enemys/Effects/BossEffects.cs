using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public sealed class BossEffects : MonoBehaviour
{
    public MobEffects.Effect attack = new MobEffects.Effect { flipY = true };
    public MobEffects.Effect shot = new MobEffects.Effect();
    public MobEffects.Effect hit = new MobEffects.Effect();
    public MobEffects.Effect death = new MobEffects.Effect { columns = 24, scale = 1.3f };
    public MobEffects.Effect notice = new MobEffects.Effect { columns = 1, fps = 20, scale = 1.5f, artFacesRight = false };
    public MobEffects.Effect chase = new MobEffects.Effect { columns = 8, fps = 16, scale = 1.5f, offset = new Vector2(-.3f, .05f), artFacesRight = false };
    [Min(1)] public float pixelsPerUnit = 64;
    [Min(.05f)] public float chaseInterval = .18f;
    public string sortingLayerOverride = "";
    public int sortingOrderOffset = 5;
    public int stage;
    public bool mainBoss;
    public MobEffects.Effect upward = new MobEffects.Effect();
    public MobEffects.Effect horizontal = new MobEffects.Effect();
    public MobEffects.Effect spin = new MobEffects.Effect();
    public MobEffects.Effect punch = new MobEffects.Effect();
    public MobEffects.Effect dash = new MobEffects.Effect { fps = 20, scale = 3f, artFacesRight = false };
    [Header("둔기 / 도끼 보스 가로베기 이펙트")]
    [Min(.01f)] public float midBoss1HorizontalScale = 1.35f;
    public float midBoss1HorizontalHeight = .25f;
    public float additionalHeavyHorizontalHeight = .15f;
    public Transform weaponAnchor;
    public Animator motionAnimator;
    [System.Serializable]
    public class ImpactTiming
    {
        public AnimationClip clip;
        public float horizontalImpact;
        public float[] impacts;
    }
    public ImpactTiming[] comboTimings;
    public enum Kind { None, Down, Up, Horizontal, Thrust, Spin, Punch, Uppercut }
    [System.Serializable]
    public class StepOverride
    {
        public int skillIndex = -1, stepIndex, pulse;
        public Kind kind;
        public float angle;
        public Vector2 offset;
    }
    public StepOverride[] overrides;
    BossSkillData executing;
    int skillIndex;
    BossAttackStepData currentStep;
    readonly Dictionary<BossAttackStepData, int> pulses = new Dictionary<BossAttackStepData, int>();
    MobEffectInstance dashInstance;
    Vector3 previousPosition;
    float lastDashMotion;
    MidBossController enemy;
    bool noticed;
    float movingTime;
    [Min(0)] public float chaseStartDelay = .3f;
    Collider2D body;
    float nextDust;
    MobEffectInstance dust;

    void Awake() { enemy = GetComponent<MidBossController>(); body = GetComponent<Collider2D>(); if (motionAnimator == null) motionAnimator = GetComponentInChildren<Animator>(true); }
    void OnEnable() { nextDust = 0; movingTime = 0; noticed = false; previousPosition = transform.position; }
    void OnDisable() { EndSkill(); if (dust != null) Destroy(dust.gameObject); dust = null; }
    Vector3 Center() { return body != null ? body.bounds.center : transform.position; }
    Vector3 Feet() { Vector3 p = Center(); if (body != null) p.y = body.bounds.min.y; return p; }
    void Update()
    {
        if (enemy == null || !enemy.FXChasing || !Grounded() || (stage == 1 && !mainBoss && !MidOneRunning()))
        { movingTime = 0; if (dust != null) dust.StopAndFade(); dust = null; return; }
        movingTime += Time.deltaTime;
        if (movingTime < chaseStartDelay) return;
        if (dust != null)
        {
            dust.transform.position = ChaseOrigin() + new Vector3(chase.offset.x * enemy.FXDirection, chase.offset.y, 0);
            dust.SetFacing(enemy.FXDirection, chase.artFacesRight);
            return;
        }
        if (Time.time < nextDust) return;
        nextDust = Time.time + chaseInterval;
        dust = Play(chase, ChaseOrigin(), enemy.FXDirection);
    }
    public void BeginSkill(BossSkillData skill, int index)
    { EndSkill(); executing = skill; skillIndex = index; pulses.Clear(); }
    public void BeginStep(BossAttackStepData step) { currentStep = step; }
    public void EndStep() { currentStep = null; ClearDash(); }
    public void EndSkill() { executing = null; EndStep(); }
    void ClearDash() { if (dashInstance != null) Destroy(dashInstance.gameObject); dashInstance = null; }
    BossClipEffects clipEffects;
    bool ClipOwnsAttack
    {
        get
        {
            if (!mainBoss || stage != 5) return false;
            if (clipEffects == null) clipEffects = GetComponentInChildren<BossClipEffects>(true);
            return clipEffects != null && clipEffects.OwnsCurrentAttack;
        }
    }
    void LateUpdate()
    {
        if (enemy != null && dust != null)
            AlignTrail(dust.GetComponent<SpriteRenderer>(), MovementContact(enemy.FXDirection), enemy.FXDirection);
        if (ClipOwnsAttack) { ClearDash(); previousPosition = transform.position; return; }
        Vector3 delta = transform.position - previousPosition;
        previousPosition = transform.position;
        // Boxing footwork needs horizontal travel, not gravity/contact jitter.
        bool translated = mainBoss && stage == 4 ? Mathf.Abs(delta.x) > .001f : delta.sqrMagnitude > .000001f;
        if (translated) lastDashMotion = Time.time;
        bool moving = Time.time - lastDashMotion < .08f;
        bool dashStep = currentStep != null && (currentStep.actionType == BossActionType.DashAttack ||
            (mainBoss && stage == 4 && skillIndex == 3 && currentStep.actionType == BossActionType.MoveForward));
        if (!dashStep || !moving || enemy == null || enemy.IsDead) { ClearDash(); return; }
        if (dashInstance == null) dashInstance = Play(dash, DashOrigin(), enemy.FXDirection, true);
        if (dashInstance != null)
        {
            dashInstance.transform.position = DashOrigin() + new Vector3(dash.offset.x * enemy.FXDirection, dash.offset.y, 0);
            float angle = Mathf.Atan2(delta.y, Mathf.Abs(delta.x)) * Mathf.Rad2Deg;
            dashInstance.transform.rotation = Quaternion.Euler(0, 0, angle * enemy.FXDirection);
            {
                dashInstance.transform.rotation = Quaternion.identity;
                AlignTrail(dashInstance.GetComponent<SpriteRenderer>(), MovementContact(enemy.FXDirection), enemy.FXDirection);
            }
        }
    }
    bool UsesHeavyWeapon()
    {
        if ((stage == 1 && !mainBoss) || (stage == 2 && mainBoss)) return true;
        if (weaponAnchor == null) return false;
        string name = weaponAnchor.name.ToLowerInvariant();
        return name == "axe" || name == "club" || name == "mace" || name == "hammer";
    }
    float HeavyHorizontalHeight => midBoss1HorizontalHeight + additionalHeavyHorizontalHeight;
    bool MidOneRunning()
    {
        if (motionAnimator == null || !motionAnimator.isActiveAndEnabled) return false;
        foreach (var clip in motionAnimator.GetCurrentAnimatorClipInfo(0))
            if (clip.weight > .5f && clip.clip.name.ToLowerInvariant().Contains("run")) return true;
        return false;
    }
    Vector3 ChaseOrigin() => MovementContact(enemy.FXDirection) - new Vector3(chase.offset.x * enemy.FXDirection, chase.offset.y, 0);
    public Vector3 MovementContact(float direction)
    {
        var root = motionAnimator != null ? motionAnimator.transform : transform;
        foreach (var foot in root.GetComponentsInChildren<SpriteRenderer>(true))
            if (foot.name == "Foot_F" && foot.sprite != null)
            {
                var b = VisibleBounds(foot);
                float x = stage == 1 && !mainBoss ? RearFeet(root, direction).x : (direction > 0 ? b.min.x : b.max.x) - direction * .08f;
                return new Vector3(x, b.min.y, foot.transform.position.z);
            }
        return Feet();
    }
    // Authored front leg is Foot_F, independent of facing or stride.
    public static Vector3 RearFeet(Transform root, float direction)
    {
        foreach (var foot in root.GetComponentsInChildren<Transform>(true))
            if (foot.name == "Foot_F")
                return foot.TransformPoint(new Vector3(-.4f, -.65f, 0));
        return root.position;
    }

    // The contact point is the leading bottom edge, not the sheet centre.
    public static void AlignTrail(SpriteRenderer sr, Vector3 contact, float facing)
    {
        if (sr == null || sr.sprite == null) return;
        Bounds bounds = VisibleBounds(sr);
        Vector3 edge = new Vector3(facing > 0 ? bounds.max.x : bounds.min.x, bounds.min.y, sr.transform.position.z);
        sr.transform.position += contact - edge;
    }

    // Cache alpha silhouettes by texture rectangle, including dynamically created FX frames.
    // Row endpoints retain exact extrema even when the foot is rotated or mirrored.
    static readonly Dictionary<(Texture2D, Rect), Vector2[]> silhouettes = new Dictionary<(Texture2D, Rect), Vector2[]>();
    public static Bounds VisibleBounds(SpriteRenderer renderer) => VisibleBounds(renderer, out _);
    static Bounds VisibleBounds(SpriteRenderer renderer, out Vector3 bottom)
    {
        bottom = renderer != null ? renderer.transform.position : Vector3.zero;
        if (renderer == null || renderer.sprite == null) return new Bounds();
        var sprite = renderer.sprite;
        Rect rect = sprite.rect;
        var key = (sprite.texture, rect);
        if (!silhouettes.TryGetValue(key, out var points))
        {
            var previous = RenderTexture.active;
            RenderTexture rt = null; Texture2D readable = null;
            try
            {
                rt = RenderTexture.GetTemporary(sprite.texture.width, sprite.texture.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(sprite.texture, rt); RenderTexture.active = rt;
                int w = Mathf.RoundToInt(rect.width), h = Mathf.RoundToInt(rect.height);
                readable = new Texture2D(w, h, TextureFormat.RGBA32, false);
                readable.ReadPixels(rect, 0, 0); readable.Apply();
                var pixels = readable.GetPixels32(); var edge = new List<Vector2>();
                for (int y = 0; y < h; y++)
                {
                    int min = w, max = -1;
                    for (int x = 0; x < w; x++) if (pixels[y * w + x].a > 0) { min = Mathf.Min(min, x); max = x; }
                    if (max < 0) continue;
                    edge.Add(new Vector2(min, y)); edge.Add(new Vector2(max + 1, y));
                    edge.Add(new Vector2(min, y + 1)); edge.Add(new Vector2(max + 1, y + 1));
                }
                points = edge.ToArray(); silhouettes[key] = points;
            }
            finally
            {
                RenderTexture.active = previous;
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                if (readable != null) { if (Application.isPlaying) Destroy(readable); else DestroyImmediate(readable); }
            }
        }
        if (points.Length == 0) return new Bounds(renderer.transform.position, Vector3.zero);
        Bounds bounds = new Bounds(); bool first = true;
        float lowest = float.PositiveInfinity, left = 0, right = 0;
        foreach (var pixel in points)
        {
            Vector2 p = (pixel - sprite.pivot) / sprite.pixelsPerUnit;
            if (renderer.flipX) p.x = -p.x;
            if (renderer.flipY) p.y = -p.y;
            Vector3 world = renderer.transform.TransformPoint(p);
            if (world.y < lowest - .00001f) { lowest = world.y; left = right = world.x; }
            else if (Mathf.Abs(world.y - lowest) <= .00001f) { left = Mathf.Min(left, world.x); right = Mathf.Max(right, world.x); }
            if (first) { bounds = new Bounds(world, Vector3.zero); first = false; } else bounds.Encapsulate(world);
        }
        bottom = new Vector3((left + right) * .5f, lowest, renderer.transform.position.z);
        return bounds;
    }
    public bool TryWeaponImpact(BossAttackStepData step, out Vector3 point)
    {
        point = default;
        if (stage != 3 || mainBoss || step.actionType != BossActionType.Slam || weaponAnchor == null) return false;
        var blade = weaponAnchor.GetComponent<SpriteRenderer>();
        if (blade == null || blade.sprite == null) return false;
        VisibleBounds(blade, out point);
        return true;
    }

    Vector3 DashOrigin() => MovementContact(enemy.FXDirection) - new Vector3(dash.offset.x * enemy.FXDirection, dash.offset.y, 0);
    int StepIndex(BossAttackStepData step) => executing?.active?.attackSteps?.IndexOf(step) ?? -1;
    int Ordinal(BossAttackStepData step, int pulse)
    {
        int n = pulse;
        if (executing?.active?.attackSteps == null) return n;
        foreach (var s in executing.active.attackSteps)
        {
            if (ReferenceEquals(s, step)) break;
            if (s != null && (s.actionType == BossActionType.MeleeAttack || s.actionType == BossActionType.Slam ||
                (stage == 4 && mainBoss && s.actionType == BossActionType.DashAttack))) n += Mathf.Max(1, s.attackCount);
        }
        return n;
    }
    int TotalHits()
    {
        int n = 0;
        if (executing?.active?.attackSteps != null)
            foreach (var s in executing.active.attackSteps)
                if (s != null && (s.actionType == BossActionType.MeleeAttack || s.actionType == BossActionType.Slam)) n += Mathf.Max(1, s.attackCount);
        return n;
    }
    // Clip seconds shared by gameplay and scene sampling. Values come from the
    // authored attack poses; movement/landing-only actions keep their own clocks.
    static readonly Dictionary<string, float[]> MotionImpacts = new Dictionary<string, float[]>
    {
        { "MainBoss1_Attack", new[] { 9f / 12f, 19f / 12f } },
        { "MainBoss1_Prepared_Attack", new[] { 9f / 12f, 19f / 12f } },
        { "MainBoss1_Prepared_Attack_Phase3", new[] { 9f / 12f, 19f / 12f } },
        { "MainBoss1_skill1", new[] { 3f / 12f, 12f / 12f, 24f / 12f } },
        { "MainBoss1_Prepared_skill1", new[] { 3f / 12f, 12f / 12f, 24f / 12f } },
        { "MainBoss1_Prepared_skill1_Phase3", new[] { 3f / 12f, 12f / 12f, 24f / 12f } },
        { "MidBoss4_Skill1", new[] { .85f, 2.50f } },
        { "MidBoss5_Attack", new[] { 32f / 12f } },
        { "MidBoss2_Attack", new[] { 6f / 12f, 13f / 12f } },
        { "MidBoss2_Skill2", new[] { 5f / 12f, 10f / 12f, 15f / 12f, 20f / 12f, 25f / 12f } },
        { "MidBoss2_Skill2-2", new[] { 5f / 12f, 11f / 12f, 17f / 12f, 23f / 12f, 29f / 12f } },
        { "MidBoss2_Skill2-3", new[] { 5f / 12f, 11f / 12f, 17f / 12f, 23f / 12f, 29f / 12f } },
        { "MidBoss2_Skill3", new[] { 14f / 12f } },
        { "MidBoss2_Skill3-2", new[] { 14f / 12f } },
        { "MidBoss2_Skill3-3", new[] { 14f / 12f } },
        { "MainBoss2_Attack", new[] { 20f / 12f } },
        { "MainBoss2_Skill1", new[] { 9f / 12f, 23f / 12f } },
        { "MainBoss2_Skill2", new[] { 15f / 12f, 24f / 12f } },
        { "MidBoss3_Attack", new[] { .70f, 1.42f } },
        { "MainBoss3_Attack", new[] { .34f, 1.10f } },
        { "MainBoss3_Skill1", new[] { .60f } },
        { "MainBoss3_Skill2", new[] { .34f, .59f, .84f, 1.09f } },
        { "MidBoss4_Attack", new[] { .60f, 1.30f, 2.25f } }
    };
    public static int ClipSkillIndex(int stageNumber, bool main, string clipName, int fallback)
    {
        string name = clipName.ToLowerInvariant();
        if (stageNumber == 3 && !main)
        {
            if (name == "midboss3_skill1") return 2; // buff
            if (name == "midboss3_skill2") return 0; // rush
            if (name == "midboss3_skill3") return 1; // leap
        }
        if (stageNumber == 3 && main && name == "mainboss3_skill3") return 3;
        if (stageNumber == 2 && main && name == "mainboss2_skill3") return 3;
        return fallback;
    }
    public bool TryMotionImpact(AnimationClip clip, int ordinal, out float impact)
    {
        impact = 0;
        if (clip == null || ordinal < 0) return false;
        if (clip.name.StartsWith("MainBoss5_"))
        {
            int n = 0;
            foreach (var cue in clip.events)
                if (cue.stringParameter == "Attack.Slam" || cue.stringParameter.StartsWith("Skill1.Hit."))
                {
                    if (n++ == ordinal) { impact = cue.time; return true; }
                }
        }
        if (MotionImpacts.TryGetValue(clip.name, out var authored) && ordinal < authored.Length)
        { impact = authored[ordinal]; return true; }
        if (comboTimings != null)
            foreach (var timing in comboTimings)
                if (timing.clip == clip && timing.impacts != null && ordinal < timing.impacts.Length)
                { impact = timing.impacts[ordinal]; return true; }
        return false;
    }
    public static bool TryMovementWindow(AnimationClip clip, BossActionType action, out float start, out float end)
    {
        start = end = 0;
        if (clip == null) return false;
        if (clip.name == "MidBoss3_Skill2" && action == BossActionType.DashAttack)
        { start = 1.08f; end = 2.08f; return true; }
        if (clip.name == "MidBoss3_Skill3")
        {
            if (action == BossActionType.Jump) { start = .85f; end = 1.60f; return true; }
            if (action == BossActionType.Slam) { start = end = 1.60f; return true; }
        }
        if (clip.name == "MainBoss3_Skill2" && action == BossActionType.DashAttack)
        { start = 1.38f; end = 1.88f; return true; }
        return false;
    }
    AnimationClip TimedClip(BossAttackStepData step, int pulse)
    {
        if ((stage == 1 && !mainBoss) || step == null || step.actionType != BossActionType.MeleeAttack || motionAnimator == null
            || motionAnimator.runtimeAnimatorController == null) return null;
        int ordinal = Ordinal(step, pulse);
        var current = motionAnimator.GetCurrentAnimatorClipInfo(0);
        foreach (var info in current)
            if (info.weight > .5f && TryMotionImpact(info.clip, ordinal, out _) &&
                SkillIndexForClip(info.clip) == skillIndex)
                return info.clip;
        foreach (var clip in motionAnimator.runtimeAnimatorController.animationClips)
        {
            int index = SkillIndexForClip(clip);
            if (index == skillIndex && TryMotionImpact(clip, ordinal, out _)) return clip;
        }
        return null;
    }
    int SkillIndexForClip(AnimationClip clip)
    {
        int index = -1;
        var match = System.Text.RegularExpressions.Regex.Match(clip.name.ToLowerInvariant(), @"skill([1-9])");
        if (match.Success) index = int.Parse(match.Groups[1].Value) - 1;
        if (clip.name.Contains("Combo3")) index = 1;
        return ClipSkillIndex(stage, mainBoss, clip.name, index);
    }
    public bool HasMotionTiming(BossAttackStepData step, int pulse = 0) => TimedClip(step, pulse) != null;
    public bool ImpactAllowed { get; private set; } = true;
    public IEnumerator WaitForImpact(BossAttackStepData step, int pulse)
    {
        ImpactAllowed = true;
        if (stage == 1 && !mainBoss)
        {
        // MidBoss1: 모든 콤보 타격을 실제 클립 시각까지 판정과 FX 함께 보류한다.
        if (stage != 1 || mainBoss || TotalHits() < 2 ||
            motionAnimator == null || comboTimings == null) yield break;
        float waited = 0;
        while (motionAnimator.isActiveAndEnabled && enemy != null && !enemy.IsDead && waited < 8f)
        {
            bool matched = false, pending = false;
            var state = motionAnimator.GetCurrentAnimatorStateInfo(0);
            foreach (var clip in motionAnimator.GetCurrentAnimatorClipInfo(0))
                foreach (var timing in comboTimings)
                    if (timing.clip == clip.clip)
                    {
                        int ordinal = Ordinal(step, pulse);
                        float impact = timing.impacts != null && ordinal < timing.impacts.Length
                            ? timing.impacts[ordinal] : ordinal == TotalHits() - 1 ? timing.horizontalImpact : -1f;
                        matched = true;
                        pending |= state.normalizedTime * clip.clip.length + .001f < impact;
                    }
            if (matched && !pending) yield break;
            if (!matched && waited > .4f) yield break;
            waited += Time.deltaTime;
            yield return null;
        }
            yield break;
        }
        var targetClip = TimedClip(step, pulse);
        if (targetClip == null) yield break;
        TryMotionImpact(targetClip, Ordinal(step, pulse), out float targetImpact);
        ImpactAllowed = false;
        bool entered = false;
        float missing = 0, stalled = 0, previous = -1;
        while (motionAnimator != null && motionAnimator.isActiveAndEnabled && enemy != null && !enemy.IsDead)
        {
            bool matched = false;
            var state = motionAnimator.GetCurrentAnimatorStateInfo(0);
            foreach (var info in motionAnimator.GetCurrentAnimatorClipInfo(0))
                if (info.clip == targetClip && info.weight > .5f)
                {
                    matched = entered = true;
                    float now = state.normalizedTime * targetClip.length;
                    if (now + .001f >= targetImpact) { ImpactAllowed = true; yield break; }
                    stalled = now > previous + .00001f ? 0 : stalled + Time.deltaTime;
                    previous = now;
                }
            // Interrupted/missing/frozen motion must never produce a delayed ghost hit.
            if (!matched) { missing += Time.deltaTime; if (entered || missing > .5f) yield break; }
            if (stalled > 1f) yield break;
            yield return null;
        }
    }
    Kind Resolve(BossAttackStepData step, int pulse, out float angle)
    {
        angle = 0;
        int ordinal = Ordinal(step, pulse), index = StepIndex(step);
        if (step.actionType == BossActionType.SpawnPrefab) return Kind.None;
        if (stage == 4 && mainBoss)
        {
            if (skillIndex == 1 && ordinal == 1) return Kind.Uppercut;
            if (skillIndex == 0 && ordinal == 3) { angle = 20; return Kind.Punch; }
            if (skillIndex == 3) { angle = ordinal % 2 == 0 ? -25 : 25; return Kind.Punch; }
            return Kind.Punch;
        }
        if (stage == 4 && !mainBoss && skillIndex < 0 && StepIndex(step) == 2) return Kind.Thrust;
        if (step.actionType == BossActionType.DashAttack)
            return mainBoss && stage == 5 && skillIndex == 2 && index == 2 ? Kind.Spin : Kind.None;
        if (step.actionType == BossActionType.Slam) return Kind.Down;
        if (stage == 1)
        {
            if (mainBoss) return (skillIndex < 0 && ordinal == 0) || (skillIndex == 0 && ordinal == 2) ? Kind.Horizontal : Kind.Down;
            return TotalHits() >= 2 && ordinal == TotalHits() - 1 ? Kind.Horizontal : Kind.Down;
        }
        if (stage == 2)
        {
            if (!mainBoss)
            {
                if (skillIndex == 2) return Kind.Down;
                if (skillIndex == 1) return Kind.Thrust;
                return ordinal % 2 == 0 ? Kind.Thrust : Kind.Up;
            }
            if (skillIndex < 0) return Kind.Up;
            if (skillIndex == 1) return ordinal == 0 ? Kind.Up : Kind.Down;
            return Kind.Down;
        }
        if (stage == 3)
        {
            if (!mainBoss) return skillIndex < 0 && ordinal == 1 ? Kind.Up : Kind.Down;
            if (skillIndex == 1) return Kind.Thrust;
            if (step.actionType == BossActionType.PersistentHitbox || (skillIndex < 0 && ordinal == 0)) return Kind.Spin;
            return Kind.Down;
        }
        if (stage == 4)
        {
            if (skillIndex == 0 || skillIndex == 1) return Kind.Spin;
            return ordinal >= 2 ? Kind.Thrust : Kind.Down;
        }
        if (stage == 5)
        {
            if (skillIndex == 0)
            {
                if (!mainBoss)
                {
                    switch (pulse % 5)
                    {
                        case 0: angle = -45; return Kind.Thrust;
                        case 1: return Kind.Up;
                        case 2: angle = 45; return Kind.Thrust;
                        case 3: return Kind.Down;
                        default: return Kind.Thrust;
                    }
                }
                switch (pulse % 4)
                { case 0: return Kind.Down; case 1: return Kind.Spin; case 2: return Kind.None; default: return Kind.Thrust; }
            }
            if (step.actionType == BossActionType.PersistentHitbox) return Kind.Spin;
        }
        return Kind.Down;
    }
    public Kind ResolveForPreview(BossSkillData skill, int index, BossAttackStepData step, int pulse, out float angle)
    {
        var oldSkill = executing; int oldIndex = skillIndex;
        try
        {
            executing = skill; skillIndex = index;
            Kind kind = Resolve(step, pulse, out angle);
            if (overrides != null) foreach (var rule in overrides)
                if (rule.skillIndex == index && rule.stepIndex == StepIndex(step) && rule.pulse == pulse)
                { kind = rule.kind; angle = rule.angle; break; }
            return kind;
        }
        finally { executing = oldSkill; skillIndex = oldIndex; }
    }
    SpriteRenderer punchFront, punchBack;
    // The animation generator uses this same front/rear-hand sequence.
    public bool TryPunchGeometry(int index, int stepIndex, out SpriteRenderer hand, out Bounds bounds)
    {
        hand = null; bounds = default;
        if (stage != 4 || !mainBoss || stepIndex < 0) return false;
        bool front = index < 0 ? stepIndex >= 2 : index == 3 ? (stepIndex == 5 || stepIndex % 2 == 0)
            : index == 1 ? stepIndex != 1 : stepIndex == 2 || stepIndex == 4;
        if (punchFront == null || punchBack == null)
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
            { if (renderer.name == "Hand_F") punchFront = renderer; else if (renderer.name == "Hand_B") punchBack = renderer; }
        hand = front ? punchFront : punchBack;
        if (hand == null || hand.sprite == null) return false;
        bounds = VisibleBounds(hand);
        // A small contact margin around the visible fist, scaled with the rig.
        bounds.Expand(new Vector3(.9f * Mathf.Abs(hand.transform.lossyScale.x), .9f * Mathf.Abs(hand.transform.lossyScale.y), 0));
        return true;
    }
    public bool TryAttackBounds(BossAttackStepData step, out Bounds bounds)
    {
        if (TryPunchGeometry(skillIndex, StepIndex(step), out _, out bounds)) return true;
        if (stage != 5 || step == null || step.damage <= 0 || weaponAnchor == null) return false;
        var blade = weaponAnchor.GetComponent<SpriteRenderer>();
        if (blade == null || blade.sprite == null) return false;
        bounds = VisibleBounds(blade);
        bounds.Expand(new Vector3(.2f, .2f, 0));
        return true;
    }
    public void PlayStep(BossAttackStepData step, Vector3 center, float direction)
    {
        if (ClipOwnsAttack) return;
        if (step == null || step.damage <= 0) return;
        int pulse = pulses.TryGetValue(step, out int n) ? n : 0;
        pulses[step] = pulse + 1;
        if (stage == 1 && !mainBoss && skillIndex == 1 && Ordinal(step, pulse) == 1) return;
        Kind kind = Resolve(step, pulse, out float angle);
        Vector2 extra = Vector2.zero;
        if (overrides != null)
            foreach (var rule in overrides)
                if (rule.skillIndex == skillIndex && rule.stepIndex == StepIndex(step) && rule.pulse == pulse)
                { kind = rule.kind; angle = rule.angle; extra = rule.offset; break; }
        if (kind == Kind.None) return;
        // 이기어검은 몸 앞에 고정하지 않고 실제 떠 있는 검 위치를 사용한다.
        if (stage == 5 && skillIndex == 0 && weaponAnchor != null)
        { var sr = weaponAnchor.GetComponent<SpriteRenderer>(); center = sr != null ? sr.bounds.center : weaponAnchor.position; }
        var effect = kind == Kind.Up ? upward : kind == Kind.Horizontal || kind == Kind.Thrust ? horizontal :
            kind == Kind.Spin ? spin : kind == Kind.Punch || kind == Kind.Uppercut ? punch : attack;
        if (TryWeaponImpact(step, out var weaponPoint))
            center = weaponPoint - new Vector3(effect.offset.x * direction, effect.offset.y, 0);
        if (kind == Kind.Uppercut) angle = 65;
        bool fromFist = TryPunchGeometry(skillIndex, StepIndex(step), out var fist, out var fistBounds);
        if (fromFist) center = fistBounds.center - new Vector3((effect.offset.x + extra.x) * direction, effect.offset.y + extra.y, 0);
        bool heavyHorizontal = UsesHeavyWeapon() && kind == Kind.Horizontal;
        if (heavyHorizontal) center.y += HeavyHorizontalHeight;
        var instance = Play(effect, center + new Vector3(extra.x * direction, extra.y, 0), direction, false, 1.5f);
        if (instance != null)
        {
            if (heavyHorizontal) instance.transform.localScale *= Mathf.Max(.01f, midBoss1HorizontalScale);
            float worldAngle = angle * direction;
            if (kind == Kind.Thrust && stage == 5 && skillIndex == 0 && weaponAnchor != null)
            {
                Vector3 blade = weaponAnchor.TransformVector(Vector3.up);
                worldAngle = Mathf.Atan2(blade.y, blade.x) * Mathf.Rad2Deg - (direction < 0 ? 180f : 0f);
            }
            if (fromFist)
            {
                var arm = fist.transform.parent.GetComponent<SpriteRenderer>();
                Vector3 axis = fistBounds.center - (arm != null ? VisibleBounds(arm).center : fist.transform.parent.position);
                if (axis.sqrMagnitude > .0001f) worldAngle = Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg - (direction < 0 ? 180f : 0f);
                instance.transform.localScale *= Mathf.Abs(fist.transform.lossyScale.y);
                instance.FollowAnchor(fist.transform, fistBounds.center);
            }
            instance.transform.rotation = Quaternion.Euler(0, 0, worldAngle);
        }
    }
    bool Grounded()
    {
        if (body == null || enemy == null) return false;
        Vector3 feet = Feet();
        return Physics2D.Raycast(feet + Vector3.up * .05f, Vector2.down, .15f, enemy.wallLayer).collider != null;
    }
    public void PlayReturnAttack(Vector3 position, float direction)
    {
        bool heavy = UsesHeavyWeapon();
        if (heavy) position.y += HeavyHorizontalHeight;
        var instance = Play(horizontal, position, direction, false, 1.5f);
        if (heavy && instance != null)
            instance.transform.localScale *= Mathf.Max(.01f, midBoss1HorizontalScale);
    }
    public void PlayShot(Vector3 position, float direction) { } // 생성물에는 추가 FX를 붙이지 않음
    public void PlayAttack(Vector3 position, float direction, bool ranged = false)
    { Play(ranged ? shot : attack, position, direction, false, 1.5f); }
    public void PlayHit(Vector2 source) { Play(hit, Center(), source.x < transform.position.x ? -1 : 1); }
    public void PlayDeath() { Play(death, Center(), enemy != null ? enemy.FXDirection : 1); }
    public void PlayNotice(float direction) { if (noticed) return; noticed = true; Play(notice, Feet(), direction); }
#if UNITY_EDITOR
    public MobEffectInstance PreviewStep(BossSkillData skill, int index, BossAttackStepData step, int pulse, Vector3 center, float direction, float age)
    {
        if (Application.isPlaying || age < 0) return null;
        var oldSkill = executing; int oldIndex = skillIndex;
        bool existed = pulses.TryGetValue(step, out int oldPulse);
        MobEffectInstance.LastPreviewSpawn = null;
        try
        {
            executing = skill; skillIndex = index; pulses[step] = pulse;
            PlayStep(step, center, direction);
            var result = MobEffectInstance.LastPreviewSpawn;
            if (result != null) result.PreviewSample(age);
            return result;
        }
        finally
        {
            executing = oldSkill; skillIndex = oldIndex;
            if (existed) pulses[step] = oldPulse; else pulses.Remove(step);
            MobEffectInstance.LastPreviewSpawn = null;
        }
    }
    public MobEffectInstance PreviewMovement(bool dashMotion, float direction, float age)
    {
        if (Application.isPlaying || age < 0) return null;
        var effect = dashMotion ? dash : chase;
        var collider = GetComponent<Collider2D>();
        Vector3 origin = collider != null ? new Vector3(collider.bounds.center.x, collider.bounds.min.y, transform.position.z) : transform.position;
        origin = MovementContact(direction) - new Vector3(effect.offset.x * direction, effect.offset.y, 0);
        var instance = Play(effect, origin, direction, true);
        if (instance != null)
        {
            instance.PreviewSample(age);
            AlignTrail(instance.GetComponent<SpriteRenderer>(), MovementContact(direction), direction);
        }
        return instance;
    }
#endif
    MobEffectInstance Play(MobEffects.Effect effect, Vector3 position, float direction, bool loop = false, float speedMultiplier = 1f)
    {
        if (!isActiveAndEnabled || effect == null) return null;
        if (effect.sheet == null && (effect.frames == null || effect.frames.Length == 0)) return null;
        int layer = 0, order = 0, best = int.MinValue;
        foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            int value = SortingLayer.GetLayerValueFromID(renderer.sortingLayerID);
            if (value > best || (value == best && renderer.sortingOrder > order))
            { best = value; layer = renderer.sortingLayerID; order = renderer.sortingOrder; }
        }
        // Detached effects must also render above the outer sorting group.
        foreach (var group in GetComponentsInParent<SortingGroup>(true))
        {
            int value = SortingLayer.GetLayerValueFromID(group.sortingLayerID);
            if (value > best || (value == best && group.sortingOrder > order))
            { best = value; layer = group.sortingLayerID; order = group.sortingOrder; }
        }
        if (!string.IsNullOrEmpty(sortingLayerOverride)) layer = SortingLayer.NameToID(sortingLayerOverride);
        position += new Vector3(effect.offset.x * Mathf.Sign(direction), effect.offset.y, 0);
        return MobEffectInstance.Spawn(effect, position, direction, pixelsPerUnit, layer,
            order + sortingOrderOffset, gameObject.scene, loop || ReferenceEquals(effect, chase), ReferenceEquals(effect, death) ? 1f : 1.5f, speedMultiplier);
    }
}
