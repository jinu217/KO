using UnityEngine;

// [데이터 에셋] 적 AI/스탯/공격 설정을 프리셋으로 저장하는 ScriptableObject
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "ScriptableObjects/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("기본 스탯")]
    [Tooltip("적 최대 체력")] public float maxHp = 20f;
    [Tooltip("플레이어에게 주는 피해량")] public float damage = 5f;
    [Tooltip("순찰/배회 이동 속도")] public float moveSpeed = 2f;
    [Tooltip("플레이어 추적 속도")] public float chaseSpeed = 3.5f;

    [Header("인식 범위")]
    [Tooltip("최초 인식 범위(노란 원)")] public float detectRange = 4f;
    [Tooltip("추적 유지 범위(빨간 원)")] public float chaseRange = 7f;
    [Tooltip("(확장용) 시야각")]
    [Range(0f, 360f)] public float viewAngle = 180f;

    [Header("AI 반응 및 지연")]
    [Tooltip("Idle 상태 지속 시간")] public float idleDuration = 2.0f;
    [Tooltip("플레이어 최초 감지 후 반응 지연")] public float reactionTime = 0.5f;
    [Tooltip("추적 방향 갱신 주기")] public float positionUpdateDelay = 0.5f;

    [Header("낭떠러지 제어")]
    [Tooltip("groundCheck에서 아래로 쏘는 Ray 길이")] public float checkDistance = 0.7f;
    [Tooltip("groundCheck의 X 오프셋(방향 전환 시 반영)")] public float groundCheckOffset = 0.5f;
    [Tooltip("절벽 회피 시 강제 이동 시간")] public float wanderDuration = 0.7f;

    [Header("공격 설정")]
    [Tooltip("초당 공격 횟수")] public float attackSpeed = 1f;
    [Tooltip("근거리 공격 박스 너비(X) / 원거리 fallback 사거리")] public float attackRange = 1.5f;
    [Tooltip("근거리 공격 박스 높이(Y)")] public float attackHeight = 1.5f;
    [Tooltip("근거리 공격 박스 Y 오프셋")] public float attackHeightOffset = 0f;
    [Tooltip("근거리 공격 박스 X 오프셋(전방 거리)")] public float attackOffset = 0.5f;

    [Header("공격 상세 시간")]
    [Tooltip("선딜레이(공격 시작 ~ 실제 판정 전)")] public float preAttackDelay = 0.4f;
    [Tooltip("후딜레이(공격 후 경직 시간)")] public float postAttackDelay = 0.5f;

    [Header("원거리 전용")]
    [Tooltip("발사할 투사체 프리팹")] public GameObject projectilePrefab;
    [Tooltip("원거리 발사 사거리(0 이하면 attackRange 사용)")] public float rangedAttackRange = 0f;

    [Header("피격 반응")]
    [Tooltip("몸무게(1=기준. 낮을수록 가벼워 넉백이 강하고 이동이 빠름, 높을수록 무거워 넉백이 약하고 이동이 느림)")]
    [Range(0.1f, 10f)] public float weight = 1f;
    [Tooltip("피격 시 행동 정지 시간")] public float hitStunDuration = 0.15f;

    [Header("벽 충돌 보정")]
    [Tooltip("벽에 넉백 충돌 시 추가 피해")] public float wallBonusDamage = 0f;
    [Tooltip("벽에 넉백 충돌 시 추가 경직")] public float wallStunDuration = 0.2f;
}
