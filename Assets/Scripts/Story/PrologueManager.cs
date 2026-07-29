using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Text;

[System.Serializable]
public class PrologueDialogue
{
    public string type;
    public string characterName;
    public string characterImage;
    public string backgroundImage;
    public string cutsceneImage;
    public string dialogue;
}

public class PrologueManager : MonoBehaviour
{
    [Header("Fade")]
    [SerializeField] private FadeManager fadeManager;

    [Header("UI")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image characterImage;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private TMP_Text dialogueText;


    [Header("Cutscene UI")]
    [SerializeField] private Image cutsceneImage;
    [SerializeField] private GameObject cutsceneTextPanel;
    [SerializeField] private TMP_Text cutsceneText;
    [SerializeField] private Image flashImage;


    [Header("Dialogue Panel")]
    [SerializeField] private GameObject dialoguePanel;


    [Header("Skip UI")]
    [SerializeField] private GameObject skipConfirmPanel;


    [Header("Dialogue Settings")]
    [SerializeField] private float typeSpeed = 0.04f;
    [Header("Cutscene Dialogue Settings")]
    [SerializeField] private float cutsceneTypeSpeed = 0.1f;


    [Header("Flash Settings")]
    [SerializeField] private float flashDuration = 0.05f;
    [SerializeField] private float flashInterval = 0.1f;
    [SerializeField] private float flashIntervalIncrease = 0.15f;
    private float currentFlashInterval;


    [Header("CSV")]
    [SerializeField] private string csvFileName = "Prologue/PrologueDialogue";


    private List<PrologueDialogue> dialogues =
        new List<PrologueDialogue>();

    private int currentDialogueIndex = 0;

    private Coroutine typingCoroutine;

    private bool isTyping = false;

    private string currentFullText;

    // 현재 Flash 연출 중인지 확인
    private bool isFlashing = false;


    private void Start()
    {
        skipConfirmPanel.SetActive(false);

        // CSV 불러오기
        LoadDialogueFromCSV();

        if (dialogues == null || dialogues.Count == 0)
        {
            Debug.LogWarning("프롤로그 대사가 없습니다.");
            return;
        }

        ShowDialogue(currentDialogueIndex);
    }

    private void Update()
    {
        // Flash 중에는 입력 받지 않음
        if (isFlashing)
        {
            return;
        }

        // 스킵 확인창이 열려 있는 경우
        if (skipConfirmPanel.activeSelf)
        {
            // Enter를 누르면 프롤로그 스킵
            if (Input.GetKeyDown(KeyCode.Return))
            {
                SkipPrologue();
            }

            // Esc를 누르면 스킵 취소
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CancelSkip();
            }

            // 스킵 확인 중에는 다른 입력을 받지 않음
            return;
        }

        // Esc를 누르면 스킵 확인창 열기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            OpenSkipConfirm();
            return;
        }

