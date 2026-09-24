using System.Collections;
using UnityEngine;

// Requires the shared MidBossController skill-animation hooks already used by Stage1/2.
public sealed class Stage3_MidBoss : MidBossController
{
    [SerializeField] private Animator bossAnimator;
    [SerializeField] private Transform visualRoot;
    private float baseScaleX = 1f;
    private string activeState;

    protected override void Awake()
    {
        base.Awake();
        if (visualRoot == null) visualRoot = transform.Find("MidBoss 3");
        if (visualRoot != null)
        {
            baseScaleX = Mathf.Abs(visualRoot.localScale.x);
            if (bossAnimator == null) bossAnimator = visualRoot.GetComponent<Animator>();
        }
        if (bossAnimator == null) bossAnimator = GetComponentInChildren<Animator>(true);
        ApplyFacingDirection();
    }

    protected override void ApplyFacingDirection()
    {
        if (visualRoot == null || visualRoot == transform) return;
        Vector3 scale = visualRoot.localScale;
        scale.x = baseScaleX * (lookDirection < 0 ? -1f : 1f);
        visualRoot.localScale = scale;
    }

    public override void RefreshSkillDirection()
    {
        if (isAttacking) return;
        base.RefreshSkillDirection();
    }

    private bool Has(string name, AnimatorControllerParameterType type)
    {
        if (bossAnimator == null || bossAnimator.runtimeAnimatorController == null) return false;
        foreach (var p in bossAnimator.parameters)
            if (p.name == name && p.type == type) return true;
        return false;
    }

    private void LateUpdate()
    {
        ApplyFacingDirection();
        if (Has("MoveSpeed", AnimatorControllerParameterType.Float))
            bossAnimator.SetFloat("MoveSpeed", !isAttacking && rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f);
        if (Has("IsChasing", AnimatorControllerParameterType.Bool)) bossAnimator.SetBool("IsChasing", isChasing);
        if (Has("IsAttacking", AnimatorControllerParameterType.Bool)) bossAnimator.SetBool("IsAttacking", isAttacking);
    }

    protected override void OnSkillExecutionStarted(BossSkillData skill, int index)
    {
        base.OnSkillExecutionStarted(skill, index);
        activeState = null;
        string trigger = index < 0 ? "Attack" : "Skill" + (index + 1);
        string state = "MidBoss3_" + trigger;
        if (!Has(trigger, AnimatorControllerParameterType.Trigger)
            || !bossAnimator.HasState(0, Animator.StringToHash("Base Layer." + state))) return;
        foreach (var p in bossAnimator.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger) bossAnimator.ResetTrigger(p.name);
        if (Has("AttackSpeed", AnimatorControllerParameterType.Float))
            bossAnimator.SetFloat("AttackSpeed", 1f / Mathf.Max(.01f, GetSkillDelay(1f)));
        if (Has("IsAttacking", AnimatorControllerParameterType.Bool)) bossAnimator.SetBool("IsAttacking", true);
        activeState = state;
        bossAnimator.SetTrigger(trigger);
    }

    protected override IEnumerator WaitForSkillAnimation(BossSkillData skill, int index)
    {
        yield return base.WaitForSkillAnimation(skill, index);
        if (bossAnimator == null || string.IsNullOrEmpty(activeState)) yield break;
        int hash = Animator.StringToHash(activeState);
        float missingTime = 0f;
        while (!isDead && bossAnimator != null && bossAnimator.isActiveAndEnabled)
        {
            var state = bossAnimator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash == hash)
            {
                if (state.normalizedTime >= 1f) break;
                missingTime = 0f;
            }
            else
            {
                missingTime += Time.unscaledDeltaTime;
                if (missingTime > .5f) break;
            }
            StopMove();
            yield return null;
        }
    }

    protected override void OnSkillExecutionFinished(int index)
    {
        activeState = null;
        if (Has("IsAttacking", AnimatorControllerParameterType.Bool)) bossAnimator.SetBool("IsAttacking", false);
        base.OnSkillExecutionFinished(index);
    }
}
