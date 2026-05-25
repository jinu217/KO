using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public Inventory inventory;

    public EquipmentSlot weaponSlot;
    public EquipmentSlot handSlot;
    public EquipmentSlot headSlot;
    public EquipmentSlot bodySlot;
    public EquipmentSlot footSlot;

    public void Equip(ItemData item)
    {
        if (item == null) return;
        Debug.Log("장착 시도: " + item.itemName);

        switch (item.equipType)
        {
            case EquipmentType.Weapon:
                Debug.Log("무기 슬롯");
                weaponSlot.SetItem(item);
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

        if (current == newItem)
            return;

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