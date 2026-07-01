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
    // Body            localPos ( 0.00,  0.00, 0)  rot 0
    // Head            localPos ( 0.05,  0.45, 0)  rot 0
    // ShoulderJoint_F localPos (-0.27,  0.32, 0)  rot 0
    // ShoulderJoint_B localPos ( 0.36,  0.28, 0)  rot 0
    // ArmJoint_F      localPos (-0.25, -0.30, 0)  rot 0
    // ArmJoint_B      localPos ( 0.06, -0.24, 0)  rot 0
    // Weapon                                       rot -65
    // LegJoint_F      localPos (-0.25, -0.75, 0)  rot 0
    // LegJoint_B      localPos ( 0.25, -0.75, 0)  rot 0
    //
    // 확인된 방향:
    //   SB rot 음수 = 어깨 뒤로 / 양수 = 어깨 앞으로
    //   AB rot 음수 = 팔 접힘   / 0에 가까울수록 펴짐
    //
    // f2 요청 자세: SB=-30, AB=-20
    // f4 임팩트:    SB=+20, AB=0, SB_X 앞으로 +0.10
    // ================================================================

    // ── A: 12프레임 루프 ─────────────────────────────────────────────
    // f0  : 대기
    // f1  : 예비 시작
    // f2  : 풀 예비 (SB=-30, AB=-20)
    // f3  : 찌르기 시작
    // f4  : 임팩트 (SB=+20, AB=0, X 앞으로)
    // f5  : 임팩트 유지
    // f6~11: 복귀

    // ── B: 9프레임 비루프 ────────────────────────────────────────────
    // f0~f5 동일, f6~8 유지

    static readonly float[] BODY_ROT_A = { 0f, 3f, 5f, 1f, -7f, -5f, -3f, -2f, -1f, 0f, 0f, 0f };
    static readonly float[] BODY_ROT_B = { 0f, 3f, 5f, 1f, -7f, -7f, -7f, -7f, -7f };

    static readonly float[] HEAD_ROT_A = { 0f, -2f, -3f, 0f, 4f, 3f, 2f, 1f, 0f, 0f, 0f, 0f };
    static readonly float[] HEAD_ROT_B = { 0f, -2f, -3f, 0f, 4f, 4f, 4f, 4f, 4f };

    static readonly float[] SF_ROT_A = { 0f, 3f, 5f, 1f, -6f, -5f, -3f, -1f, 0f, 0f, 0f, 0f };
    static readonly float[] SF_ROT_B = { 0f, 3f, 5f, 1f, -6f, -6f, -6f, -6f, -6f };

    // SB rot: f2=-30(요청), f4=+20(임팩트)
    static readonly float[] SB_ROT_A = { 0f, -15f, -30f, -10f, 20f, 15f, 8f, 4f, 1f, 0f, 0f, 0f };
    static readonly float[] SB_ROT_B = { 0f, -15f, -30f, -10f, 20f, 20f, 20f, 20f, 20f };

    // SB X: rest=0.36, 예비 시 살짝 뒤(-0.04), 임팩트 시 앞(+0.10)
    static readonly float[] SB_X_A = { 0f, 0f, -0.04f, 0.04f, 0.10f, 0.08f, 0.05f, 0.02f, 0.01f, 0f, 0f, 0f };
    static readonly float[] SB_X_B = { 0f, 0f, -0.04f, 0.04f, 0.10f, 0.10f, 0.10f, 0.10f, 0.10f };

    static readonly float[] AF_ROT_A = { 0f, 4f, 6f, 2f, -5f, -4f, -2f, -1f, 0f, 0f, 0f, 0f };
    static readonly float[] AF_ROT_B = { 0f, 4f, 6f, 2f, -5f, -5f, -5f, -5f, -5f };

    // AB rot: f2=-20(요청), f4=0(완전히 펴짐)
    static readonly float[] AB_ROT_A = { 0f, -10f, -20f, -10f, 0f, -2f, -5f, -2f, 0f, 0f, 0f, 0f };
    static readonly float[] AB_ROT_B = { 0f, -10f, -20f, -10f, 0f, 0f, 0f, 0f, 0f };

    // Weapon (base -65):
    // f2: SB(-30)+AB(-20)+WP(+10) = -40 → -65-40 ≈ 뒤 수평
    // f4: SB(+20)+AB(0)+WP(-10)   = +10 → -65+10 = -55 ≈ 앞 수평
    static readonly float[] WP_ROT_A = { 0f, 5f, 10f, 4f, -10f, -8f, -4f, -1f, 0f, 0f, 0f, 0f };
    static readonly float[] WP_ROT_B = { 0f, 5f, 10f, 4f, -10f, -10f, -10f, -10f, -10f };

    // 다리
    static readonly float[] LF_X_A = { 0f, 0f, 0f, 0.03f, 0.10f, 0.07f, 0.04f, 0.02f, 0f, 0f, 0f, 0f };
    static readonly float[] LF_ROT_A = { 0f, -2f, -4f, -6f, -16f, -11f, -6f, -3f, 0f, 0f, 0f, 0f };
    static readonly float[] FF_ROT_A = { 0f, 2f, 3f, 5f, 12f, 9f, 5f, 2f, 0f, 0f, 0f, 0f };
    static readonly float[] LB_X_A = { 0f, 0f, 0f, -0.02f, -0.05f, -0.04f, -0.02f, -0.01f, 0f, 0f, 0f, 0f };
    static readonly float[] LB_ROT_A = { 0f, 2f, 4f, 3f, 8f, 6f, 3f, 1f, 0f, 0f, 0f, 0f };
    static readonly float[] FB_ROT_A = { 0f, -1f, -2f, -2f, -6f, -4f, -2f, -1f, 0f, 0f, 0f, 0f };

    static readonly float[] LF_X_B = { 0f, 0f, 0f, 0.03f, 0.10f, 0.10f, 0.10f, 0.10f, 0.10f };
    static readonly float[] LF_ROT_B = { 0f, -2f, -4f, -6f, -16f, -16f, -16f, -16f, -16f };
    static readonly float[] FF_ROT_B = { 0f, 2f, 3f, 5f, 12f, 12f, 12f, 12f, 12f };
    static readonly float[] LB_X_B = { 0f, 0f, 0f, -0.02f, -0.05f, -0.05f, -0.05f, -0.05f, -0.05f };
    static readonly float[] LB_ROT_B = { 0f, 2f, 4f, 3f, 8f, 8f, 8f, 8f, 8f };
    static readonly float[] FB_ROT_B = { 0f, -1f, -2f, -2f, -6f, -6f, -6f, -6f, -6f };

    // ================================================================

    [MenuItem("Tools/EBasicMob/Generate Thrust A (Thrust + Recovery)")]
    public static void GenerateA()
    {
        float dur = 12f / FRAME_RATE;
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

        L(SHOULDER_F, "localPosition.x", -0.27f);
        L(SHOULDER_F, "localPosition.y", 0.32f);
        L(SHOULDER_F, "localPosition.z", 0f);
        C(SHOULDER_F, "localEulerAngles.z", SF_ROT_A, 0f);

        C(SHOULDER_B, "localPosition.x", SB_X_A, 0.36f);
        L(SHOULDER_B, "localPosition.y", 0.28f);
        L(SHOULDER_B, "localPosition.z", 0f);
        C(SHOULDER_B, "localEulerAngles.z", SB_ROT_A, 0f);

        C(ARM_F, "localEulerAngles.z", AF_ROT_A, 0f);
        C(ARM_B, "localEulerAngles.z", AB_ROT_A, 0f);
        C(WEAPON, "localEulerAngles.z", WP_ROT_A, -65f);

        C(LEG_F, "localPosition.x", LF_X_A, -0.25f);
        L(LEG_F, "localPosition.y", -0.75f);
        L(LEG_F, "localPosition.z", 0f);
        C(LEG_F, "localEulerAngles.z", LF_ROT_A, 0f);
        C(FOOT_F, "localEulerAngles.z", FF_ROT_A, 0f);

        C(LEG_B, "localPosition.x", LB_X_A, 0.25f);
        L(LEG_B, "localPosition.y", -0.75f);
        L(LEG_B, "localPosition.z", 0f);
        C(LEG_B, "localEulerAngles.z", LB_ROT_A, 0f);
        C(FOOT_B, "localEulerAngles.z", FB_ROT_A, 0f);

        SaveClip(clip, "EBasicMob_Thrust_A");
    }

    [MenuItem("Tools/EBasicMob/Generate Thrust B (Thrust + Hold)")]
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

        L(SHOULDER_F, "localPosition.x", -0.27f);
        L(SHOULDER_F, "localPosition.y", 0.32f);
        L(SHOULDER_F, "localPosition.z", 0f);
        C(SHOULDER_F, "localEulerAngles.z", SF_ROT_B, 0f);

        C(SHOULDER_B, "localPosition.x", SB_X_B, 0.36f);
        L(SHOULDER_B, "localPosition.y", 0.28f);
        L(SHOULDER_B, "localPosition.z", 0f);
        C(SHOULDER_B, "localEulerAngles.z", SB_ROT_B, 0f);

        C(ARM_F, "localEulerAngles.z", AF_ROT_B, 0f);
        C(ARM_B, "localEulerAngles.z", AB_ROT_B, 0f);
        C(WEAPON, "localEulerAngles.z", WP_ROT_B, -65f);

        C(LEG_F, "localPosition.x", LF_X_B, -0.25f);
        L(LEG_F, "localPosition.y", -0.75f);
        L(LEG_F, "localPosition.z", 0f);
        C(LEG_F, "localEulerAngles.z", LF_ROT_B, 0f);
        C(FOOT_F, "localEulerAngles.z", FF_ROT_B, 0f);

        C(LEG_B, "localPosition.x", LB_X_B, 0.25f);
        L(LEG_B, "localPosition.y", -0.75f);
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