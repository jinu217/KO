using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class BuffShamanPotionProduct
{
    [Tooltip("포션 표시 이름")]
    public string displayName;
    [Tooltip("판매 포션 ItemData")]
    public ItemData potion;
    [Tooltip("포션 1개당 코인 가격")]
    [Min(0)] public int coinPrice;
    [Tooltip("한 회차에서 구매할 수 있는 총 최대 수량")]
    [Min(1)] public int maxPurchaseCount = 99;
    [Tooltip("방향키로 포션을 가리킬 때 노란색이 되는 1차 선택 텍스트")]
    public TMP_Text primarySelectionText;
    [Tooltip("공격키로 포션을 선택했을 때 노란색이 되는 2차 선택 텍스트")]
    public TMP_Text secondarySelectionText;
    [Tooltip("구매 완료 시 비활성화할 포션 관련 오브젝트")]
    public GameObject potionObject;
}

[Serializable]
public class BuffShamanStatProduct
{
    [Tooltip("스탯 표시 이름")]
    public string displayName;
    [Tooltip("영구 강화 종류")]
    public ShamanUpgradeType upgradeType;
    [Tooltip("1회당 스탯 증가량")]
    public float amountPerPurchase = 1f;
    [Tooltip("1회당 소울 가격")]
    [Min(0)] public int soulPrice;
    [Tooltip("세이브 파일당 최대 구매 횟수")]
    [Min(1)] public int maxPurchaseCount = 1;
    [Tooltip("스탯 표시 이미지 오브젝트")]
    public Sprite statImage;
}

public class BuffShamanShopNPC : NPCShopBase
{
    private enum MainSelection
    {
        HpPotion,
        BpmPotion,
        Stat
    }

    private static readonly ShamanUpgradeType[] StatDisplayOrder =
    {
        ShamanUpgradeType.MaxHp,
        ShamanUpgradeType.AttackDamage,
        ShamanUpgradeType.Speed,
        ShamanUpgradeType.DashCount,
        ShamanUpgradeType.DashCooldownReduction,
        ShamanUpgradeType.StartingCoin,
        ShamanUpgradeType.EquipmentRerollCount
    };

    [Header("Potions - Coin")]
    [Tooltip("HP 포션 설정")]
    [SerializeField] private BuffShamanPotionProduct hpPotion = new BuffShamanPotionProduct();
    [Tooltip("BPM 포션 설정")]
    [SerializeField] private BuffShamanPotionProduct bpmPotion = new BuffShamanPotionProduct();

    [Header("Stats - Soul")]
    [Tooltip("영구 스탯 상품 목록")]
    [SerializeField] private BuffShamanStatProduct[] stats = Array.Empty<BuffShamanStatProduct>();

    [Header("Selected Product UI")]
    [Tooltip("스탯 전용 공용 이미지")]
    [SerializeField] private Image statDisplayImage;
    [Tooltip("카드 전환 중 다음 스탯을 표시할 보조 이미지")]
    [SerializeField] private Image nextStatCard;
    [Tooltip("카드가 보이는 범위를 제한할 Viewport. 비워두면 현재 카드의 부모를 사용합니다.")]
    [SerializeField] private RectTransform statCardViewport;
    [Tooltip("카드가 좌우로 이동하는 거리")]
    [SerializeField, Min(0f)] private float statCardSlideDistance = 350f;
    [Tooltip("카드 한 번의 전환 시간")]
    [SerializeField, Min(0f)] private float statCardSlideDuration = 0.25f;
    [Tooltip("포션 미선택 텍스트 색상")]
    [SerializeField] private Color potionNormalColor = Color.white;
    [Tooltip("포션 선택 텍스트 색상")]
    [SerializeField] private Color potionSelectedColor = Color.yellow;
    [Tooltip("선택 상품 이름과 가격 텍스트")]
    [SerializeField] private TMP_Text selectedInfoText;
    [Tooltip("선택 수량 구매 가능 수량 텍스트")]
    [SerializeField] private TMP_Text selectedCountText;

