using UnityEngine;
using UnityEditor;

public class NPC1_RemoveHeadPosCurves
{
    // 디버그용: 찾은 클립 전부 로그 출력
    [MenuItem("Tools/NPC1/Debug List Clips")]
    public static void ListClips()
    {
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip");
        Debug.Log($"전체 AnimationClip 수: {guids.Length}");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) continue;
            Debug.Log($"클립: {clip.name} | 경로: {path}");
        }
    }

    [MenuItem("Tools/NPC1/Remove Head Position Curves")]
    public static void RemoveAll()
    {
        int found = 0;
        int count = 0;

        string[] npcs = { "Smith_", "Buffer_", "Merchant_", "Villager1_", "Villager2_" };
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) continue;

            string clipName = clip.name;

            // NPC 필터
            bool isNPC = false;
            foreach (var npc in npcs)
                if (clipName.StartsWith(npc)) { isNPC = true; break; }
            if (!isNPC) continue;

            // Idle 필터
            if (!clipName.Contains("Idle")) continue;

            found++;
            Debug.Log($"[RemoveHeadPos] 발견: {clipName}");

            bool modified = false;
            var bindings = AnimationUtility.GetCurveBindings(clip);
            foreach (var binding in bindings)
            {
                if (binding.path == "Body/Head" &&
                    (binding.propertyName == "m_LocalPosition.x" ||
                     binding.propertyName == "m_LocalPosition.y" ||
                     binding.propertyName == "m_LocalPosition.z"))
                {
                    AnimationUtility.SetEditorCurve(clip, binding, null);
                    modified = true;
                    Debug.Log($"  → {binding.propertyName} 제거");
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(clip);
                count++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료",
            $"발견: {found}개\n수정: {count}개", "확인");
    }
}