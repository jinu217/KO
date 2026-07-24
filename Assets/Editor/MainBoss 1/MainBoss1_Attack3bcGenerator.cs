using UnityEngine;
using UnityEditor;

public class MainBoss1_Attack3BCGenerator
{
    const string SAVE_PATH = "Assets/Animations/MainBoss 1";
    const float FRAME_RATE = 12f;
    const int TOTAL_FRAMES = 35;
    const float DUR = TOTAL_FRAMES / FRAME_RATE;

    const string BODY = "Body";
    const string HEAD = "Body/Head";
    const string BOTTOM = "Body/Bottom";
    const string SF = "Body/Shoulder_F";
    const string FF = "Body/Shoulder_F/Forearm_F";
    const string SB = "Body/Shoulder_B";
    const string FB = "Body/Shoulder_B/Forearm_B";
    const string HB = "Body/Shoulder_B/Forearm_B/Hand_B";
    const string WP = "Body/Shoulder_B/Forearm_B/Hand_B/Weapon";
    const string THIGH_F = "Body/Bottom/Thigh_F";
    const string CALF_F = "Body/Bottom/Thigh_F/Calf_F";
    const string FOOT_F = "Body/Bottom/Thigh_F/Calf_F/Foot_F";
    const string THIGH_B = "Body/Bottom/Thigh_B";
    const string CALF_B = "Body/Bottom/Thigh_B/Calf_B";
    const string FOOT_B = "Body/Bottom/Thigh_B/Calf_B/Foot_B";

    // ================================================================
    // [왼발 들기]   f0~f2
    // [기 모으기]   f3~f7
    // [예비]        f8~f10
    // [발사순간]    f11  : 실측 포즈 + scale 2.5
    // [임팩트]      f12~f13
    // [팔로스루]    f14~f15
    // [복귀→Rest]   f16~f34
    //
    // 3b: Body+60 (자세A)
    // 3c: Body+30 (자세B)  → BOT_ROT만 다름
    // ================================================================

    static float[] F(float b, params (int i, float v)[] o)
    {
        var a = new float[TOTAL_FRAMES];
        for (int i = 0; i < TOTAL_FRAMES; i++) a[i] = b;
        foreach (var (i, v) in o) if (i < TOTAL_FRAMES) a[i] = v;
        return a;
    }

    // ── 자세B(Body+30) 공통 ──────────────────────────────────────────
    static readonly float[] BODY_ROT_B = F(0f,
        (0, 8f), (1, 30f), (2, 50f),
        (3, 60f), (4, 60f), (5, 62f), (6, 60f), (7, 62f),
        (8, 65f), (9, 68f), (10, 65f),
        (11, 65f),
        (12, -25f), (13, -25f),
        (14, -18f), (15, -10f),
        (16, -6f), (17, -3f), (18, -1f), (19, 0f));

    static readonly float[] BODY_ROT_C = F(0f,
        (0, 8f), (1, 18f), (2, 26f),
        (3, 30f), (4, 30f), (5, 32f), (6, 30f), (7, 32f),
        (8, 35f), (9, 38f), (10, 35f),
        (11, 65f),
        (12, -25f), (13, -25f),
        (14, -18f), (15, -10f),
        (16, -6f), (17, -3f), (18, -1f), (19, 0f));

    static readonly float[] HEAD_ROT = F(-2f,
        (0, -10f), (1, -10f), (2, -10f),
        (3, -10f), (4, -10f), (5, -12f), (6, -10f), (7, -12f),
        (8, -14f), (9, -16f), (10, -14f),
        (11, -14f),
        (12, 23f), (13, 23f),
        (14, 16f), (15, 8f),
        (16, 4f), (17, 1f), (18, -1f), (19, -2f));

    static readonly float[] BOT_ROT_B = F(0f,
        (0, -8f), (1, -28f), (2, -46f),
        (3, -50f), (4, -50f), (5, -52f), (6, -50f), (7, -52f),
        (8, -55f), (9, -58f), (10, -55f),
        (11, -55f),
        (12, 25f), (13, 25f),
        (14, 18f), (15, 10f),
        (16, 6f), (17, 3f), (18, 1f), (19, 0f));

