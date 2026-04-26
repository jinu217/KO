using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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
    private Rigidbody2D rb;
    private Vector2 moveInput;

    [Header("이동 및 점프 설정")]
    public float moveSpeed = 5f;
    public float jumpForce = 15f;
    public int maxJumpCount = 1;
    private int currentJumpCount;

    [Header("대쉬(Dash) 설정")]
    public float dashVelocity = 20f;
    public float dashTime = 0.2f;
    public float dashCooldown = 1f;
    private bool isDashing;
    private bool canDash = true;

    [Header("하향 점프(Down Jump) 설정")]
    public LayerMask thinFloorLayer; // Ground2 레이어
    private bool isDownJumping;

    [Header("지면 체크(Ground Check)")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;
    public LayerMask groundLayer;
    private bool isGrounded;
    private int currentGroundOrder;

    [Header("전투 설정")]
    public LayerMask enemyLayer;
    public WeaponType currentWeapon = WeaponType.Sword;
    public float currentHp;
    private bool isKnockback;

    private float lastHorizontalDirection = 1f;
    private float lastAttackTime;
    private float originalGravity;

    // -----------------------------------------------------------
    // 초기화 및 업데이트
    // -----------------------------------------------------------
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravity = rb.gravityScale;
        lastAttackTime = -999f;
    }

    void Start()
    {
        currentHp = GameManager.Instance.playerMaxHp;
    }

    void Update()
    {
        // 1. 지면 체크
        Collider2D groundCollider = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer | thinFloorLayer);
        isGrounded = groundCollider != null;
        if (isGrounded && groundCollider.TryGetComponent<SpriteRenderer>(out SpriteRenderer gSr))
            currentGroundOrder = gSr.sortingOrder;

        // 2. 상태 리셋 로직
        if (isDownJumping)
        {
            // 하향 점프 중 지면에 닿으면 리셋 (Y 속도가 거의 없을 때)
            if (isGrounded && rb.linearVelocity.y < -0.1f)
                ResetDownJump();
        }

        if (isGrounded && rb.linearVelocity.y <= 0.1f && !isDownJumping)
            currentJumpCount = maxJumpCount;
    }

    // -----------------------------------------------------------
    // 이동 제어 (Input System)
    // -----------------------------------------------------------
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
        if (isDashing || isKnockback) return;

        if (moveInput.x != 0) UpdateFacingDirection();
    }

    // -----------------------------------------------------------
    // 점프 및 하향 점프
    // -----------------------------------------------------------
    public void OnJump(InputAction.CallbackContext context)
    {
        if (isDashing || isKnockback || !context.started) return;

        // 하향 점프 시도 (아래 키 + 점프)
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

    void ResetDownJump()
    {
        isDownJumping = false;
        // 플레이어와 일반 지면 충돌 복구
        Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Ground"), false);
    }

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

    // -----------------------------------------------------------
    // 대쉬 (Dash)
    // -----------------------------------------------------------
    public void OnDash(InputAction.CallbackContext context)
    {
        if (isKnockback) return;
        if (context.started && canDash && !isDashing) StartCoroutine(DashAction());
    }

    private IEnumerator DashAction()
    {
        canDash = false;
        isDashing = true;
        float pG = rb.gravityScale;
        rb.gravityScale = 0f;

        float dD = moveInput.x != 0 ? Mathf.Sign(moveInput.x) : lastHorizontalDirection;
        rb.linearVelocity = new Vector2(dD * dashVelocity, 0f);

        yield return new WaitForSeconds(dashTime);

        rb.gravityScale = pG;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // -----------------------------------------------------------
    // 전투 시스템 (공격 및 피격)
    // -----------------------------------------------------------
    public void OnAttack(InputAction.CallbackContext context)
    {
        if (!context.started || isDashing || isKnockback) return;
        if (Time.time < lastAttackTime + (1f / GameManager.Instance.playerAttackSpeed)) return;

        Attack();
        lastAttackTime = Time.time;
    }

    void Attack()
    {
        float dmg = GameManager.Instance.playerAttackDamage;
        if (UnityEngine.Random.value < GameManager.Instance.playerCriticalChance)
            dmg *= GameManager.Instance.playerCriticalMultiplier;

        float dir = Mathf.Sign(transform.localScale.x);
        Vector2 sz = GetWeaponAttackSize();
        Vector2 pos = (Vector2)transform.position + Vector2.right * dir * GetWeaponOffset();

        Collider2D[] hits = Physics2D.OverlapBoxAll(pos, sz, 0f, enemyLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<EnemyController>(out var enemy))
                enemy.TakeDamage(dmg, GetWeaponKnockbackPower());
        }
    }

    public void TakeDamage(float damage, Vector2 enemyPos)
    {
        currentHp -= damage;
        Debug.Log(gameObject.name + " 피격! 남은 HP: " + currentHp);

        // 넉백 로직 시작
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

    private IEnumerator KnockbackRoutine()
    {
        yield return new WaitForSeconds(0.2f);
        isKnockback = false;
    }

    public void TakeDamage(float damage)
    {
        float dir = (transform.localScale.x > 0) ? -1f : 1f;
        TakeDamage(damage, (Vector2)transform.position + new Vector2(-dir, 0));
    }

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

    // -----------------------------------------------------------
    // 무기 데이터 및 보조 기능
    // -----------------------------------------------------------
    private void UpdateFacingDirection()
    {
        lastHorizontalDirection = Mathf.Sign(moveInput.x);
        transform.localScale = new Vector3(Mathf.Sign(moveInput.x), 1, 1);
    }

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

    void GameOver() { SceneManager.LoadScene(SceneManager.GetActiveScene().name); }

    private void OnDrawGizmos()
    {
        if (rb == null) return;
        float dir = Mathf.Sign(transform.localScale.x);
        Vector2 sz = GetWeaponAttackSize();
        Vector3 pos = transform.position + Vector3.right * dir * GetWeaponOffset();
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(pos, new Vector3(sz.x, sz.y, 0));
    }

    void FixedUpdate()
    {
        if (isDashing || isKnockback) return;

        if (isDownJumping)
        {
            // 하향 점프 시의 물리 제어는 Routine에서 수행하거나 필요시 여기서 확장
        }
        else
        {
            // 일반 이동
            rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);
        }
    }
}