    [Header("Direction Button Effect")]
    [Tooltip("위 방향키 버튼")]
    [SerializeField] private RectTransform upButton;
    [Tooltip("아래 방향키 버튼")]
    [SerializeField] private RectTransform downButton;
    [Tooltip("왼쪽 방향키 버튼")]
    [SerializeField] private RectTransform leftButton;
    [Tooltip("오른쪽 방향키 버튼")]
    [SerializeField] private RectTransform rightButton;
    [Tooltip("방향키 버튼 확대 배율")]
    [SerializeField, Min(1f)] private float buttonPressScale = 1.15f;
    [Tooltip("방향키 버튼 확대 복귀 시간")]
    [SerializeField, Min(0.01f)] private float buttonPressDuration = 0.12f;

    private MainSelection mainSelection = MainSelection.HpPotion;
    private int selectedStatIndex;
    private int purchaseCount = 1;
    private bool choosingCount;
    private Vector3 upButtonScale;
    private Vector3 downButtonScale;
    private Vector3 leftButtonScale;
    private Vector3 rightButtonScale;
    private Coroutine upButtonRoutine;
    private Coroutine downButtonRoutine;
    private Coroutine leftButtonRoutine;
    private Coroutine rightButtonRoutine;
    private Coroutine statCardRoutine;
    private Vector2 currentCardOriginalPosition;
    private Vector2 nextCardOriginalPosition;
    private bool statCardTransitioning;

    protected override CurrencyType PaymentCurrency => CurrencyType.Soul;
    protected override bool HasFirstSelection => choosingCount;

    protected override void Awake()
    {
        base.Awake();
        upButtonScale = GetButtonScale(upButton);
        downButtonScale = GetButtonScale(downButton);
        leftButtonScale = GetButtonScale(leftButton);
        rightButtonScale = GetButtonScale(rightButton);
        CacheStatCardLayout();
        ConfigureStatCardViewport();
        ResetStatCardTransition();
    }

    protected override bool CanOpenShop()
    {
        return hpPotion?.potion != null || bpmPotion?.potion != null || stats.Length > 0;
    }

    protected override void OnShopOpened()
    {
        ResetStatCardTransition();
        mainSelection = MainSelection.HpPotion;
        selectedStatIndex = 0;
        purchaseCount = 1;
        choosingCount = false;
        SelectFirstAvailableProduct();
        SetFeedback(AreAllProductsSoldOut() ? "모든 물품 구매완료!" : "마음에 드는걸 골라봐");
        RefreshUI();
    }

    protected override void OnVerticalInput(int direction)
    {
        if (choosingCount)
        {
            int maximum = GetMaximumPurchasableCount();
            if (maximum > 0 && mainSelection != MainSelection.Stat)
            {
                PlayVerticalButtonEffect(direction);
                purchaseCount = Mathf.Clamp(purchaseCount - direction, 1, maximum);
            }
        }
        else
        {
            MoveMainSelection(direction);
            purchaseCount = 1;
            SetFeedback("마음에 드는걸 골라봐");
        }

        RefreshUI();
    }

    protected override bool OnHorizontalInput(int direction)
    {
        if (mainSelection != MainSelection.Stat)
            return false;

        if (!HasAnyConfiguredStat())
            return true;

        if (statCardTransitioning)
            return true;

        PlayHorizontalButtonEffect(direction);
        int previousStatIndex = selectedStatIndex;
        Sprite previousSprite = statDisplayImage != null ? statDisplayImage.sprite : null;
        int navigationDirection = -direction;
        MoveStatSelection(navigationDirection);
        if (selectedStatIndex != previousStatIndex)
        {
            choosingCount = false;
            SetFeedback("마음에 드는걸 골라봐");
        }
        purchaseCount = GetMaximumPurchasableCount() > 0 ? 1 : 0;

        if (selectedStatIndex != previousStatIndex &&
            statDisplayImage != null && nextStatCard != null)
        {
            Sprite nextSprite = GetSelectedStat()?.statImage;
            StartStatCardTransition(previousSprite, nextSprite, navigationDirection);
        }

        RefreshUI();
        return true;
    }

