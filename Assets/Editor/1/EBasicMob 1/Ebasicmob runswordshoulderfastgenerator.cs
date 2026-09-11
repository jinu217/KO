using UnityEngine;
using UnityEditor;
using System.IO;

public class EBasicMob_RunSwordShoulderFastGenerator
{
    const string SAVE_PATH = "Assets/Animations/EBasicMob";
    const string CLIP_NAME = "EBasicMob_Run_SwordShoulder_Fast";
    const float FRAME_RATE = 12f;
    const int TOTAL_FRAMES = 8;
    const float DURATION = TOTAL_FRAMES / FRAME_RATE;

    const string BODY = "Body";
    const string HEAD = "Body/Head";
    const string SHOULDER_F = "Body/ShoulderJoint_F";
    const string SHOULDER_B = "Body/ShoulderJoint_B";
    const string ARM_F = "Body/ShoulderJoint_F/Shoulder_F/ArmJoint_F";
    const string ARM_B = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B";
    const string HAND_B = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B/Hand_B";
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
    // Hand_B          rot 105
    // Weapon          rot -65
    // LegJoint_F      pos (-0.25,-0.75)  rot 0
    // LegJoint_B      pos ( 0.25,-0.75)  rot 0
    // ================================================================

    // Body: 바운스 크게 + 앞으로 기울기 강하게
    static readonly float[] BODY_Y = { -0.07f, 0.02f, 0.08f, 0.02f, -0.07f, 0.02f, 0.08f, 0.02f };
    static readonly float[] BODY_ROT = { -10f, -8f, -6f, -8f, -10f, -8f, -6f, -8f };

    // Head: 기울기 보정 + 달리는 충격으로 살짝 흔들림
    static readonly float[] HEAD_ROT = { 6f, 4f, 2f, 4f, 6f, 4f, 2f, 4f };

    // ShoulderJoint_F: 더 큰 스윙 ±15
    static readonly float[] SF_ROT = { -14f, -8f, 3f, 12f, 16f, 10f, -3f, -11f };
    static readonly float[] SF_Y = { 0.02f, 0.01f, -0.01f, -0.02f, -0.01f, 0.01f, 0.02f, 0.01f };

    // ArmJoint_F: 어깨 따라 더 크게
    static readonly float[] AF_ROT = { -12f, -6f, 4f, 10f, 14f, 8f, -4f, -10f };

    // ShoulderJoint_B: 검 들고 뛰는 진동 ±5
    static readonly float[] SB_ROT = { -4f, -2f, 0f, 2f, 4f, 2f, 0f, -2f };
    static readonly float[] SB_Y = { 0.02f, 0.01f, -0.01f, 0.00f, 0.02f, 0.01f, -0.01f, 0.00f };

    // ArmJoint_B: 진동 ±4
    static readonly float[] AB_ROT = { -4f, -2f, 0f, 2f, 4f, 2f, 0f, -2f };

    // Hand_B: rest=105, 뛰는 충격 ±3
    static readonly float[] HB_ROT = { -3f, -1.5f, 0f, 1.5f, 3f, 1.5f, 0f, -1.5f };

    // Weapon: 진동 ±1.5
    static readonly float[] WP_ROT = { -1.5f, -0.5f, 0f, 0.5f, 1.5f, 0.5f, 0f, -0.5f };

    // ── 건메이햄 다리: 더 넓은 스트라이드, 더 높은 무릎, Y 양수만 ───
    static readonly float[] LF_X = { 0.22f, 0.10f, -0.06f, -0.24f, -0.22f, -0.06f, 0.13f, 0.22f };
    static readonly float[] LF_Y = { 0.00f, 0.04f, 0.06f, 0.20f, 0.36f, 0.24f, 0.10f, 0.00f };
    static readonly float[] LF_ROT = { 25f, 12f, -7f, -30f, -20f, -7f, 16f, 27f };
    static readonly float[] FF_ROT = { -20f, -10f, 7f, 22f, -25f, -18f, -13f, -22f };

    static readonly float[] LB_X = { -0.22f, -0.06f, 0.13f, 0.22f, 0.22f, 0.10f, -0.06f, -0.24f };
    static readonly float[] LB_Y = { 0.36f, 0.24f, 0.10f, 0.00f, 0.00f, 0.04f, 0.06f, 0.20f };
    static readonly float[] LB_ROT = { -20f, -7f, 16f, 27f, 25f, 12f, -7f, -30f };
    static readonly float[] FB_ROT = { -25f, -18f, -13f, -22f, -20f, -10f, 7f, 22f };

    [MenuItem("Tools/EBasicMob/Generate Run SwordShoulder Fast")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
            AssetDatabase.CreateFolder(Path.GetDirectoryName(SAVE_PATH), Path.GetFileName(SAVE_PATH));

        AnimationClip clip = new AnimationClip();
        clip.frameRate = FRAME_RATE;
        AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        void C(string path, string prop, float[] offsets, float baseVal = 0f)
        {
            Keyframe[] keys = new Keyframe[offsets.Length + 1];
            for (int i = 0; i < offsets.Length; i++)
                keys[i] = new Keyframe(i / FRAME_RATE, baseVal + offsets[i]);
            keys[offsets.Length] = new Keyframe(DURATION, baseVal + offsets[0]);
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
                AnimationCurve.Linear(0f, val, DURATION, val));

        // Body
        L(BODY, "localPosition.x", 0f);
        C(BODY, "localPosition.y", BODY_Y, 0f);
        L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", BODY_ROT, 0f);

        // Head
        L(HEAD, "localPosition.x", 0.05f);
        L(HEAD, "localPosition.y", 0.45f);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT, 0f);

        // ShoulderJoint_F
        L(SHOULDER_F, "localPosition.x", -0.27f);
        C(SHOULDER_F, "localPosition.y", SF_Y, 0.32f);
        L(SHOULDER_F, "localPosition.z", 0f);
        C(SHOULDER_F, "localEulerAngles.z", SF_ROT, 0f);
        C(ARM_F, "localEulerAngles.z", AF_ROT, 0f);

        // ShoulderJoint_B: X 고정
        L(SHOULDER_B, "localPosition.x", 0.36f);
        C(SHOULDER_B, "localPosition.y", SB_Y, 0.28f);
        L(SHOULDER_B, "localPosition.z", 0f);
        C(SHOULDER_B, "localEulerAngles.z", SB_ROT, 0f);
        C(ARM_B, "localEulerAngles.z", AB_ROT, 0f);

        // Hand_B: rest=105
        C(HAND_B, "localEulerAngles.z", HB_ROT, 105f);

        // Weapon: rest=-65
        C(WEAPON, "localEulerAngles.z", WP_ROT, -65f);

        // Legs
        C(LEG_F, "localPosition.x", LF_X, -0.25f);
        C(LEG_F, "localPosition.y", LF_Y, -0.75f);
        L(LEG_F, "localPosition.z", 0f);
        C(LEG_F, "localEulerAngles.z", LF_ROT, 0f);
        C(FOOT_F, "localEulerAngles.z", FF_ROT, 0f);

        C(LEG_B, "localPosition.x", LB_X, 0.25f);
        C(LEG_B, "localPosition.y", LB_Y, -0.75f);
        L(LEG_B, "localPosition.z", 0f);
        C(LEG_B, "localEulerAngles.z", LB_ROT, 0f);
        C(FOOT_B, "localEulerAngles.z", FB_ROT, 0f);

        string assetPath = $"{SAVE_PATH}/{CLIP_NAME}.anim";
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
        Debug.Log($"[EBasicMob] {CLIP_NAME} created -> {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}