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
    void RefreshSkillDirection();
    void OnSkillAttackHit(BossAttackStepData step);
    float GetSkillDamage(float baseDamage);
    float GetSkillDelay(float baseDelay);
    void ApplySkillBuff(BossBuffSkillData buff);
}

public interface IBossSpawnedSkillObject
{
    void Init(int direction, float damage, LayerMask playerLayer, float sizeMultiplier = 1f);
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
                    yield return ExecuteJump(step, context);
                    break;

                case BossActionType.Slam:
                    yield return ExecuteSlam(step, context);
                    break;

                case BossActionType.SpawnPrefab:
                    yield return ExecuteSpawn(step, context);
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

        while (timer < step.duration)
        {
            if (step.stopAtWall && context.IsWallAhead())
                break;

            Rigidbody2D body = context.BossBody;
            if (body != null)
                body.linearVelocity = new Vector2(context.LookDirection * step.moveSpeed, body.linearVelocity.y);

            if (attackDuringMove && remainingHits > 0 && HitPlayer(step, context))
                remainingHits--;

            timer += Time.deltaTime;
            yield return null;
        }

        StopHorizontal(context.BossBody);
    }

    private static IEnumerator ExecuteJump(BossAttackStepData step, IBossSkillContext context)
    {
        Rigidbody2D body = context.BossBody;
        if (body == null || step.jumpForce == Vector2.zero)
            yield break;

        body.linearVelocity = new Vector2(
            context.LookDirection * step.jumpForce.x,
            step.jumpForce.y
        );

        if (step.duration > 0f)
            yield return new WaitForSeconds(context.GetSkillDelay(step.duration));
    }

    private static IEnumerator ExecuteSlam(BossAttackStepData step, IBossSkillContext context)
    {
        Rigidbody2D body = context.BossBody;
        if (body != null && step.slamSpeed > 0f)
            body.linearVelocity = new Vector2(body.linearVelocity.x, -step.slamSpeed);

        if (step.duration > 0f)
            yield return new WaitForSeconds(context.GetSkillDelay(step.duration));

        yield return ExecuteRepeatedAttacks(step, context);
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
                    step.spawnScaleMultiplier > 0f ? step.spawnScaleMultiplier : 1f
                );

            if (i < step.spawnCount - 1 && step.spawnInterval > 0f)
                yield return new WaitForSeconds(context.GetSkillDelay(step.spawnInterval));
        }
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
                playerBody.linearVelocity = Vector2.zero;
                playerBody.AddForce(
                    new Vector2(context.LookDirection * step.knockbackForce.x, step.knockbackForce.y),
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
