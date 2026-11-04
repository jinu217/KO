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

    //============== 일반 BPM UI (4조각) ==============
    [Header("일반 BPM UI")]
    [Tooltip("기존 BPM UI 4조각. 0번이 2단계(기계장비 2개)에서, 1번이 3단계에서 ... 3번이 5단계에서 사라집니다.")]
    public Image[] baseBpmImages;

    [Tooltip("기존 BPM 조각이 사라지기 시작하는 기계장비 개수 (2 = 2개 착용부터)")]
    [SerializeField] private int replaceStartStage = 2;
    //================================================

    //============== 기계 BPM UI ==============
    [Header("기계 BPM UI")]
    [Tooltip("1~5단계 기계 BPM 오브젝트(Image). 0번=1단계 ... 4번=5단계. 씬에서는 비활성화 상태로 둬도 됩니다. 착용 개수만큼 1단계부터 차례대로 쌓여서 표시됩니다.")]
    public Image[] machineBpmImages;

    private int lastMachineCount = -1;
    //========================================

    [Header("HP 연출")]
    public float displayHp = 1f;
    public float hpSmoothSpeed = 5f;

    void Start()
    {
        if (status == null) return;

        displayHp = status.maxHp > 0f
            ? status.currentHp / status.maxHp
            : 0f;

        UpdateMachineBpmUI();
    }

    void Update()
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        if (status == null) return;

        // -------------------- HP --------------------
        float hpPercent = status.maxHp > 0
            ? status.currentHp / status.maxHp
            : 0f;

        displayHp = Mathf.Lerp(
            displayHp,
            hpPercent,
            Time.deltaTime * hpSmoothSpeed
        );

        if (HP != null)
            HP.fillAmount = displayHp;

        // HP Wave
        if (hpWaveRect != null && HP != null)
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
        if (SP != null && status.maxSp > 0)
            SP.fillAmount = status.currentSp / status.maxSp;

        //============== 기계장비 개수 확인 (fillAmount 대입보다 먼저) ==============
        UpdateMachineBpmUI();

        // -------------------- BPM --------------------
        float bpmFill = status.currentBpm / 220f;

        // 켜져 있는 기존 BPM 조각과 기계 BPM 이미지 모두 같은 양만큼 채움
        ApplyFill(baseBpmImages, bpmFill);
        ApplyFill(machineBpmImages, bpmFill);
    }

    //============== 켜져 있는 이미지에 fillAmount 적용 ==============
    void ApplyFill(Image[] images, float fill)
    {
        if (images == null) return;

        for (int i = 0; i < images.Length; i++)
        {
            Image img = images[i];

            if (img != null && img.gameObject.activeSelf)
                img.fillAmount = fill;
        }
    }

    //============== 기계 BPM UI 갱신 ==============
    void UpdateMachineBpmUI()
    {
        int count = 0;

        if (EndingManager.Instance != null)
            count = EndingManager.Instance.MachineEquipmentCount;

        count = Mathf.Clamp(count, 0, 5);

        // 개수가 바뀐 순간에만 켜고 끄기를 처리
        if (count == lastMachineCount)
            return;

        lastMachineCount = count;

        ApplyBaseBpmStage(count);
        ApplyMachineStage(count);
    }

    //============== 기존 BPM 조각 : 단계마다 1개씩 숨김 ==============
    void ApplyBaseBpmStage(int count)
    {
        if (baseBpmImages == null) return;

        // 2개 착용 → 1조각 숨김, 3개 → 2조각, ... (0~1개는 0조각)
        int hiddenCount = Mathf.Clamp(
            count - replaceStartStage + 1,
            0,
            baseBpmImages.Length
        );

        for (int i = 0; i < baseBpmImages.Length; i++)
        {
            if (baseBpmImages[i] == null) continue;

            baseBpmImages[i].gameObject.SetActive(i >= hiddenCount);
        }
    }

    //============== 기계 BPM : 착용 개수만큼 1단계부터 차례로 쌓아서 표시 ==============
    void ApplyMachineStage(int count)
    {
        if (machineBpmImages == null || machineBpmImages.Length == 0)
        {
            if (count > 0)
                Debug.LogWarning("기계 BPM UI 이미지가 연결되지 않았습니다.");

            return;
        }

        if (count > machineBpmImages.Length)
        {
            Debug.LogWarning(
                $"기계 BPM UI가 {count}단계까지 필요한데 {machineBpmImages.Length}개만 연결되어 있습니다."
            );
        }

        // 배열 길이를 넘지 않게 보정 (0이면 전부 끔)
        int stage = Mathf.Min(count, machineBpmImages.Length);

        for (int i = 0; i < machineBpmImages.Length; i++)
        {
            if (machineBpmImages[i] == null) continue;

            // 0번(1단계)부터 현재 단계까지 전부 표시
            machineBpmImages[i].gameObject.SetActive(i < stage);
        }
    }
}