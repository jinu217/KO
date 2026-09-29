using UnityEngine;
using UnityEditor;

public class EArcher2_AnimGenerators
{
    [MenuItem("Tools/2/EArcher2/Generate Hit and Death")]
    public static void GenerateHitDeath() { MobHitDeathSetup.SetupMob(2, "EArcher 2"); }

    const string SAVE_PATH = "Assets/Animations/2/EArcher 2";
    const float FRAME_RATE = 12f;

    const string BODY = "Body";
    const string HEAD = "Body/Head";
    const string SF = "Body/Shoulder_F";
    const string HF = "Body/Shoulder_F/Hand_F";
    const string SB = "Body/Shoulder_B";
    const string HB = "Body/Shoulder_B/Hand_B";
    const string BOW = "Body/Shoulder_B/Hand_B/Bow";
    const string BOTTOM = "Body/Bottom";
    const string FOOT_F = "Body/Bottom/Foot_F";
    const string FOOT_B = "Body/Bottom/Foot_B";

    // ================================================================
    // rest 실측값:
    //   Body(0,0), Head(-0.069, 0.497)
    //   SF(-0.578, 0.459), HF(-0.009, -0.370)
    //   SB(0.063, 0.413), HB(0.232, -0.308)
    //   Bow(0.118, -0.319) rot=-60
    //   Bottom(0, -0.559), Foot_F(-0.381, 0), Foot_B(0.057, 0)
    // ================================================================

    // ── Idle (48프레임) ─────────────────────────────────────────────
    static readonly float[] IDLE_BODY_Y = { 0.0f, 0.0065f, 0.0129f, 0.0191f, 0.025f, 0.0304f, 0.0354f, 0.0397f, 0.0433f, 0.0462f, 0.0483f, 0.0496f, 0.05f, 0.0496f, 0.0483f, 0.0462f, 0.0433f, 0.0397f, 0.0354f, 0.0304f, 0.025f, 0.0191f, 0.0129f, 0.0065f, 0.0f, -0.0065f, -0.0129f, -0.0191f, -0.025f, -0.0304f, -0.0354f, -0.0397f, -0.0433f, -0.0462f, -0.0483f, -0.0496f, -0.05f, -0.0496f, -0.0483f, -0.0462f, -0.0433f, -0.0397f, -0.0354f, -0.0304f, -0.025f, -0.0191f, -0.0129f, -0.0065f };
    static readonly float[] IDLE_SF_Y = { 0.4438f, 0.4451f, 0.4466f, 0.4483f, 0.4503f, 0.4523f, 0.4545f, 0.4567f, 0.459f, 0.4613f, 0.4635f, 0.4657f, 0.4677f, 0.4697f, 0.4714f, 0.4729f, 0.4742f, 0.4752f, 0.4759f, 0.4764f, 0.4765f, 0.4764f, 0.4759f, 0.4752f, 0.4742f, 0.4729f, 0.4714f, 0.4697f, 0.4677f, 0.4657f, 0.4635f, 0.4613f, 0.459f, 0.4567f, 0.4545f, 0.4523f, 0.4503f, 0.4483f, 0.4466f, 0.4451f, 0.4438f, 0.4428f, 0.4421f, 0.4416f, 0.4415f, 0.4416f, 0.4421f, 0.4428f };
    static readonly float[] IDLE_SB_Y = { 0.3978f, 0.3991f, 0.4006f, 0.4023f, 0.4042f, 0.4063f, 0.4085f, 0.4107f, 0.413f, 0.4153f, 0.4175f, 0.4197f, 0.4217f, 0.4237f, 0.4254f, 0.4269f, 0.4282f, 0.4292f, 0.4299f, 0.4304f, 0.4305f, 0.4304f, 0.4299f, 0.4292f, 0.4282f, 0.4269f, 0.4254f, 0.4237f, 0.4217f, 0.4197f, 0.4175f, 0.4153f, 0.413f, 0.4107f, 0.4085f, 0.4063f, 0.4042f, 0.4023f, 0.4006f, 0.3991f, 0.3978f, 0.3968f, 0.3961f, 0.3956f, 0.3955f, 0.3956f, 0.3961f, 0.3968f };
    static readonly float[] IDLE_BOTTOM_Y = { -0.559f, -0.5655f, -0.5719f, -0.5781f, -0.584f, -0.5894f, -0.5944f, -0.5987f, -0.6023f, -0.6052f, -0.6073f, -0.6086f, -0.609f, -0.6086f, -0.6073f, -0.6052f, -0.6023f, -0.5987f, -0.5944f, -0.5894f, -0.584f, -0.5781f, -0.5719f, -0.5655f, -0.559f, -0.5525f, -0.5461f, -0.5399f, -0.534f, -0.5286f, -0.5236f, -0.5193f, -0.5157f, -0.5128f, -0.5107f, -0.5094f, -0.509f, -0.5094f, -0.5107f, -0.5128f, -0.5157f, -0.5193f, -0.5236f, -0.5286f, -0.534f, -0.5399f, -0.5461f, -0.5525f };

