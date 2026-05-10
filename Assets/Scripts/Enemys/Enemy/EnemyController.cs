using UnityEngine;

// [적 AI - 공통 베이스]
// - 근거리/원거리 적이 공유하는 감지/이동/절벽 회피/타이머/피격 로직
// - 자식 클래스에서 Attack()을 구현해 실제 공격 동작을 정의
public abstract class EnemyController : MonoBehaviour
{
    // -------------------- 인스펙터 참조 --------------------
    [Header("데이터")]
    [Tooltip("적 행동/스탯 데이터(ScriptableObject)")]
    public EnemyData data;

    [Header("레이어")]
    [Tooltip("플레이어 탐지/공격 판정 레이어")]
    public LayerMask playerLayer;
    [Tooltip("지면 감지 레이어")]
    public LayerMask groundLayer;

    [Header("감지 위치")]
    [Tooltip("절벽 감지용 Ray 시작 위치")]
    public Transform groundCheck;
    [Tooltip("벽 감지용 기준점(선택)")]
    public Transform wallCheck;
    [Tooltip("벽 감지 거리")]
    public float wallCheckDistance = 0.2f;

    [Header("절벽 미련(확장용)")]
    [Tooltip("(확장용) 최대 망설임 횟수")]
    public int maxHesitation = 5;

    [Header("순찰 Idle 설정")]
    [Tooltip("순찰 중 랜덤 멈춤 사용 여부")]
    public bool enablePatrolIdle = true;
    [Tooltip("순찰 Idle 기본 최소 간격")]
    public float patrolIdleMinInterval = 3.0f;
    [Tooltip("순찰 Idle 기본 최대 간격")]
    public float patrolIdleMaxInterval = 6.0f;
    [Tooltip("예약 시점 도달 시 실제 Idle 진입 확률")]
    [Range(0f, 1f)] public float patrolIdleChance = 0.6f;

    [Tooltip("확률 실패 시 재시도 최소 간격")]
    public float patrolIdleRetryMinInterval = 1.0f;
    [Tooltip("확률 실패 시 재시도 최대 간격")]
    public float patrolIdleRetryMaxInterval = 2.0f;

    [Tooltip("순찰 Idle 최소 지속 시간")]
    public float patrolIdleDurationMin = 0.8f;
    [Tooltip("순찰 Idle 최대 지속 시간")]
    public float patrolIdleDurationMax = 1.4f;

    [Tooltip("2회 두리번(좌우 좌우: 왕복 2회) 확률")]
    [Range(0f, 1f)] public float secondLookChance = 0.35f;
    [Tooltip("첫 두리번 실행 시점(Idle 진행률)")]
    [Range(0f, 1f)] public float firstLookNormalizedTime = 0.33f;
    [Tooltip("두 번째 두리번 실행 시점(Idle 진행률)")]
    [Range(0f, 1f)] public float secondLookNormalizedTime = 0.66f;

    [Header("범위 시각화")]
    [Tooltip("최초 인식 범위(노란 원)")]
    [SerializeField] protected bool showDetectRangeGizmo = true;
    [Tooltip("추적 유지 범위(빨간 원)")]
    [SerializeField] protected bool showChaseRangeGizmo = true;
    [Tooltip("근거리 공격 박스(하늘색)")]
    [SerializeField] protected bool showMeleeAttackGizmo = true;
    [Tooltip("절벽 감지 Ray(파란색)")]
    [SerializeField] protected bool showGroundCheckGizmo = true;
    [Tooltip("벽 감지 Ray(흰색)")]
    [SerializeField] protected bool showWallCheckGizmo = true;

    [Header("실시간 상태")]
    [Tooltip("현재 체력(디버그 확인용)")]
    [SerializeField] protected float currentHp;

    // -------------------- 컴포넌트/대상 --------------------
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Transform playerTransform;

    // -------------------- 이동/방향 --------------------
    protected int walkDirection = 1;     // 이동 방향(-1:왼쪽, 1:오른쪽)
    protected int delayedDirection;      // 추적 지연 반영 방향
    protected int idleStartDirection;    // Idle 시작 시 바라보던 방향

