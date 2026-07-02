using UnityEngine;

// - 저장할 게임 데이터 관리

public class GameData : MonoBehaviour
{
    public static GameData Instance;

    // 플레이 정보
    public float playTime;
    public float hp;
    public float sp;
    public float bpm;

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