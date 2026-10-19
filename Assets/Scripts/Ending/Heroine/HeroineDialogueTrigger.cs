using UnityEngine;

public class HeroineDialogueTrigger : MonoBehaviour
{
    [SerializeField] private HeroineDialogueController controller;

    private void Awake()
    {
        if (controller == null)
            controller = FindFirstObjectByType<HeroineDialogueController>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (controller != null)
            controller.TryPlay();
    }
}