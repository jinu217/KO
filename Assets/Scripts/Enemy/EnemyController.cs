using UnityEngine;
using System.Collections;

// [적 AI - 공통 베이스] 근거리/원거리 적이 공유하는 감지, 이동, 절벽 회피, 상태/타이머 처리
public abstract class EnemyController : MonoBehaviour
{
    // -------------------- 인스펙터 참조 --------------------
    public EnemyData data;         // 적 행동/스탯 데이터(ScriptableObject)
    public LayerMask playerLayer;  // 플레이어 탐지/공격 판정 레이어
    public LayerMask groundLayer;  // 지면 감지 레이어
    public Transform groundCheck;  // 절벽 감지용 Ray 시작 위치

    // -------------------- 컴포넌트/대상 --------------------
    protected Rigidbody2D rb;          // 물리 이동 제어
    protected SpriteRenderer spriteRenderer; // 좌우 반전 처리
    protected Transform playerTransform;      // 현재 추적 대상 플레이어

    // -------------------- 상태 값 --------------------
    [Header("상태 정보")]
    protected float currentHp;      // 현재 체력
    protected int walkDirection = 1;// 이동 방향(-1:왼쪽, 1:오른쪽)
    protected bool isChasing;       // 플레이어 추적 중 여부
    protected bool isReacting;      // 최초 감지 반응 지연 상태
    protected bool isIdle;          // 경직/대기 상태
    protected bool isWandering;     // 절벽 회피 등 강제 배회 상태
    protected bool hasLookedAround; // Idle 중 고개 돌리기 수행 여부

    [Header("범위 및 시간 설정")]
    protected float unreachableTimer = 0f; // 추격 실패 누적 시간

    [Header("절벽 미련 설정")]
    protected int hesitationCount = 0; // (확장용) 절벽 앞 망설임 횟수
    public int maxHesitation = 5;      // (확장용) 최대 망설임 횟수
    protected float pauseTimer = 0f;   // (확장용) 일시 정지 타이머
    protected bool edgeTurnLock = false; // 절벽에서 방향 전환 1회만 수행하기 위한 락

    // -------------------- 타이머 --------------------
    [Header("타이머")]
    protected float nextUpdateTimer; // 추적 방향 갱신 시각
    protected int delayedDirection;  // 추적 지연 반영 방향
    protected float reactionTimer;   // 반응 지연 타이머
    protected float idleTimer;       // Idle 상태 타이머
    protected float wanderTimer;     // 배회 상태 타이머
    protected float attackTimer;     // 공격 상태 타이머(>0이면 공격 중)
    protected float knockbackTimer;  // 넉백 상태 타이머
    protected float lastAttackTime;  // 마지막 공격 시각

    [Header("피격 어그로")]
    protected float forcedAggroTimer; // 피격 후 강제 추격 유지 시간

    // 읽기 전용 상태 프로퍼티
    protected bool isKnockback => knockbackTimer > 0; // 넉백 중 여부
    protected bool isAttacking => attackTimer > 0;    // 공격 중 여부

    // 컴포넌트 초기화
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 시작 방향 랜덤 설정
        walkDirection = (Random.value < 0.5f) ? -1 : 1;
        delayedDirection = walkDirection;

