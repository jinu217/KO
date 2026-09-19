using UnityEngine;
using UnityEditor;

public class MainBoss1_Buff1Generator
{
    const string SAVE_PATH = "Assets/Animations/MainBoss 1";
    const string CLIP_NAME = "MainBoss1_Buff1";
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
    const string THIGH_B = "Body/Bottom/Thigh_B";
    const string CALF_B = "Body/Bottom/Thigh_B/Calf_B";

    // ================================================================
    // Buff1: 기 모으기 타입
    //
    // [준비]     f0~f3  : rest에서 칼 앞에 세우기
    // [집중]     f4~f16 : 칼을 세워 앞에 들고 멈춤, WP_SCALE 진동
    //                     몸 살짝 앞으로 숙이며 집중하는 느낌
    // [발동]     f17~f20: 몸 확 펴며 에너지 방출 느낌
    //                     WP_SCALE 크게 → 빠르게 수축
    // [복귀]     f21~f35: rest로
    // ================================================================

    static float[] F(float b, params (int i, float v)[] o)
    {
        var a = new float[TOTAL_FRAMES];
        for (int i = 0; i < TOTAL_FRAMES; i++) a[i] = b;
        foreach (var (i, v) in o) if (i < TOTAL_FRAMES) a[i] = v;
        return a;
    }

    // Body: 집중 때 앞으로 살짝, 발동 때 뒤로 확
    static readonly float[] BODY_ROT = F(0f,
        (13, -10f), (14, -5f), (15, 2f), (16, 4f), (17, 8f), (18, 5f), (19, 2f), (20, 1f), (21, 0f), (0, -17f), (1, -18f), (2, -17f), (3, -18f), (4, -17f), (5, -18f), (6, -17f), (7, -18f), (8, -17f), (9, -18f), (10, -17f), (11, -18f), (12, -17f));

    static readonly float[] HEAD_ROT = F(-2f,
        (13, 8f), (14, 3f), (15, -4f), (16, -5f), (17, -9f), (18, -6f), (19, -3f), (20, -1f), (21, -2f), (0, 15f), (1, 16f), (2, 15f), (3, 16f), (4, 15f), (5, 16f), (6, 15f), (7, 16f), (8, 15f), (9, 16f), (10, 15f), (11, 16f), (12, 15f));

    static readonly float[] BOT_ROT = F(0f,
        (13, 10f), (14, 5f), (15, -2f), (16, -4f), (17, -8f), (18, -5f), (19, -2f), (20, -1f), (21, 0f), (0, 17f), (1, 18f), (2, 17f), (3, 18f), (4, 17f), (5, 18f), (6, 17f), (7, 18f), (8, 17f), (9, 18f), (10, 17f), (11, 18f), (12, 17f));

    static readonly float[] BODY_Y = F(0f,
        (13, -0.02f), (14, 0.02f), (15, 0.04f), (16, 0.04f), (17, 0.02f), (18, 0f), (0, -0.06f), (1, -0.065f), (2, -0.06f), (3, -0.065f), (4, -0.06f), (5, -0.065f), (6, -0.06f), (7, -0.065f), (8, -0.06f), (9, -0.065f), (10, -0.06f), (11, -0.065f), (12, -0.06f));

    // SF: 칼 앞에 세우기 — SF 위로 크게 올려서 검을 앞으로
    static readonly float[] SF_ROT = F(20f,
        (13, 75f), (14, 72f), (15, 70f), (16, 20f), (17, 20f), (18, 20f), (0, 69.036f), (1, 70f), (2, 69.036f), (3, 70f), (4, 69.036f), (5, 70f), (6, 69.036f), (7, 70f), (8, 69.036f), (9, 70f), (10, 69.036f), (11, 70f), (12, 69.036f));

    static readonly float[] FF_ROT = F(15f,
        (13, 55f), (14, 28f), (15, 16f), (16, 15f), (17, 15f), (18, 15f), (0, 38.183f), (1, 39f), (2, 38.183f), (3, 39f), (4, 38.183f), (5, 39f), (6, 38.183f), (7, 39f), (8, 38.183f), (9, 39f), (10, 38.183f), (11, 39f), (12, 38.183f));

    // SB: 칼 앞에 세울 때 SF와 함께
    static readonly float[] SB_ROT = F(0f,
        (13, 20f), (14, 8f), (15, 2f), (16, 0f), (17, 0f), (0, -27.936f), (1, -29f), (2, -27.936f), (3, -29f), (4, -27.936f), (5, -29f), (6, -27.936f), (7, -29f), (8, -27.936f), (9, -29f), (10, -27.936f), (11, -29f), (12, -27.936f));

    static readonly float[] FB_ROT = F(0f,
        (13, 120f), (14, 125f), (15, 130f), (16, 0f), (17, 0f), (0, 135.138f), (1, 136f), (2, 135.138f), (3, 136f), (4, 135.138f), (5, 136f), (6, 135.138f), (7, 136f), (8, 135.138f), (9, 136f), (10, 135.138f), (11, 136f), (12, 135.138f));

    static readonly float[] HB_ROT = F(0f,
        (13, -2f), (14, -1f), (15, 0f), (16, 0f), (17, 0f), (0, 0f), (1, 0f), (2, 0f), (3, 0f), (4, 0f), (5, 0f), (6, 0f), (7, 0f), (8, 0f), (9, 0f), (10, 0f), (11, 0f), (12, 0f));

    // WP: 칼 세우기 (-90수평→0수직)
    // 집중 중 진동, 발동 때 scale 크게
    static readonly float[] WP_ROT = F(-90f);

    static readonly float[] WP_SCALE_X = F(1f);

    static readonly float[] WP_SCALE_Y = F(1f);

    static readonly float[] TF_ROT = F(0f,
        (0, -6.0f), (1, -6.0f), (2, -6.0f), (3, -6.0f), (4, -6.0f), (5, -6.0f), (6, -6.0f), (7, -6.0f), (8, -6.0f), (9, -6.0f), (10, -6.0f), (11, -6.0f), (12, -6.0f), (13, 2f), (14, 5f), (15, 4f), (16, 2f), (17, 0f));

    static readonly float[] CF_ROT = F(0f,
        (0, 4.0f), (1, 4.0f), (2, 4.0f), (3, 4.0f), (4, 4.0f), (5, 4.0f), (6, 4.0f), (7, 4.0f), (8, 4.0f), (9, 4.0f), (10, 4.0f), (11, 4.0f), (12, 4.0f), (13, -2f), (14, -4f), (15, -2f), (16, 0f));

    static readonly float[] TB_ROT = F(0f,
        (0, -5.0f), (1, -5.0f), (2, -5.0f), (3, -5.0f), (4, -5.0f), (5, -5.0f), (6, -5.0f), (7, -5.0f), (8, -5.0f), (9, -5.0f), (10, -5.0f), (11, -5.0f), (12, -5.0f), (13, 3f), (14, 6f), (15, 4f), (16, 2f), (17, 0f));

    static readonly float[] CB_ROT = F(0f,
        (0, 4.0f), (1, 4.0f), (2, 4.0f), (3, 4.0f), (4, 4.0f), (5, 4.0f), (6, 4.0f), (7, 4.0f), (8, 4.0f), (9, 4.0f), (10, 4.0f), (11, 4.0f), (12, 4.0f), (13, -2f), (14, -4f), (15, -2f), (16, 0f));

    [MenuItem("Tools/MainBoss 1/Generate Buff1")]
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