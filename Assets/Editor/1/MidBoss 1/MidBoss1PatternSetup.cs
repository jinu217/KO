#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// 기존 데이터의 데미지/범위를 최대한 보존하고 패턴 구성과 시각만 갱신합니다.
public static class MidBoss1PatternSetup
{
    const string Folder = "Assets/Prefabs/Stage1_Enemy";
    const string AnimFolder = "Assets/Animations/1/MidBoss 1";

    [MenuItem("Tools/1/MidBoss 1/Apply Combo Patterns And Effects")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!AssetDatabase.IsValidFolder(Folder)) throw new InvalidOperationException("폴더 없음: " + Folder);
        Backup(AnimFolder + "/MidBoss 1_AC.controller");
        MidBoss1_AnimGenerators.GenerateComboAttacks();
        MidBoss1_AnimGenerators.SetupGameplayAnimator();
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(AnimFolder + "/MidBoss 1_AC.controller");
        if (controller == null) throw new InvalidOperationException("MidBoss 1_AC 생성 실패");
        var combo2 = controller.animationClips.FirstOrDefault(c => c.name.EndsWith("Combo2"));
        var combo3 = controller.animationClips.FirstOrDefault(c => c.name.EndsWith("Combo3"));
        float[] times2 = MidBoss1_AnimGenerators.GetComboImpactTimes(combo2);
        float[] times3 = MidBoss1_AnimGenerators.GetComboImpactTimes(combo3);
        var updated = new HashSet<UnityEngine.Object>();
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || asset.GetComponentInChildren<Stage1_MidBoss>(true) == null) continue;
            Backup(path);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var boss in root.GetComponentsInChildren<Stage1_MidBoss>(true))
                {
                    if (boss.data == null) throw new InvalidOperationException(path + ": MidBossData 없음");
                    var animator = boss.GetComponentInChildren<Animator>(true);
                    if (animator == null) throw new InvalidOperationException(path + ": Animator 없음");
                    animator.runtimeAnimatorController = controller;
                    if (updated.Add(boss.data))
                    {
                        Backup(AssetDatabase.GetAssetPath(boss.data));
                        Undo.RecordObject(boss.data, "MidBoss1 Combo Patterns");
                        var data = boss.data;
                        if (data.skills == null) throw new InvalidOperationException("Skills 목록이 없습니다.");
                        // 기존 2/3연격을 우선 찾아 그 데미지, 범위, 쿨타임을 이어받습니다.
                        var sources = new List<BossSkillData> { data.basicAttack };
                        sources.AddRange(data.skills);
                        var source2 = sources.FirstOrDefault(s => HitCount(s) == 2) ?? data.basicAttack;
                        var source3 = sources.FirstOrDefault(s => HitCount(s) == 3) ??
                            (data.skills.Count > 1 ? data.skills[1] : source2);
                        var dashSource = sources.FirstOrDefault(s => s?.active?.attackSteps != null &&
                            s.active.attackSteps.Any(t => t != null && t.actionType == BossActionType.DashAttack));
                        var basic = BuildCombo(source2, "기본 공격 세로 → 가로", times2, combo2.length);
                        if (data.basicAttack != null)
                        { basic.cooldown = data.basicAttack.cooldown; basic.castRange = data.basicAttack.castRange; }
                        var triple = BuildCombo(source3, "2스킬 세로 → 세로 → 가로", times3, combo3.length);
                        var dash = BuildDash(dashSource);
                        while (data.skills.Count < 3) data.skills.Add(new BossSkillData());
                        data.basicAttack = basic;
                        data.skills[1] = triple;
                        data.skills[2] = dash;
                        EditorUtility.SetDirty(data);
                    }
                    var fx = boss.GetComponent<BossEffects>();
                    if (fx == null) fx = boss.gameObject.AddComponent<BossEffects>();
                    fx.stage = 1;
                    fx.mainBoss = false;
                    fx.motionAnimator = animator;
                    fx.dash.offset = new Vector2(-.3f, 0f);
                    BossEffectsSetup.ConfigureComboTimings(fx);
                    var serialized = new SerializedObject(boss);
                    serialized.FindProperty("bossAnimator").objectReferenceValue = animator;
                    serialized.FindProperty("meleeReachBonus").floatValue = .2f;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(fx);
                    count++;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[MidBoss1] " + count + "개 적용. 기본 Combo2 / 2스킬 Combo3 / 3스킬 Dash.\n" +
            "Combo2 타격(초): " + string.Join(", ", times2.Select(t => t.ToString("F3"))) + "\n" +
            "Combo3 타격(초): " + string.Join(", ", times3.Select(t => t.ToString("F3"))) +
            "\n도끼 판정 전방 +0.2, 돌진 FX 발 뒤. 1스킬은 기존 유지.");
    }

    static int HitCount(BossSkillData skill)
    {
        if (skill?.active?.attackSteps == null) return 0;
        return skill.active.attackSteps.Where(s => s != null &&
            (s.actionType == BossActionType.MeleeAttack || s.actionType == BossActionType.Slam))
            .Sum(s => Mathf.Max(1, s.attackCount));
    }
    static T Copy<T>(T value) where T : class, new()
        => value == null ? new T() : JsonUtility.FromJson<T>(JsonUtility.ToJson(value));

    static BossSkillData BuildCombo(BossSkillData source, string name, float[] times, float length)
    {
        var skill = Copy(source);
        var templates = new List<BossAttackStepData>();
        if (source?.active?.attackSteps != null)
            foreach (var s in source.active.attackSteps)
                if (s != null && (s.actionType == BossActionType.MeleeAttack || s.actionType == BossActionType.Slam))
                    for (int i = 0; i < Mathf.Max(1, s.attackCount); i++) templates.Add(s);
        skill.skillName = name;
        skill.skillType = BossSkillType.Active;
        skill.preDelay = 0;
        skill.postDelay = 0;
        if (skill.cooldown <= 0) skill.cooldown = times.Length == 2 ? 2.5f : 7f;
        if (skill.castRange <= 0) skill.castRange = 3f;
        if (skill.active == null) skill.active = new BossActiveSkillData();
        skill.active.attackSteps = new List<BossAttackStepData>();
        for (int i = 0; i < times.Length; i++)
        {
            var step = templates.Count > 0 ? Copy(templates[Mathf.Min(i, templates.Count - 1)]) :
                new BossAttackStepData { damage = 10, hitboxSize = new Vector2(3, 2.5f), hitboxOffset = new Vector2(1.5f, 0) };
            step.stepName = i == times.Length - 1 ? "가로 베기" : "세로 베기 " + (i + 1);
            step.actionType = BossActionType.MeleeAttack;
            step.attackCount = 1;
            step.interval = 0;
            step.duration = 0;
            step.preDelay = times[i] - (i == 0 ? 0 : times[i - 1]);
            step.postDelay = i == times.Length - 1 ? Mathf.Max(0, length - times[i]) : 0;
            skill.active.attackSteps.Add(step);
        }
        return skill;
    }
    static BossSkillData BuildDash(BossSkillData source)
    {
        var skill = Copy(source);
        var original = source?.active?.attackSteps?.FirstOrDefault(s => s != null && s.actionType == BossActionType.DashAttack);
        var step = Copy(original);
        skill.skillName = "3스킬 긴 준비 후 전방 돌진";
        skill.skillType = BossSkillType.Active;
        float preparation = Mathf.Max(1f, skill.preDelay + step.preDelay);
        skill.preDelay = 0;
        if (skill.cooldown <= 0) skill.cooldown = 10;
        if (skill.castRange <= 0) skill.castRange = 8;
        step.stepName = "전방 돌진";
        step.actionType = BossActionType.DashAttack;
        step.preDelay = preparation;
        step.moveSpeed = Mathf.Max(10, step.moveSpeed);
        if (step.duration <= 0) step.duration = .5f;
        if (step.maxMoveDistance <= 0) step.maxMoveDistance = 5;
        step.stopAtWall = true;
        step.attackCount = Mathf.Max(1, step.attackCount);
        if (original == null)
        {
            step.damage = 14;
            step.hitboxSize = new Vector2(2.5f, 2f);
            step.hitboxOffset = new Vector2(1, 0);
        }
        if (skill.active == null) skill.active = new BossActiveSkillData();
        skill.active.attackSteps = new List<BossAttackStepData> { step };
        return skill;
    }
    static void Backup(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        string target = "Library/MidBoss1PatternBackups/" + path;
        if (File.Exists(target)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(target));
        File.Copy(path, target);
    }
}
#endif
