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
    [SerializeField] private PlayerStatus playerStatus;
    [SerializeField] private float attackBpmMultiplier = 1.5f;

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

    private void Awake()
    {
        CachePlayerStatus();
    }

    private void Update()
    {
        if (dummyKind != TutorialDummyKind.Next)
            return;

        TutorialManager manager = TutorialManager.Instance;
        if (manager == null || !manager.IsActive)
            return;

        if (manager.CurrentStep < TutorialStep.NextDummyAttackable || manager.CurrentStep > TutorialStep.NextDummyDefeated)
            return;

        if (playerStatus == null)
            CachePlayerStatus();

        if (playerStatus != null && (playerStatus.CurrentBpmRank == BpmRank.D || playerStatus.IsExhausted))
            DestroyDummy();
    }

    public void NotifyDamagedByEmpoweredDownJump()
    {
        PlayHitReaction(destroyOnEmpoweredDownJump);
        TutorialManager.Instance?.NotifyEmpoweredDownJumpHit(this);

        if (destroyOnEmpoweredDownJump)
            DestroyDummy();
    }

    public void NotifyDamagedByAttack()
    {
        PlayHitReaction(dummyKind != TutorialDummyKind.Next && destroyOnAttack);
        AddAttackBpmBonus();

        if (dummyKind == TutorialDummyKind.Next)
            return;

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

    private void PlayHitReaction(bool finalHit)
    {
        var enemy = GetComponentInParent<EnemyController>();
        var target = objectToDisable != null ? objectToDisable : enemy != null ? enemy.gameObject : gameObject;
        ObjectHitReaction.Play(target, finalHit);
    }

    private void CachePlayerStatus()
    {
        if (playerStatus != null)
            return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerStatus = player.GetComponent<PlayerStatus>();
    }

    private void AddAttackBpmBonus()
    {
        if (playerStatus == null)
            CachePlayerStatus();

        if (playerStatus == null)
            return;

        float bonusMultiplier = Mathf.Max(0f, attackBpmMultiplier - 1f);
        playerStatus.AddBPM(playerStatus.attackBpmGain * bonusMultiplier);
    }
}
