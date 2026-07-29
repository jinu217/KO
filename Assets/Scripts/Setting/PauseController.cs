using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// =====================================================
// PauseController
// - 일시정지 메뉴 입력 처리
// - 메뉴 선택 이동
// - 하이라이트 표시
// - 선택한 메뉴 실행
// =====================================================

public class PauseController : MonoBehaviour
{
    //====================================================
    // UI REFERENCES
    //====================================================

    [Header("Menu Texts")]
    [SerializeField] private TMP_Text[] menuTexts;

    [Header("Manager")]
    [SerializeField] private PauseManager pauseManager;

    [Header("Highlight")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;

    //====================================================
    // STATE
    //====================================================

    // 현재 선택된 메뉴 번호
    private int currentIndex = 0;

    //====================================================
    // UPDATE
    //====================================================

    void Update()
    {
        // 일시정지가 아닐 때는 입력을 받지 않는다.
        if (!pauseManager.IsPaused)
            return;

        // 설정창이 열려 있으면 Pause 메뉴는 입력 금지
        if (pauseManager.IsSettingOpen)
            return;

        HandleInput();
    }

    //====================================================
    // INPUT
    //====================================================

    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.UpArrow))
            Move(-1);

        if (Input.GetKeyDown(KeyCode.DownArrow))
            Move(1);

        if (Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Z))
        {
            ExecuteMenu();
        }
    }

    //====================================================
    // MENU MOVE
    //====================================================

    void Move(int dir)
    {
        currentIndex += dir;

        if (currentIndex < 0)
            currentIndex = menuTexts.Length - 1;

        if (currentIndex >= menuTexts.Length)
            currentIndex = 0;

        UpdateSelection();
    }

    //====================================================
    // MENU EXECUTE
    //====================================================

    void ExecuteMenu()
    {
        switch (currentIndex)
        {
            // 계속하기
            case 0:
                pauseManager.Resume();
                break;

            // 설정
            case 1:
                pauseManager.OpenSetting();
                break;

            // 타이틀
            case 2:
                Time.timeScale = 1f;
                SceneManager.LoadScene("Title Scene");
                break;

            // 게임 종료
            case 3:
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }

    //====================================================
    // UI
    //====================================================

    public void ResetSelection()
    {
        currentIndex = 0;
        UpdateSelection();
    }

    void UpdateSelection()
    {
        for (int i = 0; i < menuTexts.Length; i++)
        {
            SetHighlight(menuTexts[i], i == currentIndex);
        }
    }

    void SetHighlight(TMP_Text text, bool selected)
    {
        if (text == null)
            return;

        text.color = selected ? selectedColor : normalColor;

        // 선택된 메뉴는 볼드체
        text.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
    }
}