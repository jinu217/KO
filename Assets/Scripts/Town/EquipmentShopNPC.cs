using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class EquipmentShopSlotUI
{
    [Tooltip("장비 부위")]
    public EquipmentType equipmentType;
    [Tooltip("장비 아이콘 이미지")]
    public Image iconImage;
    [Tooltip("장비 이름 능력치 가격 텍스트")]
    public TMP_Text itemText;
    [Tooltip("구매 표시 배경 이미지")]
    public Image purchaseBackground;
    [Tooltip("구매 표시 텍스트")]
    public TMP_Text purchaseText;
}

public class EquipmentShopNPC : NPCShopBase
{
    [Header("Item Database")]
    [Tooltip("전체 아이템 ItemDatabase")]
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Equipment Slots")]
    [Tooltip("Head Body Hand Foot 슬롯 UI")]
    [SerializeField] private EquipmentShopSlotUI[] slots = Array.Empty<EquipmentShopSlotUI>();

    [Header("Price By Rarity")]
    [Tooltip("노말 장비 코인 가격")]
    [SerializeField, Min(0)] private int normalPrice = 50;
    [Tooltip("레어 장비 코인 가격")]
    [SerializeField, Min(0)] private int rarePrice = 100;
    [Tooltip("에픽 장비 코인 가격")]
    [SerializeField, Min(0)] private int epicPrice = 200;
    [Tooltip("레전더리 장비 코인 가격")]
    [SerializeField, Min(0)] private int legendaryPrice = 400;

    [Header("Rarity Chance")]
    [Tooltip("노말 장비 기본 등장 확률")]
    [SerializeField] private float normalChance = 70f;
    [Tooltip("레어 장비 기본 등장 확률")]
    [SerializeField] private float rareChance = 20f;
    [Tooltip("에픽 장비 기본 등장 확률")]
    [SerializeField] private float epicChance = 9f;
    [Tooltip("레전더리 장비 기본 등장 확률")]
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

    [Header("Selection Color")]
    [Tooltip("미선택 장비 색상")]
    [SerializeField] private Color normalColor = Color.white;
    [Tooltip("선택 장비 색상")]
    [SerializeField] private Color selectedColor = Color.yellow;

    [Header("Reroll Button")]
    [Tooltip("장비 리롤 버튼 이미지")]
    [SerializeField] private Image rerollButtonImage;
    [Tooltip("남은 장비 리롤 횟수 텍스트")]
    [SerializeField] private TMP_Text rerollCountText;

    private readonly Dictionary<EquipmentType, ItemData> offers = new Dictionary<EquipmentType, ItemData>();
    private int selectedIndex;
    private bool confirming;

    private int RerollIndex => slots.Length;
    private bool IsRerollSelected => selectedIndex == RerollIndex;
    private int MaximumRerollCount => 1 + (SaveData.Instance != null
        ? Mathf.Max(0, SaveData.Instance.persistent.equipmentRerollBonus)
        : 0);
    private int RemainingRerollCount => Mathf.Max(0, MaximumRerollCount -
        (SaveData.Instance != null ? SaveData.Instance.run.equipmentRerollUsedCount : 0));

    protected override CurrencyType PaymentCurrency => CurrencyType.Coin;
    protected override bool HasFirstSelection => confirming;

    private void Start()
    {
        EnsureRoundOffers();
        confirming = false;
        selectedIndex = FindNextAvailableIndex(0, 1);
        RefreshUI();
    }

    protected override bool CanOpenShop()
    {
        if (itemDatabase != null && slots.Length > 0)
            return true;
        Debug.LogWarning("장비 목록 또는 슬롯 UI 설정 필요", this);
        return false;
    }

    protected override void OnShopOpened()
    {
        // 씬 시작 시 생성된 진열 상태를 불러오기만 한다.
        EnsureRoundOffers();
        confirming = false;
        selectedIndex = FindNextAvailableIndex(0, 1);
        RefreshUI();
    }

    protected override void OnVerticalInput(int direction)
    {
        if (confirming)
            return;
        selectedIndex = FindNextAvailableIndex(selectedIndex + direction, direction);
        RefreshUI();
    }

