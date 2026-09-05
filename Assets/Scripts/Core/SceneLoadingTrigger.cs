using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class SceneLoadingTrigger : MonoBehaviour
{
    [SerializeField] private bool oneShot = true;
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 1f, 0.5f);

    private bool used;

    private void Reset()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (used)
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null || !player.CompareTag("Player"))
            return;

        SceneTransitionManager manager = SceneTransitionManager.GetOrCreate();
        if (!manager.TryLoadPendingTransition(player))
            return;

        if (oneShot)
            used = true;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo)
            return;

        Collider2D trigger = GetComponent<Collider2D>();
        if (trigger == null)
            return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(trigger.bounds.center, trigger.bounds.size);
    }
}
