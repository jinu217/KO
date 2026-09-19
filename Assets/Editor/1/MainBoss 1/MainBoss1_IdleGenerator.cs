using UnityEngine;
using UnityEditor;
using System.IO;

public class MainBoss1_IdleGenerator
{
    const string SAVE_PATH = "Assets/Animations/MainBoss 1";
    const string CLIP_NAME = "MainBoss1_Idle";
    const float FRAME_RATE = 12f;
    const int N = 36;
    const float DUR = N / FRAME_RATE;

    const string BODY = "Body";
    const string HEAD = "Body/Head";
    const string BOTTOM = "Body/Bottom";
    const string SF = "Body/Shoulder_F";
    const string FF = "Body/Shoulder_F/Forearm_F";
    const string HF = "Body/Shoulder_F/Forearm_F/Hand_F";
    const string SB = "Body/Shoulder_B";
    const string FB = "Body/Shoulder_B/Forearm_B";
    const string HB = "Body/Shoulder_B/Forearm_B/Hand_B";
    const string WP = "Body/Shoulder_B/Forearm_B/Hand_B/Weapon";
    const string THIGH_F = "Body/Bottom/Thigh_F";
    const string CALF_F = "Body/Bottom/Thigh_F/Calf_F";
    const string THIGH_B = "Body/Bottom/Thigh_B";
    const string CALF_B = "Body/Bottom/Thigh_B/Calf_B";

    // ================================================================
    // REST POSE
    // Body rot=0  Head rot=-2  Hand_B rot=-20  Weapon rot=-90
    // Shoulder_F pos(-0.575,0.50)  Shoulder_B pos(0.26,0.55)
    // Bottom pos(-0.20,-0.30)
    //
    // 꼬인 기본자세:
    //   Body +3 / Head -5 / SF +12 / FF -18
    //   Thigh_F/B -3 / Calf_F +5 / Calf_B -4
    //
    // 패턴 구조 (늘어짐 → 탁탁):
    //   [구간A] f0~f9   : SF+FF+Head 천천히 늘어짐
    //   [탁A]   f10~f11 : 탁탁 맞아들어옴
    //   [구간B] f12~f20 : Body+Thigh 천천히 처짐, SB+FB도 흘러내림
    //   [탁B]   f21~f22 : 탁탁
    //   [구간C] f23~f31 : Head+Calf, TB 따로 늘어짐
    //   [탁C]   f32~f33 : 탁탁
    //   f34~f35 : 안정
    //
    // Hand_B rot:
    //   SB+FB 움직임을 역보정해서 Weapon이 항상 검끝 바닥 향하게
    //   Hand_B rot = base(-20) - SB_offset - FB_offset 근사치
    //   (검이 살짝 흔들리는 건 허용, 들리거나 박히는 건 방지)
    // ================================================================

    static float[] F(float b, params (int i, float v)[] o)
    {
        var a = new float[N];
        for (int i = 0; i < N; i++) a[i] = b;
        foreach (var (i, v) in o) if (i < N) a[i] = v;
        return a;
    }

    // ── Body: 구간B에서 천천히 처짐 → 탁B에서 복귀 ──────────────────
    static readonly float[] BODY_ROT = F(3f,
        // 천천히 처짐
        (12, 4f), (13, 5f), (14, 7f), (15, 8f), (16, 10f),
        (17, 11f), (18, 12f), (19, 13f), (20, 14f),
        // 탁탁
        (21, 6f), (22, 3f));

    static readonly float[] BODY_Y = F(0f,
        (12, 0.01f), (13, 0.02f), (14, 0.03f), (15, 0.04f), (16, 0.05f),
        (17, 0.06f), (18, 0.07f), (19, 0.07f), (20, 0.08f),
        (21, 0.03f), (22, 0f));

    // Bottom Y: Body Y 역보정 (발 Y rest 이하 금지)
    static readonly float[] BOT_Y = F(-0.30f,
        (12, -0.31f), (13, -0.32f), (14, -0.33f), (15, -0.34f), (16, -0.35f),
        (17, -0.36f), (18, -0.37f), (19, -0.37f), (20, -0.38f),
        (21, -0.33f), (22, -0.30f));

