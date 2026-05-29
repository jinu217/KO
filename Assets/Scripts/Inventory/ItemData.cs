using UnityEngine;

[CreateAssetMenu(menuName ="Items/Item")]
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

    public EquipmentType equipType;

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

    // 무기/머리
    public Sprite previewSprite;

    // 몸통 + 어깨
    public Sprite bodySprite;

    public Sprite shoulderLeftSprite;

    public Sprite shoulderRightSprite;

    // 손 좌/우
    public Sprite handLeftSprite;

    public Sprite handRightSprite;

    // 발 좌/우
    public Sprite footLeftSprite;

    public Sprite footRightSprite;
}