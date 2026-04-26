using UnityEngine;
using System.Collections;

public abstract class EnemyController : MonoBehaviour
{
    public EnemyData data;
    public LayerMask playerLayer;
    public LayerMask groundLayer;
    public Transform groundCheck;

    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Transform playerTransform;

    [Header("상태 정보")]
    protected float currentHp;
    protected int walkDirection = 1;
    protected bool isChasing;
    protected bool isReacting;
    protected bool isIdle;
    protected bool isWandering;
    protected bool hasLookedAround;

    [Header("범위 및 시간 설정")]
    protected float unreachableTimer = 0f; 

    [Header("절벽 미련 설정")]
    protected int hesitationCount = 0;   
    public int maxHesitation = 5;        
    protected float pauseTimer = 0f;     

    [Header("타이머")]
    protected float nextUpdateTimer;
    protected int delayedDirection;
    protected float reactionTimer;
    protected float idleTimer;
    protected float wanderTimer;
    protected float attackTimer;
    protected float knockbackTimer;
    protected float lastAttackTime;

    protected bool isKnockback => knockbackTimer > 0;
    protected bool isAttacking => attackTimer > 0;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        walkDirection = (Random.value < 0.5f) ? -1 : 1;
        delayedDirection = walkDirection;
        rb.mass = 10f;
    }

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
                Physics2D.IgnoreCollision(enemyCol, playerCol, true);
            }
        }
    }

    protected virtual void Update()
    {
        // [수정] 넉백 중이거나 경직(isIdle) 중일 때는 AI 로직 전체 차단
        if (isKnockback || isIdle)
        {
            HandleTimers();
            // 경직 중에는 제자리 정지, 넉백 중에는 물리 엔진에 맡김
            if (isIdle) rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        // 공격 및 반응 중일 때 이동 차단
        if (isAttacking || isReacting)
        {
            HandleTimers();
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            return;
        }

        CheckPlayerDetection();
        HandleTimers();
    }

    private void CheckPlayerDetection()
    {
        float currentRange = isChasing ? data.chaseRange : data.detectRange;
        Collider2D playerCol = Physics2D.OverlapCircle(transform.position, currentRange, playerLayer);

        if (playerCol != null)
        {
            playerTransform = playerCol.transform;
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
            else
            {
                if (!isReacting && reactionTimer <= 0 && !isChasing) StartReaction();
            }
        }
        else
        {
            isChasing = false;
            playerTransform = null;
        }
    }

    protected void StartAttack()
    {
        attackTimer = data.preAttackDelay + data.postAttackDelay;
        lastAttackTime = Time.time;
        Attack();
    }

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

    protected virtual void FixedUpdate()
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
        bool isInDetectRange = distanceToPlayer <= data.detectRange; // 노란색
        bool isInChaseRange = distanceToPlayer <= data.chaseRange;   // 빨간색

        // [중요] 절벽 감지 시: 방향을 틀고 탈출하는 동안(0.2초)은 감지를 무시해야 끼이지 않고 전진합니다.
        if (groundInfo.collider == null && !(isWandering && wanderTimer > 0.2f))
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

            if (!isWandering)
            {
                if (isInDetectRange) // 노란 범위: 무한 반복
                {
                    isWandering = true;
                    wanderTimer = 0.6f; // 0.6초간 확실히 후퇴
                    walkDirection *= -1;
                    delayedDirection = walkDirection;
                    Flip(walkDirection);
                    unreachableTimer = 0f;
                }
                else if (isInChaseRange) // 빨간 범위: 5초 체크
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
                else { StopChasingForced(); }
                return;
            }
        }

        // [평지 타이머]
        if (!isInDetectRange && isInChaseRange && isChasing)
        {
            unreachableTimer += Time.fixedDeltaTime;
            if (unreachableTimer >= 5.0f) StopChasingForced();
        }
        else { unreachableTimer = 0f; }

        // 3. 이동 로직
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

    private void StopChasingForced()
    {
        isWandering = true;
        wanderTimer = 2.0f; // 2초간 반대 방향으로 이동

        // 플레이어의 반대 방향 계산
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

    protected void StartReaction() { isReacting = true; isIdle = false; reactionTimer = data.reactionTime; rb.linearVelocity = Vector2.zero; }
    protected void StartIdle() { if (isChasing || isWandering) return; isIdle = true; hasLookedAround = false; idleTimer = data.idleDuration; }
    protected void ApplyMovement(float speed) { rb.linearVelocity = new Vector2(walkDirection * speed, rb.linearVelocity.y); Flip(walkDirection); }

    // [센서 위치 수정] 방향에 따라 센서를 발 앞으로 옮깁니다.
    protected void Flip(float dir)
    {
        if (Mathf.Approximately(dir, 0)) return;
        spriteRenderer.flipX = (dir < 0);

        if (groundCheck != null)
        {
            // 직접 입력했던 0.5f 대신 변수를 사용!
            groundCheck.localPosition = new Vector3(data.groundCheckOffset * Mathf.Sign(dir), groundCheck.localPosition.y, 0);
        }
    }

    protected abstract void Attack();

    protected void HandleTimers()
    {
        if (knockbackTimer > 0)
        {
            knockbackTimer -= Time.deltaTime;
            // [추가] 넉백 타이머가 끝나는 순간 즉시 경직(Idle) 상태로 전환
            if (knockbackTimer <= 0)
            {
                isIdle = true;
                idleTimer = 0.3f; // 0.3초간 멍 때리기
            }
        }

        if (attackTimer > 0) attackTimer -= Time.deltaTime;

        if (reactionTimer > 0)
        {
            reactionTimer -= Time.deltaTime;
            if (reactionTimer <= 0) { isReacting = false; isChasing = true; }
        }

        if (wanderTimer > 0)
        {
            wanderTimer -= Time.deltaTime;
            if (wanderTimer <= 0) isWandering = false;
        }

        if (isIdle)
        {
            idleTimer -= Time.deltaTime;
            // 평소 순찰 중 Idle일 때만 고개 돌리기 수행
            if (!isChasing && !isWandering && idleTimer <= data.idleDuration / 2f && !hasLookedAround)
            {
                Flip(walkDirection * -1);
                hasLookedAround = true;
            }
            if (idleTimer <= 0) isIdle = false;
        }
    }

    public virtual void TakeDamage(float damage, float kPower)
    {
        currentHp -= damage;

        // 넉백 설정
        knockbackTimer = 0.2f;
        isIdle = false; // 넉백 당하는 순간 경직 상태 해제 (새로운 넉백 우선)

        rb.linearVelocity = Vector2.zero; // 기존 속도 초기화

        float dir = playerTransform != null ? Mathf.Sign(transform.position.x - playerTransform.position.x) : -walkDirection;

        // kPower에 따라 뒤로 날아가고, 위로 살짝 뜸 (5f)
        rb.linearVelocity = new Vector2(dir * kPower, 5f);

        if (currentHp <= 0) gameObject.SetActive(false);
    }

    protected virtual void OnDrawGizmos()
    {
        if (data == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.chaseRange);

        Gizmos.color = Color.cyan;
        Vector3 attackCenter = transform.position + new Vector3(walkDirection * data.attackOffset, data.attackHeightOffset, 0);
        Gizmos.DrawWireCube(attackCenter, new Vector3(data.attackRange, data.attackHeight, 0)); 
        
        if (groundCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(groundCheck.position, Vector2.down * data.checkDistance);
        }
    }
}