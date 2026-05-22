using UnityEngine;

public class InventoryDisplay : MonoBehaviour
{
    public Inventory inventory;

    private InventorySlotUI[] slots;

    void Start()
    {
        // InventoryGrid 안의 기존 슬롯 가져오기
        slots = GetComponentsInChildren<InventorySlotUI>();

        UpdateInventory();
    }

    void UpdateInventory()
    {
        // 슬롯 초기화
        for(int i = 0; i < slots.Length; i++)
        {
            slots[i].icon.enabled = false;
        }

        // 아이템 표시
        for(int i = 0; i < inventory.items.Count; i++)
        {
            slots[i].Setup(
                inventory.items[i].item
            );
        }
    }
}