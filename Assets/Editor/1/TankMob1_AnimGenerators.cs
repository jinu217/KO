using UnityEngine;
using UnityEditor;

public class TankMob1_AnimGenerators
{
    const string SAVE_PATH = "Assets/Animations/TankMob 1";
    const float FRAME_RATE = 12f;
    const float DUR_48 = 48 / 12f;
    const float DUR_32 = 32 / 12f;

    const string BODY = "Body";
    const string HEAD = "Body/Head";
    const string SF = "Body/Shoulder_F";
    const string HF = "Body/Shoulder_F/Hand_F";
    const string SB = "Body/Shoulder_B";
    const string HB = "Body/Shoulder_B/Hand_B";
    const string CLUB = "Body/Shoulder_B/Hand_B/Club";
    const string BOTTOM = "Body/Bottom";
    const string FOOT_F = "Body/Bottom/Foot_F";
    const string FOOT_B = "Body/Bottom/Foot_B";

    // ================================================================
    // rest 실측값:
    //   Body(0,0), Head(-0.156, 0.685)
    //   SF(-0.594, 0.500), HF(-0.141, -0.328)
    //   SB(0.235, 0.516), HB(0.141, -0.344)
    //   Club(0.063, -0.360) rot=-90
    //   Bottom(0, -0.580), Foot_F(-0.475, 0), Foot_B(0.041, 0)
    // ================================================================

    // ── Idle 배열 (48프레임) ────────────────────────────────────────
    static readonly float[] IDLE_BODY_Y = { 0.0f, 0.0078f, 0.0155f, 0.023f, 0.03f, 0.0365f, 0.0424f, 0.0476f, 0.052f, 0.0554f, 0.058f, 0.0595f, 0.06f, 0.0595f, 0.058f, 0.0554f, 0.052f, 0.0476f, 0.0424f, 0.0365f, 0.03f, 0.023f, 0.0155f, 0.0078f, 0.0f, -0.0078f, -0.0155f, -0.023f, -0.03f, -0.0365f, -0.0424f, -0.0476f, -0.052f, -0.0554f, -0.058f, -0.0595f, -0.06f, -0.0595f, -0.058f, -0.0554f, -0.052f, -0.0476f, -0.0424f, -0.0365f, -0.03f, -0.023f, -0.0155f, -0.0078f };
    static readonly float[] IDLE_HEAD_Y = { 0.685f, 0.6889f, 0.6928f, 0.6965f, 0.7f, 0.7033f, 0.7062f, 0.7088f, 0.711f, 0.7127f, 0.714f, 0.7147f, 0.715f, 0.7147f, 0.714f, 0.7127f, 0.711f, 0.7088f, 0.7062f, 0.7033f, 0.7f, 0.6965f, 0.6928f, 0.6889f, 0.685f, 0.6811f, 0.6772f, 0.6735f, 0.67f, 0.6667f, 0.6638f, 0.6612f, 0.659f, 0.6573f, 0.656f, 0.6553f, 0.655f, 0.6553f, 0.656f, 0.6573f, 0.659f, 0.6612f, 0.6638f, 0.6667f, 0.67f, 0.6735f, 0.6772f, 0.6811f };
    static readonly float[] IDLE_SF_Y = { 0.4688f, 0.4714f, 0.4745f, 0.4781f, 0.482f, 0.4862f, 0.4907f, 0.4953f, 0.5f, 0.5047f, 0.5093f, 0.5138f, 0.518f, 0.5219f, 0.5255f, 0.5286f, 0.5312f, 0.5333f, 0.5348f, 0.5357f, 0.536f, 0.5357f, 0.5348f, 0.5333f, 0.5312f, 0.5286f, 0.5255f, 0.5219f, 0.518f, 0.5138f, 0.5093f, 0.5047f, 0.5f, 0.4953f, 0.4907f, 0.4862f, 0.482f, 0.4781f, 0.4745f, 0.4714f, 0.4688f, 0.4667f, 0.4652f, 0.4643f, 0.464f, 0.4643f, 0.4652f, 0.4667f };
    static readonly float[] IDLE_SB_Y = { 0.4848f, 0.4874f, 0.4905f, 0.4941f, 0.498f, 0.5022f, 0.5067f, 0.5113f, 0.516f, 0.5207f, 0.5253f, 0.5298f, 0.534f, 0.5379f, 0.5415f, 0.5446f, 0.5472f, 0.5493f, 0.5508f, 0.5517f, 0.552f, 0.5517f, 0.5508f, 0.5493f, 0.5472f, 0.5446f, 0.5415f, 0.5379f, 0.534f, 0.5298f, 0.5253f, 0.5207f, 0.516f, 0.5113f, 0.5067f, 0.5022f, 0.498f, 0.4941f, 0.4905f, 0.4874f, 0.4848f, 0.4827f, 0.4812f, 0.4803f, 0.48f, 0.4803f, 0.4812f, 0.4827f };
    static readonly float[] IDLE_BOTTOM_Y = { -0.58f, -0.5878f, -0.5955f, -0.603f, -0.61f, -0.6165f, -0.6224f, -0.6276f, -0.632f, -0.6354f, -0.638f, -0.6395f, -0.64f, -0.6395f, -0.638f, -0.6354f, -0.632f, -0.6276f, -0.6224f, -0.6165f, -0.61f, -0.603f, -0.5955f, -0.5878f, -0.58f, -0.5722f, -0.5645f, -0.557f, -0.55f, -0.5435f, -0.5376f, -0.5324f, -0.528f, -0.5246f, -0.522f, -0.5205f, -0.52f, -0.5205f, -0.522f, -0.5246f, -0.528f, -0.5324f, -0.5376f, -0.5435f, -0.55f, -0.557f, -0.5645f, -0.5722f };

