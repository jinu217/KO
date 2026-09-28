using UnityEngine;
using UnityEditor;

public class MainBoss1_Attack5Generator
{
    const string SAVE_PATH = "Assets/Animations/MainBoss 1";
    const string CLIP_NAME = "MainBoss1_Attack5";
    const float FRAME_RATE = 12f;
    const int TOTAL_FRAMES = 66;
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
    // Buff1 + 세로세로가로세로가로 5연격
    //
    // [Buff1 집중]  f0~f12  : 실측(-17/+69/+38/-28/+135)
    // [발동]        f13~f15
    // [검 높이 들기] f16~f22 : SB 크게 올림
    // [1타 세로]    f23~f28
    // [2타 세로]    f29~f34
    // [3타 가로]    f35~f44
    // [4타 세로]    f45~f50
    // [5타 가로]    f51~f62
    // [복귀]        f63~f72
    //
    // 세로 임팩트 실측: SF=-4.22, FF=48.30, SB=-10.97, FB=12.12
    // 가로 임팩트: WP -90 + scale
    // ================================================================

    static float[] F(float b, params (int i, float v)[] o)
    {
        var a = new float[TOTAL_FRAMES];
        for (int i = 0; i < TOTAL_FRAMES; i++) a[i] = b;
        foreach (var (i, v) in o) if (i < TOTAL_FRAMES) a[i] = v;
        return a;
    }

    // ── Body: Buff1=-17, 발동 후 세로베기 패턴 ─────────────────────
    static readonly float[] BODY_Y = F(0f,
        (0, -0.06f), (1, -0.065f), (2, -0.06f), (3, -0.065f), (4, -0.06f), (5, -0.065f), (6, -0.06f), (7, -0.065f), (8, -0.06f), (9, -0.065f), (10, -0.06f), (11, -0.065f), (12, -0.06f), (13, -0.02f), (14, 0.02f), (15, 0.04f), (16, 0.04f), (17, 0f), (18, 0f), (19, 0f), (20, 0.02f), (21, 0f), (58, 0.0f), (59, 0.0f), (60, 0.0f), (61, 0f));

    static readonly float[] BODY_ROT = F(0f,
        (0, -17.0f), (1, -18.0f), (2, -17.0f), (3, -18.0f), (4, -17.0f), (5, -18.0f), (6, -17.0f), (7, -18.0f), (8, -17.0f), (9, -18.0f), (10, -17.0f), (11, -18.0f), (12, -17.0f), (13, -10.0f), (14, -5.0f), (15, 2.0f), (16, -8f), (17, -8f), (18, -8f), (19, -8f), (20, -14f), (21, -18f), (22, -20f), (23, -20f), (24, -20f), (25, -20f), (26, -20f), (27, -16f), (28, -8f), (29, 0f), (30, 10f), (31, 18f), (32, 22f), (33, -26f), (34, 10f), (35, 18f), (36, 24f), (37, 26f), (38, 26f), (39, 26f), (40, -18f), (41, -26f), (42, -18f), (43, 0f), (44, -8f), (45, -14f), (46, -18f), (47, -20f), (48, -20f), (49, 0f), (50, 12f), (51, 22f), (52, 28f), (53, 30f), (54, 30f), (55, 30f), (56, 30f), (57, -22f), (58, -22.0f), (59, -22.0f), (60, -22.0f), (61, 0f), (63, 0f), (64, 0f));

    // ── Head: Body 반대 ────────────────────────────────────────────
    static readonly float[] HEAD_ROT = F(-2f,
        (0, 15.0f), (1, 16.0f), (2, 15.0f), (3, 16.0f), (4, 15.0f), (5, 16.0f), (6, 15.0f), (7, 16.0f), (8, 15.0f), (9, 16.0f), (10, 15.0f), (11, 16.0f), (12, 15.0f), (13, 8.0f), (14, 3.0f), (15, -4.0f), (34, -10.0f), (35, -18.0f), (36, -24.0f), (37, -26.0f), (38, -26.0f), (39, -26.0f), (40, 16.0f), (41, 24.0f), (42, 16.0f), (44, -4.0f), (45, -8f), (46, -12f), (47, -14f), (48, -14.0f), (50, -12.0f), (51, -22.0f), (52, -28.0f), (53, -30.0f), (54, -30.0f), (55, -30.0f), (56, -30.0f), (57, 20.0f), (58, 20.0f), (59, 20.0f), (60, 20.0f), (61, -2f));

