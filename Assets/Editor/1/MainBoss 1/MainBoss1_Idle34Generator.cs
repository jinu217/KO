using UnityEngine;
using UnityEditor;
using System.IO;

public class MainBoss1_Idle34Generator
{
    const string SAVE_PATH = "Assets/Animations/MainBoss 1";
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
    const string FOOT_F = "Body/Bottom/Thigh_F/Calf_F/Foot_F";
    const string THIGH_B = "Body/Bottom/Thigh_B";
    const string CALF_B = "Body/Bottom/Thigh_B/Calf_B";
    const string FOOT_B = "Body/Bottom/Thigh_B/Calf_B/Foot_B";


    // ── 공통 숨쉬기 배열 (32프레임) ─────────────────────────────────
    // Body Y: 천천히 올라갔다 내려옴 (들숨 길게, 날숨 짧게)
    // 어깨 Y: Body보다 2프레임 늦게 따라옴 (자연스러운 연동)
    // Bottom Y: Body Y 역보정 → 발 지면 고정
    // 어깨가 올라갈 때 가슴도 펴지는 느낌으로 SF/SB Y도 살짝
    // 숨쉬기: 16프레임 1사이클을 32프레임에 2번 반복
    static readonly float[] BREATH_BODY_Y = {
        0f,    0.010f, 0.020f, 0.026f, 0.028f, 0.028f, 0.024f, 0.018f,
        0.012f,0.006f, 0.002f, 0f,    -0.002f,-0.001f, 0f,     0f,
        0f,    0.010f, 0.020f, 0.026f, 0.028f, 0.028f, 0.024f, 0.018f,
        0.012f,0.006f, 0.002f, 0f,    -0.002f,-0.001f, 0f,     0f };

    static readonly float[] BREATH_BOT_Y_OFFSET = {
        0f,   -0.010f,-0.020f,-0.026f,-0.028f,-0.028f,-0.024f,-0.018f,
       -0.012f,-0.006f,-0.002f,0f,     0.002f, 0.001f, 0f,     0f,
        0f,   -0.010f,-0.020f,-0.026f,-0.028f,-0.028f,-0.024f,-0.018f,
       -0.012f,-0.006f,-0.002f,0f,     0.002f, 0.001f, 0f,     0f };

    // SF/SB Y: 1프레임 늦게
    static readonly float[] BREATH_SF_Y = {
        0f,    0f,     0.010f, 0.020f, 0.026f, 0.028f, 0.026f, 0.020f,
        0.014f,0.007f, 0.002f, 0f,    -0.001f, 0f,     0f,     0f,
        0f,    0f,     0.010f, 0.020f, 0.026f, 0.028f, 0.026f, 0.020f,
        0.014f,0.007f, 0.002f, 0f,    -0.001f, 0f,     0f,     0f };

    static readonly float[] BREATH_SB_Y = {
        0f,    0f,     0.010f, 0.020f, 0.026f, 0.028f, 0.026f, 0.020f,
        0.014f,0.007f, 0.002f, 0f,    -0.001f, 0f,     0f,     0f,
        0f,    0f,     0.010f, 0.020f, 0.026f, 0.028f, 0.026f, 0.020f,
        0.014f,0.007f, 0.002f, 0f,    -0.001f, 0f,     0f,     0f };

    // rot 규칙: 음수 = 앞으로 굽힘, 양수 = 뒤로 젖힘
    //
    // 양손 검 자세 기준 (실측):
    //   SF pos(-0.38, 0.34) rot +20 / FF rot +15
    //   SB pos(0.26, 0.55)  rot  0  / FB rot  0
    //   HB rot 0 / Weapon rot -90
    //
    // Body 직속 자식(Head, Bottom, SF, SB)에 Body rot 반대값 상쇄
    // Bottom = 고간, 상쇄하면 하체 월드 기준 제자리

    // ================================================================
    // Idle3: 몸 뒤로 젖힘(+20)
    //   Body +20 / Head 0+(-20)=-20 / Bottom -20
    //   SF +20+(-20)=0  / FF +15 / SB 0+(-20)=-20 / FB 0
    // ================================================================

