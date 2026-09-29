using UnityEngine;
using UnityEditor;
using System.IO;

public class MainBoss1_Attack1Generator
{
    const string SAVE_PATH = "Assets/Animations/MainBoss 1";
    const string CLIP_NAME = "MainBoss1_Attack1";
    const float FRAME_RATE = 12f;
    const int N = 32;
    const float DUR = N / FRAME_RATE;

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
    // 가로베기(f0~f11) → 세로베기(f12~f23) 2연격
    //
    // [가로베기]
    // f0      : 대기
    // f1~f4   : 예비 — SB 몸 앞으로(-50), HB 칼 뒤로 접기(+98)
    // f5~f6   : 임팩트 — SB 펼치기(+62), HB 복귀, scale.y 늘어남
    // f7~f11  : 세로베기 준비 — 몸 앞으로(-10), SB 위로 올라가기 시작
    //
    // [세로베기]
    // f12~f14 : 예비 — SB 위로 더 올림, WP 수직으로 세움
    // f15~f16 : 임팩트 — 내려 찍기, Body 앞으로 확 숙임
    // f17~f23 : 복귀
    //
    // Body rot 시 Head/Bottom/SF/SB 전부 반대 상쇄
    // ================================================================

    static float[] F(float b, params (int i, float v)[] o)
    {
        var a = new float[N];
        for (int i = 0; i < N; i++) a[i] = b;
        foreach (var (i, v) in o) if (i < N) a[i] = v;
        return a;
    }

    // ── Body ─────────────────────────────────────────────────────────
    static readonly float[] BODY_ROT = F(0f,
        // 가로베기
        (1, 12f), (2, 20f), (3, 26f), (4, 30f), (5, 30f), (6, 30f), (7, 30f), (8, 4f),
        (9, -22f), (10, -30f),
        // 전환: 천천히 세로베기 자세로 (-13)
        (11, -26f), (12, -22f), (13, -20f), (14, -18f), (15, -16f), (16, -14f), (17, -13f),
        // 세로베기: f14=실측, 이후 rot 고정
        (18, -13f),
        // 임팩트
        (19, -24f), (20, -32f),
        (21, -22f), (22, -14f), (23, -7f), (24, -2f), (25, 0f), (26, 0f), (27, 0f), (28, 0f), (29, 0f), (30, 0f), (31, 0f));

    // ── Head: Body 반대 상쇄 ─────────────────────────────────────────
    static readonly float[] HEAD_ROT = F(-2f,
        (1, -14f), (2, -22f), (3, -28f), (4, -32f), (5, -32f), (6, -32f), (7, -32f), (8, -6f),
        (9, 20f), (10, 28f),
        (11, 24f), (12, 20f), (13, 18f), (14, 16f), (15, 14f), (16, 12f), (17, 11f),
        (18, 11f),
        (19, 22f), (20, 30f),
        (21, 20f), (22, 12f), (23, 5f), (24, 1f), (25, -2f), (26, -2f), (27, -2f), (28, -2f), (29, -2f), (30, -2f), (31, -2f));

    // ── Bottom: Body 반대 상쇄 ───────────────────────────────────────
    static readonly float[] BOT_ROT = F(0f,
        (1, -12f), (2, -20f), (3, -26f), (4, -30f), (5, -30f), (6, -30f), (7, -30f), (8, -4f),
        (9, 22f), (10, 30f),
        (11, 26f), (12, 22f), (13, 20f), (14, 18f), (15, 16f), (16, 14f), (17, 13f),
        (18, 13f),
        (19, 24f), (20, 32f),
        (21, 22f), (22, 14f), (23, 7f), (24, 2f), (25, 0f), (26, 0f), (27, 0f), (28, 0f), (29, 0f), (30, 0f), (31, 0f));

    // ── SF: Body 상쇄 + 균형 ─────────────────────────────────────────
    static readonly float[] SF_ROT = F(20f,
        (1, 12f), (2, 6f), (3, 2f), (4, -2f), (5, -2f), (6, -2f), (7, -2f), (8, 14f),
        (9, 34f), (10, 42f),
        // 천천히 올라가며 양손으로 모임 (6프레임)
        (11, 55f), (12, 75f), (13, 95f), (14, 115f), (15, 130f), (16, 142f), (17, 149.82f),
        // f14~: rot 고정
        (18, 149.82f),
        (19, -10f), (20, -25f),
        (21, -14f), (22, -4f), (23, 6f), (24, 14f), (25, 18f), (26, 20f), (27, 20f), (28, 20f), (29, 20f), (30, 20f), (31, 20f));

