using System.Collections;
using UnityEngine;

// [중간 보스 공통 AI]
// - 인식/추적/공격 선택/쿨다운/공통 판정 처리 담당
// - 자식 클래스에서 Pattern1/2/3Action()을 구현해 실제 동작을 정의
public abstract class MidBossController : MonoBehaviour, IDamageable
{
    // 직전에 사용한 공격 종류(연속 패턴 사이의 전환 딜레이 판정에 사용)
    protected enum BossAttackKind
    {
        None,
        Basic,
        Pattern1,
        Pattern2,
        Pattern3
    }

    [Header("데이터")]
    [Tooltip("보스 스탯/패턴 수치")]
    public MidBossData data;

    [Header("레이어")]
    [Tooltip("플레이어 탐지/공격 판정 레이어")]
    public LayerMask playerLayer;
    [Tooltip("벽 감지 레이어")]
    public LayerMask wallLayer;

    [Header("벽 감지")]
    [Tooltip("벽 체크 Ray 시작 위치")]
    public Transform wallCheck;

    [Header("방향 전환")]
    [Tooltip("X 차이가 이 값 이하면 방향 전환을 보류(떨림 방지)")]
    [SerializeField] protected float turnDeadZone = 0.2f;

    [Header("추격 정지 거리")]
    [Tooltip("플레이어와의 X 차이가 이 값 이하면 정지")]
    [SerializeField] protected float stopDistanceX = 0.35f;

    [Header("공격 전환 딜레이")]
    [Tooltip("기본/패턴 사이 최소 대기 시간")]
    [SerializeField] protected float patternTransitionDelay = 0.5f;

    [Header("범위 시각화")]
    [Tooltip("최초 인식 범위(노란 원)")]
    [SerializeField] protected bool showDetectRangeGizmo = true;
    [Tooltip("추적 유지 범위(빨간 원)")]
    [SerializeField] protected bool showChaseRangeGizmo = true;
    [Tooltip("기본 공격 박스(하늘색)")]
    [SerializeField] protected bool showBasicAttackGizmo = true;
    [Tooltip("패턴1 박스(자홍)")]
    [SerializeField] protected bool showPattern1Gizmo = true;
    [Tooltip("패턴2 박스(녹색)")]
    [SerializeField] protected bool showPattern2Gizmo = true;
    [Tooltip("패턴3 박스(갈색)")]
    [SerializeField] protected bool showPattern3Gizmo = true;
    [Tooltip("벽 감지 Ray(흰색)")]
    [SerializeField] protected bool showWallCheckGizmo = true;

    [Header("실시간 상태")]
    [Tooltip("현재 체력(디버그 확인용)")]
    [SerializeField] protected float currentHp;

    // -------------------- 컴포넌트/대상 --------------------
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Collider2D bossCollider;
    protected Transform playerTransform;

    // -------------------- 방향/이동 --------------------
    protected int lookDirection = 1;
    protected int delayedDirection = 1;
    protected float nextPositionUpdateTime;

    // -------------------- 상태 --------------------
    protected bool isChasing;
    protected bool isAttacking;
    protected bool isDead;
    protected bool isBuffed;

    // -------------------- 쿨다운 타이머 --------------------
    protected float lastBasicAttackTime;
    protected float lastPattern1Time;
    protected float lastPattern2Time;
    protected float lastPattern3Time;

    // -------------------- 공격 전환 --------------------
    protected BossAttackKind lastAttackKind = BossAttackKind.None;
    protected float lastAttackEndTime = -999f;

