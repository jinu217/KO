using UnityEngine;

// 1스테이지 중간보스 전용 표식 클래스.
// 공격과 스킬은 연결된 MidBossData의 Basic Attack / Skills가 실행한다.
public sealed class Stage1_MidBoss : MidBossController
{
    [Header("Stage1 중간보스 거리 쿨타임 감소")]
    [SerializeField, Min(0f)] private float skill3ReductionDistance = 6f;

    protected override void Update()
    {
        base.Update();
        TryReduceRemainingSkillCooldownByDistance(2, skill3ReductionDistance);
    }
}
