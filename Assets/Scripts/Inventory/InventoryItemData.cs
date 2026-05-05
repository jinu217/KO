using UnityEngine;

// 인벤토리 아이템 데이터
[CreateAssetMenu(fileName = "NewInventoryItem", menuName = "ScriptableObjects/Inventory/ItemData")]
public class InventoryItemData : ScriptableObject
{
    [Header("기본 정보")]
    public string itemName;
    public Sprite itemSprite;
    public InventoryItemType itemType = InventoryItemType.Misc;

    [Header("종류")]
    public EquipmentType equipmentType = EquipmentType.None;        // itemType=Equipment 일 때 사용
    public InventoryWeaponType weaponType = InventoryWeaponType.None; // itemType=Weapon 일 때 사용

    [Header("스탯")]
    public ItemStats stats;
}

[System.Serializable]
public struct ItemStats
{
    public float hp;
    public float defense;
    public float moveSpeed;
    public float attack;
    public float attackSpeed;
    public float criticalChance;
    public float criticalDamage;
}
