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
    public int defense;
    public int hp;
}