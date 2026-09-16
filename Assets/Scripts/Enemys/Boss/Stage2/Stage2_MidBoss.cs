using System.Collections;
using UnityEngine;

public sealed class Stage2_MidBoss : MidBossController
{
    [Header("Stage2 애니메이션")]
    [SerializeField] private Animator bossAnimator;
    [SerializeField, Tooltip("Stage2_MidBoss 아래 MidBoss 2 시각 루트")]
    private Transform visualRoot;
    [SerializeField] private bool artworkFacesLeft;
    [Header("거리 쿨타임 감소")]
    [SerializeField, Min(0f), Tooltip("3스킬 사용 후 이 거리 이상 벌어지면 남은 쿨타임을 한 번 50% 줄입니다.")]
    private float skill3ReductionDistance = 6.5f;
    private float visualScaleX = 1f;
    private string activeState;

    protected override void Awake()
    {
        base.Awake();
        if (visualRoot == null) visualRoot = transform.Find("MidBoss 2");
        if (bossAnimator == null) bossAnimator = GetComponentInChildren<Animator>(true);
        if (visualRoot != null && visualRoot != transform) visualScaleX = Mathf.Abs(visualRoot.localScale.x);
        ApplyFacingDirection();
    }
    protected override void Update()
    {
        base.Update();
        TryReduceRemainingSkillCooldownByDistance(2, skill3ReductionDistance);
    }
    private bool Has(string name, AnimatorControllerParameterType type)
    {
        if (bossAnimator == null || bossAnimator.runtimeAnimatorController == null) return false;
        foreach (var p in bossAnimator.parameters) if (p.name == name && p.type == type) return true;
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
    protected override void ApplyFacingDirection()
    {
        if (visualRoot == null || visualRoot == transform) return;
        Vector3 scale = visualRoot.localScale;
        scale.x = visualScaleX * (lookDirection < 0 ? -1 : 1) * (artworkFacesLeft ? -1 : 1);
        visualRoot.localScale = scale;
    }
    public override void RefreshSkillDirection()
    {
        if (!isAttacking) base.RefreshSkillDirection();
    }
    private BossSkillData dashSource;
    private BossSkillData timedDashSkill;
    private float cachedDashTimeScale = -1f;
    protected override BossSkillData GetSkillData(int index)
    {
        var source = base.GetSkillData(index);
        if (index != 2 || source == null) return source;
        float timeScale = GetSkillDelay(1f);
        if (timedDashSkill == null || dashSource != source || !Mathf.Approximately(cachedDashTimeScale, timeScale))
        {
            // 실행기의 돌진 duration은 배속되지 않으므로 런타임 복사본에만 적용합니다.
            timedDashSkill = JsonUtility.FromJson<BossSkillData>(JsonUtility.ToJson(source));
            dashSource = source;
            cachedDashTimeScale = timeScale;
            if (timedDashSkill.active != null && timedDashSkill.active.attackSteps != null)
                foreach (var step in timedDashSkill.active.attackSteps)
                    if (step != null && step.actionType == BossActionType.DashAttack)
                    {
                        step.duration *= timeScale;
                        step.moveSpeed /= Mathf.Max(.01f, timeScale);
                    }
        }
        return timedDashSkill;
    }
    protected override void OnSkillExecutionStarted(BossSkillData skill, int index)
    {
        base.OnSkillExecutionStarted(skill, index);
        activeState = null;
        string action = index < 0 ? "Attack" : "Skill" + (index + 1);
        if (!Has(action, AnimatorControllerParameterType.Trigger)) return;
        string state = "MidBoss2_" + action;
        if (!bossAnimator.HasState(0, Animator.StringToHash("Base Layer." + state))) return;
        foreach (string trigger in new[] { "Attack", "Skill1", "Skill2", "Skill3" })
            if (Has(trigger, AnimatorControllerParameterType.Trigger)) bossAnimator.ResetTrigger(trigger);
        if (Has("AttackSpeed", AnimatorControllerParameterType.Float))
            bossAnimator.SetFloat("AttackSpeed", 1f / Mathf.Max(.01f, GetSkillDelay(1f)));
        if (Has("IsAttacking", AnimatorControllerParameterType.Bool)) bossAnimator.SetBool("IsAttacking", true);
        activeState = state;
        bossAnimator.SetTrigger(action);
    }
    protected override IEnumerator WaitForSkillAnimation(BossSkillData skill, int index)
    {
        yield return base.WaitForSkillAnimation(skill, index);
        if (bossAnimator == null || activeState == null) yield break;
        int hash = Animator.StringToHash(activeState);
        float missingTime = 0;
        while (!isDead && bossAnimator != null && bossAnimator.isActiveAndEnabled)
        {
            var state = bossAnimator.GetCurrentAnimatorStateInfo(0);
            if (state.shortNameHash == hash)
            {
                if (state.normalizedTime >= 1f) break;
                missingTime = 0;
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
