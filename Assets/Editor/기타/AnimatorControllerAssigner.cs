using UnityEditor;
using UnityEngine;

public class AnimatorControllerAssigner
{
    [MenuItem("Tools/Assign Animator Controllers to Prefabs")]
    public static void Assign()
    {
        int success = 0, fail = 0;

        // Assets/Animations 안의 모든 .controller 파일 탐색
        string[] guids = AssetDatabase.FindAssets("t:AnimatorController", new[] { "Assets/Animations" });

        foreach (string guid in guids)
        {
            string acPath = AssetDatabase.GUIDToAssetPath(guid);
            string acName = System.IO.Path.GetFileNameWithoutExtension(acPath); // e.g. Smith_AC

            // _AC 접미사 제거해서 캐릭터 이름 추출
            if (!acName.EndsWith("_AC")) continue;
            string charName = acName.Substring(0, acName.Length - 3); // e.g. Smith

            // 프리팹 경로
            string prefabPath = $"Assets/Prefab/{charName}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Assigner] 프리팹 없음: {prefabPath}");
                fail++;
                continue;
            }

            // Animator 컴포넌트 찾기, 없으면 루트에 추가
            var animator = prefab.GetComponent<Animator>();
            if (animator == null)
            {
                animator = prefab.AddComponent<Animator>();
                Debug.Log($"[Assigner] Animator 추가: {prefabPath}");
            }

            // AC 로드 및 할당
            var ac = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(acPath);
            animator.runtimeAnimatorController = ac;
            EditorUtility.SetDirty(prefab);

            Debug.Log($"[Assigner] {charName}: {acPath} → {prefabPath}");
            success++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료", $"성공: {success}개\n실패: {fail}개", "확인");
    }
}