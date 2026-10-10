using UnityEngine;
using UnityEngine.Events;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("State")]
    [SerializeField] private bool tutorialActive = true;
    [SerializeField] private TutorialStep currentStep = TutorialStep.BasicControls;

    [Header("Damage")]
    [Tooltip("While the tutorial is active, HP does not decrease. Hit flash and knockback still run.")]
    [SerializeField] private bool suppressPlayerHpDamage = true;

    [Header("Target Rules")]
    [SerializeField] private bool allowNonTutorialTargets = true;

    [Header("Events")]
    public UnityEvent<TutorialStep> onStepChanged;

    public bool IsActive => tutorialActive;
    public TutorialStep CurrentStep => currentStep;
    public int CurrentStepIndex => (int)currentStep;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (tutorialActive)
            LogCurrentStep();
    }

    public void SetStep(TutorialStep step)
    {
        if (!tutorialActive)
            return;

        if (currentStep == step)
            return;

        currentStep = step;
        LogCurrentStep();
        onStepChanged?.Invoke(currentStep);

        if (currentStep == TutorialStep.DashUnlocked)
        {
            AdvanceIfCurrent(TutorialStep.DashUnlocked, TutorialStep.DashPositionGuide);
        }
        else if (currentStep == TutorialStep.WeaponEquipConfirmed)
        {
            AdvanceIfCurrent(TutorialStep.WeaponEquipConfirmed, TutorialStep.FirstDummyAttackable);
        }
        else if (currentStep == TutorialStep.FirstDummyDefeated)
        {
            AdvanceIfCurrent(TutorialStep.FirstDummyDefeated, TutorialStep.NextDummyAttackable);
        }
        else if (currentStep == TutorialStep.NextDummyDefeated)
        {
            AdvanceIfCurrent(TutorialStep.NextDummyDefeated, TutorialStep.GoTown);
        }
    }

    public void SetStepByIndex(int stepIndex)
    {
        SetStep((TutorialStep)Mathf.Clamp(stepIndex, 0, (int)TutorialStep.GoTown));
    }

    private void LogCurrentStep()
    {
        Debug.Log($"[Tutorial] Current Step: {(int)currentStep} - {currentStep}", this);
    }

    public void AdvanceIfCurrent(TutorialStep expectedStep, TutorialStep nextStep)
    {
        if (!tutorialActive || currentStep != expectedStep)
            return;

        SetStep(nextStep);
    }

    public bool CanUseEmpoweredDownJump()
    {
        return !tutorialActive || currentStep >= TutorialStep.EmpoweredDownJumpUnlocked;
    }

    public bool CanDash()
    {
        return !tutorialActive || currentStep >= TutorialStep.DashUnlocked;
    }

    public bool CanAttack()
    {
        return !tutorialActive || (currentStep != TutorialStep.ChestInteractionGuide && currentStep >= TutorialStep.WeaponEquipConfirmed);
    }

    public bool CanOpenInventory()
    {
        return !tutorialActive || (currentStep != TutorialStep.ChestInteractionGuide && currentStep >= TutorialStep.InventoryEquipGuide);
    }

    public bool CanUseExtraAction()
    {
        return !tutorialActive || (currentStep != TutorialStep.ChestInteractionGuide && currentStep >= TutorialStep.WeaponEquipConfirmed);
    }

    public bool ShouldSuppressPlayerHpDamage()
    {
        return tutorialActive && suppressPlayerHpDamage;
    }

    public bool CanDamageTarget(TutorialTrainingDummy dummy)
    {
        if (!tutorialActive)
            return true;

        if (dummy == null)
            return allowNonTutorialTargets;

        return dummy.CanReceiveDamageAtStep(currentStep);
    }

    public void NotifyBookInteracted()
    {
        if (!tutorialActive || currentStep != TutorialStep.BookInteractGuide)
            return;

        SetStep(TutorialStep.BookReadComplete);
        AdvanceIfCurrent(TutorialStep.BookReadComplete, TutorialStep.MovePositionGuide);
    }

    public void NotifyChestInteracted()
    {
        AdvanceIfCurrent(TutorialStep.ChestInteractionGuide, TutorialStep.InventoryEquipGuide);
    }

    public void NotifyInventoryClosed()
    {
        AdvanceIfCurrent(TutorialStep.InventoryEquipGuide, TutorialStep.WeaponEquipConfirmed);
    }

    public void NotifyWeaponEquipped()
    {
    }

    public void NotifyEmpoweredDownJumpHit(TutorialTrainingDummy dummy)
    {
        if (dummy != null && dummy.DummyKind == TutorialDummyKind.DownJump)
            AdvanceIfCurrent(TutorialStep.UseEmpoweredDownJumpOnFirstDummy, TutorialStep.DashUnlocked);
    }

    public void NotifyDummyDefeated(TutorialTrainingDummy dummy)
    {
        if (dummy == null)
            return;

        if (dummy.DummyKind == TutorialDummyKind.First)
            AdvanceIfCurrent(TutorialStep.FirstDummyAttackable, TutorialStep.FirstDummyDefeated);
        else if (dummy.DummyKind == TutorialDummyKind.Next)
            AdvanceIfCurrent(TutorialStep.NextDummyAttackable, TutorialStep.NextDummyDefeated);
    }

}
