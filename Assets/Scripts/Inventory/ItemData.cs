using UnityEngine;

[CreateAssetMenu(menuName ="Items/Item")]
public class ItemData : ScriptableObject
{
    public int id;

    [Header("기본 정보")]
    public string itemName;
    public Sprite icon;

    [Header("아이템 종류")]
    public ItemType itemType;          // 장비, 소모품, 재료
    public EquipmentType equipType;    // 무기, 머리, 몸통...

    [Header("능력치")]
    public int attack;
    public float attackSpeed;

    public float critChance;
    public float critDamage;

    public float moveSpeed;

    public int defense;
    public int hp;

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