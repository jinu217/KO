using UnityEngine;

// 인벤토리 아이템 데이터
[CreateAssetMenu(fileName = "NewInventoryItem", menuName = "ScriptableObjects/Inventory/ItemData")]
public class InventoryItemData : ScriptableObject
{
    public string itemName;
    public InventoryItemType itemType = InventoryItemType.Misc;
    public EquipmentType equipmentType = EquipmentType.None;
}
