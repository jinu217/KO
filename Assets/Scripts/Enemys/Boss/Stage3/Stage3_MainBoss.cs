using System.Collections;
using UnityEngine;

// 일반 전투와 자아실현 욕망 스킬 사이의 연결을 담당한다.
public sealed class Stage3_MainBoss : MainBossController
{
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
        desireSkill?.Cancel();
        base.Die();
    }

    public Rigidbody2D DesireBody => rb;
    public Vector2 DesirePlayerPosition => playerTransform != null ? playerTransform.position : transform.position;
    public bool DesireIsDead => isDead;
    public bool DesireIsAttacking { get => isAttacking; set => isAttacking = value; }
    public void DesireStop() => StopMove();
    public void DesireResetCooldowns() => ResetAllSkillCooldowns();
    public IEnumerator DesireExecute(BossSkillData skill) => BossSkillExecutor.Execute(skill, this);
    public void DesireLog(string state, BossSkillData skill) => LogSkillDebug(state, skill);
}
