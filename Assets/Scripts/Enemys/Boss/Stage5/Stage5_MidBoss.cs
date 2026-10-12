using System.Collections;
using UnityEngine;

// 5스테이지 중간보스의 지속 유인과 돌진 재조준만 담당한다.
public sealed class Stage5_MidBoss : MidBossController, IBossTimedMovementContext
{
    private BossAttackStepData activePull;
    private bool pullComplete;

    protected override void OnSkillExecutionStarted(BossSkillData skill, int index)
    {
        base.OnSkillExecutionStarted(skill, index);
        activePull = index == 1 && skill?.active?.attackSteps?.Count > 0
            ? skill.active.attackSteps[0] : null;
        pullComplete = false;
    }

    protected override void OnSkillExecutionFinished(int index)
    {
        activePull = null;
        pullComplete = false;
        base.OnSkillExecutionFinished(index);
    }

    public bool UsesTimedMovement(BossAttackStepData step)
        => step != null && ReferenceEquals(step, activePull);

    public IEnumerator ExecuteTimedMovement(BossAttackStepData step)
    {
        // 도중에 플레이어가 범위에 들어오거나 벗어나도 준비시간은 끝까지 유지한다.
        float duration = Mathf.Max(0f, GetSkillDelay(step.duration));
        float elapsed = 0f;
        StopMove();
        while (!isDead && elapsed < duration)
        {
            Vector2 center = (Vector2)transform.position + new Vector2(
                lookDirection * step.hitboxOffset.x, step.hitboxOffset.y);
            Collider2D hit = Physics2D.OverlapBox(center, step.hitboxSize, 0f, playerLayer);
            Rigidbody2D playerBody = hit != null ? hit.GetComponentInParent<Rigidbody2D>() : null;
            PlayerController player = hit != null ? hit.GetComponentInParent<PlayerController>() : null;
            if (playerBody != null && (player == null || !player.IsDodging))
            {
                Vector2 target = (Vector2)transform.position + new Vector2(
                    lookDirection * step.pullTargetOffset.x, step.pullTargetOffset.y);
                // 바닥 높이는 유지하고 X축만 천천히 끌어온다.
                playerBody.MovePosition(new Vector2(
                    Mathf.MoveTowards(playerBody.position.x, target.x, step.moveSpeed * Time.fixedDeltaTime),
                    playerBody.position.y));
            }
            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        pullComplete = true;
    }

    public override void RefreshSkillDirection()
    {
        if (currentSkillIndex == 1 && pullComplete && playerTransform != null)
        {
            float difference = playerTransform.position.x - transform.position.x;
            if (Mathf.Abs(difference) > .15f)
            {
                lookDirection = difference > 0f ? 1 : -1;
                ApplyFacingDirection();
            }
            pullComplete = false;
            return;
        }
        base.RefreshSkillDirection();
    }
}
