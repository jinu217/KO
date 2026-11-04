using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

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
    [SerializeField] private GameObject settingBackground;

    [Header("Category Select Images")]
    public GameObject[] categorySelectImages;    // 카테고리 3개 (Audio, Display, Controls)

    [Header("Panel Select Cursor")]
    public RectTransform selectCursor;
    [Header("Cursor Offset")]
    public Vector2 cursorOffset;

    [Header("Audio Cursor Target")]
    public RectTransform[] audioCursorTargets;
    [Header("Display Cursor Target")]
    public RectTransform[] displayCursorTargets;

    //============== Display 화살표 ==============
    [Header("Display Arrows (항목 순서: 0 = 해상도, 1 = 디스플레이 모드)")]
    public Image[] displayLeftArrows;
    public Image[] displayRightArrows;
    [SerializeField] private Color arrowActiveColor = Color.white;                   // 변경 가능 (불투명)
    [SerializeField] private Color arrowDisabledColor = new Color(1f, 1f, 1f, 0.15f); // 변경 불가 (거의 투명)
    //============== Display 화살표 끝 ==============

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

    [Header("Managers")]
    public SettingManager settingManager;
    public TitleMenuController titleMenuController;
    public PauseManager pauseManager;

    //====================================================
    // STATE
    //====================================================

    private int categoryIndex = 0;
    private int contentIndex = 0;

    private enum MenuState { Category, Content }
    private MenuState currentState = MenuState.Category;
    private bool ignoreInput;

    //=================== 설정창 슬라이드로 열고 닫기 ======================
    [Header("Slide Animation")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private Vector2 openPosition = Vector2.zero;
    [SerializeField] private Vector2 closedPosition = new Vector2(1200f, 0f);
    [SerializeField] private float slideSpeed = 12f;
    private Vector2 targetPosition;

    //====================================================
    // INIT
    //====================================================

    void Start()
    {
        // 설정창 열고 닫기
        panelRect.anchoredPosition = closedPosition;
        targetPosition = closedPosition;

        UpdatePanel();
        UpdateSelection();
    }

    void Update()
    {
        if (!settingPanel.activeSelf)
            return;

        if (ignoreInput)
        {
            ignoreInput = false;
            return;
        }

        HandleInput();

        // 설정창 열고 닫기
        panelRect.anchoredPosition = Vector2.Lerp(
            panelRect.anchoredPosition,
            targetPosition,
            slideSpeed * Time.unscaledDeltaTime
        );
        if (settingPanel.activeSelf &&
            Vector2.Distance(panelRect.anchoredPosition, closedPosition) < 5f &&
            targetPosition == closedPosition)
        {
            settingPanel.SetActive(false);
        }
    }
    // ==== 설정창 열기 ====
    public void OpenPanel()
    {
        // 검은 뒷배경은 즉시 표시
        settingBackground.SetActive(true);

        // 설정창 활성화
        settingPanel.SetActive(true);

        // 설정창은 오른쪽에서 시작
        panelRect.anchoredPosition = closedPosition;
        targetPosition = openPosition;

        ignoreInput = true;

        // 저장된 값이 불러와진 상태일 수 있으므로 열 때 화살표 상태 갱신
        UpdateDisplayArrows();
    }

    // ==== 설정창 닫기 ====
    public void ClosePanel()
    {
        targetPosition = closedPosition;

        StopAllCoroutines();
        StartCoroutine(CloseAfterSlide());
    }

    IEnumerator CloseAfterSlide()
    {
        yield return new WaitForSecondsRealtime(0.25f);

        // 설정창 닫기
        settingPanel.SetActive(false);

        // 검은 뒷배경도 닫기
        settingBackground.SetActive(false);
    }

    //====================================================
    // PANEL CONTROL
    //====================================================

    void UpdatePanel()
    {
        bool inContent = currentState == MenuState.Content;

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
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.F)) Confirm();
        if (Input.GetKeyDown(KeyCode.Escape)) Cancel();
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
            UpdatePanel();      // 카테고리 이동 시 패널 미리보기
            contentIndex = 0;       // 패널이면 Content 선택 초기화
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
                break;

            case 1:
                bgmSlider.value = Adjust(bgmSlider.value, dir);
                settingManager.SetBGM(bgmSlider.value);
                break;

            case 2:
                sfxSlider.value = Adjust(sfxSlider.value, dir);
                settingManager.SetSFX(sfxSlider.value);
                break;
        }
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

        // 값이 바뀐 직후 화살표 밝기 갱신
        UpdateDisplayArrows();
    }

    float Adjust(float value, int dir) => Mathf.Clamp01(value + dir * 0.1f);

    //============== Display 화살표 갱신 ==============
    void UpdateDisplayArrows()
    {
        SetArrowState(0, resolutionDropdown.value, resolutionDropdown.options.Count - 1);
        SetArrowState(1, displayModeDropdown.value, 2);   // ChangeDisplay의 상한(2)과 동일
    }

    void SetArrowState(int index, int value, int maxValue)
    {
        if (displayLeftArrows != null && index < displayLeftArrows.Length && displayLeftArrows[index] != null)
            displayLeftArrows[index].color = value > 0 ? arrowActiveColor : arrowDisabledColor;

        if (displayRightArrows != null && index < displayRightArrows.Length && displayRightArrows[index] != null)
            displayRightArrows[index].color = value < maxValue ? arrowActiveColor : arrowDisabledColor;
    }
    //============== Display 화살표 갱신 끝 ==============

    //====================================================
    // CONFIRM: 카테고리 -> 패널 안으로 진입
    //====================================================

    void Confirm()
    {
        Debug.Log("Confirm 호출");
        if(currentState == MenuState.Category)
        {
            //============== Controls 카테고리는 진입 불가 ==============
            // Controls(2)는 보기 전용 패널이라 Content로 들어가지 않음
            if (categoryIndex == 2)
                return;
            //============== Controls 카테고리는 진입 불가 끝 ==============

            // 이미 미리보기 상태라 바로 조작 상태로 변경
            currentState = MenuState.Content;
            contentIndex = 0;

            UpdateSelection();
        }
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
            UpdatePanel();
            UpdateSelection();
            return;
        }

        // 카테고리 단계였으면 옵션창 전체 닫기
        PlayerPrefs.Save();

        if (pauseManager != null && pauseManager.IsPaused)
        {
            pauseManager.CloseSetting();
        }
        else if (titleMenuController != null)
        {
            titleMenuController.CloseSetting();
        }

        ResetSelection();
    }

    // ============= 선택 표시 =======================
    void MoveSelectCursor(RectTransform target)
    {
        if(selectCursor == null || target == null)
            return;

        selectCursor.position = target.position + 
            new Vector3(cursorOffset.x, cursorOffset.y, 0);
    }

    //====================================================
    // SELECTION UI
    //====================================================

    void UpdateSelection()
    {
        // 패널 안에서만 손가락 표시
        if(selectCursor != null)
        {
            bool showCursor =
                currentState == MenuState.Content &&
                categoryIndex != 2;         //컨트롤패널에선 숨기기

            selectCursor.gameObject.SetActive(showCursor);
        }

        // 카테고리 선택 이미지
        for(int i = 0; i < categorySelectImages.Length; i++)
        {
            categorySelectImages[i].SetActive(
                categoryIndex == i
            );
        }

        // 패널 내부 선택
        if(currentState == MenuState.Content)
        {
            switch(categoryIndex)
            {
                case 0:
                    MoveSelectCursor(audioCursorTargets[contentIndex]);
                    break;

                case 1:
                    MoveSelectCursor(displayCursorTargets[contentIndex]);
                    break;
            }
        }

        // Display 화살표 상태도 같이 갱신
        UpdateDisplayArrows();
    }

    int GetContentCount()
    {
        return categoryIndex switch
        {
            0 => audioCursorTargets.Length,
            1 => displayCursorTargets.Length,
            2 => 1,
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