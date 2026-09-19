using System.Collections;
using UnityEngine;

// 1스테이지 메인보스만 사용하는 전용기. 일반 BossSkillData 목록과 분리된다.
public sealed class Stage1_DesireSkill : MonoBehaviour
{
    [Header("발동")]
    [SerializeField, Tooltip("욕망 스킬을 다시 사용할 수 있을 때까지의 시간입니다. 발동 시작 순간부터 계산합니다.")]
    private float cooldown = 30f;
    [SerializeField, Tooltip("욕망 효과가 적용되기 전 제자리에서 대기하는 시간입니다.")]
    private float preDelay = 0.8f;
    [SerializeField, Tooltip("욕망 효과 적용 후 일반 전투로 돌아가기 전 대기하는 시간입니다.")]
    private float postDelay = 0.5f;

    [Header("15초 강화")]
    [SerializeField, Tooltip("공격속도·이동속도 및 강화 기본 공격이 유지되는 시간입니다.")]
    private float mainBuffDuration = 15f;
    [SerializeField, Tooltip("욕망 상태의 공격속도 배율입니다. 1.4는 40% 증가입니다.")]
    private float attackSpeedMultiplier = 1.4f;
    [SerializeField, Tooltip("욕망 상태의 이동속도 배율입니다. 1.25는 25% 증가입니다.")]
    private float moveSpeedMultiplier = 1.25f;
    [SerializeField, Tooltip("게임 시작 시 자동 생성되는 욕망 상태 기본 공격입니다. 직접 연결할 필요가 없습니다.")]
    private BossSkillData enhancedBasicAttack;

    [Header("10초 스킬 강화")]
    [SerializeField, Tooltip("욕망 상태의 1·2스킬 강화가 유지되는 시간입니다.")]
    private float skillEnhanceDuration = 10f;
    [SerializeField, Tooltip("게임 시작 시 자동 생성되는 욕망 강화 1스킬입니다. 직접 연결할 필요가 없습니다.")]
    private BossSkillData enhancedSkill1;
    [SerializeField, Tooltip("게임 시작 시 일반 2스킬의 검기 프리팹을 이용해 자동 생성됩니다.")]
    private BossSkillData enhancedSkill2;
    [SerializeField, Tooltip("일반 1스킬의 3타를 세 번 적중한 뒤 사용할 4연격입니다. 게임 시작 시 자동 생성됩니다.")]
    private BossSkillData empoweredSkill1;

    private float lastUseTime = -999f;
    public bool MainBuffActive { get; private set; }
    public bool SkillEnhanceActive { get; private set; }
    public float AttackSpeedMultiplier => MainBuffActive ? attackSpeedMultiplier : 1f;
    public float MoveSpeedMultiplier => MainBuffActive ? moveSpeedMultiplier : 1f;
    public BossSkillData EnhancedBasicAttack => enhancedBasicAttack;
    public BossSkillData EnhancedSkill1 => enhancedSkill1;
    public BossSkillData EnhancedSkill2 => enhancedSkill2;
    public BossSkillData EmpoweredSkill1 => empoweredSkill1;

    private static readonly Vector2 EnhancedVerticalKnockback = new Vector2(3f, 1.5f);
    private static readonly Vector2 EnhancedHorizontalKnockback = new Vector2(6f, 2.5f);

