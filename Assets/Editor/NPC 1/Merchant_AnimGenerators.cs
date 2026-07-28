using UnityEngine;
using UnityEditor;

public class Merchant_AnimGenerators
{
    const string SAVE_PATH = "Assets/Animations/Merchant";
    const float FRAME_RATE = 12f;
    const float DUR_IDLE = 48 / 12f;
    const float DUR_NOD = 24 / 12f;
    const float DUR_WAVE = 48 / 12f;

    const string BODY = "Body";
    const string HEAD = "Body/Head";
    const string SF = "Body/Shoulder_F";
    const string HF = "Body/Shoulder_F/Hand_F";
    const string SB = "Body/Shoulder_B";
    const string HB = "Body/Shoulder_B/Hand_B";
    const string BOTTOM = "Body/Bottom";
    const string FOOT_F = "Body/Bottom/Foot_F";
    const string FOOT_B = "Body/Bottom/Foot_B";

    // ================================================================
    // Merchant rest 실측값:
    //   Body pos(0,0), Head pos(-0.091, 0.413)
    //   SF pos(-0.308, 0.355), HF pos(-0.079, -0.283)
    //   SB pos(0.160, 0.354), HB pos(0.015, -0.264)
    //   Bottom pos(0, -0.614), Foot_F pos(-0.145, 0), Foot_B pos(0.168, 0)
    // ================================================================

    // ── Idle 배열 (48프레임) ────────────────────────────────────────
    static readonly float[] BODY_Y = { 0.0f, 0.0049f, 0.0097f, 0.0144f, 0.0187f, 0.0228f, 0.0265f, 0.0298f, 0.0325f, 0.0346f, 0.0362f, 0.0372f, 0.0375f, 0.0372f, 0.0362f, 0.0346f, 0.0325f, 0.0298f, 0.0265f, 0.0228f, 0.0187f, 0.0144f, 0.0097f, 0.0049f, 0.0f, -0.0049f, -0.0097f, -0.0144f, -0.0187f, -0.0228f, -0.0265f, -0.0298f, -0.0325f, -0.0346f, -0.0362f, -0.0372f, -0.0375f, -0.0372f, -0.0362f, -0.0346f, -0.0325f, -0.0298f, -0.0265f, -0.0228f, -0.0188f, -0.0144f, -0.0097f, -0.0049f };
    static readonly float[] HEAD_Y = { 0.413f, 0.4154f, 0.4179f, 0.4202f, 0.4224f, 0.4244f, 0.4263f, 0.4279f, 0.4292f, 0.4303f, 0.4311f, 0.4316f, 0.4317f, 0.4316f, 0.4311f, 0.4303f, 0.4292f, 0.4279f, 0.4263f, 0.4244f, 0.4224f, 0.4202f, 0.4179f, 0.4154f, 0.413f, 0.4106f, 0.4081f, 0.4058f, 0.4036f, 0.4016f, 0.3997f, 0.3981f, 0.3968f, 0.3957f, 0.3949f, 0.3944f, 0.3942f, 0.3944f, 0.3949f, 0.3957f, 0.3968f, 0.3981f, 0.3997f, 0.4016f, 0.4036f, 0.4058f, 0.4081f, 0.4106f };
    static readonly float[] SF_Y = { 0.3436f, 0.3446f, 0.3457f, 0.347f, 0.3484f, 0.35f, 0.3516f, 0.3533f, 0.355f, 0.3567f, 0.3584f, 0.36f, 0.3616f, 0.363f, 0.3643f, 0.3654f, 0.3664f, 0.3671f, 0.3677f, 0.368f, 0.3681f, 0.368f, 0.3677f, 0.3671f, 0.3664f, 0.3654f, 0.3643f, 0.363f, 0.3616f, 0.36f, 0.3584f, 0.3567f, 0.355f, 0.3533f, 0.3516f, 0.35f, 0.3484f, 0.347f, 0.3457f, 0.3446f, 0.3436f, 0.3429f, 0.3423f, 0.342f, 0.3419f, 0.342f, 0.3423f, 0.3429f };
    static readonly float[] SB_Y = { 0.3426f, 0.3436f, 0.3447f, 0.346f, 0.3474f, 0.349f, 0.3506f, 0.3523f, 0.354f, 0.3557f, 0.3574f, 0.359f, 0.3606f, 0.362f, 0.3633f, 0.3644f, 0.3654f, 0.3661f, 0.3667f, 0.367f, 0.3671f, 0.367f, 0.3667f, 0.3661f, 0.3654f, 0.3644f, 0.3633f, 0.362f, 0.3606f, 0.359f, 0.3574f, 0.3557f, 0.354f, 0.3523f, 0.3506f, 0.349f, 0.3474f, 0.346f, 0.3447f, 0.3436f, 0.3426f, 0.3419f, 0.3413f, 0.341f, 0.3409f, 0.341f, 0.3413f, 0.3419f };
    static readonly float[] BOTTOM_Y = { -0.614f, -0.6189f, -0.6237f, -0.6284f, -0.6327f, -0.6368f, -0.6405f, -0.6438f, -0.6465f, -0.6486f, -0.6502f, -0.6512f, -0.6515f, -0.6512f, -0.6502f, -0.6486f, -0.6465f, -0.6438f, -0.6405f, -0.6368f, -0.6327f, -0.6284f, -0.6237f, -0.6189f, -0.614f, -0.6091f, -0.6043f, -0.5996f, -0.5953f, -0.5912f, -0.5875f, -0.5842f, -0.5815f, -0.5794f, -0.5778f, -0.5768f, -0.5765f, -0.5768f, -0.5778f, -0.5794f, -0.5815f, -0.5842f, -0.5875f, -0.5912f, -0.5952f, -0.5996f, -0.6043f, -0.6091f };


