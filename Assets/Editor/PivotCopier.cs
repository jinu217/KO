using UnityEditor;
using UnityEngine;
using System.IO;

public class PivotCopier : EditorWindow
{
    [MenuItem("Tools/Pivot Copier")]
    static void OpenWindow()
    {
        GetWindow<PivotCopier>("Pivot Copier");
    }

    string sourceFolder = "Assets/Sprites/Knight";
    string targetFolder = "Assets/Sprites/Goblin";
    bool matchByFilename = true;

    void OnGUI()
    {
        GUILayout.Label("원본 → 대상으로 Pivot 복사", EditorStyles.boldLabel);

        sourceFolder = EditorGUILayout.TextField("원본 폴더", sourceFolder);
        targetFolder = EditorGUILayout.TextField("대상 폴더", targetFolder);
        matchByFilename = EditorGUILayout.Toggle("파일명으로 매칭", matchByFilename);

        GUILayout.Space(10);

        if (GUILayout.Button("Pivot 일괄 복사"))
        {
            CopyPivots();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "원본과 대상 sprite의 파일명이 같아야 합니다.\n" +
            "예: Knight/Head.png → Goblin/Head.png",
            MessageType.Info);
    }

    void CopyPivots()
    {
        string[] sourceGuids = AssetDatabase.FindAssets("t:Sprite", new[] { sourceFolder });
        int copied = 0;
        int skipped = 0;

        foreach (string sourceGuid in sourceGuids)
        {
            string sourcePath = AssetDatabase.GUIDToAssetPath(sourceGuid);
            string fileName = Path.GetFileNameWithoutExtension(sourcePath);

            TextureImporter sourceImporter = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
            if (sourceImporter == null) continue;

            // 대상 폴더에서 같은 이름의 sprite 찾기
            string[] targetGuids = AssetDatabase.FindAssets($"t:Sprite {fileName}", new[] { targetFolder });

            bool foundMatch = false;
            foreach (string targetGuid in targetGuids)
            {
                string targetPath = AssetDatabase.GUIDToAssetPath(targetGuid);
                string targetFileName = Path.GetFileNameWithoutExtension(targetPath);

                if (targetFileName != fileName) continue;

                TextureImporter targetImporter = AssetImporter.GetAtPath(targetPath) as TextureImporter;
                if (targetImporter == null) continue;

                // Pivot 복사
                TextureImporterSettings sourceSettings = new TextureImporterSettings();
                sourceImporter.ReadTextureSettings(sourceSettings);

                TextureImporterSettings targetSettings = new TextureImporterSettings();
                targetImporter.ReadTextureSettings(targetSettings);

                targetSettings.spriteAlignment = sourceSettings.spriteAlignment;
                targetSettings.spritePivot = sourceSettings.spritePivot;

                // PPU도 같이 복사 (선택)
                targetImporter.spritePixelsPerUnit = sourceImporter.spritePixelsPerUnit;

                targetImporter.SetTextureSettings(targetSettings);
                targetImporter.SaveAndReimport();

                Debug.Log($"복사: {fileName} → pivot ({sourceSettings.spritePivot.x:F3}, {sourceSettings.spritePivot.y:F3})");
                copied++;
                foundMatch = true;
                break;
            }

            if (!foundMatch)
            {
                Debug.LogWarning($"대상 없음: {fileName} (대상 폴더에 같은 이름의 sprite 없음)");
                skipped++;
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료",
            $"복사: {copied}개\n건너뜀: {skipped}개", "확인");
    }
}