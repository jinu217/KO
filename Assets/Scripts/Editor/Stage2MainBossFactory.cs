#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class Stage2MainBossFactory
{
    private const string DataFolder = "Assets/Data/Enemys/Stage2";
    private const string DataPath = DataFolder + "/Stage2_MainBoss.asset";
    private const string PrefabPath = "Assets/Prefabs/Stage2_Enemy/Stage2_MainBoss.prefab";
    private const string RockPath = DataFolder + "/Ex_Stage2_MainBoss_Rock.prefab";

    [MenuItem("Tools/Boss/Create Stage2 MainBoss")]
    public static void CreateOrUpdate()
    {
        EnsureFolders();
        GameObject rockPrefab = CreateRockPrefab();

        MainBossData data = AssetDatabase.LoadAssetAtPath<MainBossData>(DataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MainBossData>();
            AssetDatabase.CreateAsset(data, DataPath);
        }
        ConfigureData(data, rockPrefab);
        EditorUtility.SetDirty(data);

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root != null)
        {
            Stage2_MainBoss boss = root.GetComponent<Stage2_MainBoss>();
            if (boss == null)
                boss = root.AddComponent<Stage2_MainBoss>();
            if (root.GetComponent<Stage2_MainBossDesireSkill>() == null)
                root.AddComponent<Stage2_MainBossDesireSkill>();
            boss.data = data;
            EditorUtility.SetDirty(boss);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = data;
        Debug.Log("Stage2 MainBoss 스크립트, 데이터, 돌 파동 프리팹 연결 완료");
    }

    private static void ConfigureData(MainBossData data, GameObject rockPrefab)
    {
        data.maxHp = 1200f;
        data.chaseSpeed = 2.5f;
        data.detectRange = 12f;
        data.chaseRange = 17f;
        data.positionUpdateDelay = 0.12f;
        data.wallCheckDistance = 0.6f;
        data.phase2HpPercent = 0.7f;
        data.phase3HpPercent = 0.3f;

        data.basicAttack = Active("기본 공격 올려치기", 3f, 3.1f,
            Hit("아래에서 위로 올려치기", 15f, 2.2f, 2.8f, 1.35f, 0.3f,
                new Vector2(2.5f, 12f), 0.45f, 0.6f));

        BossSkillData skill1 = Active("1스킬 점프 연속 내려찍기", 7f, 4f,
            Hit("1타 머리 위로 들어 내려찍기", 12f, 2.3f, 2.7f, 1.35f, -0.1f,
                new Vector2(4f, 3f), 0.65f, 0.2f),
            Jump("플레이어 거리 70% 점프", 10f, 1.4f, 0.7f),
            Slam("2타 착지 내려찍기", 18f, 3.6f, 1.7f, 1.4f, -0.35f,
                new Vector2(8f, 7f), 22f, 1.2f, 0.08f, 0.55f, 0.3f));

        BossSkillData skill2 = Active("2스킬 돌진 올려치기 내려찍기", 10f, 7f,
            Dash("1타 몸통 돌진", 8f, 2.2f, 1.4f, 1.25f, -0.1f, 12f, 0.42f,
                new Vector2(4f, 2f), 0.35f, 0.15f),
            Hit("2타 올려치기", 10f, 2.2f, 2.8f, 1.35f, 0.3f,
                new Vector2(3f, 8f), 0.2f, 0.2f),
            Hit("3타 내려찍기", 13f, 3.1f, 1.65f, 1.45f, -0.35f,
                new Vector2(7f, 4f), 0.28f, 0.55f));

        BossSkillData skill3 = PendingBuff("3스킬 버프 - 내용 미정");

        BossSkillData skill4 = Active("4스킬 점프 내려찍기 돌 파동", 15f, 8.5f,
            Jump("플레이어 거리 70% 점프", 10f, 1.5f, 0.7f),
            Slam("착지 내려찍기", 30f, 3.8f, 1.8f, 1.45f, -0.35f,
                new Vector2(10f, 8f), 24f, 1.25f, 0.1f, 0.2f, 0.3f),
            new BossAttackStepData
            {
                stepName = "전방 돌 5개 순차 생성",
                actionType = BossActionType.SpawnPrefab,
                damage = 15f,
                spawnPrefab = rockPrefab,
                spawnCount = 5,
                spawnOffset = new Vector2(1.8f, -0.55f),
                spawnSpacing = 1.35f,
                spawnInterval = 0.16f,
                spawnScaleMultiplier = 1f,
                postDelay = 0.65f
            });

        data.skills.Clear();
        data.skills.Add(skill1);
        data.skills.Add(skill2);
        BossSkillData skill5 = PendingBuff("5스킬 버프 - 내용 미정");

        data.skills.Add(skill3);
        data.skills.Add(skill4);
        data.skills.Add(skill5);
    }

    private static GameObject CreateRockPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(RockPath);
        if (existing != null)
            return existing;

        GameObject rock = new GameObject("Ex_Stage2_MainBoss_Rock");
        rock.AddComponent<SpriteRenderer>();
        BoxCollider2D collider = rock.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1.1f, 1.8f);
        Rigidbody2D body = rock.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.gravityScale = 0f;
        rock.AddComponent<Stage2_BossRock>();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rock, RockPath);
        Object.DestroyImmediate(rock);
        return prefab;
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
    {
        // 세부 내용이 정해질 때까지 전투에서 선택되지 않도록 잠근다.
        return new BossSkillData
        {
            skillName = name,
            skillType = BossSkillType.Buff,
            cooldown = 0f,
            minimumPhase = 99
        };
    }

    private static BossAttackStepData Hit(string name, float damage, float width, float height,
        float offsetX, float offsetY, Vector2 knockback, float preDelay, float postDelay)
        => new BossAttackStepData
        {
            stepName = name, actionType = BossActionType.MeleeAttack, attackCount = 1, damage = damage,
            hitboxSize = new Vector2(width, height), hitboxOffset = new Vector2(offsetX, offsetY),
            knockbackForce = knockback, preDelay = preDelay, postDelay = postDelay
        };

    private static BossAttackStepData Dash(string name, float damage, float width, float height,
        float offsetX, float offsetY, float speed, float duration, Vector2 knockback, float preDelay, float postDelay)
        => new BossAttackStepData
        {
            stepName = name, actionType = BossActionType.DashAttack, attackCount = 1, damage = damage,
            hitboxSize = new Vector2(width, height), hitboxOffset = new Vector2(offsetX, offsetY),
            moveSpeed = speed, duration = duration, knockbackForce = knockback, stopAtWall = true,
            preDelay = preDelay, postDelay = postDelay
        };

    private static BossAttackStepData Jump(string name, float verticalForce, float duration, float targetDistanceRatio)
        => new BossAttackStepData
        {
            stepName = name, actionType = BossActionType.Jump,
            jumpForce = new Vector2(0f, verticalForce), duration = duration,
            targetDistanceRatio = targetDistanceRatio
        };

    private static BossAttackStepData Slam(string name, float damage, float width, float height,
        float offsetX, float offsetY, Vector2 knockback, float speed, float duration, float preDelay, float postDelay,
        float targetDistanceRatio = 0f)
        => new BossAttackStepData
        {
            stepName = name, actionType = BossActionType.Slam, attackCount = 1, damage = damage,
            hitboxSize = new Vector2(width, height), hitboxOffset = new Vector2(offsetX, offsetY),
            knockbackForce = knockback, slamSpeed = speed, duration = duration,
            preDelay = preDelay, postDelay = postDelay, targetDistanceRatio = targetDistanceRatio
        };

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder("Assets/Data/Enemys")) AssetDatabase.CreateFolder("Assets/Data", "Enemys");
        if (!AssetDatabase.IsValidFolder(DataFolder)) AssetDatabase.CreateFolder("Assets/Data/Enemys", "Stage2");
    }
}
#endif