    // ── Idle 전용 배열 (48프레임) ───────────────────────────────────
    static readonly float[] IDLE_BODY_Y = { 0.0f, 0.0049f, 0.0097f, 0.0144f, 0.0187f, 0.0228f, 0.0265f, 0.0298f, 0.0325f, 0.0346f, 0.0362f, 0.0372f, 0.0375f, 0.0372f, 0.0362f, 0.0346f, 0.0325f, 0.0298f, 0.0265f, 0.0228f, 0.0187f, 0.0144f, 0.0097f, 0.0049f, 0.0f, -0.0049f, -0.0097f, -0.0144f, -0.0187f, -0.0228f, -0.0265f, -0.0298f, -0.0325f, -0.0346f, -0.0362f, -0.0372f, -0.0375f, -0.0372f, -0.0362f, -0.0346f, -0.0325f, -0.0298f, -0.0265f, -0.0228f, -0.0188f, -0.0144f, -0.0097f, -0.0049f };
    static readonly float[] IDLE_HEAD_Y = { 0.413f, 0.4154f, 0.4179f, 0.4202f, 0.4224f, 0.4244f, 0.4263f, 0.4279f, 0.4292f, 0.4303f, 0.4311f, 0.4316f, 0.4317f, 0.4316f, 0.4311f, 0.4303f, 0.4292f, 0.4279f, 0.4263f, 0.4244f, 0.4224f, 0.4202f, 0.4179f, 0.4154f, 0.413f, 0.4106f, 0.4081f, 0.4058f, 0.4036f, 0.4016f, 0.3997f, 0.3981f, 0.3968f, 0.3957f, 0.3949f, 0.3944f, 0.3942f, 0.3944f, 0.3949f, 0.3957f, 0.3968f, 0.3981f, 0.3997f, 0.4016f, 0.4036f, 0.4058f, 0.4081f, 0.4106f };
    static readonly float[] IDLE_SF_Y = { 0.3436f, 0.3446f, 0.3457f, 0.347f, 0.3484f, 0.35f, 0.3516f, 0.3533f, 0.355f, 0.3567f, 0.3584f, 0.36f, 0.3616f, 0.363f, 0.3643f, 0.3654f, 0.3664f, 0.3671f, 0.3677f, 0.368f, 0.3681f, 0.368f, 0.3677f, 0.3671f, 0.3664f, 0.3654f, 0.3643f, 0.363f, 0.3616f, 0.36f, 0.3584f, 0.3567f, 0.355f, 0.3533f, 0.3516f, 0.35f, 0.3484f, 0.347f, 0.3457f, 0.3446f, 0.3436f, 0.3429f, 0.3423f, 0.342f, 0.3419f, 0.342f, 0.3423f, 0.3429f };
    static readonly float[] IDLE_SB_Y = { 0.3426f, 0.3436f, 0.3447f, 0.346f, 0.3474f, 0.349f, 0.3506f, 0.3523f, 0.354f, 0.3557f, 0.3574f, 0.359f, 0.3606f, 0.362f, 0.3633f, 0.3644f, 0.3654f, 0.3661f, 0.3667f, 0.367f, 0.3671f, 0.367f, 0.3667f, 0.3661f, 0.3654f, 0.3644f, 0.3633f, 0.362f, 0.3606f, 0.359f, 0.3574f, 0.3557f, 0.354f, 0.3523f, 0.3506f, 0.349f, 0.3474f, 0.346f, 0.3447f, 0.3436f, 0.3426f, 0.3419f, 0.3413f, 0.341f, 0.3409f, 0.341f, 0.3413f, 0.3419f };
    static readonly float[] IDLE_BOTTOM_Y = { -0.614f, -0.6189f, -0.6237f, -0.6284f, -0.6327f, -0.6368f, -0.6405f, -0.6438f, -0.6465f, -0.6486f, -0.6502f, -0.6512f, -0.6515f, -0.6512f, -0.6502f, -0.6486f, -0.6465f, -0.6438f, -0.6405f, -0.6368f, -0.6327f, -0.6284f, -0.6237f, -0.6189f, -0.614f, -0.6091f, -0.6043f, -0.5996f, -0.5953f, -0.5912f, -0.5875f, -0.5842f, -0.5815f, -0.5794f, -0.5778f, -0.5768f, -0.5765f, -0.5768f, -0.5778f, -0.5794f, -0.5815f, -0.5842f, -0.5875f, -0.5912f, -0.5952f, -0.5996f, -0.6043f, -0.6091f };
    // ── Nod 배열 (24프레임) ─────────────────────────────────────────
    static float[] FNod(float b, params (int i, float v)[] o)
    {
        var a = new float[48];
        for (int i = 0; i < 48; i++) a[i] = b;
        foreach (var (i, v) in o) if (i < 48) a[i] = v;
        return a;
    }

