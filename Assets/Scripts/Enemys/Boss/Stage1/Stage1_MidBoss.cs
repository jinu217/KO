using UnityEngine;

// 1스테이지 중간보스 전용 표식 클래스.
// 공격과 스킬은 연결된 MidBossData의 Basic Attack / Skills가 실행한다.
public sealed class Stage1_MidBoss : MidBossController, IBossDashAnimationContext
{
    [Header("Stage1 중간보스 거리 쿨타임 감소")]
    [SerializeField, Min(0f), Tooltip("3스킬을 한 번 이상 사용한 뒤 이 거리 이상 벌어지면 남은 쿨타임을 한 번 50% 줄입니다.")]
    private float skill3ReductionDistance = 6f;

    protected override void Update()
    {
        base.Update();
        TryReduceRemainingSkillCooldownByDistance(2, skill3ReductionDistance);
    }

    public enum PatternAnimation { Auto = 0, Buff = 3, Combo2 = 4, Combo3 = 5, Dash = 6, BasicAttack = 7 }

    [System.Serializable]
    public class SkillAnimationOverride
    {
        [Tooltip("0: Skills 첫 번째, 1: 두 번째 ... 기본 공격은 BasicAttack으로 고정됩니다.")]
        public int skillIndex;
        public PatternAnimation animation = PatternAnimation.Auto;
    }

    [Header("방향 전환")]
    [SerializeField, Tooltip("Stage1_MidBoss 아래의 MidBoss 1 오브젝트. 비워두면 이름으로 찾습니다.")]
    private Transform visualRoot;
    [SerializeField, Tooltip("X 스케일이 양수일 때 원본 그림이 왼쪽을 바라보면 체크합니다.")]
    private bool artworkFacesLeft = false;
    private float visualScaleX = 1f;

    [Header("애니메이션")]
    [SerializeField] private Animator bossAnimator;
    [SerializeField, Tooltip("자동 분류가 맞지 않는 스킬만 인덱스와 애니메이션을 지정합니다.")]
    private SkillAnimationOverride[] animationOverrides = new SkillAnimationOverride[0];
    private static readonly string[] PatternTriggers =
        { "BasicAttack", "Buff", "Combo2", "Combo3", "Dash" };
    private readonly System.Collections.Generic.Dictionary<string, AnimatorControllerParameterType> animationParameters =
        new System.Collections.Generic.Dictionary<string, AnimatorControllerParameterType>();
    private RuntimeAnimatorController cachedAnimationController;
    private readonly System.Collections.Generic.HashSet<string> animationWarnings =
        new System.Collections.Generic.HashSet<string>();

    protected override void Awake()
    {
        base.Awake();
        if (visualRoot == null) visualRoot = transform.Find("MidBoss 1");
        if (visualRoot == null || visualRoot == transform || !visualRoot.IsChildOf(transform))
        {
            Debug.LogWarning("[MidBoss1] Visual Root에 자식 MidBoss 1을 연결하세요.", this);
            visualRoot = null;
        }
        else
        {
            visualScaleX = Mathf.Abs(visualRoot.localScale.x);
            if (visualScaleX < 0.0001f) visualScaleX = 1f;
            ApplyFacingDirection();
        }
        if (bossAnimator == null) bossAnimator = GetComponentInChildren<Animator>(true);
        RefreshAnimationParameters();
    }

    // 패턴 시작 직전에 정한 방향을 연격/마무리까지 유지합니다.
    // 실행기가 매 타격 전 방향 갱신을 요청해도 lookDirection 자체를 바꾸지 않습니다.
    public override void RefreshSkillDirection()
    {
        if (isAttacking) return;
        base.RefreshSkillDirection();
    }

    protected override void ApplyFacingDirection()
    {
        if (visualRoot == null) return;
        int originalDirection = artworkFacesLeft ? -1 : 1;
        Vector3 scale = visualRoot.localScale;
        scale.x = visualScaleX * (lookDirection == originalDirection ? 1f : -1f);
        visualRoot.localScale = scale;
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
        if (HasAnimationParameter("MoveSpeed", AnimatorControllerParameterType.Float))
            bossAnimator.SetFloat("MoveSpeed", !isDead && rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f);
        if (HasAnimationParameter("IsChasing", AnimatorControllerParameterType.Bool))
            bossAnimator.SetBool("IsChasing", isChasing && !isDead);
        if (HasAnimationParameter("IsAttacking", AnimatorControllerParameterType.Bool))
            bossAnimator.SetBool("IsAttacking", isAttacking && !isDead);
        if (HasAnimationParameter("AttackSpeed", AnimatorControllerParameterType.Float))
            bossAnimator.SetFloat("AttackSpeed", Mathf.Max(0.01f, 1f / GetSkillDelay(1f)));
    }

