using UnityEngine;
using UnityEditor;
using System.IO;

public class EBasicMob_ThrustAnimationGenerator
{
    const string SAVE_PATH = "Assets/Animations/EBasicMob";
    const float FRAME_RATE = 12f;

    const string BODY = "Body";
    const string HEAD = "Body/Head";
    const string SHOULDER_F = "Body/ShoulderJoint_F";
    const string SHOULDER_B = "Body/ShoulderJoint_B";
    const string ARM_F = "Body/ShoulderJoint_F/Shoulder_F/ArmJoint_F";
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
    // Weapon                             rot -65
    // LegJoint_F      pos (-0.25,-0.75)  rot 0
    // LegJoint_B      pos ( 0.25,-0.75)  rot 0
    //
    // ── 실측 키 자세 ──────────────────────────────────────────────
    // [f2 예비]
    //   SF pos (-0.27, 0.32)  rot +25   offset: rot+25
    //   AF rot +60            offset: rot+60
    //   SB pos (0.36,  0.28)  rot -30   offset: rot-30
    //   AB rot +40            offset: rot+40
    //   Weapon rot -65        offset: 0
    //
    // [f5 임팩트]
    //   SF pos (-0.30, 0.32)  rot -25   offset: X-0.03, rot-25
    //   AF rot 0              offset: rot 0
    //   SB pos (0.46,  0.30)  rot +60   offset: X+0.10, Y+0.02, rot+60
    //   AB rot -4             offset: rot-4
    //   Weapon rot -150       offset: -85
    // ================================================================

    // ── 타이밍 ───────────────────────────────────────────────────────
    // f0  : 대기 (rest)
    // f1  : 예비 시작
    // f2  : 풀 예비 (실측)
    // f3  : 찌르기 시작
    // f4  : 찌르기 가속
    // f5  : 임팩트 (실측)
    // f6  : 임팩트 유지
    // f7+ : 복귀 (A) / 유지 (B)

    // Body: 예비 시 살짝 뒤로, 임팩트 시 앞으로 기울기
    static readonly float[] BODY_ROT_A = { 0f, 3f, 5f, 2f, 0f, -10f, -8f, -5f, -3f, -1f, 0f, 0f, 0f, 0f };
    static readonly float[] BODY_ROT_B = { 0f, 3f, 5f, 2f, 0f, -10f, -10f, -10f, -10f };

    // Head: body 보정
    static readonly float[] HEAD_ROT_A = { 0f, -2f, -3f, -1f, 0f, 6f, 5f, 3f, 1f, 0f, 0f, 0f, 0f, 0f };
    static readonly float[] HEAD_ROT_B = { 0f, -2f, -3f, -1f, 0f, 6f, 6f, 6f, 6f };

    // ShoulderJoint_F X: rest=-0.27, f2=-0.27(유지), f5=-0.30(offset -0.03)
    static readonly float[] SF_X_A = { 0f, 0f, 0f, -0.01f, -0.02f, -0.03f, -0.02f, -0.01f, 0f, 0f, 0f, 0f, 0f, 0f };
    static readonly float[] SF_X_B = { 0f, 0f, 0f, -0.01f, -0.02f, -0.03f, -0.03f, -0.03f, -0.03f };

    // ShoulderJoint_F rot: rest=0, f2=+25, f5=-25
    static readonly float[] SF_ROT_A = { 0f, 12f, 25f, 10f, -8f, -25f, -20f, -12f, -5f, -1f, 0f, 0f, 0f, 0f };
    static readonly float[] SF_ROT_B = { 0f, 12f, 25f, 10f, -8f, -25f, -25f, -25f, -25f };

    // ArmJoint_F rot: rest=0, f2=+60, f5=0
    static readonly float[] AF_ROT_A = { 0f, 30f, 60f, 30f, 10f, 0f, 0f, 5f, 8f, 4f, 1f, 0f, 0f, 0f };
    static readonly float[] AF_ROT_B = { 0f, 30f, 60f, 30f, 10f, 0f, 0f, 0f, 0f };

    // ShoulderJoint_B rot: rest=0, f2=-30, f5=+60
    static readonly float[] SB_ROT_A = { 0f, -15f, -30f, -10f, 20f, 60f, 55f, 38f, 20f, 8f, 2f, 0f, 0f, 0f };
    static readonly float[] SB_ROT_B = { 0f, -15f, -30f, -10f, 20f, 60f, 60f, 60f, 60f };

    // ShoulderJoint_B X: rest=0.36, f2=0.36(유지), f5=0.46(offset+0.10)
    static readonly float[] SB_X_A = { 0f, 0f, 0f, 0.03f, 0.07f, 0.10f, 0.09f, 0.06f, 0.03f, 0.01f, 0f, 0f, 0f, 0f };
    static readonly float[] SB_X_B = { 0f, 0f, 0f, 0.03f, 0.07f, 0.10f, 0.10f, 0.10f, 0.10f };