    static readonly float[] BOT_ROT_C = F(0f,
        (0, -8f), (1, -16f), (2, -23f),
        (3, -20f), (4, -20f), (5, -22f), (6, -20f), (7, -22f),
        (8, -25f), (9, -28f), (10, -25f),
        (11, -55f),
        (12, 25f), (13, 25f),
        (14, 18f), (15, 10f),
        (16, 6f), (17, 3f), (18, 1f), (19, 0f));

    static readonly float[] BODY_Y = F(0f,
        (1, -0.02f), (2, -0.04f),
        (3, -0.05f), (4, -0.05f), (5, -0.06f), (6, -0.05f), (7, -0.06f),
        (8, -0.03f), (9, 0f),
        (12, 0.03f), (13, 0.05f), (14, 0.02f), (15, 0f));

    static readonly float[] SF_ROT = F(0f,
        (0, 175f), (1, 170f), (2, -167.27f),
        (3, -167.27f), (4, -167.27f), (5, -165f), (6, -167.27f), (7, -165f),
        (8, -162f), (9, -158f), (10, -155f),
        (11, 104.601f),
        (12, -4.22f), (13, -4.22f),
        (14, 15f), (15, 30f), (16, 15f), (17, 8f), (18, 3f), (19, 1f), (20, 0f));

    static readonly float[] FF_ROT = F(0f,
        (0, 10f), (1, 2f), (2, -4.24f),
        (3, -4.24f), (4, -4.24f), (5, -3f), (6, -4.24f), (7, -3f),
        (8, 0f), (9, 5f), (10, 10f),
        (11, 9.602f),
        (12, 48.30f), (13, 48.30f),
        (14, 30f), (15, 15f), (16, 6f), (17, 2f), (18, 0f));

    static readonly float[] SB_ROT = F(0f,
        (0, 170f), (1, 158f), (2, -150.73f),
        (3, -150.73f), (4, -150.73f), (5, -148f), (6, -150.73f), (7, -148f),
        (8, -145f), (9, -142f), (10, -138f),
        (11, 13.372f),
        (12, -10.97f), (13, -10.97f),
        (14, 10f), (15, 40f), (16, 80f), (17, 120f), (18, 150f), (19, 168f), (20, 176f), (21, 180f));

    static readonly float[] FB_ROT = F(0f,
        (0, 60f), (1, 45f), (2, 30.32f),
        (3, 30.32f), (4, 30.32f), (5, 32f), (6, 30.32f), (7, 32f),
        (8, 35f), (9, 40f), (10, 45f),
        (11, 130.491f),
        (12, 12.12f), (13, 12.12f),
        (14, 25f), (15, 45f), (16, 60f), (17, 72f), (18, 80f), (19, 84f), (20, 85f));

    static readonly float[] HB_ROT = F(-20f,
        (0, -30f), (1, -30f), (2, -30f),
        (3, -30f), (4, -30f), (5, -32f), (6, -30f), (7, -32f),
        (8, -30f), (9, -26f), (10, -22f),
        (11, -22f),
        (12, -30f), (13, -30f),
        (14, -28f), (15, -26f),
        (16, -24f), (17, -22f), (18, -21f), (19, -20f));

    static readonly float[] WP_ROT = F(-90f,
        (8, -85f), (9, -80f), (10, -75f),
        (11, -75f),
        (12, -90f), (13, -90f),
        (14, -90f));

    // scale: 공격 구간만, 복귀는 1.0
    static readonly float[] WP_SCALE_X = F(1f,
        (3, 0.92f), (4, 1f), (5, 0.92f), (6, 1f), (7, 0.92f),
        (11, 2.5f),
        (12, 2.2f), (13, 1.8f),
        (14, 1.0f));

    static readonly float[] WP_SCALE_Y = F(1f,
        (3, 0.92f), (4, 1f), (5, 0.92f), (6, 1f), (7, 0.92f),
        (11, 2.5f),
        (12, 2.0f), (13, 1.6f),
        (14, 1.0f));

    // 오른발
    static readonly float[] TF_ROT = F(0f,
        (8, -4f), (9, -2f), (10, 0f),
        (12, 4f), (13, 4f), (14, 2f), (15, 0f));

