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
            icon.enabled = true;
        }
        else
        {
            icon.sprite = null;
            icon.enabled = false;
        }
    }

    public ItemData GetItem()
    {
        return currentItem;
    }
}