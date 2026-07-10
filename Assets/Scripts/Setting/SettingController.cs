using UnityEngine;
using TMPro;
using UnityEngine.UI;

// =====================================================
// OptionController
// - 옵션 메뉴 입력 처리 및 UI 네비게이션
// - 카테고리 / 항목 
// - SettingManager와 연결하여 값 변경 요청
// =====================================================

public class SettingController : MonoBehaviour
{
    //====================================================
    // UI REFERENCES
    //====================================================

    [Header("Category Texts")]
    public TMP_Text[] categoryTexts;      // 카테고리 3개 (Audio, Display, Controls)

    [Header("Audio Panel Texts")]
    public TMP_Text[] audioTexts;         // Master, BGM, SFX (3개)

    [Header("Display Panel Texts")]
    public TMP_Text[] displayTexts;       // Resolution, DisplayMode (2개)

    [Header("Panels")]
    public GameObject audioPanel;
    public GameObject displayPanel;
    public GameObject controlsPanel;
    public GameObject settingPanel;

    [Header("UI Controls")]
    public Slider masterSlider;
    public Slider bgmSlider;
    public Slider sfxSlider;

    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown displayModeDropdown;

    [Header("Volume Percent Texts")]
    public TMP_Text masterPercentText;
    public TMP_Text bgmPercentText;
    public TMP_Text sfxPercentText;

    [Header("Display Arrow Indicators")]
    public GameObject[] displayLeftArrows;   // 0: Resolution, 1: DisplayMode
    public GameObject[] displayRightArrows;  // 0: Resolution, 1: DisplayMode

    [Header("Managers")]
    public SettingManager settingManager;
    public TitleMenuController titleMenuController;

    //====================================================
    // STATE
    //====================================================

    private int categoryIndex = 0;
    private int contentIndex = 0;

    private enum MenuState { Category, Content }
    private MenuState currentState = MenuState.Category;

    //====================================================
    // INIT
    //====================================================

    void Start()
    {
        UpdatePanel();
        UpdateSelection();

        UpdateVolumeText(masterPercentText, masterSlider.value);
        UpdateVolumeText(bgmPercentText, bgmSlider.value);
        UpdateVolumeText(sfxPercentText, sfxSlider.value);
    }

    void Update()
    {
        if (!settingPanel.activeSelf)
            return;

        HandleInput();
    }

    //====================================================
    // PANEL CONTROL
    //====================================================

    void UpdatePanel()
    {
        audioPanel.SetActive(categoryIndex == 0);
        displayPanel.SetActive(categoryIndex == 1);
        controlsPanel.SetActive(categoryIndex == 2);
    }

