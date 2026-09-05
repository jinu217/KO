using UnityEngine;

// 2스테이지 메인 보스의 고유 타입. 전투 실행은 공통 MainBossController가 담당한다.
public sealed class Stage2_MainBoss : MainBossController
{
    [Header("Stage2 메인보스 거리 쿨타임 감소")]
    [SerializeField, Min(0f)] private float skill2ReductionDistance = 7f;
    [SerializeField, Min(0f)] private float skill4ReductionDistance = 8f;

    protected override void Update()
    {
        base.Update();
        TryReduceRemainingSkillCooldownByDistance(1, skill2ReductionDistance);
        TryReduceRemainingSkillCooldownByDistance(3, skill4ReductionDistance);
    }
}