        // Enter 입력
        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (isTyping)
            {
                // 타이핑 중이면 현재 대사 즉시 완성
                CompleteDialogue();
            }
            else
            {
                // 타이핑이 끝났으면 다음 대사
                NextDialogue();
            }
        }
    }



    //==================================================
    // CSV 불러오기
    //==================================================

    private void LoadDialogueFromCSV()
    {
        TextAsset csvFile =
            Resources.Load<TextAsset>(csvFileName);


        if (csvFile == null)
        {
            Debug.LogError(
                "CSV 파일을 찾을 수 없습니다. 경로: Resources/"
                + csvFileName
            );

            return;
        }


        // CSV를 UTF-8 인코딩으로 읽기
        string csvText =
            Encoding.UTF8.GetString(csvFile.bytes);


        string[] lines =
            csvText.Split('\n');


        // 첫 번째 줄은 헤더이므로 건너뜀
        for (int i = 1; i < lines.Length; i++)
        {
            string line =
                lines[i].Trim();


            // 빈 줄 무시
            if (string.IsNullOrEmpty(line))
                continue;


            string[] values =
                ParseCSVLine(line);


            // Type까지 포함해서 최소 6개 필요
            if (values.Length < 6)
            {
                Debug.LogWarning(
                    "CSV 데이터 형식이 잘못되었습니다. 줄: "
                    + (i + 1)
                );

                continue;
            }


            PrologueDialogue dialogue =
                new PrologueDialogue();


            dialogue.type =
                values[0].Trim();


            dialogue.characterName =
                values[1].Trim();


            dialogue.characterImage =
                values[2].Trim();


            dialogue.backgroundImage =
                values[3].Trim();


            dialogue.cutsceneImage =
                values[4].Trim();


            dialogue.dialogue =
                values[5].Trim();


            dialogues.Add(dialogue);
        }


        Debug.Log(
            "프롤로그 데이터 "
            + dialogues.Count
            + "개를 불러왔습니다."
        );
    }



    //==================================================
    // CSV 한 줄 파싱
    //==================================================

    private string[] ParseCSVLine(string line)
    {
        List<string> values =
            new List<string>();


        bool insideQuotes = false;


        string currentValue = "";


        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];


            // 큰따옴표
            if (c == '"')
            {
                insideQuotes =
                    !insideQuotes;
            }


            // 쉼표
            else if (c == ',' && !insideQuotes)
            {
                values.Add(currentValue);

                currentValue = "";
            }


            else
            {
                currentValue += c;
            }
        }


        // 마지막 값 추가
        values.Add(currentValue);


        return values.ToArray();
    }



    //==================================================
    // 스킵 기능
    //==================================================

    private void OpenSkipConfirm()
    {
        skipConfirmPanel.SetActive(true);
    }



    private void CancelSkip()
    {
        skipConfirmPanel.SetActive(false);
    }



    private void SkipPrologue()
    {
        SceneManager.LoadScene("Tutorial_0");
    }



    //==================================================
    // 현재 데이터 표시
    //==================================================

    private void ShowDialogue(int index)
    {
        if (index < 0 ||
            index >= dialogues.Count)
        {
            return;
        }


        PrologueDialogue data =
            dialogues[index];


        //========================================
        // Flash
        //========================================

        if (data.type == "Flash")
        {
            StartCoroutine(PlayFlash());

            return;
        }


        //========================================
        // 배경 이미지
        //========================================

        // CSV에 배경이 적혀있을 경우에만 변경
        // 비어있으면 기존 배경 유지
        if (!string.IsNullOrEmpty(data.backgroundImage))
        {
            Sprite backgroundSprite =
                Resources.Load<Sprite>(
                    "Prologue/Backgrounds/"
                    + data.backgroundImage
                );


            if (backgroundSprite != null)
            {
                backgroundImage.sprite =
                    backgroundSprite;
            }
            else
            {
                Debug.LogWarning(
                    "배경 이미지를 찾을 수 없습니다: "
                    + data.backgroundImage
                );
            }
        }


        //========================================
        // Cutscene
        //========================================

        if (data.type == "Cutscene")
        {
            ShowCutscene(data);

            return;
        }


        //========================================
        // Dialogue
        //========================================

        ShowNormalDialogue(data);
    }



    //==================================================
    // 일반 대사 표시
    //==================================================

    private void ShowNormalDialogue(
        PrologueDialogue data)
    {
        // 컷씬 UI 숨김
        cutsceneImage.gameObject.SetActive(false);
        cutsceneTextPanel.SetActive(false);
        cutsceneText.gameObject.SetActive(false);

        // 대사 패널 표시
        dialoguePanel.SetActive(true);

        // 캐릭터 이름
        characterNameText.text =
            data.characterName;

        // 캐릭터 이미지
        if (!string.IsNullOrEmpty(data.characterImage))
        {
            Sprite characterSprite =
                Resources.Load<Sprite>(
                    "Prologue/Characters/"
                    + data.characterImage
                );

            if (characterSprite != null)
            {
                characterImage.sprite =
                    characterSprite;

                characterImage.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning(
                    "캐릭터 이미지를 찾을 수 없습니다: "
                    + data.characterImage
                );

                characterImage.gameObject.SetActive(false);
            }
        }
        else
        {
            characterImage.gameObject.SetActive(false);
        }

        // 대사
        currentFullText =
            data.dialogue;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine =
            StartCoroutine(TypeDialogue());
    }


    //==================================================
    // 컷씬 표시
    //==================================================
    private void ShowCutscene(PrologueDialogue data)
    {
        // 일반 대사 UI 숨김
        dialoguePanel.SetActive(false);

        // 기존 캐릭터 UI 숨김
        characterImage.gameObject.SetActive(false);
        characterNameText.gameObject.SetActive(false);
        dialogueText.gameObject.SetActive(false);

        // 컷씬 이미지 표시
        if (!string.IsNullOrEmpty(data.cutsceneImage))
        {
            Sprite sceneSprite =
                Resources.Load<Sprite>(
                    "Prologue/Cutscenes/" + data.cutsceneImage
                );

            if (sceneSprite != null)
            {
                cutsceneImage.sprite = sceneSprite;
                cutsceneImage.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning(
                    "컷씬 이미지를 찾을 수 없습니다: "
                    + data.cutsceneImage
                );

                cutsceneImage.gameObject.SetActive(false);
            }
        }
        else
        {
            cutsceneImage.gameObject.SetActive(false);
        }

        // 컷씬 텍스트 패널 표시
        cutsceneTextPanel.SetActive(true);
        cutsceneText.gameObject.SetActive(true);

        currentFullText = data.dialogue;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // 대사가 없으면 자동으로 다음 단계
        if (string.IsNullOrEmpty(currentFullText))
        {
            cutsceneText.text = "";

            StartCoroutine(WaitAndNextCutscene());

            return;
        }

        // 대사가 있으면 타이핑
        typingCoroutine =
            StartCoroutine(TypeCutsceneDialogue());
    }

    private IEnumerator WaitAndNextCutscene()
    {
        // 한 프레임 기다림
        yield return null;

        NextDialogue();
    }

    private IEnumerator TypeCutsceneDialogue()
    {
        isTyping = true;

        cutsceneText.text = "";

        // 마지막 대사인지 확인
        if (currentFullText == "나한테서.. 도망쳐..")
        {
            cutsceneText.color = Color.red;
        }
        else
        {
            cutsceneText.color = Color.white;
        }

        foreach (char c in currentFullText)
        {
            cutsceneText.text += c;

            yield return new WaitForSeconds(cutsceneTypeSpeed);
        }

        isTyping = false;

        typingCoroutine = null;
    }

    //==================================================
    // Flash 연출
    //==================================================
    private IEnumerator PlayFlash()
    {
        isFlashing = true;

        // 화면 흰색
        flashImage.gameObject.SetActive(true);

        // 번쩍이는 시간
        yield return new WaitForSeconds(flashDuration);

        // 흰색 제거
        flashImage.gameObject.SetActive(false);

        // 다음 플래시까지 기다리는 시간
        yield return new WaitForSeconds(currentFlashInterval);

        // 다음 플래시를 위해 간격 증가
        currentFlashInterval += flashIntervalIncrease;

        isFlashing = false;

        // 다음 CSV 데이터로 이동
        NextDialogue();
    }


    //==================================================
    // 타이핑 기능
    //==================================================
    private IEnumerator TypeDialogue()
    {
        isTyping = true;

        dialogueText.text = "";

        foreach (char c in currentFullText)
        {
            dialogueText.text += c;

            yield return new WaitForSeconds(
                typeSpeed
            );
        }

        isTyping = false;

        typingCoroutine = null;
    }


    //==================================================
    // 타이핑 중 Enter → 즉시 완성
    //==================================================
    private void CompleteDialogue()
    {
        if (!isTyping)
            return;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);

            typingCoroutine = null;
        }


        dialogueText.text =
            currentFullText;


        isTyping = false;
    }

    //==================================================
    // 다음 대사
    //==================================================
    private void NextDialogue()
    {
        currentDialogueIndex++;

        // 마지막 데이터가 끝났을 경우
        if (currentDialogueIndex >= dialogues.Count)
        {
            EndPrologue();
            return;
        }

        ShowDialogue(
            currentDialogueIndex
        );
    }

    //==================================================
    // 프롤로그 종료
    //==================================================
    private void EndPrologue()
    {
        Debug.Log("프롤로그 종료");
        StartCoroutine(EndPrologueCoroutine());
    }

    private IEnumerator EndPrologueCoroutine()
    {
        // 화면을 검게 페이드 아웃
        yield return StartCoroutine(fadeManager.FadeOut());
        // 다음 씬으로 이동
        SceneManager.LoadScene("Tutorial_0");
    }
}