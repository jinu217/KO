//플레이어 외형(장비) 변경 연결 
using UnityEngine;

public class PlayerAppearance : MonoBehaviour
{
    [Header("머리")]
    public SpriteRenderer headRenderer;

    [Header("몸")]
    public SpriteRenderer bodyRenderer;

    [Header("어깨")]
    public SpriteRenderer shoulderLeftRenderer;
    public SpriteRenderer shoulderRightRenderer;

    [Header("손")]
    public SpriteRenderer handLeftRenderer;
    public SpriteRenderer handRightRenderer;

    [Header("발")]
    public SpriteRenderer footLeftRenderer;
    public SpriteRenderer footRightRenderer;

    public void Equip(ItemData item)
    {
        if (item == null)
            return;

        switch (item.equipType)
        {
            case EquipmentType.Head:
                headRenderer.sprite = item.previewSprite;
                break;

            case EquipmentType.Body:
                bodyRenderer.sprite = item.bodySprite;

                shoulderLeftRenderer.sprite = item.shoulderLeftSprite;
                shoulderRightRenderer.sprite = item.shoulderRightSprite;
                break;

            case EquipmentType.Hand:
                handLeftRenderer.sprite = item.handLeftSprite;
                handRightRenderer.sprite = item.handRightSprite;
                break;

            case EquipmentType.Foot:
                footLeftRenderer.sprite = item.footLeftSprite;
                footRightRenderer.sprite = item.footRightSprite;
                break;
        }
    }
}