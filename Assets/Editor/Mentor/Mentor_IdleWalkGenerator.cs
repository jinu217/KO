using UnityEngine;
using UnityEditor;
using System.IO;

public class Mentor_IdleWalkGenerator
{
    const string SAVE_PATH = "Assets/Animations/Mentor";
    const float FRAME_RATE = 12f;

    const string BODY = "Body";
    const string HEAD = "Body/Head";
    const string SHOULDER_F = "Body/ShoulderJoint_F";
    const string ARM_F = "Body/ShoulderJoint_F/Shoulder_F/ArmJoint_F";
    const string SHOULDER_B = "Body/ShoulderJoint_B";
    const string ARM_B = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B";
    const string WEAPON = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B/Hand_B/Weapon";
    const string LEG_F = "Body/LegJoint_F";
    const string FOOT_F = "Body/LegJoint_F/Foot_F";
    const string LEG_B = "Body/LegJoint_B";
    const string FOOT_B = "Body/LegJoint_B/Foot_B";

    // ================================================================
    // REST POSE
    // Body            pos (0.00,   0.00)   rot 0
    // Head            pos (-0.05,  0.62)   rot 0
    // ShoulderJoint_F pos (-0.40,  0.50)   rot 0
    // ArmJoint_F      pos (-0.12, -0.45)   rot 0
    // ShoulderJoint_B pos ( 0.15,  0.50)   rot 0
    // ArmJoint_B      pos ( 0.11, -0.45)   rot 0
    // Weapon          pos ( 0.10, -0.32)   rot -90
    // LegJoint_F      pos (-0.35, -0.68)   rot 0
    // LegJoint_B      pos ( 0.10, -0.68)   rot 0
    // ================================================================

    // ── IDLE: 32프레임 ────────────────────────────────────────────────
    // f0~f14  : 몸+어깨 함께 올라감 (SF_Y로 어깨가 2프레임 늦게 시작해 자연스럽게 따라옴)
    // f14~f16 : 몸 정점
    // f14~f20 : 어깨 계속 올라가며 몸보다 늦게 정점 도달
    // f16~f22 : 몸 먼저 내려감
    // f20~f26 : 어깨 뒤따라 내려옴
    // f26~f31 : 안정

    static readonly float[] I_BODY_Y = {
        0f,     0.003f,  0.007f,  0.011f,  0.016f,  0.021f,  0.026f,  0.030f,
        0.033f, 0.035f,  0.036f,  0.036f,  0.036f,  0.036f,  0.036f,  0.036f,
        0.034f, 0.030f,  0.024f,  0.017f,  0.011f,  0.006f,  0.002f,  0f,
       -0.002f,-0.003f, -0.002f, -0.001f,  0f,      0f,      0f,      0f };

    // SF_Y/SB_Y: 2프레임 늦게 시작해 자연스럽게 따라 올라가다 몸보다 늦게 내려옴
    static readonly float[] I_SF_Y = {
        0f,     0f,      0.002f,  0.005f,  0.009f,  0.014f,  0.019f,  0.024f,
        0.028f, 0.031f,  0.033f,  0.034f,  0.035f,  0.036f,  0.037f,  0.037f,
        0.037f, 0.035f,  0.031f,  0.026f,  0.020f,  0.013f,  0.007f,  0.003f,
        0.001f, 0f,      0f,      0f,      0f,      0f,      0f,      0f };

    static readonly float[] I_SB_Y = {
        0f,     0f,      0.002f,  0.005f,  0.009f,  0.014f,  0.019f,  0.024f,
        0.028f, 0.031f,  0.033f,  0.034f,  0.035f,  0.036f,  0.037f,  0.037f,
        0.037f, 0.035f,  0.031f,  0.026f,  0.020f,  0.013f,  0.007f,  0.003f,
        0.001f, 0f,      0f,      0f,      0f,      0f,      0f,      0f };

    // 발: Body Y 부호 반전
    static readonly float[] I_LEG_Y = {
        0f,    -0.003f, -0.007f, -0.011f, -0.016f, -0.021f, -0.026f, -0.030f,
       -0.033f,-0.035f, -0.036f, -0.036f, -0.036f, -0.036f, -0.036f, -0.036f,
       -0.034f,-0.030f, -0.024f, -0.017f, -0.011f, -0.006f, -0.002f,  0f,
        0.002f, 0.003f,  0.002f,  0.001f,  0f,      0f,      0f,      0f };

