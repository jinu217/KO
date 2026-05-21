using TMPro;
using UnityEngine;

public class ItemTooltipUI : MonoBehaviour
{
    public GameObject tooltip;

    public TMP_Text itemName;
    public TMP_Text statText;

    public void Show(ItemData item, Transform slot)
    {
        tooltip.SetActive(true);

        itemName.text = item.itemName;

        statText.text =
        "공격력 : " + item.attack +
        "\n방어력 : " + item.defense +
        "\n체력 : " + item.hp;

        // 슬롯 아래 위치
        tooltip.transform.position =
            slot.position + new Vector3(0,-100,0);
    }

    public void Hide()
    {
        tooltip.SetActive(false);
    }
}
