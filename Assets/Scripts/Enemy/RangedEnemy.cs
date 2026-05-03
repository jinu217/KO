using UnityEngine;
using System.Collections;

// [적 AI - 원거리 전용] 공격 범위 내에서 투사체를 발사하고, 범위 밖에서는 플레이어를 추적
public class RangedEnemy : EnemyController
{
    private Coroutine attackRoutine; // 중복 공격 코루틴 실행 방지

    // 실제 원거리 공격 사거리
    // rangedAttackRange > 0 이면 그 값을 사용, 아니면 attackRange를 fallback으로 사용
    private float EffectiveRangedAttackRange => (data != null && data.rangedAttackRange > 0f) ? data.rangedAttackRange : data.attackRange;

    // 프레임 로직: 상태 차단 -> 플레이어 감지 -> 원거리 공격 시도
    protected override void Update()
    {
        // 넉백/경직 상태에서는 AI 중지
        if (isKnockback || isIdle)
        {
            HandleTimers();
            if (isIdle) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // 공격/반응 상태에서는 이동 차단
        if (isAttacking || isReacting)
        {
            HandleTimers();
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // 피격 어그로가 남아있으면 탐지 범위를 무시하고 강제 추격
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

                float attackDist = Vector2.Distance(transform.position, playerTransform.position);

                // 절벽 회피 배회 중에는 방향 강제 보정을 하지 않아야 절벽 반전이 유지됨
                if (!isWandering && !edgeTurnLock)
                {
                    walkDirection = (playerTransform.position.x >= transform.position.x) ? 1 : -1;
                    Flip(walkDirection);
                }
                if (attackDist <= EffectiveRangedAttackRange)
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

                    if (Time.time > lastAttackTime + (1f / data.attackSpeed))
                    {
                        StartAttack();
                    }
                }

                HandleTimers();
                return;
            }
        }

        // 추적 중이면 chaseRange, 아니면 detectRange 기준으로 플레이어 탐지
        float currentRange = isChasing ? data.chaseRange : data.detectRange;
        Collider2D playerCol = Physics2D.OverlapCircle(transform.position, currentRange, playerLayer);

        if (playerCol != null)
        {
            bool wasChasing = isChasing;
            playerTransform = playerCol.transform;

            // 최초 감지 시 반응 지연 우선 적용
            if (!wasChasing && !isReacting && reactionTimer <= 0f)
            {
                StartReaction();
                HandleTimers();
                return;
            }

            isChasing = true;

            // 원거리 공격 사거리 내이면 정지 + 공격 쿨다운 체크
            float attackDist = Vector2.Distance(transform.position, playerTransform.position);

            // 절벽 회피 배회 중에는 방향 강제 보정을 하지 않아야 절벽 반전이 유지됨
            if (!isWandering && !edgeTurnLock)
            {
                walkDirection = (playerTransform.position.x >= transform.position.x) ? 1 : -1;
                Flip(walkDirection);
            }
            if (attackDist <= EffectiveRangedAttackRange)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

                if (Time.time > lastAttackTime + (1f / data.attackSpeed))
                {
                    StartAttack();
                }
            }
        }
        else
        {
            // 플레이어를 잃으면 추적 해제
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

        // 2) 거리 및 범위 체크(공통 범위)
        float distanceToPlayer = playerTransform != null ? Vector2.Distance(transform.position, playerTransform.position) : float.MaxValue;
        bool isInDetectRange = distanceToPlayer <= data.detectRange;
        bool isInChaseRange = distanceToPlayer <= data.chaseRange;

        // 플레이어가 원거리 공격 사거리 안이면 우선 정지 유지
        // (절벽 회피/배회로 인해 사거리 내 접근 반복되는 현상 방지)
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

        // 3) 절벽 처리: 상태와 무관하게 즉시 정지 + 방향 전환 처리
        // (배회 중 절벽 감지 예외를 두면 낙사할 수 있음)
        if (groundInfo.collider == null)
        {
            // 절벽 감지 중에는 배회 상태를 유지해 Update에서 방향이 다시 플레이어 쪽으로 덮이지 않게 함
            isWandering = true;
            wanderTimer = Mathf.Max(wanderTimer, 0.2f);

            // 절벽에서 매 프레임 반전하면 제자리에서 떨림/고착되므로 1회만 반전
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
                // 이미 반전한 뒤에도 센서가 절벽을 보고 있으면 반대 방향으로 뒤로 물러남
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
            // 원거리 공격 사거리 밖이면 추적, 안이면 정지
            float attackDistToPlayer = Vector2.Distance(transform.position, playerTransform.position);
            if (attackDistToPlayer > EffectiveRangedAttackRange)
            {
                MoveTowardsPlayer();
            }
            else
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            }
        }
        else
        {
            ApplyMovement(data.moveSpeed);
        }
    }

    // 원거리용 추적 강제 종료 처리(공통 StopChasingForced와 동일 의도)
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

    // 원거리 공격 시작: 공격 상태 시간 설정 + 공격 시퀀스 실행
    protected override void Attack()
    {
        if (attackRoutine != null) return; // 중복 실행 방지

        // 공격 상태 시간(선딜 + 후딜)
        attackTimer = data.preAttackDelay + data.postAttackDelay;

        attackRoutine = StartCoroutine(RangedAttackSequenceRoutine());
    }

    // 원거리 공격 시퀀스: 선딜 -> 발사(사거리 재검증) -> 후딜
    private IEnumerator RangedAttackSequenceRoutine()
    {
        Debug.Log($"{gameObject.name}의 원거리 조준 시작...");

        // 1) 선딜레이
        yield return new WaitForSeconds(data.preAttackDelay);

        // 2) 발사 시점에 사거리 재검증(판정 시점 기준)
        if (data.projectilePrefab != null && playerTransform != null)
        {
            float attackDistNow = Vector2.Distance(transform.position, playerTransform.position);
            if (attackDistNow <= EffectiveRangedAttackRange)
            {
                // 발사 위치(현재 바라보는 방향 전방)
                Vector3 spawnPos = transform.position + new Vector3(walkDirection * 0.5f, 0f, 0f);

                GameObject proj = Instantiate(data.projectilePrefab, spawnPos, Quaternion.identity);

                // 플레이어 현재 위치 기준 발사 방향 계산
                Vector2 shotDir = ((Vector2)playerTransform.position - (Vector2)spawnPos).normalized;

                if (proj.TryGetComponent<Projectile>(out var projectile))
                {
                    projectile.Setup(shotDir, data.damage);
                    Debug.Log($"{gameObject.name} 발사!");
                }
                else
                {
                    Debug.LogWarning($"{gameObject.name}: projectilePrefab에 Projectile 컴포넌트가 없습니다.");
                    Destroy(proj);
                }
            }
        }

        // 3) 후딜레이
        yield return new WaitForSeconds(data.postAttackDelay);

        Debug.Log($"{gameObject.name} 재정비 완료.");
        attackRoutine = null;
    }

    // 디버그 시각화: 공통 Gizmo + 원거리 실제 사거리(초록 원)
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        if (data == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, EffectiveRangedAttackRange);
    }
}
