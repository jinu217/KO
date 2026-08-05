using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class BuffShamanPotionProduct
{
    [Header("Potion Data")]
    [Tooltip("판매 포션 ItemData")]
    public ItemData potion;
    [Tooltip("포션 1개당 코인 가격")]
    [Min(0)] public int coinPrice;

    [Header("Panel Slot")]
    [Tooltip("포션 이름 및 가격 텍스트")]
    public TMP_Text productText;
    [Tooltip("포션 아이콘 이미지")]
    public Image iconImage;
}

[Serializable]
public class BuffShamanStatProduct
{
    [Header("Stat Data")]
    [Tooltip("능력치 상품 표시 이름")]
    public string displayName;
    [Tooltip("영구 강화 종류")]
    public ShamanUpgradeType upgradeType;
    [Tooltip("1회당 능력치 증가량")]
    public float amountPerPurchase = 1f;
    [Tooltip("1회당 소울 가격")]
    [Min(0)] public int soulPrice;
    [Tooltip("최대 상승 횟수")]
    [Min(1)] public int maxPurchaseCount = 1;
    [Tooltip("능력치 상품 아이콘")]
    public Sprite statIcon;

    [Header("Panel Slot")]
    [Tooltip("능력치 이름 증가량 가격 텍스트")]
    public TMP_Text productText;
    [Tooltip("능력치 아이콘 이미지")]
    public Image iconImage;
}

public class BuffShamanShopNPC : NPCShopBase
{
    [Header("Potions - Coin")]
    [Tooltip("코인 포션 상품 목록")]
    [SerializeField] private BuffShamanPotionProduct[] potions = Array.Empty<BuffShamanPotionProduct>();

    [Header("Stats - Soul")]
    [Tooltip("소울 능력치 상품 목록")]
    [SerializeField] private BuffShamanStatProduct[] stats = Array.Empty<BuffShamanStatProduct>();

    [Header("Selection UI")]
    [Tooltip("구매 수량 상승 횟수 총가격 텍스트")]
    [SerializeField] private TMP_Text quantityText;
    [Tooltip("미선택 상품 색상")]
    [SerializeField] private Color normalColor = Color.white;
    [Tooltip("선택 상품 색상")]
    [SerializeField] private Color selectedColor = Color.yellow;
    [Tooltip("최대 구매 수량 및 상승 횟수")]
    [SerializeField, Min(1)] private int maximumPurchaseCount = 99;

    private int selectedIndex;
    private int purchaseCount = 1;
    private bool choosingCount;

    private int ProductCount => potions.Length + stats.Length;
    private bool IsPotionSelected => selectedIndex < potions.Length;

    // 실제 결제 재화는 선택 상품에 따라 Coin 또는 Soul로 결정한다.
    protected override CurrencyType PaymentCurrency => CurrencyType.Soul;
    protected override bool HasFirstSelection => choosingCount;

    protected override bool CanOpenShop()
    {
        if (ProductCount > 0)
            return true;

        Debug.LogWarning("포션 또는 능력치 상품을 하나 이상 등록해 주세요.", this);
        return false;
    }

    protected override void OnShopOpened()
    {
        selectedIndex = 0;
        purchaseCount = 1;
        choosingCount = false;
        RefreshUI();
    }

    protected override void OnVerticalInput(int direction)
    {
        if (choosingCount)
            purchaseCount = Mathf.Clamp(purchaseCount - direction, 1, GetSelectedMaximumCount());
        else
            selectedIndex = WrapIndex(selectedIndex + direction, ProductCount);

        RefreshUI();
    }

    protected override void OnAttackInput()
    {
        if (!choosingCount)
        {
            if (!IsPotionSelected && GetSelectedRemainingStatCount() <= 0)
            {
                SetFeedback("최대 상승 횟수 도달");
                return;
            }

            choosingCount = true;
            purchaseCount = 1;
            SetFeedback(string.Empty);
            RefreshUI();
            return;
        }

        if (IsPotionSelected)
            PurchasePotion();
        else
            PurchaseStat();
    }

    private void PurchasePotion()
    {
        BuffShamanPotionProduct product = potions[selectedIndex];
        if (product == null || product.potion == null ||
            product.potion.itemType != ItemType.Potion || PlayerItemManager == null)
        {
            SetFeedback("포션 ItemData 설정을 확인해 주세요.");
            return;
        }

        if (!TryPay(CurrencyType.Coin, product.coinPrice * purchaseCount))
            return;

        for (int i = 0; i < purchaseCount; i++)
            PlayerItemManager.AddItem(product.potion);

        CompletePurchase($"{product.potion.itemName} {purchaseCount}개 구매 완료");
    }

