#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class Stage4MidBossFactory
{
    private const string DataFolder = "Assets/Data/Enemys/Stage4";
    private const string DataPath = DataFolder + "/Stage4_MidBoss.asset";
    private const string PrefabPath = "Assets/Prefabs/Stage4_Enemy/Stage4_MidBoss.prefab";

    [InitializeOnLoadMethod]
    private static void CreateOnFirstImport() => EditorApplication.delayCall += CreateIfMissing;

    [MenuItem("Tools/Boss/Create Stage4 MidBoss")]
    public static void CreateIfMissing()
    {
        EnsureFolders();
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
            Stage4_MidBoss boss = root.GetComponent<Stage4_MidBoss>();
            if (boss == null)
            {
                if (previousBoss != null) Object.DestroyImmediate(previousBoss);
                boss = root.AddComponent<Stage4_MidBoss>();
            }
            boss.data = data;
            boss.playerLayer = playerLayer;
            boss.wallLayer = wallLayer;
            boss.wallCheck = wallCheck;
            EditorUtility.SetDirty(boss);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
    }

    private static MidBossData CreateData()
    {
        MidBossData data = ScriptableObject.CreateInstance<MidBossData>();
        data.maxHp = 650f;
        data.chaseSpeed = 2.8f;
        data.detectRange = 10f;
        data.chaseRange = 15f;
        data.positionUpdateDelay = 0.12f;
        data.wallCheckDistance = 0.55f;

        data.basicAttack = Active("기본 공격 내려찍기 밀치기 3연격", 2.5f, 3.4f,
            Hit("1타 제자리 내려찍기", 8f, 2.4f, 2.4f, 1.35f, -0.15f, new Vector2(1.2f, 0.5f), 0.28f, 0.12f),
            Hit("2타 제자리 내려찍기", 8f, 2.4f, 2.4f, 1.35f, -0.15f, new Vector2(1.2f, 0.5f), 0.14f, 0.18f),
            Dash("3타 기 모아 전진 밀치기", 12f, 2.9f, 1.55f, 1.55f, -0.05f, 5.5f, 1.4f, 0.32f, new Vector2(7f, 3f), 0.65f, 0.4f));

        BossAttackStepData spin = Hit("1타 낫 회전 공격", 14f, 6.2f, 2.2f, 0f, 0f, new Vector2(13f, 5f), 0.5f, 0.2f);
        spin.knockbackAwayFromCenter = true;
        BossSkillData skill1 = Active("1스킬 회전 도약 내려찍기", 11f, 5.5f,
            spin,
            Jump("플레이어 방향 도약", 10f, 0.7f),
            Slam("2타 도약 내려찍기", 17f, 3.6f, 1.9f, 0f, -0.35f, 22f, 0.3f, new Vector2(8f, 7f), 0.1f, 0.55f));
        skill1.postDelay = 0.55f;

        BossSkillData skill2 = Active("2스킬 끌어오기 낫 회전", 9.5f, 5.5f,
            Pull("전방 플레이어 끌어오기", 5.2f, 2.4f, 2.6f, 0f, 1f, 0f, 15f, 0.5f, 0.45f, 0.12f),
            Dot("끌어온 뒤 전방 낫 회전 도트", 4f, 3.4f, 2f, 1.7f, 0f, 5, 0.6f, 3f, 0.15f, 0.5f));

        BossSkillData skill3 = new BossSkillData { skillName = "3스킬 전투 가속 버프", skillType = BossSkillType.Buff, cooldown = 18f, preDelay = 0.8f, postDelay = 0.4f };
        skill3.buff.duration = 8f;
        skill3.buff.effects.Add(Effect(BossBuffStat.AttackSpeed, 1.15f));
        skill3.buff.effects.Add(Effect(BossBuffStat.MoveSpeed, 1.12f));
        skill3.buff.effects.Add(Effect(BossBuffStat.BasicAttackCooldown, 0.8f));

        data.skills.Add(skill1);
        data.skills.Add(skill2);
        data.skills.Add(skill3);
        return data;
    }

    private static BossSkillData Active(string name, float cooldown, float range, params BossAttackStepData[] steps)
    {
        BossSkillData skill = new BossSkillData { skillName = name, skillType = BossSkillType.Active, cooldown = cooldown, castRange = range };
        skill.active.attackSteps.AddRange(steps);
        return skill;
    }

    private static BossAttackStepData Hit(string name, float damage, float width, float height, float offsetX, float offsetY, Vector2 knockback, float preDelay, float postDelay)
        => new BossAttackStepData { stepName = name, actionType = BossActionType.MeleeAttack, attackCount = 1, damage = damage, hitboxSize = new Vector2(width, height), hitboxOffset = new Vector2(offsetX, offsetY), knockbackForce = knockback, preDelay = preDelay, postDelay = postDelay };

    private static BossAttackStepData Dash(string name, float damage, float width, float height, float offsetX, float offsetY, float speed, float maxDistance, float duration, Vector2 knockback, float preDelay, float postDelay)
        => new BossAttackStepData { stepName = name, actionType = BossActionType.DashAttack, attackCount = 1, damage = damage, hitboxSize = new Vector2(width, height), hitboxOffset = new Vector2(offsetX, offsetY), knockbackForce = knockback, moveSpeed = speed, maxMoveDistance = maxDistance, duration = duration, stopAtWall = true, preDelay = preDelay, postDelay = postDelay };

    private static BossAttackStepData Jump(string name, float verticalForce, float ratio)
        => new BossAttackStepData { stepName = name, actionType = BossActionType.Jump, jumpForce = new Vector2(0f, verticalForce), targetDistanceRatio = ratio, duration = 2f };

    private static BossAttackStepData Slam(string name, float damage, float width, float height, float offsetX, float offsetY, float speed, float ratio, Vector2 knockback, float preDelay, float postDelay)
        => new BossAttackStepData { stepName = name, actionType = BossActionType.Slam, attackCount = 1, damage = damage, hitboxSize = new Vector2(width, height), hitboxOffset = new Vector2(offsetX, offsetY), knockbackForce = knockback, slamSpeed = speed, targetDistanceRatio = ratio, duration = 2f, preDelay = preDelay, postDelay = postDelay };

    private static BossAttackStepData Pull(string name, float width, float height, float offsetX, float offsetY, float targetX, float targetY, float speed, float duration, float preDelay, float postDelay)
        => new BossAttackStepData { stepName = name, actionType = BossActionType.PullPlayer, hitboxSize = new Vector2(width, height), hitboxOffset = new Vector2(offsetX, offsetY), pullTargetOffset = new Vector2(targetX, targetY), moveSpeed = speed, duration = duration, preDelay = preDelay, postDelay = postDelay };

    private static BossAttackStepData Dot(string name, float damage, float width, float height,
        float offsetX, float offsetY, int maxHits, float interval, float duration,
        float preDelay, float postDelay)
        => new BossAttackStepData
        {
            stepName = name,
            actionType = BossActionType.PersistentHitbox,
            attackCount = maxHits,
            damage = damage,
            hitboxSize = new Vector2(width, height),
            hitboxOffset = new Vector2(offsetX, offsetY),
            interval = interval,
            duration = duration,
            preDelay = preDelay,
            postDelay = postDelay
        };

    private static BossBuffEffectData Effect(BossBuffStat stat, float value)
        => new BossBuffEffectData { stat = stat, valueMode = BossBuffValueMode.Multiply, value = value };

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder("Assets/Data/Enemys")) AssetDatabase.CreateFolder("Assets/Data", "Enemys");
        if (!AssetDatabase.IsValidFolder(DataFolder)) AssetDatabase.CreateFolder("Assets/Data/Enemys", "Stage4");
    }
}
#endif