    // -------------------- AI 상태 --------------------
    protected bool isChasing;            // 플레이어 추적 중 여부
    protected bool isReacting;           // 최초 감지 반응 지연 상태
    protected bool isIdle;               // 경직/대기 상태
    protected bool isWandering;          // 절벽 회피 등 강제 배회 상태
    protected bool edgeTurnLock;         // 절벽에서 1회만 방향 전환하기 위한 락

    // -------------------- 순찰 두리번 상태 --------------------
    protected bool isPatrolIdleActive;       // 현재 Idle이 순찰 두리번 Idle인지 여부
    protected bool hasLookedAround;          // (호환용) Idle 중 두리번 수행 여부
    protected int patrolLookTargetCount;     // 이번 Idle에서 수행할 두리번 왕복 횟수(1~2)
    protected int patrolLookDoneCount;       // 이번 Idle에서 수행한 방향 전환 횟수

    // -------------------- 타이머 --------------------
    protected float nextUpdateTimer;          // 추적 방향 갱신 시각
    protected float reactionTimer;            // 반응 지연 타이머
    protected float idleTimer;                // Idle 상태 타이머
    protected float wanderTimer;              // 배회 상태 타이머
    protected float attackTimer;              // 공격 상태 타이머(>0이면 공격 중)
    protected float knockbackTimer;           // 넉백 상태 타이머
    protected float hitStunTimer;             // 피격 경직 타이머
    protected float chaseLostSightGraceTimer; // 추격 중 시야 이탈 허용 타이머
    protected float lastAttackTime;           // 마지막 공격 시각
    protected float unreachableTimer;         // 추격 실패 누적 시간
    protected float forcedAggroTimer;         // 피격 후 강제 추격 유지 시간

    protected float nextPatrolIdleTime;       // 다음 순찰 Idle 시작 시각
    protected float patrolIdleTotalDuration;  // 이번 순찰 Idle 총 길이

    // -------------------- 벽 충돌 보정(넉백 관련) --------------------
    protected bool hasWallImpactPending;
    protected float pendingWallImpactDamage;
    protected float pendingWallStunDuration;

    private float wallCheckLocalOffsetX;

    // 읽기 전용 상태 프로퍼티
    protected bool isKnockback => knockbackTimer > 0;
    protected bool isAttacking => attackTimer > 0;
    protected bool isHitStunned => hitStunTimer > 0;

    // 컴포넌트 초기화 + 시작 방향 랜덤 설정
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        walkDirection = (Random.value < 0.5f) ? -1 : 1;
        delayedDirection = walkDirection;

        rb.mass = 10f;

