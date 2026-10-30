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
        FindWeaponRenderer();
    }

    private void FindWeaponRenderer()
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
        ClearWeapon("장착 무기 없음");
    }

    public void ClearWeapon(string reason)
    {
        FindWeaponRenderer();
        if (weaponRenderer == null) return;
        weaponRenderer.sprite = null;
        Debug.Log($"[PlayerAppearance] 무기 숨김: {reason}", this);
    }

    private void EquipWeapon(ItemData item)
    {
        FindWeaponRenderer();
        if (weaponRenderer == null)
        {
            Debug.LogWarning("[PlayerAppearance] 무기 SpriteRenderer를 찾지 못했습니다. Weapon Renderer에 Hand_B/Sword의 렌더러를 연결하세요.", this);
            return;
        }
        if (item.previewSprite == null)
        {
            Debug.LogWarning($"[PlayerAppearance] 무기 '{item.itemName}'(ID={item.id})의 Preview Sprite가 비어 있습니다. 기존 무기 Sprite를 유지합니다. 해당 ItemData 에셋에 무기 그림을 지정하세요.", item);
            return;
        }
        weaponRenderer.sprite = item.previewSprite;
    }

    public void Equip(ItemData item)
    {
        if (item == null)
            return;

        switch (item.equipType)
        {
            case EquipmentType.Weapon:
                EquipWeapon(item);
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
