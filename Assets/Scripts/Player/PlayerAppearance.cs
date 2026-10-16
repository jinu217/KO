//플레이어 외형(장비) 변경 연결 
using UnityEngine;

public class PlayerAppearance : MonoBehaviour
{
    [Header("무기")]
    [SerializeField] private SpriteRenderer weaponRenderer;

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

    private void Awake()
    {
        if (weaponRenderer != null) return;

        // 기존 플레이어 프리팹과 씬의 손 아래 Sword 렌더러를 자동으로 연결한다.
        foreach (SpriteRenderer renderer in GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer.gameObject.name != "Sword") continue;
            weaponRenderer = renderer;
            break;
        }
    }

    public void ClearWeapon()
    {
        if (weaponRenderer != null) weaponRenderer.sprite = null;
    }

    public void Equip(ItemData item)
    {
        if (item == null)
            return;

        switch (item.equipType)
        {
            case EquipmentType.Weapon:
                if (weaponRenderer != null) weaponRenderer.sprite = item.previewSprite;
                break;

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
