using UnityEngine;

// [인벤토리 아이템 데이터]
// - ScriptableObject로 아이템 이름/아이콘/분류/스탯 보관
[CreateAssetMenu(fileName = "NewInventoryItem", menuName = "ScriptableObjects/Inventory/ItemData")]
public class InventoryItemData : ScriptableObject
{
    [Header("기본 정보")]
    [Tooltip("아이템 표시 이름")] public string itemName;
    [Tooltip("UI/보상 화면에서 사용할 아이콘")] public Sprite itemSprite;
    [Tooltip("아이템 대분류")] public InventoryItemType itemType = InventoryItemType.Misc;

    [Header("장착 타입")]
    [Tooltip("itemType=Equipment일 때 사용 (머리/몸통/장갑/신발)")]
    public EquipmentType equipmentType = EquipmentType.None;
    [Tooltip("itemType=Weapon일 때 사용")]
    public InventoryWeaponType weaponType = InventoryWeaponType.None;

    [Header("스탯")]
    [Tooltip("장착 시 적용/해제할 스탯 묶음")]
    public ItemStats stats;
}

// 장착 시 가감되는 스탯 묶음
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
