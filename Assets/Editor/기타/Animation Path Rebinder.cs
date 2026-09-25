using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class AnimationPathRebinder : EditorWindow
{

    [MenuItem("Tools/기타/Animation Path Rebinder")]
    static void OpenWindow()
    {
        GetWindow<AnimationPathRebinder>("Path Rebinder");
    }

    // 변환 규칙
    string oldPathF = "Body/ShoulderJoint_F/ArmJoint_F";
    string newPathF = "Body/ShoulderJoint_F/Shoulder_F/ArmJoint_F/Hand_F";

    string oldPathB = "Body/ShoulderJoint_B/ArmJoint_B";
    string newPathB = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B/Hand_B";

    bool onlyRotation = true;

    void OnGUI()
    {
        GUILayout.Label("Animation Path Rebinder", EditorStyles.boldLabel);
        GUILayout.Label("AnimationClip의 키 트랙 path를 변경합니다.", EditorStyles.miniLabel);
        GUILayout.Space(10);

        GUILayout.Label("F (Front)", EditorStyles.boldLabel);
        oldPathF = EditorGUILayout.TextField("기존 path", oldPathF);
        newPathF = EditorGUILayout.TextField("새 path", newPathF);

        GUILayout.Space(5);
        GUILayout.Label("B (Back)", EditorStyles.boldLabel);
        oldPathB = EditorGUILayout.TextField("기존 path", oldPathB);
        newPathB = EditorGUILayout.TextField("새 path", newPathB);

        GUILayout.Space(10);
        onlyRotation = EditorGUILayout.Toggle("회전(rot.z) 트랙만 옮김", onlyRotation);

        GUILayout.Space(10);

        if (GUILayout.Button("선택된 Animation Clip들 변환"))
        {
            RebindSelected();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "1. Project 창에서 변환할 .anim 파일들을 선택\n" +
            "2. 이 버튼 클릭\n\n" +
            "ArmJoint의 회전 키를 Hand로 옮깁니다.\n" +
            "Position 트랙도 옮기려면 위 체크박스 해제.",
            MessageType.Info);
    }

    void RebindSelected()
    {
        int clipCount = 0;
        int rebindCount = 0;
        List<string> log = new List<string>();

        foreach (var obj in Selection.objects)
        {
            if (obj is AnimationClip clip)
            {
                int result = RebindClip(clip, log);
                if (result > 0)
                {
                    clipCount++;
                    rebindCount += result;
                }
            }
        }

        AssetDatabase.SaveAssets();

        foreach (var line in log) Debug.Log(line);

        EditorUtility.DisplayDialog("완료",
            $"{clipCount}개 Clip, {rebindCount}개 트랙 변환됨\n자세한 내역은 Console 참고", "확인");
    }

    int RebindClip(AnimationClip clip, List<string> log)
    {
        int count = 0;
        var bindings = AnimationUtility.GetCurveBindings(clip);

        // 변환할 binding 수집 (먼저 모은 뒤 한꺼번에 처리)
        var toRebind = new List<(EditorCurveBinding oldBinding, string newPath, AnimationCurve curve)>();

        foreach (var binding in bindings)
        {
            if (onlyRotation && !IsRotationProperty(binding.propertyName))
            {
                continue;
            }

            string newPath = null;

            if (binding.path == oldPathF)
            {
                newPath = newPathF;
            }
            else if (binding.path == oldPathB)
            {
                newPath = newPathB;
            }

            if (newPath != null)
            {
                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                toRebind.Add((binding, newPath, curve));
            }
        }

        // 실제 변환 수행
        foreach (var (oldBinding, newPath, curve) in toRebind)
        {
            // 기존 binding 제거
            AnimationUtility.SetEditorCurve(clip, oldBinding, null);

            // 새 binding에 curve 추가
            var newBinding = new EditorCurveBinding
            {
                path = newPath,
                type = oldBinding.type,
                propertyName = oldBinding.propertyName
            };
            AnimationUtility.SetEditorCurve(clip, newBinding, curve);

            log.Add($"[{clip.name}] {oldBinding.path}/{oldBinding.propertyName} → {newPath}/{oldBinding.propertyName}");
            count++;
        }

        return count;
    }

    bool IsRotationProperty(string property)
    {
        return property.StartsWith("localEulerAngles") ||
               property.StartsWith("m_LocalRotation");
    }
}