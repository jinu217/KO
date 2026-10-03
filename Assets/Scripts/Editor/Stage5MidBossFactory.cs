#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class Stage5MidBossFactory
{
    private const string DataPath = "Assets/Data/Enemys/Stage5/Stage5_MidBoss.asset";
    private const string PrefabPath = "Assets/Prefabs/Stage5_Enemy 1/Stage5_MidBoss.prefab";

    [MenuItem("Tools/Boss/Create Stage5 MidBoss")]
    public static void CreateOrUpdate()
    {
        MidBossData data = AssetDatabase.LoadAssetAtPath<MidBossData>(DataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MidBossData>();
            Configure(data);
            AssetDatabase.CreateAsset(data, DataPath);
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null) throw new System.InvalidOperationException("Stage5_MidBoss 프리팹을 찾지 못했습니다.");
        try
        {
            MidBossController previous = root.GetComponent<MidBossController>();
            LayerMask playerLayer = previous != null ? previous.playerLayer : (LayerMask)LayerMask.GetMask("Player");
            LayerMask wallLayer = previous != null ? previous.wallLayer : (LayerMask)LayerMask.GetMask("Ground");
            Transform wallCheck = previous != null ? previous.wallCheck : root.transform.Find("WallCheck");
            Stage5_MidBoss boss = root.GetComponent<Stage5_MidBoss>();
            if (boss == null)
            {
                if (previous != null) Object.DestroyImmediate(previous);
                boss = root.AddComponent<Stage5_MidBoss>();
            }
            boss.data = data;
            boss.playerLayer = playerLayer;
            boss.wallLayer = wallLayer;
            boss.wallCheck = wallCheck;
            EditorUtility.SetDirty(boss);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("Stage5 MidBoss 데이터 및 프리팹 연결 완료");
    }

    private static void Configure(MidBossData data)
    {
        data.maxHp = 850f;
        data.chaseSpeed = 2.8f;
        data.detectRange = 11f;
        data.chaseRange = 19f;
        data.positionUpdateDelay = .12f;
        data.wallCheckDistance = .55f;

        data.basicAttack = Active("기본 공격 기 모아 2연격", 3.4f, 3.3f,
            new BossAttackStepData
            {
                stepName = "1타 1초 기 모아 짧은 돌진", actionType = BossActionType.DashAttack,
                preDelay = 1f, postDelay = .18f, duration = .28f,
                moveSpeed = 10f, maxMoveDistance = 2.2f, stopAtWall = true,
                attackCount = 1, damage = 10f, hitboxSize = new Vector2(2.1f, 1.25f),
                hitboxOffset = new Vector2(1.25f, -.1f), knockbackForce = new Vector2(3f, 1f)
            },
            new BossAttackStepData
            {
                stepName = "2타 1초 기 모아 전방 내려찍기", actionType = BossActionType.MeleeAttack,
                preDelay = 1f, postDelay = .5f, attackCount = 1, damage = 18f,
                hitboxSize = new Vector2(2.6f, 2.3f), hitboxOffset = new Vector2(1.45f, -.25f),
                knockbackForce = new Vector2(8f, 3f)
            });

        BossAttackStepData flyingSwords = new BossAttackStepData
        {
            stepName = "정사각형 검격 3초 5회", actionType = BossActionType.PersistentHitbox,
            duration = 3f, interval = .6f, attackCount = 5, damage = 5f,
            hitboxSize = new Vector2(3.2f, 3.2f), hitboxOffset = new Vector2(2.5f, 0f),
            knockbackForce = new Vector2(.8f, .3f), persistentFixedCadence = true
        };
        BossSkillData swordSkill = Active("1스킬 이기어검술", 9f, 4.2f, flyingSwords);
        swordSkill.preDelay = 1f;
        swordSkill.postDelay = .5f;

        BossAttackStepData pull = new BossAttackStepData
        {
            stepName = "2초간 약하게 지속 유인", actionType = BossActionType.PullPlayer,
            duration = 2f, moveSpeed = 1.3f,
            hitboxSize = new Vector2(6f, 2.5f), hitboxOffset = new Vector2(3f, 0f),
            pullTargetOffset = new Vector2(1.6f, 0f)
        };
        BossAttackStepData dash = new BossAttackStepData
        {
            stepName = "플레이어 방향 돌진", actionType = BossActionType.DashAttack,
            duration = .65f, moveSpeed = 13f, maxMoveDistance = 7f,
            stopAtWall = true, stopOnPlayerHit = true, attackCount = 1,
            damage = 16f, hitboxSize = new Vector2(2.2f, 1.4f),
            hitboxOffset = new Vector2(1.3f, -.1f), knockbackForce = new Vector2(10f, 3f),
            postDelay = .6f
        };
        BossSkillData pullDash = Active("2스킬 유인 후 돌진", 11f, 6f, pull, dash);

        BossSkillData buff = new BossSkillData
        {
            skillName = "3스킬 버프 (내용 미정)", skillType = BossSkillType.Buff,
            minimumPhase = 99, cooldown = 18f
        };
        data.skills.Clear();
        data.skills.Add(swordSkill);
        data.skills.Add(pullDash);
        data.skills.Add(buff);
    }

    private static BossSkillData Active(string name, float cooldown, float castRange, params BossAttackStepData[] steps)
    {
        BossSkillData skill = new BossSkillData
        {
            skillName = name, skillType = BossSkillType.Active,
            cooldown = cooldown, castRange = castRange
        };
        skill.active.attackSteps.AddRange(steps);
        return skill;
    }

    public static void Validate()
    {
        MidBossData data = AssetDatabase.LoadAssetAtPath<MidBossData>(DataPath);
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            Stage5_MidBoss boss = root.GetComponent<Stage5_MidBoss>();
            Require(boss != null && boss.data == data, "전용 스크립트 또는 데이터 참조");
            Require(root.GetComponent<Stage2_MidBoss>() == null, "2스테이지 스크립트 잔존");
            Require(boss.wallCheck != null && boss.playerLayer.value != 0, "감지/플레이어 레이어");
            Require(data.basicAttack.active.attackSteps.Count == 2, "기본 공격 2타");
            Require(data.skills.Count == 3, "스킬 슬롯 수");
            var sword = data.skills[0].active.attackSteps[0];
            Require(sword.persistentFixedCadence && sword.attackCount == 5 && Mathf.Approximately(sword.duration, 3f), "이기어검술 5회/3초");
            var pull = data.skills[1].active.attackSteps[0];
            Require(pull.actionType == BossActionType.PullPlayer && Mathf.Approximately(pull.duration, 2f), "2초 유인 데이터");
            typeof(Stage5_MidBoss).GetMethod("OnSkillExecutionStarted",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(boss, new object[] { data.skills[1], 1 });
            Require(boss.UsesTimedMovement(pull), "2스킬 유인 전용 실행 연결");
            typeof(Stage5_MidBoss).GetMethod("OnSkillExecutionFinished",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(boss, new object[] { 1 });
            Require(!boss.UsesTimedMovement(pull), "유인 종료 후 참조 해제");
            Require(data.skills[2].minimumPhase == 99, "미정 버프 비활성화");
            Debug.Log("Stage5 MidBoss 검증 통과: 전용 연결, 2연격, 5회 검격, 2초 유인, 버프 슬롯");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static void Require(bool condition, string description)
    {
        if (!condition) throw new System.InvalidOperationException("Stage5 MidBoss 검증 실패: " + description);
    }
}
#endif
