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

        string text = "";

        if (item.attack != 0)
            text += "공격력 +" + item.attack + "\n";

        if (item.attackSpeed != 0)
            text += "공격속도 +" + item.attackSpeed + "\n";

        if (item.critChance != 0)
            text += "크리확률 +" + item.critChance + "%\n";

        if (item.critDamage != 0)
            text += "크리데미지 +" + item.critDamage + "%\n";

        if (item.moveSpeed != 0)
            text += "이동속도 +" + item.moveSpeed + "\n";

        if (item.defense != 0)
            text += "방어력 +" + item.defense + "\n";

        if (item.hp != 0)
            text += "체력 +" + item.hp + "\n";

        statText.text = text;

        Canvas.ForceUpdateCanvases();

        tooltipTransform.anchoredPosition =
            slotRect.anchoredPosition + new Vector2(280f, -600f);
    }

    public void Hide()
    {
        tooltip.SetActive(false);
    }
}