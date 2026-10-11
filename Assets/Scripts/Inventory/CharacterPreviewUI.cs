using UnityEngine;
using UnityEngine.UI;

public class CharacterPreviewUI : MonoBehaviour
{
    public EquipmentManager equipmentManager;

    public Image weaponImage;

    public Image headImage;

    public Image bodyImage;
    public Image shoulderLeftImage;
    public Image shoulderRightImage;

    public Image handLeftImage;
    public Image handRightImage;

    public Image footLeftImage;
    public Image footRightImage;

    void Start()
    {
        Refresh();
    }

    //============== 미리보기 갱신 ==============
    public void Refresh()
    {
        if (equipmentManager == null)
            return;

        // 무기 장착 여부에 따라 무기 이미지 표시
        ItemData weapon = equipmentManager.weaponSlot.GetItem();

        if (weaponImage != null)
        {
            weaponImage.enabled = weapon != null;

            if (weapon != null)
                weaponImage.sprite = weapon.previewSprite;
        }

        UpdateSlot(equipmentManager.headSlot.GetItem());
        UpdateSlot(equipmentManager.bodySlot.GetItem());
        UpdateSlot(equipmentManager.handSlot.GetItem());
        UpdateSlot(equipmentManager.footSlot.GetItem());
    }

    void UpdateSlot(ItemData item)
    {
        // 무기 미장착 시 검 이미지 제거
        if (item == null)
        {
            // 무기 슬롯은 이미지 초기화
            if (weaponImage != null)
                weaponImage.sprite = null;

            return;
        }

        switch (item.equipType)
        {
            case EquipmentType.Weapon:
                if (weaponImage != null)
                {
                    weaponImage.enabled = true;
                    weaponImage.sprite = item.previewSprite;
                }
                break;
                
            case EquipmentType.Head:
                if (headImage != null)
                    headImage.sprite = item.previewSprite;
                break;

            case EquipmentType.Body:
                if (bodyImage != null)
                    bodyImage.sprite = item.bodySprite;

                if (shoulderLeftImage != null)
                    shoulderLeftImage.sprite = item.shoulderLeftSprite;

                if (shoulderRightImage != null)
                    shoulderRightImage.sprite = item.shoulderRightSprite;
                break;

            case EquipmentType.Hand:
                if (handLeftImage != null)
                    handLeftImage.sprite = item.handLeftSprite;

                if (handRightImage != null)
                    handRightImage.sprite = item.handRightSprite;
                break;

            case EquipmentType.Foot:
                if (footLeftImage != null)
                    footLeftImage.sprite = item.footLeftSprite;

                if (footRightImage != null)
                    footRightImage.sprite = item.footRightSprite;
                break;
        }
    }

    void UpdatePart(ItemData item)
    {
        if(item == null)
        {
            Debug.Log("아이템 없음");
            return;
        }

        Debug.Log(
            item.itemName +
            " / 타입: " +
            item.equipType
        );

        switch(item.equipType)
        {
            case EquipmentType.Body:

                Debug.Log(item.bodySprite);

                bodyImage.sprite =
                    item.bodySprite;

                shoulderLeftImage.sprite =
                    item.shoulderLeftSprite;

                shoulderRightImage.sprite =
                    item.shoulderRightSprite;

                break;
        }
    }
}
