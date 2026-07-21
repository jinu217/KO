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
    const string THIGH_B = "Body/Bottom/Thigh_B";
    const string CALF_B = "Body/Bottom/Thigh_B/Calf_B";

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
    static readonly float[] I3_SF_ROT = F(-1f);   // 실측
    static readonly float[] I3_FF_ROT = F(20f);   // 실측
    static readonly float[] I3_SB_ROT = F(-20f);  // 0-20
    static readonly float[] I3_FB_ROT = F(0f);
    static readonly float[] I3_HB_ROT = F(0f);
    static readonly float[] I3_WP_ROT = F(-90f);
    static readonly float[] I3_TF_ROT = F(0f);
    static readonly float[] I3_CF_ROT = F(0f);
    static readonly float[] I3_TB_ROT = F(0f);
    static readonly float[] I3_CB_ROT = F(0f);

    // ================================================================
    // Idle4: 앞으로 굽음(-30)
    //   Body -30 / Head 0+30=+30 / Bottom +30
    //   SF +20+30=+50 / FF +15 / SB 0+30=+30 / FB 0
    // ================================================================

    static readonly float[] I4_BODY_ROT = F(-30f);

    static readonly float[] I4_HEAD_ROT = F(30f,
        (3, 18f), (4, 4f), (5, 14f), (6, 24f), (7, 30f),
        (13, 18f), (14, 2f), (15, 12f), (16, 22f), (17, 30f),
        (22, 20f), (23, 8f), (24, 16f), (25, 24f), (26, 30f),
        (30, 16f), (31, 24f));

    static readonly float[] I4_HEAD_X = F(0f,
        (4, -0.06f), (5, -0.03f), (6, -0.01f),
        (14, -0.08f), (15, -0.04f), (16, -0.01f),
        (23, -0.05f), (24, -0.02f));

    static readonly float[] I4_BOT_ROT = F(30f);
    static readonly float[] I4_SF_ROT = F(29f);   // -1+30
    static readonly float[] I4_FF_ROT = F(20f);   // 유지
    static readonly float[] I4_SB_ROT = F(30f);   // 0+30
    static readonly float[] I4_FB_ROT = F(0f);
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
        I3_TF_ROT, I3_CF_ROT, I3_TB_ROT, I3_CB_ROT);

    [MenuItem("Tools/MainBoss 1/Generate Idle4 (Hunched Over)")]
    public static void GenerateIdle4() => Build("MainBoss1_Idle4",
        I4_BODY_ROT, I4_HEAD_ROT, I4_HEAD_X, I4_BOT_ROT,
        I4_SF_ROT, I4_FF_ROT, I4_SB_ROT, I4_FB_ROT, I4_HB_ROT, I4_WP_ROT,
        I4_TF_ROT, I4_CF_ROT, I4_TB_ROT, I4_CB_ROT);

    static void Build(string clipName,
        float[] bodyRot, float[] headRot, float[] headX, float[] botRot,
        float[] sfRot, float[] ffRot, float[] sbRot, float[] fbRot,
        float[] hbRot, float[] wpRot,
        float[] tfRot, float[] cfRot, float[] tbRot, float[] cbRot)
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

        L(BODY, "localPosition.x", 0f);
        L(BODY, "localPosition.y", 0f);
        L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", bodyRot);

        C(HEAD, "localPosition.x", headX);
        L(HEAD, "localPosition.y", 0.70f);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", headRot);

        L(BOTTOM, "localPosition.x", -0.20f);
        L(BOTTOM, "localPosition.y", -0.30f);
        L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", botRot);

        L(SF, "localPosition.x", -0.50f);
        L(SF, "localPosition.y", 0.50f);
        L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", sfRot);
        C(FF, "localEulerAngles.z", ffRot);

        L(SB, "localPosition.x", 0.26f);
        L(SB, "localPosition.y", 0.55f);
        L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", sbRot);
        C(FB, "localEulerAngles.z", fbRot);
        C(HB, "localEulerAngles.z", hbRot);
        C(WP, "localEulerAngles.z", wpRot);

        C(THIGH_F, "localEulerAngles.z", tfRot);
        C(CALF_F, "localEulerAngles.z", cfRot);
        C(THIGH_B, "localEulerAngles.z", tbRot);
        C(CALF_B, "localEulerAngles.z", cbRot);

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