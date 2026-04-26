using UnityEngine;
using System.Collections;

// 원거리 몬스터: 플레이어를 향해 발사체(Projectile)를 발사합니다.
public class RangedEnemy : EnemyController
{
    protected override void Attack()
    {
        // 1. 공격 전체 지속 시간 설정 (준비 시간 + 발사 후 경직)
        attackTimer = data.preAttackDelay + data.postAttackDelay;

        // 2. 발사 시퀀스 코루틴 시작
        StartCoroutine(RangedAttackSequenceRoutine());
    }

    private IEnumerator RangedAttackSequenceRoutine()
    {
        Debug.Log(gameObject.name + "의 원거리 조준 시작...");

        // 3. 선딜레이 (시그널): 플레이어가 투사체가 날아올 것을 예측하는 시간
        yield return new WaitForSeconds(data.preAttackDelay);

        // 4. 발사체 생성 및 설정
        if (data.projectilePrefab != null && playerTransform != null)
        {
            // 발사 위치 (몬스터 중심에서 약간 앞쪽으로 설정 가능)
            Vector3 spawnPos = transform.position + new Vector3(walkDirection * 0.5f, 0, 0);

            GameObject proj = Instantiate(data.projectilePrefab, spawnPos, Quaternion.identity);

            // 플레이어의 현재 위치를 향한 방향 계산
            Vector2 shotDir = (playerTransform.position - transform.position).normalized;

            // Projectile 컴포넌트의 Setup 함수 호출 (수정된 damage 변수 사용)
            if (proj.TryGetComponent<Projectile>(out var projectile))
            {
                projectile.Setup(shotDir, data.damage);
            }

            Debug.Log(gameObject.name + " 발사!");
        }

        // 5. 후딜레이 (경직): 발사 후 잠시 멈춰있는 시간
        yield return new WaitForSeconds(data.postAttackDelay);

        Debug.Log(gameObject.name + " 재정비 완료.");
    }
}