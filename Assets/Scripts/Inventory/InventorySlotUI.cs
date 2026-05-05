using UnityEngine;
using UnityEngine.UI;

// 인벤토리 개별 칸(슬롯)
// - 장비 슬롯인 경우 allowedEquipmentType으로 장착 가능 타입 제한
public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image targetImage; // 슬롯 배경(선택 색상 변경용)
    [SerializeField] private Image itemIconImage; // 아이템 아이콘 표시용

    [Header("슬롯 타입")]
    public bool isEquipmentSlot = false;
    public EquipmentType allowedEquipmentType = EquipmentType.None;

    [Header("임시 보관 아이템")]
    public InventoryItemData currentItem;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        RefreshItemVisual();
    }

    public void SetSelected(bool isSelected, Color normalColor, Color selectedColor)
    {
        if (targetImage == null) return;
        targetImage.color = isSelected ? selectedColor : normalColor;
    }

    // 이 슬롯에 아이템을 넣을 수 있는지 검사
    public bool CanPlaceItem(InventoryItemData item)
    {
        if (item == null) return true;

        // 일반 인벤토리 슬롯이면 제한 없음
        if (!isEquipmentSlot) return true;

        if (allowedEquipmentType == EquipmentType.None) return false;

        // 무기 슬롯은 Weapon 타입 아이템 허용
        if (allowedEquipmentType == EquipmentType.Weapon)
        {
            return item.itemType == InventoryItemType.Weapon;
        }

        // 방어구 슬롯은 Equipment 타입 + 부위 일치 필요
        if (item.itemType != InventoryItemType.Equipment) return false;
        return item.equipmentType == allowedEquipmentType;
    }

    // 아이템 배치 시도
    public bool TrySetItem(InventoryItemData item)
    {
        if (!CanPlaceItem(item)) return false;

        currentItem = item;
        RefreshItemVisual();
        return true;
    }

    // 슬롯 아이콘 갱신
    public void RefreshItemVisual()
    {
        if (itemIconImage == null) return;

        if (currentItem != null && currentItem.itemSprite != null)
        {
            itemIconImage.sprite = currentItem.itemSprite;
            itemIconImage.enabled = true;
        }
        else
        {
            itemIconImage.sprite = null;
            itemIconImage.enabled = false;
        }
    }

    // 외부에서 슬롯 비우기용
    public void ClearItem()
    {
        currentItem = null;
        RefreshItemVisual();
    }

    public bool IsEmpty()
    {
        return currentItem == null;
    }
}