    static readonly float[] CF_ROT = F(0f,
        (12, -2f), (13, -4f), (14, -2f), (15, 0f));

    static readonly float[] FOOT_F_ROT = F(-10f,
        (12, 0f), (13, 2f), (14, 0f), (15, -5f), (16, -8f), (17, -10f));

    // 왼발: 들렸다가 임팩트 후 내리며 rest
    static readonly float[] TB_ROT = F(0f,
        (0, 5f), (1, 28f), (2, 52.29f),
        (3, 52.29f), (4, 52.29f), (5, 54f), (6, 52.29f), (7, 54f),
        (8, 35f), (9, 15f), (10, 2f),
        (12, -6f), (13, -8f),
        (14, -6f), (15, -4f), (16, -2f), (17, 0f));

    static readonly float[] CB_ROT = F(0f,
        (0, 5f), (1, -5f), (2, -12.41f),
        (3, -12.41f), (4, -12.41f), (5, -14f), (6, -12.41f), (7, -14f),
        (8, -8f), (9, -3f), (10, 0f),
        (12, 6f), (13, 8f),
        (14, 5f), (15, 3f), (16, 1f), (17, 0f));

    static readonly float[] FOOT_B_ROT = F(0f);

    static void DoGenerate(string clipName, float[] bodyRot, float[] botRot)
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "MainBoss 1");
        }
        var clip = new AnimationClip { frameRate = FRAME_RATE };
        clip.name = clipName;
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        void C(string path, string prop, float[] vals)
        {
            var keys = new Keyframe[TOTAL_FRAMES];
            for (int i = 0; i < TOTAL_FRAMES; i++) keys[i] = new Keyframe(i / FRAME_RATE, vals[i]);
            var curve = new AnimationCurve(keys);
            for (int i = 0; i < curve.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            }
            clip.SetCurve(path, typeof(Transform), prop, curve);
        }
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", BODY_Y); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", bodyRot);
        L(HEAD, "localPosition.x", 0f); L(HEAD, "localPosition.y", 0.70f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT);
        L(BOTTOM, "localPosition.x", -0.20f); L(BOTTOM, "localPosition.y", -0.30f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", botRot);
        L(SF, "localPosition.x", -0.575f); L(SF, "localPosition.y", 0.50f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", SF_ROT); C(FF, "localEulerAngles.z", FF_ROT);
        L(SB, "localPosition.x", 0.26f); L(SB, "localPosition.y", 0.55f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", SB_ROT); C(FB, "localEulerAngles.z", FB_ROT); C(HB, "localEulerAngles.z", HB_ROT);
        C(WP, "localEulerAngles.z", WP_ROT); C(WP, "localScale.x", WP_SCALE_X); C(WP, "localScale.y", WP_SCALE_Y);
        L(WP, "localScale.z", 1f);
        C(THIGH_F, "localEulerAngles.z", TF_ROT); C(CALF_F, "localEulerAngles.z", CF_ROT);
        C(FOOT_F, "localEulerAngles.z", FOOT_F_ROT);
        C(THIGH_B, "localEulerAngles.z", TB_ROT); C(CALF_B, "localEulerAngles.z", CB_ROT);
        C(FOOT_B, "localEulerAngles.z", FOOT_B_ROT);

        string assetPath = $"{SAVE_PATH}/{clipName}.anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null) { EditorUtility.CopySerialized(clip, existing); existing.name = clipName; AssetDatabase.SaveAssets(); }
        else { AssetDatabase.CreateAsset(clip, assetPath); AssetDatabase.SaveAssets(); }
        AssetDatabase.Refresh();
        Debug.Log($"[MainBoss 1] {clipName} -> {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }

    [MenuItem("Tools/MainBoss 1/Generate Attack3b")]
    public static void GenerateB() => DoGenerate("MainBoss1_Attack3b", BODY_ROT_B, BOT_ROT_B);

    [MenuItem("Tools/MainBoss 1/Generate Attack3c")]
    public static void GenerateC() => DoGenerate("MainBoss1_Attack3c", BODY_ROT_C, BOT_ROT_C);
}