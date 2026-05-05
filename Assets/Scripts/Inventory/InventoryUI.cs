using UnityEngine;

// 인벤토리 UI 관리
// 슬롯 이동/선택 로직은 InventoryNavigationController로 분리
public class InventoryUI : MonoBehaviour
{
    [Header("패널")]
    public GameObject inventoryPanel; // 인벤토리 패널 (인스펙터에서 드래그드롭으로 할당)

    [Header("칸 이동 컨트롤러")]
    public InventoryNavigationController navigationController;

    [Header("장착 스탯 적용 대상")]
    public PlayerController playerController;

    private bool isInitialized;

    void Start()
    {
        InitializeIfNeeded();
    }

    private void InitializeIfNeeded()
    {
        if (isInitialized) return;

        if (inventoryPanel == null)
            Debug.LogWarning("InventoryPanel을 인스펙터에서 할당해 주세요.");

        if (navigationController == null)
            navigationController = GetComponentInChildren<InventoryNavigationController>(true);

        if (playerController == null)
            playerController = FindFirstObjectByType<PlayerController>();

        if (navigationController != null)
            navigationController.onConfirmSlot = HandleConfirmSlot;

        isInitialized = true;
    }

    // 인벤토리 UI 열기
    public void Open()
    {
        InitializeIfNeeded();

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            if (navigationController != null)
                navigationController.ResetNavigation();
            Debug.Log("인벤토리 UI 열림");
        }
    }

    // 인벤토리 UI 닫기
    public void Close()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
            Debug.Log("인벤토리 UI 닫음");
        }
    }

    // 플레이어 입력(OnMove)에서 호출
    public void Navigate(Vector2 moveInput)
    {
        if (inventoryPanel == null || !inventoryPanel.activeSelf) return;
        if (navigationController == null) return;

        navigationController.Navigate(moveInput);
    }

    public void ConfirmCurrentSelection()
    {
        if (inventoryPanel == null || !inventoryPanel.activeSelf) return;
        if (navigationController == null) return;

        navigationController.ConfirmCurrentSelection();
    }

    // 인벤토리 일반 슬롯에 아이템 추가 시도(앞에서부터 빈칸 우선)
    public bool TryAddItemToInventory(InventoryItemData item)
    {
        InitializeIfNeeded();

        if (item == null) return false;
        if (navigationController == null || navigationController.inventorySlots == null)
            return false;

        for (int i = 0; i < navigationController.inventorySlots.Length; i++)
        {
            InventorySlotUI slot = navigationController.inventorySlots[i];
            if (slot == null) continue;
            if (!slot.IsEmpty()) continue;

            if (slot.TrySetItem(item))
            {
                Debug.Log($"인벤토리 획득: {item.itemName} (슬롯 {i})");
                return true;
            }
        }

        Debug.LogWarning($"인벤토리가 가득 차서 '{item.itemName}'을(를) 획득하지 못했습니다.");
        return false;
    }

    private void HandleConfirmSlot(InventorySlotUI selectedSlot)
    {
        if (selectedSlot == null || selectedSlot.currentItem == null)
            return;

        // 인벤토리 슬롯에서만 장착 시도
        if (selectedSlot.isEquipmentSlot)
            return;

        if (navigationController == null || navigationController.equipmentSlots == null)
            return;

        InventoryItemData item = selectedSlot.currentItem;
        InventorySlotUI targetEquipmentSlot = FindTargetEquipmentSlot(item);

        if (targetEquipmentSlot == null)
        {
            Debug.Log($"장착 가능한 슬롯이 없습니다: {item.itemName}");
            return;
        }

        InventoryItemData previousEquipped = targetEquipmentSlot.currentItem;

        if (!targetEquipmentSlot.TrySetItem(item))
        {
            Debug.Log($"장착 실패: {item.itemName}");
            return;
        }

        if (previousEquipped == null)
        {
            selectedSlot.ClearItem();
        }
        else
        {
            // 같은 칸에 스왑
            selectedSlot.TrySetItem(previousEquipped);
        }

        if (playerController != null)
        {
            if (previousEquipped != null)
                playerController.RemoveItemStats(previousEquipped.stats);

            // 무기 장착 시 플레이어 무기 타입 동기화
            if (item.itemType == InventoryItemType.Weapon)
                playerController.SetCurrentWeaponFromInventoryType(item.weaponType);

            playerController.ApplyItemStats(item.stats);
        }

        Debug.Log($"아이템 장착 완료: {item.itemName}");
    }

    private InventorySlotUI FindTargetEquipmentSlot(InventoryItemData item)
    {
        if (item == null || navigationController == null || navigationController.equipmentSlots == null)
            return null;

        EquipmentType targetType = EquipmentType.None;

        if (item.itemType == InventoryItemType.Weapon)
            targetType = EquipmentType.Weapon;
        else if (item.itemType == InventoryItemType.Equipment)
            targetType = item.equipmentType;
        else
            return null;

        for (int i = 0; i < navigationController.equipmentSlots.Length; i++)
        {
            InventorySlotUI eqSlot = navigationController.equipmentSlots[i];
            if (eqSlot == null) continue;
            if (!eqSlot.isEquipmentSlot) continue;
            if (eqSlot.allowedEquipmentType != targetType) continue;
            if (!eqSlot.CanPlaceItem(item)) continue;
            return eqSlot;
        }

        return null;
    }
}
