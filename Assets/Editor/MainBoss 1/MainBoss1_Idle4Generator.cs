using UnityEngine;
using UnityEditor;
using System.IO;

public class MainBoss1_Idle4Generator
{
    const string SAVE_PATH  = "Assets/Animations/MainBoss 1";
    const string CLIP_NAME  = "MainBoss1_Idle4";
    const float  FRAME_RATE = 12f;
    const int    N          = 32;
    const float  DUR        = N / FRAME_RATE;

    const string BODY    = "Body";
    const string HEAD    = "Body/Head";
    const string BOTTOM  = "Body/Bottom";
    const string SF      = "Body/Shoulder_F";
    const string FF      = "Body/Shoulder_F/Forearm_F";
    const string SB      = "Body/Shoulder_B";
    const string FB      = "Body/Shoulder_B/Forearm_B";
    const string HB      = "Body/Shoulder_B/Forearm_B/Hand_B";
    const string WP      = "Body/Shoulder_B/Forearm_B/Hand_B/Weapon";
    const string THIGH_F = "Body/Bottom/Thigh_F";
    const string CALF_F  = "Body/Bottom/Thigh_F/Calf_F";
    const string THIGH_B = "Body/Bottom/Thigh_B";
    const string CALF_B  = "Body/Bottom/Thigh_B/Calf_B";

    // ================================================================
    // 자세 컨셉: 양손으로 검을 잡고 극단적으로 앞으로 굽은 상태
    //   검끝이 바닥에 꽂힌 채 양손으로 검을 짚고 기대는 느낌
    //
    // Body    rot = +30   (극단적으로 앞으로 굽음)
    // Head    rot = -20   (굽은 몸에서 고개는 앞을 보려고)
    // SF      rot = +45   (앞으로 쭉 뻗어 검을 잡음)
    // FF      rot = +20   (팔꿈치 살짝 굽음)
    // SB      rot = +30   (오른팔도 검 쪽으로)
    // FB      rot = +15
    // HB      rot = -20
    // Weapon  rot = -90   (검끝 바닥)
    // Thigh_F rot = -30   (Body 앞굽 상쇄)
    // Calf_F  rot = +20
    // Thigh_B rot = -25
    // Calf_B  rot = +15
    // Bottom Y: Body 앞굽 보정
    //
    // 고개 까딱: 극단적으로 굽은 자세에서 고개만 들었다 떨굼
    // ================================================================

    static float[] F(float b, params (int i, float v)[] o)
    {
        var a = new float[N];
        for (int i = 0; i < N; i++) a[i] = b;
        foreach (var (i, v) in o) if (i < N) a[i] = v;
        return a;
    }

    // Body: 극단적으로 앞으로 굽은 채 고정
    static readonly float[] BODY_ROT = F(30f,
        (5, 31f),(6, 30f),
        (15,31f),(16,30f),
        (24,31f),(25,30f));

    // Bottom Y: Body Y 보정 (앞으로 굽으면 발이 들릴 수 있으므로)
    static readonly float[] BOT_Y = F(-0.30f);

    // Head: 굽은 몸에서 고개 들어 앞 보려는 자세 -20
    // 까딱: 고개를 완전히 떨궜다 다시 드는 동작
    static readonly float[] HEAD_ROT = F(-20f,
        // 까딱1: 고개 완전히 떨굼 → 다시 듦
        (3, -10f),(4,  5f),(5, -5f),(6,-15f),(7,-20f),
        // 까딱2: 고개 더 크게 듦
        (13,-10f),(14, 8f),(15,-2f),(16,-12f),(17,-20f),
        // 까딱3: 빠르게 까딱
        (22, -8f),(23, 4f),(24,-10f),(25,-18f),(26,-20f),
        // 까딱4
        (30, -6f),(31,-14f));

    static readonly float[] HEAD_X = F(0f,
        (4,-0.06f),(5,-0.03f),(6,-0.01f),
        (14,-0.08f),(15,-0.03f),(16,-0.01f),
        (23,-0.05f),(24,-0.02f));

