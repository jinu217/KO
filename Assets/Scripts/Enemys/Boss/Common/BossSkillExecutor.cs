using System;
using System.Collections;
using UnityEngine;

public interface IBossSkillContext
{
    Transform BossTransform { get; }
    Rigidbody2D BossBody { get; }
    LayerMask PlayerLayer { get; }
    int LookDirection { get; }
    bool IsWallAhead();
    bool IsGrounded();
    Vector2 PlayerPosition { get; }
    float GetDistanceToGround();
    void RefreshSkillDirection();
    void OnSkillAttackHit(BossAttackStepData step);
    float GetSkillDamage(float baseDamage);
    float GetSkillDelay(float baseDelay);
    void ApplySkillBuff(BossBuffSkillData buff);
}

// 보스 전용 조건부 연계. 다른 보스의 실행 순서에는 영향을 주지 않는다.
public interface IBossConditionalStepContext
{
    bool ShouldExecuteStep(BossAttackStepData step);
}

public interface IBossTimedAttackContext
{
    bool UsesTimedAttack(BossAttackStepData step);
    IEnumerator ExecuteTimedAttack(BossAttackStepData step);
}
public interface IBossHitValidationContext
{
    bool CanHitPlayer(Collider2D player);
}

// 선택적 대쉬 알림: 기존 IBossSkillContext 구현을 변경할 필요가 없습니다.
public interface IBossDashAnimationContext
{
    void OnDashMovementStarted();
    void OnDashMovementStopped();
    IEnumerator WaitForDashRecovery();
}

// 선택적으로 생성 높이를 지정한다. 한 파동의 높이는 첫 생성 시 고정한다.
public interface IBossTimedMovementContext
{
    bool UsesTimedMovement(BossAttackStepData step);
    IEnumerator ExecuteTimedMovement(BossAttackStepData step);
}

public interface IBossScheduledSpawnContext
{
    bool IsScheduledSpawn(BossAttackStepData step);
}

// Optional fixed world height for a spawn wave (e.g. Stage2 stone pillars).
public interface IBossSpawnHeightContext
{
    bool TryGetSpawnHeight(BossAttackStepData step, out float worldY);
}

public interface IBossSpawnedSkillObject
{
    void Init(int direction, float damage, LayerMask playerLayer, float sizeMultiplier = 1f, float projectileSpeed = 0f);
}

// BossSkillData의 액티브 단계와 버프를 공통 방식으로 실행한다.
public static class BossSkillExecutor
{
#if UNITY_EDITOR
    // Editor diagnostics observes the real query; no change to damage or timing.
    public static event Action<IBossSkillContext, BossAttackStepData, Vector2, Collider2D> HitboxObserved;
    public static Bounds LastObservedBounds { get; private set; }
    public static void ObserveHitbox(IBossSkillContext context, BossAttackStepData step, Vector2 center, Collider2D hit)
    {
        LastObservedBounds = new Bounds(center, step.hitboxSize);
        HitboxObserved?.Invoke(context, step, center, hit);
    }
#endif
    public static IEnumerator Execute(BossSkillData skill, IBossSkillContext context)
    {
        if (skill == null || context == null)
            yield break;

        // 공격 시작 시 목표와 방향을 고정한다. 준비 도중 플레이어가 뒤로 넘어가도 재조준하지 않는다.
        float lockedTargetX = context.PlayerPosition.x;
        int lockedDirection = context.LookDirection < 0 ? -1 : 1;

        var effects = context.BossTransform.GetComponent<BossEffects>();
        var steps = skill.active?.attackSteps;
        bool timedFirst = steps != null && steps.Count > 0 && effects != null && effects.HasMotionTiming(steps[0]);
        float preparation = timedFirst ? 0f : skill.preDelay;
        if (context is Stage3_MidBoss)
        {
            if (skill.skillType == BossSkillType.Buff) preparation = .90f;
            else if (skill.active != null && skill.active.attackSteps != null)
                foreach (var step in skill.active.attackSteps)
                {
                    if (step.actionType == BossActionType.DashAttack) preparation = 1.08f;
                    if (step.actionType == BossActionType.Jump) { preparation = .85f; break; }
                }
        }
        if (preparation > 0f)
            yield return new WaitForSeconds(context.GetSkillDelay(preparation));

        if (skill.skillType == BossSkillType.Active)
        {
            yield return ExecuteActive(skill.active, context, lockedTargetX, lockedDirection);
        }
        else if (skill.skillType == BossSkillType.Buff)
        {
            context.ApplySkillBuff(skill.buff);
        }
        else if (skill.executionOrder == BossSkillExecutionOrder.ActiveThenBuff)
        {
            yield return ExecuteActive(skill.active, context, lockedTargetX, lockedDirection);
            context.ApplySkillBuff(skill.buff);
        }
        else
        {
            // 동시 실행은 버프를 활성화한 직후 액티브 단계를 시작한다.
            context.ApplySkillBuff(skill.buff);
            yield return ExecuteActive(skill.active, context, lockedTargetX, lockedDirection);
        }

        if (skill.postDelay > 0f)
            yield return new WaitForSeconds(context.GetSkillDelay(skill.postDelay));
    }

