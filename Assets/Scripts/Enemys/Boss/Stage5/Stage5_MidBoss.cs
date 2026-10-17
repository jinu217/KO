using System.Collections;
using UnityEngine;

// 5스테이지 중간보스의 지속 유인과 돌진 재조준만 담당한다.
public sealed class Stage5_MidBoss : MidBossController, IBossTimedMovementContext, IBossSkillContext, IBossHitValidationContext
{
    private BossAttackStepData activePull;
    private bool pullComplete;
    private BossSkillData executing;
    private Stage5BossAnimationRelay animationRelay;
    private Transform stage5Visual;
    private float visualScale=1f, actionDelay=1f;
    private bool actionActive;
    private Stage5BossAnimationRelay Relay => animationRelay != null ? animationRelay
        : (animationRelay=GetComponentInChildren<Stage5BossAnimationRelay>(true));
    float IBossSkillContext.GetSkillDelay(float value) => actionActive ? value*actionDelay : GetSkillDelay(value);
    private float Delay(float value) => actionActive ? value*actionDelay : GetSkillDelay(value);
    private void Play(string action) { if(Relay!=null)Relay.PlayState("MidBoss5_"+action,1f/Mathf.Max(.01f,actionDelay)); }
    protected override void Awake()
    {
        base.Awake();stage5Visual=transform.Find("MidBoss 5");
        if(stage5Visual==null&&transform.name=="MidBoss 5")stage5Visual=transform;
        if(stage5Visual!=null)visualScale=Mathf.Abs(stage5Visual.localScale.x);
    }
    protected override void ApplyFacingDirection()
    {
        if(stage5Visual==null)return;
        Vector3 scale=stage5Visual.localScale;scale.x=visualScale*(lookDirection<0?-1:1);stage5Visual.localScale=scale;
    }
    private void LateUpdate()
    {
        if(isDead)return;
        ApplyFacingDirection();
        if(Relay!=null)Relay.SetMotion(isAttacking||rb==null?0:Mathf.Abs(rb.linearVelocity.x),isChasing,isAttacking);
    }
    public bool CanHitPlayer(Collider2D hit)
    {
        var p=hit.GetComponentInParent<PlayerController>();return p==null||!p.IsDodging;
    }
    protected override IEnumerator WaitForSkillAnimation(BossSkillData skill,int index)
    { if(Relay!=null)yield return Relay.WaitForEnd(); }


    protected override void OnSkillExecutionStarted(BossSkillData skill, int index)
    {
        base.OnSkillExecutionStarted(skill, index);
        executing=skill;actionDelay=GetSkillDelay(1);actionActive=true;
        Play(index<0?"Attack":"skill"+(index+1));
        activePull = index == 1 && skill?.active?.attackSteps?.Count > 0
            ? skill.active.attackSteps[0] : null;
        pullComplete = false;
    }

    protected override void OnSkillExecutionFinished(int index)
    {
        actionActive=false;executing=null;
        activePull = null;
        pullComplete = false;
        base.OnSkillExecutionFinished(index);
    }

    public bool UsesTimedMovement(BossAttackStepData step)
        => step != null && (ReferenceEquals(step, activePull)
            || (executing!=null && ((currentSkillIndex<0 && executing.active.attackSteps.IndexOf(step)==0)
            || (currentSkillIndex==1 && executing.active.attackSteps.IndexOf(step)==1))));

    public IEnumerator ExecuteTimedMovement(BossAttackStepData step)
    {
        if(!ReferenceEquals(step,activePull))
        {
            if(currentSkillIndex==1){RefreshSkillDirection();Play("skill2_Dash");}
            yield return Stage5PatternMotion.Dash(step,this,()=>isDead,currentSkillIndex<0);
            if(currentSkillIndex==1)Play("skill2_End");
            yield break;
        }
        // 도중에 플레이어가 범위에 들어오거나 벗어나도 준비시간은 끝까지 유지한다.
        float duration = Mathf.Max(0f, Delay(step.duration));
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
