using System;
using System.Collections;
using UnityEngine;

// Stage5-only movement. The shared executor still owns damage calculation and knockback.
public static class Stage5PatternMotion
{
    public static IEnumerator Dash(BossAttackStepData step, IBossSkillContext owner,
        Func<bool> dead, bool holdRemainingTime)
    {
        if (step.preDelay > 0) yield return new WaitForSeconds(owner.GetSkillDelay(step.preDelay));
        owner.RefreshSkillDirection();
        Rigidbody2D body = owner.BossBody;
        if (body == null) yield break;
        float duration = Mathf.Max(0, owner.GetSkillDelay(step.duration));
        float rate = 1f / Mathf.Max(.01f, owner.GetSkillDelay(1));
        float elapsed = 0, startX = body.position.x;
        bool hit = false, stopped = false;
        // The swing is visible even on a miss; damage uses the same moving blade.
        owner.BossTransform.GetComponent<BossEffects>()?.PlayStep(step,BossSkillExecutor.AttackBounds(step,owner).center,owner.LookDirection);
        try
        {
            while (!dead() && elapsed < duration)
            {
                float remaining = step.maxMoveDistance > 0
                    ? step.maxMoveDistance - Mathf.Abs(body.position.x - startX) : float.PositiveInfinity;
                stopped |= (step.stopAtWall && owner.IsWallAhead()) || remaining <= .001f;
                if (stopped && !holdRemainingTime) break;
                float dt = Time.fixedDeltaTime;
                float travel = Mathf.Min(Mathf.Abs(step.moveSpeed) * rate * Mathf.Min(dt,duration-elapsed), Mathf.Max(0,remaining));
                body.linearVelocity = new Vector2(stopped ? 0 : owner.LookDirection * Mathf.Sign(step.moveSpeed) * travel / dt, body.linearVelocity.y);
                if (!stopped && !hit && ContainsPlayer(step,owner))
                {
                    // Dash steps in the supplied data have exactly one hit.
                    hit = BossSkillExecutor.HitPlayerAt(step,owner,BossSkillExecutor.AttackBounds(step,owner));
                    if (step.stopOnPlayerHit) break;
                }
                yield return new WaitForFixedUpdate();
                elapsed += dt;
            }
        }
        finally
        {
            if (body != null) body.linearVelocity = new Vector2(0,body.linearVelocity.y);
        }
    }
    public static bool ContainsPlayer(BossAttackStepData step, IBossSkillContext owner)
    {
        Bounds bounds=BossSkillExecutor.AttackBounds(step,owner);
        foreach(var hit in Physics2D.OverlapBoxAll(bounds.center,bounds.size,0,owner.PlayerLayer))
            if (!(owner is IBossHitValidationContext validation) || validation.CanHitPlayer(hit)) return true;
        return false;
    }
    public static bool SweepAttack(BossAttackStepData step, IBossSkillContext owner, Bounds previous, Bounds current)
    {
        float spacing=Mathf.Max(.08f,Mathf.Min(current.size.x,current.size.y)*.4f);
        int count=Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(previous.center,current.center)/spacing),1,64);
        for(int i=0;i<=count;i++)
        {
            float t=(float)i/count;
            if(BossSkillExecutor.HitPlayerAt(step,owner,new Bounds(Vector3.Lerp(previous.center,current.center,t),Vector3.Lerp(previous.size,current.size,t))))return true;
        }
        return false;
    }
}
