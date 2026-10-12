using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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
    [Tooltip("구매 버튼이 눌렸을 때 커질 배율")]
    [SerializeField, Min(1f)] private float buttonPopScale = 1.12f;
    [Tooltip("구매 버튼이 커지고 다시 돌아오는 각 구간의 시간")]
    [SerializeField, Min(0f)] private float buttonPopDuration = 0.08f;

    [Header("Hammer Animation")]
    [Tooltip("구매 연출에 사용할 망치 이미지")]
    [SerializeField] private Image hammerImage;
    [Tooltip("망치 회전 시작 Local Rotation Z")]
    [FormerlySerializedAs("hammerStartZ")]
    [SerializeField] private float hammerStartRotationZ = -65f;
    [Tooltip("망치 회전 종료 Local Rotation Z")]
    [FormerlySerializedAs("hammerEndZ")]
    [SerializeField] private float hammerEndRotationZ;
    [Tooltip("망치가 시작 회전값에서 종료 회전값까지 내려치는 시간")]
    [SerializeField, Min(0f)] private float hammerStrikeDuration = 0.25f;
    [Tooltip("망치가 종료 회전값에서 시작 회전값으로 돌아오는 시간")]
    [FormerlySerializedAs("hammerMoveDuration")]
    [SerializeField, Min(0f)] private float hammerReturnDuration = 0.5f;
    [Tooltip("마지막 내려치기 후 타격 위치에서 멈추는 시간")]
    [SerializeField, Min(0f)] private float hammerFinalImpactPause = 0.1f;
    [Tooltip("마지막 타격 후 올라오며 사라지는 시간")]
    [SerializeField, Min(0f)] private float hammerFinalReturnDuration = 0.75f;
    [Tooltip("타격 위치를 기준으로 망치가 시작할 위치 오프셋")]
    [SerializeField] private Vector2 hammerRaisedOffset = new Vector2(-45f, 35f);
    [Tooltip("망치가 그리는 포물선의 높이")]
    [SerializeField, Min(0f)] private float hammerArcHeight = 20f;
    [Tooltip("망치 내려치기와 복귀 반복 횟수")]
    [SerializeField, Min(1)] private int hammerRepeatCount = 3;

    [Header("Result UI")]
    [Tooltip("무기가 나왔을 때만 활성화할 구매 결과 전체 오브젝트")]
    [SerializeField] private GameObject resultRoot;
    [Tooltip("획득한 무기 아이콘 이미지")]
    [SerializeField] private Image resultWeaponImage;
    [Tooltip("획득한 무기의 능력치만 표시할 텍스트")]
    [SerializeField] private TMP_Text weaponText;

    private Vector3 acquireButtonOriginalScale = Vector3.one;
    private Coroutine buttonPopRoutine;
    private Coroutine hammerRoutine;
    private Vector2 hammerImpactPosition;
    private bool hammerPositionCached;
    private Color hammerOriginalColor = Color.white;

    protected override CurrencyType PaymentCurrency => CurrencyType.Coin;
    protected override bool HasFirstSelection => false;

    protected override void Awake()
    {
        base.Awake();
        CacheAcquireButtonScale();
        CacheHammerPosition();
        ResetHammer();
        SetResult(null);
    }

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
            acquireButtonImage.color = normalColor;
        RefreshPrice();
        SetResult(null);
    }

    protected override void OnShopClosed()
    {
        if (buttonPopRoutine != null)
        {
            StopCoroutine(buttonPopRoutine);
            buttonPopRoutine = null;
        }

        if (acquireButtonImage != null)
            acquireButtonImage.rectTransform.localScale = acquireButtonOriginalScale;

        if (hammerRoutine != null)
        {
            StopCoroutine(hammerRoutine);
            hammerRoutine = null;
        }

        ResetHammer();
    }

    protected override void OnVerticalInput(int direction) { }

    protected override void OnAttackInput()
    {
        if (hammerRoutine != null)
            return;

        PlayAcquireButtonPop();

        if (PlayerItemManager != null && !PlayerItemManager.HasInventorySpace())
        {
            ShowInventoryFullFeedback();
            return;
        }

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
        SetResult(null);
        RefreshPrice();
        SaveRuntimeState();
        SetFeedback(string.Empty);
        PlayHammerResult(weapon);
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
                item.equipType == EquipmentType.Weapon &&
                item.rarity != ItemRarity.None && item.rarity != ItemRarity.Mechanization)
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

    private void CacheAcquireButtonScale()
    {
        if (acquireButtonImage == null)
            return;

        Vector3 currentScale = acquireButtonImage.rectTransform.localScale;
        if (currentScale.sqrMagnitude > 0f)
            acquireButtonOriginalScale = currentScale;
    }

    private void PlayAcquireButtonPop()
    {
        if (acquireButtonImage == null)
            return;

        if (buttonPopRoutine != null)
            StopCoroutine(buttonPopRoutine);
        buttonPopRoutine = StartCoroutine(AcquireButtonPopRoutine());
    }

    private IEnumerator AcquireButtonPopRoutine()
    {
        RectTransform buttonRect = acquireButtonImage.rectTransform;
        Vector3 poppedScale = acquireButtonOriginalScale * buttonPopScale;

        yield return AnimateButtonScale(buttonRect, buttonRect.localScale, poppedScale);
        yield return AnimateButtonScale(buttonRect, poppedScale, acquireButtonOriginalScale);

        buttonRect.localScale = acquireButtonOriginalScale;
        buttonPopRoutine = null;
    }

    private IEnumerator AnimateButtonScale(
        RectTransform buttonRect,
        Vector3 startScale,
        Vector3 targetScale)
    {
        if (buttonPopDuration <= 0f)
        {
            buttonRect.localScale = targetScale;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < buttonPopDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / buttonPopDuration));
            buttonRect.localScale = Vector3.LerpUnclamped(startScale, targetScale, progress);
            yield return null;
        }

        buttonRect.localScale = targetScale;
    }

    private void PlayHammerResult(ItemData weapon)
    {
        if (hammerImage == null)
        {
            ShowPurchaseResult(weapon);
            return;
        }

        hammerRoutine = StartCoroutine(HammerResultRoutine(weapon));
    }

    private IEnumerator HammerResultRoutine(ItemData weapon)
    {
        hammerImage.gameObject.SetActive(true);

        int repeatCount = Mathf.Max(1, hammerRepeatCount);
        Vector2 raisedPosition = hammerImpactPosition + hammerRaisedOffset;
        for (int i = 0; i < repeatCount; i++)
        {
            SetHammerAlpha(1f);
            SetHammerPose(hammerStartRotationZ, raisedPosition);
            yield return AnimateHammerPose(
                hammerStartRotationZ,
                hammerEndRotationZ,
                raisedPosition,
                hammerImpactPosition,
                hammerStrikeDuration,
                false);

            bool isFinalStrike = i == repeatCount - 1;
            if (isFinalStrike && hammerFinalImpactPause > 0f)
                yield return new WaitForSecondsRealtime(hammerFinalImpactPause);

            yield return AnimateHammerPose(
                hammerEndRotationZ,
                hammerStartRotationZ,
                hammerImpactPosition,
                raisedPosition,
                isFinalStrike ? hammerFinalReturnDuration : hammerReturnDuration,
                isFinalStrike);
        }

        ResetHammer();
        hammerRoutine = null;
        ShowPurchaseResult(weapon);
    }

    private IEnumerator AnimateHammerPose(
        float startZ,
        float endZ,
        Vector2 startPosition,
        Vector2 endPosition,
        float duration,
        bool fadeOut)
    {
        if (duration <= 0f)
        {
            SetHammerPose(endZ, endPosition);
            if (fadeOut)
                SetHammerAlpha(0f);
            yield break;
        }

        Vector2 controlPoint = (startPosition + endPosition) * 0.5f +
            Vector2.up * hammerArcHeight;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / duration));
            Vector2 firstLine = Vector2.LerpUnclamped(startPosition, controlPoint, progress);
            Vector2 secondLine = Vector2.LerpUnclamped(controlPoint, endPosition, progress);
            Vector2 curvedPosition = Vector2.LerpUnclamped(firstLine, secondLine, progress);
            SetHammerPose(
                Mathf.LerpUnclamped(startZ, endZ, progress),
                curvedPosition);
            if (fadeOut)
                SetHammerAlpha(1f - progress);
            yield return null;
        }

        SetHammerPose(endZ, endPosition);
        if (fadeOut)
            SetHammerAlpha(0f);
    }

    private void ShowPurchaseResult(ItemData weapon)
    {
        SetResult(weapon);
        if (weapon != null)
            SetFeedback($"{weapon.itemName} [{weapon.rarity}] 획득!");
    }

    private void ShowInventoryFullFeedback()
    {
        SetResult(null);
        if (resultRoot != null)
            resultRoot.SetActive(true);
        SetFeedback("인벤토리가 꽉찼어!");
    }

    private void ResetHammer()
    {
        if (hammerImage == null)
            return;

        CacheHammerPosition();
        hammerImage.color = hammerOriginalColor;
        SetHammerPose(
            hammerStartRotationZ,
            hammerImpactPosition + hammerRaisedOffset);
        hammerImage.gameObject.SetActive(false);
    }

    private void CacheHammerPosition()
    {
        if (hammerPositionCached || hammerImage == null)
            return;

        hammerImpactPosition = hammerImage.rectTransform.anchoredPosition;
        hammerOriginalColor = hammerImage.color;
        hammerPositionCached = true;
    }

    private void SetHammerPose(float z, Vector2 anchoredPosition)
    {
        if (hammerImage == null)
            return;

        hammerImage.rectTransform.anchoredPosition = anchoredPosition;
        SetHammerRotationZ(z);
    }

    private void SetHammerAlpha(float normalizedAlpha)
    {
        if (hammerImage == null)
            return;

        Color color = hammerOriginalColor;
        color.a *= Mathf.Clamp01(normalizedAlpha);
        hammerImage.color = color;
    }

    private void SetHammerRotationZ(float z)
    {
        if (hammerImage == null)
            return;

        Transform hammerTransform = hammerImage.transform;
        Vector3 rotation = hammerTransform.localEulerAngles;
        rotation.z = z;
        hammerTransform.localEulerAngles = rotation;
    }

    private void SetResult(ItemData weapon)
    {
        if (resultRoot != null)
            resultRoot.SetActive(weapon != null);

        if (resultWeaponImage != null)
        {
            resultWeaponImage.sprite = weapon != null ? weapon.icon : null;
            resultWeaponImage.enabled = weapon != null && weapon.icon != null;
            resultWeaponImage.preserveAspect = true;
        }
        if (weaponText != null)
            weaponText.text = weapon != null ? BuildWeaponStats(weapon) : string.Empty;
    }

    private static string BuildWeaponStats(ItemData item)
    {
        StringBuilder text = new StringBuilder();
        AppendStat(text, item.attack != 0 ? $"공격력 +{item.attack}" : null);
        AppendStat(text, item.attackSpeed != 0f ? $"공격속도 +{item.attackSpeed:0.##}" : null);
        AppendStat(text, item.criticalChance != 0f ? $"치명타 확률 +{item.criticalChance:0.##}" : null);
        AppendStat(text, item.criticalDamage != 0f ? $"치명타 피해 +{item.criticalDamage:0.##}" : null);
        return text.ToString();
    }

    private static void AppendStat(StringBuilder text, string stat)
    {
        if (string.IsNullOrEmpty(stat))
            return;
        if (text.Length > 0)
            text.Append('\n');
        text.Append(stat);
    }

}
