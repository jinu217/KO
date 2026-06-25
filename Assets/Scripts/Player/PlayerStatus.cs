using UnityEngine;

public enum BpmRank
{
    A,
    B,
    C,
    D
}

public class PlayerStatus : MonoBehaviour
{
    private const float MinBpm = 91f;
    private const float BpmRankAUpper = 130f;
    private const float BpmRankBUpper = 170f;
    private const float BpmRankCUpper = 200f;

    [Header("HP")]
    public float currentHp = 70f;
    public float maxHp = 100f;

    [Header("SP")]
    public float currentSp = 100f;
    public float maxSp = 100f;
    public int dashCost = 15;
    public int feverDashCost = 10;

    [Header("SP Recovery")]
    public float spRecoverySpeed = 1.25f;
    public float feverSpRecoveryMultiplier = 1.5f;
    public float meditationSpRecoveryMultiplier = 1.25f;
    public float attackSpGainSingle = 1f;
    public float attackSpGainMultiple = 2f;
    public float attackSpGainBoss = 2.5f;

    [Header("BPM")]
    public float currentBpm = MinBpm;

    [Header("BPM Gain")]
    public float attackBpmGain = 8f;
    public float dashBpmGain = 12f;
    [Tooltip("BPM gain multiplier while in rank C.")]
    public float feverBpmGainMultiplier = 1.25f;

    [Header("BPM Decay")]
    public float bpmDecreaseSpeed = 2f;
    public float feverBpmDecayMultiplier = 1.1f;

    [Header("Meditation")]
    public bool isMeditating = false;
    public float meditateDecreaseSpeed = 5f;

    [Header("BPM Lock Potion")]
    public bool bpmLocked = false;
    public float bpmLockTimer = 0f;
    public float lockedBpm = MinBpm;

    [Header("BPM Rank Buffs")]
    public float tensionAttackDamageMultiplier = 1.1f;
    public float tensionDashSpeedMultiplier = 1.1f;
    public float tensionDashDistanceMultiplier = 1.1f;
    public float feverAttackSpeedMultiplier = 1.1f;
    public float feverMoveSpeedMultiplier = 1.1f;

    [Header("Exhaustion")]
    public float exhaustedFullStopDuration = 2f;
    public float exhaustedImpairDuration = 4f;
    public float exhaustedResetDelay = 5f;
    public float exhaustedMoveSpeedMultiplier = 0.35f;

    private bool isExhausted;
    private float exhaustedTimer;

    public BpmRank CurrentBpmRank
    {
        get
        {
            if (isExhausted || currentBpm > BpmRankCUpper)
                return BpmRank.D;

            if (currentBpm <= BpmRankAUpper)
                return BpmRank.A;

            if (currentBpm <= BpmRankBUpper)
                return BpmRank.B;

            return BpmRank.C;
        }
    }

    public bool IsExhausted => isExhausted;
    public bool IsExhaustedFullStop => isExhausted && exhaustedTimer < exhaustedFullStopDuration;
    public bool IsExhaustedImpaired => isExhausted && exhaustedTimer < exhaustedImpairDuration;
    public bool CanMove => !IsExhaustedFullStop;
    public bool CanAttackOrDash => !IsExhaustedImpaired;

    public float AttackDamageMultiplier
        => CurrentBpmRank == BpmRank.B || CurrentBpmRank == BpmRank.C ? tensionAttackDamageMultiplier : 1f;

    public float AttackSpeedMultiplier
        => CurrentBpmRank == BpmRank.C ? feverAttackSpeedMultiplier : 1f;

    public float MoveSpeedMultiplier
    {
        get
        {
            if (IsExhaustedFullStop)
                return 0f;

            if (IsExhaustedImpaired)
                return exhaustedMoveSpeedMultiplier;

            return CurrentBpmRank == BpmRank.C ? feverMoveSpeedMultiplier : 1f;
        }
    }

    public float DashSpeedMultiplier
        => CurrentBpmRank == BpmRank.B || CurrentBpmRank == BpmRank.C ? tensionDashSpeedMultiplier : 1f;

