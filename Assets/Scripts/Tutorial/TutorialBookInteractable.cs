using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialBookInteractable : MonoBehaviour
{
    [SerializeField] private float interactRange = 2f;
    [SerializeField] private GameObject readingPanel;
    [SerializeField] private bool completeWhenClosed = true;

    private Transform player;
    private PlayerInput playerInput;
    private InputAction interactAction;
    private bool isReading;

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
        if (playerInput == null || playerInput.actions == null)
        {
            enabled = false;
            return;
        }

        interactAction = playerInput.actions["Interact"];
        if (interactAction == null)
            enabled = false;
    }

    private void Update()
    {
        if (player == null || interactAction == null)
            return;

        TutorialManager manager = TutorialManager.Instance;
        if (manager == null || manager.CurrentStep != TutorialStep.BookInteractGuide)
            return;

        if (Vector2.Distance(transform.position, player.position) > interactRange)
            return;

        if (!interactAction.WasPressedThisFrame())
            return;

        if (isReading)
        {
            CloseBook();
            if (completeWhenClosed)
                manager.NotifyBookInteracted();

            return;
        }

        OpenBook();
    }

    public void CompleteReading()
    {
        CloseBook();
        TutorialManager.Instance?.NotifyBookInteracted();
    }

    private void OpenBook()
    {
        isReading = true;
        if (readingPanel != null)
            readingPanel.SetActive(true);
    }

    private void CloseBook()
    {
        isReading = false;
        if (readingPanel != null)
            readingPanel.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