    public void Initialize(BossDataBase bossData)
    {
        GameObject wavePrefab = FindProjectilePrefab(bossData, 1);
        empoweredSkill1 = MakeSkill("강화 1스킬", 6f,
            Hit("1타 세로", 7f, 1.6f, 2.5f, 1.2f, EnhancedVerticalKnockback),
            Hit("2타 가로", 10f, 3f, 1.7f, 1.7f, EnhancedHorizontalKnockback),
            Hit("3타 세로", 7f, 2f, 2.6f, 1.6f, EnhancedVerticalKnockback),
            Hit("4타 가로", 15f, 3.5f, 1.8f, 1.9f, new Vector2(16f, 4.5f)));

        enhancedBasicAttack = MakeSkill("욕망 기본공격", 1f,
            Hit("1타 세로", 8f, 1.9f, 2.7f, 1.35f, new Vector2(2.5f, 1f)),
            Hit("2타 가로", 11f, 3f, 1.7f, 1.65f, new Vector2(2.5f, 1f)),
            Hit("3타 세로", 8f, 2.1f, 2.8f, 1.55f, new Vector2(2.5f, 1f)));

        enhancedSkill1 = MakeSkill("욕망 1스킬", 3f,
            Hit("1타 세로", 10f, 2f, 2.8f, 1.4f, EnhancedVerticalKnockback),
            Track(), Hit("2타 세로", 10f, 2.2f, 2.9f, 1.55f, EnhancedVerticalKnockback),
            Track(), Hit("3타 가로", 15f, 3.6f, 1.9f, 1.9f, EnhancedHorizontalKnockback),
            Track(), Hit("4타 세로", 10f, 2.3f, 3f, 1.65f, EnhancedVerticalKnockback),
            Track(), Hit("5타 가로", 15f, 4f, 2f, 2.1f, EnhancedHorizontalKnockback));

        enhancedSkill2 = MakeSkill("욕망 2스킬", 5f, new BossAttackStepData
        {
            stepName = "검기 발사",
            actionType = BossActionType.SpawnPrefab,
            damage = 15f,
            preDelay = 1f, // 12 FPS / 12프레임: 첫 검기 발사
            spawnPrefab = wavePrefab,
            spawnCount = 2,
            spawnOffset = new Vector2(1.2f, 0.2f),
            spawnInterval = 0.15f,
            spawnVerticalSpacing = 0.35f,
            spawnScaleMultiplier = 1.35f,
            projectileSpeed = 11f
        });
        enhancedSkill2.castRange = 9f;
    }

    public void Configure(BossSkillData basic, BossSkillData skill1, BossSkillData skill2)
    {
        enhancedBasicAttack = basic;
        enhancedSkill1 = skill1;
        enhancedSkill2 = skill2;
    }

    public bool CanActivate(int phase) => phase >= 3 && !MainBuffActive && Time.time >= lastUseTime + cooldown;

    public IEnumerator Activate(System.Action resetCooldowns)
    {
        lastUseTime = Time.time;
        MainBossController animationOwner = GetComponent<MainBossController>();
        if (animationOwner != null) animationOwner.PlayDesireAnimation();
        if (preDelay > 0f) yield return new WaitForSeconds(preDelay);
        resetCooldowns?.Invoke();
        StartCoroutine(MainBuffRoutine());
        StartCoroutine(SkillEnhanceRoutine());
        if (postDelay > 0f) yield return new WaitForSeconds(postDelay);
        if (animationOwner != null) yield return animationOwner.WaitForDesireAnimation();
    }

    private IEnumerator MainBuffRoutine()
    {
        MainBuffActive = true;
        yield return new WaitForSeconds(mainBuffDuration);
        MainBuffActive = false;
    }

    private IEnumerator SkillEnhanceRoutine()
    {
        SkillEnhanceActive = true;
        yield return new WaitForSeconds(skillEnhanceDuration);
        SkillEnhanceActive = false;
    }

    public void Cancel()
    {
        StopAllCoroutines();
        MainBuffActive = false;
        SkillEnhanceActive = false;
    }

    private static GameObject FindProjectilePrefab(BossDataBase bossData, int skillIndex)
    {
        if (bossData?.skills == null || skillIndex < 0 || skillIndex >= bossData.skills.Count) return null;
        BossSkillData skill = bossData.skills[skillIndex];
        if (skill?.active?.attackSteps == null) return null;
        foreach (BossAttackStepData step in skill.active.attackSteps)
            if (step != null && step.actionType == BossActionType.SpawnPrefab && step.spawnPrefab != null)
                return step.spawnPrefab;
        return null;
    }

    private static BossSkillData MakeSkill(string name, float skillCooldown, params BossAttackStepData[] steps)
    {
        BossSkillData skill = new BossSkillData
        {
            skillName = name,
            skillType = BossSkillType.Active,
            cooldown = skillCooldown,
            castRange = 4f
        };
        skill.active.attackSteps.AddRange(steps);
        return skill;
    }

    private static BossAttackStepData Hit(string name, float damage, float width, float height, float offset, Vector2 knockback)
        => new BossAttackStepData
        {
            stepName = name,
            actionType = BossActionType.MeleeAttack,
            attackCount = 1,
            damage = damage,
            hitboxSize = new Vector2(width, height),
            hitboxOffset = new Vector2(offset, 0f),
            knockbackForce = knockback,
            preDelay = 0.16f,
            postDelay = 0.16f
        };

    private static BossAttackStepData Track()
        => new BossAttackStepData
        {
            stepName = "플레이어 추적",
            actionType = BossActionType.MoveForward,
            moveSpeed = 2.5f,
            duration = 0.1f,
            stopAtWall = true
        };
}
