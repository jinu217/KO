using UnityEngine;
using UnityEditor;

public class MainBoss1_Attack4aGenerator
{
    const string SAVE_PATH = "Assets/Animations/MainBoss 1";
    const string CLIP_NAME = "MainBoss1_Attack4a";
    const float FRAME_RATE = 12f;
    const int TOTAL_FRAMES = 25;
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
    // Attack4a: 검집 자세 돌진
    //
    // [시전 준비] f0~f11: 양손 허리춤, 칼 뒤로 쭉 빼기
    //   - 몸 앞으로 살짝 숙이며 무게중심 낮춤
    //   - 양팔 허리춤으로 모으기 (SF/SB 아래로)
    //   - WP rot: 140 (칼끝 뒤향함) 유지
    //   - 시전딜레이: f5~f11 자세 유지하며 진동
    //
    // [돌진] f12~f19: 자세 유지한 채 앞으로 돌진
    //   - Body X 앞으로 이동
    //   - 돌진 중 몸 더 앞으로 숙임
    //
    // [마무리] f20~f27: 돌진 끝, 복귀
    // ================================================================

    static float[] F(float b, params (int i, float v)[] o)
    {
        var a = new float[TOTAL_FRAMES];
        for (int i = 0; i < TOTAL_FRAMES; i++) a[i] = b;
        foreach (var (i, v) in o) if (i < TOTAL_FRAMES) a[i] = v;
        return a;
    }

    // Body: 준비 때 앞으로 살짝 숙임, 돌진 때 더 숙임
    static readonly float[] BODY_ROT = F(0f,
        (0, 0f), (1, -10f), (2, -15f), (3, -18f), (4, -20f),
        (5, -20f), (6, -22f), (7, -20f), (8, -22f), (9, -20f), (10, -22f), (11, -20f), // 진동
        (12, -25f), (13, -28f), (14, -30f), (15, -30f), (16, -30f), (17, -30f), (18, -28f), (19, -25f),
        (20, -18f), (21, -10f), (22, -4f), (23, 0f));

    static readonly float[] HEAD_ROT = F(-2f,
        (0, -2f), (1, 8f), (2, 13f), (3, 16f), (4, 18f),
        (5, 18f), (6, 20f), (7, 18f), (8, 20f), (9, 18f), (10, 20f), (11, 18f),
        (12, 23f), (13, 26f), (14, 28f), (15, 28f), (16, 28f), (17, 28f), (18, 26f), (19, 23f),
        (20, 16f), (21, 8f), (22, 2f), (23, -2f));

    static readonly float[] BOT_ROT = F(0f,
        (0, 0f), (1, 10f), (2, 15f), (3, 18f), (4, 20f),
        (5, 20f), (6, 22f), (7, 20f), (8, 22f), (9, 20f), (10, 22f), (11, 20f),
        (12, 25f), (13, 28f), (14, 30f), (15, 30f), (16, 30f), (17, 30f), (18, 28f), (19, 25f),
        (20, 18f), (21, 10f), (22, 4f), (23, 0f));

    // Body X: 돌진 이동 (f12~f19)
    static readonly float[] BODY_X = F(0f,
        (0, 0f), (12, 0.1f), (13, 0.3f), (14, 0.6f), (15, 1.0f),
        (16, 1.4f), (17, 1.8f), (18, 2.1f), (19, 2.3f),
        (20, 2.3f), (21, 2.3f), (22, 2.3f), (23, 2.3f),
        (24, 2.3f));

    // Body Y: 준비 때 살짝 내려감 (무게중심)
    static readonly float[] BODY_Y = F(0f,
        (0, 0f), (2, -0.02f), (3, -0.04f), (4, -0.06f),
        (5, -0.06f), (6, -0.07f), (7, -0.06f), (8, -0.07f), (9, -0.06f), (10, -0.07f), (11, -0.06f),
        (12, -0.06f), (13, -0.05f), (14, -0.04f), (15, -0.03f), (16, -0.02f), (17, -0.01f), (18, 0f));

    // SF: 허리춤으로 내리기 (검집 자세)
    // SF rot 양수로 팔이 아래로
    static readonly float[] SF_ROT = F(0f,
        (0, 20f), (1, -20f), (2, -35f), (3, -50f), (4, -60f),
        (5, -60f), (6, -62f), (7, -60f), (8, -62f), (9, -60f), (10, -62f), (11, -60f),
        (12, -60f), (13, -60f), (14, -60f), (15, -60f), (16, -60f), (17, -60f), (18, -60f), (19, -60f),
        (20, -50f), (21, -35f), (22, -18f), (23, -5f), (24, 20f));

    static readonly float[] FF_ROT = F(0f,
        (0, 15f), (1, 12f), (2, 20f), (3, 28f), (4, 35f),
        (5, 35f), (6, 37f), (7, 35f), (8, 37f), (9, 35f), (10, 37f), (11, 35f),
        (12, 35f), (13, 35f), (14, 35f), (15, 35f), (16, 35f), (17, 35f), (18, 35f), (19, 35f),
        (20, 28f), (21, 18f), (22, 8f), (23, 2f), (24, 15f));

