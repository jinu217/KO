using UnityEngine;
using UnityEngine.UI;

// 인벤토리 개별 칸(슬롯)
// - 장비 슬롯인 경우 allowedEquipmentType으로 장착 가능 타입 제한
// - 작은 아이콘은 원본 크기 유지
// - 큰 아이콘은 슬롯 최대 크기 안으로 자동 축소
public class InventorySlotUI : MonoBehaviour
{
    [SerializeField] private Image targetImage; // 슬롯 배경(선택 색상 변경용)
    [SerializeField] private Image itemIconImage; // 아이템 아이콘 표시용

    [Header("아이콘 크기")]
    [Tooltip("1이면 스프라이트 원본 픽셀 크기 기준. 전체적으로 작으면 1.2~1.5 정도로 올리세요.")]
    [SerializeField] private float iconUIScale = 1f;

    [Tooltip("인벤토리 슬롯 안에서 허용할 최대 아이콘 너비")]
    [SerializeField] private float iconMaxWidth = 60f;

    [Tooltip("인벤토리 슬롯 안에서 허용할 최대 아이콘 높이")]
    [SerializeField] private float iconMaxHeight = 60f;

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

    public bool TrySetItem(InventoryItemData item)
    {
        if (!CanPlaceItem(item))
            return false;

        currentItem = item;
        RefreshItemVisual();
        return true;
    }

    public void RefreshItemVisual()
    {
        if (itemIconImage == null)
            return;

        if (currentItem != null && currentItem.itemSprite != null)
        {
            SetItemIcon(currentItem.itemSprite);
        }
        else
        {
            ClearItemIcon();
        }
    }

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

    private void ClearItemIcon()
    {
        if (itemIconImage == null) return;

        itemIconImage.sprite = null;
        itemIconImage.enabled = false;

        AspectRatioFitter fitter = itemIconImage.GetComponent<AspectRatioFitter>();
        if (fitter != null)
            Destroy(fitter);
    }

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