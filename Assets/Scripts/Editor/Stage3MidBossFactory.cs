#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class Stage3MidBossFactory
{
    private const string DataPath = "Assets/Data/Enemys/Stage3/Stage3_MidBoss.asset";
    private const string PrefabPath = "Assets/Prefabs/Stage3_Enemy/Stage3_MidBoss.prefab";

    [InitializeOnLoadMethod]
    private static void CreateOnFirstImport()
    {
        EditorApplication.delayCall += CreateIfMissing;
    }

    [MenuItem("Tools/Boss/Create Stage3 MidBoss")]
    public static void CreateIfMissing()
    {
        MidBossData data = AssetDatabase.LoadAssetAtPath<MidBossData>(DataPath);
        if (data == null)
        {
            data = CreateData();
            AssetDatabase.CreateAsset(data, DataPath);
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root != null)
        {
            MidBossController previousBoss = root.GetComponent<MidBossController>();
            LayerMask playerLayer = previousBoss != null ? previousBoss.playerLayer : (LayerMask)LayerMask.GetMask("Player");
            LayerMask wallLayer = previousBoss != null ? previousBoss.wallLayer : (LayerMask)LayerMask.GetMask("Ground");
            Transform wallCheck = previousBoss != null ? previousBoss.wallCheck : root.transform.Find("WallCheck");

            Stage3_MidBoss boss = root.GetComponent<Stage3_MidBoss>();
            if (boss == null)
            {
                if (previousBoss != null) Object.DestroyImmediate(previousBoss);
                boss = root.AddComponent<Stage3_MidBoss>();
            }

            boss.data = data;
            boss.playerLayer = playerLayer;
            boss.wallLayer = wallLayer;
            boss.wallCheck = wallCheck;
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
    }

    private static MidBossData CreateData()
    {
        MidBossData data = ScriptableObject.CreateInstance<MidBossData>();
        data.maxHp = 500f;
        data.chaseSpeed = 2.9f;
        data.detectRange = 10f;
        data.chaseRange = 15f;
        data.positionUpdateDelay = 0.12f;
        data.wallCheckDistance = 0.55f;

        data.basicAttack = Active("기본 공격 전방 2연격", 2.2f, 3.2f,
            Hit("1타 전방 공격", 8f, 2.7f, 1.6f, 1.55f, 0f, new Vector2(2f, 1f), 0.25f, 0.06f),
            Hit("2타 전방 공격", 9f, 2.7f, 1.6f, 1.55f, 0f, new Vector2(2.5f, 1.2f), 0.04f, 0.3f));
        data.basicAttack.preDelay = 0.25f;
        data.basicAttack.postDelay = 0.4f;

        BossSkillData dash = Active("1스킬 지연 돌진", 10f, 6.5f,
            Dash("전방 돌진", 14f, 2.6f, 1.25f, 1.5f, -0.1f, 14f, 0.6f, new Vector2(10f, 3f)));
        dash.preDelay = 2f;
        dash.postDelay = 0.65f;

        BossSkillData leap = Active("2스킬 도약 내려찍기", 12f, 7.5f,
            Jump("대각선 도약", 10f, 0.7f),
            Slam("대각선 내려찍기", 16f, 3.2f, 2f, 0f, -0.35f, 17f, 0.3f, new Vector2(8f, 6f)));
        leap.preDelay = 2f;
        leap.postDelay = 0.8f;

        BossSkillData buff = new BossSkillData
        {
            skillName = "3스킬 전투 가속",
            skillType = BossSkillType.Buff,
            cooldown = 18f,
            preDelay = 0.8f,
            postDelay = 0.4f
        };
        buff.buff.duration = 8f;
        buff.buff.effects.Add(Effect(BossBuffStat.AttackSpeed, 1.15f));
        buff.buff.effects.Add(Effect(BossBuffStat.MoveSpeed, 1.12f));
        buff.buff.effects.Add(Effect(BossBuffStat.BasicAttackCooldown, 0.8f));

        data.skills.Add(dash);
        data.skills.Add(leap);
        data.skills.Add(buff);
        return data;
    }

    private static BossSkillData Active(string name, float cooldown, float range, params BossAttackStepData[] steps)
    {
        BossSkillData skill = new BossSkillData
        {
            skillName = name,
            skillType = BossSkillType.Active,
            cooldown = cooldown,
            castRange = range
        };
        skill.active.attackSteps.AddRange(steps);
        return skill;
    }

    private static BossAttackStepData Hit(string name, float damage, float width, float height, float offsetX,
        float offsetY, Vector2 knockback, float preDelay, float postDelay)
        => new BossAttackStepData
        {
            stepName = name,
            actionType = BossActionType.MeleeAttack,
            attackCount = 1,
            damage = damage,
            hitboxSize = new Vector2(width, height),
            hitboxOffset = new Vector2(offsetX, offsetY),
            knockbackForce = knockback,
            preDelay = preDelay,
            postDelay = postDelay
        };

    private static BossAttackStepData Dash(string name, float damage, float width, float height, float offsetX,
        float offsetY, float speed, float duration, Vector2 knockback)
        => new BossAttackStepData
        {
            stepName = name,
            actionType = BossActionType.DashAttack,
            attackCount = 1,
            damage = damage,
            hitboxSize = new Vector2(width, height),
            hitboxOffset = new Vector2(offsetX, offsetY),
            knockbackForce = knockback,
            moveSpeed = speed,
            duration = duration,
            stopAtWall = true
        };

    private static BossAttackStepData Jump(string name, float verticalForce, float targetDistanceRatio)
        => new BossAttackStepData
        {
            stepName = name,
            actionType = BossActionType.Jump,
            jumpForce = new Vector2(0f, verticalForce),
            targetDistanceRatio = targetDistanceRatio,
            duration = 2f
        };

    private static BossAttackStepData Slam(string name, float damage, float width, float height, float offsetX,
        float offsetY, float speed, float targetDistanceRatio, Vector2 knockback)
        => new BossAttackStepData
        {
            stepName = name,
            actionType = BossActionType.Slam,
            attackCount = 1,
            damage = damage,
            hitboxSize = new Vector2(width, height),
            hitboxOffset = new Vector2(offsetX, offsetY),
            knockbackForce = knockback,
            slamSpeed = speed,
            targetDistanceRatio = targetDistanceRatio,
            duration = 2f
        };

    private static BossBuffEffectData Effect(BossBuffStat stat, float value)
        => new BossBuffEffectData
        {
            stat = stat,
            valueMode = BossBuffValueMode.Multiply,
            value = value
        };
}
#endif
