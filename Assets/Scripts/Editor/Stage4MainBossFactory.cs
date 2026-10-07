#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class Stage4MainBossFactory
{
    private const string DataPath = "Assets/Data/Enemys/Stage4/Stage4_MainBoss.asset";
    private const string PrefabPath = "Assets/Prefabs/Stage4_Enemy/Stage4_MainBoss.prefab";
    [MenuItem("Tools/Boss/Create Stage4 MainBoss")]
    public static void CreateOrUpdate()
    {
        var data = AssetDatabase.LoadAssetAtPath<MainBossData>(DataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MainBossData>();
            Stage4BossPatterns.Configure(data);
            AssetDatabase.CreateAsset(data, DataPath);
        }
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var old = root.GetComponent<MidBossController>();
            var player = old != null ? old.playerLayer : (LayerMask)LayerMask.GetMask("Player");
            var wall = old != null ? old.wallLayer : (LayerMask)LayerMask.GetMask("Ground");
            var check = old != null ? old.wallCheck : root.transform.Find("WallCheck");
            var boss = root.GetComponent<Stage4_MainBoss>();
            if (boss == null)
            {
                if (old != null) Object.DestroyImmediate(old);
                boss = root.AddComponent<Stage4_MainBoss>();
            }
            var desire = root.GetComponent<Stage4_MainBossDesireSkill>();
            if (desire == null) desire = root.AddComponent<Stage4_MainBossDesireSkill>();
            boss.data = data; boss.playerLayer = player; boss.wallLayer = wall; boss.wallCheck = check;
            var serialized = new SerializedObject(boss);
            serialized.FindProperty("desireSkill").objectReferenceValue = desire;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
        Validate();
        Debug.Log("Stage4 MainBoss 생성 및 연결 완료");
    }

    // 실제 프리팹/데이터와 적중 콜백을 검사하는 배치 검증 진입점.
    public static void Validate()
    {
        var root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            var boss = root.GetComponent<Stage4_MainBoss>();
            var desire = root.GetComponent<Stage4_MainBossDesireSkill>();
            Require(boss != null && desire != null && boss.data != null, "보스 구성 누락");
            Require(root.GetComponent<Stage2_MainBoss>() == null, "이전 보스 컴포넌트 잔존");
            Require(boss.data.skills.Count == 5, "일반 스킬 수");
            Require(boss.data.basicAttack.active.attackSteps.Count == 3, "기본 공격 3타");
            desire.Initialize(boss);
            Set(boss, "desireSkill", desire);
            Require(desire.Basic.active.attackSteps.Count == 4, "강화 기본 공격 4타");
            Require(desire.Skill(1).active.attackSteps.Count == 5, "강화 어퍼컷 후 도약 및 스트레이트");
            Require(desire.Skill(3).active.attackSteps.Count == 6, "강화 러시 후 스트레이트");
            Require(boss.data.basicAttack.active.attackSteps.Count == 3, "원본 에셋 불변");

            var skill = boss.data.skills[1];
            Begin(boss, skill, 1);
            Require(!boss.ShouldExecuteStep(skill.active.attackSteps[2]), "보디 실패 시 어퍼컷 금지");
            boss.OnSkillAttackHit(skill.active.attackSteps[1]);
            Require(boss.ShouldExecuteStep(skill.active.attackSteps[2]), "보디 적중 시 어퍼컷 허용");

            Set(desire, "active", true);
            Set(desire, "end", Time.time + 100);
            Set(boss, "currentHp", 300f);
            Begin(boss, desire.Basic, -1);
            foreach (var step in desire.Basic.active.attackSteps) boss.OnSkillAttackHit(step);
            Require(Mathf.Approximately(boss.Hp, 300 + boss.data.maxHp * .01f), "기본 공격 패턴당 회복 1회");
            Begin(boss, desire.Skill(1), 1);
            var body = desire.Skill(1).active.attackSteps[1];
            var upper = desire.Skill(1).active.attackSteps[2];
            boss.OnSkillAttackHit(body); boss.OnSkillAttackHit(body);
            boss.OnSkillAttackHit(upper); boss.OnSkillAttackHit(upper);
            Require(Mathf.Approximately(boss.Hp, 300 + boss.data.maxHp * .05f), "보디/어퍼컷 회복 각각 1회");

            Set(desire, "startHp", 300f);
            Set(desire, "end", Time.time - 1);
            Invoke(desire, "Update");
            Require(!desire.Active && !desire.BlocksCombat, "정확히 최대 HP 5% 증가하면 정상 종료");
            Set(desire, "active", true);
            Set(boss, "currentHp", 300 + boss.data.maxHp * .05f - 1);
            Invoke(desire, "Update");
            Require(desire.BlocksCombat, "목표보다 1 부족하면 그로기");
            desire.Cancel();
            Debug.Log("Stage4 MainBoss 검증 통과: 연결, 조건부 연계, 회복 상한, HP 종료 경계");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static void Begin(Stage4_MainBoss boss, BossSkillData skill, int index)
    {
        Set(boss, "currentSkillIndex", index);
        typeof(Stage4_MainBoss).GetMethod("OnSkillExecutionStarted", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Invoke(boss, new object[] { skill, index });
    }
    private static void Set(object target, string name, object value)
    {
        for (var type = target.GetType(); type != null; type = type.BaseType)
        {
            var field = type.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly);
            if (field == null) continue;
            field.SetValue(target, value); return;
        }
        throw new System.InvalidOperationException(name);
    }
    private static void Invoke(object target, string name)
        => target.GetType().GetMethod(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(target, null);
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException("Stage4 검증 실패: " + message);
    }
}
#endif
