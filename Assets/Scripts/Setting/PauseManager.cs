using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PauseManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingPanel;

    [Header("Confirm Panel")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private TMP_Text confirmText;

    [Header("Controller")]
    [SerializeField] private PauseController pauseController;
    [SerializeField] private SettingController settingController;
    [SerializeField] private PlayerController playerController;

    [Header("inventory Pause")]
    [SerializeField] private InventoryUI inventoryUI;

    public bool IsPaused { get; private set; }
    public bool IsSettingOpen => settingPanel != null && settingPanel.activeSelf;

    // 확인 패널이 열려 있는지
    public bool IsConfirmOpen =>
        confirmPanel != null && confirmPanel.activeSelf;

    // 현재 확인 중인 작업
    private ConfirmType currentConfirmType;

    private enum ConfirmType
    {
        None,
        Title,
        Quit
    }

    void Start()
    {
        pausePanel.SetActive(false);

        if (settingPanel != null)
            settingPanel.SetActive(false);

        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        Time.timeScale = 1f;
        IsPaused = false;
    }

    void Update()
    {
        // =========================
        // 확인 패널이 열려 있을 때
        // =========================
        if (IsConfirmOpen)
        {
            HandleConfirmInput();
            return;
        }

        // =========================
        // 설정창이 열려 있을 때
        // =========================
        if (settingPanel != null && settingPanel.activeSelf)
            return;

        // =========================
        // ESC 입력
        // =========================
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 튜토리얼북이 열려 있으면 PauseManager는 처리하지 않음
            if (TutorialBookUI.IsAnyOpen)
                return;

            // 튜토리얼북이 닫혀 있을 때만 정지/재개
            if (IsPaused)
                Resume();
            else
                Pause();
        }
    }

    // =========================
    // 확인 패널 입력 처리
    // =========================
    private void HandleConfirmInput()
    {
        // Enter = 확인
        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            ConfirmAction();
        }

        // ESC = 취소
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelConfirm();
        }
    }

    // =========================
    // 게임 일시정지
    // =========================
    public void Pause()
    {
        IsPaused = true;

        pausePanel.SetActive(true);

        // 메뉴를 첫 번째로 초기화
        pauseController?.ResetSelection();

        // 플레이어의 모든 게임플레이 입력 차단
        playerController?.PauseKeyboardControl();

        Time.timeScale = 0f;
    }

    // =========================
    // 게임 재개
    // =========================
    public void Resume()
    {
        IsPaused = false;

        pausePanel.SetActive(false);

        if (settingPanel != null)
            settingPanel.SetActive(false);

        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        currentConfirmType = ConfirmType.None;

        // 플레이어 입력 허용
        playerController?.ResumeKeyboardControl();

        // 인벤토리가 열려 있으면 계속 일시정지
        if (inventoryUI != null && inventoryUI.IsOpen)
        {
            Time.timeScale = 0f;
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    // =========================
    // 설정창 열기
    // =========================
    public void OpenSetting()
    {
        pausePanel.SetActive(false);

        settingController.OpenPanel();
        settingController.ResetSelection();
    }

    // =========================
    // 설정창 닫기
    // =========================
    public void CloseSetting()
    {
        settingController.ClosePanel();

        pausePanel.SetActive(true);

        pauseController?.ResetSelection();
    }

    // =========================
    // 타이틀 확인 패널 열기
    // =========================
    public void OpenTitleConfirm()
    {
        currentConfirmType = ConfirmType.Title;

        if (confirmText != null)
            confirmText.text = "타이틀로 돌아가시겠습니까?\n\nYes(Enter)  /  No(Esc)";

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (confirmPanel != null)
            confirmPanel.SetActive(true);
    }

    // =========================
    // 게임 종료 확인 패널 열기
    // =========================
    public void OpenQuitConfirm()
    {
        currentConfirmType = ConfirmType.Quit;

        if (confirmText != null)
            confirmText.text = "게임을 종료하시겠습니까?\n\nYes(Enter)  /  No(Esc)";

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (confirmPanel != null)
            confirmPanel.SetActive(true);
    }

    // =========================
    // 확인 취소
    // =========================
    private void CancelConfirm()
    {
        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        currentConfirmType = ConfirmType.None;

        // 다시 정지 패널 열기
        if (pausePanel != null)
            pausePanel.SetActive(true);

        pauseController?.ResetSelection();
    }

    // =========================
    // 확인 실행
    // =========================
    private void ConfirmAction()
    {
        switch (currentConfirmType)
        {
            case ConfirmType.Title:
                ReturnToTitle();
                break;

            case ConfirmType.Quit:
                QuitGame();
                break;
        }
    }

    // =========================
    // 타이틀로 돌아가기
    // =========================
    private void ReturnToTitle()
    {
        Time.timeScale = 1f;

        IsPaused = false;

        if (confirmPanel != null)
            confirmPanel.SetActive(false);

        // 타이틀 씬 이름에 맞게 수정
        SceneManager.LoadScene("Title Scene");
    }

    // =========================
    // 게임 종료
    // =========================
    private void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}