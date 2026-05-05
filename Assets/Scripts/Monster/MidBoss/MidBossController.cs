using UnityEngine;
using System.Collections;

public abstract class MidBossController : MonoBehaviour
{
    [Header("데이터")]
    public MidBossData data;

    [Header("레이어")]
    public LayerMask playerLayer;
    public LayerMask wallLayer;

    [Header("벽 감지")]
    public Transform wallCheck;
    public float wallCheckDistance = 0.5f;

    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Transform playerTransform;

    protected float currentHp;
    protected int lookDirection = 1;

    protected bool isChasing;
    protected bool isAttacking;
    protected bool isDead;

    protected float lastBasicAttackTime;
    protected float lastPattern1Time;
    protected float lastPattern2Time;
    protected float lastPattern3Time;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
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

            Collider2D bossCol = GetComponent<Collider2D>();
            Collider2D playerCol = player.GetComponent<Collider2D>();

            if (bossCol != null && playerCol != null)
            {
                Physics2D.IgnoreCollision(bossCol, playerCol, true);
            }
        }

        lastBasicAttackTime = -999f;

        lastPattern1Time = Time.time - data.pattern1Cooldown + data.patternStartDelay;
        lastPattern2Time = Time.time - data.pattern2Cooldown + data.patternStartDelay;
        lastPattern3Time = Time.time - data.pattern3Cooldown + data.patternStartDelay;
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

        if (CanUseSelectedAttack())
        {
            StopMove();
            TryAttack();
        }
    }

    protected virtual void FixedUpdate()
    {
        if (data == null || isDead || isAttacking) return;

        if (isChasing && playerTransform != null)
        {
            LookAtPlayer();

            if (CanUseSelectedAttack())
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
        LookAtPlayer();

        if (IsWallInFront())
        {
            StopMove();
            return;
        }

        rb.linearVelocity = new Vector2(lookDirection * data.chaseSpeed, rb.linearVelocity.y);
    }

    protected virtual void LookAtPlayer()
    {
        if (playerTransform == null) return;

        float dir = playerTransform.position.x - transform.position.x;

        if (dir > 0)
            lookDirection = 1;
        else if (dir < 0)
            lookDirection = -1;

        if (spriteRenderer != null)
            spriteRenderer.flipX = lookDirection < 0;
    }

    protected virtual bool IsWallInFront()
    {
        if (wallCheck == null) return false;

        RaycastHit2D hit = Physics2D.Raycast(
            wallCheck.position,
            Vector2.right * lookDirection,
            wallCheckDistance,
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
            StartCoroutine(Pattern1Routine());
            return;
        }

        if (nextAttack == 2 && CanUsePattern2())
        {
            StartCoroutine(Pattern2Routine());
            return;
        }

        if (nextAttack == 3 && CanUsePattern3())
        {
            StartCoroutine(Pattern3Routine());
            return;
        }

        if (nextAttack == 0 && CanUseBasicAttack())
        {
            StartCoroutine(BasicAttackRoutine());
            return;
        }
    }

    protected virtual int GetNextAttackIndex()
    {
        bool pattern1Ready = Time.time >= lastPattern1Time + data.pattern1Cooldown;
        bool pattern2Ready = Time.time >= lastPattern2Time + data.pattern2Cooldown;
        bool pattern3Ready = Time.time >= lastPattern3Time + data.pattern3Cooldown;

        if (pattern1Ready) return 1;
        if (pattern2Ready) return 2;
        if (pattern3Ready) return 3;

        return 0;
    }

    protected virtual bool CanUseSelectedAttack()
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
            return IsPlayerInAttackRange(
                data.pattern3Range,
                data.pattern3Height,
                data.pattern3Offset,
                data.pattern3HeightOffset
            );
        }

        return CanUseBasicAttack();
    }

    protected virtual bool CanUseBasicAttack()
    {
        if (Time.time < lastBasicAttackTime + data.basicAttackCooldown)
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
        if (Time.time < lastPattern1Time + data.pattern1Cooldown)
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
        if (Time.time < lastPattern2Time + data.pattern2Cooldown)
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
        if (Time.time < lastPattern3Time + data.pattern3Cooldown)
            return false;

        return IsPlayerInAttackRange(
            data.pattern3Range,
            data.pattern3Height,
            data.pattern3Offset,
            data.pattern3HeightOffset
        );
    }

    protected virtual bool IsPlayerInAttackRange(float range, float height, float offset, float heightOffset = 0f)
    {
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

        yield return new WaitForSeconds(data.basicPreDelay);

        HitPlayer(
            data.basicAttackDamage,
            data.basicAttackRange,
            data.basicAttackHeight,
            data.basicAttackOffset,
            data.basicAttackHeightOffset
        );

        yield return new WaitForSeconds(data.basicPostDelay);

        isAttacking = false;
    }

    protected virtual IEnumerator Pattern1Routine()
    {
        isAttacking = true;
        StopMove();
        lastPattern1Time = Time.time;

        yield return new WaitForSeconds(data.pattern1PreDelay);

        Pattern1();

        yield return new WaitForSeconds(data.pattern1PostDelay);

        isAttacking = false;
    }

    protected virtual IEnumerator Pattern2Routine()
    {
        isAttacking = true;
        StopMove();
        lastPattern2Time = Time.time;

        yield return new WaitForSeconds(data.pattern2PreDelay);

        Pattern2();

        yield return new WaitForSeconds(data.pattern2PostDelay);

        isAttacking = false;
    }

    protected virtual IEnumerator Pattern3Routine()
    {
        isAttacking = true;
        StopMove();
        lastPattern3Time = Time.time;

        yield return new WaitForSeconds(data.pattern3PreDelay);

        Pattern3();

        yield return new WaitForSeconds(data.pattern3PostDelay);

        isAttacking = false;
    }

    protected virtual void HitPlayer(float damage, float range, float height, float offset, float heightOffset = 0f)
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
                player.TakeDamage(damage, (Vector2)transform.position);
            }
        }
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

    protected abstract void Pattern1();
    protected abstract void Pattern2();
    protected abstract void Pattern3();

    protected virtual void OnDrawGizmos()
    {
        if (data == null) return;

        int dir = lookDirection == 0 ? 1 : lookDirection;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.chaseRange);

        Gizmos.color = Color.cyan;
        Vector3 basicCenter = transform.position + new Vector3(
            dir * data.basicAttackOffset,
            data.basicAttackHeightOffset,
            0f
        );
        Gizmos.DrawWireCube(
            basicCenter,
            new Vector3(data.basicAttackRange, data.basicAttackHeight, 0f)
        );

        Gizmos.color = Color.magenta;
        Vector3 p1Center = transform.position + new Vector3(
            dir * data.pattern1Offset,
            data.pattern1HeightOffset,
            0f
        );
        Gizmos.DrawWireCube(
            p1Center,
            new Vector3(data.pattern1Range, data.pattern1Height, 0f)
        );

        Gizmos.color = Color.blue;
        Vector3 p2Center = transform.position + new Vector3(
            dir * data.pattern2Offset,
            data.pattern2HeightOffset,
            0f
        );
        Gizmos.DrawWireCube(
            p2Center,
            new Vector3(data.pattern2Range, data.pattern2Height, 0f)
        );

        Gizmos.color = Color.green;
        Vector3 p3Center = transform.position + new Vector3(
            dir * data.pattern3Offset,
            data.pattern3HeightOffset,
            0f
        );
        Gizmos.DrawWireCube(
            p3Center,
            new Vector3(data.pattern3Range, data.pattern3Height, 0f)
        );

        if (wallCheck != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawRay(
                wallCheck.position,
                Vector2.right * dir * wallCheckDistance
            );
        }
    }
}