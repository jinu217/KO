using System.Collections;
using UnityEngine;

// Requires the shared MidBossController skill-animation hooks already used by Stage1/2.
public sealed class Stage3_MidBoss : MidBossController, IBossTimedMovementContext
{
    [SerializeField] private Animator bossAnimator;
    [SerializeField] private Transform visualRoot;
    private float baseScaleX = 1f;
    private string activeState;
    private bool animationReturningToIdle;
    private float targetX, actionDelay = 1f, savedGravity;
    private bool ownsGravity, landed;
    private float ClipTime
    {
        get
        {
            if (bossAnimator == null || !bossAnimator.isActiveAndEnabled || string.IsNullOrEmpty(activeState)) return float.PositiveInfinity;
            var state = bossAnimator.GetCurrentAnimatorStateInfo(0);
            if (!state.IsName(activeState) && !state.IsName("Base Layer." + activeState)) return float.PositiveInfinity;
            foreach (var info in bossAnimator.GetCurrentAnimatorClipInfo(0))
                if (info.weight > .5f) return state.normalizedTime * info.clip.length;
            return float.PositiveInfinity;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        if (visualRoot == null) visualRoot = transform.Find("MidBoss 3");
        if (visualRoot != null)
        {
            baseScaleX = Mathf.Abs(visualRoot.localScale.x);
            if (bossAnimator == null) bossAnimator = visualRoot.GetComponent<Animator>();
        }
        if (bossAnimator == null) bossAnimator = GetComponentInChildren<Animator>(true);
        ApplyFacingDirection();
    }

    protected override void ApplyFacingDirection()
    {
        if (visualRoot == null || visualRoot == transform) return;
        Vector3 scale = visualRoot.localScale;
        scale.x = baseScaleX * (lookDirection < 0 ? -1f : 1f);
        visualRoot.localScale = scale;
    }

    public override void RefreshSkillDirection()
    {
        if (isAttacking) return;
        base.RefreshSkillDirection();
    }

    private bool Has(string name, AnimatorControllerParameterType type)
    {
        if (bossAnimator == null || bossAnimator.runtimeAnimatorController == null) return false;
        foreach (var p in bossAnimator.parameters)
            if (p.name == name && p.type == type) return true;
        return false;
    }

    private void LateUpdate()
    {
        ApplyFacingDirection();
        if (!isDead && !isAttacking && bossAnimator != null && !bossAnimator.IsInTransition(0))
        {
            var state = bossAnimator.GetCurrentAnimatorStateInfo(0);
            if (state.normalizedTime >= 1f && !state.IsName("MidBoss3_Idle") && !state.IsName("MidBoss3_Walk") && !state.IsName("MidBoss3_Run"))
                ReturnToLocomotion();
        }
        if (Has("MoveSpeed", AnimatorControllerParameterType.Float))
            bossAnimator.SetFloat("MoveSpeed", !isAttacking && rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f);
        if (Has("IsChasing", AnimatorControllerParameterType.Bool)) bossAnimator.SetBool("IsChasing", isChasing);
        if (Has("IsAttacking", AnimatorControllerParameterType.Bool))
            bossAnimator.SetBool("IsAttacking", isAttacking && !animationReturningToIdle);
    }

    protected override void OnSkillExecutionStarted(BossSkillData skill, int index)
    {
        base.OnSkillExecutionStarted(skill, index);
        activeState = null;
        targetX = PlayerPosition.x;
        actionDelay = Mathf.Max(.01f, GetSkillDelay(1f));
        landed = false;
        animationReturningToIdle = false;
        string trigger = "Attack";
        if (index >= 0)
        {
            trigger = "Skill1"; // Buff
            if (skill.active != null && skill.active.attackSteps != null)
                foreach (var step in skill.active.attackSteps)
                {
                    if (step.actionType == BossActionType.DashAttack) trigger = "Skill2";
                    if (step.actionType == BossActionType.Jump || step.actionType == BossActionType.Slam) { trigger = "Skill3"; break; }
                }
        }
        string state = "MidBoss3_" + trigger;
        if (!Has(trigger, AnimatorControllerParameterType.Trigger)
            || !bossAnimator.HasState(0, Animator.StringToHash("Base Layer." + state))) return;
        foreach (var p in bossAnimator.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger) bossAnimator.ResetTrigger(p.name);
        if (Has("AttackSpeed", AnimatorControllerParameterType.Float))
            bossAnimator.SetFloat("AttackSpeed", 1f / Mathf.Max(.01f, GetSkillDelay(1f)));
        if (Has("IsAttacking", AnimatorControllerParameterType.Bool)) bossAnimator.SetBool("IsAttacking", true);
        activeState = state;
        // Action triggers only have transitions from locomotion states. A new action
        // requested during Hit or the previous action's exit blend would otherwise
        // leave IsAttacking true on a finished clip while its gameplay steps run.
        bossAnimator.Play(Animator.StringToHash("Base Layer." + state), 0, 0f);
        bossAnimator.Update(0f);
    }

    protected override IEnumerator WaitForSkillAnimation(BossSkillData skill, int index)
    {
        yield return base.WaitForSkillAnimation(skill, index);
        if (bossAnimator == null || string.IsNullOrEmpty(activeState)) yield break;
        int hash = Animator.StringToHash(activeState);
        float missingTime = 0f;
        while (!isDead && bossAnimator != null && bossAnimator.isActiveAndEnabled)
        {
            var state = bossAnimator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash == hash)
            {
                if (state.normalizedTime >= 1f)
                {
                    // Release the Animator condition while keeping the combat AI locked
                    // until the exit blend has actually completed.
                    animationReturningToIdle = true;
                    ReturnToLocomotion();
                    if (Has("IsAttacking", AnimatorControllerParameterType.Bool))
                        bossAnimator.SetBool("IsAttacking", false);
                    float exitWait = 0f;
                    while (!isDead && bossAnimator != null && bossAnimator.isActiveAndEnabled
                        && bossAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash == hash
                        && exitWait < .5f)
                    {
                        StopMove();
                        exitWait += Time.deltaTime;
                        yield return null;
                    }
                    yield break;
                }
                missingTime = 0f;
            }
            else
            {
                missingTime += Time.unscaledDeltaTime;
                if (missingTime > .5f) break;
            }
            StopMove();
            yield return null;
        }
    }