    static readonly float[] NOD_HEAD_ROT = FNod(0f,
        (2, 8f), (3, 18f), (4, 12f), (5, 3f), (6, 0f),
        (8, 6f), (9, 12f), (10, 7f), (11, 2f), (12, 0f));

    static readonly float[] NOD_HEAD_Y = FNod(0.413f,
        (3, 0.407f), (4, 0.410f), (5, 0.413f),
        (9, 0.408f), (10, 0.411f), (11, 0.413f), (23, 0.413f));

    // ── Wave 배열 (48프레임) ────────────────────────────────────────
    static readonly float[] WAVE_SB_ROT = { 0.0f, 25.0f, 50.0f, 75.0f, 100.0f, 107.07f, 110.0f, 107.07f, 100.0f, 92.93f, 90.0f, 92.93f, 100.0f, 107.07f, 110.0f, 107.07f, 100.0f, 92.93f, 90.0f, 72.0f, 54.0f, 36.0f, 18.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] WAVE_HB_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 27.5f, 39.87f, 45.0f, 39.87f, 27.5f, 15.13f, 10.0f, 15.13f, 27.5f, 39.87f, 45.0f, 39.87f, 27.5f, 15.13f, 10.0f, 6.67f, 3.33f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] WAVE_HEAD_ROT = { 0.0f, 2.0f, 4.0f, 6.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 5.33f, 2.67f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

    // ================================================================
    // Generate Idle
    // ================================================================
    [MenuItem("Tools/Merchant/Generate Idle")]
    public static void GenerateIdle()
    {
        var clip = MakeClip("Merchant_Idle", 48, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 48);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_NOD, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", IDLE_BODY_Y); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);
        L(HEAD, "localPosition.x", -0.091f); C(HEAD, "localPosition.y", IDLE_HEAD_Y); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);
        L(SF, "localPosition.x", -0.308f); C(SF, "localPosition.y", IDLE_SF_Y); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.079f); L(HF, "localPosition.y", -0.283f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.160f); C(SB, "localPosition.y", IDLE_SB_Y); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.015f); L(HB, "localPosition.y", -0.264f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(BOTTOM, "localPosition.x", 0f); C(BOTTOM, "localPosition.y", IDLE_BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.145f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.168f); L(FOOT_B, "localPosition.y", 0f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

        Save(clip, "Merchant_Idle");
    }

    // ================================================================
    // Generate Nod
    // ================================================================
    [MenuItem("Tools/Merchant/Generate IdleNod")]
    public static void GenerateIdleNod()
    {
        var clip = MakeClip("Merchant_IdleNod", 48, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 48);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_IDLE, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", BODY_Y); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);
        L(HEAD, "localPosition.x", -0.091f); C(HEAD, "localPosition.y", NOD_HEAD_Y); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", NOD_HEAD_ROT);
        L(SF, "localPosition.x", -0.308f); C(SF, "localPosition.y", SF_Y); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.079f); L(HF, "localPosition.y", -0.283f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.160f); C(SB, "localPosition.y", SB_Y); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.015f); L(HB, "localPosition.y", -0.264f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(BOTTOM, "localPosition.x", 0f); C(BOTTOM, "localPosition.y", BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.145f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.168f); L(FOOT_B, "localPosition.y", 0f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

        Save(clip, "Merchant_IdleNod");
    }

    // ================================================================
    // Generate Wave
    // ================================================================
    [MenuItem("Tools/Merchant/Generate IdleWave")]
    public static void GenerateIdleWave()
    {
        var clip = MakeClip("Merchant_IdleWave", 48, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 48);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_WAVE, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", BODY_Y); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);
        L(HEAD, "localPosition.x", -0.091f); C(HEAD, "localPosition.y", HEAD_Y); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", WAVE_HEAD_ROT);
        L(SF, "localPosition.x", -0.308f); C(SF, "localPosition.y", SF_Y); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.079f); L(HF, "localPosition.y", -0.283f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.160f); C(SB, "localPosition.y", SB_Y); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", WAVE_SB_ROT);
        L(HB, "localPosition.x", 0.015f); L(HB, "localPosition.y", -0.264f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", WAVE_HB_ROT);
        L(BOTTOM, "localPosition.x", 0f); C(BOTTOM, "localPosition.y", BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.145f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.168f); L(FOOT_B, "localPosition.y", 0f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

        Save(clip, "Merchant_IdleWave");
    }

    // ── 공통 헬퍼 ───────────────────────────────────────────────────
    static AnimationClip MakeClip(string name, int totalFrames, bool loop)
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "Merchant");
        }
        var clip = new AnimationClip { frameRate = FRAME_RATE };
        clip.name = name;
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, s);
        return clip;
    }

    static void SetCurve(AnimationClip clip, string path, string prop, float[] vals, int total)
    {
        var keys = new Keyframe[total];
        for (int i = 0; i < total; i++) keys[i] = new Keyframe(i / FRAME_RATE, vals[i]);
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < curve.keys.Length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
        }
        clip.SetCurve(path, typeof(Transform), prop, curve);
    }

    static void Save(AnimationClip clip, string name)
    {
        string assetPath = $"{SAVE_PATH}/{name}.anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null) { EditorUtility.CopySerialized(clip, existing); existing.name = name; AssetDatabase.SaveAssets(); }
        else { AssetDatabase.CreateAsset(clip, assetPath); AssetDatabase.SaveAssets(); }
        AssetDatabase.Refresh();
        Debug.Log($"[Merchant] {name} → {assetPath}");
    }

    [MenuItem("Tools/Merchant/Generate Wave")]
    public static void GenerateWave()
    {
        var clip = MakeClip("Merchant_Wave", 48, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 48);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_WAVE, val));

        L(BODY, "localPosition.x", 0f); L(BODY, "localPosition.y", 0f); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);
        L(HEAD, "localPosition.x", -0.091f); L(HEAD, "localPosition.y", 0.413f); L(HEAD, "localPosition.z", 0f);
        var waveHeadKeys = new Keyframe[48];
        float[] waveHeadDown = { 0.0f, -2.0f, -4.0f, -6.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -5.33f, -2.67f, -0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
        for (int i = 0; i < 48; i++) waveHeadKeys[i] = new Keyframe(i / FRAME_RATE, waveHeadDown[i]);
        clip.SetCurve(HEAD, typeof(Transform), "localEulerAngles.z", new AnimationCurve(waveHeadKeys));
        L(SF, "localPosition.x", -0.308f); L(SF, "localPosition.y", 0.355f); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.079f); L(HF, "localPosition.y", -0.283f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.160f); L(SB, "localPosition.y", 0.354f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", WAVE_SB_ROT);
        L(HB, "localPosition.x", 0.015f); L(HB, "localPosition.y", -0.264f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", WAVE_HB_ROT);
        L(BOTTOM, "localPosition.x", 0f); L(BOTTOM, "localPosition.y", -0.614f); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.145f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.168f); L(FOOT_B, "localPosition.y", 0f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

        Save(clip, "Merchant_Wave");
    }
}