    // ── WALK: 8프레임, 절도 있고 묵직한 걸음 ────────────────────────
    static readonly float[] W_BODY_Y = { -0.010f, 0.003f, 0.010f, 0.003f, -0.010f, 0.003f, 0.010f, 0.003f };
    static readonly float[] W_BODY_ROT = { -1f, -1f, -1f, -1f, -1f, -1f, -1f, -1f };
    static readonly float[] W_HEAD_ROT = { 0f, 0f, 0.5f, 0f, 0f, 0f, 0.5f, 0f };

    static readonly float[] W_SF_ROT = { -7f, -4f, 1f, 5f, 8f, 5f, 0f, -4f };
    static readonly float[] W_SF_Y = { 0.005f, 0f, -0.005f, -0.005f, 0f, 0.005f, 0.005f, 0f };
    static readonly float[] W_AF_ROT = { -5f, -3f, 1f, 4f, 6f, 4f, 0f, -3f };

    static readonly float[] W_SB_ROT = { 5f, 3f, -1f, -4f, -6f, -3f, 0f, 3f };
    static readonly float[] W_SB_Y = { -0.003f, 0f, 0.003f, 0.003f, 0f, -0.003f, -0.003f, 0f };
    static readonly float[] W_AB_ROT = { 3f, 2f, -1f, -3f, -4f, -2f, 0f, 2f };

    static readonly float[] W_WP_ROT = { -2f, -1f, 0f, 1f, 2f, 1f, 0f, -1f };

    static readonly float[] W_LF_X = { 0.11f, 0.05f, -0.02f, -0.12f, -0.10f, -0.02f, 0.06f, 0.11f };
    static readonly float[] W_LF_Y = { 0.00f, 0.010f, 0.018f, 0.065f, 0.130f, 0.075f, 0.025f, 0.00f };
    static readonly float[] W_LF_ROT = { 11f, 6f, -2f, -13f, -9f, -2f, 7f, 12f };
    static readonly float[] W_FF_ROT = { -9f, -5f, 2f, 9f, -11f, -7f, -5f, -10f };

    static readonly float[] W_LB_X = { -0.10f, -0.02f, 0.06f, 0.11f, 0.11f, 0.05f, -0.02f, -0.12f };
    static readonly float[] W_LB_Y = { 0.130f, 0.075f, 0.025f, 0.00f, 0.00f, 0.010f, 0.018f, 0.065f };
    static readonly float[] W_LB_ROT = { -9f, -2f, 7f, 12f, 11f, 6f, -2f, -13f };
    static readonly float[] W_FB_ROT = { -11f, -7f, -5f, -10f, -9f, -5f, 2f, 9f };

    static readonly float[] W_LEG_Y_COMP = { 0.010f, -0.003f, -0.010f, -0.003f, 0.010f, -0.003f, -0.010f, -0.003f };

    // ================================================================

    [MenuItem("Tools/Mentor/Generate Idle")]
    public static void GenerateIdle()
    {
        int n = 32;
        float dur = n / FRAME_RATE;
        AnimationClip clip = MakeClip("Mentor_Idle", dur, loop: true);

        void C(string path, string prop, float[] off, float bv = 0f) =>
            AddCurve(clip, path, prop, off, bv, dur, loop: true);
        void L(string path, string prop, float val) =>
            LockAt(clip, path, prop, val, dur);

        L(BODY, "localPosition.x", 0f);
        C(BODY, "localPosition.y", I_BODY_Y, 0f);
        L(BODY, "localPosition.z", 0f);

        L(HEAD, "localPosition.x", -0.05f);
        L(HEAD, "localPosition.y", 0.62f);
        L(HEAD, "localPosition.z", 0f);

        L(SHOULDER_F, "localPosition.x", -0.40f);
        C(SHOULDER_F, "localPosition.y", I_SF_Y, 0.50f);
        L(SHOULDER_F, "localPosition.z", 0f);

        L(SHOULDER_B, "localPosition.x", 0.15f);
        C(SHOULDER_B, "localPosition.y", I_SB_Y, 0.50f);
        L(SHOULDER_B, "localPosition.z", 0f);

        L(LEG_F, "localPosition.x", -0.35f);
        C(LEG_F, "localPosition.y", I_LEG_Y, -0.68f);
        L(LEG_F, "localPosition.z", 0f);

        L(LEG_B, "localPosition.x", 0.10f);
        C(LEG_B, "localPosition.y", I_LEG_Y, -0.68f);
        L(LEG_B, "localPosition.z", 0f);

        SaveClip(clip, "Mentor_Idle");
    }

