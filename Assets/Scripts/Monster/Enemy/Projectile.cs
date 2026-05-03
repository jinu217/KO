using UnityEngine;

// [원거리 발사체] 직선 이동 후 플레이어/지형 충돌 시 처리하는 투사체 로직
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;    // 발사체 이동 속도
    [SerializeField] private float lifeTime = 3f;  // 자동 소멸 시간(초)

    private Vector2 direction;      // 발사 방향(정규화)
    private float damage;           // 타격 데미지
    private bool initialized = false; // Setup 완료 여부

    // 발사 직후 초기 설정(방향, 데미지)
    public void Setup(Vector2 dir, float dmg)
    {
        direction = dir.normalized;
        damage = dmg;
        initialized = true;

        // 진행 방향으로 스프라이트 회전(2D)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // 일정 시간 후 자동 삭제
        Destroy(gameObject, lifeTime);
    }

    // 매 프레임 발사체 이동
    private void Update()
    {
        if (!initialized) return;
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    // 트리거 충돌 처리: 플레이어 타격 / 지형 충돌 시 파괴
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어 충돌
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<PlayerController>(out var player))
            {
                // 충돌 지점 전달(플레이어 넉백 방향 계산용)
                player.TakeDamage(damage, (Vector2)transform.position);
            }

            Destroy(gameObject);
            return;
        }

        // 지형 충돌(Ground / Ground2 레이어)
        int ground = LayerMask.NameToLayer("Ground");
        int ground2 = LayerMask.NameToLayer("Ground2");

        if (collision.gameObject.layer == ground || collision.gameObject.layer == ground2)
        {
            Destroy(gameObject);
        }
    }
}
