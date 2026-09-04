using System.Collections;
using UnityEngine;

public sealed class Stage1_MainBoss : MainBossController
{
    [Header("Stage1 전용")]
    [SerializeField] private Stage1_DesireSkill desireSkill;
    [SerializeField] private BossSkillData empoweredSkill1;
    [SerializeField] private GameObject swordWavePrefab;

    private int skill1FinalHitCount;
    private bool nextSkill1Empowered;
    private bool usingEmpoweredSkill1;

    protected override void Awake()
    {
        base.Awake();
        if (desireSkill == null) desireSkill = GetComponent<Stage1_DesireSkill>();
        if (desireSkill == null) desireSkill = gameObject.AddComponent<Stage1_DesireSkill>();
        BuildStage1SpecialAttacks();
    }

    protected override void Update()
    {
        base.Update();
        if (isDead || isAttacking || desireSkill == null) return;
        if (desireSkill.CanActivate(currentPhase)) StartCoroutine(UseDesire());
    }

    private IEnumerator UseDesire()
    {
        isAttacking = true;
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        yield return desireSkill.Activate(ResetAllSkillCooldowns);
        isAttacking = false;
    }

    protected override BossSkillData GetBasicAttackData()
    {
        if (desireSkill != null && desireSkill.MainBuffActive && desireSkill.EnhancedBasicAttack != null)
            return desireSkill.EnhancedBasicAttack;
        return base.GetBasicAttackData();
    }

    protected override BossSkillData GetSkillData(int index)
    {
        if (desireSkill != null && desireSkill.SkillEnhanceActive)
        {
            if (index == 0 && desireSkill.EnhancedSkill1 != null) return desireSkill.EnhancedSkill1;
            if (index == 1 && desireSkill.EnhancedSkill2 != null) return desireSkill.EnhancedSkill2;
        }
        if (index == 0 && nextSkill1Empowered && empoweredSkill1 != null)
        {
            usingEmpoweredSkill1 = true;
            return empoweredSkill1;
        }
        return base.GetSkillData(index);
    }

    public override void OnSkillAttackHit(BossAttackStepData step)
    {
        if (currentSkillIndex != 0 || usingEmpoweredSkill1 || (desireSkill != null && desireSkill.SkillEnhanceActive)) return;
        if (step.stepName != null && step.stepName.Contains("3타"))
        {
            skill1FinalHitCount++;
            if (skill1FinalHitCount >= 3)
            {
                skill1FinalHitCount = 0;
                nextSkill1Empowered = true;
            }
        }
    }

    protected override void OnSkillExecutionFinished(int index)
    {
        if (index == 0 && usingEmpoweredSkill1)
        {
            usingEmpoweredSkill1 = false;
            nextSkill1Empowered = false;
        }
    }

    protected override float GetExternalMoveMultiplier()
        => desireSkill != null ? desireSkill.MoveSpeedMultiplier : 1f;
    protected override float GetExternalAttackSpeedMultiplier()
        => desireSkill != null ? desireSkill.AttackSpeedMultiplier : 1f;
    protected override bool IsExternalBuffBlocking()
        => desireSkill != null && desireSkill.MainBuffActive;

    protected override void Die()
    {
        desireSkill?.Cancel();
        base.Die();
    }

    private void BuildStage1SpecialAttacks()
    {
        empoweredSkill1 = MakeSkill("강화 1스킬", 6f,
            Hit("1타 세로", 7f, 1.6f, 2.5f, 1.2f),
            Hit("2타 가로", 10f, 3f, 1.7f, 1.7f),
            Hit("3타 세로", 7f, 2f, 2.6f, 1.6f),
            Hit("4타 가로", 15f, 3.5f, 1.8f, 1.9f, new Vector2(15f, 3.5f)));

        BossSkillData desireBasic = MakeSkill("욕망 기본공격", 1f,
            Hit("1타 세로", 8f, 1.9f, 2.7f, 1.35f),
            Hit("2타 가로", 11f, 3f, 1.7f, 1.65f),
            Hit("3타 세로", 8f, 2.1f, 2.8f, 1.55f));

        BossSkillData desireSkill1 = MakeSkill("욕망 1스킬", 3f,
            Hit("1타 세로", 10f, 2f, 2.8f, 1.4f),
            Track(), Hit("2타 세로", 10f, 2.2f, 2.9f, 1.55f),
            Track(), Hit("3타 가로", 15f, 3.6f, 1.9f, 1.9f),
            Track(), Hit("4타 세로", 10f, 2.3f, 3f, 1.65f),
            Track(), Hit("5타 가로", 15f, 4f, 2f, 2.1f));

        BossSkillData desireSkill2 = ProjectileSkill("욕망 2스킬", 5f, 15f, 2);
        desireSkill2.active.attackSteps[0].spawnScaleMultiplier = 1.35f;
        desireSkill2.active.attackSteps[0].spawnVerticalSpacing = 0.35f;
        desireSkill2.forceCastMinDistance = 4f;
        desireSkill2.forceCastRemainingCooldown = 3f;
        desireSkill.Configure(desireBasic, desireSkill1, desireSkill2);
    }

    private BossSkillData MakeSkill(string name, float cooldown, params BossAttackStepData[] steps)
    {
        BossSkillData skill = new BossSkillData { skillName = name, skillType = BossSkillType.Active, cooldown = cooldown, castRange = 4f };
        skill.active.attackSteps.AddRange(steps);
        return skill;
    }

    private BossSkillData ProjectileSkill(string name, float cooldown, float damage, int count)
    {
        BossSkillData skill = MakeSkill(name, cooldown, new BossAttackStepData
        {
            stepName = "검기 발사", actionType = BossActionType.SpawnPrefab, damage = damage,
            spawnPrefab = swordWavePrefab, spawnCount = count, spawnOffset = new Vector2(1.2f, 0.2f),
            spawnInterval = 0.15f
        });
        skill.castRange = 9f;
        return skill;
    }

    private static BossAttackStepData Hit(string name, float damage, float width, float height, float offset, Vector2 knockback = default)
        => new BossAttackStepData
        {
            stepName = name, actionType = BossActionType.MeleeAttack, attackCount = 1, damage = damage,
            hitboxSize = new Vector2(width, height), hitboxOffset = new Vector2(offset, 0f),
            knockbackForce = knockback, preDelay = 0.16f, postDelay = 0.16f
        };

    private static BossAttackStepData Track()
        => new BossAttackStepData
        {
            stepName = "플레이어 추적", actionType = BossActionType.MoveForward,
            moveSpeed = 2.5f, duration = 0.1f, stopAtWall = true
        };
}
