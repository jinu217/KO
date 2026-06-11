using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [Header("HP")]
    public float currentHp = 70f;
    public float maxHp = 100f;

    [Header("SP")]
    public float currentSp = 100f;
    public float maxSp = 100f;
    public int dashCost = 20;

    [Header("SP 회복")]
    public float spRecoverySpeed = 1f;

    [Header("BPM")]
    public float currentBpm = 91f;

    [Header("BPM 증가량")]
    public float attackBpmGain = 8f;
    public float dashBpmGain = 12f;

    [Header("BPM 감소")]
    public float bpmDecreaseSpeed = 2f;

    [Header("명상")]
    public bool isMeditating = false;
    public float meditateDecreaseSpeed = 15f;

    [Header("BPM 고정 포션")]
    public bool bpmLocked = false;
    public float bpmLockTimer = 0f;
    public float lockedBpm = 91f;

    void Update()
    {
        HandleBPMDecrease();
        RecoverSP();
    }

    // -------------------- HP --------------------
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

    // -------------------- SP --------------------
    void RecoverSP()
    {
        if (currentSp < maxSp)
        {
            currentSp += spRecoverySpeed * Time.deltaTime;

            if (currentSp > maxSp)
                currentSp = maxSp;
        }
    }

    // -------------------- BPM --------------------
    public void AddBPM(float amount)
    {
        currentBpm += amount;

        if (currentBpm > 220f)
            currentBpm = 220f;
    }

    void HandleBPMDecrease()
    {
        if (bpmLocked)
        {
            currentBpm = lockedBpm;

            bpmLockTimer -= Time.deltaTime;

            if (bpmLockTimer <= 0)
                bpmLocked = false;

            return;
        }

        currentBpm -= bpmDecreaseSpeed * Time.deltaTime;

        if (isMeditating)
        {
            currentBpm -= meditateDecreaseSpeed * Time.deltaTime;
        }

        if (currentBpm < 60f)
            currentBpm = 60f;
    }

    public string GetBPMRank()
    {
        if (currentBpm <= 130)
            return "A";

        if (currentBpm <= 170)
            return "B";

        if (currentBpm <= 200)
            return "C";

        return "D";
    }
    // -------------------- 공격, 대쉬, 피격 --------------------
    public void OnAttack()
    {
        if (!bpmLocked)
        {
            AddBPM(attackBpmGain);
        }
    }

    public void OnDash()
    {
        if (currentSp < dashCost)
            return;

        currentSp -= dashCost;

        if (!bpmLocked)
        {
            AddBPM(dashBpmGain);
        }
    }

    public void TakeDamage(float damage, Vector2 hitPoint)
    {
        currentHp -= damage;

        if (currentHp < 0f)
            currentHp = 0f;

        Debug.Log("HP: " + currentHp);
    }
}