    [MenuItem("Tools/Mentor/Generate Walk")]
    public static void GenerateWalk()
    {
        int n = 8;
        float dur = n / FRAME_RATE;
        AnimationClip clip = MakeClip("Mentor_Walk", dur, loop: true);

        void C(string path, string prop, float[] off, float bv = 0f) =>
            AddCurve(clip, path, prop, off, bv, dur, loop: true);
        void L(string path, string prop, float val) =>
            LockAt(clip, path, prop, val, dur);

        L(BODY, "localPosition.x", 0f);
        C(BODY, "localPosition.y", W_BODY_Y, 0f);
        L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", W_BODY_ROT, 0f);

        L(HEAD, "localPosition.x", -0.05f);
        L(HEAD, "localPosition.y", 0.62f);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", W_HEAD_ROT, 0f);

        L(SHOULDER_F, "localPosition.x", -0.40f);
        C(SHOULDER_F, "localPosition.y", W_SF_Y, 0.50f);
        L(SHOULDER_F, "localPosition.z", 0f);
        C(SHOULDER_F, "localEulerAngles.z", W_SF_ROT, 0f);
        C(ARM_F, "localEulerAngles.z", W_AF_ROT, 0f);

        L(SHOULDER_B, "localPosition.x", 0.15f);
        C(SHOULDER_B, "localPosition.y", W_SB_Y, 0.50f);
        L(SHOULDER_B, "localPosition.z", 0f);
        C(SHOULDER_B, "localEulerAngles.z", W_SB_ROT, 0f);
        C(ARM_B, "localEulerAngles.z", W_AB_ROT, 0f);
        C(WEAPON, "localEulerAngles.z", W_WP_ROT, -90f);

        float[] lfY = new float[n];
        float[] lbY = new float[n];
        for (int i = 0; i < n; i++)
        {
            lfY[i] = W_LF_Y[i] + W_LEG_Y_COMP[i];
            lbY[i] = W_LB_Y[i] + W_LEG_Y_COMP[i];
        }

        C(LEG_F, "localPosition.x", W_LF_X, -0.35f);
        C(LEG_F, "localPosition.y", lfY, -0.68f);
        L(LEG_F, "localPosition.z", 0f);
        C(LEG_F, "localEulerAngles.z", W_LF_ROT, 0f);
        C(FOOT_F, "localEulerAngles.z", W_FF_ROT, 0f);

        C(LEG_B, "localPosition.x", W_LB_X, 0.10f);
        C(LEG_B, "localPosition.y", lbY, -0.68f);
        L(LEG_B, "localPosition.z", 0f);
        C(LEG_B, "localEulerAngles.z", W_LB_ROT, 0f);
        C(FOOT_B, "localEulerAngles.z", W_FB_ROT, 0f);

        SaveClip(clip, "Mentor_Walk");
    }

    // ── Helpers ─────────────────────────────────────────────────────

    static AnimationClip MakeClip(string name, float dur, bool loop)
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = FRAME_RATE;
        clip.name = name;
        AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, s);
        return clip;
    }

    static void AddCurve(AnimationClip clip, string path, string prop,
        float[] offsets, float baseVal, float dur, bool loop)
    {
        int n = offsets.Length;
        Keyframe[] keys = new Keyframe[n + 1];
        for (int i = 0; i < n; i++)
            keys[i] = new Keyframe(i / FRAME_RATE, baseVal + offsets[i]);
        keys[n] = new Keyframe(dur, baseVal + (loop ? offsets[0] : offsets[n - 1]));
        AnimationCurve c = new AnimationCurve(keys);
        for (int i = 0; i < c.keys.Length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(c, i, AnimationUtility.TangentMode.Auto);
            AnimationUtility.SetKeyRightTangentMode(c, i, AnimationUtility.TangentMode.Auto);
        }
        clip.SetCurve(path, typeof(Transform), prop, c);
    }

    static void LockAt(AnimationClip clip, string path, string prop, float val, float dur) =>
        clip.SetCurve(path, typeof(Transform), prop,
            AnimationCurve.Linear(0f, val, dur, val));

    static void SaveClip(AnimationClip clip, string clipName)
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "Mentor");
        }
        string assetPath = $"{SAVE_PATH}/{clipName}.anim";
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(clip, existing);
            existing.name = clipName;
            AssetDatabase.SaveAssets();
        }
        else
        {
            AssetDatabase.CreateAsset(clip, assetPath);
            AssetDatabase.SaveAssets();
        }
        AssetDatabase.Refresh();
        Debug.Log($"[Mentor] {clipName} created -> {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}