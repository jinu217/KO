using UnityEngine;

// - 저장할 게임 데이터 관리

public class SaveData : MonoBehaviour
{
    public static SaveData Instance;

    [Header("Item Restore")]
    [SerializeField] private ItemDatabase itemDatabase;
    
    public PersistentData persistent = new PersistentData();
    public RunData run = new RunData();

    // New 게임과 Load 게임 판별
    public bool isLoaded = false;
    public int currentSlot;

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

    public ItemData GetItemById(int id)
    {
        if (id < 0)
            return null;

        if (itemDatabase != null)
            return itemDatabase.GetById(id);

        Debug.LogError("[SaveData] ItemDatabase가 연결되지 않아 아이템을 복원할 수 없습니다.");
        return null;
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetForNewGame(int slot = 1)
    {
        currentSlot = Mathf.Max(1, slot);
        isLoaded = false;
        playTime = 0f;
        hp = 0f;
        sp = 0f;
        bpm = 0f;
        deathCount = 0;
        currentStage = string.Empty;
        spawnPosition = Vector3.zero;
        potionCount = 0;

        persistent = new PersistentData
        {
            nextStage = "Stage1-1"
        };
        run = new RunData();

    }

    public void ResetRun()
    {
        isLoaded = false;
        run = new RunData();
        spawnPosition = Vector3.zero;
        potionCount = 0;
    }
}