    // ── Head: 구간A에서 천천히 늘어짐 → 탁A / 구간C에서 또 처짐 → 탁C ─
    static readonly float[] HEAD_ROT = F(-5f,
        // 구간A: 천천히 늘어짐 (고개 점점 숙여짐)
        (0, -5f), (1, -6f), (2, -7f), (3, -9f), (4, -11f),
        (5, -13f), (6, -15f), (7, -17f), (8, -19f), (9, -21f),
        // 탁A: 탁탁 맞아들어옴
        (10, -8f), (11, -5f),
        // 구간C: 다른 방향으로 또 늘어짐
        (23, -5f), (24, -4f), (25, -3f), (26, -1f), (27, 1f),
        (28, 3f), (29, 5f), (30, 7f), (31, 9f),
        // 탁C
        (32, -2f), (33, -5f));

    static readonly float[] HEAD_X = F(0f,
        (3, -0.01f), (4, -0.02f), (5, -0.03f), (6, -0.04f),
        (7, -0.05f), (8, -0.06f), (9, -0.07f),
        (10, -0.02f), (11, 0f),
        (24, 0.01f), (25, 0.02f), (26, 0.03f), (27, 0.04f),
        (28, 0.05f), (29, 0.06f), (30, 0.07f), (31, 0.08f),
        (32, 0.02f), (33, 0f));

    // ── SF+FF+HF: SF는 지금 방향, FF+HF는 f16~f22 반대로 꺾임 ───────
    // SF: 구간B 중반부터 흘러내림 → 탁C
    static readonly float[] SF_ROT = F(12f,
        (16, 11f), (17, 10f), (18, 8f), (19, 6f), (20, 4f),
        (21, 3f), (22, 2f), (23, 1f), (24, 0f), (25, -1f),
        (26, -2f), (27, -3f), (28, -4f), (29, -5f), (30, -6f),
        (31, -7f),
        (32, 5f), (33, 12f));

    // FF: f16~f20 SF 반대방향으로 꺾임(양수), f20 이후 원래 방향으로
    static readonly float[] FF_ROT = F(-18f,
        (16, -14f), (17, -8f), (18, -2f), (19, 6f), (20, 12f),   // 반대로 꺾임
        (21, 8f), (22, 4f),                                 // 탁 후 돌아오기 시작
        (23, -5f), (24, -12f), (25, -18f), (26, -24f),          // 원래 방향으로 늘어짐
        (27, -30f), (28, -36f), (29, -42f), (30, -46f), (31, -49f),
        (32, -30f), (33, -18f));

    // HF: FF랑 연동, f16~f22 같이 꺾임 (FF보다 조금 더 과장)
    static readonly float[] HF_ROT = F(0f,
        (16, 5f), (17, 12f), (18, 20f), (19, 28f), (20, 35f),   // 반대로 꺾임
        (21, 22f), (22, 10f),                               // 탁 후 복귀
        (23, 3f), (24, 0f));

    // ── SB+FB: 구간B에서 Body와 함께 흘러내림 → 탁B ─────────────────
    static readonly float[] SB_ROT = F(0f,
        (12, 0f), (13, -2f), (14, -4f), (15, -6f), (16, -8f),
        (17, -10f), (18, -12f), (19, -14f), (20, -16f),
        // 탁B
        (21, -6f), (22, 0f));

    static readonly float[] FB_ROT = F(0f,
        (12, 0f), (13, 2f), (14, 4f), (15, 6f), (16, 8f),
        (17, 10f), (18, 12f), (19, 13f), (20, 15f),
        // 탁B
        (21, 5f), (22, 0f));

    // Hand_B: SB+FB 역보정으로 Weapon 방향 유지
    // Hand_B rot = -20 - SB_offset * 0.6 - FB_offset * 0.4 (근사치)
    // SB가 음수(-16)면 Hand_B를 양수로, FB가 양수(+15)면 Hand_B를 음수로
    static readonly float[] HB_ROT = F(-20f,
        (12, -20f), (13, -20f), (14, -20f), (15, -19f), (16, -18f),
        (17, -17f), (18, -16f), (19, -15f), (20, -14f),
        (21, -18f), (22, -20f));

