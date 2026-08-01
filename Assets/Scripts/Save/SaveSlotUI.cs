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

    [Header("Selection UI")]
    public TMP_Text startText;
    public GameObject deleteImage;

    [Header("Sprites")]
    public Sprite darkTombSprite;
    public Sprite brightTombSprite;

    private bool hasSave;

    // 저장 데이터 존재 여부 설정
    public void SetHasSave(bool value)
    {
        hasSave = value;

        // 빈 슬롯이면 선택 UI 숨김
        if (!hasSave)
        {
            if (startText != null)
                startText.gameObject.SetActive(false);

            if (deleteImage != null)
                deleteImage.SetActive(false);
        }
    }

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

        // 저장 데이터가 있는 슬롯만 START / 삭제 표시
        if (hasSave)
        {
            if (startText != null)
                startText.gameObject.SetActive(selected);

            if (deleteImage != null)
                deleteImage.SetActive(selected);
        }
        else
        {
            if (startText != null)
                startText.gameObject.SetActive(false);

            if (deleteImage != null)
                deleteImage.SetActive(false);
        }
    }

    private void SetTextAlpha(TMP_Text text, float alpha)
    {
        if (text == null)
            return;

        Color c = text.color;
        c.a = alpha;
        text.color = c;
    }
}