        if (wallCheck != null)
            wallCheckLocalOffsetX = Mathf.Abs(wallCheck.localPosition.x);
    }

    // 시작 시 체력 초기화 + 플레이어/적-적 충돌 설정
    protected virtual void Start()
    {
        if (data != null) currentHp = data.maxHp;

        Collider2D enemyCol = GetComponent<Collider2D>();
        GameObject player = GameObject.FindWithTag("Player");

        if (player != null && enemyCol != null)
        {
            Collider2D playerCol = player.GetComponent<Collider2D>();
            if (playerCol != null)
                Physics2D.IgnoreCollision(enemyCol, playerCol, true);
        }

        int enemyLayerIndex = LayerMask.NameToLayer("Enemy");
        if (enemyLayerIndex != -1)
            Physics2D.IgnoreLayerCollision(enemyLayerIndex, enemyLayerIndex, true);

        ScheduleNextPatrolIdle();
    }

    // 프레임 로직: 상태 차단 → 플레이어 감지 → 타이머 처리
    protected virtual void Update()
    {
        if (isKnockback || isHitStunned)
        {
            HandleTimers();
            return;
        }

        // Idle 중에도 플레이어 감지는 수행(눈 마주치면 즉시 인식/반응)
        if (isIdle)
        {
            CheckPlayerDetection();
            HandleTimers();

            if (isIdle)
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            return;
        }

        if (isAttacking || isReacting)
        {
            HandleTimers();
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        CheckPlayerDetection();
        HandleTimers();
    }

    // 물리 프레임 로직: 절벽 감지 + 이동 상태 처리
    protected virtual void FixedUpdate()
    {
        if (isKnockback)
        {
            PreventKnockbackFallFromLedge();
            CheckKnockbackWallImpact();
            return;
        }

        if (isHitStunned)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

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
        bool isInDetectRange = distanceToPlayer <= data.detectRange;
        bool isInChaseRange = distanceToPlayer <= data.chaseRange;

        // 절벽 감지: 상태와 무관하게 즉시 정지 + 방향 전환 처리
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
            // 근거리 공격 가능 범위 안이면 쿨타임 중에도 제자리 유지
            if (playerTransform != null && IsPlayerInMeleeAttackRange())
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            else
                MoveTowardsPlayer();
        }
        else
        {
            ApplyMovement(data.moveSpeed);
        }
    }

    // 플레이어 탐지 + 공격 시도 판단(피격 어그로 중에는 시야/등뒤 무시)
    private void CheckPlayerDetection()
    {
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
                chaseLostSightGraceTimer = 0.35f;

                if (!isWandering && !edgeTurnLock)
                {
                    float dx = playerTransform.position.x - transform.position.x;
                    if (Mathf.Abs(dx) > 0.05f)
                    {
                        walkDirection = dx > 0f ? 1 : -1;
                        delayedDirection = walkDirection;
                        Flip(walkDirection);
                    }
                }

                if (IsPlayerInMeleeAttackRange())
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

                    if (Time.time > lastAttackTime + (1f / data.attackSpeed))
                        StartAttack();
                }

                return;
            }
        }

        // 추적 중에는 chaseRange, 평소에는 detectRange
        float currentRange = isChasing ? data.chaseRange : data.detectRange;
        Collider2D playerCol = Physics2D.OverlapCircle(transform.position, currentRange, playerLayer);

        if (playerCol != null)
        {
            bool inView = IsTargetInView(playerCol.transform);

            // 비추격 상태에서 시야 밖이면 인식하지 않음(추격 중에는 시야 잠깐 잃어도 유지)
            if (!inView && !isChasing)
            {
                playerTransform = null;
                return;
            }

            bool wasChasing = isChasing;
            playerTransform = playerCol.transform;
            isChasing = true;
            chaseLostSightGraceTimer = 0.35f;

            if (!wasChasing && !isReacting && reactionTimer <= 0f)
            {
                StartReaction();
                return;
            }

            if (IsPlayerInMeleeAttackRange())
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
            chaseLostSightGraceTimer = 0f;
        }
    }

    // 공격 시작 공통 처리(공격 시각 갱신 + 자식 구현 호출)
    protected void StartAttack()
    {
        lastAttackTime = Time.time;
        Attack();
    }

    // 자식 클래스에서 구현할 실제 공격 함수
    protected abstract void Attack();

    // 근거리 타격 판정(전방 박스)
    protected void CheckHit()
    {
        Vector2 attackPos = (Vector2)transform.position + new Vector2(walkDirection * data.attackOffset, data.attackHeightOffset);
        Vector2 attackSize = new Vector2(data.attackRange, data.attackHeight);

        Collider2D hit = Physics2D.OverlapBox(attackPos, attackSize, 0, playerLayer);

        if (hit != null && hit.TryGetComponent<PlayerController>(out var player))
            player.TakeDamage(data.damage, (Vector2)transform.position);
    }

    // 현재 바라보는 방향(시야각 포함) 안에 타겟이 있는지 확인
    protected bool IsTargetInView(Transform target)
    {
        if (target == null || data == null) return false;

        Vector2 toTarget = (target.position - transform.position);
        if (toTarget.sqrMagnitude <= 0.0001f) return true;

        Vector2 forward = new Vector2(walkDirection, 0f);
        float halfView = Mathf.Clamp(data.viewAngle * 0.5f, 0f, 180f);
        float angle = Vector2.Angle(forward, toTarget.normalized);

        return angle <= halfView;
    }

    // 근거리 공격 박스 기준으로 플레이어가 사거리 안에 있는지 확인
    protected bool IsPlayerInMeleeAttackRange()
    {
        if (playerTransform == null) return false;

        Vector2 attackPos = (Vector2)transform.position + new Vector2(walkDirection * data.attackOffset, data.attackHeightOffset);
        Vector2 attackSize = new Vector2(data.attackRange, data.attackHeight);

        Collider2D hit = Physics2D.OverlapBox(attackPos, attackSize, 0, playerLayer);
        return hit != null;
    }

    // 플레이어 쪽으로 추적 이동(방향 갱신 지연 포함)
    protected void MoveTowardsPlayer()
    {
        if (playerTransform == null) return;

        if (Time.time >= nextUpdateTimer)
        {
            float dx = playerTransform.position.x - transform.position.x;

            // 위/아래에 플레이어가 있어 X 차이가 거의 0이면 기존 방향 유지
            if (Mathf.Abs(dx) > 0.05f)
                delayedDirection = dx > 0f ? 1 : -1;

            nextUpdateTimer = Time.time + data.positionUpdateDelay;
        }

        if (delayedDirection == 0)
            delayedDirection = (walkDirection == 0) ? 1 : walkDirection;

        walkDirection = delayedDirection;
        rb.linearVelocity = new Vector2(walkDirection * data.chaseSpeed, rb.linearVelocity.y);
        Flip(walkDirection);
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

    // 반응 상태 시작(최초 감지 후 반응 지연)
    protected void StartReaction()
    {
        isReacting = true;
        isIdle = false;
        reactionTimer = data.reactionTime;
        rb.linearVelocity = Vector2.zero;
    }

    // 순찰 두리번 Idle 시작
    protected void StartIdle()
    {
        if (isChasing || isWandering) return;

        isIdle = true;
        isPatrolIdleActive = true;
        hasLookedAround = false;
        patrolLookDoneCount = 0;
        patrolLookTargetCount = (Random.value <= secondLookChance) ? 2 : 1;

        idleStartDirection = walkDirection;

        float minDuration = Mathf.Max(0.05f, patrolIdleDurationMin);
        float maxDuration = Mathf.Max(minDuration, patrolIdleDurationMax);
        patrolIdleTotalDuration = Random.Range(minDuration, maxDuration);
        idleTimer = patrolIdleTotalDuration;
    }

    // 단순 이동 적용
    protected void ApplyMovement(float speed)
    {
        rb.linearVelocity = new Vector2(walkDirection * speed, rb.linearVelocity.y);
        Flip(walkDirection);
    }

    // 좌우 반전 + groundCheck/wallCheck 센서 위치 보정
    protected void Flip(float dir)
    {
        if (Mathf.Approximately(dir, 0)) return;
        spriteRenderer.flipX = (dir < 0);

        if (groundCheck != null)
            groundCheck.localPosition = new Vector3(data.groundCheckOffset * Mathf.Sign(dir), groundCheck.localPosition.y, 0);

        if (wallCheck != null)
        {
            float wallX = wallCheckLocalOffsetX;
            if (Mathf.Approximately(wallX, 0f))
                wallX = Mathf.Abs(wallCheck.localPosition.x);

            wallCheck.localPosition = new Vector3(wallX * Mathf.Sign(dir), wallCheck.localPosition.y, wallCheck.localPosition.z);
        }
    }

    // 다음 순찰 Idle 시작 시각 예약(기본 간격)
    protected void ScheduleNextPatrolIdle()
    {
        float min = Mathf.Max(0.1f, patrolIdleMinInterval);
        float max = Mathf.Max(min, patrolIdleMaxInterval);
        nextPatrolIdleTime = Time.time + Random.Range(min, max);
    }

    // 순찰 Idle 확률 실패 시 빠른 재시도 예약
    protected void ScheduleNextPatrolIdleRetry()
    {
        float min = Mathf.Max(0.1f, patrolIdleRetryMinInterval);
        float max = Mathf.Max(min, patrolIdleRetryMaxInterval);
        nextPatrolIdleTime = Time.time + Random.Range(min, max);
    }

    // 모든 상태 타이머 갱신 + 순찰 Idle 진입 체크
    protected void HandleTimers()
    {
        if (knockbackTimer > 0)
        {
            knockbackTimer -= Time.deltaTime;
            if (knockbackTimer <= 0)
            {
                // 넉백 종료 후 짧은 경직 부여(순찰 Idle 아님)
                isIdle = true;
                isPatrolIdleActive = false;
                idleTimer = 0.3f;
            }
        }

        if (hitStunTimer > 0)
        {
            hitStunTimer -= Time.deltaTime;
            if (hitStunTimer < 0f) hitStunTimer = 0f;
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

        if (isIdle) UpdatePatrolIdleLook();

        if (forcedAggroTimer > 0f)
            forcedAggroTimer -= Time.deltaTime;

        // 순찰 중 일정 확률로 두리번 Idle 진입
        if (enablePatrolIdle
            && !isIdle
            && !isChasing
            && !isWandering
            && !isReacting
            && !isAttacking
            && !isKnockback
            && forcedAggroTimer <= 0f
            && Time.time >= nextPatrolIdleTime)
        {
            if (Random.value <= Mathf.Clamp01(patrolIdleChance))
                StartIdle();
            else
                ScheduleNextPatrolIdleRetry();
        }
    }

    // 순찰 Idle 동안 진행률에 따라 두리번(좌우 반전) 수행
    private void UpdatePatrolIdleLook()
    {
        idleTimer -= Time.deltaTime;

        if (isPatrolIdleActive)
        {
            float total = Mathf.Max(0.001f, patrolIdleTotalDuration);
            float progress = Mathf.Clamp01(1f - (idleTimer / total));

            bool canLook = !isChasing && !isWandering && !isReacting && !isAttacking && !isKnockback;

            float firstT = Mathf.Clamp01(firstLookNormalizedTime);
            float secondT = Mathf.Clamp01(secondLookNormalizedTime);
            if (secondT <= firstT) secondT = Mathf.Min(0.99f, firstT + 0.05f);

            // 왕복 1회 = 방향 전환 2번 / 왕복 2회 = 방향 전환 4번
            int targetFlipCount = patrolLookTargetCount * 2;
            float thirdT = Mathf.Lerp(secondT, 1f, 0.45f);
            float fourthT = Mathf.Lerp(secondT, 1f, 0.85f);

            if (canLook && patrolLookDoneCount < 1 && progress >= firstT) DoPatrolLookFlip();
            if (canLook && patrolLookDoneCount < 2 && progress >= secondT) DoPatrolLookFlip();
            if (canLook && targetFlipCount >= 4 && patrolLookDoneCount < 3 && progress >= thirdT) DoPatrolLookFlip();
            if (canLook && targetFlipCount >= 4 && patrolLookDoneCount < 4 && progress >= fourthT) DoPatrolLookFlip();
        }

        if (idleTimer <= 0)
        {
            isIdle = false;

            // 순찰 두리번 Idle은 종료 시 원래 보던 방향으로 복귀
            if (isPatrolIdleActive && !isChasing && !isWandering && !isReacting && !isAttacking && !isKnockback)
            {
                walkDirection = idleStartDirection;
                delayedDirection = walkDirection;
                Flip(walkDirection);
            }

            isPatrolIdleActive = false;
            ScheduleNextPatrolIdle();
        }
    }

    // 두리번 1회 수행(방향 반전 + 카운트)
    private void DoPatrolLookFlip()
    {
        walkDirection *= -1;
        delayedDirection = walkDirection;
        Flip(walkDirection);
        patrolLookDoneCount++;
        hasLookedAround = true;
    }

    // 피격 처리(체력 감소 + 넉백 + 경직 + 벽 충돌 추가 효과 예약)
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

        // 뒤에서 맞아도 즉시 플레이어를 인지하도록 방향 갱신
        if (playerTransform != null)
        {
            float dx = playerTransform.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.05f)
            {
                walkDirection = dx > 0f ? 1 : -1;
                delayedDirection = walkDirection;
                Flip(walkDirection);
            }
        }

        float resistance = (data != null) ? Mathf.Clamp01(data.knockbackResistance) : 0f;
        float effectiveKnockback = Mathf.Max(0f, kPower * (1f - resistance));

        knockbackTimer = Mathf.Max(0.05f, 0.12f + effectiveKnockback * 0.02f);
        hitStunTimer = Mathf.Max(hitStunTimer, data != null ? data.hitStunDuration : 0.15f);
        isIdle = false;
        rb.linearVelocity = Vector2.zero;

        float dir = playerTransform != null ? Mathf.Sign(transform.position.x - playerTransform.position.x) : -walkDirection;
        float knockbackY = Mathf.Lerp(2.5f, 5f, 1f - resistance);
        rb.linearVelocity = new Vector2(dir * effectiveKnockback, knockbackY);

        hasWallImpactPending = true;
        pendingWallImpactDamage = data != null ? data.wallBonusDamage : 0f;
        pendingWallStunDuration = data != null ? data.wallStunDuration : 0f;

        if (currentHp <= 0) gameObject.SetActive(false);
    }

    // 넉백 중 절벽으로 밀려 떨어지는 상황 방지
    protected virtual void PreventKnockbackFallFromLedge()
    {
        if (groundCheck == null || data == null) return;
        if (Mathf.Approximately(rb.linearVelocity.x, 0f)) return;

        float movingDir = Mathf.Sign(rb.linearVelocity.x);
        Vector2 rayOrigin = (Vector2)transform.position + new Vector2(data.groundCheckOffset * movingDir, groundCheck.localPosition.y);
        RaycastHit2D groundInfo = Physics2D.Raycast(rayOrigin, Vector2.down, data.checkDistance, groundLayer);

        if (groundInfo.collider == null)
        {
            // 수직 넉백은 유지하고 수평만 차단해 낙사 방지
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            walkDirection = (int)-movingDir;
            delayedDirection = walkDirection;
            Flip(walkDirection);
        }
    }

    // 넉백 중 벽 충돌 시 추가 피해/스턴 처리
    protected virtual void CheckKnockbackWallImpact()
    {
        if (!hasWallImpactPending) return;
        if (wallCheck == null) return;

        int wallMask = groundLayer;
        float moveDir = !Mathf.Approximately(rb.linearVelocity.x, 0f) ? Mathf.Sign(rb.linearVelocity.x) : walkDirection;
        RaycastHit2D hit = Physics2D.Raycast(wallCheck.position, Vector2.right * moveDir, wallCheckDistance, wallMask);
        if (hit.collider == null) return;

        hasWallImpactPending = false;
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        if (pendingWallImpactDamage > 0f)
        {
            currentHp -= pendingWallImpactDamage;
            if (currentHp <= 0f)
            {
                gameObject.SetActive(false);
                return;
            }
        }

        if (pendingWallStunDuration > 0f)
            hitStunTimer = Mathf.Max(hitStunTimer, pendingWallStunDuration);
    }

    // 디버그 시각화: 인스펙터의 [범위 시각화] 토글로 각 항목 개별 제어
    protected virtual void OnDrawGizmos()
    {
        if (data == null) return;

        if (showDetectRangeGizmo)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, data.detectRange);
        }

        if (showChaseRangeGizmo)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, data.chaseRange);
        }

        if (showMeleeAttackGizmo)
        {
            Gizmos.color = Color.cyan;
            Vector3 attackCenter = transform.position + new Vector3(walkDirection * data.attackOffset, data.attackHeightOffset, 0);
            Gizmos.DrawWireCube(attackCenter, new Vector3(data.attackRange, data.attackHeight, 0));
        }

        if (showGroundCheckGizmo && groundCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(groundCheck.position, 0.05f);
            Gizmos.DrawRay(groundCheck.position, Vector2.down * data.checkDistance);
        }

        if (showWallCheckGizmo && wallCheck != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(wallCheck.position, 0.05f);

            float dir = (rb != null && !Mathf.Approximately(rb.linearVelocity.x, 0f)) ? Mathf.Sign(rb.linearVelocity.x) : walkDirection;
            if (Mathf.Approximately(dir, 0f)) dir = 1f;

            Gizmos.DrawRay(wallCheck.position, Vector2.right * dir * wallCheckDistance);
        }
    }
}
