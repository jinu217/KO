using UnityEngine;

public class TutorialDashInstructor : MonoBehaviour
{
    [Header("Tutorial Step")]
    [Tooltip("교관 공격이 시작되는 튜토리얼 단계입니다.")]
    [SerializeField] private TutorialStep activeFromStep = TutorialStep.DashPositionGuide;

    [Tooltip("교관 공격이 끝나는 튜토리얼 단계입니다.")]
    [SerializeField] private TutorialStep activeUntilStep = TutorialStep.ChestInteractionGuide;

    [Header("Attack")]
    [Tooltip("공격할 플레이어 레이어입니다. 비워두면 태그 Player도 같이 확인합니다.")]
    [SerializeField] private LayerMask playerLayer;

    [Tooltip("몇 초마다 공격할지 설정합니다.")]
    [SerializeField] private float attackInterval = 1f;

    [Tooltip("교관 기준 공격 범위 크기입니다.")]
    [SerializeField] private Vector2 attackSize = new Vector2(3f, 1.5f);

    [Tooltip("교관 기준 공격 범위 중심 오프셋입니다. 기본값은 왼쪽(-X) 공격입니다.")]
    [SerializeField] private Vector2 attackOffset = new Vector2(-1.5f, 0f);

    [Tooltip("피격 시 데미지입니다. 튜토리얼 교관은 기본 0입니다.")]
    [SerializeField] private float damage = 0f;

    [Tooltip("켜면 맞은 플레이어에게 넉백을 적용합니다.")]
    [SerializeField] private bool applyKnockback = true;

    [Tooltip("켜면 피격 무적도 같이 적용합니다. 보통 교관은 꺼둡니다.")]
    [SerializeField] private bool applyIFrame = false;

    [Header("Debug")]
    [SerializeField] private bool showAttackGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(1f, 0.2f, 0.1f, 0.7f);

    private float nextAttackTime;

    private void OnEnable()
    {
        nextAttackTime = Time.time + attackInterval;
    }

    private void Update()
    {
        if (!IsActiveStep())
            return;

        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + Mathf.Max(0.01f, attackInterval);
        Attack();
    }

    private void Attack()
    {
        Vector2 center = (Vector2)transform.position + attackOffset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, attackSize, 0f, GetLayerMask());

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
                continue;

            PlayerController player = hit.GetComponentInParent<PlayerController>();
            if (player == null || !player.CompareTag("Player"))
                continue;

            player.TakeDamage(damage, transform.position, applyKnockback, applyIFrame);
            return;
        }
    }

    private int GetLayerMask()
    {
        return playerLayer.value != 0 ? playerLayer.value : ~0;
    }

    private bool IsActiveStep()
    {
        TutorialManager manager = TutorialManager.Instance;
        if (manager == null || !manager.IsActive)
            return true;

        TutorialStep step = manager.CurrentStep;
        return step >= activeFromStep && step <= activeUntilStep;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showAttackGizmo)
            return;

        Gizmos.color = gizmoColor;
        Vector3 center = transform.position + (Vector3)attackOffset;
        Gizmos.DrawWireCube(center, new Vector3(attackSize.x, attackSize.y, 0f));
    }
}
