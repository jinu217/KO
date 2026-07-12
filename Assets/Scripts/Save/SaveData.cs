using UnityEngine;

// - 저장할 게임 데이터 관리

public class SaveData : MonoBehaviour
{
    public static SaveData Instance;

    // New 게임과 Load 게임 판별
    public bool isLoaded = false;

    // 플레이 정보
    public float playTime;
    public float hp;
    public float sp;
    public float bpm;

    // 죽은 횟수
    public int deathCount;


    // 현재 진행 정보
    public string currentStage;

    // 플레이어 위치
    public Vector3 spawnPosition;

    // 인벤토리
    public int potionCount;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}