using UnityEngine;
using UnityEditor;

public class Villager1_AnimGenerators
{
    const string SAVE_PATH = "Assets/Animations/Villager1";
    const float FRAME_RATE = 12f;
    const float DUR_48 = 48 / 12f;
    const float DUR_24 = 24 / 12f;
    const float DUR_32 = 32 / 12f;

    const string BODY = "Body";
    const string HEAD = "Body/Head";
    const string SF = "Body/Shoulder_F";
    const string HF = "Body/Shoulder_F/Hand_F";
    const string SB = "Body/Shoulder_B";
    const string HB = "Body/Shoulder_B/Hand_B";
    const string BOTTOM = "Body/Bottom";
    const string FOOT_F = "Body/Bottom/Foot_F";
    const string FOOT_B = "Body/Bottom/Foot_B";

    // rest 실측값:
    //   Body pos(0,0), Head pos(-0.093, 0.485)
    //   SF pos(-0.390, 0.359), HF pos(-0.020, -0.281)
    //   SB pos(0.157, 0.359), HB pos(0.043, -0.281)
    //   Bottom pos(0, -0.574), Foot_F pos(-0.250, 0), Foot_B pos(0.110, 0)

    // ── 숨쉬기 배열 (48프레임) ──────────────────────────────────────
    static readonly float[] BODY_Y = { 0.0f, 0.0065f, 0.0129f, 0.0191f, 0.025f, 0.0304f, 0.0354f, 0.0397f, 0.0433f, 0.0462f, 0.0483f, 0.0496f, 0.05f, 0.0496f, 0.0483f, 0.0462f, 0.0433f, 0.0397f, 0.0354f, 0.0304f, 0.025f, 0.0191f, 0.0129f, 0.0065f, 0.0f, -0.0065f, -0.0129f, -0.0191f, -0.025f, -0.0304f, -0.0354f, -0.0397f, -0.0433f, -0.0462f, -0.0483f, -0.0496f, -0.05f, -0.0496f, -0.0483f, -0.0462f, -0.0433f, -0.0397f, -0.0354f, -0.0304f, -0.025f, -0.0191f, -0.0129f, -0.0065f };
    static readonly float[] HEAD_Y = { 0.485f, 0.4883f, 0.4915f, 0.4946f, 0.4975f, 0.5002f, 0.5027f, 0.5048f, 0.5067f, 0.5081f, 0.5091f, 0.5098f, 0.51f, 0.5098f, 0.5091f, 0.5081f, 0.5067f, 0.5048f, 0.5027f, 0.5002f, 0.4975f, 0.4946f, 0.4915f, 0.4883f, 0.485f, 0.4817f, 0.4785f, 0.4754f, 0.4725f, 0.4698f, 0.4673f, 0.4652f, 0.4633f, 0.4619f, 0.4609f, 0.4602f, 0.46f, 0.4602f, 0.4609f, 0.4619f, 0.4633f, 0.4652f, 0.4673f, 0.4698f, 0.4725f, 0.4754f, 0.4785f, 0.4817f };
    static readonly float[] SF_Y = { 0.333f, 0.3352f, 0.3378f, 0.3407f, 0.344f, 0.3475f, 0.3512f, 0.3551f, 0.359f, 0.3629f, 0.3668f, 0.3705f, 0.374f, 0.3773f, 0.3802f, 0.3828f, 0.385f, 0.3867f, 0.388f, 0.3887f, 0.389f, 0.3887f, 0.388f, 0.3867f, 0.385f, 0.3828f, 0.3802f, 0.3773f, 0.374f, 0.3705f, 0.3668f, 0.3629f, 0.359f, 0.3551f, 0.3512f, 0.3475f, 0.344f, 0.3407f, 0.3378f, 0.3352f, 0.333f, 0.3313f, 0.33f, 0.3293f, 0.329f, 0.3293f, 0.33f, 0.3313f };
    static readonly float[] SB_Y = { 0.333f, 0.3352f, 0.3378f, 0.3407f, 0.344f, 0.3475f, 0.3512f, 0.3551f, 0.359f, 0.3629f, 0.3668f, 0.3705f, 0.374f, 0.3773f, 0.3802f, 0.3828f, 0.385f, 0.3867f, 0.388f, 0.3887f, 0.389f, 0.3887f, 0.388f, 0.3867f, 0.385f, 0.3828f, 0.3802f, 0.3773f, 0.374f, 0.3705f, 0.3668f, 0.3629f, 0.359f, 0.3551f, 0.3512f, 0.3475f, 0.344f, 0.3407f, 0.3378f, 0.3352f, 0.333f, 0.3313f, 0.33f, 0.3293f, 0.329f, 0.3293f, 0.33f, 0.3313f };
    static readonly float[] BOTTOM_Y = { -0.574f, -0.5805f, -0.5869f, -0.5931f, -0.599f, -0.6044f, -0.6094f, -0.6137f, -0.6173f, -0.6202f, -0.6223f, -0.6236f, -0.624f, -0.6236f, -0.6223f, -0.6202f, -0.6173f, -0.6137f, -0.6094f, -0.6044f, -0.599f, -0.5931f, -0.5869f, -0.5805f, -0.574f, -0.5675f, -0.5611f, -0.5549f, -0.549f, -0.5436f, -0.5386f, -0.5343f, -0.5307f, -0.5278f, -0.5257f, -0.5244f, -0.524f, -0.5244f, -0.5257f, -0.5278f, -0.5307f, -0.5343f, -0.5386f, -0.5436f, -0.549f, -0.5549f, -0.5611f, -0.5675f };

