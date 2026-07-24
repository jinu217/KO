using UnityEngine;
using UnityEditor;

public class MainBoss1_Attack3Generator
{
    const string SAVE_PATH = "Assets/Animations/MainBoss 1";
    const string CLIP_NAME = "MainBoss1_Attack3";
    const float FRAME_RATE = 12f;
    const int TOTAL_FRAMES = 33;
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
    const string THIGH_B = "Body/Bottom/Thigh_B";
    const string CALF_B = "Body/Bottom/Thigh_B/Calf_B";

    // ================================================================
    // [기 모으기]  f0~f5
    // [예비]       f6~f8
    // [발사순간]   f9   : 실측 포즈 + scale 2.5
    // [임팩트]     f10~f11
    // [팔로스루]   f12~f13
    // [복귀→Rest]  f14~f32
    // ================================================================

    static float[] F(float b, params (int i, float v)[] o)
    {
        var a = new float[TOTAL_FRAMES];
        for (int i = 0; i < TOTAL_FRAMES; i++) a[i] = b;
        foreach (var (i, v) in o) if (i < TOTAL_FRAMES) a[i] = v;
        return a;
    }

    static readonly float[] BODY_ROT = F(0f,
        (0, 8f), (1, 10f), (2, 8f), (3, 10f), (4, 8f), (5, 8f),
        (6, 14f), (7, 18f), (8, 22f),
        (9, 65f),
        (10, -25f), (11, -25f),
        (12, -18f), (13, -10f),
        (14, -6f), (15, -3f), (16, -1f), (17, 0f));

    static readonly float[] HEAD_ROT = F(-2f,
        (0, -10f), (1, -12f), (2, -10f), (3, -12f), (4, -10f), (5, -10f),
        (6, -16f), (7, -20f), (8, -24f),
        (9, -14f),
        (10, 23f), (11, 23f),
        (12, 16f), (13, 8f),
        (14, 4f), (15, 1f), (16, -1f), (17, -2f));

    static readonly float[] BOT_ROT = F(0f,
        (0, -8f), (1, -10f), (2, -8f), (3, -10f), (4, -8f), (5, -8f),
        (6, -14f), (7, -18f), (8, -22f),
        (9, -55f),
        (10, 25f), (11, 25f),
        (12, 18f), (13, 10f),
        (14, 6f), (15, 3f), (16, 1f), (17, 0f));

    static readonly float[] BODY_Y = F(0f,
        (1, -0.02f), (2, -0.04f), (3, -0.02f), (4, 0f), (5, 0.02f),
        (10, 0.03f), (11, 0.05f), (12, 0.02f), (13, 0f));

    static readonly float[] SF_ROT = F(0f,
        (0, 180f), (1, 182f), (2, 180f), (3, 182f), (4, 180f), (5, 180f),
        (6, 175f), (7, 170f), (8, 165f),
        (9, 104.601f),
        (10, -4.22f), (11, -4.22f),
        (12, 15f), (13, 30f), (14, 15f), (15, 8f), (16, 3f), (17, 1f), (18, 0f));

    static readonly float[] FF_ROT = F(0f,
        (0, 20f), (1, 22f), (2, 20f), (3, 22f), (4, 20f), (5, 20f),
        (6, 18f), (7, 15f), (8, 12f),
        (9, 9.602f),
        (10, 48.30f), (11, 48.30f),
        (12, 30f), (13, 15f), (14, 6f), (15, 2f), (16, 0f));

    static readonly float[] SB_ROT = F(0f,
        (0, 180f), (1, 182f), (2, 180f), (3, 182f), (4, 180f), (5, 180f),
        (6, 185f), (7, 192f), (8, 198f),
        (9, 13.372f),
        (10, -10.97f), (11, -10.97f),
        (12, 10f), (13, 40f), (14, 80f), (15, 120f), (16, 150f), (17, 168f), (18, 176f), (19, 180f));

