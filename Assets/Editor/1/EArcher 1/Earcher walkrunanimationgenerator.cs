using UnityEngine;
using UnityEditor;
using System.IO;

public class EArcher_WalkRunAnimationGenerator
{
    const string SAVE_PATH = "Assets/Animations/EArcher 1";
    const float FRAME_RATE = 12f;

    // 좌표 추출 기준 정확한 경로
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
    // Body            pos (0.00,  0.00)  rot 0
    // Head            pos (0.05,  0.45)  rot 0
    // ShoulderJoint_F pos (-0.27, 0.32)  rot 0
    // ArmJoint_F      rot 0
    // ShoulderJoint_B pos (0.36,  0.28)  rot 0
    // ArmJoint_B      rot 0
    // Weapon(Bow)     pos (0.06, -0.28)  rot -65
    // LegJoint_F      pos (-0.25,-0.75)  rot 0
    // LegJoint_B      pos ( 0.25,-0.75)  rot 0
    // ================================================================

    // ── IDLE: 16프레임 루프, 차분한 숨쉬기 ────────────────────────
    // Body Y만 천천히 오르내림, 팔/머리 rot 없음
    static readonly float[] I_BODY_Y = {
        0f, 0.005f, 0.010f, 0.015f, 0.018f, 0.022f, 0.025f, 0.027f,
        0.025f, 0.022f, 0.018f, 0.015f, 0.010f, 0.005f, 0.002f, 0f };

    // ── IDLE 발 보정: Body Y 올라간 만큼 LegJoint Y 반대로 ──────────
    // I_BODY_Y와 부호 반대, 크기 동일
    static readonly float[] I_LF_Y = {
        0f, -0.005f, -0.010f, -0.015f, -0.018f, -0.022f, -0.025f, -0.027f,
        -0.025f, -0.022f, -0.018f, -0.015f, -0.010f, -0.005f, -0.002f, 0f };
    static readonly float[] I_LB_Y = {
        0f, -0.005f, -0.010f, -0.015f, -0.018f, -0.022f, -0.025f, -0.027f,
        -0.025f, -0.022f, -0.018f, -0.015f, -0.010f, -0.005f, -0.002f, 0f };

    static readonly float[] I_SF_Y = {
        0f, 0.003f, 0.005f, 0.008f, 0.010f, 0.012f, 0.013f, 0.014f,
        0.013f, 0.012f, 0.010f, 0.008f, 0.005f, 0.003f, 0.001f, 0f };
    static readonly float[] I_SB_Y = {
        0f, 0.003f, 0.005f, 0.008f, 0.010f, 0.012f, 0.013f, 0.014f,
        0.013f, 0.012f, 0.010f, 0.008f, 0.005f, 0.003f, 0.001f, 0f };

    // ── WALK: 8프레임 루프 ──────────────────────────────────────────
    static readonly float[] W_BODY_Y = { -0.02f, 0.01f, 0.03f, 0.01f, -0.02f, 0.01f, 0.03f, 0.01f };
    static readonly float[] W_BODY_ROT = { -2f, -1f, -1f, -2f, -2f, -1f, -1f, -2f };
    static readonly float[] W_HEAD_ROT = { 1f, 1f, 0f, 1f, 1f, 1f, 0f, 1f };

    static readonly float[] W_SF_ROT = { -7f, -4f, 1f, 6f, 8f, 5f, -1f, -5f };
    static readonly float[] W_SF_Y = { 0.01f, 0.00f, -0.01f, -0.01f, 0.00f, 0.01f, 0.01f, 0.00f };
    static readonly float[] W_AF_ROT = { -6f, -3f, 1f, 5f, 7f, 4f, -1f, -4f };

