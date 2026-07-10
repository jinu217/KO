using UnityEngine;
using UnityEngine.UI;

public class GlowPulse : MonoBehaviour
{
    [Header("Animation")]
    public float moveDistance = 80f;
    public float animationSpeed = 4f;

    [Header("Scale")]
    public float startScaleX = 0.2f;

    private RectTransform rect;
    private Image image;

    private Vector2 targetPos;
    private Vector2 hiddenPos;

    private Vector3 targetScale;
    private Vector3 hiddenScale;

    private bool isVisible;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();

        targetPos = rect.anchoredPosition;
        hiddenPos = targetPos + Vector2.up * moveDistance;

        targetScale = Vector3.one;
        hiddenScale = new Vector3(startScaleX, 1f, 1f);

        HideInstant();
    }

    void Update()
    {
        Vector2 pos = Vector2.Lerp(
            rect.anchoredPosition,
            isVisible ? targetPos : hiddenPos,
            Time.deltaTime * animationSpeed);

        rect.anchoredPosition = pos;

        Vector3 scale = Vector3.Lerp(
            rect.localScale,
            isVisible ? targetScale : hiddenScale,
            Time.deltaTime * animationSpeed);

        rect.localScale = scale;

        Color c = image.color;
        c.a = Mathf.Lerp(c.a, isVisible ? 1f : 0f, Time.deltaTime * animationSpeed);
        image.color = c;
    }

    public void Show()
    {
        isVisible = true;
    }

    public void Hide()
    {
        isVisible = false;
    }

    void HideInstant()
    {
        rect.anchoredPosition = hiddenPos;
        rect.localScale = hiddenScale;

        Color c = image.color;
        c.a = 0f;
        image.color = c;
    }
}