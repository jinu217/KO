#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PlayerMovementEffectsSetup
{
    [MenuItem("Tools/Player/Effects/Setup Walk Jump Dash")]
    static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Play를 종료하세요.");
        string[] roots = Directory.GetDirectories("Assets", "사용할 이펙트", SearchOption.AllDirectories);
        if (roots.Length != 1) throw new InvalidOperationException("Assets 아래 사용할 이펙트 폴더가 정확히 하나 필요합니다.");
        string root = roots[0].Replace('\\', '/');
        string sheetPath = root + "/걸을때 지속.png";
        if (!File.Exists(sheetPath)) sheetPath = root + "/걸을 때 지속.png";
        Import(sheetPath, false);
        Texture2D sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
        if (sheet == null || sheet.width != 160 || sheet.height != 20)
            throw new InvalidOperationException("걸음 시트는 제공된 160x20 PNG여야 합니다.");
        Sprite[] jump = Frames(root + "/점프", "SP701_", 8);
        Sprite[] dash = Frames(root + "/대쉬", "SM7001_", 6);
        GameObject selected = Selection.activeGameObject;
        if (selected != null && !EditorUtility.IsPersistent(selected))
        {
            PlayerController controller = selected.GetComponentInParent<PlayerController>();
            if (controller == null) controller = selected.GetComponentInChildren<PlayerController>(true);
            if (controller == null) throw new InvalidOperationException("선택 오브젝트에서 PlayerController를 찾지 못했습니다.");
            Configure(controller.gameObject, sheet, jump, dash, true);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            Debug.Log("선택한 씬 Player에 이펙트 연결 완료. 씬을 저장하세요."); return;
        }
        string prefabPath = selected != null ? AssetDatabase.GetAssetPath(selected) : "";
        if (!prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
        {
            string[] candidates = AssetDatabase.FindAssets("Player t:Prefab").Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p) == "Player").ToArray();
            if (candidates.Length != 1) throw new InvalidOperationException("Project 창에서 사용할 Player 프리팹을 선택한 후 실행하세요.");
            prefabPath = candidates[0];
        }
        GameObject contents = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            PlayerController[] controllers = contents.GetComponentsInChildren<PlayerController>(true);
            if (controllers.Length != 1) throw new InvalidOperationException("PlayerController가 정확히 하나 필요합니다.");
            Configure(controllers[0].gameObject, sheet, jump, dash, false);
            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
        AssetDatabase.SaveAssets(); Debug.Log("Player 이동 이펙트 연결 완료: " + prefabPath);
    }
    [MenuItem("Tools/Player/Effects/Apply To Selected Scenes Or Folders")]
    static void BatchScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || AnimationMode.InAnimationMode())
            throw new InvalidOperationException("Play와 Animation Preview를 종료하세요.");
        var paths = new System.Collections.Generic.SortedSet<string>(StringComparer.Ordinal);
        foreach (UnityEngine.Object item in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(item);
            if (AssetDatabase.IsValidFolder(path))
                foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { path })) paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            else if (path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)) paths.Add(path);
        }
        if (paths.Count == 0) throw new InvalidOperationException("Project 창에서 씬 파일 또는 씬이 들어 있는 폴더를 선택하세요.");
        Texture2D sheet; Sprite[] jump, dash; LoadEffects(out sheet, out jump, out dash);
        var report = new System.Text.StringBuilder(); int updated = 0, skipped = 0;
        var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        try
        {
            foreach (string path in paths)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
                bool opened = false; int undoGroup = -1;
                try
                {
                    if (scene.IsValid() && (!scene.isLoaded || scene.isDirty))
                    { report.AppendLine("생략 (열린 씬의 미저장 변경 또는 unloaded 상태): " + path); skipped++; continue; }
                    if (!scene.IsValid())
                    { scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path, UnityEditor.SceneManagement.OpenSceneMode.Additive); opened = true; }
                    var matches = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                        .Where(t => t.name == "Player" && HasCommonParent(t)).ToArray();
                    if (matches.Length != 1 || matches[0].GetComponent<PlayerController>() == null)
                    { report.AppendLine("생략 (Common 아래 Player/컨트롤러가 없거나 중복): " + path); skipped++; continue; }
                    Undo.IncrementCurrentGroup(); undoGroup = Undo.GetCurrentGroup();
                    Undo.SetCurrentGroupName("Apply player movement effects");
                    Configure(matches[0].gameObject, sheet, jump, dash, true);
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
                    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene)) throw new IOException("씬 저장 실패");
                    Undo.CollapseUndoOperations(undoGroup); undoGroup = -1;
                    updated++; report.AppendLine("완료: " + path);
                }
                catch (Exception ex)
                {
                    if (undoGroup >= 0) Undo.RevertAllDownToGroup(undoGroup);
                    skipped++; report.AppendLine("오류: " + path + " — " + ex.Message);
                }
                finally
                {
                    if (opened && scene.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
        finally { if (active.IsValid() && active.isLoaded) UnityEngine.SceneManagement.SceneManager.SetActiveScene(active); }
        Debug.Log("이동 이펙트 적용: " + updated + "개 씬 / 생략·오류: " + skipped + "\n" + report);
    }
    static bool HasCommonParent(Transform child)
    {
        for (Transform t = child.parent; t != null; t = t.parent) if (t.name == "Common") return true;
        return false;
    }
    static void LoadEffects(out Texture2D sheet, out Sprite[] jump, out Sprite[] dash)
    {
        string[] roots = Directory.GetDirectories("Assets", "사용할 이펙트", SearchOption.AllDirectories);
        if (roots.Length != 1) throw new InvalidOperationException("Assets 아래 사용할 이펙트 폴더가 정확히 하나 필요합니다.");
        string root = roots[0].Replace('\\', '/');
        string sheetPath = root + "/걸을때 지속.png";
        if (!File.Exists(sheetPath)) sheetPath = root + "/걸을 때 지속.png";
        Import(sheetPath, false); sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
        if (sheet == null || sheet.width != 160 || sheet.height != 20) throw new InvalidOperationException("걸음 시트는 160x20 PNG여야 합니다.");
        jump = Frames(root + "/점프", "SP701_", 8); dash = Frames(root + "/대쉬", "SM7001_", 6);
    }
    static void Configure(GameObject player, Texture2D sheet, Sprite[] jump, Sprite[] dash, bool scene)
    {
        PlayerMovementEffects fx = player.GetComponent<PlayerMovementEffects>();
        bool added = fx == null;
        if (added) fx = scene ? Undo.AddComponent<PlayerMovementEffects>(player) : player.AddComponent<PlayerMovementEffects>();
        if (scene) Undo.RecordObject(fx, "Setup movement effects");
        if (added)
        {
            fx.walkArtFacesRight = false; fx.jumpArtFacesRight = false;
            fx.dashArtFacesRight = true; fx.reverseDashImage = true;
        }
        fx.walkOffset = new Vector2(-.7f, -.1f);
        fx.jumpOffset = Vector2.zero;
        fx.dashOffset = new Vector2(0, -.1f);
        fx.walkSheet = sheet; fx.walkColumns = 8; fx.jumpFrames = jump; fx.dashFrames = dash;
        fx.groundAnchor = player.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => string.Equals(t.name, "GroundCheck", StringComparison.OrdinalIgnoreCase));
        EditorUtility.SetDirty(fx);
        if (scene && PrefabUtility.IsPartOfPrefabInstance(fx)) PrefabUtility.RecordPrefabInstancePropertyModifications(fx);
    }
    static Sprite[] Frames(string folder, string prefix, int count)
    {
        Sprite[] frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            string path = folder + "/" + prefix + (i + 1).ToString("00") + ".png";
            Import(path, true); frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (frames[i] == null) throw new InvalidOperationException("Sprite 로드 실패: " + path);
        }
        return frames;
    }
    static void Import(string path, bool sprite)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("이펙트 파일이 없습니다", path);
        AssetDatabase.ImportAsset(path);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("PNG Importer 없음: " + path);
        importer.textureType = sprite ? TextureImporterType.Sprite : TextureImporterType.Default;
        if (sprite)
        {
            importer.spriteImportMode = SpriteImportMode.Single; importer.spritePixelsPerUnit = 64;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom; settings.spritePivot = new Vector2(.5f, 0);
            importer.SetTextureSettings(settings);
        }
        importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true; importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }
}
#endif
