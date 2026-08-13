using UnityEngine;
using UnityEditor;

public class SpeedMob2_AnimGenerators
{
    const string SAVE_PATH = "Assets/Animations/SpeedMob 2";
    const float FRAME_RATE = 12f;
    const float DUR_48 = 48 / 12f;
    const float DUR_32 = 32 / 12f;
    const float DUR_8 = 8 / 12f;

    const string BODY = "Body";
    const string HEAD = "Body/Head";
    const string SF = "Body/Shoulder_F";
    const string HF = "Body/Shoulder_F/Hand_F";
    const string SB = "Body/Shoulder_B";
    const string HB = "Body/Shoulder_B/Hand_B";
    const string WEAPON = "Body/Shoulder_B/Hand_B/Weapon";
    const string BOTTOM = "Body/Bottom";
    const string FOOT_F = "Body/Bottom/Foot_F";
    const string FOOT_B = "Body/Bottom/Foot_B";

    // ================================================================
    // rest 실측값:
    //   Body(0,0), Head(-0.096, 0.398)
    //   SF(-0.460, 0.418), HF(-0.062, -0.338)
    //   SB(0.134, 0.418), HB(0.062, -0.306)
    //   Sword(0.134, -0.224) rot=-60
    //   Bottom(-0.125, -0.496), Foot_F(-0.249, 0), Foot_B(0.173, 0)
    // ================================================================

    // ── Idle 배열 (48프레임) ────────────────────────────────────────
    static readonly float[] IDLE_BODY_Y = { 0.0f, 0.0065f, 0.0129f, 0.0191f, 0.025f, 0.0304f, 0.0354f, 0.0397f, 0.0433f, 0.0462f, 0.0483f, 0.0496f, 0.05f, 0.0496f, 0.0483f, 0.0462f, 0.0433f, 0.0397f, 0.0354f, 0.0304f, 0.025f, 0.0191f, 0.0129f, 0.0065f, 0.0f, -0.0065f, -0.0129f, -0.0191f, -0.025f, -0.0304f, -0.0354f, -0.0397f, -0.0433f, -0.0462f, -0.0483f, -0.0496f, -0.05f, -0.0496f, -0.0483f, -0.0462f, -0.0433f, -0.0397f, -0.0354f, -0.0304f, -0.025f, -0.0191f, -0.0129f, -0.0065f };
    static readonly float[] IDLE_SF_Y = { 0.4028f, 0.4041f, 0.4056f, 0.4073f, 0.4093f, 0.4113f, 0.4135f, 0.4157f, 0.418f, 0.4203f, 0.4225f, 0.4247f, 0.4267f, 0.4287f, 0.4304f, 0.4319f, 0.4332f, 0.4342f, 0.4349f, 0.4354f, 0.4355f, 0.4354f, 0.4349f, 0.4342f, 0.4332f, 0.4319f, 0.4304f, 0.4287f, 0.4267f, 0.4247f, 0.4225f, 0.4203f, 0.418f, 0.4157f, 0.4135f, 0.4113f, 0.4093f, 0.4073f, 0.4056f, 0.4041f, 0.4028f, 0.4018f, 0.4011f, 0.4006f, 0.4005f, 0.4006f, 0.4011f, 0.4018f };
    static readonly float[] IDLE_SB_Y = { 0.4028f, 0.4041f, 0.4056f, 0.4073f, 0.4093f, 0.4113f, 0.4135f, 0.4157f, 0.418f, 0.4203f, 0.4225f, 0.4247f, 0.4267f, 0.4287f, 0.4304f, 0.4319f, 0.4332f, 0.4342f, 0.4349f, 0.4354f, 0.4355f, 0.4354f, 0.4349f, 0.4342f, 0.4332f, 0.4319f, 0.4304f, 0.4287f, 0.4267f, 0.4247f, 0.4225f, 0.4203f, 0.418f, 0.4157f, 0.4135f, 0.4113f, 0.4093f, 0.4073f, 0.4056f, 0.4041f, 0.4028f, 0.4018f, 0.4011f, 0.4006f, 0.4005f, 0.4006f, 0.4011f, 0.4018f };
    static readonly float[] IDLE_BOTTOM_Y = { -0.496f, -0.5025f, -0.5089f, -0.5151f, -0.521f, -0.5264f, -0.5314f, -0.5357f, -0.5393f, -0.5422f, -0.5443f, -0.5456f, -0.546f, -0.5456f, -0.5443f, -0.5422f, -0.5393f, -0.5357f, -0.5314f, -0.5264f, -0.521f, -0.5151f, -0.5089f, -0.5025f, -0.496f, -0.4895f, -0.4831f, -0.4769f, -0.471f, -0.4656f, -0.4606f, -0.4563f, -0.4527f, -0.4498f, -0.4477f, -0.4464f, -0.446f, -0.4464f, -0.4477f, -0.4498f, -0.4527f, -0.4563f, -0.4606f, -0.4656f, -0.471f, -0.4769f, -0.4831f, -0.4895f };

