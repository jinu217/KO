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
    public GameObject[] menuChoices;     // Start, Load, Setting, Exit의 선택 이미지

    [Header("Load Menu")]
    public TMP_Text[] loadSlots;         // 로드 슬롯 텍스트

    public Outline[] slotOutlines;

    public FadeManager fadeManager;
    public GameObject mainMenu;
    public GameObject settingPanel;
    public GameObject loadPanel;

    public OptionController optionController;

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
                yield return StartCoroutine(fadeManager.FadeOut());
                SceneManager.LoadScene("Stage1-1");
                break;

            case 1:     // LOAD
                OpenLoadMenu();
                break;

            case 2:     // OPTION
                OpenOption();
                break;

            case 3:     // EXIT
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
        // 저장 슬롯 정보 표시
        for (int i = 0; i < loadSlots.Length; i++)
        {
            int slot = i + 1;

            if (PlayerPrefs.HasKey($"SaveSlot{slot}_PlayTime"))
            {
                float playTime = PlayerPrefs.GetFloat($"SaveSlot{slot}_PlayTime");

                int hour = (int)(playTime / 3600);
                int minute = (int)((playTime % 3600) / 60);
                int second = (int)(playTime % 60);

                loadSlots[i].text =
                    $"PLAY TIME\n{hour:00}:{minute:00}:{second:00}";

                // 저장 데이터 있음
                slotOutlines[i].effectColor = Color.blue;
            }
            else
            {
                loadSlots[i].text = "EMPTY";

                // 저장 데이터 없음
                slotOutlines[i].effectColor = Color.yellow;
            }
        }
    }

    void OpenLoadMenu()
    {
        isLoadOpen = true;

        mainMenu.SetActive(false);
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
                index = loadSlots.Length - 1;

            UpdateLoadSelection();
        }

        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            index++;

            if (index >= loadSlots.Length)
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

    void UpdateLoadSelection()
    {
        // 선택된 슬롯 강조
        for (int i = 0; i < loadSlots.Length; i++)
        {
            bool selected = (i == index);

            if (selected)
            {
                slotOutlines[i].effectDistance = new Vector2(4, -4);
            }
            else
            {
                slotOutlines[i].effectDistance = new Vector2(2, -2);
            }
        }
    }

    void CloseLoadMenu()
    {
        isLoadOpen = false;

        loadPanel.SetActive(false);
        mainMenu.SetActive(true);

        index = 0;
        UpdateSelection();
    }

    //============== 옵션 메뉴 ==================
    void OpenOption()
    {
        isOptionOpen = true;

        mainMenu.SetActive(false);
        settingPanel.SetActive(true);

        if (optionController != null)
            optionController.ResetSelection();

        isSelecting = false;
    }

    public void CloseOption()
    {
        isOptionOpen = false;

        settingPanel.SetActive(false);
        mainMenu.SetActive(true);

        index = 0;
        UpdateSelection();

        isSelecting = false;
    }

    //============== 게임 로드 ==================
    void LoadGame(int slot)
    {
        // 저장 데이터 불러오기
        float hp = PlayerPrefs.GetFloat($"SaveSlot{slot}_HP", 100f);

        float x = PlayerPrefs.GetFloat($"SaveSlot{slot}_PosX", 0);
        float y = PlayerPrefs.GetFloat($"SaveSlot{slot}_PosY", 0);
        float z = PlayerPrefs.GetFloat($"SaveSlot{slot}_PosZ", 0);

        GameData.Instance.hp = hp;
        GameData.Instance.spawnPosition = new Vector3(x, y, z);

        StartCoroutine(LoadScene());
    }

    IEnumerator LoadScene()
    {
        yield return fadeManager.FadeOut();
        SceneManager.LoadScene("Stage1-1");
    }
}