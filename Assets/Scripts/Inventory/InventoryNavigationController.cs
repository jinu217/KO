using UnityEngine;

// 인벤토리 칸 선택/이동 전용 컨트롤러
// 사용자 지정 이동 규칙(장비 5칸 + 인벤토리 12칸)에 맞춘 네비게이션
public class InventoryNavigationController : MonoBehaviour
{
    private enum SelectionArea
    {
        None,
        Equipment,
        Inventory
    }

    public System.Action<InventorySlotUI> onConfirmSlot;
    [Header("장비 슬롯(5)")]
    public InventorySlotUI[] equipmentSlots = new InventorySlotUI[5];

    [Header("인벤토리 슬롯(12)")]
    public InventorySlotUI[] inventorySlots = new InventorySlotUI[12];

    [Header("표시 색")]
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(1f, 0.9f, 0.3f);

    [Header("입력 반복 지연")]
    public float moveRepeatDelay = 0.16f;

    // 장비 슬롯 인덱스 규약
    // 0: 머리, 1: 몸통, 2: 장갑, 3: 신발, 4: 무기
    private const int EQ_HEAD = 0;
    private const int EQ_BODY = 1;
    private const int EQ_GLOVES = 2;
    private const int EQ_BOOTS = 3;
    private const int EQ_WEAPON = 4;

    private SelectionArea selectionArea = SelectionArea.None;
    private int equipmentIndex = 0;
    private int inventoryIndex = 0;
    private float lastMoveTime;

    public void ResetNavigation()
    {
        // 1) 인벤토리 오픈 시 아무것도 선택되지 않음
        selectionArea = SelectionArea.None;
        equipmentIndex = Mathf.Clamp(equipmentIndex, 0, Mathf.Max(0, equipmentSlots.Length - 1));
        inventoryIndex = Mathf.Clamp(inventoryIndex, 0, Mathf.Max(0, inventorySlots.Length - 1));
        RefreshSelectionVisual();
    }

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

        // 2) 첫 입력 규칙
        if (selectionArea == SelectionArea.None)
        {
            if (dy > 0)
                SelectEquipment(EQ_HEAD);   // 위: 머리
            else
                SelectInventory(0);         // 좌/우/아래: 인벤토리 0번

            RefreshSelectionVisual();
            return;
        }

        if (selectionArea == SelectionArea.Equipment)
            MoveInEquipment(dx, dy);
        else if (selectionArea == SelectionArea.Inventory)
            MoveInInventory(dx, dy);

        RefreshSelectionVisual();
    }

    private void MoveInEquipment(int dx, int dy)
    {
        // 9) 머리 <-> 몸통
        // 10) 몸통 <-> 신발
        // 11) 몸통 <-> 무기(좌우 일부)
        // 12) 몸통 <-> 장갑(좌우 일부)
        // 13) 신발 아래 -> 인벤토리 0
        // 14) 장갑/무기 위 -> 머리, 아래 -> 신발

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

    private void MoveInInventory(int dx, int dy)
    {
        // 인벤토리 인덱스 0~11, 4열 고정
        // 3) 0~3 아래 -> 4~7
        // 4) 0~3 위 -> 신발
        // 5) 4~7 아래 -> 8~11
        // 6) 4~7 위 -> 0~3
        // 7) 8~11 아래 -> 반응 없음
        // 8) 8~11 위 -> 4~7

        if (inventorySlots == null || inventorySlots.Length <= 0) return;

        int maxIndex = Mathf.Min(12, inventorySlots.Length) - 1;
        if (maxIndex < 0) return;

        inventoryIndex = Mathf.Clamp(inventoryIndex, 0, maxIndex);

        int row = inventoryIndex / 4; // 0,1,2

        // 좌우 이동(0~11 전체 연속 이동)
        // 예: 3에서 오른쪽 -> 4, 8에서 왼쪽 -> 7
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

        // 상하 이동(요구 규칙 적용)
        if (dy < 0)
        {
            if (row == 0)
            {
                int target = inventoryIndex + 4;
                if (target <= maxIndex) SelectInventory(target);
            }
            else if (row == 1)
            {
                int target = inventoryIndex + 4;
                if (target <= maxIndex) SelectInventory(target);
            }
            // row == 2 이면 반응 없음
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

    public void ConfirmCurrentSelection()
    {
        InventorySlotUI slot = GetSelectedSlot();
        if (slot == null) return;
        onConfirmSlot?.Invoke(slot);
    }

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
        // SelectionArea.None이면 아무 슬롯도 선택하지 않음
    }
}
