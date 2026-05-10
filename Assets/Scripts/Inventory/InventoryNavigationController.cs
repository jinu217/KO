using UnityEngine;

// [인벤토리 네비게이션 컨트롤러]
// - 장비 슬롯(5칸) + 인벤토리 슬롯(12칸)의 선택 이동 규칙 담당
// - 실제 장착/해제 로직은 InventoryUI가 처리
public class InventoryNavigationController : MonoBehaviour
{
    private enum SelectionArea
    {
        None,
        Equipment,
        Inventory
    }

    // 슬롯 확인(Z 입력) 시 InventoryUI에서 구독하는 콜백
    public System.Action<InventorySlotUI> onConfirmSlot;

    [Header("장비 슬롯(5)")]
    [Tooltip("순서: 0=머리, 1=몸통, 2=장갑, 3=신발, 4=무기")]
    public InventorySlotUI[] equipmentSlots = new InventorySlotUI[5];

    [Header("인벤토리 슬롯(12)")]
    [Tooltip("4x3 그리드(0~11)")]
    public InventorySlotUI[] inventorySlots = new InventorySlotUI[12];

    [Header("선택 색상")]
    [Tooltip("기본 슬롯 색상")] public Color normalColor = Color.white;
    [Tooltip("선택된 슬롯 색상")] public Color selectedColor = new Color(1f, 0.9f, 0.3f);

    [Header("입력 반복 지연")]
    [Tooltip("연속 입력 사이의 최소 간격(초)")]
    public float moveRepeatDelay = 0.16f;

    [Header("디버그")]
    [Tooltip("선택 이동 디버그 로그 출력")]
    [SerializeField] private bool showDebugLog = false;

    // 장비 슬롯 인덱스 규약
    private const int EQ_HEAD = 0;
    private const int EQ_BODY = 1;
    private const int EQ_GLOVES = 2;
    private const int EQ_BOOTS = 3;
    private const int EQ_WEAPON = 4;

    private SelectionArea selectionArea = SelectionArea.None;
    private int equipmentIndex = 0;
    private int inventoryIndex = 0;
    private float lastMoveTime;

    // 인벤토리 UI 오픈 시 선택 상태 초기화
    public void ResetNavigation()
    {
        selectionArea = SelectionArea.None;
        equipmentIndex = Mathf.Clamp(equipmentIndex, 0, Mathf.Max(0, equipmentSlots.Length - 1));
        inventoryIndex = Mathf.Clamp(inventoryIndex, 0, Mathf.Max(0, inventorySlots.Length - 1));
        RefreshSelectionVisual();
    }

    // Move 입력 기반 선택 이동
    public void Navigate(Vector2 moveInput)
    {
        if (Mathf.Abs(moveInput.x) < 0.5f && Mathf.Abs(moveInput.y) < 0.5f) return;
        if (Time.unscaledTime < lastMoveTime + moveRepeatDelay) return;

        lastMoveTime = Time.unscaledTime;

        int dx = 0;
        int dy = 0;

        if (Mathf.Abs(moveInput.x) >= Mathf.Abs(moveInput.y))
            dx = moveInput.x > 0 ? 1 : -1;
        else
            dy = moveInput.y > 0 ? 1 : -1;

        // 첫 입력: 위는 장비(머리), 그 외는 인벤토리 0번
        if (selectionArea == SelectionArea.None)
        {
            if (dy > 0)
                SelectEquipment(EQ_HEAD);
            else
                SelectInventory(0);

            RefreshSelectionVisual();
            return;
        }

        if (selectionArea == SelectionArea.Equipment)
            MoveInEquipment(dx, dy);
        else if (selectionArea == SelectionArea.Inventory)
            MoveInInventory(dx, dy);

        if (showDebugLog)
            Debug.Log($"[InventoryNav] area={selectionArea}, eqIndex={equipmentIndex}, invIndex={inventoryIndex}");

        RefreshSelectionVisual();
    }

    // 현재 선택 슬롯 반환
    public InventorySlotUI GetSelectedSlot()
    {
        if (selectionArea == SelectionArea.Equipment)
        {
            if (equipmentSlots != null && equipmentIndex >= 0 && equipmentIndex < equipmentSlots.Length)
                return equipmentSlots[equipmentIndex];
        }
        else if (selectionArea == SelectionArea.Inventory)
        {
            if (inventorySlots != null && inventoryIndex >= 0 && inventoryIndex < inventorySlots.Length)
                return inventorySlots[inventoryIndex];
        }

        return null;
    }

    // 현재 선택 슬롯에 대해 InventoryUI에 확정 콜백 호출
    public void ConfirmCurrentSelection()
    {
        InventorySlotUI slot = GetSelectedSlot();
        if (slot == null) return;

        onConfirmSlot?.Invoke(slot);
    }

