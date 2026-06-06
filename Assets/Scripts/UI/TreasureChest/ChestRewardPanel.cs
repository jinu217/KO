using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// [보물상자 보상 선택 UI]
// - 보상 후보(최대 3개) 버튼을 표시하고 방향키/Attack 입력으로 선택 지급
// - Open/Close로 타임스케일을 제어하며, IsAnyOpen으로 다른 스크립트가 UI 상태를 확인
public class ChestRewardPanel : MonoBehaviour
{
    // 씬 내 ChestRewardUI가 하나라도 열려 있으면 true(PlayerController 입력 차단 등에 사용)
    public static bool IsAnyOpen { get; private set; }

    [Header("패널")]
    [Tooltip("보상 UI 루트 패널 오브젝트(미연결 시 this.gameObject 사용)")]
    [SerializeField] private GameObject rewardPanel;

    [Header("보상 버튼(최대 3)")]
    [Tooltip("보상 슬롯 버튼 배열(인스펙터에서 3개 할당)")]
    [SerializeField] private Button[] rewardButtons = new Button[3];
    [Tooltip("슬롯별 아이템 이름 텍스트")]
    [SerializeField] private TMP_Text[] rewardTexts = new TMP_Text[3];
    [Tooltip("슬롯별 아이템 아이콘 Image")]
    [SerializeField] private Image[] rewardIcons = new Image[3];

    [Header("아이콘 크기")]
    [Tooltip("스프라이트 픽셀 기준 배율(1 = 원본. 작게 보이면 1.2~1.5로 증가)")]
    [SerializeField] private float iconUIScale = 1f;
    [Tooltip("아이콘 최대 너비(px)")]
    [SerializeField] private float iconMaxWidth = 120f;
    [Tooltip("아이콘 최대 높이(px)")]
    [SerializeField] private float iconMaxHeight = 80f;

    [Header("선택 색상")]
    [Tooltip("비선택 슬롯 버튼 배경 색상")]
    [SerializeField] private Color normalColor = Color.white;
    [Tooltip("선택 중인 슬롯 버튼 배경 색상")]
    [SerializeField] private Color selectedColor = new Color(1f, 0.9f, 0.3f);

    [Header("입력")]
    [Tooltip("방향키 연속 입력 간격(초)")]
    [SerializeField] private float moveRepeatDelay = 0.16f;

    [Header("옵션")]
    [Tooltip("보상 선택 후 패널 자동 닫기 여부")]
    [SerializeField] private bool hideAfterClaim = true;

    [Header("범위 시각화")]
    [Tooltip("보상 패널 기준점 Gizmo 표시(OnDrawGizmosSelected)")]
    [SerializeField] private bool showRewardPanelCenterGizmo = false;
    [Tooltip("기준점 Gizmo 반지름")]
    [SerializeField] private float rewardPanelCenterGizmoRadius = 0.2f;

    // 현재 열린 보상 목록 및 연결 참조
    private List<ItemData> currentRewards;
    private TreasureChest currentChest;
    private InventoryUI inventoryUI;

    // 입력 액션 캐시
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction attackAction;

    // 선택 상태
    private int selectedIndex = -1;
    private float lastMoveTime;
    private bool isInitialized;

    // 현재 rewardPanel이 활성화 상태인지 여부
    public bool IsOpen => rewardPanel != null && rewardPanel.activeSelf;

    // rewardPanel 미연결 시 this.gameObject로 폴백
    private void Awake()
    {
        if (rewardPanel == null)
            rewardPanel = gameObject;
    }

    private void Start()
    {
        InitializeIfNeeded();
    }

    // PlayerInput 컴포넌트에서 Move/Attack 액션 참조 캐시(중복 호출 방지)
    private void InitializeIfNeeded()
    {
        if (isInitialized) return;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogWarning("ChestRewardUI: Player 태그 오브젝트를 찾지 못했습니다.");
            return;
        }

        playerInput = playerObj.GetComponent<PlayerInput>();
        if (playerInput == null || playerInput.actions == null)
        {
            Debug.LogWarning("ChestRewardUI: PlayerInput 또는 InputActionAsset을 찾지 못했습니다.");
            return;
        }

