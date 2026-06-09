using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class AnimationRelativeCopier : EditorWindow
{
    [MenuItem("Tools/Animation Relative Copier")]
    static void OpenWindow()
    {
        GetWindow<AnimationRelativeCopier>("Animation Relative Copier");
    }

    AnimationClip sourceClip;
    GameObject sourceRoot;
    GameObject targetRoot;
    string outputPath = "Assets/Animations/Converted.anim";

    void OnGUI()
    {
        GUILayout.Label("Animation Δ 변환", EditorStyles.boldLabel);
        GUILayout.Space(5);

        sourceClip = (AnimationClip)EditorGUILayout.ObjectField("원본 Clip", sourceClip, typeof(AnimationClip), false);
        sourceRoot = (GameObject)EditorGUILayout.ObjectField("원본 캐릭터 (rest)", sourceRoot, typeof(GameObject), true);
        targetRoot = (GameObject)EditorGUILayout.ObjectField("대상 캐릭터 (rest)", targetRoot, typeof(GameObject), true);
        outputPath = EditorGUILayout.TextField("저장 경로", outputPath);

        GUILayout.Space(10);

        if (GUILayout.Button("변환 + 저장"))
        {
            Convert();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "원본 Clip의 각 트랜스폼 키에서 원본 캐릭터의 rest pose를 빼서 Δ를 구하고,\n" +
            "그 Δ를 대상 캐릭터의 rest pose에 더한 새 Clip을 만듭니다.\n\n" +
            "Rotation은 각도가 절대적이므로 동일하게 Δ로 변환됩니다.",
            MessageType.Info);
    }

    void Convert()
    {
        if (sourceClip == null || sourceRoot == null || targetRoot == null)
        {
            EditorUtility.DisplayDialog("오류", "모든 필드를 채워주세요", "확인");
            return;
        }

        // 새 Clip 생성 (원본 복사)
        AnimationClip newClip = new AnimationClip();
        newClip.frameRate = sourceClip.frameRate;
        newClip.legacy = sourceClip.legacy;
        newClip.wrapMode = sourceClip.wrapMode;

        var bindings = AnimationUtility.GetCurveBindings(sourceClip);
        int converted = 0;
        int copiedAsIs = 0;

        foreach (var binding in bindings)
        {
            var sourceCurve = AnimationUtility.GetEditorCurve(sourceClip, binding);
            if (sourceCurve == null) continue;

            // 해당 트랜스폼 path로 원본/대상 캐릭터에서 본 찾기
            Transform sourceBone = sourceRoot.transform.Find(binding.path);
            Transform targetBone = targetRoot.transform.Find(binding.path);

            if (sourceBone == null || targetBone == null)
            {
                // 본이 없는 트랙은 그대로 복사
                AnimationUtility.SetEditorCurve(newClip, binding, sourceCurve);
                copiedAsIs++;
                continue;
            }

            // 채널 종류 판별
            float sourceRest = GetRestValue(sourceBone, binding.propertyName);
            float targetRest = GetRestValue(targetBone, binding.propertyName);

            if (float.IsNaN(sourceRest) || float.IsNaN(targetRest))
            {
                // 처리 못하는 채널은 그대로
                AnimationUtility.SetEditorCurve(newClip, binding, sourceCurve);
                copiedAsIs++;
                continue;
            }

            // 키값 변환
            var keys = sourceCurve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                float delta = keys[i].value - sourceRest;
                keys[i].value = targetRest + delta;
            }

            AnimationCurve newCurve = new AnimationCurve(keys);
            AnimationUtility.SetEditorCurve(newClip, binding, newCurve);
            converted++;
        }

        // 저장
        AssetDatabase.CreateAsset(newClip, outputPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("완료",
            $"변환된 채널: {converted}\n그대로 복사된 채널: {copiedAsIs}\n저장 위치: {outputPath}",
            "확인");
    }

    float GetRestValue(Transform bone, string property)
    {
        switch (property)
        {
            case "m_LocalPosition.x": return bone.localPosition.x;
            case "m_LocalPosition.y": return bone.localPosition.y;
            case "m_LocalPosition.z": return bone.localPosition.z;

            // Rotation은 Euler 각도로 비교 (애니메이션 보통 Euler 키)
            case "localEulerAnglesRaw.x": return bone.localEulerAngles.x;
            case "localEulerAnglesRaw.y": return bone.localEulerAngles.y;
            case "localEulerAnglesRaw.z": return bone.localEulerAngles.z;

            // Scale은 변환 안 함 (그대로 복사)
            case "m_LocalScale.x":
            case "m_LocalScale.y":
            case "m_LocalScale.z":
                return float.NaN;  // NaN 반환하면 그대로 복사됨
        }
        return float.NaN;
    }
}