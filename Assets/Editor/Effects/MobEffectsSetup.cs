#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MobEffectsSetup
{
    static Texture2D attackSheet, shotSheet, hitSheet, deathSheet, chaseSheet;
    static Sprite[] noticeFrames;
    [MenuItem("Tools/Effects/Mobs/Apply To Selected Scenes Or Prefabs Or Folders")]
    public static void ApplySelected()
    {
        if (!Ready()) return;
        var paths = new HashSet<string>();
        foreach (var selected in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(selected);
            if (AssetDatabase.IsValidFolder(path))
            {
                foreach (string filter in new[] { "t:Scene", "t:Prefab" })
                    foreach (var guid in AssetDatabase.FindAssets(filter, new[] { path })) paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            }
            else if (path.EndsWith(".prefab") || path.EndsWith(".unity")) paths.Add(path);
        }
        if (paths.Count == 0) { Debug.LogWarning("Project 창에서 씬, 프리팹 또는 해당 폴더를 선택하세요."); return; }
        Run(paths);
    }
    [MenuItem("Tools/Effects/Mobs/Apply All Stage 1-5 Prefabs")]
    public static void ApplyAll()
    {
        if (!Ready()) return;
        var paths = AssetDatabase.FindAssets("t:Prefab").Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => Regex.IsMatch(p, @"^Assets/Prefabs/Stage[1-5]_Enemy/", RegexOptions.IgnoreCase)).ToArray();
        if (paths.Length == 0)
        {
            Debug.LogWarning("Assets/Prefabs/Stage1_Enemy ~ Stage5_Enemy 폴더에서 프리팹을 찾지 못했습니다.");
            return;
        }
        Run(paths);
    }
    static bool Ready()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating || AnimationMode.InAnimationMode())
        { Debug.LogWarning("Play와 Animation Preview를 종료하고 컴파일 완료 후 실행하세요."); return false; }
        return true;
    }
    static void Run(IEnumerable<string> paths)
    {
        try { LoadEffects(); } catch (Exception e) { Debug.LogError(e.Message); return; }
        int done = 0, skipped = 0;
        foreach (string path in paths.OrderBy(p => p))
        {
            try { bool changed = path.EndsWith(".unity") ? ApplyToScene(path) : ApplyToPrefab(path); if (changed) done++; else skipped++; }
            catch (Exception e) { skipped++; Debug.LogError(path + "\n" + e); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"일반몹 FX: {done}개 에셋 적용 / {skipped}개 생략. 보스·NPC·훈련용 더미 제외.");
    }
    static bool IsTarget(EnemyController enemy)
    {
        return (enemy is MeleeEnemy || enemy is RangedEnemy) && enemy.GetComponent<TutorialTrainingDummy>() == null;
    }
    static bool ApplyToPrefab(string path)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null || !asset.GetComponentsInChildren<EnemyController>(true).Any(IsTarget)) return false;
        Backup(path);
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (var enemy in root.GetComponentsInChildren<EnemyController>(true).Where(IsTarget)) Configure(enemy, false);
            PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            if (!success) throw new InvalidOperationException("프리팹 저장 실패: " + path);
            return true;
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    static void Backup(string path)
    {
        string destination = Path.Combine("Library", "MobFXSceneBackups", DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"), path);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)); File.Copy(path, destination, false);
    }
    static void Configure(EnemyController enemy, bool undo)
    {
        var fx = enemy.GetComponent<MobEffects>();
        if (fx == null) fx = undo ? Undo.AddComponent<MobEffects>(enemy.gameObject) : enemy.gameObject.AddComponent<MobEffects>();
        if (undo) Undo.RecordObject(fx, "Setup Mob FX");
        // Preserve existing per-prefab offsets, speed, scale and user-selected effects on reruns.
        if (fx.attack.sheet == null) fx.attack.sheet = attackSheet;
        if (fx.shot.sheet == null) fx.shot.sheet = shotSheet;
        if (fx.hit.sheet == null) fx.hit.sheet = hitSheet;
        if (fx.death.sheet == null) fx.death.sheet = deathSheet;
        if (fx.chase.sheet == null) fx.chase.sheet = chaseSheet;
        if (fx.notice.frames == null || fx.notice.frames.Length == 0) fx.notice.frames = noticeFrames;
        EditorUtility.SetDirty(fx);
        if (PrefabUtility.IsPartOfPrefabInstance(fx)) PrefabUtility.RecordPrefabInstancePropertyModifications(fx);
    }
    static void LoadEffects()
    {
        attackSheet = LoadTexture("공격/slash_medium.png");
        shotSheet = LoadTexture("공격/sting.png");
        hitSheet = LoadTexture("데미지받을시/impact_medium.png");
        deathSheet = LoadTexture("공격&데미지받을시&사망&대쉬등 다있는곳/death_01.png");
        chaseSheet = LoadTexture("걸을때 지속.png");
        noticeFrames = new Sprite[6];
        for (int i = 0; i < 6; i++)
        {
            var texture = LoadTexture("대쉬/SM7001_" + (i + 1).ToString("00") + ".png");
            string path = AssetDatabase.GetAssetPath(texture);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
            if (sprites.Length != 1) throw new InvalidOperationException("발견 먼지 PNG는 Sprite 1개여야 합니다: " + path);
            noticeFrames[i] = sprites[0];
        }
        if (attackSheet.width % 10 != 0 || hitSheet.width % 10 != 0 || shotSheet.width % 10 != 0 || deathSheet.width % 24 != 0 || chaseSheet.width % 8 != 0)
            throw new InvalidOperationException("이펙트 시트 크기가 원본과 다릅니다. 임포트 Max Size/플랫폼 Override를 확인하세요.");
    }
    static Texture2D LoadTexture(string suffix)
    {
        var matches = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(suffix) + " t:Texture2D")
            .Select(AssetDatabase.GUIDToAssetPath).Where(p => p.EndsWith("/사용할 이펙트/" + suffix, StringComparison.OrdinalIgnoreCase)).ToArray();
        if (matches.Length != 1) throw new InvalidOperationException("이펙트 PNG를 하나로 찾지 못했습니다: " + suffix);
        string path = matches[0]; var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        if (importer.spriteImportMode == SpriteImportMode.None) importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
        importer.spritePixelsPerUnit = 64; importer.maxTextureSize = Mathf.Max(4096, importer.maxTextureSize);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static bool ApplyToScene(string scenePath)
    {
        string validationError;
        if (!ValidateLocalReferences(scenePath, out validationError))
        {
            Debug.LogWarning($"Mob FX 생략 (씬 참조 확인 필요): {scenePath}\n{validationError}");
            return false;
        }

        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool openedByTool = !scene.IsValid();
        if (scene.IsValid() && (!scene.isLoaded || scene.isDirty))
        {
            Debug.LogWarning($"Mob FX 생략 (열린 씬에 미저장 변경이 있거나 unloaded 상태): {scenePath}");
            return false;
        }

        Scene previousActive = SceneManager.GetActiveScene();
        var loadProblems = new List<string>();
        Application.LogCallback onLoadLog = (message, stack, type) =>
        {
            if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception ||
                message.IndexOf("Transform component could not be found", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("Broken text PPtr", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("SetSystemInterested", StringComparison.OrdinalIgnoreCase) >= 0 ||
                message.IndexOf("previous changes registered", StringComparison.OrdinalIgnoreCase) >= 0)
                loadProblems.Add(message);
        };
        try
        {
            if (openedByTool)
            {
                Application.logMessageReceived += onLoadLog;
                try
                {
                    scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }
                finally
                {
                    Application.logMessageReceived -= onLoadLog;
                }
            }

            if (loadProblems.Count > 0)
            {
                Debug.LogWarning($"Mob FX 생략: 씬 로딩 오류가 있어 변경/저장하지 않았습니다. {scenePath}\n" +
                    string.Join("\n", loadProblems));
                return false;
            }
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning($"씬을 열지 못해 생략: {scenePath}");
                return false;
            }

            var targets = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<EnemyController>(true)).Where(IsTarget).ToArray();
            if (targets.Length == 0) return false;

            // 저장 전 원본을 보관합니다. Assets 밖이므로 Unity가 에셋으로 임포트하지 않습니다.
            string backupPath = Path.Combine("Library", "MobFXSceneBackups",
                DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"), scenePath);
            Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
            File.Copy(scenePath, backupPath, false);

            foreach (var target in targets) Configure(target, !openedByTool);
            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene))
            {
                Debug.LogError($"씬 저장 실패: {scenePath}");
                return false;
            }
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Mob FX 적용 중 오류 ({scenePath}): {exception}");
            return false;
        }
        finally
        {
            Application.logMessageReceived -= onLoadLog;
            if (openedByTool)
            {
                // OpenScene이 예외를 던진 뒤에도 씬이 등록되어 있을 수 있습니다.
                Scene loaded = SceneManager.GetSceneByPath(scenePath);
                if (loaded.IsValid() && loaded.isLoaded)
                    EditorSceneManager.CloseScene(loaded, true);
            }
            if (previousActive.IsValid() && previousActive.isLoaded)
                SceneManager.SetActiveScene(previousActive);
        }
    }

    private static bool ValidateLocalReferences(string scenePath, out string error)
    {
        error = null;
        string yaml = File.ReadAllText(scenePath);
        if (!yaml.TrimStart('\uFEFF', ' ', '\r', '\n').StartsWith("%YAML", StringComparison.Ordinal))
        {
            error = "텍스트 YAML 씬이 아니므로 자동 저장하지 않습니다.";
            return false;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in Regex.Matches(yaml, @"(?m)^--- !u!\d+ &(-?\d+)(?: stripped)?\s*$"))
            ids.Add(match.Groups[1].Value);

        foreach (Match match in Regex.Matches(yaml, @"\{\s*fileID:\s*(-?\d+)([^}]*?)\}"))
        {
            string id = match.Groups[1].Value;
            // guid가 있는 참조는 다른 에셋의 ID이므로 여기서 검사하지 않습니다.
            if (id == "0" || match.Groups[2].Value.IndexOf("guid:", StringComparison.Ordinal) >= 0)
                continue;
            if (!ids.Contains(id))
            {
                error = $"씬 내부 fileID {id}의 정의가 없습니다. 원본 씬 복구/참조 수정이 필요합니다.";
                return false;
            }
        }
        return true;
    }

}
#endif
