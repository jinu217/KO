using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlot : MonoBehaviour
{
    public EquipmentType slotType;
    public Image icon;

    //============== 기계장비 착용 배경 ==============
    [Header("기계장비 착용 배경")]
    [Tooltip("기계장비를 착용했을 때만 보이는 배경 Image 오브젝트. icon보다 Hierarchy 위쪽에 둬야 아이콘 뒤에 그려집니다.")]
    public Image mechanizedBackgroundImage;
    //======================================================

    private ItemData currentItem;

    //============== 오브젝트가 켜질 때 배경 상태 맞추기 ==============
    void OnEnable()
    {
        UpdateBackground();
    }

    public void SetItem(ItemData item)
    {
        currentItem = item;

        if (item != null)
        {
            icon.sprite = item.icon;

            // 항상 보이게
            Color c = icon.color;
            c.a = 1f;
            icon.color = c;
        }
        else
        {
            icon.sprite = null;

            // 빈 슬롯일 때 투명하게
            Color c = icon.color;
            c.a = 0f;
            icon.color = c;
        }

        //============== 기계장비 배경 갱신 ==============
        UpdateBackground();
    }

    //============== 기계장비 착용 여부에 따라 배경 표시/숨김 ==============
    void UpdateBackground()
    {
        if (mechanizedBackgroundImage == null) return;

        bool isMachine = currentItem != null
                         && currentItem.rarity == ItemRarity.Mechanization;

        mechanizedBackgroundImage.gameObject.SetActive(isMachine);
    }

    public ItemData GetItem()
    {
        return currentItem;
    }
}