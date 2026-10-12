using UnityEngine;

public enum ItemRarity
{
    None,
    Normal,
    Rare,
    Epic,
    Legendary,
    [InspectorName("mechanization")]
    Mechanization
}

[CreateAssetMenu(menuName = "Items/Item")]
public class ItemData : ScriptableObject
{
    // 기본 정보 =============================
    [Header("기본 정보")]
    public int id;
    public string itemName;
    public Sprite icon;


    // 아이템 종류 ============================
    [Header("아이템 종류")]
    public ItemType itemType;

    [Tooltip("포션과 재화는 None 장비는 Normal 이상")]
    public ItemRarity rarity = ItemRarity.None;

    // 장비 타입 (머리, 몸통, 장갑, 신발 등)
    public EquipmentType equipType;

    // 무기 타입 
    public InventoryWeaponType weaponType = InventoryWeaponType.None;

    // 재화 타입
    public CurrencyType currencyType;


    // 장비 능력치 ============================
    [Header("장비 능력치")]
    public int attack;
    public float attackSpeed;
    public float criticalChance;
    public float criticalDamage;
    public float moveSpeed;
    public int defense;
    public int hp;


    // 포션 ====================================
    [Header("포션")]
    public int healAmount;

    public float bpmReduceAmount;


    // 재화 =====================================
    [Header("재화")]
    public int currencyAmount;


    // 캐릭터 미리보기 ============================
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

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (itemType == ItemType.Equipment && rarity == ItemRarity.None)
            rarity = ItemRarity.Normal;
        else if (itemType != ItemType.Equipment)
            rarity = ItemRarity.None;
    }
#endif
}
