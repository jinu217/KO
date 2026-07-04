using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public Inventory inventory;
    public EquipmentSlot weaponSlot;
    public EquipmentSlot handSlot;
    public EquipmentSlot headSlot;
    public EquipmentSlot bodySlot;
    public EquipmentSlot footSlot;
    public ItemData[] startEquipItems;
    public CharacterPreviewUI previewUI;
    public PlayerStats stats = new PlayerStats();
    public System.Action onStatsChanged;
    public PlayerStats baseStats;
    public PlayerAppearance playerAppearance; //플레이어 외형 장비연결

    void Awake()
    {
        baseStats = new PlayerStats();

        baseStats.attack = 10;
        baseStats.attackSpeed = 1f;
        baseStats.critChance = 5f;
        baseStats.critDamage = 150f;
        baseStats.moveSpeed = 5f;
        baseStats.defense = 3;
        baseStats.hp = 100;
    }

    void Start()
    {
        foreach(ItemData item in startEquipItems)
        {
            if(item != null)
                Equip(item);
        }
    }

    public void Equip(ItemData item)
    {
        if (item == null) return;

        switch (item.equipType)
        {
            case EquipmentType.Weapon:
                ReplaceIfDifferent(weaponSlot, item);
                TutorialManager.Instance?.NotifyWeaponEquipped();
                break;

            case EquipmentType.Hand:
                ReplaceIfDifferent(handSlot, item);
                break;

            case EquipmentType.Head:
                ReplaceIfDifferent(headSlot, item);
                break;

            case EquipmentType.Body:
                ReplaceIfDifferent(bodySlot, item);
                break;

            case EquipmentType.Foot:
                ReplaceIfDifferent(footSlot, item);
                break;
        }

        if (playerAppearance != null){
            playerAppearance.Equip(item);}

        previewUI.Refresh();
        RefreshStats();
    }

    void ReplaceIfDifferent(EquipmentSlot slot, ItemData newItem)
    {
        ItemData current = slot.GetItem();

        if (current != null)
        {
            inventory.AddItem(current);
        }

        slot.SetItem(newItem);
    }

    public void Unequip(int index)
    {
        ItemData item = null;

        switch (index)
        {
            case 0: item = weaponSlot.GetItem(); weaponSlot.SetItem(null); break;
            case 1: item = handSlot.GetItem(); handSlot.SetItem(null); break;
            case 2: item = headSlot.GetItem(); headSlot.SetItem(null); break;
            case 3: item = bodySlot.GetItem(); bodySlot.SetItem(null); break;
            case 4: item = footSlot.GetItem(); footSlot.SetItem(null); break;
        }

        if (item != null)
            inventory.AddItem(item);
        
        previewUI.Refresh();
    }

    public void RefreshStats()
    {
        stats.Clear();

        stats.attack = baseStats.attack;
        stats.attackSpeed = baseStats.attackSpeed;
        stats.critChance = baseStats.critChance;
        stats.critDamage = baseStats.critDamage;
        stats.moveSpeed = baseStats.moveSpeed;
        stats.defense = baseStats.defense;
        stats.hp = baseStats.hp;

        Add(weaponSlot);
        Add(headSlot);
        Add(bodySlot);
        Add(handSlot);
        Add(footSlot);

        onStatsChanged?.Invoke();
    }

    void Add(EquipmentSlot slot)
    {
        ItemData item = slot.GetItem();

        if (item == null)
        {
            Debug.Log(slot.name + " = EMPTY");
            return;
        }

        Debug.Log(slot.name + " = " + item.itemName);

        stats.Add(item);
    }
}