    private void PurchaseStat()
    {
        BuffShamanStatProduct product = stats[selectedIndex - potions.Length];
        if (product == null || product.amountPerPurchase <= 0f || PlayerController == null)
        {
            SetFeedback("능력치 증가량 설정을 확인해 주세요.");
            return;
        }

        int remainingCount = GetRemainingStatCount(product);
        if (remainingCount <= 0 || purchaseCount > remainingCount)
        {
            SetFeedback("최대 상승 횟수 도달");
            return;
        }

        if (!TryPay(CurrencyType.Soul, product.soulPrice * purchaseCount))
            return;

        PlayerController.PurchasePersistentUpgrade(
            product.upgradeType,
            product.amountPerPurchase,
            purchaseCount);

        CompletePurchase($"{GetStatName(product)} {purchaseCount}회 상승 완료");
    }

    private void CompletePurchase(string message)
    {
        SaveRuntimeState();
        SetFeedback(message);
        choosingCount = false;
        purchaseCount = 1;
        RefreshUI();
    }

    protected override void CancelFirstSelection()
    {
        choosingCount = false;
        purchaseCount = 1;
        SetFeedback(string.Empty);
        RefreshUI();
    }

    private void RefreshUI()
    {
        for (int i = 0; i < potions.Length; i++)
            RefreshPotionSlot(potions[i], i, i == selectedIndex);

        for (int i = 0; i < stats.Length; i++)
        {
            int combinedIndex = potions.Length + i;
            RefreshStatSlot(stats[i], combinedIndex == selectedIndex);
        }

        if (quantityText == null)
            return;

        quantityText.gameObject.SetActive(choosingCount);
        if (!choosingCount)
            return;

        if (IsPotionSelected)
        {
            BuffShamanPotionProduct product = potions[selectedIndex];
            quantityText.text = $"수량: {purchaseCount} / 합계: {product.coinPrice * purchaseCount} Coin";
        }
        else
        {
            BuffShamanStatProduct product = stats[selectedIndex - potions.Length];
            quantityText.text = $"횟수: {purchaseCount} / 합계: {product.soulPrice * purchaseCount} Soul";
        }
    }

    private void RefreshPotionSlot(BuffShamanPotionProduct product, int index, bool selected)
    {
        if (product == null)
            return;

        string itemName = product.potion != null ? product.potion.itemName : "미설정 포션";
        if (product.productText != null)
        {
            product.productText.text = $"{itemName} - {product.coinPrice} Coin";
            product.productText.color = selected ? selectedColor : normalColor;
        }

        SetIcon(product.iconImage, product.potion != null ? product.potion.icon : null, selected);
    }

    private void RefreshStatSlot(BuffShamanStatProduct product, bool selected)
    {
        if (product == null)
            return;

        int purchasedCount = PlayerController != null
            ? PlayerController.GetPersistentUpgradePurchaseCount(product.upgradeType)
            : 0;
        string actionText = choosingCount && selected ? "  [상승]" : string.Empty;
        if (product.productText != null)
        {
            product.productText.text = $"{GetStatName(product)} +{product.amountPerPurchase:0.##} - {product.soulPrice} Soul ({purchasedCount}/{product.maxPurchaseCount}){actionText}";
            product.productText.color = selected ? selectedColor : normalColor;
        }

        SetIcon(product.iconImage, product.statIcon, selected);
    }

    private void SetIcon(Image iconImage, Sprite icon, bool selected)
    {
        if (iconImage == null)
            return;

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        iconImage.preserveAspect = true;
        iconImage.color = selected ? selectedColor : normalColor;
    }

    private static string GetStatName(BuffShamanStatProduct product)
    {
        if (!string.IsNullOrWhiteSpace(product.displayName))
            return product.displayName;

        return product.upgradeType switch
        {
            ShamanUpgradeType.AttackDamage => "공격력 증가",
            ShamanUpgradeType.MaxHp => "최대 체력 증가",
            ShamanUpgradeType.AttackSpeed => "공격속도 증가",
            ShamanUpgradeType.DashCount => "대시 횟수 증가",
            ShamanUpgradeType.DashCooldownReduction => "대시 쿨다운 감소",
            ShamanUpgradeType.StartingCoin => "시작 코인 증가",
            ShamanUpgradeType.EquipmentRerollCount => "장비 리롤 횟수 증가",
            _ => product.upgradeType.ToString()
        };
    }

    private int GetSelectedMaximumCount()
    {
        if (IsPotionSelected)
            return maximumPurchaseCount;

        return Mathf.Max(1, Mathf.Min(maximumPurchaseCount, GetSelectedRemainingStatCount()));
    }

    private int GetSelectedRemainingStatCount()
    {
        return GetRemainingStatCount(stats[selectedIndex - potions.Length]);
    }

    private int GetRemainingStatCount(BuffShamanStatProduct product)
    {
        if (product == null)
            return 0;

        int purchasedCount = PlayerController != null
            ? PlayerController.GetPersistentUpgradePurchaseCount(product.upgradeType)
            : 0;
        return Mathf.Max(0, product.maxPurchaseCount - purchasedCount);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        maximumPurchaseCount = Mathf.Max(1, maximumPurchaseCount);

        for (int i = 0; i < potions.Length; i++)
            RefreshPotionSlot(potions[i], i, i == selectedIndex);

        for (int i = 0; i < stats.Length; i++)
            RefreshStatSlot(stats[i], potions.Length + i == selectedIndex);
    }
#endif
}
