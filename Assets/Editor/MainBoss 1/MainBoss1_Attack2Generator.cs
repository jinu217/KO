using UnityEngine;
using UnityEditor;
using System.IO;

public class MainBoss1_Attack2Generator
{
    const string SAVE_PATH = "Assets/Animations/MainBoss 1";
    const string CLIP_NAME = "MainBoss1_Attack2";
    const float FRAME_RATE = 12f;
    const int TOTAL_FRAMES = 36;
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
    // 십자베기-십자베기-가로베기(넉백) 3연격
    //
    // [1타] f0~f8  : 왼쪽위→오른쪽아래 (Body 오른쪽+로 틀며)
    //   f0~f2  : 예비 — 양팔 왼쪽 위로
    //   f3~f4  : 임팩트 — 오른쪽 아래로 내려베기
    //   f5~f8  : 2타 전환
    //
    // [2타] f9~f17 : 오른쪽위→왼쪽아래 (Body 왼쪽-로 틀며, 1타 반대)
    //   f9~f11 : 예비 — 양팔 오른쪽 위로
    //   f12~f13: 임팩트 — 왼쪽 아래로 내려베기
    //   f14~f17: 3타 전환
    //
    // [3타] f18~f35: 가로베기 넉백
    //   f18~f23: 예비 — 길게 대기
    //   f24~f25: 임팩트
    //   f26~f27: 팔로스루
    //   f28~f35: 복귀
    //
    // Body rot 시 Head/Bottom/SF/SB 전부 반대 상쇄
    // 양손 내내 검 잡음 (SF/FF가 SB/FB와 함께 움직임)
    // ================================================================

    static float[] F(float b, params (int i, float v)[] o)
    {
        var a = new float[TOTAL_FRAMES];
        for (int i = 0; i < TOTAL_FRAMES; i++) a[i] = b;
        foreach (var (i, v) in o) if (i < TOTAL_FRAMES) a[i] = v;
        return a;
    }

    // ── Body ─────────────────────────────────────────────────────────
    // 1타: 오른쪽(+)으로 틀기 → 임팩트 때 왼쪽 아래로 무게 이동
    // 2타: 왼쪽(-)으로 틀기 → 임팩트 때 오른쪽 아래로
    // 3타: 오른쪽(+) 예비 → 왼쪽(-) 임팩트
    static readonly float[] BODY_ROT = F(0f,
        // 1타 예비: 살짝 앞으로(-)
        (0, 8f), (1, 14f), (2, 18f),
        // 1타 임팩트: 실측 -20
        (3, 20f), (4, 20f),
        // 2타 전환: 반대로
        (5, 16f), (6, 10f), (7, 4f), (8, 0f),
        // 2타 예비: 반대(+)
        (9, -8f), (10, -14f), (11, -18f),
        // 2타 임팩트
        (12, -20f), (13, -20f),
        // 3타 전환
        (14, -16f), (15, -10f), (16, -4f), (17, 2f),
        // 3타 예비: 오른쪽 크게
        (18, 8f), (19, 16f), (20, 22f), (21, 26f), (22, 28f), (23, 28f),
        // 3타 임팩트
        (24, -20f), (25, -30f),
        (26, -24f), (27, -16f),
        (28, -10f), (29, -4f), (30, -1f), (31, 0f));

    // ── Head: Body 반대 상쇄 ─────────────────────────────────────────
    static readonly float[] HEAD_ROT = F(-2f,
        (0, -10f), (1, -16f), (2, -20f),
        (3, -22f), (4, -22f),
        (5, -18f), (6, -12f), (7, -6f), (8, -2f),
        (9, 6f), (10, 12f), (11, 16f),
        (12, 18f), (13, 18f),
        (14, 14f), (15, 8f), (16, 2f), (17, -4f),
        (18, -10f), (19, -18f), (20, -24f), (21, -28f), (22, -30f), (23, -30f),
        (24, 18f), (25, 28f),
        (26, 22f), (27, 14f),
        (28, 8f), (29, 2f), (30, -1f), (31, -2f));

