using UnityEngine;
using UnityEditor;

/// <summary>
/// 하강점프(발판 통과) 전용 "떨어지는 동작만" 클립.
/// 기존 PlayerDownJumpLandingGenerator에서 착지(Impact/Recover/rest복귀) 부분을 전부 빼고
/// 드롭 + 낙하유지까지만 남겼다. 논루프 클립이라 마지막 프레임(낙하 유지 포즈)에서
/// 자동으로 멈춰있고, Animator 쪽에서 IsGrounded == true가 되는 순간 바로
/// Idle/Locomotion으로 트랜지션하면 된다(별도 착지 애니메이션 없이).
///
/// 리그 구조는 Jump_1.anim / Jump_a.anim과 동일:
///   Body ├ Head, ShoulderJoint_F/B(→Shoulder→ArmJoint→Hand), LegJoint_F/B
/// </summary>
public static class PlayerDownJumpFallGenerator
{
    const string SAVE_PATH = "Assets/Animations/1/Player";
    const float FRAME_RATE = 12f;
    const int N = 15; // f0~f14

    const int F_DROP = 5; // 발판 통과 순간, 다리를 위로 확 당김
    const int F_FALL = 14; // 낙하 유지 포즈(클립 끝, 여기서 자동으로 멈춰있음)

    const string BODY = "Body";
    const string HEAD = "Body/Head";
    const string SHOULDER_F = "Body/ShoulderJoint_F";
    const string SHOULDER_B = "Body/ShoulderJoint_B";
    const string LEG_F = "Body/LegJoint_F";
    const string LEG_B = "Body/LegJoint_B";
    const string HAND_F = "Body/ShoulderJoint_F/Shoulder_F/ArmJoint_F/Hand_F";
    const string HAND_B = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B/Hand_B";

    [MenuItem("Tools/1/Player/Generate DownJump Fall Only")]
    public static void Generate()
    {
        var clip = new AnimationClip { name = "DownJumpFall" };
        clip.frameRate = FRAME_RATE;

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false; // 마지막 프레임(낙하 유지 포즈)에서 자동으로 멈춤
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // ------------------------------------------------------------
        // 회전(Z) — Body/Head는 수평 유지(요청대로), 다리/팔로만 낙하 자세 표현
        // ------------------------------------------------------------

        SetRotZ(clip, BODY,
            Key(0, 0), Key(F_DROP, -8), Key(8, -1), Key(11, 0), Key(F_FALL, 0));

        SetRotZ(clip, HEAD,
            Key(0, 0), Key(F_DROP, 5), Key(8, 1), Key(11, 0), Key(F_FALL, 0));

        SetRotZ(clip, SHOULDER_F,
            Key(0, 0), Key(F_DROP, 10), Key(F_FALL, 6));

        SetRotZ(clip, SHOULDER_B,
            Key(0, 0), Key(F_DROP, -10), Key(F_FALL, -6));

        SetRotZ(clip, LEG_F,
            Key(0, 0), Key(F_DROP, 35), Key(F_FALL, 15));

        SetRotZ(clip, LEG_B,
            Key(0, 0), Key(F_DROP, -32), Key(F_FALL, -14));

        SetRotZ(clip, HAND_F,
            Key(0, 0), Key(F_DROP, 25), Key(F_FALL, 12));

        SetRotZ(clip, HAND_B,
            Key(0, 0), Key(F_DROP, -25), Key(F_FALL, -12));

        // ------------------------------------------------------------
        // Body/Head 위치 (코스메틱 — 드롭 순간만 살짝 다운, 이후 낙하 유지)
        // ------------------------------------------------------------

        SetPosY(clip, BODY,
            Key(0, 0f), Key(F_DROP, -0.08f), Key(F_FALL, 0.02f));

        SetPosY(clip, HEAD,
            Key(0, 0.2875f), Key(F_DROP, 0.287f), Key(F_FALL, 0.2865f));

        // ------------------------------------------------------------
        // 저장
        // ------------------------------------------------------------

        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            System.IO.Directory.CreateDirectory(SAVE_PATH);
            AssetDatabase.Refresh();
        }

        string assetPath = $"{SAVE_PATH}/DownJumpFall.anim";
        AssetDatabase.CreateAsset(clip, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PlayerDownJumpFallGenerator] 생성 완료: {assetPath}");

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
        // Keep authored motion deltas while using the new head rest position.
        if (path == HEAD)
        {
            var keys = curve.keys;
            for (int i = 0; i < keys.Length; i++) keys[i].value += 0.042f;
            curve.keys = keys;
        }
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
