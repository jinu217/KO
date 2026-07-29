using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SaveSlotUI : MonoBehaviour
{
    [Header("UI")]
    public Image tombImage;
    public GameObject glowObject;
    public MonoBehaviour nameFlicker;
    public GlowPulse glowPulse;

    [Header("SaveData Text")]
    public TMP_Text playTimeText;
    public TMP_Text deathText;

    [Header("Sprites")]
    public Sprite darkTombSprite;
    public Sprite brightTombSprite;

    public void SetSelected(bool selected)
    {
        // 묘비 변경
        tombImage.sprite = selected ? brightTombSprite : darkTombSprite;

        // 후광
        if (selected)
            glowPulse.Show();
        else
            glowPulse.Hide();

        // 이름 깜빡임
        if (nameFlicker != null)
            nameFlicker.enabled = selected;

        // 선택&미선택 시 텍스트 밝기 변화
        float alpha = selected ? 1f : 0.2f;

        SetTextAlpha(playTimeText, alpha);
        SetTextAlpha(deathText, alpha);
    }

    void SetTextAlpha(TMP_Text text, float alpha)
    {
        Color c = text.color;
        c.a = alpha;
        text.color = c;
    }
}