    static readonly float[] W_SB_ROT = { 6f, 4f, -1f, -5f, -7f, -4f, 1f, 5f };
    static readonly float[] W_SB_Y = { -0.01f, 0.00f, 0.01f, 0.01f, 0.00f, -0.01f, -0.01f, 0.00f };
    static readonly float[] W_AB_ROT = { 5f, 3f, -1f, -4f, -6f, -3f, 1f, 4f };
    static readonly float[] W_WP_ROT = { -3f, -2f, 0f, 2f, 3f, 2f, 0f, -2f };

    static readonly float[] W_LF_X = { 0.12f, 0.05f, -0.03f, -0.12f, -0.10f, -0.03f, 0.06f, 0.12f };
    static readonly float[] W_LF_Y = { 0.00f, 0.01f, 0.02f, 0.08f, 0.16f, 0.10f, 0.04f, 0.00f };
    static readonly float[] W_LF_ROT = { 12f, 6f, -3f, -15f, -10f, -3f, 8f, 14f };
    static readonly float[] W_FF_ROT = { -10f, -5f, 3f, 10f, -12f, -9f, -6f, -11f };
    static readonly float[] W_LB_X = { -0.10f, -0.03f, 0.06f, 0.12f, 0.12f, 0.05f, -0.03f, -0.12f };
    static readonly float[] W_LB_Y = { 0.16f, 0.10f, 0.04f, 0.00f, 0.00f, 0.01f, 0.02f, 0.08f };
    static readonly float[] W_LB_ROT = { -10f, -3f, 8f, 14f, 12f, 6f, -3f, -15f };
    static readonly float[] W_FB_ROT = { -12f, -9f, -6f, -11f, -10f, -5f, 3f, 10f };

    // ── RUN: 8프레임 루프 ───────────────────────────────────────────
    static readonly float[] R_BODY_Y = { -0.06f, 0.02f, 0.07f, 0.02f, -0.06f, 0.02f, 0.07f, 0.02f };
    static readonly float[] R_BODY_ROT = { -9f, -7f, -5f, -7f, -9f, -7f, -5f, -7f };
    static readonly float[] R_HEAD_ROT = { 5f, 4f, 2f, 4f, 5f, 4f, 2f, 4f };

    static readonly float[] R_SF_ROT = { -13f, -7f, 3f, 11f, 15f, 9f, -3f, -10f };
    static readonly float[] R_SF_Y = { 0.02f, 0.01f, -0.01f, -0.02f, -0.01f, 0.01f, 0.02f, 0.01f };
    static readonly float[] R_AF_ROT = { -11f, -6f, 3f, 9f, 13f, 7f, -3f, -9f };

    static readonly float[] R_SB_ROT = { 12f, 7f, -3f, -10f, -13f, -7f, 3f, 10f };
    static readonly float[] R_SB_Y = { -0.02f, -0.01f, 0.01f, 0.02f, 0.02f, 0.01f, -0.01f, -0.02f };
    static readonly float[] R_AB_ROT = { 9f, 5f, -2f, -8f, -10f, -5f, 2f, 8f };
    static readonly float[] R_WP_ROT = { -5f, -3f, 0f, 3f, 5f, 3f, 0f, -3f };

    static readonly float[] R_LF_X = { 0.22f, 0.10f, -0.06f, -0.24f, -0.22f, -0.06f, 0.13f, 0.22f };
    static readonly float[] R_LF_Y = { 0.00f, 0.04f, 0.06f, 0.20f, 0.36f, 0.24f, 0.10f, 0.00f };
    static readonly float[] R_LF_ROT = { 25f, 12f, -7f, -30f, -20f, -7f, 16f, 27f };
    static readonly float[] R_FF_ROT = { -20f, -10f, 7f, 22f, -25f, -18f, -13f, -22f };
    static readonly float[] R_LB_X = { -0.22f, -0.06f, 0.13f, 0.22f, 0.22f, 0.10f, -0.06f, -0.24f };
    static readonly float[] R_LB_Y = { 0.36f, 0.24f, 0.10f, 0.00f, 0.00f, 0.04f, 0.06f, 0.20f };
    static readonly float[] R_LB_ROT = { -20f, -7f, 16f, 27f, 25f, 12f, -7f, -30f };
    static readonly float[] R_FB_ROT = { -25f, -18f, -13f, -22f, -20f, -10f, 7f, 22f };

