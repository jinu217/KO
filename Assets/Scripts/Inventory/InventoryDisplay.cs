using UnityEngine;

//============== 인벤토리 표시 ==============

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

    //============== 초기화 ==============

    private void Awake()
    {
        slots = GetComponentsInChildren<InventorySlotUI>(true);

        FindInventory();

        if (inventory != null)
            inventory.SetCapacity(slots.Length);
    }

    private void Start()
    {
        FindInventory();

        if (inventory == null)
        {
            Debug.LogError("[InventoryDisplay] Player의 Inventory를 찾을 수 없습니다.");
            return;
        }

        inventory.SetCapacity(slots != null ? slots.Length : 0);

        inventory.onInventoryChanged -= UpdateInventory;
        inventory.onInventoryChanged += UpdateInventory;

        UpdateInventory();
        RefreshSelection();
    }

    //============== 현재 플레이어 Inventory 찾기 ==============

    private void FindInventory()
    {
        // Inspector에 직접 연결되어 있으면 우선 사용
        if (inventory != null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            inventory = player.GetComponent<Inventory>();

            if (inventory != null)
            {
                Debug.Log("[InventoryDisplay] Player의 Inventory 연결 완료");
                return;
            }
        }

        // Player 태그가 없거나 Inventory가 없는 경우
        Inventory foundInventory = FindFirstObjectByType<Inventory>();

        if (foundInventory != null)
        {
            inventory = foundInventory;

            Debug.Log("[InventoryDisplay] Inventory 자동 연결 완료");
        }
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.onInventoryChanged -= UpdateInventory;
    }

    //============== 인벤토리 갱신 ==============

    public void UpdateInventory()
    {
        if (slots == null)
            return;

        if (inventory == null)
        {
            FindInventory();

            if (inventory == null)
                return;
        }

        for (int i = 0; i < slots.Length; i++)
            slots[i].Setup(null);

        if (inventory.items != null)
        {
            for (int i = 0; i < inventory.items.Count && i < slots.Length; i++)
            {
                if (inventory.items[i] != null)
                    slots[i].Setup(inventory.items[i].item);
            }
        }

        RefreshSelection();
    }

    //============== 선택 ==============

    public void SetSelectedIndex(int index)
    {
        selectedIndex = index;
        RefreshSelection();
    }

    private void RefreshSelection()
    {
        if (slots == null || slots.Length == 0)
            return;

        selectedIndex = Mathf.Clamp(
            selectedIndex,
            0,
            slots.Length - 1
        );

        for (int i = 0; i < slots.Length; i++)
            slots[i].SetSelected(i == selectedIndex);

        if (tooltip == null)
            return;

        if (inventory == null ||
            inventory.items == null ||
            inventory.items.Count == 0)
        {
            tooltip.Hide();
            return;
        }

        if (selectedIndex < 0 ||
            selectedIndex >= inventory.items.Count)
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

        RectTransform slotRect =
            slots[selectedIndex].GetComponent<RectTransform>();

        tooltip.Show(item, slotRect);
    }

    //============== 장비 장착 ==============
    public void EquipSelected(int index)
    {
        if (inventory == null)
            return;

        if (index < 0 || index >= inventory.items.Count)
            return;

        ItemData item = inventory.items[index].item;

        if (item == null)
            return;

        //============== 이미 같은 장비가 장착되어 있는지 확인 ==============

        if (inventory.equipmentManager != null)
        {
            ItemData equippedItem = null;

            switch (item.equipType)
            {
                case EquipmentType.Weapon:
                    if (inventory.equipmentManager.weaponSlot != null)
                        equippedItem = inventory.equipmentManager.weaponSlot.GetItem();
                    break;

                case EquipmentType.Hand:
                    if (inventory.equipmentManager.handSlot != null)
                        equippedItem = inventory.equipmentManager.handSlot.GetItem();
                    break;

                case EquipmentType.Head:
                    if (inventory.equipmentManager.headSlot != null)
                        equippedItem = inventory.equipmentManager.headSlot.GetItem();
                    break;

                case EquipmentType.Body:
                    if (inventory.equipmentManager.bodySlot != null)
                        equippedItem = inventory.equipmentManager.bodySlot.GetItem();
                    break;

                case EquipmentType.Foot:
                    if (inventory.equipmentManager.footSlot != null)
                        equippedItem = inventory.equipmentManager.footSlot.GetItem();
                    break;
            }

            // 이미 같은 장비가 장착되어 있으면 아무것도 하지 않음
            if (equippedItem == item)
            {
                Debug.Log($"[InventoryDisplay] 이미 장착 중인 장비입니다 : {item.itemName}");
                return;
            }
        }

        // 장비 장착
        inventory.EquipItem(item);

        // 장착에 사용한 인벤토리 아이템 제거
        inventory.RemoveItem(index);
    }

    //============== 아이템 버리기 ==============

    public void DropSelected(int index)
    {
        Debug.Log($"DropSelected 호출 : {index}");

        if (inventory == null)
            return;

        inventory.RemoveItem(index);

        selectedIndex = Mathf.Clamp(
            selectedIndex,
            0,
            Mathf.Max(inventory.items.Count - 1, 0)
        );

        RefreshSelection();
    }
}