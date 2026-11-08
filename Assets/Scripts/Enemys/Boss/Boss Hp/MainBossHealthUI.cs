using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//============== 메인보스 HP UI ==============

public class MainBossHealthUI : MonoBehaviour
{
    public static MainBossHealthUI Instance;

    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image hpFill;
    [SerializeField] private TMP_Text bossName;
    [SerializeField] private TMP_Text hpText;

    [Header("Armor UI")]
    [SerializeField] private BossArmorUI armorUI;

    [Header("페이드아웃")]
    [SerializeField] private float hideFadeDuration = 1f;

    private float maxHp;

    private CanvasGroup canvasGroup;
    private Coroutine hideCoroutine;

    //============== 보스 이름 변환 규칙 ==============
    // 1) "Stage숫자"로 시작/포함하는 메인보스 프리팹 이름 → "숫자기사"
    //    예) "Stage1_MainBoss", "Stage5_MainBoss", "Stage5_MainBoss_OrbitSpare", "Stage2_MainBoss(Clone)"
    private static readonly Regex StagePattern =
        new Regex(@"stage\s*_?\s*(\d+)", RegexOptions.IgnoreCase);

    // 2) 이름에 "Knight"가 들어 있고 숫자가 있으면 "숫자 + 기사"로 바꿉니다.
    // 프리팹 이름 그대로 넘어오는 경우를 위해 앞뒤에 뭐가 붙어 있어도 동작합니다.
    // 예) "No.1 Knight", "No1Knight", "Knight1", "Knight_1", "No.1 Knight(Clone)" → "1기사"
    private static readonly Regex KnightPattern =
        new Regex(@"knight", RegexOptions.IgnoreCase);

    private static readonly Regex NumberPattern =
        new Regex(@"\d+");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (root == null)
            root = gameObject;

        // CanvasGroup 가져오기
        canvasGroup = root.GetComponent<CanvasGroup>();

        // 없으면 자동 추가
        if (canvasGroup == null)
            canvasGroup = root.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;

        root.SetActive(false);
    }

    //============== HP바 표시 ==============

    public void Show(string name, float currentHp, float maxHp)
    {
        // 기존 페이드아웃 중이면 중지
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        root.SetActive(true);

        // 다시 완전히 보이게
        canvasGroup.alpha = 1f;

        this.maxHp = maxHp;

        // 이름 표시: "No.1 Knight" 형식이면 "1기사"로 바꿔서 보여줍니다.
        if (bossName != null)
            bossName.text = FormatBossName(name);

        float hpPercent = Mathf.Clamp01(currentHp / maxHp);

        if (hpFill != null)
            hpFill.fillAmount = hpPercent;

        // HP 텍스트 갱신
        UpdateHPText(currentHp);

        // 갑옷 초기화
        if (armorUI != null)
        {
            armorUI.ResetArmor();
            armorUI.UpdateArmor(hpPercent);
        }
    }

    //============== 보스 이름 변환 ==============
    // 규칙에 맞지 않는 이름(예: 다른 보스 이름)은 그대로 표시합니다.
    private static string FormatBossName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return rawName;

        // Stage숫자_MainBoss 형식이면 스테이지 번호가 곧 기사 번호입니다.
        Match stage = StagePattern.Match(rawName);

        if (stage.Success)
            return GetKnightDisplayName(stage.Groups[1].Value);

        // "Knight"가 없는 이름(다른 보스 등)은 그대로 둡니다.
        if (!KnightPattern.IsMatch(rawName))
            return rawName;

        Match number = NumberPattern.Match(rawName);

        return number.Success ? GetKnightDisplayName(number.Value) : rawName;
    }

    //============== 기사 번호 → 표시 이름 ==============
    // 5기사(라스트보스)는 기계장비 0~2개면 "형", 3개 이상이면 "5기사"로 표시합니다.
    // EndingManager가 없는 씬에서는 기계장비 0개로 취급합니다. (대화 시스템과 동일한 기준)
    private static string GetKnightDisplayName(string number)
    {
        if (number == "5")
        {
            int machineCount = EndingManager.Instance != null
                ? EndingManager.Instance.MachineEquipmentCount
                : 0;

            return machineCount <= 2 ? "형" : "5기사";
        }

        return number + "기사";
    }

    //============== HP 갱신 ==============

    public void UpdateHP(float currentHp)
    {
        if (maxHp <= 0)
            return;

        float hpPercent = Mathf.Clamp01(currentHp / maxHp);

        if (hpFill != null)
            hpFill.fillAmount = hpPercent;

        // HP 텍스트 갱신
        UpdateHPText(currentHp);

        // 갑옷 상태 갱신
        if (armorUI != null)
            armorUI.UpdateArmor(hpPercent);
    }

    //============== HP 텍스트 ==============

    private void UpdateHPText(float currentHp)
    {
        if (hpText == null)
            return;

        // HP가 0 아래로 내려가지 않도록 제한
        currentHp = Mathf.Max(0f, currentHp);

        hpText.text =
            $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";
    }

    //============== HP바 페이드아웃 ==============

    public void Hide()
    {
        if (!root.activeSelf)
            return;

        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);

        hideCoroutine = StartCoroutine(FadeOut());
    }

    //============== HP바 즉시 숨기기 ==============
    public void HideImmediate()
    {
        // 진행 중인 페이드아웃 중지
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        // 즉시 투명하게 변경
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        // UI 비활성화
        if (root != null)
            root.SetActive(false);
    }

    private IEnumerator FadeOut()
    {
        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        while (timer < hideFadeDuration)
        {
            timer += Time.deltaTime;

            float progress =
                Mathf.Clamp01(timer / hideFadeDuration);

            canvasGroup.alpha =
                Mathf.Lerp(startAlpha, 0f, progress);

            yield return null;
        }

        canvasGroup.alpha = 0f;

        root.SetActive(false);

        hideCoroutine = null;
    }
}