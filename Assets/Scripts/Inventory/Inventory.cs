using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public List<InventorySlot> items =
        new List<InventorySlot>();

    public ItemData[] startItems;

    void Awake()
    {
        foreach(ItemData item in startItems)
        {
            AddItem(item);
        }
    }

    public void AddItem(ItemData item)
    {
        items.Add(
            new InventorySlot(item,1)
        );

        Debug.Log("인벤 추가: " + item.itemName);
    }
}