using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialBookInteractable : MonoBehaviour
{
    [Tooltip("플레이어가 책을 열 수 있는 거리입니다.")]
    [SerializeField] private float interactRange = 2f;

    [Tooltip("튜토리얼 북 UI 전체 오브젝트입니다.")]
    [SerializeField] private GameObject readingPanel;

    [Tooltip("메뉴/이미지/설명/입력 처리를 담당하는 튜토리얼 북 UI입니다. 비워두면 Reading Panel 안에서 자동으로 찾습니다.")]
    [SerializeField] private TutorialBookUI bookUI;

    [Tooltip("책을 닫았을 때 튜토리얼 단계를 다음 단계로 넘길지 여부입니다.")]
    [SerializeField] private bool completeWhenClosed = true;

    [Header("Input")]
    [Tooltip("플레이어 Input Actions의 책 다시 열기/닫기 액션 이름입니다.")]
    [SerializeField] private string tutorialBookActionName = "TutorialBook";

    [Tooltip("처음 책을 읽고 닫은 뒤부터 TutorialBook 버튼으로 다시 열 수 있습니다.")]
    [SerializeField] private bool allowToggleAfterFirstClose = true;

    private Transform player;
    private PlayerInput playerInput;
    private InventoryUI inventoryUI;
    private InputAction tutorialBookAction;
    private bool isReading;
    private bool completedFirstRead;

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            enabled = false;
            return;
        }

        player = playerObj.transform;
        playerInput = playerObj.GetComponent<PlayerInput>();
        inventoryUI = FindInventoryUIAnyState();
        if (playerInput == null || playerInput.actions == null)
        {
            enabled = false;
            return;
        }

        tutorialBookAction = playerInput.actions.FindAction(tutorialBookActionName, false);

        if (bookUI == null && readingPanel != null)
            bookUI = readingPanel.GetComponentInChildren<TutorialBookUI>(true);

        if (bookUI != null)
            bookUI.Closed += HandleBookUIClosed;
    }

    private void OnDestroy()
    {
        if (bookUI != null)
            bookUI.Closed -= HandleBookUIClosed;
    }

    private void Update()
    {
        if (IsOutsideTutorial())
        {
            HandleFreeTutorialBookToggle();
            return;
        }

        HandleTutorialBookToggle();
        HandleInitialBookInteraction();
    }

    private void HandleFreeTutorialBookToggle()
    {
        if (!WasTutorialBookPressed())
            return;

        bool bookIsOpen = bookUI != null ? bookUI.IsOpen : isReading;
        if (bookIsOpen)
            return;

        OpenBook();
    }

    public void CompleteReading()
    {
        CloseBook(true);
        TutorialManager.Instance?.NotifyBookInteracted();
    }

    private void HandleInitialBookInteraction()
    {
        if (player == null || tutorialBookAction == null)
            return;

        TutorialManager manager = TutorialManager.Instance;
        if (manager == null || manager.CurrentStep != TutorialStep.BookInteractGuide)
            return;

        if (Vector2.Distance(transform.position, player.position) > interactRange)
            return;

        if (!WasTutorialBookPressed())
            return;

        if (ShouldBlockTutorialBookOpen())
            return;

        OpenBook();
    }

    private void HandleTutorialBookToggle()
    {
        if (isReading || !allowToggleAfterFirstClose || !completedFirstRead || tutorialBookAction == null)
            return;

        if (!WasTutorialBookPressed())
            return;

        if (ShouldBlockTutorialBookOpen())
            return;

        OpenBook();
    }

    private void OpenBook()
    {
        isReading = true;
        if (bookUI != null)
            bookUI.Open();
        else if (readingPanel != null)
            readingPanel.SetActive(true);
    }

    private void CloseBook(bool markFirstReadComplete)
    {
        isReading = false;
        if (markFirstReadComplete)
            completedFirstRead = true;

        if (bookUI != null)
            bookUI.Close();
        else if (readingPanel != null)
            readingPanel.SetActive(false);
    }

    private void HandleBookUIClosed()
    {
        if (!isReading)
            return;

        isReading = false;
        completedFirstRead = true;

        TutorialManager manager = TutorialManager.Instance;
        if (completeWhenClosed && manager != null && manager.CurrentStep == TutorialStep.BookInteractGuide)
            manager.NotifyBookInteracted();
    }

    private bool ShouldBlockTutorialBookOpen()
    {
        if (bookUI != null && bookUI.IsOpen)
            return true;

        if (ChestRewardPanel.IsAnyOpen)
            return true;

        if (inventoryUI == null)
            inventoryUI = FindInventoryUIAnyState();

        return inventoryUI != null && inventoryUI.IsOpen;
    }

    private bool IsOutsideTutorial()
    {
        TutorialManager manager = TutorialManager.Instance;
        return manager == null || !manager.IsActive;
    }

    private bool WasTutorialBookPressed()
    {
        return tutorialBookAction != null && tutorialBookAction.WasPressedThisFrame();
    }

    private InventoryUI FindInventoryUIAnyState()
    {
        InventoryUI found = FindFirstObjectByType<InventoryUI>();
        if (found != null)
            return found;

        InventoryUI[] all = Resources.FindObjectsOfTypeAll<InventoryUI>();
        for (int i = 0; i < all.Length; i++)
        {
            InventoryUI ui = all[i];
            if (ui == null || !ui.gameObject.scene.IsValid())
                continue;

            return ui;
        }

        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
