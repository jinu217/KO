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
    //============== 무기 종류별 표시 설정 끝 ==============

    //============== 무기 아이템별 보정 설정 (신규) ==============
    // 종류별 설정을 먼저 적용한 뒤, 그 위에 이 값을 덧입힘 (종류별 설정을 덮어쓰는 게 아니라 "추가 보정")
    [System.Serializable]
    public class WeaponItemOverride
    {
        [Tooltip("보정할 무기 아이템 (ItemData 에셋을 끌어다 넣기)")]
        public ItemData item;

        [Tooltip("크기 배율. 1 = 종류별 크기 그대로, 1.2 = 20% 크게, 0.8 = 20% 작게")]
        public float sizeMultiplier = 1f;

        [Tooltip("종류별 위치에 추가로 더해지는 위치 보정")]
        public Vector2 positionOffset = Vector2.zero;

        [Tooltip("종류별 각도에 추가로 더해지는 각도 (예: 종류별 -45 + 이 값 10 = -35)")]
        public float rotationZ = 0f;

        [Tooltip("체크하면 종류별 좌우 반전 상태를 뒤집음")]
        public bool flipX = false;
    }

    [Header("무기 아이템별 보정 (이미지가 달라서 따로 맞춰야 하는 무기만 추가)")]
    public WeaponItemOverride[] weaponItemOverrides;
    //============== 무기 아이템별 보정 설정 끝 ==============

    [Header("무기 위치 조절용 (테스트 때만 켜기)")]
    [Tooltip("켜면 매 프레임 미리보기를 갱신해서, 값을 바꾸는 즉시 화면에 반영됨")]
    public bool liveTune = false;

    // 씬에서 배치해 둔 weaponImage 위치 = 손이 무기를 잡는 위치
    private Vector2 weaponBasePosition;

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

            // 무기 종류별 설정 + 아이템별 보정 적용
            if (weapon != null)
                ApplyWeaponSetting(weapon);
        }

        UpdateSlot(equipmentManager.headSlot.GetItem());
        UpdateSlot(equipmentManager.bodySlot.GetItem());
        UpdateSlot(equipmentManager.handSlot.GetItem());
        UpdateSlot(equipmentManager.footSlot.GetItem());
    }
    //============== 미리보기 갱신 끝 ==============

    //============== 무기 설정 적용 (종류별 → 아이템별 보정) ==============
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

        // 이 아이템 전용 보정 (없으면 null → 보정 없이 종류별 설정만 사용)
        WeaponItemOverride ov = FindItemOverride(weapon);

        float sizeMul = ov != null ? ov.sizeMultiplier : 1f;
        Vector2 posAdd = ov != null ? ov.positionOffset : Vector2.zero;
        float rotAdd = ov != null ? ov.rotationZ : 0f;
        bool flipToggle = ov != null && ov.flipX;

        Sprite sprite = weaponImage.sprite;
        RectTransform rt = weaponImage.rectTransform;

        // ----- 크기: 긴 변을 종류별 targetLength에 맞춘 뒤, 아이템별 배율 곱함 -----
        Vector2 spriteSize = sprite.rect.size;
        float longSide = Mathf.Max(spriteSize.x, spriteSize.y);
        float factor = longSide > 0f ? setting.targetLength / longSide : 1f;

        rt.sizeDelta = spriteSize * factor * sizeMul;

        // ----- 잡는 곳(Pivot): 씬에 설정해 둔 Pivot을 그대로 사용하므로 코드에서 건드리지 않음 -----

        // ----- 위치: 종류별 보정 + 아이템별 보정 -----
        rt.anchoredPosition = weaponBasePosition + setting.positionOffset + posAdd;

        // ----- 각도: 종류별 각도 + 아이템별 각도 -----
        rt.localRotation = Quaternion.Euler(0f, 0f, setting.rotationZ + rotAdd);

        // ----- 반전: 종류별 반전 상태에서 아이템별 체크 시 뒤집기 -----
        bool finalFlip = setting.flipX ^ flipToggle;
        rt.localScale = new Vector3(finalFlip ? -1f : 1f, 1f, 1f);
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

    // 아이템별 보정 찾기 (신규)
    WeaponItemOverride FindItemOverride(ItemData weapon)
    {
        if (weaponItemOverrides == null || weapon == null)
            return null;

        foreach (WeaponItemOverride o in weaponItemOverrides)
        {
            if (o.item == weapon)
                return o;
        }

        return null;
    }
    //============== 무기 설정 적용 끝 ==============

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