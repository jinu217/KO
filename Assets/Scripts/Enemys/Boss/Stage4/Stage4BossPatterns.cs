using UnityEngine;

// 생성 시 사용하는 초기 수치. 생성 후에는 데이터 에셋에서 조절한다.
public static class Stage4BossPatterns
{
    public static void Configure(MainBossData data)
    {
        data.maxHp = 1800;
        data.chaseSpeed = 3.4f;
        data.detectRange = 12;
        data.chaseRange = 24;
        data.positionUpdateDelay = .1f;
        data.wallCheckDistance = .5f;
        data.phase2HpPercent = .7f;
        data.phase3HpPercent = .3f;
        data.basicAttack = Skill("기본 공격 3연타", 2, 2.2f,
            Punch("1타 잽", 11, .2f), Punch("2타 잽", 11, .1f), Punch("3타 전진 스트레이트", 16, .15f, true));
        data.skills.Clear();
        data.skills.Add(Skill("1스킬 러시 콤비네이션", 7, 2.8f,
            Punch("잽", 9, .25f, true), Punch("잽", 9, .07f, true),
            Punch("훅", 12, .08f, true), Punch("보디 블로", 12, .08f, true), Punch("스트레이트", 18, .12f, true)));
        var upper = Punch("보디 적중 후 어퍼컷", 22, .12f);
        upper.hitboxSize = new Vector2(1.8f, 2.8f);
        upper.hitboxOffset = new Vector2(1, .6f);
        upper.knockbackForce = new Vector2(1.5f, 11);
        data.skills.Add(Skill("2스킬 보디 블로 어퍼컷", 9, 2.7f,
            new BossAttackStepData { stepName = "한 발 전진", actionType = BossActionType.MoveForward, preDelay = .35f, moveSpeed = 5, maxMoveDistance = .6f, duration = .15f, stopAtWall = true },
            Punch("보디 블로", 18, .08f), upper));
        data.skills.Add(new BossSkillData { skillName = "3스킬 버프 (내용 미정)", skillType = BossSkillType.Buff, minimumPhase = 99, cooldown = 20 });
        var rush = Skill("4스킬 뎀프시 러시", 12, 10,
            new BossAttackStepData { stepName = "자세 낮추고 접근", actionType = BossActionType.MoveForward, preDelay = .8f, moveSpeed = 14, maxMoveDistance = 8, duration = .7f, stopAtWall = true },
            Punch("왼쪽 훅", 13, .12f, true), Punch("오른쪽 훅", 13, .1f, true),
            Punch("왼쪽 훅", 13, .1f, true), Punch("강한 오른쪽 훅", 22, .16f, true));
        rush.minimumPhase = 2;
        rush.active.attackSteps[4].knockbackForce = new Vector2(5, 2);
        data.skills.Add(rush);
        data.skills.Add(new BossSkillData { skillName = "5스킬 버프 (내용 미정)", skillType = BossSkillType.Buff, minimumPhase = 99, cooldown = 20 });
    }
    public static BossAttackStepData Punch(string name, float damage, float delay, bool advance = false)
        => new BossAttackStepData
        {
            stepName = name, actionType = advance ? BossActionType.DashAttack : BossActionType.MeleeAttack,
            attackCount = 1, damage = damage, preDelay = delay, postDelay = .07f,
            hitboxSize = new Vector2(1.9f, 1.6f), hitboxOffset = new Vector2(1.05f, 0),
            knockbackForce = new Vector2(1.5f, .4f), moveSpeed = advance ? 4 : 0,
            maxMoveDistance = advance ? .45f : 0, duration = advance ? .12f : 0, stopAtWall = true
        };
    private static BossSkillData Skill(string name, float cooldown, float range, params BossAttackStepData[] steps)
    {
        var skill = new BossSkillData { skillName = name, skillType = BossSkillType.Active, cooldown = cooldown, castRange = range, postDelay = .25f };
        skill.active.attackSteps.AddRange(steps);
        return skill;
    }
}
