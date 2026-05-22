using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    public ItemData item;

    public ItemTooltipUI tooltipUI;

    public Image icon;

    public void Setup(ItemData newItem)
    {
        item = newItem;

        icon.sprite = item.icon;
        icon.enabled = true;
    }

    public void OnClick()
    {
        tooltipUI.Show(item, transform);
    }
}