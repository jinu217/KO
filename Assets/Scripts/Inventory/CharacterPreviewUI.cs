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

    public void Refresh()
    {
        UpdateSlot(
            equipmentManager.weaponSlot.GetItem());

        UpdateSlot(
            equipmentManager.headSlot.GetItem());

        UpdateSlot(
            equipmentManager.bodySlot.GetItem());

        UpdateSlot(
            equipmentManager.handSlot.GetItem());

        UpdateSlot(
            equipmentManager.footSlot.GetItem());
    }

    void UpdateSlot(ItemData item)
    {
        if (item == null)
            return;

        switch(item.equipType)
        {
            case EquipmentType.Weapon:

                weaponImage.sprite =
                    item.previewSprite;
                break;

            case EquipmentType.Head:

                headImage.sprite =
                    item.previewSprite;
                break;

            case EquipmentType.Body:

                bodyImage.sprite =
                    item.bodySprite;

                shoulderLeftImage.sprite =
                    item.shoulderLeftSprite;

                shoulderRightImage.sprite =
                    item.shoulderRightSprite;
                break;

            case EquipmentType.Hand:

                handLeftImage.sprite =
                    item.handLeftSprite;

                handRightImage.sprite =
                    item.handRightSprite;
                break;

            case EquipmentType.Foot:

                footLeftImage.sprite =
                    item.footLeftSprite;

                footRightImage.sprite =
                    item.footRightSprite;
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