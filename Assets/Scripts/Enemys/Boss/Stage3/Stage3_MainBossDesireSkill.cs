using System.Collections;
using UnityEngine;

// 3스테이지 메인보스의 자아실현 욕망, 성취 단계와 마무리 공격을 관리한다.
[DisallowMultipleComponent]
public sealed class Stage3_MainBossDesireSkill : MonoBehaviour
{
    [Header("발동")]
    [SerializeField, InspectorName("지속시간"), Min(0f), Tooltip("성취를 쌓을 수 있고 기본 강화 효과가 유지되는 시간입니다.")]
    private float duration = 20f;
    [SerializeField, InspectorName("쿨타임"), Min(0f), Tooltip("욕망 스킬 재사용 대기시간입니다. 발동 시작 순간부터 계산합니다.")]
    private float cooldown = 50f;
    [SerializeField, InspectorName("발동 선딜레이"), Min(0f), Tooltip("욕망 스킬을 시작할 때 제자리에서 대기하는 시간입니다.")]
    private float castDelay = 0.8f;

    [Header("기본 강화")]
    [SerializeField, InspectorName("공격력 배율"), Min(0f), Tooltip("욕망 지속 중 공격력 배율입니다. 1.1은 10% 증가입니다.")]
    private float damageMultiplier = 1.1f;
    [SerializeField, InspectorName("공격속도 배율"), Min(0f), Tooltip("욕망 지속 중 공격속도 배율입니다. 1.15는 15% 증가입니다.")]
    private float attackSpeedMultiplier = 1.15f;
    [SerializeField, InspectorName("이동속도 배율"), Min(0f), Tooltip("욕망 지속 중 이동속도 배율입니다. 1.15는 15% 증가입니다.")]
    private float moveSpeedMultiplier = 1.15f;
    [SerializeField, InspectorName("성취 2단계 스킬 쿨타임 배율"), Min(0f), Tooltip("성취 2개부터 적용할 일반 스킬 쿨타임 배율입니다. 0.9는 10% 감소입니다.")]
    private float achievedCooldownMultiplier = 0.9f;

    [Header("강화 공격")]
    [SerializeField, InspectorName("성취 1단계 기본 공격"), Tooltip("게임 시작 시 기존 기본 공격을 바탕으로 자동 생성됩니다.")]
    private BossSkillData enhancedBasicAttack;
    [SerializeField, InspectorName("성취 2단계 4스킬"), Tooltip("게임 시작 시 기존 4스킬 뒤에 0.2초 대기와 재돌진을 추가해 자동 생성됩니다.")]
    private BossSkillData enhancedSkill4;

    [Header("자아의 완성")]
    [SerializeField, InspectorName("힘 모으기 시간"), Min(0f), Tooltip("마무리 공격에서 높게 도약하기 전 힘을 모으는 시간입니다.")]
    private float finisherChargeDelay = 1.2f;
    [SerializeField, InspectorName("도약 높이"), Min(0f), Tooltip("자아의 완성 도약에 사용하는 위쪽 속도입니다.")]
    private float finisherJumpForce = 13f;
    [SerializeField, InspectorName("내려찍기 피해"), Min(0f), Tooltip("자아의 완성 내려찍기가 주는 피해입니다.")]
    private float finisherSlamDamage = 25f;
    [SerializeField, InspectorName("돌진 예고시간"), Min(0f), Tooltip("내려찍기 적중 후 플레이어에게 돌진하기 전 대기시간입니다.")]
    private float followupTelegraphDelay = 0.5f;
    [SerializeField, InspectorName("마무리 넉백 공격 피해"), Min(0f), Tooltip("후속 돌진 뒤 전방 넉백 공격이 주는 피해입니다.")]
    private float followupDamage = 20f;
    [SerializeField, InspectorName("회피 시 그로기 시간"), Min(0f), Tooltip("플레이어가 내려찍기를 피했을 때 보스가 행동하지 못하는 시간입니다.")]
    private float missedFinisherGroggy = 5f;
    [SerializeField, InspectorName("시간초과 그로기 시간"), Min(0f), Tooltip("20초 안에 성취 3개를 얻지 못했을 때 보스가 행동하지 못하는 시간입니다.")]
    private float timeoutGroggy = 3f;

    private readonly bool[] achievedPatterns = new bool[4];
    private Stage3_MainBoss owner;
    private BossSkillData finisherOpening;
    private BossSkillData finisherFollowup;
    private BossAttackStepData finisherSlamStep;
    private float lastUseTime = -999f;
    private float endTime;
    private int achievementCount;
    private bool active;
    private bool finisherQueued;
    private bool finisherRunning;
    private bool failureQueued;
    private bool groggy;
    private bool finisherSlamHit;