    //====================================================
    // INPUT
    //====================================================

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow)) Move(-1);
        if (Input.GetKeyDown(KeyCode.DownArrow)) Move(1);
        if (Input.GetKeyDown(KeyCode.LeftArrow)) ChangeValue(-1);
        if (Input.GetKeyDown(KeyCode.RightArrow)) ChangeValue(1);
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Z)) Confirm();
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.X)) Cancel();
    }

    //====================================================
    // MOVE
    //====================================================

    void Move(int dir)
    {
        switch (currentState)
        {
            case MenuState.Category:
                categoryIndex = Mathf.Clamp(categoryIndex + dir, 0, 2);
                UpdatePanel();          // 카테고리 이동 시 패널도 즉시 전환
                break;

            case MenuState.Content:
                int count = GetContentCount();
                if (count > 0)
                    contentIndex = Mathf.Clamp(contentIndex + dir, 0, count - 1);
                break;
        }

        UpdateSelection();
    }

    //====================================================
    // VALUE CHANGE (기존과 동일)
    //====================================================

    void ChangeValue(int dir)
    {
        if (currentState != MenuState.Content) return;

        switch (categoryIndex)
        {
            case 0: ChangeAudio(dir); break;
            case 1: ChangeDisplay(dir); break;
        }
    }

    void ChangeAudio(int dir)
    {
        switch (contentIndex)
        {
            case 0:
                masterSlider.value = Adjust(masterSlider.value, dir);
                settingManager.SetMaster(masterSlider.value);
                UpdateVolumeText(masterPercentText, masterSlider.value);
                break;

            case 1:
                bgmSlider.value = Adjust(bgmSlider.value, dir);
                settingManager.SetBGM(bgmSlider.value);
                UpdateVolumeText(bgmPercentText, bgmSlider.value);
                break;

            case 2:
                sfxSlider.value = Adjust(sfxSlider.value, dir);
                settingManager.SetSFX(sfxSlider.value);
                UpdateVolumeText(sfxPercentText, sfxSlider.value);
                break;
        }
    }

    void UpdateVolumeText(TMP_Text text, float value)
    {
        if (text == null) return;
        text.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    void ChangeDisplay(int dir)
    {
        switch (contentIndex)
        {
            case 0:
                resolutionDropdown.value = Mathf.Clamp(resolutionDropdown.value + dir, 0, resolutionDropdown.options.Count - 1);
                settingManager.SetResolution(resolutionDropdown.value);
                break;
            case 1:
                displayModeDropdown.value = Mathf.Clamp(displayModeDropdown.value + dir, 0, 2);
                settingManager.SetDisplayMode(displayModeDropdown.value);
                break;
        }

        UpdateDisplayArrows();
    }

    float Adjust(float value, int dir) => Mathf.Clamp01(value + dir * 0.1f);

    //====================================================
    // CONFIRM: 카테고리 -> 패널 안으로 진입
    //====================================================

    void Confirm()
    {
        if (currentState == MenuState.Category)
        {
            // Controls처럼 내용이 없는 카테고리는 진입 막기
            if (GetContentCount() == 0)
                return;

            currentState = MenuState.Content;
            contentIndex = 0;
            UpdateSelection();
        }
        // Content 상태에서 Confirm은 슬라이더/드롭다운 조작이라 별도 처리 없음
    }

    //====================================================
    // CANCEL: 계층적으로 뒤로가기
    //====================================================

    void Cancel()
    {
        if (currentState == MenuState.Content)
        {
            // 패널 안에 있었으면 카테고리 선택으로만 되돌아감
            currentState = MenuState.Category;
            contentIndex = 0;
            UpdateSelection();
            return;
        }

        // 카테고리 단계였으면 옵션창 전체 닫기
        PlayerPrefs.Save();
        titleMenuController.CloseOption();

        categoryIndex = 0;
        contentIndex = 0;
        UpdatePanel();
        UpdateSelection();
    }

    //====================================================
    // SELECTION UI
    //====================================================

    void UpdateSelection()
    {
        UpdateDisplayArrows();
    }

    void UpdateDisplayArrows()
    {
        bool inDisplayContent = (categoryIndex == 1) && (currentState == MenuState.Content);

        for (int i = 0; i < displayLeftArrows.Length; i++)
        {
            bool show = inDisplayContent && i == contentIndex;

            if (displayLeftArrows[i] != null)
                displayLeftArrows[i].SetActive(show && CanDecrease(i));

            if (displayRightArrows[i] != null)
                displayRightArrows[i].SetActive(show && CanIncrease(i));
        }
    }

    bool CanDecrease(int index)
    {
        return index switch
        {
            0 => resolutionDropdown.value > 0,
            1 => displayModeDropdown.value > 0,
            _ => false
        };
    }

    bool CanIncrease(int index)
    {
        return index switch
        {
            0 => resolutionDropdown.value < resolutionDropdown.options.Count - 1,
            1 => displayModeDropdown.value < 2,
            _ => false
        };
    }

    int GetContentCount()
    {
        return categoryIndex switch
        {
            0 => audioTexts.Length,
            1 => displayTexts.Length,
            2 => 0,
            _ => 0
        };
    }

    //====================================================
    // RESET
    //====================================================

    public void ResetSelection()
    {
        categoryIndex = 0;
        contentIndex = 0;
        currentState = MenuState.Category;

        UpdatePanel();
        UpdateSelection();
    }
}