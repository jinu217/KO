using UnityEngine;
using UnityEditor;

public class EBasicMob2_AnimGenerators
{
    const string SAVE_PATH = "Assets/Animations/2/EBasicMob 2";
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
    static readonly float[] IDLE_SF_Y = { 0.5208f, 0.5221f, 0.5236f, 0.5253f, 0.5272f, 0.5293f, 0.5315f, 0.5337f, 0.536f, 0.5383f, 0.5405f, 0.5427f, 0.5448f, 0.5467f, 0.5484f, 0.5499f, 0.5512f, 0.5522f, 0.5529f, 0.5534f, 0.5535f, 0.5534f, 0.5529f, 0.5522f, 0.5512f, 0.5499f, 0.5484f, 0.5467f, 0.5448f, 0.5427f, 0.5405f, 0.5383f, 0.536f, 0.5337f, 0.5315f, 0.5293f, 0.5272f, 0.5253f, 0.5236f, 0.5221f, 0.5208f, 0.5198f, 0.5191f, 0.5186f, 0.5185f, 0.5186f, 0.5191f, 0.5198f };
    static readonly float[] IDLE_SB_Y = { 0.4128f, 0.4141f, 0.4156f, 0.4173f, 0.4193f, 0.4213f, 0.4235f, 0.4257f, 0.428f, 0.4303f, 0.4325f, 0.4347f, 0.4367f, 0.4387f, 0.4404f, 0.4419f, 0.4432f, 0.4442f, 0.4449f, 0.4454f, 0.4455f, 0.4454f, 0.4449f, 0.4442f, 0.4432f, 0.4419f, 0.4404f, 0.4387f, 0.4367f, 0.4347f, 0.4325f, 0.4303f, 0.428f, 0.4257f, 0.4235f, 0.4213f, 0.4193f, 0.4173f, 0.4156f, 0.4141f, 0.4128f, 0.4118f, 0.4111f, 0.4106f, 0.4105f, 0.4106f, 0.4111f, 0.4118f };
    static readonly float[] IDLE_BOTTOM_Y = { -0.559f, -0.5655f, -0.5719f, -0.5781f, -0.584f, -0.5894f, -0.5944f, -0.5987f, -0.6023f, -0.6052f, -0.6073f, -0.6086f, -0.609f, -0.6086f, -0.6073f, -0.6052f, -0.6023f, -0.5987f, -0.5944f, -0.5894f, -0.584f, -0.5781f, -0.5719f, -0.5655f, -0.559f, -0.5525f, -0.5461f, -0.5399f, -0.534f, -0.5286f, -0.5236f, -0.5193f, -0.5157f, -0.5128f, -0.5107f, -0.5094f, -0.509f, -0.5094f, -0.5107f, -0.5128f, -0.5157f, -0.5193f, -0.5236f, -0.5286f, -0.534f, -0.5399f, -0.5461f, -0.5525f };

