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
    [SerializeField] private PlayerController playerController;
    public PlayerAppearance playerAppearance; //플레이어 외형 장비연결

    void Awake()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();
    }

    void Start()
    {
        if (SaveData.Instance != null && SaveData.Instance.run.initialized)
        {
            RefreshStats();
            return;
        }

        foreach(ItemData item in startEquipItems)
        {
            if(item != null)
                Equip(item);
        }
    }

    public void RestoreEquipment(ItemData weapon, ItemData hand, ItemData head, ItemData body, ItemData foot)
    {
        SetSlotWithoutInventory(weaponSlot, weapon);
        SetSlotWithoutInventory(handSlot, hand);
        SetSlotWithoutInventory(headSlot, head);
        SetSlotWithoutInventory(bodySlot, body);
        SetSlotWithoutInventory(footSlot, foot);

        ApplyAppearance(weapon);
        ApplyAppearance(hand);
        ApplyAppearance(head);
        ApplyAppearance(body);
        ApplyAppearance(foot);

        if (previewUI != null)
            previewUI.Refresh();

        RefreshStats();
    }

    private void SetSlotWithoutInventory(EquipmentSlot slot, ItemData item)
    {
        if (slot != null)
            slot.SetItem(item);
    }

    private void ApplyAppearance(ItemData item)
    {
        if (item != null && playerAppearance != null)
            playerAppearance.Equip(item);
    }

    public void Equip(ItemData item)
    {
        if (item == null) return;

        switch (item.equipType)
        {
            case EquipmentType.Weapon:
                ReplaceIfDifferent(weaponSlot, item);
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

        if (previewUI != null)
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
        
        if (previewUI != null)
            previewUI.Refresh();
        RefreshStats();
    }

    public void RefreshStats()
    {
        if (playerController == null)
            playerController = GetComponent<PlayerController>();

        ItemData weapon = GetItem(weaponSlot);
        ItemData head = GetItem(headSlot);
        ItemData body = GetItem(bodySlot);
        ItemData hand = GetItem(handSlot);
        ItemData foot = GetItem(footSlot);

        if (playerController != null)
        {
            playerController.RecalculateEquipmentStats(weapon, head, body, hand, foot);
            stats = playerController.Stats;
        }

        onStatsChanged?.Invoke();
    }

    private ItemData GetItem(EquipmentSlot slot)
    {
        return slot != null ? slot.GetItem() : null;
    }

    void Add(EquipmentSlot slot)
    {
        if (slot == null)
            return;

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
