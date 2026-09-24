using System.Collections;
using UnityEngine;

// Basic Attack + Skills 데이터만 실행하는 중간보스 공통 컨트롤러
public class MidBossController : MonoBehaviour, IDamageable, IBossSkillContext
{
    private enum AttackGizmoSelection
    {
        All,
        BasicAttack,
        Skill1,
        Skill2,
        Skill3,
        Skill4,
        Skill5
    }

    [Header("데이터")]
    [Tooltip("보스의 체력, 이동, 기본 공격과 일반 스킬 설정이 들어 있는 데이터 에셋입니다.")]
    public BossDataBase data;
    [Header("레이어")]
    [Tooltip("공격 판정과 플레이어 탐색에 사용할 플레이어 레이어입니다.")]
    public LayerMask playerLayer;
    [Tooltip("벽 감지와 착지 판정에 사용할 지형 레이어입니다.")]
    public LayerMask wallLayer;
    [Header("감지 위치")]
    [Tooltip("보스 전방의 벽을 확인할 기준 위치입니다. 보스의 자식 오브젝트인 벽 감지 위치를 연결합니다.")]
    public Transform wallCheck;
    [SerializeField, Tooltip("플레이어와의 X축 차이가 이 값보다 작으면 방향을 전환하지 않습니다.")]
    private float turnDeadZone = 0.2f;
    [Header("기즈모")]
    [SerializeField, Tooltip("공격 범위 기즈모 표시 여부")] private bool showGizmos = true;
    [SerializeField, Tooltip("전체, 기본 공격 또는 확인할 스킬 하나를 선택")]
    private AttackGizmoSelection attackGizmoSelection = AttackGizmoSelection.All;
    [SerializeField, Tooltip("감지 범위, 추적 범위와 벽 감지선도 함께 표시")]
    private bool showDetectionGizmos;
    [Header("디버그")]
    [SerializeField, Tooltip("기본 공격과 스킬의 시작·종료 및 쿨타임 변경 내용을 콘솔에 출력합니다.")]
    private bool logSkillUsage = true;
    [SerializeField, Tooltip("실행 중 확인하기 위한 현재 체력입니다. 게임 시작 시 데이터의 최대 체력으로 초기화됩니다.")]
    protected float currentHp;

    protected Rigidbody2D rb;
    protected SpriteRenderer spriteRenderer;
    protected Collider2D bossCollider;
    protected Transform playerTransform;
    protected int lookDirection = 1;

