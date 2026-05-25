using UnityEngine;

public class InventoryDisplay : MonoBehaviour
{
    public Inventory inventory;
    public ItemTooltipUI tooltip;
    private InventorySlotUI[] slots;

    private int selectedIndex = 0;
    private const int COLUMN_COUNT = 3;
    public int GetSlotCount()
    {
        return slots != null ? slots.Length : 0;
    }

    void Awake()
    {
        slots = GetComponentsInChildren<InventorySlotUI>(true);
    }

    void Start()
    {
        if (inventory == null)
        {
            Debug.LogError("Inventory 연결 안됨!");
            return;
        }

        inventory.onInventoryChanged += UpdateInventory;
        UpdateInventory();

        RefreshSelection();
    }

    void Update()
    {

    }

    void MoveSelection(int amount)
    {
        if (slots == null || slots.Length == 0) return;

        selectedIndex += amount;
        selectedIndex = Mathf.Clamp(selectedIndex, 0, slots.Length - 1);

        RefreshSelection();
    }

    public void UpdateInventory()
    {
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
            slots[i].Setup(null);

        for (int i = 0; i < inventory.items.Count && i < slots.Length; i++)
            slots[i].Setup(inventory.items[i].item);

        RefreshSelection();
    }

    public void SetSelectedIndex(int index)
    {
        selectedIndex = index;
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        if (slots == null || slots.Length == 0) return;

        selectedIndex = Mathf.Clamp(selectedIndex, 0, slots.Length - 1);

        for (int i = 0; i < slots.Length; i++)
            slots[i].SetSelected(i == selectedIndex);

        if (tooltip == null) return;

        if (inventory == null ||
            inventory.items == null ||
            inventory.items.Count == 0)
        {
            tooltip.Hide();
            return;
        }

        if (selectedIndex < 0 || selectedIndex >= inventory.items.Count)
        {
            tooltip.Hide();
            return;
        }

        InventorySlot slotData = inventory.items[selectedIndex];

        if (slotData == null || slotData.item == null)
        {
            tooltip.Hide();
            return;
        }

        ItemData item = slotData.item;

        RectTransform slotRect = slots[selectedIndex].GetComponent<RectTransform>();

        tooltip.Show(item, slotRect);
    }

    // =========================
    // 장비
    // =========================

    public void EquipSelected(int index)
    {
        if (index < 0 || index >= inventory.items.Count)
            return;

        ItemData item = inventory.items[index].item;

        if (item == null) return;

        inventory.EquipItem(item);

        inventory.RemoveItem(index);
    }

    public void UnequipSelected(int index)
    {
        inventory.equipmentManager.Unequip(index);
        UpdateInventory();
    }

    public void DropSelected(int index)
    {
        inventory.RemoveItem(index);

        selectedIndex = Mathf.Clamp(
            selectedIndex,
            0,
            Mathf.Max(inventory.items.Count - 1, 0)
        );
    }
}