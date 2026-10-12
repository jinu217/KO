using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("피격 연출")]
    [Tooltip("플레이어 피격 시 깜빡이는 지속 시간(초)")]
    [Min(0f)] public float hitFlashDuration = 0.2f;
    [Tooltip("일반 몬스터 피격 시 깜빡이는 지속 시간(초)")]
    [Min(0f)] public float enemyHitFlashDuration = 0.1f;
    [Tooltip("피격 깜빡임의 켜짐/꺼짐 전환 간격(초)")]
    [Min(0.001f)] public float hitFlashInterval = 0.04f;

    public static GameManager Instance;

    [Header("Debug")]
    [SerializeField] private bool showInitLog;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);

            if (showInitLog)
                Debug.Log("[GameManager] initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        // 게임 플레이 시간 누적
        if (SaveData.Instance == null)
            return;

        // 일시정지 중에는 플레이 시간 증가하지 않음
        if (Time.timeScale == 0f)
            return;

        SaveData.Instance.persistent.playTime += Time.deltaTime;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
