#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class Stage5MainBossFactory
{
    private const string DataPath = "Assets/Data/Enemys/Stage5/Stage5_MainBoss.asset";
    private const string PrefabPath = "Assets/Prefabs/Stage5_Enemy 1/Stage5_MainBoss.prefab";

    [InitializeOnLoadMethod]
    private static void ScheduleFirstImport() => EditorApplication.delayCall += CreateIfMissing;

    private static void CreateIfMissing()
    {
        MainBossData data = AssetDatabase.LoadAssetAtPath<MainBossData>(DataPath);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (data != null && prefab != null && prefab.GetComponent<Stage5_MainBoss>() != null
            && prefab.GetComponent<Stage5_MainBossDesireSkill>() != null) return;
        CreateOrUpdate();
    }

    [MenuItem("Tools/Boss/Create Stage5 MainBoss")]
    public static void CreateOrUpdate()
    {
        MainBossData data = AssetDatabase.LoadAssetAtPath<MainBossData>(DataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MainBossData>();
            Configure(data);
            AssetDatabase.CreateAsset(data, DataPath);
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null) throw new System.InvalidOperationException("Stage5_MainBoss 프리팹을 찾지 못했습니다.");
        try
        {
            MainBossController old = root.GetComponent<MainBossController>();
            LayerMask player = old != null ? old.playerLayer : (LayerMask)LayerMask.GetMask("Player");
            LayerMask wall = old != null ? old.wallLayer : (LayerMask)LayerMask.GetMask("Ground");
            Transform check = old != null ? old.wallCheck : root.transform.Find("WallCheck");
            Stage5_MainBoss boss = root.GetComponent<Stage5_MainBoss>();
            if (boss == null)
            {
                if (old != null) Object.DestroyImmediate(old);
                boss = root.AddComponent<Stage5_MainBoss>();
            }
            // 이전 스테이지 전용 욕망 컴포넌트가 복사되어 있다면 이 프리팹에서는 사용하지 않는다.
            Stage2_MainBossDesireSkill oldDesire = root.GetComponent<Stage2_MainBossDesireSkill>();
            if (oldDesire != null) Object.DestroyImmediate(oldDesire);
            if (root.GetComponent<Stage5_MainBossDesireSkill>() == null)
                root.AddComponent<Stage5_MainBossDesireSkill>();
            boss.data = data;
            boss.playerLayer = player;
            boss.wallLayer = wall;
            boss.wallCheck = check;
            EditorUtility.SetDirty(boss);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("Stage5 MainBoss 일반 공격·스킬 데이터 및 프리팹 연결 완료");
    }

    private static void Configure(MainBossData data)
    {
        data.maxHp = 2000f;
        data.chaseSpeed = 3.1f;
        data.detectRange = 12f;
        data.chaseRange = 22f;
        data.positionUpdateDelay = .12f;
        data.wallCheckDistance = .55f;
        data.phase2HpPercent = .7f;
        data.phase3HpPercent = .3f;

        Vector2 sharedSize = new Vector2(2.8f, 2.2f);
        Vector2 sharedOffset = new Vector2(1.45f, -.1f);
        var slam = Hit("1타 제자리 검 내려찍기", 11f, sharedSize, sharedOffset,
            new Vector2(4f, 1.5f), .35f, .1f);
        var risingSpin = new BossAttackStepData
        {
            stepName = "2타 검 회전하며 일어서기 (넉백 없음)",
            actionType = BossActionType.PersistentHitbox, duration = .72f,
            interval = .18f, attackCount = 4, damage = 3f,
            hitboxSize = sharedSize, hitboxOffset = sharedOffset,
            knockbackForce = Vector2.zero, persistentFixedCadence = true,
            preDelay = .1f, postDelay = .12f
        };
        var lunge = Dash("3타 전방 검 내지르기", 16f,
            new Vector2(2.5f, 1.5f), new Vector2(1.5f, 0f),
            new Vector2(7f, 2f), 11f, 2.8f, .35f);
        lunge.preDelay = .12f;
        lunge.postDelay = .42f;
        data.basicAttack = Active("기본 공격 검 3연타", 3.1f, 3.2f, slam, risingSpin, lunge);

        BossSkillData swordWalk = Active("1스킬 이기어검 8걸음", 13f, 5.2f);
        swordWalk.postDelay = .55f;
        for (int i = 0; i < 8; i++)
        {
            swordWalk.active.attackSteps.Add(new BossAttackStepData
            {
                stepName = $"{i + 1}걸음 플레이어에게 접근",
                actionType = BossActionType.MoveForward, moveSpeed = 1.25f,
                duration = .32f, maxMoveDistance = .5f, stopAtWall = true
            });
            var cuts = Hit($"{i + 1}걸음 주변 4회 검격", 2.5f,
                new Vector2(3.4f, 2.4f), Vector2.zero, Vector2.zero, .04f, .08f);
            cuts.attackCount = 4;
            cuts.interval = .11f;
            swordWalk.active.attackSteps.Add(cuts);
        }

        BossSkillData buff2 = new BossSkillData
        {
            skillName = "2스킬 버프 (내용 미정)", skillType = BossSkillType.Buff,
            minimumPhase = 99, cooldown = 20f
        };

        var floatUp = new BossAttackStepData
        {
            stepName = "1단계 살짝 떠오르기", actionType = BossActionType.Jump,
            jumpForce = new Vector2(0f, 4.5f), duration = .65f
        };
        var rush = Dash("2단계 공중 전방 돌진", 15f,
            new Vector2(2.5f, 2.4f), new Vector2(1.25f, -.5f),
            new Vector2(1.5f, 1f), 15f, 6f, .48f);
        rush.stopOnPlayerHit = true;
        var vault = Dash("3단계 적중 시 위로 돌아 플레이어 뒤 공격", 18f,
            new Vector2(2.2f, 2.4f), Vector2.zero,
            new Vector2(4f, 2f), 10f, 3.5f, .75f);
        vault.postDelay = .45f;
        BossSkillData aerial = Active("3스킬 공중 돌진 후 뒤로 넘기", 11f, 7f,
            floatUp, rush, vault);
        aerial.postDelay = .5f;

        BossSkillData buff4 = new BossSkillData
        {
            skillName = "4스킬 버프 (내용 미정)", skillType = BossSkillType.Buff,
            minimumPhase = 99, cooldown = 20f
        };

        BossSkillData cross = Active("5스킬 플레이어 주변 교차 베기", 15f, 9f);
        cross.minimumPhase = 2;
        cross.preDelay = .45f;
        cross.postDelay = .65f;
        for (int i = 0; i < 4; i++)
        {
            var slash = Dash($"{i + 1}번째 방향 교차 베기", 10f,
                new Vector2(2f, 2.1f), Vector2.zero,
                new Vector2(1.5f, .5f), 18f, 6f, .28f);
            slash.postDelay = .08f;
            cross.active.attackSteps.Add(slash);
        }

        data.skills.Clear();
        data.skills.Add(swordWalk);
        data.skills.Add(buff2);
        data.skills.Add(aerial);
        data.skills.Add(buff4);
        data.skills.Add(cross);
    }

    private static BossSkillData Active(string name, float cooldown, float range,
        params BossAttackStepData[] steps)
    {
        var skill = new BossSkillData
        {
            skillName = name, skillType = BossSkillType.Active,
            cooldown = cooldown, castRange = range
        };
        skill.active.attackSteps.AddRange(steps);
        return skill;
    }

    private static BossAttackStepData Hit(string name, float damage, Vector2 size,
        Vector2 offset, Vector2 knockback, float preDelay, float postDelay)
        => new BossAttackStepData
        {
            stepName = name, actionType = BossActionType.MeleeAttack,
            attackCount = 1, damage = damage,
            hitboxSize = size, hitboxOffset = offset,
            knockbackForce = knockback, preDelay = preDelay, postDelay = postDelay
        };

    private static BossAttackStepData Dash(string name, float damage, Vector2 size,
        Vector2 offset, Vector2 knockback, float speed, float distance, float duration)
        => new BossAttackStepData
        {
            stepName = name, actionType = BossActionType.DashAttack,
            attackCount = 1, damage = damage,
            hitboxSize = size, hitboxOffset = offset, knockbackForce = knockback,
            moveSpeed = speed, maxMoveDistance = distance,
            duration = duration, stopAtWall = true
        };

    public static void Validate()
    {
        MainBossData data = AssetDatabase.LoadAssetAtPath<MainBossData>(DataPath);
        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        if (root == null) throw new System.InvalidOperationException("Stage5_MainBoss 프리팹이 없습니다.");
        try
        {
            Stage5_MainBoss boss = root.GetComponent<Stage5_MainBoss>();
            Require(data != null && boss != null && boss.data == data, "전용 스크립트/데이터 연결");
            Require(root.GetComponent<Stage5_MainBossDesireSkill>() != null, "욕망 컴포넌트 연결");
            Require(root.GetComponent<Stage2_MainBoss>() == null, "이전 스테이지 스크립트 제거");
            Require(root.GetComponent<Stage2_MainBossDesireSkill>() == null, "이전 욕망 컴포넌트 제거");
            Require(boss.wallCheck != null && boss.playerLayer.value != 0, "벽 감지/플레이어 레이어");
            Require(data.basicAttack.active.attackSteps.Count == 3, "기본 공격 3연타");
            var first = data.basicAttack.active.attackSteps[0];
            var second = data.basicAttack.active.attackSteps[1];
            Require(first.hitboxSize == second.hitboxSize && first.hitboxOffset == second.hitboxOffset
                && second.knockbackForce == Vector2.zero, "1·2타 동일 범위와 2타 넉백 없음");
            Require(data.skills.Count == 5 && data.skills[0].active.attackSteps.Count == 16,
                "1스킬 8걸음과 각 4회 공격");
            for (int i = 1; i < 16; i += 2)
                Require(data.skills[0].active.attackSteps[i].attackCount == 4, "걸음별 4회 판정");
            Require(data.skills[1].minimumPhase == 99 && data.skills[3].minimumPhase == 99,
                "내용 미정 버프 비활성화");
            var aerial = data.skills[2];
            Require(aerial.active.attackSteps.Count == 3 && aerial.active.attackSteps[1].stopOnPlayerHit,
                "공중 돌진 적중 조건");
            typeof(MidBossController).GetField("currentSkillIndex",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(boss, 2);
            typeof(Stage5_MainBoss).GetMethod("OnSkillExecutionStarted",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(boss, new object[] { aerial, 2 });
            Require(!boss.ShouldExecuteStep(aerial.active.attackSteps[2]), "돌진 미적중 시 3타 금지");
            boss.OnSkillAttackHit(aerial.active.attackSteps[1]);
            Require(boss.ShouldExecuteStep(aerial.active.attackSteps[2]), "돌진 적중 시 3타 허용");
            Require(data.skills[4].active.attackSteps.Count == 4, "5스킬 네 방향 베기");
            Debug.Log("Stage5 MainBoss 검증 통과: 전용 연결, 기본 3연타, 8×4 검격, 조건부 3타, 네 방향 베기, 욕망 컴포넌트");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException("Stage5 MainBoss 검증 실패: " + message);
    }
}
#endif
