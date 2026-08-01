using UnityEngine;

public class GameManager : MonoBehaviour
{
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