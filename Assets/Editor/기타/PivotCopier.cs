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

    string sourceFolder = "Assets/Mob/";
    string targetFolder = "Assets/Mob/";
    bool matchByFilename = true;

    void OnGUI()
    {
        GUILayout.Label("원본 → 대상으로 Pivot 복사", EditorStyles.boldLabel);
        GUILayout.Space(5);

        // 원본 폴더
        GUILayout.Label("원본 폴더");
        DrawFolderField(ref sourceFolder, "source");

        GUILayout.Space(5);

        // 대상 폴더
        GUILayout.Label("대상 폴더");
        DrawFolderField(ref targetFolder, "target");

        GUILayout.Space(5);
        matchByFilename = EditorGUILayout.Toggle("파일명으로 매칭", matchByFilename);
        GUILayout.Space(10);

        if (GUILayout.Button("Pivot 일괄 복사", GUILayout.Height(30)))
            CopyPivots();

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "원본과 대상 sprite의 파일명이 같아야 합니다.\n" +
            "예: Knight/Head.png → Goblin/Head.png\n" +
            "폴더를 텍스트 필드 또는 드롭 존에 드래그앤드롭하세요.",
            MessageType.Info);
    }

    void DrawFolderField(ref string folderPath, string id)
    {
        var rect = EditorGUILayout.GetControlRect();
        folderPath = EditorGUI.TextField(rect, folderPath);

        // 드롭 존 (텍스트필드 바로 아래)
        var dropRect = EditorGUILayout.GetControlRect(GUILayout.Height(24));
        GUI.Box(dropRect, "← 폴더 드래그앤드롭", EditorStyles.helpBox);

        var evt = Event.current;
        if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform)
            && (rect.Contains(evt.mousePosition) || dropRect.Contains(evt.mousePosition)))
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var path in DragAndDrop.paths)
                {
                    if (AssetDatabase.IsValidFolder(path))
                    {
                        folderPath = path;
                        GUI.changed = true;
                        break;
                    }
                }
            }
            evt.Use();
        }
    }

    void CopyPivots()
    {
        string[] sourceGuids = AssetDatabase.FindAssets("t:Sprite", new[] { sourceFolder });
        int copied = 0, skipped = 0;

        foreach (string sourceGuid in sourceGuids)
        {
            string sourcePath = AssetDatabase.GUIDToAssetPath(sourceGuid);
            string fileName = Path.GetFileNameWithoutExtension(sourcePath);
            TextureImporter sourceImporter = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
            if (sourceImporter == null) continue;

            string[] targetGuids = AssetDatabase.FindAssets($"t:Sprite {fileName}", new[] { targetFolder });
            bool foundMatch = false;

            foreach (string targetGuid in targetGuids)
            {
                string targetPath = AssetDatabase.GUIDToAssetPath(targetGuid);
                if (Path.GetFileNameWithoutExtension(targetPath) != fileName) continue;

                TextureImporter targetImporter = AssetImporter.GetAtPath(targetPath) as TextureImporter;
                if (targetImporter == null) continue;

                TextureImporterSettings sourceSettings = new TextureImporterSettings();
                sourceImporter.ReadTextureSettings(sourceSettings);
                TextureImporterSettings targetSettings = new TextureImporterSettings();
                targetImporter.ReadTextureSettings(targetSettings);

                targetSettings.spriteAlignment = sourceSettings.spriteAlignment;
                targetSettings.spritePivot = sourceSettings.spritePivot;
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
                Debug.LogWarning($"대상 없음: {fileName}");
                skipped++;
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료", $"복사: {copied}개\n건너뜀: {skipped}개", "확인");
    }
}