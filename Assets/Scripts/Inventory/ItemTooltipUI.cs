using TMPro;
using UnityEngine;

public class ItemTooltipUI : MonoBehaviour
{
    public GameObject tooltip;

    public TMP_Text itemName;
    public TMP_Text statText;

    public RectTransform tooltipTransform;

    public void Show(ItemData item, RectTransform slotRect)
    {
        if (item == null)
        {
            Hide();
            return;
        }

        tooltip.SetActive(true);

        itemName.text = item.itemName;

        statText.text =
            "공격력 : " + item.attack +
            "\n방어력 : " + item.defense +
            "\n체력 : " + item.hp;

        tooltip.SetActive(true);

        Canvas.ForceUpdateCanvases();

        Vector2 slotPos = slotRect.anchoredPosition;

        // 슬롯 아래 + 약간 오른쪽
        tooltipTransform.anchoredPosition =
            slotPos + new Vector2(140f, -185f);
    }

    public void Hide()
    {
        tooltip.SetActive(false);
    }
}