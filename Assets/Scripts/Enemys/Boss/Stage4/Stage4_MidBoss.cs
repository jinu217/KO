using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Stage 4 only. The base controller still owns detection, cooldowns, stats and death.
// Empty execution copies bypass the old shared jump/step executor.
// Gameplay below uses the same clip clock as MidBoss4_AnimGenerators.
public sealed class Stage4_MidBoss : MidBossController
{
    [Header("MidBoss 4 애니메이션")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private Animator actionAnimator;
    [SerializeField] private Transform scythe;
    [SerializeField] private Vector2 scytheHitboxLocalOffset = new Vector2(0, .8f);
    private readonly Dictionary<int, BossSkillData> executionCopies = new Dictionary<int, BossSkillData>();
    private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();
    private readonly RaycastHit2D[] pullHits = new RaycastHit2D[16];
    private BossSkillData sourceSkill;
    private float lockedScaleX = 1, actionRate = 1;
    private int lockedDirection = 1;
    private bool actionLock;
    private Vector2 jumpStart;
    private float jumpDistance, savedGravity;
    private bool jumpOwned, slamHit;
    private float pushTravel;
    private static readonly string[] StateNames = { "MidBoss4_Attack", "MidBoss4_Skill1", "MidBoss4_Skill2", "MidBoss4_Buff" };
    private static readonly float[] Durations = { 3.1f, 3.3f, 3.6f, 3f };

    protected override void Awake()
    {
        base.Awake();
        if (visualRoot == null)
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
                if (child.name == "MidBoss 4" && child.Find("Body") != null) { visualRoot = child; break; }
        if (visualRoot == null && transform.Find("Body") != null) visualRoot = transform;
        if (visualRoot != null)
        {
            lockedScaleX = Mathf.Abs(visualRoot.localScale.x);
            if (actionAnimator == null) actionAnimator = visualRoot.GetComponent<Animator>();
            if (scythe == null) scythe = visualRoot.Find("Body/Shoulder_B/Forearm_B/Hand_B/Scythe");
        }
        if (actionAnimator == null)
            Debug.LogError("MidBoss 4: 시각 루트의 Animator를 연결하고 Setup Animator를 실행하세요.", this);
    }

    protected override BossSkillData GetBasicAttackData() { return ExecutionCopy(-1, data.basicAttack); }
    protected override BossSkillData GetSkillData(int index)
    {
        BossSkillData original = base.GetSkillData(index);
        return index >= 0 && index < 3 ? ExecutionCopy(index, original) : original;
    }

    private BossSkillData ExecutionCopy(int index, BossSkillData original)
    {
        if (original == null) return null;
        BossSkillData copy;
        if (!executionCopies.TryGetValue(index, out copy))
        {
            copy = JsonUtility.FromJson<BossSkillData>(JsonUtility.ToJson(original));
            copy.skillType = BossSkillType.Active;
            copy.preDelay = copy.postDelay = 0;
            copy.active = null;
            executionCopies[index] = copy;
        }
        // Preserve authoring changes to AI decisions; no modifications to the asset at runtime.
        copy.cooldown = original.cooldown;
        copy.castRange = original.castRange;
        copy.minimumPhase = original.minimumPhase;
        copy.forceCastMinDistance = original.forceCastMinDistance;
        copy.forceCastRemainingCooldown = original.forceCastRemainingCooldown;
        return copy;
    }

    protected override void ApplyFacingDirection()
    {
        if (actionLock) return;
        if (visualRoot == null) return;
        Vector3 scale = visualRoot.localScale;
        scale.x = lockedScaleX * lookDirection;
        visualRoot.localScale = scale;
    }
    public override void RefreshSkillDirection()
    {
        if (actionLock || isAttacking || isDead) return;
        base.RefreshSkillDirection();
    }

    protected override void OnSkillExecutionStarted(BossSkillData skill, int index)
    {
        sourceSkill = index < 0 ? data.basicAttack : data.skills[index];
        Effects?.BeginSkill(sourceSkill, index);
        lockedDirection = lookDirection;
        ApplyFacingDirection();
        actionLock = true;
        slamHit = false; pushTravel = 0;
        jumpDistance = Mathf.Clamp((PlayerPosition.x - transform.position.x) * lockedDirection, 0, 4);
        actionRate = 1f / Mathf.Max(.01f, GetSkillDelay(1f));
        StopHorizontal();
        if (actionAnimator == null) return;
        actionAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        actionAnimator.applyRootMotion = false;
        actionAnimator.speed = actionRate; // freeze attack-speed multiplier for this whole cast
        int slot = Mathf.Clamp(index + 1, 0, 3);
        int hash = Animator.StringToHash("Base Layer." + StateNames[slot]);
        if (actionAnimator.HasState(0, hash)) actionAnimator.Play(hash, 0, 0);
        else Debug.LogError("MidBoss 4 Animator 상태가 없습니다: " + StateNames[slot], this);
    }

    protected override IEnumerator WaitForSkillAnimation(BossSkillData skill, int index)
    {
        int slot = Mathf.Clamp(index + 1, 0, 3);
        float length = Mathf.Max(Durations[slot], DataDuration(sourceSkill)), clock = 0, previous = -1;
        bool buffApplied = false;
        int pulse = 0;
        while (clock < length && !isDead)
        {
            // Animation has evaluated before reading the moving scythe's world position.
            yield return new WaitForEndOfFrame();
            if (isDead) { RestoreJump(); yield break; }
            previous = clock;
            clock = Mathf.Min(length, clock + Time.deltaTime * actionRate);
            StopHorizontal();
            lookDirection = lockedDirection;
            if (index < 0)
            {
                if (clock >= 2.07f && previous < 2.25f)
                {
                    var push = Step(2); float distance = Mathf.Min(push.maxMoveDistance - pushTravel, push.moveSpeed * Time.deltaTime * actionRate);
                    if (distance > 0) { MoveSafely(new Vector2(lockedDirection * distance, 0)); pushTravel += distance; }
                }
                float hitTime = 0;
                for (int i = 0; i < 3; i++)
                {
                    BossAttackStepData step = Step(i);
                    if (step == null) continue;
                    hitTime += Mathf.Max(0, step.preDelay);
                    if (previous < hitTime && clock >= hitTime)
                        Damage(step, RootCenter(step));
                    hitTime += Mathf.Max(0, step.postDelay);
                }
            }
            else if (index == 0)
            {
                if (previous < .85f && clock >= .85f) Damage(Step(0), RootCenter(Step(0)));
                if (clock >= 1.4f && !slamHit && rb != null)
                {
                    if (!jumpOwned) { jumpStart = rb.position; savedGravity = rb.gravityScale; rb.gravityScale = 0; rb.linearVelocity = Vector2.zero; jumpOwned = true; }
                    float u = Mathf.Clamp01((clock - 1.4f) / 1.1f);
                    float height = u <= .54545f ? 2.1f * Mathf.Sin(u / .54545f * Mathf.PI * .5f) :
                        2.1f * Mathf.Cos((u - .54545f) / .45455f * Mathf.PI * .5f);
                    Vector2 target = jumpStart + new Vector2(lockedDirection * jumpDistance * u, height);
                    bool blocked = MoveSafely(target - rb.position);
                    if (clock >= 2.5f || (clock >= 2f && blocked && IsGrounded()))
                    { Damage(Step(2), RootCenter(Step(2))); slamHit = true; RestoreJump(); }
                }
            }
            else if (index == 1)
            {
                BossAttackStepData pull = Step(0), spin = Step(1);
                if (pull != null)
                {
                    float start = sourceSkill.preDelay + pull.preDelay;
                    float end = start + pull.duration;
                    if (clock >= start && previous < end) Pull(pull);
                    if (spin != null)
                    {
                        float spinStart = end + pull.postDelay + spin.preDelay;
                        float interval = Mathf.Max(.01f, spin.interval);
                        while (pulse < spin.attackCount && spinStart + pulse * interval <= clock
                            && spinStart + pulse * interval < spinStart + spin.duration)
                        {
                            Damage(spin, RootCenter(spin));
                            pulse++;
                        }
                    }
                }
            }
            else if (!buffApplied && clock >= sourceSkill.preDelay)
            {
                buffApplied = true;
                ApplySkillBuff(sourceSkill.buff);
            }
        }
    }

    protected override void OnSkillExecutionFinished(int index)
    {
        RestoreJump();
        actionLock = false;
        if (actionAnimator != null && !isDead) actionAnimator.speed = 1;
        StopHorizontal();
    }

    private void RestoreJump()
    { if (jumpOwned && rb != null) { rb.gravityScale = savedGravity; rb.linearVelocity = Vector2.zero; } jumpOwned = false; }
    private bool MoveSafely(Vector2 delta)
    {
        if (rb == null || delta.magnitude < .00001f) return false;
        var filter = new ContactFilter2D(); filter.SetLayerMask(wallLayer); filter.useTriggers = false;
        float distance = delta.magnitude, allowed = distance;
        int count = rb.Cast(delta / distance, filter, pullHits, distance + .01f);
        for (int i = 0; i < count; i++) if (Vector2.Dot(pullHits[i].normal, delta) < 0)
                allowed = Mathf.Min(allowed, Mathf.Max(0, pullHits[i].distance - .01f));
        rb.position += delta / distance * allowed; return allowed < distance - .001f;
    }
    protected override void Die() { RestoreJump(); base.Die(); }
    private void LateUpdate()
    {
        if (isDead) return;
        if (actionLock)
        {
            lookDirection = lockedDirection;
            if (visualRoot != null)
            {
                Vector3 scale = visualRoot.localScale;
                scale.x = lockedScaleX * lockedDirection;
                visualRoot.localScale = scale;
            }
        }
        if (actionAnimator == null) return;
        actionAnimator.SetFloat("MoveSpeed", rb == null ? 0 : Mathf.Abs(rb.linearVelocity.x));
        actionAnimator.SetBool("IsChasing", isChasing);
    }
    private static float DataDuration(BossSkillData skill)
    {
        if (skill == null) return 0;
        float length = Mathf.Max(0, skill.preDelay) + Mathf.Max(0, skill.postDelay);
        if (skill.skillType != BossSkillType.Buff && skill.active != null && skill.active.attackSteps != null)
            foreach (var step in skill.active.attackSteps)
                if (step != null) length += Mathf.Max(0, step.preDelay) + Mathf.Max(0, step.duration) + Mathf.Max(0, step.postDelay);
        return length;
    }
    private void StopHorizontal()
    {
        if (rb != null) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }
    private BossAttackStepData Step(int index)
    {
        if (sourceSkill == null || sourceSkill.active == null || sourceSkill.active.attackSteps == null
            || index >= sourceSkill.active.attackSteps.Count) return null;
        return sourceSkill.active.attackSteps[index];
    }
    private Vector2 RootCenter(BossAttackStepData step)
    {
        return (Vector2)transform.position + new Vector2(lockedDirection * step.hitboxOffset.x, step.hitboxOffset.y);
    }
    private void Damage(BossAttackStepData step, Vector2 center)
    {
        if (step.damage <= 0) return;
        Effects?.PlayStep(step, center, lockedDirection);
        hitTargets.Clear();
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(center, step.hitboxSize, 0, playerLayer))
        {
            if (!BossSkillExecutor.CanDamagePlayer(hit)) continue;
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || !hitTargets.Add(target)) continue;
            bool knockback = step.knockbackForce != Vector2.zero;
            target.TakeDamage(GetSkillDamage(step.damage), transform.position, knockback, false);
            OnSkillAttackHit(step);
            Rigidbody2D body = hit.attachedRigidbody;
            if (body != null && knockback)
            {
                body.linearVelocity = Vector2.zero;
                float direction = step.knockbackAwayFromCenter ? Mathf.Sign(body.position.x - center.x) : lockedDirection;
                if (direction == 0) direction = lockedDirection;
                body.AddForce(new Vector2(direction * step.knockbackForce.x, step.knockbackForce.y), ForceMode2D.Impulse);
            }
        }
    }
    private void Pull(BossAttackStepData step)
    {
        var bodies = new HashSet<Rigidbody2D>();
        foreach (Collider2D hit in Physics2D.OverlapBoxAll(RootCenter(step), step.hitboxSize, 0, playerLayer))
        {
            if (!BossSkillExecutor.CanDamagePlayer(hit)) continue;
            Rigidbody2D body = hit.attachedRigidbody;
            if (body == null || !bodies.Add(body)) continue;
            float targetX = transform.position.x + lockedDirection * step.pullTargetOffset.x;
            float delta = Mathf.MoveTowards(body.position.x, targetX, step.moveSpeed * Time.deltaTime) - body.position.x;
            if (Mathf.Abs(delta) < .001f) continue;
            Vector2 direction = new Vector2(Mathf.Sign(delta), 0);
            float distance = Mathf.Abs(delta);
            var filter = new ContactFilter2D();
            filter.SetLayerMask(wallLayer); filter.useTriggers = false;
            int count = body.Cast(direction, filter, pullHits, distance + .02f);
            for (int i = 0; i < count; i++) distance = Mathf.Min(distance, Mathf.Max(0, pullHits[i].distance - .02f));
            body.MovePosition(body.position + direction * distance);
        }
    }
}
