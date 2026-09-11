using UnityEngine;
using UnityEditor;

public class BasicMob2_AnimGenerators
{
    const string SAVE_PATH = "Assets/Animations/2/BasicMob 2";
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
    const string SWORD = "Body/Shoulder_B/Hand_B/Sword";
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
    static readonly float[] RUN_HEAD_ROT = { 15.0f, 14.23f, 13.59f, 13.15f, 13.0f, 13.15f, 13.59f, 14.23f, 15.0f, 15.77f, 16.41f, 16.85f, 17.0f, 16.85f, 16.41f, 15.77f, 15.0f };
    static readonly float[] RUN_SB_ROT = { 30.0f, 29.23f, 28.59f, 28.15f, 28.0f, 28.15f, 28.59f, 29.23f, 30.0f, 30.77f, 31.41f, 31.85f, 32.0f, 31.85f, 31.41f, 30.77f, 30.0f };
    static readonly float[] RUN_SF_ROT = { 0.0f, 9.18f, 16.97f, 22.17f, 24.0f, 22.17f, 16.97f, 9.18f, 0.0f, -9.18f, -16.97f, -22.17f, -24.0f, -22.17f, -16.97f, -9.18f, -0.0f };
    static readonly float[] RUN_HF_ROT = { 32.5f, 43.02f, 51.95f, 57.91f, 60.0f, 57.91f, 51.95f, 43.02f, 32.5f, 21.98f, 13.05f, 7.09f, 5.0f, 7.09f, 13.05f, 21.98f, 32.5f };
    static readonly float[] RUN_FF_X = { -0.249f, -0.283f, -0.317f, -0.35f, -0.384f, -0.418f, -0.452f, -0.485f, -0.519f, -0.485f, -0.452f, -0.418f, -0.384f, -0.35f, -0.317f, -0.283f, -0.249f };
    static readonly float[] RUN_FF_Y = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0612f, 0.1131f, 0.1478f, 0.16f, 0.1478f, 0.1131f, 0.0612f, 0.0f };
    static readonly float[] RUN_FB_X = { -0.087f, -0.053f, -0.019f, 0.014f, 0.048f, 0.082f, 0.116f, 0.149f, 0.183f, 0.149f, 0.115f, 0.082f, 0.048f, 0.014f, -0.02f, -0.053f, -0.087f };
    static readonly float[] RUN_FB_Y = { 0.0f, 0.0612f, 0.1131f, 0.1478f, 0.16f, 0.1478f, 0.1131f, 0.0612f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] RUN_BODY_ROT = { -15.0f, -14.23f, -13.59f, -13.15f, -13.0f, -13.15f, -13.59f, -14.23f, -15.0f, -15.77f, -16.41f, -16.85f, -17.0f, -16.85f, -16.41f, -15.77f, -15.0f };
    static readonly float[] RUN_BOT_ROT = { 15.0f, 14.23f, 13.59f, 13.15f, 13.0f, 13.15f, 13.59f, 14.23f, 15.0f, 15.77f, 16.41f, 16.85f, 17.0f, 16.85f, 16.41f, 15.77f, 15.0f };

    // ── Attack 배열 (20프레임) ──────────────────────────────────────
    // 숏소드 슬래시: SB 올리고 빠르게 내려치기, 임팩트 -25도 오버슈트
    static readonly float[] ATK_SB_ROT = { 0.0f, 6.0f, 12.0f, 18.0f, 24.0f, 30.0f, 2.5f, -25.0f, -25.0f, -25.0f, -22.5f, -20.0f, -17.5f, -15.0f, -12.5f, -10.0f, -7.5f, -5.0f, -2.5f, 0.0f };
    static readonly float[] ATK_HB_ROT = { 0.0f, 9.0f, 18.0f, 27.0f, 36.0f, 45.0f, 27.5f, 10.0f, 10.0f, 10.0f, 9.0f, 8.0f, 7.0f, 6.0f, 5.0f, 4.0f, 3.0f, 2.0f, 1.0f, 0.0f };
    static readonly float[] ATK_SF_ROT = { 0.0f, -3.0f, -6.0f, -9.0f, -12.0f, -15.0f, -1.5f, 12.0f, 12.0f, 12.0f, 10.8f, 9.6f, 8.4f, 7.2f, 6.0f, 4.8f, 3.6f, 2.4f, 1.2f, 0.0f };
    static readonly float[] ATK_HF_ROT = { 0.0f, -2.0f, -4.0f, -6.0f, -8.0f, -10.0f, -1.0f, 8.0f, 8.0f, 8.0f, 7.2f, 6.4f, 5.6f, 4.8f, 4.0f, 3.2f, 2.4f, 1.6f, 0.8f, 0.0f };
    static readonly float[] ATK_BODY_ROT = { 0.0f, 1.6f, 3.2f, 4.8f, 6.4f, 8.0f, -1.0f, -10.0f, -10.0f, -10.0f, -9.0f, -8.0f, -7.0f, -6.0f, -5.0f, -4.0f, -3.0f, -2.0f, -1.0f, 0.0f };
    static readonly float[] ATK_BOT_ROT = { -0.0f, -1.6f, -3.2f, -4.8f, -6.4f, -8.0f, 1.0f, 10.0f, 10.0f, 10.0f, 9.0f, 8.0f, 7.0f, 6.0f, 5.0f, 4.0f, 3.0f, 2.0f, 1.0f, -0.0f };
    static readonly float[] ATK_BODY_Y = { 0.0f, 0.0f, 0.01f, 0.01f, 0.02f, 0.02f, -0.01f, -0.03f, -0.03f, -0.03f, -0.03f, -0.02f, -0.02f, -0.02f, -0.01f, -0.01f, -0.01f, -0.01f, -0.0f, 0.0f };
    static readonly float[] ATK_BOTTOM_Y = { -0.496f, -0.496f, -0.506f, -0.506f, -0.516f, -0.516f, -0.486f, -0.466f, -0.466f, -0.466f, -0.466f, -0.476f, -0.476f, -0.476f, -0.486f, -0.486f, -0.486f, -0.486f, -0.496f, -0.496f };

    // ================================================================
    // Generate All
    // ================================================================
    [MenuItem("Tools/BasicMob2/Generate All")]
    public static void GenerateAll()
    {
        GenerateIdle();
        GenerateWalk();
        GenerateRun();
        GenerateAttack();
        Debug.Log("[BasicMob2] 전체 애니메이션 생성 완료");
    }

    // ================================================================
    // Generate Idle
    // ================================================================
    [MenuItem("Tools/BasicMob2/Generate Idle")]
    public static void GenerateIdle()
    {
        var clip = MakeClip("BasicMob2_Idle", 48, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 48);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_48, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", IDLE_BODY_Y); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);
        L(HEAD, "localPosition.x", -0.096f); L(HEAD, "localPosition.y", 0.398f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", RUN_HEAD_ROT);
        L(SF, "localPosition.x", -0.460f); C(SF, "localPosition.y", IDLE_SF_Y); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.062f); L(HF, "localPosition.y", -0.338f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.134f); C(SB, "localPosition.y", IDLE_SB_Y); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.062f); L(HB, "localPosition.y", -0.306f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(SWORD, "localPosition.x", 0.134f); L(SWORD, "localPosition.y", -0.224f); L(SWORD, "localPosition.z", 0f);
        L(SWORD, "localEulerAngles.z", -60f);
        L(BOTTOM, "localPosition.x", -0.125f); C(BOTTOM, "localPosition.y", IDLE_BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.249f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.173f); L(FOOT_B, "localPosition.y", 0f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "BasicMob2_Idle");
    }

    // ================================================================
    // Generate Walk
    // ================================================================
    [MenuItem("Tools/BasicMob2/Generate Walk")]
    public static void GenerateWalk()
    {
        var clip = MakeClip("BasicMob2_Walk", 16, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 16);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, 16 / 12f, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", WALK_BODY_Y); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", WALK_BODY_ROT);
        L(HEAD, "localPosition.x", -0.096f); L(HEAD, "localPosition.y", 0.398f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", RUN_HEAD_ROT);
        L(SF, "localPosition.x", -0.460f); L(SF, "localPosition.y", 0.418f); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.062f); L(HF, "localPosition.y", -0.338f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.134f); L(SB, "localPosition.y", 0.418f); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.062f); L(HB, "localPosition.y", -0.306f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(SWORD, "localPosition.x", 0.134f); L(SWORD, "localPosition.y", -0.224f); L(SWORD, "localPosition.z", 0f);
        L(SWORD, "localEulerAngles.z", -60f);
        L(BOTTOM, "localPosition.x", -0.125f); L(BOTTOM, "localPosition.y", -0.496f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", WALK_BOT_ROT);
        C(FOOT_F, "localPosition.x", WALK_FF_X); C(FOOT_F, "localPosition.y", WALK_FF_Y); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        C(FOOT_B, "localPosition.x", WALK_FB_X); C(FOOT_B, "localPosition.y", WALK_FB_Y); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "BasicMob2_Walk");
    }

    // ================================================================
    // Generate Run
    // ================================================================
    [MenuItem("Tools/BasicMob2/Generate Run")]
    public static void GenerateRun()
    {
        var clip = MakeClip("BasicMob2_Run", 17, true);
        void C(string path, string prop, float[] vals) => SetCurveSpeed(clip, path, prop, vals, 17, 3f);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, 17 / 12f / 3f, val));

        L(BODY, "localPosition.x", 0f); L(BODY, "localPosition.y", 0f); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", RUN_BODY_ROT);
        L(HEAD, "localPosition.x", -0.096f); L(HEAD, "localPosition.y", 0.398f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", RUN_HEAD_ROT);
        L(SF, "localPosition.x", -0.460f); L(SF, "localPosition.y", 0.418f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", RUN_SF_ROT);
        L(HF, "localPosition.x", -0.062f); L(HF, "localPosition.y", -0.338f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", RUN_HF_ROT);
        L(SB, "localPosition.x", 0.134f); L(SB, "localPosition.y", 0.418f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", RUN_SB_ROT);
        L(HB, "localPosition.x", 0.062f); L(HB, "localPosition.y", -0.306f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 65f);
        L(SWORD, "localPosition.x", 0.134f); L(SWORD, "localPosition.y", -0.224f); L(SWORD, "localPosition.z", 0f);
        L(SWORD, "localEulerAngles.z", -75f);
        L(BOTTOM, "localPosition.x", -0.125f); L(BOTTOM, "localPosition.y", -0.496f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", RUN_BOT_ROT);
        C(FOOT_F, "localPosition.x", RUN_FF_X); C(FOOT_F, "localPosition.y", RUN_FF_Y); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        C(FOOT_B, "localPosition.x", RUN_FB_X); C(FOOT_B, "localPosition.y", RUN_FB_Y); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "BasicMob2_Run");
    }

    // ================================================================
    // Generate Attack
    // ================================================================
    [MenuItem("Tools/BasicMob2/Generate Attack")]
    public static void GenerateAttack()
    {
        const float DUR_ATK = 20 / 12f;
        var clip = MakeClip("BasicMob2_Attack", 20, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 20);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_ATK, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", ATK_BODY_Y); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", ATK_BODY_ROT);
        L(HEAD, "localPosition.x", -0.096f); L(HEAD, "localPosition.y", 0.398f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", RUN_HEAD_ROT);
        L(SF, "localPosition.x", -0.460f); L(SF, "localPosition.y", 0.418f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", ATK_SF_ROT);
        L(HF, "localPosition.x", -0.062f); L(HF, "localPosition.y", -0.338f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", ATK_HF_ROT);
        L(SB, "localPosition.x", 0.134f); L(SB, "localPosition.y", 0.418f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", ATK_SB_ROT);
        L(HB, "localPosition.x", 0.062f); L(HB, "localPosition.y", -0.306f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", ATK_HB_ROT);
        L(SWORD, "localPosition.x", 0.134f); L(SWORD, "localPosition.y", -0.224f); L(SWORD, "localPosition.z", 0f);
        L(SWORD, "localEulerAngles.z", -60f);
        L(BOTTOM, "localPosition.x", -0.125f); C(BOTTOM, "localPosition.y", ATK_BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", ATK_BOT_ROT);
        L(FOOT_F, "localPosition.x", -0.249f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.173f); L(FOOT_B, "localPosition.y", 0f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "BasicMob2_Attack");
    }

    // ── 공통 헬퍼 ───────────────────────────────────────────────────
    static AnimationClip MakeClip(string name, int totalFrames, bool loop)
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            if (!AssetDatabase.IsValidFolder("Assets/Animations/2"))
                AssetDatabase.CreateFolder("Assets/Animations", "2");
            AssetDatabase.CreateFolder("Assets/Animations/2", "BasicMob 2");
        }
        var clip = new AnimationClip { frameRate = FRAME_RATE };
        clip.name = name;
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, s);
        return clip;
    }

    static void SetCurveSpeed(AnimationClip clip, string path, string prop, float[] vals, int total, float speed)
    {
        var keys = new Keyframe[total];
        for (int i = 0; i < total; i++) keys[i] = new Keyframe(i / FRAME_RATE / speed, vals[i]);
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < curve.keys.Length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
        }
        clip.SetCurve(path, typeof(Transform), prop, curve);
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
        Debug.Log($"[BasicMob2] {name} → {assetPath}");
    }
}