    // Weapon: Hand_B 보정으로 거의 고정 (±5 이내)
    static readonly float[] WP_ROT = F(-90f,
        (12, -90f), (13, -91f), (14, -91f), (15, -92f), (16, -92f),
        (17, -92f), (18, -93f), (19, -93f), (20, -93f),
        (21, -91f), (22, -90f));

    // ── Thigh_F: Body와 함께 처짐 보정 + 구간C에서 독립 틱 ──────────
    static readonly float[] TF_ROT = F(-3f,
        (12, -4f), (13, -5f), (14, -7f), (15, -8f), (16, -10f),
        (17, -11f), (18, -12f), (19, -13f), (20, -14f),
        (21, -6f), (22, -3f),
        // 구간C 독립: f25~29 살짝 늘어짐
        (25, -4f), (26, -5f), (27, -6f), (28, -7f), (29, -8f),
        (32, -4f), (33, -3f));

    static readonly float[] CF_ROT = F(5f,
        (12, 6f), (13, 7f), (14, 8f), (15, 9f), (16, 10f),
        (17, 11f), (18, 12f), (19, 12f), (20, 13f),
        (21, 8f), (22, 5f));

    // ── Thigh_B: 구간A 시작부터 혼자 먼저 늘어짐 (f0~f9) → 탁A ─────
    static readonly float[] TB_ROT = F(-3f,
        (0, -3f), (1, -4f), (2, -6f), (3, -7f), (4, -9f),
        (5, -10f), (6, -11f), (7, -12f), (8, -13f), (9, -14f),
        // 탁A (Head 탁이랑 같이)
        (10, -6f), (11, -3f),
        // 구간C에서 또 독립 늘어짐
        (24, -3f), (25, -5f), (26, -7f), (27, -9f), (28, -10f),
        (29, -11f), (30, -12f), (31, -13f),
        (32, -5f), (33, -3f));

    static readonly float[] CB_ROT = F(-4f,
        // 구간B 중반부터 흘러내림
        (15, -5f), (16, -6f), (17, -7f), (18, -8f), (19, -9f), (20, -10f),
        (21, -5f), (22, -4f),
        // 구간C
        (26, -5f), (27, -6f), (28, -7f), (29, -8f), (30, -9f), (31, -10f),
        (32, -5f), (33, -4f));

    [MenuItem("Tools/MainBoss 1/Generate Idle")]
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
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

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
        C(BODY, "localPosition.y", BODY_Y);
        L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", BODY_ROT);

        C(HEAD, "localPosition.x", HEAD_X);
        L(HEAD, "localPosition.y", 0.70f);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT);

        L(BOTTOM, "localPosition.x", -0.20f);
        C(BOTTOM, "localPosition.y", BOT_Y);
        L(BOTTOM, "localPosition.z", 0f);

        L(SF, "localPosition.x", -0.575f);
        L(SF, "localPosition.y", 0.50f);
        L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", SF_ROT);
        C(FF, "localEulerAngles.z", FF_ROT);
        C(HF, "localEulerAngles.z", HF_ROT);

        L(SB, "localPosition.x", 0.26f);
        L(SB, "localPosition.y", 0.55f);
        L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", SB_ROT);
        C(FB, "localEulerAngles.z", FB_ROT);
        C(HB, "localEulerAngles.z", HB_ROT);
        C(WP, "localEulerAngles.z", WP_ROT);

        C(THIGH_F, "localEulerAngles.z", TF_ROT);
        C(CALF_F, "localEulerAngles.z", CF_ROT);
        C(THIGH_B, "localEulerAngles.z", TB_ROT);
        C(CALF_B, "localEulerAngles.z", CB_ROT);

        string assetPath = $"{SAVE_PATH}/{CLIP_NAME}.anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(clip, existing);
            existing.name = CLIP_NAME;
            AssetDatabase.SaveAssets();
        }
        else
        {
            AssetDatabase.CreateAsset(clip, assetPath);
            AssetDatabase.SaveAssets();
        }
        AssetDatabase.Refresh();
        Debug.Log($"[MainBoss 1] {CLIP_NAME} -> {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}