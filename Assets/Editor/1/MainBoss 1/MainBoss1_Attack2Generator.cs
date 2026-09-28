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
        (0, -8.0f), (1, -14.0f), (2, -18.0f), (3, -20.0f), (4, -20.0f), (5, -16.0f), (6, -10.0f), (7, -4.0f), (9, 8.0f), (10, 14.0f), (11, 18.0f), (12, 20.0f), (13, 20.0f), (14, 16.0f), (15, 10.0f), (16, 4.0f), (17, -2.0f), (18, -8.0f), (19, -16.0f), (20, -22.0f), (21, -26.0f), (22, -28.0f), (23, -28.0f), (24, 20.0f), (25, 30.0f), (26, 24.0f), (27, 16.0f), (28, 10.0f), (29, 4.0f), (30, 1.0f));

    // ── SF: 양손으로 검 잡음 — SB와 함께 움직임 ─────────────────────
    // 1타 예비: 왼쪽 위로 (SF도 같이)
    // 1타 임팩트: 오른쪽 아래로 따라감
    // 2타: 반대
    static readonly float[] SF_ROT = F(20f,
        (0, 129.96f), (1, 137.54f), (2, 120.69f), (3, -5.17f), (4, -4.77f), (5, 3.85f), (6, 40.83f), (7, 70.07f), (8, 101.92f), (9, 124.92f), (10, 115.97f), (11, 95.74f), (12, 4.33f), (13, 4.33f), (14, -44.29f), (15, 7.37f), (16, 53.14f), (17, 96.19f), (18, 14.0f), (19, 8.0f), (20, 4.0f), (21, 0.0f), (22, -2.0f), (23, -2.0f), (24, 28.0f), (25, 36.0f), (26, 26.0f), (27, 22.0f), (28, 21.0f));

    static readonly float[] FF_ROT = F(15f,
        (0, -29.43f), (1, 21.08f), (2, 71.21f), (3, 55.72f), (4, 58.48f), (5, 39.07f), (6, -5.75f), (7, -29.85f), (8, -3.05f), (9, 16.04f), (10, 64.06f), (11, 88.28f), (12, 26.6f), (13, 26.6f), (14, 86.02f), (15, 66.52f), (16, 37.31f), (17, -8.54f), (18, 10.0f), (19, 6.0f), (20, 3.0f), (21, 0.0f), (22, -1.0f), (23, -1.0f), (24, 20.0f), (25, 26.0f), (26, 18.0f), (27, 16.0f));

    // ── SB: 양손 검 — SF와 연동 ──────────────────────────────────────
    static readonly float[] SB_ROT = F(0f,
        (0, 79.7f), (1, 108.7f), (2, 130.4f), (3, -21.0f), (4, -21.0f), (5, -10.5f), (7, 12.0f), (8, 43.5f), (9, 79.7f), (10, 108.7f), (11, 130.4f), (12, -21.0f), (13, -21.0f), (14, -10.5f), (15, 7.2f), (16, 29.0f), (17, 50.7f), (18, -12.0f), (19, -30.0f), (20, -50.0f), (21, -65.0f), (22, -72.0f), (23, -72.0f), (24, 52.0f), (25, 88.0f), (26, 68.0f), (27, 45.0f), (28, 24.0f), (29, 10.0f), (30, 2.0f));

    static readonly float[] FB_ROT = F(0f,
        (0, 57.1f), (1, 91.4f), (2, 118.8f), (3, 22.9f), (4, 22.9f), (5, 11.4f), (6, 20.0f), (7, 35.0f), (8, 80.0f), (9, 102.8f), (10, 125.7f), (11, 141.7f), (12, 22.9f), (13, 22.9f), (14, 11.4f), (15, 27.4f), (16, 36.6f), (17, 45.7f), (18, -8.0f), (19, -20.0f), (20, -32.0f), (21, -40.0f), (22, -44.0f), (23, -44.0f), (24, 30.0f), (25, 54.0f), (26, 42.0f), (27, 28.0f), (28, 14.0f), (29, 5.0f), (30, 1.0f));

    // HB: 세로베기 때 자연스럽게, 3타 때 칼 뒤로
    static readonly float[] HB_ROT = F(0f,
        (0, -22.0f), (1, -24.0f), (2, -26.0f), (3, -8f), (4, -8f), (5, -12.0f), (6, -18.0f), (7, -20.0f), (8, -20.0f), (9, -22.0f), (10, -24.0f), (11, -26.0f), (12, -8f), (13, -8f), (14, -12.0f), (15, -18.0f), (16, -20.0f), (17, -20.0f), (18, 12.0f), (19, 34.0f), (20, 60.0f), (21, 79.0f), (23, 10.0f), (24, -15.0f), (25, -20.0f), (26, -15.0f), (27, -8.0f), (28, -5.0f), (29, -2.0f));

    // ── Weapon rot/scale ─────────────────────────────────────────────
    // 세로베기: -90 수평 유지
    // 3타 예비: 140(뒤)
    // 3타 임팩트: -90(수평)
    static readonly float[] WP_ROT = F(-90f,
        // 1타 예비
        (0, -70f), (1, -50f), (2, -30f),
        // 1타 임팩트: 오른쪽 아래
        (3, -120f), (4, -125f),
        // 전환
        (5, -100f), (6, -90f), (7, -85f), (8, -75f),
        // 2타 예비
        (9, -70f), (10, -50f), (11, -30f),
        // 2타 임팩트: -90 유지 (scale로 방향 표현)
        (12, -90f), (13, -90f),
        // 전환
        (14, -90f), (15, -90f), (16, -90f), (17, -90f),
        // 3타
        (18, 50f), (19, 95f), (20, 120f), (21, 135f),
        (22, 180f), (23, 165f),
        (24, -150f), (25, -180f), (26, -165f), (27, -150f));

    static readonly float[] WP_SCALE_X = F(1f,
        // 1타 예비: 살짝 얇아짐
        (2, 0.6f),
        // 1타 임팩트: 뻗어나감
        (3, 1.0f), (4, 1.1f),
        (5, 1.0f),
        // 2타 예비: 얇아짐
        (11, 0.6f),
        // 2타 임팩트: 뻗어나감
        (12, 1.0f), (13, 1.1f),
        // 2타 끝: 카메라 앞으로 빠짐
        (14, 0.05f),
        (15, -0.7f), (16, -1.0f),
        (17, 1.0f),
        // 3타 예비
        (21, 0.6f), (22, 0.3f), (23, 0.05f),
        (24, 0.05f), (25, 0.15f),
        (26, 0.3f), (27, 0.05f),
        (28, -0.6f), (29, -1.0f),
        (30, -0.4f), (31, 1.0f));

    static readonly float[] WP_SCALE_Y = F(1f,
        // 1타 임팩트: 칼날 뻗어나감
        (3, 1.0f), (4, 1.1f), (5, 1.0f), (6, 1.0f),
        // 2타 임팩트
        (12, 1.0f), (13, 1.1f),
        (14, 1.0f), (15, 1.0f), (16, 1.0f), (17, 1.0f),
        // 3타
        (24, 0.35f), (25, -0.15f),
        (26, 1.5f), (27, 1.7f),
        (28, 1.3f), (29, 1.1f), (30, 1.0f));

    // ── 다리 ─────────────────────────────────────────────────────────
    static readonly float[] TF_ROT = F(0f,
        // 1타 세로베기: 체중 앞으로
        (0, -6f), (1, -8f), (2, -10f),
        (3, -14f), (4, -14f),
        (5, -10f), (6, -8f), (7, -6f), (8, -4f),
        // 2타 세로베기
        (9, -6f), (10, -8f), (11, -10f),
        (12, -14f), (13, -14f),
        (14, -10f), (15, -6f), (16, -3f), (17, 0f),
        // 3타 가로베기 예비: 앞발 살짝 뒤로
        (18, -4f), (19, -6f), (20, -8f), (21, -8f), (22, -8f), (23, -8f),
        // 3타 임팩트: 앞발 앞으로
        (24, 6f), (25, 10f), (26, 8f), (27, 5f),
        (28, 2f), (29, 0f), (30, 0f), (31, 0f));

    static readonly float[] CF_ROT = F(0f,
        (0, 4f), (1, 5f), (2, 6f),
        (3, 8f), (4, 8f),
        (5, 6f), (6, 5f), (7, 4f), (8, 3f),
        (9, 4f), (10, 5f), (11, 6f),
        (12, 8f), (13, 8f),
        (14, 6f), (15, 4f), (16, 2f), (17, 0f),
        (18, 2f), (19, 4f), (20, 5f), (21, 5f), (22, 5f), (23, 5f),
        (24, -4f), (25, -6f), (26, -5f), (27, -3f),
        (28, -1f), (29, 0f), (30, 0f), (31, 0f));

    static readonly float[] TB_ROT = F(0f,
        (0, -4f), (1, -5f), (2, -6f),
        (3, -8f), (4, -8f),
        (5, -6f), (6, -4f), (7, -3f), (8, -2f),
        (9, -4f), (10, -5f), (11, -6f),
        (12, -8f), (13, -8f),
        (14, -6f), (15, -4f), (16, -2f), (17, 0f),
        // 3타 예비: 뒷발 뒤로 체중
        (18, 5f), (19, 8f), (20, 10f), (21, 10f), (22, 10f), (23, 10f),
        // 임팩트
        (24, -4f), (25, -6f), (26, -4f), (27, -2f),
        (28, -1f), (29, 0f), (30, 0f), (31, 0f));

    static readonly float[] CB_ROT = F(0f,
        (0, 2f), (1, 3f), (2, 4f),
        (3, 5f), (4, 5f),
        (5, 4f), (6, 3f), (7, 2f), (8, 1f),
        (9, 2f), (10, 3f), (11, 4f),
        (12, 5f), (13, 5f),
        (14, 4f), (15, 2f), (16, 1f), (17, 0f),
        (18, -2f), (19, -4f), (20, -5f), (21, -5f), (22, -5f), (23, -5f),
        (24, 3f), (25, 4f), (26, 3f), (27, 2f),
        (28, 1f), (29, 0f), (30, 0f), (31, 0f));

    [MenuItem("Tools/1/MainBoss 1/Generate Attack2")]
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