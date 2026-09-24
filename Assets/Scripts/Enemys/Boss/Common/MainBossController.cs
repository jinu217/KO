using System.Collections;
using UnityEngine;

public class MainBossController : MidBossController
{
    [Header("메인보스 애니메이션")]
    [SerializeField] private Animator bossAnimator;
    [Header("방향 전환")]
    [SerializeField, Tooltip("몸체 파츠 전체를 담은 시각 루트. Stage1_MainBoss 아래 MainBoss 1을 연결합니다.")]
    private Transform visualRoot;
    [SerializeField, Tooltip("원본 그림이 왼쪽을 보는 경우에만 켭니다. 현재 원본은 오른쪽(+X)을 봅니다.")]
    private bool artworkFacesLeft = false;
    private float visualScaleX = 1f;
    private int shownPhase = 1;
    private bool phaseAnimationPlaying;
    private string activeAnimation;
    private bool desireOwnsAttackLock;
    protected MainBossData MainData => data as MainBossData;
    protected virtual string AnimationPrefix => "MainBoss1_";
    protected virtual string VisualRootName => "MainBoss 1";
    protected virtual bool HasPhaseEntryAnimations => true;
    protected virtual bool UsesPhase3ActionStates => true;
    protected virtual string DesireAnimationAction => "Dskill";
    protected virtual string SkillAnimationAction(int index) => index < 0 ? "Attack" : "skill" + (index + 1);


    protected override void Awake()
    {
        base.Awake();
        if (bossAnimator == null) bossAnimator = GetComponentInChildren<Animator>(true);
        if (visualRoot == null) visualRoot = transform.Find(VisualRootName);
        if (visualRoot != null && visualRoot != transform)
        {
            visualScaleX = Mathf.Abs(visualRoot.localScale.x);
            ApplyFacingDirection();
        }
    }

    protected override void ApplyFacingDirection()
    {
        // 물리/감지 루트는 유지하고 조립된 시각 하이어라키 전체를 반전합니다.
        if (visualRoot == null || visualRoot == transform) return;
        Vector3 scale = visualRoot.localScale;
        scale.x = visualScaleX * (lookDirection < 0 ? -1f : 1f) * (artworkFacesLeft ? -1f : 1f);
        visualRoot.localScale = scale;
    }

    protected virtual void LateUpdate()
    {
        // 클립이 시각 루트 Scale을 기록했더라도 현재 바라보는 방향을 유지합니다.
        ApplyFacingDirection();
        // 종료 훅과 별개로 실제 AI 상태를 반영해 공격 플래그가 남지 않게 합니다.
        SetBusy(isAttacking || phaseAnimationPlaying);
        float moveSpeed = !isAttacking && rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;
        if (HasParameter("MoveSpeed", AnimatorControllerParameterType.Float)) bossAnimator.SetFloat("MoveSpeed", moveSpeed);
        if (HasParameter("IsChasing", AnimatorControllerParameterType.Bool)) bossAnimator.SetBool("IsChasing", isChasing);
        if (HasParameter("LocomotionSpeed", AnimatorControllerParameterType.Float))
            bossAnimator.SetFloat("LocomotionSpeed", Mathf.Clamp(moveSpeed / (isChasing ? 2.8f : 1.4f), 0.5f, 2f));
    }

    protected bool HasParameter(string parameter, AnimatorControllerParameterType type)
    {
        if (bossAnimator == null || bossAnimator.runtimeAnimatorController == null) return false;
        foreach (var p in bossAnimator.parameters)
            if (p.name == parameter && p.type == type) return true;
        return false;
    }

    protected void SetChasingAnimation(bool value)
    {
        if (HasParameter("IsChasing", AnimatorControllerParameterType.Bool)) bossAnimator.SetBool("IsChasing", value);
    }

    private void SetBusy(bool value)
    {
        if (HasParameter("IsAttacking", AnimatorControllerParameterType.Bool))
            bossAnimator.SetBool("IsAttacking", value);
    }

    protected bool PlayAction(string action, bool scaleWithAttackSpeed = true)
    {
        if (!HasParameter(action, AnimatorControllerParameterType.Trigger)) return false;
        string state = AnimationPrefix + action;
        if (UsesPhase3ActionStates && shownPhase >= 3 && action != "P2E" && action != "P3E") state += "_Phase3";
        if (!bossAnimator.HasState(0, Animator.StringToHash("Base Layer." + state))) return false;
        foreach (var parameter in bossAnimator.parameters)
            if (parameter.type == AnimatorControllerParameterType.Trigger) bossAnimator.ResetTrigger(parameter.name);
        if (HasParameter("AttackSpeed", AnimatorControllerParameterType.Float))
            bossAnimator.SetFloat("AttackSpeed", scaleWithAttackSpeed ? 1f / Mathf.Max(0.01f, GetSkillDelay(1f)) : 1f);
        activeAnimation = state;
        SetBusy(true);
        bossAnimator.SetTrigger(action);
        return true;
    }