    // ── Bottom: Body 반대 ──────────────────────────────────────────
    static readonly float[] BOT_ROT = F(0f,
        (0, 17.0f), (1, 18.0f), (2, 17.0f), (3, 18.0f), (4, 17.0f), (5, 18.0f), (6, 17.0f), (7, 18.0f), (8, 17.0f), (9, 18.0f), (10, 17.0f), (11, 18.0f), (12, 17.0f), (13, 10.0f), (14, 5.0f), (15, -2.0f), (16, 8f), (17, 8f), (18, 8f), (19, 8f), (20, 14f), (21, 18f), (22, 20f), (23, 20f), (24, 20f), (25, 20f), (26, 20f), (27, 16f), (28, 8f), (29, 0f), (30, -10f), (31, -18f), (32, -22f), (33, 26f), (34, -10f), (35, -18f), (36, -24f), (37, -26f), (38, -26f), (39, -26f), (40, 18f), (41, 26f), (42, 18f), (43, 0f), (44, 8f), (45, 14f), (46, 18f), (47, 20f), (48, 20f), (49, 0f), (50, -12f), (51, -22f), (52, -28f), (53, -30f), (54, -30f), (55, -30f), (56, -30f), (57, 22f), (58, 22.0f), (59, 22.0f), (60, 22.0f), (61, 0f), (63, 0f), (64, 0f));

    // ── SF: Buff1=-167.27→192.73, 들기=높게, 세로=임팩트 ──────────
    static readonly float[] SF_ROT = F(20f,
        (0, 69.036f), (1, 70.0f), (2, 69.036f), (3, 70.0f), (4, 69.036f), (5, 70.0f), (6, 69.036f), (7, 70.0f), (8, 69.036f), (9, 70.0f), (10, 69.036f), (11, 70.0f), (12, 69.036f), (13, 75.0f), (14, 72.0f), (15, 70.0f), (16, 60f), (17, 60f), (18, 60f), (19, 60f), (20, 80f), (21, 100f), (22, 115f), (23, 115f), (24, 115f), (25, 130f), (26, 145f), (27, -4.22f), (28, -4.22f), (29, 20f), (30, 95f), (31, 138f), (32, 148f), (33, -48.126f), (34, 14f), (35, 8f), (36, 2f), (37, -2f), (38, -2f), (39, -2f), (40, 28f), (41, 36f), (42, 26f), (43, 20f), (44, 80f), (45, 119.017f), (46, 130.457f), (47, -14.176f), (48, -4.22f), (49, 20f), (50, 14f), (51, 8f), (52, 2f), (53, -2f), (54, -2f), (55, -2f), (56, -2f), (57, 28f), (58, 28.0f), (59, 28.0f), (60, 28.0f), (61, 20f), (63, 20f), (64, 20f));

    static readonly float[] FF_ROT = F(15f,
        (0, 38.183f), (1, 39.0f), (2, 38.183f), (3, 39.0f), (4, 38.183f), (5, 39.0f), (6, 38.183f), (7, 39.0f), (8, 38.183f), (9, 39.0f), (10, 38.183f), (11, 39.0f), (12, 38.183f), (13, 55.0f), (14, 28.0f), (15, 16.0f), (16, 40f), (17, 40f), (18, 40f), (19, 40f), (20, 55f), (21, 70f), (22, 80f), (23, 80f), (24, 80f), (25, 88f), (26, 95f), (27, 48.30f), (28, 48.30f), (29, 15f), (30, 62f), (31, 15f), (32, 25f), (33, 71.49f), (34, 10f), (35, 5f), (36, 1f), (37, -1f), (38, -1f), (39, -1f), (40, 20f), (41, 26f), (42, 18f), (43, 15f), (44, 55f), (45, 31.084f), (46, 33.459f), (47, 37.447f), (48, 48.30f), (49, 15f), (50, 10f), (51, 5f), (52, 1f), (53, -1f), (54, -1f), (55, -1f), (56, -1f), (57, 20f), (58, 20.0f), (59, 20.0f), (60, 20.0f), (61, 15f), (63, 15f), (64, 15f));

