using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [Header("용량")]
    [Tooltip("인벤토리 UI가 연결되기 전에 사용할 기본 슬롯 개수")]
    [SerializeField, Min(1)] private int capacity = 9;

    public EquipmentManager equipmentManager;

    public List<InventorySlot> items = new List<InventorySlot>();

    public ItemData[] startItems;

    public Action onInventoryChanged;

    public int Capacity => capacity;
    public int Count => items != null ? items.Count : 0;
    public bool HasSpace => Count < capacity;

    public void SetCapacity(int slotCount)
    {
        if (slotCount > 0)
            capacity = slotCount;
    }

    void Awake()
    {
        if (SaveData.Instance != null && SaveData.Instance.run.initialized)
            return;

        foreach (ItemData item in startItems)
        {
            AddItem(item);
        }
    }

    public void AddItem(ItemData item)
    {
        if (item == null)
            return;

        // 인벤토리가 가득 차 있으면 추가하지 않음
        if (!HasSpace)
            return;

        items.Add(new InventorySlot(item, 1));
        onInventoryChanged?.Invoke();
    }


    public void ReplaceItems(IEnumerable<ItemData> restoredItems)
    {
        items.Clear();

        if (restoredItems != null)
        {
            foreach (ItemData item in restoredItems)
            {
                if (item != null)
                    items.Add(new InventorySlot(item, 1));
            }
        }

        onInventoryChanged?.Invoke();
    }

    public void RemoveItem(int index)
    {
        if (index < 0 || index >= items.Count)
            return;

        items.RemoveAt(index);

        onInventoryChanged?.Invoke();
    }

    public void EquipItem(ItemData item)
    {
        if (item == null) return;
        if (item.itemType != ItemType.Equipment) return;

        equipmentManager.Equip(item);
        onInventoryChanged?.Invoke();
    }
}
