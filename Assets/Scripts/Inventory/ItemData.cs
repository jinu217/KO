using UnityEngine;

[CreateAssetMenu(menuName ="Items/Item")]
public class ItemData : ScriptableObject
{
    public int id;
    public string itemName;
    public Sprite icon;

    public EquipmentType equipType;

    public int attack;
    public int defense;
    public int hp;
}