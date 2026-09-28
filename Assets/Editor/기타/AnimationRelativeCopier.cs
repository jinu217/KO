using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class AnimationRelativeCopier : EditorWindow
{
    [MenuItem("Tools/기타/Animation Relative Copier")]
    static void OpenWindow()
    {
        GetWindow<AnimationRelativeCopier>("Animation Relative Copier");
    }

    AnimationClip sourceClip;
    GameObject sourceRoot;
    GameObject targetRoot;
    string outputPath = "Assets/Animations/Converted.anim";
    bool mirrorX = false;  // ← 1. 필드로 추가

    void OnGUI()
    {
        GUILayout.Label("Animation Δ 변환", EditorStyles.boldLabel);
        GUILayout.Space(5);

        sourceClip = (AnimationClip)EditorGUILayout.ObjectField("원본 Clip", sourceClip, typeof(AnimationClip), false);
        sourceRoot = (GameObject)EditorGUILayout.ObjectField("원본 캐릭터 (rest)", sourceRoot, typeof(GameObject), true);
        targetRoot = (GameObject)EditorGUILayout.ObjectField("대상 캐릭터 (rest)", targetRoot, typeof(GameObject), true);
        outputPath = EditorGUILayout.TextField("저장 경로", outputPath);
        mirrorX = EditorGUILayout.Toggle("Mirror X (좌우 반전)", mirrorX);  // ← 2. GUI에 토글 추가

        GUILayout.Space(10);

        if (GUILayout.Button("변환 + 저장"))
        {
            Convert();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "원본 Clip의 각 트랜스폼 키에서 원본 캐릭터의 rest pose를 빼서 Δ를 구하고,\n" +
            "그 Δ를 대상 캐릭터의 rest pose에 더한 새 Clip을 만듭니다.\n\n" +
            "Mirror X: 캐릭터의 보는 방향이 반대일 때 체크 (X 위치, Z 회전 Δ 부호 반전).",
            MessageType.Info);
    }

    void Convert()
    {
        if (sourceClip == null || sourceRoot == null || targetRoot == null)
        {
            EditorUtility.DisplayDialog("오류", "모든 필드를 채워주세요", "확인");
            return;
        }

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

            Transform sourceBone = sourceRoot.transform.Find(binding.path);
            Transform targetBone = targetRoot.transform.Find(binding.path);

            if (sourceBone == null || targetBone == null)
            {
                AnimationUtility.SetEditorCurve(newClip, binding, sourceCurve);
                copiedAsIs++;
                continue;
            }

            float sourceRest = GetRestValue(sourceBone, binding.propertyName);
            float targetRest = GetRestValue(targetBone, binding.propertyName);

            if (float.IsNaN(sourceRest) || float.IsNaN(targetRest))
            {
                AnimationUtility.SetEditorCurve(newClip, binding, sourceCurve);
                copiedAsIs++;
                continue;
            }

            var keys = sourceCurve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                float delta = keys[i].value - sourceRest;

                // ← 3. Mirror X 처리 추가
                if (mirrorX && binding.propertyName == "m_LocalPosition.x")
                {
                    delta = -delta;
                }
                if (mirrorX && binding.propertyName == "localEulerAnglesRaw.z")
                {
                    delta = -delta;
                }

                keys[i].value = targetRest + delta;
            }

            AnimationCurve newCurve = new AnimationCurve(keys);
            AnimationUtility.SetEditorCurve(newClip, binding, newCurve);
            converted++;
        }

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

            case "localEulerAnglesRaw.x": return bone.localEulerAngles.x;
            case "localEulerAnglesRaw.y": return bone.localEulerAngles.y;
            case "localEulerAnglesRaw.z": return bone.localEulerAngles.z;

            case "m_LocalScale.x":
            case "m_LocalScale.y":
            case "m_LocalScale.z":
                return float.NaN;
        }
        return float.NaN;
    }
}