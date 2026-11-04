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


    // 무기 미리보기 개별 보정 ============================
    // 스프라이트 Pivot은 애니메이션용이라 건드리지 않고, 인벤토리 미리보기용 값만 여기서 따로 관리
    [Header("무기 미리보기 개별 보정 (무기 장비만 사용)")]

    [Tooltip("손으로 쥐는 곳 (이미지 안에서의 0~1 비율). X: 0=왼쪽 끝, 1=오른쪽 끝 / Y: 0=아래 끝, 1=위 끝")]
    public Vector2 previewGripPivot = new Vector2(0.5f, 0.5f);

    [Tooltip("종류별 기본 길이에 곱해지는 배율. 1 = 그대로, 1.2 = 20% 크게")]
    public float previewScaleMultiplier = 1f;

    [Tooltip("종류별 기본 각도에 더해지는 각도. 이 무기만 기울기가 다를 때 사용")]
    public float previewRotationOffset = 0f;

    [Tooltip("종류별 위치 보정에 더해지는 위치. 잡는 곳이 어긋날 때 사용")]
    public Vector2 previewPositionOffset = Vector2.zero;
    // 무기 미리보기 개별 보정 끝 ============================

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (itemType == ItemType.Equipment && rarity == ItemRarity.None)
            rarity = ItemRarity.Normal;
        else if (itemType != ItemType.Equipment)
            rarity = ItemRarity.None;

        // 배율이 0이면 무기가 안 보이므로 방지
        if (previewScaleMultiplier <= 0f)
            previewScaleMultiplier = 1f;
    }
#endif
}