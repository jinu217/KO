using System.Collections;
using UnityEngine;

// 일반 패턴의 특수 이동과 전용 욕망 컴포넌트의 공통 보스 연결을 담당한다.
public sealed class Stage5_MainBoss : MainBossController, IBossTimedMovementContext,
    IBossConditionalStepContext, IBossHitValidationContext, IBossSkillContext
{
    [Header("욕망 스킬")]
    [SerializeField, Tooltip("3페이즈 욕망 스킬의 시간·거리 스택·체력 되감기와 그로기를 담당합니다.")]
    private Stage5_MainBossDesireSkill desireSkill;
    [Header("3스킬 후속 이동")]
    [SerializeField, Min(0f), Tooltip("돌진 적중 후 플레이어 뒤쪽에 도달할 X축 거리입니다.")]
    private float vaultBehindDistance = 1.8f;
    [SerializeField, Min(0f), Tooltip("플레이어 위로 넘어갈 때 추가 상승 높이입니다.")]
    private float vaultArcHeight = 2.1f;

    [Header("5스킬 콘티 이동 (화면 기준, 좌우 반전하지 않음)")]
    [SerializeField, Min(.2f)] private float crossUpperHeight = 1.8f;
    [SerializeField, Min(1f)] private float finalRushDistance = 8f;
    private Vector2 crossOrigin, crossCenter;
    private Vector2 crossReturnPosition;
    private bool? crossTrigger;
    private readonly RaycastHit2D[] crossWallHits = new RaycastHit2D[16];
    private readonly Collider2D[] crossOverlaps = new Collider2D[16];

    [Header("이기어검 예비 버전")]
    public bool useOrbitSword;
    private Stage5OrbitSword orbitSword;
    public bool OrbitSwordActive => orbitSword != null && orbitSword.Active;
    private BossSkillData executingSkill;
    private bool dashHitPlayer;
    private int lastSwordCrossing;
    private int lockedDashDirection;
    private Stage5BossAnimationRelay animationRelay;
    private float actionDelay = 1f;
    private float? airGravity;
    private bool specialAnimation;
    private Stage5BossAnimationRelay Relay => animationRelay != null ? animationRelay
        : (animationRelay = GetComponentInChildren<Stage5BossAnimationRelay>(true));
    float IBossSkillContext.GetSkillDelay(float value) => Delay(value);
    private float Delay(float value) => executingSkill != null ? value * actionDelay : GetSkillDelay(value);
    private void Play(string action, bool unscaled = false)
    { if (Relay != null) Relay.PlayState("MainBoss5_" + action, unscaled ? 1 : 1f / Mathf.Max(.01f, actionDelay)); }
    protected override IEnumerator WaitForSkillAnimation(BossSkillData skill, int index)
    {
        if (Relay != null) yield return Relay.WaitForEnd();
        if(index==4 && crossTrigger.HasValue && rb!=null && !isDead)
        {
            Vector2 start=rb.position,target=CrossGroundPosition();
            for(float t=0;t<.2f;t+=Time.fixedDeltaTime)
            { rb.MovePosition(Vector2.Lerp(start,target,Mathf.SmoothStep(0,1,t/.2f)));yield return new WaitForFixedUpdate(); }
            rb.position=target;
        }
    }
    private Coroutine specialRoutine;
    public void BeginDesirePose() { StartSpecial(DesirePose()); }
    public void BeginDesireSuccessPose() { StartSpecial(DesireSuccessPose()); }
    private void StartSpecial(IEnumerator routine)
    {
        if (specialRoutine != null) StopCoroutine(specialRoutine);
        specialRoutine = StartCoroutine(routine);
    }
    private IEnumerator DesireSuccessPose()
    {
        specialAnimation = true; isAttacking = true; StopMove();
        string name = Relay != null && Relay.transform.localToWorldMatrix.determinant < 0 ? "DesireSuccess_Mirrored" : "DesireSuccess";
        Play(name, true);
        if (Relay != null) yield return Relay.WaitForEnd();
        specialAnimation = false; isAttacking = false; specialRoutine = null;
    }
    private IEnumerator DesirePose()
    {
        specialAnimation = true; isAttacking = true; StopMove(); Play("Dskill", true);
        if (Relay != null) yield return Relay.WaitForEnd();
        specialAnimation = false; isAttacking = false;
    }
    public void BeginGroggyPose(float seconds) { StartSpecial(GroggyPose(seconds)); }
    private IEnumerator GroggyPose(float seconds)
    {
        specialAnimation = true; isAttacking = true; Play("Groggy", true);
        yield return new WaitForSeconds(Mathf.Max(0, seconds - 12f / 12f));
        if (!isDead) { Play("GroggyEnd", true); if (Relay != null) yield return Relay.WaitForEnd(); }
        specialAnimation = false; isAttacking = false;
    }
    private void RestoreAir()
    {
        if(crossTrigger.HasValue && bossCollider!=null)
        {
            if(rb!=null)rb.position=CrossGroundPosition();
            bossCollider.isTrigger=crossTrigger.Value;crossTrigger=null;
        }
        if (airGravity.HasValue && rb != null) rb.gravityScale = airGravity.Value;
        airGravity = null; if (Relay != null) Relay.SetRuntimeHidden(false);
    }
    private Vector2 CrossGroundPosition()
    {
        Vector2 target=AboveFloor(rb.position);
        return CanOccupy(target)?target:crossReturnPosition;
    }


    protected override string AnimationPrefix => "MainBoss5_";
    protected override string VisualRootName => "MainBoss 5";
    protected override bool HasPhaseEntryAnimations => false;
    protected override bool UsesPhase3ActionStates => false;

    protected override void Awake()
    {
        base.Awake();
        if (desireSkill == null) desireSkill = GetComponent<Stage5_MainBossDesireSkill>();
        if (desireSkill == null) desireSkill = gameObject.AddComponent<Stage5_MainBossDesireSkill>();
        desireSkill.Initialize(this);
    }

    protected override bool UpdatePriorityBehavior()
        => desireSkill != null && desireSkill.Tick(currentPhase);

    protected override bool CanRunCombatAI()
        => !specialAnimation && (desireSkill == null || !desireSkill.Groggy);

    protected override float GetExternalDamageMultiplier()
        => desireSkill != null ? desireSkill.DamageMultiplier : 1f;

    protected override float GetExternalAttackSpeedMultiplier()
        => desireSkill != null ? desireSkill.AttackSpeedMultiplier : 1f;

    protected override float GetExternalMoveMultiplier()
        => desireSkill != null ? desireSkill.MoveSpeedMultiplier : 1f;

    public override void TakeDamage(float damage, Vector2 sourcePos, bool applyKnockback = false, bool applyIFrame = false)
    {
        float incoming = desireSkill != null ? desireSkill.IncomingDamageMultiplier : 1f;
        base.TakeDamage(damage * incoming, sourcePos, applyKnockback, applyIFrame);
    }

    public float DesireHp => currentHp;
    public bool DesireBusy => isAttacking;
    public bool DesireInCombat => isChasing;
    public Transform DesirePlayer => playerTransform;
    public void DesireStop() => StopMove();
    public void DesireInterrupt() { orbitSword?.Cancel(); InterruptCurrentSkill(); RestoreAir(); executingSkill = null; }
    public void DesireLog(string message)
        => LogSkillDebug(message, new BossSkillData { skillName = "시간을 되돌리는 애정" });
    public void DesireRestoreHp(float value)
    {
        if (isDead) return;
        currentHp = Mathf.Clamp(value, 0f, data.maxHp);
        UpdateHealthUI();
    }

    protected override void Die()
    {
        orbitSword?.Cancel();
        RestoreAir();
        desireSkill?.Cancel();
        if (specialRoutine != null) StopCoroutine(specialRoutine);
        specialRoutine = null; specialAnimation = false;
        base.Die();
    }

    protected override void ApplyFacingDirection()
    {
        base.ApplyFacingDirection();
        // Whole visual hierarchy is flipped by the base controller.
    }

    protected override void OnSkillExecutionStarted(BossSkillData skill, int index)
    {
        actionDelay = GetSkillDelay(1f);
        executingSkill = skill;
        lastSwordCrossing = 0;
        dashHitPlayer = false;
        lockedDashDirection = lookDirection < 0 ? -1 : 1;
        if (index == 4 && rb != null)
        {
            crossOrigin = rb.position;
            crossReturnPosition = rb.position;
            crossCenter = playerTransform != null ? (Vector2)playerTransform.position : crossOrigin;
            // The four screen-space passes must straddle the target even when it
            // is left of the caster. Keep the authored 16-unit travel and poses.
            crossOrigin = crossCenter + new Vector2(-finalRushDistance,-crossUpperHeight);
            if(bossCollider!=null) { crossTrigger=bossCollider.isTrigger;bossCollider.isTrigger=true; }
            airGravity = rb.gravityScale; rb.gravityScale = 0; rb.linearVelocity = Vector2.zero;
        }
        base.OnSkillExecutionStarted(skill, index);
        Play(index < 0 ? "Attack" : "skill" + (index + 1));
        if(index==0 && useOrbitSword)
        {
            orbitSword=GetComponent<Stage5OrbitSword>() ?? gameObject.AddComponent<Stage5OrbitSword>();
            orbitSword.Begin(this,playerTransform);
        }
    }

    protected override void OnSkillExecutionFinished(int index)
    {
        orbitSword?.Cancel();
        RestoreAir();
        executingSkill = null;
        dashHitPlayer = false;
        base.OnSkillExecutionFinished(index);
    }

    public bool CanHitPlayer(Collider2D hit)
    {
        PlayerController player = hit.GetComponentInParent<PlayerController>();
        return player == null || !player.IsDodging;
    }

    public override void OnSkillAttackHit(BossAttackStepData step)
    {
        if (currentSkillIndex == 2 && StepIndex(step) == 1)
            dashHitPlayer = true;
    }

    public bool ShouldExecuteStep(BossAttackStepData step)
        => !(currentSkillIndex == 0 && useOrbitSword && StepIndex(step) % 2 == 1)
            && (currentSkillIndex != 2 || StepIndex(step) != 2 || dashHitPlayer);

    public bool UsesTimedMovement(BossAttackStepData step)
    {
        int index = StepIndex(step);
        return (currentSkillIndex < 0 && index == 2)
            || (currentSkillIndex == 0 && index >= 0 && index % 2 == 0)
            || (currentSkillIndex == 2 && index >= 0 && index <= 2)
            || (currentSkillIndex == 4 && index >= 0 && index < 4);
    }

    public IEnumerator ExecuteTimedMovement(BossAttackStepData step)
    {
        int index = StepIndex(step);
        if (currentSkillIndex < 0)
            yield return Stage5PatternMotion.Dash(step, this, () => isDead, true);
        else if (currentSkillIndex == 0) yield return WalkOneStep(step);
        else if (currentSkillIndex == 2)
        {
            if (index == 0)
            {
                if (rb == null) yield break;
                airGravity = rb.gravityScale; rb.gravityScale = 0; rb.linearVelocity = Vector2.zero;
                float elapsed = 0, duration = Delay(step.duration); Vector2 start = rb.position;
                while (!isDead && elapsed < duration)
                {
                    float u = Mathf.Clamp01((elapsed + Time.fixedDeltaTime) / Mathf.Max(.01f, duration * (3f / 10f)));
                    rb.MovePosition(start + Vector2.up * (.32f * u * u * (3 - 2 * u)));
                    yield return new WaitForFixedUpdate(); elapsed += Time.fixedDeltaTime;
                }
            }
            else if (index == 1)
            {
                yield return Stage5PatternMotion.Dash(step, this, () => isDead, false);
                if (!dashHitPlayer) { RestoreAir(); Play("skill3_Miss"); }
            }
            else
            {
                Play("skill3_Follow"); yield return VaultBehindPlayer(step); RestoreAir();
            }
        }
        else if (currentSkillIndex == 4) yield return CrossSlash(step, index);
    }

    public override void RefreshSkillDirection()
    {
        if (isDead || !isAttacking || playerTransform == null || currentSkillIndex != 0)
        {
            base.RefreshSkillDirection();
            return;
        }
        FaceTowards(playerTransform.position.x);
    }

    private int StepIndex(BossAttackStepData step)
        => executingSkill?.active?.attackSteps?.IndexOf(step) ?? -1;

    private void FaceTowards(float x)
    {
        float delta = x - transform.position.x;
        if (Mathf.Abs(delta) <= .15f) return;
        lookDirection = delta > 0f ? 1 : -1;
        ApplyFacingDirection();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (isDead || !isAttacking || executingSkill == null || currentSkillIndex != 0 || rb == null) return;
        var animator = Relay != null ? Relay.GetComponent<Animator>() : null;
        if (animator == null || !animator.isActiveAndEnabled) { StopMove(); return; }
        var clips = animator.GetCurrentAnimatorClipInfo(0);
        if (clips.Length == 0 || !clips[0].clip.name.StartsWith("MainBoss5_skill1")) { StopMove(); return; }
        float frame = animator.GetCurrentAnimatorStateInfo(0).normalizedTime * clips[0].clip.length * 12f;
        bool active = frame >= 12 && frame < 84;
        if (active && playerTransform != null) FaceTowards(playerTransform.position.x);
        bool walk = active && playerTransform != null && Mathf.Abs(playerTransform.position.x - rb.position.x) > .7f && !IsWallAhead();
        var steps = executingSkill.active.attackSteps;
        float speed = steps.Count > 0 ? steps[0].moveSpeed : 1.25f;
        // Own velocity for the entire casting walk, including the intervening attack steps.
        rb.linearVelocity = new Vector2(walk ? lookDirection * speed / Mathf.Max(.01f, actionDelay) : 0, rb.linearVelocity.y);
        int crossing = Mathf.FloorToInt((frame - 12) / 9f);
        if (!useOrbitSword && active && crossing > lastSwordCrossing && crossing <= 7 && frame >= 12 + crossing * 9 + .6f)
        {
            lastSwordCrossing = crossing;
            // Once per side change: close contact through the body, never a continuous damage aura.
            if (steps.Count > 1 && bossCollider != null)
            {
                Bounds close = bossCollider.bounds;
                close.Expand(new Vector3(.8f, .1f, 0));
                BossSkillExecutor.HitPlayerAt(steps[1], this, close);
            }
        }
    }

    private IEnumerator WalkOneStep(BossAttackStepData step)
    {
        if (step.preDelay > 0f) yield return new WaitForSeconds(Delay(step.preDelay));
        yield return new WaitForSeconds(Delay(step.duration));
    }

    private IEnumerator VaultBehindPlayer(BossAttackStepData step)
    {
        if (rb == null || playerTransform == null) yield break;
        if (step.preDelay > 0f) yield return new WaitForSeconds(Delay(step.preDelay));
        Vector2 start = rb.position;
        float forward = lockedDashDirection * (playerTransform.position.x - start.x);
        float travel = Mathf.Clamp(forward + vaultBehindDistance, .8f, Mathf.Max(.8f, step.maxMoveDistance));
        float targetX = start.x + lockedDashDirection * travel;
        float groundY = start.y;
        float feetOffset = bossCollider != null ? start.y - bossCollider.bounds.min.y : 0.5f;
        Vector2 rayOrigin = new Vector2(targetX, playerTransform.position.y + 1f);
        RaycastHit2D floor = Physics2D.Raycast(rayOrigin, Vector2.down, 30f, wallLayer);
        if (floor.collider != null) groundY = floor.point.y + feetOffset;

        Vector2 target = new Vector2(targetX, groundY);
        float duration = Mathf.Max(.01f, Delay(step.duration));
        float gravity = rb.gravityScale;
        CollisionDetectionMode2D collisionMode = rb.collisionDetectionMode;
        try
        {
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.linearVelocity = Vector2.zero;
            float elapsed = 0f;
            bool turned = false;
            while (!isDead && elapsed < duration)
            {
                float u = Mathf.Clamp01(elapsed / duration);
                float progress = u * u * (3 - 2 * u);
                if (!turned && u >= .5f)
                {
                    // The landing slash faces the player after vaulting past them.
                    lookDirection = playerTransform.position.x >= target.x ? 1 : -1;
                    ApplyFacingDirection(); turned = true;
                }
                Vector2 arc = Vector2.Lerp(start, target, progress)
                    + Vector2.up * (Mathf.Sin(progress * Mathf.PI) * vaultArcHeight);
                rb.MovePosition(arc);
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            rb.MovePosition(target);
            yield return new WaitForFixedUpdate();
            if (!isDead) yield return BossSkillExecutor.ExecuteRepeatedAttacks(step, this);
        }
        finally
        {
            if (rb != null)
            {
                rb.gravityScale = gravity;
                rb.collisionDetectionMode = collisionMode;
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    // Screen-space sequence: upper right -> lower left -> blink/right to upper left
    // -> blink/cast origin to long horizontal right rush. First two passes are continuous.
    private IEnumerator CrossSlash(BossAttackStepData step, int index)
    {
        if (rb == null || index < 0 || index >= 4) yield break;
        if (step.preDelay > 0) yield return new WaitForSeconds(Delay(step.preDelay));
        Vector2 start = rb.position;
        if (index == 0) start = crossOrigin;
        else if (index == 2) start = crossOrigin + Vector2.right * finalRushDistance * 2;
        else if (index == 3) start = new Vector2(crossOrigin.x,crossCenter.y);
        try
        {
            rb.linearVelocity = Vector2.zero;
            // Keep the character visible during the pre-pass pause and reposition.
            yield return new WaitForSeconds(Delay(2f / 12f));
            if (isDead) yield break;
            if (index == 0 || index >= 2)
            {
                // Ground overlap is intentional during this attack; restore a
                // solid grounded body only after the last motion finishes.
                rb.position = start;
                transform.position = new Vector3(start.x,start.y,transform.position.z);
            }
            else start = rb.position;
            // Symmetric paths around the player, including the low halves below ground.
            float direction = index == 0 || index == 3 ? 1 : -1;
            float endY = index == 0 || index == 2
                ? crossCenter.y + crossUpperHeight
                : index == 1 ? crossOrigin.y : start.y;
            Vector2 end = new Vector2(start.x + direction * finalRushDistance * 2, endY);
            if (Relay != null) Relay.SetRuntimeHidden(false);
            lookDirection = end.x >= start.x ? 1 : -1; ApplyFacingDirection();
            float duration = Mathf.Max(.01f, Delay(step.duration)), elapsed = 0;
            bool dealtDamage = false, blocked = false;
            Bounds previous=BossSkillExecutor.AttackBounds(step,this);
            while (!isDead && elapsed < duration)
            {
                float next = Mathf.Min(duration, elapsed + Time.fixedDeltaTime);
                // Two frames of fast attack, followed by the unchanged six-frame inertial brake.
                float u = next / duration;
                float progress = CrossSlashProgress(u);
                Vector2 target = Vector2.Lerp(start, end, progress);
                if (!blocked)
                {
                    Vector2 delta = target - rb.position; float distance = delta.magnitude;
                    if (distance > .0001f && bossCollider != null)
                    {
                        var filter = new ContactFilter2D(); filter.SetLayerMask(wallLayer); filter.useTriggers = false;
                        int count = bossCollider.Cast(delta / distance, filter, crossWallHits, distance + .02f);
                        float allowed = distance;
                        for (int i = 0; i < count; i++)
                            if (crossWallHits[i].collider != null && crossWallHits[i].collider.attachedRigidbody != rb
                                && crossWallHits[i].distance>.001f && Mathf.Abs(crossWallHits[i].normal.x)>.65f)
                                allowed = Mathf.Min(allowed, Mathf.Max(0, crossWallHits[i].distance - .02f));
                        if (allowed < distance) { target = rb.position + delta / distance * allowed; blocked = true; }
                    }
                    rb.MovePosition(target);
                }
                yield return new WaitForFixedUpdate(); elapsed = next;
                Bounds current=BossSkillExecutor.AttackBounds(step,this);
                if (!isDead && !dealtDamage)
                    dealtDamage=Stage5PatternMotion.SweepAttack(step,this,previous,current);
                previous=current;
            }
        }
        finally
        {
            if (Relay != null) Relay.SetRuntimeHidden(false);
            if (rb != null) rb.linearVelocity = Vector2.zero;
            // Keep gravity disabled across the inter-pass pauses; RestoreAir runs at action end/death.
        }
    }
    public static float CrossSlashProgress(float u)
    {
        u = Mathf.Clamp01(u);
        float brake = Mathf.Clamp01((u - .25f) / .75f);
        return u < .25f ? 3.2f * u : .8f + .2f * (1 - (1 - brake) * (1 - brake));
    }
    private Vector2 AboveFloor(Vector2 target)
    {
        if (bossCollider == null || rb == null) return target;
        float footOffset = rb.position.y - bossCollider.bounds.min.y;
        Vector2 origin = new Vector2(target.x, Mathf.Max(crossReturnPosition.y, target.y) + crossUpperHeight + 2);
        var hit = Physics2D.Raycast(origin, Vector2.down, 30, wallLayer);
        if (hit.collider != null) target.y = Mathf.Max(target.y, hit.point.y + footOffset + .025f);
        return target;
    }
    private bool CanOccupy(Vector2 target)
    {
        if (bossCollider == null) return true;
        Bounds bounds = bossCollider.bounds;
        Vector2 center = (Vector2)bounds.center + (target - rb.position);
        var filter = new ContactFilter2D(); filter.SetLayerMask(wallLayer); filter.useTriggers = false;
        int count = Physics2D.OverlapBox(center, (Vector2)bounds.size * .98f, 0, filter, crossOverlaps);
        for (int i = 0; i < count; i++)
            if (crossOverlaps[i] != null && crossOverlaps[i].attachedRigidbody != rb) return false;
        return true;
    }

    private bool PlayerInHitbox(BossAttackStepData step)
    {
        return Stage5PatternMotion.ContainsPlayer(step, this);
    }
}
