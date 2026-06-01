using UnityEngine;
using UnityEngine.UI;

public class StatusUI : MonoBehaviour
{
    [Header("HP")]
    public float currentHp = 70f;
    public float maxHp = 100f;

    [Header("HP 부드러운 감소")]
    public float displayHp = 1f;

    public float hpSmoothSpeed = 5f;

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
    public float bpmDecreaseSpeed = 2f;

    [Header("명상")]
    public bool isMeditating = false;
    public float meditateDecreaseSpeed = 15f;

    [Header("BPM 고정 포션")]
    public bool bpmLocked = false;
    public float bpmLockTimer = 0f;
    public float lockedBpm = 91f;

    [Header("UI")]
    
    // HP
    public Image HP;

    // HpWave
    public RectTransform hpWaveRect;

    // SP
    public Image SP;

    // BPM
    public Image BPM;

    void Start()
    {
        // 시작 시 현재 HP로 초기화
        displayHp = currentHp / maxHp;
    }

    void Update()
    {
        // ===== 테스트 =====

        // HP 감소
        if (Input.GetKeyDown(KeyCode.A))
            Damage(10);

        // HP 회복
        if (Input.GetKeyDown(KeyCode.S))
            Heal(10);

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
        // =========================
        // HP
        // =========================

        float hpPercent = currentHp / maxHp;

        // 부드럽게 HP 따라가기
        displayHp = Mathf.Lerp(
            displayHp,
            hpPercent,
            Time.deltaTime * hpSmoothSpeed
        );

        // HP 게이지 적용
        if (HP != null)
        {
            HP.fillAmount = displayHp;
        }

        // =========================
        // HP 웨이브
        // =========================

        if (hpWaveRect != null)
        {
            // HP바 세로 길이
            float height = HP.rectTransform.rect.height;

            Vector2 pos = hpWaveRect.anchoredPosition;

            // HP 끝 위치 계산
            pos.y =
                (height * displayHp)
                - (height / 2f)
                - 16f;

            // =========================
            // 자연스러운 물결
            // =========================

            // 큰 흐름
            float mainWave =
                Mathf.Sin(Time.time * 1.2f) * 6f;

            // 잔물결
            float smallWave =
                Mathf.Sin(Time.time * 4.5f) * 2.5f;

            // 흔들림 합치기
            pos.x = mainWave + smallWave;

            // 아주 살짝 위아래 출렁임
            pos.y += Mathf.Sin(Time.time * 2f) * 0.5f;

            // 적용
            hpWaveRect.anchoredPosition = pos;
        }

        // =========================
        // SP
        // =========================

        float spPercent = currentSp / maxSp;

        if (SP != null)
        {
            SP.fillAmount = spPercent;
        }

        // =========================
        // BPM
        // =========================

        float bpmPercent = currentBpm / 220f;

        if (BPM != null)
        {
            BPM.fillAmount = bpmPercent;
        }
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

        if (!bpmLocked)
        {
            AddBPM(dashBpmGain);
        }

        Debug.Log("대쉬 사용");
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

    // =========================
    // BPM
    // =========================

    public void Attack()
    {
        if (!bpmLocked)
        {
            AddBPM(attackBpmGain);
        }

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
        // BPM 고정 포션 효과
        if (bpmLocked)
        {
            currentBpm = lockedBpm;

            bpmLockTimer -= Time.deltaTime;

            if (bpmLockTimer <= 0)
            {
                bpmLocked = false;
            }

            return;
        }

        // 항상 자연 감소
        currentBpm -= bpmDecreaseSpeed * Time.deltaTime;

        // 명상 중이면 추가 감소
        if (isMeditating)
        {
            currentBpm -= meditateDecreaseSpeed * Time.deltaTime;
        }

        // 최소 BPM
        if (currentBpm < 60f)
        {
            currentBpm = 60f;
        }
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
}