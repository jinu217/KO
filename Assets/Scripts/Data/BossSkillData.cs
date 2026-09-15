using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class BossDataBase : ScriptableObject
{
    [Header("기본 스탯")]
    [Tooltip("최대 체력")] public float maxHp;
    [Tooltip("플레이어 추적 속도")] public float chaseSpeed;
    [Tooltip("최초 인식 범위")] public float detectRange;
    [Tooltip("추적 유지 범위")] public float chaseRange;
    [Tooltip("방향 갱신 지연")] public float positionUpdateDelay;
    [Tooltip("벽 감지 거리")] public float wallCheckDistance;
    [Header("기본 공격")] public BossSkillData basicAttack = new BossSkillData { skillName = "기본 공격", skillType = BossSkillType.Active };
    [Header("일반 스킬")]
    [Tooltip("목록 크기가 일반 스킬 개수입니다. 욕망 같은 보스 전용기는 포함하지 않습니다.")]
    public List<BossSkillData> skills = new List<BossSkillData>();
}

public enum BossSkillType
{
    Active,
    Buff,
    ActiveAndBuff
}

public enum BossSkillExecutionOrder
{
    ActiveThenBuff,
    BuffThenActive,
    Simultaneous
}

public enum BossActionType
{
    None,
    Wait,
    MeleeAttack,
    MoveForward,
    DashAttack,
    Jump,
    Slam,
    SpawnPrefab,
    PersistentHitbox
}

public enum BossBuffStat
{
    MoveSpeed,
    AttackSpeed,
    Damage,
    Defense,
    Cooldown,
    BasicAttackCooldown,
    MaxHp
}

public enum BossBuffValueMode
{
    Add,
    Multiply,
    Percent
}

[Serializable]
public class BossAttackStepData
{
    [Tooltip("인스펙터에서 구분하기 위한 공격 단계 이름입니다.")]
    public string stepName;
    [Tooltip("이 단계에서 실행할 행동입니다. '없음'을 선택하면 아무 행동도 하지 않습니다.")]
    public BossActionType actionType;

    [Header("타이밍")]
    [Tooltip("이 단계의 행동이 시작되기 전 대기시간(초)입니다. 0이면 즉시 시작합니다.")]
    [Min(0f)] public float preDelay;
    [Tooltip("이 단계의 행동이 끝난 뒤 대기시간(초)입니다. 0이면 바로 다음 단계로 넘어갑니다.")]
    [Min(0f)] public float postDelay;
    [Tooltip("이동, 돌진 등 지속 행동의 실행 시간(초)입니다. 0이면 지속 행동을 실행하지 않습니다.")]
    [Min(0f)] public float duration;
    [Tooltip("연속 공격 또는 반복 생성 사이의 간격(초)입니다. 0이면 지연 없이 실행합니다.")]
    [Min(0f)] public float interval;

    [Header("공격")]
    [Tooltip("이 단계에서 실행할 공격 횟수입니다. 0이면 공격 판정을 실행하지 않습니다.")]
    [Min(0)] public int attackCount;
    [Tooltip("한 번의 공격이 플레이어에게 주는 기본 피해량입니다. 0이면 피해를 주지 않습니다.")]
    [Min(0f)] public float damage;
    [Tooltip("공격 판정 박스의 가로(X), 세로(Y) 크기입니다. 어느 한쪽이 0이면 공격 판정을 실행하지 않습니다.")]
    public Vector2 hitboxSize;
    [Tooltip("보스 위치를 기준으로 한 공격 판정 중심의 X/Y 거리입니다. X는 보스가 보는 방향에 맞춰 반전됩니다.")]
    public Vector2 hitboxOffset;
    [Tooltip("적중한 플레이어에게 적용할 X/Y 넉백 힘입니다. (0, 0)이면 넉백하지 않습니다.")]
    public Vector2 knockbackForce;

    [Header("이동")]
    [Tooltip("전진 또는 돌진 속도입니다. 0이면 이동하지 않습니다.")]
    [Min(0f)] public float moveSpeed;
    [Tooltip("이동 또는 돌진이 진행할 최대 거리입니다. 0이면 실행 시간으로만 종료를 판단합니다.")]
    [Min(0f)] public float maxMoveDistance;
    [Tooltip("점프 시 적용할 X/Y 속도입니다. X는 보스가 보는 방향에 맞춰 반전됩니다. (0, 0)이면 점프하지 않습니다.")]
    public Vector2 jumpForce;
    [Tooltip("내려찍기 단계의 아래 방향 낙하 속도입니다. 0이면 강제 낙하하지 않습니다.")]
    [Min(0f)] public float slamSpeed;
    [Tooltip("활성화하면 이동 또는 돌진 중 전방 벽을 감지했을 때 해당 단계를 즉시 종료합니다.")]
    public bool stopAtWall;
    [Tooltip("활성화하면 돌진 공격이 플레이어에게 적중한 순간 해당 돌진 단계를 종료합니다.")]
    public bool stopOnPlayerHit;

