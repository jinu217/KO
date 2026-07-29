using System.Collections;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class NPCProximitySpeech : MonoBehaviour
{
    [Header("Speech Bubble")]
    [Tooltip("NPC 자식에 배치한 말풍선 전체 오브젝트입니다.")]
    [SerializeField] private GameObject speechRoot;

    [Tooltip("말풍선에 표시할 TextMeshPro 텍스트입니다.")]
    [SerializeField] private TMP_Text speechText;

    [Tooltip("플레이어가 가까이 왔을 때 표시할 대사입니다.")]
    [TextArea(2, 4)]
    [SerializeField] private string message = "안녕하세요!";

    [Tooltip("말풍선이 화면에 표시되는 시간입니다.")]
    [Min(0f)]
    [SerializeField] private float displayDuration = 2.5f;

    private bool hasPlayed;
    private Coroutine hideRoutine;

    private void Reset()
    {
        Collider2D trigger = GetComponent<Collider2D>();
        trigger.isTrigger = true;

        speechText = GetComponentInChildren<TMP_Text>(true);
        if (speechText != null)
            speechRoot = speechText.transform.parent.gameObject;
    }

    private void Awake()
    {
        if (speechText == null && speechRoot != null)
            speechText = speechRoot.GetComponentInChildren<TMP_Text>(true);

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
        if (hasPlayed || !IsPlayer(other))
            return;

        hasPlayed = true;
        ShowSpeech();
    }

    private static bool IsPlayer(Collider2D other)
    {
        return other.CompareTag("Player") ||
               (other.attachedRigidbody != null &&
                other.attachedRigidbody.CompareTag("Player"));
    }

    private void ShowSpeech()
    {
        if (speechRoot == null)
        {
            Debug.LogWarning($"[{name}] NPC 말풍선 오브젝트가 연결되지 않았습니다.", this);
            return;
        }

        if (speechText != null)
            speechText.text = message;

        SetSpeechVisible(true);

        if (displayDuration > 0f)
            hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        SetSpeechVisible(false);
        hideRoutine = null;
    }

    private void SetSpeechVisible(bool visible)
    {
        if (speechRoot != null && speechRoot.activeSelf != visible)
            speechRoot.SetActive(visible);
    }
}