    private static IEnumerator ExecuteActive(BossActiveSkillData active, IBossSkillContext context, float lockedTargetX, int lockedDirection)
    {
        if (active?.attackSteps == null)
            yield break;

        JumpTargetState jumpTarget = new JumpTargetState
        {
            lockedTargetX = lockedTargetX,
            lockedDirection = lockedDirection
        };

        foreach (BossAttackStepData step in active.attackSteps)
        {
            if (step == null || step.actionType == BossActionType.None)
                continue;
            if (context is IBossConditionalStepContext conditional && !conditional.ShouldExecuteStep(step))
                continue;

            if (step.actionType == BossActionType.SpawnPrefab
                && context is IBossScheduledSpawnContext scheduled
                && scheduled.IsScheduledSpawn(step))
                continue;

            if (context is IBossTimedAttackContext attack && attack.UsesTimedAttack(step))
            {
                var attackFX = context.BossTransform.GetComponent<BossEffects>();
                attackFX?.BeginStep(step);
                try { yield return attack.ExecuteTimedAttack(step); }
                finally { attackFX?.EndStep(); }
                if (step.postDelay > 0f) yield return new WaitForSeconds(context.GetSkillDelay(step.postDelay));
                continue;
            }

            if (context is IBossTimedMovementContext timed && timed.UsesTimedMovement(step))
            {
                var timedFX = context.BossTransform.GetComponent<BossEffects>();
                timedFX?.BeginStep(step);
                try { yield return timed.ExecuteTimedMovement(step); }
                finally { timedFX?.EndStep(); }
                if (step.postDelay > 0f)
                    yield return new WaitForSeconds(context.GetSkillDelay(step.postDelay));
                continue;
            }

            context.BossTransform.GetComponent<BossEffects>()?.BeginStep(step);
            context.RefreshSkillDirection();

            if (step.preDelay > 0f && !(context.BossTransform.GetComponent<BossEffects>()?.HasMotionTiming(step) ?? false))
                yield return new WaitForSeconds(context.GetSkillDelay(step.preDelay));

            switch (step.actionType)
            {
                case BossActionType.Wait:
                    if (step.duration > 0f)
                        yield return new WaitForSeconds(context.GetSkillDelay(step.duration));
                    break;

                case BossActionType.MeleeAttack:
                    yield return ExecuteRepeatedAttacks(step, context);
                    break;

                case BossActionType.MoveForward:
                    yield return ExecuteMove(step, context, false);
                    break;

                case BossActionType.DashAttack:
                    yield return ExecuteMove(step, context, true);
                    break;

                case BossActionType.Jump:
                    yield return ExecuteJump(step, context, jumpTarget);
                    break;

                case BossActionType.Slam:
                    yield return ExecuteSlam(step, context, jumpTarget);
                    break;

                case BossActionType.SpawnPrefab:
                    yield return ExecuteSpawn(step, context);
                    break;

                case BossActionType.PersistentHitbox:
                    yield return ExecutePersistentHitbox(step, context);
                    break;

                case BossActionType.PullPlayer:
                    yield return ExecutePullPlayer(step, context);
                    break;

            }

            context.BossTransform.GetComponent<BossEffects>()?.EndStep();
            if (step.postDelay > 0f)
                yield return new WaitForSeconds(context.GetSkillDelay(step.postDelay));
        }
    }

