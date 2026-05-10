using UnityEngine;

// [중간보스 데이터]
// - MidBossController/Stage1_MidBoss가 참조하는 모든 패턴 수치 묶음
[CreateAssetMenu(fileName = "NewMidBossData", menuName = "ScriptableObjects/MidBossData")]
public class MidBossData : ScriptableObject
{
    [Header("기본 스탯")]
    [Tooltip("최대 체력")] public float maxHp = 300f;
    [Tooltip("플레이어 추적 속도")] public float chaseSpeed = 2.5f;

    [Header("인식/추적")]
    [Tooltip("최초 인식 범위(노란 원)")] public float detectRange = 8f;
    [Tooltip("추적 유지 범위(빨간 원)")] public float chaseRange = 12f;
    [Tooltip("바라보는 방향 갱신 지연")] public float positionUpdateDelay = 0.15f;

    [Header("기본 공격")]
    [Tooltip("기본 공격 쿨다운")] public float basicAttackCooldown = 1.5f;
    [Tooltip("선딜레이")] public float basicPreDelay = 0.25f;
    [Tooltip("후딜레이")] public float basicPostDelay = 0.45f;
    [Tooltip("연타 횟수")] public int basicAttackCount = 1;
    [Tooltip("연타 간 간격")] public float basicAttackInterval = 0.15f;
    [Tooltip("타격 데미지")] public float basicAttackDamage = 10f;
    [Tooltip("공격 박스 너비(X)")] public float basicAttackRange = 2f;
    [Tooltip("공격 박스 높이(Y)")] public float basicAttackHeight = 1.5f;
    [Tooltip("공격 박스 X 오프셋(전방 거리)")] public float basicAttackOffset = 1.2f;
    [Tooltip("공격 박스 Y 오프셋")] public float basicAttackHeightOffset = 0f;

    [Header("패턴 1")]
    [Tooltip("쿨다운")] public float pattern1Cooldown = 6f;
    [Tooltip("선딜레이")] public float pattern1PreDelay = 0.4f;
    [Tooltip("후딜레이")] public float pattern1PostDelay = 0.6f;
    [Tooltip("연타 횟수")] public int pattern1AttackCount = 1;
    [Tooltip("연타 간 간격")] public float pattern1AttackInterval = 0.2f;
    [Tooltip("타격 데미지")] public float pattern1Damage = 20f;
    [Tooltip("공격 박스 너비(X)")] public float pattern1Range = 4f;
    [Tooltip("공격 박스 높이(Y)")] public float pattern1Height = 1.8f;
    [Tooltip("공격 박스 X 오프셋")] public float pattern1Offset = 2f;
    [Tooltip("공격 박스 Y 오프셋")] public float pattern1HeightOffset = 0f;
    [Tooltip("돌진 이동 속도")] public float pattern1MoveSpeed = 10f;
    [Tooltip("돌진 지속 시간")] public float pattern1Duration = 0.35f;

    [Header("패턴 2")]
    [Tooltip("쿨다운")] public float pattern2Cooldown = 8f;
    [Tooltip("선딜레이")] public float pattern2PreDelay = 0.5f;
    [Tooltip("후딜레이")] public float pattern2PostDelay = 0.7f;
    [Tooltip("연타 횟수")] public int pattern2AttackCount = 3;
    [Tooltip("연타 간 간격")] public float pattern2AttackInterval = 0.25f;
    [Tooltip("타격 데미지(자식 클래스에서 배율 적용 가능)")] public float pattern2Damage = 14f;
    [Tooltip("공격 박스 너비(X)")] public float pattern2Range = 2.2f;
    [Tooltip("공격 박스 높이(Y)")] public float pattern2Height = 2.2f;
    [Tooltip("공격 박스 X 오프셋")] public float pattern2Offset = 2.3f;
    [Tooltip("공격 박스 Y 오프셋")] public float pattern2HeightOffset = 0f;
    [Tooltip("이동 속도(0이면 정지)")] public float pattern2MoveSpeed = 0f;
    [Tooltip("동작 지속 시간(0이면 즉시 종료)")] public float pattern2Duration = 0f;

    [Header("패턴 3")]
    [Tooltip("쿨다운")] public float pattern3Cooldown = 12f;
    [Tooltip("선딜레이")] public float pattern3PreDelay = 0.6f;
    [Tooltip("후딜레이")] public float pattern3PostDelay = 0.5f;
    [Tooltip("연타 횟수(0=직접 데미지 없음)")] public int pattern3AttackCount = 0;
    [Tooltip("연타 간 간격")] public float pattern3AttackInterval = 0f;
    [Tooltip("타격 데미지")] public float pattern3Damage = 0f;
    [Tooltip("공격 박스 너비(X) - 0이면 Gizmo 미표시")] public float pattern3Range = 0f;
    [Tooltip("공격 박스 높이(Y) - 0이면 Gizmo 미표시")] public float pattern3Height = 0f;
    [Tooltip("공격 박스 X 오프셋")] public float pattern3Offset = 0f;
    [Tooltip("공격 박스 Y 오프셋")] public float pattern3HeightOffset = 0f;
    [Tooltip("이동 속도(0이면 정지)")] public float pattern3MoveSpeed = 0f;
    [Tooltip("버프 지속 시간")] public float pattern3Duration = 5f;

    [Header("패턴3 버프 배율")]
    [Tooltip("이동 속도 배율")] public float buffMoveSpeedMultiplier = 1.25f;
    [Tooltip("공격 데미지 배율")] public float buffDamageMultiplier = 1.35f;
    [Tooltip("쿨다운 배율(1보다 작을수록 더 자주)")] public float buffCooldownMultiplier = 0.75f;

    [Header("벽 감지")]
    [Tooltip("wallCheck에서 전방으로 쏘는 Ray 길이")] public float wallCheckDistance = 0.5f;
}
