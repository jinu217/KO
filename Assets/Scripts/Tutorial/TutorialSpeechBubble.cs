using TMPro;
using UnityEngine;

public class TutorialSpeechBubble : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Text speechText;
    [SerializeField] private SpriteRenderer bubbleBackground;
    [SerializeField] private LineRenderer tailLine;
    [SerializeField] private Transform tailTarget;

    [Header("Layout")]
    [SerializeField] private Vector2 padding = new Vector2(0.6f, 0.35f);
    [SerializeField] private Vector2 minSize = new Vector2(2.2f, 0.8f);
    [SerializeField] private Vector2 textOffset = Vector2.zero;
    [SerializeField] private Vector2 tailStartOffset = new Vector2(0f, -0.35f);
    [SerializeField] private Vector2 tailEndOffset = new Vector2(0f, 0.35f);

    private void Reset()
    {
        speechText = GetComponentInChildren<TMP_Text>(true);
        bubbleBackground = GetComponentInChildren<SpriteRenderer>(true);
        tailLine = GetComponentInChildren<LineRenderer>(true);

        if (transform.parent != null)
            tailTarget = transform.parent;
    }

    private void Awake()
    {
        CacheReferences();
        UpdateBubble();
    }

    private void OnEnable()
    {
        UpdateBubble();
    }

    private void LateUpdate()
    {
        UpdateBubble();
    }

    private void CacheReferences()
    {
        if (speechText == null)
            speechText = GetComponentInChildren<TMP_Text>(true);

        if (bubbleBackground == null)
            bubbleBackground = GetComponentInChildren<SpriteRenderer>(true);

        if (tailLine == null)
            tailLine = GetComponentInChildren<LineRenderer>(true);

        if (tailTarget == null && transform.parent != null)
            tailTarget = transform.parent;
    }

    private void UpdateBubble()
    {
        if (speechText == null)
            return;

        speechText.ForceMeshUpdate();
        Bounds textBounds = speechText.textBounds;
        Vector2 bubbleSize = new Vector2(
            Mathf.Max(minSize.x, textBounds.size.x + padding.x * 2f),
            Mathf.Max(minSize.y, textBounds.size.y + padding.y * 2f));

        if (bubbleBackground != null)
        {
            bubbleBackground.transform.localPosition = Vector3.zero;
            Vector2 spriteSize = bubbleBackground.sprite != null ? bubbleBackground.sprite.bounds.size : Vector2.one;
            bubbleBackground.transform.localScale = new Vector3(
                spriteSize.x > 0f ? bubbleSize.x / spriteSize.x : bubbleSize.x,
                spriteSize.y > 0f ? bubbleSize.y / spriteSize.y : bubbleSize.y,
                1f);
        }

        speechText.transform.localPosition = new Vector3(textOffset.x, textOffset.y, speechText.transform.localPosition.z);

        if (tailLine == null)
            return;

        tailLine.useWorldSpace = true;
        tailLine.positionCount = 2;

        Vector3 start = transform.TransformPoint(new Vector3(tailStartOffset.x, -bubbleSize.y * 0.5f + tailStartOffset.y, 0f));
        Vector3 end = tailTarget != null
            ? tailTarget.TransformPoint(new Vector3(tailEndOffset.x, tailEndOffset.y, 0f))
            : transform.position + new Vector3(0f, -1f, 0f);

        tailLine.SetPosition(0, start);
        tailLine.SetPosition(1, end);
    }
}
