using UnityEngine;
using UnityEditor;

public class MainBoss1_Attack3BCGenerator
{
    const string SAVE_PATH = "Assets/Animations/MainBoss 1";
    const float FRAME_RATE = 12f;
    const int TOTAL_FRAMES = 23;
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

    // f0~f2  : 왼발 들기
    // f3~f7  : 기 모으기
    // f8~f10 : 예비
    // f11    : 발사 순간 (scale 2.5)
    // f12~f13: 임팩트
    // f14~f15: hold
    // f18    : rest

    static float[] F(float b, params (int i, float v)[] o)
    {
        var a = new float[TOTAL_FRAMES];
        for (int i = 0; i < TOTAL_FRAMES; i++) a[i] = b;
        foreach (var (i, v) in o) if (i < TOTAL_FRAMES) a[i] = v;
        return a;
    }

    // ── 자세B (Body+60) ───────────────────────────────────────────────
    static readonly float[] BODY_ROT_B = F(0f,
        (0, 8f), (1, 30f), (2, 50f),
        (3, 60f), (4, 60f), (5, 62f), (6, 60f), (7, 62f),
        (8, 65f), (9, 68f), (10, 65f),
        (11, 65f),
        (12, -25f), (13, -25f), (14, -25f), (15, -25f), (16, -25f), (17, -25f),
        (18, -25f), (19, -25f), (20, 0f), (21, 0f), (22, 0f));

    // ── 자세C (Body+30) ───────────────────────────────────────────────
    static readonly float[] BODY_ROT_C = F(0f,
        (0, 8f), (1, 18f), (2, 26f),
        (3, 30f), (4, 30f), (5, 32f), (6, 30f), (7, 32f),
        (8, 35f), (9, 38f), (10, 35f),
        (11, 65f),
        (12, -25f), (13, -25f), (14, -25f), (15, -25f), (16, -25f), (17, -25f),
        (18, -25f), (19, -25f), (20, 0f), (21, 0f), (22, 0f));

    static readonly float[] HEAD_ROT = F(-2f,
        (0, -10f), (1, -10f), (2, -10f),
        (3, -10f), (4, -10f), (5, -12f), (6, -10f), (7, -12f),
        (8, -14f), (9, -16f), (10, -14f),
        (11, -14f),
        (12, 23f), (13, 23f), (14, 23f), (15, 23f), (16, 23f), (17, 23f),
        (18, 23f), (19, 23f), (20, -2f), (21, -2f), (22, -2f));

    static readonly float[] BOT_ROT_B = F(0f,
        (0, -8f), (1, -28f), (2, -46f),
        (3, -50f), (4, -50f), (5, -52f), (6, -50f), (7, -52f),
        (8, -55f), (9, -58f), (10, -55f),
        (11, -55f),
        (12, 25f), (13, 25f), (14, 25f), (15, 25f), (16, 25f), (17, 25f),
        (18, 25f), (19, 25f), (20, 0f), (21, 0f), (22, 0f));

    static readonly float[] BOT_ROT_C = F(0f,
        (0, -8f), (1, -16f), (2, -23f),
        (3, -20f), (4, -20f), (5, -22f), (6, -20f), (7, -22f),
        (8, -25f), (9, -28f), (10, -25f),
        (11, -55f),
        (12, 25f), (13, 25f), (14, 25f), (15, 25f), (16, 25f), (17, 25f),
        (18, 25f), (19, 25f), (20, 0f), (21, 0f), (22, 0f));

    static readonly float[] BODY_Y = F(0f,
        (1, -0.02f), (2, -0.04f),
        (3, -0.05f), (4, -0.05f), (5, -0.06f), (6, -0.05f), (7, -0.06f),
        (8, -0.03f), (9, 0f),
        (12, 0.05f), (13, 0.05f), (14, 0.05f), (15, 0.05f), (16, 0.05f), (17, 0.05f),
        (18, 0.05f), (19, 0.05f), (20, 0f), (21, 0f), (22, 0f));

