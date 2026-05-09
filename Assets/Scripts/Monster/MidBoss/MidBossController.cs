using System.Collections;
using UnityEngine;

public abstract class MidBossController : MonoBehaviour
{
    [Header("데이터")]
    public MidBossData data;

    [Header("레이어")]
    public LayerMask playerLayer;
    public LayerMask wallLayer;

    [Header("벽 감지")]
    public Transform wallCheck;

    [Header("방향 전환")]
    [SerializeField] protected float turnDeadZone = 0.2f;

    [Header("추격 정지 거리")]
    [SerializeField] protected float stopDistanceX = 0.35f;

    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Collider2D bossCollider;
    protected Transform playerTransform;

    protected float currentHp;
    protected int lookDirection = 1;

    protected float nextPositionUpdateTime;
    protected int delayedDirection = 1;

    protected bool isChasing;
    protected bool isAttacking;
    protected bool isDead;
    protected bool isBuffed;

    protected float lastBasicAttackTime;
    protected float lastPattern1Time;
    protected float lastPattern2Time;
    protected float lastPattern3Time;

    [Header("공격 전환 딜레이")]
    [SerializeField] protected float patternTransitionDelay = 0.5f;

    protected enum BossAttackKind
    {
        None,
        Basic,
        Pattern1,
        Pattern2,
        Pattern3
    }

