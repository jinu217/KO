using UnityEditor;
using UnityEngine;

public class SpriteSettingsBatchSetter
{
    [MenuItem("Tools/Sprites/Set Selected Sprite Settings")]
    public static void SetSelectedSprites()
    {
        int count = 0;

        // Project 창에서 선택한 파일 전부 가져옴 (폴더를 선택했으면 그 안의 파일들까지 전부 포함)
        Object[] selectedObjects = Selection.GetFiltered<Object>(
            SelectionMode.Assets | SelectionMode.DeepAssets
        );

        foreach (Object obj in selectedObjects)
        {
            string path = AssetDatabase.GetAssetPath(obj);

            // Texture2D가 아니면 무시
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            bool changed = false;

            // Texture Type: Sprite
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            // Sprite Mode: Single
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            // PPU: 64
            if (importer.spritePixelsPerUnit != 64f)
            {
                importer.spritePixelsPerUnit = 64f;
                changed = true;
            }

            // Pivot: Custom
            // 기존 Pivot 위치 유지
            var spriteSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(spriteSettings);

            if (spriteSettings.spriteAlignment != (int)SpriteAlignment.Custom)
            {
                spriteSettings.spriteAlignment = (int)SpriteAlignment.Custom;
                importer.SetTextureSettings(spriteSettings);
                changed = true;
            }

            // Filter Mode: Point
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }

            if (changed)
            {
                EditorUtility.SetDirty(importer);
                AssetDatabase.ImportAsset(
                    path,
                    ImportAssetOptions.ForceUpdate
                );

                count++;
                Debug.Log($"[SpriteSettings] 적용: {path}");
            }
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "완료",
            $"{count}개 스프라이트 설정 완료",
            "확인"
        );
    }

    // 선택된 파일이 없으면 메뉴 비활성화
    [MenuItem("Tools/Sprites/Set Selected Sprite Settings", true)]
    private static bool ValidateSetSelectedSprites()
    {
        return Selection.objects != null &&
               Selection.objects.Length > 0;
    }
}