    // ── Bottom: Body 반대 상쇄 ───────────────────────────────────────
    static readonly float[] BOT_ROT = F(0f,
        (0, -8f), (1, -14f), (2, -18f),
        (3, -20f), (4, -20f),
        (5, -16f), (6, -10f), (7, -4f), (8, 0f),
        (9, 8f), (10, 14f), (11, 18f),
        (12, 20f), (13, 20f),
        (14, 16f), (15, 10f), (16, 4f), (17, -2f),
        (18, -8f), (19, -16f), (20, -22f), (21, -26f), (22, -28f), (23, -28f),
        (24, 20f), (25, 30f),
        (26, 24f), (27, 16f),
        (28, 10f), (29, 4f), (30, 1f), (31, 0f));

    // ── SF: 양손으로 검 잡음 — SB와 함께 움직임 ─────────────────────
    // 1타 예비: 왼쪽 위로 (SF도 같이)
    // 1타 임팩트: 오른쪽 아래로 따라감
    // 2타: 반대
    static readonly float[] SF_ROT = F(20f,
        // 세로베기: GripPoint IK (OK=정확히 닿음, STRETCH=최대뻗기)
        (0, 126.67f), (1, 137.94f), (2, 144.27f),      // STRETCH
        (3, -45.56f), (4, -45.56f),                    // OK
        (5, -9.82f),                              // OK
        (6, 35.69f), (7, 44.84f), (8, 47.17f),      // STRETCH
        (9, 76.59f), (10, 102.11f), (11, 122.0f),    // STRETCH
        (12, -46.9f), (13, -46.9f),                // OK
        (14, 0.02f),                             // OK
        (15, 57.08f), (16, 84.89f), (17, 109.48f),  // STRETCH
                                                    // 3타
        (18, 14f), (19, 8f), (20, 4f), (21, 0f), (22, -2f), (23, -2f),
        (24, 28f), (25, 36f), (26, 26f), (27, 22f),
        (28, 21f), (29, 20f), (30, 20f), (31, 20f));

    static readonly float[] FF_ROT = F(15f,
        // 세로베기: GripPoint IK 교정값
        (0, -30.64f), (1, -30.63f), (2, -30.6f),
        (3, 46.37f), (4, 46.37f),                  // OK: 손 검에 붙음
        (5, 24.53f),                              // OK
        (6, -29.24f), (7, -29.54f), (8, -29.61f),
        (9, -30.27f), (10, -30.52f), (11, -30.6f),
        (12, 48.03f), (13, 48.03f),                // OK
        (14, 16.22f),                             // OK
        (15, -29.96f), (16, -30.41f), (17, -30.59f),
        // 3타
        (18, 10f), (19, 6f), (20, 3f), (21, 0f), (22, -1f), (23, -1f),
        (24, 20f), (25, 26f), (26, 18f), (27, 16f),
        (28, 15f), (29, 15f), (30, 15f), (31, 15f));

    // ── SB: 양손 검 — SF와 연동 ──────────────────────────────────────
    static readonly float[] SB_ROT = F(0f,
        // 1타 예비: 위로
        (0, 88f), (1, 102f), (2, 110f),
        // 1타 임팩트: 실측
        (3, -47.87f), (4, -47.87f),
        // 2타 전환: 반대쪽 위로
        (5, -25f), (6, -10f), (7, -2f), (8, 0f),
        // 2타 예비
        (9, 30f), (10, 60f), (11, 85f),
        // 2타 임팩트
        (12, -47.87f), (13, -47.87f),
        // 3타 전환
        (14, -20f), (15, 10f), (16, 40f), (17, 68f),
        // 3타 예비
        (18, -12f), (19, -30f), (20, -50f), (21, -65f), (22, -72f), (23, -72f),
        // 3타 임팩트
        (24, 52f), (25, 88f),
        (26, 68f), (27, 45f),
        (28, 24f), (29, 10f), (30, 2f), (31, 0f));

    static readonly float[] FB_ROT = F(0f,
        (0, 32f), (1, 38f), (2, 42f),
        // 임팩트: 실측 FB=0
        (3, 0f), (4, 0f),
        (5, -10f), (6, -4f), (7, -1f), (8, 0f),
        (9, 10f), (10, 20f), (11, 28f),
        (12, 0f), (13, 0f),
        (14, -10f), (15, 0f), (16, 10f), (17, 22f),
        (18, -8f), (19, -20f), (20, -32f), (21, -40f), (22, -44f), (23, -44f),
        (24, 30f), (25, 54f),
        (26, 42f), (27, 28f),
        (28, 14f), (29, 5f), (30, 1f), (31, 0f));

