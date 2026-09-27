using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Speech Effect")]
    [Tooltip("전체 말풍선 효과 강도 배율. 테스트용 기본값은 3입니다.")]
    [SerializeField, Min(1f)] private float effectStrength = 3f;

    [Tooltip("말풍선이 등장할 때 순간적으로 커지는 최대 크기입니다. 1.2는 원래 크기의 1.2배입니다.")]
    [SerializeField, Min(1f)] private float popScale = 1.2f;

    [Tooltip("말풍선이 0에서 최대 크기까지 빠르게 커지는 시간입니다.")]
    [SerializeField, Min(0f)] private float popDuration = 0.08f;

    [Tooltip("최대 크기에서 원래 크기로 되돌아오는 시간입니다.")]
    [SerializeField, Min(0f)] private float settleDuration = 0.07f;

    [Tooltip("말풍선이 좌우로 흔들리는 전체 시간입니다.")]
    [SerializeField, Min(0f)] private float shakeDuration = 0.25f;

    [Tooltip("말풍선이 좌우로 회전하는 기본 각도입니다. Effect Strength가 함께 적용됩니다.")]
    [SerializeField, Min(0f)] private float shakeAngle = 5f;

    [Tooltip("흔들림 시간 동안 좌우로 왕복하는 횟수입니다.")]
    [SerializeField, Min(1)] private int shakeCount = 6;

    [Tooltip("말풍선이 사라질 때 위로 올라가는 거리입니다. Effect Strength가 함께 적용됩니다.")]
    [SerializeField, Min(0f)] private float hideRiseDistance = 0.2f;

    [Tooltip("말풍선이 위로 올라가면서 작아지고 투명해지는 시간입니다.")]
    [SerializeField, Min(0f)] private float hideEffectDuration = 0.15f;

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

    [Tooltip("자동 이동을 시작한 뒤 씬을 전환하기까지 기다리는 시간")]
    [SerializeField, Min(0f)] private float autoMoveDuration = 3f;

    [Tooltip("자동 이동 시간이 끝난 뒤 불러올 씬 이름")]
    [SerializeField] private string targetSceneName = "Stage1-1";

    [SerializeField] private string targetSpawnId;
    [SerializeField] private bool saveBeforeSceneLoad;

    private bool hasPlayed;
    private Coroutine hideRoutine;
    private Vector3 speechOriginalScale = Vector3.one;
    private Vector3 speechOriginalPosition;
    private Vector2 speechOriginalAnchoredPosition;
    private Quaternion speechOriginalRotation = Quaternion.identity;
    private RectTransform speechRectTransform;
    private SpriteRenderer[] speechRenderers;
    private Color[] speechRendererColors;
    private TMP_Text[] speechTexts;
    private Color[] speechTextColors;
    private Graphic[] speechGraphics;
    private Color[] speechGraphicColors;

    private void Reset()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;
    }

    private void Awake()
    {
        CacheReferences();
        CacheSpeechEffectReferences();
        CacheCameraFollow();
        CacheFocusCamera();
        SetSpeechVisible(false);
        RestoreSpeechVisual();
    }

    private void OnDisable()
    {
        if (hideRoutine != null)
        {
            StopCoroutine(hideRoutine);
            hideRoutine = null;
        }

        SetSpeechVisible(false);
        RestoreSpeechVisual();
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

        StartCoroutine(MoveAndZoomToFocus());
        SetSpeechVisible(true);
        PrepareSpeechVisual();

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(PlaySequence(player));
    }

    private IEnumerator PlaySequence(PlayerController player)
    {
        yield return PlayShowEffect();

        if (displayDuration > 0f)
            yield return new WaitForSeconds(displayDuration);

        yield return PlayHideEffect();
        SetSpeechVisible(false);

        if (player == null)
        {
            hideRoutine = null;
            yield break;
        }

        SceneTransitionManager transitionManager = SceneTransitionManager.GetOrCreate();

        // 이전 씬에서 시작된 로딩 연출이 완전히 끝난 뒤 다음 전환을 준비한다.
        while (transitionManager.IsTransitioning)
            yield return null;

        transitionManager.PrepareAutoExit(
            player,
            targetSceneName,
            targetSpawnId,
            saveBeforeSceneLoad,
            autoMoveSpeed);

        if (autoMoveDuration > 0f)
            yield return new WaitForSecondsRealtime(autoMoveDuration);

        if (!transitionManager.TryLoadPendingTransition(player))
        {
            Debug.LogError(
                $"[{name}] {targetSceneName} 씬 전환을 시작하지 못했습니다.",
                this);
        }

        hideRoutine = null;
    }

    private void CacheSpeechEffectReferences()
    {
        if (speechRoot == null)
            return;

        Transform rootTransform = speechRoot.transform;
        speechRectTransform = rootTransform as RectTransform;
        speechOriginalScale = rootTransform.localScale;
        speechOriginalPosition = rootTransform.localPosition;
        if (speechRectTransform != null)
            speechOriginalAnchoredPosition = speechRectTransform.anchoredPosition;
        speechOriginalRotation = rootTransform.localRotation;

        speechRenderers = speechRoot.GetComponentsInChildren<SpriteRenderer>(true);
        speechRendererColors = new Color[speechRenderers.Length];
        for (int i = 0; i < speechRenderers.Length; i++)
            speechRendererColors[i] = speechRenderers[i].color;

        speechTexts = speechRoot.GetComponentsInChildren<TMP_Text>(true);
        speechTextColors = new Color[speechTexts.Length];
        for (int i = 0; i < speechTexts.Length; i++)
            speechTextColors[i] = speechTexts[i].color;

        speechGraphics = speechRoot.GetComponentsInChildren<Graphic>(true);
        speechGraphicColors = new Color[speechGraphics.Length];
        for (int i = 0; i < speechGraphics.Length; i++)
            speechGraphicColors[i] = speechGraphics[i].color;
    }

    private void PrepareSpeechVisual()
    {
        if (speechRoot == null)
            return;

        Transform rootTransform = speechRoot.transform;
        SetSpeechPosition(speechOriginalPosition, speechOriginalAnchoredPosition);
        rootTransform.localRotation = speechOriginalRotation;
        rootTransform.localScale = Vector3.zero;
        SetSpeechAlpha(1f);
    }

    private void RestoreSpeechVisual()
    {
        if (speechRoot == null)
            return;

        Transform rootTransform = speechRoot.transform;
        SetSpeechPosition(speechOriginalPosition, speechOriginalAnchoredPosition);
        rootTransform.localRotation = speechOriginalRotation;
        rootTransform.localScale = speechOriginalScale;
        SetSpeechAlpha(1f);
    }

    private IEnumerator PlayShowEffect()
    {
        Transform rootTransform = speechRoot.transform;
        float strength = Mathf.Max(3f, effectStrength);
        float strongPopScale = 1f + (popScale - 1f) * strength;
        Vector3 overshootScale = speechOriginalScale * strongPopScale;

        yield return AnimateSpeechScale(rootTransform, Vector3.zero, overshootScale, popDuration);
        yield return AnimateSpeechScale(rootTransform, overshootScale, speechOriginalScale, settleDuration);

        if (shakeDuration <= 0f)
            yield break;

        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            yield return new WaitForEndOfFrame();
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / shakeDuration);
            float damping = 1f - progress;
            float angle = Mathf.Sin(progress * shakeCount * Mathf.PI * 2f) *
                          shakeAngle * strength * damping;
            rootTransform.localRotation = speechOriginalRotation * Quaternion.Euler(0f, 0f, angle);
        }

        rootTransform.localRotation = speechOriginalRotation;
    }

    private IEnumerator AnimateSpeechScale(
        Transform rootTransform,
        Vector3 startScale,
        Vector3 endScale,
        float duration)
    {
        if (duration <= 0f)
        {
            rootTransform.localScale = endScale;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            yield return new WaitForEndOfFrame();
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            rootTransform.localScale = Vector3.LerpUnclamped(startScale, endScale, progress);
        }

        rootTransform.localScale = endScale;
    }

    private IEnumerator PlayHideEffect()
    {
        Transform rootTransform = speechRoot.transform;
        Vector3 startPosition = speechOriginalPosition;
        float strongRiseDistance = hideRiseDistance * Mathf.Max(3f, effectStrength);
        Vector3 endPosition = speechOriginalPosition + Vector3.up * strongRiseDistance;
        Vector2 startAnchoredPosition = speechOriginalAnchoredPosition;
        Vector2 endAnchoredPosition = speechOriginalAnchoredPosition + Vector2.up * strongRiseDistance;

        if (hideEffectDuration <= 0f)
        {
            SetSpeechPosition(endPosition, endAnchoredPosition);
            rootTransform.localScale = Vector3.zero;
            SetSpeechAlpha(0f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < hideEffectDuration)
        {
            yield return new WaitForEndOfFrame();
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / hideEffectDuration));
            SetSpeechPosition(
                Vector3.Lerp(startPosition, endPosition, progress),
                Vector2.Lerp(startAnchoredPosition, endAnchoredPosition, progress));
            rootTransform.localScale = Vector3.Lerp(speechOriginalScale, Vector3.zero, progress);
            SetSpeechAlpha(1f - progress);
        }
    }

    private void SetSpeechAlpha(float alpha)
    {
        if (speechRenderers != null)
        {
            for (int i = 0; i < speechRenderers.Length; i++)
            {
                Color color = speechRendererColors[i];
                color.a *= alpha;
                speechRenderers[i].color = color;
            }
        }

        if (speechTexts != null)
        {
            for (int i = 0; i < speechTexts.Length; i++)
            {
                Color color = speechTextColors[i];
                color.a *= alpha;
                speechTexts[i].color = color;
            }
        }

        if (speechGraphics != null)
        {
            for (int i = 0; i < speechGraphics.Length; i++)
            {
                Color color = speechGraphicColors[i];
                color.a *= alpha;
                speechGraphics[i].color = color;
            }
        }
    }

    private void SetSpeechPosition(Vector3 localPosition, Vector2 anchoredPosition)
    {
        if (speechRectTransform != null)
            speechRectTransform.anchoredPosition = anchoredPosition;
        else if (speechRoot != null)
            speechRoot.transform.localPosition = localPosition;
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

    private IEnumerator MoveAndZoomToFocus()
    {
        if (focusCamera == null || !focusCamera.orthographic)
            yield break;

        Transform focusTarget = cameraFocusTarget != null ? cameraFocusTarget : transform;
        Vector3 startPosition = focusCamera.transform.position;
        Vector3 targetPosition = focusTarget.position;

        if (cameraFollow != null)
        {
            cameraFollow.target = focusTarget;
            targetPosition += cameraFollow.offset;

            if (cameraFollow.lockX)
                targetPosition.x = startPosition.x;
            if (cameraFollow.lockY)
                targetPosition.y = startPosition.y;

            targetPosition = cameraFollow.ClampToBounds(
                targetPosition,
                focusedOrthographicSize);

            cameraFollow.enabled = false;
        }
        else
        {
            targetPosition.z = startPosition.z;
        }

        float startSize = focusCamera.orthographicSize;
        float targetSize = focusedOrthographicSize;

        if (zoomDuration <= 0f)
        {
            focusCamera.transform.position = targetPosition;
            focusCamera.orthographicSize = targetSize;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < zoomDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / zoomDuration);
            progress = Mathf.SmoothStep(0f, 1f, progress);
            focusCamera.transform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                progress);
            focusCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, progress);
            yield return null;
        }

        focusCamera.transform.position = targetPosition;
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
