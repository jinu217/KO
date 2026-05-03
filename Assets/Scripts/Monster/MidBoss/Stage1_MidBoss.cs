using UnityEngine;

public class Stage1_MidBoss : MidBossController
{
    protected override void Pattern1()
    {
        Debug.Log("스테이지 1 중간보스 패턴 1 사용");

        HitPlayer(
            data.pattern1Damage,
            data.pattern1Range,
            data.pattern1Height,
            data.pattern1Offset,
            data.pattern1HeightOffset
        );
    }

    protected override void Pattern2()
    {
        Debug.Log("스테이지 1 중간보스 패턴 2 사용");

        HitPlayer(
            data.pattern2Damage,
            data.pattern2Range,
            data.pattern2Height,
            data.pattern2Offset,
            data.pattern2HeightOffset
        );
    }

    protected override void Pattern3()
    {
        Debug.Log("스테이지 1 중간보스 패턴 3 사용");

        HitPlayer(
            data.pattern3Damage,
            data.pattern3Range,
            data.pattern3Height,
            data.pattern3Offset,
            data.pattern3HeightOffset
        );
    }
}