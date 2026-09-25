using UnityEngine;
using UnityEditor;
using System.IO;

public class SpeedMob_Attack1Generator
{
    const string SAVE_PATH = "Assets/Animations/SpeedMob 1";
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
    // Body pos(0,0) rot 0 / Head pos(0,0.324) / SF pos(-0.21,0.22)
    // SB pos(0.27,0.22) / Weapon rot -65
    // LegF pos(-0.20,-0.475) / LegB pos(0.15,-0.475)
    //
    // 1顫 鎬腦晦: f0=渠晦, f1~f2=蕨綠, f3~f4=歜ねお, f5~f8=犒敝 (9Щ溯歜)
    // 2顫 漆晦:   f0=渠晦, f1~f2=蕨綠, f3~f4=歜ねお, f5~f8=犒敝 (9Щ溯歜)
    // ================================================================

    // 式式 Attack1 (鎬腦晦): 9Щ溯歜 式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
    // f0=渠晦, f1~f2=蕨綠(菴煎), f3~f4=歜ねお, f5~f8=犒敝
    static readonly float[] A1_BODY_ROT = { 0f, 5f, 8f, -10f, -12f, -6f, -3f, -1f, 0f };
    static readonly float[] A1_HEAD_ROT = { 0f, -3f, -4f, 6f, 7f, 4f, 2f, 1f, 0f };
    static readonly float[] A1_SF_ROT = { 0f, 8f, 12f, -8f, -12f, -6f, -3f, -1f, 0f };
    static readonly float[] A1_AF_ROT = { 0f, 6f, 10f, -6f, -10f, -5f, -2f, 0f, 0f };
    static readonly float[] A1_SB_ROT = { 0f, -12f, -25f, 40f, 45f, 22f, 8f, 2f, 0f };
    static readonly float[] A1_AB_ROT = { 0f, -15f, -30f, 5f, 0f, -5f, -2f, 0f, 0f };
    static readonly float[] A1_WP_ROT = { 0f, -8f, -15f, 25f, 30f, 15f, 6f, 1f, 0f };
    static readonly float[] A1_LF_X = { 0f, 0f, 0f, 0.06f, 0.08f, 0.05f, 0.02f, 0.01f, 0f };
    static readonly float[] A1_LB_X = { 0f, 0f, 0f, -0.03f, -0.04f, -0.02f, -0.01f, 0f, 0f };
    static readonly float[] A1_LEG_COMP = { 0f, -5f, -8f, 10f, 12f, 6f, 3f, 1f, 0f };

    // 式式 Attack2 (漆晦): 9Щ溯歜 式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式式
    // f0=渠晦, f1~f2=蕨綠(嬪煎 菟晦), f3~f4=歜ねお(頂溥藹), f5~f8=犒敝
    static readonly float[] A2_BODY_ROT = { 0f, 10f, 18f, -20f, -16f, -8f, -4f, -1f, 0f };
    static readonly float[] A2_HEAD_ROT = { 0f, -5f, -9f, 11f, 9f, 5f, 2f, 1f, 0f };
    static readonly float[] A2_SF_ROT = { 0f, -18f, -28f, 16f, 12f, 6f, 2f, 0f, 0f };
    static readonly float[] A2_AF_ROT = { 0f, -15f, -22f, 12f, 9f, 4f, 1f, 0f, 0f };
    static readonly float[] A2_SB_ROT = { 0f, 55f, 90f, -40f, -60f, -30f, -10f, -2f, 0f };
    static readonly float[] A2_AB_ROT = { 0f, -15f, -25f, 30f, 40f, 18f, 5f, 0f, 0f };
    static readonly float[] A2_WP_ROT = { 0f, -10f, -25f, -50f, -65f, -32f, -12f, -2f, 0f };
    static readonly float[] A2_LF_X = { 0f, 0.06f, 0.05f, 0.14f, 0.16f, 0.09f, 0.04f, 0.01f, 0f };
    static readonly float[] A2_LB_X = { 0f, -0.03f, -0.02f, -0.07f, -0.08f, -0.05f, -0.02f, 0f, 0f };
    static readonly float[] A2_LEG_COMP = { 0f, -10f, -18f, 20f, 16f, 8f, 4f, 1f, 0f };

