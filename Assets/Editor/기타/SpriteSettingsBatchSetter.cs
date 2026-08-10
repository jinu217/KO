using UnityEditor;
using UnityEngine;

public class SpriteSettingsBatchSetter
{
    [MenuItem("Tools/Sprites/Set All Sprite Settings")]
    public static void SetAllSprites()
    {
        int count = 0;
        string[] guids = AssetDatabase.FindAssets("t:Texture2D");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool changed = false;

            // Texture Type: Sprite
            if (importer.textureType != TextureImporterType.Sprite)
            { importer.textureType = TextureImporterType.Sprite; changed = true; }

            // Sprite Mode: Single
            if (importer.spriteImportMode != SpriteImportMode.Single)
            { importer.spriteImportMode = SpriteImportMode.Single; changed = true; }

            // PPU: 64
            if (importer.spritePixelsPerUnit != 64f)
            { importer.spritePixelsPerUnit = 64f; changed = true; }

            // Pivot: Custom (기존 pivot 위치 유지)
            var spriteSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(spriteSettings);
            if (spriteSettings.spriteAlignment != (int)SpriteAlignment.Custom)
            {
                spriteSettings.spriteAlignment = (int)SpriteAlignment.Custom;
                importer.SetTextureSettings(spriteSettings);
                changed = true;
            }

            // Filter Mode: Point (No Filter)
            if (importer.filterMode != FilterMode.Point)
            { importer.filterMode = FilterMode.Point; changed = true; }

            if (changed)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                count++;
                Debug.Log($"[SpriteSettings] 적용: {path}");
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료", $"{count}개 스프라이트 설정 완료", "확인");
    }
}
