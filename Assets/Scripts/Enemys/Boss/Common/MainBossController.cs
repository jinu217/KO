using System.Collections;
using UnityEngine;

// [메인보스 공통 AI]
// - 인식/추적/페이즈 전환/스킬 선택/쿨다운/버프 상태를 공통으로 관리
// - 자식 클래스(Stage1_MainBoss 등)에서 BasicAttackAction/Skill1/2/4/Desire의 실제 연출 구현
public abstract class MainBossController : MonoBehaviour, IDamageable
{
    // 보스가 다음에 사용할 공격 종류
    protected enum BossAttackKind
    {
        None,
        Basic,
        Skill1,
        Skill2,
        Skill3,
        Skill4,
        Skill5,
        Desire
    }

    [Header("데이터")]
    [Tooltip("이 보스가 사용할 MainBossData 에셋")]
    [SerializeField] protected MainBossData data;

    [Header("레이어")]
    [Tooltip("플레이어 감지용 레이어")]
    [SerializeField] protected LayerMask playerLayer;
    [Tooltip("벽 감지용 레이어")]
    [SerializeField] protected LayerMask wallLayer;

    [Header("감지 위치")]
    [Tooltip("벽 감지 Ray 시작점 Transform")]
    [SerializeField] protected Transform wallCheck;

    [Header("방향 전환")]
    [Tooltip("이 거리 이내면 방향을 바꾸지 않음(떨림 방지)")]
    [SerializeField] protected float turnDeadZone = 0.2f;
    [Tooltip("이 거리 이내로 접근 시 추격을 멈추는 X 거리")]
    [SerializeField] protected float stopDistanceX = 0.35f;

    [Header("스킬 전환 제한")]
    [Tooltip("스킬 사용 직후 다른 스킬 사용 불가 시간(연속 스킬 난사 방지)")]
    [SerializeField] protected float skillGlobalLockDuration = 1f;

    [Header("범위 시각화")]
    [Tooltip("최초 인식 범위(노란 원) 표시")]
    [SerializeField] protected bool showDetectRangeGizmo = true;
    [Tooltip("추적 유지 범위(빨간 원) 표시")]
    [SerializeField] protected bool showChaseRangeGizmo = true;
    [Tooltip("기본 공격 박스 표시")]
    [SerializeField] protected bool showBasicAttackGizmo = true;
    [Tooltip("4스킬(돌진) 박스 표시")]
    [SerializeField] protected bool showSkill4Gizmo = true;
    [Tooltip("벽 감지 Ray 표시")]
    [SerializeField] protected bool showWallCheckGizmo = true;

    [Header("실시간 상태")]
    [Tooltip("현재 체력(런타임 디버그용)")]
    [SerializeField] protected float currentHp;

    // 컴포넌트 참조
    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Collider2D bossCollider;
    protected Transform playerTransform;

    // 페이즈
    protected int currentPhase = 1;

    // 방향 상태
    protected int lookDirection = 1;       // 현재 바라보는 방향(1=오른쪽, -1=왼쪽)
    protected int delayedDirection = 1;    // 다음 갱신 시 적용될 방향
    protected float nextPositionUpdateTime; // 다음 방향 갱신 가능 시간

    // 행동 상태
    protected bool isChasing;
    protected bool isAttacking;
    protected bool isDead;

    // 스킬별 마지막 사용 시간(쿨다운 계산용)
    protected float lastBasicTime = -999f;
    protected float lastSkill1Time = -999f;
    protected float lastSkill2Time = -999f;
    protected float lastSkill3Time = -999f;
    protected float lastSkill4Time = -999f;
    protected float lastSkill5Time = -999f;
    protected float lastDesireTime = -999f;

    // 다음 스킬 사용 가능 시점(글로벌 락)
    protected float nextSkillAvailableTime = -999f;

    // 버프 활성 상태
    protected bool attackSpeedBuffActive;
    protected bool moveSpeedBuffActive;
    protected bool desireBuffActive;
    protected bool desireEnhanceActive;

    // 활성 버프 코루틴 핸들
    protected Coroutine attackSpeedBuffRoutine;
    protected Coroutine moveSpeedBuffRoutine;
    protected Coroutine desireBuffRoutine;
    protected Coroutine desireEnhanceRoutine;

    // 1스킬 강화 진행 상태(자식에서 사용)
    protected int skill1HitSuccessCount;
    protected bool nextSkill1Empowered;

