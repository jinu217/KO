using UnityEngine;

// [공통 시스템] 게임 전역 상태(플레이어 스탯/포션)를 보관하는 싱글톤 매니저
public class GameManager : MonoBehaviour
{
    public static GameManager Instance; // 어디서든 접근할 수 있는 전역 인스턴스

    [Header("플레이어 현재 스탯")]
    public float playerMaxHp = 50f;               // 플레이어 최대 체력
    public float playerAttackDamage = 10f;        // 기본 공격 데미지
    public float playerAttackSpeed = 1f;          // 초당 공격 횟수
    public float playerCriticalChance = 0.5f;     // 크리티컬 발생 확률 (0~1)
    public float playerCriticalMultiplier = 1.5f; // 크리티컬 데미지 배율
    public float playerDefense = 10f;             // 방어력(피해 감소 계산에 사용)

    [Header("플레이어 이동/액션 스탯")]
    public float playerMoveSpeed = 5f;            // 좌우 이동 속도
    public float playerJumpForce = 15f;           // 점프 힘
    public int playerMaxJumpCount = 1;            // 최대 점프 횟수
    public float playerDashVelocity = 20f;        // 대쉬 속도
    public float playerDashTime = 0.2f;           // 대쉬 지속 시간
    public float playerDashCooldown = 1f;         // 대쉬 쿨다운

    [Header("포션 설정")]
    public int potionCount = 5;                   // 보유 포션 개수
    public float potionHealAmount = 20f;          // 고정 회복량
    public float potionHealPercent = 0.2f;        // 비율 회복량(포션 회복량 기준)

    // 씬 전체에서 단 하나만 유지되는 싱글톤 초기화
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // 씬 전환 시에도 스탯 데이터 유지
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 중복 생성 방지
            Destroy(gameObject);
        }
    }
}
