using System.Collections;
using TMPro;
using UnityEngine;

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
        if (!IsPlayer(other))
            return;

        if (playOnlyOnce && hasPlayed)
            return;

        hasPlayed = true;
        ShowSpeech();
    }

    private void ShowSpeech()
    {
        if (speechRoot == null)
        {
            Debug.LogWarning($"[{name}] Speech Root가 연결되지 않았습니다.", this);
            return;
        }

        if (speechText != null)
            speechText.text = message;

        SetSpeechVisible(true);

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        if (displayDuration > 0f)
            yield return new WaitForSeconds(displayDuration);

        SetSpeechVisible(false);
        hideRoutine = null;
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

    private static bool IsPlayer(Collider2D other)
    {
        return other.CompareTag("Player") ||
               (other.attachedRigidbody != null &&
                other.attachedRigidbody.CompareTag("Player"));
    }
}
