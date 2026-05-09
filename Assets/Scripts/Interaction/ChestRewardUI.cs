using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// 보물상자 보상 선택 UI
// - 랜덤으로 나온 아이템 3개를 버튼으로 표시
// - 방향키 이동 + Attack(Z)으로 하나 선택 시 인벤토리에 지급
// - 작은 아이콘은 원본 크기 유지
// - 큰 아이콘은 보상 슬롯 최대 크기 안으로 자동 축소
public class ChestRewardUI : MonoBehaviour
{
    public static bool IsAnyOpen { get; private set; }

    [Header("패널")]
    [SerializeField] private GameObject rewardPanel;

    [Header("보상 버튼(최대 3)")]
    [SerializeField] private Button[] rewardButtons = new Button[3];
    [SerializeField] private TMP_Text[] rewardTexts = new TMP_Text[3];
    [SerializeField] private Image[] rewardIcons = new Image[3];

    [Header("아이콘 크기")]
    [Tooltip("1이면 스프라이트 원본 픽셀 크기 기준. 전체적으로 작으면 1.2~1.5 정도로 올리세요.")]
    [SerializeField] private float iconUIScale = 1f;

    [Tooltip("보상 슬롯 안에서 허용할 최대 아이콘 너비")]
    [SerializeField] private float iconMaxWidth = 120f;

    [Tooltip("보상 슬롯 안에서 허용할 최대 아이콘 높이")]
    [SerializeField] private float iconMaxHeight = 80f;

    [Header("선택 색상")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0.3f);

    [Header("입력")]
    [SerializeField] private float moveRepeatDelay = 0.16f;

    [Header("옵션")]
    [SerializeField] private bool hideAfterClaim = true;

    private List<InventoryItemData> currentRewards;
    private TreasureChest currentChest;
    private InventoryUI inventoryUI;

    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction attackAction;

    private int selectedIndex = -1;
    private float lastMoveTime;
    private bool isInitialized;

    public bool IsOpen => rewardPanel != null && rewardPanel.activeSelf;

    private void Awake()
    {
        if (rewardPanel == null)
            rewardPanel = gameObject;
    }

    private void Start()
    {
        InitializeIfNeeded();
    }

    private void InitializeIfNeeded()
    {
        if (isInitialized) return;

        if (rewardPanel == null)
            Debug.LogWarning("ChestRewardUI의 rewardPanel이 비어 있어 자기 자신을 사용합니다.");

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning("ChestRewardUI: Player 태그 오브젝트를 찾지 못했습니다.");
            return;
        }