    protected override void OnShopClosed()
    {
        ResetStatCardTransition();
    }

    protected override void OnAttackInput()
    {
        if (AreAllProductsSoldOut() && !HasAnyConfiguredStat())
        {
            SetFeedback("모든 물품 구매완료!");
            return;
        }

        if (!choosingCount)
        {
            int maximum = GetMaximumPurchasableCount();
            if (maximum <= 0)
            {
                SetUnavailableFeedback();
                return;
            }

            choosingCount = true;
            purchaseCount = 1;
            SetFeedback($"{GetSelectedName()}을 구매할거야?");
            RefreshUI();
            return;
        }

        if (mainSelection == MainSelection.Stat)
            PurchaseStat();
        else
            PurchasePotion(GetSelectedPotion());
    }

    protected override void CancelFirstSelection()
    {
        choosingCount = false;
        purchaseCount = 1;
        SetFeedback("마음에 드는걸 골라봐");
        RefreshUI();
    }

    private void PurchasePotion(BuffShamanPotionProduct product)
    {
        if (product == null || product.potion == null ||
            product.potion.itemType != ItemType.Potion || PlayerItemManager == null)
        {
            SetFeedback("포션 설정 오류");
            return;
        }

        if (!TryPay(CurrencyType.Coin, product.coinPrice * purchaseCount))
            return;

        for (int i = 0; i < purchaseCount; i++)
            PlayerItemManager.AddItem(product.potion);

        AddSelectedPotionPurchaseCount(purchaseCount);
        if (GetRemainingPotionPurchaseCount(product) <= 0)
            SetSelectedPurchasedForRound();
        CompletePurchase();
    }

    private void PurchaseStat()
    {
        BuffShamanStatProduct product = GetSelectedStat();
        if (product == null || product.amountPerPurchase <= 0f || PlayerController == null)
        {
            SetFeedback("스탯 설정 오류");
            return;
        }

        int remaining = GetRemainingStatPurchaseCount(product);
        if (purchaseCount <= 0 || purchaseCount > remaining)
        {
            SetFeedback("최대 구매 횟수 도달");
            return;
        }

        if (!TryPay(CurrencyType.Soul, product.soulPrice * purchaseCount))
            return;

        PlayerController.PurchasePersistentUpgrade(
            product.upgradeType,
            product.amountPerPurchase,
            purchaseCount);

        SetSelectedPurchasedForRound();
        CompletePurchase();
    }

    private void CompletePurchase()
    {
        SaveRuntimeState();
        choosingCount = false;
        purchaseCount = 1;
        if (!IsMainSelectionAvailable(mainSelection))
            MoveToNextAvailableProduct();
        SetFeedback(AreAllProductsSoldOut() ? "모든 물품 구매완료!" : "마음에 드는걸 골라봐");
        RefreshUI();
    }

    private void RefreshUI()
    {
        RefreshImageObjects();
        RefreshMainSelectionUI();

        if (AreAllProductsSoldOut())
        {
            if (selectedInfoText != null) selectedInfoText.text = string.Empty;
            if (selectedCountText != null) selectedCountText.text = string.Empty;
            SetDirectionButtonsActive(false);
            return;
        }

        CurrencyType currencyType = GetSelectedCurrencyType();
        string currencyName = currencyType == CurrencyType.Coin ? "COIN" : "SOUL";
        if (selectedInfoText != null)
            selectedInfoText.text = $"{GetSelectedName()}\n{currencyName} : {GetSelectedUnitPrice() * Mathf.Max(1, purchaseCount)}";

        int maximum = GetMaximumPurchasableCount();
        if (selectedCountText != null)
            selectedCountText.text = mainSelection == MainSelection.Stat && maximum > 0
                ? "1/1"
                : maximum > 0
                ? $"{Mathf.Clamp(purchaseCount, 1, maximum)}/{maximum}"
                : "0/0";
    }

