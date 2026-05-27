using UnityEngine;

// [스테이지1 보스 검기 투사체]
// - 직선 이동 후 플레이어 1회 적중 시 파괴
public class Stage1_BossSwordWave : MonoBehaviour
{
    [Header("기본 설정")]
    [Tooltip("발사체 이동 속도")]
    [SerializeField] private float speed = 8f;
    [Tooltip("자동 소멸 시간(초)")]
    [SerializeField] private float lifeTime = 3f;

    [Header("디버그")]
    [Tooltip("초기화/적중 디버그 로그 출력")]
    [SerializeField] private bool showDebugLog = false;

    private int direction = 1;        // 진행 방향(-1 또는 1)
    private float damage;             // 타격 데미지
    private LayerMask playerLayer;    // 플레이어 충돌 판정 레이어
    private bool hasHit;              // 이미 1회 적중 처리했는지 여부

    private Rigidbody2D rb;
    private Collider2D hitCollider;
    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hitCollider = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // 생성 직후 초기화: 방향/데미지/레이어/크기 배율 설정 + 자동 소멸 예약
    public void Init(int direction, float damage, LayerMask playerLayer, float sizeMultiplier = 1f)
    {
        this.direction = direction >= 0 ? 1 : -1;
        this.damage = damage;
        this.playerLayer = playerLayer;

        transform.localScale *= Mathf.Max(0.01f, sizeMultiplier);

        if (spriteRenderer != null)
            spriteRenderer.flipX = this.direction < 0;

        if (rb != null)
            rb.linearVelocity = new Vector2(this.direction * speed, 0f);

        if (showDebugLog)
            Debug.Log($"[BossSwordWave] init dir={this.direction}, dmg={damage}, speed={speed}");

        Destroy(gameObject, lifeTime);
    }

    // 플레이어 레이어 충돌 시 1회 데미지 후 파괴
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        if (((1 << other.gameObject.layer) & playerLayer) == 0)
            return;

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            hasHit = true;
            target.TakeDamage(damage, (Vector2)transform.position, true, false);

            if (showDebugLog)
                Debug.Log($"[BossSwordWave] hit player dmg={damage}");

            Destroy(gameObject);
        }
    }
}