    protected override void OnAttackInput()
    {
        if (IsRerollSelected)
        {
            RerollEquipment();
            return;
        }

        if (slots.Length == 0 || selectedIndex < 0 || selectedIndex >= slots.Length ||
            IsSold(slots[selectedIndex].equipmentType))
        {
            SetFeedback("구매 가능한 장비 없음");
            return;
        }

        if (!confirming)
        {
            confirming = true;
            SetFeedback(string.Empty);
            RefreshUI();
            return;
        }

        EquipmentType type = slots[selectedIndex].equipmentType;
        ItemData item = GetOffer(type);
        if (item == null || PlayerItemManager == null)
        {
            SetFeedback("장비 설정 오류");
            return;
        }

        if (!TryPay(GetPrice(item.rarity)))
            return;

        PlayerItemManager.AddItem(item);
        SetSold(type, true);
        confirming = false;
        SaveRuntimeState();
        SetFeedback($"{item.itemName} 구매 완료");
        selectedIndex = FindNextAvailableIndex(selectedIndex, 1);
        RefreshUI();
    }

    protected override void CancelFirstSelection()
    {
        confirming = false;
        SetFeedback(string.Empty);
        RefreshUI();
    }

    private void EnsureRoundOffers()
    {
        if (SaveData.Instance == null)
            return;

        RunData run = SaveData.Instance.run;
        int round = Mathf.Max(0, SaveData.Instance.persistent.deathCount);
        bool newRound = run.equipmentShopRound != round;
        bool needsInitialOffers = !run.equipmentOffersInitialized;
        if (newRound || needsInitialOffers)
        {
            run.equipmentShopRound = round;
            run.equipmentOffersInitialized = true;
            run.equipmentRerollUsedCount = 0;
            GenerateOffers(run);
        }

        offers.Clear();
        offers[EquipmentType.Head] = FindPoolItem(run.offeredHeadItemId);
        offers[EquipmentType.Body] = FindPoolItem(run.offeredBodyItemId);
        offers[EquipmentType.Hand] = FindPoolItem(run.offeredHandItemId);
        offers[EquipmentType.Foot] = FindPoolItem(run.offeredFootItemId);
    }

    private void GenerateOffers(RunData run)
    {
        run.offeredHeadItemId = RollEquipment(EquipmentType.Head)?.id ?? -1;
        run.offeredBodyItemId = RollEquipment(EquipmentType.Body)?.id ?? -1;
        run.offeredHandItemId = RollEquipment(EquipmentType.Hand)?.id ?? -1;
        run.offeredFootItemId = RollEquipment(EquipmentType.Foot)?.id ?? -1;
        run.offeredHeadSold = false;
        run.offeredBodySold = false;
        run.offeredHandSold = false;
        run.offeredFootSold = false;
    }

    private void RerollEquipment()
    {
        if (SaveData.Instance == null || RemainingRerollCount <= 0)
        {
            SetFeedback("남은 리롤 횟수 없음");
            return;
        }

        RunData run = SaveData.Instance.run;
        run.equipmentRerollUsedCount++;
        GenerateOffers(run);
        EnsureRoundOffers();
        confirming = false;
        selectedIndex = FindNextAvailableIndex(0, 1);
        SaveRuntimeState();
        SetFeedback("장비 리롤 완료");
        RefreshUI();
    }

    private ItemData RollEquipment(EquipmentType type)
    {
        List<ItemData> valid = new List<ItemData>();
        if (itemDatabase != null)
        {
            foreach (ItemData item in itemDatabase.Items)
            {
                if (item != null && item.itemType == ItemType.Equipment &&
                    item.equipType == type && type != EquipmentType.Weapon &&
                    item.rarity != ItemRarity.None)
                    valid.Add(item);
            }
        }
        if (valid.Count == 0)
            return null;

        ItemRarity rarity = RollRarity();
        List<ItemData> rarityItems = valid.FindAll(item => item.rarity == rarity);
        if (rarityItems.Count == 0)
            rarityItems = valid;
        return rarityItems[UnityEngine.Random.Range(0, rarityItems.Count)];
    }

