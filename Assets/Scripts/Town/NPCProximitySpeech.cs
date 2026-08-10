using System.Collections;
using TMPro;
using UnityEngine;

public class NPCProximitySpeech : MonoBehaviour
{
    [Header("Speech Bubble")]
    [Tooltip("NPC 말풍선 전체 오브젝트입니다.")]
    [SerializeField] private GameObject speechRoot;

    [Tooltip("말풍선에 표시할 TextMeshPro 텍스트입니다.")]
    [SerializeField] private TMP_Text speechText;

    [Header("Messages")]
    [Tooltip("위에서 아래 순서로 출력한 뒤 다시 첫 번째 대사부터 반복합니다.")]
    [TextArea(2, 4)]
    [SerializeField] private string[] messages;

    [Tooltip("기존 단일 대사 데이터입니다. Messages가 비어 있을 때만 사용합니다.")]
    [TextArea(2, 4)]
    [SerializeField] private string message = "안녕하세요.";

    [Header("Timing")]
    [Tooltip("씬에서 이 NPC가 활성화된 후 첫 대사를 시작하기까지 기다리는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float startDelay = 1f;

    [Tooltip("각 대사를 화면에 표시하는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float displayDuration = 1f;

    [Tooltip("말풍선을 숨긴 뒤 다음 대사를 표시하기까지 기다리는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float intervalDuration = 0.5f;

    [Header("Scale Animation")]
    [Tooltip("말풍선이 작은 크기에서 원래 크기로 커지는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float showScaleDuration = 0.2f;

    [Tooltip("말풍선이 원래 크기에서 작아지는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float hideScaleDuration = 0.15f;

    [Tooltip("등장·퇴장 시 사용할 크기 변화 곡선입니다.")]
    [SerializeField] private AnimationCurve scaleCurve =
        new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 2f),
            new Keyframe(1f, 1f, 0f, 0f));

    private Coroutine speechRoutine;
    private Vector3 originalSpeechScale = Vector3.one;

    private void Reset()
    {
        speechText = GetComponentInChildren<TMP_Text>(true);
        if (speechText != null)
            speechRoot = speechText.transform.parent.gameObject;
    }

    private void Awake()
    {
        CacheReferences();
        CacheOriginalScale();
        SetSpeechVisible(false);
    }

    private void OnEnable()
    {
        CacheReferences();
        CacheOriginalScale();

        if (speechRoutine != null)
            StopCoroutine(speechRoutine);

        speechRoutine = StartCoroutine(RepeatSpeechRoutine());
    }

    private void OnDisable()
    {
        if (speechRoutine != null)
        {
            StopCoroutine(speechRoutine);
            speechRoutine = null;
        }

        if (speechRoot != null)
            speechRoot.transform.localScale = originalSpeechScale;

        SetSpeechVisible(false);
    }

    private IEnumerator RepeatSpeechRoutine()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        while (true)
        {
            int messageCount = messages != null ? messages.Length : 0;

            if (messageCount == 0)
            {
                yield return ShowOneMessage(message);
                continue;
            }

            for (int i = 0; i < messageCount; i++)
                yield return ShowOneMessage(messages[i]);
        }
    }

    private IEnumerator ShowOneMessage(string nextMessage)
    {
        if (speechText != null)
            speechText.text = nextMessage ?? string.Empty;

        yield return AnimateSpeechScale(true, showScaleDuration);

        if (displayDuration > 0f)
            yield return new WaitForSeconds(displayDuration);

        yield return AnimateSpeechScale(false, hideScaleDuration);

        if (intervalDuration > 0f)
            yield return new WaitForSeconds(intervalDuration);
        else if (displayDuration <= 0f)
            yield return null;
    }

    private void CacheReferences()
    {
        if (speechText == null && speechRoot != null)
            speechText = speechRoot.GetComponentInChildren<TMP_Text>(true);

        if (speechRoot == null && speechText != null)
            speechRoot = speechText.transform.parent.gameObject;
    }

    private void CacheOriginalScale()
    {
        if (speechRoot == null)
            return;

        Vector3 currentScale = speechRoot.transform.localScale;
        if (currentScale.sqrMagnitude > 0f)
            originalSpeechScale = currentScale;
    }

    private IEnumerator AnimateSpeechScale(bool showing, float duration)
    {
        if (speechRoot == null)
            yield break;

        Transform speechTransform = speechRoot.transform;
        Vector3 startScale = showing ? Vector3.zero : originalSpeechScale;
        Vector3 endScale = showing ? originalSpeechScale : Vector3.zero;

        if (showing)
        {
            speechTransform.localScale = Vector3.zero;
            SetSpeechVisible(true);
        }

        if (duration <= 0f)
        {
            speechTransform.localScale = endScale;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float curvedProgress = scaleCurve != null
                    ? scaleCurve.Evaluate(progress)
                    : Mathf.SmoothStep(0f, 1f, progress);

                speechTransform.localScale = Vector3.LerpUnclamped(
                    startScale,
                    endScale,
                    curvedProgress);

                yield return null;
            }

            speechTransform.localScale = endScale;
        }

        if (!showing)
        {
            SetSpeechVisible(false);
            speechTransform.localScale = originalSpeechScale;
        }
    }

    private void SetSpeechVisible(bool visible)
    {
        if (speechRoot != null && speechRoot.activeSelf != visible)
            speechRoot.SetActive(visible);
    }
}