    // 장비 슬롯 내 이동 규칙(머리-몸통-신발 세로축, 장갑-몸통-무기 가로축)
    private void MoveInEquipment(int dx, int dy)
    {
        switch (equipmentIndex)
        {
            case EQ_HEAD:
                if (dy < 0) SelectEquipment(EQ_BODY);
                else if (dx > 0) SelectEquipment(EQ_WEAPON);
                else if (dx < 0) SelectEquipment(EQ_GLOVES);
                break;

            case EQ_BODY:
                if (dy > 0) SelectEquipment(EQ_HEAD);
                else if (dy < 0) SelectEquipment(EQ_BOOTS);
                else if (dx > 0) SelectEquipment(EQ_WEAPON);
                else if (dx < 0) SelectEquipment(EQ_GLOVES);
                break;

            case EQ_GLOVES:
                if (dx > 0) SelectEquipment(EQ_BODY);
                else if (dy > 0) SelectEquipment(EQ_HEAD);
                else if (dy < 0) SelectEquipment(EQ_BOOTS);
                break;

            case EQ_BOOTS:
                if (dy > 0) SelectEquipment(EQ_BODY);
                else if (dy < 0) SelectInventory(0);
                else if (dx > 0) SelectEquipment(EQ_WEAPON);
                else if (dx < 0) SelectEquipment(EQ_GLOVES);
                break;

            case EQ_WEAPON:
                if (dx < 0) SelectEquipment(EQ_BODY);
                else if (dy > 0) SelectEquipment(EQ_HEAD);
                else if (dy < 0) SelectEquipment(EQ_BOOTS);
                break;
        }
    }

    // 인벤토리 슬롯(4x3) 이동 규칙
    private void MoveInInventory(int dx, int dy)
    {
        if (inventorySlots == null || inventorySlots.Length <= 0) return;

        int maxIndex = Mathf.Min(12, inventorySlots.Length) - 1;
        if (maxIndex < 0) return;

        inventoryIndex = Mathf.Clamp(inventoryIndex, 0, maxIndex);
        int row = inventoryIndex / 4;

        if (dx > 0 && inventoryIndex < maxIndex)
        {
            SelectInventory(inventoryIndex + 1);
            return;
        }

        if (dx < 0 && inventoryIndex > 0)
        {
            SelectInventory(inventoryIndex - 1);
            return;
        }

        if (dy < 0)
        {
            if (row == 0 || row == 1)
            {
                int target = inventoryIndex + 4;
                if (target <= maxIndex) SelectInventory(target);
            }
        }
        else if (dy > 0)
        {
            if (row == 0)
            {
                SelectEquipment(EQ_BOOTS);
            }
            else if (row == 1 || row == 2)
            {
                int target = inventoryIndex - 4;
                if (target >= 0) SelectInventory(target);
            }
        }
    }

    private void SelectEquipment(int index)
    {
        if (equipmentSlots == null || equipmentSlots.Length == 0) return;
        equipmentIndex = Mathf.Clamp(index, 0, equipmentSlots.Length - 1);
        selectionArea = SelectionArea.Equipment;
    }

    private void SelectInventory(int index)
    {
        if (inventorySlots == null || inventorySlots.Length == 0) return;
        inventoryIndex = Mathf.Clamp(index, 0, inventorySlots.Length - 1);
        selectionArea = SelectionArea.Inventory;
    }

    // 선택 색상 시각화 갱신(모든 슬롯 normal로 초기화 후 현재 선택 슬롯만 selected)
    private void RefreshSelectionVisual()
    {
        if (equipmentSlots != null)
        {
            for (int i = 0; i < equipmentSlots.Length; i++)
            {
                if (equipmentSlots[i] != null)
                    equipmentSlots[i].SetSelected(false, normalColor, selectedColor);
            }
        }

        if (inventorySlots != null)
        {
            for (int i = 0; i < inventorySlots.Length; i++)
            {
                if (inventorySlots[i] != null)
                    inventorySlots[i].SetSelected(false, normalColor, selectedColor);
            }
        }

        if (selectionArea == SelectionArea.Equipment)
        {
            if (equipmentSlots != null && equipmentIndex >= 0 && equipmentIndex < equipmentSlots.Length && equipmentSlots[equipmentIndex] != null)
                equipmentSlots[equipmentIndex].SetSelected(true, normalColor, selectedColor);
        }
        else if (selectionArea == SelectionArea.Inventory)
        {
            if (inventorySlots != null && inventoryIndex >= 0 && inventoryIndex < inventorySlots.Length && inventorySlots[inventoryIndex] != null)
                inventorySlots[inventoryIndex].SetSelected(true, normalColor, selectedColor);
        }
    }
}
