#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class Stage3MainBossFactory
{
    private const string DataFolder = "Assets/Data/Enemys/Stage3";
    private const string DataPath = DataFolder + "/Stage3_MainBoss.asset";
    private const string PrefabPath = "Assets/Prefabs/Stage3_Enemy/Stage3_MainBoss.prefab";

    [InitializeOnLoadMethod]
    private static void CreateOnFirstImport()
    {
        EditorApplication.delayCall += CreateOrUpdate;
    }

    [MenuItem("Tools/Boss/Create Stage3 MainBoss")]
    public static void CreateOrUpdate()
    {
        EnsureFolders();

        MainBossData data = AssetDatabase.LoadAssetAtPath<MainBossData>(DataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MainBossData>();
            AssetDatabase.CreateAsset(data, DataPath);
            ConfigureData(data);
            EditorUtility.SetDirty(data);
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root != null)
        {
            MainBossController previousBoss = root.GetComponent<MainBossController>();
            LayerMask playerLayer = previousBoss != null ? previousBoss.playerLayer : (LayerMask)LayerMask.GetMask("Player");
            LayerMask wallLayer = previousBoss != null ? previousBoss.wallLayer : (LayerMask)LayerMask.GetMask("Ground");
            Transform wallCheck = previousBoss != null ? previousBoss.wallCheck : root.transform.Find("WallCheck");

            Stage3_MainBoss boss = root.GetComponent<Stage3_MainBoss>();
            if (boss == null)
            {
                if (previousBoss != null) Object.DestroyImmediate(previousBoss);
                boss = root.AddComponent<Stage3_MainBoss>();
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

    private static void ConfigureData(MainBossData data)
    {
        data.maxHp = 1450f;
        data.chaseSpeed = 2.8f;
        data.detectRange = 12f;
        data.chaseRange = 18f;
        data.positionUpdateDelay = 0.12f;
        data.wallCheckDistance = 0.6f;
        data.phase2HpPercent = 0.7f;
        data.phase3HpPercent = 0.3f;

        data.basicAttack = Active("기본 공격 2연격", 2.3f, 3.5f,
            Hit("1타 작은 범위", 9f, 2f, 1.7f, 1.2f, 0f, new Vector2(2f, 1f), 0.28f, 0.14f),
            Hit("2타 큰 범위", 13f, 3.8f, 2.2f, 1.8f, 0f, new Vector2(5f, 2f), 0.2f, 0.45f));

        BossSkillData skill1 = Active("1스킬 부식 2연격", 9f, 5f,
            Hit("1타 전방 큰 범위", 13f, 4.8f, 2.2f, 2.4f, 0f, new Vector2(5f, 2f), 0.45f, 0.2f),
            Dot("2타 지속 피해 범위", 4f, 3.2f, 1.8f, 1.7f, -0.1f, 5, 0.45f, 3f));
        skill1.postDelay = 0.55f;

        BossSkillData skill2 = Active("2스킬 4연격 돌진", 11f, 6f,
            Hit("1타", 5f, 2.5f, 1.6f, 1.45f, 0f, new Vector2(1.5f, 0.6f), 0.2f, 0.08f),
            Hit("2타", 5f, 2.6f, 1.65f, 1.5f, 0f, new Vector2(1.5f, 0.6f), 0.07f, 0.08f),
            Hit("3타", 6f, 2.8f, 1.7f, 1.6f, 0.05f, new Vector2(2f, 0.8f), 0.07f, 0.08f),
            Hit("4타", 7f, 3f, 1.8f, 1.7f, 0f, new Vector2(2.5f, 1f), 0.07f, 0.16f),
            Dash("마무리 돌진", 15f, 2.7f, 1.35f, 1.5f, -0.1f, 15f, 0.6f,
                new Vector2(11f, 3f), false, 0.1f, 0.55f));

        BossSkillData skill3 = PendingBuff("3스킬 버프 - 내용 미정");

        BossSkillData skill4 = Active("4스킬 돌진 후진 지속 공격", 13f, 8f,
            Dash("플레이어 충돌 또는 최대 거리까지 돌진", 14f, 2.6f, 1.3f, 1.45f, -0.1f, 16f, 0.75f,
                new Vector2(10f, 3f), true, 0.5f, 0.05f),
            BackJump("가볍게 후방 점프", 3.5f, 5f, 1.2f),
            Dot("전방 작은 지속 피해 범위", 4.5f, 2.5f, 1.5f, 1.4f, -0.1f, 5, 0.4f, 2.6f));
        skill4.postDelay = 0.65f;

        BossSkillData skill5 = PendingBuff("5스킬 버프 - 내용 미정");

        data.skills.Clear();
        data.skills.Add(skill1);
        data.skills.Add(skill2);
        data.skills.Add(skill3);
        data.skills.Add(skill4);
        data.skills.Add(skill5);
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

    private static BossSkillData PendingBuff(string name)
        => new BossSkillData
        {
            skillName = name,
            skillType = BossSkillType.Buff,
            cooldown = 0f,
            minimumPhase = 99
        };

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

    private static BossAttackStepData Dot(string name, float damage, float width, float height, float offsetX,
        float offsetY, int maxHits, float interval, float duration)
        => new BossAttackStepData
        {
            stepName = name,
            actionType = BossActionType.PersistentHitbox,
            attackCount = maxHits,
            damage = damage,
            hitboxSize = new Vector2(width, height),
            hitboxOffset = new Vector2(offsetX, offsetY),
            interval = interval,
            duration = duration
        };

    private static BossAttackStepData Dash(string name, float damage, float width, float height, float offsetX,
        float offsetY, float speed, float duration, Vector2 knockback, bool stopOnPlayerHit,
        float preDelay, float postDelay)
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
            maxMoveDistance = stopOnPlayerHit ? 12f : 0f,
            duration = duration,
            stopAtWall = true,
            stopOnPlayerHit = stopOnPlayerHit,
            preDelay = preDelay,
            postDelay = postDelay
        };

    private static BossAttackStepData BackJump(string name, float horizontalForce, float verticalForce, float duration)
        => new BossAttackStepData
        {
            stepName = name,
            actionType = BossActionType.Jump,
            jumpForce = new Vector2(-horizontalForce, verticalForce),
            duration = duration
        };

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder("Assets/Data/Enemys")) AssetDatabase.CreateFolder("Assets/Data", "Enemys");
        if (!AssetDatabase.IsValidFolder(DataFolder)) AssetDatabase.CreateFolder("Assets/Data/Enemys", "Stage3");
    }
}
#endif
