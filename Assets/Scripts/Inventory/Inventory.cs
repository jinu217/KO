using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public EquipmentManager equipmentManager;

    public List<InventorySlot> items = new List<InventorySlot>();

    public ItemData[] startItems;

    public Action onInventoryChanged;

    void Awake()
    {
        foreach (ItemData item in startItems)
        {
            AddItem(item);
        }
    }

    public void AddItem(ItemData item)
    {
        items.Add(new InventorySlot(item, 1));
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