    public static IEnumerator ExecuteRepeatedAttacks(BossAttackStepData step, IBossSkillContext context)
    {
        for (int i = 0; i < step.attackCount; i++)
        {
            var fx = context.BossTransform.GetComponent<BossEffects>();
            if (fx != null)
            {
                yield return fx.WaitForImpact(step, i);
                if (!fx.ImpactAllowed) yield break;
            }
            PlayAttackEffect(step, context);
            HitPlayer(step, context);
            if (i < step.attackCount - 1 && step.interval > 0f && !(fx != null && fx.HasMotionTiming(step, i + 1)))
                yield return new WaitForSeconds(context.GetSkillDelay(step.interval));
        }
    }

    private static IEnumerator ExecuteMove(BossAttackStepData step, IBossSkillContext context, bool attackDuringMove)
    {
        float timer = 0f;
        int remainingHits = step.attackCount;
        Rigidbody2D body = context.BossBody;
        IBossDashAnimationContext animation = attackDuringMove ? context as IBossDashAnimationContext : null;
        if (animation == null)
        {
            yield return ExecuteMoveWithoutDashAnimation(step, context, attackDuringMove);
            yield break;
        }
        if (attackDuringMove) PlayAttackEffect(step, context);
        float startX = body != null ? body.position.x : context.BossTransform.position.x;
        animation?.OnDashMovementStarted();
        try
        {
            while (timer < step.duration && body != null)
            {
                if (step.stopAtWall && context.IsWallAhead()) break;
                float remainingDistance = step.maxMoveDistance > 0f
                    ? step.maxMoveDistance - Mathf.Abs(body.position.x - startX)
                    : float.PositiveInfinity;
                if (remainingDistance <= 0.001f) break;
                float dt = Time.fixedDeltaTime;
                float travelTime = Mathf.Min(dt, step.duration - timer);
                float distance = Mathf.Min(Mathf.Abs(step.moveSpeed) * travelTime, remainingDistance);
                float velocity = context.LookDirection * Mathf.Sign(step.moveSpeed) * distance / dt;
                body.linearVelocity = new Vector2(velocity, body.linearVelocity.y);

                if (attackDuringMove && remainingHits > 0 && HitPlayer(step, context))
                {
                    remainingHits--;
                    if (step.stopOnPlayerHit) break;
                }
                yield return new WaitForFixedUpdate();
                timer += dt;
            }
        }
        finally
        {
            StopHorizontal(body);
            animation?.OnDashMovementStopped();
        }
        // 제동 모션이 끝날 때까지 이 스킬의 실행 상태를 유지합니다.
        if (animation != null) yield return animation.WaitForDashRecovery();
    }