    private BuffShamanPotionProduct GetSelectedPotion()
    {
        return mainSelection == MainSelection.HpPotion ? hpPotion : bpmPotion;
    }

    private BuffShamanStatProduct GetSelectedStat()
    {
        ShamanUpgradeType targetType = StatDisplayOrder[selectedStatIndex];
        return Array.Find(stats, product => product != null && product.upgradeType == targetType);
    }

    private string GetSelectedName()
    {
        if (mainSelection == MainSelection.HpPotion)
            return GetPotionName(hpPotion, "HP 포션");
        if (mainSelection == MainSelection.BpmPotion)
            return GetPotionName(bpmPotion, "BPM 포션");

        BuffShamanStatProduct stat = GetSelectedStat();
        if (stat != null && !string.IsNullOrWhiteSpace(stat.displayName))
            return stat.displayName;

        return GetDefaultStatName(StatDisplayOrder[selectedStatIndex]);
    }

    private CurrencyType GetSelectedCurrencyType()
    {
        return mainSelection == MainSelection.Stat ? CurrencyType.Soul : CurrencyType.Coin;
    }

    private int GetSelectedUnitPrice()
    {
        if (mainSelection != MainSelection.Stat)
            return GetSelectedPotion()?.coinPrice ?? 0;
        return GetSelectedStat()?.soulPrice ?? 0;
    }

    private int GetMaximumPurchasableCount()
    {
        if (IsSelectedPurchasedForRound())
            return 0;

        int unitPrice = GetSelectedUnitPrice();
        CurrencyType currencyType = GetSelectedCurrencyType();
        int ownedCurrency = Currency == null
            ? 0
            : currencyType == CurrencyType.Coin ? Currency.coin : Currency.soul;
        int affordableCount = unitPrice <= 0 ? int.MaxValue : ownedCurrency / unitPrice;

        int productLimit;
        if (mainSelection == MainSelection.Stat)
        {
            BuffShamanStatProduct stat = GetSelectedStat();
            if (stat == null || stat.amountPerPurchase <= 0f)
                return 0;
            productLimit = GetRemainingStatPurchaseCount(stat) > 0 ? 1 : 0;
        }
        else
        {
            BuffShamanPotionProduct potion = GetSelectedPotion();
            productLimit = GetRemainingPotionPurchaseCount(potion);
        }

        return Mathf.Max(0, Mathf.Min(productLimit, affordableCount));
    }

    private int GetRemainingStatPurchaseCount(BuffShamanStatProduct product)
    {
        if (product == null)
            return 0;

        int purchased = PlayerController != null
            ? PlayerController.GetPersistentUpgradePurchaseCount(product.upgradeType)
            : 0;
        return Mathf.Max(0, product.maxPurchaseCount - purchased);
    }

    private int GetRemainingPotionPurchaseCount(BuffShamanPotionProduct product)
    {
        if (product == null)
            return 0;

        return Mathf.Max(
            0,
            Mathf.Max(1, product.maxPurchaseCount) - GetSelectedPotionPurchaseCount());
    }

    private int GetSelectedPotionPurchaseCount()
    {
        if (SaveData.Instance == null)
            return 0;

        return mainSelection == MainSelection.HpPotion
            ? SaveData.Instance.run.shamanHpPotionPurchasedCount
            : SaveData.Instance.run.shamanBpmPotionPurchasedCount;
    }

    private void AddSelectedPotionPurchaseCount(int amount)
    {
        if (SaveData.Instance == null || amount <= 0)
            return;

        if (mainSelection == MainSelection.HpPotion)
            SaveData.Instance.run.shamanHpPotionPurchasedCount += amount;
        else if (mainSelection == MainSelection.BpmPotion)
            SaveData.Instance.run.shamanBpmPotionPurchasedCount += amount;
    }

