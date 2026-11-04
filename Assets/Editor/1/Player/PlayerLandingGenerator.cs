using UnityEngine;
using UnityEditor;

/// <summary>
/// Jump_a.anim의 착지 구간(60fps 기준 f42~f60, 약 18프레임)을 참고해서
/// 훨씬 짧고 투박한(거칠게 뚝 끊기는) 착지 전용 클립을 새로 만든다.
///
/// 원본 착지 구간과 다른 점:
///   - 임팩트까지 진입 시간: 18프레임 → 3프레임(거의 즉시 스냅)
///   - 회복까지 포함 전체 길이: 18프레임 → 10프레임
///   - 부드러운 접선(Smooth) 대신 Linear 접선을 써서 뚝뚝 끊기는 투박한 느낌
///   - Body/Head 회전은 그대로 수평 유지(기울지 않음), 착지 충격은 다리/스쿼시로만 표현
///
/// 리그 구조는 Jump_1.anim / Jump_a.anim과 동일:
///   Body ├ Head, ShoulderJoint_F/B(→Shoulder→ArmJoint→Hand), LegJoint_F/B
/// </summary>
public static class PlayerLandingGenerator
{
    const string SAVE_PATH = "Assets/Animations/1/Player";
    const float FRAME_RATE = 60f; // Jump_a.anim과 동일한 프레임 기준(60fps)
    const int N = 11; // f0~f10

    // 타임라인 (60fps 기준 프레임 번호, Jump_a.anim에서 쓰던 것과 같은 단위)
    const int F_IMPACT = 3;   // 착지 임팩트(거의 즉시 스냅)
    const int F_HOLD_END = 5; // 임팩트 자세 짧게 유지
    const int F_END = 10;     // rest로 복귀

    const string BODY = "Body";
    const string HEAD = "Body/Head";
    const string SHOULDER_F = "Body/ShoulderJoint_F";
    const string SHOULDER_B = "Body/ShoulderJoint_B";
    const string LEG_F = "Body/LegJoint_F";
    const string LEG_B = "Body/LegJoint_B";
    const string HAND_F = "Body/ShoulderJoint_F/Shoulder_F/ArmJoint_F/Hand_F";
    const string HAND_B = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B/Hand_B";

    [MenuItem("Tools/1/Player/Generate Landing (Short & Blunt)")]
    public static void Generate()
    {
        var clip = new AnimationClip { name = "Landing" };
        clip.frameRate = FRAME_RATE;

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // ------------------------------------------------------------
        // 회전(Z) — Body/Head는 수평 유지, 다리/팔로만 충격 표현
        // ------------------------------------------------------------

        SetRotZ(clip, BODY,
            Key(0, 0), Key(F_IMPACT, -4), Key(F_HOLD_END, -4), Key(F_END, 0));

        SetRotZ(clip, HEAD,
            Key(0, 0), Key(F_IMPACT, 3), Key(F_HOLD_END, 3), Key(F_END, 0));

        SetRotZ(clip, SHOULDER_F,
            Key(0, 0), Key(F_IMPACT, 10), Key(F_HOLD_END, 10), Key(F_END, 0));

        SetRotZ(clip, SHOULDER_B,
            Key(0, 0), Key(F_IMPACT, -10), Key(F_HOLD_END, -10), Key(F_END, 0));

        SetRotZ(clip, LEG_F,
            Key(0, 0), Key(F_IMPACT, -40), Key(F_HOLD_END, -40), Key(F_END, 0));

        SetRotZ(clip, LEG_B,
            Key(0, 0), Key(F_IMPACT, -30), Key(F_HOLD_END, -30), Key(F_END, 0));

        SetRotZ(clip, HAND_F,
            Key(0, 0), Key(F_IMPACT, 26), Key(F_HOLD_END, 26), Key(F_END, 0));

        SetRotZ(clip, HAND_B,
            Key(0, 0), Key(F_IMPACT, -26), Key(F_HOLD_END, -26), Key(F_END, 0));

        // ------------------------------------------------------------
        // Body/Head 위치 — 임팩트에서 확 눌리는 스쿼시
        // ------------------------------------------------------------

        SetPosY(clip, BODY,
            Key(0, 0f), Key(F_IMPACT, -0.22f), Key(F_HOLD_END, -0.22f), Key(F_END, 0f));

        SetPosY(clip, HEAD,
            Key(0, 0.2875f), Key(F_IMPACT, 0.282f), Key(F_HOLD_END, 0.282f), Key(F_END, 0.2875f));

        // ------------------------------------------------------------
        // 저장
        // ------------------------------------------------------------

        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            System.IO.Directory.CreateDirectory(SAVE_PATH);
            AssetDatabase.Refresh();
        }

        string assetPath = $"{SAVE_PATH}/Landing.anim";
        AssetDatabase.CreateAsset(clip, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PlayerLandingGenerator] 생성 완료: {assetPath}");

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }

    // ---- 헬퍼 ----

    struct KeyPoint
    {
        public int frame;
        public float value;
    }

    static KeyPoint Key(int frame, float value) => new KeyPoint { frame = frame, value = value };

    static void SetRotZ(AnimationClip clip, string path, params KeyPoint[] points)
    {
        var curve = BuildLinearCurve(points);
        clip.SetCurve(path, typeof(Transform), "localEulerAngles.z", curve);
    }

    static void SetPosY(AnimationClip clip, string path, params KeyPoint[] points)
    {
        var curve = BuildLinearCurve(points);
        // Keep authored motion deltas while using the new head rest position.
        if (path == HEAD)
        {
            var keys = curve.keys;
            for (int i = 0; i < keys.Length; i++) keys[i].value += 0.042f;
            curve.keys = keys;
        }
        clip.SetCurve(path, typeof(Transform), "localPosition.y", curve);
    }

    // Linear 접선으로 만들어서 부드럽지 않고 뚝뚝 끊기는(투박한) 느낌을 낸다
    static AnimationCurve BuildLinearCurve(KeyPoint[] points)
    {
        var keyframes = new Keyframe[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            float time = points[i].frame / FRAME_RATE;
            keyframes[i] = new Keyframe(time, points[i].value);
        }

        var curve = new AnimationCurve(keyframes);

        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }

        return curve;
    }
}