    private ItemData FindPoolItem(int id)
    {
        if (itemDatabase == null)
            return null;

        ItemData item = itemDatabase.GetById(id);
        if (item == null || item.itemType != ItemType.Equipment ||
            item.equipType == EquipmentType.Weapon || item.rarity == ItemRarity.None)
            return null;

        return item;
    }

    private ItemData GetOffer(EquipmentType type)
    {
        offers.TryGetValue(type, out ItemData item);
        return item;
    }

    private int FindNextAvailableIndex(int start, int direction)
    {
        int selectionCount = slots.Length + 1;
        int index = WrapIndex(start, selectionCount);
        for (int i = 0; i < selectionCount; i++)
        {
            if (index == RerollIndex)
                return index;

            if (!IsSold(slots[index].equipmentType) && GetOffer(slots[index].equipmentType) != null)
                return index;
            index = WrapIndex(index + direction, selectionCount);
        }
        return RerollIndex;
    }

    private void RefreshUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            EquipmentShopSlotUI slot = slots[i];
            if (slot == null)
                continue;
            ItemData item = GetOffer(slot.equipmentType);
            bool sold = IsSold(slot.equipmentType);
            bool selected = !IsRerollSelected && i == selectedIndex && !sold;

            if (slot.iconImage != null)
            {
                slot.iconImage.sprite = item != null ? item.icon : null;
                slot.iconImage.enabled = item != null && item.icon != null;
                slot.iconImage.preserveAspect = true;
                slot.iconImage.color = selected ? selectedColor : normalColor;
            }
            if (slot.itemText != null)
                slot.itemText.text = sold ? "판매 완료" : item != null ? BuildDescription(item) : "상품 없음";

            bool showPurchase = confirming && selected;
            if (slot.purchaseBackground != null)
                slot.purchaseBackground.gameObject.SetActive(showPurchase);
            if (slot.purchaseText != null)
            {
                slot.purchaseText.gameObject.SetActive(showPurchase);
                slot.purchaseText.text = "구매";
            }
        }


        if (rerollButtonImage != null)
            rerollButtonImage.color = IsRerollSelected ? selectedColor : normalColor;

        if (rerollCountText != null)
            rerollCountText.text = $"리롤 {RemainingRerollCount}/{MaximumRerollCount}";
    }

    private string BuildDescription(ItemData item)
    {
        StringBuilder text = new StringBuilder($"{item.itemName} [{item.rarity}] - {GetPrice(item.rarity)} Coin");
        if (item.hp != 0) text.Append($"\n체력 +{item.hp}");
        if (item.defense != 0) text.Append($"\n방어력 +{item.defense}");
        if (item.attack != 0) text.Append($"\n공격력 +{item.attack}");
        if (item.attackSpeed != 0f) text.Append($"\n공격속도 +{item.attackSpeed:0.##}");
        if (item.moveSpeed != 0f) text.Append($"\n이동속도 +{item.moveSpeed:0.##}");
        return text.ToString();
    }

    private int GetPrice(ItemRarity rarity)
    {
        return rarity switch
        {
            ItemRarity.Normal => normalPrice,
            ItemRarity.Rare => rarePrice,
            ItemRarity.Epic => epicPrice,
            ItemRarity.Legendary => legendaryPrice,
            _ => normalPrice
        };
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

    private bool IsSold(EquipmentType type)
    {
        if (SaveData.Instance == null) return false;
        RunData run = SaveData.Instance.run;
        return type switch
        {
            EquipmentType.Head => run.offeredHeadSold,
            EquipmentType.Body => run.offeredBodySold,
            EquipmentType.Hand => run.offeredHandSold,
            EquipmentType.Foot => run.offeredFootSold,
            _ => true
        };
    }

    private void SetSold(EquipmentType type, bool sold)
    {
        RunData run = SaveData.Instance.run;
        switch (type)
        {
            case EquipmentType.Head: run.offeredHeadSold = sold; break;
            case EquipmentType.Body: run.offeredBodySold = sold; break;
            case EquipmentType.Hand: run.offeredHandSold = sold; break;
            case EquipmentType.Foot: run.offeredFootSold = sold; break;
        }
    }
}