    // SF: 앞으로 쭉 뻗어 검 짚음
    static readonly float[] SF_ROT = F(45f,
        (5, 46f),(6, 45f),
        (15,46f),(16,45f));

    static readonly float[] FF_ROT = F(20f);

    // SB: 오른팔도 검 쪽으로
    static readonly float[] SB_ROT = F(30f,
        (5, 31f),(6, 30f),
        (15,31f),(16,30f));

    static readonly float[] FB_ROT = F(15f);

    // Hand_B: 검 방향 유지
    static readonly float[] HB_ROT = F(-20f);
    static readonly float[] WP_ROT = F(-90f);

    // Thigh: Body +30 상쇄
    static readonly float[] TF_ROT = F(-30f,
        (5,-31f),(6,-30f),
        (15,-31f),(16,-30f),
        (24,-31f),(25,-30f));

    static readonly float[] CF_ROT = F(20f);

    static readonly float[] TB_ROT = F(-25f,
        (5,-26f),(6,-25f),
        (15,-26f),(16,-25f),
        (24,-26f),(25,-25f));

    static readonly float[] CB_ROT = F(15f);

    [MenuItem("Tools/MainBoss 1/Generate Idle4 (Hunched Over)")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "MainBoss 1");
        }

        var clip = new AnimationClip { frameRate = FRAME_RATE };
        clip.name = CLIP_NAME;
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        void C(string path, string prop, float[] vals)
        {
            var keys = new Keyframe[N + 1];
            for (int i = 0; i < N; i++) keys[i] = new Keyframe(i / FRAME_RATE, vals[i]);
            keys[N] = new Keyframe(DUR, vals[0]);
            var curve = new AnimationCurve(keys);
            for (int i = 0; i < curve.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve,  i, AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            }
            clip.SetCurve(path, typeof(Transform), prop, curve);
        }

        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop,
                AnimationCurve.Linear(0f, val, DUR, val));

        L(BODY, "localPosition.x",    0f);
        L(BODY, "localPosition.y",    0f);
        L(BODY, "localPosition.z",    0f);
        C(BODY, "localEulerAngles.z", BODY_ROT);

        C(HEAD, "localPosition.x",    HEAD_X);
        L(HEAD, "localPosition.y",    0.70f);
        L(HEAD, "localPosition.z",    0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT);

        L(BOTTOM, "localPosition.x", -0.20f);
        C(BOTTOM, "localPosition.y",  BOT_Y);
        L(BOTTOM, "localPosition.z",  0f);

        L(SF, "localPosition.x",   -0.575f);
        L(SF, "localPosition.y",    0.50f);
        L(SF, "localPosition.z",   0f);
        C(SF, "localEulerAngles.z", SF_ROT);
        C(FF, "localEulerAngles.z", FF_ROT);

        L(SB, "localPosition.x",   0.26f);
        L(SB, "localPosition.y",   0.55f);
        L(SB, "localPosition.z",   0f);
        C(SB, "localEulerAngles.z", SB_ROT);
        C(FB, "localEulerAngles.z", FB_ROT);
        C(HB, "localEulerAngles.z", HB_ROT);
        C(WP, "localEulerAngles.z", WP_ROT);

        C(THIGH_F, "localEulerAngles.z", TF_ROT);
        C(CALF_F,  "localEulerAngles.z", CF_ROT);
        C(THIGH_B, "localEulerAngles.z", TB_ROT);
        C(CALF_B,  "localEulerAngles.z", CB_ROT);

        string assetPath = $"{SAVE_PATH}/{CLIP_NAME}.anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null) { EditorUtility.CopySerialized(clip, existing); existing.name = CLIP_NAME; AssetDatabase.SaveAssets(); }
        else { AssetDatabase.CreateAsset(clip, assetPath); AssetDatabase.SaveAssets(); }
        AssetDatabase.Refresh();
        Debug.Log($"[MainBoss 1] {CLIP_NAME} -> {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}
