using UnityEngine;

public class PotionBag : MonoBehaviour
{
    public int healPotionCount;

    public int bpmPotionCount;

    public StatusUI statusUI;

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

    // 체력 포션 사용
    public void UseHealPotion(ItemData item)
    {
        if (healPotionCount <= 0)
            return;

        healPotionCount--;

        statusUI.Heal(item.healAmount);
    }

    // BPM 포션 사용
    public void UseBPMPotion(ItemData item)
    {
        if (bpmPotionCount <= 0)
            return;

        bpmPotionCount--;

        statusUI.currentBpm -= item.bpmReduceAmount;

        if (statusUI.currentBpm < 91)
            statusUI.currentBpm = 91;
    }
    //테스트용 코드
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            healPotionCount++;
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            bpmPotionCount++;
        }
    }
}