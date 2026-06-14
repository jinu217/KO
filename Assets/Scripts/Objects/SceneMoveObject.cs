using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class SceneMoveObject : MonoBehaviour
{
    [SerializeField] private string targetSceneName;
    [SerializeField] private float delayBeforeSceneLoad = 2f;
    [SerializeField] private float autoMoveSpeed = 5f;

    private bool isMovingScene;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isMovingScene || !other.CompareTag("Player")) return;
        if (gameObject.layer != LayerMask.NameToLayer("sceneMoveObeject")) return;

        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        StartCoroutine(MoveSceneRoutine(player));
    }

    private IEnumerator MoveSceneRoutine(PlayerController player)
    {
        isMovingScene = true;

        player.StartAutoMoveForward(autoMoveSpeed);

        yield return new WaitForSeconds(delayBeforeSceneLoad);

        player.ResumeKeyboardControl();

        if (!string.IsNullOrWhiteSpace(targetSceneName))
            SceneManager.LoadScene(targetSceneName);
        else
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
