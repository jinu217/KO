using UnityEngine;

// [공통 시스템] 게임 전역 상태(플레이어 스탯/포션)를 보관하는 싱글톤 매니저
public class GameManager : MonoBehaviour
{
    // 어디서든 접근 가능한 전역 인스턴스
    public static GameManager Instance;

    [Header("디버그")]
    [Tooltip("초기화 시 콘솔 로그 출력")]
    [SerializeField] private bool showInitLog = false;

    [Header("플레이어 현재 스탯")]
    [Tooltip("플레이어 최대 체력")] public float playerMaxHp = 50f;
    [Tooltip("기본 공격 데미지")] public float playerAttackDamage = 10f;
    [Tooltip("초당 공격 횟수")] public float playerAttackSpeed = 1f;
    [Tooltip("크리티컬 발생 확률 (0~1)")] public float playerCriticalChance = 0.5f;
    [Tooltip("크리티컬 데미지 배율")] public float playerCriticalMultiplier = 1.5f;
    [Tooltip("방어력 (피해 감소 계산에 사용)")] public float playerDefense = 10f;

    [Header("플레이어 이동/액션 스탯")]
    [Tooltip("좌우 이동 속도")] public float playerMoveSpeed = 5f;
    [Tooltip("점프 힘 (PlayerController가 자동 계산할 수도 있음)")] public float playerJumpForce = 15f;
    [Tooltip("최대 점프 횟수")] public int playerMaxJumpCount = 1;
    [Tooltip("대쉬 속도")] public float playerDashVelocity = 20f;
    [Tooltip("대쉬 지속 시간")] public float playerDashTime = 0.2f;
    [Tooltip("대쉬 쿨다운")] public float playerDashCooldown = 1f;

    [Header("포션 설정")]
    [Tooltip("보유 포션 개수")] public int potionCount = 5;
    [Tooltip("고정 회복량")] public float potionHealAmount = 20f;
    [Tooltip("비율 회복량(고정 회복량 기준)")] public float potionHealPercent = 0.2f;

    // 씬 전체에서 단 하나만 유지되는 싱글톤 초기화
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (showInitLog)
                Debug.Log("[GameManager] 싱글톤 초기화 완료");
        }
        else
        {
            if (showInitLog)
                Debug.LogWarning("[GameManager] 중복 인스턴스 감지 - 새 인스턴스 삭제");

            Destroy(gameObject);
        }
    }
}