    [Header("생성")]
    [Tooltip("투사체 또는 소환 단계에서 생성할 프리팹입니다. 비어 있으면 아무것도 생성하지 않습니다.")]
    public GameObject spawnPrefab;
    [Tooltip("생성할 프리팹 개수입니다. 0이면 생성하지 않습니다.")]
    [Min(0)] public int spawnCount;
    [Tooltip("보스 위치를 기준으로 한 첫 프리팹 생성 위치입니다. X는 보스가 보는 방향에 맞춰 반전됩니다.")]
    public Vector2 spawnOffset;
    [Tooltip("여러 프리팹을 생성할 때 각 프리팹 사이의 전방 거리입니다. 0이면 같은 위치에 생성합니다.")]
    [Min(0f)] public float spawnSpacing;
    [Tooltip("여러 프리팹을 생성할 때 각 프리팹 사이의 수직 거리입니다. 양수면 뒤에 생성되는 개체가 더 위에 배치됩니다.")]
    public float spawnVerticalSpacing;
    [Tooltip("여러 프리팹을 순차 생성할 때 생성 사이의 대기시간(초)입니다. 0이면 동시에 생성합니다.")]
    [Min(0f)] public float spawnInterval;
    [Tooltip("생성물의 크기 배율입니다. 0이면 기본 크기 1배로 처리합니다.")]
    [Min(0f)] public float spawnScaleMultiplier;
    [Tooltip("생성된 투사체의 이동속도입니다. 0이면 프리팹의 기본 속도를 사용합니다.")]
    [Min(0f)] public float projectileSpeed;
    [Tooltip("점프 또는 내려찍기 시, 시전 순간 플레이어까지의 가로 거리 중 이 단계에서 이동할 비율입니다. 0이면 기존 속도 방식을 사용합니다.")]
    [Range(0f, 1f)] public float targetDistanceRatio;
}

[Serializable]
public class BossActiveSkillData
{
    [Tooltip("액티브 스킬이 순서대로 실행할 행동 목록입니다. 목록 크기가 행동 단계 개수입니다.")]
    public List<BossAttackStepData> attackSteps = new List<BossAttackStepData>();
}

[Serializable]
public class BossBuffEffectData
{
    [Tooltip("버프로 변경할 보스 능력치를 선택합니다.")]
    public BossBuffStat stat;
    [Tooltip("고정값 증가는 입력값을 더하고, 배율 적용은 입력값을 곱하며, 비율 증가는 입력한 백분율만큼 증가시킵니다.")]
    public BossBuffValueMode valueMode = BossBuffValueMode.Multiply;
    [Tooltip("능력치에 적용할 값입니다. 배율 적용에서 1은 변화 없음, 비율 증가에서 25는 25% 증가를 뜻합니다.")]
    public float value;
}

[Serializable]
public class BossBuffSkillData
{
    [Tooltip("버프가 유지되는 시간(초)입니다. 0이면 버프를 적용하지 않습니다.")]
    [Min(0f)] public float duration;
    [Tooltip("동시에 적용할 버프 효과 목록입니다. 목록 크기가 상승시킬 능력치 개수입니다.")]
    public List<BossBuffEffectData> effects = new List<BossBuffEffectData>();
}

[Serializable]
public class BossSkillData
{
    [Tooltip("인스펙터와 디버그 로그에서 사용할 스킬 이름입니다.")]
    public string skillName;
    [Tooltip("액티브는 공격, 버프는 능력치 강화, 액티브+버프는 공격과 능력치 강화를 함께 실행합니다.")]
    public BossSkillType skillType;
    [Tooltip("액티브+버프 스킬일 때 공격과 버프의 실행 순서를 결정합니다.")]
    public BossSkillExecutionOrder executionOrder;

    [Tooltip("스킬을 다시 사용할 수 있을 때까지의 시간(초)입니다. 0이면 쿨타임이 없습니다.")]
    [Min(0f)] public float cooldown;
    [Tooltip("스킬을 사용할 수 있는 최소 페이즈입니다. 중간보스처럼 페이즈가 없으면 1을 사용합니다.")]
    [Min(1)] public int minimumPhase = 1;
    [Tooltip("스킬을 시작할 수 있는 플레이어와의 최대 거리입니다. 0이면 거리 제한이 없습니다.")]
    [Min(0f)] public float castRange;
    [Tooltip("플레이어가 이 거리 이상 떨어져 있으면 남은 쿨타임 조건에 따라 강제 시전을 허용합니다. 0이면 강제 시전을 사용하지 않습니다.")]
    [Min(0f)] public float forceCastMinDistance;
    [Tooltip("남은 쿨타임이 이 값 이하이고 강제 시전 거리 조건을 만족하면 쿨타임을 무시합니다. 0이면 강제 시전을 사용하지 않습니다.")]
    [Min(0f)] public float forceCastRemainingCooldown;
    [Tooltip("스킬 전체가 시작되기 전 대기시간(초)입니다. 0이면 즉시 시작합니다.")]
    [Min(0f)] public float preDelay;
    [Tooltip("스킬의 모든 동작이 끝난 뒤 대기시간(초)입니다. 0이면 즉시 다음 행동이 가능합니다.")]
    [Min(0f)] public float postDelay;

    [Tooltip("액티브 또는 액티브+버프 스킬의 공격 및 이동 단계 설정입니다.")]
    public BossActiveSkillData active = new BossActiveSkillData();
    [Tooltip("버프 또는 액티브+버프 스킬의 지속시간과 능력치 효과 설정입니다.")]
    public BossBuffSkillData buff = new BossBuffSkillData();

    public bool HasActive => skillType == BossSkillType.Active || skillType == BossSkillType.ActiveAndBuff;
    public bool HasBuff => skillType == BossSkillType.Buff || skillType == BossSkillType.ActiveAndBuff;
}