    public bool HasFirstAchievement => active && achievementCount >= 1;
    public bool HasSecondAchievement => active && achievementCount >= 2;
    public BossSkillData EnhancedBasicAttack => enhancedBasicAttack;
    public BossSkillData EnhancedSkill4 => enhancedSkill4;
    public float DamageMultiplier => active ? damageMultiplier : 1f;
    public float AttackSpeedMultiplier => active ? attackSpeedMultiplier : 1f;
    public float MoveSpeedMultiplier => active ? moveSpeedMultiplier : 1f;
    public float SkillCooldownMultiplier => HasSecondAchievement ? achievedCooldownMultiplier : 1f;

    public void Initialize(Stage3_MainBoss boss, BossDataBase bossData)
    {
        owner = boss;
        BuildEnhancedAttacks(bossData);
        BuildFinisher();
    }

    public bool Tick(int phase)
    {
        if (owner == null || owner.DesireIsDead) return false;

        if (!active)
        {
            if (!groggy && phase >= 3 && Time.time >= lastUseTime + cooldown && !owner.DesireIsAttacking)
            {
                StartCoroutine(Activate());
                return true;
            }
            return groggy;
        }

        if (finisherQueued)
        {
            if (!finisherRunning && !owner.DesireIsAttacking)
                StartCoroutine(UseFinisher());
            return true;
        }

        if (Time.time >= endTime)
        {
            failureQueued = true;
            if (!owner.DesireIsAttacking)
                StartCoroutine(FailByTimeout());
            return true;
        }

        return finisherRunning || failureQueued || groggy;
    }

    public void OnAttackHit(int skillIndex, BossAttackStepData step)
    {
        if (finisherRunning)
        {
            if (ReferenceEquals(step, finisherSlamStep)) finisherSlamHit = true;
            return;
        }

        if (!active || finisherQueued || failureQueued || Time.time >= endTime) return;
        int pattern = PatternIndex(skillIndex);
        if (pattern < 0 || achievedPatterns[pattern]) return;

        achievedPatterns[pattern] = true;
        achievementCount++;
        owner.DesireLog($"성취 {achievementCount}단계 획득", CurrentPatternSkill(skillIndex));
        if (achievementCount >= 3)
            finisherQueued = true;
    }

    public void Cancel()
    {
        StopAllCoroutines();
        EndDesire();
        groggy = false;
    }

    private IEnumerator Activate()
    {
        owner.DesireIsAttacking = true;
        owner.DesireStop();
        lastUseTime = Time.time;
        endTime = Time.time + duration;
        active = true;
        ResetAchievements();
        owner.DesireResetCooldowns();
        owner.DesireLog("욕망 스킬 시작", DesireLogSkill());
        owner.PlayDesireAnimation();
        if (castDelay > 0f) yield return new WaitForSeconds(castDelay);
        yield return owner.WaitForDesireAnimation();
        owner.DesireIsAttacking = false;
    }

    private IEnumerator UseFinisher()
    {
        finisherRunning = true;
        finisherSlamHit = false;
        owner.DesireIsAttacking = true;
        owner.DesireStop();
        owner.DesireLog("자아의 완성 시작", finisherOpening);
        yield return owner.DesireExecute(finisherOpening);

        if (!finisherSlamHit)
        {
            EndDesire();
            yield return Groggy(missedFinisherGroggy);
            finisherRunning = false;
            yield break;
        }

        yield return owner.DesireExecute(finisherFollowup);
        EndDesire();
        finisherRunning = false;
        owner.DesireIsAttacking = false;
    }

    private IEnumerator FailByTimeout()
    {
        failureQueued = false;
        owner.DesireIsAttacking = true;
        owner.DesireStop();
        owner.DesireLog("욕망 스킬 실패", DesireLogSkill());
        EndDesire();
        yield return Groggy(timeoutGroggy);
    }

    private IEnumerator Groggy(float seconds)
    {
        groggy = true;
        owner.DesireStop();
        if (seconds > 0f) yield return new WaitForSeconds(seconds);
        groggy = false;
        if (owner != null) owner.DesireIsAttacking = false;
    }

    private void EndDesire()
    {
        active = false;
        finisherQueued = false;
        failureQueued = false;
        achievementCount = 0;
        for (int i = 0; i < achievedPatterns.Length; i++) achievedPatterns[i] = false;
    }

    private void ResetAchievements()
    {
        achievementCount = 0;
        finisherQueued = false;
        finisherRunning = false;
        failureQueued = false;
        for (int i = 0; i < achievedPatterns.Length; i++) achievedPatterns[i] = false;
    }