        rb.mass = 10f;
    }

    // 시작 시 체력/플레이어 충돌 무시 설정
    protected virtual void Start()
    {
        if (data != null) currentHp = data.maxHp;

        Collider2D enemyCol = GetComponent<Collider2D>();
        GameObject player = GameObject.FindWithTag("Player");

        if (player != null && enemyCol != null)
        {
            Collider2D playerCol = player.GetComponent<Collider2D>();
            if (playerCol != null)
            {
                // 적-플레이어 직접 물리 충돌 무시(공격 판정은 별도 처리)
                Physics2D.IgnoreCollision(enemyCol, playerCol, true);
            }
        }

        // 몬스터끼리는 서로 겹치도록 Enemy 레이어 간 물리 충돌 무시
        int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
        if (enemyLayerIndex != -1)
        {
            Physics2D.IgnoreLayerCollision(enemyLayerIndex, enemyLayerIndex, true);
        }
    }

    // 프레임 로직: 상태 차단 -> 플레이어 감지 -> 타이머 처리
    protected virtual void Update()
    {
        // 넉백/경직 중에는 AI 중지
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

        CheckPlayerDetection();
        HandleTimers();
    }

    // 플레이어 탐지 + 공격 시도 판단
    private void CheckPlayerDetection()
    {
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

                float xDist = (playerTransform.position.x - transform.position.x) * walkDirection;
                float yDist = Mathf.Abs(transform.position.y - playerTransform.position.y);

                if (xDist > 0 && xDist <= data.attackRange && yDist <= data.attackHeight / 2f)
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

                    if (Time.time > lastAttackTime + (1f / data.attackSpeed))
                    {
                        StartAttack();
                    }
                }

                return;
            }
        }

        // 추적 중에는 chaseRange, 평소에는 detectRange 사용
        float currentRange = isChasing ? data.chaseRange : data.detectRange;
        Collider2D playerCol = Physics2D.OverlapCircle(transform.position, currentRange, playerLayer);

        if (playerCol != null)
        {
            bool wasChasing = isChasing;
            playerTransform = playerCol.transform;

            // 최초 감지 시 반응 지연 우선
            if (!wasChasing && !isReacting && reactionTimer <= 0f)
            {
                StartReaction();
                return;
            }

            isChasing = true;

            float xDist = (playerTransform.position.x - transform.position.x) * walkDirection;
            float yDist = Mathf.Abs(transform.position.y - playerTransform.position.y);

            // 전방 + 공격 높이 범위 안이면 공격 쿨다운 체크 후 StartAttack
            if (xDist > 0 && xDist <= data.attackRange && yDist <= data.attackHeight / 2f)
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
    }

    // 공격 시작 공통 처리(공격 시각 갱신)
    protected void StartAttack()
    {
        lastAttackTime = Time.time;
        Attack(); // 실제 공격 로직은 자식 클래스 구현
    }

    // 근거리 타격 판정(전방 박스)
    protected void CheckHit()
    {
        Vector2 attackPos = (Vector2)transform.position + new Vector2(walkDirection * data.attackOffset, data.attackHeightOffset);
        Vector2 attackSize = new Vector2(data.attackRange, data.attackHeight);

        Collider2D hit = Physics2D.OverlapBox(attackPos, attackSize, 0, playerLayer);

        if (hit != null)
        {
            if (hit.TryGetComponent<PlayerController>(out var player))
            {
                player.TakeDamage(data.damage, (Vector2)transform.position);
            }
        }
    }

    // 플레이어 쪽으로 추적 이동(방향 갱신 지연 포함)
    protected void MoveTowardsPlayer()
    {
        if (playerTransform == null) return;

        if (Time.time >= nextUpdateTimer)
        {
            float realDir = Mathf.Sign(playerTransform.position.x - transform.position.x);
            delayedDirection = (int)realDir;
            nextUpdateTimer = Time.time + data.positionUpdateDelay;
        }

        walkDirection = delayedDirection;
        rb.linearVelocity = new Vector2(walkDirection * data.chaseSpeed, rb.linearVelocity.y);
        Flip(walkDirection);
    }

    // 물리 프레임 로직: 절벽 감지 + 이동 상태 처리
    protected virtual void FixedUpdate()
    {
        if (isKnockback) return;
        if (isIdle || isReacting || isAttacking)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // 피격 어그로 중에는 강제로 플레이어 참조를 유지 시도
        if (forcedAggroTimer > 0f && playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        // 1) 절벽 감지
        RaycastHit2D groundInfo = Physics2D.Raycast(groundCheck.position, Vector2.down, data.checkDistance, groundLayer);

        // 2) 거리/범위 체크
        float distanceToPlayer = playerTransform != null ? Vector2.Distance(transform.position, playerTransform.position) : float.MaxValue;
        bool isInDetectRange = distanceToPlayer <= data.detectRange; // 노란 원
        bool isInChaseRange = distanceToPlayer <= data.chaseRange;   // 빨간 원

        // 절벽 발견 시: 상태와 무관하게 즉시 정지 + 방향 전환 처리
        // (배회 중 예외로 절벽 감지를 건너뛰면 한 발 더 나가서 떨어질 수 있음)
        if (groundInfo.collider == null)
        {
            // 절벽 감지 중에는 배회 상태를 유지해 방향 강제 갱신을 막음
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
                        StopChasingForced();
                    }
                }
                else
                {
                    StopChasingForced();
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

        // 평지에서 추적 불가 누적 시간 관리
        if (!isInDetectRange && isInChaseRange && isChasing)
        {
            unreachableTimer += Time.fixedDeltaTime;
            if (unreachableTimer >= 5.0f) StopChasingForced();
        }
        else
        {
            unreachableTimer = 0f;
        }

        // 3) 이동 상태 분기
        if (isWandering)
        {
            rb.linearVelocity = new Vector2(walkDirection * data.moveSpeed, rb.linearVelocity.y);
            Flip(walkDirection);
        }
        else if (isChasing)
        {
            MoveTowardsPlayer();
        }
        else
        {
            ApplyMovement(data.moveSpeed);
        }
    }

    // 추적 강제 종료 + 반대 방향 배회 전환
    private void StopChasingForced()
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

    // 반응 상태 시작
    protected void StartReaction()
    {
        isReacting = true;
        isIdle = false;
        reactionTimer = data.reactionTime;
        rb.linearVelocity = Vector2.zero;
    }

    // Idle 상태 시작
    protected void StartIdle()
    {
        if (isChasing || isWandering) return;
        isIdle = true;
        hasLookedAround = false;
        idleTimer = data.idleDuration;
    }

    // 단순 이동 적용
    protected void ApplyMovement(float speed)
    {
        rb.linearVelocity = new Vector2(walkDirection * speed, rb.linearVelocity.y);
        Flip(walkDirection);
    }

    // 좌우 반전 + groundCheck 센서 위치 보정
    protected void Flip(float dir)
    {
        if (Mathf.Approximately(dir, 0)) return;
        spriteRenderer.flipX = (dir < 0);

        if (groundCheck != null)
        {
            groundCheck.localPosition = new Vector3(data.groundCheckOffset * Mathf.Sign(dir), groundCheck.localPosition.y, 0);
        }
    }

    // 자식 클래스에서 구현할 실제 공격 함수
    protected abstract void Attack();

    // 모든 상태 타이머 갱신
    protected void HandleTimers()
    {
        if (knockbackTimer > 0)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0)
            {
                // 넉백 종료 후 짧은 경직 부여
                isIdle = true;
                idleTimer = 0.3f;
            }
        }

        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        if (reactionTimer > 0)
        {
            reactionTimer -= Time.deltaTime;
            if (reactionTimer <= 0)
            {
                isReacting = false;
                isChasing = true;
            }
        }

        if (wanderTimer > 0)
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0) isWandering = false;
        }

        if (isIdle)
        {
            idleTimer -= Time.deltaTime;

            // 비전투 Idle 중 한 번 고개 돌리기
            if (!isChasing && !isWandering && idleTimer <= data.idleDuration / 2f && !hasLookedAround)
            {
                Flip(walkDirection * -1);
                hasLookedAround = true;
            }

            if (idleTimer <= 0) isIdle = false;
        }

        if (forcedAggroTimer > 0f)
            forcedAggroTimer -= Time.deltaTime;
    }

    // 피격 처리(체력 감소 + 넉백)
    public virtual void TakeDamage(float damage, float kPower)
    {
        currentHp -= damage;

        // 피격 시 2초간 강제 추격
        forcedAggroTimer = 2f;
        isChasing = true;
        isReacting = false;
        isWandering = false;

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        knockbackTimer = 0.2f;
        isIdle = false;
        rb.linearVelocity = Vector2.zero;

        float dir = playerTransform != null ? Mathf.Sign(transform.position.x - playerTransform.position.x) : -walkDirection;
        rb.linearVelocity = new Vector2(dir * kPower, 5f);

        if (currentHp <= 0) gameObject.SetActive(false);
    }

    // 디버그 시각화: 감지/추적/공격/절벽 감지 영역 표시
    protected virtual void OnDrawGizmos()
    {
        if (data == null) return;

        // detectRange (노란 원)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.detectRange);

        // chaseRange (빨간 원)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.chaseRange);

        // 근거리 공격 박스(하늘색)
        Gizmos.color = Color.cyan;
        Vector3 attackCenter = transform.position + new Vector3(walkDirection * data.attackOffset, data.attackHeightOffset, 0);
        Gizmos.DrawWireCube(attackCenter, new Vector3(data.attackRange, data.attackHeight, 0));

        // 절벽 감지 Ray(파란색)
        if (groundCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(groundCheck.position, Vector2.down * data.checkDistance);
        }
    }
}