    protected bool isChasing;
    protected bool isAttacking;
    protected bool isDead;
    private bool healthUIShown;
    public bool IsDead => isDead;
    public event System.Action Defeated;
    protected int currentPhase = 1;
    protected int currentSkillIndex = -1;
    private bool buffActive;
    private bool initialCooldownsApplied;
    private float lastBasicTime = -999f;
    private float[] lastSkillTimes;
    private int[] skillUseCounts;
    private bool[] distanceCooldownReduced;
    private Coroutine buffRoutine;
    private float moveMultiplier = 1f;
    private float damageMultiplier = 1f;
    private float attackSpeedMultiplier = 1f;
    private float cooldownMultiplier = 1f;
    private float basicCooldownMultiplier = 1f;
    private float defenseBonus;
    private float moveAdd;
    private float damageAdd;
    private float attackSpeedAdd;
    private float cooldownAdd;
    private float basicCooldownAdd;
    private float maxHpBonus;

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
            Debug.LogError($"{name}에 MidBossData가 연결되지 않았습니다.");
            enabled = false;
            return;
        }

        currentHp = data.maxHp;
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            Collider2D playerCollider = player.GetComponent<Collider2D>();
            if (bossCollider != null && playerCollider != null)
                Physics2D.IgnoreCollision(bossCollider, playerCollider, true);
        }

        lastSkillTimes = new float[data.skills != null ? data.skills.Count : 0];
        // 실제 초기 쿨타임은 플레이어를 최초 인식하는 순간 적용한다.
        lastBasicTime = -999f;
    }

    protected virtual void Update()
    {
        if (data == null || isDead) return;
        FindAndDetectPlayer();
        UpdatePhase();
        ApplyInitialCooldownsOnFirstDetection();
        // 스킬 실행 중 이동은 BossSkillExecutor가 담당합니다.
        if (isAttacking) return;
        if (UpdatePriorityBehavior()) { StopMove(); return; }
        if (!CanRunCombatAI()) { StopMove(); return; }
        if (!isChasing || playerTransform == null) { StopMove(); return; }

        FacePlayer();
        BossSkillData skill = GetReadySkill(out int index) ?? GetBasicAttackData();
        if (!InCastRange(skill)) return;
        StopMove();

        if (index >= 0) StartCoroutine(ExecuteSkill(skill, index));
        else if (skill != null && Time.time >= lastBasicTime + AdjustBasicCooldown(skill.cooldown))
            StartCoroutine(ExecuteSkill(skill, -1));
    }

    protected virtual void FixedUpdate()
    {
        if (data == null || isDead || isAttacking || !CanRunCombatAI() || !isChasing || playerTransform == null) return;
        BossSkillData skill = GetReadySkill(out _) ?? GetBasicAttackData();
        if (InCastRange(skill)) { StopMove(); return; }
        if (IsWallInFront()) { StopMove(); return; }
        float moveSpeed = (data.chaseSpeed * moveMultiplier + moveAdd) * GetExternalMoveMultiplier();
        rb.linearVelocity = new Vector2(lookDirection * Mathf.Max(0f, moveSpeed), rb.linearVelocity.y);
    }

    private void FindAndDetectPlayer()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }
        if (playerTransform == null) return;
        float distance = Vector2.Distance(transform.position, playerTransform.position);
        if (!isChasing && distance <= data.detectRange)
        {
            isChasing = true;

            // 처음 조우했을 때 HP UI 표시
            if (!healthUIShown)
            {
                healthUIShown = true;
                ShowHealthUI();
            }
        }
        else if (isChasing && distance > data.chaseRange)
        {
            isChasing = false;
        }
    }

    // 최초 인식 시 일반 공격은 즉시 준비하고, 나머지 스킬은 쿨타임이 50% 지난 상태로 시작한다.
    private void ApplyInitialCooldownsOnFirstDetection()
    {
        if (initialCooldownsApplied || !isChasing) return;

        initialCooldownsApplied = true;
        lastBasicTime = -999f;
        EnsureTimers();

        for (int i = 0; i < lastSkillTimes.Length; i++)
        {
            BossSkillData skill = GetSkillData(i);
            float cooldown = skill != null ? AdjustCooldown(skill.cooldown) : 0f;
            lastSkillTimes[i] = Time.time - cooldown * 0.5f;
        }
    }

    private void FacePlayer()
    {
        float delta = playerTransform.position.x - transform.position.x;
        if (Mathf.Abs(delta) <= turnDeadZone) return;
        lookDirection = delta > 0f ? 1 : -1;
        ApplyFacingDirection();
    }

    // 보스별 시각 하이어라키에 맞게 방향 전환 방법을 재정의합니다.
    protected virtual void ApplyFacingDirection()
    {
        if (spriteRenderer != null) spriteRenderer.flipX = lookDirection < 0;
    }

    private BossSkillData GetReadySkill(out int index)
    {
        index = -1;
        if (data.skills == null) return null;
        EnsureTimers();
        for (int i = 0; i < data.skills.Count; i++)
        {
            BossSkillData skill = GetSkillData(i);
            if (skill == null || currentPhase < skill.minimumPhase) continue;
            float remaining = lastSkillTimes[i] + AdjustCooldown(skill.cooldown) - Time.time;
            bool normalReady = remaining <= 0f;
            bool forceReady = skill.forceCastMinDistance > 0f
                && skill.forceCastRemainingCooldown > 0f
                && remaining <= skill.forceCastRemainingCooldown
                && Vector2.Distance(transform.position, playerTransform.position) >= skill.forceCastMinDistance;
            if (!normalReady && !forceReady) continue;
            if ((buffActive || IsExternalBuffBlocking()) && skill.HasBuff) continue;
            if (!InCastRange(skill)) continue;
            index = i;
            return skill;
        }
        return null;
    }

    private bool InCastRange(BossSkillData skill)
    {
        if (skill == null || playerTransform == null) return false;
        return skill.castRange <= 0f || Vector2.Distance(transform.position, playerTransform.position) <= skill.castRange;
    }

    private IEnumerator ExecuteSkill(BossSkillData skill, int index)
    {
        isAttacking = true;
        currentSkillIndex = index;
        StopMove();
        if (index < 0) lastBasicTime = Time.time;
        else
        {
            EnsureTimers();
            lastSkillTimes[index] = Time.time;
            skillUseCounts[index]++;
            distanceCooldownReduced[index] = false;
        }
        LogSkillDebug(index < 0 ? "기본 공격 시작" : $"{index + 1}스킬 시작", skill);
        OnSkillExecutionStarted(skill, index);
        yield return BossSkillExecutor.Execute(skill, this);
        yield return WaitForSkillAnimation(skill, index);
        OnSkillExecutionFinished(index);
        LogSkillDebug(index < 0 ? "기본 공격 종료" : $"{index + 1}스킬 종료", skill);
        currentSkillIndex = -1;
        isAttacking = false;
    }

    private void EnsureTimers()
    {
        int count = data.skills != null ? data.skills.Count : 0;
        if (lastSkillTimes == null || lastSkillTimes.Length != count)
            lastSkillTimes = new float[count];
        if (skillUseCounts == null || skillUseCounts.Length != count)
            skillUseCounts = new int[count];
        if (distanceCooldownReduced == null || distanceCooldownReduced.Length != count)
            distanceCooldownReduced = new bool[count];
    }

    protected bool IsWallInFront()
    {
        if (wallCheck == null) return false;
        return Physics2D.Raycast(wallCheck.position, Vector2.right * lookDirection, data.wallCheckDistance, wallLayer).collider != null;
    }

    protected void StopMove()
    {
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
    }

    private float AdjustCooldown(float value)
        => Mathf.Max(0f, (value * cooldownMultiplier + cooldownAdd) * GetExternalCooldownMultiplier());
    private float AdjustBasicCooldown(float value)
        => Mathf.Max(0f, (value * basicCooldownMultiplier + basicCooldownAdd) * GetExternalBasicCooldownMultiplier());

    public virtual void TakeDamage(float damage, Vector2 sourcePos, bool applyKnockback = false, bool applyIFrame = false)
    {
        if (isDead) return;
        currentHp -= Mathf.Max(0f, damage - defenseBonus);
        UpdateHealthUI();
        isChasing = true;
        if (currentHp <= 0f) Die();
    }

    protected virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        RecordKill();
        HideHealthUI();
        StopAllCoroutines();
        StopMove();
        gameObject.SetActive(false);
        Defeated?.Invoke();
    }

    public Transform BossTransform => transform;
    public Rigidbody2D BossBody => rb;
    public LayerMask PlayerLayer => playerLayer;
    public int LookDirection => lookDirection;
    public Vector2 PlayerPosition => playerTransform != null ? playerTransform.position : transform.position;
    public bool IsWallAhead() => IsWallInFront();
    public bool IsGrounded()
    {
        if (bossCollider == null) return false;
        Bounds bounds = bossCollider.bounds;
        Vector2 origin = new Vector2(bounds.center.x, bounds.min.y + 0.03f);
        float width = Mathf.Max(0.1f, bounds.size.x * 0.75f);
        return Physics2D.BoxCast(origin, new Vector2(width, 0.08f), 0f, Vector2.down, 0.12f, wallLayer).collider != null;
    }
    public float GetDistanceToGround()
    {
        if (bossCollider == null) return 0f;
        Bounds bounds = bossCollider.bounds;
        RaycastHit2D hit = Physics2D.Raycast(new Vector2(bounds.center.x, bounds.min.y + 0.03f), Vector2.down, 100f, wallLayer);
        return hit.collider != null ? hit.distance : 0f;
    }
    public virtual void RefreshSkillDirection()
    {
        // 보스 공통: 시전 시작 방향을 유지. 추적 이동은 Update/FixedUpdate의
        // isAttacking 검사로 차단하며 돌진 등 실행기 자체 이동은 허용합니다.
        if (isAttacking) return;
        if (playerTransform != null) FacePlayer();
    }
    public virtual void OnSkillAttackHit(BossAttackStepData step) { }
    protected virtual BossSkillData GetBasicAttackData() => data.basicAttack;
    protected virtual void OnSkillExecutionStarted(BossSkillData skill, int index) { }
    // 게임플레이 효과 실행이 끝나도 필요한 마무리 모션 동안 AI 잠금을 유지합니다.
    protected virtual IEnumerator WaitForSkillAnimation(BossSkillData skill, int index) { yield break; }
    protected virtual void OnSkillExecutionFinished(int index) { }
    protected virtual BossSkillData GetSkillData(int index)
    {
        if (data.skills == null || index < 0 || index >= data.skills.Count) return null;
        return data.skills[index];
    }
    protected virtual float GetExternalMoveMultiplier() => 1f;
    protected virtual float GetExternalAttackSpeedMultiplier() => 1f;
    protected virtual float GetExternalDamageMultiplier() => 1f;
    protected virtual float GetExternalCooldownMultiplier() => 1f;
    protected virtual float GetExternalBasicCooldownMultiplier() => 1f;
    protected virtual bool IsExternalBuffBlocking() => false;
    protected virtual bool CanRunCombatAI() => true;
    protected virtual bool UpdatePriorityBehavior() => false;
    protected void ResetAllSkillCooldowns()
    {
        lastBasicTime = -999f;
        EnsureTimers();
        for (int i = 0; i < lastSkillTimes.Length; i++) lastSkillTimes[i] = -999f;
    }

    // 보스 전용 규칙에서 호출한다. 해당 스킬을 한 번 이상 사용한 뒤,
    // 한 쿨타임 주기당 한 번만 현재 남은 쿨타임을 지정 배율로 줄인다.
    protected bool TryReduceRemainingSkillCooldownByDistance(int skillIndex, float minimumDistance, float remainingMultiplier = 0.5f)
    {
        if (data == null || playerTransform == null || isDead || isAttacking)
            return false;

        EnsureTimers();
        if (skillIndex < 0 || skillIndex >= lastSkillTimes.Length
            || skillUseCounts[skillIndex] <= 0 || distanceCooldownReduced[skillIndex])
            return false;

        BossSkillData skill = GetSkillData(skillIndex);
        if (skill == null || Vector2.Distance(transform.position, playerTransform.position) < minimumDistance)
            return false;

        float cooldown = AdjustCooldown(skill.cooldown);
        float remaining = lastSkillTimes[skillIndex] + cooldown - Time.time;
        if (remaining <= 0f)
            return false;

        float multiplier = Mathf.Clamp01(remainingMultiplier);
        float reducedRemaining = remaining * multiplier;
        lastSkillTimes[skillIndex] = Time.time + reducedRemaining - cooldown;
        distanceCooldownReduced[skillIndex] = true;
        if (logSkillUsage)
        {
            float distance = Vector2.Distance(transform.position, playerTransform.position);
            Debug.Log($"[BossSkill] {name} | {skillIndex + 1}스킬 거리 쿨타임 감소 | "
                + $"거리 {distance:F2}/{minimumDistance:F2} | 남은 시간 {remaining:F2}s → {reducedRemaining:F2}s", this);
        }
        return true;
    }

    protected void LogSkillDebug(string state, BossSkillData skill)
    {
        if (!logSkillUsage || skill == null) return;
        float distance = playerTransform != null
            ? Vector2.Distance(transform.position, playerTransform.position)
            : -1f;
        Debug.Log($"[BossSkill] {name} | {state} | {skill.skillName} | "
            + $"Phase {currentPhase} | 거리 {(distance >= 0f ? distance.ToString("F2") : "없음")} | 쿨타임 {skill.cooldown:F2}s", this);
    }
    protected virtual void UpdatePhase() { currentPhase = 1; }
    protected virtual void ShowHealthUI() => MidBossHealthUI.Instance?.Show(name, currentHp, data.maxHp);
    protected virtual void UpdateHealthUI() => MidBossHealthUI.Instance?.UpdateHP(currentHp);
    protected virtual void HideHealthUI() => MidBossHealthUI.Instance?.Hide();
    protected virtual void RecordKill()
    {
        if (SaveData.Instance != null) SaveData.Instance.run.midBossKillCount++;
    }
    public float GetSkillDamage(float baseDamage)
        => Mathf.Max(0f, (baseDamage * damageMultiplier + damageAdd) * GetExternalDamageMultiplier());
    public float GetSkillDelay(float baseDelay)
        => baseDelay / Mathf.Max(0.01f, (attackSpeedMultiplier + attackSpeedAdd) * GetExternalAttackSpeedMultiplier());

    public void ApplySkillBuff(BossBuffSkillData buff)
    {
        if (buff == null || buff.duration <= 0f || buff.effects == null || buff.effects.Count == 0) return;
        if (buffRoutine != null) StopCoroutine(buffRoutine);
        buffRoutine = StartCoroutine(BuffRoutine(buff));
    }

    private IEnumerator BuffRoutine(BossBuffSkillData buff)
    {
        ResetBuff();
        buffActive = true;
        foreach (BossBuffEffectData effect in buff.effects) ApplyEffect(effect);
        yield return new WaitForSeconds(buff.duration);
        ResetBuff();
        buffActive = false;
        buffRoutine = null;
    }

    private void ApplyEffect(BossBuffEffectData effect)
    {
        float factor = effect.valueMode == BossBuffValueMode.Multiply ? effect.value
            : effect.valueMode == BossBuffValueMode.Percent ? 1f + effect.value / 100f : 1f;
        switch (effect.stat)
        {
            case BossBuffStat.MoveSpeed:
                if (effect.valueMode == BossBuffValueMode.Add) moveAdd += effect.value; else moveMultiplier *= factor;
                break;
            case BossBuffStat.Damage:
                if (effect.valueMode == BossBuffValueMode.Add) damageAdd += effect.value; else damageMultiplier *= factor;
                break;
            case BossBuffStat.AttackSpeed:
                if (effect.valueMode == BossBuffValueMode.Add) attackSpeedAdd += effect.value; else attackSpeedMultiplier *= factor;
                break;
            case BossBuffStat.Cooldown:
                if (effect.valueMode == BossBuffValueMode.Add) cooldownAdd += effect.value; else cooldownMultiplier *= factor;
                break;
            case BossBuffStat.BasicAttackCooldown:
                if (effect.valueMode == BossBuffValueMode.Add) basicCooldownAdd += effect.value; else basicCooldownMultiplier *= factor;
                break;
            case BossBuffStat.Defense: defenseBonus += effect.value; break;
            case BossBuffStat.MaxHp:
                float maxHpFactor = effect.valueMode == BossBuffValueMode.Add ? 1f : factor;
                float bonus = effect.valueMode == BossBuffValueMode.Add ? effect.value : data.maxHp * (maxHpFactor - 1f);
                maxHpBonus += bonus;
                currentHp += Mathf.Max(0f, bonus);
                UpdateHealthUI();
                break;
        }
    }

    private void ResetBuff()
    {
        if (maxHpBonus != 0f)
        {
            currentHp = Mathf.Min(currentHp, data.maxHp);
            maxHpBonus = 0f;
            UpdateHealthUI();
        }
        moveMultiplier = damageMultiplier = attackSpeedMultiplier = cooldownMultiplier = 1f;
        basicCooldownMultiplier = 1f;
        defenseBonus = 0f;
        moveAdd = damageAdd = attackSpeedAdd = cooldownAdd = basicCooldownAdd = 0f;
    }

    protected virtual void OnDrawGizmos()
    {
        if (!showGizmos || data == null) return;
        if (showDetectionGizmos)
        {
            Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, data.detectRange);
            Gizmos.color = Color.red; Gizmos.DrawWireSphere(transform.position, data.chaseRange);
            if (wallCheck != null) { Gizmos.color = Color.white; Gizmos.DrawRay(wallCheck.position, Vector2.right * lookDirection * data.wallCheckDistance); }
        }

        // 기본공격과 모든 스킬의 각 타격 단계는 서로 독립된 Size/Offset으로 표시한다.
        if (attackGizmoSelection == AttackGizmoSelection.All
            || attackGizmoSelection == AttackGizmoSelection.BasicAttack)
            DrawSkillHitboxes(data.basicAttack, Color.cyan);

        if (data.skills == null) return;
        Color[] skillColors =
        {
            new Color(1f, 0.35f, 1f),
            new Color(0.2f, 1f, 0.35f),
            new Color(1f, 0.55f, 0.1f),
            new Color(0.3f, 0.65f, 1f),
            new Color(1f, 0.2f, 0.2f)
        };

        if (attackGizmoSelection == AttackGizmoSelection.All)
        {
            for (int i = 0; i < data.skills.Count; i++)
                DrawSkillHitboxes(data.skills[i], skillColors[i % skillColors.Length]);
            return;
        }

        int selectedSkillIndex = (int)attackGizmoSelection - (int)AttackGizmoSelection.Skill1;
        if (selectedSkillIndex >= 0 && selectedSkillIndex < data.skills.Count)
            DrawSkillHitboxes(data.skills[selectedSkillIndex], skillColors[selectedSkillIndex % skillColors.Length]);
    }

    private void DrawSkillHitboxes(BossSkillData skill, Color baseColor)
    {
        if (skill == null || !skill.HasActive || skill.active?.attackSteps == null) return;

        int direction = lookDirection == 0 ? 1 : lookDirection;
        int visibleHitIndex = 0;

        foreach (BossAttackStepData step in skill.active.attackSteps)
        {
            if (!HasAttackHitbox(step)) continue;

            // 같은 스킬의 연타는 밝기를 달리해 각 공격 범위를 구분한다.
            float brightness = Mathf.Lerp(0.55f, 1f, (visibleHitIndex % 3) / 2f);
            Gizmos.color = new Color(
                baseColor.r * brightness,
                baseColor.g * brightness,
                baseColor.b * brightness,
                1f
            );

            Vector3 center = transform.position + new Vector3(
                direction * step.hitboxOffset.x,
                step.hitboxOffset.y,
                0f
            );
            Gizmos.DrawWireCube(center, new Vector3(step.hitboxSize.x, step.hitboxSize.y, 0f));
            visibleHitIndex++;
        }
    }

    private static bool HasAttackHitbox(BossAttackStepData step)
    {
        if (step == null) return false;
        if (step.hitboxSize.x <= 0f || step.hitboxSize.y <= 0f) return false;

        if (step.actionType == BossActionType.PullPlayer) return true;
        if (step.attackCount <= 0) return false;

        return step.actionType == BossActionType.MeleeAttack
            || step.actionType == BossActionType.DashAttack
            || step.actionType == BossActionType.Slam
            || step.actionType == BossActionType.PersistentHitbox;
    }
}
