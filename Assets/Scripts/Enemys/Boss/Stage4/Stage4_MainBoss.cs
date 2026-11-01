using System.Collections;
using UnityEngine;

public sealed class Stage4_MainBoss : MainBossController, IBossConditionalStepContext, IBossTimedMovementContext, IBossHitValidationContext, IBossTimedAttackContext
{
    [SerializeField, Tooltip("생존 본능의 강화, 회복 및 종료 판정을 담당하는 컴포넌트입니다.")]
    private Stage4_MainBossDesireSkill desireSkill;
    private BossSkillData executing;
    private bool enhancedAnimation;
    private string patternAction;
    private float patternStepStart, patternLength;
    private bool bodyHit;
    private bool patternHealed, uppercutHealed;
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
    // MainBossController flips the whole MainBoss 4 visual root.
    // Do not additionally flip one SpriteRenderer.
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
        enhancedAnimation = desireSkill.Active;
        bodyHit = patternHealed = uppercutHealed = false;
        base.OnSkillExecutionStarted(skill, index);
        patternAction = index < 0 ? "Attack" : "skill" + (index + 1);
        if (enhancedAnimation && (index < 0 || index == 3)) patternAction += "_Desire";
        PlayAction(patternAction);
    }
    public override void RefreshSkillDirection()
    {
        if (isAttacking || isDead) return;
        base.RefreshSkillDirection();
    }
    public bool ShouldExecuteStep(BossAttackStepData step)
    {
        if (desireSkill.BlocksCombat) return false;
        int stepIndex = executing?.active?.attackSteps.IndexOf(step) ?? -1;
        if (stepIndex < 0) return false;
        if (currentSkillIndex == 1 && stepIndex >= 2 && !bodyHit) return false;
        string action = patternAction;
        int start = 0, end = executing.active.attackSteps.Count;
        float initial = executing.preDelay;
        if (currentSkillIndex == 1)
        {
            if (stepIndex >= 2) { action = "skill2_Uppercut" + (enhancedAnimation ? "_Desire" : ""); start = 2; initial = 0; }
            else end = Mathf.Min(2, end);
        }
        float offset = initial, length = initial + executing.postDelay + .32f;
        for (int i = start; i < end; i++)
        {
            float time = AnimationStepLength(executing.active.attackSteps[i]);
            length += time; if (i < stepIndex) offset += time;
        }
        // Same integrated state, no generated per-hit clips. Seek only at executor boundaries.
        Animator patternAnimator = GetDeathAnimator();
        patternStepStart = offset; patternLength = length;
        if (patternAnimator != null && PlayAction(action))
        {
            patternAnimator.ResetTrigger(action);
            patternAnimator.Play(Animator.StringToHash("Base Layer.MainBoss4_" + action), 0, offset / length);
        }
        patternAction = action;
        return true;
    }
    private static float AnimationStepLength(BossAttackStepData step)
        => Mathf.Max(.08f, step.preDelay + (step.actionType == BossActionType.MeleeAttack ? 0 : step.duration) + step.postDelay);
    protected override IEnumerator WaitForSkillAnimation(BossSkillData skill, int index)
    {
        if (skill.skillType == BossSkillType.Buff) { yield return base.WaitForSkillAnimation(skill, index); yield break; }
        yield return WaitForAction("MainBoss4_" + patternAction);
    }
    public void BeginDesirePose() { PlayDesireAnimation(); }
    public IEnumerator FinishDesirePose() { yield return WaitForDesireAnimation(); }
    public void BeginGroggyPose() { isAttacking = true; PlayAction("Groggy", false); }
    public void EndGroggyPose() { isAttacking = false; }
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
    public bool UsesTimedAttack(BossAttackStepData step)
        => step.damage > 0 && (step.actionType == BossActionType.MeleeAttack || step.actionType == BossActionType.DashAttack);
    // Matches the launch portion of MainBoss4_AnimGenerators.StepPose.
    public static float PunchLaunchTime(int skill, int n, BossAttackStepData step)
    {
        float impact = Mathf.Max(.04f, step.preDelay);
        if (skill == 1 && n >= 4) return impact * .20f;
        bool hook = (skill == 0 && n == 2) || (skill == 3 && n >= 1 && n <= 4);
        bool body = (skill == 0 && n == 3) || (skill == 1 && n == 1);
        return (hook || body || (skill == 3 && n == 5)) ? Mathf.Max(0, impact - .055f) : impact * .65f;
    }
    public IEnumerator ExecuteTimedAttack(BossAttackStepData step)
    {
        int n = executing.active.attackSteps.IndexOf(step);
        float launch = PunchLaunchTime(currentSkillIndex, n, step);
        float impact = Mathf.Max(.04f, step.preDelay);
        if (launch > 0) yield return new WaitForSeconds(GetSkillDelay(launch));
        var fx = GetComponent<BossEffects>();
        Bounds previous = BossSkillExecutor.AttackBounds(step, this);
        fx?.PlayStep(step, previous.center, LookDirection);
        bool hit = false;
        float start = Time.time, startX = transform.position.x;
        float extension = GetSkillDelay(impact - launch);
        float duration = step.actionType == BossActionType.DashAttack ? step.duration : 0;
        try
        {
            do
            {
                yield return null;
                float elapsed = Time.time - start;
                if (step.actionType == BossActionType.DashAttack && elapsed >= extension)
                {
                    bool stopped = (step.stopAtWall && IsWallAhead())
                        || (step.maxMoveDistance > 0 && Mathf.Abs(transform.position.x - startX) >= step.maxMoveDistance)
                        || (hit && step.stopOnPlayerHit);
                    if (stopped) StopMove();
                    else rb.linearVelocity = new Vector2(LookDirection * step.moveSpeed, rb.linearVelocity.y);
                }
                Bounds current = BossSkillExecutor.AttackBounds(step, this);
                if (!hit)
                {
                    // Subdivide the actual fist travel so fast hooks/uppercuts cannot skip a player.
                    float spacing = Mathf.Max(.02f, Mathf.Min(current.size.x, current.size.y) * .4f);
                    int samples = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(previous.center, current.center) / spacing), 1, 32);
                    for (int i = 0; i <= samples && !hit; i++)
                    {
                        float t = (float)i / samples;
                        var contact = new Bounds(Vector3.Lerp(previous.center, current.center, t), Vector3.Lerp(previous.size, current.size, t));
                        hit = BossSkillExecutor.HitPlayerAt(step, this, contact);
                    }
                }
                previous = current;
            } while (!isDead && PunchWindowOpen(impact, duration, start, extension));
        }
        finally { if (step.actionType == BossActionType.DashAttack) StopMove(); }
    }
    private bool PunchWindowOpen(float impact, float duration, float started, float extension)
    {
        // Coroutines resume before Animator sampling. Keep the contact window open
        // until the visible impact pose has actually been sampled (also at low FPS).
        var a = GetDeathAnimator();
        if (a != null && a.isActiveAndEnabled && patternLength > 0
            && Time.time - started < extension + duration + .25f)
        {
            var state = a.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash == Animator.StringToHash("MainBoss4_" + patternAction))
                return state.normalizedTime * patternLength < patternStepStart + impact + duration;
        }
        return Time.time - started < extension + duration;
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
