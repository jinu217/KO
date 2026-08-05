using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class MidBossFootStabilizer
{

    const string PATH_BODY = "Body";
    const string PATH_LOWERBODY = "Body/LowerBody";
    const string PATH_THIGH_F = "Body/LowerBody/ThighJoint_F";
    const string PATH_THIGH_B = "Body/LowerBody/ThighJoint_B";
    const string PATH_KNEE_F = "Body/LowerBody/ThighJoint_F/Thigh_F/KneeJoint_F";
    const string PATH_KNEE_B = "Body/LowerBody/ThighJoint_B/Thigh_B/KneeJoint_B";
    const string PATH_FOOT_F = "Body/LowerBody/ThighJoint_F/Thigh_F/KneeJoint_F/Foot_F";
    const string PATH_FOOT_B = "Body/LowerBody/ThighJoint_B/Thigh_B/KneeJoint_B/Foot_B";

    static readonly Vector2 LOWER_REST = new Vector2(0.05f, -0.6f);

    [MenuItem("Tools/Stabilize MidBoss Feet (Selected Clips)")]
    static void Stabilize()
    {
        var clips = Selection.objects.OfType<AnimationClip>().ToList();
        if (clips.Count == 0)
        {
            EditorUtility.DisplayDialog("오류", "AnimationClip(.anim)을 선택해주세요", "확인");
            return;
        }

        int processed = 0;
        foreach (var clip in clips)
        {
            if (ProcessClip(clip)) processed++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("완료",
            $"{processed}개 클립 발 안정화 완료\n" +
            $"기존 발 관련 키는 모두 제거하고 새로 박았습니다", "확인");
    }

    static bool ProcessClip(AnimationClip clip)
    {
        // ========== 0단계: 기존 발 관련 키 모두 제거 ==========
        // 이전 보정 도구들이 박았던 흔적 모두 청소
        clip.SetCurve(PATH_THIGH_F, typeof(Transform), "m_LocalPosition.x", null);
        clip.SetCurve(PATH_THIGH_F, typeof(Transform), "m_LocalPosition.y", null);
        clip.SetCurve(PATH_THIGH_B, typeof(Transform), "m_LocalPosition.x", null);
        clip.SetCurve(PATH_THIGH_B, typeof(Transform), "m_LocalPosition.y", null);
        clip.SetCurve(PATH_LOWERBODY, typeof(Transform), "m_LocalPosition.x", null);
        clip.SetCurve(PATH_LOWERBODY, typeof(Transform), "m_LocalPosition.y", null);
        clip.SetCurve(PATH_LOWERBODY, typeof(Transform), "localEulerAnglesRaw.z", null);
        clip.SetCurve(PATH_FOOT_F, typeof(Transform), "localEulerAnglesRaw.z", null);
        clip.SetCurve(PATH_FOOT_B, typeof(Transform), "localEulerAnglesRaw.z", null);

        // ========== 1단계: 키 시점 수집 ==========
        var bodyRotCurve = GetRotCurve(clip, PATH_BODY);
        var bodyPosXCurve = GetPosCurve(clip, PATH_BODY, "x");
        var bodyPosYCurve = GetPosCurve(clip, PATH_BODY, "y");
        var thighFCurve = GetRotCurve(clip, PATH_THIGH_F);
        var thighBCurve = GetRotCurve(clip, PATH_THIGH_B);
        var kneeFCurve = GetRotCurve(clip, PATH_KNEE_F);
        var kneeBCurve = GetRotCurve(clip, PATH_KNEE_B);

        var times = new HashSet<float>();
        AddKeyTimes(times, bodyRotCurve);
        AddKeyTimes(times, bodyPosXCurve);
        AddKeyTimes(times, bodyPosYCurve);
        AddKeyTimes(times, thighFCurve);
        AddKeyTimes(times, thighBCurve);
        AddKeyTimes(times, kneeFCurve);
        AddKeyTimes(times, kneeBCurve);

        if (times.Count == 0)
        {
            Debug.LogWarning($"[{clip.name}] 관련 키 없음");
            return false;
        }

        var sortedTimes = times.OrderBy(t => t).ToList();

        // ========== 2단계: 보정 키 계산 ==========
        var lowerRotZ = new AnimationCurve();
        var lowerPosX = new AnimationCurve();
        var lowerPosY = new AnimationCurve();
        var footFRotZ = new AnimationCurve();
        var footBRotZ = new AnimationCurve();

        foreach (var t in sortedTimes)
        {
            float bodyR = Eval(bodyRotCurve, t);
            float bodyX = Eval(bodyPosXCurve, t);
            float bodyY = Eval(bodyPosYCurve, t);
            float thighF = Eval(thighFCurve, t);
            float thighB = Eval(thighBCurve, t);
            float kneeF = Eval(kneeFCurve, t);
            float kneeB = Eval(kneeBCurve, t);

            // === LowerBody.rot.z = -Body.rot.z (회전 완전 상쇄) ===
            lowerRotZ.AddKey(t, -bodyR);

            // === LowerBody.localPosition: Body 변동 완전 상쇄 ===
            // LowerBody.world = Body.world + R(Body.rot) * LowerBody.local
            // LowerBody.world = LOWER_REST (rest 유지) 조건:
            // LowerBody.local = R(-Body.rot) * (LOWER_REST - Body.pos)
            float bodyRad = bodyR * Mathf.Deg2Rad;
            float cos = Mathf.Cos(bodyRad);
            float sin = Mathf.Sin(bodyRad);

            float dx = LOWER_REST.x - bodyX;
            float dy = LOWER_REST.y - bodyY;

            // R(-bodyR) 적용:
            // R(-θ)*(x,y) = (x*cos(θ) + y*sin(θ), -x*sin(θ) + y*cos(θ))
            lowerPosX.AddKey(t, dx * cos + dy * sin);
            lowerPosY.AddKey(t, -dx * sin + dy * cos);

            // === Foot 회전 상쇄 ===
            // 이제 Body.rot + LowerBody.rot = 0이므로
            // Foot 절대 회전 = ThighJoint.rot + KneeJoint.rot + Foot.rot
            // Foot.rot.z = -(ThighJoint + KneeJoint)
            footFRotZ.AddKey(t, -(thighF + kneeF));
            footBRotZ.AddKey(t, -(thighB + kneeB));
        }

        SetAutoTangents(lowerRotZ);
        SetAutoTangents(lowerPosX);
        SetAutoTangents(lowerPosY);
        SetAutoTangents(footFRotZ);
        SetAutoTangents(footBRotZ);

        // ========== 3단계: 키 박기 ==========
        clip.SetCurve(PATH_LOWERBODY, typeof(Transform), "localEulerAnglesRaw.z", lowerRotZ);
        clip.SetCurve(PATH_LOWERBODY, typeof(Transform), "m_LocalPosition.x", lowerPosX);
        clip.SetCurve(PATH_LOWERBODY, typeof(Transform), "m_LocalPosition.y", lowerPosY);
        clip.SetCurve(PATH_FOOT_F, typeof(Transform), "localEulerAnglesRaw.z", footFRotZ);
        clip.SetCurve(PATH_FOOT_B, typeof(Transform), "localEulerAnglesRaw.z", footBRotZ);

        EditorUtility.SetDirty(clip);
        Debug.Log($"[{clip.name}] 발 안정화 완료: {sortedTimes.Count}개 키");
        return true;
    }

    static void AddKeyTimes(HashSet<float> set, AnimationCurve curve)
    {
        if (curve == null) return;
        foreach (var k in curve.keys) set.Add(k.time);
    }

    static AnimationCurve GetRotCurve(AnimationClip clip, string path)
    {
        var binding = new EditorCurveBinding
        {
            path = path,
            type = typeof(Transform),
            propertyName = "localEulerAnglesRaw.z"
        };
        return AnimationUtility.GetEditorCurve(clip, binding);
    }

    static AnimationCurve GetPosCurve(AnimationClip clip, string path, string axis)
    {
        var binding = new EditorCurveBinding
        {
            path = path,
            type = typeof(Transform),
            propertyName = "m_LocalPosition." + axis
        };
        return AnimationUtility.GetEditorCurve(clip, binding);
    }

    static float Eval(AnimationCurve curve, float time)
    {
        if (curve == null) return 0f;
        return curve.Evaluate(time);
    }

    static void SetAutoTangents(AnimationCurve curve)
    {
        for (int i = 0; i < curve.keys.Length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
        }
    }

    [MenuItem("Tools/Remove MidBoss Foot Stabilization (Selected Clips)")]
    static void Remove()
    {
        var clips = Selection.objects.OfType<AnimationClip>().ToList();
        if (clips.Count == 0)
        {
            EditorUtility.DisplayDialog("오류", "AnimationClip을 선택해주세요", "확인");
            return;
        }

        foreach (var clip in clips)
        {
            clip.SetCurve(PATH_THIGH_F, typeof(Transform), "m_LocalPosition.x", null);
            clip.SetCurve(PATH_THIGH_F, typeof(Transform), "m_LocalPosition.y", null);
            clip.SetCurve(PATH_THIGH_B, typeof(Transform), "m_LocalPosition.x", null);
            clip.SetCurve(PATH_THIGH_B, typeof(Transform), "m_LocalPosition.y", null);
            clip.SetCurve(PATH_LOWERBODY, typeof(Transform), "m_LocalPosition.x", null);
            clip.SetCurve(PATH_LOWERBODY, typeof(Transform), "m_LocalPosition.y", null);
            clip.SetCurve(PATH_LOWERBODY, typeof(Transform), "localEulerAnglesRaw.z", null);
            clip.SetCurve(PATH_FOOT_F, typeof(Transform), "localEulerAnglesRaw.z", null);
            clip.SetCurve(PATH_FOOT_B, typeof(Transform), "localEulerAnglesRaw.z", null);
            EditorUtility.SetDirty(clip);
        }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("완료", $"{clips.Count}개 클립의 발 보정 키 모두 제거", "확인");
    }
}