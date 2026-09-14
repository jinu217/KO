using System.Collections;
using UnityEngine;

public sealed class Stage1_MainBoss : MainBossController
{
    [Header("Stage1 전용")]
    [SerializeField, Tooltip("1스테이지 메인보스의 욕망 강화 상태를 관리하는 전용 컴포넌트입니다.")]
    private Stage1_DesireSkill desireSkill;

    [Header("거리 쿨타임 감소")]
    [SerializeField, Min(0f), Tooltip("2스킬 검기를 한 번 이상 사용한 뒤 이 거리 이상 벌어지면 남은 쿨타임을 한 번 50% 줄입니다.")]
    private float skill2ReductionDistance = 5f;
    [SerializeField, Min(0f), Tooltip("4스킬 돌진을 한 번 이상 사용한 뒤 이 거리 이상 벌어지면 남은 쿨타임을 한 번 50% 줄입니다.")]
    private float skill4ReductionDistance = 8f;

    private int skill1FinalHitCount;
    private bool nextSkill1Empowered;
    private bool usingEmpoweredSkill1;

    protected override void Awake()
    {
        base.Awake();
        if (desireSkill == null) desireSkill = GetComponent<Stage1_DesireSkill>();
        if (desireSkill == null) desireSkill = gameObject.AddComponent<Stage1_DesireSkill>();
        desireSkill.Initialize(data);
    }

    protected override void Update()
    {
        base.Update();
        TryReduceRemainingSkillCooldownByDistance(1, skill2ReductionDistance);
        TryReduceRemainingSkillCooldownByDistance(3, skill4ReductionDistance);
        if (isDead || isAttacking || desireSkill == null) return;
        if (desireSkill.CanActivate(currentPhase)) StartCoroutine(UseDesire());
    }

    private IEnumerator UseDesire()
    {
        isAttacking = true;
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        LogSkillDebug("욕망 스킬 시작", new BossSkillData { skillName = "욕망 스킬", cooldown = 30f });
        yield return desireSkill.Activate(ResetAllSkillCooldowns);
        LogSkillDebug("욕망 스킬 시전 종료", new BossSkillData { skillName = "욕망 스킬", cooldown = 30f });
        isAttacking = false;
    }

    protected override BossSkillData GetBasicAttackData()
    {
        if (desireSkill != null && desireSkill.MainBuffActive && desireSkill.EnhancedBasicAttack != null)
            return desireSkill.EnhancedBasicAttack;
        return base.GetBasicAttackData();
    }

    protected override BossSkillData GetSkillData(int index)
    {
        if (desireSkill != null && desireSkill.SkillEnhanceActive)
        {
            if (index == 0 && desireSkill.EnhancedSkill1 != null) return desireSkill.EnhancedSkill1;
            if (index == 1 && desireSkill.EnhancedSkill2 != null) return desireSkill.EnhancedSkill2;
        }
        if (index == 0 && nextSkill1Empowered && desireSkill != null && desireSkill.EmpoweredSkill1 != null)
        {
            usingEmpoweredSkill1 = true;
            return desireSkill.EmpoweredSkill1;
        }
        return base.GetSkillData(index);
    }

    public override void OnSkillAttackHit(BossAttackStepData step)
    {
        if (currentSkillIndex != 0 || usingEmpoweredSkill1 || (desireSkill != null && desireSkill.SkillEnhanceActive)) return;
        if (step.stepName != null && step.stepName.Contains("3타"))
        {
            skill1FinalHitCount++;
            if (skill1FinalHitCount >= 3)
            {
                skill1FinalHitCount = 0;
                nextSkill1Empowered = true;
            }
        }
    }

    protected override void OnSkillExecutionFinished(int index)
    {
        if (index == 0 && usingEmpoweredSkill1)
        {
            usingEmpoweredSkill1 = false;
            nextSkill1Empowered = false;
        }
    }

    protected override float GetExternalMoveMultiplier()
        => desireSkill != null ? desireSkill.MoveSpeedMultiplier : 1f;
    protected override float GetExternalAttackSpeedMultiplier()
        => desireSkill != null ? desireSkill.AttackSpeedMultiplier : 1f;
    protected override bool IsExternalBuffBlocking()
        => desireSkill != null && desireSkill.MainBuffActive;

    protected override void Die()
    {
        desireSkill?.Cancel();
        base.Die();
    }

}