    static readonly float[] FB_ROT = F(0f,
        (0, 85f), (1, 87f), (2, 85f), (3, 87f), (4, 85f), (5, 85f),
        (6, 88f), (7, 92f), (8, 96f),
        (9, 130.491f),
        (10, 12.12f), (11, 12.12f),
        (12, 25f), (13, 45f), (14, 60f), (15, 72f), (16, 80f), (17, 84f), (18, 85f));

    static readonly float[] HB_ROT = F(-20f,
        (0, -30f), (1, -30f), (2, -30f), (3, -30f), (4, -30f), (5, -30f),
        (6, -28f), (7, -25f), (8, -22f),
        (9, -22f),
        (10, -30f), (11, -30f),
        (12, -28f), (13, -26f),
        (14, -24f), (15, -22f), (16, -21f), (17, -20f));

    static readonly float[] WP_ROT = F(-90f,
        (6, -85f), (7, -80f), (8, -75f),
        (9, -75f),
        (10, -90f), (11, -90f),
        (12, -90f));

    // scale: 공격 구간만, 복귀부터는 1.0 유지
    static readonly float[] WP_SCALE_X = F(1f,
        (0, 1f), (1, 0.92f), (2, 1f), (3, 0.92f), (4, 1f), (5, 1f),
        (9, 2.5f),
        (10, 2.2f), (11, 1.8f),
        (12, 1.0f));

    static readonly float[] WP_SCALE_Y = F(1f,
        (0, 1f), (1, 0.92f), (2, 1f), (3, 0.92f), (4, 1f), (5, 1f),
        (9, 2.5f),
        (10, 2.0f), (11, 1.6f),
        (12, 1.0f));

    static readonly float[] TF_ROT = F(0f,
        (0, -6f), (1, -7f), (2, -8f), (3, -7f), (4, -6f), (5, -6f),
        (9, 0f),
        (10, 4f), (11, 4f),
        (12, 2f), (13, 0f));

    static readonly float[] CF_ROT = F(0f,
        (0, 4f), (1, 5f), (2, 6f), (3, 5f), (4, 4f), (5, 4f),
        (10, -2f), (11, -4f),
        (12, -2f), (13, 0f));

    static readonly float[] TB_ROT = F(0f,
        (0, -4f), (1, -5f), (2, -6f), (3, -5f), (4, -4f), (5, -4f),
        (10, -10f), (11, -12f),
        (12, -8f), (13, -6f), (14, -4f), (15, -2f), (16, 0f));

    static readonly float[] CB_ROT = F(0f,
        (0, 2f), (1, 3f), (2, 4f), (3, 3f), (4, 2f), (5, 2f),
        (10, 6f), (11, 8f),
        (12, 5f), (13, 3f), (14, 2f), (15, 1f), (16, 0f));

    [MenuItem("Tools/MainBoss 1/Generate Attack3")]
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
        C(BODY, "localEulerAngles.z", BODY_ROT);
        L(HEAD, "localPosition.x", 0f); L(HEAD, "localPosition.y", 0.70f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT);
        L(BOTTOM, "localPosition.x", -0.20f); L(BOTTOM, "localPosition.y", -0.30f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", BOT_ROT);
        L(SF, "localPosition.x", -0.575f); L(SF, "localPosition.y", 0.50f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", SF_ROT); C(FF, "localEulerAngles.z", FF_ROT);
        L(SB, "localPosition.x", 0.26f); L(SB, "localPosition.y", 0.55f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", SB_ROT); C(FB, "localEulerAngles.z", FB_ROT); C(HB, "localEulerAngles.z", HB_ROT);
        C(WP, "localEulerAngles.z", WP_ROT); C(WP, "localScale.x", WP_SCALE_X); C(WP, "localScale.y", WP_SCALE_Y);
        L(WP, "localScale.z", 1f);
        C(THIGH_F, "localEulerAngles.z", TF_ROT); C(CALF_F, "localEulerAngles.z", CF_ROT);
        C(THIGH_B, "localEulerAngles.z", TB_ROT); C(CALF_B, "localEulerAngles.z", CB_ROT);

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