    // SB: 허리춤으로 내리며 뒤로 (검집 자세)
    static readonly float[] SB_ROT = F(0f,
        (0, 0f), (1, -30f), (2, -50f), (3, -65f), (4, -75f),
        (5, -75f), (6, -77f), (7, -75f), (8, -77f), (9, -75f), (10, -77f), (11, -75f),
        (12, -75f), (13, -75f), (14, -75f), (15, -75f), (16, -75f), (17, -75f), (18, -75f), (19, -75f),
        (20, -60f), (21, -40f), (22, -20f), (23, -6f), (24, 0f));

    static readonly float[] FB_ROT = F(0f,
        (0, 0f), (1, 18f), (2, 30f), (3, 40f), (4, 48f),
        (5, 48f), (6, 50f), (7, 48f), (8, 50f), (9, 48f), (10, 50f), (11, 48f),
        (12, 48f), (13, 48f), (14, 48f), (15, 48f), (16, 48f), (17, 48f), (18, 48f), (19, 48f),
        (20, 38f), (21, 24f), (22, 12f), (23, 3f), (24, 0f));

    static readonly float[] HB_ROT = F(-20f,
        (0, 0f), (1, -15f), (2, -10f), (3, -6f), (4, -3f),
        (5, -3f), (6, -1f), (7, -3f), (8, -1f), (9, -3f), (10, -1f), (11, -3f),
        (12, -3f), (13, -3f), (14, -3f), (15, -3f), (16, -3f), (17, -3f), (18, -3f), (19, -3f),
        (20, -6f), (21, -10f), (22, -14f), (23, -17f), (24, 0f));

    // WP: 140 (칼끝 뒤) 유지
    static readonly float[] WP_ROT = F(-90f,
        (0, -90f), (1, -50f), (2, 0f), (3, 50f), (4, 95f), (5, 130f),
        (6, 140f), (7, 140f), (8, 140f), (9, 140f), (10, 140f), (11, 140f),
        (12, 140f), (13, 140f), (14, 140f), (15, 140f), (16, 140f), (17, 140f), (18, 140f), (19, 140f),
        (20, 120f), (21, 90f), (22, 40f), (23, -20f), (24, -90f));

    static readonly float[] WP_SCALE_X = F(1f);
    static readonly float[] WP_SCALE_Y = F(1f);

    // 다리: 돌진 자세 (앞으로 달리는 느낌)
    static readonly float[] TF_ROT = F(0f,
        (0, 0f), (3, -5f), (4, -8f),
        (5, -8f), (6, -9f), (7, -8f), (8, -9f), (9, -8f), (10, -9f), (11, -8f),
        (12, -6f), (13, -3f), (14, 0f), (15, 5f), (16, 8f), (17, 10f), (18, 10f), (19, 8f),
        (20, 5f), (21, 2f), (22, 0f));

    static readonly float[] CF_ROT = F(0f,
        (0, 0f), (3, 3f), (4, 5f),
        (5, 5f), (6, 6f), (7, 5f), (8, 6f), (9, 5f), (10, 6f), (11, 5f),
        (12, 4f), (13, 2f), (14, 0f), (15, -3f), (16, -5f), (17, -6f), (18, -6f), (19, -5f),
        (20, -3f), (21, -1f), (22, 0f));

    static readonly float[] TB_ROT = F(0f,
        (0, 0f), (3, 4f), (4, 6f),
        (5, 6f), (6, 7f), (7, 6f), (8, 7f), (9, 6f), (10, 7f), (11, 6f),
        (12, 5f), (13, 3f), (14, 0f), (15, -4f), (16, -6f), (17, -8f), (18, -8f), (19, -6f),
        (20, -4f), (21, -2f), (22, 0f));

    static readonly float[] CB_ROT = F(0f,
        (0, 0f), (3, -2f), (4, -4f),
        (5, -4f), (6, -5f), (7, -4f), (8, -5f), (9, -4f), (10, -5f), (11, -4f),
        (12, -3f), (13, -1f), (14, 0f), (15, 2f), (16, 4f), (17, 5f), (18, 5f), (19, 4f),
        (20, 2f), (21, 1f), (22, 0f));

    [MenuItem("Tools/1/MainBoss 1/Generate Attack4a")]
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

        C(BODY, "localPosition.x", BODY_X);
        C(BODY, "localPosition.y", BODY_Y);
        L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", BODY_ROT);
        L(HEAD, "localPosition.x", 0f); L(HEAD, "localPosition.y", 0.70f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT);
        L(BOTTOM, "localPosition.x", -0.20f); L(BOTTOM, "localPosition.y", -0.30f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", BOT_ROT);
        L(SF, "localPosition.x", -0.38f); L(SF, "localPosition.y", 0.34f); L(SF, "localPosition.z", 0f);
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