    // 컴포넌트 캐싱
    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bossCollider = GetComponent<Collider2D>();
    }

    // 데이터 검증, 플레이어 탐색, 충돌 무시 설정, 모든 스킬 쿨타임 시작, 시작시 체력 초기화
    protected virtual void Start()
    {
        if (data == null)
        {
            Debug.LogError($"{gameObject.name}에 MainBossData가 연결되지 않았습니다.");
            enabled = false;
            return;
        }

        // hp바 표시 및 초기 체력 설정
        currentHp = data.maxHp;
        MainBossHealthUI.Instance?.Show(gameObject.name,currentHp,data.maxHp);

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;

            Collider2D playerCol = player.GetComponent<Collider2D>();
            if (bossCollider != null && playerCol != null)
                Physics2D.IgnoreCollision(bossCollider, playerCol, true);
        }

        delayedDirection = lookDirection;
        nextPositionUpdateTime = Time.time;

        // 시작하자마자 스킬이 사용되지 않도록 모든 스킬 쿨타임 시작
        lastSkill1Time = Time.time;
        lastSkill2Time = Time.time;
        lastSkill3Time = Time.time;
        lastSkill4Time = Time.time;
        lastSkill5Time = Time.time;
        lastDesireTime = Time.time;

        nextSkillAvailableTime = Time.time;
    }

    // 매 프레임 상태 갱신: 탐지 → 페이즈 → 추적/공격 분기
    protected virtual void Update()
    {
        if (data == null || isDead) return;

        DetectPlayer();
        UpdatePhase();

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

        UpdateLookDirection();

        if (IsInSelectedAttackRange())
        {
            StopMove();

            if (CanUseSelectedAttack())
                TryAttack();
        }
    }

    // 물리 프레임: 추적 이동 처리
    protected virtual void FixedUpdate()
    {
        if (data == null || isDead || isAttacking) return;

        if (!isChasing || playerTransform == null)
        {
            StopMove();
            return;
        }

        UpdateLookDirection();

        if (IsInSelectedAttackRange())
        {
            StopMove();
            return;
        }

        MoveToPlayer();
    }

    // 플레이어 탐지 상태 갱신(감지 범위 진입 시 추적 시작, 이탈 시 해제)
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

    // 현재 체력 비율 기반 페이즈 전환
    protected virtual void UpdatePhase()
    {
        float hpPercent = currentHp / data.maxHp;

        if (hpPercent <= data.phase3HpPercent)
            currentPhase = 3;
        else if (hpPercent <= data.phase2HpPercent)
            currentPhase = 2;
        else
            currentPhase = 1;
    }

    // 일정 주기로만 바라보는 방향 갱신(급격한 떨림 방지)
    protected virtual void UpdateLookDirection()
    {
        if (playerTransform == null) return;
        if (Time.time < nextPositionUpdateTime) return;

        float dir = playerTransform.position.x - transform.position.x;

        if (Mathf.Abs(dir) < turnDeadZone)
            return;

        delayedDirection = dir > 0 ? 1 : -1;
        lookDirection = delayedDirection;

        nextPositionUpdateTime = Time.time + data.positionUpdateDelay;

        if (spriteRenderer != null)
            spriteRenderer.flipX = lookDirection < 0;
    }

    // 플레이어를 향해 이동(정지거리 또는 벽 앞에서는 멈춤)
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

    // 버프 상태를 반영한 최종 이동속도 계산
    protected virtual float GetMoveSpeed()
    {
        float speed = data.chaseSpeed;

        if (moveSpeedBuffActive)
            speed *= data.skill5MoveSpeedMultiplier;

        if (desireBuffActive)
            speed *= data.desireMoveSpeedMultiplier;

        return speed;
    }

    // 버프 상태를 반영한 최종 공격 딜레이(선/후딜) 계산
    protected virtual float GetAttackDelay(float baseDelay)
    {
        float multiplier = 1f;

        if (attackSpeedBuffActive)
            multiplier *= data.skill3AttackSpeedMultiplier;

        if (desireBuffActive)
            multiplier *= data.desireAttackSpeedMultiplier;

        return baseDelay / multiplier;
    }

    // 진행 방향에 벽이 있는지 검사
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

    // 현재 조건에서 사용할 다음 공격을 선택해 코루틴 실행
    protected virtual void TryAttack()
    {
        if (isAttacking) return;

        BossAttackKind next = GetNextAttackKind();

        switch (next)
        {
            case BossAttackKind.Desire:
                StartCoroutine(DesireRoutine());
                break;

            case BossAttackKind.Skill1:
                StartCoroutine(Skill1Routine());
                break;

            case BossAttackKind.Skill2:
                StartCoroutine(Skill2Routine());
                break;

            case BossAttackKind.Skill3:
                StartCoroutine(Skill3Routine());
                break;

            case BossAttackKind.Skill4:
                StartCoroutine(Skill4Routine());
                break;

            case BossAttackKind.Skill5:
                StartCoroutine(Skill5Routine());
                break;

            case BossAttackKind.Basic:
                StartCoroutine(BasicAttackRoutine());
                break;
        }
    }

    // 쿨다운/페이즈/강화 상태를 고려해 다음 공격 종류 계산
    protected virtual BossAttackKind GetNextAttackKind()
    {
        bool canUseSkillNow = Time.time >= nextSkillAvailableTime;

        if (canUseSkillNow)
        {
            if (currentPhase >= 3 && CanUseDesire())
                return BossAttackKind.Desire;

            if (CanUseSkill1())
                return BossAttackKind.Skill1;

            if (CanUseSkill2())
                return BossAttackKind.Skill2;

            if (currentPhase >= 1 && CanUseSkill3())
                return BossAttackKind.Skill3;

            if (currentPhase >= 2 && CanUseSkill4())
                return BossAttackKind.Skill4;

            if (currentPhase >= 2 && CanUseSkill5())
                return BossAttackKind.Skill5;
        }

        return BossAttackKind.Basic;
    }

    // 선택된 공격이 사거리 안인지 검사
    protected virtual bool IsInSelectedAttackRange()
    {
        BossAttackKind next = GetNextAttackKind();

        if (next == BossAttackKind.Skill2)
            return IsPlayerInSkill2Range();

        if (next == BossAttackKind.Skill4)
            return true;

        if (next == BossAttackKind.Skill3 || next == BossAttackKind.Skill5 || next == BossAttackKind.Desire)
            return true;

        return IsPlayerInBasicRange();
    }

    // 선택된 공격을 실제로 사용 가능한지 검사(쿨다운 + 사거리)
    protected virtual bool CanUseSelectedAttack()
    {
        BossAttackKind next = GetNextAttackKind();

        return next switch
        {
            BossAttackKind.Desire => CanUseDesire(),
            BossAttackKind.Skill1 => CanUseSkill1(),
            BossAttackKind.Skill2 => CanUseSkill2(),
            BossAttackKind.Skill3 => CanUseSkill3(),
            BossAttackKind.Skill4 => CanUseSkill4(),
            BossAttackKind.Skill5 => CanUseSkill5(),
            BossAttackKind.Basic => CanUseBasicAttack(),
            _ => false
        };
    }

    // 기본 공격 사용 가능 여부 검사
    protected virtual bool CanUseBasicAttack()
    {
        float cooldown = desireBuffActive ? data.desireBasicCooldown : data.basicCooldown;

        if (Time.time < lastBasicTime + cooldown)
            return false;

        return IsPlayerInBasicRange();
    }

    // 1스킬 사용 가능 여부 검사
    protected virtual bool CanUseSkill1()
    {
        float cooldown = desireEnhanceActive ? data.desireSkill1Cooldown : data.skill1Cooldown;

        if (Time.time < lastSkill1Time + cooldown)
            return false;

        return IsPlayerInBasicRange();
    }

    // 2스킬 사용 가능 여부 검사(쿨 종료 + 거리, 또는 강제 캐스트 조건)
    protected virtual bool CanUseSkill2()
    {
        float cooldown = desireEnhanceActive ? data.desireSkill2Cooldown : data.skill2Cooldown;

        if (Time.time >= lastSkill2Time + cooldown)
            return IsPlayerInSkill2Range();

        if (CanForceSkill2ByDistance())
            return true;

        return false;
    }

    // 일정 잔여 쿨다운 + 거리 조건에서 2스킬 강제 캐스트 가능 여부 검사
    protected virtual bool CanForceSkill2ByDistance()
    {
        if (playerTransform == null) return false;

        float cooldown = desireEnhanceActive ? data.desireSkill2Cooldown : data.skill2Cooldown;
        float forceRemaining = desireEnhanceActive
            ? data.desireSkill2ForceCastRemainingCooldown
            : data.skill2ForceCastRemainingCooldown;

        float remainingCooldown = lastSkill2Time + cooldown - Time.time;

        if (remainingCooldown > forceRemaining)
            return false;

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (distance < data.skill2MinDistanceToForceCast)
            return false;

        return distance <= data.skill2CastRange;
    }

    // 3스킬(공속 버프) 사용 가능 여부 검사
    protected virtual bool CanUseSkill3()
    {
        if (desireBuffActive) return false;
        if (attackSpeedBuffActive) return false;
        if (Time.time < lastSkill3Time + data.skill3Cooldown) return false;
        return true;
    }

    // 4스킬(돌진) 사용 가능 여부 검사
    protected virtual bool CanUseSkill4()
    {
        if (currentPhase < 2) return false;
        if (Time.time < lastSkill4Time + data.skill4Cooldown) return false;
        return true;
    }

    // 5스킬(이속 버프) 사용 가능 여부 검사
    protected virtual bool CanUseSkill5()
    {
        if (currentPhase < 2) return false;
        if (desireBuffActive) return false;
        if (moveSpeedBuffActive) return false;
        if (Time.time < lastSkill5Time + data.skill5Cooldown) return false;
        return true;
    }

    // 욕망 스킬 사용 가능 여부 검사(페이즈3 + 미발동 상태)
    protected virtual bool CanUseDesire()
    {
        if (currentPhase < 3) return false;
        if (desireBuffActive) return false;
        if (Time.time < lastDesireTime + data.desireCooldown) return false;
        return true;
    }

    // 기본 공격 박스 안에 플레이어가 있는지 검사
    protected virtual bool IsPlayerInBasicRange()
    {
        return IsPlayerInAttackRange(
            data.basicHorizontalRange,
            data.basicVerticalHeight,
            data.basicHorizontalOffset
        );
    }

    // 2스킬 캐스트 거리 안에 플레이어가 있는지 검사
    protected virtual bool IsPlayerInSkill2Range()
    {
        if (playerTransform == null) return false;

        float distance = Vector2.Distance(transform.position, playerTransform.position);
        return distance <= data.skill2CastRange;
    }

    // 임의 박스 영역 안에 플레이어가 있는지 검사
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

    // 임의 박스 영역에서 플레이어 콜라이더 가져오기(자식이 데미지 처리 시 사용)
    protected virtual Collider2D GetPlayerHit(float range, float height, float offset, float heightOffset = 0f)
    {
        Vector2 center = (Vector2)transform.position + new Vector2(
            lookDirection * offset,
            heightOffset
        );

        Vector2 size = new Vector2(range, height);

        return Physics2D.OverlapBox(center, size, 0f, playerLayer);
    }

    // 박스 영역 안에서 플레이어 적중 시 데미지(+옵션 넉백) 처리
    protected virtual void HitPlayer(float damage, float range, float height, float offset, bool knockback = false, float knockbackPower = 0f)
    {
        Collider2D hit = GetPlayerHit(range, height, offset);

        if (hit == null) return;

        IDamageable target = hit.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage, (Vector2)transform.position, knockback, false);
            if (knockback && hit.TryGetComponent<PlayerController>(out var pc))
                ApplyKnockbackToPlayer(pc, knockbackPower);
        }
    }

    // 플레이어에게 사선 위쪽으로 강제 넉백 적용
    protected virtual void ApplyKnockbackToPlayer(PlayerController player, float power)
    {
        Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
        if (playerRb == null) return;

        Vector2 dir = new Vector2(lookDirection, 0.25f).normalized;

        playerRb.linearVelocity = Vector2.zero;
        playerRb.AddForce(dir * power, ForceMode2D.Impulse);
    }

    // 스킬 사용 직후 공통 잠금(연속 스킬 난사 방지)
    protected virtual void StartSkillGlobalLock()
    {
        nextSkillAvailableTime = Time.time + skillGlobalLockDuration;
    }

    // 기본 공격 코루틴: 선딜 → 자식 BasicAttackAction → 후딜
    protected virtual IEnumerator BasicAttackRoutine()
    {
        isAttacking = true;
        StopMove();
        lastBasicTime = Time.time;

        yield return new WaitForSeconds(GetAttackDelay(data.basicPreDelay));

        yield return BasicAttackAction();

        yield return new WaitForSeconds(GetAttackDelay(data.basicPostDelay));

        isAttacking = false;
    }

    // 1스킬 코루틴: 선딜 → 자식 Skill1Action → 후딜
    protected virtual IEnumerator Skill1Routine()
    {
        isAttacking = true;
        StartSkillGlobalLock();
        StopMove();
        lastSkill1Time = Time.time;

        yield return new WaitForSeconds(GetAttackDelay(data.skill1PreDelay));

        yield return Skill1Action();

        yield return new WaitForSeconds(GetAttackDelay(data.skill1PostDelay));

        isAttacking = false;
    }

    // 2스킬 코루틴: 선딜 → 자식 Skill2Action → 후딜
    protected virtual IEnumerator Skill2Routine()
    {
        isAttacking = true;
        StartSkillGlobalLock();
        StopMove();
        lastSkill2Time = Time.time;

        yield return new WaitForSeconds(GetAttackDelay(data.skill2PreDelay));

        yield return Skill2Action();

        yield return new WaitForSeconds(GetAttackDelay(data.skill2PostDelay));

        isAttacking = false;
    }

    // 3스킬 코루틴: 선딜 → 공격속도 버프 시작 → 후딜
    protected virtual IEnumerator Skill3Routine()
    {
        isAttacking = true;
        StartSkillGlobalLock();
        StopMove();
        lastSkill3Time = Time.time;

        yield return new WaitForSeconds(data.skill3PreDelay);

        StartAttackSpeedBuff();

        yield return new WaitForSeconds(data.skill3PostDelay);

        isAttacking = false;
    }

    // 4스킬 코루틴: 선딜 → 자식 Skill4Action → 후딜
    protected virtual IEnumerator Skill4Routine()
    {
        isAttacking = true;
        StartSkillGlobalLock();
        StopMove();
        lastSkill4Time = Time.time;

        yield return new WaitForSeconds(data.skill4PreDelay);

        yield return Skill4Action();

        yield return new WaitForSeconds(data.skill4PostDelay);

        isAttacking = false;
    }

    // 5스킬 코루틴: 선딜 → 이동속도 버프 시작 → 후딜
    protected virtual IEnumerator Skill5Routine()
    {
        isAttacking = true;
        StartSkillGlobalLock();
        StopMove();
        lastSkill5Time = Time.time;

        yield return new WaitForSeconds(data.skill5PreDelay);

        StartMoveSpeedBuff();

        yield return new WaitForSeconds(data.skill5PostDelay);

        isAttacking = false;
    }

    // 욕망 스킬 코루틴: 선딜 → 강화 활성화 → 자식 DesireAction → 후딜
    protected virtual IEnumerator DesireRoutine()
    {
        isAttacking = true;
        StartSkillGlobalLock();
        StopMove();
        lastDesireTime = Time.time;

        yield return new WaitForSeconds(data.desirePreDelay);

        ActivateDesire();

        yield return DesireAction();

        yield return new WaitForSeconds(data.desirePostDelay);

        isAttacking = false;
    }

    // 공격속도 버프 시작(중복 시 기존 코루틴 중단 후 재시작)
    protected virtual void StartAttackSpeedBuff()
    {
        if (attackSpeedBuffRoutine != null)
            StopCoroutine(attackSpeedBuffRoutine);

        attackSpeedBuffRoutine = StartCoroutine(AttackSpeedBuffRoutine());
    }

    // 공격속도 버프 지속 시간 동안 활성 상태 유지
    protected virtual IEnumerator AttackSpeedBuffRoutine()
    {
        attackSpeedBuffActive = true;
        yield return new WaitForSeconds(data.skill3Duration);
        attackSpeedBuffActive = false;
        attackSpeedBuffRoutine = null;
    }

    // 이동속도 버프 시작(중복 시 기존 코루틴 중단 후 재시작)
    protected virtual void StartMoveSpeedBuff()
    {
        if (moveSpeedBuffRoutine != null)
            StopCoroutine(moveSpeedBuffRoutine);

        moveSpeedBuffRoutine = StartCoroutine(MoveSpeedBuffRoutine());
    }

    // 이동속도 버프 지속 시간 동안 활성 상태 유지
    protected virtual IEnumerator MoveSpeedBuffRoutine()
    {
        moveSpeedBuffActive = true;
        yield return new WaitForSeconds(data.skill5Duration);
        moveSpeedBuffActive = false;
        moveSpeedBuffRoutine = null;
    }

    // 욕망(강화) 상태 활성화 + 쿨다운 리셋
    protected virtual void ActivateDesire()
    {
        ResetAllSkillCooldowns();

        if (desireBuffRoutine != null)
            StopCoroutine(desireBuffRoutine);

        if (desireEnhanceRoutine != null)
            StopCoroutine(desireEnhanceRoutine);

        desireBuffRoutine = StartCoroutine(DesireBuffRoutine());
        desireEnhanceRoutine = StartCoroutine(DesireEnhanceRoutine());
    }

    // 욕망 버프 지속 시간 동안 활성 상태 유지(다른 버프 비활성화)
    protected virtual IEnumerator DesireBuffRoutine()
    {
        desireBuffActive = true;

        attackSpeedBuffActive = false;
        moveSpeedBuffActive = false;

        yield return new WaitForSeconds(data.desireBuffDuration);

        desireBuffActive = false;
        desireBuffRoutine = null;
    }

    // 욕망 강화(스킬 데미지 강화) 지속 시간 유지
    protected virtual IEnumerator DesireEnhanceRoutine()
    {
        desireEnhanceActive = true;

        yield return new WaitForSeconds(data.desireEnhanceDuration);

        desireEnhanceActive = false;
        desireEnhanceRoutine = null;
    }

    // 욕망 발동 시 스킬 쿨다운 재설정
    protected virtual void ResetAllSkillCooldowns()
    {
        lastSkill1Time = -999f;
        lastSkill2Time = -999f;

        // 욕망 중 다른 버프 스킬은 사용 금지 조건이 있으므로 바로 사용 가능하게 풀지 않음
        lastSkill3Time = Time.time;
        lastSkill4Time = -999f;
        lastSkill5Time = Time.time;
    }

    // 수평 이동 정지(중력은 유지)
    protected virtual void StopMove()
    {
        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    // 외부에서 보스에게 피해 적용(체력 0 이하 시 사망 처리), 보스 HP바 연동
    // IDamageable 구현 — 보스는 넉백 없음, applyKnockback 무시
    public virtual void TakeDamage(float damage, Vector2 sourcePos, bool applyKnockback = false, bool applyIFrame = false)
    {
        if (isDead) return;

        currentHp -= damage;
        MainBossHealthUI.Instance?.UpdateHP(currentHp);
        isChasing = true;

        if (currentHp <= 0f)
            Die();
    }

    // 사망 처리: 모든 코루틴 중단 + 비활성화, HP바 숨김처리
    protected virtual void Die()
    {
        isDead = true;
        // 메인보스 처치 기록
        if (SaveData.Instance != null)
        {
            SaveData.Instance.run.mainBossKillCount++;
        }
        MainBossHealthUI.Instance?.Hide();
        StopMove();
        StopAllCoroutines();
        gameObject.SetActive(false);
    }

    // 자식 클래스에서 구현해야 하는 실제 공격 연출
    protected abstract IEnumerator BasicAttackAction();
    protected abstract IEnumerator Skill1Action();
    protected abstract IEnumerator Skill2Action();
    protected abstract IEnumerator Skill4Action();
    protected abstract IEnumerator DesireAction();

    // 인식/공격/벽체크 범위 Gizmo 시각화(인스펙터 토글)
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
            DrawAttackBox(Color.cyan, data.basicHorizontalRange, data.basicVerticalHeight, data.basicHorizontalOffset, 0f, dir);

        if (showSkill4Gizmo)
            DrawAttackBox(Color.magenta, data.skill4Range, data.skill4Height, data.skill4Offset, 0f, dir);

        if (showWallCheckGizmo && wallCheck != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(wallCheck.position, 0.05f);
            Gizmos.DrawRay(wallCheck.position, Vector2.right * dir * data.wallCheckDistance);
        }
    }

    // 공격 박스 와이어 큐브 한 개 그리기
    protected virtual void DrawAttackBox(Color color, float range, float height, float offset, float heightOffset, int dir)
    {
        Gizmos.color = color;

        Vector3 center = transform.position + new Vector3(
            dir * offset,
            heightOffset,
            0f
        );

        Gizmos.DrawWireCube(center, new Vector3(range, height, 0f));
    }
}