    // ── Walk 배열 (32프레임) ────────────────────────────────────────
    static readonly float[] WALK_FF_X = { -0.249f, -0.257f, -0.264f, -0.272f, -0.279f, -0.286f, -0.294f, -0.301f, -0.309f, -0.301f, -0.294f, -0.286f, -0.279f, -0.272f, -0.264f, -0.257f };
    static readonly float[] WALK_FF_Y = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0191f, 0.0354f, 0.0462f, 0.05f, 0.0462f, 0.0354f, 0.0191f };
    static readonly float[] WALK_FB_X = { 0.113f, 0.12f, 0.128f, 0.136f, 0.143f, 0.15f, 0.158f, 0.166f, 0.173f, 0.165f, 0.158f, 0.15f, 0.143f, 0.135f, 0.128f, 0.12f };
    static readonly float[] WALK_FB_Y = { 0.0f, 0.0191f, 0.0354f, 0.0462f, 0.05f, 0.0462f, 0.0354f, 0.0191f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] WALK_BODY_ROT = { -0.0f, -1.53f, -2.83f, -3.7f, -4.0f, -3.7f, -2.83f, -1.53f, -0.0f, 1.53f, 2.83f, 3.7f, 4.0f, 3.7f, 2.83f, 1.53f };
    static readonly float[] WALK_BOT_ROT = { 0.0f, 1.53f, 2.83f, 3.7f, 4.0f, 3.7f, 2.83f, 1.53f, 0.0f, -1.53f, -2.83f, -3.7f, -4.0f, -3.7f, -2.83f, -1.53f };
    static readonly float[] WALK_BODY_Y = { 0.0f, 0.0057f, 0.008f, 0.0057f, 0.0f, 0.0057f, 0.008f, 0.0057f, 0.0f, 0.0057f, 0.008f, 0.0057f, 0.0f, 0.0057f, 0.008f, 0.0057f };

    // ── Run 배열 (8프레임) ──────────────────────────────────────────
    static readonly float[] RUN_FF_X = { -0.249f, -0.322f, -0.396f, -0.469f, -0.396f, -0.322f };
    static readonly float[] RUN_FF_Y = { 0.0f, 0.0f, 0.0f, 0.0f, 0.1386f, 0.1386f };
    static readonly float[] RUN_FB_X = { -0.047f, 0.026f, 0.1f, 0.173f, 0.1f, 0.026f };
    static readonly float[] RUN_FB_Y = { 0.0f, 0.1386f, 0.1386f, 0.0f, 0.0f, 0.0f };
    static readonly float[] RUN_BODY_ROT = { -10.0f, -7.4f, -7.4f, -10.0f, -12.6f, -12.6f };
    static readonly float[] RUN_BOT_ROT = { 10.0f, 7.4f, 7.4f, 10.0f, 12.6f, 12.6f };

    // ── Attack 배열 (20프레임) ──────────────────────────────────────
    // 숏소드 슬래시: SB 올리고 빠르게 내려치기, 임팩트 -25도 오버슈트
    static readonly float[] ATK_SB_ROT = { 0.0f, -10.0f, -20.0f, 2.5f, 25.0f, 25.0f, 16.67f, 8.33f, 0.0f };
    static readonly float[] ATK_HB_ROT = { 0.0f, -10.0f, -20.0f, 2.5f, 25.0f, 25.0f, 16.67f, 8.33f, 0.0f };
    static readonly float[] ATK_SF_ROT = { 0.0f, 5.0f, 10.0f, 0.0f, -10.0f, -8.0f, -5.33f, -2.67f, 0.0f };
    static readonly float[] ATK_HF_ROT = { 0.0f, 4.0f, 8.0f, 0.0f, -8.0f, -6.0f, -4.0f, -2.0f, 0.0f };
    static readonly float[] ATK_BODY_ROT = { 0.0f, 5.0f, 10.0f, -2.5f, -15.0f, -12.0f, -8.0f, -4.0f, 0.0f };
    static readonly float[] ATK_BOT_ROT = { -0.0f, -5.0f, -10.0f, 2.5f, 15.0f, 12.0f, 8.0f, 4.0f, -0.0f };
    static readonly float[] ATK_WP_ROT = { -60.0f, -60.0f, -60.0f, -90.0f, -120.0f, -120.0f, -100.0f, -80.0f, -60.0f };
    static readonly float[] ATK_BOTTOM_X = { -0.125f, -0.1682f, -0.2111f, -0.1034f, 0.0034f, -0.0219f, -0.056f, -0.0904f, -0.125f };
    static readonly float[] ATK_FB_X = { 0.173f, 0.173f, 0.173f, 0.233f, 0.293f, 0.293f, 0.253f, 0.213f, 0.173f };
    static readonly float[] ATK_FB_Y = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] ATK_BODY_Y = { 0.0f, 0.01f, 0.02f, -0.01f, -0.04f, -0.03f, -0.02f, -0.01f, 0.0f };
    static readonly float[] ATK_BOTTOM_Y = { -0.496f, -0.506f, -0.516f, -0.486f, -0.456f, -0.466f, -0.476f, -0.486f, -0.496f };

    // ================================================================
    // Generate All
    // ================================================================
    [MenuItem("Tools/SpeedMob2/Generate All")]
    public static void GenerateAll()
    {
        GenerateIdle();
        GenerateWalk();
        GenerateRun();
        GenerateAttack();
        GenerateAttack2();
        Debug.Log("[SpeedMob2] 전체 애니메이션 생성 완료");
    }

    // ================================================================
    // Generate Idle
    // ================================================================
    [MenuItem("Tools/SpeedMob2/Generate Idle")]
    public static void GenerateIdle()
    {
        var clip = MakeClip("SpeedMob2_Idle", 48, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 48);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_48, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", IDLE_BODY_Y); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);
        L(HEAD, "localPosition.x", -0.096f); L(HEAD, "localPosition.y", 0.398f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);
        L(SF, "localPosition.x", -0.460f); C(SF, "localPosition.y", IDLE_SF_Y); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.062f); L(HF, "localPosition.y", -0.338f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.134f); C(SB, "localPosition.y", IDLE_SB_Y); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.062f); L(HB, "localPosition.y", -0.306f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(WEAPON, "localPosition.x", 0.109f); L(WEAPON, "localPosition.y", -0.247f); L(WEAPON, "localPosition.z", 0f);
        L(WEAPON, "localEulerAngles.z", -60f);
        L(BOTTOM, "localPosition.x", -0.125f); C(BOTTOM, "localPosition.y", IDLE_BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.249f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.173f); L(FOOT_B, "localPosition.y", 0f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "SpeedMob2_Idle");
    }

    // ================================================================
    // Generate Walk
    // ================================================================
    [MenuItem("Tools/SpeedMob2/Generate Walk")]
    public static void GenerateWalk()
    {
        var clip = MakeClip("SpeedMob2_Walk", 16, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 16);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, 16 / 12f, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", WALK_BODY_Y); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", WALK_BODY_ROT);
        L(HEAD, "localPosition.x", -0.096f); L(HEAD, "localPosition.y", 0.398f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);
        L(SF, "localPosition.x", -0.460f); L(SF, "localPosition.y", 0.418f); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.062f); L(HF, "localPosition.y", -0.338f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.134f); L(SB, "localPosition.y", 0.418f); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.062f); L(HB, "localPosition.y", -0.306f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(WEAPON, "localPosition.x", 0.109f); L(WEAPON, "localPosition.y", -0.247f); L(WEAPON, "localPosition.z", 0f);
        L(WEAPON, "localEulerAngles.z", -60f);
        L(BOTTOM, "localPosition.x", -0.125f); L(BOTTOM, "localPosition.y", -0.496f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", WALK_BOT_ROT);
        C(FOOT_F, "localPosition.x", WALK_FF_X); C(FOOT_F, "localPosition.y", WALK_FF_Y); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        C(FOOT_B, "localPosition.x", WALK_FB_X); C(FOOT_B, "localPosition.y", WALK_FB_Y); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "SpeedMob2_Walk");
    }

    // ================================================================
    // Generate Run
    // ================================================================
    [MenuItem("Tools/SpeedMob2/Generate Run")]
    public static void GenerateRun()
    {
        var clip = MakeClip("SpeedMob2_Run", 6, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 6);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, 6 / 12f, val));

        L(BODY, "localPosition.x", 0f); L(BODY, "localPosition.y", 0f); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", RUN_BODY_ROT);
        L(HEAD, "localPosition.x", -0.096f); L(HEAD, "localPosition.y", 0.398f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);
        L(SF, "localPosition.x", -0.460f); L(SF, "localPosition.y", 0.418f); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.062f); L(HF, "localPosition.y", -0.338f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.134f); L(SB, "localPosition.y", 0.418f); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.062f); L(HB, "localPosition.y", -0.306f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(WEAPON, "localPosition.x", 0.109f); L(WEAPON, "localPosition.y", -0.247f); L(WEAPON, "localPosition.z", 0f);
        L(WEAPON, "localEulerAngles.z", -60f);
        L(BOTTOM, "localPosition.x", -0.125f); L(BOTTOM, "localPosition.y", -0.496f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", RUN_BOT_ROT);
        C(FOOT_F, "localPosition.x", RUN_FF_X); C(FOOT_F, "localPosition.y", RUN_FF_Y); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        C(FOOT_B, "localPosition.x", RUN_FB_X); C(FOOT_B, "localPosition.y", RUN_FB_Y); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "SpeedMob2_Run");
    }

    // ================================================================
    // Generate Attack
    // ================================================================
    [MenuItem("Tools/SpeedMob2/Generate Attack")]
    public static void GenerateAttack()
    {
        var clip = MakeClip("SpeedMob2_Attack", 9, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 9);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, 9 / 12f, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", ATK_BODY_Y); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", ATK_BODY_ROT);
        L(HEAD, "localPosition.x", -0.096f); L(HEAD, "localPosition.y", 0.398f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);
        L(SF, "localPosition.x", -0.460f); L(SF, "localPosition.y", 0.418f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", ATK_SF_ROT);
        L(HF, "localPosition.x", -0.062f); L(HF, "localPosition.y", -0.338f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", ATK_HF_ROT);
        L(SB, "localPosition.x", 0.134f); L(SB, "localPosition.y", 0.418f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", ATK_SB_ROT);
        L(HB, "localPosition.x", 0.062f); L(HB, "localPosition.y", -0.306f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", ATK_HB_ROT);
        L(WEAPON, "localPosition.x", 0.109f); L(WEAPON, "localPosition.y", -0.247f); L(WEAPON, "localPosition.z", 0f);
        C(WEAPON, "localEulerAngles.z", ATK_WP_ROT);
        C(BOTTOM, "localPosition.x", ATK_BOTTOM_X); C(BOTTOM, "localPosition.y", ATK_BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", ATK_BOT_ROT);
        L(FOOT_F, "localPosition.x", -0.249f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        C(FOOT_B, "localPosition.x", ATK_FB_X); C(FOOT_B, "localPosition.y", ATK_FB_Y); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "SpeedMob2_Attack");
    }

    // ── 공통 헬퍼 ───────────────────────────────────────────────────
    static AnimationClip MakeClip(string name, int totalFrames, bool loop)
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "BasicMob 2");
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
        Debug.Log($"[SpeedMob2] {name} → {assetPath}");
    }
    // ── Attack2 배열 (18프레임: 찌르기 + 슬래시 2연격) ────────────
    static readonly float[] ATK2_SB_ROT = { 0.0f, -10.0f, -20.0f, 2.5f, 25.0f, 25.0f, 16.67f, 8.33f, 0.0f, -6.67f, -13.33f, -20.0f, 2.5f, 25.0f, 25.0f, 16.67f, 8.33f, 0.0f };
    static readonly float[] ATK2_HB_ROT = { 0.0f, -10.0f, -20.0f, 2.5f, 25.0f, 25.0f, 16.67f, 8.33f, 0.0f, -6.67f, -13.33f, -20.0f, 2.5f, 25.0f, 25.0f, 16.67f, 8.33f, 0.0f };
    static readonly float[] ATK2_WP_ROT = { -60.0f, -60.0f, -60.0f, -90.0f, -120.0f, -120.0f, -100.0f, -80.0f, -60.0f, -60.0f, -60.0f, -60.0f, -90.0f, -120.0f, -120.0f, -100.0f, -80.0f, -60.0f };
    static readonly float[] ATK2_SF_ROT = { 0.0f, 5.0f, 10.0f, 0.0f, -10.0f, -8.0f, -5.33f, -2.67f, 0.0f, 3.33f, 6.67f, 10.0f, 0.0f, -10.0f, -8.0f, -5.33f, -2.67f, 0.0f };
    static readonly float[] ATK2_HF_ROT = { 0.0f, 4.0f, 8.0f, 0.0f, -8.0f, -6.0f, -4.0f, -2.0f, 0.0f, 2.67f, 5.33f, 8.0f, 0.0f, -8.0f, -6.0f, -4.0f, -2.0f, 0.0f };
    static readonly float[] ATK2_BODY_ROT = { 0.0f, 5.0f, 10.0f, -2.5f, -15.0f, -12.0f, -8.0f, -4.0f, 0.0f, 3.33f, 6.67f, 10.0f, -2.5f, -15.0f, -12.0f, -8.0f, -4.0f, 0.0f };
    static readonly float[] ATK2_BOT_ROT = { -0.0f, -5.0f, -10.0f, 2.5f, 15.0f, 12.0f, 8.0f, 4.0f, -0.0f, -3.33f, -6.67f, -10.0f, 2.5f, 15.0f, 12.0f, 8.0f, 4.0f, -0.0f };
    static readonly float[] ATK2_BOTTOM_X = { -0.125f, -0.1682f, -0.2111f, -0.1034f, 0.0034f, -0.0219f, -0.056f, -0.0904f, -0.125f, -0.1538f, -0.1826f, -0.2111f, -0.1034f, 0.0034f, -0.0219f, -0.056f, -0.0904f, -0.125f };
    static readonly float[] ATK2_FB_X = { 0.173f, 0.173f, 0.173f, 0.233f, 0.293f, 0.293f, 0.253f, 0.213f, 0.173f, 0.173f, 0.173f, 0.173f, 0.233f, 0.293f, 0.293f, 0.253f, 0.213f, 0.173f };
    static readonly float[] ATK2_FB_Y = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] ATK2_BODY_Y = { 0.0f, 0.01f, 0.02f, -0.01f, -0.04f, -0.03f, -0.02f, -0.01f, 0.0f, 0.01f, 0.01f, 0.02f, -0.01f, -0.04f, -0.03f, -0.02f, -0.01f, 0.0f };
    static readonly float[] ATK2_BOTTOM_Y = { -0.496f, -0.506f, -0.516f, -0.486f, -0.456f, -0.466f, -0.476f, -0.486f, -0.496f, -0.506f, -0.506f, -0.516f, -0.486f, -0.456f, -0.466f, -0.476f, -0.486f, -0.496f };

    // ================================================================
    // Generate Attack2
    // ================================================================
    [MenuItem("Tools/SpeedMob2/Generate Attack2")]
    public static void GenerateAttack2()
    {
        var clip = MakeClip("SpeedMob2_Attack2", 18, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 18);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, 18 / 12f, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", ATK2_BODY_Y); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", ATK2_BODY_ROT);
        L(HEAD, "localPosition.x", -0.096f); L(HEAD, "localPosition.y", 0.398f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);
        L(SF, "localPosition.x", -0.460f); L(SF, "localPosition.y", 0.418f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", ATK2_SF_ROT);
        L(HF, "localPosition.x", -0.062f); L(HF, "localPosition.y", -0.338f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", ATK2_HF_ROT);
        L(SB, "localPosition.x", 0.134f); L(SB, "localPosition.y", 0.418f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", ATK2_SB_ROT);
        L(HB, "localPosition.x", 0.062f); L(HB, "localPosition.y", -0.306f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", ATK2_HB_ROT);
        L(WEAPON, "localPosition.x", 0.109f); L(WEAPON, "localPosition.y", -0.247f); L(WEAPON, "localPosition.z", 0f);
        C(WEAPON, "localEulerAngles.z", ATK2_WP_ROT);
        C(BOTTOM, "localPosition.x", ATK2_BOTTOM_X); C(BOTTOM, "localPosition.y", ATK2_BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", ATK2_BOT_ROT);
        L(FOOT_F, "localPosition.x", -0.249f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        C(FOOT_B, "localPosition.x", ATK2_FB_X); C(FOOT_B, "localPosition.y", ATK2_FB_Y); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "SpeedMob2_Attack2");
    }
}