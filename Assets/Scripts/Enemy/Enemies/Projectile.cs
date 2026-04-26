using UnityEngine;

// 발사체 로직: 원거리 몬스터가 발사하며, 플레이어 충돌 시 데미지를 입힙니다.
public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    private Vector2 direction;
    private float damage;

    // 발사체 초기 설정 (방향, 데미지)
    public void Setup(Vector2 dir, float dmg)
    {
        direction = dir.normalized; // 방향 벡터 정규화
        damage = dmg;

        // [추가] 투사체가 날아가는 방향을 바라보도록 회전 설정
        // 화살 같은 긴 발사체를 위해 필수적인 로직입니다.
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // 3초 뒤 자동 소멸 (화면 밖으로 무한히 나가는 것 방지)
        Destroy(gameObject, 3f);
    }

    void Update()
    {
        // 설정된 방향으로 매 프레임 이동
        // transform.right를 사용하면 회전된 방향 기준으로 직선 이동합니다.
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. 플레이어와 충돌했는지 확인
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<PlayerController>(out var player))
            {
                // [중요] 투사체가 부딪힌 현재 위치를 전달하여 플레이어가 반대 방향으로 넉백되게 함
                player.TakeDamage(damage, (Vector2)transform.position);
            }

            // 충돌 후 발사체 제거 (폭발 이펙트 등이 있다면 여기서 생성)
            Destroy(gameObject);
        }
        // 2. 지형에 부딪혔을 때 소멸 (Ground와 Ground2 모두 포함하고 싶다면 Layer로 체크하는 것이 좋음)
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Ground") ||
                 collision.gameObject.layer == LayerMask.NameToLayer("Ground2"))
        {
            Destroy(gameObject);
        }
    }
}