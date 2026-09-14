using UnityEngine;

// 일반 전투 흐름만 담당하며 안전 욕구의 고유 규칙은 전용 컴포넌트에 위임한다.
public sealed class Stage2_MainBoss : MainBossController
{
    [Header("욕망 스킬")]
    [SerializeField, Tooltip("2스테이지 메인보스의 안전 필드와 복귀 행동을 관리하는 전용 욕망 컴포넌트입니다.")]
    private Stage2_MainBossDesireSkill desireSkill;
    [Header("거리 쿨타임 감소")]
    [SerializeField, Min(0f), Tooltip("2스킬을 한 번 이상 사용한 뒤 이 거리 이상 벌어지면 남은 쿨타임을 한 번 50% 줄입니다.")]
    private float skill2ReductionDistance = 7f;
    [SerializeField, Min(0f), Tooltip("4스킬을 한 번 이상 사용한 뒤 이 거리 이상 벌어지면 남은 쿨타임을 한 번 50% 줄입니다.")]
    private float skill4ReductionDistance = 8f;

    protected override void Awake()
    {
        base.Awake();
        if (desireSkill == null) desireSkill = GetComponent<Stage2_MainBossDesireSkill>();
        if (desireSkill == null) desireSkill = gameObject.AddComponent<Stage2_MainBossDesireSkill>();
        desireSkill.Initialize(this);
    }

    protected override void Update()
    {
        base.Update();
        if (desireSkill == null || !desireSkill.FieldActive)
        {
            TryReduceRemainingSkillCooldownByDistance(1, skill2ReductionDistance);
            TryReduceRemainingSkillCooldownByDistance(3, skill4ReductionDistance);
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        desireSkill?.FixedTick();
    }

    protected override bool UpdatePriorityBehavior()
        => desireSkill != null && desireSkill.Tick(currentPhase);

    protected override bool CanRunCombatAI()
        => desireSkill == null || desireSkill.CanRunCombatAI();

    protected override float GetExternalDamageMultiplier()
        => desireSkill != null ? desireSkill.OutgoingDamageMultiplier : 1f;

    public override void TakeDamage(float damage, Vector2 sourcePos, bool applyKnockback = false, bool applyIFrame = false)
    {
        desireSkill?.OnBossDamaged();
        float adjustedDamage = desireSkill != null ? desireSkill.ModifyIncomingDamage(damage) : damage;
        base.TakeDamage(adjustedDamage, sourcePos, applyKnockback, applyIFrame);
    }

    protected override void Die()
    {
        desireSkill?.Cancel();
        base.Die();
    }

    // 전용 욕망 스킬과 공통 컨트롤러 사이의 좁은 연결 지점.
    public Rigidbody2D DesireBody => rb;
    public Transform DesirePlayer => playerTransform;
    public LayerMask DesirePlayerLayer => playerLayer;
    public int DesireLookDirection => lookDirection;
    public bool DesireIsDead => isDead;
    public float DesireMaxHp => data != null ? data.maxHp : 0f;
    public bool DesireIsAttacking { get => isAttacking; set => isAttacking = value; }
    public void DesireSetChasing(bool value) => isChasing = value;
    public void DesireStop() => StopMove();
    public void DesireFacePlayer() => RefreshSkillDirection();
    public void DesireHeal(float amount)
    {
        if (data == null || amount <= 0f) return;
        currentHp = Mathf.Min(data.maxHp, currentHp + amount);
        UpdateHealthUI();
    }
    public void DesireLog(string state, string skillName, float skillCooldown)
        => LogSkillDebug(state, new BossSkillData { skillName = skillName, cooldown = skillCooldown });
}