    static float[] F(float b, params (int i, float v)[] o)
    {
        var a = new float[N];
        for (int i = 0; i < N; i++) a[i] = b;
        foreach (var (i, v) in o) if (i < N) a[i] = v;
        return a;
    }

    static readonly float[] I3_BODY_ROT = F(20f);

    static readonly float[] I3_HEAD_ROT = F(-20f,
        (4, -33f), (5, -23f), (6, -20f),
        (12, -6f), (13, -16f), (14, -20f),
        (20, -34f), (21, -23f), (22, -20f),
        (28, -4f), (29, -13f), (30, -20f));

    static readonly float[] I3_HEAD_X = F(0f,
        (4, -0.05f), (5, -0.02f),
        (12, 0.03f), (13, 0.01f),
        (20, -0.07f), (21, -0.03f),
        (28, 0.04f), (29, 0.01f));

    static readonly float[] I3_BOT_ROT = F(-20f);
    static readonly float[] I3_SF_ROT = F(0.76f);   // 실측
    static readonly float[] I3_FF_ROT = F(19.28f);  // 실측
    static readonly float[] I3_SB_ROT = F(-20f);
    static readonly float[] I3_FB_ROT = F(0f);
    static readonly float[] I3_HB_ROT = F(0f);
    static readonly float[] I3_WP_ROT = F(-90f);
    static readonly float[] I3_TF_ROT = F(0f);
    static readonly float[] I3_CF_ROT = F(0f);
    static readonly float[] I3_TB_ROT = F(0f);
    static readonly float[] I3_CB_ROT = F(0f);

    // Foot_F: 갑작스럽게 탁 들었다 빠르게 내림, 여유 없는 느낌
    // Foot_F: 각도 15로 줄임, 급격하게
    static readonly float[] I3_FTF_ROT = F(0f,
        (3, 13f), (4, 15f), (5, 4f), (6, 0f),
        (14, 12f), (15, 15f), (16, 2f),
        (22, 2f), (23, 13f), (24, 15f), (25, 2f));

    // Foot_B: 엇박, 15 기준
    static readonly float[] I3_FTB_ROT = F(0f,
        (8, 12f), (9, 15f), (10, 3f),
        (17, 15f), (18, 13f), (19, 2f),
        (28, 4f), (29, 13f), (30, 15f), (31, 2f));

    // ================================================================
    // Idle4: 앞으로 굽음(-30), f0 기준 실측 (2026-07-22)
    //   Body -30 / Head -2+30=+28 / Bottom +30
    //   SF(-0.38,0.34) rot+20+30=+50 / FF +15
    //   SB rot 0+30=+30 / FB 0 / HB 0 / Weapon -90
    // ================================================================

    static readonly float[] I4_BODY_ROT = F(-30f);

    static readonly float[] I4_HEAD_ROT = F(28f,
        (0, 28f), (1, 34f), (2, 40f), (3, 46f), (4, 52f), (5, 56f), (6, 58f),
        (7, 58f), (8, 58f),
        (9, 52f), (10, 44f), (11, 38f), (12, 33f),
        (13, 38f), (14, 48f),
        (15, 48f),
        (16, 44f),
        (17, 50f), (18, 56f), (19, 60f),
        (20, 53f), (21, 44f), (22, 36f), (23, 28f),
        (24, 24f), (25, 24f),
        (26, 18f), (27, 10f),
        (28, 16f), (29, 22f), (30, 28f),
        (31, 32f));

    static readonly float[] I4_HEAD_X = F(0f,
        (4, -0.04f), (5, -0.06f), (6, -0.07f),
        (9, -0.05f), (12, -0.02f),
        (14, -0.05f), (19, -0.08f),
        (23, -0.03f), (27, 0.04f), (30, -0.01f));

    static readonly float[] I4_BOT_ROT = F(30f);
    static readonly float[] I4_SF_ROT = F(47.44f);  // 실측
    static readonly float[] I4_FF_ROT = F(-29.16f); // 실측
    static readonly float[] I4_SB_ROT = F(-16.59f); // 실측
    static readonly float[] I4_FB_ROT = F(32.41f);  // 실측
    static readonly float[] I4_HB_ROT = F(0f);
    static readonly float[] I4_WP_ROT = F(-90f);
    static readonly float[] I4_TF_ROT = F(0f);
    static readonly float[] I4_CF_ROT = F(0f);
    static readonly float[] I4_TB_ROT = F(0f);
    static readonly float[] I4_CB_ROT = F(0f);

