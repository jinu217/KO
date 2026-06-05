using UnityEngine;

// [메인보스 데이터]
// - MainBossController/Stage1_MainBoss가 참조하는 기본공격/스킬/욕망 수치 묶음
[CreateAssetMenu(fileName = "MainBossData", menuName = "ScriptableObjects/MainBossData")]
public class MainBossData : ScriptableObject
{
    [Header("기본 스탯")]
    [Tooltip("최대 체력")] public float maxHp = 1000f;
    [Tooltip("플레이어 추적 속도")] public float chaseSpeed = 2.8f;

    [Header("인식/추적")]
    [Tooltip("최초 인식 범위(노란 원)")] public float detectRange = 12f;
    [Tooltip("추적 유지 범위(빨간 원)")] public float chaseRange = 16f;
    [Tooltip("바라보는 방향 갱신 지연")] public float positionUpdateDelay = 0.12f;
    [Tooltip("벽 감지 Ray 길이")] public float wallCheckDistance = 0.5f;

    [Header("페이즈 전환")]
    [Tooltip("페이즈2 진입 HP 비율")] public float phase2HpPercent = 0.7f;
    [Tooltip("페이즈3 진입 HP 비율")] public float phase3HpPercent = 0.3f;

    [Header("기본 공격")]
    [Tooltip("쿨다운")] public float basicCooldown = 2f;
    [Tooltip("선딜레이")] public float basicPreDelay = 0.25f;
    [Tooltip("후딜레이")] public float basicPostDelay = 0.35f;
    [Tooltip("연타 간 간격")] public float basicComboInterval = 0.22f;

    [Tooltip("세로 베기 데미지")] public float basicVerticalDamage = 7f;
    [Tooltip("가로 베기 데미지")] public float basicHorizontalDamage = 10f;

    [Tooltip("세로 베기 박스 너비")] public float basicVerticalRange = 1.5f;
    [Tooltip("세로 베기 박스 높이")] public float basicVerticalHeight = 2.4f;
    [Tooltip("세로 베기 박스 X 오프셋")] public float basicVerticalOffset = 1.2f;

    [Tooltip("가로 베기 박스 너비")] public float basicHorizontalRange = 2.4f;
    [Tooltip("가로 베기 박스 높이")] public float basicHorizontalHeight = 1.5f;
    [Tooltip("가로 베기 박스 X 오프셋")] public float basicHorizontalOffset = 1.4f;

    [Header("1스킬")]
    [Tooltip("쿨다운")] public float skill1Cooldown = 6f;
    [Tooltip("선딜레이")] public float skill1PreDelay = 0.35f;
    [Tooltip("후딜레이")] public float skill1PostDelay = 0.5f;
    [Tooltip("연타 간 간격")] public float skill1ComboInterval = 0.25f;

    [Tooltip("세로 베기 데미지")] public float skill1VerticalDamage = 7f;
    [Tooltip("가로 베기 데미지")] public float skill1HorizontalDamage = 10f;
    [Tooltip("강화 4타 마지막 가로 데미지")] public float skill1FinalHorizontalDamage = 15f;

    [Tooltip("3타 일반 넉백 파워")] public float skill1KnockbackPower = 10f;
    [Tooltip("강화/욕망 마지막 강 넉백 파워")] public float skill1StrongKnockbackPower = 15f;

    [Header("2스킬")]
    [Tooltip("쿨다운")] public float skill2Cooldown = 10f;
    [Tooltip("선딜레이")] public float skill2PreDelay = 0.45f;
    [Tooltip("후딜레이")] public float skill2PostDelay = 0.4f;
    [Tooltip("타격 데미지")] public float skill2Damage = 10f;

    [Tooltip("쿨 잔여 이하일 때 강제 시전 허용")] public float skill2ForceCastRemainingCooldown = 5f;
    [Tooltip("강제 시전 최소 거리(이보다 가까우면 시전 안 함)")] public float skill2MinDistanceToForceCast = 4f;
    [Tooltip("강제 시전 최대 사거리")] public float skill2CastRange = 9f;

