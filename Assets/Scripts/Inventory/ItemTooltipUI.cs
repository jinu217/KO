using TMPro;
using UnityEngine;

public class ItemTooltipUI : MonoBehaviour
{
    public GameObject tooltip;
    public TMP_Text itemName;
    public TMP_Text statText;
    public RectTransform tooltipTransform;

    // 툴팁 고정 위치
    public Vector2 fixedPosition = new Vector2(500f, 0f);

    public void Show(ItemData item, RectTransform slotRect)
    {
        if (item == null)
        {
            Hide();
            return;
        }

        tooltip.SetActive(true);

        itemName.text = item.itemName;

        string text = "";

        if (item.attack != 0)
            text += "공격력 +" + item.attack + "\n";

        if (item.attackSpeed != 0)
            text += "공격속도 +" + item.attackSpeed + "\n";

        if (item.criticalChance != 0)
            text += "크리확률 +" + item.criticalChance + "%\n";

        if (item.criticalDamage != 0)
            text += "크리데미지 +" + item.criticalDamage + "%\n";

        if (item.moveSpeed != 0)
            text += "이동속도 +" + item.moveSpeed + "\n";

        if (item.defense != 0)
            text += "방어력 +" + item.defense + "\n";

        if (item.hp != 0)
            text += "체력 +" + item.hp + "\n";

        statText.text = text;

        Canvas.ForceUpdateCanvases();

        // 기존 슬롯 위치 따라가기 비활성화
        // tooltipTransform.anchoredPosition =
        //     slotRect.anchoredPosition + new Vector2(280f, -600f);

        // 고정 위치
        tooltipTransform.anchoredPosition = new Vector2(-150f, -305f);
    }

    public void Hide()
    {
        tooltip.SetActive(false);
    }
}