    static readonly float[] FF_ROT = F(15f,
        (1, 8f), (2, 4f), (3, 1f), (4, -2f), (5, -2f), (6, -2f), (7, -2f), (8, 12f),
        (9, 22f), (10, 28f),
        (11, 30f), (12, 24f), (13, 20f), (14, 16f), (15, 13f), (16, 11f), (17, 10.62f),
        (18, 10.62f),
        (19, -5f), (20, -15f),
        (21, -8f), (22, 2f), (23, 8f), (24, 12f), (25, 14f), (26, 15f), (27, 15f), (28, 15f), (29, 15f), (30, 15f), (31, 15f));

    // ── SB: 가로베기 후 위로 올려 세로베기 ──────────────────────────
    static readonly float[] SB_ROT = F(0f,
        (1, -22f), (2, -45f), (3, -62f), (4, -72f), (5, -72f), (6, -72f), (7, -55f), (8, 4f),
        (9, 45f), (10, 80f),
        // 천천히 실측 자세로 (6프레임)
        (11, 82f), (12, 88f), (13, 93f), (14, 98f), (15, 103f), (16, 106f), (17, 107.29f),
        (18, 107.29f),
        (19, -45f), (20, -78f),
        (21, -58f), (22, -35f), (23, -18f), (24, -7f), (25, -1f), (26, 0f), (27, 0f), (28, 0f), (29, 0f), (30, 0f), (31, 0f));

    static readonly float[] FB_ROT = F(0f,
        (1, -14f), (2, -28f), (3, -40f), (4, -48f), (5, -48f), (6, -48f), (7, -38f), (8, -10f),
        (9, 26f), (10, 50f),
        (11, 60f), (12, 70f), (13, 78f), (14, 86f), (15, 94f), (16, 100f), (17, 103.10f),
        (18, 103.10f),
        (19, -30f), (20, -55f),
        (21, -40f), (22, -24f), (23, -11f), (24, -4f), (25, 0f), (26, 0f), (27, 0f), (28, 0f), (29, 0f), (30, 0f), (31, 0f));

    // ── HB: 가로베기 때 칼 접기, 세로베기 때 손목 보정 ──────────────
    static readonly float[] HB_ROT = F(0f,
        (1, 8f), (2, 18f), (3, 28f), (4, 35f), (5, 35f), (6, 35f), (7, 30f), (8, -6f),
        (9, -15f), (10, -25f),
        (11, -18f), (12, -12f), (13, -6f), (14, -3f), (15, -2f),
        (16, -2f),
        (17, 15f), (18, 25f),
        (19, 18f), (20, 10f), (21, 4f), (22, 1f), (23, 0f), (24, 0f), (25, 0f), (26, 0f), (27, 0f), (28, 0f), (29, 0f), (30, 0f), (31, 0f));

    // ── Weapon rot: 수평(-90) 유지 ──────────────────────────────────
    static readonly float[] WP_ROT = F(-90f,
        // 예비: 뒤(140) 고정
        (1, 140f), (2, 140f), (3, 140f), (4, 140f), (5, 140f), (6, 140f), (7, 140f), (8, -90f),
        // 임팩트: 순간 수평으로 전환 (scale로 연출하므로 rot은 점프)
        (9, -90f), (10, -90f),
        (11, -90f), (12, -90f), (13, -90f), (14, -90f), (15, -90f),
        (16, -90f), (17, -90f), (18, -90f),
        (19, -90f), (20, -90f), (21, -90f), (22, -90f), (23, -90f), (24, -90f), (25, -90f), (26, -90f), (27, -90f), (28, -90f), (29, -90f), (30, -90f), (31, -90f));

    // ── Weapon scale.x (두께) ────────────────────────────────────────
    // f3~f4: 예비 — 얇아짐
    // f5: 임팩트 — 두께 복귀
    // f6: 임팩트 직후 — 칼이 카메라 앞 통과하며 빠르게 0으로
    // f7: 거의 0 (옆면만 보임 = 뒤편으로 넘어가는 중)
    // f8~f9: 음수 (뒤편에 있는 상태)
    // f10~f11: 다시 양수로 복귀 (앞으로 나옴)
    static readonly float[] WP_SCALE_X = F(1f,
        // 예비: 칼 두께 살짝 줄어들며 긴장감
        (4, 0.7f), (5, 0.4f), (6, 0.05f), (7, 0.05f), (8, 0.6f),   // 임팩트 직전 거의 0
                                                                   // 임팩트: 수평으로 쭉 나타남
        (9, 1.3f), (10, 1.5f),
        // 통과: 뒤편으로
        (11, 0.3f), (12, 0.05f),
        (13, -0.6f), (14, -1.0f),
        (15, -0.4f), (16, 1.0f),
        // 세로베기 임팩트 직전
        (19, 0.4f), (20, 0.1f),
        (21, 1.0f));