    [Header("3스킬 - 공격속도 버프")]
    [Tooltip("쿨다운")] public float skill3Cooldown = 20f;
    [Tooltip("선딜레이")] public float skill3PreDelay = 0.35f;
    [Tooltip("후딜레이")] public float skill3PostDelay = 0.25f;
    [Tooltip("버프 지속 시간")] public float skill3Duration = 10f;
    [Tooltip("공격 속도 배율")] public float skill3AttackSpeedMultiplier = 1.35f;

    [Header("4스킬 - 돌진")]
    [Tooltip("쿨다운")] public float skill4Cooldown = 10f;
    [Tooltip("선딜레이")] public float skill4PreDelay = 0.75f;
    [Tooltip("후딜레이")] public float skill4PostDelay = 0.6f;
    [Tooltip("타격 데미지")] public float skill4Damage = 15f;
    [Tooltip("돌진 속도")] public float skill4DashSpeed = 15f;
    [Tooltip("돌진 지속 시간")] public float skill4DashDuration = 0.45f;
    [Tooltip("타격 박스 너비")] public float skill4Range = 2.4f;
    [Tooltip("타격 박스 높이")] public float skill4Height = 1.4f;
    [Tooltip("타격 박스 X 오프셋")] public float skill4Offset = 1.4f;

    [Header("5스킬 - 이동속도 버프")]
    [Tooltip("쿨다운")] public float skill5Cooldown = 20f;
    [Tooltip("선딜레이")] public float skill5PreDelay = 0.35f;
    [Tooltip("후딜레이")] public float skill5PostDelay = 0.25f;
    [Tooltip("버프 지속 시간")] public float skill5Duration = 10f;
    [Tooltip("이동 속도 배율")] public float skill5MoveSpeedMultiplier = 1.35f;

    [Header("욕망 스킬")]
    [Tooltip("쿨다운")] public float desireCooldown = 30f;
    [Tooltip("선딜레이")] public float desirePreDelay = 0.8f;
    [Tooltip("후딜레이")] public float desirePostDelay = 0.5f;
    [Tooltip("욕망 버프 지속 시간(이속/공속/기본 강화)")] public float desireBuffDuration = 15f;
    [Tooltip("강화 지속 시간(1스킬/2스킬 강화)")] public float desireEnhanceDuration = 10f;

    [Tooltip("공격 속도 배율")] public float desireAttackSpeedMultiplier = 1.4f;
    [Tooltip("이동 속도 배율")] public float desireMoveSpeedMultiplier = 1.25f;

    [Tooltip("욕망 상태 기본공격 쿨다운")] public float desireBasicCooldown = 1f;
    [Tooltip("욕망 상태 기본공격 세로 데미지")] public float desireBasicVerticalDamage = 8f;
    [Tooltip("욕망 상태 기본공격 가로 데미지")] public float desireBasicHorizontalDamage = 11f;

    [Tooltip("욕망 상태 1스킬 쿨다운")] public float desireSkill1Cooldown = 3f;
    [Tooltip("욕망 상태 1스킬 세로 데미지")] public float desireSkill1VerticalDamage = 10f;
    [Tooltip("욕망 상태 1스킬 가로 데미지")] public float desireSkill1HorizontalDamage = 15f;

    [Tooltip("욕망 상태 2스킬 쿨다운")] public float desireSkill2Cooldown = 5f;
    [Tooltip("욕망 상태 2스킬 데미지")] public float desireSkill2Damage = 15f;
    [Tooltip("욕망 상태 2스킬 강제 시전 잔여 쿨")] public float desireSkill2ForceCastRemainingCooldown = 3f;
    [Tooltip("욕망 상태 2스킬 발사체 개수")] public int desireSkill2ProjectileCount = 2;
}