    [MenuItem("Tools/1/SpeedMob 1/Generate Attack1 (Thrust Only)")]
    public static void GenerateAttack1() => Build("SpeedMob1_Attack1", 9,
        A1_BODY_ROT, A1_HEAD_ROT,
        A1_SF_ROT, A1_AF_ROT,
        A1_SB_ROT, A1_AB_ROT, A1_WP_ROT,
        A1_LF_X, A1_LB_X, A1_LEG_COMP);

    [MenuItem("Tools/1/SpeedMob 1/Generate Attack2 (Slash Only)")]
    public static void GenerateAttack2() => Build("SpeedMob1_Attack2", 9,
        A2_BODY_ROT, A2_HEAD_ROT,
        A2_SF_ROT, A2_AF_ROT,
        A2_SB_ROT, A2_AB_ROT, A2_WP_ROT,
        A2_LF_X, A2_LB_X, A2_LEG_COMP);

    static void Build(string clipName, int totalFrames,
        float[] bodyRot, float[] headRot,
        float[] sfRot, float[] afRot,
        float[] sbRot, float[] abRot, float[] wpRot,
        float[] lfX, float[] lbX, float[] legComp)
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "SpeedMob 1");
        }

        float dur = totalFrames / FRAME_RATE;
        AnimationClip clip = new AnimationClip();
        clip.frameRate = FRAME_RATE;
        clip.name = clipName;
        AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        void C(string path, string prop, float[] offsets, float baseVal = 0f)
        {
            int n = offsets.Length;
            Keyframe[] keys = new Keyframe[n + 1];
            for (int i = 0; i < n; i++)
                keys[i] = new Keyframe(i / FRAME_RATE, baseVal + offsets[i]);
            keys[n] = new Keyframe(dur, baseVal + offsets[0]);
            AnimationCurve c = new AnimationCurve(keys);
            for (int i = 0; i < c.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(c, i, AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(c, i, AnimationUtility.TangentMode.Auto);
            }
            clip.SetCurve(path, typeof(Transform), prop, c);
        }

        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop,
                AnimationCurve.Linear(0f, val, dur, val));

        L(BODY, "localPosition.x", 0f);
        L(BODY, "localPosition.y", 0f);
        L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", bodyRot, 0f);

        L(HEAD, "localPosition.x", 0f);
        L(HEAD, "localPosition.y", 0.324f);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", headRot, 0f);

        L(SHOULDER_F, "localPosition.x", -0.21f);
        L(SHOULDER_F, "localPosition.y", 0.22f);
        L(SHOULDER_F, "localPosition.z", 0f);
        C(SHOULDER_F, "localEulerAngles.z", sfRot, 0f);
        C(ARM_F, "localEulerAngles.z", afRot, 0f);

        L(SHOULDER_B, "localPosition.x", 0.27f);
        L(SHOULDER_B, "localPosition.y", 0.22f);
        L(SHOULDER_B, "localPosition.z", 0f);
        C(SHOULDER_B, "localEulerAngles.z", sbRot, 0f);
        C(ARM_B, "localEulerAngles.z", abRot, 0f);
        C(WEAPON, "localEulerAngles.z", wpRot, -65f);

        C(LEG_F, "localPosition.x", lfX, -0.20f);
        L(LEG_F, "localPosition.y", -0.475f);
        L(LEG_F, "localPosition.z", 0f);
        C(LEG_F, "localEulerAngles.z", legComp, 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);

        C(LEG_B, "localPosition.x", lbX, 0.15f);
        L(LEG_B, "localPosition.y", -0.475f);
        L(LEG_B, "localPosition.z", 0f);
        C(LEG_B, "localEulerAngles.z", legComp, 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

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
        Debug.Log($"[SpeedMob 1] {clipName} created -> {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}