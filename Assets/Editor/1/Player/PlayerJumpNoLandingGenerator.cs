using UnityEngine;
using UnityEditor;

/// <summary>
/// Jump_a.anim에서 착지 구간(60fps 기준 f42 이후 임팩트/회복)을 잘라내고
/// 점프(상승)+낙하까지만 남긴 클립. 원본 Jump_a의 실제 커브값을 그대로 옮겼다.
/// 논루프라서 마지막 프레임(낙하 유지 포즈, f42)에서 자동으로 멈춰있고,
/// Animator 쪽에서 IsGrounded == true가 되는 순간 별도 착지 애니메이션 없이
/// 바로 Idle/Locomotion으로 트랜지션하면 된다.
///
/// 리그 구조: Body ├ Head, ShoulderJoint_F/B(→Shoulder→ArmJoint→Hand), LegJoint_F/B
/// 프레임 기준은 원본 Jump_a.anim과 동일하게 60fps 유지(f0~f42).
/// </summary>
public static class PlayerJumpNoLandingGenerator
{
    const string SAVE_PATH = "Assets/Animations/1/Player";
    const float FRAME_RATE = 60f; // Jump_a.anim과 동일 기준
    const int F_END = 42;         // 착지 직전(낙하 유지) 프레임에서 클립 종료

    const string BODY = "Body";
    const string HEAD = "Body/Head";
    const string SHOULDER_F = "Body/ShoulderJoint_F";
    const string SHOULDER_B = "Body/ShoulderJoint_B";
    const string LEG_F = "Body/LegJoint_F";
    const string LEG_B = "Body/LegJoint_B";
    const string HAND_F = "Body/ShoulderJoint_F/Shoulder_F/ArmJoint_F/Hand_F";
    const string HAND_B = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B/Hand_B";

    [MenuItem("Tools/Player/Generate Jump (No Landing)")]
    public static void Generate()
    {
        var clip = new AnimationClip { name = "Jump" };
        clip.frameRate = FRAME_RATE;

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // ------------------------------------------------------------
        // 회전(Z) — Jump_a.anim 원본 값 그대로, f42(착지 직전) 이후는 잘라냄
        // ------------------------------------------------------------

        SetRotZ(clip, BODY,
            Key(0, 0), Key(5, -2), Key(12, 3), Key(22, 2), Key(30, 0), Key(38, -1));

        SetRotZ(clip, HEAD,
            Key(0, 0), Key(5, -2), Key(12, -5), Key(22, 4), Key(30, 2));

        SetRotZ(clip, SHOULDER_F,
            Key(0, 0), Key(5, 4), Key(12, -8), Key(22, -6), Key(30, -3));

        SetRotZ(clip, SHOULDER_B,
            Key(0, 0), Key(5, -4), Key(12, 8), Key(22, 6), Key(30, 3));

        SetRotZ(clip, LEG_F,
            Key(0, 0), Key(5, -15), Key(12, 20), Key(22, -50), Key(30, -55), Key(38, -30), Key(F_END, -5));

        SetRotZ(clip, LEG_B,
            Key(0, 0), Key(6, -10), Key(13, 25), Key(24, 40), Key(30, 45), Key(38, 25), Key(F_END, 5));

        SetRotZ(clip, HAND_F,
            Key(0, 0), Key(5, 15), Key(12, -20), Key(22, -15), Key(30, -10));

        SetRotZ(clip, HAND_B,
            Key(0, 0), Key(5, -15), Key(12, 20), Key(22, 15), Key(30, 10));

        // ------------------------------------------------------------
        // Body/Head 위치 (코스메틱 스쿼시&스트레치, 원본 값 그대로)
        // ------------------------------------------------------------

        SetPosY(clip, BODY,
            Key(0, 0f), Key(5, -0.05f), Key(12, 0.075f), Key(22, 0.19f), Key(30, 0.2f), Key(38, 0.1f));

        SetPosY(clip, HEAD,
            Key(0, 0.2875f), Key(5, 0.287f), Key(12, 0.2865f), Key(22, 0.2885f), Key(30, 0.289f));

        // ------------------------------------------------------------
        // 저장
        // ------------------------------------------------------------

        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            System.IO.Directory.CreateDirectory(SAVE_PATH);
            AssetDatabase.Refresh();
        }

        string assetPath = $"{SAVE_PATH}/Jump.anim";
        AssetDatabase.CreateAsset(clip, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PlayerJumpNoLandingGenerator] 생성 완료: {assetPath}");

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
        var curve = BuildCurve(points);
        clip.SetCurve(path, typeof(Transform), "localEulerAngles.z", curve);
    }

    static void SetPosY(AnimationClip clip, string path, params KeyPoint[] points)
    {
        var curve = BuildCurve(points);
        clip.SetCurve(path, typeof(Transform), "localPosition.y", curve);
    }

    static AnimationCurve BuildCurve(KeyPoint[] points)
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
            curve.SmoothTangents(i, 0f);
        }

        return curve;
    }
}
