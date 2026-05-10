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

    // 실제 원거리 공격 사거리(rangedAttackRange > 0이면 그 값, 아니면 attackRange로 fallback)
    private float EffectiveRangedAttackRange =>
        (data != null && data.rangedAttackRange > 0f) ? data.rangedAttackRange : data.attackRange;

    // 프레임 로직: 상태 차단 → 플레이어 감지 → 사거리 내 정지 + 공격 시도
    protected override void Update()
    {
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

    // 물리 프레임 로직: 절벽 회피 + 원거리 전용 이동(사거리 내 정지, 밖에서 추적)
    protected override void FixedUpdate()
    {
        if (isKnockback) return;
        if (isIdle || isReacting || isAttacking)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // 1) 절벽 감지
        RaycastHit2D groundInfo = Physics2D.Raycast(groundCheck.position, Vector2.down, data.checkDistance, groundLayer);

        // 2) 거리 및 범위 체크
        float distanceToPlayer = playerTransform != null ? Vector2.Distance(transform.position, playerTransform.position) : float.MaxValue;
        bool isInDetectRange = distanceToPlayer <= data.detectRange;
        bool isInChaseRange = distanceToPlayer <= data.chaseRange;

        // 사거리 안이면 우선 정지(절벽 회피로 인한 사거리 내 접근 반복 방지)
        if (isChasing && playerTransform != null)
        {
            float attackDistToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (attackDistToPlayer <= EffectiveRangedAttackRange)
            {
                isWandering = false;
                unreachableTimer = 0f;
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                return;
            }
        }

        // 3) 절벽 처리: 상태와 무관하게 즉시 정지 + 방향 전환
        if (groundInfo.collider == null)
        {
            isWandering = true;
            wanderTimer = Mathf.Max(wanderTimer, 0.2f);

            if (!edgeTurnLock)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

                if (isInDetectRange)
                {
                    isWandering = true;
                    wanderTimer = 0.6f;
                    walkDirection *= -1;
                    delayedDirection = walkDirection;
                    Flip(walkDirection);
                    unreachableTimer = 0f;
                }
                else if (isInChaseRange)
                {
                    unreachableTimer += Time.fixedDeltaTime;
                    if (unreachableTimer < 5.0f)
                    {
                        isWandering = true;
                        wanderTimer = 0.6f;
                        walkDirection *= -1;
                        delayedDirection = walkDirection;
                        Flip(walkDirection);
                    }
                    else
                    {
                        StopChasingForcedForRanged();
                    }
                }
                else
                {
                    StopChasingForcedForRanged();
                }

                edgeTurnLock = true;
            }
            else
            {
                rb.linearVelocity = new Vector2(walkDirection * data.moveSpeed, rb.linearVelocity.y);
                Flip(walkDirection);
            }

            return;
        }
        else
        {
            edgeTurnLock = false;
        }

        // 4) 평지 추적 실패 타이머
        if (!isInDetectRange && isInChaseRange && isChasing)
        {
            unreachableTimer += Time.fixedDeltaTime;
            if (unreachableTimer >= 5.0f) StopChasingForcedForRanged();
        }
        else
        {
            unreachableTimer = 0f;
        }

        // 5) 원거리 이동 분기
        if (isWandering)
        {
            rb.linearVelocity = new Vector2(walkDirection * data.moveSpeed, rb.linearVelocity.y);
            Flip(walkDirection);
        }
        else if (isChasing)
        {
            // 사거리 밖이면 추적, 안이면 정지
            float attackDistToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (attackDistToPlayer > EffectiveRangedAttackRange)
                MoveTowardsPlayer();
            else
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
        else
        {
            ApplyMovement(data.moveSpeed);
        }
    }

    // 원거리용 추적 강제 종료 처리
    private void StopChasingForcedForRanged()
    {
        isWandering = true;
        wanderTimer = 2.0f;

        if (playerTransform != null)
            walkDirection = transform.position.x > playerTransform.position.x ? 1 : -1;
        else
            walkDirection *= -1;

        delayedDirection = walkDirection;
        isChasing = false;
        playerTransform = null;
        unreachableTimer = 0f;
        nextUpdateTimer = Time.time + 1.0f;
    }

    // 원거리 공격 시작: 공격 상태 시간 설정 + 공격 시퀀스 실행(중복 실행 방지)
    protected override void Attack()
    {
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
