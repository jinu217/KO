using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingPanel;

    [Header("Controller")]
    [SerializeField] private PauseController pauseController;
    [SerializeField] private SettingController settingController;
    [SerializeField] private PlayerController playerController;

    public bool IsPaused { get; private set; }
    public bool IsSettingOpen => settingPanel != null && settingPanel.activeSelf;

    void Start()
    {
        pausePanel.SetActive(false);

        if (settingPanel != null)
            settingPanel.SetActive(false);

        Time.timeScale = 1f;
        IsPaused = false;
    }

    void Update()
    {
        // 설정창이 열려 있을 때는 ESC 처리를 SettingController가 담당
        if (settingPanel != null && settingPanel.activeSelf)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // 튜토리얼북이 열려 있으면 PauseManager는 ESC를 처리하지 않는다.
            if (TutorialBookUI.IsAnyOpen)
                return;

            // 튜토리얼북이 닫혀 있을 때만 정지/재개
            if (IsPaused)
                Resume();
            else
                Pause();
        }
    }

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

    //======= 게임 재개 =========
    public void Resume()
    {
        IsPaused = false;

        pausePanel.SetActive(false);

        if (settingPanel != null)
            settingPanel.SetActive(false);

        // 플레이어 입력 허용
        playerController?.ResumeKeyboardControl();

        // 게임 재개
        Time.timeScale = 1f;
    }

    public void OpenSetting()
    {
        pausePanel.SetActive(false);
        settingController.OpenPanel();
        settingController.ResetSelection();
    }

    public void CloseSetting()
    {
        settingController.ClosePanel();
        pausePanel.SetActive(true);
        pauseController?.ResetSelection();
    }
}