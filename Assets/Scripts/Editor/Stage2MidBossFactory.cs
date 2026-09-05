#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class Stage2MidBossFactory
{
    private const string DataPath = "Assets/Data/Enemys/Stage2/Stage2_MidBoss.asset";
    private const string PrefabPath = "Assets/Prefabs/Stage2_Enemy/Stage2_MidBoss.prefab";

    [MenuItem("Tools/Boss/Create Stage2 MidBoss")]
    public static void CreateIfMissing()
    {
        MidBossData data = AssetDatabase.LoadAssetAtPath<MidBossData>(DataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MidBossData>();
            data.maxHp = 420f;
            data.chaseSpeed = 2.7f;
            data.detectRange = 9f;
            data.chaseRange = 13f;
            data.positionUpdateDelay = 0.12f;
            data.wallCheckDistance = 0.55f;

            data.basicAttack = Active("찌르기 올려치기", 2.4f, 3f,
                Hit("1타 찌르기", 7f, 2.3f, 1.15f, 1.35f, 0f, new Vector2(2f, 1f), 0.2f, 0.15f),
                Move("한 발 전진", 3.2f, 0.18f),
                Hit("2타 올려치기", 11f, 2f, 2.5f, 1.35f, 0.25f, new Vector2(7f, 9f), 0.24f, 0.35f));
            data.basicAttack.preDelay = 0.3f;
            data.basicAttack.postDelay = 0.45f;

            BossSkillData buff = new BossSkillData
            {
                skillName = "1스킬 가짜 영광",
                skillType = BossSkillType.Buff,
                cooldown = 18f,
                preDelay = 0.8f,
                postDelay = 0.4f
            };
            buff.buff.duration = 8f;
            buff.buff.effects.Add(Effect(BossBuffStat.AttackSpeed, 1.15f));
            buff.buff.effects.Add(Effect(BossBuffStat.MoveSpeed, 1.12f));
            buff.buff.effects.Add(Effect(BossBuffStat.BasicAttackCooldown, 0.8f));

            BossSkillData fiveHit = Active("2스킬 전방 5연격", 8.5f, 3.4f,
                Hit("1타", 3f, 2.4f, 1.4f, 1.45f, 0f, new Vector2(0.8f, 0.4f), 0.08f, 0.06f),
                Hit("2타", 3f, 2.5f, 1.45f, 1.5f, 0.05f, new Vector2(0.8f, 0.4f), 0.06f, 0.06f),
                Hit("3타", 3.5f, 2.6f, 1.5f, 1.55f, 0f, new Vector2(0.9f, 0.45f), 0.06f, 0.06f),
                Hit("4타", 3.5f, 2.7f, 1.5f, 1.6f, 0.05f, new Vector2(0.9f, 0.45f), 0.06f, 0.06f),
                Hit("5타", 5f, 2.9f, 1.6f, 1.7f, 0f, new Vector2(1f, 0.5f), 0.08f, 0.18f));
            fiveHit.preDelay = 0.35f;
            fiveHit.postDelay = 0.45f;

            BossSkillData slamCharge = Active("3스킬 내려찍기 돌진", 11f, 4f,
                Hit("1타 기 모아 내려찍기", 10f, 2.2f, 2.8f, 1.35f, 0f, new Vector2(3f, 2f), 0.85f, 0.25f),
                Dash("2타 무기 들고 돌진", 14f, 2.5f, 1.25f, 1.45f, -0.15f, 13f, 0.55f, new Vector2(11f, 3f)));
            slamCharge.preDelay = 0.45f;
            slamCharge.postDelay = 0.65f;

            data.skills.Add(buff);
            data.skills.Add(fiveHit);
            data.skills.Add(slamCharge);
            AssetDatabase.CreateAsset(data, DataPath);
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root != null)
        {
            Stage2_MidBoss boss = root.GetComponent<Stage2_MidBoss>();
            if (boss == null) boss = root.AddComponent<Stage2_MidBoss>();
            boss.data = data;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
    }

    private static BossSkillData Active(string name, float cooldown, float range, params BossAttackStepData[] steps)
    {
        BossSkillData skill = new BossSkillData { skillName = name, skillType = BossSkillType.Active, cooldown = cooldown, castRange = range };
        skill.active.attackSteps.AddRange(steps);
        return skill;
    }

    private static BossAttackStepData Hit(string name, float damage, float width, float height, float offsetX,
        float offsetY, Vector2 knockback, float preDelay, float postDelay)
        => new BossAttackStepData
        {
            stepName = name, actionType = BossActionType.MeleeAttack, attackCount = 1, damage = damage,
            hitboxSize = new Vector2(width, height), hitboxOffset = new Vector2(offsetX, offsetY),
            knockbackForce = knockback, preDelay = preDelay, postDelay = postDelay
        };

    private static BossAttackStepData Move(string name, float speed, float duration)
        => new BossAttackStepData { stepName = name, actionType = BossActionType.MoveForward, moveSpeed = speed, duration = duration, stopAtWall = true };

    private static BossAttackStepData Dash(string name, float damage, float width, float height, float offsetX,
        float offsetY, float speed, float duration, Vector2 knockback)
        => new BossAttackStepData
        {
            stepName = name, actionType = BossActionType.DashAttack, attackCount = 1, damage = damage,
            hitboxSize = new Vector2(width, height), hitboxOffset = new Vector2(offsetX, offsetY),
            moveSpeed = speed, duration = duration, knockbackForce = knockback, stopAtWall = true
        };

    private static BossBuffEffectData Effect(BossBuffStat stat, float value)
        => new BossBuffEffectData { stat = stat, valueMode = BossBuffValueMode.Multiply, value = value };
}
#endif
