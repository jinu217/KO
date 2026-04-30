using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// [플레이어] 이동/점프/대쉬/공격/피격/인벤토리/포션을 담당하는 플레이어 컨트롤러
public enum WeaponType
{
    Sword,
    Mace,
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
    public float moveSpeed = 5f;   // 좌우 이동 속도
    public float jumpForce = 15f;  // 점프 초기 속도(힘)
    public int maxJumpCount = 1;   // 최대 점프 횟수(1=단점프, 2=더블점프)
    private int currentJumpCount;  // 남은 점프 횟수

    // -------------------- 대쉬 --------------------
    [Header("대쉬(Dash) 설정")]
    public float dashVelocity = 20f; // 대쉬 속도
    public float dashTime = 0.2f;    // 대쉬 지속 시간
    public float dashCooldown = 1f;  // 대쉬 재사용 대기 시간
    private bool isDashing;          // 대쉬 중 여부
    private bool canDash = true;     // 대쉬 가능 여부

    // -------------------- 하향 점프 --------------------
    [Header("하향 점프(Down Jump) 설정")]
    public LayerMask thinFloorLayer; // 점프스루 발판 레이어(Ground2)
    private bool isDownJumping;      // 하향 점프 진행 중 여부

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

        currentHp = GameManager.Instance.playerMaxHp;
        inventoryUI = FindFirstObjectByType<InventoryUI>();
    }

    // 매 프레임 상태 업데이트(지면 체크, 점프 리셋)
    void Update()
    {
        // 인벤토리 열림 중에는 조작 로직 중지
        if (isInventoryOpen) return;

        // 1) 지면 체크(일반 지면 + 점프스루 발판)
        Collider2D groundCollider = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer | thinFloorLayer);
        isGrounded = groundCollider != null;

        if (isGrounded && groundCollider.TryGetComponent<SpriteRenderer>(out SpriteRenderer gSr))
            currentGroundOrder = gSr.sortingOrder;

        // 2) 하향 점프 상태 리셋
        if (isDownJumping)
        {
            if (isGrounded && rb.linearVelocity.y < -0.1f)
                ResetDownJump();
        }

        // 3) 지면 착지 시 점프 횟수 복구
        if (isGrounded && rb.linearVelocity.y <= 0.1f && !isDownJumping)
            currentJumpCount = maxJumpCount;
    }

    // -------------------- 입력 콜백: 이동 --------------------
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        if (isDashing || isKnockback || isInventoryOpen) return;

        if (moveInput.x != 0) UpdateFacingDirection();
    }

    // -------------------- 입력 콜백: 점프/하향 점프 --------------------
    public void OnJump(InputAction.CallbackContext context)
    {
        if (isDashing || isKnockback || isInventoryOpen || !context.started) return;

        // 하향 점프: 아래 입력 + 지상 + Ground2 위
        if (isGrounded && moveInput.y < -0.1f)
        {
            Collider2D hit = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, thinFloorLayer);
            if (hit != null)
            {
                StartCoroutine(ThinFloorDownJumpRoutine());
                return;
            }
        }

        // 일반 점프
        if (currentJumpCount > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            currentJumpCount--;
        }
    }

    // 하향 점프 종료 시 충돌 복구
    void ResetDownJump()
    {
        isDownJumping = false;
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Ground"), false);
    }

    // Ground2 통과용 하향 점프 루틴
    private IEnumerator ThinFloorDownJumpRoutine()
    {
        isDownJumping = true;
        int pL = LayerMask.NameToLayer("Player"), tL = LayerMask.NameToLayer("Ground2");

        Physics2D.IgnoreLayerCollision(pL, tL, true);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -2f);

        yield return new WaitForSeconds(0.45f);

        Physics2D.IgnoreLayerCollision(pL, tL, false);
        isDownJumping = false;
    }

    // -------------------- 입력 콜백: 대쉬 --------------------
    public void OnDash(InputAction.CallbackContext context)
    {
        if (isKnockback || isInventoryOpen) return;
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
        rb.linearVelocity = new Vector2(dashDir * dashVelocity, 0f);

        yield return new WaitForSeconds(dashTime);

        rb.gravityScale = prevGravity;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // -------------------- 입력 콜백: 공격 --------------------
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.started || isDashing || isKnockback || isInventoryOpen) return;
        if (Time.time < lastAttackTime + (1f / GameManager.Instance.playerAttackSpeed)) return;

        Attack();
        lastAttackTime = Time.time;
    }

    // -------------------- 입력 콜백: 인벤토리 --------------------
    public void OnInventory(InputAction.CallbackContext context)
    {
        if (!context.started) return;

        isInventoryOpen = !isInventoryOpen;
        OpenInventory();
    }

    // -------------------- 입력 콜백: 포션 --------------------
    public void OnHPPotion(InputAction.CallbackContext context)
    {
        if (!context.started || isInventoryOpen) return;
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

        Collider2D[] hits = Physics2D.OverlapBoxAll(pos, size, 0f, enemyLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<EnemyController>(out var enemy))
                enemy.TakeDamage(dmg, GetWeaponKnockbackPower());
        }
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

    // 피격 처리(방어력 적용 + 넉백 + 게임오버 체크)
    public void TakeDamage(float damage, Vector2 enemyPos)
    {
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

        if (currentHp <= 0) GameOver();
    }

    // 오버로드: 적 위치 없이 들어온 데미지 처리용
    public void TakeDamage(float damage)
    {
        float dir = (transform.localScale.x > 0) ? -1f : 1f;
        Vector2 enemyPos = (Vector2)transform.position + new Vector2(-dir * 2f, 0);
        TakeDamage(damage, enemyPos);
    }

    // 넉백 상태 해제 루틴
    private IEnumerator KnockbackRoutine()
    {
        yield return new WaitForSeconds(0.2f);
        isKnockback = false;
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
            case WeaponType.Mace: return new Vector2(1.2f, 2.0f);
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
            case WeaponType.Mace: return 0.8f;
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
            case WeaponType.Mace: return 10f;
            case WeaponType.Spear: return 3f;
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

    // 디버그 시각화: 현재 무기 공격 박스
    private void OnDrawGizmos()
    {
        if (rb == null) return;

        float dir = Mathf.Sign(transform.localScale.x);
        Vector2 size = GetWeaponAttackSize();
        Vector3 pos = transform.position + Vector3.right * dir * GetWeaponOffset();

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(pos, new Vector3(size.x, size.y, 0));
    }

    // 물리 프레임 이동 처리
    void FixedUpdate()
    {
        if (isDashing || isKnockback) return;

        if (isDownJumping)
        {
            // 하향 점프 중 확장 포인트
        }
        else
        {
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        }
    }
}
