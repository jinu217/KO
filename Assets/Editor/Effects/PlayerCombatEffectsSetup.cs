using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayerCombatEffectsSetup
{
    private const int ProjectPixelsPerUnit = 64;

    [MenuItem("Tools/Effects/Setup Player Combat FX")]
    private static void SetupCombatEffects()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.isUpdating || AnimationMode.InAnimationMode())
        {
            Debug.LogError("Play, Animation Preview/Record를 종료하고 컴파일이 끝난 뒤 실행하세요.");
            return;
        }

        Sprite cutMedium = LoadSheet("cut_medium", "공격");
        Sprite slashBig = LoadSheet("slash_big", "공격");
        Sprite slashMedium = LoadSheet("slash_medium", "공격");
        Sprite sting = LoadSheet("sting", "공격");
        Sprite impactBig = LoadSheet("impact_big", "데미지받을시");
        Sprite impactMedium = LoadSheet("impact_medium", "데미지받을시");

        if (cutMedium == null || slashBig == null || slashMedium == null || sting == null ||
            impactBig == null || impactMedium == null)
        {
            Debug.LogError("공격/피격 PNG 시트를 모두 찾지 못했습니다. Assets/Sprites/사용할 이펙트 폴더에 압축을 풀었는지 확인하세요.");
            return;
        }

        List<string> scenePaths = GetSelectedScenePaths();
        int updatedScenes = 0;
        int skippedScenes = 0;

        foreach (string scenePath in scenePaths)
        {
            if (ApplyToScene(scenePath, cutMedium, slashBig, slashMedium, sting, impactBig, impactMedium))
                updatedScenes++;
            else
                skippedScenes++;
        }

        string prefabPath = GetSelectedPlayerPrefabPath();
        if (scenePaths.Count == 0 && string.IsNullOrEmpty(prefabPath))
            prefabPath = FindUniquePlayerPrefabPath();
        if (!string.IsNullOrEmpty(prefabPath))
        {
            if (ApplyToPrefab(prefabPath, cutMedium, slashBig, slashMedium, sting, impactBig, impactMedium))
                Debug.Log($"Player Combat FX 프리팹 적용 완료: {prefabPath}");
        }

        if (scenePaths.Count > 0)
        {
            Debug.Log($"Player Combat FX 씬 적용 완료: {updatedScenes}개 씬 / 생략·오류: {skippedScenes}개 씬");
            return;
        }

        if (string.IsNullOrEmpty(prefabPath))
            Debug.LogError("Project 창에서 씬 파일/씬 폴더 또는 Player 프리팹을 선택한 뒤 실행하세요.");
    }

    private static List<string> GetSelectedScenePaths()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (UnityEngine.Object selected in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(selected);
            if (string.IsNullOrEmpty(path))
                continue;

            if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            {
                paths.Add(path);
            }
            else if (AssetDatabase.IsValidFolder(path))
            {
                foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { path }))
                {
                    string scenePath = AssetDatabase.GUIDToAssetPath(guid);
                    if (scenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                        paths.Add(scenePath);
                }
            }
        }

        var result = new List<string>(paths);
        result.Sort(StringComparer.OrdinalIgnoreCase);
        return result;
    }

    private static bool ApplyToScene(string scenePath, Sprite cutMedium, Sprite slashBig,
        Sprite slashMedium, Sprite sting, Sprite impactBig, Sprite impactMedium)
    {
        string validationError;
        if (!ValidateLocalReferences(scenePath, out validationError))
        {
            Debug.LogWarning($"Combat FX 생략 (씬 참조 확인 필요): {scenePath}\n{validationError}");
            return false;
        }

        Scene scene = SceneManager.GetSceneByPath(scenePath);
        bool openedByTool = !scene.IsValid();
        if (scene.IsValid() && (!scene.isLoaded || scene.isDirty))
        {
            Debug.LogWarning($"Combat FX 생략 (열린 씬에 미저장 변경이 있거나 unloaded 상태): {scenePath}");
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
                Debug.LogWarning($"Combat FX 생략: 씬 로딩 오류가 있어 변경/저장하지 않았습니다. {scenePath}\n" +
                    string.Join("\n", loadProblems));
                return false;
            }
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning($"씬을 열지 못해 생략: {scenePath}");
                return false;
            }

            Transform player = FindCommonPlayer(scene);
            if (player == null)
            {
                Debug.LogWarning($"Common 아래 Player를 하나로 찾지 못해 생략: {scenePath}");
                return false;
            }
            if (player.GetComponent<PlayerController>() == null)
            {
                Debug.LogWarning($"PlayerController가 없어 생략: {scenePath}");
                return false;
            }

            // 저장 전 원본을 보관합니다. Assets 밖이므로 Unity가 에셋으로 임포트하지 않습니다.
            string backupPath = Path.Combine("Library", "PlayerCombatFXSceneBackups",
                DateTime.Now.ToString("yyyyMMdd_HHmmss_fff"), scenePath);
            Directory.CreateDirectory(Path.GetDirectoryName(backupPath));
            File.Copy(scenePath, backupPath, false);

            PlayerCombatEffects effects = player.GetComponent<PlayerCombatEffects>();
            if (effects == null)
                effects = openedByTool ? player.gameObject.AddComponent<PlayerCombatEffects>() :
                    Undo.AddComponent<PlayerCombatEffects>(player.gameObject);

            Configure(effects, cutMedium, slashBig, slashMedium, sting, impactBig, impactMedium, !openedByTool);
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
            Debug.LogError($"Combat FX 적용 중 오류 ({scenePath}): {exception}");
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

    private static Transform FindCommonPlayer(Scene scene)
    {
        var matches = new List<Transform>();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
            {
                if (candidate.name != "Player")
                    continue;

                Transform ancestor = candidate.parent;
                while (ancestor != null && ancestor.name != "Common")
                    ancestor = ancestor.parent;
                if (ancestor != null)
                    matches.Add(candidate);
            }
        }
        return matches.Count == 1 ? matches[0] : null;
    }

    private static bool ApplyToPrefab(string prefabPath, Sprite cutMedium, Sprite slashBig,
        Sprite slashMedium, Sprite sting, Sprite impactBig, Sprite impactMedium)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            if (root.GetComponent<PlayerController>() == null)
            {
                Debug.LogError("선택한 프리팹 루트에 PlayerController가 없습니다.");
                return false;
            }

            PlayerCombatEffects effects = root.GetComponent<PlayerCombatEffects>();
            if (effects == null)
                effects = root.AddComponent<PlayerCombatEffects>();

            Configure(effects, cutMedium, slashBig, slashMedium, sting, impactBig, impactMedium, false);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            AssetDatabase.SaveAssets();
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void Configure(PlayerCombatEffects effects, Sprite cutMedium, Sprite slashBig,
        Sprite slashMedium, Sprite sting, Sprite impactBig, Sprite impactMedium, bool recordUndo)
    {
        if (recordUndo)
            Undo.RecordObject(effects, "Setup Player Combat FX");
        var serialized = new SerializedObject(effects);
        serialized.FindProperty("cutMedium").objectReferenceValue = cutMedium;
        serialized.FindProperty("slashBig").objectReferenceValue = slashBig;
        serialized.FindProperty("slashMedium").objectReferenceValue = slashMedium;
        serialized.FindProperty("sting").objectReferenceValue = sting;
        serialized.FindProperty("impactBig").objectReferenceValue = impactBig;
        serialized.FindProperty("impactMedium").objectReferenceValue = impactMedium;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(effects);
        if (PrefabUtility.IsPartOfPrefabInstance(effects))
            PrefabUtility.RecordPrefabInstancePropertyModifications(effects);
    }

    private static string GetSelectedPlayerPrefabPath()
    {
        foreach (UnityEngine.Object selected in Selection.objects)
        {
            string selectedPath = AssetDatabase.GetAssetPath(selected);
            if (!string.IsNullOrEmpty(selectedPath) && selectedPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) &&
                Path.GetFileNameWithoutExtension(selectedPath).StartsWith("Player", StringComparison.Ordinal))
                return selectedPath;
        }

        string activePath = Selection.activeObject != null ? AssetDatabase.GetAssetPath(Selection.activeObject) : null;
        if (!string.IsNullOrEmpty(activePath) && activePath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) &&
            Path.GetFileNameWithoutExtension(activePath).StartsWith("Player", StringComparison.Ordinal))
            return activePath;
        return null;
    }

    private static string FindUniquePlayerPrefabPath()
    {
        string match = null;
        foreach (string guid in AssetDatabase.FindAssets("Player t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) != "Player")
                continue;
            if (match != null)
            {
                Debug.LogWarning("Player 프리팹이 여러 개 있습니다. Project 창에서 사용할 프리팹을 선택하세요.");
                return null;
            }
            match = path;
        }
        return match;
    }

    private static Sprite LoadSheet(string fileName, string requiredFolder)
    {
        string[] guids = AssetDatabase.FindAssets(fileName + " t:Texture2D");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith("/" + fileName + ".png", StringComparison.OrdinalIgnoreCase) ||
                !path.Contains("/" + requiredFolder + "/"))
                continue;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                bool changed = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    changed = true;
                }
                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    changed = true;
                }
                if (!importer.isReadable)
                {
                    importer.isReadable = true;
                    changed = true;
                }
                if (importer.spritePixelsPerUnit != ProjectPixelsPerUnit)
                {
                    importer.spritePixelsPerUnit = ProjectPixelsPerUnit;
                    changed = true;
                }
                if (importer.filterMode != FilterMode.Point)
                {
                    importer.filterMode = FilterMode.Point;
                    changed = true;
                }
                if (importer.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    changed = true;
                }
                if (changed)
                    importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return null;
    }
}
