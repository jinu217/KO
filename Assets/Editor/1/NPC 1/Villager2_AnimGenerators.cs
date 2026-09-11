using UnityEngine;
using UnityEditor;

public class Villager2_AnimGenerators
{
    const string SAVE_PATH = "Assets/Animations/Villager2";
    const float FRAME_RATE = 12f;
    const float DUR_48 = 48 / 12f;
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
    //   Body pos(0,0), Head pos(-0.093, 0.434)
    //   SF pos(-0.320, 0.292), HF pos(0.013, -0.164)
    //   SB pos(0.118, 0.300), HB pos(0.012, -0.172)
    //   Bottom pos(0, -0.583), Foot_F pos(-0.155, 0), Foot_B pos(0.079, 0)

    // ── 숨쉬기 배열 (48프레임) ──────────────────────────────────────
    static readonly float[] BODY_Y = { 0.0f, 0.0049f, 0.0097f, 0.0144f, 0.0187f, 0.0228f, 0.0265f, 0.0298f, 0.0325f, 0.0346f, 0.0362f, 0.0372f, 0.0375f, 0.0372f, 0.0362f, 0.0346f, 0.0325f, 0.0298f, 0.0265f, 0.0228f, 0.0187f, 0.0144f, 0.0097f, 0.0049f, 0.0f, -0.0049f, -0.0097f, -0.0144f, -0.0187f, -0.0228f, -0.0265f, -0.0298f, -0.0325f, -0.0346f, -0.0362f, -0.0372f, -0.0375f, -0.0372f, -0.0362f, -0.0346f, -0.0325f, -0.0298f, -0.0265f, -0.0228f, -0.0188f, -0.0144f, -0.0097f, -0.0049f };
    static readonly float[] HEAD_Y = { 0.434f, 0.4364f, 0.4389f, 0.4412f, 0.4434f, 0.4454f, 0.4473f, 0.4489f, 0.4502f, 0.4513f, 0.4521f, 0.4526f, 0.4527f, 0.4526f, 0.4521f, 0.4513f, 0.4502f, 0.4489f, 0.4473f, 0.4454f, 0.4434f, 0.4412f, 0.4389f, 0.4364f, 0.434f, 0.4316f, 0.4291f, 0.4268f, 0.4246f, 0.4226f, 0.4207f, 0.4191f, 0.4178f, 0.4167f, 0.4159f, 0.4154f, 0.4153f, 0.4154f, 0.4159f, 0.4167f, 0.4178f, 0.4191f, 0.4207f, 0.4226f, 0.4246f, 0.4268f, 0.4291f, 0.4316f };
    static readonly float[] SF_Y = { 0.2806f, 0.2816f, 0.2827f, 0.284f, 0.2854f, 0.287f, 0.2886f, 0.2903f, 0.292f, 0.2937f, 0.2954f, 0.297f, 0.2986f, 0.3f, 0.3013f, 0.3024f, 0.3034f, 0.3041f, 0.3047f, 0.305f, 0.3051f, 0.305f, 0.3047f, 0.3041f, 0.3034f, 0.3024f, 0.3013f, 0.3f, 0.2986f, 0.297f, 0.2954f, 0.2937f, 0.292f, 0.2903f, 0.2886f, 0.287f, 0.2854f, 0.284f, 0.2827f, 0.2816f, 0.2806f, 0.2799f, 0.2793f, 0.279f, 0.2789f, 0.279f, 0.2793f, 0.2799f };
    static readonly float[] SB_Y = { 0.2886f, 0.2896f, 0.2907f, 0.292f, 0.2934f, 0.295f, 0.2966f, 0.2983f, 0.3f, 0.3017f, 0.3034f, 0.305f, 0.3066f, 0.308f, 0.3093f, 0.3104f, 0.3114f, 0.3121f, 0.3127f, 0.313f, 0.3131f, 0.313f, 0.3127f, 0.3121f, 0.3114f, 0.3104f, 0.3093f, 0.308f, 0.3066f, 0.305f, 0.3034f, 0.3017f, 0.3f, 0.2983f, 0.2966f, 0.295f, 0.2934f, 0.292f, 0.2907f, 0.2896f, 0.2886f, 0.2879f, 0.2873f, 0.287f, 0.2869f, 0.287f, 0.2873f, 0.2879f };
    static readonly float[] BOTTOM_Y = { -0.583f, -0.5879f, -0.5927f, -0.5974f, -0.6017f, -0.6058f, -0.6095f, -0.6128f, -0.6155f, -0.6176f, -0.6192f, -0.6202f, -0.6205f, -0.6202f, -0.6192f, -0.6176f, -0.6155f, -0.6128f, -0.6095f, -0.6058f, -0.6017f, -0.5974f, -0.5927f, -0.5879f, -0.583f, -0.5781f, -0.5733f, -0.5686f, -0.5643f, -0.5602f, -0.5565f, -0.5532f, -0.5505f, -0.5484f, -0.5468f, -0.5458f, -0.5455f, -0.5458f, -0.5468f, -0.5484f, -0.5505f, -0.5532f, -0.5565f, -0.5602f, -0.5642f, -0.5686f, -0.5733f, -0.5781f };