    // HB: 세로베기 때 자연스럽게, 3타 때 칼 뒤로
    static readonly float[] HB_ROT = F(0f,
        (0, -10f), (1, -14f), (2, -18f),
        (3, 12f), (4, 20f),
        (5, 14f), (6, 6f), (7, 1f), (8, 0f),
        (9, -8f), (10, -14f), (11, -18f),
        (12, 10f), (13, 18f),
        (14, 12f), (15, 6f), (16, -2f), (17, -6f),
        (18, 8f), (19, 20f), (20, 30f), (21, 36f), (22, 38f), (23, 38f),
        (24, -15f), (25, -25f),
        (26, -18f), (27, -10f),
        (28, -5f), (29, -2f), (30, 0f), (31, 0f));

    // ── Weapon rot/scale ─────────────────────────────────────────────
    // 세로베기: -90 수평 유지
    // 3타 예비: 140(뒤)
    // 3타 임팩트: -90(수평)
    static readonly float[] WP_ROT = F(-90f,
        (18, 50f), (19, 95f), (20, 120f), (21, 135f), (22, 140f), (23, 140f),
        (24, -90f), (25, -90f), (26, -90f), (27, -90f));

    static readonly float[] WP_SCALE_X = F(1f,
        // 세로베기 임팩트: 살짝 얇아짐
        (3, 0.8f), (4, 0.6f), (5, 1.0f),
        (12, 0.8f), (13, 0.6f), (14, 1.0f),
        // 3타 예비: 얇아짐
        (21, 0.6f), (22, 0.3f), (23, 0.05f),
        // 3타 베기 중간
        (24, 0.05f),
        // 임팩트
        (25, 1.4f),
        (26, 0.3f), (27, 0.05f),
        (28, -0.6f), (29, -1.0f),
        (30, -0.4f), (31, 1.0f));

    static readonly float[] WP_SCALE_Y = F(1f,
        // 세로베기 임팩트: 늘어남
        (3, 1.4f), (4, 1.6f), (5, 1.2f), (6, 1.0f),
        (12, 1.4f), (13, 1.6f), (14, 1.2f), (15, 1.0f),
        // 3타 베기 중간
        (24, 0.35f),
        // 임팩트
        (25, 1.5f), (26, 1.7f),
        (27, 1.3f), (28, 1.1f), (29, 1.0f));

    // ── 다리 ─────────────────────────────────────────────────────────
    static readonly float[] TF_ROT = F(0f,
        // 1타: Body -20 상쇄 → +20, 실측 +18
        (0, 18f), (1, 18f), (2, 18f),
        (3, 22f), (4, 24f),
        (5, 20f), (6, 18f), (7, 10f), (8, 4f),
        // 2타: 반대쪽 체중이동
        (9, 18f), (10, 18f), (11, 18f),
        (12, 22f), (13, 24f),
        (14, 20f), (15, 18f), (16, 18f), (17, 18f),
        // 3타
        (18, -8f), (19, -16f), (20, -22f), (21, -26f), (22, -28f), (23, -28f),
        (24, 10f), (25, 14f),
        (26, 10f), (27, 6f),
        (28, 3f), (29, 1f), (30, 0f), (31, 0f));

    static readonly float[] CF_ROT = F(0f,
        // 실측 Calf -8
        (0, -8f), (1, -8f), (2, -8f),
        (3, -10f), (4, -12f),
        (5, -9f), (6, -8f), (7, -4f), (8, -1f),
        (9, -8f), (10, -8f), (11, -8f),
        (12, -10f), (13, -12f),
        (14, -9f), (15, -8f), (16, -8f), (17, -8f),
        (18, 3f), (19, 6f), (20, 9f), (21, 10f), (22, 11f), (23, 11f),
        (24, -5f), (25, -8f),
        (26, -5f), (27, -3f),
        (28, -1f), (29, 0f), (30, 0f), (31, 0f));

    static readonly float[] TB_ROT = F(0f,
        (0, 18f), (1, 18f), (2, 18f),
        (3, 22f), (4, 24f),
        (5, 20f), (6, 18f), (7, 10f), (8, 4f),
        (9, 18f), (10, 18f), (11, 18f),
        (12, 22f), (13, 24f),
        (14, 20f), (15, 18f), (16, 18f), (17, 18f),
        (18, -8f), (19, -16f), (20, -22f), (21, -26f), (22, -28f), (23, -28f),
        (24, 10f), (25, 14f),
        (26, 10f), (27, 6f),
        (28, 3f), (29, 1f), (30, 0f), (31, 0f));

