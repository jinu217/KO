using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

// - 타이틀 메뉴 입력 처리 및 선택
// - 새 게임 시작, 저장 데이터 불러오기
// - 옵션 메뉴, 로드 메뉴 열기/닫기
// - 저장 슬롯 표시 및 게임 로드

// [조작키]
// ↑ / ↓      : 메뉴
// Enter / Z  : 선택
// Esc / X    : 뒤로가기 (로드, 옵션)
// ← / →      : 슬롯 이동

public class TitleMenuController : MonoBehaviour
{
    [Header("Main Menu Choice")]
    public GameObject[] menuChoices;     // Start, Load, Setting, Quit의 선택 이미지

    [Header("Load Slot Text")]
    public TMP_Text[] playTimeTexts;
    public TMP_Text[] deathTexts;

    [Header("Save Slot UI")]
    public SaveSlotUI[] saveSlotUI;

    [Header("Panels")]
    public FadeManager fadeManager;
    public GameObject mainMenu;
    public GameObject settingPanel;
    public GameObject loadPanel;

    [Header("Title")]
    public GameObject Titletext;

    public SettingController optionController;

    private int index = 0;

    private bool isSelecting = false;
    private bool isOptionOpen = false;
    private bool isLoadOpen = false;

    //============== 초기화 ==================
    IEnumerator Start()
    {
        settingPanel.SetActive(false);
        loadPanel.SetActive(false);
        mainMenu.SetActive(true);

        UpdateLoadSlots();

        // 모든 선택 이미지 OFF
        foreach (GameObject choice in menuChoices)
        {
            choice.SetActive(false);
        }

        yield return null;
        UpdateSelection();
    }

    //============== 메인 메뉴 입력 ==================
    void Update()
    {
        if (isSelecting) return;
        if (isOptionOpen) return;

        // Load 메뉴가 열려있을 때
        if (isLoadOpen)
        {
            HandleLoadInput();
            return;
        }

        // 위
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            index--;

            if (index < 0)
                index = menuChoices.Length - 1;

            UpdateSelection();
        }

        // 아래
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            index++;

            if (index >= menuChoices.Length)
                index = 0;

            UpdateSelection();
        }

        // 선택
        if (Input.GetKeyDown(KeyCode.Return))
        {
            StartCoroutine(SelectMenu());
        }
    }

    //============== 메인 메뉴 ==================
    void UpdateSelection()
    {
        Debug.Log($"UpdateSelection : {index}");
        // 현재 선택된 메뉴 표시
        for (int i = 0; i < menuChoices.Length; i++)
        {
            menuChoices[i].SetActive(i == index);
        }
    }

    // 타이틀 선택지 선택시 동작
    IEnumerator SelectMenu()
    {
        isSelecting = true;

        switch (index)
        {
            case 0:     // START
                int newGameSlot = Mathf.Max(1, SaveData.Instance.currentSlot);
                SaveSystem.Delete(newGameSlot);
                SaveData.Instance.ResetForNewGame(newGameSlot);
                yield return StartCoroutine(fadeManager.FadeOut());     // 화면 페이드 아웃
                SceneManager.LoadScene("Prologue");     // 프롤로그 시작
                break;

            case 1:     // LOAD
                OpenLoadMenu();
                break;

            case 2:     // OPTION
                OpenSetting();
                break;

            case 3:     // QUIT
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #else
                    Application.Quit();
                #endif
                    break;
        }

        isSelecting = false;
    }

    //============== 로드 메뉴 ==================
    void UpdateLoadSlots()
    {
        for (int i = 0; i < playTimeTexts.Length; i++)
        {
            int slot = i + 1;
            SaveFile file = SaveSystem.Load(slot);

            if (file != null)
            {
                float playTime = file.persistent.playTime;
                int deaths = file.persistent.deathCount;

                int hour = (int)(playTime / 3600);
                int minute = (int)((playTime % 3600) / 60);
                int second = (int)(playTime % 60);

                playTimeTexts[i].text = $"PLAY TIME\n{hour:00}:{minute:00}:{second:00}";
                deathTexts[i].text = $"DEATHS\n{deaths}";
            }
            else
            {
                playTimeTexts[i].text = "";
                deathTexts[i].text = "EMPTY";
            }
        }
    }

    void UpdateLoadSelection()
    {
        for (int i = 0; i < saveSlotUI.Length; i++)
        {
            saveSlotUI[i].SetSelected(i == index);
        }
    }

    void OpenLoadMenu()
    {
        isLoadOpen = true;

        // 다른 패널 숨기기 (투구, 타이틀제목)
        mainMenu.SetActive(false);
        Titletext.SetActive(false);
        
        loadPanel.SetActive(true);

        index = 0;
        UpdateLoadSelection();
    }

    void HandleLoadInput()
    {
        // 로드 메뉴 입력 처리
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            index--;

            if (index < 0)
                index = deathTexts.Length - 1;

            UpdateLoadSelection();
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            index++;

            if (index >= deathTexts.Length)
                index = 0;

            UpdateLoadSelection();
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            LoadGame(index + 1);
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseLoadMenu();
        }
    }

    void CloseLoadMenu()
    {
        isLoadOpen = false;

        loadPanel.SetActive(false);
        
        // 숨긴 패널 표시 (투구, 타이틀제목)
        Titletext.SetActive(true);
        mainMenu.SetActive(true);

        index = 0;
        UpdateLoadSelection();
        UpdateSelection();
    }

    //============== 설정 메뉴 ==================
    void OpenSetting()
    {
        isOptionOpen = true;

        mainMenu.SetActive(false);

        if (optionController != null)
        {
            optionController.OpenPanel();
            optionController.ResetSelection();
        }

        isSelecting = false;
    }

    public void CloseSetting()
    {
        isOptionOpen = false;

        if (optionController != null)
            optionController.ClosePanel();

        mainMenu.SetActive(true);

        index = 0;
        UpdateSelection();

        isSelecting = false;
    }

    //============== 게임 로드 ==================
    void LoadGame(int slot)
    {
        if (!SaveManager.Instance.HasSave(slot))
        {
            Debug.Log("저장 데이터가 없습니다.");
            return;
        }

        SaveData.Instance.currentSlot = slot;

        SaveManager.Instance.Load(slot);

        StartCoroutine(LoadScene());
    }

    IEnumerator LoadScene()
    {
        yield return fadeManager.FadeOut();
        SceneManager.LoadScene(SaveData.Instance.persistent.nextStage);
    }
}
