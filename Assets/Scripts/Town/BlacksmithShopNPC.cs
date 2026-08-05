using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BlacksmithShopNPC : NPCShopBase
{
    [Header("Item Database")]
    [Tooltip("전체 아이템 ItemDatabase")]
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Price")]
    [Tooltip("회차별 첫 무기 획득 가격")]
    [SerializeField, Min(0)] private int basePrice = 50;
    [Tooltip("무기 획득 가격 텍스트")]
    [SerializeField] private TMP_Text priceText;

    [Header("Rarity Chance")]
    [Tooltip("노말 무기 기본 등장 확률")]
    [SerializeField] private float normalChance = 70f;
    [Tooltip("레어 무기 기본 등장 확률")]
    [SerializeField] private float rareChance = 20f;
    [Tooltip("에픽 무기 기본 등장 확률")]
    [SerializeField] private float epicChance = 9f;
    [Tooltip("레전더리 무기 기본 등장 확률")]
    [SerializeField] private float legendaryChance = 1f;
    [Header("Chance Change Per Round")]
    [Tooltip("사망 횟수당 노말 확률 변화량")]
    [SerializeField] private float normalChangePerRound = -5f;
    [Tooltip("사망 횟수당 레어 확률 변화량")]
    [SerializeField] private float rareChangePerRound = 3f;
    [Tooltip("사망 횟수당 에픽 확률 변화량")]
    [SerializeField] private float epicChangePerRound = 1.5f;
    [Tooltip("사망 횟수당 레전더리 확률 변화량")]
    [SerializeField] private float legendaryChangePerRound = 0.5f;

    [Header("Acquire Button")]
    [Tooltip("무기 획득 버튼 이미지")]
    [SerializeField] private Image acquireButtonImage;
    [Tooltip("버튼 기본 색상")]
    [SerializeField] private Color normalColor = Color.white;
    [Tooltip("버튼 선택 색상")]
    [SerializeField] private Color selectedColor = Color.yellow;

    [Header("Result UI")]
    [Tooltip("획득한 무기 아이콘 이미지")]
    [SerializeField] private Image resultWeaponImage;
    [Tooltip("획득한 무기 이름 및 능력치 텍스트")]
    [SerializeField] private TMP_Text resultWeaponText;

    protected override CurrencyType PaymentCurrency => CurrencyType.Coin;
    protected override bool HasFirstSelection => false;

    protected override bool CanOpenShop()
    {
        if (GetValidWeapons().Count > 0)
            return true;
        Debug.LogWarning("대장장이 무기 목록 설정 필요", this);
        return false;
    }

    protected override void OnShopOpened()
    {
        EnsureCurrentRound();
        if (acquireButtonImage != null)
            acquireButtonImage.color = selectedColor;
        RefreshPrice();
        SetResult(null);
    }

    protected override void OnVerticalInput(int direction) { }

    protected override void OnAttackInput()
    {
        EnsureCurrentRound();
        int price = CalculateCurrentPrice();
        ItemData weapon = RollWeapon();
        if (weapon == null)
        {
            SetFeedback("획득 가능한 무기 없음");
            return;
        }
        if (!TryPay(price))
            return;

        PlayerItemManager.AddItem(weapon);
        SaveData.Instance.run.blacksmithPurchaseCount++;
        SetResult(weapon);
        RefreshPrice();
        SaveRuntimeState();
        SetFeedback($"{weapon.itemName} 획득 완료");
    }

    protected override void CancelFirstSelection() { }

    private void EnsureCurrentRound()
    {
        if (SaveData.Instance == null)
            return;
        int round = Mathf.Max(0, SaveData.Instance.persistent.deathCount);
        if (SaveData.Instance.run.blacksmithRound == round)
            return;
        SaveData.Instance.run.blacksmithRound = round;
        SaveData.Instance.run.blacksmithPurchaseCount = 0;
    }

    private int CalculateCurrentPrice()
    {
        int price = Mathf.Max(0, basePrice);
        int count = SaveData.Instance != null ? SaveData.Instance.run.blacksmithPurchaseCount : 0;
        for (int i = 0; i < count; i++)
            price += Mathf.FloorToInt(price * 0.1f + 0.5f);
        return price;
    }

    private void RefreshPrice()
    {
        if (priceText != null)
            priceText.text = $"{CalculateCurrentPrice()} Coin";
    }

    private ItemData RollWeapon()
    {
        List<ItemData> validWeapons = GetValidWeapons();
        ItemRarity rarity = RollRarity();
        List<ItemData> rarityWeapons = validWeapons.FindAll(item => item.rarity == rarity);
        if (rarityWeapons.Count == 0)
            rarityWeapons = validWeapons;
        return rarityWeapons.Count > 0 ? rarityWeapons[UnityEngine.Random.Range(0, rarityWeapons.Count)] : null;
    }

    private List<ItemData> GetValidWeapons()
    {
        List<ItemData> weapons = new List<ItemData>();
        if (itemDatabase == null)
            return weapons;

        foreach (ItemData item in itemDatabase.Items)
        {
            if (item != null && item.itemType == ItemType.Equipment &&
                item.equipType == EquipmentType.Weapon && item.rarity != ItemRarity.None)
                weapons.Add(item);
        }

        return weapons;
    }

    private ItemRarity RollRarity()
    {
        int round = SaveData.Instance != null ? Mathf.Max(0, SaveData.Instance.persistent.deathCount) : 0;
        float normal = Mathf.Max(0f, normalChance + normalChangePerRound * round);
        float rare = Mathf.Max(0f, rareChance + rareChangePerRound * round);
        float epic = Mathf.Max(0f, epicChance + epicChangePerRound * round);
        float legendary = Mathf.Max(0f, legendaryChance + legendaryChangePerRound * round);
        float roll = UnityEngine.Random.Range(0f, normal + rare + epic + legendary);
        if (roll < normal) return ItemRarity.Normal;
        if ((roll -= normal) < rare) return ItemRarity.Rare;
        if ((roll -= rare) < epic) return ItemRarity.Epic;
        return ItemRarity.Legendary;
    }

    private void SetResult(ItemData weapon)
    {
        if (resultWeaponImage != null)
        {
            resultWeaponImage.sprite = weapon != null ? weapon.icon : null;
            resultWeaponImage.enabled = weapon != null && weapon.icon != null;
            resultWeaponImage.preserveAspect = true;
        }
        if (resultWeaponText != null)
            resultWeaponText.text = weapon != null ? BuildItemDescription(weapon) : string.Empty;
    }

    private static string BuildItemDescription(ItemData item)
    {
        StringBuilder text = new StringBuilder($"{item.itemName} [{item.rarity}]");
        if (item.attack != 0) text.Append($"\n공격력 +{item.attack}");
        if (item.attackSpeed != 0f) text.Append($"\n공격속도 +{item.attackSpeed:0.##}");
        if (item.criticalChance != 0f) text.Append($"\n치명타 확률 +{item.criticalChance:0.##}");
        if (item.criticalDamage != 0f) text.Append($"\n치명타 피해 +{item.criticalDamage:0.##}");
        return text.ToString();
    }

}
