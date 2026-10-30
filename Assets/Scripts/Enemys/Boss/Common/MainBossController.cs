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
    private bool animationReturningToIdle;
    private string activeAnimation;
    private bool desireOwnsAttackLock;
    
    //============== 메인보스 대화 ==============
    [Header("메인보스 대화")]
    [SerializeField] private MainBossDialogueController dialogueController;
    [SerializeField] private string bossId = "Knight1";
    [SerializeField] private bool playEncounterDialogue = true;
    private bool encounterDialogueStarted;
    private bool encounterDialogueRunning;

    protected MainBossData MainData => data as MainBossData;
    protected virtual string AnimationPrefix => "MainBoss1_";
    protected virtual string VisualRootName => "MainBoss 1";
    protected virtual bool HasPhaseEntryAnimations => true;
    protected virtual bool UsesPhase3ActionStates => true;
    protected virtual string DesireAnimationAction => "Dskill";
    protected virtual string SkillAnimationAction(int index) => index < 0 ? "Attack" : "skill" + (index + 1);


    protected override void Awake()
    {
        //============== 기본 초기화 ==============
        base.Awake();
        if (bossAnimator == null) bossAnimator = GetComponentInChildren<Animator>(true);
        if (visualRoot == null) visualRoot = transform.Find(VisualRootName);

        //============== 대화 컨트롤러 자동 연결 ==============
        // Inspector에서 연결하지 않았을 때 씬에서 찾아 연결합니다.
        if (dialogueController == null)
            dialogueController = FindFirstObjectByType<MainBossDialogueController>();

        //============== 처음 바라볼 방향: 왼쪽 고정 ==============
        lookDirection = -1;

        //============== 시각 루트 방향 적용 ==============
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
    
    //============== 조우 대화 시작 (감지/트리거 공통 진입점) ==============
    // 보스 감지와 BossDialogueTrigger 양쪽에서 이 메서드로만 조우 대화를 시작합니다.
    // 보스당 한 번만 시도하며, 대화가 실제로 시작되면 true를 반환합니다.
    public bool TryStartEncounterDialogue()
    {
        if (isDead || !playEncounterDialogue)
            return false;

        // 이미 시도했거나 진행 중이면 다시 시작하지 않습니다.
        if (encounterDialogueStarted || encounterDialogueRunning)
            return false;

        if (dialogueController == null)
        {
            Debug.LogWarning(
                $"[{name}] MainBossDialogueController가 연결되지 않았습니다.", this);
            return false;
        }

        // 한 보스의 조우 대화는 한 번만 시도합니다.
        encounterDialogueStarted = true;

        if (dialogueController.TryPlayEncounter(this, bossId))
        {
            // 대화가 시작되면 감지 처리(HP바 표시 등)는 대화가 끝난 뒤로 미룹니다.
            encounterDialogueRunning = true;
            return true;
        }

        return false;
    }

    //============== 조우 대화 ==============
    protected override void OnPlayerDetected()
    {
        // 조우 대화가 진행 중이면 감지 처리를 대화가 끝날 때까지 미룹니다.
        if (encounterDialogueRunning)
            return;

        // 대화가 시작되면 감지 처리는 OnEncounterDialogueFinished에서 실행됩니다.
        if (TryStartEncounterDialogue())
            return;

        // 대화를 이미 했거나, 쓰지 않거나, 대사가 없으면 기존처럼 바로 감지 처리합니다.
        base.OnPlayerDetected();
    }

    //============== 조우 대사 종료 후 감지 처리 ==============
    // MainBossDialogueController가 조우 대화가 끝났을 때 호출합니다.
    public void OnEncounterDialogueFinished()
    {
        encounterDialogueRunning = false;

        // 대화 중 이미 죽은 경우에는 아무것도 하지 않습니다.
        if (isDead)
            return;

        // 조우 대사가 끝난 뒤 HP UI를 표시합니다.
        ShowHealthUIIfAllowed();

        // 미뤄 둔 감지 처리를 실행합니다.
        base.OnPlayerDetected();
    }

    //============== 사망 후 대화 ==============
    protected override IEnumerator BeforeDeactivateAfterDeath()
    {
        // HP UI는 처치 후 보스 대화가 시작되기 전에 숨깁니다.
        HideHealthUI();

        if (dialogueController == null)
        {
            Debug.LogError(
                $"[{name}] MainBossDialogueController가 연결되지 않았습니다.",
                this);
            yield break;
        }

        Debug.Log($"[{name}] 쓰러진 상태에서 처치 후 보스 대화를 시작합니다.", this);
        yield return dialogueController.PlayAfterDefeat(this, bossId);
        Debug.Log($"[{name}] 처치 후 보스 대화가 끝났습니다.", this);
    }

    protected virtual void LateUpdate()
    {
        if (isDead) return;
        // 클립이 시각 루트 Scale을 기록했더라도 현재 바라보는 방향을 유지합니다.
        ApplyFacingDirection();
        // 종료 훅과 별개로 실제 AI 상태를 반영해 공격 플래그가 남지 않게 합니다.
        SetBusy(!dialoguePaused && (isAttacking || phaseAnimationPlaying) && !animationReturningToIdle);

        float moveSpeed = !dialoguePaused && !isAttacking && rb != null
            ? Mathf.Abs(rb.linearVelocity.x)
            : 0f;

        if (HasParameter("IsChasing", AnimatorControllerParameterType.Bool)) bossAnimator.SetBool("IsChasing", isChasing && !dialoguePaused);
        if (HasParameter("MoveSpeed", AnimatorControllerParameterType.Float)) bossAnimator.SetFloat("MoveSpeed", moveSpeed);
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
        if (isDead) return false;
        if (!HasParameter(action, AnimatorControllerParameterType.Trigger)) return false;
        string state = AnimationPrefix + action;
        if (UsesPhase3ActionStates && shownPhase >= 3 && action != "P2E" && action != "P3E") state += "_Phase3";
        if (!bossAnimator.HasState(0, Animator.StringToHash("Base Layer." + state))) return false;
        foreach (var parameter in bossAnimator.parameters)
            if (parameter.type == AnimatorControllerParameterType.Trigger) bossAnimator.ResetTrigger(parameter.name);
        if (HasParameter("AttackSpeed", AnimatorControllerParameterType.Float))
            bossAnimator.SetFloat("AttackSpeed", scaleWithAttackSpeed ? 1f / Mathf.Max(0.01f, GetSkillDelay(1f)) : 1f);
        activeAnimation = state;
        animationReturningToIdle = false;
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
                if (current.normalizedTime >= 1f)
                {
                    // 공격 종료 조건을 Animator가 평가하고 실제로 상태를 빠져나갈 때까지 잠근다.
                    animationReturningToIdle = true;
                    SetBusy(false);
                    float exitWait = 0f;
                    while (!isDead && bossAnimator != null && bossAnimator.isActiveAndEnabled
                        && bossAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash == hash
                        && exitWait < 0.5f)
                    {
                        StopMove();
                        exitWait += Time.deltaTime;
                        yield return null;
                    }
                    yield break;
                }
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
        animationReturningToIdle = false;
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
        animationReturningToIdle = false;
        activeAnimation = null;
        base.OnSkillExecutionFinished(index);
    }

    public void PlayDesireAnimation()
    {
        if (isDead) return;
        desireOwnsAttackLock = !isAttacking;
        isAttacking = true;
        StopMove();
        PlayAction(DesireAnimationAction, false);
    }
    public IEnumerator WaitForDesireAnimation()
    {
        yield return WaitForAction(activeAnimation);
        SetBusy(false);
        animationReturningToIdle = false;
        activeAnimation = null;
        if (desireOwnsAttackLock) isAttacking = false;
        desireOwnsAttackLock = false;
        DeferNextCombatAction();
    }

    // 전용 욕망 코루틴을 취소할 때 남은 애니메이션 잠금 정보도 정리한다.
    // 실제 행동 잠금은 취소를 요청한 욕망 컴포넌트가 다음 상태에 맞게 설정한다.
    public void CancelDesireAnimation()
    {
        activeAnimation = null;
        desireOwnsAttackLock = false;
        animationReturningToIdle = false;
        SetBusy(false);
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
        if (!isDead && !isAttacking && !AnimatorRecoveryPending && !phaseAnimationPlaying && shownPhase < currentPhase)
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
        animationReturningToIdle = false;
        phaseAnimationPlaying = false;
        isAttacking = false;
        DeferNextCombatAction();
    }

    protected override Animator GetDeathAnimator()
    {
        return bossAnimator != null ? bossAnimator : base.GetDeathAnimator();
    }

    protected override void Die()
    {
        if (isDead) return;
        phaseAnimationPlaying = false;
        desireOwnsAttackLock = false;
        activeAnimation = null;
        base.Die();
    }

    //============== 조우 대사 중 HP UI 표시 방지 ==============
    protected override bool CanShowHealthUIOnDetection()
    {
        return !encounterDialogueRunning;
    }

    protected override void ShowHealthUI() => MainBossHealthUI.Instance?.Show(name, currentHp, data.maxHp);
    protected override void UpdateHealthUI() => MainBossHealthUI.Instance?.UpdateHP(currentHp);
    protected override void HideHealthUI() => MainBossHealthUI.Instance?.Hide();
    protected override void RecordKill()
    {
        if (SaveData.Instance != null) SaveData.Instance.run.mainBossKillCount++;
    }
}
