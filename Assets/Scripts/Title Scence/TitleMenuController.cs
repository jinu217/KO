using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

// - 타이틀 메뉴 입력 처리 및 선택
// - 새 게임 시작
// - 저장 데이터 불러오기 및 삭제
// - 옵션 메뉴, 로드 메뉴 열기/닫기
// - 저장 슬롯 표시

// [조작키]
// ↑ / ↓      : 메인 메뉴 이동
// Enter / F  : 선택
// Esc        : 뒤로가기 (로드, 옵션, 확인창)
// ← / →      : 슬롯 이동
// Delete     : 저장 데이터 삭제

public class TitleMenuController : MonoBehaviour
{
    //==================================================ㄹ
    // 열거형
    //==================================================

    private enum ConfirmationType
    {
        None,
        Delete,
        NewGame
    }

    //==================================================
    // Inspector
    //==================================================

    [Header("Main Menu Choice")]
    public GameObject[] menuChoices;

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

    [Header("Confirmation")]
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private TMP_Text confirmationText;
    [SerializeField] private GameObject pressEnterText;

    [Header("Title")]
    public GameObject Titletext;

    [Header("Option")]
    public SettingController optionController;

    //==================================================
    // 상태 변수
    //==================================================

    private AudioClip playingTitleBGM;
    private int index = 0;
    private int lastMenuSoundIndex = -1;
    private int lastSlotSoundIndex = -1;

    private bool isSelecting = false;
    private bool isOptionOpen = false;
    private bool isLoadOpen = false;

    // true  : START → 새 게임 슬롯 선택
    // false : LOAD → 저장 데이터 불러오기
    private bool isNewGameMode = false;

    // 확인창이 열려있는지
    private bool isConfirmationOpen = false;

    // 현재 확인창의 용도
    private ConfirmationType confirmationType = ConfirmationType.None;

    // 삭제 대상 슬롯
    private int deleteTargetSlot = -1;

    // 새 게임으로 덮어쓸 슬롯
    private int newGameTargetSlot = -1;

    // 깜빡임
    private Coroutine blinkCoroutine;


    //==================================================
    // 초기화
    //==================================================

    IEnumerator Start()
    {
        settingPanel.SetActive(false);
        loadPanel.SetActive(false);
        confirmationPanel.SetActive(false);
        pressEnterText.SetActive(false);
        mainMenu.SetActive(true);

        UpdateLoadSlots();

        // 모든 선택 이미지 OFF
        foreach (GameObject choice in menuChoices)
        {
            choice.SetActive(false);
        }

        yield return null;

        var audioLibrary = Resources.Load<UIAudioLibrary>("Audio/UIAudioLibrary");
        if (AudioManager.Instance != null && audioLibrary != null && audioLibrary.titleBGM != null)
        {
            playingTitleBGM = audioLibrary.titleBGM;
            AudioManager.Instance.PlayBGM(playingTitleBGM);
        }
        UpdateSelection();
    }


    //==================================================
    // 메인 메뉴 입력
    //==================================================

    private void OnDestroy()
    {
        var audio = AudioManager.Instance;
        if (playingTitleBGM != null && audio != null && audio.bgmSource != null &&
            audio.bgmSource.clip == playingTitleBGM)
            audio.StopBGM();
    }