    // ── SB: Buff1=-27.94, 들기=크게, 세로=임팩트 ─────────────────
    static readonly float[] SB_ROT = F(0f,
        (0, -27.936f), (1, -29.0f), (2, -27.936f), (3, -29.0f), (4, -27.936f), (5, -29.0f), (6, -27.936f), (7, -29.0f), (8, -27.936f), (9, -29.0f), (10, -27.936f), (11, -29.0f), (12, -27.936f), (13, 20.0f), (14, 8.0f), (15, 2.0f), (16, 50f), (17, 50f), (18, 50f), (19, 50f), (20, 90f), (21, 130f), (22, 165f), (23, 175f), (24, 180f), (25, 155.0f), (26, 140.0f), (27, -10.97f), (28, -10.97f), (29, 0.0f), (30, 80f), (31, 105f), (32, 125f), (33, -30f), (34, -15.0f), (35, -40.0f), (36, -60.0f), (37, -72.0f), (38, -72.0f), (39, -72.0f), (40, 52.0f), (41, 88.0f), (42, 60.0f), (43, 0.0f), (44, 65.0f), (45, 90f), (46, 110f), (47, -20f), (48, -10.97f), (49, 0.0f), (50, -18.0f), (51, -45.0f), (52, -65.0f), (53, -80.0f), (54, -80.0f), (55, -80.0f), (56, -80.0f), (57, 60.0f), (58, 60.0f), (59, 60.0f), (60, 60.0f), (61, 0f), (63, 0.0f));

    static readonly float[] FB_ROT = F(0f,
        (0, 135.138f), (1, 136.0f), (2, 135.138f), (3, 136.0f), (4, 135.138f), (5, 136.0f), (6, 135.138f), (7, 136.0f), (8, 135.138f), (9, 136.0f), (10, 135.138f), (11, 136.0f), (12, 135.138f), (13, 120.0f), (14, 125.0f), (15, 130.0f), (16, 30f), (17, 30f), (18, 30f), (19, 30f), (20, 55f), (21, 80f), (22, 100f), (23, 108f), (24, 112f), (25, 82.0f), (26, 76.0f), (27, 12.12f), (28, 12.12f), (29, 0.0f), (30, 48f), (31, 65f), (32, 78f), (33, 0f), (34, -8.0f), (35, -24.0f), (36, -36.0f), (37, -44.0f), (38, -44.0f), (39, -44.0f), (40, 30.0f), (41, 56.0f), (42, 38.0f), (43, 0.0f), (44, 40.0f), (45, 58f), (46, 72f), (47, 0f), (48, 12.12f), (49, 0.0f), (50, -10.0f), (51, -28.0f), (52, -42.0f), (53, -52.0f), (54, -52.0f), (55, -52.0f), (56, -52.0f), (57, 36.0f), (58, 36.0f), (59, 36.0f), (60, 36.0f), (61, 0f), (63, 0.0f));

    static readonly float[] HB_ROT = F(0f,
        (0, 0.0f), (1, 0.0f), (2, 0.0f), (3, 0.0f), (4, 0.0f), (5, 0.0f), (6, 0.0f), (7, 0.0f), (8, 0.0f), (9, 0.0f), (10, 0.0f), (11, 0.0f), (12, 0.0f), (13, -2.0f), (14, -1.0f), (15, 0.0f), (16, 0f), (17, 0f), (18, 0f), (19, 0f), (20, -4f), (21, -8f), (22, -10f), (23, -10f), (24, -10f), (25, -14f), (26, -18f), (27, 10f), (28, 10f), (29, 0f), (30, -10f), (31, -14f), (32, -18f), (33, -15f), (34, 8f), (35, 20f), (36, 30f), (37, 38f), (38, 38f), (39, 38f), (40, 0f), (41, -25f), (42, -15f), (43, 0f), (44, -10f), (45, -14f), (46, -18f), (47, 0f), (48, 12f), (49, 0f), (50, 10f), (51, 24f), (52, 34f), (53, 42f), (54, 42f), (55, 42f), (56, 42f), (57, -18f), (58, -18.0f), (59, -18.0f), (60, -18.0f), (61, 0f), (63, 0f), (64, 0f));

    // ── WP rot/scale ──────────────────────────────────────────────
    static readonly float[] WP_ROT = F(-90f,
        (34, 50f), (35, 95f), (36, 120f), (37, 140f), (38, 140f), (39, 140f), (40, -155f), (41, -140f), (42, -140f), (43, -90f), (50, 50f), (51, 95f), (52, 120f), (53, 140f), (54, 140f), (55, 140f), (56, 140f), (57, -145f), (58, -145.0f), (59, -145.0f), (60, -145.0f), (61, -90f));

    static readonly float[] WP_SCALE_X = F(1f,
        (36, 0.6f), (37, 0.3f), (38, 0.05f), (39, 0.05f), (40, 0.05f), (41, 0.05f), (42, 0.3f), (43, 1.0f), (52, 0.6f), (53, 0.3f), (54, 0.05f), (55, 0.05f), (56, 0.05f), (57, -0.2f), (58, -0.2f), (59, -0.2f), (60, -0.2f), (61, 0.6f));