    // 실행기와 애니메이션 양쪽이 끝날 때까지 다음 행동을 막습니다.
    protected IEnumerator WaitForAction(string state)
    {
        if (bossAnimator == null || string.IsNullOrEmpty(state)) yield break;
        int hash = Animator.StringToHash(state);
        float enterWait = 0f;
        while (!isDead && bossAnimator != null && bossAnimator.isActiveAndEnabled)
        {
            var current = bossAnimator.GetCurrentAnimatorStateInfo(0);
            if (current.shortNameHash == hash)
            {
                if (current.normalizedTime >= 1f) yield break;
                enterWait = 0f;
            }
            else if (!bossAnimator.IsInTransition(0)
                || bossAnimator.GetNextAnimatorStateInfo(0).shortNameHash != hash)
            {
                enterWait += Time.unscaledDeltaTime;
                if (enterWait > 0.5f) yield break;
            }
            StopMove();
            yield return null;
        }
    }

    protected override void OnSkillExecutionStarted(BossSkillData skill, int index)
    {
        base.OnSkillExecutionStarted(skill, index);
        activeAnimation = null;
        PlayAction(SkillAnimationAction(index));
    }

    protected override IEnumerator WaitForSkillAnimation(BossSkillData skill, int index)
    {
        yield return base.WaitForSkillAnimation(skill, index);
        yield return WaitForAction(activeAnimation);
    }

    protected override void OnSkillExecutionFinished(int index)
    {
        SetBusy(false);
        activeAnimation = null;
        base.OnSkillExecutionFinished(index);
    }

    public void PlayDesireAnimation()
    {
        desireOwnsAttackLock = !isAttacking;
        isAttacking = true;
        StopMove();
        PlayAction(DesireAnimationAction, false);
    }
    public IEnumerator WaitForDesireAnimation()
    {
        yield return WaitForAction(activeAnimation);
        SetBusy(false);
        activeAnimation = null;
        if (desireOwnsAttackLock) isAttacking = false;
        desireOwnsAttackLock = false;
    }

    protected override void UpdatePhase()
    {
        if (MainData == null || MainData.maxHp <= 0f) { currentPhase = 1; return; }
        float hpRate = currentHp / MainData.maxHp;
        currentPhase = hpRate <= MainData.phase3HpPercent ? 3 : hpRate <= MainData.phase2HpPercent ? 2 : 1;
        if (!HasPhaseEntryAnimations)
        {
            shownPhase = currentPhase;
            if (HasParameter("Phase", AnimatorControllerParameterType.Int)) bossAnimator.SetInteger("Phase", shownPhase);
            return;
        }
        if (!isDead && !isAttacking && !phaseAnimationPlaying && shownPhase < currentPhase)
            StartCoroutine(EnterPhases());
    }

    private IEnumerator EnterPhases()
    {
        phaseAnimationPlaying = true;
        isAttacking = true;
        StopMove();
        // 큰 피해로 두 문턱을 넘었어도 진입 모션은 2 → 3 순서로 재생합니다.
        while (!isDead && shownPhase < currentPhase)
        {
            int entering = shownPhase + 1;
            if (PlayAction(entering == 2 ? "P2E" : "P3E", false))
                yield return WaitForAction(activeAnimation);
            shownPhase = entering;
            if (HasParameter("Phase", AnimatorControllerParameterType.Int)) bossAnimator.SetInteger("Phase", shownPhase);
        }
        activeAnimation = null;
        SetBusy(false);
        phaseAnimationPlaying = false;
        isAttacking = false;
    }

    protected override void ShowHealthUI() => MainBossHealthUI.Instance?.Show(name, currentHp, data.maxHp);
    protected override void UpdateHealthUI() => MainBossHealthUI.Instance?.UpdateHP(currentHp);
    protected override void HideHealthUI() => MainBossHealthUI.Instance?.Hide();
    protected override void RecordKill()
    {
        if (SaveData.Instance != null) SaveData.Instance.run.mainBossKillCount++;
    }
}
