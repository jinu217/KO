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

public interface IBossSpawnedSkillObject
{
    void Init(int direction, float damage, LayerMask playerLayer, float sizeMultiplier = 1f, float projectileSpeed = 0f);
}

// BossSkillData의 액티브 단계와 버프를 공통 방식으로 실행한다.
public static class BossSkillExecutor
{
    public static IEnumerator Execute(BossSkillData skill, IBossSkillContext context)
    {
        if (skill == null || context == null)
            yield break;

        if (skill.preDelay > 0f)
            yield return new WaitForSeconds(context.GetSkillDelay(skill.preDelay));

        if (skill.skillType == BossSkillType.Active)
        {
            yield return ExecuteActive(skill.active, context);
        }
        else if (skill.skillType == BossSkillType.Buff)
        {
            context.ApplySkillBuff(skill.buff);
        }
        else if (skill.executionOrder == BossSkillExecutionOrder.ActiveThenBuff)
        {
            yield return ExecuteActive(skill.active, context);
            context.ApplySkillBuff(skill.buff);
        }
        else
        {
            // 동시 실행은 버프를 활성화한 직후 액티브 단계를 시작한다.
            context.ApplySkillBuff(skill.buff);
            yield return ExecuteActive(skill.active, context);
        }

        if (skill.postDelay > 0f)
            yield return new WaitForSeconds(context.GetSkillDelay(skill.postDelay));
    }

    private static IEnumerator ExecuteActive(BossActiveSkillData active, IBossSkillContext context)
    {
        if (active?.attackSteps == null)
            yield break;

        JumpTargetState jumpTarget = new JumpTargetState();

        foreach (BossAttackStepData step in active.attackSteps)
        {
            if (step == null || step.actionType == BossActionType.None)
                continue;

            context.RefreshSkillDirection();

            if (step.preDelay > 0f)
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

            if (step.postDelay > 0f)
                yield return new WaitForSeconds(context.GetSkillDelay(step.postDelay));
        }
    }

    private static IEnumerator ExecuteRepeatedAttacks(BossAttackStepData step, IBossSkillContext context)
    {
        for (int i = 0; i < step.attackCount; i++)
        {
            HitPlayer(step, context);
            if (i < step.attackCount - 1 && step.interval > 0f)
                yield return new WaitForSeconds(context.GetSkillDelay(step.interval));
        }
    }

    private static IEnumerator ExecuteMove(BossAttackStepData step, IBossSkillContext context, bool attackDuringMove)
    {
        float timer = 0f;
        int remainingHits = step.attackCount;
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
            jumpTarget.totalDeltaX = context.PlayerPosition.x - startX;
            jumpTarget.completedRatio = step.targetDistanceRatio;
            targetX = startX + jumpTarget.totalDeltaX * step.targetDistanceRatio;
        }

        body.linearVelocity = new Vector2(
            useTargetRatio ? 0f : context.LookDirection * step.jumpForce.x,
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
        if (body != null && step.slamSpeed > 0f)
            body.linearVelocity = new Vector2(body.linearVelocity.x, -step.slamSpeed);

        // 낙하 시작 직후의 기존 지면 접촉값을 건너뛴 뒤 실제 착지를 기다린다.
        yield return new WaitForFixedUpdate();
        float timeout = step.duration > 0f ? context.GetSkillDelay(step.duration) : 2f;
        float elapsed = 0f;
        bool useTargetRatio = jumpTarget.active && step.targetDistanceRatio > 0f;
        float startX = body != null ? body.position.x : 0f;
        float targetX = useTargetRatio
            ? jumpTarget.startX + jumpTarget.totalDeltaX * Mathf.Clamp01(jumpTarget.completedRatio + step.targetDistanceRatio)
            : startX;
        float startY = body != null ? body.position.y : 0f;
        float groundDistance = Mathf.Max(0.01f, context.GetDistanceToGround());
        while (!context.IsGrounded() && elapsed < timeout)
        {
            if (useTargetRatio && body != null)
            {
                float progress = Mathf.Clamp01((startY - body.position.y) / groundDistance);
                body.position = new Vector2(Mathf.Lerp(startX, targetX, progress), body.position.y);
            }
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        if (useTargetRatio && body != null)
            body.position = new Vector2(targetX, body.position.y);

        yield return ExecuteRepeatedAttacks(step, context);
    }

    private sealed class JumpTargetState
    {
        public bool active;
        public float startX;
        public float totalDeltaX;
        public float completedRatio;
    }

    private static IEnumerator ExecuteSpawn(BossAttackStepData step, IBossSkillContext context)
    {
        if (step.spawnPrefab == null)
            yield break;

        for (int i = 0; i < step.spawnCount; i++)
        {
            Vector3 position = context.BossTransform.position + new Vector3(
                context.LookDirection * (step.spawnOffset.x + step.spawnSpacing * i),
                step.spawnOffset.y + step.spawnVerticalSpacing * i,
                0f
            );

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
            if (playerEntered && !playerInside)
                yield break;

            if (playerInside)
            {
                playerEntered = true;
                if (elapsed >= nextHitTime)
                {
                    HitPlayer(step, context);
                    hitCount++;
                    nextHitTime = elapsed + interval;
                }
            }

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

    private static bool HitPlayer(BossAttackStepData step, IBossSkillContext context)
    {
        if (step.damage <= 0f || step.hitboxSize.x <= 0f || step.hitboxSize.y <= 0f)
            return false;

        Vector2 center = (Vector2)context.BossTransform.position + new Vector2(
            context.LookDirection * step.hitboxOffset.x,
            step.hitboxOffset.y
        );

        Collider2D hit = Physics2D.OverlapBox(center, step.hitboxSize, 0f, context.PlayerLayer);
        if (hit == null)
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
