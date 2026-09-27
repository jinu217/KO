using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialNPCSpeaker : MonoBehaviour
{
    [System.Serializable]
    public class SpeechEvent
    {
        [Tooltip("이 대사가 실행될 튜토리얼 단계입니다.")]
        public TutorialStep triggerStep;

        [Tooltip("NPC가 말풍선에 출력할 대사입니다.")]
        [TextArea(2, 4)] public string message;

        [Tooltip("이 단계가 시작된 뒤 대사를 실행하기 전까지 기다릴 시간입니다. 0이면 바로 실행됩니다.")]
        public float startDelay;

        [Tooltip("말풍선이 화면에 보이는 시간입니다.")]
        public float speechDuration = 2f;

        [Header("Movement")]
        [Tooltip("이 단계가 되면 NPC가 이동할 위치입니다. 있으면 아래 방향/거리 값보다 우선합니다.")]
        public Transform moveTarget;

        [Tooltip("Move Target을 쓰지 않을 때 NPC가 움직일 방향입니다.")]
        public Vector2 moveDirection = Vector2.right;

        [Tooltip("Move Target을 쓰지 않을 때 현재 위치에서 이동할 거리입니다.")]
        public float moveDistance = 1f;

        [Tooltip("NPC 이동 속도입니다.")]
        public float moveSpeed = 2.5f;

        [Header("Camera Focus")]
        [Tooltip("켜면 이 대사 동안 카메라가 NPC 쪽으로 줌인하고, 플레이어 조작이 잠깁니다.")]
        public bool focusCamera;

        [Tooltip("카메라가 NPC를 비추고 유지되는 총 시간입니다.")]
        public float cameraFocusDuration = 2f;

        [Tooltip("카메라 줌인 크기입니다. 값이 작을수록 더 확대됩니다.")]
        public float cameraZoomSize = 3f;

        [Header("End")]
        [Tooltip("켜면 이 이동 목표에 도착했을 때 천천히 투명해진 뒤 비활성화됩니다.")]
        public bool fadeOutAndDisableOnArrive;

        [Tooltip("투명해지는 데 걸리는 시간입니다.")]
        public float fadeOutDuration = 1.5f;

        [Tooltip("비활성화할 오브젝트입니다. 비워두면 이 NPC 오브젝트를 비활성화합니다.")]
        public GameObject objectToDisable;
    }

    [Header("Speech")]
    [Tooltip("튜토리얼 단계별로 실행할 NPC 대사 목록입니다.")]
    [SerializeField] private SpeechEvent[] speechEvents;

    [Tooltip("대사를 출력할 TextMeshPro 텍스트입니다. SpeechRoot 안의 SpeechText를 넣으면 됩니다.")]
    [SerializeField] private TMP_Text speechText;

    [Tooltip("말풍선 전체 오브젝트입니다.")]
    [SerializeField] private GameObject speechRoot;

    [Tooltip("BasicControls 단계의 시작 대사가 Start Delay 0으로 되어 있을 때 대신 적용할 기본 지연 시간입니다.")]
    [SerializeField] private float basicControlsFallbackStartDelay = 1f;

    [Header("Player Intro Before First NPC Speech")]
    [Tooltip("첫 NPC 대사를 시작하기 전에 플레이어 포커스/페이드/플레이어 대사 연출을 한 번 실행합니다.")]
    [SerializeField] private bool playPlayerIntroBeforeFirstSpeech = true;

    [Tooltip("씬 진입 시 검은 화면으로 시작하는 페이드 패널의 CanvasGroup입니다.")]
    [SerializeField] private CanvasGroup playerIntroFadeGroup;

    [Tooltip("플레이어 말풍선 전체 오브젝트입니다.")]
    [SerializeField] private GameObject playerSpeechRoot;

    [Tooltip("플레이어 말풍선에 표시할 텍스트입니다.")]
    [SerializeField] private TMP_Text playerSpeechText;

    [TextArea(2, 4)]
    [SerializeField] private string playerIntroMessage = "...";

    [Tooltip("씬 진입 후 검은 화면을 유지하는 시간입니다.")]
    [SerializeField] private float playerIntroBlackHoldDuration = 1f;

    [Tooltip("검은 화면에서 게임 화면으로 밝아지는 시간입니다.")]
    [SerializeField] private float playerIntroFadeInDuration = 1f;

    [SerializeField] private float playerIntroSpeechDuration = 1.5f;

    [Tooltip("플레이어에게 줌인하는 시간입니다. 말풍선 표시 시간보다 짧게 설정하면 빠르게 줌인한 뒤 화면을 유지합니다.")]
    [SerializeField] private float playerCameraZoomDuration = 0.35f;

    [Tooltip("검은 화면 동안 카메라가 플레이어를 비출 때 사용하는 위치 보정입니다.")]
    [SerializeField] private Vector3 playerCameraFocusOffset = new Vector3(0f, 1.2f, -10f);

    [Tooltip("플레이어 말풍선 연출 중 카메라 Orthographic Size입니다.")]
    [SerializeField] private float playerCameraZoomSize = 3f;

    [Header("Movement")]
    [Tooltip("목표 위치에 이 거리 이하로 가까워지면 도착한 것으로 봅니다.")]
    [SerializeField] private float arriveDistance = 0.03f;

    [Tooltip("NPC 카메라 줌이 시작된 후 대사를 표시하기까지의 시간입니다.")]
    [SerializeField] private float npcSpeechStartDelayAfterFocusBegins = 0.25f;

    [Tooltip("NPC 대사가 화면에 나타난 후 이동을 시작하기까지 제자리에 머무는 시간입니다.")]
    [SerializeField] private float movementDelayAfterSpeechStarts = 0.3f;

    [Header("Camera Focus")]
    [Tooltip("NPC 줌인 연출에 사용할 카메라입니다. 비워두면 Main Camera를 자동으로 찾습니다.")]
    [SerializeField] private Camera targetCamera;

    [Tooltip("기존 카메라 추적 스크립트입니다. NPC 줌인 중에는 잠시 꺼졌다가 다시 켜집니다.")]
    [SerializeField] private CameraFollow2D cameraFollow;

    [Tooltip("NPC 기준으로 카메라가 바라볼 위치 보정값입니다. 보통 z는 -10으로 둡니다.")]
    [SerializeField] private Vector3 cameraFocusOffset = new Vector3(0f, 1f, -10f);

    [Tooltip("카메라가 NPC 쪽으로 이동하거나 원래 위치로 돌아가는 데 걸리는 시간입니다.")]
    [SerializeField] private float cameraBlendTime = 0.35f;

    [Header("Player Lock")]
    [Tooltip("조작을 잠글 플레이어 컨트롤러입니다. 비워두면 Player 태그 오브젝트에서 자동으로 찾습니다.")]
    [SerializeField] private PlayerController playerController;

    public bool IsCameraFocusing => focusRoutine != null;
    public event Action<TutorialStep> CameraFocusFinished;

    private readonly HashSet<int> playedEventIndices = new HashSet<int>();
    private Coroutine speechRoutine;
    private Coroutine focusRoutine;
    private Coroutine fadeRoutine;
    private Coroutine playerIntroRoutine;
    private bool hasPlayedPlayerIntro;
    private bool hasPlayerIntroCameraState;
    private Vector3 playerIntroCameraStartPosition;
    private float playerIntroCameraStartSize;
    private bool playerIntroCameraFollowWasEnabled;
    private SpeechEvent cameraRestoreSpeechEvent;

    [Header("Animation")]
    [SerializeField] private Animator npcAnimator;
    private bool hasMoveSpeedParameter;
    private TutorialDashInstructor dashInstructor;

    private bool hasMoveTarget;
    private Vector3 currentMoveTarget;
    private float currentMoveSpeed;
    private SpeechEvent currentMoveEvent;

    private void Reset()
    {
        speechEvents = new[]
        {
            new SpeechEvent { triggerStep = TutorialStep.BasicControls, message = "Move and jump.", startDelay = 2f, focusCamera = true },
            new SpeechEvent { triggerStep = TutorialStep.BookInteractGuide, message = "Read the book." },
            new SpeechEvent { triggerStep = TutorialStep.EmpoweredDownJumpUnlocked, message = "Use empowered down jump.", focusCamera = true },
            new SpeechEvent { triggerStep = TutorialStep.DashPositionGuide, message = "Dash to the marker." },
            new SpeechEvent { triggerStep = TutorialStep.FirstDummyAttackable, message = "Attack the dummy.", focusCamera = true },
        };
    }

    private void Awake()
    {
        if (npcAnimator == null) npcAnimator = GetComponentInChildren<Animator>(true);
        dashInstructor = GetComponent<TutorialDashInstructor>();
        if (npcAnimator != null)
            foreach (var parameter in npcAnimator.parameters)
                if (parameter.name == "MoveSpeed" && parameter.type == AnimatorControllerParameterType.Float)
                    hasMoveSpeedParameter = true;
        CacheSpeechText();
        CacheReferences();
        SetSpeechVisible(false);
        SetPlayerSpeechVisible(false);

        if (playerIntroFadeGroup != null)
        {
            playerIntroFadeGroup.gameObject.SetActive(playPlayerIntroBeforeFirstSpeech);
            playerIntroFadeGroup.alpha = playPlayerIntroBeforeFirstSpeech ? 1f : 0f;
            playerIntroFadeGroup.blocksRaycasts = playPlayerIntroBeforeFirstSpeech;
            playerIntroFadeGroup.interactable = false;
        }
    }

    private void OnEnable()
    {
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.onStepChanged.AddListener(HandleStepChanged);
    }

    private void Start()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.onStepChanged.RemoveListener(HandleStepChanged);
            TutorialManager.Instance.onStepChanged.AddListener(HandleStepChanged);
            HandleStepChanged(TutorialManager.Instance.CurrentStep);
        }
    }

    private void OnDisable()
    {
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.onStepChanged.RemoveListener(HandleStepChanged);
    }

    private void Update()
    {
        Vector3 previousPosition = transform.position;
        if (dashInstructor == null || !dashInstructor.IsPerformingAttack)
            MoveTowardCurrentTarget();
        if (npcAnimator != null && hasMoveSpeedParameter)
            npcAnimator.SetFloat("MoveSpeed", Time.deltaTime > 0f
                ? Vector3.Distance(previousPosition, transform.position) / Time.deltaTime : 0f);
    }

    public bool HasFocusForStep(TutorialStep step)
    {
        SpeechEvent speechEvent = FindSpeechEvent(step);
        return speechEvent != null && speechEvent.focusCamera;
    }

    public void PlayStepNow(TutorialStep step)
    {
        SpeechEvent speechEvent = FindSpeechEvent(step);
        if (speechEvent == null)
            return;

        PlaySpeechEvent(speechEvent);
    }

    private void HandleStepChanged(TutorialStep step)
    {
        if (TutorialBookStepOpener.ShouldDelayNpcForStep(step, this))
            return;

        PlayStepNow(step);
    }

    private SpeechEvent FindSpeechEvent(TutorialStep step)
    {
        if (speechEvents == null)
            return null;

        foreach (SpeechEvent speechEvent in speechEvents)
        {
            if (speechEvent != null && speechEvent.triggerStep == step)
                return speechEvent;
        }

        return null;
    }

    private void PlaySpeechEvent(SpeechEvent speechEvent)
    {
        if (playerIntroRoutine != null)
            return;

        if (playPlayerIntroBeforeFirstSpeech && !hasPlayedPlayerIntro)
        {
            hasPlayedPlayerIntro = true;

            if (playerIntroRoutine != null)
                StopCoroutine(playerIntroRoutine);

            playerIntroRoutine = StartCoroutine(PlayerIntroThenSpeechRoutine(speechEvent));
            return;
        }

        PlaySpeechEventImmediately(speechEvent);
    }

    private void PlaySpeechEventImmediately(SpeechEvent speechEvent)
    {
        int eventIndex = Array.IndexOf(speechEvents, speechEvent);
        if (eventIndex >= 0 && playedEventIndices.Contains(eventIndex))
        {
            ApplyMoveTarget(speechEvent);
            if (speechEvent.focusCamera)
                StartCameraFocus(speechEvent);
            return;
        }

        if (eventIndex >= 0)
            playedEventIndices.Add(eventIndex);

        if (speechEvent.focusCamera)
            StartCameraFocus(speechEvent);

        StartSpeech(speechEvent);
    }

    private IEnumerator PlayerIntroThenSpeechRoutine(SpeechEvent speechEvent)
    {
        CacheReferences();
        playerController?.PauseKeyboardControl();
        bool cameraFollowWasEnabled = cameraFollow != null && cameraFollow.enabled;

        if (targetCamera != null)
        {
            hasPlayerIntroCameraState = true;
            playerIntroCameraStartPosition = targetCamera.transform.position;
            playerIntroCameraStartSize = targetCamera.orthographicSize;
            playerIntroCameraFollowWasEnabled = cameraFollowWasEnabled;
        }

        yield return new WaitForSeconds(Mathf.Max(0f, playerIntroBlackHoldDuration));

        yield return FadePlayerIntro(1f, 0f, playerIntroFadeInDuration);

        yield return ZoomToPlayerRoutine(playerCameraZoomDuration);

        if (playerSpeechText != null)
            playerSpeechText.text = playerIntroMessage;

        SetPlayerSpeechVisible(true);
        yield return new WaitForSeconds(Mathf.Max(0f, playerIntroSpeechDuration));
        SetPlayerSpeechVisible(false);

        if (cameraFollow != null)
            cameraFollow.enabled = cameraFollowWasEnabled;

        playerController?.ResumeKeyboardControl();
        playerIntroRoutine = null;
        cameraRestoreSpeechEvent = speechEvent;

        PlaySpeechEventImmediately(speechEvent);
    }

    private IEnumerator ZoomToPlayerRoutine(float duration)
    {
        if (targetCamera == null || playerController == null)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, duration));
            yield break;
        }

        if (cameraFollow != null)
            cameraFollow.enabled = false;

        Vector3 startPosition = targetCamera.transform.position;
        float startSize = targetCamera.orthographicSize;
        float safeDuration = Mathf.Max(0f, duration);

        if (safeDuration <= 0f)
        {
            Vector3 focusPosition = playerController.transform.position + playerCameraFocusOffset;
            targetCamera.transform.position = ClampCameraToBounds(focusPosition, playerCameraZoomSize);
            targetCamera.orthographicSize = playerCameraZoomSize;
            yield break;
        }

        float timer = 0f;
        while (timer < safeDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / safeDuration);
            Vector3 focusPosition = playerController.transform.position + playerCameraFocusOffset;
            float currentSize = Mathf.Lerp(startSize, playerCameraZoomSize, t);
            Vector3 currentPosition = Vector3.Lerp(startPosition, focusPosition, t);

            targetCamera.transform.position = ClampCameraToBounds(currentPosition, currentSize);
            targetCamera.orthographicSize = currentSize;
            yield return null;
        }
    }

    private IEnumerator FadePlayerIntro(float from, float to, float duration)
    {
        if (playerIntroFadeGroup == null)
            yield break;

        playerIntroFadeGroup.gameObject.SetActive(true);
        playerIntroFadeGroup.blocksRaycasts = true;
        playerIntroFadeGroup.alpha = from;

        float safeDuration = Mathf.Max(0f, duration);
        if (safeDuration <= 0f)
        {
            playerIntroFadeGroup.alpha = to;
        }
        else
        {
            float timer = 0f;
            while (timer < safeDuration)
            {
                timer += Time.deltaTime;
                playerIntroFadeGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(timer / safeDuration));
                yield return null;
            }

            playerIntroFadeGroup.alpha = to;
        }

        if (to <= 0f)
        {
            playerIntroFadeGroup.blocksRaycasts = false;
            playerIntroFadeGroup.gameObject.SetActive(false);
        }
    }

    private void ApplyMoveTarget(SpeechEvent speechEvent)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        currentMoveEvent = speechEvent;

        if (speechEvent.moveTarget != null)
        {
            currentMoveTarget = speechEvent.moveTarget.position;
        }
        else
        {
            Vector3 moveDir = new Vector3(speechEvent.moveDirection.x, speechEvent.moveDirection.y, 0f);
            if (moveDir.sqrMagnitude <= 0.0001f)
                moveDir = Vector3.right;

            currentMoveTarget = transform.position + moveDir.normalized * speechEvent.moveDistance;
        }

        currentMoveSpeed = Mathf.Max(0.01f, speechEvent.moveSpeed);
        hasMoveTarget = true;
    }

    private void MoveTowardCurrentTarget()
    {
        if (!hasMoveTarget)
            return;

        transform.position = Vector3.MoveTowards(transform.position, currentMoveTarget, currentMoveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, currentMoveTarget) > arriveDistance)
            return;

        hasMoveTarget = false;

        if (currentMoveEvent != null && currentMoveEvent.fadeOutAndDisableOnArrive && fadeRoutine == null)
            fadeRoutine = StartCoroutine(FadeOutAndDisableRoutine(currentMoveEvent));
    }

    private void StartSpeech(SpeechEvent speechEvent)
    {
        if (speechRoutine != null)
            StopCoroutine(speechRoutine);

        speechRoutine = StartCoroutine(SpeechRoutine(speechEvent));
    }

    private IEnumerator SpeechRoutine(SpeechEvent speechEvent)
    {
        if (speechEvent.focusCamera && npcSpeechStartDelayAfterFocusBegins > 0f)
            yield return new WaitForSeconds(npcSpeechStartDelayAfterFocusBegins);

        float delay = speechEvent.startDelay;
        if (!speechEvent.focusCamera &&
            speechEvent.triggerStep == TutorialStep.BasicControls &&
            delay <= 0f)
        {
            delay = basicControlsFallbackStartDelay;
        }

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (speechText != null)
            speechText.text = speechEvent.message;

        SetSpeechVisible(true);

        float moveDelay = Mathf.Max(0f, movementDelayAfterSpeechStarts);
        if (moveDelay > 0f)
            yield return new WaitForSeconds(moveDelay);

        ApplyMoveTarget(speechEvent);

        float remainingSpeechDuration = Mathf.Max(0f, speechEvent.speechDuration - moveDelay);
        if (remainingSpeechDuration > 0f)
            yield return new WaitForSeconds(remainingSpeechDuration);

        SetSpeechVisible(false);

        if (speechEvent == cameraRestoreSpeechEvent)
        {
            cameraRestoreSpeechEvent = null;
            yield return RestoreCameraAfterIntroSpeech(speechEvent);
        }

        speechRoutine = null;
    }

    private IEnumerator RestoreCameraAfterIntroSpeech(SpeechEvent speechEvent)
    {
        playerController?.PauseKeyboardControl();

        bool interruptedNpcFocus = focusRoutine != null;
        if (focusRoutine != null)
        {
            StopCoroutine(focusRoutine);
            focusRoutine = null;
        }

        if (!hasPlayerIntroCameraState || targetCamera == null)
        {
            playerController?.ResumeKeyboardControl();
            yield break;
        }

        Vector3 restoreFromPosition = targetCamera.transform.position;
        float restoreFromSize = targetCamera.orthographicSize;
        float duration = Mathf.Max(0f, cameraBlendTime);
        Vector3 restoreTargetPosition = playerIntroCameraStartPosition;

        if (cameraFollow != null && playerController != null)
            restoreTargetPosition = playerController.transform.position + cameraFollow.offset;

        restoreTargetPosition = ClampCameraToBounds(restoreTargetPosition, playerIntroCameraStartSize);

        if (cameraFollow != null)
            cameraFollow.enabled = false;

        if (duration <= 0f)
        {
            targetCamera.transform.position = restoreTargetPosition;
            targetCamera.orthographicSize = playerIntroCameraStartSize;
        }
        else
        {
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = Mathf.Clamp01(timer / duration);
                float size = Mathf.Lerp(restoreFromSize, playerIntroCameraStartSize, t);
                Vector3 position = Vector3.Lerp(restoreFromPosition, restoreTargetPosition, t);

                targetCamera.transform.position = ClampCameraToBounds(position, size);
                targetCamera.orthographicSize = size;
                yield return null;
            }
        }

        if (cameraFollow != null)
            cameraFollow.enabled = playerIntroCameraFollowWasEnabled;

        hasPlayerIntroCameraState = false;
        playerController?.ResumeKeyboardControl();

        if (interruptedNpcFocus)
            CameraFocusFinished?.Invoke(speechEvent.triggerStep);
    }

    private void StartCameraFocus(SpeechEvent speechEvent)
    {
        if (focusRoutine != null)
            StopCoroutine(focusRoutine);

        focusRoutine = StartCoroutine(CameraFocusRoutine(speechEvent));
    }

    private IEnumerator CameraFocusRoutine(SpeechEvent speechEvent)
    {
        CacheReferences();

        if (targetCamera == null)
            yield break;

        playerController?.PauseKeyboardControl();

        Vector3 cameraStartPosition = targetCamera.transform.position;
        float cameraStartSize = targetCamera.orthographicSize;
        bool cameraFollowWasEnabled = cameraFollow != null && cameraFollow.enabled;

        if (cameraFollow != null)
            cameraFollow.enabled = false;

        float timer = 0f;
        float duration = Mathf.Max(0f, speechEvent.cameraFocusDuration);

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float focusT = cameraBlendTime > 0f ? Mathf.Clamp01(timer / cameraBlendTime) : 1f;
            Vector3 focusPosition = transform.position + cameraFocusOffset;
            focusPosition = ClampCameraToBounds(focusPosition, speechEvent.cameraZoomSize);

            targetCamera.transform.position = Vector3.Lerp(cameraStartPosition, focusPosition, focusT);
            targetCamera.orthographicSize = Mathf.Lerp(cameraStartSize, speechEvent.cameraZoomSize, focusT);
            yield return null;
        }

        Vector3 restoreStartPosition = targetCamera.transform.position;
        float restoreStartSize = targetCamera.orthographicSize;
        float restoreTimer = 0f;

        while (restoreTimer < cameraBlendTime)
        {
            restoreTimer += Time.deltaTime;
            float restoreT = cameraBlendTime > 0f ? Mathf.Clamp01(restoreTimer / cameraBlendTime) : 1f;
            float restoreSize = Mathf.Lerp(restoreStartSize, cameraStartSize, restoreT);
            Vector3 restorePosition = Vector3.Lerp(restoreStartPosition, cameraStartPosition, restoreT);
            targetCamera.transform.position = ClampCameraToBounds(restorePosition, restoreSize);
            targetCamera.orthographicSize = restoreSize;
            yield return null;
        }

        if (cameraFollow != null)
            cameraFollow.enabled = cameraFollowWasEnabled;

        playerController?.ResumeKeyboardControl();
        focusRoutine = null;
        CameraFocusFinished?.Invoke(speechEvent.triggerStep);
    }

    private IEnumerator FadeOutAndDisableRoutine(SpeechEvent speechEvent)
    {
        GameObject target = speechEvent.objectToDisable != null ? speechEvent.objectToDisable : gameObject;
        float duration = Mathf.Max(0.01f, speechEvent.fadeOutDuration);
        float timer = 0f;

        SpriteRenderer[] spriteRenderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        TMP_Text[] texts = target.GetComponentsInChildren<TMP_Text>(true);
        Graphic[] graphics = target.GetComponentsInChildren<Graphic>(true);

        Color[] spriteColors = new Color[spriteRenderers.Length];
        Color[] textColors = new Color[texts.Length];
        Color[] graphicColors = new Color[graphics.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
            spriteColors[i] = spriteRenderers[i].color;

        for (int i = 0; i < texts.Length; i++)
            textColors[i] = texts[i].color;

        for (int i = 0; i < graphics.Length; i++)
            graphicColors[i] = graphics[i].color;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(timer / duration);

            for (int i = 0; i < spriteRenderers.Length; i++)
                spriteRenderers[i].color = WithAlpha(spriteColors[i], spriteColors[i].a * alpha);

            for (int i = 0; i < texts.Length; i++)
                texts[i].color = WithAlpha(textColors[i], textColors[i].a * alpha);

            for (int i = 0; i < graphics.Length; i++)
                graphics[i].color = WithAlpha(graphicColors[i], graphicColors[i].a * alpha);

            yield return null;
        }

        target.SetActive(false);
    }

    private Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private Vector3 ClampCameraToBounds(Vector3 position, float orthographicSize)
    {
        if (cameraFollow == null)
            return position;

        return cameraFollow.ClampToBounds(position, orthographicSize);
    }

    private void CacheReferences()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null && cameraFollow == null)
            cameraFollow = targetCamera.GetComponent<CameraFollow2D>();

        if (playerController == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                playerController = player.GetComponent<PlayerController>();
        }
    }

    private void SetSpeechVisible(bool visible)
    {
        if (speechRoot != null)
            speechRoot.SetActive(visible);
        else if (speechText != null)
            speechText.gameObject.SetActive(visible);
    }

    private void SetPlayerSpeechVisible(bool visible)
    {
        if (playerSpeechRoot != null)
            playerSpeechRoot.SetActive(visible);
        else if (playerSpeechText != null)
            playerSpeechText.gameObject.SetActive(visible);
    }

    private void CacheSpeechText()
    {
        if (speechText == null && speechRoot != null)
            speechText = speechRoot.GetComponentInChildren<TMP_Text>(true);

        if (speechText == null)
            speechText = GetComponentInChildren<TMP_Text>(true);

        if (speechText != null)
            speechText.text = "";
    }
}
