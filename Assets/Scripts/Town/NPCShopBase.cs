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

    private const string InteractActionName = "Interact";
    private const string MoveActionName = "Move";
    private const string AttackActionName = "Attack";
    private const string EscapeActionName = "ESC";

    protected PlayerController PlayerController { get; private set; }
    protected PlayerItemManager PlayerItemManager { get; private set; }
    protected CurrencyManager Currency { get; private set; }
    protected PlayerRuntimeState RuntimeState { get; private set; }

    private PlayerInput playerInput;
    private InputAction interactAction;
    private InputAction moveAction;
    private InputAction attackAction;
    private InputAction escapeAction;
    private int playerColliderCount;
    private bool verticalHeld;
    private bool horizontalHeld;

    protected abstract CurrencyType PaymentCurrency { get; }
    protected abstract bool HasFirstSelection { get; }

    protected virtual void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    private void Update()
    {
        if (playerColliderCount <= 0 || playerInput == null)
            return;

        if (activeShop != this)
        {
            if (activeShop == null && Pressed(interactAction))
                OpenShop();
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

        if (Pressed(attackAction))
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
            shopPanel.SetActive(true);
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
            shopPanel.SetActive(false);
        PlayerController?.ResumeKeyboardControl();
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
        attackAction = playerInput.actions.FindAction(AttackActionName, false);
        escapeAction = playerInput.actions.FindAction(EscapeActionName, false);
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
