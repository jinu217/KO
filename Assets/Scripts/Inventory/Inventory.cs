using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public List<InventorySlot> items
        = new List<InventorySlot>();


    public void AddItem(ItemData item)
    {
        items.Add(
            new InventorySlot(item,1)
        );
    }
}