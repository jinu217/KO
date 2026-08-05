using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class NPC1_SpriteRenamer
{
    static readonly Dictionary<string, string> NameMap = new Dictionary<string, string>
    {
        { "앞어깨",    "Shoulder_F" },
        { "앞전완",    "Hand_F"     },
        { "머리",      "Head"       },
        { "몸",        "Body"       },
        { "뒷어깨",    "Shoulder_B" },
        { "뒷전완",    "Hand_B"     },
    };

    // 발(앞뒤) → Foot_F, Foot_B 두 개로 복사
    const string FOOT_KR = "발(앞뒤)";

    [MenuItem("Tools/NPC1/스프라이트 이름 영문 변환")]
    static void RenameAll()
    {
        int count = 0;
        int skip = 0;

        string[] guids = AssetDatabase.FindAssets("t:Sprite");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            string dir = Path.GetDirectoryName(path).Replace("\\", "/");

            // 발(앞뒤): 복사해서 Foot_F / Foot_B 두 개 생성
            if (fileName == FOOT_KR)
            {
                string footF = $"{dir}/Foot_F{ext}";
                string footB = $"{dir}/Foot_B{ext}";

                // Foot_F: 원본을 복사
                if (AssetDatabase.LoadAssetAtPath<Object>(footF) == null)
                {
                    AssetDatabase.CopyAsset(path, footF);
                    Debug.Log($"[Renamer] {fileName}{ext} → Foot_F{ext} (복사)");
                    count++;
                }
                else { skip++; }

                // Foot_B: 복사
                if (AssetDatabase.LoadAssetAtPath<Object>(footB) == null)
                {
                    AssetDatabase.CopyAsset(path, footB);
                    Debug.Log($"[Renamer] {fileName}{ext} → Foot_B{ext} (복사)");
                    count++;
                }
                else { skip++; }

                // 원본 삭제
                AssetDatabase.DeleteAsset(path);
                Debug.Log($"[Renamer] 원본 삭제: {path}");
                continue;
            }

            // 일반 이름 변환
            if (NameMap.TryGetValue(fileName, out string newName))
            {
                string newPath = $"{dir}/{newName}{ext}";
                if (AssetDatabase.LoadAssetAtPath<Object>(newPath) != null)
                {
                    Debug.LogWarning($"[Renamer] 이미 존재: {newPath} (스킵)");
                    skip++;
                    continue;
                }

                string error = AssetDatabase.RenameAsset(path, newName + ext);
                if (string.IsNullOrEmpty(error))
                {
                    Debug.Log($"[Renamer] {fileName}{ext} → {newName}{ext}");
                    count++;
                }
                else
                {
                    Debug.LogError($"[Renamer] 실패: {path} ({error})");
                }
            }
        }

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료",
            $"변환/생성 완료: {count}개\n스킵: {skip}개", "확인");
    }
}