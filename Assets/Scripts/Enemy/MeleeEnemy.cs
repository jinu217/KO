using UnityEngine;
using System.Collections;

// [적 AI - 근거리 전용] 근접 사각형 판정(CheckHit)으로 플레이어를 타격
public class MeleeEnemy : EnemyController
{
    // 근거리 공격 시작: 공격 상태 시간 설정 + 공격 시퀀스 실행
    protected override void Attack()
    {
        // 공격 중에는 이동이 막히도록 전체 공격 시간 설정
        attackTimer = data.preAttackDelay + data.postAttackDelay;

        // 실제 공격(선딜 -> 판정 -> 후딜) 코루틴 시작
        StartCoroutine(AttackSequenceRoutine());
    }

    // 근거리 공격 시퀀스
    private IEnumerator AttackSequenceRoutine()
    {
        Debug.Log(gameObject.name + "의 공격 준비... (선딜레이)");

        // 1) 선딜: 예비 동작 시간
        yield return new WaitForSeconds(data.preAttackDelay);

        // 2) 실제 타격 판정: EnemyController.CheckHit() 사용
        CheckHit();

        Debug.Log(gameObject.name + "의 공격 판정 발생!");

        // 3) 후딜: 공격 후 경직 시간
        yield return new WaitForSeconds(data.postAttackDelay);

        Debug.Log(gameObject.name + "의 공격 종료.");
    }
}