    // ShoulderJoint_B Y: rest=0.28, f5=0.30(offset+0.02)
    static readonly float[] SB_Y_A = { 0f, 0f, 0f, 0.01f, 0.01f, 0.02f, 0.02f, 0.01f, 0.01f, 0f, 0f, 0f, 0f, 0f };
    static readonly float[] SB_Y_B = { 0f, 0f, 0f, 0.01f, 0.01f, 0.02f, 0.02f, 0.02f, 0.02f };

    // ArmJoint_B rot: rest=0, f2=+40, f5=-4
    static readonly float[] AB_ROT_A = { 0f, 20f, 40f, 20f, 8f, -4f, -4f, -2f, 0f, 0f, 0f, 0f, 0f, 0f };
    static readonly float[] AB_ROT_B = { 0f, 20f, 40f, 20f, 8f, -4f, -4f, -4f, -4f };

    // Weapon (base -65): f2=0(offset), f5=-85(offset, 실측 -150)
    static readonly float[] WP_ROT_A = { 0f, 0f, 0f, -25f, -60f, -85f, -80f, -55f, -28f, -10f, -3f, 0f, 0f, 0f };
    static readonly float[] WP_ROT_B = { 0f, 0f, 0f, -25f, -60f, -85f, -85f, -85f, -85f };

    // 다리: 발 Y는 최소한만 (땅 안 뚫게)
    static readonly float[] LF_X_A = { 0f, 0.02f, 0.04f, 0.08f, 0.14f, 0.20f, 0.18f, 0.12f, 0.07f, 0.03f, 0.01f, 0f, 0f, 0f };
    static readonly float[] LF_Y_A = { 0f, 0f, 0f, -0.01f, -0.01f, -0.02f, -0.01f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };
    static readonly float[] LF_ROT_A = { 0f, -3f, -6f, -12f, -20f, -28f, -24f, -16f, -9f, -4f, -1f, 0f, 0f, 0f };
    static readonly float[] FF_ROT_A = { 0f, 2f, 4f, 8f, 14f, 20f, 17f, 11f, 6f, 3f, 1f, 0f, 0f, 0f };
    static readonly float[] LB_X_A = { 0f, 0f, -0.02f, -0.04f, -0.07f, -0.10f, -0.09f, -0.06f, -0.03f, -0.01f, 0f, 0f, 0f, 0f };
    static readonly float[] LB_Y_A = { 0f, 0f, 0f, 0.01f, 0.02f, 0.03f, 0.02f, 0.01f, 0f, 0f, 0f, 0f, 0f, 0f };
    static readonly float[] LB_ROT_A = { 0f, 3f, 6f, 10f, 15f, 20f, 17f, 11f, 6f, 3f, 1f, 0f, 0f, 0f };
    static readonly float[] FB_ROT_A = { 0f, -2f, -4f, -7f, -10f, -14f, -12f, -8f, -4f, -2f, -1f, 0f, 0f, 0f };

    static readonly float[] LF_X_B = { 0f, 0.02f, 0.04f, 0.08f, 0.14f, 0.20f, 0.20f, 0.20f, 0.20f };
    static readonly float[] LF_Y_B = { 0f, 0f, 0f, -0.01f, -0.01f, -0.02f, -0.02f, -0.02f, -0.02f };
    static readonly float[] LF_ROT_B = { 0f, -3f, -6f, -12f, -20f, -28f, -28f, -28f, -28f };
    static readonly float[] FF_ROT_B = { 0f, 2f, 4f, 8f, 14f, 20f, 20f, 20f, 20f };
    static readonly float[] LB_X_B = { 0f, 0f, -0.02f, -0.04f, -0.07f, -0.10f, -0.10f, -0.10f, -0.10f };
    static readonly float[] LB_Y_B = { 0f, 0f, 0f, 0.01f, 0.02f, 0.03f, 0.03f, 0.03f, 0.03f };
    static readonly float[] LB_ROT_B = { 0f, 3f, 6f, 10f, 15f, 20f, 20f, 20f, 20f };
    static readonly float[] FB_ROT_B = { 0f, -2f, -4f, -7f, -10f, -14f, -14f, -14f, -14f };

    // ================================================================

    [MenuItem("Tools/1/EBasicMob/Generate Thrust A (Thrust + Recovery)")]
    public static void GenerateA()
    {
        float dur = 14f / FRAME_RATE;
        AnimationClip clip = MakeClip(loop: true);

        void C(string path, string prop, float[] vals, float baseVal = 0f) =>
            AddCurve(clip, path, prop, vals, baseVal, dur, loop: true);
        void L(string path, string prop, float val) =>
            LockAt(clip, path, prop, val, dur);

        L(BODY, "localPosition.x", 0f);
        L(BODY, "localPosition.y", 0f);
        L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", BODY_ROT_A, 0f);

        L(HEAD, "localPosition.x", 0.05f);
        L(HEAD, "localPosition.y", 0.45f);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT_A, 0f);