    public bool UsesTimedMovement(BossAttackStepData step)
        => step != null && (step.actionType == BossActionType.DashAttack || step.actionType == BossActionType.Jump || step.actionType == BossActionType.Slam);

    public IEnumerator ExecuteTimedMovement(BossAttackStepData step)
    {
        if (rb == null) yield break;
        if (step.actionType == BossActionType.Slam)
        {
            if (landed && !isDead) yield return BossSkillExecutor.ExecuteRepeatedAttacks(step, this);
            yield break;
        }
        if (step.actionType == BossActionType.DashAttack)
        {
            while (!isDead && ClipTime < 1.08f) { StopMove(); yield return null; }
            float startX = rb.position.x;
            int remaining = step.attackCount;
            bool stopped = false;
            float nextHit = 0;
            // Preserve configured total travel, distribute it over the authored 1s rush.
            float speed = step.moveSpeed * step.duration / actionDelay;
            var single = JsonUtility.FromJson<BossAttackStepData>(JsonUtility.ToJson(step)); single.attackCount = 1;
            try
            {
                while (!isDead && ClipTime < 2.08f)
                {
                    if ((step.stopAtWall && IsWallAhead()) || (step.maxMoveDistance > 0 && Mathf.Abs(rb.position.x - startX) >= step.maxMoveDistance)) stopped = true;
                    rb.linearVelocity = new Vector2(stopped ? 0 : lookDirection * speed, rb.linearVelocity.y);
                    if (!stopped && remaining > 0 && Time.time >= nextHit)
                    {
                        Vector2 center = (Vector2)transform.position + new Vector2(lookDirection * step.hitboxOffset.x, step.hitboxOffset.y);
                        var hit = Physics2D.OverlapBox(center, step.hitboxSize, 0, playerLayer);
                        if (BossSkillExecutor.CanDamagePlayer(hit))
                        {
                            yield return BossSkillExecutor.ExecuteRepeatedAttacks(single, this);
                            remaining--; nextHit = Time.time + GetSkillDelay(step.interval);
                            if (step.stopOnPlayerHit) stopped = true;
                        }
                    }
                    yield return new WaitForFixedUpdate();
                }
            }
            finally { StopMove(); }
            yield break;
        }
        while (!isDead && ClipTime < .85f) { StopMove(); yield return null; }
        if (isDead || float.IsInfinity(ClipTime)) yield break;
        Vector2 origin = rb.position;
        float destination = origin.x + Mathf.Max(0, (targetX - origin.x) * lookDirection) * lookDirection;
        savedGravity = rb.gravityScale; ownsGravity = true;
        float gravity = Mathf.Max(.01f, Mathf.Abs(Physics2D.gravity.y * savedGravity));
        float height = step.jumpForce.y * step.jumpForce.y / (2f * gravity);
        var filter = new ContactFilter2D(); filter.SetLayerMask(wallLayer); filter.useTriggers = false;
        var hits = new RaycastHit2D[32]; bool blockedOnFloor = false;
        rb.gravityScale = 0; rb.linearVelocity = Vector2.zero;
        try
        {
            while (!isDead && !float.IsInfinity(ClipTime))
            {
                float t = ClipTime;
                float u = Mathf.Clamp01((t - .85f) / .35f), v = Mathf.Clamp01((t - 1.20f) / .40f);
                float y = t <= 1.20f ? height * (2 * u - u * u) : height * (1 - v * v);
                float ratio = t <= 1.20f ? step.targetDistanceRatio * u : Mathf.Lerp(step.targetDistanceRatio, 1f, v);
                Vector2 target = new Vector2(Mathf.Lerp(origin.x, destination, ratio), origin.y + y);
                if (!blockedOnFloor)
                {
                    // A side wall only blocks horizontal travel. Clamping the entire
                    // diagonal also stopped descent, causing the landing hit to vanish.
                    MoveJumpAxis(new Vector2(target.x - rb.position.x, 0), filter, hits);
                    blockedOnFloor = MoveJumpAxis(new Vector2(0, target.y - rb.position.y), filter, hits) && t > 1.20f;
                }
                rb.linearVelocity = Vector2.zero;
                if (t >= 1.60f) { landed = blockedOnFloor || IsGrounded(); break; }
                yield return new WaitForFixedUpdate();
            }
        }
        finally { RestoreGravity(); }
    }
    bool MoveJumpAxis(Vector2 travel, ContactFilter2D filter, RaycastHit2D[] hits)
    {
        float distance = travel.magnitude;
        if (distance <= .00001f) return false;
        int count = rb.Cast(travel / distance, filter, hits, distance + .01f);
        float allowed = distance; bool floor = false;
        for (int i = 0; i < count; i++)
        {
            if (Vector2.Dot(travel, hits[i].normal) >= -.00001f) continue;
            float safe = Mathf.Max(0, hits[i].distance - .01f);
            if (safe > allowed) continue;
            allowed = safe; floor = travel.y < 0 && hits[i].normal.y >= .35f;
        }
        rb.position += travel.normalized * allowed;
        Physics2D.SyncTransforms();
        return floor;
    }
    void RestoreGravity()
    {
        if (!ownsGravity) return;
        if (rb != null) { rb.gravityScale = savedGravity; rb.linearVelocity = Vector2.zero; }
        ownsGravity = false;
    }
    void OnDisable() { RestoreGravity(); }

    protected override void OnSkillExecutionFinished(int index)
    {
        RestoreGravity();
        if (!isDead) ReturnToLocomotion();
        activeState = null;
        animationReturningToIdle = false;
        if (Has("IsAttacking", AnimatorControllerParameterType.Bool)) bossAnimator.SetBool("IsAttacking", false);
        base.OnSkillExecutionFinished(index);
    }
    void ReturnToLocomotion()
    {
        if (bossAnimator == null || !bossAnimator.isActiveAndEnabled) return;
        if (Has("IsAttacking", AnimatorControllerParameterType.Bool)) bossAnimator.SetBool("IsAttacking", false);
        if (Has("AttackSpeed", AnimatorControllerParameterType.Float)) bossAnimator.SetFloat("AttackSpeed", 1);
        string state = isChasing ? "MidBoss3_Run" : rb != null && Mathf.Abs(rb.linearVelocity.x) > .01f ? "MidBoss3_Walk" : "MidBoss3_Idle";
        int hash = Animator.StringToHash("Base Layer." + state);
        if (bossAnimator.HasState(0, hash) && !bossAnimator.GetCurrentAnimatorStateInfo(0).IsName(state))
            bossAnimator.CrossFadeInFixedTime(hash, .06f, 0);
    }
}
