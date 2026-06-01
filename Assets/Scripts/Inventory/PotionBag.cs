using UnityEngine;

public class PotionBag : MonoBehaviour
{
    public int healPotionCount;
    public int bpmPotionCount;

    public StatusUI statusUI;

    // 포션 데이터 연결
    public ItemData healPotionData;
    public ItemData bpmPotionData;

    public void AddPotion(ItemData item)
    {
        // 체력 포션
        if (item.healAmount > 0)
        {
            healPotionCount++;
        }

        // BPM 포션
        if (item.bpmReduceAmount > 0)
        {
            bpmPotionCount++;
        }
    }

    // 체력 포션 사용 (Q)
    public void UseHealPotion()
    {
        if (healPotionCount <= 0)
            return;

        healPotionCount--;

        statusUI.Heal(healPotionData.healAmount);
    }

    // BPM 포션 사용 (R)
    public void UseBPMPotion()
    {
        if (bpmPotionCount <= 0)
            return;

        bpmPotionCount--;

        statusUI.bpmLocked = true;
        statusUI.bpmLockTimer = 15f;

        statusUI.lockedBpm = statusUI.currentBpm;
        statusUI.bpmLocked = true;
        statusUI.bpmLockTimer = 15f;
    }

    // 테스트용 코드
    void Update()
    {
        // 체력 포션 사용
        if (Input.GetKeyDown(KeyCode.Q))
        {
            UseHealPotion();
        }

        // BPM 포션 사용
        if (Input.GetKeyDown(KeyCode.R))
        {
            UseBPMPotion();
        }

        // 체력 포션 추가 테스트
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            healPotionCount++;
        }

        // BPM 포션 추가 테스트
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            bpmPotionCount++;
        }
    }
}