using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//============== 중간보스 HP UI ==============

public class MidBossHealthUI : MonoBehaviour
{
    public static MidBossHealthUI Instance;

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

    //============== 중간보스 이름 ==============
    // 프리팹 이름 "Stage숫자_MidBoss"의 숫자로 이름을 고릅니다. (인덱스 0 = 스테이지 1)
    // 예) "Stage1_MidBoss", "Stage3_MidBoss(Clone)"
    // 이름을 바꾸고 싶으면 이 목록의 문자열만 수정하면 됩니다.
    private static readonly string[] MidBossNames =
    {
        "도적단 행동대장",   // Stage1 : 성 밖의 도적단
        "성문 파수대장",     // Stage2 : 블랙우드 성문
        "철갑 돌격대장",     // Stage3 : 나이트테크로 무장한 병력
        "침식 실험 감독관",  // Stage4 : 기계화 실험이 이뤄지는 연구동
        "심층부 근위대장",   // Stage5 : 라스트보스 직전의 최심부
    };

    private static readonly Regex StagePattern =
        new Regex(@"stage\s*_?\s*(\d+)", RegexOptions.IgnoreCase);

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

        // 이름 표시: "Stage숫자_MidBoss" 형식이면 스테이지별 이름으로 바꿔서 보여줍니다.
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
    // 규칙에 맞지 않는 이름(1~5 밖의 스테이지 등)은 그대로 표시합니다.
    private static string FormatBossName(string rawName)
    {
        if (string.IsNullOrEmpty(rawName))
            return rawName;

        Match stage = StagePattern.Match(rawName);

        if (stage.Success
            && int.TryParse(stage.Groups[1].Value, out int stageNumber)
            && stageNumber >= 1
            && stageNumber <= MidBossNames.Length)
        {
            return MidBossNames[stageNumber - 1];
        }

        return rawName;
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
    // HP 텍스트 표시
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