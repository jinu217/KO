using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventorySlotUI : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    public ItemData item;
    public Image icon;
    public GameObject highlight;

    void Awake()
    {
        if (highlight != null)
            highlight.SetActive(false);
    }

    public void Setup(ItemData newItem)
    {
        item = newItem;

        if (item != null)
        {
            icon.sprite = item.icon;
            icon.enabled = true;
        }
        else
        {
            icon.sprite = null;
            icon.enabled = false;
        }
    }

    // 기존 InventoryDisplay에서 호출하는 함수
    public void SetSelected(bool isSelected)
    {
        if (highlight != null)
            highlight.SetActive(isSelected);
    }

    // EventSystem 선택
    public void OnSelect(BaseEventData eventData)
    {
        SetSelected(true);
    }

    // EventSystem 선택 해제
    public void OnDeselect(BaseEventData eventData)
    {
        SetSelected(false);
    }
}