    static readonly float[] CB_ROT = F(0f,
        (0, -8f), (1, -8f), (2, -8f),
        (3, -10f), (4, -12f),
        (5, -9f), (6, -8f), (7, -4f), (8, -1f),
        (9, -8f), (10, -8f), (11, -8f),
        (12, -10f), (13, -12f),
        (14, -9f), (15, -8f), (16, -8f), (17, -8f),
        (18, 3f), (19, 6f), (20, 9f), (21, 10f), (22, 11f), (23, 11f),
        (24, -5f), (25, -8f),
        (26, -5f), (27, -3f),
        (28, -1f), (29, 0f), (30, 0f), (31, 0f));

    [MenuItem("Tools/MainBoss 1/Generate Attack2")]
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
            var keys = new Keyframe[TOTAL_FRAMES + 1];
            for (int i = 0; i < TOTAL_FRAMES; i++) keys[i] = new Keyframe(i / FRAME_RATE, vals[i]);
            keys[TOTAL_FRAMES] = new Keyframe(DUR, vals[0]);
            var curve = new AnimationCurve(keys);
            for (int i = 0; i < curve.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            }
            clip.SetCurve(path, typeof(Transform), prop, curve);
        }

        // 특정 프레임 구간을 Linear로 강제 (가로베기 임팩트 구간 등)
        void CWithLinear(string path, string prop, float[] vals, params int[] linearFrames)
        {
            var keys = new Keyframe[TOTAL_FRAMES + 1];
            for (int i = 0; i < TOTAL_FRAMES; i++) keys[i] = new Keyframe(i / FRAME_RATE, vals[i]);
            keys[TOTAL_FRAMES] = new Keyframe(DUR, vals[0]);
            var curve = new AnimationCurve(keys);
            var linearSet = new System.Collections.Generic.HashSet<int>(linearFrames);
            for (int i = 0; i < curve.keys.Length; i++)
            {
                if (linearSet.Contains(i) || (i > 0 && linearSet.Contains(i - 1)))
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                }
                else
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
                    AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
                }
            }
            clip.SetCurve(path, typeof(Transform), prop, curve);
        }

        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop,
                AnimationCurve.Linear(0f, val, DUR, val));

        L(BODY, "localPosition.x", 0f);
        L(BODY, "localPosition.y", 0f);
        L(BODY, "localPosition.z", 0f);
        CWithLinear(BODY, "localEulerAngles.z", BODY_ROT, 23, 24, 25, 26);

        L(HEAD, "localPosition.x", 0f);
        L(HEAD, "localPosition.y", 0.70f);
        L(HEAD, "localPosition.z", 0f);
        CWithLinear(HEAD, "localEulerAngles.z", HEAD_ROT, 23, 24, 25, 26);

        L(BOTTOM, "localPosition.x", -0.20f);
        L(BOTTOM, "localPosition.y", -0.30f);
        L(BOTTOM, "localPosition.z", 0f);
        CWithLinear(BOTTOM, "localEulerAngles.z", BOT_ROT, 23, 24, 25, 26);

        L(SF, "localPosition.x", -0.38f);
        L(SF, "localPosition.y", 0.34f);
        L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", SF_ROT);
        C(FF, "localEulerAngles.z", FF_ROT);

        L(SB, "localPosition.x", 0.26f);
        L(SB, "localPosition.y", 0.55f);
        L(SB, "localPosition.z", 0f);
        // 가로베기 임팩트(f23~f26): Linear로 직선 움직임
        CWithLinear(SB, "localEulerAngles.z", SB_ROT, 23, 24, 25, 26);
        CWithLinear(FB, "localEulerAngles.z", FB_ROT, 23, 24, 25, 26);
        CWithLinear(HB, "localEulerAngles.z", HB_ROT, 23, 24, 25, 26);

        CWithLinear(WP, "localEulerAngles.z", WP_ROT, 23, 24, 25, 26);
        CWithLinear(WP, "localScale.x", WP_SCALE_X, 23, 24, 25, 26);
        CWithLinear(WP, "localScale.y", WP_SCALE_Y, 23, 24, 25, 26);
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