    private void SetUnavailableFeedback()
    {
        if (IsSelectedPurchasedForRound())
            SetFeedback("이번 회차 구매 완료");
        else if (mainSelection == MainSelection.Stat && GetSelectedStat() == null)
            SetFeedback("스탯 설정 오류");
        else if (mainSelection == MainSelection.Stat && GetRemainingStatPurchaseCount(GetSelectedStat()) <= 0)
            SetFeedback("최대 구매 횟수 도달");
        else
            SetFeedback(GetSelectedCurrencyType() == CurrencyType.Coin ? "코인 부족" : "소울 부족");
    }

    private void RefreshImageObjects()
    {
        if (statCardTransitioning)
            return;

        BuffShamanStatProduct stat = GetSelectedStat();
        if (statDisplayImage == null)
            return;

        bool hasStatImage = stat != null && stat.statImage != null;
        statDisplayImage.gameObject.SetActive(hasStatImage);
        statDisplayImage.sprite = stat != null ? stat.statImage : null;
        statDisplayImage.preserveAspect = true;
        statDisplayImage.enabled = hasStatImage;
    }

    private void RefreshMainSelectionUI()
    {
        bool allSoldOut = AreAllProductsSoldOut();
        bool hasNavigableProduct = !allSoldOut || HasAnyConfiguredStat();
        if (upButton != null)
            upButton.gameObject.SetActive(hasNavigableProduct);
        if (downButton != null)
            downButton.gameObject.SetActive(hasNavigableProduct);

        bool hpAvailable = IsMainSelectionAvailable(MainSelection.HpPotion);
        bool bpmAvailable = IsMainSelectionAvailable(MainSelection.BpmPotion);
        SetPotionObjectActive(hpPotion?.potionObject, hpPotion?.primarySelectionText, hpAvailable);
        SetPotionObjectActive(bpmPotion?.potionObject, bpmPotion?.primarySelectionText, bpmAvailable);
        RefreshPotionTextColors(hpPotion, mainSelection == MainSelection.HpPotion);
        RefreshPotionTextColors(bpmPotion, mainSelection == MainSelection.BpmPotion);

        bool showStatDirectionButtons = mainSelection == MainSelection.Stat &&
            HasAnyConfiguredStat();
        if (leftButton != null)
            leftButton.gameObject.SetActive(showStatDirectionButtons);
        if (rightButton != null)
            rightButton.gameObject.SetActive(showStatDirectionButtons);
    }

    private void SelectFirstAvailableProduct()
    {
        for (int i = 0; i < 3; i++)
        {
            MainSelection candidate = (MainSelection)i;
            if (!IsMainSelectionAvailable(candidate))
                continue;

            mainSelection = candidate;
            if (candidate == MainSelection.Stat)
                MoveStatSelection(0);
            return;
        }

        if (HasAnyConfiguredStat())
        {
            mainSelection = MainSelection.Stat;
            MoveStatSelection(0);
        }
    }

    private void MoveMainSelection(int direction)
    {
        for (int offset = 1; offset <= 3; offset++)
        {
            MainSelection candidate = (MainSelection)WrapIndex(
                (int)mainSelection + direction * offset, 3);
            if (!IsMainSelectionNavigable(candidate))
                continue;

            mainSelection = candidate;
            if (candidate == MainSelection.Stat &&
                !IsStatConfigured(StatDisplayOrder[selectedStatIndex]))
                MoveStatSelection(direction);
            return;
        }
    }

    private void MoveStatSelection(int direction)
    {
        int step = direction == 0 ? 1 : direction;
        int startOffset = direction == 0 ? 0 : 1;
        for (int offset = startOffset; offset < StatDisplayOrder.Length + startOffset; offset++)
        {
            int candidate = WrapIndex(selectedStatIndex + step * offset, StatDisplayOrder.Length);
            if (!IsStatConfigured(StatDisplayOrder[candidate]))
                continue;

            selectedStatIndex = candidate;
            return;
        }
    }