    // ── Walk 배열 (32프레임) ────────────────────────────────────────
    static readonly float[] WALK_FF_X = { -0.381f, -0.389f, -0.396f, -0.404f, -0.411f, -0.418f, -0.426f, -0.433f, -0.441f, -0.433f, -0.426f, -0.418f, -0.411f, -0.404f, -0.396f, -0.389f };
    static readonly float[] WALK_FF_Y = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0191f, 0.0354f, 0.0462f, 0.05f, 0.0462f, 0.0354f, 0.0191f };
    static readonly float[] WALK_FB_X = { -0.003f, 0.004f, 0.012f, 0.019f, 0.027f, 0.034f, 0.042f, 0.049f, 0.057f, 0.05f, 0.042f, 0.035f, 0.027f, 0.02f, 0.012f, 0.005f };
    static readonly float[] WALK_FB_Y = { 0.0f, 0.0191f, 0.0354f, 0.0462f, 0.05f, 0.0462f, 0.0354f, 0.0191f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] WALK_BODY_ROT = { -0.0f, -1.53f, -2.83f, -3.7f, -4.0f, -3.7f, -2.83f, -1.53f, -0.0f, 1.53f, 2.83f, 3.7f, 4.0f, 3.7f, 2.83f, 1.53f };
    static readonly float[] WALK_BOT_ROT = { 0.0f, 1.53f, 2.83f, 3.7f, 4.0f, 3.7f, 2.83f, 1.53f, 0.0f, -1.53f, -2.83f, -3.7f, -4.0f, -3.7f, -2.83f, -1.53f };
    static readonly float[] WALK_BODY_Y = { 0.0f, 0.0057f, 0.008f, 0.0057f, 0.0f, 0.0057f, 0.008f, 0.0057f, 0.0f, 0.0057f, 0.008f, 0.0057f, 0.0f, 0.0057f, 0.008f, 0.0057f };

    // ── Run 배열 (8프레임) ──────────────────────────────────────────
    static readonly float[] RUN_FF_X = { -0.381f, -0.411f, -0.441f, -0.471f, -0.441f, -0.411f };
    static readonly float[] RUN_FF_Y = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0606f, 0.0606f };
    static readonly float[] RUN_FB_X = { -0.033f, -0.003f, 0.027f, 0.057f, 0.027f, -0.003f };
    static readonly float[] RUN_FB_Y = { 0.0f, 0.0606f, 0.0606f, 0.0f, 0.0f, 0.0f };
    static readonly float[] RUN_BODY_ROT = { -15.0f, -13.27f, -13.27f, -15.0f, -16.73f, -16.73f };
    static readonly float[] RUN_BOT_ROT = { 15.0f, 13.27f, 13.27f, 15.0f, 16.73f, 16.73f };

    // ── Attack 배열 (20프레임) ──────────────────────────────────────
    // 숏소드 슬래시: SB 올리고 빠르게 내려치기, 임팩트 -25도 오버슈트
    static readonly float[] ATK_FB_X = { 0.057f, 0.057f, 0.057f, 0.077f, 0.097f, 0.117f, 0.117f, 0.1124f, 0.1078f, 0.1032f, 0.0985f, 0.0939f, 0.0893f, 0.0847f, 0.0801f, 0.0755f, 0.0708f, 0.0662f, 0.0616f, 0.057f };
    static readonly float[] ATK_FB_Y = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] ATK_SB_ROT = { 0.0f, 6.0f, 12.0f, 18.0f, 24.0f, 30.0f, 2.5f, -25.0f, -25.0f, -25.0f, -22.5f, -20.0f, -17.5f, -15.0f, -12.5f, -10.0f, -7.5f, -5.0f, -2.5f, 0.0f };
    static readonly float[] ATK_HB_ROT = { 0.0f, 9.0f, 18.0f, 27.0f, 36.0f, 45.0f, 27.5f, 10.0f, 10.0f, 10.0f, 9.0f, 8.0f, 7.0f, 6.0f, 5.0f, 4.0f, 3.0f, 2.0f, 1.0f, 0.0f };
    static readonly float[] ATK_SF_ROT = { 0.0f, -3.0f, -6.0f, -9.0f, -12.0f, -15.0f, -1.5f, 12.0f, 12.0f, 12.0f, 10.8f, 9.6f, 8.4f, 7.2f, 6.0f, 4.8f, 3.6f, 2.4f, 1.2f, 0.0f };
    static readonly float[] ATK_HF_ROT = { 0.0f, -2.0f, -4.0f, -6.0f, -8.0f, -10.0f, -1.0f, 8.0f, 8.0f, 8.0f, 7.2f, 6.4f, 5.6f, 4.8f, 4.0f, 3.2f, 2.4f, 1.6f, 0.8f, 0.0f };
    static readonly float[] ATK_BODY_ROT = { 0.0f, 1.6f, 3.2f, 4.8f, 6.4f, 8.0f, -1.0f, -10.0f, -10.0f, -10.0f, -9.0f, -8.0f, -7.0f, -6.0f, -5.0f, -4.0f, -3.0f, -2.0f, -1.0f, 0.0f };
    static readonly float[] ATK_BOT_ROT = { -0.0f, -1.6f, -3.2f, -4.8f, -6.4f, -8.0f, 1.0f, 10.0f, 10.0f, 10.0f, 9.0f, 8.0f, 7.0f, 6.0f, 5.0f, 4.0f, 3.0f, 2.0f, 1.0f, -0.0f };
    static readonly float[] ATK_BODY_Y = { 0.0f, 0.0f, 0.01f, 0.01f, 0.02f, 0.02f, -0.01f, -0.03f, -0.03f, -0.03f, -0.03f, -0.02f, -0.02f, -0.02f, -0.01f, -0.01f, -0.01f, -0.01f, -0.0f, 0.0f };
    static readonly float[] ATK_BOTTOM_Y = { -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f };

    // ================================================================
    // Generate All
    // ================================================================
    [MenuItem("Tools/EBasicMob2/Generate All")]
    public static void GenerateAll()
    {
        GenerateIdle();
        GenerateWalk();
        GenerateRun();
        GenerateAttack();
        GenerateAttack2();
        GenerateAC();
        Debug.Log("[EBasicMob2] 전체 애니메이션 생성 완료");
    }

    // ================================================================
    // Generate Idle
    // ================================================================
    [MenuItem("Tools/EBasicMob2/Generate Idle")]
    public static void GenerateIdle()
    {
        var clip = MakeClip("EBasicMob2_Idle", 48, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 48);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_48, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", IDLE_BODY_Y); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);
        L(HEAD, "localPosition.x", -0.084f); L(HEAD, "localPosition.y", 0.481f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);
        L(SF, "localPosition.x", -0.538f); C(SF, "localPosition.y", IDLE_SF_Y); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.049f); L(HF, "localPosition.y", -0.446f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.126f); C(SB, "localPosition.y", IDLE_SB_Y); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.169f); L(HB, "localPosition.y", -0.323f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(SWORD, "localPosition.x", 0.086f); L(SWORD, "localPosition.y", -0.298f); L(SWORD, "localPosition.z", 0f);
        L(SWORD, "localEulerAngles.z", -60f);
        L(BOTTOM, "localPosition.x", 0.000f); C(BOTTOM, "localPosition.y", IDLE_BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.381f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.057f); L(FOOT_B, "localPosition.y", 0f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "EBasicMob2_Idle");
    }

    // ================================================================
    // Generate Walk
    // ================================================================
    [MenuItem("Tools/EBasicMob2/Generate Walk")]
    public static void GenerateWalk()
    {
        var clip = MakeClip("EBasicMob2_Walk", 16, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 16);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, 16 / 12f, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", WALK_BODY_Y); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", WALK_BODY_ROT);
        L(HEAD, "localPosition.x", -0.084f); L(HEAD, "localPosition.y", 0.481f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);
        L(SF, "localPosition.x", -0.538f); L(SF, "localPosition.y", 0.536f); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.049f); L(HF, "localPosition.y", -0.446f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.126f); L(SB, "localPosition.y", 0.428f); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.169f); L(HB, "localPosition.y", -0.323f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(SWORD, "localPosition.x", 0.086f); L(SWORD, "localPosition.y", -0.298f); L(SWORD, "localPosition.z", 0f);
        L(SWORD, "localEulerAngles.z", -60f);
        L(BOTTOM, "localPosition.x", 0.000f); L(BOTTOM, "localPosition.y", -0.559f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", WALK_BOT_ROT);
        C(FOOT_F, "localPosition.x", WALK_FF_X); C(FOOT_F, "localPosition.y", WALK_FF_Y); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        C(FOOT_B, "localPosition.x", WALK_FB_X); C(FOOT_B, "localPosition.y", WALK_FB_Y); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "EBasicMob2_Walk");
    }

    // ================================================================
    // Generate Run
    // ================================================================
    [MenuItem("Tools/EBasicMob2/Generate Run")]
    public static void GenerateRun()
    {
        var clip = MakeClip("EBasicMob2_Run", 6, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 6);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, 6 / 12f, val));

        L(BODY, "localPosition.x", 0f); L(BODY, "localPosition.y", 0f); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", RUN_BODY_ROT);
        L(HEAD, "localPosition.x", -0.084f); L(HEAD, "localPosition.y", 0.481f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);
        L(SF, "localPosition.x", -0.538f); L(SF, "localPosition.y", 0.536f); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.049f); L(HF, "localPosition.y", -0.446f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 60f);
        L(SB, "localPosition.x", 0.126f); L(SB, "localPosition.y", 0.428f); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 15f);
        L(HB, "localPosition.x", 0.169f); L(HB, "localPosition.y", -0.323f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 75f);
        L(SWORD, "localPosition.x", 0.086f); L(SWORD, "localPosition.y", -0.298f); L(SWORD, "localPosition.z", 0f);
        L(SWORD, "localEulerAngles.z", -75f);
        L(BOTTOM, "localPosition.x", 0.000f); L(BOTTOM, "localPosition.y", -0.559f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", RUN_BOT_ROT);
        C(FOOT_F, "localPosition.x", RUN_FF_X); C(FOOT_F, "localPosition.y", RUN_FF_Y); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        C(FOOT_B, "localPosition.x", RUN_FB_X); C(FOOT_B, "localPosition.y", RUN_FB_Y); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "EBasicMob2_Run");
    }

    // ================================================================
    // Generate Attack
    // ================================================================
    [MenuItem("Tools/EBasicMob2/Generate Attack")]
    public static void GenerateAttack()
    {
        const float DUR_ATK = 20 / 12f;
        var clip = MakeClip("EBasicMob2_Attack", 20, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 20);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_ATK, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", ATK_BODY_Y); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", ATK_BODY_ROT);
        L(HEAD, "localPosition.x", -0.084f); L(HEAD, "localPosition.y", 0.481f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);
        L(SF, "localPosition.x", -0.538f); L(SF, "localPosition.y", 0.536f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", ATK_SF_ROT);
        L(HF, "localPosition.x", -0.049f); L(HF, "localPosition.y", -0.446f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", ATK_HF_ROT);
        L(SB, "localPosition.x", 0.126f); L(SB, "localPosition.y", 0.428f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", ATK_SB_ROT);
        L(HB, "localPosition.x", 0.169f); L(HB, "localPosition.y", -0.323f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", ATK_HB_ROT);
        L(SWORD, "localPosition.x", 0.086f); L(SWORD, "localPosition.y", -0.298f); L(SWORD, "localPosition.z", 0f);
        L(SWORD, "localEulerAngles.z", -60f);
        L(BOTTOM, "localPosition.x", -0.125f); C(BOTTOM, "localPosition.y", ATK_BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", ATK_BOT_ROT);
        L(FOOT_F, "localPosition.x", -0.381f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        C(FOOT_B, "localPosition.x", ATK_FB_X); C(FOOT_B, "localPosition.y", ATK_FB_Y); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "EBasicMob2_Attack");
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
            AssetDatabase.CreateFolder("Assets/Animations/2", "EBasicMob 2");
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
        Debug.Log($"[EBasicMob2] {name} → {assetPath}");
    }
    // ================================================================
    // Generate AC
    // ================================================================
    [MenuItem("Tools/EBasicMob2/Generate AC")]
    public static void GenerateAC()
    {
        string acPath = $"{SAVE_PATH}/EBasicMob2_AC.controller";

        var ac = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(acPath);
        if (ac == null)
            ac = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(acPath);

        var rootSM = ac.layers[0].stateMachine;

        var existingNames = new System.Collections.Generic.HashSet<string>();
        foreach (var s in rootSM.states) existingNames.Add(s.state.name);

        string[] clipNames = { "EBasicMob2_Idle", "EBasicMob2_Walk", "EBasicMob2_Run", "EBasicMob2_Attack", "EBasicMob2_Attack2" };

        foreach (string clipName in clipNames)
        {
            string clipPath = $"{SAVE_PATH}/{clipName}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null) { Debug.LogWarning($"[EBasicMob2] 클립 없음: {clipPath}"); continue; }

            if (!existingNames.Contains(clipName))
            {
                var state = rootSM.AddState(clipName);
                state.motion = clip;
            }
            else
            {
                foreach (var cs in rootSM.states)
                    if (cs.state.name == clipName) { cs.state.motion = clip; break; }
            }
        }

        if (rootSM.states.Length > 0)
            rootSM.defaultState = rootSM.states[0].state;

        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[EBasicMob2] AC 저장 완료: {acPath}");
    }
    // ── Attack2 배열 (14프레임: 빠른 복귀) ─────────────────────────
    static readonly float[] ATK2_FB_X = { 0.057f, 0.057f, 0.057f, 0.077f, 0.097f, 0.117f, 0.117f, 0.1124f, 0.1078f, 0.1032f, 0.0985f, 0.0847f, 0.0708f, 0.057f };
    static readonly float[] ATK2_FB_Y = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] ATK2_SB_ROT = { 0.0f, 6.0f, 12.0f, 18.0f, 24.0f, 30.0f, 2.5f, -25.0f, -25.0f, -25.0f, -22.5f, -15.0f, -7.5f, -0.0f };
    static readonly float[] ATK2_HB_ROT = { 0.0f, 9.0f, 18.0f, 27.0f, 36.0f, 45.0f, 27.5f, 10.0f, 10.0f, 10.0f, 9.0f, 6.0f, 3.0f, 0.0f };
    static readonly float[] ATK2_SF_ROT = { 0.0f, -3.0f, -6.0f, -9.0f, -12.0f, -15.0f, -1.5f, 12.0f, 12.0f, 12.0f, 10.8f, 7.2f, 3.6f, 0.0f };
    static readonly float[] ATK2_HF_ROT = { 0.0f, -2.0f, -4.0f, -6.0f, -8.0f, -10.0f, -1.0f, 8.0f, 8.0f, 8.0f, 7.2f, 4.8f, 2.4f, 0.0f };
    static readonly float[] ATK2_BODY_ROT = { 0.0f, 1.6f, 3.2f, 4.8f, 6.4f, 8.0f, -1.0f, -10.0f, -10.0f, -10.0f, -9.0f, -6.0f, -3.0f, -0.0f };
    static readonly float[] ATK2_BOT_ROT = { -0.0f, -1.6f, -3.2f, -4.8f, -6.4f, -8.0f, 1.0f, 10.0f, 10.0f, 10.0f, 9.0f, 6.0f, 3.0f, 0.0f };
    static readonly float[] ATK2_BODY_Y = { 0.0f, 0.0f, 0.01f, 0.01f, 0.02f, 0.02f, -0.01f, -0.03f, -0.03f, -0.03f, -0.03f, -0.02f, -0.01f, -0.0f };
    static readonly float[] ATK2_BOTTOM_Y = { -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f, -0.559f };

    [MenuItem("Tools/EBasicMob2/Generate Attack2")]
    public static void GenerateAttack2()
    {
        var clip = MakeClip("EBasicMob2_Attack2", 14, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 14);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, 14 / 12f, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", ATK2_BODY_Y); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", ATK2_BODY_ROT);
        L(HEAD, "localPosition.x", -0.084f); L(HEAD, "localPosition.y", 0.481f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);
        L(SF, "localPosition.x", -0.538f); L(SF, "localPosition.y", 0.536f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", ATK2_SF_ROT);
        L(HF, "localPosition.x", -0.049f); L(HF, "localPosition.y", -0.446f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", ATK2_HF_ROT);
        L(SB, "localPosition.x", 0.126f); L(SB, "localPosition.y", 0.428f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", ATK2_SB_ROT);
        L(HB, "localPosition.x", 0.169f); L(HB, "localPosition.y", -0.323f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", ATK2_HB_ROT);
        L(SWORD, "localPosition.x", 0.086f); L(SWORD, "localPosition.y", -0.298f); L(SWORD, "localPosition.z", 0f);
        L(SWORD, "localEulerAngles.z", -60f);
        L(BOTTOM, "localPosition.x", 0.000f); C(BOTTOM, "localPosition.y", ATK2_BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", ATK2_BOT_ROT);
        L(FOOT_F, "localPosition.x", -0.381f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        C(FOOT_B, "localPosition.x", ATK2_FB_X); C(FOOT_B, "localPosition.y", ATK2_FB_Y); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "EBasicMob2_Attack2");
    }
}