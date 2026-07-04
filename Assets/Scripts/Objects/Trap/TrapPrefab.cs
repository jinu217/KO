using UnityEngine;

// [함정 오브젝트]
// - 플레이어가 범위에 닿는 순간 즉시 데미지 → 이후 tickInterval 쿨타임 시작
// - 쿨타임은 플레이어 부재 중에도 항상 진행
//   → 플레이어가 잠시 벗어났다 돌아와도 쿨타임이 다 찼으면 즉시 데미지
public class TrapPrefab : MonoBehaviour
{
    [Header("데미지 설정")]
    [Tooltip("1회 피해량 (iFrame/넉백 없음)")]
    [SerializeField] private float damage = 5f;
    [Tooltip("공격 쿨타임(초). 플레이어가 없어도 타이머는 계속 진행")]
    [SerializeField] private float tickInterval = 0.5f;

    [Header("감지 범위")]
    [Tooltip("데미지 판정 박스 크기")]
    [SerializeField] private Vector2 damageSize = new Vector2(1f, 1f);
    [Tooltip("데미지 판정 박스 중심 오프셋")]
    [SerializeField] private Vector2 damageOffset = Vector2.zero;
    [Tooltip("플레이어 레이어 마스크")]
    [SerializeField] private LayerMask playerLayer;

    [Header("범위 시각화")]
    [SerializeField] private bool showTrapGizmo = true;

    // 음수 무한대로 초기화 → 첫 접촉 시 쿨타임 대기 없이 즉시 데미지
    private float lastDamageTime = float.NegativeInfinity;

    private void Update()
    {
        // 쿨타임 미완료면 감지 자체를 건너뜀 (쿨타임은 계속 흐르는 중)
        if (Time.time < lastDamageTime + tickInterval) return;

        Vector2 center = (Vector2)transform.position + damageOffset;
        Collider2D hit = Physics2D.OverlapBox(center, damageSize, 0f, playerLayer);
        if (hit == null) return;

        IDamageable target = hit.GetComponentInParent<IDamageable>();
        if (target == null) return;

        bool tutorialKnockback = TutorialManager.Instance != null && TutorialManager.Instance.IsActive;
        target.TakeDamage(damage, (Vector2)transform.position, tutorialKnockback, false);
        lastDamageTime = Time.time;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showTrapGizmo) return;

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
        Vector2 center = (Vector2)transform.position + damageOffset;
        Gizmos.DrawWireCube(center, damageSize);
    }
}