    private void MoveToNextAvailableProduct()
    {
        if (AreAllProductsSoldOut())
            return;

        if (mainSelection == MainSelection.Stat && IsMainSelectionAvailable(MainSelection.Stat))
        {
            MoveStatSelection(1);
            return;
        }

        MoveMainSelection(1);
    }

    private bool IsMainSelectionAvailable(MainSelection selection)
    {
        if (selection == MainSelection.HpPotion)
            return hpPotion?.potion != null && !IsProductPurchasedForRound(0);
        if (selection == MainSelection.BpmPotion)
            return bpmPotion?.potion != null && !IsProductPurchasedForRound(1);

        for (int i = 0; i < StatDisplayOrder.Length; i++)
        {
            if (IsStatAvailable(StatDisplayOrder[i]))
                return true;
        }
        return false;
    }

    private bool IsMainSelectionNavigable(MainSelection selection)
    {
        if (selection == MainSelection.Stat)
            return HasAnyConfiguredStat();

        return IsMainSelectionAvailable(selection);
    }

    private bool HasAnyConfiguredStat()
    {
        for (int i = 0; i < StatDisplayOrder.Length; i++)
        {
            if (IsStatConfigured(StatDisplayOrder[i]))
                return true;
        }

        return false;
    }

    private bool IsStatConfigured(ShamanUpgradeType type)
    {
        BuffShamanStatProduct stat = Array.Find(
            stats,
            product => product != null && product.upgradeType == type);
        return stat != null && stat.amountPerPurchase > 0f;
    }

    private bool IsStatAvailable(ShamanUpgradeType type)
    {
        BuffShamanStatProduct stat = Array.Find(
            stats, product => product != null && product.upgradeType == type);
        return stat != null &&
            stat.amountPerPurchase > 0f &&
            GetRemainingStatPurchaseCount(stat) > 0 &&
            !IsProductPurchasedForRound(GetStatProductId(type));
    }

    private bool AreAllProductsSoldOut()
    {
        return !IsMainSelectionAvailable(MainSelection.HpPotion) &&
            !IsMainSelectionAvailable(MainSelection.BpmPotion) &&
            !IsMainSelectionAvailable(MainSelection.Stat);
    }

    private static void SetPotionObjectActive(
        GameObject productObject,
        TMP_Text fallbackText,
        bool active)
    {
        GameObject target = productObject != null
            ? productObject
            : fallbackText != null ? fallbackText.gameObject : null;
        if (target != null)
            target.SetActive(active);
    }

    private string GetPotionName(BuffShamanPotionProduct product, string fallbackName)
    {
        if (product != null && !string.IsNullOrWhiteSpace(product.displayName))
            return product.displayName;

        return fallbackName;
    }

    private void RefreshPotionTextColors(BuffShamanPotionProduct product, bool isPointed)
    {
        if (product == null)
            return;

        if (product.primarySelectionText != null)
        {
            product.primarySelectionText.text =
                $"{GetPotionName(product, "포션")}\nCOIN : {product.coinPrice}";
            product.primarySelectionText.color = isPointed && !choosingCount
                ? potionSelectedColor
                : potionNormalColor;
        }

        if (product.secondarySelectionText != null)
        {
            product.secondarySelectionText.color = isPointed && choosingCount
                ? potionSelectedColor
                : potionNormalColor;
        }
    }

    private void SetDirectionButtonsActive(bool active)
    {
        if (upButton != null) upButton.gameObject.SetActive(active);
        if (downButton != null) downButton.gameObject.SetActive(active);
        if (leftButton != null) leftButton.gameObject.SetActive(active);
        if (rightButton != null) rightButton.gameObject.SetActive(active);
    }

