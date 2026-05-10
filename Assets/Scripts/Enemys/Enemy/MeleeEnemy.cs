using UnityEngine;
using System.Collections;

// [적 AI - 근거리 전용]
// - EnemyController 공통 기능을 사용해 근접 사각형 판정(CheckHit) 공격 수행
public class MeleeEnemy : EnemyController
{
    [Header("디버그")]
    [Tooltip("공격 단계 디버그 로그 출력")]
    [SerializeField] private bool showAttackLog = false;

    // 근거리 공격 시작: 선딜/후딜을 포함한 공격 상태 시간 설정
    protected override void Attack()
    {
        attackTimer = data.preAttackDelay + data.postAttackDelay;
        StartCoroutine(AttackSequenceRoutine());
    }

    // 근거리 공격 시퀀스: 선딜 → 실제 타격 판정 → 후딜
    private IEnumerator AttackSequenceRoutine()
    {
        if (showAttackLog)
            Debug.Log($"[{gameObject.name}] 근거리 공격 준비");

        yield return new WaitForSeconds(data.preAttackDelay);

        CheckHit();

        if (showAttackLog)
            Debug.Log($"[{gameObject.name}] 근거리 공격 판정 발생");

        yield return new WaitForSeconds(data.postAttackDelay);

        if (showAttackLog)
            Debug.Log($"[{gameObject.name}] 근거리 공격 종료");
    }
}
