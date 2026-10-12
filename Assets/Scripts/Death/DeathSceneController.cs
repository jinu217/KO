using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathSceneController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text countText;

    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private CanvasGroup countGroup;

    [Header("Animation")]
    [SerializeField] private float fadeTime = 1.5f;
    [SerializeField] private float waitTime = 1.5f;

    [Header("Result UI")]
    [SerializeField] private GameObject resultPanel;
    // 결과창 목록
    [SerializeField] private TMP_Text killCountText;
    [SerializeField] private TMP_Text midBossKillCountText;
    [SerializeField] private TMP_Text mainBossKillCountText;
    [SerializeField] private TMP_Text soulRewardText;
    [SerializeField] private TMP_Text currentSoulText;

    // 숫자만 표시하는 Text
    [SerializeField] private TMP_Text killCountValueText;
    [SerializeField] private TMP_Text midBossKillCountValueText;
    [SerializeField] private TMP_Text mainBossKillCountValueText;
    [SerializeField] private TMP_Text soulRewardValueText;
    [SerializeField] private TMP_Text currentSoulValueText;

    [Header("Result Animation")]
    [SerializeField] private float resultStartDelay = 1.5f;
    [SerializeField] private float countDuration = 1f;
    [SerializeField] private float countInterval = 0.5f;

    [Header("Continue UI")]
    [SerializeField] private TMP_Text continueText;

    private int deathCount = 0;
    private bool canContinue = false;
    private bool isWaitingForContinue = false;
    private Coroutine continueBlinkCoroutine;

    private void Start()
    {
        if (SaveData.Instance != null)
        {
            deathCount = SaveData.Instance.persistent.deathCount;
            Debug.Log("DeathScene에서 deathCount = " + deathCount);
        }
        else
        {
            Debug.Log("DeathScene에서 SaveData가 없습니다.");
        }

        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
        }

        if (continueText != null)
        {
            continueText.gameObject.SetActive(false);
        }

        StartCoroutine(DeathRoutine());
    }

    private void Update()
    {
        if (!isWaitingForContinue)
            return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            canContinue = true;
        }
    }


    // =========================================================
    // 결과창 연출
    // =========================================================

    private IEnumerator ShowResult()
    {
        RunData run = SaveData.Instance.run;

        int killCount = run.killCount;
        int midBossKillCount = run.midBossKillCount;
        int mainBossKillCount = run.mainBossKillCount;

        // 보상 기준
        int normalKillReward = 1;
        int midBossReward = 50;
        int mainBossReward = 100;

        int rewardSoul =
            killCount * normalKillReward
            + midBossKillCount * midBossReward
            + mainBossKillCount * mainBossReward;

        // 현재 보유 Soul
        int currentSoul =
            SaveData.Instance.persistent.soul;

        // =====================================================
        // 모든 결과 항목 숨김
        // =====================================================

        killCountText.gameObject.SetActive(false);
        midBossKillCountText.gameObject.SetActive(false);
        mainBossKillCountText.gameObject.SetActive(false);
        soulRewardText.gameObject.SetActive(false);
        currentSoulText.gameObject.SetActive(false);

        killCountValueText.gameObject.SetActive(false);
        midBossKillCountValueText.gameObject.SetActive(false);
        mainBossKillCountValueText.gameObject.SetActive(false);
        soulRewardValueText.gameObject.SetActive(false);
        currentSoulValueText.gameObject.SetActive(false);

        // =====================================================
        // 씬 시작 후 설정한 시간만큼 대기
        // =====================================================

        yield return new WaitForSeconds(resultStartDelay);

        // =====================================================
        // 결과창 활성화
        // =====================================================

        resultPanel.SetActive(true);

        // =====================================================
        // 1. 일반 몬스터 처치
        // =====================================================

        killCountText.gameObject.SetActive(true);
        killCountValueText.gameObject.SetActive(true);

        killCountText.text = "일반 몬스터 처치";
        killCountValueText.text = "0";

        yield return CountNumber(
            0,
            killCount,
            countDuration,
            value =>
            {
                killCountValueText.text = value.ToString();
            }
        );

        yield return new WaitForSeconds(countInterval);


        // =====================================================
        // 2. 미드보스 처치
        // =====================================================

        midBossKillCountText.gameObject.SetActive(true);
        midBossKillCountValueText.gameObject.SetActive(true);

        midBossKillCountText.text = "미드보스 처치";
        midBossKillCountValueText.text = "0";

        yield return CountNumber(
            0,
            midBossKillCount,
            countDuration,
            value =>
            {
                midBossKillCountValueText.text =
                    value.ToString();
            }
        );

        yield return new WaitForSeconds(countInterval);


        // =====================================================
        // 3. 메인보스 처치
        // =====================================================

        mainBossKillCountText.gameObject.SetActive(true);
        mainBossKillCountValueText.gameObject.SetActive(true);

        mainBossKillCountText.text = "메인보스 처치";
        mainBossKillCountValueText.text = "0";

        yield return CountNumber(
            0,
            mainBossKillCount,
            countDuration,
            value =>
            {
                mainBossKillCountValueText.text =
                    value.ToString();
            }
        );

        yield return new WaitForSeconds(countInterval);


        // =====================================================
        // 4. 획득 Soul
        // =====================================================

        soulRewardText.gameObject.SetActive(true);
        soulRewardValueText.gameObject.SetActive(true);

        soulRewardText.text = "획득 Soul";
        soulRewardValueText.text = "0";

        yield return CountNumber(
            0,
            rewardSoul,
            countDuration,
            value =>
            {
                soulRewardValueText.text =
                    value.ToString();
            }
        );


        // =====================================================
        // 획득 Soul을 영구 재화에 추가
        // =====================================================

        SaveData.Instance.persistent.soul += rewardSoul;

        Debug.Log($"획득 Soul : {rewardSoul}");


        yield return new WaitForSeconds(countInterval);


        // =====================================================
        // 5. 현재 보유 Soul
        // =====================================================

        int newCurrentSoul =
            SaveData.Instance.persistent.soul;

        currentSoulText.gameObject.SetActive(true);
        currentSoulValueText.gameObject.SetActive(true);

        currentSoulText.text = "현재 보유 Soul";
        currentSoulValueText.text = currentSoul.ToString();

        yield return CountNumber(
            currentSoul,
            newCurrentSoul,
            countDuration,
            value =>
            {
                currentSoulValueText.text =
                    value.ToString();
            }
        );


        Debug.Log(
            $"현재 보유 Soul : {SaveData.Instance.persistent.soul}"
        );
    }


    // =========================================================
    // 숫자 카운팅
    // =========================================================

    private IEnumerator CountNumber(
        int startValue,
        int targetValue,
        float duration,
        System.Action<int> onValueChanged)
    {
        // 목표값이 0인 경우
        if (targetValue <= 0)
        {
            onValueChanged?.Invoke(0);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );

            int currentValue =
                Mathf.RoundToInt(
                    Mathf.Lerp(
                        startValue,
                        targetValue,
                        progress
                    )
                );

            onValueChanged?.Invoke(currentValue);

            yield return null;
        }

        // 마지막에는 정확한 값 표시
        onValueChanged?.Invoke(targetValue);
    }

    // 깜빡임 코루틴 추가
    private IEnumerator BlinkContinueText()
    {
        while (isWaitingForContinue)
        {
            // 서서히 나타남
            yield return FadeTextAlpha(continueText, 0f, 1f, 0.5f);

            // 잠시 유지
            yield return new WaitForSeconds(0.3f);

            // 서서히 사라짐
            yield return FadeTextAlpha(continueText, 1f, 0f, 0.5f);

            // 잠시 유지
            yield return new WaitForSeconds(0.3f);
        }
    }

    private IEnumerator FadeTextAlpha(TMP_Text text, float from, float to, float duration)
    {
        float elapsed = 0f;

        Color color = text.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float alpha =
                Mathf.Lerp(
                    from,
                    to,
                    elapsed / duration
                );

            color.a = alpha;
            text.color = color;

            yield return null;
        }

        color.a = to;
        text.color = color;
    }


    // =========================================================
    // 데스씬 전체 진행
    // =========================================================

    private IEnumerator DeathRoutine()
    {
        //================== 로드로 진입한 경우 ====================

        if (SaveData.Instance.isLoadIntro)
        {
            resultPanel.SetActive(false);

            titleText.text = "영애님을 구하는";
            countText.text = $"{deathCount + 1}번째 시도";

            titleGroup.alpha = 0;
            countGroup.alpha = 0;

            yield return new WaitForSeconds(0.5f);

            yield return Fade(titleGroup, 0, 1);

            yield return new WaitForSeconds(0.5f);

            yield return Fade(countGroup, 0, 1);

            yield return new WaitForSeconds(waitTime);

            StartCoroutine(Fade(titleGroup, 1, 0));
            StartCoroutine(Fade(countGroup, 1, 0));

            yield return new WaitForSeconds(fadeTime);

            SaveData.Instance.isLoadIntro = false;

            SceneManager.LoadScene(
                SaveData.Instance.persistent.nextStage
            );

            yield break;
        }


        //================== 사망으로 진입한 경우 ====================

        continueText.gameObject.SetActive(false);


        //================== 결과창 순차 표시 ====================

        yield return StartCoroutine(ShowResult());


        //================== F key 입력 대기 ====================

        continueText.text = "\" F \" Key를 눌러 계속..";
        canContinue = false;
        isWaitingForContinue = true;
        continueText.gameObject.SetActive(true);

        // F key 안내 깜빡임 시작
        continueBlinkCoroutine =
            StartCoroutine(BlinkContinueText());

        while (!canContinue)
        {
            yield return null;
        }

        isWaitingForContinue = false;

        // 깜빡임 종료
        if (continueBlinkCoroutine != null)
        {
            StopCoroutine(continueBlinkCoroutine);
            continueBlinkCoroutine = null;
        }

        // 텍스트 알파 복구
        Color color = continueText.color;
        color.a = 1f;
        continueText.color = color;

        // Enter 안내 숨김
        continueText.gameObject.SetActive(false);

        resultPanel.SetActive(false);


        //================== 사망 문구 ====================

        titleText.text = "영애님을 구하는";
        countText.text = $"{deathCount + 1}번째 시도";

        titleGroup.alpha = 0;
        countGroup.alpha = 0;

        yield return new WaitForSeconds(0.5f);

        yield return Fade(titleGroup, 0, 1);

        yield return new WaitForSeconds(0.5f);

        yield return Fade(countGroup, 0, 1);

        yield return new WaitForSeconds(waitTime);

        StartCoroutine(Fade(titleGroup, 1, 0));
        StartCoroutine(Fade(countGroup, 1, 0));

        yield return new WaitForSeconds(fadeTime);


        //================== 다음 회차 시작 ====================

        SaveData.Instance.persistent.nextStage = "Tutorial_N";

        SaveData.Instance.ResetRun();

        SaveManager.Instance.Save(
            SaveData.Instance.currentSlot
        );

        SceneManager.LoadScene("Tutorial_N");
    }


    // =========================================================
    // 페이드
    // =========================================================

    private IEnumerator Fade(
        CanvasGroup group,
        float from,
        float to)
    {
        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;

            group.alpha =
                Mathf.Lerp(
                    from,
                    to,
                    t / fadeTime
                );

            yield return null;
        }

        group.alpha = to;
    }
}