    static readonly float[] SF_ROT = F(0f,
        (0, 175f), (1, 170f), (2, 192.73f), (3, 192.73f), (4, 192.73f), (5, 195f), (6, 192.73f), (7, 195f),
        (8, 198f), (9, 202f), (10, 205f),
        (11, 104.601f),
        (12, -4.22f), (13, -4.22f), (14, -4.22f), (15, -4.22f),
        (16, -4.22f), (17, -4.22f), (18, 25.56f), (19, 25.56f));

    static readonly float[] FF_ROT = F(0f,
        (0, 10f), (1, 2f), (2, -4.24f),
        (3, -4.24f), (4, -4.24f), (5, -3f), (6, -4.24f), (7, -3f),
        (8, 0f), (9, 5f), (10, 10f),
        (11, 9.602f),
        (12, 48.30f), (13, 48.30f), (14, 48.30f), (15, 48.30f), (16, 48.30f), (17, 48.30f),
        (18, 48.30f), (19, 48.30f), (20, 7.39f), (21, 7.39f), (22, 7.39f));

    static readonly float[] SB_ROT = F(0f,
        (0, 170f), (1, 158f), (2, 209.27f), (3, 209.27f), (4, 209.27f), (5, 212f), (6, 209.27f), (7, 212f),
        (8, 215f), (9, 218f), (10, 222f),
        (11, 13.372f),
        (12, -10.97f), (13, -10.97f), (14, -10.97f), (15, -10.97f),
        (16, -10.97f), (17, -10.97f), (18, 0f), (19, 0f));

    static readonly float[] FB_ROT = F(0f,
        (0, 60f), (1, 45f), (2, 30.32f),
        (3, 30.32f), (4, 30.32f), (5, 32f), (6, 30.32f), (7, 32f),
        (8, 35f), (9, 40f), (10, 45f),
        (11, 130.491f),
        (12, 12.12f), (13, 12.12f), (14, 12.12f), (15, 12.12f), (16, 12.12f), (17, 12.12f),
        (18, 12.12f), (19, 12.12f), (20, 0f), (21, 0f), (22, 0f));

    static readonly float[] HB_ROT = F(-20f,
        (0, -30f), (1, -30f), (2, -30f),
        (3, -30f), (4, -30f), (5, -32f), (6, -30f), (7, -32f),
        (8, -30f), (9, -26f), (10, -22f),
        (11, -22f),
        (12, -30f), (13, -30f), (14, -30f), (15, -30f), (16, -30f), (17, -30f),
        (18, -30f), (19, -30f), (20, 0f), (21, 0f), (22, 0f));

    static readonly float[] WP_ROT = F(-90f,
        (8, -85f), (9, -80f), (10, -75f),
        (11, -75f),
        (12, -90f), (13, -90f), (14, -90f), (15, -90f), (16, -90f), (17, -90f), (21, -90f), (22, -90f));

    static readonly float[] WP_SCALE_X = F(1f,
        (0, 1f), (1, 1f), (2, 1f),
        (3, 0.92f), (4, 1f), (5, 0.92f), (6, 1f), (7, 0.92f),
        (10, 1.08f), (11, 1.08f),
        (12, 1.08f), (13, 1.08f), (14, 1.08f), (15, 1.08f),
        (16, 1.0f), (21, 1.0f), (22, 1.0f));

    static readonly float[] WP_SCALE_Y = F(1f,
        (0, 1f), (1, 1f), (2, 1f),
        (3, 0.92f), (4, 1f), (5, 0.92f), (6, 1f), (7, 0.92f),
        (10, 1.08f), (11, 1.08f),
        (12, 1.08f), (13, 1.08f), (14, 1.08f), (15, 1.08f),
        (16, 1.0f), (21, 1.0f), (22, 1.0f));

    static readonly float[] TF_ROT = F(0f,
        (8, -4f), (9, -2f), (10, 0f),
        (12, 4f), (13, 4f), (14, 4f), (15, 4f), (16, 4f), (17, 4f),
        (18, 4f), (19, 4f), (20, 0f), (21, 0f), (22, 0f));

