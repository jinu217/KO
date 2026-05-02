using UnityEngine;
using UnityEngine.UI;

// 인벤토리 개별 칸(슬롯)
// - 장비 슬롯인 경우 allowedEquipmentType으로 장착 가능 타입 제한
public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image targetImage;

    [Header("슬롯 타입")]
    public bool isEquipmentSlot = false;
    public EquipmentType allowedEquipmentType = EquipmentType.None;

    [Header("임시 보관 아이템")]
    public InventoryItemData currentItem;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();
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

        // 장비 슬롯이면 장비 아이템 + 타입 일치 필요
        if (item.itemType != InventoryItemType.Equipment) return false;
        if (allowedEquipmentType == EquipmentType.None) return false;

        return item.equipmentType == allowedEquipmentType;
    }

    // 아이템 배치 시도
    public bool TrySetItem(InventoryItemData item)
    {
        if (!CanPlaceItem(item)) return false;
        currentItem = item;
        return true;
    }
}