    protected override void OnSkillExecutionStarted(BossSkillData skill, int index)
    {
        base.OnSkillExecutionStarted(skill, index);
        string trigger = ResolvePatternAnimation(skill, index).ToString();
        SetDashAnimationBool("DashMoving", false);
        SetDashAnimationBool("DashFinished", false);
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
        if (HasAnimationParameter("MoveSpeed", AnimatorControllerParameterType.Float)) bossAnimator.SetFloat("MoveSpeed", 0f);
        if (HasAnimationParameter("IsAttacking", AnimatorControllerParameterType.Bool)) bossAnimator.SetBool("IsAttacking", true);
        if (HasAnimationParameter("AttackSpeed", AnimatorControllerParameterType.Float))
            bossAnimator.SetFloat("AttackSpeed", Mathf.Max(0.01f, 1f / GetSkillDelay(1f)));
        bossAnimator.SetTrigger(trigger);
    }

    protected override System.Collections.IEnumerator WaitForSkillAnimation(BossSkillData skill, int index)
    {
        PatternAnimation pattern = ResolvePatternAnimation(skill, index);
        if (pattern != PatternAnimation.Buff && pattern != PatternAnimation.Combo2 && pattern != PatternAnimation.Combo3)
            yield break;
        if (bossAnimator == null || !bossAnimator.isActiveAndEnabled || bossAnimator.runtimeAnimatorController == null)
            yield break;
        // 실행 지연이 0인 버프도 Trigger를 Animator가 평가한 뒤 완료 여부를 확인합니다.
        StopMove();
        yield return null;
        int finishingState = Animator.StringToHash("MidBoss 1_" + pattern);
        while (!isDead && bossAnimator != null && bossAnimator.isActiveAndEnabled &&
            bossAnimator.runtimeAnimatorController != null && bossAnimator.layerCount > 0)
        {
            bool currentFinishing = bossAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash == finishingState;
            bool nextFinishing = bossAnimator.IsInTransition(0) &&
                bossAnimator.GetNextAnimatorStateInfo(0).shortNameHash == finishingState;
            // 버프/콤보의 마무리 및 Idle 전환이 끝날 때까지 AI 잠금을 유지합니다.
            if (!currentFinishing && !nextFinishing) break;
            StopMove();
            yield return null;
        }
    }

    private PatternAnimation ResolvePatternAnimation(BossSkillData skill, int index)
    {
        if (index < 0) return PatternAnimation.BasicAttack;
        if (animationOverrides != null)
            foreach (var entry in animationOverrides)
                if (entry != null && entry.skillIndex == index && entry.animation != PatternAnimation.Auto &&
                    System.Enum.IsDefined(typeof(PatternAnimation), entry.animation))
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
        return PatternAnimation.BasicAttack;
    }
    private void SetDashAnimationBool(string parameter, bool value)
    {
        if (HasAnimationParameter(parameter, AnimatorControllerParameterType.Bool))
            bossAnimator.SetBool(parameter, value);
    }

    public void OnDashMovementStarted()
    {
        SetDashAnimationBool("DashFinished", false);
        SetDashAnimationBool("DashMoving", true);
        // 실제 이동 시작 프레임에 루프 상태로 진입합니다. 짧은 대쉬도 준비 모션에 가려지지 않습니다.
        if (bossAnimator != null && bossAnimator.isActiveAndEnabled && bossAnimator.layerCount > 0)
        {
            int state = Animator.StringToHash(bossAnimator.GetLayerName(0) + ".MidBoss 1_DashMove");
            if (bossAnimator.HasState(0, state))
            {
                if (HasAnimationParameter("Dash", AnimatorControllerParameterType.Trigger)) bossAnimator.ResetTrigger("Dash");
                bossAnimator.Play(state, 0, 0f);
            }
        }
    }

    public void OnDashMovementStopped()
    {
        SetDashAnimationBool("DashMoving", false);
        SetDashAnimationBool("DashFinished", true);
        if (bossAnimator != null && bossAnimator.isActiveAndEnabled && bossAnimator.layerCount > 0)
        {
            int state = Animator.StringToHash(bossAnimator.GetLayerName(0) + ".MidBoss 1_DashEnd");
            if (bossAnimator.HasState(0, state)) bossAnimator.Play(state, 0, 0f);
        }
    }

    public System.Collections.IEnumerator WaitForDashRecovery()
    {
        // DashEnd 원본 0.30초 + Idle 전환 여유. 실제 공격속도와 같은 배율로 진행합니다.
        if (!HasAnimationParameter("DashMoving", AnimatorControllerParameterType.Bool)) yield break;
        float progress = 0f;
        while (progress < 0.34f && !isDead)
        {
            progress += Time.deltaTime / Mathf.Max(0.0001f, GetSkillDelay(1f));
            yield return null;
        }
    }
}
