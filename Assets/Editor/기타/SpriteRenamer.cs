using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class SpriteRenamer
{
    static readonly Dictionary<string, string> NameMap = new Dictionary<string, string>
    {
        { "앞어깨", "Shoulder_F" },
        { "앞전완", "Forearm_F" },
        { "앞손", "Hand_F" },
        { "앞허벅지", "Thigh_F" },
        { "앞종아리", "Calf_F" },
        { "머리", "Head" },
        { "몸", "Body" },
        { "몸통", "Body" },
        { "뒷어깨", "Shoulder_B" },
        { "뒷전완", "Forearm_B" },
        { "뒷손", "Hand_B" },
        { "뒷허벅지", "Thigh_B" },
        { "뒷종아리", "Calf_B" },
        { "고간", "Bottom" }
    };

    const string FOOT_KR = "발(앞뒤)";

    [MenuItem("Tools/Sprites/스프라이트 이름 영문 변환")]
    static void RenameSelectedFolders()
    {
        string[] guids = Selection.assetGUIDs;

        if (guids == null || guids.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "알림",
                "Project 창에서 변환할 폴더를 선택해주세요.",
                "확인"
            );
            return;
        }

        int folderCount = 0;
        int count = 0;
        int skip = 0;

        foreach (string guid in guids)
        {
            string folderPath = AssetDatabase.GUIDToAssetPath(guid);

            // 선택한 항목이 폴더가 아니면 무시
            if (string.IsNullOrEmpty(folderPath) ||
                !AssetDatabase.IsValidFolder(folderPath))
            {
                continue;
            }

            folderCount++;

            Debug.Log($"[Renamer] 선택된 폴더: {folderPath}");

            // 폴더 바로 안의 Texture2D 검색
            string[] spriteGuids =
                AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });

            foreach (string spriteGuid in spriteGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(spriteGuid);

                // 하위 폴더는 제외
                string parent =
                    Path.GetDirectoryName(path).Replace("\\", "/");

                if (parent != folderPath)
                    continue;

                string fileName =
                    Path.GetFileNameWithoutExtension(path);

                string ext =
                    Path.GetExtension(path);

                // PNG만 처리
                if (ext.ToLower() != ".png")
                    continue;

                // ====================================================
                // 발(앞뒤) → Foot_F / Foot_B
                // ====================================================
                if (fileName == FOOT_KR)
                {
                    string footF = $"{folderPath}/Foot_F{ext}";
                    string footB = $"{folderPath}/Foot_B{ext}";

                    if (AssetDatabase.LoadAssetAtPath<Object>(footF) == null)
                    {
                        if (AssetDatabase.CopyAsset(path, footF))
                        {
                            Debug.Log(
                                $"[Renamer] {folderPath} : {fileName} → Foot_F"
                            );
                            count++;
                        }
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[Renamer] 이미 존재: {footF}"
                        );
                        skip++;
                    }

                    if (AssetDatabase.LoadAssetAtPath<Object>(footB) == null)
                    {
                        if (AssetDatabase.CopyAsset(path, footB))
                        {
                            Debug.Log(
                                $"[Renamer] {folderPath} : {fileName} → Foot_B"
                            );
                            count++;
                        }
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"[Renamer] 이미 존재: {footB}"
                        );
                        skip++;
                    }

                    AssetDatabase.DeleteAsset(path);
                    continue;
                }

                // ====================================================
                // 일반 이름 변환
                // ====================================================
                if (!NameMap.TryGetValue(fileName, out string newName))
                {
                    Debug.LogWarning(
                        $"[Renamer] 이름 매칭 실패: {folderPath}/{fileName}{ext}"
                    );
                    skip++;
                    continue;
                }

                string newPath =
                    $"{folderPath}/{newName}{ext}";

                // 이미 존재하면 스킵
                if (AssetDatabase.LoadAssetAtPath<Object>(newPath) != null)
                {
                    Debug.LogWarning(
                        $"[Renamer] 이미 존재: {newPath}"
                    );
                    skip++;
                    continue;
                }

                string error =
                    AssetDatabase.RenameAsset(path, newName);

                if (string.IsNullOrEmpty(error))
                {
                    Debug.Log(
                        $"[Renamer] {folderPath} : {fileName} → {newName}"
                    );
                    count++;
                }
                else
                {
                    Debug.LogError(
                        $"[Renamer] 변경 실패: {path} ({error})"
                    );
                    skip++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[Renamer] 완료 - 폴더 {folderCount}개 / 변환 {count}개 / 스킵 {skip}개"
        );

        EditorUtility.DisplayDialog(
            "완료",
            $"선택 폴더: {folderCount}개\n" +
            $"변환/생성: {count}개\n" +
            $"스킵: {skip}개",
            "확인"
        );
    }
}