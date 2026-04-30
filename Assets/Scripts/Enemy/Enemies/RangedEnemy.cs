using UnityEngine;
using System.Collections;

// 원거리 몬스터: 플레이어를 향해 발사체(Projectile)를 발사합니다.
public class RangedEnemy : EnemyController
{
    private Coroutine attackRoutine; // 중복 코루틴 방지용

    // 원거리 전용 공격 사거리 (인스펙터의 rangedAttackRange가 0보다 크면 그 값을 사용)
    private float EffectiveRangedAttackRange => (data != null && data.rangedAttackRange > 0f) ? data.rangedAttackRange : data.attackRange;

    protected override void Update()
    {
        // 넉백/경직 처리
        if (isKnockback || isIdle)
        {
            HandleTimers();
            if (isIdle) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // 공격/반응 중 이동 차단
        if (isAttacking || isReacting)
        {
            HandleTimers();
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        float currentRange = isChasing ? data.chaseRange : data.detectRange;
        Collider2D playerCol = Physics2D.OverlapCircle(transform.position, currentRange, playerLayer);

        if (playerCol != null)
        {
            bool wasChasing = isChasing;
            playerTransform = playerCol.transform;

            // 최초 감지 반응 시간
            if (!wasChasing && !isReacting && reactionTimer <= 0f)
            {
                StartReaction();
                HandleTimers();
                return;
            }

            isChasing = true;

            // 항상 플레이어 쪽을 보도록 보정
            walkDirection = (playerTransform.position.x >= transform.position.x) ? 1 : -1;
            Flip(walkDirection);

            float attackDist = Vector2.Distance(transform.position, playerTransform.position);

            // [핵심 변경] 기존 공격 범위(attackRange) 안에서만 발사
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
            isChasing = false;
            playerTransform = null;
        }

        HandleTimers();
    }

    protected override void FixedUpdate()
    {
        if (isKnockback) return;
        if (isIdle || isReacting || isAttacking)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // 1. 낭떠러지 감지
        RaycastHit2D groundInfo = Physics2D.Raycast(groundCheck.position, Vector2.down, data.checkDistance, groundLayer);

        // 2. 거리 및 범위 체크
        float distanceToPlayer = playerTransform != null ? Vector2.Distance(transform.position, playerTransform.position) : float.MaxValue;
        bool isInDetectRange = distanceToPlayer <= data.detectRange;
        bool isInChaseRange = distanceToPlayer <= data.chaseRange;

        // [우선 고정] 플레이어가 공격 범위 안이면 어떤 상태든 제자리 유지
        // (절벽 회피/배회 로직보다 우선 적용해서 '한 번 쏘고 다가감' 반복 차단)
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

        // 절벽 감지 시 처리(기존 구조 유지)
        if (groundInfo.collider == null && !(isWandering && wanderTimer > 0.2f))
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            if (!isWandering)
            {
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

                return;
            }
        }

        // 평지 타이머(기존 구조 유지)
        if (!isInDetectRange && isInChaseRange && isChasing)
        {
            unreachableTimer += Time.fixedDeltaTime;
            if (unreachableTimer >= 5.0f) StopChasingForcedForRanged();
        }
        else
        {
            unreachableTimer = 0f;
        }

        // 3. 이동 로직
        if (isWandering)
        {
            rb.linearVelocity = new Vector2(walkDirection * data.moveSpeed, rb.linearVelocity.y);
            Flip(walkDirection);
        }
        else if (isChasing)
        {
            // [핵심 변경] 후퇴 없음: 공격 범위(attackRange) 밖에서만 접근, 안에서는 정지
            // 이동 기준도 가로거리(X) 기반으로 맞춰서 사거리 내 접근 반복 방지
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

    protected override void Attack()
    {
        // 이미 공격 시퀀스가 돌고 있으면 중복 실행 방지
        if (attackRoutine != null) return;

        // 공격 전체 지속 시간 설정 (준비 시간 + 발사 후 경직)
        attackTimer = data.preAttackDelay + data.postAttackDelay;

        // 발사 시퀀스 코루틴 시작
        attackRoutine = StartCoroutine(RangedAttackSequenceRoutine());
    }

    private IEnumerator RangedAttackSequenceRoutine()
    {
        Debug.Log($"{gameObject.name}의 원거리 조준 시작...");

        // 1) 선딜레이
        yield return new WaitForSeconds(data.preAttackDelay);

        // 2) 발사 직전에도 공격 범위 재확인 (근거리 CheckHit처럼 판정 시점 기준)
        if (data.projectilePrefab != null && playerTransform != null)
        {
            float attackDistNow = Vector2.Distance(transform.position, playerTransform.position);
            if (attackDistNow <= EffectiveRangedAttackRange)
            {
                // 발사 위치 (좌우 걷는 방향 기준으로 앞쪽)
                Vector3 spawnPos = transform.position + new Vector3(walkDirection * 0.5f, 0f, 0f);

                GameObject proj = Instantiate(data.projectilePrefab, spawnPos, Quaternion.identity);

                // spawnPos 기준으로 발사 방향 계산
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
        attackRoutine = null; // 공격 종료
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        if (data == null) return;

        // 원거리 전용: 실제 발사 판정 반경 표시
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, EffectiveRangedAttackRange);
    }
}
