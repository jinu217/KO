using UnityEngine;

public class PotionBag : MonoBehaviour
{
    public int healPotionCount;
    public int bpmPotionCount;

    public PlayerStatus playerStatus;

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

    // 체력 포션 사용 (p)
    public void UseHealPotion()
    {
        if (healPotionCount <= 0)
            return;

        if (playerStatus == null || healPotionData == null)
        {
            Debug.LogWarning("PotionBag의 PlayerStatus 또는 Heal Potion Data 연결을 확인해 주세요.", this);
            return;
        }

        healPotionCount--;

        playerStatus.Heal(healPotionData.healAmount);
    }

    //============== BPM 포션 사용 ==============
    public void UseBPMPotion()
    {
        if (bpmPotionCount <= 0)
            return;

        if (playerStatus == null || bpmPotionData == null)
        {
            Debug.LogWarning("PotionBag의 PlayerStatus 또는 BPM Potion Data 연결을 확인해 주세요.", this);
            return;
        }

        bpmPotionCount--;

        //============== BPM 포션 효과 ==============

        // 포션 사용 즉시 BPM 감소
        if (bpmPotionData.bpmReduceAmount > 0)
        {
            playerStatus.ReduceBPM(bpmPotionData.bpmReduceAmount);
        }

        Debug.Log(
            $"BPM 포션 사용! BPM -{bpmPotionData.bpmReduceAmount} / "
        );
    }
}
