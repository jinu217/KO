#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class BossEffectsSetup
{
    [MenuItem("Tools/Effects/Bosses/Apply All Stage 1-5 Prefabs")]
    public static void ApplyAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Texture2D attack, shot, hit, death, chase, spin;
        Sprite[] notice;
        try
        {
            attack = Load("공격/slash_big.png");
            shot = Load("공격/sting.png");
            spin = Load("공격/cut_medium.png");
            hit = Load("데미지받을시/impact_big.png");
            death = Load("공격&데미지받을시&사망&대쉬등 다있는곳/death_01.png");
            chase = Load("걸을때 지속.png");
            notice = Enumerable.Range(1, 6).Select(i =>
                AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(Load("대쉬/SM7001_" + i.ToString("00") + ".png")))
                    .OfType<Sprite>().Single()).ToArray();
            if (attack.width % 10 != 0 || shot.width % 10 != 0 || hit.width % 10 != 0 || death.width % 24 != 0 || chase.width % 8 != 0)
                throw new InvalidOperationException("이펙트 시트 크기를 확인하세요.");
        }
        catch (Exception e) { Debug.LogException(e); return; }
        int count = 0;
        for (int stage = 1; stage <= 5; stage++)
        {
            string folder = "Assets/Prefabs/Stage" + stage + "_Enemy";
            if (!AssetDatabase.IsValidFolder(folder)) { Debug.LogWarning("보스 FX 폴더 없음: " + folder); continue; }
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (asset == null || asset.GetComponentInChildren<MidBossController>(true) == null) continue;
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    foreach (var boss in root.GetComponentsInChildren<MidBossController>(true))
                    {
                        var fx = boss.GetComponent<BossEffects>();
                        bool added = fx == null;
                        if (added) fx = boss.gameObject.AddComponent<BossEffects>();
                        fx.stage = stage;
                        fx.mainBoss = boss is MainBossController;
                        fx.attack.sheet = attack; fx.attack.columns = 10; fx.attack.flipY = true; fx.attack.scale = 2;
                        fx.upward.sheet = attack; fx.upward.columns = 10; fx.upward.flipY = false; fx.upward.scale = 2;
                        fx.horizontal.sheet = shot; fx.horizontal.columns = 10; fx.horizontal.flipY = false; fx.horizontal.scale = 2;
                        fx.spin.sheet = spin; fx.spin.columns = 10; fx.spin.flipY = false; fx.spin.scale = 2;
                        fx.punch.sheet = hit; fx.punch.columns = 10; fx.punch.flipY = false; fx.punch.scale = 1.6f;
                        fx.dash.frames = notice; fx.dash.sheet = null; fx.dash.scale = 3; fx.dash.fps = 20;
                        // 플레이어 대쉬 원본 이미지와 동일한 좌우 방향.
                        fx.dash.artFacesRight = false;
                        fx.dash.offset = new Vector2(-.3f, 0);
                        if (fx.hit.sheet == null) fx.hit.sheet = hit;
                        if (fx.death.sheet == null) fx.death.sheet = death;
                        if (fx.chase.sheet == null) fx.chase.sheet = chase;
                        if (fx.notice.frames == null || fx.notice.frames.Length == 0) fx.notice.frames = notice;
                        fx.motionAnimator = boss.GetComponentInChildren<Animator>(true);
                        fx.weaponAnchor = boss.GetComponentsInChildren<Transform>(true).FirstOrDefault(t =>
                            new[] { "Sword", "Weapon", "Axe", "Scythe", "Spear", "Mace" }.Contains(t.name));
                        if (added) { fx.hit.scale = 1.2f; fx.death.scale = 2; }
                        if (stage == 1 && !fx.mainBoss) ConfigureComboTimings(fx);
                        EditorUtility.SetDirty(fx);
                        count++;
                    }
                    string backup = "Library/BossEffectsBackups/" + path;
                    if (!File.Exists(backup)) { Directory.CreateDirectory(Path.GetDirectoryName(backup)); File.Copy(path, backup); }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                catch (Exception e) { Debug.LogError(path + "\n" + e); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("보스 FX 적용 완료: " + count + "개. 연결된 씬 프리팹에도 적용됩니다.");
    }
    static void ConfigureComboTimings(BossEffects fx)
    {
        var list = new System.Collections.Generic.List<BossEffects.ImpactTiming>();
        if (fx.motionAnimator != null && fx.motionAnimator.runtimeAnimatorController != null)
            foreach (var clip in fx.motionAnimator.runtimeAnimatorController.animationClips.Distinct())
            {
                if (clip == null || (!clip.name.Contains("Combo2") && !clip.name.Contains("Combo3"))) continue;
                float impact = -1;
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if (binding.propertyName != "m_LocalScale.y" || !binding.path.EndsWith("/Weapon")) continue;
                    var keys = AnimationUtility.GetEditorCurve(clip, binding).keys;
                    for (int i = 1; i < keys.Length; i++)
                        if (keys[i - 1].value < 0 && keys[i].value > 0) impact = keys[i].time;
                }
                if (impact >= 0)
                {
                    list.Add(new BossEffects.ImpactTiming { clip = clip, horizontalImpact = impact });
                    Debug.Log(clip.name + " 가로베기 타격: " + impact.ToString("F3") + "초 / " + (impact * clip.frameRate).ToString("F1") + "프레임 (클립 원본 시간)");
                }
                else Debug.LogWarning(clip.name + ": 가로베기 반전 키를 찾지 못했습니다. BossEffects Combo Timings에 타격 시간을 지정하세요.");
            }
        fx.comboTimings = list.ToArray();
    }
    [MenuItem("Tools/Effects/Bosses/Report Spawn Prefabs")]
    public static void ReportSpawns()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root == null) continue;
            foreach (var boss in root.GetComponentsInChildren<MidBossController>(true))
            {
                if (boss.data == null) continue;
                var skills = new System.Collections.Generic.List<BossSkillData> { boss.data.basicAttack };
                if (boss.data.skills != null) skills.AddRange(boss.data.skills);
                foreach (var skill in skills)
                    if (skill?.active?.attackSteps != null)
                        foreach (var step in skill.active.attackSteps)
                            if (step != null && step.actionType == BossActionType.SpawnPrefab)
                                Debug.Log(path + " / " + skill.skillName + " / " + (step.spawnPrefab != null ? AssetDatabase.GetAssetPath(step.spawnPrefab) : "미지정"));
                foreach (var component in boss.GetComponents<MonoBehaviour>())
                {
                    if (component == null || !component.GetType().Name.Contains("Desire")) continue;
                    var property = new SerializedObject(component).GetIterator();
                    while (property.NextVisible(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is GameObject)
                            Debug.Log(path + " / " + component.GetType().Name + "." + property.name + " / " + AssetDatabase.GetAssetPath(property.objectReferenceValue));
                }
            }
        }
    }
    static Texture2D Load(string suffix)
    {
        var paths = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(suffix) + " t:Texture2D")
            .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith("/사용할 이펙트/" + suffix, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (paths.Length != 1) throw new InvalidOperationException("이펙트를 하나로 찾을 수 없음: " + suffix);
        var importer = (TextureImporter)AssetImporter.GetAtPath(paths[0]);
        importer.textureType = TextureImporterType.Sprite;
        if (importer.spriteImportMode == SpriteImportMode.None) importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.spritePixelsPerUnit = 64;
        importer.maxTextureSize = Mathf.Max(4096, importer.maxTextureSize);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(paths[0]);
    }
}
#endif
