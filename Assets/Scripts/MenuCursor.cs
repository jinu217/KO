using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class MenuCursor : MonoBehaviour
{
    // 🔥 커서 하나
    public RectTransform cursor;

    public Vector2[] cursorOffsets;

    public TMP_Text[] texts;          // 메인 메뉴
    public TMP_Text[] loadSlots;      // 🔥 로드 메뉴 추가

    public FadeManager fadeManager;

    public GameObject mainMenu;
    public GameObject optionPanel;
    public GameObject loadPanel;      // 🔥 추가

    public OptionCursor optionCursor;

    private int index = 0;

    private bool isSelecting = false;
    private bool isOptionOpen = false;
    private bool isLoadOpen = false;  // 🔥 추가

    IEnumerator Start()
    {
        optionPanel.SetActive(false);
        loadPanel.SetActive(false);
        mainMenu.SetActive(true);

        yield return null;
        UpdateSelection();
    }

    void Update()
    {
        if (isSelecting) return;
        if (isOptionOpen) return;

        // 🔥 로드 메뉴 상태면 여기로 빠짐
        if (isLoadOpen)
        {
            HandleLoadInput();
            return;
        }

        // 🔼 위
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            index--;
            if (index < 0)
                index = texts.Length - 1;

            UpdateSelection();
        }

        // 🔽 아래
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            index++;
            if (index >= texts.Length)
                index = 0;

            UpdateSelection();
        }

        // ⏎ 선택
        if (Input.GetKeyDown(KeyCode.Return))
        {
            StartCoroutine(SelectMenu());
        }
    }

    void UpdateSelection()
    {
        for (int i = 0; i < texts.Length; i++)
        {
            texts[i].color = (i == index) ? Color.yellow : Color.white;
        }

        cursor.position =
            texts[index].rectTransform.position +
            (Vector3)cursorOffsets[index];
    }

    IEnumerator SelectMenu()
    {
        isSelecting = true;

        switch (index)
        {
            case 0: // START
                yield return StartCoroutine(fadeManager.FadeOut());
                SceneManager.LoadScene("SampleScene");
                break;

            case 1: // LOAD (ROAD GAME)
                OpenLoadMenu();
                break;

            case 2: // OPTION
                OpenOption();
                break;

            case 3: // QUIT
                Application.Quit();
                break;
        }

        isSelecting = false;
    }

    // 🔥 OPTION
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

    // 🔥 LOAD MENU
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
            loadSlots[i].color = (i == index) ? Color.yellow : Color.white;
        }

        cursor.position =
            loadSlots[index].rectTransform.position +
            (Vector3)cursorOffsets[index];
    }

    void CloseLoadMenu()
    {
        isLoadOpen = false;
        loadPanel.SetActive(false);
        mainMenu.SetActive(true);

        index = 0;
        UpdateSelection();
    }

    // 🔥 LOAD GAME
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