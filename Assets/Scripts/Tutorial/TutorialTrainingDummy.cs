using UnityEngine;

public enum TutorialDummyKind
{
    DownJump,
    First,
    Next
}

public class TutorialTrainingDummy : MonoBehaviour
{
    [SerializeField] private TutorialDummyKind dummyKind;
    [SerializeField] private bool destroyOnEmpoweredDownJump = true;
    [SerializeField] private bool destroyOnAttack = true;
    [SerializeField] private GameObject objectToDisable;

    public TutorialDummyKind DummyKind => dummyKind;

    public bool CanReceiveDamageAtStep(TutorialStep step)
    {
        if (dummyKind == TutorialDummyKind.DownJump)
            return step == TutorialStep.UseEmpoweredDownJumpOnFirstDummy;

        if (dummyKind == TutorialDummyKind.First)
        {
            return step == TutorialStep.FirstDummyAttackable;
        }

        return step >= TutorialStep.NextDummyAttackable && step <= TutorialStep.NextDummyDefeated;
    }

    public void NotifyDamagedByEmpoweredDownJump()
    {
        TutorialManager.Instance?.NotifyEmpoweredDownJumpHit(this);

        if (destroyOnEmpoweredDownJump)
            DestroyDummy();
    }

    public void NotifyDamagedByAttack()
    {
        if (destroyOnAttack)
            DestroyDummy();
    }

    public void NotifyDefeated()
    {
        TutorialManager.Instance?.NotifyDummyDefeated(this);
    }

    private void DestroyDummy()
    {
        if (objectToDisable != null && !objectToDisable.activeSelf)
            return;

        if (objectToDisable == null && !gameObject.activeSelf)
            return;

        NotifyDefeated();

        GameObject targetObject = objectToDisable;
        if (targetObject == null)
        {
            EnemyController enemy = GetComponentInParent<EnemyController>();
            targetObject = enemy != null ? enemy.gameObject : gameObject;
        }

        targetObject.SetActive(false);
    }
}
