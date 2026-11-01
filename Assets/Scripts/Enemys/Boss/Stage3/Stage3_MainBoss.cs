using System.Collections;
using UnityEngine;

// 일반 전투와 자아실현 욕망 스킬 사이의 연결을 담당한다.
public sealed class Stage3_MainBoss : MainBossController, IBossTimedMovementContext
{
    protected override string AnimationPrefix => "MainBoss3_";
    protected override string VisualRootName => "MainBoss 3";
    protected override bool HasPhaseEntryAnimations => false;
    protected override bool UsesPhase3ActionStates => false;
    protected override string DesireAnimationAction => "Buff";
    protected override string SkillAnimationAction(int index)
    {
        switch (index)
        {
            case -1: return "Attack";
            case 0: return "Skill1";
            case 1: return "Skill2";
            case 2:
            case 4: return "Buff";
            case 3: return "Skill3";
            default: return "Skill" + (index + 1);
        }
    }

    private float motionStarted, motionSecondsPerClipSecond = 1f;
    private int motionDirection;
    private BossAttackStepData timedDash, timedBackstep, backstepDamage;
    private float savedGravity;
    private bool ownsGravity;

    protected override void OnSkillExecutionStarted(BossSkillData skill, int index)
    {
        base.OnSkillExecutionStarted(skill, index);
        motionStarted = Time.time;
        motionSecondsPerClipSecond = Mathf.Max(.01f, GetSkillDelay(1f));
        motionDirection = LookDirection < 0 ? -1 : 1;
        timedDash = timedBackstep = backstepDamage = null;
        var steps = skill?.active?.attackSteps;
        if (steps == null) return;
        if (index == 1 && steps.Count >= 5) timedDash = steps[4];
        if (index == 3 && steps.Count >= 3)
        {
            timedDash = steps[0]; timedBackstep = steps[1]; backstepDamage = steps[2];
        }
    }

    public bool UsesTimedMovement(BossAttackStepData step)
        => step != null && (ReferenceEquals(step, timedDash)
            || ReferenceEquals(step, timedBackstep) || ReferenceEquals(step, backstepDamage));

    private float MotionTime => (Time.time - motionStarted) / motionSecondsPerClipSecond;

    public IEnumerator ExecuteTimedMovement(BossAttackStepData step)
    {
        // The damage step runs during the backstep, not after it.
        if (ReferenceEquals(step, backstepDamage)) yield break;
        bool retreat = ReferenceEquals(step, timedBackstep);
        float begin = retreat ? 1.12f : currentSkillIndex == 1 ? 1.38f : .42f;
        float end = retreat ? 1.78f : currentSkillIndex == 1 ? 1.88f : .92f;
        while (!isDead && MotionTime < begin) { StopMove(); yield return null; }
        if (isDead || rb == null) yield break;
        float startX = rb.position.x;
        float duration = (end - begin) * motionSecondsPerClipSecond;
        BossAttackStepData damage = retreat ? backstepDamage : step;
        BossAttackStepData singleHit = damage == null ? null
            : JsonUtility.FromJson<BossAttackStepData>(JsonUtility.ToJson(damage));
        if (singleHit != null) singleHit.attackCount = 1;
        int hits = 0;
        float nextHit = begin;
        bool stopped = false;
        if (retreat)
        {
            savedGravity = rb.gravityScale; ownsGravity = true; rb.gravityScale = 0f;
        }
        try
        {
            while (!isDead && rb != null && MotionTime < end)
            {
                float now = MotionTime;
                if (!retreat && ((step.stopAtWall && IsWallAhead())
                    || (step.maxMoveDistance > 0f && Mathf.Abs(rb.position.x - startX) >= step.maxMoveDistance))) stopped = true;
                float vx = retreat ? step.jumpForce.x : step.moveSpeed;
                vx = stopped ? 0f : motionDirection * vx / motionSecondsPerClipSecond;
                float vy = rb.linearVelocity.y;
                if (retreat)
                {
                    // Short controlled hop, returning to the departure height.
                    float u = Mathf.Clamp01((now - begin) / (end - begin));
                    float height = Mathf.Max(.08f, step.jumpForce.y * (end - begin) * .25f);
                    vy = 4f * height * (1f - 2f * u) / duration;
                }
                rb.linearVelocity = new Vector2(vx, vy);
                if (!stopped && singleHit != null && hits < damage.attackCount && now >= nextHit)
                {
                    Bounds contact = BossSkillExecutor.AttackBounds(damage, this);
                    Collider2D hit = Physics2D.OverlapBox(contact.center, contact.size, 0f, PlayerLayer);
                    if (BossSkillExecutor.CanDamagePlayer(hit))
                    {
                        yield return BossSkillExecutor.ExecuteRepeatedAttacks(singleHit, this);
                        hits++;
                        nextHit = now + Mathf.Max(.01f, damage.interval);
                        if (!retreat && step.stopOnPlayerHit) stopped = true;
                    }
                }
                yield return new WaitForFixedUpdate();
            }
        }
        finally
        {
            RestoreMotionGravity();
            if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        }
        // Early collision must not advance the next animation window.
        while (!isDead && MotionTime < end) yield return null;
    }

