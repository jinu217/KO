using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public Inventory inventory;

    public EquipmentSlot weaponSlot;
    public EquipmentSlot handSlot;
    public EquipmentSlot headSlot;
    public EquipmentSlot bodySlot;
    public EquipmentSlot footSlot;

    // 시작 장비
    public ItemData[] startEquipItems;

    void Start()
    {
        foreach(ItemData item in startEquipItems)
        {
            if(item != null)
                Equip(item);
        }
    }

    public void Equip(ItemData item)
    {
        if (item == null) return;
        Debug.Log("장착 시도: " + item.itemName);

        switch (item.equipType)
        {
            case EquipmentType.Weapon:
                ReplaceIfDifferent(weaponSlot, item);
                break;

            case EquipmentType.Hand:
                ReplaceIfDifferent(handSlot, item);
                break;

            case EquipmentType.Head:
                ReplaceIfDifferent(headSlot, item);
                break;

            case EquipmentType.Body:
                ReplaceIfDifferent(bodySlot, item);
                break;

            case EquipmentType.Foot:
                ReplaceIfDifferent(footSlot, item);
                break;
        }
    }

    void ReplaceIfDifferent(EquipmentSlot slot, ItemData newItem)
    {
        ItemData current = slot.GetItem();

        if (current != null)
        {
            inventory.AddItem(current);
        }

        slot.SetItem(newItem);
    }

    public void Unequip(int index)
    {
        ItemData item = null;

        switch (index)
        {
            case 0: item = weaponSlot.GetItem(); weaponSlot.SetItem(null); break;
            case 1: item = handSlot.GetItem(); handSlot.SetItem(null); break;
            case 2: item = headSlot.GetItem(); headSlot.SetItem(null); break;
            case 3: item = bodySlot.GetItem(); bodySlot.SetItem(null); break;
            case 4: item = footSlot.GetItem(); footSlot.SetItem(null); break;
        }

        if (item != null)
            inventory.AddItem(item);
    }
}