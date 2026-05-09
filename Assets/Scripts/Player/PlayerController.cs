using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// [플레이어] 이동/점프/대쉬/공격/피격/인벤토리/포션을 담당하는 플레이어 컨트롤러
public enum WeaponType
{
    Sword,
    Blunt,
    Spear,
    Axe,
    Scythe
}

public class PlayerController : MonoBehaviour
{
    // -------------------- 컴포넌트/입력 --------------------
    private Rigidbody2D rb;      // 플레이어 물리 제어
    private Vector2 moveInput;   // 입력 시스템에서 받은 이동 입력

    // -------------------- 이동/점프 --------------------
    [Header("이동 및 점프 설정")]
    private int currentJumpCount;  // 남은 점프 횟수

    [Tooltip("타일 1칸의 월드 높이(기본 1유닛)")]
    public float tileHeight = 1f;
    [Tooltip("목표 점프 높이(타일 개수)")]
    public float jumpHeightInTiles = 3f;
    [Tooltip("true면 목표 타일 높이에 맞춰 점프 힘을 자동 계산")]
    public bool useCalculatedJumpForce = true;

    [Tooltip("점프 버튼을 빨리 떼면 상승 속도를 얼마나 줄일지(0~1)")]
    [Range(0.1f, 1f)] public float jumpCutMultiplier = 0.5f;
    [Tooltip("점프 버튼 유지 시 추가 상승이 적용되는 최대 시간")]
    public float maxJumpHoldTime = 0.12f;
    [Tooltip("점프 유지 중 중력을 얼마나 약하게 할지(낮을수록 더 높이 점프)")]
    [Range(0.1f, 1f)] public float jumpHoldGravityMultiplier = 0.6f;

    [Header("가속/감속 및 에어 컨트롤")]
    [Tooltip("저속/고속에서의 지상 가속도")]
    public float groundAccelAtLowSpeed = 60f;
    public float groundAccelAtHighSpeed = 25f;
    [Tooltip("저속/고속에서의 지상 감속도")]
    public float groundDecelAtLowSpeed = 70f;
    public float groundDecelAtHighSpeed = 15f;

    [Tooltip("저속/고속에서의 공중 가속도")]
    public float airAccelAtLowSpeed = 30f;
    public float airAccelAtHighSpeed = 12f;
    [Tooltip("저속/고속에서의 공중 감속도")]
    public float airDecelAtLowSpeed = 20f;
    public float airDecelAtHighSpeed = 8f;

    private bool isJumpHeld;            // 점프 버튼 유지 상태
    private float jumpHoldTimer;        // 점프 유지 타이머

    [Header("점프 판정 보정")]
    [Tooltip("발판에서 떨어진 직후에도 점프를 허용하는 시간")]
    public float coyoteTime = 0.1f;
    [Tooltip("공중 입력 점프를 착지 직후 실행하기 위해 보관하는 시간")]
    public float jumpBufferTime = 0.12f;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool hasBurnedGroundJumpAfterCoyote;
    private bool wasGrounded;

    [Header("모서리 밀림 보정")]
    [Tooltip("머리 모서리 걸림 보정에 사용할 콜라이더(비우면 자동 탐색)")]
    public Collider2D bodyCollider;
    [Tooltip("머리 위 모서리 감지 거리")]
    public float cornerCorrectionCheckDistance = 0.08f;
    [Tooltip("모서리 보정 시 옆으로 밀어주는 거리")]
    public float cornerCorrectionPushX = 0.12f;
    [Tooltip("모서리 보정 연속 발동 간격")]
    public float cornerCorrectionCooldown = 0.05f;
    private float cornerCorrectionTimer;

    // -------------------- 대쉬 --------------------
    [Header("대쉬(Dash) 설정")]
    [Tooltip("대쉬 이동 거리")]
    public float dashDistance = 3f;
    [Tooltip("대쉬 이동 속도")]
    public float dashSpeed = 20f;
    [Tooltip("대쉬 쿨다운")]
    public float dashCooldown = 0.5f;

    private bool isDashing;          // 대쉬 중 여부
    private bool canDash = true;     // 대쉬 가능 여부

    // -------------------- 하향 점프 --------------------
    [Header("하향 점프(Down Jump) 설정")]
    public LayerMask thinFloorLayer; // 점프스루 발판 레이어(Ground2)
    [Tooltip("강화 하강 판정용 연속 입력 허용 시간")]
    public float empoweredDownJumpWindow = 0.5f;
    [Tooltip("강화 하강 착지 충격 범위(사각형)")]
    public Vector2 empoweredDownJumpImpactSize = new Vector2(2.5f, 1.5f);
    [Tooltip("강화 하강 착지 충격 중심 오프셋(위/아래, 좌/우 조정)")]
    public Vector2 empoweredDownJumpImpactOffset = Vector2.zero;
    [Tooltip("강화 하강 착지 충격 데미지")]
    public float empoweredDownJumpDamage = 5f;
    [Tooltip("강화 하강 착지 넉백 파워")]
    public float empoweredDownJumpKnockbackPower = 6f;
    [Tooltip("강화 하강 중 하강 속도 배율(일반 하강 대비)")]
    public float empoweredDownJumpFallSpeedMultiplier = 1.2f;
    [Tooltip("강화 하강 착지 후 이동 불가 시간")]
    public float empoweredDownJumpMoveDelay = 0.5f;
    [Tooltip("강화 하강 디버그 로그 출력")]
    public bool debugEmpoweredDownJump = true;