    private void RestoreMotionGravity()
    {
        if (!ownsGravity) return;
        if (rb != null) rb.gravityScale = savedGravity;
        ownsGravity = false;
    }

    private void OnDisable() { RestoreMotionGravity(); }

    public override void RefreshSkillDirection()
    {
        if (isAttacking) return;
        base.RefreshSkillDirection();
    }

    [Header("욕망 스킬")]
    [SerializeField, InspectorName("욕망 스킬 컴포넌트"), Tooltip("3스테이지 메인보스의 성취와 자아의 완성을 관리하는 전용 컴포넌트입니다.")]
    private Stage3_MainBossDesireSkill desireSkill;

    protected override void Awake()
    {
        base.Awake();
        if (desireSkill == null) desireSkill = GetComponent<Stage3_MainBossDesireSkill>();
        if (desireSkill == null) desireSkill = gameObject.AddComponent<Stage3_MainBossDesireSkill>();
        desireSkill.Initialize(this, data);
    }

    protected override bool UpdatePriorityBehavior()
        => desireSkill != null && desireSkill.Tick(currentPhase);

    protected override BossSkillData GetBasicAttackData()
        => desireSkill != null && desireSkill.HasFirstAchievement
            ? desireSkill.EnhancedBasicAttack
            : base.GetBasicAttackData();

    protected override BossSkillData GetSkillData(int index)
    {
        if (index == 3 && desireSkill != null && desireSkill.HasSecondAchievement)
            return desireSkill.EnhancedSkill4;
        return base.GetSkillData(index);
    }

    public override void OnSkillAttackHit(BossAttackStepData step)
    {
        desireSkill?.OnAttackHit(currentSkillIndex, step);
    }

    protected override float GetExternalDamageMultiplier()
        => desireSkill != null ? desireSkill.DamageMultiplier : 1f;

    protected override float GetExternalAttackSpeedMultiplier()
        => desireSkill != null ? desireSkill.AttackSpeedMultiplier : 1f;

    protected override float GetExternalMoveMultiplier()
        => desireSkill != null ? desireSkill.MoveSpeedMultiplier : 1f;

    protected override float GetExternalCooldownMultiplier()
        => desireSkill != null ? desireSkill.SkillCooldownMultiplier : 1f;

    protected override void Die()
    {
        RestoreMotionGravity();
        desireSkill?.Cancel();
        base.Die();
    }

    public Rigidbody2D DesireBody => rb;
    public Vector2 DesirePlayerPosition => playerTransform != null ? playerTransform.position : transform.position;
    public bool DesireIsDead => isDead;
    public bool DesireInCombat => isChasing;
    public bool DesireIsAttacking { get => isAttacking; set => isAttacking = value; }
    public void DesireStop() => StopMove();
    public void DesireInterruptSkill()
    {
        InterruptCurrentSkill();
        RestoreMotionGravity();
    }
    public void DesireResetCooldowns() => ResetAllSkillCooldowns();
    public IEnumerator DesireExecute(BossSkillData skill) => BossSkillExecutor.Execute(skill, this);
    public void DesireLog(string state, BossSkillData skill) => LogSkillDebug(state, skill);
}