    // ── Walk 배열 (32프레임) ────────────────────────────────────────
    static readonly float[] WALK_FF_X = { -0.155f, -0.159f, -0.163f, -0.166f, -0.17f, -0.174f, -0.177f, -0.181f, -0.185f, -0.189f, -0.193f, -0.196f, -0.2f, -0.204f, -0.207f, -0.211f, -0.215f, -0.211f, -0.207f, -0.204f, -0.2f, -0.196f, -0.193f, -0.189f, -0.185f, -0.181f, -0.177f, -0.174f, -0.17f, -0.166f, -0.163f, -0.159f };
    static readonly float[] WALK_FF_Y = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0117f, 0.023f, 0.0333f, 0.0424f, 0.0499f, 0.0554f, 0.0588f, 0.06f, 0.0588f, 0.0554f, 0.0499f, 0.0424f, 0.0333f, 0.023f, 0.0117f };
    static readonly float[] WALK_FB_X = { 0.019f, 0.023f, 0.026f, 0.03f, 0.034f, 0.038f, 0.041f, 0.045f, 0.049f, 0.053f, 0.056f, 0.06f, 0.064f, 0.068f, 0.071f, 0.075f, 0.079f, 0.075f, 0.072f, 0.068f, 0.064f, 0.06f, 0.057f, 0.053f, 0.049f, 0.045f, 0.042f, 0.038f, 0.034f, 0.03f, 0.027f, 0.023f };
    static readonly float[] WALK_FB_Y = { 0.0f, 0.0117f, 0.023f, 0.0333f, 0.0424f, 0.0499f, 0.0554f, 0.0588f, 0.06f, 0.0588f, 0.0554f, 0.0499f, 0.0424f, 0.0333f, 0.023f, 0.0117f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] WALK_BODY_ROT = { -0.0f, -0.78f, -1.53f, -2.22f, -2.83f, -3.33f, -3.7f, -3.92f, -4.0f, -3.92f, -3.7f, -3.33f, -2.83f, -2.22f, -1.53f, -0.78f, -0.0f, 0.78f, 1.53f, 2.22f, 2.83f, 3.33f, 3.7f, 3.92f, 4.0f, 3.92f, 3.7f, 3.33f, 2.83f, 2.22f, 1.53f, 0.78f };
    static readonly float[] WALK_BOT_ROT = { 0.0f, 0.78f, 1.53f, 2.22f, 2.83f, 3.33f, 3.7f, 3.92f, 4.0f, 3.92f, 3.7f, 3.33f, 2.83f, 2.22f, 1.53f, 0.78f, 0.0f, -0.78f, -1.53f, -2.22f, -2.83f, -3.33f, -3.7f, -3.92f, -4.0f, -3.92f, -3.7f, -3.33f, -2.83f, -2.22f, -1.53f, -0.78f };
    static readonly float[] WALK_SF_ROT = { -0.0f, -4.88f, -9.57f, -13.89f, -17.68f, -20.79f, -23.1f, -24.52f, -25.0f, -24.52f, -23.1f, -20.79f, -17.68f, -13.89f, -9.57f, -4.88f, -0.0f, 4.88f, 9.57f, 13.89f, 17.68f, 20.79f, 23.1f, 24.52f, 25.0f, 24.52f, 23.1f, 20.79f, 17.68f, 13.89f, 9.57f, 4.88f };
    static readonly float[] WALK_SB_ROT = { 0.0f, 4.88f, 9.57f, 13.89f, 17.68f, 20.79f, 23.1f, 24.52f, 25.0f, 24.52f, 23.1f, 20.79f, 17.68f, 13.89f, 9.57f, 4.88f, 0.0f, -4.88f, -9.57f, -13.89f, -17.68f, -20.79f, -23.1f, -24.52f, -25.0f, -24.52f, -23.1f, -20.79f, -17.68f, -13.89f, -9.57f, -4.88f };
    static readonly float[] WALK_HF_ROT = { 10.0f, 8.05f, 6.17f, 4.44f, 2.93f, 1.69f, 0.76f, 0.19f, 0.0f, 0.19f, 0.76f, 1.69f, 2.93f, 4.44f, 6.17f, 8.05f, 10.0f, 11.95f, 13.83f, 15.56f, 17.07f, 18.31f, 19.24f, 19.81f, 20.0f, 19.81f, 19.24f, 18.31f, 17.07f, 15.56f, 13.83f, 11.95f };
    static readonly float[] WALK_HB_ROT = { 10.0f, 11.95f, 13.83f, 15.56f, 17.07f, 18.31f, 19.24f, 19.81f, 20.0f, 19.81f, 19.24f, 18.31f, 17.07f, 15.56f, 13.83f, 11.95f, 10.0f, 8.05f, 6.17f, 4.44f, 2.93f, 1.69f, 0.76f, 0.19f, 0.0f, 0.19f, 0.76f, 1.69f, 2.93f, 4.44f, 6.17f, 8.05f };
    static readonly float[] WALK_BODY_Y = { 0.0f, 0.0031f, 0.0057f, 0.0074f, 0.008f, 0.0074f, 0.0057f, 0.0031f, 0.0f, 0.0031f, 0.0057f, 0.0074f, 0.008f, 0.0074f, 0.0057f, 0.0031f, 0.0f, 0.0031f, 0.0057f, 0.0074f, 0.008f, 0.0074f, 0.0057f, 0.0031f, 0.0f, 0.0031f, 0.0057f, 0.0074f, 0.008f, 0.0074f, 0.0057f, 0.0031f };

    // ── Wave 배열 (48프레임) ────────────────────────────────────────
    static readonly float[] WAVE_SB_ROT = { 0.0f, 35.0f, 70.0f, 105.0f, 130.0f, 137.07f, 140.0f, 137.07f, 130.0f, 122.93f, 120.0f, 122.93f, 130.0f, 137.07f, 140.0f, 137.07f, 130.0f, 122.93f, 120.0f, 86.67f, 43.33f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] WAVE_HB_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 27.5f, 39.87f, 45.0f, 39.87f, 27.5f, 15.13f, 10.0f, 15.13f, 27.5f, 39.87f, 45.0f, 39.87f, 27.5f, 15.13f, 10.0f, 6.67f, 3.33f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] WAVE_HEAD_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 3.54f, 5.0f, 3.54f, 0.0f, -3.54f, -5.0f, -3.54f, -0.0f, 3.54f, 5.0f, 3.54f, 0.0f, -3.54f, -5.0f, -3.33f, -1.67f, -0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

    [MenuItem("Tools/Villager2/Generate All")]
    public static void GenerateAll()
    {
        GenerateIdle();
        GenerateWalk();
        GenerateWave();
        GenerateIdleWave();
        UnityEngine.Debug.Log("[Villager2] 전체 애니메이션 생성 완료");
    }

    [MenuItem("Tools/Villager2/Generate Idle")]
    public static void GenerateIdle()
    {
        var clip = MakeClip("Villager2_Idle", 48, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 48);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_48, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", BODY_Y); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);
        L(HEAD, "localPosition.x", -0.093f); C(HEAD, "localPosition.y", HEAD_Y); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);
        L(SF, "localPosition.x", -0.320f); C(SF, "localPosition.y", SF_Y); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", 0.013f); L(HF, "localPosition.y", -0.164f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.118f); C(SB, "localPosition.y", SB_Y); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.012f); L(HB, "localPosition.y", -0.172f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(BOTTOM, "localPosition.x", 0f); C(BOTTOM, "localPosition.y", BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.155f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.079f); L(FOOT_B, "localPosition.y", 0f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "Villager2_Idle");
    }

    [MenuItem("Tools/Villager2/Generate Walk")]
    public static void GenerateWalk()
    {
        var clip = MakeClip("Villager2_Walk", 32, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 32);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_32, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", WALK_BODY_Y); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", WALK_BODY_ROT);
        L(HEAD, "localPosition.x", -0.093f); L(HEAD, "localPosition.y", 0.434f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);
        L(SF, "localPosition.x", -0.320f); L(SF, "localPosition.y", 0.292f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", WALK_SF_ROT);
        L(HF, "localPosition.x", 0.013f); L(HF, "localPosition.y", -0.164f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", WALK_HF_ROT);
        L(SB, "localPosition.x", 0.118f); L(SB, "localPosition.y", 0.300f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", WALK_SB_ROT);
        L(HB, "localPosition.x", 0.012f); L(HB, "localPosition.y", -0.172f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", WALK_HB_ROT);
        L(BOTTOM, "localPosition.x", 0f); L(BOTTOM, "localPosition.y", -0.583f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", WALK_BOT_ROT);
        C(FOOT_F, "localPosition.x", WALK_FF_X); C(FOOT_F, "localPosition.y", WALK_FF_Y); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        C(FOOT_B, "localPosition.x", WALK_FB_X); C(FOOT_B, "localPosition.y", WALK_FB_Y); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "Villager2_Walk");
    }

    [MenuItem("Tools/Villager2/Generate Wave")]
    public static void GenerateWave()
    {
        var clip = MakeClip("Villager2_Wave", 48, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 48);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_48, val));

        L(BODY, "localPosition.x", 0f); L(BODY, "localPosition.y", 0f); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);
        L(HEAD, "localPosition.x", -0.093f); L(HEAD, "localPosition.y", 0.434f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", WAVE_HEAD_ROT);
        L(SF, "localPosition.x", -0.320f); L(SF, "localPosition.y", 0.292f); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", 0.013f); L(HF, "localPosition.y", -0.164f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.118f); L(SB, "localPosition.y", 0.300f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", WAVE_SB_ROT);
        L(HB, "localPosition.x", 0.012f); L(HB, "localPosition.y", -0.172f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", WAVE_HB_ROT);
        L(BOTTOM, "localPosition.x", 0f); L(BOTTOM, "localPosition.y", -0.583f); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.155f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.079f); L(FOOT_B, "localPosition.y", 0f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "Villager2_Wave");
    }

    [MenuItem("Tools/Villager2/Generate IdleWave")]
    public static void GenerateIdleWave()
    {
        var clip = MakeClip("Villager2_IdleWave", 48, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 48);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_48, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", BODY_Y); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);
        L(HEAD, "localPosition.x", -0.093f); C(HEAD, "localPosition.y", HEAD_Y); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", WAVE_HEAD_ROT);
        L(SF, "localPosition.x", -0.320f); C(SF, "localPosition.y", SF_Y); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", 0.013f); L(HF, "localPosition.y", -0.164f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.118f); C(SB, "localPosition.y", SB_Y); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", WAVE_SB_ROT);
        L(HB, "localPosition.x", 0.012f); L(HB, "localPosition.y", -0.172f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", WAVE_HB_ROT);
        L(BOTTOM, "localPosition.x", 0f); C(BOTTOM, "localPosition.y", BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.155f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.079f); L(FOOT_B, "localPosition.y", 0f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "Villager2_IdleWave");
    }

    static AnimationClip MakeClip(string name, int totalFrames, bool loop)
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "Villager2");
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
        Debug.Log($"[Villager2] {name} → {assetPath}");
    }
}