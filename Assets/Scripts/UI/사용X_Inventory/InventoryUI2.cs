using UnityEngine;

// [인벤토리 UI 매니저]
// - 인벤토리 패널 열기/닫기
// - 슬롯 이동/선택 입력 위임(InventoryNavigationController)
// - 슬롯 확정 시 장비 슬롯에 장착 처리 + PlayerController 스탯 동기화
/*
public class InventoryUI : MonoBehaviour
{
    [Header("패널")]
    [Tooltip("인벤토리 패널(인스펙터에서 드래그드롭으로 할당)")]
    public GameObject inventoryPanel;

    [Header("디버그")]
    [Tooltip("열림/닫힘/획득 디버그 로그 출력")]
    [SerializeField] private bool showDebugLog = true;

    [Header("칸 이동 컨트롤러")]
    [Tooltip("선택 이동/확인 입력을 처리할 NavigationController")]
    public InventoryNavigationController navigationController;

    [Header("장착 스탯 적용 대상")]
    [Tooltip("스탯/무기 동기화 대상 PlayerController")]
    public PlayerController playerController;

    private bool isInitialized;

    void Start()
    {
        InitializeIfNeeded();
    }

    // 자동 참조 연결 + 콜백 등록(중복 호출 안전)
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

            if (showDebugLog)
                Debug.Log("인벤토리 UI 열림");
        }
    }

    // 인벤토리 UI 닫기
    public void Close()
    {
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);

            if (showDebugLog)
                Debug.Log("인벤토리 UI 닫음");
        }
    }

    // 플레이어 입력(OnMove)에서 호출 - 패널 열려 있을 때만 동작
    public void Navigate(Vector2 moveInput)
    {
        if (inventoryPanel == null || !inventoryPanel.activeSelf) return;
        if (navigationController == null) return;

        navigationController.Navigate(moveInput);
    }

    // 플레이어 입력(OnAttack)에서 호출 - 현재 선택 슬롯 확정
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
                if (showDebugLog)
                    Debug.Log($"인벤토리 획득: {item.itemName} (슬롯 {i})");
                return true;
            }
        }

        Debug.LogWarning($"인벤토리가 가득 차서 '{item.itemName}'을(를) 획득하지 못했습니다.");
        return false;
    }

    // 슬롯 확정 콜백: 인벤토리 슬롯이면 매칭되는 장비 슬롯에 장착(필요 시 스왑)
    private void HandleConfirmSlot(InventorySlotUI selectedSlot)
    {
        if (selectedSlot == null || selectedSlot.currentItem == null)
            return;

        if (selectedSlot.isEquipmentSlot)
            return;

        if (navigationController == null || navigationController.equipmentSlots == null)
            return;

        InventoryItemData item = selectedSlot.currentItem;
        InventorySlotUI targetEquipmentSlot = FindTargetEquipmentSlot(item);

        if (targetEquipmentSlot == null)
        {
            if (showDebugLog)
                Debug.Log($"장착 가능한 슬롯이 없습니다: {item.itemName}");
            return;
        }

        InventoryItemData previousEquipped = targetEquipmentSlot.currentItem;

        if (!targetEquipmentSlot.TrySetItem(item))
        {
            if (showDebugLog)
                Debug.Log($"장착 실패: {item.itemName}");
            return;
        }

        if (previousEquipped == null)
            selectedSlot.ClearItem();
        else
            selectedSlot.TrySetItem(previousEquipped);

        if (playerController != null)
        {
            if (previousEquipped != null)
                playerController.RemoveItemStats(previousEquipped.stats);

            if (item.itemType == InventoryItemType.Weapon)
                playerController.SetCurrentWeaponFromInventoryType(item.weaponType);

            playerController.ApplyItemStats(item.stats);
        }

        if (showDebugLog)
            Debug.Log($"아이템 장착 완료: {item.itemName}");
    }

    // 아이템 타입에 맞는 장비 슬롯 탐색(첫 매칭 슬롯 반환)
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
*/