        moveAction = playerInput.actions["Move"];
        attackAction = playerInput.actions["Attack"];
        isInitialized = true;
    }

    // 패널이 열려 있을 때만 키 입력 처리
    private void Update()
    {
        if (!IsOpen) return;
        HandleKeyboardInput();
    }

    // 보상 목록과 참조를 받아 패널 열기 + 타임스케일 0 설정
    public void Open(List<ItemData> rewards, TreasureChest ownerChest, InventoryUI targetInventory)
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

    // 패널 닫기: 참조 정리 + 타임스케일 복구
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

    // 방향키로 선택 이동, Attack 버튼으로 현재 선택 아이템 지급(비동기 타임 기준)
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
                if (move.x > 0.5f || move.y < -0.5f) direction = 1;
                else if (move.x < -0.5f || move.y > 0.5f) direction = -1;

                if (direction != 0)
                {
                    MoveSelection(direction);
                    lastMoveTime = Time.unscaledTime;
                }
            }
        }

        if (attackAction != null && attackAction.WasPressedThisFrame() && selectedIndex >= 0)
            OnSelectReward(selectedIndex);
    }

    // direction 방향으로 유효한 다음 슬롯으로 선택 이동
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

    // 첫 번째 유효한 슬롯을 selectedIndex로 설정
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

    // 인덱스가 유효한 보상 슬롯인지 검사(null/범위초과/비활성 제외)
    private bool IsValidRewardIndex(int i)
    {
        if (currentRewards == null) return false;
        if (i < 0 || i >= currentRewards.Count) return false;
        if (i >= rewardButtons.Length) return false;
        if (rewardButtons[i] == null || !rewardButtons[i].gameObject.activeSelf) return false;
        return currentRewards[i] != null;
    }

    // 현재 보상 목록에 맞춰 버튼/텍스트/아이콘 갱신 + 리스너 연결
    private void RefreshButtons()
    {
        for (int i = 0; i < rewardButtons.Length; i++)
        {
            Button btn = rewardButtons[i];
            if (btn == null) continue;

            btn.onClick.RemoveAllListeners();

            bool valid = currentRewards != null && i < currentRewards.Count && currentRewards[i] != null;
            btn.gameObject.SetActive(valid);

            if (!valid)
            {
                if (rewardTexts != null && i < rewardTexts.Length && rewardTexts[i] != null)
                    rewardTexts[i].text = string.Empty;

                if (rewardIcons != null && i < rewardIcons.Length && rewardIcons[i] != null)
                    ClearRewardIcon(rewardIcons[i]);

                continue;
            }

            int captured = i;
            ItemData item = currentRewards[captured];

            if (rewardTexts != null && captured < rewardTexts.Length && rewardTexts[captured] != null)
                rewardTexts[captured].text = item.itemName;

            if (rewardIcons != null && captured < rewardIcons.Length && rewardIcons[captured] != null)
                SetRewardIcon(rewardIcons[captured], item.icon/*itemSprite*/);

            btn.onClick.AddListener(() => OnSelectReward(captured));
        }
    }

    // 아이콘 스프라이트 설정 + 최대 크기(iconMaxWidth/Height) 기준 비율 맞춤
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

    // 아이콘 스프라이트 제거 + AspectRatioFitter 정리
    private void ClearRewardIcon(Image icon)
    {
        if (icon == null) return;

        icon.sprite = null;
        icon.enabled = false;

        AspectRatioFitter fitter = icon.GetComponent<AspectRatioFitter>();
        if (fitter != null)
            Destroy(fitter);
    }

    // selectedIndex 기준으로 선택/비선택 색상 갱신
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

    // 모든 버튼 onClick 리스너 제거(Close 전 중복 호출 방지)
    private void ClearButtonListeners()
    {
        if (rewardButtons == null) return;

        for (int i = 0; i < rewardButtons.Length; i++)
        {
            if (rewardButtons[i] != null)
                rewardButtons[i].onClick.RemoveAllListeners();
        }
    }

    // 선택된 아이템을 인벤토리에 추가하고 보상 지급 완료 처리
    private void OnSelectReward(int index)
    {
        if (currentRewards == null || index < 0 || index >= currentRewards.Count) return;

        ItemData selected = currentRewards[index];
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

    // 보상 패널 기준점 Gizmo 표시(showRewardPanelCenterGizmo 켤 때만)
    private void OnDrawGizmosSelected()
    {
        if (!showRewardPanelCenterGizmo) return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.01f, rewardPanelCenterGizmoRadius));
    }
}
