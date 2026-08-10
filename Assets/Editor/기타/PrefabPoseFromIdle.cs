using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class PrefabPoseFromIdle
{
    [MenuItem("Tools/Set Prefab Pose From Idle Clip")]
    public static void SetPose()
    {
        int success = 0, fail = 0;

        // Assets/Animations 안의 모든 AnimationClip 탐색
        string[] guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Animations" });

        foreach (string guid in guids)
        {
            string clipPath = AssetDatabase.GUIDToAssetPath(guid);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null) continue;

            // _Idle 클립만 처리
            string clipName = clip.name;
            if (!clipName.EndsWith("_Idle")) continue;

            // 캐릭터 이름 추출 (e.g. Smith_Idle → Smith)
            string charName = clipName.Substring(0, clipName.Length - 5);

            // 프리팹 로드
            string prefabPath = $"Assets/Prefab/{charName}.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[PoseFromIdle] 프리팹 없음: {prefabPath}");
                fail++;
                continue;
            }

            // 클립의 f0 키프레임 값 읽기
            var bindings = AnimationUtility.GetCurveBindings(clip);
            var poseData = new Dictionary<string, Dictionary<string, float>>();

            foreach (var binding in bindings)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                if (curve == null || curve.keys.Length == 0) continue;

                float value = curve.Evaluate(0f);  // f0 값

                if (!poseData.ContainsKey(binding.path))
                    poseData[binding.path] = new Dictionary<string, float>();
                poseData[binding.path][binding.propertyName] = value;
            }

            // 프리팹에 적용
            bool modified = false;
            foreach (var kvp in poseData)
            {
                string objPath = kvp.Key;
                var props = kvp.Value;

                // 오브젝트 찾기
                Transform target = string.IsNullOrEmpty(objPath)
                    ? prefab.transform
                    : prefab.transform.Find(objPath);

                if (target == null)
                {
                    Debug.LogWarning($"[PoseFromIdle] {charName}: 경로 없음 '{objPath}'");
                    continue;
                }

                Vector3 pos = target.localPosition;
                Vector3 rot = target.localEulerAngles;

                if (props.ContainsKey("m_LocalPosition.x")) pos.x = props["m_LocalPosition.x"];
                if (props.ContainsKey("m_LocalPosition.y")) pos.y = props["m_LocalPosition.y"];
                if (props.ContainsKey("m_LocalPosition.z")) pos.z = props["m_LocalPosition.z"];
                if (props.ContainsKey("m_LocalRotation.x") || props.ContainsKey("localEulerAnglesRaw.z"))
                {
                    // quaternion 방식
                }
                // EulerAngles 방식
                if (props.ContainsKey("localEulerAnglesRaw.x")) rot.x = props["localEulerAnglesRaw.x"];
                if (props.ContainsKey("localEulerAnglesRaw.y")) rot.y = props["localEulerAnglesRaw.y"];
                if (props.ContainsKey("localEulerAnglesRaw.z")) rot.z = props["localEulerAnglesRaw.z"];

                target.localPosition = pos;
                target.localEulerAngles = rot;
                modified = true;
            }

            if (modified)
            {
                EditorUtility.SetDirty(prefab);
                Debug.Log($"[PoseFromIdle] {charName} 적용 완료");
                success++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료", $"성공: {success}개\n실패: {fail}개", "확인");
    }
}