    static readonly float[] CF_ROT = F(0f,
        (12, -2f), (13, -2f), (14, -2f), (15, -4f), (16, -4f), (17, -4f),
        (18, -4f), (19, -4f), (20, 0f), (21, 0f), (22, 0f));

    static readonly float[] FOOT_F_ROT = F(-10f,
        (12, 0f), (13, 0f), (14, 0f), (15, 2f), (16, 2f), (17, 2f),
        (18, 2f), (19, 2f), (20, -10f), (21, -10f), (22, -10f));

    static readonly float[] TB_ROT = F(0f,
        (0, 5f), (1, 28f), (2, 52.29f),
        (3, 52.29f), (4, 52.29f), (5, 54f), (6, 52.29f), (7, 54f),
        (8, 35f), (9, 15f), (10, 2f),
        (12, -6f), (13, -6f), (14, -6f), (15, -8f), (16, -8f), (17, -8f),
        (18, -8f), (19, -8f), (20, 0f), (21, 0f), (22, 0f));

    static readonly float[] CB_ROT = F(0f,
        (0, 5f), (1, -5f), (2, -12.41f),
        (3, -12.41f), (4, -12.41f), (5, -14f), (6, -12.41f), (7, -14f),
        (8, -8f), (9, -3f), (10, 0f),
        (12, 6f), (13, 6f), (14, 6f), (15, 8f), (16, 8f), (17, 8f),
        (18, 8f), (19, 8f), (20, 0f), (21, 0f), (22, 0f));

    static readonly float[] FOOT_B_ROT = F(0f, (21, 0f), (22, 0f));

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

        // WP_ROT 전용: 전구간 Linear (오버슈트 방지)
        void CLinear(string path, string prop, float[] vals)
        {
            var keys = new Keyframe[TOTAL_FRAMES];
            for (int i = 0; i < TOTAL_FRAMES; i++) keys[i] = new Keyframe(i / FRAME_RATE, vals[i]);
            var curve = new AnimationCurve(keys);
            for (int i = 0; i < curve.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
            clip.SetCurve(path, typeof(Transform), prop, curve);
        }
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", BODY_Y); L(BODY, "localPosition.z", 0f);
        CLinear(BODY, "localEulerAngles.z", bodyRot);
        L(HEAD, "localPosition.x", 0f); L(HEAD, "localPosition.y", 0.70f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT);
        L(BOTTOM, "localPosition.x", -0.20f); L(BOTTOM, "localPosition.y", -0.30f); L(BOTTOM, "localPosition.z", 0f);
        CLinear(BOTTOM, "localEulerAngles.z", botRot);
        L(SF, "localPosition.x", -0.38f); L(SF, "localPosition.y", 0.34f); L(SF, "localPosition.z", 0f);
        CLinear(SF, "localEulerAngles.z", SF_ROT); CLinear(FF, "localEulerAngles.z", FF_ROT);
        L(SB, "localPosition.x", 0.26f); L(SB, "localPosition.y", 0.55f); L(SB, "localPosition.z", 0f);
        CLinear(SB, "localEulerAngles.z", SB_ROT); CLinear(FB, "localEulerAngles.z", FB_ROT); CLinear(HB, "localEulerAngles.z", HB_ROT);
        CLinear(WP, "localEulerAngles.z", WP_ROT); CLinear(WP, "localScale.x", WP_SCALE_X); CLinear(WP, "localScale.y", WP_SCALE_Y);
        L(WP, "localScale.z", 1f);
        CLinear(THIGH_F, "localEulerAngles.z", TF_ROT); CLinear(CALF_F, "localEulerAngles.z", CF_ROT);
        CLinear(FOOT_F, "localEulerAngles.z", FOOT_F_ROT);
        CLinear(THIGH_B, "localEulerAngles.z", TB_ROT); CLinear(CALF_B, "localEulerAngles.z", CB_ROT);
        CLinear(FOOT_B, "localEulerAngles.z", FOOT_B_ROT);

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