    private void BuildEnhancedAttacks(BossDataBase bossData)
    {
        BossSkillData basic = bossData != null ? bossData.basicAttack : null;
        enhancedBasicAttack = CloneSkill(basic, "성취 1단계 기본 공격");
        if (enhancedBasicAttack != null)
        {
            enhancedBasicAttack.active.attackSteps.Add(new BossAttackStepData
            {
                stepName = "3타 짧은 전진 공격",
                actionType = BossActionType.DashAttack,
                attackCount = 1,
                damage = 10f,
                hitboxSize = new Vector2(2.4f, 1.6f),
                hitboxOffset = new Vector2(1.35f, 0f),
                knockbackForce = new Vector2(3f, 1.5f),
                moveSpeed = 5f,
                maxMoveDistance = 1.2f,
                duration = 0.3f,
                stopAtWall = true,
                preDelay = 0.12f,
                postDelay = 0.3f
            });
        }

        BossSkillData skill4 = bossData?.skills != null && bossData.skills.Count > 3 ? bossData.skills[3] : null;
        enhancedSkill4 = CloneSkill(skill4, "성취 2단계 강화 4스킬");
        if (enhancedSkill4 != null)
        {
            enhancedSkill4.active.attackSteps.Add(new BossAttackStepData
            {
                stepName = "도트 종료 후 대기",
                actionType = BossActionType.Wait,
                duration = 0.2f
            });
            enhancedSkill4.active.attackSteps.Add(new BossAttackStepData
            {
                stepName = "3타 전방 재돌진",
                actionType = BossActionType.DashAttack,
                attackCount = 1,
                damage = 15f,
                hitboxSize = new Vector2(2.7f, 1.35f),
                hitboxOffset = new Vector2(1.5f, -0.1f),
                knockbackForce = new Vector2(11f, 3f),
                moveSpeed = 16f,
                maxMoveDistance = 8f,
                duration = 0.6f,
                stopAtWall = true,
                stopOnPlayerHit = true,
                postDelay = 0.5f
            });
        }
    }

    private void BuildFinisher()
    {
        finisherSlamStep = new BossAttackStepData
        {
            stepName = "자아의 완성 내려찍기",
            actionType = BossActionType.Slam,
            attackCount = 1,
            damage = finisherSlamDamage,
            hitboxSize = new Vector2(4f, 2.2f),
            hitboxOffset = new Vector2(0f, -0.35f),
            knockbackForce = new Vector2(12f, 10f),
            slamSpeed = 25f,
            targetDistanceRatio = 0.3f,
            duration = 2f,
            postDelay = 0.15f
        };
        finisherOpening = MakeSkill("자아의 완성", 0f,
            new BossAttackStepData { stepName = "힘 모으기", actionType = BossActionType.Wait, duration = finisherChargeDelay },
            new BossAttackStepData
            {
                stepName = "높게 도약",
                actionType = BossActionType.Jump,
                jumpForce = new Vector2(0f, finisherJumpForce),
                targetDistanceRatio = 0.7f,
                duration = 2f
            },
            finisherSlamStep);

        finisherFollowup = MakeSkill("자아의 완성 후속 공격", 0f,
            new BossAttackStepData { stepName = "돌진 예고", actionType = BossActionType.Wait, duration = followupTelegraphDelay },
            new BossAttackStepData
            {
                stepName = "플레이어에게 돌진",
                actionType = BossActionType.MoveForward,
                moveSpeed = 17f,
                maxMoveDistance = 8f,
                duration = 0.55f,
                stopAtWall = true
            },
            new BossAttackStepData
            {
                stepName = "전방 강한 넉백 공격",
                actionType = BossActionType.MeleeAttack,
                attackCount = 1,
                damage = followupDamage,
                hitboxSize = new Vector2(3.4f, 1.9f),
                hitboxOffset = new Vector2(1.8f, 0f),
                knockbackForce = new Vector2(17f, 7f),
                preDelay = 0.1f,
                postDelay = 0.65f
            });
    }

    private static BossSkillData CloneSkill(BossSkillData source, string name)
    {
        if (source == null) return null;
        BossSkillData clone = new BossSkillData
        {
            skillName = name,
            skillType = source.skillType,
            executionOrder = source.executionOrder,
            cooldown = source.cooldown,
            minimumPhase = source.minimumPhase,
            castRange = source.castRange,
            preDelay = source.preDelay,
            postDelay = source.postDelay
        };
        if (source.active?.attackSteps != null)
            clone.active.attackSteps.AddRange(source.active.attackSteps);
        return clone;
    }

    private static BossSkillData MakeSkill(string name, float cooldown, params BossAttackStepData[] steps)
    {
        BossSkillData skill = new BossSkillData
        {
            skillName = name,
            skillType = BossSkillType.Active,
            cooldown = cooldown
        };
        skill.active.attackSteps.AddRange(steps);
        return skill;
    }

    private BossSkillData CurrentPatternSkill(int skillIndex)
    {
        if (skillIndex < 0) return enhancedBasicAttack ?? MakeSkill("기본 공격", 0f);
        if (owner?.data?.skills != null && skillIndex < owner.data.skills.Count)
            return owner.data.skills[skillIndex];
        return DesireLogSkill();
    }

    private BossSkillData DesireLogSkill()
        => new BossSkillData { skillName = "완성에 대한 집착", cooldown = cooldown };

    private static int PatternIndex(int skillIndex)
    {
        if (skillIndex < 0) return 0;
        if (skillIndex == 0) return 1;
        if (skillIndex == 1) return 2;
        if (skillIndex == 3) return 3;
        return -1;
    }
}