    private bool IsSelectedPurchasedForRound()
    {
        return IsProductPurchasedForRound(GetSelectedProductId());
    }

    private bool IsProductPurchasedForRound(int productId)
    {
        return SaveData.Instance != null &&
            SaveData.Instance.run.shamanPurchasedProductIds != null &&
            SaveData.Instance.run.shamanPurchasedProductIds.Contains(productId);
    }

    private void SetSelectedPurchasedForRound()
    {
        if (SaveData.Instance == null)
            return;

        SaveData.Instance.run.shamanPurchasedProductIds ??= new System.Collections.Generic.List<int>();
        int productId = GetSelectedProductId();
        if (!SaveData.Instance.run.shamanPurchasedProductIds.Contains(productId))
            SaveData.Instance.run.shamanPurchasedProductIds.Add(productId);
    }

    private int GetSelectedProductId()
    {
        if (mainSelection == MainSelection.HpPotion)
            return 0;
        if (mainSelection == MainSelection.BpmPotion)
            return 1;
        return GetStatProductId(StatDisplayOrder[selectedStatIndex]);
    }

    private static int GetStatProductId(ShamanUpgradeType type)
    {
        return 100 + (int)type;
    }

    private void CacheStatCardLayout()
    {
        if (statDisplayImage != null)
        {
            RectTransform currentRect = statDisplayImage.rectTransform;
            currentCardOriginalPosition = currentRect.anchoredPosition;
        }

        if (nextStatCard != null)
        {
            RectTransform nextRect = nextStatCard.rectTransform;
            nextCardOriginalPosition = nextRect.anchoredPosition;
        }
    }

    private void ConfigureStatCardViewport()
    {
        if (statCardViewport == null && statDisplayImage != null)
            statCardViewport = statDisplayImage.rectTransform.parent as RectTransform;

        if (statCardViewport != null &&
            statCardViewport.GetComponent<RectMask2D>() == null)
            statCardViewport.gameObject.AddComponent<RectMask2D>();
    }

    private void StartStatCardTransition(
        Sprite previousSprite,
        Sprite nextSprite,
        int navigationDirection)
    {
        ResetStatCardTransition();

        statCardTransitioning = true;
        float direction = navigationDirection >= 0 ? 1f : -1f;
        RectTransform currentRect = statDisplayImage.rectTransform;
        RectTransform nextRect = nextStatCard.rectTransform;

        statDisplayImage.sprite = previousSprite;
        statDisplayImage.preserveAspect = true;
        statDisplayImage.gameObject.SetActive(previousSprite != null);
        statDisplayImage.enabled = previousSprite != null;
        SetCardOpaque(statDisplayImage);
        currentRect.anchoredPosition = currentCardOriginalPosition;

        nextStatCard.sprite = nextSprite;
        nextStatCard.preserveAspect = true;
        nextStatCard.gameObject.SetActive(nextSprite != null);
        nextStatCard.enabled = nextSprite != null;
        SetCardOpaque(nextStatCard);
        nextRect.anchoredPosition = nextCardOriginalPosition +
            Vector2.right * statCardSlideDistance * direction;

        if (statCardSlideDuration <= 0f)
        {
            CompleteStatCardTransition(nextSprite);
            return;
        }

        statCardRoutine = StartCoroutine(StatCardTransitionRoutine(nextSprite, direction));
    }

    private IEnumerator StatCardTransitionRoutine(Sprite nextSprite, float direction)
    {
        RectTransform currentRect = statDisplayImage.rectTransform;
        RectTransform nextRect = nextStatCard.rectTransform;
        Vector2 currentTarget = currentCardOriginalPosition +
            Vector2.left * statCardSlideDistance * direction;
        Vector2 nextStart = nextCardOriginalPosition +
            Vector2.right * statCardSlideDistance * direction;
        float elapsed = 0f;

        while (elapsed < statCardSlideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / statCardSlideDuration));

