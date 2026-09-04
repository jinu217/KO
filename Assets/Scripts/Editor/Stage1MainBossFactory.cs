#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class Stage1MainBossFactory
{
    private const string DataPath = "Assets/Data/Enemys/Stage1/Stage1_MainBoss.asset";
    private const string PrefabPath = "Assets/Prefabs/Stage1_Enemy/Stage1_MainBoss.prefab";
    private const string WavePath = "Assets/Data/Enemys/Stage1/Ex_Stage1_MainBoss_SwordWave.prefab";

    [MenuItem("Tools/Boss/Create Stage1 MainBoss")]
    public static void CreateIfMissing()
    {
        MainBossData data = AssetDatabase.LoadAssetAtPath<MainBossData>(DataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MainBossData>();
            data.maxHp = 1000f; data.chaseSpeed = 2.8f; data.detectRange = 12f; data.chaseRange = 16f;
            data.positionUpdateDelay = 0.12f; data.wallCheckDistance = 0.5f;
            data.phase2HpPercent = 0.7f; data.phase3HpPercent = 0.3f;
            data.basicAttack = Active("기본공격 2연격", 2f, 3f,
                Hit("1타 세로", 7f, 1.5f, 2.4f, 1.2f), Hit("2타 가로", 10f, 2.6f, 1.5f, 1.4f));

            BossSkillData skill1 = Active("1스킬 3연격", 6f, 3.5f,
                Hit("1타 세로", 7f, 1.6f, 2.5f, 1.2f), Hit("2타 세로", 7f, 1.8f, 2.5f, 1.5f),
                Hit("3타 가로", 10f, 3f, 1.7f, 1.7f, new Vector2(10f, 2.5f)));

            GameObject wave = AssetDatabase.LoadAssetAtPath<GameObject>(WavePath);
            BossSkillData skill2 = Active("2스킬 검기", 10f, 9f, new BossAttackStepData
            {
                stepName = "검기 발사", actionType = BossActionType.SpawnPrefab, damage = 10f,
                spawnPrefab = wave, spawnCount = 1, spawnOffset = new Vector2(1.2f, 0.2f)
            });
            skill2.forceCastMinDistance = 4f; skill2.forceCastRemainingCooldown = 5f;

            BossSkillData skill3 = Buff("3스킬 공속 강화", 20f, 10f, BossBuffStat.AttackSpeed, 1.5f);
            BossSkillData skill4 = Active("4스킬 돌진", 10f, 0f, new BossAttackStepData
            {
                stepName = "장거리 돌진", actionType = BossActionType.DashAttack, damage = 15f, attackCount = 1,
                hitboxSize = new Vector2(2.4f, 1f), hitboxOffset = new Vector2(1.4f, -0.25f),
                moveSpeed = 15f, duration = 0.8f, preDelay = 0.75f, postDelay = 0.6f, stopAtWall = true
            });
            skill4.minimumPhase = 2;
            BossSkillData skill5 = Buff("5스킬 이속 강화", 20f, 10f, BossBuffStat.MoveSpeed, 1.5f);
            skill5.minimumPhase = 2;
            data.skills.AddRange(new[] { skill1, skill2, skill3, skill4, skill5 });
            AssetDatabase.CreateAsset(data, DataPath);
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root != null)
        {
            Stage1_MainBoss boss = root.GetComponent<Stage1_MainBoss>();
            if (boss == null) boss = root.AddComponent<Stage1_MainBoss>();
            boss.data = data;
            SerializedObject serializedBoss = new SerializedObject(boss);
            serializedBoss.FindProperty("swordWavePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(WavePath);
            serializedBoss.ApplyModifiedPropertiesWithoutUndo();
            if (root.GetComponent<Stage1_DesireSkill>() == null) root.AddComponent<Stage1_DesireSkill>();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
    }

    private static BossSkillData Active(string name, float cooldown, float range, params BossAttackStepData[] steps)
    {
        BossSkillData skill = new BossSkillData { skillName = name, skillType = BossSkillType.Active, cooldown = cooldown, castRange = range };
        skill.active.attackSteps.AddRange(steps); return skill;
    }
    private static BossSkillData Buff(string name, float cooldown, float duration, BossBuffStat stat, float value)
    {
        BossSkillData skill = new BossSkillData { skillName = name, skillType = BossSkillType.Buff, cooldown = cooldown };
        skill.buff.duration = duration; skill.buff.effects.Add(new BossBuffEffectData { stat = stat, valueMode = BossBuffValueMode.Multiply, value = value }); return skill;
    }
    private static BossAttackStepData Hit(string name, float damage, float width, float height, float offset, Vector2 knockback = default)
        => new BossAttackStepData { stepName = name, actionType = BossActionType.MeleeAttack, attackCount = 1, damage = damage,
            hitboxSize = new Vector2(width, height), hitboxOffset = new Vector2(offset, 0f), knockbackForce = knockback,
            preDelay = 0.18f, postDelay = 0.18f };
}
#endif
