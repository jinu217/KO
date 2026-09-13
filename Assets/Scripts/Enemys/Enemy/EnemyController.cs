using UnityEngine;

// [적 AI - 공통 베이스]
// - 근거리/원거리 적이 공유하는 감지/이동/절벽 회피/타이머/피격 로직
// - 자식 클래스에서 Attack()을 구현해 실제 공격 동작을 정의
public abstract class EnemyController : MonoBehaviour, IDamageable
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
    [Tooltip("파괴 가능 오브젝트 레이어 (이 레이어가 앞에 있으면 뒤돌아감)")]
    public LayerMask breakableLayer;

    [Header("감지 위치")]
    [Tooltip("절벽 감지용 시작 위치")]
    public Transform groundCheck;
    [Tooltip("벽 감지용 기준점(선택)")]
    public Transform wallCheck;
    [Tooltip("벽 감지 거리")]
    public float wallCheckDistance = 0.2f;
    [Tooltip("넉백 방향 지면 감지 센서 (몬스터 뒤쪽 자식 Transform)\nground/ground2 감지 시 X넉백, 미감지 시 Y넉백")]
    public Transform knockbackCheck;

    [Header("넉백 설정")]
    [Tooltip("낭떠러지 감지 시 Y축 넉백 속도 (뒤에 낭떠러지 있을 때만 적용)")]
    [SerializeField] protected float knockbackCliffYPower = 3f;

    [Header("절벽 미련(확장용)")]
    [Tooltip("(확장용) 최대 망설임 횟수")]
    public int maxHesitation = 5;

    [Header("절벽 감지(BoxCast)")]
    [Tooltip("전방 발판 감지 박스 크기(X=너비, Y=높이)")]
    [SerializeField] protected Vector2 ledgeProbeSize = new Vector2(0.18f, 0.12f);
    [Tooltip("Wander 종료 직후 stuckTimer 누적을 막는 유예 시간(초)\n경사 정상부 물리 걸림으로 인한 오판 방지. 0.3~0.6 권장")]
    [SerializeField] protected float wanderEndGraceTime = 0.5f;
    [Tooltip("STUCK_RECOVERY 발동까지 제자리 허용 시간(초). 경사가 있는 맵은 0.4~0.6 권장")]
    [SerializeField] protected float stuckRecoveryTime = 0.4f;
    [Tooltip("경사→평지 미세 단차 탈출 시 위로 직접 이동할 거리\n경사 top Y와 평지 Y의 차이보다 크게 설정. 기본 0.1 권장")]
    [SerializeField] protected float microStepEscapeHeight = 0.1f;
    [Tooltip("Wander 종료 직후 STUCK_RECOVERY가 방향을 바꾸지 않고 현재 방향을 유지하는 시간(초)\n경사 내려온 방향으로 재추락하는 문제 방지")]
    [SerializeField] protected float postWanderStuckIgnoreTime = 0.8f;

    [Header("순찰 Idle 설정")]
    [Tooltip("순찰 중 랜덤 멈춤 사용 여부")]
    public bool enablePatrolIdle = true;
    [Tooltip("순찰 Idle는 지면에 안정적으로 붙어있을 때만 시작")]
    public bool patrolIdleOnlyWhenGrounded = true;
    [Tooltip("순찰 Idle 기본 최소 간격")]
    public float patrolIdleMinInterval = 3.0f;
    [Tooltip("순찰 Idle 기본 최대 간격")]
    public float patrolIdleMaxInterval = 6.0f;
    [Tooltip("방향 전환 직후 순찰 Idle 진입을 막는 시간")]
    public float patrolIdleBlockAfterTurn = 1.0f;
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

    [Header("순찰 정지 설정 (두리번 없이 제자리 멈춤)")]
    [Tooltip("순찰 중 랜덤 정지 기능 사용 여부")]
    public bool enablePatrolPause = true;
    [Tooltip("순찰 정지 최소 간격(초)")]
    public float patrolPauseMinInterval = 4.0f;
    [Tooltip("순찰 정지 최대 간격(초)")]
    public float patrolPauseMaxInterval = 8.0f;
    [Tooltip("정지 지속 최소 시간(초)")]
    public float patrolPauseDurationMin = 2.0f;
    [Tooltip("정지 지속 최대 시간(초)")]
    public float patrolPauseDurationMax = 3.0f;
    [Tooltip("예약 시점 도달 시 실제 정지 진입 확률")]
    [Range(0f, 1f)] public float patrolPauseChance = 0.5f;

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
    [Tooltip("넉백 지면 감지 센서(주황색)")]
    [SerializeField] protected bool showKnockbackCheckGizmo = true;

    [Header("넉백 감지 범위")]
    [Tooltip("넉백 지면 감지 박스 크기(X=너비, Y=높이)")]
    [SerializeField] protected Vector2 knockbackProbeSize = new Vector2(0.3f, 0.3f);


    [Header("실시간 상태")]
    [Tooltip("현재 체력(디버그 확인용)")]
    [SerializeField] protected float currentHp;

    // -------------------- 컴포넌트/대상 --------------------
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    private HitFlashEffect hitFlash;   // 피격 깜빡임 효과 컴포넌트
    protected Transform playerTransform;
    protected Collider2D bodyCollider;
    protected Animator animator;

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
    protected bool isPatrolPausing;      // 순찰 정지(두리번 없이 멈춤) 중 여부

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
    protected float patrolPauseTimer;         // 순찰 정지 잔여 시간
    protected float nextPatrolPauseTime;      // 다음 순찰 정지 예약 시각
    protected float stuckTimer;               // 순찰 중 제자리 정지 누적 시간
    protected Vector2 patrolLastPos;          // 순찰 stuck 판단용 이전 위치
    private float wanderEndGraceTimer;        // wander 종료 후 stuckTimer 유예 타이머
    private float postWanderStuckIgnoreTimer; // wander 종료 후 STUCK_RECOVERY 방향전환 금지 타이머

    // -------------------- 벽 충돌 보정(넉백 관련) --------------------
    protected bool hasWallImpactPending;
    protected float pendingWallImpactDamage;
    protected float pendingWallStunDuration;

    private float wallCheckLocalOffsetX;
    private float knockbackCheckLocalOffsetX; // Awake 시 저장한 knockbackCheck X 절댓값
    private float pendingKnockbackXPower = 5f; // 다음 피격에 적용할 X 넉백 파워 (PlayerController에서 주입)
    private float originalGravity;  // 초기 gravityScale 저장 (경사 정지 시 복구용)
    private int lastFacingDirection = 1;

    // 읽기 전용 상태 프로퍼티
    protected bool isKnockback => knockbackTimer > 0;
    protected bool isAttacking => attackTimer > 0;
    protected bool isHitStunned => hitStunTimer > 0;

    // 컴포넌트 초기화 + 시작 방향 랜덤 설정
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        hitFlash = GetComponent<HitFlashEffect>();
        bodyCollider = GetComponent<Collider2D>();
        animator = GetComponentInChildren<Animator>();

        walkDirection = (Random.value < 0.5f) ? -1 : 1;
        delayedDirection = walkDirection;
        lastFacingDirection = walkDirection;

        rb.mass = 10f;
        originalGravity = rb.gravityScale;
        patrolLastPos = rb.position;

        if (wallCheck != null)
            wallCheckLocalOffsetX = Mathf.Abs(wallCheck.localPosition.x);
        if (knockbackCheck != null)
            knockbackCheckLocalOffsetX = Mathf.Abs(knockbackCheck.localPosition.x);
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
        ScheduleNextPatrolPause();
    }

    // 프레임 로직: 상태 차단 → 플레이어 감지 → 타이머 처리
    protected virtual void Update()
    {
        UpdateAnimatorParameters();

        if (isKnockback || isHitStunned)
        {
            HandleTimers();
            return;
        }

        // Idle 중에도 플레이어 감지는 수행(눈 마주치면 즉시 인식/반응)
        if (isPatrolPausing)
        {
            CheckPlayerDetection();
            HandleTimers();
            return;
        }

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

    // Animator 파라미터 갱신
    protected virtual void UpdateAnimatorParameters()
    {
        if (animator == null || rb == null) return;

        animator.SetFloat(
            "MoveSpeed",
            Mathf.Abs(rb.linearVelocity.x)
        );
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

        // 경사 미끄러짐 방지: 모든 early return 이전에 적용
        // "정지 의도" 상태(pause/idle/react/attack)이거나 velocity.x가 거의 0이면 중력 차단
        // → 정지 진입 첫 프레임에도 즉시 중력을 끊어 순간 미끄러짐 방지
        {
            bool intendingToStop = isPatrolPausing || isIdle || isReacting || isAttacking;
            bool groundedAndStill = IsGroundedNow()
                && (intendingToStop || Mathf.Abs(rb.linearVelocity.x) <= 0.05f)
                && !isKnockback;
            rb.gravityScale = groundedAndStill ? 0f : originalGravity;
        }

        if (isPatrolPausing || isIdle || isReacting || isAttacking)
        {
            // 정지: 경사 법선 방향으로 X=0 투영 → 미끄러짐 없음
            ApplySlopeAwareVelocity(0f);
            return;
        }

        // Y 속도는 ApplySlopeAwareVelocity에서 경사 방향으로 처리

        // 피격 어그로 중에는 강제로 플레이어 참조를 유지 시도
        if (forcedAggroTimer > 0f && playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        // 1) 절벽/내리막 감지 (접지 상태 + 방향 전환 유예 종료 후 검사)
        bool isGrounded = IsGroundedNow();
        bool hasGroundAhead = !isGrounded || HasGroundAhead();


        // 2) 거리/범위 체크
        float distanceToPlayer = playerTransform != null ? Vector2.Distance(transform.position, playerTransform.position) : float.MaxValue;
        bool isInDetectRange = distanceToPlayer <= data.detectRange;
        bool isInChaseRange = distanceToPlayer <= data.chaseRange;

        // 절벽 감지: 상태와 무관하게 즉시 정지 + 방향 전환 처리
        if (!hasGroundAhead)
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
                ApplySlopeAwareVelocity(walkDirection * GetEffectiveMoveSpeed(data.moveSpeed));
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

        // 3) 벽 감지: ground/ground2 또는 Breakable 오브젝트가 앞에 있으면 뒤돌아감
        if (wallCheck != null)
        {
            float wallDir = walkDirection == 0 ? 1f : Mathf.Sign(walkDirection);

            // 지형 벽 감지
            RaycastHit2D wallHit = Physics2D.Raycast(
                wallCheck.position, Vector2.right * wallDir, wallCheckDistance, groundLayer);

            // Breakable 오브젝트 감지 (breakableLayer가 지정된 경우)
            RaycastHit2D breakableHit = breakableLayer != 0
                ? Physics2D.Raycast(wallCheck.position, Vector2.right * wallDir, wallCheckDistance, breakableLayer)
                : default;

            if (wallHit.collider != null || breakableHit.collider != null)
            {
                walkDirection *= -1;
                delayedDirection = walkDirection;
                Flip(walkDirection);
                ApplySlopeAwareVelocity(0f);
                return;
            }
        }

        // 4) 이동 상태 분기
        if (isWandering)
        {
            ApplySlopeAwareVelocity(walkDirection * GetEffectiveMoveSpeed(data.moveSpeed));
            Flip(walkDirection);
            stuckTimer = 0f;
            patrolLastPos = rb.position;
        }
        else if (isChasing)
        {
            // 근거리 공격 가능 범위 안이면 쿨타임 중에도 제자리 유지
            if (playerTransform != null && IsPlayerInMeleeAttackRange())
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            else
                MoveTowardsPlayer();

            stuckTimer = 0f;
            patrolLastPos = rb.position;
        }
        else
        {
            float moveSpeed = GetEffectiveMoveSpeed(data.moveSpeed);
            Vector2 currentPos = rb.position;
            float movedDist = Vector2.Distance(currentPos, patrolLastPos);

            ApplyMovement(moveSpeed);

            // 실제 위치 변화 기반으로 stuck 판단 (velocity set 직후 값 의존 X)
            // wanderEndGraceTimer 중엔 누적 금지 (경사 정상부 물리 걸림 오판 방지)
            if (wanderEndGraceTimer > 0f)
            {
                wanderEndGraceTimer -= Time.fixedDeltaTime;
                stuckTimer = 0f;
            }
            else if (movedDist <= 0.0025f)
                stuckTimer += Time.fixedDeltaTime;
            else
                stuckTimer = 0f;

            patrolLastPos = currentPos;

            // postWanderStuckIgnoreTimer 감소
            if (postWanderStuckIgnoreTimer > 0f)
                postWanderStuckIgnoreTimer -= Time.fixedDeltaTime;

            if (stuckTimer >= stuckRecoveryTime)
            {
                // 경사→평지 전환부 미세 단차(약 0.04유닛)에 박스 콜라이더가 맞닿아
                // linearVelocity/AddForce가 반력으로 상쇄됨 → rb.position 직접 조정
                // AddForce는 Physics2D가 완전히 상쇄하므로 효과 없음
                rb.position += new Vector2(0f, microStepEscapeHeight);
                rb.linearVelocity = new Vector2(walkDirection * moveSpeed, rb.linearVelocity.y);

                stuckTimer = 0f;
                patrolLastPos = rb.position;
            }
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

        if (animator != null)
            animator.SetTrigger("Attack");

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

        if (hit != null)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            target?.TakeDamage(data.damage, (Vector2)transform.position);
        }
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
        rb.linearVelocity = new Vector2(walkDirection * GetEffectiveMoveSpeed(data.chaseSpeed), rb.linearVelocity.y);
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

    // 전방 지면 존재 여부 확인 (BoxCast 기반)
    // 절벽 감지: groundCheck 위치 OverlapBox — 지면 조금이라도 닿으면 true
    protected bool HasGroundAhead()
    {
        if (groundCheck == null) return true;
        Vector2 size = new Vector2(Mathf.Max(0.05f, ledgeProbeSize.x), Mathf.Max(0.05f, ledgeProbeSize.y));
        return Physics2D.OverlapBox(groundCheck.position, size, 0f, groundLayer) != null;
    }

    // 현재 지면 접지 여부(발밑) 확인
    protected bool IsGroundedNow()
    {
        if (bodyCollider == null) return true;

        Bounds b = bodyCollider.bounds;
        float probeH = 0.08f;
        Vector2 size = new Vector2(Mathf.Max(0.06f, b.size.x * 0.7f), probeH);
        Vector2 center = new Vector2(b.center.x, b.min.y - (probeH * 0.5f) - 0.02f);

        return Physics2D.OverlapBox(center, size, 0f, groundLayer) != null;
    }

    // 단순 이동 적용
    // 몸무게 기반 이동속도 보정(weight=1 기준, 클수록 느림, 작을수록 빠름)
    protected float GetEffectiveMoveSpeed(float baseSpeed)
    {
        float weight = (data != null) ? Mathf.Max(data.weight, 0.1f) : 1f;
        return baseSpeed / Mathf.Sqrt(weight);
    }

    // 경사면 법선 기반 속도 투영:
    // - 지면 위: Raycast로 법선 감지 → xSpeed를 경사 방향으로 투영
    //   → 내리막에서 콜라이더가 붙어서 이동 / 오르막도 동일 slope 속도 유지
    //   → xSpeed=0 이면 투영 결과 0 → 정지 시 미끄러짐 없음
    // - 공중: Y 보존 (자연스러운 낙하)
    protected void ApplySlopeAwareVelocity(float xSpeed)
    {
        if (!IsGroundedNow())
        {
            rb.linearVelocity = new Vector2(xSpeed, rb.linearVelocity.y);
            return;
        }

        // Raycast 시작점: 콜라이더 바닥(bodyCollider.bounds.min.y)에서 조금 위
        // rb.position(중앙)에서 쏘면 캡슐 콜라이더 높이만큼 짧아 경사면 미감지
        Vector2 origin;
        float probeLen;
        if (bodyCollider != null)
        {
            origin = new Vector2(rb.position.x, bodyCollider.bounds.min.y + 0.05f);
            probeLen = 0.35f; // 바닥에서 0.35f 아래까지 탐색
        }
        else
        {
            origin = (Vector2)rb.position;
            probeLen = 1.0f;
        }

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.down, probeLen, groundLayer);

        if (hit.collider != null)
        {
            Vector2 normal = hit.normal;

            // 경사 법선 90도 회전 → 경사면 접선(tangent)
            Vector2 tangent = new Vector2(normal.y, -normal.x);
            if (tangent.x < 0f) tangent = -tangent; // +X 방향 보장

            float speed = Mathf.Abs(xSpeed);
            Vector2 dir = xSpeed >= 0f ? tangent : -tangent;
            rb.linearVelocity = dir * speed;
        }
        else
        {
            // 경사면 미감지(평지 이음새 등): Y=0
            rb.linearVelocity = new Vector2(xSpeed, 0f);
        }
    }

    protected void ApplyMovement(float speed)
    {
        ApplySlopeAwareVelocity(walkDirection * speed);
        Flip(walkDirection);
    }

    // 좌우 반전 + groundCheck/wallCheck 센서 위치 보정
    protected void Flip(float dir)
    {
        if (Mathf.Approximately(dir, 0)) return;

        int facing = dir < 0f ? -1 : 1;
        if (facing != lastFacingDirection)
        {
            edgeTurnLock = false;
            nextPatrolIdleTime = Mathf.Max(nextPatrolIdleTime, Time.time + Mathf.Max(0f, patrolIdleBlockAfterTurn));
            nextPatrolPauseTime = Mathf.Max(nextPatrolPauseTime, Time.time + Mathf.Max(0f, patrolIdleBlockAfterTurn));
            lastFacingDirection = facing;
        }

        spriteRenderer.flipX = (dir < 0);

        // groundCheck X: 인스펙터에서 설정한 현재 localPosition.x 절댓값으로 방향 미러링
        // 캐싱 없이 매 Flip마다 현재값 직접 읽음 → 씬에서 위치 변경 즉시 반영
        if (groundCheck != null)
            groundCheck.localPosition = new Vector3(
                Mathf.Abs(groundCheck.localPosition.x) * Mathf.Sign(dir),
                groundCheck.localPosition.y,
                groundCheck.localPosition.z);

        if (wallCheck != null)
        {
            float wallX = wallCheckLocalOffsetX;
            if (Mathf.Approximately(wallX, 0f))
                wallX = Mathf.Abs(wallCheck.localPosition.x);

            wallCheck.localPosition = new Vector3(wallX * Mathf.Sign(dir), wallCheck.localPosition.y, wallCheck.localPosition.z);
        }

        // knockbackCheck: 진행 방향 반대(뒤쪽)에 배치
        if (knockbackCheck != null)
        {
            float kbX = knockbackCheckLocalOffsetX > 0f
                ? knockbackCheckLocalOffsetX
                : Mathf.Abs(knockbackCheck.localPosition.x);
            knockbackCheck.localPosition = new Vector3(-kbX * Mathf.Sign(dir), knockbackCheck.localPosition.y, knockbackCheck.localPosition.z);
        }
    }

    // 다음 순찰 Idle 시작 시각 예약(기본 간격)
    // 순찰 정지(두리번 없이 멈춤) 시작
    protected void StartPatrolPause()
    {
        if (isChasing || isWandering || isIdle) return;

        isPatrolPausing = true;
        patrolPauseTimer = Random.Range(
            Mathf.Max(0.1f, patrolPauseDurationMin),
            Mathf.Max(patrolPauseDurationMin, patrolPauseDurationMax));
    }

    // 다음 순찰 정지 시각 예약
    protected void ScheduleNextPatrolPause()
    {
        float min = Mathf.Max(0.1f, patrolPauseMinInterval);
        float max = Mathf.Max(min, patrolPauseMaxInterval);
        nextPatrolPauseTime = Time.time + Random.Range(min, max);
    }

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
            if (wanderTimer <= 0)
            {
                isWandering = false;
                // wander 종료 후 유예 타이머 시작 (경사 정상부 물리 걸림 오판 방지)
                wanderEndGraceTimer = wanderEndGraceTime;
                postWanderStuckIgnoreTimer = postWanderStuckIgnoreTime;
                stuckTimer = 0f;
                patrolLastPos = rb != null ? rb.position : (Vector2)transform.position;
                ScheduleNextPatrolIdle();
            }
        }

        if (isIdle) UpdatePatrolIdleLook();

        if (forcedAggroTimer > 0f)
            forcedAggroTimer -= Time.deltaTime;

        // 순찰 정지 타이머 갱신
        if (isPatrolPausing)
        {
            patrolPauseTimer -= Time.deltaTime;
            if (patrolPauseTimer <= 0f)
            {
                isPatrolPausing = false;
                ScheduleNextPatrolPause();
                // 정지 끝난 뒤 두리번이 즉시 발동되지 않도록 Idle 타이머도 재예약
                ScheduleNextPatrolIdle();
            }
        }

        // ── 순찰 행동(두리번 / 정지) 공통 진입 조건 ──────────────────────
        bool canStartByGround = !patrolIdleOnlyWhenGrounded || IsGroundedNow();
        bool canStartByVelocity = rb == null || Mathf.Abs(rb.linearVelocity.x) > 0.05f;
        bool baseCondition = canStartByGround
                                && canStartByVelocity
                                && !isIdle
                                && !isPatrolPausing
                                && !isChasing
                                && !isWandering
                                && !isReacting
                                && !isAttacking
                                && !isKnockback
                                && forcedAggroTimer <= 0f;

        // 두리번
        if (enablePatrolIdle && baseCondition && Time.time >= nextPatrolIdleTime)
        {
            if (Random.value <= Mathf.Clamp01(patrolIdleChance))
                StartIdle();
            else
                ScheduleNextPatrolIdleRetry();
        }

        // 정지 (두리번과 동일한 baseCondition 적용)
        if (enablePatrolPause && baseCondition && Time.time >= nextPatrolPauseTime)
        {
            if (Random.value <= Mathf.Clamp01(patrolPauseChance))
                StartPatrolPause();
            else
                ScheduleNextPatrolPause();
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
            ScheduleNextPatrolPause(); // 두리번 중 만료된 정지 타이머 재예약
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
    // 다음 TakeDamage에서 사용할 X 넉백 파워 주입 (PlayerController.GetWeaponKnockbackPower()로 호출)
    public void SetNextKnockbackPower(float power) => pendingKnockbackXPower = power;

    // IDamageable 구현
    // applyKnockback: 넉백 적용 여부 / applyIFrame: 적에게는 미적용(무적 없음)
    public virtual void TakeDamage(float damage, Vector2 sourcePos, bool applyKnockback = true, bool applyIFrame = false)
    {
        TutorialTrainingDummy tutorialDummy = GetComponent<TutorialTrainingDummy>();
        if (TutorialManager.Instance != null && !TutorialManager.Instance.CanDamageTarget(tutorialDummy))
            return;

        currentHp -= damage;
        // 데미지 텍스트 생성
        DamageTextManager.Instance?.ShowDamage(Mathf.RoundToInt(damage), transform.position + Vector3.up * 1.5f);

        hitFlash?.Play();

        forcedAggroTimer = 2f;
        isChasing = true;
        isReacting = false;
        isWandering = false;
        isPatrolPausing = false;

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) playerTransform = playerObj.transform;
        }

        // 피격 방향으로 즉시 바라봄
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

        hitStunTimer = Mathf.Max(hitStunTimer, data != null ? data.hitStunDuration : 0.15f);
        isIdle = false;
        rb.gravityScale = originalGravity;  // 경사 정지 중 gravityScale=0 상태에서 피격 시 중력 복구
        rb.linearVelocity = Vector2.zero;     // 기존 속도 초기화 후 넉백 적용

        if (applyKnockback)
        {
            float weight = (data != null) ? Mathf.Max(data.weight, 0.1f) : 1f;
            float kDir = sourcePos.x < transform.position.x ? 1f : -1f;

            // knockbackCheck 센서로 뒤쪽 지면 확인
            // 감지됨 → 뒤에 발판 있음 → X축 넉백
            // 미감지 → 뒤가 낭떠러지 → Y축 넉백
            Vector2 kbSize = new Vector2(Mathf.Max(0.05f, knockbackProbeSize.x), Mathf.Max(0.05f, knockbackProbeSize.y));
            bool groundBehind = knockbackCheck != null &&
                Physics2D.OverlapBox(knockbackCheck.position, kbSize, 0f, groundLayer) != null;

            if (groundBehind)
            {
                float effectiveX = Mathf.Max(0f, pendingKnockbackXPower / weight);
                knockbackTimer = Mathf.Max(0.05f, 0.1f + effectiveX * 0.015f);
                rb.linearVelocity = new Vector2(kDir * effectiveX, 0f);
                pendingKnockbackXPower = 5f; // 사용 후 기본값으로 초기화
            }
            else
            {
                float effectiveY = Mathf.Max(0f, knockbackCliffYPower / weight);
                knockbackTimer = Mathf.Max(0.05f, 0.1f + effectiveY * 0.015f);
                rb.linearVelocity = new Vector2(0f, effectiveY);
            }
        }

        hasWallImpactPending = true;
        pendingWallImpactDamage = data != null ? data.wallBonusDamage : 0f;
        pendingWallStunDuration = data != null ? data.wallStunDuration : 0f;

        if (currentHp <= 0)
        {
            tutorialDummy?.NotifyDefeated();
            // 일반 적 처치 횟수 기록
            if (tutorialDummy == null && SaveData.Instance != null)
            {
                SaveData.Instance.run.killCount++;
            }
            gameObject.SetActive(false);
        }
    }

    // 넉백 방향에 낭떠러지가 있는지 확인 (groundCheck를 해당 방향으로 임시 이동해 검사)

    // 넉백 중 X축 이동 시 knockbackCheck(뒤쪽)로 지면 재확인
    // 지면 없어지면 X넉백 즉시 중단
    protected virtual void PreventKnockbackFallFromLedge()
    {
        if (Mathf.Approximately(rb.linearVelocity.x, 0f)) return;

        if (knockbackCheck == null) return;

        Vector2 kbSz = new Vector2(Mathf.Max(0.05f, knockbackProbeSize.x), Mathf.Max(0.05f, knockbackProbeSize.y));
        bool stillHasGround = Physics2D.OverlapBox(knockbackCheck.position, kbSz, 0f, groundLayer) != null;

        if (!stillHasGround)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
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

        // 벽 충돌 데미지: 기본 데미지 + 몸무게의 10%
        float weightBonus = (data != null) ? data.weight * 0.1f : 0f;
        float totalWallDamage = pendingWallImpactDamage + weightBonus;

        if (totalWallDamage > 0f)
        {
            currentHp -= totalWallDamage;
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
            Vector2 size = new Vector2(Mathf.Max(0.05f, ledgeProbeSize.x), Mathf.Max(0.05f, ledgeProbeSize.y));
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(groundCheck.position, 0.05f);
            Gizmos.DrawWireCube(groundCheck.position, new Vector3(size.x, size.y, 0f));
        }

        if (showWallCheckGizmo && wallCheck != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(wallCheck.position, 0.05f);

            float dir = (rb != null && !Mathf.Approximately(rb.linearVelocity.x, 0f)) ? Mathf.Sign(rb.linearVelocity.x) : walkDirection;
            if (Mathf.Approximately(dir, 0f)) dir = 1f;

            Gizmos.DrawRay(wallCheck.position, Vector2.right * dir * wallCheckDistance);
        }

        if (showKnockbackCheckGizmo && knockbackCheck != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f); // 주황색
            Vector2 size = new Vector2(Mathf.Max(0.05f, knockbackProbeSize.x), Mathf.Max(0.05f, knockbackProbeSize.y));
            Gizmos.DrawWireSphere(knockbackCheck.position, 0.05f);
            Gizmos.DrawWireCube(knockbackCheck.position, new Vector3(size.x, size.y, 0f));
        }
    }
}
