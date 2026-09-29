using UnityEngine;
using UnityEditor;
using System.IO;

public class SpeedMob_IdleWalkRunGenerator
{
    const string SAVE_PATH = "Assets/Animations/SpeedMob 1";
    const float FRAME_RATE = 12f;

    // ================================================================
    // 정확한 히에라키 경로
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
    // Head            pos (0.00,   0.324)  rot 0
    // ShoulderJoint_F pos (-0.21,  0.22)   rot 0
    // ArmJoint_F      pos (-0.235,-0.21)   rot 0
    // ShoulderJoint_B pos ( 0.27,  0.22)   rot 0
    // ArmJoint_B      pos ( 0.045,-0.21)   rot 0
    // Weapon          pos ( 0.06, -0.20)   rot -65
    // LegJoint_F      pos (-0.20, -0.475)  rot 0
    // LegJoint_B      pos ( 0.15, -0.475)  rot 0
    // ================================================================

    // ── IDLE: 16프레임, 숨쉬기 + 단검 살짝 흔들림 ───────────────────
    static readonly float[] I_BODY_Y = {
        0f, 0.004f, 0.008f, 0.012f, 0.015f, 0.018f, 0.020f, 0.021f,
        0.020f, 0.018f, 0.015f, 0.012f, 0.008f, 0.004f, 0.001f, 0f };

    static readonly float[] I_SF_Y = {
        0f, 0.002f, 0.004f, 0.006f, 0.008f, 0.009f, 0.010f, 0.011f,
        0.010f, 0.009f, 0.008f, 0.006f, 0.004f, 0.002f, 0.001f, 0f };
    static readonly float[] I_SB_Y = {
        0f, 0.002f, 0.004f, 0.006f, 0.008f, 0.009f, 0.010f, 0.011f,
        0.010f, 0.009f, 0.008f, 0.006f, 0.004f, 0.002f, 0.001f, 0f };

    // 단검 살짝 흔들림
    static readonly float[] I_WP_ROT = {
        0f, 0f, -1f, -2f, -1f, 0f, 1f, 2f,
        1f, 0f, -1f, -2f, -1f, 0f, 1f, 0f };

    // 발 보정: Body Y 반대
    static readonly float[] I_LF_Y = {
        0f, -0.004f, -0.008f, -0.012f, -0.015f, -0.018f, -0.020f, -0.021f,
        -0.020f, -0.018f, -0.015f, -0.012f, -0.008f, -0.004f, -0.001f, 0f };
    static readonly float[] I_LB_Y = {
        0f, -0.004f, -0.008f, -0.012f, -0.015f, -0.018f, -0.020f, -0.021f,
        -0.020f, -0.018f, -0.015f, -0.012f, -0.008f, -0.004f, -0.001f, 0f };

    // ── WALK: 8프레임 ────────────────────────────────────────────────
    static readonly float[] W_BODY_Y = { -0.015f, 0.008f, 0.020f, 0.008f, -0.015f, 0.008f, 0.020f, 0.008f };
    static readonly float[] W_BODY_ROT = { -2f, -1f, -1f, -2f, -2f, -1f, -1f, -2f };

    static readonly float[] W_HEAD_ROT = { 1f, 1f, 0f, 1f, 1f, 1f, 0f, 1f };

    static readonly float[] W_SF_ROT = { -7f, -4f, 1f, 6f, 8f, 5f, -1f, -5f };
    static readonly float[] W_SF_Y = { 0.008f, 0f, -0.008f, -0.008f, 0f, 0.008f, 0.008f, 0f };
    static readonly float[] W_AF_ROT = { -6f, -3f, 1f, 5f, 7f, 4f, -1f, -4f };

