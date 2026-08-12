using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class NPCProximitySpeechTrigger : MonoBehaviour
{
    [Header("Speech Bubble")]
    [Tooltip("활성화할 NPC 말풍선 전체 오브젝트입니다.")]
    [SerializeField] private GameObject speechRoot;

    [Tooltip("말풍선에 표시할 TextMeshPro 텍스트입니다.")]
    [SerializeField] private TMP_Text speechText;

    [TextArea(2, 4)]
    [SerializeField] private string message = "안녕하세요.";

    [Tooltip("말풍선을 화면에 표시하는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float displayDuration = 2.5f;

    [Tooltip("체크하면 이 지역에 처음 들어왔을 때 한 번만 실행합니다.")]
    [SerializeField] private bool playOnlyOnce = true;

    [Header("Trigger Sequence")]
    [Tooltip("카메라 추적 스크립트. 비워두면 Main Camera에서 자동으로 찾습니다.")]
    [SerializeField] private CameraFollow2D cameraFollow;

    [Tooltip("카메라가 포커싱할 대상. 비워두면 이 트리거 오브젝트를 사용합니다.")]
    [SerializeField] private Transform cameraFocusTarget;

    [Tooltip("확대할 카메라. 비워두면 Camera Follow가 붙은 카메라를 사용합니다.")]
    [SerializeField] private Camera focusCamera;

    [Tooltip("포커싱 시 적용할 Orthographic Size. 값이 작을수록 더 확대됩니다.")]
    [SerializeField, Min(0.01f)] private float focusedOrthographicSize = 3.5f;

    [Tooltip("현재 배율에서 포커싱 배율까지 확대되는 시간")]
    [SerializeField, Min(0f)] private float zoomDuration = 0.5f;

    [Tooltip("대사가 끝난 후 플레이어가 자동으로 이동하는 속도")]
    [SerializeField, Min(0f)] private float autoMoveSpeed = 5f;

    [Tooltip("자동 이동을 시작한 뒤 씬이 전환되기까지의 시간")]
    [SerializeField, Min(0f)] private float autoMoveDuration = 3f;

    [Tooltip("자동 이동이 끝난 후 불러올 씬 이름")]
    [SerializeField] private string targetSceneName = "Stage1-1";

    private bool hasPlayed;
    private Coroutine hideRoutine;

    private void Reset()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    private void Awake()
    {
        CacheReferences();
        CacheCameraFollow();
        CacheFocusCamera();
        SetSpeechVisible(false);
    }

    private void OnDisable()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        SetSpeechVisible(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = GetPlayer(other);
        if (player == null)
            return;

        if (playOnlyOnce && hasPlayed)
            return;

        hasPlayed = true;
        StartSequence(player);
    }

    private void StartSequence(PlayerController player)
    {
        if (speechRoot == null)
        {
            Debug.LogWarning($"[{name}] Speech Root가 연결되지 않았습니다.", this);
            return;
        }

        if (speechText != null)
            speechText.text = message;

        player.PauseKeyboardControl();

        Rigidbody2D playerBody = player.GetComponent<Rigidbody2D>();
        if (playerBody != null)
            playerBody.linearVelocity = new Vector2(0f, playerBody.linearVelocity.y);

        FocusCamera();
        StartCoroutine(ZoomInAndHold());
        SetSpeechVisible(true);

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(PlaySequence(player));
    }

    private IEnumerator PlaySequence(PlayerController player)
    {
        if (displayDuration > 0f)
            yield return new WaitForSeconds(displayDuration);

        SetSpeechVisible(false);

        if (player == null)
        {
            hideRoutine = null;
            yield break;
        }

        player.StartAutoMoveForward(autoMoveSpeed);

        if (autoMoveDuration > 0f)
            yield return new WaitForSeconds(autoMoveDuration);

        if (!string.IsNullOrWhiteSpace(targetSceneName))
            SceneManager.LoadScene(targetSceneName);

        hideRoutine = null;
    }

    private void CacheCameraFollow()
    {
        if (cameraFollow != null)
            return;

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
            cameraFollow = mainCamera.GetComponent<CameraFollow2D>();
    }

    private void CacheFocusCamera()
    {
        if (focusCamera != null)
            return;

        if (cameraFollow != null)
            focusCamera = cameraFollow.GetComponent<Camera>();

        if (focusCamera == null)
            focusCamera = Camera.main;
    }

    private void FocusCamera()
    {
        if (cameraFollow != null)
            cameraFollow.target = cameraFocusTarget != null ? cameraFocusTarget : transform;
    }

    private IEnumerator ZoomInAndHold()
    {
        if (focusCamera == null || !focusCamera.orthographic)
            yield break;

        float startSize = focusCamera.orthographicSize;
        float targetSize = focusedOrthographicSize;

        if (zoomDuration <= 0f)
        {
            focusCamera.orthographicSize = targetSize;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / zoomDuration);
            progress = Mathf.SmoothStep(0f, 1f, progress);
            focusCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, progress);
            yield return null;
        }

        focusCamera.orthographicSize = targetSize;
    }

    private void CacheReferences()
    {
        if (speechText == null && speechRoot != null)
            speechText = speechRoot.GetComponentInChildren<TMP_Text>(true);
    }

    private void SetSpeechVisible(bool visible)
    {
        if (speechRoot != null && speechRoot.activeSelf != visible)
            speechRoot.SetActive(visible);
    }

    private static PlayerController GetPlayer(Collider2D other)
    {
        if (!other.CompareTag("Player") &&
            (other.attachedRigidbody == null || !other.attachedRigidbody.CompareTag("Player")))
        {
            return null;
        }

        return other.GetComponentInParent<PlayerController>();
    }
}
