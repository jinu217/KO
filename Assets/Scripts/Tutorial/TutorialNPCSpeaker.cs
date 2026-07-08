using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

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

        [Tooltip("대사 중 NPC가 움직일 방향입니다. 예: (1, 0)은 오른쪽, (-1, 0)은 왼쪽입니다.")]
        public Vector2 moveDirection = Vector2.right;

        [Tooltip("대사 중 NPC가 움직일 거리입니다.")]
        public float moveDistance = 1f;

        [Tooltip("NPC가 목표 위치까지 이동하는 데 걸리는 시간입니다.")]
        public float moveDuration = 1f;

        [Tooltip("켜면 이 대사 동안 카메라가 NPC 쪽으로 줌인하고, 플레이어 조작이 잠깁니다.")]
        public bool focusCamera;

        [Tooltip("카메라가 NPC를 비추고 유지되는 총 시간입니다.")]
        public float cameraFocusDuration = 2f;

        [Tooltip("카메라 줌인 크기입니다. 값이 작을수록 더 확대됩니다.")]
        public float cameraZoomSize = 3f;
    }

    [Header("Speech")]
    [Tooltip("튜토리얼 단계별로 실행할 NPC 대사 목록입니다.")]
    [SerializeField] private SpeechEvent[] speechEvents;

    [Tooltip("대사를 출력할 TextMeshPro 텍스트입니다. SpeechRoot 안의 SpeechText를 넣으면 됩니다.")]
    [SerializeField] private TMP_Text speechText;

    [Tooltip("말풍선 전체 오브젝트입니다. 이 오브젝트가 켜지고 꺼지면서 말풍선 이미지와 텍스트가 같이 보입니다.")]
    [SerializeField] private GameObject speechRoot;

    [Tooltip("BasicControls 단계의 시작 대사가 Start Delay 0으로 되어 있을 때 대신 적용할 기본 지연 시간입니다.")]
    [SerializeField] private float basicControlsFallbackStartDelay = 1f;

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

    private readonly HashSet<int> playedEventIndices = new HashSet<int>();
    private Coroutine speechRoutine;

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

    private void HandleStepChanged(TutorialStep step)
    {
        if (speechEvents == null)
            return;

        for (int i = 0; i < speechEvents.Length; i++)
        {
            SpeechEvent speechEvent = speechEvents[i];
            if (speechEvent == null || speechEvent.triggerStep != step)
                continue;

            if (playedEventIndices.Contains(i))
                return;

            playedEventIndices.Add(i);

            if (speechRoutine != null)
                StopCoroutine(speechRoutine);

            speechRoutine = StartCoroutine(PlaySpeechRoutine(speechEvent));
            return;
        }
    }

    private IEnumerator PlaySpeechRoutine(SpeechEvent speechEvent)
    {
        float delay = speechEvent.startDelay;
        if (speechEvent.triggerStep == TutorialStep.BasicControls && delay <= 0f)
            delay = basicControlsFallbackStartDelay;

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (speechText != null)
            speechText.text = speechEvent.message;

        SetSpeechVisible(true);

        if (speechEvent.focusCamera && playerController != null)
            playerController.PauseKeyboardControl();

        Vector3 cameraStartPosition = Vector3.zero;
        float cameraStartSize = 0f;
        bool cameraFollowWasEnabled = false;

        if (speechEvent.focusCamera && targetCamera != null)
        {
            cameraStartPosition = targetCamera.transform.position;
            cameraStartSize = targetCamera.orthographicSize;
            cameraFollowWasEnabled = cameraFollow != null && cameraFollow.enabled;

            if (cameraFollow != null)
                cameraFollow.enabled = false;
        }

        float totalDuration = Mathf.Max(speechEvent.speechDuration, speechEvent.moveDuration);
        if (speechEvent.focusCamera)
            totalDuration = Mathf.Max(totalDuration, speechEvent.cameraFocusDuration);

        Vector3 npcStart = transform.position;
        Vector3 moveDir = new Vector3(speechEvent.moveDirection.x, speechEvent.moveDirection.y, 0f);
        if (moveDir.sqrMagnitude <= 0.0001f)
            moveDir = Vector3.right;

        Vector3 npcEnd = npcStart + moveDir.normalized * speechEvent.moveDistance;
        float timer = 0f;

        while (timer < totalDuration)
        {
            timer += Time.deltaTime;

            if (speechEvent.moveDuration > 0f)
            {
                float moveT = Mathf.Clamp01(timer / speechEvent.moveDuration);
                transform.position = Vector3.Lerp(npcStart, npcEnd, moveT);
            }

            if (speechEvent.focusCamera && targetCamera != null)
            {
                float focusT = cameraBlendTime > 0f ? Mathf.Clamp01(timer / cameraBlendTime) : 1f;
                Vector3 focusPosition = transform.position + cameraFocusOffset;
                targetCamera.transform.position = Vector3.Lerp(cameraStartPosition, focusPosition, focusT);
                targetCamera.orthographicSize = Mathf.Lerp(cameraStartSize, speechEvent.cameraZoomSize, focusT);
            }

            yield return null;
        }

        if (speechEvent.focusCamera && targetCamera != null)
        {
            Vector3 restoreStartPosition = targetCamera.transform.position;
            float restoreStartSize = targetCamera.orthographicSize;
            float restoreTimer = 0f;

            while (restoreTimer < cameraBlendTime)
            {
                restoreTimer += Time.deltaTime;
                float restoreT = cameraBlendTime > 0f ? Mathf.Clamp01(restoreTimer / cameraBlendTime) : 1f;
                targetCamera.transform.position = Vector3.Lerp(restoreStartPosition, cameraStartPosition, restoreT);
                targetCamera.orthographicSize = Mathf.Lerp(restoreStartSize, cameraStartSize, restoreT);
                yield return null;
            }

            if (cameraFollow != null)
                cameraFollow.enabled = cameraFollowWasEnabled;
        }

        if (speechEvent.focusCamera && playerController != null)
            playerController.ResumeKeyboardControl();

        SetSpeechVisible(false);
        speechRoutine = null;
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
