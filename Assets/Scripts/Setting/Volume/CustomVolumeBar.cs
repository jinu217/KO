using UnityEngine;

public class CustomVolumeBar : MonoBehaviour
{
    [Range(0,1)]
    public float value = 1f;

    [SerializeField] private RectTransform fillMask;
    [SerializeField] private RectTransform handle;
    [SerializeField] private RectTransform background;

    private float width;

    void Start()
    {
        width = background.rect.width;
        Refresh();
    }

    public void SetValue(float v)
    {
        value = Mathf.Clamp01(v);
        Refresh();
    }

    void Refresh()
    {
        float fillWidth = width * value;

        // FillMask 길이 변경
        fillMask.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Horizontal,
            fillWidth);

        // Handle 이동
        handle.anchoredPosition =
            new Vector2(fillWidth, handle.anchoredPosition.y);
    }
}