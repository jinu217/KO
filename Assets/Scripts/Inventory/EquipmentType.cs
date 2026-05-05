// 장비 착용 부위 타입 (방어구 전용)
public enum EquipmentType
{
    None,
    Head,
    Body,
    Gloves,
    Boots,
    Weapon // 기존 슬롯 호환용 (무기 슬롯)
}

// 인벤토리 무기 종류 (PlayerController의 WeaponType과 이름 충돌 방지)
public enum InventoryWeaponType
{
    None,
    Sword,   // 칼
    Blunt,   // 둔기
    Spear,   // 창
    Axe,     // 도끼
    Scythe   // 낫
}
