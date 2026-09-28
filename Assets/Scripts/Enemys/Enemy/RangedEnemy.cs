using UnityEngine;
using System.Collections;

// [적 AI - 원거리 전용]
// - 공격 범위 내에서 투사체 발사
// - 범위 밖에서는 플레이어 추적
// - 공통 절벽/이동 로직은 EnemyController 사용
public class RangedEnemy : EnemyController
{
    [Header("범위 시각화")]
    [Tooltip("원거리 공격 사거리(초록 원)")]
    [SerializeField] private bool showRangedAttackRangeGizmo = true;

    [Header("디버그")]
    [Tooltip("공격/조준 디버그 로그 출력")]
    [SerializeField] private bool showAttackLog = true;

    private Coroutine attackRoutine;

    protected override void CancelAttackForReaction()
    {
        if (attackRoutine != null) StopCoroutine(attackRoutine);
        attackRoutine = null;
        base.CancelAttackForReaction();
    }

    // 실제 원거리 공격 사거리(rangedAttackRange > 0이면 그 값, 아니면 attackRange로 fallback)
    private float EffectiveRangedAttackRange =>
        (data != null && data.rangedAttackRange > 0f) ? data.rangedAttackRange : data.attackRange;

    // 프레임 로직: 상태 차단 → 플레이어 감지 → 사거리 내 정지 + 공격 시도
    protected override void Update()
    {
        if (isDead) return;
        if (isHitStunned) { base.Update(); return; }
        UpdateAnimatorParameters();
        if (isKnockback || isIdle)
        {
            HandleTimers();
            if (isIdle) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        if (isAttacking || isReacting)
        {
            HandleTimers();
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // 피격 어그로가 남아있으면 탐지 범위 무시하고 강제 추격
        if (forcedAggroTimer > 0f)
        {
            if (playerTransform == null)
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null) playerTransform = playerObj.transform;
            }

            if (playerTransform != null)
            {
                isChasing = true;

                if (!isWandering && !edgeTurnLock)
                {
                    walkDirection = (playerTransform.position.x >= transform.position.x) ? 1 : -1;
                    Flip(walkDirection);
                }

                float attackDist = Vector2.Distance(transform.position, playerTransform.position);
                if (attackDist <= EffectiveRangedAttackRange)
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

                    if (Time.time > lastAttackTime + (1f / data.attackSpeed))
                        StartAttack();
                }

                HandleTimers();
                return;
            }
        }

        // 추적 중에는 chaseRange, 평소에는 detectRange
        float currentRange = isChasing ? data.chaseRange : data.detectRange;
        Collider2D playerCol = Physics2D.OverlapCircle(transform.position, currentRange, playerLayer);

        if (playerCol != null)
        {
            bool wasChasing = isChasing;
            playerTransform = playerCol.transform;

            if (!wasChasing && !isReacting && reactionTimer <= 0f)
            {
                StartReaction();
                HandleTimers();
                return;
            }

            isChasing = true;

            if (!isWandering && !edgeTurnLock)
            {
                walkDirection = (playerTransform.position.x >= transform.position.x) ? 1 : -1;
                Flip(walkDirection);
            }

            float attackDist = Vector2.Distance(transform.position, playerTransform.position);
            if (attackDist <= EffectiveRangedAttackRange)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

                if (Time.time > lastAttackTime + (1f / data.attackSpeed))
                    StartAttack();
            }
        }
        else
        {
            isChasing = false;
            playerTransform = null;
        }

        HandleTimers();
    }

    // 물리 프레임 로직: 부모의 절벽감지/이동 로직을 그대로 사용하고
    // 원거리 전용 동작(사거리 내 정지)만 부모 호출 전에 처리
    protected override void FixedUpdate()
    {
        if (isDead) return;
        if (isHitStunned) { base.FixedUpdate(); return; }
        // 사거리 안이면 이동 없이 제자리 정지 (부모 FixedUpdate 호출 전 처리)
        if (!isKnockback && isChasing && playerTransform != null && !isIdle && !isReacting && !isAttacking)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= EffectiveRangedAttackRange)
            {
                isWandering = false;
                unreachableTimer = 0f;
                ApplySlopeAwareVelocity(0f);
                return;
            }
        }

        // 절벽감지/이동 분기는 부모(EnemyController.FixedUpdate) 그대로 사용
        base.FixedUpdate();
    }



    // 원거리 공격 시작: 공격 상태 시간 설정 + 공격 시퀀스 실행(중복 실행 방지)
    protected override void Attack()
    {
        if (isDead || isHitStunned) return;
        if (attackRoutine != null) return;

        attackTimer = data.preAttackDelay + data.postAttackDelay;
        attackRoutine = StartCoroutine(RangedAttackSequenceRoutine());
    }

    // 원거리 공격 시퀀스: 선딜 → 발사(사거리 재검증) → 후딜
    private IEnumerator RangedAttackSequenceRoutine()
    {
        if (showAttackLog)
            Debug.Log($"{gameObject.name}의 원거리 조준 시작...");

        yield return new WaitForSeconds(data.preAttackDelay);

        if (isDead || isHitStunned) { attackRoutine = null; yield break; }

        if (data.projectilePrefab != null && playerTransform != null)
        {
            float attackDistNow = Vector2.Distance(transform.position, playerTransform.position);
            if (attackDistNow <= EffectiveRangedAttackRange)
            {
                Vector3 spawnPos = transform.position + new Vector3(walkDirection * 0.5f, 0f, 0f);
                GameObject proj = Instantiate(data.projectilePrefab, spawnPos, Quaternion.identity);

                // 플레이어 콜라이더 중앙을 조준 (없으면 transform.position 사용)
                Collider2D playerCol = playerTransform.GetComponent<Collider2D>();
                Vector2 targetPos = playerCol != null
                    ? (Vector2)playerCol.bounds.center
                    : (Vector2)playerTransform.position;

                if (proj.TryGetComponent<Enemy_Projectile>(out var projectile))
                {
                    projectile.Setup(targetPos, data.damage);
                    if (showAttackLog) Debug.Log($"{gameObject.name} 발사!");
                }
                else
                {
                    Debug.LogWarning($"{gameObject.name}: projectilePrefab에 Projectile 컴포넌트가 없습니다.");
                    Destroy(proj);
                }
            }
        }

        yield return new WaitForSeconds(data.postAttackDelay);

        if (showAttackLog)
            Debug.Log($"{gameObject.name} 재정비 완료.");

        attackRoutine = null;
    }

    // 디버그 시각화: 부모 Gizmo + 원거리 실제 사거리(초록 원)
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (data == null) return;
        if (!showRangedAttackRangeGizmo) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, EffectiveRangedAttackRange);
    }
}