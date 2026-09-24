using UnityEngine;
using UnityEditor;

/// <summary>
/// Jump_1.anim의 곡선 스타일(진폭/속도감)을 참고해서 새로 설계한
/// "하단점프(발판 통과) + 착지" 애니메이션 생성기.
///
/// 리그 구조 (Jump_1.anim에서 확인된 것과 동일):
///   Body
///   ├── Head
///   ├── ShoulderJoint_F → Shoulder_F → ArmJoint_F → Hand_F
///   ├── ShoulderJoint_B → Shoulder_B → ArmJoint_B → Hand_B
///   ├── LegJoint_F
///   └── LegJoint_B
///
/// rest 기준: 전부 회전 0, Body 위치 (0,0,0), Head 위치 (0, 0.2875, 0)
/// (Jump_1.anim의 f0 값과 동일하게 맞춤)
///
/// 타임라인(12fps, 30프레임, 논루프):
///   f0  rest
///   f5  드롭(발판 통과 순간, 다리를 위로 확 당김)
///   f8  몸통/머리 기울기 정리 완료(이후 수평 유지)
///   f14 낙하 유지(팔다리는 계속 크게 움직이지만 몸통은 수평)
///   f19 착지 임팩트(다리 깊게 굽혀 충격 흡수, 몸 확 눌림 — 몸통 자체는 안 기울어짐)
///   f25 회복(서서히 중립 자세로)
///   f29 rest 복귀
/// </summary>
public static class PlayerDownJumpLandingGenerator
{
    const string SAVE_PATH = "Assets/Animations/1/Player";
    const float FRAME_RATE = 12f;
    const int N = 30;

    // 타임라인 프레임 지점
    const int F_DROP = 5;
    const int F_FALL = 14;
    const int F_IMPACT = 19;
    const int F_RECOVER = 25;
    const int F_END = 29;

    // 본 경로
    const string BODY = "Body";
    const string HEAD = "Body/Head";
    const string SHOULDER_F = "Body/ShoulderJoint_F";
    const string SHOULDER_B = "Body/ShoulderJoint_B";
    const string LEG_F = "Body/LegJoint_F";
    const string LEG_B = "Body/LegJoint_B";
    const string HAND_F = "Body/ShoulderJoint_F/Shoulder_F/ArmJoint_F/Hand_F";
    const string HAND_B = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B/Hand_B";

    [MenuItem("Tools/Player/Generate DownJump+Landing")]
    public static void Generate()
    {
        var clip = new AnimationClip { name = "DownJumpLanding" };
        clip.frameRate = FRAME_RATE;

        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // ------------------------------------------------------------
        // 회전(Z만 사용, 2D 픽셀 리그)
        // ------------------------------------------------------------

        SetRotZ(clip, BODY,
            Key(0, 0), Key(F_DROP, -8), Key(8, -1), Key(11, 0), Key(F_END, 0));

        SetRotZ(clip, HEAD,
            Key(0, 0), Key(F_DROP, 5), Key(8, 1), Key(11, 0), Key(F_END, 0));

        SetRotZ(clip, SHOULDER_F,
            Key(0, 0), Key(F_DROP, 10), Key(F_FALL, 6), Key(F_IMPACT, 14),
            Key(F_RECOVER, 4), Key(F_END, 0));

        SetRotZ(clip, SHOULDER_B,
            Key(0, 0), Key(F_DROP, -10), Key(F_FALL, -6), Key(F_IMPACT, -14),
            Key(F_RECOVER, -4), Key(F_END, 0));

        SetRotZ(clip, LEG_F,
            Key(0, 0), Key(F_DROP, 35), Key(F_FALL, 15), Key(F_IMPACT, -30),
            Key(F_RECOVER, -8), Key(F_END, 0));

        SetRotZ(clip, LEG_B,
            Key(0, 0), Key(F_DROP, -32), Key(F_FALL, -14), Key(F_IMPACT, 28),
            Key(F_RECOVER, 8), Key(F_END, 0));

        SetRotZ(clip, HAND_F,
            Key(0, 0), Key(F_DROP, 25), Key(F_FALL, 12), Key(F_IMPACT, 30),
            Key(F_RECOVER, 8), Key(F_END, 0));

        SetRotZ(clip, HAND_B,
            Key(0, 0), Key(F_DROP, -25), Key(F_FALL, -12), Key(F_IMPACT, -30),
            Key(F_RECOVER, -8), Key(F_END, 0));

        // ------------------------------------------------------------
        // Body/Head 위치 (코스메틱 스쿼시&스트레치 — 실제 낙하 이동은 물리가 담당)
        // ------------------------------------------------------------

        SetPosY(clip, BODY,
            Key(0, 0f), Key(F_DROP, -0.08f), Key(F_FALL, 0.02f), Key(F_IMPACT, -0.25f),
            Key(F_RECOVER, -0.05f), Key(F_END, 0f));

        SetPosY(clip, HEAD,
            Key(0, 0.2875f), Key(F_DROP, 0.287f), Key(F_FALL, 0.2865f), Key(F_IMPACT, 0.285f),
            Key(F_RECOVER, 0.2872f), Key(F_END, 0.2875f));

        // ------------------------------------------------------------
        // 저장
        // ------------------------------------------------------------

        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            System.IO.Directory.CreateDirectory(SAVE_PATH);
            AssetDatabase.Refresh();
        }

        string assetPath = $"{SAVE_PATH}/DownJumpLanding.anim";
        AssetDatabase.CreateAsset(clip, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[PlayerDownJumpLandingGenerator] 생성 완료: {assetPath}");

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }

    // ---- 헬퍼 ----

    struct KeyPoint
    {
        public int frame;
        public float value;
    }

    static KeyPoint Key(int frame, float value) => new KeyPoint { frame = frame, value = value };

    // 프레임 지점들을 부드러운(자동 접선) 커브로 만들어 회전 Z에 적용
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