using UnityEngine;

[CreateAssetMenu(menuName = "Items/Item")]
public class ItemData : ScriptableObject
{
    public int id;

    // =========================
    // 기본 정보
    // =========================

    [Header("기본 정보")]
    public string itemName;

    public Sprite icon;

    // =========================
    // 아이템 종류
    // =========================

    [Header("아이템 종류")]
    public ItemType itemType;

    // 장비 타입 (머리, 몸통, 장갑, 신발 등)
    public EquipmentType equipType;

    // 무기 타입 : 추후 아이템데이터 분리시 사용
    // public InventoryWeaponType weaponType = InventoryWeaponType.None;

    // 재화 타입
    public CurrencyType currencyType;

    // =========================
    // 장비 능력치
    // =========================

    [Header("장비 능력치")]
    public int attack;

    public float attackSpeed;

    public float critChance;

    public float critDamage;

    public float moveSpeed;

    public int defense;

    public int hp;

    // =========================
    // 포션
    // =========================

    [Header("포션")]
    public int healAmount;

    public float bpmReduceAmount;

    // =========================
    // 재화
    // =========================

    [Header("재화")]
    public int currencyAmount;

    // =========================
    // 캐릭터 미리보기
    // =========================

    [Header("캐릭터 미리보기 이미지")]

    // 무기 / 머리
    public Sprite previewSprite;

    // 몸통
    public Sprite bodySprite;

    // 어깨
    public Sprite shoulderLeftSprite;
    public Sprite shoulderRightSprite;

    // 손
    public Sprite handLeftSprite;
    public Sprite handRightSprite;

    // 발
    public Sprite footLeftSprite;
    public Sprite footRightSprite;
}

// 임의 연결
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
public enum InventoryWeaponType
{
    None,
    Sword,
    Blunt,
    Spear,
    Axe,
    Scythe
}