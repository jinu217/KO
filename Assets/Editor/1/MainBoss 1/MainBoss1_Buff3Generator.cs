using UnityEngine;
using UnityEditor;

public class MainBoss1_Buff3Generator
{
    const string SAVE_PATH = "Assets/Animations/MainBoss 1";
    const string CLIP_NAME = "MainBoss1_Buff3";
    const float FRAME_RATE = 12f;
    const int TOTAL_FRAMES = 32;
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

    // ================================================================
    // Buff3: 무릎꿇고 왼손 칼 위로, 오른손 가슴
    //
    // 실측 자세:
    //   Body 0, Bottom +30
    //   TF=0, CF=-90, Foot_F=0
    //   TB=+15, CB=-45, Foot_B=0
    //   SF=26.14, FF=102.67, SB=180, FB=0, HB=0, WP=-90
    //
    // [무릎 꿇기]  f0~f6 : rest에서 무릎 꿇는 자세로
    // [팔 이동]    f7~f10: SF 가슴으로, SB 위로 올리기
    // [유지]       f11~f18: 자세 유지, 미세 진동
    // [복귀]       f19~f31: rest로
    // ================================================================

    static float[] F(float b, params (int i, float v)[] o)
    {
        var a = new float[TOTAL_FRAMES];
        for (int i = 0; i < TOTAL_FRAMES; i++) a[i] = b;
        foreach (var (i, v) in o) if (i < TOTAL_FRAMES) a[i] = v;
        return a;
    }

    static readonly float[] BODY_ROT = F(0f);
    static readonly float[] BODY_Y = F(0f,
        (0, 0f), (1, -0.10f), (2, -0.22f), (3, -0.34f), (4, -0.46f), (5, -0.56f), (6, -0.65f),
        (7, -0.65f), (8, -0.65f), (9, -0.65f), (10, -0.65f),
        (11, -0.65f), (12, -0.65f), (13, -0.65f), (14, -0.65f), (15, -0.65f), (16, -0.65f), (17, -0.65f), (18, -0.65f),
        (19, -0.58f), (20, -0.48f), (21, -0.36f), (22, -0.24f), (23, -0.14f), (24, -0.06f), (25, -0.02f), (26, 0f));

    static readonly float[] HEAD_ROT = F(-2f);

    static readonly float[] BOT_ROT = F(0f,
        (0, 0f), (1, 5f), (2, 10f), (3, 16f), (4, 22f), (5, 27f), (6, 30f), (7, 30f), (8, 30f), (9, 30f), (10, 30f), (11, 30f), (12, 30f), (13, 30f), (14, 30f), (15, 30f), (16, 30f), (17, 30f), (18, 30f), (19, 28f), (20, 24f), (21, 18f), (22, 12f), (23, 7f), (24, 3f), (25, 1f), (26, 0f));

    // SF: rest(20) → 26.14 (가슴 위로)
    static readonly float[] SF_ROT = F(20f,
        (1, 21f), (2, 22f), (3, 23f), (4, 24f), (5, 25f), (6, 25.5f), (7, 26f), (8, 26.14f), (9, 26.14f), (10, 26.14f), (11, 26.14f), (12, 26.14f), (13, 26.14f), (14, 26.14f), (15, 26.14f), (16, 26.14f), (17, 26.14f), (18, 26.14f), (19, 24f), (20, 23f), (21, 22f), (22, 21f), (23, 20f));

    // FF: rest(15) → 102.67
    static readonly float[] FF_ROT = F(15f,
        (1, 25f), (2, 38f), (3, 52f), (4, 65f), (5, 80f), (6, 90f), (7, 96f), (8, 100f), (9, 102f), (10, 102.672f), (11, 102.672f), (12, 102.672f), (13, 102.672f), (14, 102.672f), (15, 102.672f), (16, 102.672f), (17, 102.672f), (18, 102.672f), (19, 90f), (20, 74f), (21, 56f), (22, 40f), (23, 28f), (24, 20f), (25, 17f), (26, 15f));

    // SB: rest(0) → 180 (위로 크게)
    static readonly float[] SB_ROT = F(0f,
        (1, 18f), (2, 36f), (3, 58f), (4, 82f), (5, 108f), (6, 136f), (7, 154f), (8, 168f), (9, 176f), (10, 180f), (11, 180f), (12, 180f), (13, 180f), (14, 180f), (15, 180f), (16, 180f), (17, 180f), (18, 180f), (19, 165f), (20, 140f), (21, 108f), (22, 74f), (23, 44f), (24, 20f), (25, 7f), (26, 0f));