    // 패턴3 박스가 0/0/0 같이 미설정이어도 갈색이 잘 보이도록 정의(Unity에는 Color.brown이 없음)
    private static readonly Color BrownColor = new Color(0.6f, 0.3f, 0.1f);

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bossCollider = GetComponent<Collider2D>();
    }

    // 시작 시 체력 초기화 + 플레이어 충돌 무시 + 패턴 시작 지연 적용
    protected virtual void Start()
    {
        if (data == null)
        {
            Debug.LogError($"{gameObject.name}에 MidBossData가 연결되지 않았습니다.");
            enabled = false;
            return;
        }

        // hp바 표시 및 초기 체력 설정
        currentHp = data.maxHp;
        MidBossHealthUI.Instance?.Show(gameObject.name,currentHp,data.maxHp);

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;

            Collider2D playerCol = player.GetComponent<Collider2D>();
            if (bossCollider != null && playerCol != null)
                Physics2D.IgnoreCollision(bossCollider, playerCol, true);
        }

        // 시작 직후 즉시 공격하지 않도록 모든 쿨타임을 현재 시각으로 초기화
        // 각 패턴의 첫 사용은 자신의 쿨타임이 지난 뒤부터 가능
        lastBasicAttackTime = Time.time;
        lastPattern1Time = Time.time;
        lastPattern2Time = Time.time;
        lastPattern3Time = Time.time;

        delayedDirection = lookDirection;
        nextPositionUpdateTime = Time.time;
    }

    // 프레임 로직: 탐지 → 공격 가능 시 시도 → 그 외엔 정지
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

        if (IsInSelectedAttackRange())
        {
            StopMove();

            if (CanUseSelectedAttack())
                TryAttack();
        }
    }

    // 물리 프레임 로직: 사거리 안이면 정지, 밖이면 추적
    protected virtual void FixedUpdate()
    {
        if (data == null || isDead || isAttacking) return;

        if (isChasing && playerTransform != null)
        {
            LookAtPlayer();

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

    // 플레이어 탐지 상태 갱신(범위 진입 시 추적 시작, 이탈 시 해제)
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

    // 추적 이동(벽 감지/정지 거리 고려)
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
        rb.linearVelocity = new Vector2(delayedDirection * speed, rb.linearVelocity.y);
    }

    // 현재 적용되는 이동 속도(버프 시 배율 적용)
    protected virtual float GetMoveSpeed()
    {
        float speed = data.chaseSpeed;
        if (isBuffed) speed *= data.buffMoveSpeedMultiplier;
        return speed;
    }

    // 플레이어 방향으로 바라보기(갱신 지연 포함, dead-zone 적용)
    protected virtual void LookAtPlayer()
    {
        if (playerTransform == null) return;
        if (Time.time < nextPositionUpdateTime) return;

        float dir = playerTransform.position.x - transform.position.x;
        if (Mathf.Abs(dir) < turnDeadZone) return;

        delayedDirection = dir > 0 ? 1 : -1;
        lookDirection = delayedDirection;

        nextPositionUpdateTime = Time.time + data.positionUpdateDelay;

        if (spriteRenderer != null)
            spriteRenderer.flipX = lookDirection < 0;
    }

    // 전방에 벽이 있는지 확인
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

    // 다음 공격 패턴 실행(전환 딜레이 검사 포함)
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

    // 쿨다운 기반 다음 공격 인덱스 계산(0=기본, 1/2/3=패턴)
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

    // 현재 적용되는 쿨다운(버프 시 배율 적용)
    protected virtual float GetCooldown(float baseCooldown)
    {
        if (isBuffed) return baseCooldown * data.buffCooldownMultiplier;
        return baseCooldown;
    }

    // 다음에 사용할 공격의 사거리 안에 플레이어가 있는지(범위만 체크)
    protected virtual bool IsInSelectedAttackRange()
    {
        int nextAttack = GetNextAttackIndex();

        if (nextAttack == 1)
            return IsPlayerInAttackRange(data.pattern1Range, data.pattern1Height, data.pattern1Offset, data.pattern1HeightOffset);

        if (nextAttack == 2)
            return IsPlayerInAttackRange(data.pattern2Range, data.pattern2Height, data.pattern2Offset, data.pattern2HeightOffset);

        if (nextAttack == 3)
            return true;

        return IsPlayerInAttackRange(data.basicAttackRange, data.basicAttackHeight, data.basicAttackOffset, data.basicAttackHeightOffset);
    }

    // 다음에 사용할 공격이 쿨타임 + 범위 모두 충족하는지
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
        if (Time.time < lastBasicAttackTime + GetCooldown(data.basicAttackCooldown)) return false;
        return IsPlayerInAttackRange(data.basicAttackRange, data.basicAttackHeight, data.basicAttackOffset, data.basicAttackHeightOffset);
    }

    protected virtual bool CanUsePattern1()
    {
        if (Time.time < lastPattern1Time + GetCooldown(data.pattern1Cooldown)) return false;
        return IsPlayerInAttackRange(data.pattern1Range, data.pattern1Height, data.pattern1Offset, data.pattern1HeightOffset);
    }

    protected virtual bool CanUsePattern2()
    {
        if (Time.time < lastPattern2Time + GetCooldown(data.pattern2Cooldown)) return false;
        return IsPlayerInAttackRange(data.pattern2Range, data.pattern2Height, data.pattern2Offset, data.pattern2HeightOffset);
    }

    protected virtual bool CanUsePattern3()
    {
        if (isBuffed) return false;
        if (Time.time < lastPattern3Time + data.pattern3Cooldown) return false;
        return true;
    }

    // 전방 박스 판정으로 플레이어가 범위 내인지 확인
    protected virtual bool IsPlayerInAttackRange(float range, float height, float offset, float heightOffset = 0f)
    {
        if (range <= 0f || height <= 0f) return true;

        Vector2 center = (Vector2)transform.position + new Vector2(lookDirection * offset, heightOffset);
        Vector2 size = new Vector2(range, height);

        Collider2D hit = Physics2D.OverlapBox(center, size, 0f, playerLayer);
        return hit != null;
    }

    // 기본 공격 코루틴: 선딜 → 연타 → 후딜
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

    // 패턴1 코루틴: 선딜 → 자식 Pattern1Action → 후딜
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

    // 패턴2 코루틴: 선딜 → 자식 Pattern2Action → 후딜
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

    // 패턴3 코루틴: 선딜 → 자식 Pattern3Action → 후딜
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

    // 기본 공격 1타 동작(자식 클래스에서 오버라이드 가능)
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

    // 직전 공격 종료 후 일정 시간 대기를 강제(연속 패턴 난사 방지)
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

    // 자식 구현: 각 패턴의 실제 동작
    protected abstract IEnumerator Pattern1Action();
    protected abstract IEnumerator Pattern2Action();
    protected abstract IEnumerator Pattern3Action();

    // 최종 데미지 계산(버프 적용)
    protected virtual float GetFinalDamage(float baseDamage)
    {
        if (isBuffed) return baseDamage * data.buffDamageMultiplier;
        return baseDamage;
    }

    // 전방 박스 범위에 플레이어가 있으면 데미지 적용
    protected virtual void HitPlayer(float damage, float range, float height, float offset, float heightOffset = 0f)
    {
        Vector2 center = (Vector2)transform.position + new Vector2(lookDirection * offset, heightOffset);
        Vector2 size = new Vector2(range, height);

        Collider2D hit = Physics2D.OverlapBox(center, size, 0f, playerLayer);
        if (hit == null) return;

        if (hit.TryGetComponent<PlayerController>(out var player))
            player.TakeDamageNoIFrame(damage, (Vector2)transform.position);
    }

    // 전방 박스 범위에서 플레이어 콜라이더 반환(자식이 직접 후처리할 때 사용)
    protected virtual Collider2D GetPlayerHit(float range, float height, float offset, float heightOffset = 0f)
    {
        Vector2 center = (Vector2)transform.position + new Vector2(lookDirection * offset, heightOffset);
        Vector2 size = new Vector2(range, height);

        return Physics2D.OverlapBox(center, size, 0f, playerLayer);
    }

    protected virtual void StopMove()
    {
        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    // 보스 피격 처리(체력 감소/추적 유지/사망 체크), 보스 HP바 연동
    // IDamageable 구현 — 보스는 넉백 없음, applyKnockback 무시
    public virtual void TakeDamage(float damage, Vector2 sourcePos, bool applyKnockback = false, bool applyIFrame = false)
    {
        if (isDead) return;

        currentHp -= damage;
        MidBossHealthUI.Instance?.UpdateHP(currentHp);
        isChasing = true;
        StopMove();

        if (currentHp <= 0)
            Die();
    }

    // 보스 사망 처리, HP바 숨김처리
    protected virtual void Die()
    {
        isDead = true;
        // 미드보스 처치 기록
        if (SaveData.Instance != null)
        {
            SaveData.Instance.run.midBossKillCount++;
        }
        MidBossHealthUI.Instance?.Hide();
        StopMove();
        gameObject.SetActive(false);
    }

    // 패턴 시작/종료 훅(자식에서 오버라이드)
    protected virtual void OnBasicAttackStart() { }
    protected virtual void OnBasicAttackEnd() { }
    protected virtual void OnPattern1Start() { }
    protected virtual void OnPattern1End() { }
    protected virtual void OnPattern2Start() { }
    protected virtual void OnPattern2End() { }
    protected virtual void OnPattern3Start() { }
    protected virtual void OnPattern3End() { }

    // 디버그 시각화: 인스펙터의 [범위 시각화] 토글로 각 항목 개별 제어
    protected virtual void OnDrawGizmos()
    {
        if (data == null) return;

        int dir = lookDirection == 0 ? 1 : lookDirection;

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

        if (showBasicAttackGizmo)
            DrawAttackBox(Color.cyan, data.basicAttackRange, data.basicAttackHeight, data.basicAttackOffset, data.basicAttackHeightOffset, dir);

        if (showPattern1Gizmo)
            DrawAttackBox(Color.magenta, data.pattern1Range, data.pattern1Height, data.pattern1Offset, data.pattern1HeightOffset, dir);

        if (showPattern2Gizmo)
            DrawAttackBox(Color.green, data.pattern2Range, data.pattern2Height, data.pattern2Offset, data.pattern2HeightOffset, dir);

        if (showPattern3Gizmo)
            DrawAttackBox(BrownColor, data.pattern3Range, data.pattern3Height, data.pattern3Offset, data.pattern3HeightOffset, dir);

        if (showWallCheckGizmo && wallCheck != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(wallCheck.position, 0.05f);
            Gizmos.DrawRay(wallCheck.position, Vector2.right * dir * data.wallCheckDistance);
        }
    }

    // 공격 박스 와이어큐브 그리기 헬퍼(range/height 0이면 그리지 않음)
    protected virtual void DrawAttackBox(Color color, float range, float height, float offset, float heightOffset, int dir)
    {
        if (range <= 0f || height <= 0f) return;

        Gizmos.color = color;
        Vector3 center = transform.position + new Vector3(dir * offset, heightOffset, 0f);
        Gizmos.DrawWireCube(center, new Vector3(range, height, 0f));
    }
}
