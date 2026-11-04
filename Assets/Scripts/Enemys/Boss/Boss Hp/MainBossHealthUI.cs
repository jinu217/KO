using System.Collections;
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

        if (bossName != null)
            bossName.text = name;

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