    [Header("낙하 데미지/스턴 설정")]
    [Tooltip("이 높이 이상 떨어지면 낙하 데미지 적용")]
    public float fallDamageMinHeight = 3f;
    [Tooltip("낙하 데미지 값")]
    public float fallDamageAmount = 5f;
    [Tooltip("낙하 데미지 시 스턴(이동 지연) 시간")]
    public float fallDamageStunDelay = 0.5f;

    private bool isDownJumping;      // 하향 점프 진행 중 여부
    private bool isEmpoweredDownJump;// 강화 하향 점프 여부
    private float downJumpStartTime = -999f;
    private bool empowerWindowActive;
    private bool wasDownInputHeld;
    private Collider2D ignoredThinFloorCollider;
    private bool isMoveDelayed;
    private float moveDelayEndTime;
    private bool isTrackingFall;
    private float fallStartY;
    private bool ignoreFallDamageOnNextLanding;

    // -------------------- 지면 체크 --------------------
    [Header("지면 체크(Ground Check)")]
    public Transform groundCheck;        // 발밑 체크 기준점
    public float groundCheckRadius = 0.25f; // 체크 반지름
    public LayerMask groundLayer;        // 일반 지면 레이어
    private bool isGrounded;             // 지면 접지 여부
    private int currentGroundOrder;      // 현재 밟고 있는 지면의 sortingOrder

    // -------------------- 전투 --------------------
    [Header("전투 설정")]
    public LayerMask enemyLayer;           // 공격 판정 대상 레이어
    public WeaponType currentWeapon = WeaponType.Sword; // 현재 무기 타입
    public float currentHp;                // 현재 체력
    private bool isKnockback;              // 넉백 상태 여부

    [Header("타격 연출")]
    [Tooltip("공격 적중 시 적용할 히트 스탑 시간")]
    public float hitStopDuration = 0.03f;
    [Tooltip("히트 스탑 시 느려지는 배율(0에 가까울수록 강함)")]
    [Range(0f, 1f)] public float hitStopTimeScale = 0.05f;
    private bool isHitStopRunning;

    [Header("피격 보정")]
    [Tooltip("피격 직후 추가 피해를 무시하는 무적 시간")]
    public float damageInvincibleTime = 0.1f;
    [Tooltip("무적 중 깜빡임 간격")]
    public float invincibleBlinkInterval = 0.04f;
    private bool isDamageInvincible;
    private Coroutine invincibleRoutine;
    private SpriteRenderer[] hitFlashRenderers;

    // -------------------- 인벤토리 --------------------
    [Header("인벤토리 설정")]
    private bool isInventoryOpen; // 인벤토리 UI 열림 상태
    private InventoryUI inventoryUI; // 인벤토리 UI 참조

    // -------------------- 내부 보조 값 --------------------
    private float lastHorizontalDirection = 1f; // 마지막으로 바라본 좌우 방향
    private float lastAttackTime;               // 마지막 공격 시각
    private float originalGravity;              // 초기 중력 값(대쉬 시 복원용)

    // 컴포넌트 초기화
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();

        hitFlashRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        originalGravity = rb.gravityScale;
        lastAttackTime = -999f;
    }

    // 시작 시 GameManager/InventoryUI 연결 및 체력 초기화
    void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager 인스턴스를 찾을 수 없습니다. 씬에 GameManager를 배치해 주세요.");
            enabled = false;
            return;
        }

        // 플레이어 관련 스탯은 GameManager 기준으로 사용
        currentHp = GameManager.Instance.playerMaxHp;
        currentJumpCount = GameManager.Instance.playerMaxJumpCount;

        if (useCalculatedJumpForce)
            GameManager.Instance.playerJumpForce = CalculateJumpForceFromTiles();

        inventoryUI = FindInventoryUIAnyState();
    }

    // 매 프레임 상태 업데이트(지면 체크, 점프 리셋)
    void Update()
    {
        // UI 열림 중에는 조작 로직 중지
        if (isInventoryOpen || ChestRewardUI.IsAnyOpen) return;

        // 1) 지면 체크(일반 지면 + 점프스루 발판)
        Collider2D groundCollider = FindGroundCollider();
        isGrounded = groundCollider != null;

        if (isGrounded && groundCollider.TryGetComponent<SpriteRenderer>(out SpriteRenderer gSr))
            currentGroundOrder = gSr.sortingOrder;

        // 지면 이탈/착지 전환 감지(낙하 데미지용)
        if (wasGrounded && !isGrounded)
        {
            isTrackingFall = true;
            fallStartY = transform.position.y;
        }

        // 2) 하향 점프 상태 리셋 + 강화 입력(아래) 감지
        if (isDownJumping)
        {
            HandleEmpoweredDownJumpInput();

            if (isGrounded && rb.linearVelocity.y < -0.1f)
                ResetDownJump();
        }
        else
        {
            wasDownInputHeld = moveInput.y < -0.1f;
        }

        // 3) 코요테 타임 & 점프 횟수/버퍼 관리
        if (isGrounded && rb.linearVelocity.y <= 0.1f && !isDownJumping)
        {
            currentJumpCount = GameManager.Instance.playerMaxJumpCount;
            coyoteTimer = coyoteTime;
            hasBurnedGroundJumpAfterCoyote = false;

            // 착지 직전 점프 입력 버퍼 처리
            TryConsumeBufferedJump();
        }
        else
        {
            coyoteTimer -= Time.deltaTime;

            // 코요테 타임이 끝났는데 아직 '첫 점프'를 안 썼다면, 지상 점프 권한 1회 소모
            if (!isGrounded && coyoteTimer <= 0f && !hasBurnedGroundJumpAfterCoyote)
            {
                int maxJump = GameManager.Instance.playerMaxJumpCount;
                if (currentJumpCount == maxJump)
                    currentJumpCount = Mathf.Max(0, maxJump - 1);

                hasBurnedGroundJumpAfterCoyote = true;
            }
        }

        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
            TryConsumeBufferedJump();
        }

        // 4) 가변 점프 높이(버튼 유지 시 상승 보정)
        if (!isDashing)
        {
            if (isDownJumping && isEmpoweredDownJump)
            {
                rb.gravityScale = originalGravity * empoweredDownJumpFallSpeedMultiplier;
            }
            else if (isJumpHeld && !isGrounded && rb.linearVelocity.y > 0f)
            {
                jumpHoldTimer += Time.deltaTime;
                if (jumpHoldTimer <= maxJumpHoldTime)
                    rb.gravityScale = originalGravity * jumpHoldGravityMultiplier;
                else
                {
                    isJumpHeld = false;
                    rb.gravityScale = originalGravity;
                }
            }
            else
            {
                rb.gravityScale = originalGravity;
            }
        }

        // 이동 지연 타이머 갱신
        if (isMoveDelayed && Time.time >= moveDelayEndTime)
            isMoveDelayed = false;

        // 착지 시 낙하 데미지/스턴 처리(강화 하강 제외)
        if (!wasGrounded && isGrounded)
            HandleLandingFallDamage();

        wasGrounded = isGrounded;
    }

    // -------------------- 입력 콜백: 이동 --------------------
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();

        // 인벤토리 열림 중에는 플레이어 이동 대신 인벤토리 선택 커서 이동에 사용
        if (isInventoryOpen)
        {
            if (inventoryUI != null) inventoryUI.Navigate(moveInput);
            return;
        }

        // 보상 UI 열림 중에는 플레이어 이동 입력 무시
        if (ChestRewardUI.IsAnyOpen) return;

        if (isDashing || isKnockback || isMoveDelayed) return;

        if (moveInput.x != 0) UpdateFacingDirection();
    }

    // -------------------- 입력 콜백: 점프/하향 점프 --------------------
    public void OnJump(InputAction.CallbackContext context)
    {
        if (isDashing || isKnockback || isMoveDelayed || isInventoryOpen || ChestRewardUI.IsAnyOpen) return;

        // 버튼을 뗐을 때: 상승 속도 컷(짧은 점프)
        if (context.canceled)
        {
            isJumpHeld = false;
            if (rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

            if (!isDashing)
                rb.gravityScale = originalGravity;
            return;
        }

        if (!context.started) return;

        // 하강 점프 시작: 아래 + 점프 (Ground2 위)
        if (moveInput.y < -0.1f && !isDownJumping)
        {
            Collider2D hit = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, thinFloorLayer);
            if (hit != null)
            {
                isEmpoweredDownJump = false;
                StartCoroutine(ThinFloorDownJumpRoutine(hit));

                if (debugEmpoweredDownJump)
                    Debug.Log($"[DownJump] Start normal down jump t={Time.time:F3}");

                return;
            }
        }

        // 점프 입력 버퍼 저장(착지 직전 입력 허용)
        jumpBufferTimer = jumpBufferTime;

        // 지금 점프 가능하면 즉시 실행
        TryConsumeBufferedJump();
    }

    // 하향 점프 종료 시 충돌 복구
    void ResetDownJump()
    {
        bool landedWithEmpowered = isEmpoweredDownJump;
        isDownJumping = false;

        if (ignoredThinFloorCollider != null && bodyCollider != null)
        {
            Physics2D.IgnoreCollision(bodyCollider, ignoredThinFloorCollider, false);
            ignoredThinFloorCollider = null;
        }

        if (landedWithEmpowered)
        {
            if (debugEmpoweredDownJump)
                Debug.Log($"[DownJump] Empowered landing impact t={Time.time:F3}");

            ApplyEmpoweredDownJumpImpact();
            ApplyMoveDelay(empoweredDownJumpMoveDelay);
            ignoreFallDamageOnNextLanding = true;
        }

        isEmpoweredDownJump = false;
        downJumpStartTime = -999f;
        empowerWindowActive = false;
        wasDownInputHeld = moveInput.y < -0.1f;
    }

    // Ground2 통과용 하향 점프 루틴(시작 발판만 무시)
    private IEnumerator ThinFloorDownJumpRoutine(Collider2D startThinFloor)
    {
        isDownJumping = true;
        isEmpoweredDownJump = false;
        downJumpStartTime = Time.time;
        empowerWindowActive = true;
        wasDownInputHeld = moveInput.y < -0.1f;

        // 하강 시작 시점부터 낙하 추적
        isTrackingFall = true;
        fallStartY = transform.position.y;

        if (debugEmpoweredDownJump)
            Debug.Log($"[DownJump] Window open ({empoweredDownJumpWindow:F2}s) from t={downJumpStartTime:F3}");

        if (startThinFloor != null && bodyCollider != null)
        {
            ignoredThinFloorCollider = startThinFloor;
            Physics2D.IgnoreCollision(bodyCollider, ignoredThinFloorCollider, true);
        }

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -2f);

        if (ignoredThinFloorCollider != null)
        {
            while (ignoredThinFloorCollider != null)
            {
                if (bodyCollider == null) break;

                Bounds floorBounds = ignoredThinFloorCollider.bounds;
                Bounds playerBounds = bodyCollider.bounds;

                // 시작 발판을 충분히 통과하면 충돌 복구
                if (playerBounds.max.y < floorBounds.min.y - 0.02f)
                {
                    Physics2D.IgnoreCollision(bodyCollider, ignoredThinFloorCollider, false);
                    ignoredThinFloorCollider = null;
                    break;
                }

                yield return null;
            }
        }
    }

    // -------------------- 입력 콜백: 대쉬 --------------------
    public void OnDash(InputAction.CallbackContext context)
    {
        if (isKnockback || isMoveDelayed || isInventoryOpen || ChestRewardUI.IsAnyOpen) return;
        if (context.started && canDash && !isDashing) StartCoroutine(DashAction());
    }

    // 대쉬 실행 루틴
    private IEnumerator DashAction()
    {
        canDash = false;
        isDashing = true;

        float prevGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        float dashDir = moveInput.x != 0 ? Mathf.Sign(moveInput.x) : lastHorizontalDirection;
        float moved = 0f;

        // 대쉬 시작 시 관성 제거
        rb.linearVelocity = Vector2.zero;

        while (moved < dashDistance)
        {
            float step = dashSpeed * Time.fixedDeltaTime;
            float remain = dashDistance - moved;
            if (step > remain) step = remain;

            rb.MovePosition(rb.position + new Vector2(dashDir * step, 0f));
            moved += step;

            yield return new WaitForFixedUpdate();
        }

        // 대쉬 종료 시 즉시 정지(미끄러짐 방지)
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = prevGravity;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // -------------------- 입력 콜백: 공격 --------------------
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.started || isDashing || isKnockback || ChestRewardUI.IsAnyOpen) return;

        // 인벤토리 열림 중 Attack(Z)는 아이템 장착 확인 키로 사용
        if (isInventoryOpen)
        {
            if (inventoryUI != null) inventoryUI.ConfirmCurrentSelection();
            return;
        }

        if (Time.time < lastAttackTime + (1f / GameManager.Instance.playerAttackSpeed)) return;

        Attack();
        lastAttackTime = Time.time;
    }

    // -------------------- 입력 콜백: 인벤토리 --------------------
    public void OnInventory(InputAction.CallbackContext context)
    {
        if (!context.started || ChestRewardUI.IsAnyOpen) return;

        if (inventoryUI == null)
            inventoryUI = FindInventoryUIAnyState();

        if (inventoryUI == null)
        {
            Debug.LogWarning("InventoryUI를 찾을 수 없습니다. InventoryPanel에 InventoryUI가 붙어있는지 확인해 주세요.");
            return;
        }

        isInventoryOpen = !isInventoryOpen;
        OpenInventory();
    }

    // -------------------- 입력 콜백: 포션 --------------------
    public void OnHPPotion(InputAction.CallbackContext context)
    {
        if (!context.started || isInventoryOpen || ChestRewardUI.IsAnyOpen) return;
        UsePotion();
    }

    // 실제 공격 판정 처리(무기별 범위/넉백 적용)
    void Attack()
    {
        float dmg = GameManager.Instance.playerAttackDamage;

        // 크리티컬 계산
        if (UnityEngine.Random.value < GameManager.Instance.playerCriticalChance)
            dmg *= GameManager.Instance.playerCriticalMultiplier;

        float dir = Mathf.Sign(transform.localScale.x);
        Vector2 size = GetWeaponAttackSize();
        Vector2 pos = (Vector2)transform.position + Vector2.right * dir * GetWeaponOffset();

        bool hitAny = false;
        Collider2D[] hits = Physics2D.OverlapBoxAll(pos, size, 0f, enemyLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<EnemyController>(out var enemy))
            {
                enemy.TakeDamage(dmg, GetWeaponKnockbackPower());
                hitAny = true;
            }
        }

        if (hitAny)
            StartCoroutine(HitStopRoutine());
    }

    // 인벤토리 UI 토글 + 타임스케일 제어
    private void OpenInventory()
    {
        if (isInventoryOpen)
        {
            Debug.Log("인벤토리 열기 - 게임 멈춘됨");
            if (inventoryUI != null) inventoryUI.Open();
            Time.timeScale = 0f;
        }
        else
        {
            Debug.Log("인벤토리 닫기 - 게임 재개시");
            if (inventoryUI != null) inventoryUI.Close();
            Time.timeScale = 1f;
        }
    }

    // 포션 사용 처리
    private void UsePotion()
    {
        if (GameManager.Instance.potionCount <= 0)
        {
            Debug.Log("포션이 없습니다!");
            return;
        }

        float baseHeal = GameManager.Instance.potionHealAmount;
        float percentHeal = baseHeal * GameManager.Instance.potionHealPercent;
        float totalHeal = baseHeal + percentHeal;

        currentHp = Mathf.Min(currentHp + totalHeal, GameManager.Instance.playerMaxHp);
        GameManager.Instance.potionCount--;

        Debug.Log("포션 사용! " + totalHeal + "의 HP 회복됨. 남은 포션: " + GameManager.Instance.potionCount + " | 현재 HP: " + currentHp);
    }

    public void ApplyItemStats(ItemStats stats)
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.playerMaxHp += stats.hp;
        GameManager.Instance.playerDefense += stats.defense;
        GameManager.Instance.playerAttackDamage += stats.attack;
        GameManager.Instance.playerAttackSpeed += stats.attackSpeed;
        GameManager.Instance.playerCriticalChance += stats.criticalChance;
        GameManager.Instance.playerCriticalMultiplier += stats.criticalDamage;

        GameManager.Instance.playerMoveSpeed += stats.moveSpeed;

        // 최대 체력 증가 시 현재 체력도 함께 보정
        currentHp += stats.hp;

        Debug.Log($"아이템 스탯 적용: HP+{stats.hp}, 방어+{stats.defense}, 이동속도+{stats.moveSpeed}, 공격력+{stats.attack}, 공격속도+{stats.attackSpeed}, 치확+{stats.criticalChance}, 치피+{stats.criticalDamage}");
    }

    public void RemoveItemStats(ItemStats stats)
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.playerMaxHp -= stats.hp;
        GameManager.Instance.playerDefense -= stats.defense;
        GameManager.Instance.playerAttackDamage -= stats.attack;
        GameManager.Instance.playerAttackSpeed -= stats.attackSpeed;
        GameManager.Instance.playerCriticalChance -= stats.criticalChance;
        GameManager.Instance.playerCriticalMultiplier -= stats.criticalDamage;

        GameManager.Instance.playerMoveSpeed -= stats.moveSpeed;

        // 최대 체력이 줄어들면 현재 체력도 최대치 이내로 보정
        currentHp = Mathf.Min(currentHp, GameManager.Instance.playerMaxHp);

        Debug.Log($"아이템 스탯 제거: HP-{stats.hp}, 방어-{stats.defense}, 이동속도-{stats.moveSpeed}, 공격력-{stats.attack}, 공격속도-{stats.attackSpeed}, 치확-{stats.criticalChance}, 치피-{stats.criticalDamage}");
    }

    public void SetCurrentWeaponFromInventoryType(InventoryWeaponType inventoryWeaponType)
    {
        switch (inventoryWeaponType)
        {
            case InventoryWeaponType.Sword:
                currentWeapon = WeaponType.Sword;
                break;
            case InventoryWeaponType.Blunt:
                currentWeapon = WeaponType.Blunt;
                break;
            case InventoryWeaponType.Spear:
                currentWeapon = WeaponType.Spear;
                break;
            case InventoryWeaponType.Axe:
                currentWeapon = WeaponType.Axe;
                break;
            case InventoryWeaponType.Scythe:
                currentWeapon = WeaponType.Scythe;
                break;
            default:
                break;
        }

        Debug.Log($"현재 무기 타입 변경: {currentWeapon}");
    }

    // 피격 처리(방어력 적용 + 넉백 + 게임오버 체크)
    public void TakeDamage(float damage, Vector2 enemyPos)
    {
        TakeDamageInternal(damage, enemyPos, true);
    }

    // 보스/특수 패턴용: 피격 무적 프레임 없이 데미지 처리
    public void TakeDamageNoIFrame(float damage, Vector2 enemyPos)
    {
        TakeDamageInternal(damage, enemyPos, false);
    }

    private void TakeDamageInternal(float damage, Vector2 enemyPos, bool applyIFrame)
    {
        // 대쉬 중에는 무적 처리
        if (isDashing) return;

        // 일반 몬스터 공격에만 무적 프레임 적용
        if (applyIFrame && isDamageInvincible) return;

        float defenseRate = GameManager.Instance.playerDefense / (GameManager.Instance.playerDefense + 100f);
        float finalDamage = Mathf.Max(damage * 0.2f, damage * (1f - defenseRate));

        currentHp -= finalDamage;
        Debug.Log(gameObject.name + " 피격! 원래 데미지: " + damage + " -> 최종 데미지: " + finalDamage + " | 남은 HP: " + currentHp);

        isKnockback = true;
        rb.linearVelocity = Vector2.zero;

        float knockbackDirX = (enemyPos.x > transform.position.x) ? -1f : 1f;
        if (Mathf.Approximately(enemyPos.x, transform.position.x)) knockbackDirX = 1f;

        Vector2 knockbackForce = new Vector2(knockbackDirX * 3f, 5f);
        rb.AddForce(knockbackForce, ForceMode2D.Impulse);

        StartCoroutine(KnockbackRoutine());
        StartCoroutine(IgnoreEnemyCollisionRoutine());

        if (applyIFrame)
        {
            if (invincibleRoutine != null) StopCoroutine(invincibleRoutine);
            invincibleRoutine = StartCoroutine(DamageInvincibleRoutine());
        }

        if (currentHp <= 0) GameOver();
    }

    // 오버로드: 적 위치 없이 들어온 데미지 처리용(일반 몬스터 규칙: 무적 프레임 적용)
    public void TakeDamage(float damage)
    {
        float dir = (transform.localScale.x > 0) ? -1f : 1f;
        Vector2 enemyPos = (Vector2)transform.position + new Vector2(-dir * 2f, 0);
        TakeDamageInternal(damage, enemyPos, true);
    }

    // 넉백 상태 해제 루틴
    private IEnumerator KnockbackRoutine()
    {
        yield return new WaitForSeconds(0.2f);
        isKnockback = false;
    }

    // 피격 직후 무적 + 깜빡임 처리
    private IEnumerator DamageInvincibleRoutine()
    {
        isDamageInvincible = true;

        float timer = 0f;
        bool visible = true;

        while (timer < damageInvincibleTime)
        {
            timer += Time.deltaTime;

            if (hitFlashRenderers != null && hitFlashRenderers.Length > 0)
            {
                visible = !visible;
                foreach (var sr in hitFlashRenderers)
                {
                    if (sr != null) sr.enabled = visible;
                }
            }

            yield return new WaitForSeconds(invincibleBlinkInterval);
        }

        if (hitFlashRenderers != null)
        {
            foreach (var sr in hitFlashRenderers)
            {
                if (sr != null) sr.enabled = true;
            }
        }

        isDamageInvincible = false;
        invincibleRoutine = null;
    }

    // 공격 적중 시 짧은 히트 스탑
    private IEnumerator HitStopRoutine()
    {
        if (isHitStopRunning) yield break;
        if (Time.timeScale <= 0f) yield break;

        isHitStopRunning = true;

        float prevScale = Time.timeScale;
        float prevFixed = Time.fixedDeltaTime;

        Time.timeScale = hitStopTimeScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        yield return new WaitForSecondsRealtime(hitStopDuration);

        Time.timeScale = prevScale;
        Time.fixedDeltaTime = prevFixed;
        isHitStopRunning = false;
    }

    // 플레이어-적 충돌 일시 무시 루틴(연속 피격 완화)
    private IEnumerator IgnoreEnemyCollisionRoutine()
    {
        int pL = LayerMask.NameToLayer("Player");
        int eL = LayerMask.NameToLayer("Enemy");

        if (pL != -1 && eL != -1)
        {
            Physics2D.IgnoreLayerCollision(pL, eL, true);
            yield return new WaitForSeconds(0.5f);
            Physics2D.IgnoreLayerCollision(pL, eL, false);
        }
    }

    // -------------------- 무기 보조 함수 --------------------

    // 바라보는 방향 갱신(입력 기준)
    private void UpdateFacingDirection()
    {
        lastHorizontalDirection = Mathf.Sign(moveInput.x);
        transform.localScale = new Vector3(Mathf.Sign(moveInput.x), 1, 1);
    }

    // 무기별 공격 박스 크기 반환
    private Vector2 GetWeaponAttackSize()
    {
        switch (currentWeapon)
        {
            case WeaponType.Sword: return new Vector2(2.0f, 1.2f);
            case WeaponType.Blunt: return new Vector2(1.2f, 2.0f);
            case WeaponType.Spear: return new Vector2(3.5f, 0.6f);
            case WeaponType.Axe: return new Vector2(1.8f, 1.8f);
            case WeaponType.Scythe: return new Vector2(2.5f, 1.5f);
            default: return new Vector2(2.0f, 1.2f);
        }
    }

    // 무기별 공격 박스 전방 오프셋 반환
    private float GetWeaponOffset()
    {
        switch (currentWeapon)
        {
            case WeaponType.Sword: return 1.2f;
            case WeaponType.Blunt: return 0.8f;
            case WeaponType.Spear: return 2.0f;
            case WeaponType.Axe: return 1.0f;
            case WeaponType.Scythe: return 1.5f;
            default: return 1.2f;
        }
    }

    // 무기별 넉백 수치 반환
    private float GetWeaponKnockbackPower()
    {
        switch (currentWeapon)
        {
            case WeaponType.Sword: return 5f;
            case WeaponType.Blunt: return 10f;
            case WeaponType.Spear: return 100f;
            case WeaponType.Axe: return 8f;
            case WeaponType.Scythe: return 6f;
            default: return 5f;
        }
    }

    // 플레이어 사망 처리(현재 씬 재시작)
    void GameOver()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // 비활성 오브젝트까지 포함해서 InventoryUI 탐색
    private InventoryUI FindInventoryUIAnyState()
    {
        // 우선 활성 오브젝트에서 탐색
        InventoryUI found = FindFirstObjectByType<InventoryUI>();
        if (found != null) return found;

        // 비활성 오브젝트까지 포함 탐색
        var all = Resources.FindObjectsOfTypeAll<InventoryUI>();
        foreach (var ui in all)
        {
            if (ui == null) continue;
            if (!ui.gameObject.scene.IsValid()) continue; // 에셋/프리팹 제외
            return ui;
        }

        return null;
    }

    // 목표 타일 높이에 맞는 점프 힘 계산
    private float CalculateJumpForceFromTiles()
    {
        float gravity = Mathf.Abs(Physics2D.gravity.y * originalGravity);
        if (gravity <= 0.0001f) return GameManager.Instance.playerJumpForce;

        float targetHeight = Mathf.Max(0.1f, tileHeight * jumpHeightInTiles);
        return Mathf.Sqrt(2f * gravity * targetHeight);
    }

    // 현재 상태에서 점프 가능한지 확인(코요테 타임/다단 점프 포함)
    private bool CanJumpNow()
    {
        if (currentJumpCount <= 0) return false;

        if (isGrounded) return true;

        if (coyoteTimer > 0f) return true;

        // 공중 추가 점프는 첫 점프를 소모했을 때만 허용
        return currentJumpCount < GameManager.Instance.playerMaxJumpCount;
    }

    // 점프 입력 버퍼를 실제 점프로 소비
    private bool TryConsumeBufferedJump()
    {
        if (jumpBufferTimer <= 0f) return false;
        if (!CanJumpNow()) return false;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, GameManager.Instance.playerJumpForce);
        currentJumpCount--;

        isJumpHeld = true;
        jumpHoldTimer = 0f;
        jumpBufferTimer = 0f;

        // 코요테 타임으로 점프했다면 즉시 소진
        if (!isGrounded)
        {
            coyoteTimer = 0f;
            hasBurnedGroundJumpAfterCoyote = true;
        }

        return true;
    }

    // 머리 위 발판 모서리 걸림 시 좌우로 살짝 밀어주는 보정
    private void HandleCornerCorrection()
    {
        if (bodyCollider == null) return;
        if (isGrounded) return;
        if (rb.linearVelocity.y <= 0f) return;

        cornerCorrectionTimer -= Time.fixedDeltaTime;
        if (cornerCorrectionTimer > 0f) return;

        Bounds b = bodyCollider.bounds;
        float skin = 0.02f;
        Vector2 topLeft = new Vector2(b.min.x + skin, b.max.y);
        Vector2 topRight = new Vector2(b.max.x - skin, b.max.y);
        int ceilingMask = groundLayer | thinFloorLayer;

        bool hitLeft = Physics2D.Raycast(topLeft, Vector2.up, cornerCorrectionCheckDistance, ceilingMask);
        bool hitRight = Physics2D.Raycast(topRight, Vector2.up, cornerCorrectionCheckDistance, ceilingMask);

        // 한쪽만 걸렸을 때 반대쪽으로 밀기
        if (hitLeft == hitRight) return;

        float pushX = hitLeft ? cornerCorrectionPushX : -cornerCorrectionPushX;
        rb.position += new Vector2(pushX, 0f);
        cornerCorrectionTimer = cornerCorrectionCooldown;
    }

    // 현재 속도 비율(0~1)에 따라 가속/감속 계수 계산
    private float GetHorizontalControlRate(bool accelerating, bool grounded, float speed01)
    {
        speed01 = Mathf.Clamp01(speed01);

        if (grounded)
        {
            if (accelerating)
                return Mathf.Lerp(groundAccelAtLowSpeed, groundAccelAtHighSpeed, speed01);
            return Mathf.Lerp(groundDecelAtLowSpeed, groundDecelAtHighSpeed, speed01);
        }

        if (accelerating)
            return Mathf.Lerp(airAccelAtLowSpeed, airAccelAtHighSpeed, speed01);
        return Mathf.Lerp(airDecelAtLowSpeed, airDecelAtHighSpeed, speed01);
    }

    // 하강 시작 후 0.5초 내 "아래" 신규 입력을 받으면 강화 하강 전환
    private void HandleEmpoweredDownJumpInput()
    {
        if (!empowerWindowActive || isEmpoweredDownJump)
        {
            wasDownInputHeld = moveInput.y < -0.1f;
            return;
        }

        float elapsed = Time.time - downJumpStartTime;
        if (elapsed > empoweredDownJumpWindow)
        {
            empowerWindowActive = false;
            if (debugEmpoweredDownJump)
                Debug.Log($"[DownJump] Window expired (elapsed={elapsed:F3})");

            wasDownInputHeld = moveInput.y < -0.1f;
            return;
        }

        bool isDownHeld = moveInput.y < -0.1f;
        bool downPressedThisFrame = isDownHeld && !wasDownInputHeld;

        if (downPressedThisFrame)
        {
            isEmpoweredDownJump = true;
            empowerWindowActive = false;

            // 강화 하강 전환 즉시 하강 속도를 보정해서 체감이 바로 나게 함
            if (rb.linearVelocity.y < 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * empoweredDownJumpFallSpeedMultiplier);

            if (debugEmpoweredDownJump)
                Debug.Log($"[DownJump] Empowered ON at elapsed={elapsed:F3}");
        }

        wasDownInputHeld = isDownHeld;
    }

    // 하향 점프 중 시작 발판(무시 중 콜라이더)은 지면 판정에서 제외
    private Collider2D FindGroundCollider()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundLayer | thinFloorLayer);
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (isDownJumping && ignoredThinFloorCollider != null && hit == ignoredThinFloorCollider) continue;
            return hit;
        }

        return null;
    }

    // 강화 하향 점프 착지 충격
    private void ApplyEmpoweredDownJumpImpact()
    {
        Vector2 center = (Vector2)transform.position + empoweredDownJumpImpactOffset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, empoweredDownJumpImpactSize, 0f, enemyLayer);

        HashSet<int> damagedRootIds = new HashSet<int>();
        int damagedCount = 0;

        foreach (var hit in hits)
        {
            if (hit == null) continue;

            EnemyController enemy = hit.GetComponentInParent<EnemyController>();
            if (enemy != null)
            {
                int rootId = enemy.gameObject.GetInstanceID();
                if (damagedRootIds.Add(rootId))
                {
                    enemy.TakeDamage(empoweredDownJumpDamage, empoweredDownJumpKnockbackPower);
                    damagedCount++;

                    if (debugEmpoweredDownJump)
                        Debug.Log($"[DownJump] Hit Enemy: {enemy.gameObject.name}, dmg={empoweredDownJumpDamage}, kb={empoweredDownJumpKnockbackPower}");
                }
                continue;
            }

            MidBossController midBoss = hit.GetComponentInParent<MidBossController>();
            if (midBoss != null)
            {
                int rootId = midBoss.gameObject.GetInstanceID();
                if (damagedRootIds.Add(rootId))
                {
                    midBoss.TakeDamage(empoweredDownJumpDamage, empoweredDownJumpKnockbackPower);
                    damagedCount++;

                    if (debugEmpoweredDownJump)
                        Debug.Log($"[DownJump] Hit MidBoss: {midBoss.gameObject.name}, dmg={empoweredDownJumpDamage}, kb={empoweredDownJumpKnockbackPower}");
                }
            }
        }

        if (debugEmpoweredDownJump)
            Debug.Log($"[DownJump] Impact summary: colliders={hits.Length}, targetsDamaged={damagedCount}, center={center}, size={empoweredDownJumpImpactSize}");
    }

    // 이동 지연 적용(중복 시 더 긴 시간 유지)
    private void ApplyMoveDelay(float delay)
    {
        float targetEnd = Time.time + Mathf.Max(0f, delay);
        if (!isMoveDelayed)
        {
            isMoveDelayed = true;
            moveInput.x = 0f;
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            moveDelayEndTime = targetEnd;
            return;
        }

        moveDelayEndTime = Mathf.Max(moveDelayEndTime, targetEnd);
    }

    // 착지 시 낙하 데미지/스턴 처리(강화 하강 착지는 제외)
    private void HandleLandingFallDamage()
    {
        if (!isTrackingFall)
        {
            ignoreFallDamageOnNextLanding = false;
            return;
        }

        float fallenHeight = fallStartY - transform.position.y;

        if (ignoreFallDamageOnNextLanding)
        {
            if (debugEmpoweredDownJump)
                Debug.Log($"[Fall] ignored after empowered landing (fallen={fallenHeight:F2})");

            ignoreFallDamageOnNextLanding = false;
            isTrackingFall = false;
            return;
        }

        if (!isDownJumping && fallenHeight >= fallDamageMinHeight)
        {
            currentHp -= fallDamageAmount;
            ApplyMoveDelay(fallDamageStunDelay);

            if (debugEmpoweredDownJump)
                Debug.Log($"[Fall] damage applied: fallen={fallenHeight:F2}, dmg={fallDamageAmount}, stun={fallDamageStunDelay}, hp={currentHp:F1}");

            if (currentHp <= 0f)
                GameOver();
        }

        isTrackingFall = false;
    }

    // 디버그 시각화: 현재 무기 공격 박스
    private void OnDrawGizmos()
    {
        // 에디터(플레이 전)에서도 보이도록 Rigidbody2D null 체크로 막지 않음
        float facing = transform.localScale.x;
        if (Mathf.Approximately(facing, 0f)) facing = 1f;

        float dir = Mathf.Sign(facing);
        Vector2 size = GetWeaponAttackSize();
        Vector3 pos = transform.position + Vector3.right * dir * GetWeaponOffset();

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(pos, new Vector3(size.x, size.y, 0));

        Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.8f);
        Vector3 impactCenter = transform.position + (Vector3)empoweredDownJumpImpactOffset;
        Gizmos.DrawWireCube(impactCenter, new Vector3(empoweredDownJumpImpactSize.x, empoweredDownJumpImpactSize.y, 0f));
    }

    // 물리 프레임 이동 처리
    void FixedUpdate()
    {
        if (isDashing || isKnockback) return;

        if (isDownJumping)
        {
            // 하향 점프 중 확장 포인트
            return;
        }

        if (isMoveDelayed)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        // 넉백 중/해제 직후 관성 제거 목적(수평 속도 잔여 방지)
        if (!isKnockback && Mathf.Abs(moveInput.x) <= 0.01f && !isDownJumping)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        HandleCornerCorrection();

        float maxSpeed = GameManager.Instance.playerMoveSpeed;
        float targetSpeedX = moveInput.x * maxSpeed;

        // 관성/미끄러짐 완전 제거: 항상 즉시 목표 속도로 고정
        if (Mathf.Abs(moveInput.x) <= 0.01f)
            targetSpeedX = 0f;

        rb.linearVelocity = new Vector2(targetSpeedX, rb.linearVelocity.y);
    }
}