    public float DashDistanceMultiplier
        => CurrentBpmRank == BpmRank.B || CurrentBpmRank == BpmRank.C ? tensionDashDistanceMultiplier : 1f;

    private float BpmGainMultiplier
        => CurrentBpmRank == BpmRank.C ? feverBpmGainMultiplier : 1f;

    private void Update()
    {
        HandleExhaustion();
        HandleBPMDecrease();
        RecoverSP();
    }

    public void Damage(int value)
    {
        currentHp -= value;

        if (currentHp < 0)
            currentHp = 0;
    }

    public void Heal(int value)
    {
        currentHp += value;

        if (currentHp > maxHp)
            currentHp = maxHp;
    }

    private void RecoverSP()
    {
        if (currentSp < maxSp)
        {
            float recoveryMultiplier = CurrentBpmRank == BpmRank.C ? feverSpRecoveryMultiplier : 1f;
            if (isMeditating)
                recoveryMultiplier *= meditationSpRecoveryMultiplier;

            currentSp += spRecoverySpeed * recoveryMultiplier * Time.deltaTime;

            if (currentSp > maxSp)
                currentSp = maxSp;
        }
    }

    public void RecoverSPInstant(float amount)
    {
        if (amount <= 0f)
            return;

        currentSp += amount;

        if (currentSp > maxSp)
            currentSp = maxSp;
    }

    public void OnAttackHit(int hitCount, bool hitBoss)
    {
        if (hitCount <= 0)
            return;

        if (hitBoss)
        {
            RecoverSPInstant(attackSpGainBoss);
            return;
        }

        RecoverSPInstant(hitCount >= 2 ? attackSpGainMultiple : attackSpGainSingle);
    }

    public void AddBPM(float amount)
    {
        if (isExhausted)
            return;

        currentBpm += amount;

        if (currentBpm > 220f)
            currentBpm = 220f;

        if (currentBpm > BpmRankCUpper)
            StartExhaustion();
    }

    private void HandleBPMDecrease()
    {
        if (isExhausted)
            return;

        if (bpmLocked)
        {
            currentBpm = lockedBpm;

            bpmLockTimer -= Time.deltaTime;

            if (bpmLockTimer <= 0)
                bpmLocked = false;

            return;
        }

        if (currentBpm > BpmRankAUpper)
        {
            float decayMultiplier = CurrentBpmRank == BpmRank.C ? feverBpmDecayMultiplier : 1f;
            currentBpm -= bpmDecreaseSpeed * decayMultiplier * Time.deltaTime;
        }

        if (isMeditating)
            currentBpm -= meditateDecreaseSpeed * Time.deltaTime;

        if (currentBpm < MinBpm)
            currentBpm = MinBpm;
    }

    public string GetBPMRank()
    {
        return CurrentBpmRank.ToString();
    }

    public void OnAttack()
    {
        if (!bpmLocked)
            AddBPM(attackBpmGain * BpmGainMultiplier);
    }

    public bool OnDash()
    {
        int currentDashCost = CurrentBpmRank == BpmRank.C ? feverDashCost : dashCost;

        if (currentSp < currentDashCost)
            return false;

        currentSp -= currentDashCost;

        if (!bpmLocked)
            AddBPM(dashBpmGain * BpmGainMultiplier);

        return true;
    }

    public void TakeDamage(float damage, Vector2 hitPoint)
    {
        currentHp -= damage;

        if (currentHp < 0f)
            currentHp = 0f;

        Debug.Log("HP: " + currentHp);
    }

    private void StartExhaustion()
    {
        isExhausted = true;
        exhaustedTimer = 0f;
        bpmLocked = false;
    }

    private void HandleExhaustion()
    {
        if (!isExhausted)
            return;

        exhaustedTimer += Time.deltaTime;

        if (exhaustedTimer >= exhaustedResetDelay)
        {
            currentBpm = MinBpm;
            isExhausted = false;
            exhaustedTimer = 0f;
        }
    }
}