    static readonly float[] W_SB_ROT = { 7f, 4f, -1f, -5f, -7f, -4f, 1f, 5f };
    static readonly float[] W_SB_Y = { -0.008f, 0f, 0.008f, 0.008f, 0f, -0.008f, -0.008f, 0f };
    static readonly float[] W_AB_ROT = { 5f, 3f, -1f, -4f, -6f, -3f, 1f, 4f };
    static readonly float[] W_WP_ROT = { -3f, -2f, 0f, 2f, 3f, 2f, 0f, -2f };

    // 걷기 다리: Y 양수만 (위로만), base는 AddCurve에서 -0.475 적용
    static readonly float[] W_LF_X = { 0.10f, 0.04f, -0.02f, -0.10f, -0.08f, -0.02f, 0.05f, 0.10f };
    static readonly float[] W_LF_Y = { 0.00f, 0.005f, 0.015f, 0.060f, 0.120f, 0.070f, 0.025f, 0.00f };
    static readonly float[] W_LF_ROT = { 10f, 5f, -3f, -12f, -8f, -3f, 6f, 11f };
    static readonly float[] W_FF_ROT = { -8f, -4f, 2f, 8f, -10f, -7f, -5f, -9f };

    static readonly float[] W_LB_X = { -0.08f, -0.02f, 0.05f, 0.10f, 0.10f, 0.04f, -0.02f, -0.10f };
    static readonly float[] W_LB_Y = { 0.120f, 0.070f, 0.025f, 0.00f, 0.00f, 0.005f, 0.015f, 0.060f };
    static readonly float[] W_LB_ROT = { -8f, -3f, 6f, 11f, 10f, 5f, -3f, -12f };
    static readonly float[] W_FB_ROT = { -10f, -7f, -5f, -9f, -8f, -4f, 2f, 8f };

    // ── RUN: 8프레임, 빠르고 민첩한 느낌 ────────────────────────────
    static readonly float[] R_BODY_Y = { -0.050f, 0.015f, 0.055f, 0.015f, -0.050f, 0.015f, 0.055f, 0.015f };
    static readonly float[] R_BODY_ROT = { -12f, -9f, -6f, -9f, -12f, -9f, -6f, -9f };

    static readonly float[] R_HEAD_ROT = { 7f, 5f, 3f, 5f, 7f, 5f, 3f, 5f };

    // 단검몹은 팔 스윙이 더 날카롭고 큼
    static readonly float[] R_SF_ROT = { -18f, -9f, 4f, 14f, 20f, 12f, -4f, -14f };
    static readonly float[] R_SF_Y = { 0.015f, 0.008f, -0.008f, -0.015f, -0.008f, 0.008f, 0.015f, 0.008f };
    static readonly float[] R_AF_ROT = { -14f, -7f, 3f, 11f, 16f, 9f, -3f, -11f };

    static readonly float[] R_SB_ROT = { 18f, 9f, -4f, -14f, -20f, -12f, 4f, 14f };
    static readonly float[] R_SB_Y = { -0.015f, -0.008f, 0.008f, 0.015f, 0.015f, 0.008f, -0.008f, -0.015f };
    static readonly float[] R_AB_ROT = { 13f, 7f, -3f, -10f, -15f, -9f, 3f, 10f };
    static readonly float[] R_WP_ROT = { -6f, -3f, 0f, 3f, 6f, 3f, 0f, -3f };

    // 뛰기 다리: EArcher 스타일, Y는 0이상만, rot.z로 발 방향
    static readonly float[] R_LF_X = { 0.18f, 0.08f, -0.05f, -0.20f, -0.18f, -0.05f, 0.10f, 0.18f };
    static readonly float[] R_LF_Y = { 0.00f, 0.04f, 0.06f, 0.18f, 0.30f, 0.20f, 0.08f, 0.00f };
    static readonly float[] R_LF_ROT = { 22f, 10f, -6f, -26f, -16f, -6f, 14f, 24f };
    static readonly float[] R_FF_ROT = { -18f, -9f, 5f, 18f, -22f, -15f, -11f, -19f };

