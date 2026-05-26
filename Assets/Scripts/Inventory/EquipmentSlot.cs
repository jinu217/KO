using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlot : MonoBehaviour
{
    public EquipmentType slotType;
    public Image icon;

    private ItemData currentItem;

    public void SetItem(ItemData item)
    {
        currentItem = item;

        if (item != null)
        {
            icon.sprite = item.icon;

            // 항상 보이게
            Color c = icon.color;
            c.a = 1f;
            icon.color = c;
        }
        else
        {
            icon.sprite = null;

            // 빈 슬롯일 때 투명하게
            Color c = icon.color;
            c.a = 0f;
            icon.color = c;
        }
    }

    public ItemData GetItem()
    {
        return currentItem;
    }
}