    // FB: rest(0) → 0 (그대로, 팔꿈치 안굽힘)
    static readonly float[] FB_ROT = F(0f,
        (7, 0f), (8, 0f), (9, 0f), (10, 0f), (11, 0f), (12, 0f), (13, 0f), (14, 0f), (15, 0f), (16, 0f), (17, 0f), (18, 0f), (19, 0f), (26, 0f));

    static readonly float[] HB_ROT = F(0f,
        (12, 0f), (13, 0f), (14, 0f), (15, 0f), (16, 0f), (17, 0f));
    static readonly float[] WP_ROT = F(-90f);
    static readonly float[] WP_SCALE_X = F(1f);
    static readonly float[] WP_SCALE_Y = F(1f);

    // 무릎: Buff2와 동일 (Foot_F=0이 차이)
    static readonly float[] TF_ROT = F(0f,
        (12, 0f), (13, 0f), (14, 0f), (15, 0f), (16, 0f), (17, 0f));

    static readonly float[] CF_ROT = F(0f,
        (1, -10f), (2, -22f), (3, -36f), (4, -52f), (5, -68f), (6, -80f), (7, -85f), (8, -88f), (9, -90f), (10, -90f), (11, -90f), (12, -90f), (13, -90f), (14, -90f), (15, -90f), (16, -90f), (17, -90f), (18, -90f), (19, -80f), (20, -65f), (21, -48f), (22, -33f), (23, -20f), (24, -10f), (25, -4f), (26, 0f));

    static readonly float[] FOOT_F_ROT = F(0f,
        (12, 0f), (13, 0f), (14, 0f), (15, 0f), (16, 0f), (17, 0f));  // Buff2와 달리 발끝 0

    static readonly float[] TB_ROT = F(0f,
        (1, 2f), (2, 5f), (3, 8f), (4, 10f), (5, 13f), (6, 14f), (7, 15f), (8, 15f), (9, 15f), (10, 15f), (11, 15f), (12, 15f), (13, 15f), (14, 15f), (15, 15f), (16, 15f), (17, 15f), (18, 15f), (19, 13f), (20, 10f), (21, 8f), (22, 6f), (23, 4f), (24, 2f), (25, 1f), (26, 0f));

    static readonly float[] CB_ROT = F(0f,
        (1, -5f), (2, -10f), (3, -18f), (4, -26f), (5, -34f), (6, -40f), (7, -43f), (8, -45f), (9, -45f), (10, -45f), (11, -45f), (12, -45f), (13, -45f), (14, -45f), (15, -45f), (16, -45f), (17, -45f), (18, -45f), (19, -40f), (20, -33f), (21, -25f), (22, -17f), (23, -10f), (24, -5f), (25, -2f), (26, 0f));

    [MenuItem("Tools/MainBoss 1/Generate Buff3")]
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
        C(BODY, "localEulerAngles.z", BODY_ROT);
        L(HEAD, "localPosition.x", 0f); L(HEAD, "localPosition.y", 0.70f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT);
        L(BOTTOM, "localPosition.x", -0.20f); L(BOTTOM, "localPosition.y", -0.30f); L(BOTTOM, "localPosition.z", 0f);
        CLinear(BOTTOM, "localEulerAngles.z", BOT_ROT);
        L(SF, "localPosition.x", -0.38f); L(SF, "localPosition.y", 0.34f); L(SF, "localPosition.z", 0f);
        CLinear(SF, "localEulerAngles.z", SF_ROT); CLinear(FF, "localEulerAngles.z", FF_ROT);
        L(SB, "localPosition.x", 0.26f); L(SB, "localPosition.y", 0.55f); L(SB, "localPosition.z", 0f);
        CLinear(SB, "localEulerAngles.z", SB_ROT); CLinear(FB, "localEulerAngles.z", FB_ROT); C(HB, "localEulerAngles.z", HB_ROT);
        C(WP, "localEulerAngles.z", WP_ROT); C(WP, "localScale.x", WP_SCALE_X); C(WP, "localScale.y", WP_SCALE_Y);
        L(WP, "localScale.z", 1f);
        CLinear(THIGH_F, "localEulerAngles.z", TF_ROT); CLinear(CALF_F, "localEulerAngles.z", CF_ROT);
        CLinear(FOOT_F, "localEulerAngles.z", FOOT_F_ROT);
        CLinear(THIGH_B, "localEulerAngles.z", TB_ROT); CLinear(CALF_B, "localEulerAngles.z", CB_ROT);

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