        C(SHOULDER_F, "localPosition.x", SF_X_A, -0.27f);
        L(SHOULDER_F, "localPosition.y", 0.32f);
        L(SHOULDER_F, "localPosition.z", 0f);
        C(SHOULDER_F, "localEulerAngles.z", SF_ROT_A, 0f);
        C(ARM_F, "localEulerAngles.z", AF_ROT_A, 0f);

        C(SHOULDER_B, "localPosition.x", SB_X_A, 0.36f);
        C(SHOULDER_B, "localPosition.y", SB_Y_A, 0.28f);
        L(SHOULDER_B, "localPosition.z", 0f);
        C(SHOULDER_B, "localEulerAngles.z", SB_ROT_A, 0f);
        C(ARM_B, "localEulerAngles.z", AB_ROT_A, 0f);
        C(WEAPON, "localEulerAngles.z", WP_ROT_A, -65f);

        C(LEG_F, "localPosition.x", LF_X_A, -0.25f);
        C(LEG_F, "localPosition.y", LF_Y_A, -0.75f);
        L(LEG_F, "localPosition.z", 0f);
        C(LEG_F, "localEulerAngles.z", LF_ROT_A, 0f);
        C(FOOT_F, "localEulerAngles.z", FF_ROT_A, 0f);

        C(LEG_B, "localPosition.x", LB_X_A, 0.25f);
        C(LEG_B, "localPosition.y", LB_Y_A, -0.75f);
        L(LEG_B, "localPosition.z", 0f);
        C(LEG_B, "localEulerAngles.z", LB_ROT_A, 0f);
        C(FOOT_B, "localEulerAngles.z", FB_ROT_A, 0f);

        SaveClip(clip, "EBasicMob_Thrust_A");
    }

    [MenuItem("Tools/1/EBasicMob/Generate Thrust B (Thrust + Hold)")]
    public static void GenerateB()
    {
        float dur = 9f / FRAME_RATE;
        AnimationClip clip = MakeClip(loop: false);

        void C(string path, string prop, float[] vals, float baseVal = 0f) =>
            AddCurve(clip, path, prop, vals, baseVal, dur, loop: false);
        void L(string path, string prop, float val) =>
            LockAt(clip, path, prop, val, dur);

        L(BODY, "localPosition.x", 0f);
        L(BODY, "localPosition.y", 0f);
        L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", BODY_ROT_B, 0f);

        L(HEAD, "localPosition.x", 0.05f);
        L(HEAD, "localPosition.y", 0.45f);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT_B, 0f);

        C(SHOULDER_F, "localPosition.x", SF_X_B, -0.27f);
        L(SHOULDER_F, "localPosition.y", 0.32f);
        L(SHOULDER_F, "localPosition.z", 0f);
        C(SHOULDER_F, "localEulerAngles.z", SF_ROT_B, 0f);
        C(ARM_F, "localEulerAngles.z", AF_ROT_B, 0f);

        C(SHOULDER_B, "localPosition.x", SB_X_B, 0.36f);
        C(SHOULDER_B, "localPosition.y", SB_Y_B, 0.28f);
        L(SHOULDER_B, "localPosition.z", 0f);
        C(SHOULDER_B, "localEulerAngles.z", SB_ROT_B, 0f);
        C(ARM_B, "localEulerAngles.z", AB_ROT_B, 0f);
        C(WEAPON, "localEulerAngles.z", WP_ROT_B, -65f);

        C(LEG_F, "localPosition.x", LF_X_B, -0.25f);
        C(LEG_F, "localPosition.y", LF_Y_B, -0.75f);
        L(LEG_F, "localPosition.z", 0f);
        C(LEG_F, "localEulerAngles.z", LF_ROT_B, 0f);
        C(FOOT_F, "localEulerAngles.z", FF_ROT_B, 0f);

        C(LEG_B, "localPosition.x", LB_X_B, 0.25f);
        C(LEG_B, "localPosition.y", LB_Y_B, -0.75f);
        L(LEG_B, "localPosition.z", 0f);
        C(LEG_B, "localEulerAngles.z", LB_ROT_B, 0f);
        C(FOOT_B, "localEulerAngles.z", FB_ROT_B, 0f);

        SaveClip(clip, "EBasicMob_Thrust_B");
    }

    // ── Helpers ─────────────────────────────────────────────────────

    static AnimationClip MakeClip(bool loop)
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = FRAME_RATE;
        AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, s);
        return clip;
    }

    static void AddCurve(AnimationClip clip, string path, string prop,
        float[] vals, float baseVal, float dur, bool loop)
    {
        int n = vals.Length;
        Keyframe[] keys = new Keyframe[n + 1];
        for (int i = 0; i < n; i++)
            keys[i] = new Keyframe(i / FRAME_RATE, baseVal + vals[i]);
        keys[n] = new Keyframe(dur, baseVal + (loop ? vals[0] : vals[n - 1]));

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
            AssetDatabase.CreateFolder(Path.GetDirectoryName(SAVE_PATH), Path.GetFileName(SAVE_PATH));

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
        Debug.Log($"[EBasicMob] {clipName} created -> {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}