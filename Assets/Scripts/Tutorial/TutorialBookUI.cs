using TMPro;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TutorialBookUI : MonoBehaviour
{
    public static bool IsAnyOpen { get; private set; }

    [System.Serializable]
    public class TutorialBookEntry
    {
        [Tooltip("왼쪽 메뉴에 표시될 튜토리얼 이름입니다.")]
        public string menuLabel;

        [Tooltip("오른쪽 페이지 위쪽에 표시될 제목입니다.")]
        public string title;

        [Tooltip("설명 파트별로 바뀔 제목입니다. 비워두면 위의 기본 제목을 사용합니다.")]
        public string[] titleParts;

        [Tooltip("오른쪽 페이지의 스크린샷 영역에 표시될 이미지입니다.")]
        public Sprite screenshot;

        [Tooltip("오른쪽 페이지의 설명입니다. 너무 길면 Element를 2개로 만들어 좌우 방향키로 넘기면 됩니다.")]
        [TextArea(3, 6)] public string[] descriptionParts;
    }

    [Header("Root")]
    [Tooltip("튜토리얼 북 전체 UI입니다. 비워두면 이 스크립트가 붙은 오브젝트를 켜고 끕니다.")]
    [SerializeField] private GameObject bookRoot;

    [Header("Menu")]
    [Tooltip("왼쪽 튜토리얼 메뉴 텍스트 목록입니다. 위에서부터 순서대로 넣습니다.")]
    [SerializeField] private TMP_Text[] menuTexts;

    [Tooltip("현재 선택된 메뉴를 가리키는 손가락 UI입니다. 이 오브젝트의 처음 위치를 기준으로 움직입니다.")]
    [SerializeField] private RectTransform fingerPointer;

    [Tooltip("손가락 UI가 위/아래 입력마다 이동할 Y 간격입니다.")]
    [SerializeField] private float fingerYStep = 55f;

    [Tooltip("선택된 메뉴 글자 색입니다.")]
    [SerializeField] private Color selectedMenuColor = Color.white;

    [Tooltip("선택되지 않은 메뉴 글자 색입니다.")]
    [SerializeField] private Color normalMenuColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Page")]
    [Tooltip("오른쪽 페이지의 제목 텍스트입니다.")]
    [SerializeField] private TMP_Text titleText;

    [Tooltip("오른쪽 페이지의 스크린샷 이미지입니다.")]
    [SerializeField] private Image screenshotImage;

    [Tooltip("오른쪽 페이지의 설명 텍스트입니다.")]
    [SerializeField] private TMP_Text descriptionText;

    [Tooltip("현재 설명 파트 번호 표시 텍스트입니다. 예: 1 / 2")]
    [SerializeField] private TMP_Text pageNumberText;

    [Header("Contents")]
    [Tooltip("튜토리얼 북에 들어갈 메뉴, 이미지, 제목, 설명 데이터입니다.")]
    [SerializeField] private TutorialBookEntry[] entries;

    [Header("Input")]
    [Tooltip("플레이어 PlayerInput에서 읽을 이동 액션 이름입니다. 기본값은 Move입니다.")]
    [SerializeField] private string moveActionName = "Move";

    [Tooltip("책 UI를 닫을 입력 액션 이름입니다. PlayerInput에 만든 ESC 액션 이름과 같아야 합니다.")]
    [SerializeField] private string closeActionName = "ESC";

    [Tooltip("방향키 입력을 한 번 누른 것으로 판단할 최소 입력값입니다.")]
    [SerializeField] private float inputThreshold = 0.5f;

    [Tooltip("키보드 방향키를 직접 읽는 보조 입력입니다.")]
    [SerializeField] private bool useKeyboardArrowFallback = true;

    [Header("Player Lock")]
    [Tooltip("책 UI가 열렸을 때 조작을 잠글 플레이어 컨트롤러입니다. 비워두면 Player 태그에서 자동으로 찾습니다.")]
    [SerializeField] private PlayerController playerController;

    [Tooltip("책 UI 입력을 읽을 PlayerInput입니다. 비워두면 Player 태그에서 자동으로 찾습니다.")]
    [SerializeField] private PlayerInput playerInput;

    public bool IsOpen => isOpen;
    public event Action Closed;

    private InputAction moveAction;
    private InputAction closeAction;
    private Vector2 previousMoveInput;
    private int selectedIndex;
    private int descriptionPartIndex;
    private bool isOpen;
    private Vector3 fingerStartAnchoredPosition;
    private bool hasFingerStartPosition;

    private void Awake()
    {
        EnsureRoot();
        CachePlayerReferences();
        CaptureFingerStartPosition();
        SetRootActive(false);
    }

    private void Update()
    {
        if (!isOpen)
            return;

        if (ShouldCloseFromTutorialBookInput())
        {
            Close();
            return;
        }

        Vector2 input = ReadMoveInput();
        HandleMenuInput(input);
        previousMoveInput = input;
    }

    public void Open()
    {
        OpenAt(selectedIndex);
    }

    public void OpenAt(int pageIndex)
    {
        EnsureRoot();
        CachePlayerReferences();
        CaptureFingerStartPosition();

        isOpen = true;
        IsAnyOpen = true;
        selectedIndex = Mathf.Clamp(pageIndex, 0, Mathf.Max(0, GetSelectionCount() - 1));
        descriptionPartIndex = 0;
        previousMoveInput = Vector2.zero;

        playerController?.PauseKeyboardControl();
        SetRootActive(true);
        Refresh();
    }

    public void Toggle()
    {
        if (isOpen)
            Close();
        else
            Open();
    }

    public void Close()
    {
        EnsureRoot();
        isOpen = false;
        IsAnyOpen = false;
        previousMoveInput = Vector2.zero;

        SetRootActive(false);
        playerController?.ResumeKeyboardControl();
        Closed?.Invoke();
    }

    private void OnDisable()
    {
        if (isOpen)
        {
            isOpen = false;
            IsAnyOpen = false;
        }
    }

    private void HandleMenuInput(Vector2 input)
    {
        if (WasPressed(input.y, previousMoveInput.y, true))
        {
            ChangeSelection(-1);
            return;
        }

        if (WasPressed(input.y, previousMoveInput.y, false))
        {
            ChangeSelection(1);
            return;
        }

        if (WasPressed(input.x, previousMoveInput.x, true))
        {
            ChangeDescriptionPart(1);
            return;
        }

        if (WasPressed(input.x, previousMoveInput.x, false))
            ChangeDescriptionPart(-1);
    }

    private bool WasPressed(float current, float previous, bool positive)
    {
        if (positive)
            return current >= inputThreshold && previous < inputThreshold;

        return current <= -inputThreshold && previous > -inputThreshold;
    }

    private Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;

        if (moveAction == null)
            CacheInputAction();

        if (moveAction != null)
            input = moveAction.ReadValue<Vector2>();

        if (!useKeyboardArrowFallback || Keyboard.current == null)
            return input;

        if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            input.y = 1f;
        else if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            input.y = -1f;

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            input.x = 1f;
        else if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
            input.x = -1f;

        return input;
    }

    private void ChangeSelection(int direction)
    {
        int count = GetSelectionCount();
        if (count <= 0)
            return;

        selectedIndex = Mathf.Clamp(selectedIndex + direction, 0, count - 1);
        descriptionPartIndex = 0;
        Refresh();
    }

    private void ChangeDescriptionPart(int direction)
    {
        TutorialBookEntry entry = GetCurrentEntry();
        int partCount = GetDescriptionPartCount(entry);
        if (partCount <= 1)
            return;

        descriptionPartIndex = Mathf.Clamp(descriptionPartIndex + direction, 0, partCount - 1);
        RefreshPage();
    }

    private void Refresh()
    {
        RefreshMenu();
        RefreshPage();
    }

    private void RefreshMenu()
    {
        int count = menuTexts != null ? menuTexts.Length : 0;
        for (int i = 0; i < count; i++)
        {
            TMP_Text menuText = menuTexts[i];
            if (menuText == null)
                continue;

            TutorialBookEntry entry = entries != null && i < entries.Length ? entries[i] : null;
            menuText.text = entry != null ? entry.menuLabel : "";
            menuText.color = i == selectedIndex ? selectedMenuColor : normalMenuColor;
        }

        MoveFingerPointer();
    }

    private void MoveFingerPointer()
    {
        if (fingerPointer == null)
            return;

        fingerPointer.anchoredPosition3D = fingerStartAnchoredPosition + Vector3.down * (selectedIndex * fingerYStep);
    }

    private void RefreshPage()
    {
        TutorialBookEntry entry = GetCurrentEntry();

        if (screenshotImage != null)
        {
            screenshotImage.sprite = entry != null ? entry.screenshot : null;
            screenshotImage.enabled = screenshotImage.sprite != null;
        }

        int partCount = GetDescriptionPartCount(entry);
        descriptionPartIndex = Mathf.Clamp(descriptionPartIndex, 0, Mathf.Max(0, partCount - 1));

        if (titleText != null)
            titleText.text = GetTitleForCurrentPart(entry);

        if (descriptionText != null)
            descriptionText.text = partCount > 0 ? entry.descriptionParts[descriptionPartIndex] : "";

        if (pageNumberText != null)
            pageNumberText.text = partCount > 1 ? $"{descriptionPartIndex + 1} / {partCount}" : "";
    }

    private TutorialBookEntry GetCurrentEntry()
    {
        if (entries == null || entries.Length == 0)
            return null;

        if (selectedIndex < 0 || selectedIndex >= entries.Length)
            return null;

        return entries[selectedIndex];
    }

    private int GetEntryCount()
    {
        return entries != null ? entries.Length : 0;
    }

    private int GetSelectionCount()
    {
        int count = GetEntryCount();

        if (menuTexts != null)
            count = Mathf.Max(count, menuTexts.Length);

        return count;
    }

    private int GetDescriptionPartCount(TutorialBookEntry entry)
    {
        return entry != null && entry.descriptionParts != null ? entry.descriptionParts.Length : 0;
    }

    private string GetTitleForCurrentPart(TutorialBookEntry entry)
    {
        if (entry == null)
            return "";

        if (entry.titleParts != null &&
            descriptionPartIndex >= 0 &&
            descriptionPartIndex < entry.titleParts.Length &&
            !string.IsNullOrEmpty(entry.titleParts[descriptionPartIndex]))
        {
            return entry.titleParts[descriptionPartIndex];
        }

        return entry.title;
    }

    private void CachePlayerReferences()
    {
        if (playerController != null && playerInput != null)
        {
            CacheInputAction();
            return;
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
            return;

        if (playerController == null)
            playerController = player.GetComponent<PlayerController>();

        if (playerInput == null)
            playerInput = player.GetComponent<PlayerInput>();

        CacheInputAction();
    }

    private void CacheInputAction()
    {
        if (playerInput == null || playerInput.actions == null || string.IsNullOrEmpty(moveActionName))
            return;

        moveAction = playerInput.actions.FindAction(moveActionName, false);
        closeAction = playerInput.actions.FindAction(closeActionName, false);
    }

    private bool ShouldCloseFromTutorialBookInput()
    {
        if (closeAction == null)
            return false;

        return closeAction.WasPressedThisFrame();
    }

    private void CaptureFingerStartPosition()
    {
        if (hasFingerStartPosition || fingerPointer == null)
            return;

        fingerStartAnchoredPosition = fingerPointer.anchoredPosition3D;
        hasFingerStartPosition = true;
    }

    private void SetRootActive(bool active)
    {
        EnsureRoot();

        if (bookRoot != null)
            bookRoot.SetActive(active);
    }

    private void EnsureRoot()
    {
        if (bookRoot == null)
            bookRoot = gameObject;
    }
}
