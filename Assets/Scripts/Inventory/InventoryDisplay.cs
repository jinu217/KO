using UnityEngine;

public class InventoryDisplay : MonoBehaviour
{
    public Inventory inventory;

    private InventorySlotUI[] slots;

    void Start()
    {
        slots = GetComponentsInChildren<InventorySlotUI>();

        UpdateInventory();
    }

    public void UpdateInventory()
    {
        // 슬롯 초기화
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].item = null;
            slots[i].icon.sprite = null;
            slots[i].icon.enabled = false;
        }

        // 아이템 채우기
        for (int i = 0; i < inventory.items.Count && i < slots.Length; i++)
        {
            slots[i].Setup(inventory.items[i].item);
        }
    }
}