    // ── Weapon scale.y (길이) ────────────────────────────────────────
    static readonly float[] WP_SCALE_Y = F(1f,
        // f7: 베기 중간 — 칼 짧아짐
        (7, 0.35f),
        // 임팩트: 칼 뻗어나감
        (9, 1.4f), (10, 1.6f),
        (11, 1.2f), (12, 1.0f),
        // 세로베기 임팩트
        (19, 1.5f), (20, 1.8f),
        (21, 1.3f), (22, 1.1f), (23, 1.0f));

    // ── 다리 ─────────────────────────────────────────────────────────
    static readonly float[] TF_ROT = F(0f,
        (1, -2f), (2, -4f), (3, -5f), (4, -5f), (5, -5f), (6, -5f), (7, -5f), (8, 2f),
        (9, 6f), (10, 10f),
        (11, 9f), (12, 9f), (13, 9f), (14, 9f), (15, 8f), (16, 8f), (17, 8f),
        (18, 8f),
        (19, 14f), (20, 18f),
        (21, 12f), (22, 6f), (23, 2f), (24, 0f), (25, 0f), (26, 0f), (27, 0f), (28, 0f), (29, 0f), (30, 0f), (31, 0f));

    static readonly float[] CF_ROT = F(0f,
        (1, 1f), (2, 3f), (3, 4f), (4, 4f),
        (9, -4f), (10, -7f),
        (11, -5f), (12, -5f), (13, -5f), (14, -5f), (15, -5f), (16, -5f), (17, -5f),
        (18, -5f),
        (19, -9f), (20, -12f),
        (21, -8f), (22, -4f), (23, -1f), (24, 0f), (25, 0f), (26, 0f), (27, 0f), (28, 0f), (29, 0f), (30, 0f), (31, 0f));

    static readonly float[] TB_ROT = F(0f,
        (1, 4f), (2, 6f), (3, 8f), (4, 8f),
        (9, -4f), (10, -6f),
        (11, -5f), (12, -5f), (13, -5f), (14, -5f), (15, -5f), (16, -5f), (17, -5f),
        (18, -5f),
        (19, -9f), (20, -12f),
        (21, -8f), (22, -4f), (23, -2f), (24, 0f), (25, 0f), (26, 0f), (27, 0f), (28, 0f), (29, 0f), (30, 0f), (31, 0f));

    static readonly float[] CB_ROT = F(0f,
        (1, -2f), (2, -4f), (3, -5f), (4, -5f), (5, -5f), (6, -5f), (7, -5f), (8, 0f),
        (9, 3f), (10, 5f),
        (11, 4f), (12, 4f), (13, 4f), (14, 4f), (15, 4f), (16, 4f), (17, 4f),
        (18, 4f),
        (19, 7f), (20, 9f),
        (21, 6f), (22, 3f), (23, 1f), (24, 0f), (25, 0f), (26, 0f), (27, 0f), (28, 0f), (29, 0f), (30, 0f), (31, 0f));

    [MenuItem("Tools/1/MainBoss 1/Generate Attack1")]
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
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            }
            clip.SetCurve(path, typeof(Transform), prop, curve);
        }

        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop,
                AnimationCurve.Linear(0f, val, DUR, val));

        L(BODY, "localPosition.x", 0f);
        L(BODY, "localPosition.y", 0f);
        L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", BODY_ROT);

        L(HEAD, "localPosition.x", 0f);
        L(HEAD, "localPosition.y", 0.70f);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT);

        L(BOTTOM, "localPosition.x", -0.20f);
        L(BOTTOM, "localPosition.y", -0.30f);
        L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", BOT_ROT);

        L(SF, "localPosition.x", -0.38f);
        L(SF, "localPosition.y", 0.34f);
        L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", SF_ROT);
        C(FF, "localEulerAngles.z", FF_ROT);

        L(SB, "localPosition.x", 0.26f);
        L(SB, "localPosition.y", 0.55f);
        L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", SB_ROT);
        C(FB, "localEulerAngles.z", FB_ROT);
        C(HB, "localEulerAngles.z", HB_ROT);

        C(WP, "localEulerAngles.z", WP_ROT);
        C(WP, "localScale.x", WP_SCALE_X);
        C(WP, "localScale.y", WP_SCALE_Y);
        L(WP, "localScale.z", 1f);

        C(THIGH_F, "localEulerAngles.z", TF_ROT);
        C(CALF_F, "localEulerAngles.z", CF_ROT);
        C(THIGH_B, "localEulerAngles.z", TB_ROT);
        C(CALF_B, "localEulerAngles.z", CB_ROT);

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