    // ── Walk (16프레임) ─────────────────────────────────────────────
    static readonly float[] WALK_FF_X = { -0.381f, -0.39f, -0.399f, -0.407f, -0.416f, -0.425f, -0.433f, -0.442f, -0.451f, -0.442f, -0.433f, -0.425f, -0.416f, -0.407f, -0.399f, -0.39f };
    static readonly float[] WALK_FF_Y = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0191f, 0.0354f, 0.0462f, 0.05f, 0.0462f, 0.0354f, 0.0191f };
    static readonly float[] WALK_FB_X = { -0.013f, -0.004f, 0.005f, 0.013f, 0.022f, 0.031f, 0.04f, 0.048f, 0.057f, 0.048f, 0.04f, 0.031f, 0.022f, 0.013f, 0.004f, -0.004f };
    static readonly float[] WALK_FB_Y = { 0.0f, 0.0191f, 0.0354f, 0.0462f, 0.05f, 0.0462f, 0.0354f, 0.0191f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] WALK_BODY_ROT = { -0.0f, -1.53f, -2.83f, -3.7f, -4.0f, -3.7f, -2.83f, -1.53f, -0.0f, 1.53f, 2.83f, 3.7f, 4.0f, 3.7f, 2.83f, 1.53f };
    static readonly float[] WALK_BOT_ROT = { 0.0f, 1.53f, 2.83f, 3.7f, 4.0f, 3.7f, 2.83f, 1.53f, 0.0f, -1.53f, -2.83f, -3.7f, -4.0f, -3.7f, -2.83f, -1.53f };
    static readonly float[] WALK_BODY_Y = { 0.0f, 0.0057f, 0.008f, 0.0057f, 0.0f, 0.0057f, 0.008f, 0.0057f, 0.0f, 0.0057f, 0.008f, 0.0057f, 0.0f, 0.0057f, 0.008f, 0.0057f };

    // ── Run (8프레임) ───────────────────────────────────────────────
    static readonly float[] RUN_HEAD_ROT = { 8.0f, 7.23f, 6.59f, 6.15f, 6.0f, 6.15f, 6.59f, 7.23f, 8.0f, 8.77f, 9.41f, 9.85f, 10.0f, 9.85f, 9.41f, 8.77f, 8.0f };
    static readonly float[] RUN_SB_ROT = { 16.0f, 15.23f, 14.59f, 14.15f, 14.0f, 14.15f, 14.59f, 15.23f, 16.0f, 16.77f, 17.41f, 17.85f, 18.0f, 17.85f, 17.41f, 16.77f, 16.0f };
    static readonly float[] RUN_SF_ROT = { 0.0f, 7.65f, 14.14f, 18.48f, 20.0f, 18.48f, 14.14f, 7.65f, 0.0f, -7.65f, -14.14f, -18.48f, -20.0f, -18.48f, -14.14f, -7.65f, -0.0f };
    static readonly float[] RUN_HF_ROT = { 32.5f, 43.02f, 51.95f, 57.91f, 60.0f, 57.91f, 51.95f, 43.02f, 32.5f, 21.98f, 13.05f, 7.09f, 5.0f, 7.09f, 13.05f, 21.98f, 32.5f };
    static readonly float[] RUN_FF_X = { -0.381f, -0.423f, -0.464f, -0.506f, -0.546f, -0.586f, -0.629f, -0.671f, -0.711f, -0.671f, -0.629f, -0.586f, -0.546f, -0.504f, -0.464f, -0.422f, -0.381f };
    static readonly float[] RUN_FF_Y = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.1072f, 0.198f, 0.2586f, 0.28f, 0.2586f, 0.198f, 0.1072f, 0.0f };
    static readonly float[] RUN_FB_X = { -0.063f, -0.022f, 0.020f, 0.062f, 0.102f, 0.144f, 0.185f, 0.227f, 0.267f, 0.227f, 0.185f, 0.143f, 0.102f, 0.060f, 0.020f, -0.022f, -0.063f };
    static readonly float[] RUN_FB_Y = { 0.0f, 0.1072f, 0.198f, 0.2586f, 0.28f, 0.2586f, 0.198f, 0.1072f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] RUN_BODY_ROT = { -8.0f, -7.23f, -6.59f, -6.15f, -6.0f, -6.15f, -6.59f, -7.23f, -8.0f, -8.77f, -9.41f, -9.85f, -10.0f, -9.85f, -9.41f, -8.77f, -8.0f };
    static readonly float[] RUN_BOT_ROT = { 8.0f, 7.23f, 6.59f, 6.15f, 6.0f, 6.15f, 6.59f, 7.23f, 8.0f, 8.77f, 9.41f, 9.85f, 10.0f, 9.85f, 9.41f, 8.77f, 8.0f };

    // ── Attack (24프레임) ───────────────────────────────────────────
    // f0~f8:  활 당기기 (SB -45, HB -30, Body 뒤로)
    // f9~f12: 조준 유지
    // f13~f15: 발사 (SB +20, Body 앞으로 훅)
    // f16~f23: 복귀
    static readonly float[] ATK_BOW_ROT = { -60f, -58f, -55f, -53f, -52f, -52f, -52f, -65f, -70f, -66f, -63f, -61f, -60f, -60f };
    static readonly float[] ATK_BOW_HEAD_ROT = { 0f, -1f, -2f, -3f, -3f, -3f, -4f, 2f, 3f, 2f, 1f, 0f, 0f, 0f };
    static readonly float[] ATK_SB_ROT = { 0f, 15f, 30f, 40f, 45f, 45f, 48f, -20f, -30f, -15f, -6f, -2f, 0f, 0f };
    static readonly float[] ATK_HB_ROT = { 0f, 8f, 16f, 22f, 25f, 25f, 26f, -15f, -22f, -10f, -4f, -1f, 0f, 0f };
    static readonly float[] ATK_SF_ROT = { 0f, 20f, 42f, 55f, 60f, 60f, 62f, 30f, 15f, 8f, 4f, 1f, 0f, 0f };
    static readonly float[] ATK_HF_ROT = { 0f, 10f, 22f, 28f, 30f, 30f, 32f, 15f, 8f, 4f, 2f, 0f, 0f, 0f };
    static readonly float[] ATK_BODY_ROT = { 0f, -2f, -4f, -5f, -5f, -5f, -6f, 3f, 5f, 3f, 1f, 0f, 0f, 0f };
    static readonly float[] ATK_BOT_ROT = { 0f, 2f, 4f, 5f, 5f, 5f, 6f, -3f, -5f, -3f, -1f, 0f, 0f, 0f };
    static readonly float[] ATK_BODY_Y = { 0.0f, 0.005f, 0.01f, 0.015f, 0.02f, 0.02f, 0.02f, -0.01f, -0.02f, -0.01f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] ATK_BOTTOM_Y = { -0.559f, -0.564f, -0.569f, -0.574f, -0.579f, -0.579f, -0.579f, -0.549f, -0.539f, -0.549f, -0.559f, -0.559f, -0.559f, -0.559f };

    // ================================================================
    [MenuItem("Tools/2/EArcher2/Generate All")]
    public static void GenerateAll()
    {
        GenerateIdle(); GenerateWalk(); GenerateRun(); GenerateAttack();
        GenerateAC();
        Debug.Log("[EArcher2] 전체 애니메이션 생성 완료");
    }

    [MenuItem("Tools/2/EArcher2/Generate Idle")]
    public static void GenerateIdle()
    {
        var clip = MakeClip("EArcher2_Idle", 48, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 48);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, 4f, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", IDLE_BODY_Y); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);
        L(HEAD, "localPosition.x", -0.069f); L(HEAD, "localPosition.y", 0.497f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", ATK_BOW_HEAD_ROT);
        L(SF, "localPosition.x", -0.578f); C(SF, "localPosition.y", IDLE_SF_Y); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.009f); L(HF, "localPosition.y", -0.370f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.063f); C(SB, "localPosition.y", IDLE_SB_Y); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.232f); L(HB, "localPosition.y", -0.308f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(BOW, "localPosition.x", 0.118f); L(BOW, "localPosition.y", -0.319f); L(BOW, "localPosition.z", 0f);
        C(BOW, "localEulerAngles.z", ATK_BOW_ROT);
        L(BOTTOM, "localPosition.x", 0f); C(BOTTOM, "localPosition.y", IDLE_BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.381f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.057f); L(FOOT_B, "localPosition.y", 0f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "EArcher2_Idle");
    }

    [MenuItem("Tools/2/EArcher2/Generate Walk")]
    public static void GenerateWalk()
    {
        var clip = MakeClip("EArcher2_Walk", 16, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 16);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, 16 / 12f, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", WALK_BODY_Y); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", WALK_BODY_ROT);
        L(HEAD, "localPosition.x", -0.069f); L(HEAD, "localPosition.y", 0.497f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", ATK_BOW_HEAD_ROT);
        L(SF, "localPosition.x", -0.578f); L(SF, "localPosition.y", 0.459f); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.009f); L(HF, "localPosition.y", -0.370f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.063f); L(SB, "localPosition.y", 0.413f); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.232f); L(HB, "localPosition.y", -0.308f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(BOW, "localPosition.x", 0.118f); L(BOW, "localPosition.y", -0.319f); L(BOW, "localPosition.z", 0f);
        C(BOW, "localEulerAngles.z", ATK_BOW_ROT);
        L(BOTTOM, "localPosition.x", 0f); L(BOTTOM, "localPosition.y", -0.559f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", WALK_BOT_ROT);
        C(FOOT_F, "localPosition.x", WALK_FF_X); C(FOOT_F, "localPosition.y", WALK_FF_Y); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        C(FOOT_B, "localPosition.x", WALK_FB_X); C(FOOT_B, "localPosition.y", WALK_FB_Y); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "EArcher2_Walk");
    }

    [MenuItem("Tools/2/EArcher2/Generate Run")]
    public static void GenerateRun()
    {
        var clip = MakeClip("EArcher2_Run", 17, true);
        void C(string path, string prop, float[] vals) => SetCurveSpeed(clip, path, prop, vals, 17, 3f);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, 17 / 12f / 3f, val));

        L(BODY, "localPosition.x", 0f); L(BODY, "localPosition.y", 0f); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", RUN_BODY_ROT);
        L(HEAD, "localPosition.x", -0.069f); L(HEAD, "localPosition.y", 0.497f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", RUN_HEAD_ROT);
        L(SF, "localPosition.x", -0.578f); L(SF, "localPosition.y", 0.459f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", RUN_SF_ROT);
        L(HF, "localPosition.x", -0.009f); L(HF, "localPosition.y", -0.37f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", RUN_HF_ROT);
        L(SB, "localPosition.x", 0.063f); L(SB, "localPosition.y", 0.413f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", RUN_SB_ROT);
        L(HB, "localPosition.x", 0.232f); L(HB, "localPosition.y", -0.308f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(BOW, "localPosition.x", 0.118f); L(BOW, "localPosition.y", -0.319f); L(BOW, "localPosition.z", 0f);
        L(BOW, "localEulerAngles.z", -60f);
        L(BOTTOM, "localPosition.x", 0.0f); L(BOTTOM, "localPosition.y", -0.559f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", RUN_BOT_ROT);
        C(FOOT_F, "localPosition.x", RUN_FF_X); C(FOOT_F, "localPosition.y", RUN_FF_Y); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        C(FOOT_B, "localPosition.x", RUN_FB_X); C(FOOT_B, "localPosition.y", RUN_FB_Y); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "EArcher2_Run");
    }

    public static void GenerateAttack()
    {
        var clip = MakeClip("EArcher2_Attack", 14, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 14);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, 14 / 12f, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", ATK_BODY_Y); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", ATK_BODY_ROT);
        L(HEAD, "localPosition.x", -0.069f); L(HEAD, "localPosition.y", 0.497f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", ATK_BOW_HEAD_ROT);
        L(SF, "localPosition.x", -0.578f); L(SF, "localPosition.y", 0.459f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", ATK_SF_ROT);
        L(HF, "localPosition.x", -0.009f); L(HF, "localPosition.y", -0.370f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", ATK_HF_ROT);
        L(SB, "localPosition.x", 0.063f); L(SB, "localPosition.y", 0.413f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", ATK_SB_ROT);
        L(HB, "localPosition.x", 0.232f); L(HB, "localPosition.y", -0.308f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", ATK_HB_ROT);
        L(BOW, "localPosition.x", 0.118f); L(BOW, "localPosition.y", -0.319f); L(BOW, "localPosition.z", 0f);
        C(BOW, "localEulerAngles.z", ATK_BOW_ROT);
        L(BOTTOM, "localPosition.x", 0f); C(BOTTOM, "localPosition.y", ATK_BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", ATK_BOT_ROT);
        L(FOOT_F, "localPosition.x", -0.381f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.057f); L(FOOT_B, "localPosition.y", 0f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "EArcher2_Attack");
    }

    static AnimationClip MakeClip(string name, int totalFrames, bool loop)
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            if (!AssetDatabase.IsValidFolder("Assets/Animations/2"))
                AssetDatabase.CreateFolder("Assets/Animations", "2");
            AssetDatabase.CreateFolder("Assets/Animations/2", "EArcher 2");
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
        Debug.Log($"[EArcher2] {name} → {assetPath}");
    }
    // ================================================================
    // Generate AC
    // ================================================================
    [MenuItem("Tools/2/EArcher2/Generate AC")]
    public static void GenerateAC()
    {
        string acPath = $"{SAVE_PATH}/EArcher2_AC.controller";

        // AC 생성 또는 로드
        var ac = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(acPath);
        if (ac == null)
            ac = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(acPath);

        var rootSM = ac.layers[0].stateMachine;

        // 기존 State 이름 목록
        var existingNames = new System.Collections.Generic.HashSet<string>();
        foreach (var s in rootSM.states) existingNames.Add(s.state.name);

        // 클립 이름 목록
        string[] clipNames = { "EArcher2_Idle", "EArcher2_Walk", "EArcher2_Run", "EArcher2_Attack" };

        foreach (string clipName in clipNames)
        {
            string clipPath = $"{SAVE_PATH}/{clipName}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
            {
                Debug.LogWarning($"[EArcher2] 클립 없음: {clipPath} — Generate All 먼저 실행하세요");
                continue;
            }

            if (!existingNames.Contains(clipName))
            {
                var state = rootSM.AddState(clipName);
                state.motion = clip;
                Debug.Log($"[EArcher2] AC에 추가: {clipName}");
            }
            else
            {
                // 이미 있으면 motion만 갱신
                foreach (var cs in rootSM.states)
                {
                    if (cs.state.name == clipName)
                    { cs.state.motion = clip; break; }
                }
            }
        }

        // 첫 State를 Default로
        if (rootSM.states.Length > 0)
            rootSM.defaultState = rootSM.states[0].state;

        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[EArcher2] AC 저장 완료: {acPath}");
        MobHitDeathSetup.SetupMob(2, "EArcher 2");
    }
}