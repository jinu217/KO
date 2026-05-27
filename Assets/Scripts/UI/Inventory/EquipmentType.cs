// [장비 슬롯 타입]
// - 인벤토리 장비 슬롯의 허용 타입과 아이템 장착 가능 여부 판정에 사용
public enum EquipmentType
{
    None,
    Head,
    Body,
    Gloves,
    Boots,
    Weapon
}

// [인벤토리 무기 타입]
// - PlayerController.WeaponType과 분리하여 인벤토리 데이터 직렬화 안정성 유지
public enum InventoryWeaponType
{
    None,
    Sword,
    Blunt,
    Spear,
    Axe,
    Scythe
}
