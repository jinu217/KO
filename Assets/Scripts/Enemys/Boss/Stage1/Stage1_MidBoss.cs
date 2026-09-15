using UnityEngine;

// 1스테이지 중간보스 전용 표식 클래스.
// 공격과 스킬은 연결된 MidBossData의 Basic Attack / Skills가 실행한다.
public sealed class Stage1_MidBoss : MidBossController
{
    [Header("Stage1 중간보스 거리 쿨타임 감소")]
    [SerializeField, Min(0f), Tooltip("3스킬을 한 번 이상 사용한 뒤 이 거리 이상 벌어지면 남은 쿨타임을 한 번 50% 줄입니다.")]
    private float skill3ReductionDistance = 6f;

    protected override void Update()
    {
        base.Update();
        TryReduceRemainingSkillCooldownByDistance(2, skill3ReductionDistance);
    }

    public enum PatternAnimation { Auto, VerticalAttack, HorizontalAttack, Buff, Combo2, Combo3, Dash }

    [System.Serializable]
    public class SkillAnimationOverride
    {
        [Tooltip("-1: 기본 공격, 0: Skills 첫 번째, 1: 두 번째 ...")]
        public int skillIndex;
        public PatternAnimation animation = PatternAnimation.Auto;
    }

    [Header("애니메이션")]
    [SerializeField] private Animator bossAnimator;
    [SerializeField, Tooltip("자동 분류가 맞지 않는 스킬만 인덱스와 애니메이션을 지정합니다.")]
    private SkillAnimationOverride[] animationOverrides = new SkillAnimationOverride[0];
    private static readonly string[] PatternTriggers =
        { "VerticalAttack", "HorizontalAttack", "Buff", "Combo2", "Combo3", "Dash" };
    private readonly System.Collections.Generic.Dictionary<string, AnimatorControllerParameterType> animationParameters =
        new System.Collections.Generic.Dictionary<string, AnimatorControllerParameterType>();
    private RuntimeAnimatorController cachedAnimationController;
    private readonly System.Collections.Generic.HashSet<string> animationWarnings =
        new System.Collections.Generic.HashSet<string>();

    protected override void Awake()
    {
        base.Awake();
        if (bossAnimator == null) bossAnimator = GetComponentInChildren<Animator>(true);
        RefreshAnimationParameters();
    }

    private void RefreshAnimationParameters()
    {
        animationParameters.Clear();
        cachedAnimationController = bossAnimator != null ? bossAnimator.runtimeAnimatorController : null;
        if (cachedAnimationController == null) return;
        foreach (var parameter in bossAnimator.parameters)
            animationParameters[parameter.name] = parameter.type;
    }

    private bool HasAnimationParameter(string name, AnimatorControllerParameterType type)
    {
        if (bossAnimator == null || bossAnimator.runtimeAnimatorController == null) return false;
        if (cachedAnimationController != bossAnimator.runtimeAnimatorController) RefreshAnimationParameters();
        return animationParameters.TryGetValue(name, out var actual) && actual == type;
    }

    private void LateUpdate()
    {
        if (HasAnimationParameter("IsMoving", AnimatorControllerParameterType.Bool))
            bossAnimator.SetBool("IsMoving", !isDead && !isAttacking && rb != null && Mathf.Abs(rb.linearVelocity.x) > 0.05f);
        if (HasAnimationParameter("IsAttacking", AnimatorControllerParameterType.Bool))
            bossAnimator.SetBool("IsAttacking", isAttacking && !isDead);
        if (HasAnimationParameter("AttackSpeed", AnimatorControllerParameterType.Float))
            bossAnimator.SetFloat("AttackSpeed", Mathf.Max(0.01f, 1f / GetSkillDelay(1f)));
    }

    protected override void OnSkillExecutionStarted(BossSkillData skill, int index)
    {
        base.OnSkillExecutionStarted(skill, index);
        string trigger = ResolvePatternAnimation(skill, index).ToString();
        if (!HasAnimationParameter(trigger, AnimatorControllerParameterType.Trigger))
        {
            if (animationWarnings.Add(trigger))
                Debug.LogWarning("[MidBoss1] Animator 또는 Trigger가 없습니다: " + trigger +
                    ". Animator 연결 후 Tools/MidBoss1/Setup Gameplay Animator를 실행하세요.", this);
            return;
        }
        // 이전에 소비되지 않은 패턴 Trigger가 뒤늦게 발동하지 않도록 정리합니다.
        foreach (string other in PatternTriggers)
            if (HasAnimationParameter(other, AnimatorControllerParameterType.Trigger)) bossAnimator.ResetTrigger(other);
        if (HasAnimationParameter("IsMoving", AnimatorControllerParameterType.Bool)) bossAnimator.SetBool("IsMoving", false);
        if (HasAnimationParameter("IsAttacking", AnimatorControllerParameterType.Bool)) bossAnimator.SetBool("IsAttacking", true);
        if (HasAnimationParameter("AttackSpeed", AnimatorControllerParameterType.Float))
            bossAnimator.SetFloat("AttackSpeed", Mathf.Max(0.01f, 1f / GetSkillDelay(1f)));
        bossAnimator.SetTrigger(trigger);
    }

    private PatternAnimation ResolvePatternAnimation(BossSkillData skill, int index)
    {
        if (animationOverrides != null)
            foreach (var entry in animationOverrides)
                if (entry != null && entry.skillIndex == index && entry.animation != PatternAnimation.Auto)
                    return entry.animation;
        // 스킬 순서는 가정하지 않고 실제 데이터의 행동 종류/타격 수로 기본 분류합니다.
        int hits = 0;
        if (skill != null && skill.HasActive && skill.active != null && skill.active.attackSteps != null)
            foreach (var step in skill.active.attackSteps)
            {
                if (step == null) continue;
                if (step.actionType == BossActionType.DashAttack) return PatternAnimation.Dash;
                if (step.actionType == BossActionType.MeleeAttack || step.actionType == BossActionType.Slam)
                    hits += Mathf.Max(0, step.attackCount);
            }
        if (hits >= 3) return PatternAnimation.Combo3;
        if (hits == 2) return PatternAnimation.Combo2;
        if (hits == 0 && skill != null && skill.HasBuff) return PatternAnimation.Buff;
        return PatternAnimation.VerticalAttack;
    }
}
