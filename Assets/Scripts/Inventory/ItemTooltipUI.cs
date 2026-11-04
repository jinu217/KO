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

    //============== 기계장비 네온 이름 ==============
    [Header("기계장비 네온 이름")]
    [Tooltip("기계장비 이름에만 쓸 네온용 머티리얼 (TMP 폰트 머티리얼을 복제해서 Glow/Outline 설정)")]
    public Material neonNameMaterial;

    [Tooltip("기계장비 이름 글자색")]
    public Color neonNameColor = new Color(0f, 1f, 1f, 1f);
    //======================================================

    // 일반 장비용 원래 스타일 (처음 Show 때 한 번만 저장)
    private Material defaultNameMaterial;
    private Color defaultNameColor;
    private bool defaultNameCached = false;

    public void Show(ItemData item, RectTransform slotRect)
    {
        if (item == null)
        {
            Hide();
            return;
        }

        tooltip.SetActive(true);

        itemName.text = item.itemName;

        //============== 이름 스타일 적용 (기계장비면 네온, 아니면 원래대로) ==============
        ApplyNameStyle(item);

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

    //============== 이름 스타일 적용 ==============
    void ApplyNameStyle(ItemData item)
    {
        if (itemName == null)
            return;

        // 원래 스타일을 바꾸기 전에 한 번만 저장
        if (!defaultNameCached)
        {
            defaultNameCached = true;
            defaultNameMaterial = itemName.fontSharedMaterial;
            defaultNameColor = itemName.color;
        }

        bool isMachine = item.rarity == ItemRarity.Mechanization;

        if (isMachine && neonNameMaterial != null)
        {
            itemName.fontSharedMaterial = neonNameMaterial;
            itemName.color = neonNameColor;
        }
        else
        {
            itemName.fontSharedMaterial = defaultNameMaterial;
            itemName.color = defaultNameColor;
        }
    }

    public void Hide()
    {
        tooltip.SetActive(false);
    }
}