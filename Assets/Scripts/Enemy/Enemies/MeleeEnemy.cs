using UnityEngine;
using System.Collections;

// 근거리 몬스터: 플레이어에게 접근하여 전방 사각형 범위에 타격을 가합니다.
public class MeleeEnemy : EnemyController
{
    protected override void Attack()
    {
        // 1. 공격 전체 지속 시간 설정 (선딜레이 + 후딜레이)
        // 이 시간 동안 부모 클래스(EnemyController)의 Update에서 이동을 제한합니다.
        attackTimer = data.preAttackDelay + data.postAttackDelay;

        // 2. 공격 시퀀스 코루틴 실행
        // 애니메이션을 사용 중이라면 여기서 애니메이션 트리거를 실행하세요.
        // GetComponent<Animator>().SetTrigger("Attack");

        StartCoroutine(AttackSequenceRoutine());
    }

    private IEnumerator AttackSequenceRoutine()
    {
        Debug.Log(gameObject.name + "의 공격 준비... (선딜레이)");

        // 3. 선딜레이 대기 (플레이어가 보고 피할 수 있는 시간)
        yield return new WaitForSeconds(data.preAttackDelay);

        // 4. 실제 타격 판정 실행
        // 부모 클래스에 구현된 CheckHit()을 호출하여 
        // 몬스터 전방 사각형 범위 내에 플레이어가 있는지 체크 후 데미지를 입힙니다.
        CheckHit();

        Debug.Log(gameObject.name + "의 공격 판정 발생!");

        // 5. 후딜레이 대기 (공격 후 헛스윙 하거나 자세를 추스르는 시간)
        yield return new WaitForSeconds(data.postAttackDelay);

        Debug.Log(gameObject.name + "의 공격 종료.");
    }
}