    void Update()
    {
        if (isSelecting) return;
        if (isOptionOpen) return;

        // 로드 / 새 게임 슬롯 메뉴
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
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.F))
        {
            SelectMenu();
        }
    }


    //==================================================
    // 메인 메뉴
    //==================================================

    void UpdateSelection()
    {
        if (lastMenuSoundIndex >= 0 && lastMenuSoundIndex != index)
            UISound.Play(UISoundId.TitleNavigate);
        lastMenuSoundIndex = index;
        Debug.Log($"UpdateSelection : {index}");

        for (int i = 0; i < menuChoices.Length; i++)
        {
            menuChoices[i].SetActive(i == index);
        }
    }

    void SelectMenu()
    {
        isSelecting = true;

        switch (index)
        {
            case 0:     // START
                isNewGameMode = true;
                OpenLoadMenu();
                break;

            case 1:     // LOAD
                isNewGameMode = false;
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


    //==================================================
    // 새 게임
    //==================================================

    // 슬롯 선택 후 새 게임 시작 여부 확인
    void StartNewGame(int slot)
    {
        // 빈 슬롯
        if (!SaveManager.Instance.HasSave(slot))
        {
            BeginNewGame(slot);
            return;
        }

        // 기존 저장 데이터가 있는 경우
        newGameTargetSlot = slot;

        OpenConfirmation(
            ConfirmationType.NewGame,
            $"SAVE{slot}의 기존 저장 데이터를 삭제하고\n새 게임을 시작하시겠습니까?\n\nYes(Enter/F)  /  No(Esc)"
        );
    }

    // 실제 새 게임 시작
    void BeginNewGame(int slot)
    {
        UISound.Play(UISoundId.SlotConfirm);
        Debug.Log($"새 게임 시작 - 슬롯 {slot}");

        SaveData.Instance.currentSlot = slot;

        // 새 게임 데이터 초기화
        SaveData.Instance.ResetForNewGame(slot);

        StartCoroutine(StartNewGameCoroutine());
    }

    IEnumerator StartNewGameCoroutine()
    {
        yield return StartCoroutine(fadeManager.FadeOut());

        SceneManager.LoadScene("Prologue");
    }


    //==================================================
    // 슬롯 메뉴
    //==================================================

    // 저장 슬롯 정보 갱신
    void UpdateLoadSlots()
    {
        for (int i = 0; i < playTimeTexts.Length; i++)
        {
            int slot = i + 1;

            SaveFile file = SaveSystem.Load(slot);

            bool hasSave = file != null;

            // 저장 데이터 존재 여부 전달
            if (i < saveSlotUI.Length)
            {
                saveSlotUI[i].SetHasSave(hasSave);
            }

            if (hasSave)
            {
                float playTime = file.persistent.playTime;
                int deaths = file.persistent.deathCount;

                int hour = (int)(playTime / 3600);
                int minute = (int)((playTime % 3600) / 60);
                int second = (int)(playTime % 60);

                playTimeTexts[i].text =
                    $"PLAY TIME\n{hour:00}:{minute:00}:{second:00}";

                deathTexts[i].text =
                    $"DEATHS {deaths}";
            }
            else
            {
                playTimeTexts[i].text = "";
                deathTexts[i].text = "EMPTY";
            }
        }
    }

    // 현재 선택된 슬롯 표시
    void UpdateLoadSelection()
    {
        if (isLoadOpen && lastSlotSoundIndex >= 0 && lastSlotSoundIndex != index)
            UISound.Play(UISoundId.SlotNavigate);
        lastSlotSoundIndex = index;
        for (int i = 0; i < saveSlotUI.Length; i++)
        {
            saveSlotUI[i].SetSelected(i == index, isNewGameMode);
        }
    }

    // 슬롯 메뉴 열기
    void OpenLoadMenu()
    {
        lastSlotSoundIndex = -1;
        isLoadOpen = true;

        mainMenu.SetActive(false);
        Titletext.SetActive(false);

        loadPanel.SetActive(true);

        // START일 때만 Press Enter 표시 + 깜빡임
        if (isNewGameMode)
        {
            pressEnterText.SetActive(true);

            if (blinkCoroutine != null)
                StopCoroutine(blinkCoroutine);

            blinkCoroutine = StartCoroutine(BlinkPressEnter());
        }
        else
        {
            pressEnterText.SetActive(false);
        }

        // 최신 저장 데이터 갱신
        UpdateLoadSlots();

        // 첫 번째 슬롯 선택
        index = 0;

        UpdateLoadSelection();
    }

    // 슬롯 메뉴 입력
    void HandleLoadInput()
    {
        // 확인창이 열려있으면 확인창만 처리
        if (isConfirmationOpen)
        {
            HandleConfirmationInput();
            return;
        }

        // 왼쪽 슬롯 이동
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            index--;

            if (index < 0)
                index = saveSlotUI.Length - 1;

            UpdateLoadSelection();
        }

        // 오른쪽 슬롯 이동
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            index++;

            if (index >= saveSlotUI.Length)
                index = 0;

            UpdateLoadSelection();
        }

        // 슬롯 선택
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.F))
        {
            int slot = index + 1;

            // START → 새 게임
            if (isNewGameMode)
            {
                StartNewGame(slot);
                return;
            }

            // LOAD → 저장 데이터 불러오기
            if (SaveManager.Instance.HasSave(slot))
            {
                LoadGame(slot);
            }
            else
            {
                Debug.Log("빈 슬롯입니다.");
            }
        }

        // 삭제
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            // 새 게임 모드에서는 삭제 불가
            if (!isNewGameMode)
            {
                OpenDeleteConfirm();
            }
        }

        // 뒤로가기
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseLoadMenu();
        }
    }

    // 슬롯 메뉴 닫기
    void CloseLoadMenu()
    {
        UISound.Play(UISoundId.Cancel);
        lastMenuSoundIndex = -1;
        lastSlotSoundIndex = -1;
        isLoadOpen = false;

        loadPanel.SetActive(false);
        
        // Press Enter 숨기기 + 깜빡임 종료
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        pressEnterText.SetActive(false); 

        Titletext.SetActive(true);
        mainMenu.SetActive(true);

        // 기본값으로 초기화
        isNewGameMode = false;

        index = 0;

        UpdateLoadSelection();
        UpdateSelection();
    }

    // 깜빡임 효과
    IEnumerator BlinkPressEnter()
    {
        TMP_Text text = pressEnterText.GetComponent<TMP_Text>();
        Color color = text.color;

        while (true)
        {
            // 서서히 나타남
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 0.5f;   // 숫자가 작을수록 느려짐
                color.a = Mathf.Lerp(0f, 1f, t);
                text.color = color;
                yield return null;
            }

            // 서서히 사라짐
            t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime * 0.5f;
                color.a = Mathf.Lerp(1f, 0f, t);
                text.color = color;
                yield return null;
            }
        }
    }


    //==================================================
    // 확인창
    //==================================================

    // 확인창 열기
    void OpenConfirmation(ConfirmationType type, string message)
    {
        if (!isConfirmationOpen) UISound.Play(UISoundId.DialogOpen);
        confirmationType = type;

        confirmationText.text = message;

        isConfirmationOpen = true;

        confirmationPanel.SetActive(true);
    }

    // 확인창 입력
    void HandleConfirmationInput()
    {
        // Enter / F = 확인
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.F))
        {
            ConfirmAction();
            return;
        }

        // ESC = 취소
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelConfirmation();
        }
    }

    // 확인 동작
    void ConfirmAction()
    {
        switch (confirmationType)
        {
            case ConfirmationType.Delete:
                ConfirmDelete();
                break;

            case ConfirmationType.NewGame:
                ConfirmNewGame();
                break;
        }
    }

    //==================================================
    // 저장 데이터 삭제
    //==================================================

    // 삭제 확인창 열기
    void OpenDeleteConfirm()
    {
        int slot = index + 1;

        // 빈 슬롯
        if (!SaveManager.Instance.HasSave(slot))
        {
            Debug.Log("삭제할 저장 데이터가 없습니다.");
            return;
        }

        deleteTargetSlot = slot;

        OpenConfirmation(
            ConfirmationType.Delete,
            $"SAVE{slot}의 저장 데이터를 삭제하시겠습니까?\n\nYes(Enter/F)  /  No(Esc)"
        );
    }

    // 삭제 확정
    void ConfirmDelete()
    {
        if (deleteTargetSlot <= 0)
            return;

        SaveManager.Instance.Delete(deleteTargetSlot);
        UISound.Play(UISoundId.DeleteConfirm);

        Debug.Log($"슬롯 {deleteTargetSlot} 삭제 완료");

        // 상태 초기화
        deleteTargetSlot = -1;
        confirmationType = ConfirmationType.None;
        isConfirmationOpen = false;

        confirmationPanel.SetActive(false);

        // 슬롯 정보 갱신
        UpdateLoadSlots();

        // 선택 상태 갱신
        UpdateLoadSelection();
    }

    //==================================================
    // 새 게임 덮어쓰기
    //==================================================

    // 기존 저장 데이터 삭제 후 새 게임
    void ConfirmNewGame()
    {
        if (newGameTargetSlot <= 0)
            return;

        int slot = newGameTargetSlot;

        // 기존 저장 데이터 삭제
        SaveManager.Instance.Delete(slot);

        Debug.Log($"슬롯 {slot} 기존 데이터 삭제");

        // 상태 초기화
        newGameTargetSlot = -1;
        confirmationType = ConfirmationType.None;
        isConfirmationOpen = false;

        confirmationPanel.SetActive(false);

        // 새 게임 시작
        BeginNewGame(slot);
    }

    // 확인 취소
    void CancelConfirmation()
    {
        UISound.Play(UISoundId.Cancel);
        confirmationType = ConfirmationType.None;

        deleteTargetSlot = -1;
        newGameTargetSlot = -1;

        isConfirmationOpen = false;

        confirmationPanel.SetActive(false);

        // 슬롯 선택 상태 유지
        UpdateLoadSelection();
    }


    //==================================================
    // 설정 메뉴
    //==================================================

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
        lastMenuSoundIndex = -1;
        isOptionOpen = false;

        if (optionController != null)
        {
            optionController.ClosePanel();
        }

        mainMenu.SetActive(true);

        index = 0;

        UpdateSelection();

        isSelecting = false;
    }


    //==================================================
    // 게임 로드
    //==================================================

    void LoadGame(int slot)
    {
        if (!SaveManager.Instance.HasSave(slot))
        {
            Debug.Log("저장 데이터가 없습니다.");
            return;
        }

        UISound.Play(UISoundId.SlotConfirm);
        SaveData.Instance.currentSlot = slot;

        SaveManager.Instance.Load(slot);

        SaveData.Instance.isLoadIntro = true;

        StartCoroutine(LoadScene());
    }

    IEnumerator LoadScene()
    {
        yield return StartCoroutine(fadeManager.FadeOut());

        SceneManager.LoadScene("DeathScene");
    }
}