    protected BossAttackKind lastAttackKind = BossAttackKind.None;
    protected float lastAttackEndTime = -999f;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bossCollider = GetComponent<Collider2D>();
    }

    protected virtual void Start()
    {
        if (data == null)
        {
            Debug.LogError($"{gameObject.name}에 MidBossData가 연결되지 않았습니다.");
            enabled = false;
            return;
        }

        currentHp = data.maxHp;

        GameObject player = GameObject.FindWithTag("Player");

        if (player != null)
        {
            playerTransform = player.transform;

            Collider2D playerCol = player.GetComponent<Collider2D>();
            if (bossCollider != null && playerCol != null)
                Physics2D.IgnoreCollision(bossCollider, playerCol, true);
        }

        lastBasicAttackTime = -999f;

        lastPattern1Time = Time.time - data.pattern1Cooldown + data.patternStartDelay;
        lastPattern2Time = Time.time - data.pattern2Cooldown + data.patternStartDelay;
        lastPattern3Time = Time.time - data.pattern3Cooldown + data.patternStartDelay;

        delayedDirection = lookDirection;
        nextPositionUpdateTime = Time.time;
    }

    protected virtual void Update()
    {
        if (data == null || isDead) return;

        DetectPlayer();

        if (isAttacking)
        {
            StopMove();
            return;
        }

        if (!isChasing || playerTransform == null)
        {
            StopMove();
            return;
        }

        LookAtPlayer();

        // 범위 안이면 무조건 정지
        // 쿨타임까지 준비됐을 때만 공격 실행
        if (IsInSelectedAttackRange())
        {
            StopMove();

            if (CanUseSelectedAttack())
                TryAttack();
        }
    }

    protected virtual void FixedUpdate()
    {
        if (data == null || isDead || isAttacking) return;

        if (isChasing && playerTransform != null)
        {
            LookAtPlayer();

            // 범위 안이면 쿨타임 중이어도 다가가지 않음
            if (IsInSelectedAttackRange())
            {
                StopMove();
                return;
            }

            MoveToPlayer();
        }
        else
        {
            StopMove();
        }
    }

    protected virtual void DetectPlayer()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        if (playerTransform == null) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (!isChasing)
        {
            if (distance <= data.detectRange)
                isChasing = true;
        }
        else
        {
            if (distance > data.chaseRange)
                isChasing = false;
        }
    }

    protected virtual void MoveToPlayer()
    {
        if (playerTransform == null)
        {
            StopMove();
            return;
        }

        float xDistance = Mathf.Abs(playerTransform.position.x - transform.position.x);

        if (xDistance <= stopDistanceX)
        {
            StopMove();
            return;
        }

        if (IsWallInFront())
        {
            StopMove();
            return;
        }

        float speed = GetMoveSpeed();

        rb.linearVelocity = new Vector2(
            delayedDirection * speed,
            rb.linearVelocity.y
        );
    }

    protected virtual float GetMoveSpeed()
    {
        float speed = data.chaseSpeed;

        if (isBuffed)
            speed *= data.buffMoveSpeedMultiplier;

        return speed;
    }

    protected virtual void LookAtPlayer()
    {
        if (playerTransform == null) return;

        // 플레이어 위치 방향을 일정 시간마다만 갱신
        if (Time.time < nextPositionUpdateTime)
            return;

        float dir = playerTransform.position.x - transform.position.x;

        if (Mathf.Abs(dir) < turnDeadZone)
            return;

        delayedDirection = dir > 0 ? 1 : -1;
        lookDirection = delayedDirection;

        nextPositionUpdateTime = Time.time + data.positionUpdateDelay;

        if (spriteRenderer != null)
            spriteRenderer.flipX = lookDirection < 0;
    }

    protected virtual bool IsWallInFront()
    {
        if (wallCheck == null) return false;

        RaycastHit2D hit = Physics2D.Raycast(
            wallCheck.position,
            Vector2.right * lookDirection,
            data.wallCheckDistance,
            wallLayer
        );

        return hit.collider != null;
    }

    protected virtual void TryAttack()
    {
        if (isAttacking) return;

        int nextAttack = GetNextAttackIndex();

        if (nextAttack == 1 && CanUsePattern1())
        {
            if (!CanStartPatternByTransitionRule()) return;
            StartCoroutine(Pattern1Routine());
            return;
        }

        if (nextAttack == 2 && CanUsePattern2())
        {
            if (!CanStartPatternByTransitionRule()) return;
            StartCoroutine(Pattern2Routine());
            return;
        }

        if (nextAttack == 3 && CanUsePattern3())
        {
            if (!CanStartPatternByTransitionRule()) return;
            StartCoroutine(Pattern3Routine());
            return;
        }

        if (nextAttack == 0 && CanUseBasicAttack())
        {
            StartCoroutine(BasicAttackRoutine());
        }
    }

    protected virtual int GetNextAttackIndex()
    {
        bool pattern1Ready = Time.time >= lastPattern1Time + GetCooldown(data.pattern1Cooldown);
        bool pattern2Ready = Time.time >= lastPattern2Time + GetCooldown(data.pattern2Cooldown);
        bool pattern3Ready = Time.time >= lastPattern3Time + GetCooldown(data.pattern3Cooldown);

        if (pattern1Ready) return 1;
        if (pattern2Ready) return 2;
        if (pattern3Ready) return 3;

        return 0;
    }

    protected virtual float GetCooldown(float baseCooldown)
    {
        if (isBuffed)
            return baseCooldown * data.buffCooldownMultiplier;

        return baseCooldown;
    }

    // 범위만 체크
    protected virtual bool IsInSelectedAttackRange()
    {
        int nextAttack = GetNextAttackIndex();

        if (nextAttack == 1)
        {
            return IsPlayerInAttackRange(
                data.pattern1Range,
                data.pattern1Height,
                data.pattern1Offset,
                data.pattern1HeightOffset
            );
        }

        if (nextAttack == 2)
        {
            return IsPlayerInAttackRange(
                data.pattern2Range,
                data.pattern2Height,
                data.pattern2Offset,
                data.pattern2HeightOffset
            );
        }

        if (nextAttack == 3)
        {
            return true;
        }

        return IsPlayerInAttackRange(
            data.basicAttackRange,
            data.basicAttackHeight,
            data.basicAttackOffset,
            data.basicAttackHeightOffset
        );
    }

    // 쿨타임 + 범위 체크
    protected virtual bool CanUseSelectedAttack()
    {
        int nextAttack = GetNextAttackIndex();

        if (nextAttack == 1) return CanUsePattern1();
        if (nextAttack == 2) return CanUsePattern2();
        if (nextAttack == 3) return CanUsePattern3();

        return CanUseBasicAttack();
    }

    protected virtual bool CanUseBasicAttack()
    {
        if (Time.time < lastBasicAttackTime + GetCooldown(data.basicAttackCooldown))
            return false;

        return IsPlayerInAttackRange(
            data.basicAttackRange,
            data.basicAttackHeight,
            data.basicAttackOffset,
            data.basicAttackHeightOffset
        );
    }

    protected virtual bool CanUsePattern1()
    {
        if (Time.time < lastPattern1Time + GetCooldown(data.pattern1Cooldown))
            return false;

        return IsPlayerInAttackRange(
            data.pattern1Range,
            data.pattern1Height,
            data.pattern1Offset,
            data.pattern1HeightOffset
        );
    }

    protected virtual bool CanUsePattern2()
    {
        if (Time.time < lastPattern2Time + GetCooldown(data.pattern2Cooldown))
            return false;

        return IsPlayerInAttackRange(
            data.pattern2Range,
            data.pattern2Height,
            data.pattern2Offset,
            data.pattern2HeightOffset
        );
    }

    protected virtual bool CanUsePattern3()
    {
        if (isBuffed) return false;

        if (Time.time < lastPattern3Time + data.pattern3Cooldown)
            return false;

        return true;
    }

    protected virtual bool IsPlayerInAttackRange(
        float range,
        float height,
        float offset,
        float heightOffset = 0f
    )
    {
        if (range <= 0f || height <= 0f)
            return true;

        Vector2 center = (Vector2)transform.position + new Vector2(
            lookDirection * offset,
            heightOffset
        );

        Vector2 size = new Vector2(range, height);

        Collider2D hit = Physics2D.OverlapBox(center, size, 0f, playerLayer);
        return hit != null;
    }

    protected virtual IEnumerator BasicAttackRoutine()
    {
        isAttacking = true;
        StopMove();
        lastBasicAttackTime = Time.time;

        OnBasicAttackStart();

        yield return new WaitForSeconds(data.basicPreDelay);

        for (int i = 0; i < data.basicAttackCount; i++)
        {
            BasicAttackAction(i);

            if (i < data.basicAttackCount - 1)
                yield return new WaitForSeconds(data.basicAttackInterval);
        }

        yield return new WaitForSeconds(data.basicPostDelay);

        OnBasicAttackEnd();
        MarkAttackEnded(BossAttackKind.Basic);
        isAttacking = false;
    }

    protected virtual IEnumerator Pattern1Routine()
    {
        isAttacking = true;
        StopMove();
        lastPattern1Time = Time.time;

        OnPattern1Start();

        yield return new WaitForSeconds(data.pattern1PreDelay);

        yield return Pattern1Action();

        yield return new WaitForSeconds(data.pattern1PostDelay);

        OnPattern1End();
        MarkAttackEnded(BossAttackKind.Pattern1);
        isAttacking = false;
    }

    protected virtual IEnumerator Pattern2Routine()
    {
        isAttacking = true;
        StopMove();
        lastPattern2Time = Time.time;

        OnPattern2Start();

        yield return new WaitForSeconds(data.pattern2PreDelay);

        yield return Pattern2Action();

        yield return new WaitForSeconds(data.pattern2PostDelay);

        OnPattern2End();
        MarkAttackEnded(BossAttackKind.Pattern2);
        isAttacking = false;
    }

    protected virtual IEnumerator Pattern3Routine()
    {
        isAttacking = true;
        StopMove();
        lastPattern3Time = Time.time;

        OnPattern3Start();

        yield return new WaitForSeconds(data.pattern3PreDelay);

        yield return Pattern3Action();

        yield return new WaitForSeconds(data.pattern3PostDelay);

        OnPattern3End();
        MarkAttackEnded(BossAttackKind.Pattern3);
        isAttacking = false;
    }

    protected virtual void BasicAttackAction(int attackIndex)
    {
        HitPlayer(
            GetFinalDamage(data.basicAttackDamage),
            data.basicAttackRange,
            data.basicAttackHeight,
            data.basicAttackOffset,
            data.basicAttackHeightOffset
        );
    }

    protected virtual bool CanStartPatternByTransitionRule()
    {
        if (patternTransitionDelay <= 0f) return true;

        bool wasBasicOrPattern = lastAttackKind != BossAttackKind.None;
        if (!wasBasicOrPattern) return true;

        return Time.time >= lastAttackEndTime + patternTransitionDelay;
    }

    protected virtual void MarkAttackEnded(BossAttackKind kind)
    {
        lastAttackKind = kind;
        lastAttackEndTime = Time.time;
    }

    protected abstract IEnumerator Pattern1Action();
    protected abstract IEnumerator Pattern2Action();
    protected abstract IEnumerator Pattern3Action();

    protected virtual float GetFinalDamage(float baseDamage)
    {
        if (isBuffed)
            return baseDamage * data.buffDamageMultiplier;

        return baseDamage;
    }

    protected virtual void HitPlayer(
        float damage,
        float range,
        float height,
        float offset,
        float heightOffset = 0f
    )
    {
        Vector2 center = (Vector2)transform.position + new Vector2(
            lookDirection * offset,
            heightOffset
        );

        Vector2 size = new Vector2(range, height);

        Collider2D hit = Physics2D.OverlapBox(center, size, 0f, playerLayer);

        if (hit != null)
        {
            if (hit.TryGetComponent<PlayerController>(out var player))
            {
                player.TakeDamageNoIFrame(damage, (Vector2)transform.position);
            }
        }
    }

    protected virtual Collider2D GetPlayerHit(
    float range,
    float height,
    float offset,
    float heightOffset = 0f
)
    {
        Vector2 center = (Vector2)transform.position + new Vector2(
            lookDirection * offset,
            heightOffset
        );

        Vector2 size = new Vector2(range, height);

        return Physics2D.OverlapBox(center, size, 0f, playerLayer);
    }

    protected virtual void StopMove()
    {
        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    public virtual void TakeDamage(float damage, float knockbackPower)
    {
        if (isDead) return;

        currentHp -= damage;
        isChasing = true;

        StopMove();

        if (currentHp <= 0)
            Die();
    }

    protected virtual void Die()
    {
        isDead = true;
        StopMove();
        gameObject.SetActive(false);
    }

    protected virtual void OnBasicAttackStart() { }
    protected virtual void OnBasicAttackEnd() { }

    protected virtual void OnPattern1Start() { }
    protected virtual void OnPattern1End() { }

    protected virtual void OnPattern2Start() { }
    protected virtual void OnPattern2End() { }

    protected virtual void OnPattern3Start() { }
    protected virtual void OnPattern3End() { }

    protected virtual void OnDrawGizmos()
    {
        if (data == null) return;

        int dir = lookDirection == 0 ? 1 : lookDirection;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.chaseRange);

        DrawAttackBox(Color.cyan, data.basicAttackRange, data.basicAttackHeight, data.basicAttackOffset, data.basicAttackHeightOffset, dir);
        DrawAttackBox(Color.magenta, data.pattern1Range, data.pattern1Height, data.pattern1Offset, data.pattern1HeightOffset, dir);
        DrawAttackBox(Color.green, data.pattern2Range, data.pattern2Height, data.pattern2Offset, data.pattern2HeightOffset, dir);
        DrawAttackBox(Color.brown, data.pattern3Range, data.pattern3Height, data.pattern3Offset, data.pattern3HeightOffset, dir);

        if (wallCheck != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawRay(
                wallCheck.position,
                Vector2.right * dir * data.wallCheckDistance
            );
        }
    }

    protected virtual void DrawAttackBox(
        Color color,
        float range,
        float height,
        float offset,
        float heightOffset,
        int dir
    )
    {
        if (range <= 0f || height <= 0f) return;

        Gizmos.color = color;

        Vector3 center = transform.position + new Vector3(
            dir * offset,
            heightOffset,
            0f
        );

        Gizmos.DrawWireCube(center, new Vector3(range, height, 0f));
    }
}