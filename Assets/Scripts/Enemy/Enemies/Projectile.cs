using UnityEngine;

// 발사체 로직: 원거리 몬스터가 발사하며, 플레이어 충돌 시 데미지를 입힙니다.
public class Projectile : MonoBehaviour
{
    [SerializeField] private float speed = 10f;
    [SerializeField] private float lifeTime = 3f;

    private Vector2 direction;
    private float damage;
    private bool initialized = false;

    // 발사체 초기 설정 (방향, 데미지)
    public void Setup(Vector2 dir, float dmg)
    {
        direction = dir.normalized;
        damage = dmg;
        initialized = true;

        // 발사 방향 회전(2D)
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // 자동 소멸
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (!initialized) return;
        transform.position += (Vector3)(direction * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 플레이어 충돌
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<PlayerController>(out var player))
            {
                // 투사체 위치 전달 (넉백 방향 계산용)
                player.TakeDamage(damage, (Vector2)transform.position);
            }

            Destroy(gameObject);
            return;
        }

        // 지형 충돌 (Ground / Ground2)
        int ground = LayerMask.NameToLayer("Ground");
        int ground2 = LayerMask.NameToLayer("Ground2");

        if (collision.gameObject.layer == ground || collision.gameObject.layer == ground2)
        {
            Destroy(gameObject);
        }
    }
}