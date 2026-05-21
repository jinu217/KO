using UnityEngine;
using UnityEngine.UI;

public class HPUI : MonoBehaviour
{
    [Header("HP")]
    public int currentHp = 7;
    public int maxHp = 10;

    // =========================
    // 1. 하트형
    // =========================

    public Image[] hearts;

    public Sprite fullHeart;
    public Sprite halfHeart;
    public Sprite emptyHeart;

    // =========================
    // 2. 가로 바
    // =========================

    public Image horizontalBar;

    // Image Type = Filled
    // Fill Method = Horizontal

    // =========================
    // 3. 세로 바
    // =========================

    public Image verticalBar;

    // Image Type = Filled
    // Fill Method = Vertical

    // =========================
    // 4. 원형 게이지
    // =========================

    public Image circleBar;

    // Image Type = Filled
    // Fill Method = Radial 360

    // =========================
    // 5. 반원 게이지
    // =========================

    public Image semiCircleBar;

    // Image Type = Filled
    // Fill Method = Radial 360
    // 반원 이미지를 사용

    void Update()
    {
        // 테스트
        if (Input.GetKeyDown(KeyCode.A))
            Damage(1);

        if (Input.GetKeyDown(KeyCode.S))
            Heal(1);

        UpdateUI();
    }

    // 전체 UI 업데이트
    void UpdateUI()
    {
        float percent = (float)currentHp / maxHp;

        // =========================
        // 하트
        // =========================

        for (int i = 0; i < hearts.Length; i++)
        {
            int value = (i * 2) + 2;

            if (currentHp >= value)
                hearts[i].sprite = fullHeart;

            else if (currentHp == value - 1)
                hearts[i].sprite = halfHeart;

            else
                hearts[i].sprite = emptyHeart;
        }

        // =========================
        // 바 / 원형 공통
        // =========================

        if (horizontalBar != null)
            horizontalBar.fillAmount = percent;

        if (verticalBar != null)
            verticalBar.fillAmount = percent;

        if (circleBar != null)
            circleBar.fillAmount = percent;

        if (semiCircleBar != null)
            semiCircleBar.fillAmount = percent;
    }

    // 데미지
    public void Damage(int value)
    {
        currentHp -= value;

        if (currentHp < 0)
            currentHp = 0;
    }

    // 회복
    public void Heal(int value)
    {
        currentHp += value;

        if (currentHp > maxHp)
            currentHp = maxHp;
    }
}