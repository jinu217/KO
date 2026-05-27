using UnityEngine;

// [원거리 발사체]
// - 발사 시 목표 위치(플레이어 중앙)까지의 탄도 속도를 계산하여 포물선으로 날아감
// - 매 프레임 속도 방향으로 스프라이트가 회전하여 화살이 궤적을 따라 기울어지는 연출
public class Enemy_Projectile : MonoBehaviour
{
    [Header("기본 설정")]
    [Tooltip("수평 기준 속도(비행 시간 = 수평거리 / speed). 클수록 화살이 빠르고 납작하게 날아감")]
    [SerializeField] private float speed = 10f;
    [Tooltip("자동 소멸 시간(초) — 실제 비행 시간보다 길게 설정됨")]
    [SerializeField] private float lifeTime = 4f;
    [Tooltip("중력 배율 (클수록 포물선이 가파름, 0.4~0.8 권장)")]
    [SerializeField] private float gravityScale = 0.6f;
    [Tooltip("최소 비행 시간(초) — 매우 가까운 거리에서 화살이 너무 빠르지 않도록")]
    [SerializeField] private float minFlightTime = 0.4f;
    [Tooltip("최대 비행 시간(초) — 먼 거리에서 포물선이 너무 높아지지 않도록")]
    [SerializeField] private float maxFlightTime = 2.0f;

    [Header("디버그")]
    [Tooltip("발사/적중 디버그 로그 출력")]
    [SerializeField] private bool showDebugLog = false;

    private float damage;
    private bool initialized;

    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Setup 전 낙하 방지
        if (rb != null)
            rb.gravityScale = 0f;
    }

    // 목표 위치(플레이어 중앙)까지 포물선 탄도 계산 후 초기 속도 부여
    public void Setup(Vector2 targetPos, float dmg)
    {
        damage = dmg;
        initialized = true;

        Vector2 startPos = transform.position;

        // 비행 시간: 거리 / 수평 기준 속도, min~max 사이로 고정
        float dist = Vector2.Distance(startPos, targetPos);
        float T = Mathf.Clamp(dist / Mathf.Max(speed, 0.01f), minFlightTime, maxFlightTime);

        // 탄도 공식: y(T) = vy*T - 0.5*g*T²  →  vy = (Δy + 0.5*g*T²) / T
        float g = Mathf.Abs(Physics2D.gravity.y) * gravityScale;
        float vx = (targetPos.x - startPos.x) / T;
        float vy = (targetPos.y - startPos.y) / T + 0.5f * g * T;

        if (rb != null)
        {
            rb.gravityScale = gravityScale;
            rb.linearVelocity = new Vector2(vx, vy);
        }

        // 발사 각도로 초기 스프라이트 회전
        float angle = Mathf.Atan2(vy, vx) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (showDebugLog)
            Debug.Log($"[Projectile] target={targetPos}, dist={dist:F2}, T={T:F2}, v=({vx:F2},{vy:F2})");

        Destroy(gameObject, Mathf.Max(lifeTime, T + 0.3f));
    }

    // 매 프레임 속도 방향으로 스프라이트 회전 (화살이 포물선 궤적을 따라 기울어짐)
    private void Update()
    {
        if (!initialized || rb == null) return;

        if (rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    // 트리거 충돌: 플레이어 적중 시 데미지 + 파괴 / 지형 충돌 시 파괴
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            IDamageable target = collision.GetComponentInParent<IDamageable>();
            target?.TakeDamage(damage, (Vector2)transform.position, true, true);

            if (showDebugLog)
                Debug.Log($"[Projectile] hit player dmg={damage}");

            Destroy(gameObject);
            return;
        }

        int ground  = LayerMask.NameToLayer("Ground");
        int ground2 = LayerMask.NameToLayer("Ground2");

        if (collision.gameObject.layer == ground || collision.gameObject.layer == ground2)
            Destroy(gameObject);
    }
}