    // ================================================================

    [MenuItem("Tools/1/EArcher 1/Generate Idle")]
    public static void GenerateIdle()
    {
        int n = 16;
        float dur = n / FRAME_RATE;
        AnimationClip clip = MakeClip(dur, loop: true);

        void C(string path, string prop, float[] off, float baseVal = 0f) =>
            AddCurve(clip, path, prop, off, baseVal, dur, loop: true);
        void L(string path, string prop, float val) =>
            LockAt(clip, path, prop, val, dur);

        L(BODY, "localPosition.x", 0f);
        C(BODY, "localPosition.y", I_BODY_Y, 0f);
        L(BODY, "localPosition.z", 0f);

        L(HEAD, "localPosition.x", 0.05f);
        L(HEAD, "localPosition.y", 0.45f);
        L(HEAD, "localPosition.z", 0f);

        L(SHOULDER_F, "localPosition.x", -0.27f);
        C(SHOULDER_F, "localPosition.y", I_SF_Y, 0.32f);
        L(SHOULDER_F, "localPosition.z", 0f);

        L(SHOULDER_B, "localPosition.x", 0.36f);
        C(SHOULDER_B, "localPosition.y", I_SB_Y, 0.28f);
        L(SHOULDER_B, "localPosition.z", 0f);

        L(LEG_F, "localPosition.x", -0.25f);
        C(LEG_F, "localPosition.y", I_LF_Y, -0.75f);
        L(LEG_F, "localPosition.z", 0f);
        L(LEG_F, "localEulerAngles.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(LEG_B, "localPosition.x", 0.25f);
        C(LEG_B, "localPosition.y", I_LB_Y, -0.75f);
        L(LEG_B, "localPosition.z", 0f);
        L(LEG_B, "localEulerAngles.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

        SaveClip(clip, "EArcher1_Idle");
    }

    [MenuItem("Tools/1/EArcher 1/Generate Walk")]
    public static void GenerateWalk()
    {
        int n = 8;
        float dur = n / FRAME_RATE;
        AnimationClip clip = MakeClip(dur, loop: true);

        void C(string path, string prop, float[] off, float baseVal = 0f) =>
            AddCurve(clip, path, prop, off, baseVal, dur, loop: true);
        void L(string path, string prop, float val) =>
            LockAt(clip, path, prop, val, dur);

        L(BODY, "localPosition.x", 0f);
        C(BODY, "localPosition.y", W_BODY_Y, 0f);
        L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", W_BODY_ROT, 0f);

        L(HEAD, "localPosition.x", 0.05f);
        L(HEAD, "localPosition.y", 0.45f);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", W_HEAD_ROT, 0f);

        L(SHOULDER_F, "localPosition.x", -0.27f);
        C(SHOULDER_F, "localPosition.y", W_SF_Y, 0.32f);
        L(SHOULDER_F, "localPosition.z", 0f);
        C(SHOULDER_F, "localEulerAngles.z", W_SF_ROT, 0f);
        C(ARM_F, "localEulerAngles.z", W_AF_ROT, 0f);

        L(SHOULDER_B, "localPosition.x", 0.36f);
        C(SHOULDER_B, "localPosition.y", W_SB_Y, 0.28f);
        L(SHOULDER_B, "localPosition.z", 0f);
        C(SHOULDER_B, "localEulerAngles.z", W_SB_ROT, 0f);
        C(ARM_B, "localEulerAngles.z", W_AB_ROT, 0f);
        C(WEAPON, "localEulerAngles.z", W_WP_ROT, -65f);

        C(LEG_F, "localPosition.x", W_LF_X, -0.25f);
        C(LEG_F, "localPosition.y", W_LF_Y, -0.75f);
        L(LEG_F, "localPosition.z", 0f);
        C(LEG_F, "localEulerAngles.z", W_LF_ROT, 0f);
        C(FOOT_F, "localEulerAngles.z", W_FF_ROT, 0f);

        C(LEG_B, "localPosition.x", W_LB_X, 0.25f);
        C(LEG_B, "localPosition.y", W_LB_Y, -0.75f);
        L(LEG_B, "localPosition.z", 0f);
        C(LEG_B, "localEulerAngles.z", W_LB_ROT, 0f);
        C(FOOT_B, "localEulerAngles.z", W_FB_ROT, 0f);

        SaveClip(clip, "EArcher1_Walk");
    }

    [MenuItem("Tools/1/EArcher 1/Generate Run")]
    public static void GenerateRun()
    {
        int n = 8;
        float dur = n / FRAME_RATE;
        AnimationClip clip = MakeClip(dur, loop: true);

        void C(string path, string prop, float[] off, float baseVal = 0f) =>
            AddCurve(clip, path, prop, off, baseVal, dur, loop: true);
        void L(string path, string prop, float val) =>
            LockAt(clip, path, prop, val, dur);

        L(BODY, "localPosition.x", 0f);
        C(BODY, "localPosition.y", R_BODY_Y, 0f);
        L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", R_BODY_ROT, 0f);

        L(HEAD, "localPosition.x", 0.05f);
        L(HEAD, "localPosition.y", 0.45f);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", R_HEAD_ROT, 0f);

        L(SHOULDER_F, "localPosition.x", -0.27f);
        C(SHOULDER_F, "localPosition.y", R_SF_Y, 0.32f);
        L(SHOULDER_F, "localPosition.z", 0f);
        C(SHOULDER_F, "localEulerAngles.z", R_SF_ROT, 0f);
        C(ARM_F, "localEulerAngles.z", R_AF_ROT, 0f);

        L(SHOULDER_B, "localPosition.x", 0.36f);
        C(SHOULDER_B, "localPosition.y", R_SB_Y, 0.28f);
        L(SHOULDER_B, "localPosition.z", 0f);
        C(SHOULDER_B, "localEulerAngles.z", R_SB_ROT, 0f);
        C(ARM_B, "localEulerAngles.z", R_AB_ROT, 0f);
        C(WEAPON, "localEulerAngles.z", R_WP_ROT, -65f);

        C(LEG_F, "localPosition.x", R_LF_X, -0.25f);
        C(LEG_F, "localPosition.y", R_LF_Y, -0.75f);
        L(LEG_F, "localPosition.z", 0f);
        C(LEG_F, "localEulerAngles.z", R_LF_ROT, 0f);
        C(FOOT_F, "localEulerAngles.z", R_FF_ROT, 0f);

        C(LEG_B, "localPosition.x", R_LB_X, 0.25f);
        C(LEG_B, "localPosition.y", R_LB_Y, -0.75f);
        L(LEG_B, "localPosition.z", 0f);
        C(LEG_B, "localEulerAngles.z", R_LB_ROT, 0f);
        C(FOOT_B, "localEulerAngles.z", R_FB_ROT, 0f);

        SaveClip(clip, "EArcher1_Run");
    }

    // ── Helpers ─────────────────────────────────────────────────────

    static AnimationClip MakeClip(float dur, bool loop)
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = FRAME_RATE;
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
            AssetDatabase.CreateFolder("Assets/Animations", "EArcher 1");
        }
        string assetPath = $"{SAVE_PATH}/{clipName}.anim";
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(clip, existing);
            AssetDatabase.SaveAssets();
        }
        else
        {
            AssetDatabase.CreateAsset(clip, assetPath);
            AssetDatabase.SaveAssets();
        }
        AssetDatabase.Refresh();
        Debug.Log($"[EArcher 1] {clipName} created -> {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}