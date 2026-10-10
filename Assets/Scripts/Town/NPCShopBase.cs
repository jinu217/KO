using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public abstract class NPCShopBase : MonoBehaviour
{
    private static NPCShopBase activeShop;

    [Header("Common Shop UI")]
    [Tooltip("상점 패널")]
    [SerializeField] protected GameObject shopPanel;
    [Tooltip("구매 결과 및 오류 안내 텍스트")]
    [SerializeField] protected TMPro.TMP_Text feedbackText;

    [Header("Panel Open Animation")]
    [Tooltip("상점 패널이 아래에서 시작할 거리")]
    [SerializeField, Min(0f)] private float panelOpenOffsetY = 500f;
    [Tooltip("상점 패널이 원래 위치로 올라오는 시간")]
    [SerializeField, Min(0f)] private float panelOpenDuration = 0.35f;

    private const string InteractActionName = "Interact";
    private const string MoveActionName = "Move";
    private const string EscapeActionName = "ESC";

    protected PlayerController PlayerController { get; private set; }
    protected PlayerItemManager PlayerItemManager { get; private set; }
    protected CurrencyManager Currency { get; private set; }
    protected PlayerRuntimeState RuntimeState { get; private set; }

    private PlayerInput playerInput;
    private InputAction interactAction;
    private InputAction moveAction;
    private InputAction escapeAction;
    private int playerColliderCount;
    private bool verticalHeld;
    private bool horizontalHeld;
    private RectTransform[] animatedPanelChildren = System.Array.Empty<RectTransform>();
    private Vector2[] animatedChildrenOriginalPositions = System.Array.Empty<Vector2>();
    private Coroutine panelOpenRoutine;

    protected abstract CurrencyType PaymentCurrency { get; }
    protected abstract bool HasFirstSelection { get; }

    protected virtual void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (shopPanel != null)
        {
            CacheAnimatedPanelChildren();
            shopPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerColliderCount <= 0 || playerInput == null)
            return;

        if (activeShop != this)
        {
            //============== 인벤토리가 열려 있으면 NPC 상호작용 차단 ==============
            if (IsPlayerInventoryOpen())
                return;

            if (activeShop == null && Pressed(interactAction))
            {
                OpenShop();
            }

            return;
        }

        if (Pressed(escapeAction))
        {
            if (HasFirstSelection)
                CancelFirstSelection();
            else
                CloseShop();

            return;
        }

        Vector2 move = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;

        int vertical = ReadAxisStep(move.y, ref verticalHeld);
        int horizontal = ReadAxisStep(move.x, ref horizontalHeld);

        if (horizontal != 0)
        {
            if (OnHorizontalInput(horizontal))
                return;

            if (HasFirstSelection)
            {
                CancelFirstSelection();
                return;
            }
        }

        if (vertical != 0)
            OnVerticalInput(vertical);

        //============== NPC 선택 / 구매 ==============

        if (Pressed(interactAction))
            OnAttackInput();
    }

    private static bool Pressed(InputAction action)
    {
        return action != null && action.WasPressedThisFrame();
    }

    private static int ReadAxisStep(float value, ref bool held)
    {
        bool pressed = Mathf.Abs(value) >= 0.5f;
        int step = pressed && !held ? (value > 0f ? -1 : 1) : 0;
        held = pressed;
        return step;
    }

    private void OpenShop()
    {
        if (!CanOpenShop())
            return;

        activeShop = this;
        ShopInteractionState.IsOpen = true;
        verticalHeld = false;
        horizontalHeld = false;
        PlayerController?.PauseKeyboardControl();
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            PlayPanelOpenAnimation();
        }
        SetFeedback(string.Empty);
        OnShopOpened();
    }

    public void CloseShop()
    {
        if (activeShop != this)
            return;

        OnShopClosed();
        activeShop = null;
        ShopInteractionState.IsOpen = false;
        ShopInteractionState.LastClosedFrame = Time.frameCount;
        if (shopPanel != null)
        {
            StopPanelOpenAnimation();
            shopPanel.SetActive(false);
        }
        PlayerController?.ResumeKeyboardControl();
    }

    private void PlayPanelOpenAnimation()
    {
        if (animatedPanelChildren.Length == 0)
            return;

        StopPanelOpenAnimation();
        SetAnimatedChildrenProgress(0f);

        if (panelOpenDuration <= 0f)
        {
            SetAnimatedChildrenProgress(1f);
            return;
        }

        panelOpenRoutine = StartCoroutine(PanelOpenRoutine());
    }

    private IEnumerator PanelOpenRoutine()
    {
        float elapsed = 0f;

        while (elapsed < panelOpenDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / panelOpenDuration));
            SetAnimatedChildrenProgress(progress);
            yield return null;
        }

        SetAnimatedChildrenProgress(1f);
        panelOpenRoutine = null;
    }

    private void StopPanelOpenAnimation()
    {
        if (panelOpenRoutine != null)
        {
            StopCoroutine(panelOpenRoutine);
            panelOpenRoutine = null;
        }

        SetAnimatedChildrenProgress(1f);
    }

    private void CacheAnimatedPanelChildren()
    {
        if (shopPanel == null)
            return;

        Transform panelTransform = shopPanel.transform;
        int childCount = panelTransform.childCount;
        animatedPanelChildren = new RectTransform[childCount];
        animatedChildrenOriginalPositions = new Vector2[childCount];

        for (int i = 0; i < childCount; i++)
        {
            RectTransform child = panelTransform.GetChild(i) as RectTransform;
            animatedPanelChildren[i] = child;
            if (child != null)
                animatedChildrenOriginalPositions[i] = child.anchoredPosition;
        }
    }

    private void SetAnimatedChildrenProgress(float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);
        Vector2 startOffset = Vector2.down * panelOpenOffsetY;

        for (int i = 0; i < animatedPanelChildren.Length; i++)
        {
            RectTransform child = animatedPanelChildren[i];
            if (child == null)
                continue;

            child.anchoredPosition = animatedChildrenOriginalPositions[i] +
                Vector2.LerpUnclamped(startOffset, Vector2.zero, clampedProgress);
        }
    }

    protected bool TryPay(int amount)
    {
        return TryPay(PaymentCurrency, amount);
    }

    protected bool TryPay(CurrencyType currencyType, int amount)
    {
        if (Currency != null && Currency.TrySpend(currencyType, amount))
            return true;

        SetFeedback(currencyType == global::CurrencyType.Coin ? "코인이 부족합니다." : "소울이 부족합니다.");
        return false;
    }

    protected void SaveRuntimeState()
    {
        RuntimeState?.Capture();

        if (SaveManager.Instance != null && SaveData.Instance != null && SaveData.Instance.currentSlot > 0)
            SaveManager.Instance.Save(SaveData.Instance.currentSlot);
    }

    protected void SetFeedback(string message)
    {
        if (feedbackText != null)
            feedbackText.text = message;
    }

    protected static int WrapIndex(int index, int count)
    {
        return count <= 0 ? 0 : (index % count + count) % count;
    }

    protected abstract bool CanOpenShop();
    protected abstract void OnShopOpened();
    protected abstract void OnVerticalInput(int direction);
    protected virtual bool OnHorizontalInput(int direction) { return false; }
    protected abstract void OnAttackInput();
    protected abstract void CancelFirstSelection();
    protected virtual void OnShopClosed() { }

    private void CachePlayer(PlayerController controller)
    {
        PlayerController = controller;
        GameObject player = controller.gameObject;
        playerInput = player.GetComponent<PlayerInput>();
        PlayerItemManager = player.GetComponent<PlayerItemManager>();
        Currency = player.GetComponent<CurrencyManager>();
        RuntimeState = player.GetComponent<PlayerRuntimeState>();

        if (playerInput == null)
            return;
        interactAction = playerInput.actions.FindAction(InteractActionName, false);
        moveAction = playerInput.actions.FindAction(MoveActionName, false);
        escapeAction = playerInput.actions.FindAction(EscapeActionName, false);
    }

    private bool IsPlayerInventoryOpen()
    {
        if (PlayerController == null)
            return false;

        return PlayerController.IsInventoryOpen;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController controller = other.GetComponentInParent<PlayerController>();
        if (controller == null)
            return;
        playerColliderCount++;
        CachePlayer(controller);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerController>() != PlayerController)
            return;
        playerColliderCount = Mathf.Max(0, playerColliderCount - 1);
        if (playerColliderCount == 0)
            CloseShop();
    }

    protected virtual void OnDisable()
    {
        CloseShop();
    }
}
