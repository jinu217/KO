using System.Collections;
using UnityEngine;

// 일반 패턴의 특수 이동과 전용 욕망 컴포넌트의 공통 보스 연결을 담당한다.
public sealed class Stage5_MainBoss : MainBossController, IBossTimedMovementContext,
    IBossConditionalStepContext, IBossHitValidationContext
{
    [Header("욕망 스킬")]
    [SerializeField, Tooltip("3페이즈 욕망 스킬의 시간·거리 스택·체력 되감기와 그로기를 담당합니다.")]
    private Stage5_MainBossDesireSkill desireSkill;
    [Header("3스킬 후속 이동")]
    [SerializeField, Min(0f), Tooltip("돌진 적중 후 플레이어 뒤쪽에 도달할 X축 거리입니다.")]
    private float vaultBehindDistance = 1.8f;
    [SerializeField, Min(0f), Tooltip("플레이어 위로 넘어갈 때 추가 상승 높이입니다.")]
    private float vaultArcHeight = 2.1f;

    [Header("5스킬 교차 이동")]
    [SerializeField, Tooltip("각 베기를 시작할 플레이어 기준 위치입니다. 순서대로 네 방향에서 교차합니다.")]
    private Vector2[] sweepStarts =
    {
        new Vector2(-2.8f, 0.2f), new Vector2(2.8f, 1.8f),
        new Vector2(2.8f, 0.2f), new Vector2(-2.8f, 1.8f)
    };
    [SerializeField, Min(0f), Tooltip("다음 베기 시작 위치까지 공격 없이 빠르게 이동하는 시간입니다.")]
    private float sweepRepositionDuration = 0.16f;
    [SerializeField, Tooltip("교차 베기가 통과할 상대 위치의 Y축 높이입니다.")]
    private float sweepCrossHeight = 1f;

    private BossSkillData executingSkill;
    private bool dashHitPlayer;
    private int lockedDashDirection;

    protected override string AnimationPrefix => "MainBoss5_";
    protected override string VisualRootName => "MainBoss 5";
    protected override bool HasPhaseEntryAnimations => false;
    protected override bool UsesPhase3ActionStates => false;

    protected override void Awake()
    {
        base.Awake();
        if (desireSkill == null) desireSkill = GetComponent<Stage5_MainBossDesireSkill>();
        if (desireSkill == null) desireSkill = gameObject.AddComponent<Stage5_MainBossDesireSkill>();
        desireSkill.Initialize(this);
    }

    protected override bool UpdatePriorityBehavior()
        => desireSkill != null && desireSkill.Tick(currentPhase);

    protected override bool CanRunCombatAI()
        => desireSkill == null || !desireSkill.Groggy;

    protected override float GetExternalDamageMultiplier()
        => desireSkill != null ? desireSkill.DamageMultiplier : 1f;

    protected override float GetExternalAttackSpeedMultiplier()
        => desireSkill != null ? desireSkill.AttackSpeedMultiplier : 1f;

    protected override float GetExternalMoveMultiplier()
        => desireSkill != null ? desireSkill.MoveSpeedMultiplier : 1f;

    public override void TakeDamage(float damage, Vector2 sourcePos, bool applyKnockback = false, bool applyIFrame = false)
    {
        float incoming = desireSkill != null ? desireSkill.IncomingDamageMultiplier : 1f;
        base.TakeDamage(damage * incoming, sourcePos, applyKnockback, applyIFrame);
    }

    public float DesireHp => currentHp;
    public bool DesireBusy => isAttacking;
    public bool DesireInCombat => isChasing;
    public Transform DesirePlayer => playerTransform;
    public void DesireStop() => StopMove();
    public void DesireInterrupt() => InterruptCurrentSkill();
    public void DesireLog(string message)
        => LogSkillDebug(message, new BossSkillData { skillName = "시간을 되돌리는 애정" });
    public void DesireRestoreHp(float value)
    {
        if (isDead) return;
        currentHp = Mathf.Clamp(value, 0f, data.maxHp);
        UpdateHealthUI();
    }

    protected override void Die()
    {
        desireSkill?.Cancel();
        base.Die();
    }

    protected override void ApplyFacingDirection()
    {
        base.ApplyFacingDirection();
        if (spriteRenderer != null) spriteRenderer.flipX = lookDirection < 0;
    }

    protected override void OnSkillExecutionStarted(BossSkillData skill, int index)
    {
        executingSkill = skill;
        dashHitPlayer = false;
        lockedDashDirection = lookDirection < 0 ? -1 : 1;
        base.OnSkillExecutionStarted(skill, index);
    }

    protected override void OnSkillExecutionFinished(int index)
    {
        executingSkill = null;
        dashHitPlayer = false;
        base.OnSkillExecutionFinished(index);
    }

    public bool CanHitPlayer(Collider2D hit)
    {
        PlayerController player = hit.GetComponentInParent<PlayerController>();
        return player == null || !player.IsDodging;
    }

    public override void OnSkillAttackHit(BossAttackStepData step)
    {
        if (currentSkillIndex == 2 && StepIndex(step) == 1)
            dashHitPlayer = true;
    }

    public bool ShouldExecuteStep(BossAttackStepData step)
        => currentSkillIndex != 2 || StepIndex(step) != 2 || dashHitPlayer;

    public bool UsesTimedMovement(BossAttackStepData step)
    {
        int index = StepIndex(step);
        return (currentSkillIndex == 0 && index >= 0 && index % 2 == 0)
            || (currentSkillIndex == 2 && index == 2)
            || (currentSkillIndex == 4 && index >= 0 && index < sweepStarts.Length);
    }

    public IEnumerator ExecuteTimedMovement(BossAttackStepData step)
    {
        if (currentSkillIndex == 0)
            yield return WalkOneStep(step);
        else if (currentSkillIndex == 2)
            yield return VaultBehindPlayer(step);
        else if (currentSkillIndex == 4)
            yield return CrossSlash(step, StepIndex(step));
    }

    public override void RefreshSkillDirection()
    {
        if (isDead || !isAttacking || playerTransform == null || currentSkillIndex != 0)
        {
            base.RefreshSkillDirection();
            return;
        }
        FaceTowards(playerTransform.position.x);
    }

    private int StepIndex(BossAttackStepData step)
        => executingSkill?.active?.attackSteps?.IndexOf(step) ?? -1;

    private void FaceTowards(float x)
    {
        float delta = x - transform.position.x;
        if (Mathf.Abs(delta) <= .15f) return;
        lookDirection = delta > 0f ? 1 : -1;
        ApplyFacingDirection();
    }

    private IEnumerator WalkOneStep(BossAttackStepData step)
    {
        if (step.preDelay > 0f) yield return new WaitForSeconds(GetSkillDelay(step.preDelay));
        float elapsed = 0f;
        float duration = GetSkillDelay(step.duration);
        try
        {
            while (!isDead && rb != null && elapsed < duration)
            {
                if (playerTransform != null) FaceTowards(playerTransform.position.x);
                bool walk = playerTransform != null
                    && Mathf.Abs(playerTransform.position.x - transform.position.x) > .7f
                    && !IsWallAhead();
                rb.linearVelocity = new Vector2(walk ? lookDirection * step.moveSpeed : 0f, rb.linearVelocity.y);
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }
        finally { StopMove(); }
    }

    private IEnumerator VaultBehindPlayer(BossAttackStepData step)
    {
        if (rb == null || playerTransform == null) yield break;
        if (step.preDelay > 0f) yield return new WaitForSeconds(GetSkillDelay(step.preDelay));
        Vector2 start = rb.position;
        float forward = lockedDashDirection * (playerTransform.position.x - start.x);
        float travel = Mathf.Clamp(forward + vaultBehindDistance, .8f, Mathf.Max(.8f, step.maxMoveDistance));
        float targetX = start.x + lockedDashDirection * travel;
        float groundY = start.y;
        float feetOffset = bossCollider != null ? start.y - bossCollider.bounds.min.y : 0.5f;
        Vector2 rayOrigin = new Vector2(targetX, playerTransform.position.y + 1f);
        RaycastHit2D floor = Physics2D.Raycast(rayOrigin, Vector2.down, 30f, wallLayer);
        if (floor.collider != null) groundY = floor.point.y + feetOffset;

        Vector2 target = new Vector2(targetX, groundY);
        float duration = Mathf.Max(.01f, GetSkillDelay(step.duration));
        float gravity = rb.gravityScale;
        CollisionDetectionMode2D collisionMode = rb.collisionDetectionMode;
        bool dealtDamage = false;
        try
        {
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearVelocity = Vector2.zero;
            float elapsed = 0f;
            while (!isDead && elapsed < duration)
            {
                float progress = Mathf.Clamp01(elapsed / duration);
                Vector2 arc = Vector2.Lerp(start, target, progress)
                    + Vector2.up * (Mathf.Sin(progress * Mathf.PI) * vaultArcHeight);
                rb.MovePosition(arc);
                if (!dealtDamage && PlayerInHitbox(step))
                {
                    yield return BossSkillExecutor.ExecuteRepeatedAttacks(step, this);
                    dealtDamage = true;
                }
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            rb.MovePosition(target);
        }
        finally
        {
            if (rb != null)
            {
                rb.gravityScale = gravity;
                rb.collisionDetectionMode = collisionMode;
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    private IEnumerator CrossSlash(BossAttackStepData step, int index)
    {
        if (rb == null || playerTransform == null || index < 0 || index >= sweepStarts.Length) yield break;
        if (step.preDelay > 0f) yield return new WaitForSeconds(GetSkillDelay(step.preDelay));
        Vector2 center = playerTransform.position;
        Vector2 start = center + sweepStarts[index];
        Vector2 end = center + new Vector2(-sweepStarts[index].x, 2f * sweepCrossHeight - sweepStarts[index].y);
        float gravity = rb.gravityScale;
        CollisionDetectionMode2D collisionMode = rb.collisionDetectionMode;
        try
        {
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearVelocity = Vector2.zero;

            Vector2 from = rb.position;
            float travelTime = Mathf.Max(.01f, GetSkillDelay(sweepRepositionDuration));
            float elapsed = 0f;
            while (!isDead && elapsed < travelTime)
            {
                rb.MovePosition(Vector2.Lerp(from, start, Mathf.Clamp01(elapsed / travelTime)));
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            rb.MovePosition(start);
            lookDirection = end.x >= start.x ? 1 : -1;
            ApplyFacingDirection();

            float slashTime = Mathf.Max(.01f, GetSkillDelay(step.duration));
            elapsed = 0f;
            bool dealtDamage = false;
            while (!isDead && elapsed < slashTime)
            {
                float progress = Mathf.Clamp01(elapsed / slashTime);
                rb.MovePosition(Vector2.Lerp(start, end, progress));
                if (!dealtDamage && PlayerInHitbox(step))
                {
                    yield return BossSkillExecutor.ExecuteRepeatedAttacks(step, this);
                    dealtDamage = true;
                }
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            rb.MovePosition(end);
        }
        finally
        {
            if (rb != null)
            {
                rb.gravityScale = gravity;
                rb.collisionDetectionMode = collisionMode;
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    private bool PlayerInHitbox(BossAttackStepData step)
    {
        if (step.hitboxSize.x <= 0f || step.hitboxSize.y <= 0f) return false;
        Vector2 center = (Vector2)transform.position + new Vector2(
            lookDirection * step.hitboxOffset.x, step.hitboxOffset.y);
        Collider2D hit = Physics2D.OverlapBox(center, step.hitboxSize, 0f, playerLayer);
        return hit != null && CanHitPlayer(hit);
    }
}
