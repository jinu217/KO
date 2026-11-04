#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
static class MainBoss1PatternFXReview
{
    const string Folder = "C:/Users/User/Documents/Codex/2026-10-26/new-chat-3/work/mainboss1_pattern_review";
    static MainBoss1PatternFXReview() { EditorApplication.update += Poll; }
    static void Poll()
    {
        if(EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(Folder + "/request.txt")) return;
        string request = File.ReadAllText(Folder + "/request.txt");
        if(SessionState.GetString("MainBoss1PatternFXReview", "") == request) return;
        SessionState.SetString("MainBoss1PatternFXReview", request);
        try { Verify(); } catch(Exception e) { File.WriteAllText(Folder + "/result.txt", e.ToString()); }
    }
    static void Require(bool ok, string message) { if(!ok) throw new Exception(message); }
    static void Verify()
    {
        var settings = MainBoss1SlashComparison.Settings;
        var oldMode = settings.mode;
        var report = new StringBuilder();
        string path = "Assets/Prefabs/Stage1_Enemy/Stage1_MainBoss.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var fx = root.GetComponent<BossEffects>();
            var data = root.GetComponent<MidBossController>().data;
            var desire = root.GetComponent<Stage1_DesireSkill>();
            if(desire == null) desire = root.AddComponent<Stage1_DesireSkill>();
            desire.Initialize(data);
            settings.mode = MainBoss1SlashComparison.DisplayMode.OriginalSlash;
            Check(fx, data.basicAttack, -1, "VH", report);
            Check(fx, data.skills[0], 0, "VVH", report);
            Check(fx, desire.EmpoweredSkill1, 0, "VHVH", report);
            Check(fx, desire.EnhancedBasicAttack, -1, "VHV", report);
            Check(fx, desire.EnhancedSkill1, 0, "VVHVH", report);
            Check(fx, data.skills[3], 3, "H", report);
            foreach(var skill in new[] {data.skills[1], desire.EnhancedSkill2})
                foreach(var step in skill.active.attackSteps)
                    Require(fx.ResolveForPreview(skill, 1, step, 0, out _) == BossEffects.Kind.None, "Projectile modified");
            settings.mode = MainBoss1SlashComparison.DisplayMode.WeaponTrail;
            Require(fx.ResolveForPreview(data.basicAttack, -1, data.basicAttack.active.attackSteps[0], 0, out _) == BossEffects.Kind.Horizontal, "Current mode changed");
            settings.mode = MainBoss1SlashComparison.DisplayMode.OriginalSlash;
            for(int stage = 1; stage <= 5; stage++) foreach(bool main in new[] {false, true})
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Prefabs/Stage{stage}_Enemy/Stage{stage}_{(main ? "Main" : "Mid")}Boss.prefab");
                Require(MainBoss1SlashComparison.UseOriginal(prefab.GetComponent<BossEffects>()) == (stage == 1 && main), "Other boss changed");
            }
            report.AppendLine("Projectile / current trail mode / other 9 bosses preserved. DONE");
        }
        finally { settings.mode = oldMode; PrefabUtility.UnloadPrefabContents(root); }
        File.WriteAllText(Folder + "/result.txt", report.ToString());
    }
    static void Check(BossEffects fx, BossSkillData skill, int index, string expected, StringBuilder report)
    {
        string actual = "";
        foreach(var step in skill.active.attackSteps)
        {
            if(step.damage <= 0 || step.actionType == BossActionType.SpawnPrefab) continue;
            var kind = fx.ResolveForPreview(skill, index, step, 0, out var angle);
            Require(kind == BossEffects.Kind.Down || kind == BossEffects.Kind.Horizontal, "Unexpected kind " + kind);
            actual += kind == BossEffects.Kind.Horizontal ? "H" : "V";
            foreach(int facing in new[] {-1, 1})
            {
                var instance = fx.PreviewStep(skill, index, step, 0, fx.transform.position, facing, .08f);
                Require(instance != null, "Missing original FX");
                try
                {
                    var sr = instance.GetComponent<SpriteRenderer>();
                    string sheet = kind == BossEffects.Kind.Horizontal ? "slash_medium" : "slash_big";
                    Require(sr.enabled && sr.sprite != null && sr.sprite.texture.name == sheet, "Wrong/missing sheet " + sheet + " actual=" + (sr.sprite != null ? sr.sprite.texture.name : "null") + " enabled=" + sr.enabled + " configured=" + (MainBoss1SlashComparison.Settings.horizontalSlash.sheet != null ? MainBoss1SlashComparison.Settings.horizontalSlash.sheet.name : "null"));
                    Require(Mathf.Abs(Mathf.DeltaAngle(instance.transform.eulerAngles.z, angle * facing)) < .01f, "Wrong facing rotation");
                    Require(Mathf.Approximately(instance.transform.localScale.x, instance.transform.localScale.y), "Distorted sheet");
                    Require(sr.color.a == 1f, "Unwanted faded echo");
                }
                finally { UnityEngine.Object.DestroyImmediate(instance.gameObject); }
            }
        }
        Require(actual == expected, skill.skillName + " " + actual + " != " + expected);
        report.AppendLine(skill.skillName + ": " + actual + " | both facings / slash sheet / uniform scale PASS");
    }
}
#endif
