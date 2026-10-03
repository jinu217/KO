using System.Collections;
using UnityEngine;

public sealed class Stage4_MainBoss : MainBossController, IBossConditionalStepContext, IBossTimedMovementContext, IBossHitValidationContext
{
    [SerializeField, Tooltip("생존 본능의 강화, 회복 및 종료 판정을 담당하는 컴포넌트입니다.")]
    private Stage4_MainBossDesireSkill desireSkill;
    private BossSkillData executing;
    private bool bodyHit;
    private bool patternHealed, uppercutHealed;
    private float nextAimTime;
    [SerializeField, Min(0), Tooltip("러시 연격 중 방향을 다시 확인하는 최소 간격입니다.")]
    private float comboAimInterval = .35f;
    protected override string AnimationPrefix => "MainBoss4_";
    protected override string VisualRootName => "MainBoss 4";
    protected override bool HasPhaseEntryAnimations => false;
    protected override bool UsesPhase3ActionStates => false;

    protected override void Awake()
    {
        base.Awake();
        desireSkill = GetComponent<Stage4_MainBossDesireSkill>();
        if (desireSkill == null) desireSkill = gameObject.AddComponent<Stage4_MainBossDesireSkill>();
        desireSkill.Initialize(this);
    }
    protected override void ApplyFacingDirection()
    {
        base.ApplyFacingDirection();
        if (spriteRenderer != null) spriteRenderer.flipX = lookDirection < 0;
    }
    public bool CanHitPlayer(Collider2D player)
    {
        var controller = player.GetComponentInParent<PlayerController>();
        return controller == null || !controller.IsDodging;
    }
    protected override bool UpdatePriorityBehavior() => desireSkill.Tick(currentPhase);
    protected override bool CanRunCombatAI() => !desireSkill.BlocksCombat;
    protected override bool IsExternalBuffBlocking() => desireSkill.Active || desireSkill.BlocksCombat;
    protected override BossSkillData GetBasicAttackData() => desireSkill.Active ? desireSkill.Basic : base.GetBasicAttackData();
    protected override BossSkillData GetSkillData(int index)
        => desireSkill != null && desireSkill.Active ? desireSkill.Skill(index) : base.GetSkillData(index);
    protected override float GetExternalDamageMultiplier() => desireSkill.Active ? desireSkill.DamageMultiplier : 1f;
    protected override float GetExternalAttackSpeedMultiplier() => desireSkill.Active ? desireSkill.AttackSpeedMultiplier : 1f;
    protected override float GetExternalMoveMultiplier() => desireSkill.Active ? desireSkill.MoveMultiplier : 1f;
    protected override void OnSkillExecutionStarted(BossSkillData skill, int index)
    {
        executing = skill;
        nextAimTime = Time.time + comboAimInterval;
        bodyHit = patternHealed = uppercutHealed = false;
        base.OnSkillExecutionStarted(skill, index);
    }
    public override void RefreshSkillDirection()
    {
        if (!isAttacking) { base.RefreshSkillDirection(); return; }
        if (isDead || playerTransform == null || currentSkillIndex != 0 || Time.time < nextAimTime) return;
        nextAimTime = Time.time + comboAimInterval * (desireSkill.Active ? .6f : 1f);
        float delta = PlayerPosition.x - transform.position.x;
        if (Mathf.Abs(delta) < .4f) return;
        lookDirection = delta > 0 ? 1 : -1;
        ApplyFacingDirection();
    }
    public bool ShouldExecuteStep(BossAttackStepData step)
    {
        if (desireSkill.BlocksCombat) return false;
        // 2스킬의 첫 이동/보디 블로 이후 단계는 보디 블로 적중이 필요하다.
        return currentSkillIndex != 1 || executing.active.attackSteps.IndexOf(step) < 2 || bodyHit;
    }
    public override void OnSkillAttackHit(BossAttackStepData step)
    {
        int position = executing?.active.attackSteps.IndexOf(step) ?? -1;
        if (currentSkillIndex == 1 && position == 1) bodyHit = true;
        if ((currentSkillIndex == 1 && position == 1)
            || (desireSkill.Active && currentSkillIndex == 0 && position == 4))
            playerTransform?.GetComponent<PlayerController>()?.ApplyMoveDelay(.2f);
        if (!desireSkill.Active) return;
        if (currentSkillIndex == 1 && position == 2 && !uppercutHealed)
        {
            uppercutHealed = true;
            Heal(desireSkill.UppercutHeal);
        }
        else if (!patternHealed && (currentSkillIndex != 1 || position == 1))
        {
            patternHealed = true;
            Heal(desireSkill.HealRatio(currentSkillIndex));
        }
    }
    public bool UsesTimedMovement(BossAttackStepData step)
        => currentSkillIndex == 3 && executing?.active.attackSteps.IndexOf(step) == 0;
    public IEnumerator ExecuteTimedMovement(BossAttackStepData step)
    {
        if (step.preDelay > 0) yield return new WaitForSeconds(GetSkillDelay(step.preDelay));
        float start = transform.position.x;
        float until = Time.time + step.duration;
        RefreshSkillDirection();
        try
        {
            while (!isDead && !desireSkill.BlocksCombat && Time.time < until
                && Mathf.Abs(transform.position.x - start) < step.maxMoveDistance
                && Mathf.Abs(PlayerPosition.x - transform.position.x) > 1.4f && !IsWallAhead())
            {
                rb.linearVelocity = new Vector2(LookDirection * step.moveSpeed, rb.linearVelocity.y);
                yield return new WaitForFixedUpdate();
            }
        }
        finally { StopMove(); }
    }
    public float Hp => currentHp;
    public int Phase => currentPhase;
    public bool InCombat => isChasing;
    public void InterruptAttack() => InterruptCurrentSkill();
    public void DesireLog(string message) => LogSkillDebug(message, new BossSkillData { skillName = "생존 본능" });
    public bool Busy { get => isAttacking; set => isAttacking = value; }
    public void Halt() => StopMove();
    public void Heal(float ratio)
    {
        if (isDead || ratio <= 0f) return;
        currentHp = Mathf.Min(data.maxHp, currentHp + data.maxHp * ratio);
        UpdateHealthUI();
    }
    protected override void Die() { desireSkill.Cancel(); base.Die(); }
}
