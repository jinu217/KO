using UnityEngine;

// 닫힌 위치에 배치한 문에 부착한다. 입장 트리거는 움직이는 문의 자식으로 두지 않는다.
[DisallowMultipleComponent]
public class BossDoor : MonoBehaviour
{
    [SerializeField, Min(0f)] private float openHeight = 5f;
    [SerializeField, Min(0.01f)] private float moveSpeed = 3f;
    [Tooltip("퇴장문에만 연결합니다. 연결된 보스가 처치되면 열립니다.")]
    [SerializeField] private MidBossController boss;

    private Vector3 closedPosition;
    private bool wantsOpen;

    private void Awake()
    {
        closedPosition = transform.position;
    }

    private void OnEnable()
    {
        if (boss == null) return;
        boss.Defeated += HandleBossDefeated;
        if (boss.IsDead) HandleBossDefeated();
    }

    private void OnDisable()
    {
        if (boss != null) boss.Defeated -= HandleBossDefeated;
    }

    private void FixedUpdate()
    {
        Vector3 target = closedPosition + (wantsOpen ? Vector3.up * openHeight : Vector3.zero);
        transform.position = Vector3.MoveTowards(transform.position, target,
            Mathf.Max(0.01f, moveSpeed) * Time.fixedDeltaTime);
    }

    public void OpenEntrance()
    {
        if (boss != null) return;
        wantsOpen = true;
    }

    public void CloseEntrance()
    {
        if (boss != null) return;
        wantsOpen = false;
    }

    private void HandleBossDefeated()
    {
        wantsOpen = true;
    }
}
