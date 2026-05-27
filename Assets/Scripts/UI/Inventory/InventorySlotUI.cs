using UnityEngine;
using UnityEngine.UI;

// [인벤토리 슬롯 UI]
// - 슬롯 선택 색상 표시
// - 아이템 아이콘 표시/해제
// - 장비 슬롯일 경우 허용 타입 검사
public class InventorySlotUI : MonoBehaviour
{
    [Header("이미지 참조")]
    [Tooltip("슬롯 배경 이미지(선택 색상 변경)")]
    [SerializeField] private Image targetImage;
    [Tooltip("아이템 아이콘 이미지")]
    [SerializeField] private Image itemIconImage;

    [Header("아이콘 크기")]
    [Tooltip("1이면 원본 픽셀 기준. 전체가 작으면 1.2~1.5로 증가")]
    [SerializeField] private float iconUIScale = 1f;
    [Tooltip("아이콘 최대 너비(px)")]
    [SerializeField] private float iconMaxWidth = 60f;
    [Tooltip("아이콘 최대 높이(px)")]
    [SerializeField] private float iconMaxHeight = 60f;

    [Header("슬롯 타입")]
    [Tooltip("이 슬롯이 장비 슬롯이면 true")]
    public bool isEquipmentSlot = false;
    [Tooltip("isEquipmentSlot일 때 허용되는 장비 타입")]
    public EquipmentType allowedEquipmentType = EquipmentType.None;

    [Header("현재 아이템")]
    [Tooltip("현재 슬롯에 들어 있는 아이템(없으면 null)")]
    public InventoryItemData currentItem;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        RefreshItemVisual();
    }

    // 슬롯 선택 여부에 따라 배경 색상 반영
    public void SetSelected(bool isSelected, Color normalColor, Color selectedColor)
    {
        if (targetImage == null) return;
        targetImage.color = isSelected ? selectedColor : normalColor;
    }

    // 현재 슬롯에 해당 아이템 배치 가능한지 검사
    public bool CanPlaceItem(InventoryItemData item)
    {
        if (item == null) return true;
        if (!isEquipmentSlot) return true;

        if (allowedEquipmentType == EquipmentType.None)
            return false;

        if (allowedEquipmentType == EquipmentType.Weapon)
            return item.itemType == InventoryItemType.Weapon;

        if (item.itemType != InventoryItemType.Equipment)
            return false;

        return item.equipmentType == allowedEquipmentType;
    }

    // 아이템 배치 시도(허용 타입 검사 후 currentItem 갱신)
    public bool TrySetItem(InventoryItemData item)
    {
        if (!CanPlaceItem(item))
            return false;

        currentItem = item;
        RefreshItemVisual();
        return true;
    }

    // currentItem 상태를 아이콘에 반영
    public void RefreshItemVisual()
    {
        if (itemIconImage == null) return;

        if (currentItem != null && currentItem.itemSprite != null)
            SetItemIcon(currentItem.itemSprite);
        else
            ClearItemIcon();
    }

    // 슬롯 아이템 비우기
    public void ClearItem()
    {
        currentItem = null;
        RefreshItemVisual();
    }

    // 슬롯 비어 있는지 여부
    public bool IsEmpty()
    {
        return currentItem == null;
    }

    // 아이콘 스프라이트 적용 + 슬롯 최대 크기 안으로 비율 축소
    private void SetItemIcon(Sprite sprite)
    {
        if (itemIconImage == null) return;

        itemIconImage.sprite = sprite;
        itemIconImage.enabled = sprite != null;
        itemIconImage.preserveAspect = true;

        AspectRatioFitter fitter = itemIconImage.GetComponent<AspectRatioFitter>();
        if (fitter != null)
            Destroy(fitter);

        if (sprite == null) return;

        RectTransform rt = itemIconImage.rectTransform;
        float safeScale = Mathf.Max(0.01f, iconUIScale);

        float width = sprite.rect.width * safeScale;
        float height = sprite.rect.height * safeScale;

        float widthScale = iconMaxWidth / Mathf.Max(width, 0.01f);
        float heightScale = iconMaxHeight / Mathf.Max(height, 0.01f);
        float fitScale = Mathf.Min(widthScale, heightScale, 1f);

        width *= fitScale;
        height *= fitScale;

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(width, height);
    }

    // 아이콘 제거
    private void ClearItemIcon()
    {
        if (itemIconImage == null) return;

        itemIconImage.sprite = null;
        itemIconImage.enabled = false;

        AspectRatioFitter fitter = itemIconImage.GetComponent<AspectRatioFitter>();
        if (fitter != null)
            Destroy(fitter);
    }
}
