using UnityEngine;

// 게임매니저
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("플레이어 현재 스탯")]
    public float playerMaxHp = 50f;               // 최대 체력
    public float playerAttackDamage = 10f;        // 공격력
    public float playerAttackSpeed = 1f;          // 초당 공격 횟수
    public float playerCriticalChance = 0.5f;     // 크리티컬 확률 (50%)
    public float playerCriticalMultiplier = 1.5f; // 크리티컬 데미지 배율
    public float playerDefense = 10f;             // 방어력

    [Header("포션 설정")]
    public int potionCount = 5;                   // 포션 개수 (초기값)
    public float potionHealAmount = 20f;          // 포션 회복량 (고정)
    public float potionHealPercent = 0.2f;        // 포션 회복 % (최대 HP의 20%)

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 씬이 바뀌어도 플레이어 정보(체력, 강화 상태 등)가 유지되도록 설정
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}