using UnityEngine;

// 일반 전투 흐름만 담당하며 안전 욕구의 고유 규칙은 전용 컴포넌트에 위임한다.
public sealed class Stage2_MainBoss : MainBossController, IBossSpawnHeightContext, IBossScheduledSpawnContext, IBossTimedMovementContext, IBossHitValidationContext
{
    protected override string AnimationPrefix => "MainBoss2_";
    protected override string VisualRootName => "MainBoss 2";
    protected override bool HasPhaseEntryAnimations => false;
    protected override bool UsesPhase3ActionStates => false;
    protected override string DesireAnimationAction => "Buff";
    protected override string SkillAnimationAction(int index)
    {
        switch (index)
        {
            case -1: return "Attack";
            case 0: return "Skill1";
            case 1: return "Skill2";
            case 3: return "Skill3"; // 데이터의 4스킬 = 돌 파동 클립
            default: return "Buff"; // 비활성 상태인 3/5스킬의 버프 슬롯
        }
    }
    protected override void LateUpdate()
    {
        base.LateUpdate();
        SetChasingAnimation(isChasing || (desireSkill != null && desireSkill.IsReturning));
    }
    public void PlayReturnAttackAnimation() { PlayAction("Attack", false); }
    public System.Collections.IEnumerator WaitForReturnAttackAnimation()
    {
        yield return WaitForAction("MainBoss2_Attack");
    }
    public void DesireFaceDirection(int direction)
    {
        if (isAttacking) return;
        lookDirection = direction < 0 ? -1 : 1;
        ApplyFacingDirection();
    }

    private BossSkillData timedSkill;
    private int timedSkillIndex;
    private float timedCastStart, timedDelayScale, timedTargetX;
    private int timedDirection;
    private Animator timedAnimator;
    private bool timedStateEntered, timedLanded;
    private float savedTimedGravity;
    private bool ownsTimedGravity;

