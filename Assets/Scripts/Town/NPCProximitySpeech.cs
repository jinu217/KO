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

    private Coroutine speechRoutine;

    private void Reset()
    {
        speechText = GetComponentInChildren<TMP_Text>(true);
        if (speechText != null)
            speechRoot = speechText.transform.parent.gameObject;
    }

    private void Awake()
    {
        CacheReferences();
        SetSpeechVisible(false);
    }

    private void OnEnable()
    {
        CacheReferences();

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

        SetSpeechVisible(true);

        if (displayDuration > 0f)
            yield return new WaitForSeconds(displayDuration);

        SetSpeechVisible(false);

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

    private void SetSpeechVisible(bool visible)
    {
        if (speechRoot != null && speechRoot.activeSelf != visible)
            speechRoot.SetActive(visible);
    }
}
