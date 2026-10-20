using System.IO;
using UnityEditor;
using UnityEngine;

public static class PlayerCombatEffectsSetup
{
    private const int ProjectPixelsPerUnit = 64;

    [MenuItem("Tools/Effects/Setup Player Combat FX")]
    private static void SetupSelectedPlayerPrefab()
    {
        string prefabPath = GetSelectedPlayerPrefabPath();
        if (string.IsNullOrEmpty(prefabPath))
        {
            Debug.LogError("Player 프리팹을 Project 창에서 선택한 뒤 다시 실행하세요.");
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

        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            if (root.GetComponent<PlayerController>() == null)
            {
                Debug.LogError("선택한 프리팹 루트에 PlayerController가 없습니다.");
                return;
            }

            PlayerCombatEffects effects = root.GetComponent<PlayerCombatEffects>();
            if (effects == null)
                effects = root.AddComponent<PlayerCombatEffects>();

            SerializedObject serializedEffects = new SerializedObject(effects);
            serializedEffects.FindProperty("cutMedium").objectReferenceValue = cutMedium;
            serializedEffects.FindProperty("slashBig").objectReferenceValue = slashBig;
            serializedEffects.FindProperty("slashMedium").objectReferenceValue = slashMedium;
            serializedEffects.FindProperty("sting").objectReferenceValue = sting;
            serializedEffects.FindProperty("impactBig").objectReferenceValue = impactBig;
            serializedEffects.FindProperty("impactMedium").objectReferenceValue = impactMedium;
            serializedEffects.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Player 공격/피격 이펙트 연결 완료: {prefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static string GetSelectedPlayerPrefabPath()
    {
        Object selected = Selection.activeObject;
        string selectedPath = selected != null ? AssetDatabase.GetAssetPath(selected) : null;
        if (!string.IsNullOrEmpty(selectedPath) && selectedPath.EndsWith(".prefab") &&
            Path.GetFileNameWithoutExtension(selectedPath).StartsWith("Player"))
            return selectedPath;

        string[] candidates = AssetDatabase.FindAssets("Player t:Prefab");
        string match = null;
        foreach (string guid in candidates)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) != "Player")
                continue;
            if (match != null)
            {
                Debug.LogWarning("Player 프리팹이 여러 개 있습니다. 사용할 프리팹을 Project 창에서 선택하세요.");
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
            if (!path.EndsWith("/" + fileName + ".png") || !path.Contains("/" + requiredFolder + "/"))
                continue;

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
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
                {
                    importer.SaveAndReimport();
                    AssetDatabase.Refresh();
                }
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
                return sprite;
        }

        Debug.LogError($"'{fileName}.png'을 '{requiredFolder}' 폴더에서 찾지 못했습니다.");
        return null;
    }
}