    [MenuItem("Tools/MainBoss 1/Generate Idle3 (Leaning Back)")]
    public static void GenerateIdle3() => Build("MainBoss1_Idle3",
        I3_BODY_ROT, I3_HEAD_ROT, I3_HEAD_X, I3_BOT_ROT,
        I3_SF_ROT, I3_FF_ROT, I3_SB_ROT, I3_FB_ROT, I3_HB_ROT, I3_WP_ROT,
        I3_TF_ROT, I3_CF_ROT, I3_TB_ROT, I3_CB_ROT,
        I3_FTF_ROT, I3_FTB_ROT,
        sfPosX: -0.50f, sfPosY: 0.50f);

    [MenuItem("Tools/MainBoss 1/Generate Idle4 (Hunched Over)")]
    public static void GenerateIdle4() => Build("MainBoss1_Idle4",
        I4_BODY_ROT, I4_HEAD_ROT, I4_HEAD_X, I4_BOT_ROT,
        I4_SF_ROT, I4_FF_ROT, I4_SB_ROT, I4_FB_ROT, I4_HB_ROT, I4_WP_ROT,
        I4_TF_ROT, I4_CF_ROT, I4_TB_ROT, I4_CB_ROT,
        null, null,
        sfPosX: -0.38f, sfPosY: 0.34f);

    static void Build(string clipName,
        float[] bodyRot, float[] headRot, float[] headX, float[] botRot,
        float[] sfRot, float[] ffRot, float[] sbRot, float[] fbRot,
        float[] hbRot, float[] wpRot,
        float[] tfRot, float[] cfRot, float[] tbRot, float[] cbRot,
        float[] ftfRot, float[] ftbRot,
        float sfPosX = -0.38f, float sfPosY = 0.34f)
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

        // 숨쉬기: Body Y 올라갈 때 Bottom Y 반대, 어깨 Y 따라옴
        float[] botYFull = new float[N];
        float[] sfYFull = new float[N];
        float[] sbYFull = new float[N];
        for (int i = 0; i < N; i++)
        {
            botYFull[i] = -0.30f + BREATH_BOT_Y_OFFSET[i];
            sfYFull[i] = sfPosY + BREATH_SF_Y[i];
            sbYFull[i] = 0.55f + BREATH_SB_Y[i];
        }

        L(BODY, "localPosition.x", 0f);
        C(BODY, "localPosition.y", BREATH_BODY_Y);
        L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", bodyRot);

        C(HEAD, "localPosition.x", headX);
        L(HEAD, "localPosition.y", 0.70f);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", headRot);

        L(BOTTOM, "localPosition.x", -0.20f);
        C(BOTTOM, "localPosition.y", botYFull);
        L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", botRot);

        L(SF, "localPosition.x", sfPosX);
        C(SF, "localPosition.y", sfYFull);
        L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", sfRot);
        C(FF, "localEulerAngles.z", ffRot);

        L(SB, "localPosition.x", 0.26f);
        C(SB, "localPosition.y", sbYFull);
        L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", sbRot);
        C(FB, "localEulerAngles.z", fbRot);
        C(HB, "localEulerAngles.z", hbRot);
        C(WP, "localEulerAngles.z", wpRot);

        C(THIGH_F, "localEulerAngles.z", tfRot);
        C(CALF_F, "localEulerAngles.z", cfRot);
        C(THIGH_B, "localEulerAngles.z", tbRot);
        C(CALF_B, "localEulerAngles.z", cbRot);

        if (ftfRot != null) C(FOOT_F, "localEulerAngles.z", ftfRot);
        if (ftbRot != null) C(FOOT_B, "localEulerAngles.z", ftbRot);

        string assetPath = $"{SAVE_PATH}/{clipName}.anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null) { EditorUtility.CopySerialized(clip, existing); existing.name = clipName; AssetDatabase.SaveAssets(); }
        else { AssetDatabase.CreateAsset(clip, assetPath); AssetDatabase.SaveAssets(); }
        AssetDatabase.Refresh();
        Debug.Log($"[MainBoss 1] {clipName} -> {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}