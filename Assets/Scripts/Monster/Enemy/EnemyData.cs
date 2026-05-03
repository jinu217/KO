using UnityEngine;

// [데이터 에셋] 적 AI/스탯/공격 설정을 프리셋으로 저장하는 ScriptableObject
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "ScriptableObjects/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("기본 스탯")]
    public float maxHp = 20f;            // 적 최대 체력
    public float damage = 5f;            // 적 공격력(플레이어에게 주는 피해량)
    public float moveSpeed = 2f;         // 순찰/배회 이동 속도
    public float chaseSpeed = 3.5f;      // 플레이어 추적 속도

    [Header("인식 범위")]
    public float detectRange = 4f;       // 최초 인식 범위(노란 원)
    public float chaseRange = 7f;       // 추적 유지 범위(빨간 원)
    [Range(0, 360)]
    public float viewAngle = 180f;       // (확장용) 시야각

    [Header("AI 반응 및 지연")]
    public float idleDuration = 2.0f;        // Idle 상태 지속 시간
    public float reactionTime = 0.5f;        // 플레이어 최초 감지 후 반응 지연
    public float positionUpdateDelay = 0.5f; // 추적 방향 갱신 주기

    [Header("낭떠러지 제어")]
    public float checkDistance = 0.7f;     // groundCheck에서 아래로 쏘는 Ray 길이
    public float groundCheckOffset = 0.5f; // groundCheck의 X 오프셋(방향 전환 시 반영)
    public float wanderDuration = 0.7f;    // 절벽 회피 시 강제 이동 시간

    [Header("공격 설정")]
    public float attackSpeed = 1f;       // 초당 공격 횟수
    public float attackRange = 1.5f;     // 근거리 공격 박스 너비(X) / 원거리 fallback 사거리
    public float attackHeight = 1.5f;    // 근거리 공격 박스 높이(Y)
    public float attackHeightOffset = 0f;// 근거리 공격 박스 Y 오프셋
    public float attackOffset = 0.5f;    // 근거리 공격 박스 X 오프셋(전방 거리)

    [Header("공격 상세 시간")]
    public float preAttackDelay = 0.4f;  // 선딜레이(공격 시작 ~ 실제 판정 전)
    public float postAttackDelay = 0.5f; // 후딜레이(공격 후 경직 시간)

    [Header("원거리 전용")]
    public GameObject projectilePrefab;   // 발사할 투사체 프리팹
    public float rangedAttackRange = 0f;  // 원거리 발사 사거리(0 이하면 attackRange 사용)
}
