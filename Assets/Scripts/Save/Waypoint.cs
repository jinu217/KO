using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// [웨이포인트]
// - 플레이어가 지나가면 자동으로 저장
// - 다음에 이어하기 시 시작할 스테이지를 저장
// - 플레이어를 자동으로 앞으로 이동시킨 후 다음 씬으로 이동
// - 죽으면 이 위치에서 부활하지 않으며, 이어하기에서만 사용된다.

[RequireComponent(typeof(Collider2D))]
public class Waypoint : MonoBehaviour
{
    [Header("다음 스테이지")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private string targetSpawnId;

    [Header("연출")]
    [SerializeField] private float delayBeforeSceneLoad = 2f;
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

        // 플레이어만 동작
        if (!other.CompareTag("Player"))
            return;

        PlayerController player = other.GetComponentInParent<PlayerController>();

        if (player == null)
            return;

        StartCoroutine(MoveSceneRoutine(player));
    }

    // 저장 → 자동 이동 → 씬 이동
    private IEnumerator MoveSceneRoutine(PlayerController player)
    {
        isMovingScene = true;

        // 현재 플레이어 상태 저장
        player.UpdateSaveData();

        // 이어하기 시 시작할 다음 스테이지 저장
        SaveData.Instance.persistent.nextStage = targetSceneName;
        SaveData.Instance.run.spawnId = targetSpawnId;

        // 저장
        SaveManager.Instance.Save(SaveData.Instance.currentSlot);

        Debug.Log($"Waypoint 저장 완료 : {targetSceneName}");

        // 플레이어 자동 이동
        player.StartAutoMoveForward(autoMoveSpeed);

        // 연출 대기
        yield return new WaitForSeconds(delayBeforeSceneLoad);

        // 조작 복구
        player.ResumeKeyboardControl();

        // 다음 씬 이동
        if (!string.IsNullOrWhiteSpace(targetSceneName))
            SceneManager.LoadScene(targetSceneName);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
