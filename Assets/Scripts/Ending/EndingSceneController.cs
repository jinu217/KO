using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class EndingSceneController : MonoBehaviour
{
    [System.Serializable]
    public class EndingBackground
    {
        public EndingType endingType;
        public Sprite backgroundImage;
    }
    
    [Header("페이드 연출")]
    [SerializeField] private FadeManager fadeManager;

    [Header("UI 연결")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image gradientShadowImage;
    [SerializeField] private TMP_Text endingTitleText;
    [SerializeField] private TMP_Text endingStoryText;
    [SerializeField] private TMP_Text continueText;

    [Header("엔딩별 배경 이미지")]
    [SerializeField] private List<EndingBackground> endingBackgrounds =
        new List<EndingBackground>();

    [Header("그라데이션 그림자")]
    [SerializeField] private Sprite gradientShadowSprite;

    [Header("엔딩 연출 시간")]
    [SerializeField] private float backgroundWaitTime = 2f;
    [SerializeField] private float shadowMoveDuration = 1.5f;

    [Header("텍스트 타이핑")]
    [SerializeField] private float titleTypingSpeed = 20f;
    [SerializeField] private float storyTypingSpeed = 30f;

    //============== 텍스트 표시 딜레이 ==============
    [Header("텍스트 표시 딜레이")]
    [SerializeField] private float textStartDelay = 0.5f;
    [SerializeField] private float continueTextDelay = 0.4f;

    //============== ContinueText 깜빡임 ==============
    [Header("ContinueText 깜빡임")]
    [SerializeField] private float continueBlinkSpeed = 1f;
    private Coroutine continueBlinkCoroutine;

    //============== 엔딩 테스트 모드 ==============
    [Header("테스트 모드")]
    [SerializeField] private bool useTestMode = false;
    [SerializeField] private EndingType testEndingType = EndingType.TrueEnding;

    [Header("타이틀 씬")]
    [SerializeField] private string titleSceneName = "Title Scene";

    private EndingType currentEndingType;
    private List<string> currentParagraphs = new List<string>();

    private bool endingInitialized;
    private bool canReturnToTitle;
    private bool isReturningToTitle = false;
    
    private void Start()
    {
        InitializeEnding();
    }
    
    private void Update()
    {
        if (!endingInitialized || !canReturnToTitle || isReturningToTitle)
            return;

        if (Keyboard.current == null)
            return;

        foreach (var key in Keyboard.current.allKeys)
        {
            if (key.wasPressedThisFrame)
            {
                StartCoroutine(ReturnToTitle());
                break;
            }
        }
    }

    //============== 타이틀 씬 복귀 ==============
    private IEnumerator ReturnToTitle()
    {
        isReturningToTitle = true;
        canReturnToTitle = false;

        if (fadeManager != null)
            yield return StartCoroutine(fadeManager.FadeOut());

        SceneManager.LoadScene(titleSceneName);
    }


    //============== 엔딩 초기화 ==============
    private void InitializeEnding()
    {
        //============== 엔딩 판정 / 테스트 모드 ==============
        if (useTestMode)
        {
            // 테스트 모드에서는 Inspector에서 선택한 엔딩 사용
            currentEndingType = testEndingType;

            Debug.Log(
                $"[EndingSceneController] 엔딩 테스트 모드: {currentEndingType}");
        }
        else
        {
            // 실제 게임에서는 EndingManager를 통해 엔딩 판정
            if (EndingManager.Instance == null)
            {
                Debug.LogError(
                    "[EndingSceneController] EndingManager가 없습니다.");
                return;
            }

            currentEndingType = EndingManager.Instance.DetermineEnding();

            if (currentEndingType == EndingType.None)
            {
                Debug.LogError(
                    "[EndingSceneController] 엔딩 판정에 실패했습니다.");
                return;
            }
        }

        //============== 엔딩 제목 설정 ==============
        if (endingTitleText != null)
        {
            endingTitleText.text = GetEndingTitle(currentEndingType);
            endingTitleText.maxVisibleCharacters = 0;
            endingTitleText.gameObject.SetActive(false);
        }

        //============== 엔딩 배경 설정 ==============
        SetEndingBackground(currentEndingType);

        //============== 그라데이션 설정 ==============
        SetGradientShadow();

        //============== 엔딩 서술문 설정 ==============
        currentParagraphs = GetEndingParagraphs(currentEndingType);

        if (currentParagraphs.Count == 0)
        {
            Debug.LogError("[EndingSceneController] 엔딩 서술문이 없습니다.");
            return;
        }

        if (endingStoryText != null)
        {
            endingStoryText.text = string.Join(
                " ", currentParagraphs);

            endingStoryText.maxVisibleCharacters = 0;
            endingStoryText.gameObject.SetActive(false);
        }

        //============== 계속 안내 숨기기 ==============
        if (continueText != null)
            continueText.gameObject.SetActive(false);

        endingInitialized = true;

        //============== 엔딩 연출 시작 ==============
        StartCoroutine(PlayEndingSequence());
    }

    //============== 엔딩 연출 순서 ==============
    private IEnumerator PlayEndingSequence()
    {
        //============== 씬 시작 페이드 인 ==============
        if (fadeManager != null)
            yield return StartCoroutine(fadeManager.FadeIn());

        // 1. 배경만 표시하고 대기
        if (backgroundImage != null)
            backgroundImage.gameObject.SetActive(true);

        if (gradientShadowImage != null)
            gradientShadowImage.gameObject.SetActive(false);

        yield return new WaitForSeconds(
            Mathf.Max(0f, backgroundWaitTime));

        // 2. 그라데이션 그림자 등장
        if (gradientShadowImage != null &&
            gradientShadowSprite != null)
        {
            yield return StartCoroutine(PlayShadowAnimation());
        }

        //============== 텍스트 표시 전 딜레이 ==============
        yield return new WaitForSeconds(
            Mathf.Max(0f, textStartDelay));

        // 3. 타이틀과 스토리 표시
        if (endingTitleText != null)
            endingTitleText.gameObject.SetActive(true);

        if (endingStoryText != null)
            endingStoryText.gameObject.SetActive(true);

        // 두 텍스트를 동시에 타이핑
        yield return StartCoroutine(PlayTextTyping());

        //============== ContinueText 표시 전 딜레이 ==============
        yield return new WaitForSeconds(
            Mathf.Max(0f, continueTextDelay));

        // 4. 타이핑 완료 후 입력 안내 표시
        if (continueText != null)
        {
            continueText.gameObject.SetActive(true);

            // 기존 투명도 초기화
            Color color = continueText.color;
            color.a = 1f;
            continueText.color = color;

            // 깜빡임 시작
            if (continueBlinkCoroutine != null)
                StopCoroutine(continueBlinkCoroutine);

            continueBlinkCoroutine = StartCoroutine(BlinkContinueText());
        }
        canReturnToTitle = true;
    }

    //============== 그라데이션 그림자 설정 ==============
    private void SetGradientShadow()
    {
        if (gradientShadowImage == null)
        {
            Debug.LogWarning(
                "[EndingSceneController] GradientShadow가 연결되지 않았습니다.");
            return;
        }

        gradientShadowImage.sprite = gradientShadowSprite;
        gradientShadowImage.raycastTarget = false;
        gradientShadowImage.preserveAspect = false;
        gradientShadowImage.gameObject.SetActive(false);
    }

    //============== 그라데이션 왼쪽에서 오른쪽으로 이동 ==============
    private IEnumerator PlayShadowAnimation()
    {
        RectTransform shadowRect =
            gradientShadowImage.rectTransform;

        RectTransform parentRect =
            shadowRect.parent as RectTransform;

        if (parentRect == null)
        {
            gradientShadowImage.gameObject.SetActive(true);
            yield break;
        }

        // 최종 위치 저장
        Vector2 targetPosition = shadowRect.anchoredPosition;

        // 화면 왼쪽 바깥에서 시작
        Vector2 startPosition = targetPosition +
            Vector2.left * parentRect.rect.width;

        shadowRect.anchoredPosition = startPosition;
        gradientShadowImage.gameObject.SetActive(true);

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, shadowMoveDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);

            // 부드럽게 감속하며 이동
            float easedProgress =
                1f - Mathf.Pow(1f - progress, 3f);

            shadowRect.anchoredPosition = Vector2.Lerp(
                startPosition,
                targetPosition,
                easedProgress);

            yield return null;
        }

        shadowRect.anchoredPosition = targetPosition;
    }

    //============== 타이틀 + 스토리 동시 타이핑 ==============
    private IEnumerator PlayTextTyping()
    {
        if (endingTitleText != null)
            endingTitleText.ForceMeshUpdate();

        if (endingStoryText != null)
            endingStoryText.ForceMeshUpdate();

        int titleLength = endingTitleText != null
            ? endingTitleText.textInfo.characterCount
            : 0;

        int storyLength = endingStoryText != null
            ? endingStoryText.textInfo.characterCount
            : 0;

        float titleProgress = 0f;
        float storyProgress = 0f;

        while (titleProgress < titleLength ||
               storyProgress < storyLength)
        {
            titleProgress +=
                Mathf.Max(0f, titleTypingSpeed) * Time.deltaTime;

            storyProgress +=
                Mathf.Max(0f, storyTypingSpeed) * Time.deltaTime;

            if (endingTitleText != null)
            {
                endingTitleText.maxVisibleCharacters =
                    Mathf.Min(
                        titleLength,
                        Mathf.FloorToInt(titleProgress));
            }

            if (endingStoryText != null)
            {
                endingStoryText.maxVisibleCharacters =
                    Mathf.Min(
                        storyLength,
                        Mathf.FloorToInt(storyProgress));
            }

            yield return null;
        }

        // 마지막 글자까지 확실하게 표시
        if (endingTitleText != null)
            endingTitleText.maxVisibleCharacters = titleLength;

        if (endingStoryText != null)
            endingStoryText.maxVisibleCharacters = storyLength;
    }

    //============== ContinueText 깜빡임 효과 ==============
    private IEnumerator BlinkContinueText()
    {
        if (continueText == null)
            yield break;

        TMP_Text text = continueText;
        Color color = text.color;

        while (true)
        {
            // 투명도 1 → 0 사이를 반복
            color.a = Mathf.PingPong(
                Time.unscaledTime * continueBlinkSpeed, 1f);

            text.color = color;

            yield return null;
        }
    }

    //============== 엔딩 제목 ==============
    private string GetEndingTitle(EndingType type)
    {
        switch (type)
        {
            case EndingType.TrueEnding:
                return "TRUE ENDING : TOMORROW";

            case EndingType.HappyEnding:
                return "HAPPY ENDING..? : DOUBT";

            case EndingType.NormalEnding1:
                return "NORMAL ENDING I : SECLUSION";

            case EndingType.NormalEnding2:
                return "NORMAL ENDING II : OBEDIENCE";

            case EndingType.HiddenEnding:
                return "HIDDEN ENDING : ACCEPTED";

            case EndingType.BadEnding:
                return "BAD ENDING : LOST";

            default:
                return "ENDING";
        }
    }

    //============== 엔딩 서술문 ==============
    private List<string> GetEndingParagraphs(EndingType type)
    {
        switch (type)
        {
            case EndingType.TrueEnding:
                return new List<string>
                {
                    "형의 기계 이식을 제거하는 것은 길고도 고단한 여정이 될 것이다...",
                    "하지만 영애와 기술자들이 피해를 되돌리기 위해 쉴 없이 힘써준 덕분에, 희망이 뿌리내리기 시작했다.",
                    "치유를 위해 분주하던 그 나날들 속에서, 우리는 알 수 있었다... 우리가 이 평화로운 풍경을 다시 함께 바라볼 수 있는 날이 더 이상 먼 꿈이 아닌, 머지않아 닿을 미래라는 것을."
                };

            case EndingType.HappyEnding:
                return new List<string>
                {
                    "모두를 구하기 위해, 난 기계를 받아들일 수밖에 없었다...",
                    "하지만 블랙우드를 거치며, 무쇠에 이성을 빼앗긴 기사들을 본 뒤로...",
                    "내 몸 안에서 요동치는 이 톱니바퀴들을 더는 믿을 수 없다."
                };

            case EndingType.NormalEnding1:
                return new List<string>
                {
                    "날 속인 그녀를 죽였다...",
                    "하지만 분노의 불꽃이 사그라들자 깨달았다... 그녀의 마지막 얼굴이 기억나지 않는다는 것을.",
                    "내가 그녀를 베었을 때, 그 마지막 일격으로 내가 누구였는지를 보여주던 유일한 거울을 깨뜨려 버린 것이다.",
                    "이제 돌아갈 곳은 없다. 오직 내가 저지른 짓이 남긴, 거대하고 침묵뿐인 허공만이 남아있을 뿐이다."
                };

            case EndingType.NormalEnding2:
                return new List<string>
                {
                    "형은... 이제 되돌릴 수 없었어. 구원할 방법도 없었지. 그래서... 내가 끝내야만 했어.",
                    "하지만 이 손을 바라보고 있으면 두려워... 나 또한 언젠가, 내가 파괴한 그 괴물처럼 변해버리는 것은 아닐까?"
                };

            case EndingType.HiddenEnding:
                return new List<string>
                {
                    "형이 적국의 기사단장 자리를 권했을 때...",
                    "내 안의 기계는 걷잡을 수 없는 욕망을 채우기 위해, 날 대신해 선택을 내렸다.",
                    "나는 그의 손을 잡았다. 이제, 블랙우드의 기사로 나아간다."
                };

            case EndingType.BadEnding:
                return new List<string>
                {
                    "기계가 내 이성을 앗아갔다... 내가 그들을 전부 도살했다. 나의 형... 영애... 그들의 피가 내 손에 묻었으나, 아무것도 느껴지지 않았다.",
                    "이제, 기사들이 내가 되어버린 이 괴물을 토벌하기 위해 찾아왔다. 나에게 구원이란 없다."
                };

            default:
                return new List<string>();
        }
    }

    //============== 엔딩 배경 설정 ==============
    private void SetEndingBackground(EndingType type)
    {
        if (backgroundImage == null)
        {
            Debug.LogError(
                "[EndingSceneController] BackgroundImage가 연결되지 않았습니다.");
            return;
        }

        foreach (EndingBackground data in endingBackgrounds)
        {
            if (data.endingType == type)
            {
                backgroundImage.sprite = data.backgroundImage;
                backgroundImage.enabled = data.backgroundImage != null;
                return;
            }
        }

        backgroundImage.enabled = false;

        Debug.LogWarning(
            $"[EndingSceneController] 배경 이미지가 연결되지 않았습니다: {type}");
    }
}