        playerInput = playerObj.GetComponent<PlayerInput>();
        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogWarning("ChestRewardUI: PlayerInput 또는 actions를 찾지 못했습니다.");
            return;
        }

        moveAction = playerInput.actions["Move"];
        attackAction = playerInput.actions["Attack"];

        isInitialized = true;
    }

    private void Update()
    {
        if (!IsOpen) return;

        HandleKeyboardInput();
    }

    public void Open(List<InventoryItemData> rewards, TreasureChest ownerChest, InventoryUI targetInventory)
    {
        InitializeIfNeeded();

        currentRewards = rewards;
        currentChest = ownerChest;
        inventoryUI = targetInventory;

        if (rewardPanel != null)
            rewardPanel.SetActive(true);

        Time.timeScale = 0f;
        IsAnyOpen = true;

        RefreshButtons();
        SelectFirstValid();
        RefreshSelectionVisual();
    }

    public void Close()
    {
        ClearButtonListeners();

        if (rewardPanel != null)
            rewardPanel.SetActive(false);

        currentRewards = null;
        currentChest = null;
        inventoryUI = null;

        selectedIndex = -1;
        IsAnyOpen = false;
        Time.timeScale = 1f;
    }

    private void HandleKeyboardInput()
    {
        if (!isInitialized)
            InitializeIfNeeded();

        if (moveAction != null)
        {
            Vector2 move = moveAction.ReadValue<Vector2>();

            if (Time.unscaledTime >= lastMoveTime + moveRepeatDelay)
            {
                int direction = 0;

                if (move.x > 0.5f || move.y < -0.5f)
                    direction = 1;
                else if (move.x < -0.5f || move.y > 0.5f)
                    direction = -1;

                if (direction != 0)
                {
                    MoveSelection(direction);
                    lastMoveTime = Time.unscaledTime;
                }
            }
        }

        if (attackAction != null && attackAction.WasPressedThisFrame())
        {
            if (selectedIndex >= 0)
                OnSelectReward(selectedIndex);
        }
    }

    private void MoveSelection(int direction)
    {
        if (currentRewards == null || currentRewards.Count == 0) return;

        if (selectedIndex < 0)
            SelectFirstValid();

        if (selectedIndex < 0) return;

        int next = selectedIndex;

        for (int i = 0; i < rewardButtons.Length; i++)
        {
            next = (next + direction + rewardButtons.Length) % rewardButtons.Length;

            if (IsValidRewardIndex(next))
            {
                selectedIndex = next;
                RefreshSelectionVisual();
                return;
            }
        }
    }

    private void SelectFirstValid()
    {
        selectedIndex = -1;

        for (int i = 0; i < rewardButtons.Length; i++)
        {
            if (IsValidRewardIndex(i))
            {
                selectedIndex = i;
                return;
            }
        }
    }

    private bool IsValidRewardIndex(int i)
    {
        if (currentRewards == null) return false;
        if (i < 0 || i >= currentRewards.Count) return false;
        if (i >= rewardButtons.Length) return false;
        if (rewardButtons[i] == null || !rewardButtons[i].gameObject.activeSelf) return false;

        return currentRewards[i] != null;
    }

    private void RefreshButtons()
    {
        for (int i = 0; i < rewardButtons.Length; i++)
        {
            Button btn = rewardButtons[i];
            if (btn == null) continue;

            btn.onClick.RemoveAllListeners();

            bool valid = currentRewards != null &&
                         i < currentRewards.Count &&
                         currentRewards[i] != null;

            btn.gameObject.SetActive(valid);

            if (!valid)
            {
                if (rewardTexts != null &&
                    i < rewardTexts.Length &&
                    rewardTexts[i] != null)
                {
                    rewardTexts[i].text = string.Empty;
                }

                if (rewardIcons != null &&
                    i < rewardIcons.Length &&
                    rewardIcons[i] != null)
                {
                    ClearRewardIcon(rewardIcons[i]);
                }

                continue;
            }

            int captured = i;
            InventoryItemData item = currentRewards[captured];

            if (rewardTexts != null &&
                captured < rewardTexts.Length &&
                rewardTexts[captured] != null)
            {
                rewardTexts[captured].text = item.itemName;
            }

            if (rewardIcons != null &&
                captured < rewardIcons.Length &&
                rewardIcons[captured] != null)
            {
                SetRewardIcon(rewardIcons[captured], item.itemSprite);
            }

            btn.onClick.AddListener(() => OnSelectReward(captured));
        }
    }

    private void SetRewardIcon(Image icon, Sprite sprite)
    {
        if (icon == null) return;

        icon.sprite = sprite;
        icon.enabled = sprite != null;
        icon.preserveAspect = true;

        AspectRatioFitter fitter = icon.GetComponent<AspectRatioFitter>();
        if (fitter != null)
            Destroy(fitter);

        if (sprite == null) return;

        RectTransform rt = icon.rectTransform;

        float safeScale = Mathf.Max(0.01f, iconUIScale);

        float width = sprite.rect.width * safeScale;
        float height = sprite.rect.height * safeScale;

        float widthScale = iconMaxWidth / Mathf.Max(width, 0.01f);
        float heightScale = iconMaxHeight / Mathf.Max(height, 0.01f);

        float fitScale = Mathf.Min(widthScale, heightScale, 1f);

        width *= fitScale;
        height *= fitScale;

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(width, height);
    }

    private void ClearRewardIcon(Image icon)
    {
        if (icon == null) return;

        icon.sprite = null;
        icon.enabled = false;

        AspectRatioFitter fitter = icon.GetComponent<AspectRatioFitter>();
        if (fitter != null)
            Destroy(fitter);
    }

    private void RefreshSelectionVisual()
    {
        for (int i = 0; i < rewardButtons.Length; i++)
        {
            if (rewardButtons[i] == null) continue;

            Image btnImage = rewardButtons[i].GetComponent<Image>();
            if (btnImage == null) continue;

            btnImage.color = (i == selectedIndex) ? selectedColor : normalColor;
        }
    }

    private void ClearButtonListeners()
    {
        if (rewardButtons == null) return;

        for (int i = 0; i < rewardButtons.Length; i++)
        {
            if (rewardButtons[i] != null)
                rewardButtons[i].onClick.RemoveAllListeners();
        }
    }

    private void OnSelectReward(int index)
    {
        if (currentRewards == null || index < 0 || index >= currentRewards.Count) return;

        InventoryItemData selected = currentRewards[index];
        if (selected == null) return;

        if (inventoryUI == null)
        {
            Debug.LogWarning("ChestRewardUI: InventoryUI 참조가 없어 보상을 지급할 수 없습니다.");
            return;
        }

        bool added = inventoryUI.TryAddItemToInventory(selected);
        if (!added) return;

        Debug.Log($"보물상자 보상 선택: {selected.itemName}");

        if (currentChest != null)
            currentChest.NotifyRewardClaimed();

        if (hideAfterClaim)
            Close();
    }
}