            currentRect.anchoredPosition = Vector2.LerpUnclamped(
                currentCardOriginalPosition,
                currentTarget,
                progress);
            nextRect.anchoredPosition = Vector2.LerpUnclamped(
                nextStart,
                nextCardOriginalPosition,
                progress);
            yield return null;
        }

        CompleteStatCardTransition(nextSprite);
    }

    private void CompleteStatCardTransition(Sprite nextSprite)
    {
        statDisplayImage.sprite = nextSprite;
        ResetStatCardTransition();
        RefreshUI();
    }

    private void ResetStatCardTransition()
    {
        if (statCardRoutine != null)
        {
            StopCoroutine(statCardRoutine);
            statCardRoutine = null;
        }

        statCardTransitioning = false;

        if (statDisplayImage != null)
        {
            RectTransform currentRect = statDisplayImage.rectTransform;
            currentRect.anchoredPosition = currentCardOriginalPosition;
        }

        if (nextStatCard != null)
        {
            RectTransform nextRect = nextStatCard.rectTransform;
            nextRect.anchoredPosition = nextCardOriginalPosition;
            nextStatCard.gameObject.SetActive(false);
        }
    }

    private static void SetCardOpaque(Image image)
    {
        if (image == null)
            return;

        Color color = image.color;
        color.a = 1f;
        image.color = color;
    }

    private static Vector3 GetButtonScale(RectTransform button)
    {
        return button != null ? button.localScale : Vector3.one;
    }

    private void PlayVerticalButtonEffect(int direction)
    {
        if (direction < 0)
            RestartButtonEffect(upButton, upButtonScale, ref upButtonRoutine);
        else
            RestartButtonEffect(downButton, downButtonScale, ref downButtonRoutine);
    }

    private void PlayHorizontalButtonEffect(int direction)
    {
        if (direction < 0)
            RestartButtonEffect(rightButton, rightButtonScale, ref rightButtonRoutine);
        else
            RestartButtonEffect(leftButton, leftButtonScale, ref leftButtonRoutine);
    }

    private void RestartButtonEffect(
        RectTransform button,
        Vector3 originalScale,
        ref Coroutine routine)
    {
        if (button == null)
            return;

        if (routine != null)
            StopCoroutine(routine);
        button.localScale = originalScale;
        routine = StartCoroutine(ButtonPressRoutine(button, originalScale));
    }

    private System.Collections.IEnumerator ButtonPressRoutine(
        RectTransform button,
        Vector3 originalScale)
    {
        float halfDuration = buttonPressDuration * 0.5f;
        Vector3 pressedScale = originalScale * buttonPressScale;
        float time = 0f;

        while (time < halfDuration)
        {
            time += Time.unscaledDeltaTime;
            button.localScale = Vector3.Lerp(originalScale, pressedScale, time / halfDuration);
            yield return null;
        }

        time = 0f;
        while (time < halfDuration)
        {
            time += Time.unscaledDeltaTime;
            button.localScale = Vector3.Lerp(pressedScale, originalScale, time / halfDuration);
            yield return null;
        }

        button.localScale = originalScale;
    }

    private static string GetDefaultStatName(ShamanUpgradeType type)
    {
        return type switch
        {
            ShamanUpgradeType.MaxHp => "최대체력",
            ShamanUpgradeType.AttackDamage => "공격력",
            ShamanUpgradeType.Speed => "속도",
            ShamanUpgradeType.DashCount => "대쉬횟수",
            ShamanUpgradeType.DashCooldownReduction => "대쉬쿨타임",
            ShamanUpgradeType.StartingCoin => "시작코인",
            ShamanUpgradeType.EquipmentRerollCount => "장비리롤",
            _ => type.ToString()
        };
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        buttonPressScale = Mathf.Max(1f, buttonPressScale);
        buttonPressDuration = Mathf.Max(0.01f, buttonPressDuration);
    }
#endif
}
