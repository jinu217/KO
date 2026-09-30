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

    [Header("Animation")]
    [SerializeField] private Animator npcAnimator;
    [Tooltip("찌르기 시작부터 타격까지 시간(초). 클립에 맞춰 조절하세요.")]
    [Min(0f)][SerializeField] private float attackHitDelay = 0.5f;
    [Tooltip("Animator가 없을 때 사용하는 공격 전체 시간입니다.")]
    [Min(0f)][SerializeField] private float fallbackAttackDuration = 1.166667f;
    public bool IsPerformingAttack { get; private set; }
    private bool hasAttackTrigger;
    private bool hitApplied;
    private bool enteredAttackState;
    private float attackElapsed;
    private float nextAttackTime;

    private void Awake()
    {
        if (npcAnimator == null) npcAnimator = GetComponentInChildren<Animator>(true);
        if (npcAnimator != null)
            foreach (var p in npcAnimator.parameters)
                if (p.name == "Attack" && p.type == AnimatorControllerParameterType.Trigger)
                    hasAttackTrigger = true;
    }

    private void OnDisable()
    {
        IsPerformingAttack = false;
        if (npcAnimator != null && hasAttackTrigger) npcAnimator.ResetTrigger("Attack");
    }

    private void OnEnable()
    {
        nextAttackTime = Time.time + attackInterval;
    }

    private void Update()
    {
        if (!IsActiveStep())
        {
            IsPerformingAttack = false;
            if (npcAnimator != null && hasAttackTrigger) npcAnimator.ResetTrigger("Attack");
            nextAttackTime = Time.time + Mathf.Max(.01f, attackInterval);
            return;
        }
        if (IsPerformingAttack)
        {
            attackElapsed += Time.deltaTime;
            bool animated = npcAnimator != null && npcAnimator.isActiveAndEnabled && hasAttackTrigger;
            var state = animated ? npcAnimator.GetCurrentAnimatorStateInfo(0) : default(AnimatorStateInfo);
            bool thrust = animated && state.IsName("Mentor_Thrust");
            if (thrust) enteredAttackState = true;
            // Use the playing state's time, so its speed also changes hit timing.
            float motionTime = thrust ? state.normalizedTime * state.length : attackElapsed;
            if (!hitApplied && (thrust || !animated) && motionTime >= attackHitDelay)
            {
                hitApplied = true;
                Attack();
            }
            bool finished = animated
                ? (enteredAttackState && (!thrust || state.normalizedTime >= 1f))
                : attackElapsed >= Mathf.Max(attackHitDelay, fallbackAttackDuration);
            if (animated && !enteredAttackState && attackElapsed > 1f)
            {
                npcAnimator.ResetTrigger("Attack");
                finished = true;
                Debug.LogWarning("Mentor_Thrust에 진입하지 못했습니다. Setup Mentor Animator와 Animator 연결을 확인하세요.", this);
            }
            if (finished)
            {
                IsPerformingAttack = false;
                nextAttackTime = Time.time + Mathf.Max(.01f, attackInterval);
            }
            return;
        }
        if (Time.time < nextAttackTime) return;
        IsPerformingAttack = true;
        attackElapsed = 0f;
        hitApplied = enteredAttackState = false;
        if (npcAnimator != null && hasAttackTrigger) npcAnimator.SetTrigger("Attack");
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