    private float TimedClipSeconds()
    {
        string stateName = timedSkillIndex == 0 ? "MainBoss2_Skill1" : "MainBoss2_Skill3";
        if (timedAnimator != null && timedAnimator.isActiveAndEnabled && timedAnimator.runtimeAnimatorController != null)
        {
            var state = timedAnimator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(stateName) || state.IsName("Base Layer." + stateName))
            {
                foreach (var info in timedAnimator.GetCurrentAnimatorClipInfo(0))
                {
                    if (info.clip == null) continue;
                    timedStateEntered = true;
                    return state.normalizedTime * info.clip.length;
                }
            }
            if (timedStateEntered) return float.PositiveInfinity;
            if (Time.time - timedCastStart < 0.5f) return 0f;
        }
        return (Time.time - timedCastStart) / timedDelayScale;
    }

    public bool UsesTimedMovement(BossAttackStepData step)
    {
        if ((timedSkillIndex != 0 && timedSkillIndex != 3) || timedSkill?.active?.attackSteps == null)
            return false;
        if (step.actionType != BossActionType.Jump && step.actionType != BossActionType.Slam)
            return false;
        foreach (var candidate in timedSkill.active.attackSteps)
            if (object.ReferenceEquals(candidate, step)) return true;
        return false;
    }

    public System.Collections.IEnumerator ExecuteTimedMovement(BossAttackStepData step)
    {
        if (step.actionType == BossActionType.Slam)
        {
            if (timedLanded && !isDead)
                yield return BossSkillExecutor.ExecuteRepeatedAttacks(step, this);
            yield break;
        }
        if (rb == null) yield break;
        float takeoff = (timedSkillIndex == 0 ? 16f : 7f) / 12f;
        float apex = (timedSkillIndex == 0 ? 20f : 12f) / 12f;
        float landing = (timedSkillIndex == 0 ? 23f : 19f) / 12f;
        while (!isDead && TimedClipSeconds() < takeoff)
        {
            StopMove();
            yield return null;
        }
        if (isDead || float.IsInfinity(TimedClipSeconds())) yield break;
        Vector2 origin = rb.position;
        float distance = Mathf.Max(0f, (timedTargetX - origin.x) * timedDirection);
        float targetX = origin.x + distance * timedDirection;
        float oldGravity = rb.gravityScale;
        float gravity = Mathf.Abs(Physics2D.gravity.y * oldGravity);
        if (gravity < 0.01f) gravity = 9.81f;
        // 기존 점프 힘으로 계산한 높이를 유지하고 도달 시간만 클립에 맞춘다.
        float height = step.jumpForce.y * step.jumpForce.y / (2f * gravity);
        float apexRatio = Mathf.Clamp01(step.targetDistanceRatio);
        var filter = new ContactFilter2D();
        filter.SetLayerMask(wallLayer);
        filter.useTriggers = false;
        var hits = new RaycastHit2D[32];
        bool grounded = false;
        try
        {
            savedTimedGravity = oldGravity;
            ownsTimedGravity = true;
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            while (!isDead && rb != null)
            {
                float t = TimedClipSeconds();
                if (float.IsInfinity(t)) yield break;
                float u = Mathf.Clamp01((t - takeoff) / (apex - takeoff));
                float v = Mathf.Clamp01((t - apex) / (landing - apex));
                float y = t <= apex ? height * (2f * u - u * u) : height * (1f - v * v);
                float xRatio = t <= apex ? apexRatio * u : Mathf.Lerp(apexRatio, 1f, v);
                Vector2 target = new Vector2(Mathf.Lerp(origin.x, targetX, xRatio), origin.y + y);
                if (!grounded)
                {
                    Vector2 travel = target - rb.position;
                    float length = travel.magnitude;
                    if (length > 0.00001f)
                    {
                        int count = rb.Cast(travel / length, filter, hits, length + 0.01f);
                        float allowed = length;
                        bool hitFloor = false;
                        for (int i = 0; i < count; i++)
                        {
                            if (Vector2.Dot(travel, hits[i].normal) >= -0.00001f) continue;
                            float safe = Mathf.Max(0f, hits[i].distance - 0.01f);
                            if (safe > allowed) continue;
                            allowed = safe;
                            hitFloor = t > apex && hits[i].normal.y >= 0.35f;
                        }
                        grounded = hitFloor;
                        target = rb.position + travel.normalized * allowed;
                    }
                    rb.position = target;
                }
                rb.linearVelocity = Vector2.zero;
                if (t >= landing)
                {
                    timedLanded = grounded || IsGrounded();
                    break;
                }
                yield return new WaitForFixedUpdate();
            }
        }
        finally
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                RestoreTimedGravity();
            }
        }
    }

    private void RestoreTimedGravity()
    {
        if (!ownsTimedGravity) return;
        if (rb != null) rb.gravityScale = savedTimedGravity;
        ownsTimedGravity = false;
    }

    protected override void OnSkillExecutionFinished(int index)
    {
        if (stoneSpawnRoutine != null) StopCoroutine(stoneSpawnRoutine);
        stoneSpawnRoutine = null;
        stoneSpawnRunning = false;
        scheduledStoneStep = null;
        timedSkill = null;
        RestoreTimedGravity();
        base.OnSkillExecutionFinished(index);
    }

    private BossAttackStepData scheduledStoneStep;
    private Coroutine stoneSpawnRoutine;
    private bool stoneSpawnRunning;

    public bool IsScheduledSpawn(BossAttackStepData step)
        => object.ReferenceEquals(step, scheduledStoneStep);

    protected override void OnSkillExecutionStarted(BossSkillData skill, int index)
    {
        if (stoneSpawnRoutine != null) StopCoroutine(stoneSpawnRoutine);
        stoneSpawnRoutine = null;
        stoneSpawnRunning = false;
        scheduledStoneStep = null;
        base.OnSkillExecutionStarted(skill, index);
        timedSkillIndex = index;
        timedSkill = skill;
        timedCastStart = Time.time;
        timedDelayScale = Mathf.Max(0.001f, GetSkillDelay(1f));
        timedTargetX = playerTransform != null ? playerTransform.position.x : transform.position.x;
        timedDirection = lookDirection < 0 ? -1 : 1;
        timedAnimator = GetComponentInChildren<Animator>(true);
        timedStateEntered = false;
        timedLanded = false;
        if (index != 3 || skill?.active?.attackSteps == null) return;
        foreach (BossAttackStepData step in skill.active.attackSteps)
        {
            if (step != null && step.actionType == BossActionType.SpawnPrefab)
            {
                scheduledStoneStep = step;
                stoneSpawnRunning = true;
                stoneSpawnRoutine = StartCoroutine(SpawnStoneAtFrame19(step));
                break;
            }
        }
    }

    private System.Collections.IEnumerator SpawnStoneAtFrame19(BossAttackStepData step)
    {
        const float markerTime = 19f / 12f;
        Animator animator = GetComponentInChildren<Animator>(true);
        float fallbackTime = Time.time + GetSkillDelay(markerTime);
        bool entered = false;
        try
        {
            // 트리거가 새 상태에 적용된 다음 실제 재생 위치를 읽는다.
            yield return null;
            while (!isDead)
            {
                bool found = false;
                float clipTime = 0f;
                if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
                {
                    AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                    if (state.IsName("MainBoss2_Skill3") || state.IsName("Base Layer.MainBoss2_Skill3"))
                    {
                        foreach (AnimatorClipInfo info in animator.GetCurrentAnimatorClipInfo(0))
                        {
                            if (info.clip == null) continue;
                            clipTime = state.normalizedTime * info.clip.length;
                            found = true;
                            break;
                        }
                    }
                }
                if (found)
                {
                    entered = true;
                    if (clipTime >= markerTime) break;
                }
                else if (entered) yield break; // 중단된 시전은 생성하지 않는다.
                else if (Time.time >= fallbackTime) break;
                yield return null;
            }
            if (isDead) yield break;
            yield return BossSkillExecutor.ExecuteSpawn(step, this);
            if (step.postDelay > 0f)
                yield return new WaitForSeconds(GetSkillDelay(step.postDelay));
        }
        finally
        {
            stoneSpawnRunning = false;
            stoneSpawnRoutine = null;
        }
    }

    protected override System.Collections.IEnumerator WaitForSkillAnimation(BossSkillData skill, int index)
    {
        yield return base.WaitForSkillAnimation(skill, index);
        while (stoneSpawnRunning && !isDead) yield return null;
    }

    [Header("돌기둥 생성 높이")]
    [SerializeField, Tooltip("비워두면 MainBoss 2 아래 Foot_B를 자동으로 찾습니다.")]
    private Transform stoneWaveFoot;
    [SerializeField, Tooltip("Foot_B 월드 높이 기준 보정값입니다. 기존 spawnOffset.y 대신 사용합니다.")]
    private float stoneWaveHeightOffset = 0f;

    public bool TryGetSpawnHeight(BossAttackStepData step, out float worldY)
    {
        worldY = 0f;
        // 데이터의 4스킬(화면의 Skill3) 돌기둥 생성 단계에만 적용한다.
        BossSkillData stoneSkill = GetSkillData(3);
        if (stoneSkill?.active?.attackSteps == null) return false;
        bool isStoneStep = false;
        foreach (BossAttackStepData candidate in stoneSkill.active.attackSteps)
        {
            if (object.ReferenceEquals(candidate, step)) { isStoneStep = true; break; }
        }
        if (!isStoneStep) return false;
        if (stoneWaveFoot == null)
            stoneWaveFoot = transform.Find("MainBoss 2/Body/Bottom/Thigh_B/Calf_B/Foot_B");
        if (stoneWaveFoot == null) return false;
        worldY = stoneWaveFoot.position.y + stoneWaveHeightOffset;
        return true;
    }

    [Header("욕망 스킬")]
    [SerializeField, Tooltip("2스테이지 메인보스의 안전 필드와 복귀 행동을 관리하는 전용 욕망 컴포넌트입니다.")]
    private Stage2_MainBossDesireSkill desireSkill;
    [Header("거리 쿨타임 감소")]
    [SerializeField, Min(0f), Tooltip("2스킬을 한 번 이상 사용한 뒤 이 거리 이상 벌어지면 남은 쿨타임을 한 번 50% 줄입니다.")]
    private float skill2ReductionDistance = 7f;
    [SerializeField, Min(0f), Tooltip("4스킬을 한 번 이상 사용한 뒤 이 거리 이상 벌어지면 남은 쿨타임을 한 번 50% 줄입니다.")]
    private float skill4ReductionDistance = 8f;

    protected override float GetDashCooldownReductionDistance(int index)
        => desireSkill != null && desireSkill.FieldActive ? 0f
            : index == 1 ? skill2ReductionDistance
            : index == 3 ? skill4ReductionDistance : base.GetDashCooldownReductionDistance(index);

    protected override void Awake()
    {
        base.Awake();
        if (desireSkill == null) desireSkill = GetComponent<Stage2_MainBossDesireSkill>();
        if (desireSkill == null) desireSkill = gameObject.AddComponent<Stage2_MainBossDesireSkill>();
        desireSkill.Initialize(this);
    }

    protected override void Update()
    {
        base.Update();
        if (desireSkill == null || !desireSkill.FieldActive)
        {
            TryReduceRemainingSkillCooldownByDistance(1, skill2ReductionDistance);
            TryReduceRemainingSkillCooldownByDistance(3, skill4ReductionDistance);
        }
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        desireSkill?.FixedTick();
    }

    protected override bool UpdatePriorityBehavior()
        => desireSkill != null && desireSkill.Tick(currentPhase);

    protected override bool CanRunCombatAI()
        => desireSkill == null || desireSkill.CanRunCombatAI();

    public bool CanHitPlayer(Collider2D player)
        => desireSkill == null || desireSkill.CanHitPlayer(player);

    protected override float GetExternalDamageMultiplier()
        => desireSkill != null ? desireSkill.OutgoingDamageMultiplier : 1f;

    public override void TakeDamage(float damage, Vector2 sourcePos, bool applyKnockback = false, bool applyIFrame = false)
    {
        desireSkill?.OnBossDamaged();
        float adjustedDamage = desireSkill != null ? desireSkill.ModifyIncomingDamage(damage) : damage;
        base.TakeDamage(adjustedDamage, sourcePos, applyKnockback, applyIFrame);
    }

    protected override void Die()
    {
        RestoreTimedGravity();
        desireSkill?.Cancel();
        base.Die();
    }

    // 전용 욕망 스킬과 공통 컨트롤러 사이의 좁은 연결 지점.
    public Rigidbody2D DesireBody => rb;
    public Transform DesirePlayer => playerTransform;
    public LayerMask DesirePlayerLayer => playerLayer;
    public int DesireLookDirection => lookDirection;
    public bool DesireIsDead => isDead;
    public bool DesireInCombat => isChasing;
    public float DesireMaxHp => data != null ? data.maxHp : 0f;
    public bool DesireIsAttacking { get => isAttacking; set => isAttacking = value; }
    public void DesireSetChasing(bool value) => isChasing = value;
    public void DesireStop() => StopMove();
    public void DesireInterruptSkill() => InterruptCurrentSkill();
    public void DesireFacePlayer() => RefreshSkillDirection();
    public void DesireHeal(float amount)
    {
        if (data == null || amount <= 0f) return;
        currentHp = Mathf.Min(data.maxHp, currentHp + amount);
        UpdateHealthUI();
    }
    public void DesireLog(string state, string skillName, float skillCooldown)
        => LogSkillDebug(state, new BossSkillData { skillName = skillName, cooldown = skillCooldown });
}