    private static IEnumerator ExecuteMoveWithoutDashAnimation(BossAttackStepData step, IBossSkillContext context, bool attackDuringMove)
    {
        float timer = 0f;
        int remainingHits = step.attackCount;
        if (attackDuringMove) PlayAttackEffect(step, context);
        float startX = context.BossBody != null ? context.BossBody.position.x : context.BossTransform.position.x;

        while (timer < step.duration)
        {
            if (step.stopAtWall && context.IsWallAhead())
                break;
            if (step.maxMoveDistance > 0f
                && Mathf.Abs(context.BossTransform.position.x - startX) >= step.maxMoveDistance)
                break;

            Rigidbody2D body = context.BossBody;
            if (body != null)
                body.linearVelocity = new Vector2(context.LookDirection * step.moveSpeed, body.linearVelocity.y);

            if (attackDuringMove && remainingHits > 0 && HitPlayer(step, context))
            {
                remainingHits--;
                if (step.stopOnPlayerHit)
                    break;
            }

            timer += Time.deltaTime;
            yield return null;
        }

        StopHorizontal(context.BossBody);
    }

    private static IEnumerator ExecuteJump(BossAttackStepData step, IBossSkillContext context, JumpTargetState jumpTarget)
    {
        Rigidbody2D body = context.BossBody;
        if (body == null || step.jumpForce == Vector2.zero)
            yield break;

        bool useTargetRatio = step.targetDistanceRatio > 0f;
        float startX = body.position.x;
        float targetX = startX;
        if (useTargetRatio)
        {
            jumpTarget.active = true;
            jumpTarget.startX = startX;
            // 이미 목표를 지나쳤다면 제자리 점프한다. 뒤쪽 목표로 역주행하지 않는다.
            float forwardDistance = Mathf.Max(0f, (jumpTarget.lockedTargetX - startX) * jumpTarget.lockedDirection);
            jumpTarget.totalDeltaX = forwardDistance * jumpTarget.lockedDirection;
            jumpTarget.completedRatio = step.targetDistanceRatio;
            targetX = startX + jumpTarget.totalDeltaX * step.targetDistanceRatio;
        }

        body.linearVelocity = new Vector2(
            useTargetRatio ? 0f : jumpTarget.lockedDirection * step.jumpForce.x,
            step.jumpForce.y
        );

        // 고정 대기 대신 상승 정점까지 기다려 내려찍기가 자연스럽게 이어지게 한다.
        float timeout = step.duration > 0f ? context.GetSkillDelay(step.duration) : 2f;
        float elapsed = 0f;
        yield return new WaitForFixedUpdate();
        while (body.linearVelocity.y > 0f && elapsed < timeout)
        {
            if (useTargetRatio)
            {
                float progress = 1f - Mathf.Clamp01(body.linearVelocity.y / Mathf.Max(0.01f, step.jumpForce.y));
                body.position = new Vector2(Mathf.Lerp(startX, targetX, progress), body.position.y);
            }
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        if (useTargetRatio)
            body.position = new Vector2(targetX, body.position.y);
    }

    private static IEnumerator ExecuteSlam(BossAttackStepData step, IBossSkillContext context, JumpTargetState jumpTarget)
    {
        Rigidbody2D body = context.BossBody;
        if (body == null) yield break;
        bool useTargetRatio = jumpTarget.active && step.targetDistanceRatio > 0f;
        float startX = body.position.x;
        float targetX = useTargetRatio
            ? jumpTarget.startX + jumpTarget.totalDeltaX * Mathf.Clamp01(jumpTarget.completedRatio + step.targetDistanceRatio)
            : startX;
        float startY = body.position.y;
        float groundDistance = Mathf.Max(0.01f, context.GetDistanceToGround());
        float timeout = step.duration > 0f ? context.GetSkillDelay(step.duration) : 2f;
        var owner = context.BossTransform.GetComponent<MidBossController>();
        int groundMask = owner != null ? (int)owner.wallLayer
            : Physics2D.GetLayerCollisionMask(body.gameObject.layer) & ~(int)context.PlayerLayer;
        var filter = new ContactFilter2D();
        filter.SetLayerMask(groundMask);
        filter.useTriggers = false;
        var hits = new RaycastHit2D[32];
        CollisionDetectionMode2D previousMode = body.collisionDetectionMode;
        bool landed = false;
        try
        {
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            if (step.slamSpeed > 0f)
                body.linearVelocity = new Vector2(useTargetRatio ? 0f : body.linearVelocity.x, -step.slamSpeed);
            float elapsed = 0f;
            while (body != null && elapsed < timeout)
            {
                float dt = Time.fixedDeltaTime;
                // 다음 물리 스텝의 중력까지 포함하여 콜라이더 전체 경로를 검사합니다.
                float nextVy = body.linearVelocity.y + Physics2D.gravity.y * body.gravityScale * dt;
                float dy = Mathf.Min(0f, nextVy * dt);
                float dx = body.linearVelocity.x * dt;
                if (useTargetRatio)
                {
                    float progress = Mathf.Clamp01((startY - body.position.y - dy) / groundDistance);
                    dx = Mathf.Lerp(startX, targetX, progress) - body.position.x;
                }
                Vector2 travel = new Vector2(dx, dy);
                float distance = travel.magnitude;
                float nearest = float.PositiveInfinity;
                if (distance > 0.00001f)
                {
                    int count = body.Cast(travel / distance, filter, hits, distance + 0.01f);
                    for (int i = 0; i < count; i++)
                    {
                        // 옆벽은 착지로 처리하지 않습니다.
                        if (hits[i].collider == null || hits[i].normal.y < 0.35f) continue;
                        nearest = Mathf.Min(nearest, hits[i].distance);
                    }
                }
                if (!float.IsPositiveInfinity(nearest))
                {
                    body.position += travel.normalized * Mathf.Clamp(nearest - 0.01f, 0f, distance);
                    body.linearVelocity = Vector2.zero;
                    landed = true;
                    break;
                }
                if (context.IsGrounded() && body.linearVelocity.y <= 0f)
                {
                    body.linearVelocity = Vector2.zero;
                    landed = true;
                    break;
                }
                if (useTargetRatio)
                {
                    // 수평 이동도 속도로 적용하여 물리 충돌을 우회하지 않습니다.
                    body.linearVelocity = new Vector2(dx / dt, body.linearVelocity.y);
                }
                elapsed += dt;
                yield return new WaitForFixedUpdate();
            }
        }
        finally
        {
            if (body != null)
            {
                body.linearVelocity = new Vector2(0f, Mathf.Max(0f, body.linearVelocity.y));
                body.collisionDetectionMode = previousMode;
            }
        }
        // 타임아웃을 착지로 간주해 공중에서 내려찍기 판정을 만들지 않습니다.
        if (landed) yield return ExecuteRepeatedAttacks(step, context);
    }

    private sealed class JumpTargetState
    {
        public float lockedTargetX;
        public int lockedDirection;
        public bool active;
        public float startX;
        public float totalDeltaX;
        public float completedRatio;
    }

    public static IEnumerator ExecuteSpawn(BossAttackStepData step, IBossSkillContext context)
    {
        if (step.spawnPrefab == null)
            yield break;

        float spawnHeight = 0f;
        bool overrideHeight = context is IBossSpawnHeightContext heightContext
            && heightContext.TryGetSpawnHeight(step, out spawnHeight);

        for (int i = 0; i < step.spawnCount; i++)
        {
            Vector3 position = context.BossTransform.position + new Vector3(
                context.LookDirection * (step.spawnOffset.x + step.spawnSpacing * i),
                step.spawnOffset.y + step.spawnVerticalSpacing * i,
                0f
            );

            if (overrideHeight)
                position.y = spawnHeight + step.spawnVerticalSpacing * i;

            GameObject spawned = UnityEngine.Object.Instantiate(step.spawnPrefab, position, Quaternion.identity);
            IBossSpawnedSkillObject spawnedSkill = spawned.GetComponent<IBossSpawnedSkillObject>();
            if (spawnedSkill != null)
                spawnedSkill.Init(
                    context.LookDirection,
                    context.GetSkillDamage(step.damage),
                    context.PlayerLayer,
                    step.spawnScaleMultiplier > 0f ? step.spawnScaleMultiplier : 1f,
                    step.projectileSpeed
                );

            if (i < step.spawnCount - 1 && step.spawnInterval > 0f)
                yield return new WaitForSeconds(context.GetSkillDelay(step.spawnInterval));
        }
    }

    private static IEnumerator ExecutePersistentHitbox(BossAttackStepData step, IBossSkillContext context)
    {
        if (step.attackCount <= 0 || step.damage <= 0f || step.hitboxSize.x <= 0f || step.hitboxSize.y <= 0f)
            yield break;

        float interval = Mathf.Max(0.01f, context.GetSkillDelay(step.interval));
        float lifetime = step.duration > 0f
            ? context.GetSkillDelay(step.duration)
            : interval * step.attackCount;
        float elapsed = 0f;
        float nextHitTime = 0f;
        int hitCount = 0;
        bool playerEntered = false;

        while (elapsed <= lifetime && hitCount < step.attackCount)
        {
            bool playerInside = IsPlayerInHitbox(step, context);
            if (!step.persistentFixedCadence && playerEntered && !playerInside)
                yield break;

            if (step.persistentFixedCadence)
            {
                if (elapsed >= nextHitTime)
                {
                    PlayAttackEffect(step, context);
                    if (playerInside) HitPlayer(step, context);
                    hitCount++;
                    nextHitTime = elapsed + interval;
                }
            }
            else if (playerInside)
            {
                playerEntered = true;
                if (elapsed >= nextHitTime)
                {
                    PlayAttackEffect(step, context);
                    HitPlayer(step, context);
                    hitCount++;
                    nextHitTime = elapsed + interval;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }
        if (step.persistentFixedCadence)
            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
    }

    private static IEnumerator ExecutePullPlayer(BossAttackStepData step, IBossSkillContext context)
    {
        if (step.hitboxSize.x <= 0f || step.hitboxSize.y <= 0f)
            yield break;

        Vector2 center = (Vector2)context.BossTransform.position + new Vector2(
            context.LookDirection * step.hitboxOffset.x,
            step.hitboxOffset.y
        );
        Collider2D hit = Physics2D.OverlapBox(center, step.hitboxSize, 0f, context.PlayerLayer);
        Rigidbody2D playerBody = hit != null ? hit.GetComponentInParent<Rigidbody2D>() : null;
        if (playerBody == null)
            yield break;

        float elapsed = 0f;
        float pullSpeed = Mathf.Max(0f, step.moveSpeed);
        float pullDuration = Mathf.Max(0f, context.GetSkillDelay(step.duration));
        while (elapsed < pullDuration)
        {
            Vector2 target = (Vector2)context.BossTransform.position + new Vector2(
                context.LookDirection * step.pullTargetOffset.x,
                step.pullTargetOffset.y
            );
            if (Vector2.Distance(playerBody.position, target) <= 0.05f)
                break;

            playerBody.MovePosition(Vector2.MoveTowards(
                playerBody.position,
                target,
                pullSpeed * Time.fixedDeltaTime
            ));
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        playerBody.linearVelocity = Vector2.zero;
    }

    private static bool IsPlayerInHitbox(BossAttackStepData step, IBossSkillContext context)
    {
        Vector2 center = (Vector2)context.BossTransform.position + new Vector2(
            context.LookDirection * step.hitboxOffset.x,
            step.hitboxOffset.y
        );
        return Physics2D.OverlapBox(center, step.hitboxSize, 0f, context.PlayerLayer) != null;
    }

    // 피해를 거부하는 회피는 적중 알림과 별도 넉백도 발생시키지 않는다.
    public static bool CanDamagePlayer(Collider2D hit)
    {
        if (hit == null) return false;
        PlayerController player = hit.GetComponentInParent<PlayerController>();
        return player == null || !player.IsDodging;
    }

    private static void PlayAttackEffect(BossAttackStepData step, IBossSkillContext context)
    {
        if (step.damage <= 0 || step.hitboxSize.x <= 0 || step.hitboxSize.y <= 0) return;
        var fx = context.BossTransform.GetComponent<BossEffects>();
        if (fx == null) return;
        Vector3 center = MeleeCenter(step, context);
        fx.PlayStep(step, center, context.LookDirection);
    }

    public static Bounds AttackBounds(BossAttackStepData step, IBossSkillContext context)
    {
        var effects = context.BossTransform.GetComponent<BossEffects>();
        if (effects != null && effects.TryAttackBounds(step, out var fist)) return fist;
        return new Bounds(MeleeCenter(step, context), step.hitboxSize);
    }
    private static Vector3 MeleeCenter(BossAttackStepData step, IBossSkillContext context)
    {
        var fx = context.BossTransform.GetComponent<BossEffects>();
        if (fx != null && fx.TryAttackBounds(step, out var fist)) return fist.center;
        if (fx != null && fx.TryWeaponImpact(step, out var weaponPoint))
            return weaponPoint + Vector3.up * (step.hitboxSize.y * .5f);
        float reach = 0f;
        if ((step.actionType == BossActionType.MeleeAttack || step.actionType == BossActionType.Slam) &&
            context is Stage1_MidBoss midBoss1) reach = midBoss1.MeleeReachBonus;
        return context.BossTransform.position + new Vector3(
            context.LookDirection * (step.hitboxOffset.x + reach), step.hitboxOffset.y, 0);
    }

    private static bool HitPlayer(BossAttackStepData step, IBossSkillContext context)
    {
        if (step.damage <= 0f || step.hitboxSize.x <= 0f || step.hitboxSize.y <= 0f)
            return false;

        Bounds bounds = AttackBounds(step, context);
        return HitPlayerAt(step, context, bounds);
    }

    public static bool HitPlayerAt(BossAttackStepData step, IBossSkillContext context, Bounds bounds)
    {
        if (step.damage <= 0f || bounds.size.x <= 0f || bounds.size.y <= 0f) return false;
        Vector2 center = bounds.center;
        Collider2D hit = Physics2D.OverlapBox(center, bounds.size, 0f, context.PlayerLayer);
#if UNITY_EDITOR
        LastObservedBounds = bounds;
        HitboxObserved?.Invoke(context, step, center, hit);
#endif
        if (!CanDamagePlayer(hit))
            return false;

        if (context is IBossHitValidationContext validator && !validator.CanHitPlayer(hit))
            return false;

        IDamageable target = hit.GetComponentInParent<IDamageable>();
        if (target == null)
            return false;

        bool knockback = step.knockbackForce != Vector2.zero;
        target.TakeDamage(context.GetSkillDamage(step.damage), context.BossTransform.position, knockback, false);
        context.OnSkillAttackHit(step);

        if (knockback)
        {
            Rigidbody2D playerBody = hit.GetComponentInParent<Rigidbody2D>();
            if (playerBody != null)
            {
                float knockbackDirection = context.LookDirection;
                if (step.knockbackAwayFromCenter)
                {
                    knockbackDirection = Mathf.Sign(playerBody.position.x - center.x);
                    if (knockbackDirection == 0f) knockbackDirection = context.LookDirection;
                }
                playerBody.linearVelocity = Vector2.zero;
                playerBody.AddForce(
                    new Vector2(knockbackDirection * step.knockbackForce.x, step.knockbackForce.y),
                    ForceMode2D.Impulse
                );
            }
        }

        return true;
    }

    private static void StopHorizontal(Rigidbody2D body)
    {
        if (body != null)
            body.linearVelocity = new Vector2(0f, body.linearVelocity.y);
    }
}
