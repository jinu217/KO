using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[DisallowMultipleComponent]
public class BossDoorTrigger : MonoBehaviour
{
    public enum DoorAction { Open, Close }

    [SerializeField] private BossDoor door;
    [SerializeField] private DoorAction action;

    private void Reset()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (door == null || other.GetComponentInParent<PlayerController>() == null) return;

        if (action == DoorAction.Open) door.OpenEntrance();
        else door.CloseEntrance();
    }
}
