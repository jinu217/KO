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
    [SerializeField] private PlayerController playerController;

    private const float MinBpm = 91f;
    private const float BpmRankAUpper = 130f;
    private const float BpmRankBUpper = 170f;
    private const float BpmRankCUpper = 200f;

    [Header("HP")]
    [Tooltip("현재 체력")]
    public float currentHp = 70f;
    [Tooltip("최대 체력 (사용하는지 모름)")]
    public float maxHp => playerController != null ? playerController.Stats.maxHp : 50f;

    [Header("SP")]
    [Tooltip("현재 SP")]
    public float currentSp = 100f;
    [Tooltip("최대 SP")]
    public float maxSp = 100f;
    [Tooltip("대쉬 필요 SP")]
    public int dashCost = 12;
    [Tooltip("C 구간 :: 대쉬 필요 SP")]
    public int feverDashCost = 8;

    [Header("SP Recovery")]
    [Tooltip("SP 자연 회복량")]
    public float spRecoverySpeed = 1.5f;
    [Tooltip("C 구간 :: SP 자연 회복량 추가 배율")]
    public float feverSpRecoveryMultiplier = 1.75f;
    [Tooltip("명상 SP 자연 회복량 추가 배율")]
    public float meditationSpRecoveryMultiplier = 2.25f;
    [Tooltip("일반 몬스터 1마리 적중 시 회복 SP")]
    public float attackSpGainSingle = 1.25f;
    [Tooltip("일반 몬스터 2마리 이상 적중 시 회복 SP")]
    public float attackSpGainMultiple = 1.5f;
    [Tooltip("MidBoss 또는 MainBoss 몬스터 적중 시 회복 SP")]
    public float attackSpGainBoss = 2f;

    [Header("BPM")]
    [Tooltip("BPM")]
    public float currentBpm = MinBpm;

    [Header("BPM Gain")]
    [Tooltip("공격 1회 사용 시 증가하는 BPM")]
    public float attackBpmGain = 8f;
    [Tooltip("대시 1회 사용 시 증가하는 BPM")]
    public float dashBpmGain = 12f;
    [Tooltip("C 구간 :: 공격/대시 BPM 증가량 추가 배율")]
    public float feverBpmGainMultiplier = 1.25f;

    [Header("BPM Decay")]
    [Tooltip("BPM 자연 감소량")]
    public float bpmDecreaseSpeed = 2.25f;
    [Tooltip("C 구간 :: BPM 자연 감소량 추가 배율")]
    public float feverBpmDecayMultiplier = 1.15f;

    [Header("Meditation")]
    [Tooltip("현재 명상 중 여부")]
    public bool isMeditating = false;
    [Tooltip("명상 중 추가 BPM 감소량")]
    public float meditateDecreaseSpeed = 3f;

    [Header("BPM Lock Potion")]
    [Tooltip("true이면 BPM을 lockedBpm 값으로 고정")]
    public bool bpmLocked = false;
    [Tooltip("BPM 고정 남은 시간")]
    public float bpmLockTimer = 0f;
    [Tooltip("BPM 고정 중 유지할 BPM 값")]
    public float lockedBpm = MinBpm;

    [Header("BPM Rank Buffs")]
    [Tooltip("B/C 구간 :: 공격력 추가 배율")]
    public float tensionAttackDamageMultiplier = 1.15f;
    [Tooltip("B/C 구간 :: 대시 속도 추가 배율")]
    public float tensionDashSpeedMultiplier = 1.15f;
    [Tooltip("B/C 구간 :: 대시 거리 추가 배율")]
    public float tensionDashDistanceMultiplier = 1.15f;
    [Tooltip("C 구간 :: 공격속도 추가 배율")]
    public float feverAttackSpeedMultiplier = 1.15f;
    [Tooltip("C 구간 :: 이동속도 추가 배율")]
    public float feverMoveSpeedMultiplier = 1.15f;

    [Header("Exhaustion")]
    [Tooltip("탈진 :: 행동 불능 시간")]
    public float exhaustedFullStopDuration = 2f;
    [Tooltip("탈진 :: 공격/대시가 막히고 이동속도가 감소하는 시간")]
    public float exhaustedImpairDuration = 4f;
    [Tooltip("탈진 :: BPM을 91로 되돌리고 탈진을 해제하는 시간")]
    public float exhaustedResetDelay = 5f;
    [Tooltip("탈진 :: 이동속도에 곱해지는 배율")]
    public float exhaustedMoveSpeedMultiplier = 0.35f;

    private bool isExhausted;
    private float exhaustedTimer;

    private void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
    }

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

        if (currentBpm > MinBpm)
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
