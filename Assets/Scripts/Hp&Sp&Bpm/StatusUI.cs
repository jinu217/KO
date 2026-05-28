using UnityEngine;
using UnityEngine.UI;

public class StatusUI : MonoBehaviour
{
    [Header("HP")]
    public float currentHp = 70f;
    public float maxHp = 100f;

    [Header("SP")]
    public float currentSp = 100f;
    public float maxSp = 100f;
    public int dashCost = 20;

    [Header("SP 회복")]
    public float spRecoverySpeed = 1f;

    [Header("BPM")]
    public float currentBpm = 91f;

    // A = 91~130
    // B = 131~170
    // C = 171~200
    // D = 201~

    [Header("BPM 증가량")]
    public float attackBpmGain = 8f;
    public float dashBpmGain = 12f;

    [Header("BPM 감소")]
    public float bpmDecreaseSpeed = 5f;

    [Header("명상")]
    public bool isMeditating = false;
    public float meditateDecreaseSpeed = 30f;

    [Header("UI")]
    // HP
    public Image HP;
    // HpWave
    public RectTransform hpWaveRect;

    // SP
    public Image SP;

    // BPM
    public Image BPM;

    void Update()
    {
        // ===== 테스트 =====

        // HP 감소
        if (Input.GetKeyDown(KeyCode.A))
            Damage(1);

        // HP 회복
        if (Input.GetKeyDown(KeyCode.S))
            Heal(1);

        // 공격
        if (Input.GetKeyDown(KeyCode.Mouse0))
            Attack();

        // 대쉬
        if (Input.GetKeyDown(KeyCode.Space))
            Dash();

        // 명상
        isMeditating = Input.GetKey(KeyCode.F);

        // BPM 감소 처리
        HandleBPMDecrease();
        // SP 회복
        RecoverSP();
        // UI 갱신
        UpdateUI();
    }

    // =========================
    // UI 업데이트
    // =========================

    void UpdateUI()
    {
        // HP
        float hpPercent = (float)currentHp / maxHp;

        // HP 기본 게이지
        if (HP != null)
            HP.fillAmount = hpPercent;

         // 웨이브 위치 이동
        if (hpWaveRect != null)
        {
            // HP바 전체 길이
            float width = HP.rectTransform.rect.width;
            // 현재 위치
            Vector2 pos = hpWaveRect.anchoredPosition;
            // HP 끝 위치 계산
            pos.y = (width * hpPercent) - (width / 2f) -7f;
            // 살짝 흔들림 추가
            pos.x = Mathf.Sin(Time.time * 5f) * 3f;
            // 적용
            hpWaveRect.anchoredPosition = pos;
        }

        // SP
        float spPercent = currentSp / maxSp;

        if (SP != null)
            SP.fillAmount = spPercent;

        // BPM
        float bpmPercent = currentBpm / 220f;

        if (BPM != null)
            BPM.fillAmount = bpmPercent;
    }

    // =========================
    // HP
    // =========================

    public void Damage(int value)
    {
        currentHp -= value;

        if (currentHp < 0)
            currentHp = 0;
    }

    public void Heal(int value)
    {
        currentHp += value;

        if (currentHp > maxHp)
            currentHp = maxHp;
    }

    // =========================
    // SP
    // =========================

    public void Dash()
    {
        // SP 부족
        if (currentSp < dashCost)
            return;

        currentSp -= dashCost;

        AddBPM(dashBpmGain);

        Debug.Log("대쉬 사용");
    }

    // =========================
    // BPM
    // =========================

    public void Attack()
    {
        AddBPM(attackBpmGain);

        Debug.Log("공격");
    }

    public void AddBPM(float amount)
    {
        currentBpm += amount;

        if (currentBpm > 220)
            currentBpm = 220;
    }

    void HandleBPMDecrease()
    {
        // B단계 이상부터 자연 감소
        if (currentBpm >= 131)
        {
            currentBpm -= bpmDecreaseSpeed * Time.deltaTime;
        }

        // 명상 중 강제 감소
        if (isMeditating)
        {
            currentBpm -= meditateDecreaseSpeed * Time.deltaTime;
        }

        // 최소 BPM 제한
        if (currentBpm < 91)
            currentBpm = 91;
    }

    // =========================
    // BPM 단계 확인
    // =========================

    public string GetBPMRank()
    {
        if (currentBpm <= 130)
            return "A";

        if (currentBpm <= 170)
            return "B";

        if (currentBpm <= 200)
            return "C";

        return "D";
    }

    void RecoverSP()
    {
        if (currentSp < maxSp)
        {
            currentSp += spRecoverySpeed * Time.deltaTime;

            if (currentSp > maxSp)
                currentSp = maxSp;
        }
    }
}