
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

    [Header("UI 연결")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private TMP_Text endingTitleText;
    [SerializeField] private TMP_Text endingStoryText;
    [SerializeField] private TMP_Text continueText;

    [Header("엔딩별 배경 이미지")]
    [SerializeField] private List<EndingBackground> endingBackgrounds =
        new List<EndingBackground>();

    [Header("타이틀 씬")]
    [SerializeField] private string titleSceneName = "Title";

    private EndingType currentEndingType;
    private List<string> currentParagraphs = new List<string>();
    private int paragraphIndex;
    private bool endingInitialized;

    private void Start()
    {
        InitializeEnding();
    }

    private void Update()
    {
        if (!endingInitialized || Keyboard.current == null)
            return;

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            ShowNextParagraph();
        }
    }

    //============== 엔딩 초기화 ==============
    private void InitializeEnding()
    {
        if (EndingManager.Instance == null)
        {
            Debug.LogError("[EndingSceneController] EndingManager가 없습니다.");
            return;
        }

        currentEndingType = EndingManager.Instance.DetermineEnding();

        if (currentEndingType == EndingType.None)
        {
            Debug.LogError("[EndingSceneController] 엔딩 판정에 실패했습니다.");
            return;
        }

        //============== 제목 설정 ==============
        if (endingTitleText != null)
            endingTitleText.text = GetEndingTitle(currentEndingType);

        //============== 배경 이미지 설정 ==============
        SetEndingBackground(currentEndingType);

        //============== 서술문 설정 ==============
        currentParagraphs = GetEndingParagraphs(currentEndingType);

        if (currentParagraphs.Count == 0)
        {
            Debug.LogError("[EndingSceneController] 엔딩 서술문이 없습니다.");
            return;
        }

        paragraphIndex = 0;
        endingInitialized = true;
        ShowParagraph();
    }

    //============== 엔딩 제목 ==============
    private string GetEndingTitle(EndingType type)
    {
        switch (type)
        {
            case EndingType.TrueEnding:
                return "TRUE ENDING";

            case EndingType.HappyEnding:
                return "HAPPY ENDING";

            case EndingType.NormalEnding1:
                return "NORMAL ENDING I";

            case EndingType.NormalEnding2:
                return "NORMAL ENDING II";

            case EndingType.HiddenEnding:
                return "HIDDEN ENDING";

            case EndingType.BadEnding:
                return "BAD ENDING";

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
                    "기사는 마침내 자신이 지키고자 했던 것을 마주했다.",
                    "기계의 힘이 아닌 자신의 의지로 선택한 길이었다.",
                    "긴 싸움 끝에, 새로운 내일이 시작되었다."
                };

            case EndingType.HappyEnding:
                return new List<string>
                {
                    "모든 것이 완벽하지는 않았다.",
                    "그러나 그들은 서로의 곁에 남기로 했다.",
                    "상처 입은 세계에도 다시 희망이 피어나기 시작했다."
                };

            case EndingType.NormalEnding1:
                return new List<string>
                {
                    "전투는 끝났지만, 모든 것을 구할 수는 없었다.",
                    "기사는 승리의 무게를 안고 앞으로 나아갔다."
                };

            case EndingType.NormalEnding2:
                return new List<string>
                {
                    "그녀가 없는 세계는 이전과 같을 수 없었다.",
                    "기사는 돌이킬 수 없는 선택을 가슴에 새겼다."
                };

            case EndingType.HiddenEnding:
                return new List<string>
                {
                    "모두가 끝이라고 믿었던 순간, 감춰진 진실이 모습을 드러냈다.",
                    "기사는 누구도 알지 못했던 새로운 가능성을 발견했다.",
                    "이 세계의 운명은 아직 결정되지 않았다."
                };

            case EndingType.BadEnding:
                return new List<string>
                {
                    "기계의 힘은 마침내 모든 것을 집어삼켰다.",
                    "지키려 했던 것들은 사라지고, 왕국에는 침묵만이 남았다.",
                    "그 누구도 이 결말을 되돌릴 수 없었다."
                };

            default:
                return new List<string>();
        }
    }

    //============== 엔딩 배경 설정 ==============
    private void SetEndingBackground(EndingType type)
    {
        if (backgroundImage == null)
            return;

        foreach (EndingBackground data in endingBackgrounds)
        {
            if (data.endingType == type)
            {
                backgroundImage.sprite = data.backgroundImage;
                backgroundImage.enabled = data.backgroundImage != null;
                return;
            }
        }

        Debug.LogWarning($"[EndingSceneController] 배경 이미지가 연결되지 않았습니다: {type}");
    }

    //============== 다음 문단 ==============
    private void ShowNextParagraph()
    {
        paragraphIndex++;

        if (paragraphIndex >= currentParagraphs.Count)
        {
            SceneManager.LoadScene(titleSceneName);
            return;
        }

        ShowParagraph();
    }

    //============== 문단 표시 ==============
    private void ShowParagraph()
    {
        if (endingStoryText != null)
            endingStoryText.text = currentParagraphs[paragraphIndex];

        if (continueText != null)
        {
            bool isLastParagraph =
                paragraphIndex == currentParagraphs.Count - 1;

            continueText.text = isLastParagraph
                ? " F 키를 눌러 타이틀로 돌아가기"
                : " F 키를 눌러 계속";
        }
    }
}
