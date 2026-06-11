using UnityEngine;
using UnityEngine.UI;

public class StatusUI : MonoBehaviour
{
    [Header("연결")]
    public PlayerStatus status;

    [Header("HP UI")]
    public Image HP;
    public RectTransform hpWaveRect;

    [Header("SP UI")]
    public Image SP;

    [Header("BPM UI")]
    public Image BPM;

    [Header("HP 연출")]
    public float displayHp = 1f;
    public float hpSmoothSpeed = 5f;

    void Start()
    {
        displayHp = status.currentHp / status.maxHp;
        if (status == null) return;
    }

    void Update()
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        // -------------------- HP --------------------
        float hpPercent = status.maxHp > 0 ? status.currentHp / status.maxHp : 0;

        displayHp = Mathf.Lerp(
            displayHp,
            hpPercent,
            Time.deltaTime * hpSmoothSpeed
        );

        if (HP != null)
            HP.fillAmount = displayHp;

        // HP Wave
        if (hpWaveRect != null)
        {
            float height = HP.rectTransform.rect.height;

            Vector2 pos = hpWaveRect.anchoredPosition;

            pos.y =
                (height * displayHp)
                - (height / 2f)
                - 16f;

            float mainWave = Mathf.Sin(Time.time * 1.2f) * 6f;
            float smallWave = Mathf.Sin(Time.time * 4.5f) * 2.5f;

            pos.x = mainWave + smallWave;
            pos.y += Mathf.Sin(Time.time * 2f) * 0.5f;

            hpWaveRect.anchoredPosition = pos;
        }

        // -------------------- SP --------------------
        if (SP != null)
            SP.fillAmount = status.currentSp / status.maxSp;

        // -------------------- BPM --------------------
        if (BPM != null)
            BPM.fillAmount = status.currentBpm / 220f;
    }
}