    static readonly float[] WP_SCALE_Y = F(1f,
        (58, 1f), (59, 1f), (60, 1.0f), (61, 1f));

    // ── 다리 ──────────────────────────────────────────────────────
    static readonly float[] TF_ROT = F(0f,
        (0, -6.0f), (12, -6.0f), (13, 2.0f), (14, 5.0f), (15, 4.0f), (16, -6.0f), (26, -10.0f), (27, -10.0f), (30, 6.0f), (32, 0.0f), (34, -6f), (35, -6f), (36, -6f), (37, -6f), (38, -8f), (39, -8f), (40, 4f), (41, 6f), (42, 4f), (43, 0f), (44, -4f), (45, -6f), (46, -6f), (47, -4f), (48, 0f), (49, -4f), (50, -6f), (51, -8f), (52, -8f), (53, -8f), (54, -8f), (55, 5f), (56, 6f), (57, 4f), (58, 4.0f), (59, 4.0f), (60, 4.0f), (61, 0f));

    static readonly float[] CF_ROT = F(0f,
        (0, 4.0f), (12, 4.0f), (13, -2.0f), (14, -4.0f), (15, -2.0f), (16, 4.0f), (26, 6.0f), (27, 6.0f), (30, -4.0f), (32, 0.0f), (34, 4f), (35, 4f), (36, 4f), (37, 4f), (38, 5f), (39, 5f), (40, -3f), (41, -4f), (42, -3f), (43, 0f), (44, 3f), (45, 4f), (46, 4f), (47, 2f), (48, 0f), (49, 3f), (50, 4f), (51, 5f), (52, 5f), (53, 5f), (54, 5f), (55, -3f), (56, -4f), (57, -2f), (58, -2.0f), (59, -2.0f), (60, -2.0f), (61, 0f));

    static readonly float[] TB_ROT = F(0f,
        (0, -5.0f), (12, -5.0f), (13, 3.0f), (14, 6.0f), (15, 4.0f), (16, -5.0f), (26, -8.0f), (27, -8.0f), (30, 6.0f), (32, 0.0f), (34, 5f), (35, 5f), (36, 5f), (37, 5f), (38, 6f), (39, 6f), (40, -3f), (41, -4f), (42, -3f), (43, 0f), (44, -4f), (45, -6f), (46, -6f), (47, -3f), (48, 0f), (49, 3f), (50, 5f), (51, 7f), (52, 7f), (53, 7f), (54, 7f), (55, -4f), (56, -5f), (57, -3f), (58, -3.0f), (59, -3.0f), (60, -3.0f), (61, 0f));

    static readonly float[] CB_ROT = F(0f,
        (0, 4.0f), (12, 4.0f), (13, -2.0f), (14, -4.0f), (15, -2.0f), (16, 4.0f), (26, 5.0f), (27, 5.0f), (30, -3.0f), (32, 0.0f), (34, -3f), (35, -3f), (36, -3f), (37, -3f), (38, -4f), (39, -4f), (40, 2f), (41, 3f), (42, 2f), (43, 0f), (44, 3f), (45, 4f), (46, 4f), (47, 2f), (48, 0f), (49, -2f), (50, -3f), (51, -4f), (52, -4f), (53, -4f), (54, -4f), (55, 3f), (56, 3f), (57, 2f), (58, 2.0f), (59, 2.0f), (60, 2.0f), (61, 0f));

    [MenuItem("Tools/1/MainBoss 1/Generate Attack5")]
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
        C(BOTTOM, "localEulerAngles.z", BOT_ROT);
        L(SF, "localPosition.x", -0.38f); L(SF, "localPosition.y", 0.34f); L(SF, "localPosition.z", 0f);
        CLinear(SF, "localEulerAngles.z", SF_ROT); C(FF, "localEulerAngles.z", FF_ROT);
        L(SB, "localPosition.x", 0.26f); L(SB, "localPosition.y", 0.55f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", SB_ROT); C(FB, "localEulerAngles.z", FB_ROT); C(HB, "localEulerAngles.z", HB_ROT);
        CLinear(WP, "localEulerAngles.z", WP_ROT); CLinear(WP, "localScale.x", WP_SCALE_X); CLinear(WP, "localScale.y", WP_SCALE_Y);
        L(WP, "localScale.z", 1f);
        CLinear(THIGH_F, "localEulerAngles.z", TF_ROT); CLinear(CALF_F, "localEulerAngles.z", CF_ROT);
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