    // ── Walk 배열 (32프레임) ────────────────────────────────────────
    static readonly float[] WALK_FF_X = { -0.475f, -0.479f, -0.484f, -0.488f, -0.492f, -0.497f, -0.501f, -0.506f, -0.51f, -0.514f, -0.519f, -0.523f, -0.527f, -0.532f, -0.536f, -0.541f, -0.545f, -0.541f, -0.536f, -0.532f, -0.528f, -0.523f, -0.519f, -0.514f, -0.51f, -0.506f, -0.501f, -0.497f, -0.493f, -0.488f, -0.484f, -0.479f };
    static readonly float[] WALK_FF_Y = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0098f, 0.0191f, 0.0278f, 0.0354f, 0.0416f, 0.0462f, 0.049f, 0.05f, 0.049f, 0.0462f, 0.0416f, 0.0354f, 0.0278f, 0.0191f, 0.0098f };
    static readonly float[] WALK_FB_X = { -0.029f, -0.025f, -0.02f, -0.016f, -0.011f, -0.007f, -0.003f, 0.002f, 0.006f, 0.01f, 0.015f, 0.019f, 0.024f, 0.028f, 0.032f, 0.037f, 0.041f, 0.037f, 0.032f, 0.028f, 0.024f, 0.019f, 0.015f, 0.01f, 0.006f, 0.002f, -0.003f, -0.007f, -0.012f, -0.016f, -0.02f, -0.025f };
    static readonly float[] WALK_FB_Y = { 0.0f, 0.0098f, 0.0191f, 0.0278f, 0.0354f, 0.0416f, 0.0462f, 0.049f, 0.05f, 0.049f, 0.0462f, 0.0416f, 0.0354f, 0.0278f, 0.0191f, 0.0098f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] WALK_SF_ROT = { -0.0f, -4.88f, -9.57f, -13.89f, -17.68f, -20.79f, -23.1f, -24.52f, -25.0f, -24.52f, -23.1f, -20.79f, -17.68f, -13.89f, -9.57f, -4.88f, -0.0f, 4.88f, 9.57f, 13.89f, 17.68f, 20.79f, 23.1f, 24.52f, 25.0f, 24.52f, 23.1f, 20.79f, 17.68f, 13.89f, 9.57f, 4.88f };
    static readonly float[] WALK_SB_ROT = { 0.0f, 4.88f, 9.57f, 13.89f, 17.68f, 20.79f, 23.1f, 24.52f, 25.0f, 24.52f, 23.1f, 20.79f, 17.68f, 13.89f, 9.57f, 4.88f, 0.0f, -4.88f, -9.57f, -13.89f, -17.68f, -20.79f, -23.1f, -24.52f, -25.0f, -24.52f, -23.1f, -20.79f, -17.68f, -13.89f, -9.57f, -4.88f };
    static readonly float[] WALK_HF_ROT = { 15.0f, 12.07f, 9.26f, 6.67f, 4.39f, 2.53f, 1.14f, 0.29f, 0.0f, 0.29f, 1.14f, 2.53f, 4.39f, 6.67f, 9.26f, 12.07f, 15.0f, 17.93f, 20.74f, 23.33f, 25.61f, 27.47f, 28.86f, 29.71f, 30.0f, 29.71f, 28.86f, 27.47f, 25.61f, 23.33f, 20.74f, 17.93f };
    static readonly float[] WALK_HB_ROT = { 15.0f, 17.93f, 20.74f, 23.33f, 25.61f, 27.47f, 28.86f, 29.71f, 30.0f, 29.71f, 28.86f, 27.47f, 25.61f, 23.33f, 20.74f, 17.93f, 15.0f, 12.07f, 9.26f, 6.67f, 4.39f, 2.53f, 1.14f, 0.29f, 0.0f, 0.29f, 1.14f, 2.53f, 4.39f, 6.67f, 9.26f, 12.07f };
    static readonly float[] WALK_BODY_ROT = { -0.0f, -0.98f, -1.91f, -2.78f, -3.54f, -4.16f, -4.62f, -4.9f, -5.0f, -4.9f, -4.62f, -4.16f, -3.54f, -2.78f, -1.91f, -0.98f, -0.0f, 0.98f, 1.91f, 2.78f, 3.54f, 4.16f, 4.62f, 4.9f, 5.0f, 4.9f, 4.62f, 4.16f, 3.54f, 2.78f, 1.91f, 0.98f };
    static readonly float[] WALK_BOT_ROT = { 0.0f, 0.98f, 1.91f, 2.78f, 3.54f, 4.16f, 4.62f, 4.9f, 5.0f, 4.9f, 4.62f, 4.16f, 3.54f, 2.78f, 1.91f, 0.98f, 0.0f, -0.98f, -1.91f, -2.78f, -3.54f, -4.16f, -4.62f, -4.9f, -5.0f, -4.9f, -4.62f, -4.16f, -3.54f, -2.78f, -1.91f, -0.98f };
    static readonly float[] WALK_BODY_Y = { 0.0f, 0.0038f, 0.0071f, 0.0092f, 0.01f, 0.0092f, 0.0071f, 0.0038f, 0.0f, 0.0038f, 0.0071f, 0.0092f, 0.01f, 0.0092f, 0.0071f, 0.0038f, 0.0f, 0.0038f, 0.0071f, 0.0092f, 0.01f, 0.0092f, 0.0071f, 0.0038f, 0.0f, 0.0038f, 0.0071f, 0.0092f, 0.01f, 0.0092f, 0.0071f, 0.0038f };

    // ================================================================
    // Generate Idle
    // ================================================================
    [MenuItem("Tools/1/TankMob1/Generate Idle")]
    public static void GenerateIdle()
    {
        var clip = MakeClip("TankMob1_Idle", 48, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 48);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_48, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", IDLE_BODY_Y); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);
        L(HEAD, "localPosition.x", -0.156f); L(HEAD, "localPosition.y", 0.685f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);
        L(SF, "localPosition.x", -0.594f); C(SF, "localPosition.y", IDLE_SF_Y); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.141f); L(HF, "localPosition.y", -0.328f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", RUN_HF_ROT);
        L(SB, "localPosition.x", 0.235f); C(SB, "localPosition.y", IDLE_SB_Y); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.141f); C(HB, "localPosition.y", ATK_HB_Y); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(CLUB, "localPosition.x", 0.063f); L(CLUB, "localPosition.y", -0.360f); L(CLUB, "localPosition.z", 0f);
        L(CLUB, "localEulerAngles.z", -90f);
        L(BOTTOM, "localPosition.x", 0f); C(BOTTOM, "localPosition.y", IDLE_BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.475f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        C(FOOT_B, "localPosition.x", ATK_FB_X); C(FOOT_B, "localPosition.y", ATK_FB_Y); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", ATK_FB_ROT);
        Save(clip, "TankMob1_Idle");
    }

    // ================================================================
    // Generate Walk
    // ================================================================
    [MenuItem("Tools/1/TankMob1/Generate Walk")]
    public static void GenerateWalk()
    {
        var clip = MakeClip("TankMob1_Walk", 32, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 32);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_32, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", WALK_BODY_Y); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", WALK_BODY_ROT);
        L(HEAD, "localPosition.x", -0.156f); L(HEAD, "localPosition.y", 0.685f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);
        L(SF, "localPosition.x", -0.594f); L(SF, "localPosition.y", 0.500f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", RUN_SF_ROT);
        L(HF, "localPosition.x", -0.141f); L(HF, "localPosition.y", -0.328f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", RUN_HF_ROT);
        L(SB, "localPosition.x", 0.235f); C(SB, "localPosition.y", ATK_SB_Y); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.141f); C(HB, "localPosition.y", ATK_HB_Y); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(CLUB, "localPosition.x", 0.063f); L(CLUB, "localPosition.y", -0.360f); L(CLUB, "localPosition.z", 0f);
        L(CLUB, "localEulerAngles.z", -90f);
        L(BOTTOM, "localPosition.x", 0f); L(BOTTOM, "localPosition.y", -0.580f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", WALK_BOT_ROT);
        C(FOOT_F, "localPosition.x", WALK_FF_X); C(FOOT_F, "localPosition.y", WALK_FF_Y); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        C(FOOT_B, "localPosition.x", WALK_FB_X); C(FOOT_B, "localPosition.y", WALK_FB_Y); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);
        Save(clip, "TankMob1_Walk");
    }

    // ── 공통 헬퍼 ───────────────────────────────────────────────────
    static AnimationClip MakeClip(string name, int totalFrames, bool loop)
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "TankMob 1");
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
        Debug.Log($"[TankMob1] {name} → {assetPath}");
    }
    // ── Run 배열 (16프레임) ─────────────────────────────────────────
    static readonly float[] RUN_FF_X = { -0.475f, -0.542f, -0.61f, -0.677f, -0.745f, -0.677f, -0.61f, -0.542f };
    static readonly float[] RUN_FF_Y = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.1485f, 0.21f, 0.1485f };
    static readonly float[] RUN_FB_X = { -0.229f, -0.162f, -0.094f, -0.026f, 0.041f, -0.027f, -0.094f, -0.162f };
    static readonly float[] RUN_FB_Y = { 0.0f, 0.1485f, 0.21f, 0.1485f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] RUN_BODY_ROT = { -8.0f, -5.88f, -5.0f, -5.88f, -8.0f, -10.12f, -11.0f, -10.12f };
    static readonly float[] RUN_BOT_ROT = { 8.0f, 5.88f, 5.0f, 5.88f, 8.0f, 10.12f, 11.0f, 10.12f };
    static readonly float[] RUN_SF_ROT = { 0.0f, 3.54f, 5.0f, 3.54f, 0.0f, -3.54f, -5.0f, -3.54f };
    static readonly float[] RUN_HF_ROT = { 60.0f, 63.54f, 65.0f, 63.54f, 60.0f, 56.46f, 55.0f, 56.46f };
    static readonly float[] RUN_HB_ROT = { 120.0f, 121.77f, 122.5f, 121.77f, 120.0f, 118.23f, 117.5f, 118.23f };

    // ================================================================
    // Generate Run
    // ================================================================
    [MenuItem("Tools/1/TankMob1/Generate Run")]
    public static void GenerateRun()
    {
        const float DUR_RUN = 8 / 12f;
        var clip = MakeClip("TankMob1_Run", 8, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 8);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_RUN, val));

        L(BODY, "localPosition.x", 0f); L(BODY, "localPosition.y", 0f); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", RUN_BODY_ROT);
        L(HEAD, "localPosition.x", -0.156f); L(HEAD, "localPosition.y", 0.685f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);
        L(SF, "localPosition.x", -0.594f); L(SF, "localPosition.y", 0.500f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", RUN_SF_ROT);
        L(HF, "localPosition.x", -0.141f); L(HF, "localPosition.y", -0.328f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", RUN_HF_ROT);
        L(SB, "localPosition.x", 0.235f); C(SB, "localPosition.y", ATK_SB_Y); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.141f); C(HB, "localPosition.y", ATK_HB_Y); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", RUN_HB_ROT);
        L(CLUB, "localPosition.x", 0.063f); L(CLUB, "localPosition.y", -0.360f); L(CLUB, "localPosition.z", 0f);
        L(CLUB, "localEulerAngles.z", -90f);
        L(BOTTOM, "localPosition.x", 0f); L(BOTTOM, "localPosition.y", -0.580f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", RUN_BOT_ROT);
        C(FOOT_F, "localPosition.x", RUN_FF_X); C(FOOT_F, "localPosition.y", RUN_FF_Y); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        C(FOOT_B, "localPosition.x", RUN_FB_X); C(FOOT_B, "localPosition.y", RUN_FB_Y); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

        Save(clip, "TankMob1_Run");
    }
    // ── Attack 배열 (36프레임) ──────────────────────────────────────
    static readonly float[] ATK_FB_ROT = { 0.0f, 4.5f, 9.0f, 13.5f, 18.0f, 22.5f, 27.0f, 31.5f, 36.0f, 40.5f, 45.0f, 30.0f, 15.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] ATK_FB_X = { 0.041f, 0.0469f, 0.0528f, 0.0587f, 0.0646f, 0.0705f, 0.0764f, 0.0823f, 0.0882f, 0.0941f, 0.1f, 0.0803f, 0.0607f, 0.041f, 0.041f, 0.041f, 0.041f, 0.041f, 0.041f, 0.041f };
    static readonly float[] ATK_FB_Y = { 0.0f, 0.025f, 0.05f, 0.075f, 0.1f, 0.125f, 0.15f, 0.175f, 0.2f, 0.225f, 0.25f, 0.1633f, 0.0767f, -0.01f, -0.005f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] ATK_SB_Y = { 0.516f, 0.521f, 0.526f, 0.531f, 0.536f, 0.541f, 0.546f, 0.551f, 0.556f, 0.561f, 0.566f, 0.5393f, 0.5127f, 0.486f, 0.486f, 0.486f, 0.4935f, 0.501f, 0.5085f, 0.516f };
    static readonly float[] ATK_HB_Y = { -0.344f, -0.339f, -0.334f, -0.329f, -0.324f, -0.319f, -0.314f, -0.309f, -0.304f, -0.299f, -0.294f, -0.3207f, -0.3473f, -0.374f, -0.374f, -0.374f, -0.3665f, -0.359f, -0.3515f, -0.344f };
    static readonly float[] ATK_SB_ROT = { 0.0f, 6.0f, 12.0f, 18.0f, 24.0f, 30.0f, 36.0f, 42.0f, 48.0f, 54.0f, 60.0f, 31.67f, 3.33f, -25.0f, -25.0f, -25.0f, -18.75f, -12.5f, -6.25f, 0.0f };
    static readonly float[] ATK_HB_ROT = { 0.0f, 7.5f, 15.0f, 22.5f, 30.0f, 37.5f, 45.0f, 52.5f, 60.0f, 67.5f, 75.0f, 58.33f, 41.67f, 25.0f, 25.0f, 25.0f, 18.75f, 12.5f, 6.25f, 0.0f };
    static readonly float[] ATK_BODY_ROT = { 0.0f, 0.8f, 1.6f, 2.4f, 3.2f, 4.0f, 4.8f, 5.6f, 6.4f, 7.2f, 8.0f, 0.33f, -7.33f, -15.0f, -13.5f, -12.0f, -9.0f, -6.0f, -3.0f, 0.0f };
    static readonly float[] ATK_BOT_ROT = { -0.0f, -0.8f, -1.6f, -2.4f, -3.2f, -4.0f, -4.8f, -5.6f, -6.4f, -7.2f, -8.0f, -0.33f, 7.33f, 15.0f, 13.5f, 12.0f, 9.0f, 6.0f, 3.0f, -0.0f };
    static readonly float[] ATK_SF_ROT = { 0.0f, -2.0f, -4.0f, -6.0f, -8.0f, -10.0f, -12.0f, -14.0f, -16.0f, -18.0f, -20.0f, -5.0f, 10.0f, 25.0f, 22.5f, 20.0f, 15.0f, 10.0f, 5.0f, 0.0f };
    static readonly float[] ATK_HF_ROT = { 0.0f, -1.2f, -2.4f, -3.6f, -4.8f, -6.0f, -7.2f, -8.4f, -9.6f, -10.8f, -12.0f, -3.0f, 6.0f, 15.0f, 13.5f, 12.0f, 9.0f, 6.0f, 3.0f, 0.0f };
    static readonly float[] ATK_BODY_Y = { 0.0f, 0.0f, 0.0f, 0.01f, 0.01f, 0.01f, 0.01f, 0.01f, 0.02f, 0.02f, 0.02f, 0.0f, -0.02f, -0.04f, -0.04f, -0.03f, -0.02f, -0.01f, -0.01f, 0.0f };
    static readonly float[] ATK_BOTTOM_Y = { -0.58f, -0.58f, -0.58f, -0.59f, -0.59f, -0.59f, -0.59f, -0.59f, -0.6f, -0.6f, -0.6f, -0.58f, -0.56f, -0.54f, -0.54f, -0.55f, -0.56f, -0.57f, -0.57f, -0.58f };

    // ================================================================
    // Generate Attack
    // ================================================================
    [MenuItem("Tools/1/TankMob1/Generate Attack")]
    public static void GenerateAttack()
    {
        const float DUR_ATK = 20 / 12f;
        var clip = MakeClip("TankMob1_Attack", 20, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 20);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR_ATK, val));

        // Body: 예비→앞으로 숙이기→복귀
        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", ATK_BODY_Y); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", ATK_BODY_ROT);

        L(HEAD, "localPosition.x", -0.156f); L(HEAD, "localPosition.y", 0.685f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);

        // SF/HF: 무게중심만
        L(SF, "localPosition.x", -0.594f); L(SF, "localPosition.y", 0.500f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", ATK_SF_ROT);
        L(HF, "localPosition.x", -0.141f); L(HF, "localPosition.y", -0.328f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", ATK_HF_ROT);

        // SB/HB: 크게 올렸다 내리찍기
        L(SB, "localPosition.x", 0.235f); C(SB, "localPosition.y", ATK_SB_Y); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", ATK_SB_ROT);
        L(HB, "localPosition.x", 0.141f); C(HB, "localPosition.y", ATK_HB_Y); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", ATK_HB_ROT);

        L(CLUB, "localPosition.x", 0.063f); L(CLUB, "localPosition.y", -0.360f); L(CLUB, "localPosition.z", 0f);
        L(CLUB, "localEulerAngles.z", -90f);

        L(BOTTOM, "localPosition.x", 0f); C(BOTTOM, "localPosition.y", ATK_BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", ATK_BOT_ROT);

        L(FOOT_F, "localPosition.x", -0.475f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        C(FOOT_B, "localPosition.x", ATK_FB_X); C(FOOT_B, "localPosition.y", ATK_FB_Y); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", ATK_FB_ROT);

        Save(clip, "TankMob1_Attack");
    }
}