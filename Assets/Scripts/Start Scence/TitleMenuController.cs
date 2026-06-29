using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class TitleMenuController : MonoBehaviour
{
    [Header("Main Menu Choice")]
    public GameObject[] menuChoices;     // Start, Load, Setting, Exit의 선택 이미지

    [Header("Load Menu")]
    public TMP_Text[] loadSlots;         // 로드 슬롯 텍스트

    public FadeManager fadeManager;
    public GameObject mainMenu;
    public GameObject optionPanel;
    public GameObject loadPanel;

    public OptionCursor optionCursor;

    private int index = 0;

    private bool isSelecting = false;
    private bool isOptionOpen = false;
    private bool isLoadOpen = false;

    IEnumerator Start()
    {
        //optionPanel.SetActive(false);
        //loadPanel.SetActive(false);
        //mainMenu.SetActive(true);

        //UpdateLoadSlots();

        // 모든 선택 이미지 OFF
        foreach (GameObject choice in menuChoices)
        {
            choice.SetActive(false);
        }

        yield return null;
        UpdateSelection();
    }

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

    void UpdateSelection()
    {
        for (int i = 0; i < menuChoices.Length; i++)
        {
            menuChoices[i].SetActive(i == index);
        }
    }

    IEnumerator SelectMenu()
    {

        Debug.Log($"선택한 메뉴 : {index}");
        yield break;
        
        /*
        isSelecting = true;

        switch (index)
        {
            case 0:     // START
                yield return StartCoroutine(fadeManager.FadeOut());
                SceneManager.LoadScene("SampleScene");
                break;

            case 1:     // LOAD
                OpenLoadMenu();
                break;

            case 2:     // OPTION
                OpenOption();
                break;

            case 3:     // EXIT
                Application.Quit();
                break;
                
        }*/

        isSelecting = false;
    }

    void UpdateLoadSlots()
    {
        for (int i = 0; i < loadSlots.Length; i++)
        {
            int slot = i + 1;

            if (PlayerPrefs.HasKey($"SaveSlot{slot}_Level"))
            {
                int level = PlayerPrefs.GetInt($"SaveSlot{slot}_Level");
                float hp = PlayerPrefs.GetFloat($"SaveSlot{slot}_HP");

                loadSlots[i].text =
                    $"SLOT {slot}\n" +
                    $"LV {level}\n" +
                    $"HP {hp}";
            }
            else
            {
                loadSlots[i].text =
                    $"SLOT {slot}\nEMPTY";
            }
        }
    }

    // OPTION
    void OpenOption()
    {
        isOptionOpen = true;

        mainMenu.SetActive(false);
        optionPanel.SetActive(true);

        if (optionCursor != null)
            optionCursor.ResetSelection();

        isSelecting = false;
    }

    public void CloseOption()
    {
        isOptionOpen = false;

        optionPanel.SetActive(false);
        mainMenu.SetActive(true);

        index = 0;
        UpdateSelection();

        isSelecting = false;
    }

    // LOAD MENU
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
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            index--;

            if (index < 0)
                index = loadSlots.Length - 1;

            UpdateLoadSelection();
        }

        if (Input.GetKeyDown(KeyCode.DownArrow))
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
        for (int i = 0; i < loadSlots.Length; i++)
        {
            loadSlots[i].color =
                (i == index) ? Color.yellow : Color.white;
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

    void LoadGame(int slot)
    {
        int level = PlayerPrefs.GetInt($"SaveSlot{slot}_Level", 1);
        float hp = PlayerPrefs.GetFloat($"SaveSlot{slot}_HP", 100f);

        float x = PlayerPrefs.GetFloat($"SaveSlot{slot}_PosX", 0);
        float y = PlayerPrefs.GetFloat($"SaveSlot{slot}_PosY", 0);
        float z = PlayerPrefs.GetFloat($"SaveSlot{slot}_PosZ", 0);

        GameData.Instance.level = level;
        GameData.Instance.hp = hp;
        GameData.Instance.spawnPosition = new Vector3(x, y, z);

        StartCoroutine(LoadScene());
    }

    IEnumerator LoadScene()
    {
        yield return fadeManager.FadeOut();
        SceneManager.LoadScene("SampleScene");
    }
}