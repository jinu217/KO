using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    public ItemData item;
    public ItemTooltipUI tooltipUI;
    public Image icon;

    void Awake()
    {
        tooltipUI = FindFirstObjectByType<ItemTooltipUI>();
    }

    public void Setup(ItemData newItem)
    {
        item = newItem;

        icon.sprite = item.icon;
        icon.enabled = true;
    }

    public void OnClick()
    {
        if(item != null)
        {
            tooltipUI.Show(item, transform);
        }
    }
}