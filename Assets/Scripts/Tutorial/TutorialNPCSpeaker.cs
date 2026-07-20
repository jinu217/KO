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

    [Header("Movement")]
    [Tooltip("목표 위치에 이 거리 이하로 가까워지면 도착한 것으로 봅니다.")]
    [SerializeField] private float arriveDistance = 0.03f;

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
        CacheSpeechText();
        CacheReferences();
        SetSpeechVisible(false);
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
        MoveTowardCurrentTarget();
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

        ApplyMoveTarget(speechEvent);
        StartSpeech(speechEvent);

        if (speechEvent.focusCamera)
            StartCameraFocus(speechEvent);
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
        float delay = speechEvent.startDelay;
        if (speechEvent.triggerStep == TutorialStep.BasicControls && delay <= 0f)
            delay = basicControlsFallbackStartDelay;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (speechText != null)
            speechText.text = speechEvent.message;

        SetSpeechVisible(true);
        yield return new WaitForSeconds(Mathf.Max(0f, speechEvent.speechDuration));
        SetSpeechVisible(false);
        speechRoutine = null;
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