    // ── Wave 배열 ──────────────────────────────────────────────────
    static readonly float[] WAVE_SB_ROT = { 0.0f, 35.0f, 70.0f, 105.0f, 130.0f, 137.07f, 140.0f, 137.07f, 130.0f, 122.93f, 120.0f, 122.93f, 130.0f, 137.07f, 140.0f, 137.07f, 130.0f, 122.93f, 120.0f, 86.67f, 43.33f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] WAVE_HB_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 27.5f, 39.87f, 45.0f, 39.87f, 27.5f, 15.13f, 10.0f, 15.13f, 27.5f, 39.87f, 45.0f, 39.87f, 27.5f, 15.13f, 10.0f, 6.67f, 3.33f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] WAVE_HEAD_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 3.54f, 5.0f, 3.54f, 0.0f, -3.54f, -5.0f, -3.54f, 0.0f, 3.54f, 5.0f, 3.54f, 0.0f, -3.54f, -5.0f, -3.33f, -1.67f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

    // ── 걷기 배열 (32프레임 루프) ──────────────────────────────────
    // 걷기: SF/SB 앞뒤 스윙, Body 좌우, 발 교대
    static float[] FW(float b, params (int i, float v)[] o)
    {
        var a = new float[32];
        for (int i = 0; i < 32; i++) a[i] = b;
        foreach (var (i, v) in o) if (i < 32) a[i] = v;
        return a;
    }

    static readonly float[] WALK_FF_X = { -0.25f, -0.254f, -0.258f, -0.261f, -0.265f, -0.269f, -0.273f, -0.276f, -0.28f, -0.284f, -0.287f, -0.291f, -0.295f, -0.299f, -0.302f, -0.306f, -0.31f, -0.306f, -0.302f, -0.299f, -0.295f, -0.291f, -0.287f, -0.284f, -0.28f, -0.276f, -0.273f, -0.269f, -0.265f, -0.261f, -0.258f, -0.254f };
    static readonly float[] WALK_FB_X = { 0.05f, 0.054f, 0.058f, 0.061f, 0.065f, 0.069f, 0.073f, 0.076f, 0.08f, 0.084f, 0.087f, 0.091f, 0.095f, 0.099f, 0.103f, 0.106f, 0.11f, 0.106f, 0.103f, 0.099f, 0.095f, 0.091f, 0.087f, 0.084f, 0.08f, 0.076f, 0.073f, 0.069f, 0.065f, 0.061f, 0.058f, 0.054f };
    static readonly float[] WALK_FF_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] WALK_FB_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] WALK_FF_Y = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0117f, 0.023f, 0.0333f, 0.0424f, 0.0499f, 0.0554f, 0.0588f, 0.06f, 0.0588f, 0.0554f, 0.0499f, 0.0424f, 0.0333f, 0.023f, 0.0117f };
    static readonly float[] WALK_FB_Y = { 0.0f, 0.0117f, 0.023f, 0.0333f, 0.0424f, 0.0499f, 0.0554f, 0.0588f, 0.06f, 0.0588f, 0.0554f, 0.0499f, 0.0424f, 0.0333f, 0.023f, 0.0117f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

    static readonly float[] WALK_BODY_ROT = { -0.0f, -0.78f, -1.53f, -2.22f, -2.83f, -3.33f, -3.7f, -3.92f, -4.0f, -3.92f, -3.7f, -3.33f, -2.83f, -2.22f, -1.53f, -0.78f, -0.0f, 0.78f, 1.53f, 2.22f, 2.83f, 3.33f, 3.7f, 3.92f, 4.0f, 3.92f, 3.7f, 3.33f, 2.83f, 2.22f, 1.53f, 0.78f };
    static readonly float[] WALK_BOT_ROT = { 0.0f, 0.78f, 1.53f, 2.22f, 2.83f, 3.33f, 3.7f, 3.92f, 4.0f, 3.92f, 3.7f, 3.33f, 2.83f, 2.22f, 1.53f, 0.78f, 0.0f, -0.78f, -1.53f, -2.22f, -2.83f, -3.33f, -3.7f, -3.92f, -4.0f, -3.92f, -3.7f, -3.33f, -2.83f, -2.22f, -1.53f, -0.78f };
    static readonly float[] WALK_SF_ROT = { -0.0f, -4.88f, -9.57f, -13.89f, -17.68f, -20.79f, -23.1f, -24.52f, -25.0f, -24.52f, -23.1f, -20.79f, -17.68f, -13.89f, -9.57f, -4.88f, -0.0f, 4.88f, 9.57f, 13.89f, 17.68f, 20.79f, 23.1f, 24.52f, 25.0f, 24.52f, 23.1f, 20.79f, 17.68f, 13.89f, 9.57f, 4.88f };
    static readonly float[] WALK_SB_ROT = { 0.0f, 4.88f, 9.57f, 13.89f, 17.68f, 20.79f, 23.1f, 24.52f, 25.0f, 24.52f, 23.1f, 20.79f, 17.68f, 13.89f, 9.57f, 4.88f, 0.0f, -4.88f, -9.57f, -13.89f, -17.68f, -20.79f, -23.1f, -24.52f, -25.0f, -24.52f, -23.1f, -20.79f, -17.68f, -13.89f, -9.57f, -4.88f };
    static readonly float[] WALK_HF_ROT = { 15.0f, 12.07f, 9.26f, 6.67f, 4.39f, 2.53f, 1.14f, 0.29f, 0.0f, 0.29f, 1.14f, 2.53f, 4.39f, 6.67f, 9.26f, 12.07f, 15.0f, 17.93f, 20.74f, 23.33f, 25.61f, 27.47f, 28.86f, 29.71f, 30.0f, 29.71f, 28.86f, 27.47f, 25.61f, 23.33f, 20.74f, 17.93f };
    static readonly float[] WALK_HB_ROT = { 15.0f, 17.93f, 20.74f, 23.33f, 25.61f, 27.47f, 28.86f, 29.71f, 30.0f, 29.71f, 28.86f, 27.47f, 25.61f, 23.33f, 20.74f, 17.93f, 15.0f, 12.07f, 9.26f, 6.67f, 4.39f, 2.53f, 1.14f, 0.29f, 0.0f, 0.29f, 1.14f, 2.53f, 4.39f, 6.67f, 9.26f, 12.07f };
    // 발: 발목 위아래
    static readonly float[] WALK_FOOT_F_Y = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0059f, 0.0115f, 0.0167f, 0.0212f, 0.0249f, 0.0277f, 0.0294f, 0.03f, 0.0294f, 0.0277f, 0.0249f, 0.0212f, 0.0167f, 0.0115f, 0.0059f };
    static readonly float[] WALK_FOOT_B_Y = { 0.0f, 0.0059f, 0.0115f, 0.0167f, 0.0212f, 0.0249f, 0.0277f, 0.0294f, 0.03f, 0.0294f, 0.0277f, 0.0249f, 0.0212f, 0.0167f, 0.0115f, 0.0059f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    // Body Y: 걸을때 위아래 바운스
    static readonly float[] WALK_BODY_Y = { 0.0f, 0.0031f, 0.0057f, 0.0074f, 0.008f, 0.0074f, 0.0057f, 0.0031f, 0.0f, 0.0031f, 0.0057f, 0.0074f, 0.008f, 0.0074f, 0.0057f, 0.0031f, 0.0f, 0.0031f, 0.0057f, 0.0074f, 0.008f, 0.0074f, 0.0057f, 0.0031f, 0.0f, 0.0031f, 0.0057f, 0.0074f, 0.008f, 0.0074f, 0.0057f, 0.0031f };
    static readonly float[] WALK_BOTTOM_Y = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

    // ================================================================
    // Generate Idle
    // ================================================================
    [MenuItem("Tools/Villager1/Generate Idle")]
    public static void GenerateIdle()
    {
        var clip = MakeClip("Villager1_Idle", 48, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 48);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_48, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", BODY_Y); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);
        L(HEAD, "localPosition.x", -0.093f); C(HEAD, "localPosition.y", HEAD_Y); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);
        L(SF, "localPosition.x", -0.390f); C(SF, "localPosition.y", SF_Y); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.020f); L(HF, "localPosition.y", -0.281f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.157f); C(SB, "localPosition.y", SB_Y); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.043f); L(HB, "localPosition.y", -0.281f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(BOTTOM, "localPosition.x", 0f); C(BOTTOM, "localPosition.y", BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.250f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.110f); L(FOOT_B, "localPosition.y", 0f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "Villager1_Idle");
    }

    // ================================================================
    // Generate Walk
    // ================================================================
    [MenuItem("Tools/Villager1/Generate Walk")]
    public static void GenerateWalk()
    {
        var clip = MakeClip("Villager1_Walk", 32, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 32);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_32, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", WALK_BODY_Y); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", WALK_BODY_ROT);
        L(HEAD, "localPosition.x", -0.093f); L(HEAD, "localPosition.y", 0.485f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);
        L(SF, "localPosition.x", -0.390f); L(SF, "localPosition.y", 0.359f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", WALK_SF_ROT);
        L(HF, "localPosition.x", -0.020f); L(HF, "localPosition.y", -0.281f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", WALK_HF_ROT);
        L(SB, "localPosition.x", 0.157f); L(SB, "localPosition.y", 0.359f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", WALK_SB_ROT);
        L(HB, "localPosition.x", 0.043f); L(HB, "localPosition.y", -0.281f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", WALK_HB_ROT);
        L(BOTTOM, "localPosition.x", 0f); L(BOTTOM, "localPosition.y", -0.574f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", WALK_BOT_ROT);
        // 발: X 앞뒤 이동 + Y 살짝 들림 + rot 수평 유지
        C(FOOT_F, "localPosition.x", WALK_FF_X); C(FOOT_F, "localPosition.y", WALK_FF_Y); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        C(FOOT_B, "localPosition.x", WALK_FB_X); C(FOOT_B, "localPosition.y", WALK_FB_Y); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

        Save(clip, "Villager1_Walk");
    }

    // ================================================================
    // Generate Wave (손흔들기만)
    // ================================================================
    [MenuItem("Tools/Villager1/Generate Wave")]
    public static void GenerateWave()
    {
        var clip = MakeClip("Villager1_Wave", 48, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 48);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_48, val));

        L(BODY, "localPosition.x", 0f); L(BODY, "localPosition.y", 0f); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);
        L(HEAD, "localPosition.x", -0.093f); L(HEAD, "localPosition.y", 0.485f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", WAVE_HEAD_ROT);
        L(SF, "localPosition.x", -0.390f); L(SF, "localPosition.y", 0.359f); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.020f); L(HF, "localPosition.y", -0.281f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.157f); L(SB, "localPosition.y", 0.359f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", WAVE_SB_ROT);
        L(HB, "localPosition.x", 0.043f); L(HB, "localPosition.y", -0.281f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", WAVE_HB_ROT);
        L(BOTTOM, "localPosition.x", 0f); L(BOTTOM, "localPosition.y", -0.574f); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.250f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.110f); L(FOOT_B, "localPosition.y", 0f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "Villager1_Wave");
    }

    // ================================================================
    // Generate IdleWave (숨쉬기 + 손흔들기)
    // ================================================================
    [MenuItem("Tools/Villager1/Generate IdleWave")]
    public static void GenerateIdleWave()
    {
        var clip = MakeClip("Villager1_IdleWave", 48, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 48);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_48, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", BODY_Y); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);
        L(HEAD, "localPosition.x", -0.093f); C(HEAD, "localPosition.y", HEAD_Y); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", WAVE_HEAD_ROT);
        L(SF, "localPosition.x", -0.390f); C(SF, "localPosition.y", SF_Y); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.020f); L(HF, "localPosition.y", -0.281f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.157f); C(SB, "localPosition.y", SB_Y); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", WAVE_SB_ROT);
        L(HB, "localPosition.x", 0.043f); L(HB, "localPosition.y", -0.281f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", WAVE_HB_ROT);
        L(BOTTOM, "localPosition.x", 0f); C(BOTTOM, "localPosition.y", BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.250f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.110f); L(FOOT_B, "localPosition.y", 0f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "Villager1_IdleWave");
    }

    // ── 공통 헬퍼 ───────────────────────────────────────────────────
    static AnimationClip MakeClip(string name, int totalFrames, bool loop)
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "Villager1");
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
        Debug.Log($"[Villager1] {name} → {assetPath}");
    }
}