    static readonly float[] R_LB_X = { -0.18f, -0.05f, 0.10f, 0.18f, 0.18f, 0.08f, -0.05f, -0.20f };
    static readonly float[] R_LB_Y = { 0.30f, 0.20f, 0.08f, 0.00f, 0.00f, 0.04f, 0.06f, 0.18f };
    static readonly float[] R_LB_ROT = { -16f, -6f, 14f, 24f, 22f, 10f, -6f, -26f };
    static readonly float[] R_FB_ROT = { -22f, -15f, -11f, -19f, -18f, -9f, 5f, 18f };

    // Body rot 보정 (뛰기 발 지면 고정)
    static readonly float[] R_LEG_ROT_COMP = { 12f, 9f, 6f, 9f, 12f, 9f, 6f, 9f };

    // ================================================================

    [MenuItem("Tools/1/SpeedMob 1/Generate Idle")]
    public static void GenerateIdle()
    {
        int n = 16;
        float dur = n / FRAME_RATE;
        AnimationClip clip = MakeClip(dur, loop: true);

        void C(string path, string prop, float[] off, float bv = 0f) =>
            AddCurve(clip, path, prop, off, bv, dur, loop: true);
        void L(string path, string prop, float val) =>
            LockAt(clip, path, prop, val, dur);

        L(BODY, "localPosition.x", 0f);
        C(BODY, "localPosition.y", I_BODY_Y, 0f);
        L(BODY, "localPosition.z", 0f);

        L(HEAD, "localPosition.x", 0f);
        L(HEAD, "localPosition.y", 0.324f);
        L(HEAD, "localPosition.z", 0f);

        L(SHOULDER_F, "localPosition.x", -0.21f);
        C(SHOULDER_F, "localPosition.y", I_SF_Y, 0.22f);
        L(SHOULDER_F, "localPosition.z", 0f);

        L(SHOULDER_B, "localPosition.x", 0.27f);
        C(SHOULDER_B, "localPosition.y", I_SB_Y, 0.22f);
        L(SHOULDER_B, "localPosition.z", 0f);
        C(WEAPON, "localEulerAngles.z", I_WP_ROT, -65f);

        L(LEG_F, "localPosition.x", -0.20f);
        C(LEG_F, "localPosition.y", I_LF_Y, -0.475f);
        L(LEG_F, "localPosition.z", 0f);
        L(LEG_F, "localEulerAngles.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);

        L(LEG_B, "localPosition.x", 0.15f);
        C(LEG_B, "localPosition.y", I_LB_Y, -0.475f);
        L(LEG_B, "localPosition.z", 0f);
        L(LEG_B, "localEulerAngles.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

        SaveClip(clip, "SpeedMob1_Idle");
    }

    [MenuItem("Tools/1/SpeedMob 1/Generate Walk")]
    public static void GenerateWalk()
    {
        int n = 8;
        float dur = n / FRAME_RATE;
        AnimationClip clip = MakeClip(dur, loop: true);

        void C(string path, string prop, float[] off, float bv = 0f) =>
            AddCurve(clip, path, prop, off, bv, dur, loop: true);
        void L(string path, string prop, float val) =>
            LockAt(clip, path, prop, val, dur);

        L(BODY, "localPosition.x", 0f);
        C(BODY, "localPosition.y", W_BODY_Y, 0f);
        L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", W_BODY_ROT, 0f);

        L(HEAD, "localPosition.x", 0f);
        L(HEAD, "localPosition.y", 0.324f);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", W_HEAD_ROT, 0f);

        L(SHOULDER_F, "localPosition.x", -0.21f);
        C(SHOULDER_F, "localPosition.y", W_SF_Y, 0.22f);
        L(SHOULDER_F, "localPosition.z", 0f);
        C(SHOULDER_F, "localEulerAngles.z", W_SF_ROT, 0f);
        C(ARM_F, "localEulerAngles.z", W_AF_ROT, 0f);

        L(SHOULDER_B, "localPosition.x", 0.27f);
        C(SHOULDER_B, "localPosition.y", W_SB_Y, 0.22f);
        L(SHOULDER_B, "localPosition.z", 0f);
        C(SHOULDER_B, "localEulerAngles.z", W_SB_ROT, 0f);
        C(ARM_B, "localEulerAngles.z", W_AB_ROT, 0f);
        C(WEAPON, "localEulerAngles.z", W_WP_ROT, -65f);

        C(LEG_F, "localPosition.x", W_LF_X, -0.20f);
        C(LEG_F, "localPosition.y", W_LF_Y, -0.475f);
        L(LEG_F, "localPosition.z", 0f);
        C(LEG_F, "localEulerAngles.z", W_LF_ROT, 0f);
        C(FOOT_F, "localEulerAngles.z", W_FF_ROT, 0f);

        C(LEG_B, "localPosition.x", W_LB_X, 0.15f);
        C(LEG_B, "localPosition.y", W_LB_Y, -0.475f);
        L(LEG_B, "localPosition.z", 0f);
        C(LEG_B, "localEulerAngles.z", W_LB_ROT, 0f);
        C(FOOT_B, "localEulerAngles.z", W_FB_ROT, 0f);

        SaveClip(clip, "SpeedMob1_Walk");
    }

    [MenuItem("Tools/1/SpeedMob 1/Generate Run")]
    public static void GenerateRun()
    {
        int n = 8;
        float dur = n / FRAME_RATE;
        AnimationClip clip = MakeClip(dur, loop: true);

        void C(string path, string prop, float[] off, float bv = 0f) =>
            AddCurve(clip, path, prop, off, bv, dur, loop: true);
        void L(string path, string prop, float val) =>
            LockAt(clip, path, prop, val, dur);

        L(BODY, "localPosition.x", 0f);
        C(BODY, "localPosition.y", R_BODY_Y, 0f);
        L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", R_BODY_ROT, 0f);

        L(HEAD, "localPosition.x", 0f);
        L(HEAD, "localPosition.y", 0.324f);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", R_HEAD_ROT, 0f);

        L(SHOULDER_F, "localPosition.x", -0.21f);
        C(SHOULDER_F, "localPosition.y", R_SF_Y, 0.22f);
        L(SHOULDER_F, "localPosition.z", 0f);
        C(SHOULDER_F, "localEulerAngles.z", R_SF_ROT, 0f);
        C(ARM_F, "localEulerAngles.z", R_AF_ROT, 0f);

        L(SHOULDER_B, "localPosition.x", 0.27f);
        C(SHOULDER_B, "localPosition.y", R_SB_Y, 0.22f);
        L(SHOULDER_B, "localPosition.z", 0f);
        C(SHOULDER_B, "localEulerAngles.z", R_SB_ROT, 0f);
        C(ARM_B, "localEulerAngles.z", R_AB_ROT, 0f);
        C(WEAPON, "localEulerAngles.z", R_WP_ROT, -65f);

        C(LEG_F, "localPosition.x", R_LF_X, -0.20f);
        C(LEG_F, "localPosition.y", R_LF_Y, -0.475f);
        L(LEG_F, "localPosition.z", 0f);
        C(LEG_F, "localEulerAngles.z", R_LF_ROT, 0f);
        C(FOOT_F, "localEulerAngles.z", R_FF_ROT, 0f);

        C(LEG_B, "localPosition.x", R_LB_X, 0.15f);
        C(LEG_B, "localPosition.y", R_LB_Y, -0.475f);
        L(LEG_B, "localPosition.z", 0f);
        C(LEG_B, "localEulerAngles.z", R_LB_ROT, 0f);
        C(FOOT_B, "localEulerAngles.z", R_FB_ROT, 0f);

        SaveClip(clip, "SpeedMob1_Run");
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
            AssetDatabase.CreateFolder("Assets/Animations", "SpeedMob 1");
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
        Debug.Log($"[SpeedMob 1] {clipName} created -> {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}