using UnityEngine;

// 2스테이지 중간보스는 공통 데이터 스킬만 사용한다.
public sealed class Stage2_MidBoss : MidBossController
{
    [Header("거리 쿨타임 감소")]
    [SerializeField, Min(0f), Tooltip("3스킬을 한 번 이상 사용한 뒤 이 거리 이상 벌어지면 남은 쿨타임을 한 번 50% 줄입니다.")]
    private float skill3ReductionDistance = 6.5f;

    protected override void Update()
    {
        base.Update();
        TryReduceRemainingSkillCooldownByDistance(2, skill3ReductionDistance);
    }
}
