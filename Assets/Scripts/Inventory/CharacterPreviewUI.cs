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

    //============== 무기 종류별 표시 설정 ==============
    [System.Serializable]
    public class WeaponPreviewSetting
    {
        public InventoryWeaponType type;                  // 검, 낫, 단검, 둔기

        [Tooltip("이 종류의 무기가 화면에서 차지할 긴 변의 길이(UI 픽셀). 이미지 크기와 상관없이 이 길이에 맞춰짐")]
        public float targetLength = 200f;

        public Vector2 positionOffset = Vector2.zero;     // 손 위치 기준 미세 조정
        public float rotationZ = 0f;                      // 이 종류의 기본 각도
        public bool flipX = false;                        // 좌우 반전
    }

    [Header("무기 종류별 표시 설정 (검, 낫, 단검, 둔기)")]
    public WeaponPreviewSetting[] weaponSettings;

    [Header("무기 위치 조절용 (테스트 때만 켜기)")]
    [Tooltip("켜면 매 프레임 미리보기를 갱신해서, 값을 바꾸는 즉시 화면에 반영됨")]
    public bool liveTune = false;

    // 씬에서 배치해 둔 weaponImage 위치 = 손이 무기를 잡는 위치
    private Vector2 weaponBasePosition;
    //============== 무기 종류별 표시 설정 끝 ==============

    void Awake()
    {
        if (weaponImage != null)
            weaponBasePosition = weaponImage.rectTransform.anchoredPosition;
    }

    void Start()
    {
        Refresh();
    }

    //============== 실시간 조절 ==============
    void Update()
    {
        if (liveTune)
            Refresh();
    }
    //============== 실시간 조절 끝 ==============

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
            weaponImage.sprite = weapon != null ? weapon.previewSprite : null;

            // 무기 종류 + 무기 개별 보정을 적용
            if (weapon != null)
                ApplyWeaponSetting(weapon);
        }

        UpdateSlot(equipmentManager.headSlot.GetItem());
        UpdateSlot(equipmentManager.bodySlot.GetItem());
        UpdateSlot(equipmentManager.handSlot.GetItem());
        UpdateSlot(equipmentManager.footSlot.GetItem());
    }
    //============== 미리보기 갱신 끝 ==============

    //============== 무기 종류별 + 개별 보정 적용 ==============
    void ApplyWeaponSetting(ItemData weapon)
    {
        if (weaponImage == null || weaponImage.sprite == null)
            return;

        WeaponPreviewSetting setting = FindWeaponSetting(weapon.weaponType);

        if (setting == null)
        {
            Debug.LogWarning($"[CharacterPreviewUI] {weapon.weaponType} 설정이 없습니다. ({weapon.itemName})");
            return;
        }

        Sprite sprite = weaponImage.sprite;
        RectTransform rt = weaponImage.rectTransform;

        // ----- 크기: 긴 변을 종류별 targetLength에 맞추고, 무기별 배율을 곱함 -----
        Vector2 spriteSize = sprite.rect.size;
        float longSide = Mathf.Max(spriteSize.x, spriteSize.y);
        float factor = longSide > 0f ? setting.targetLength / longSide : 1f;
        factor *= weapon.previewScaleMultiplier;

        rt.sizeDelta = spriteSize * factor;

        // ----- 잡는 곳(Pivot): 스프라이트 Pivot이 아니라 ItemData의 previewGripPivot 사용 -----
        rt.pivot = weapon.previewGripPivot;

        // ----- 위치: 종류별 보정 + 무기별 보정 -----
        rt.anchoredPosition = weaponBasePosition
                              + setting.positionOffset
                              + weapon.previewPositionOffset;

        // ----- 각도: 종류별 기본 각도 + 무기별 보정 각도 -----
        rt.localRotation = Quaternion.Euler(0f, 0f, setting.rotationZ + weapon.previewRotationOffset);

        // ----- 반전 -----
        rt.localScale = new Vector3(setting.flipX ? -1f : 1f, 1f, 1f);
    }

    WeaponPreviewSetting FindWeaponSetting(InventoryWeaponType type)
    {
        if (weaponSettings == null)
            return null;

        foreach (WeaponPreviewSetting s in weaponSettings)
        {
            if (s.type == type)
                return s;
        }

        return null;
    }
    //============== 무기 종류별 + 개별 보정 적용 끝 ==============

    void UpdateSlot(ItemData item)
    {
        if (item == null)
            return;

        switch (item.equipType)
        {
            case EquipmentType.Weapon:
                if (weaponImage != null)
                {
                    weaponImage.enabled = true;
                    weaponImage.sprite = item.previewSprite;
                    ApplyWeaponSetting(item);
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