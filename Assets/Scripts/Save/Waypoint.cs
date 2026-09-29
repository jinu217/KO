using UnityEngine;

// [웨이포인트]
// - 플레이어가 지나가면 자동 전진을 시작
// - 앞쪽의 SceneLoadingTrigger에 도달하면 저장 후 다음 씬으로 이동
// - 죽으면 이 위치에서 부활하지 않으며, 이어하기에서만 사용된다.

[RequireComponent(typeof(Collider2D))]
public class Waypoint : MonoBehaviour
{
    [Header("다음 스테이지")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnId;

    [Header("연출")]
    [SerializeField] private float autoMoveSpeed = 5f;

    // 씬 이동 중복 실행 방지
    private bool isMovingScene;

    // Collider를 Trigger로 자동 설정
    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    // 플레이어가 웨이포인트에 진입했을 때
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 이미 이동 중이면 무시
        if (isMovingScene)
            return;

        if (!StageClearManager.CanLeave(gameObject.scene))
            return;

        // 플레이어만 동작
        if (!other.CompareTag("Player"))
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        isMovingScene = true;
        SceneTransitionManager.GetOrCreate().PrepareAutoExit(
            player,
            targetSceneName,
            targetSpawnId,
            true,
            autoMoveSpeed);
    }
}
