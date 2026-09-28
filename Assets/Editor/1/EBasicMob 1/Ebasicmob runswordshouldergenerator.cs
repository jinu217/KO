using UnityEngine;
using UnityEditor;
using System.IO;

public class EBasicMob_RunSwordShoulderGenerator
{
    const string SAVE_PATH = "Assets/Animations/EBasicMob";
    const string CLIP_NAME = "EBasicMob_Run_SwordShoulder";
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
    // REST POSE (이 모션 기준)
    // Body            pos (0.00,  0.00)  rot 0
    // Head            pos (0.05,  0.45)  rot 0
    // ShoulderJoint_F pos (-0.27, 0.32)  rot 0
    // ArmJoint_F      rot 0
    // ShoulderJoint_B pos (0.36,  0.28)  rot 0
    // ArmJoint_B      rot 0
    // Hand_B          rot 105            ← 검 어깨에 지는 자세
    // Weapon          rot -65
    // LegJoint_F      pos (-0.25,-0.75)  rot 0
    // LegJoint_B      pos ( 0.25,-0.75)  rot 0
    // ================================================================

    // Body: Y 바운스만, 달리면서 앞으로 살짝 기울기
    static readonly float[] BODY_Y = { -0.04f, 0.01f, 0.05f, 0.01f, -0.04f, 0.01f, 0.05f, 0.01f };
    static readonly float[] BODY_ROT = { -5f, -4f, -3f, -4f, -5f, -4f, -3f, -4f };

    // Head: 기울기 보정
    static readonly float[] HEAD_ROT = { 3f, 2f, 1f, 2f, 3f, 2f, 1f, 2f };

    // ShoulderJoint_F (오른손): 달리기 스윙 ±10
    static readonly float[] SF_ROT = { -9f, -5f, 2f, 8f, 10f, 6f, -2f, -7f };
    static readonly float[] SF_Y = { 0.01f, 0.00f, -0.01f, -0.01f, 0.00f, 0.01f, 0.01f, 0.00f };

    // ArmJoint_F: 어깨 따라 작게 스윙
    static readonly float[] AF_ROT = { -8f, -4f, 2f, 7f, 9f, 5f, -2f, -6f };

    // ShoulderJoint_B (왼손/검): 검을 어깨에 지고 있으므로 진동만 최소화
    // 달리는 동안 어깨가 살짝 올라갔다 내려오는 느낌
    static readonly float[] SB_ROT = { -2f, -1f, 0f, 1f, 2f, 1f, 0f, -1f };
    static readonly float[] SB_Y = { 0.01f, 0.00f, -0.01f, 0.00f, 0.01f, 0.00f, -0.01f, 0.00f };

    // ArmJoint_B: 거의 고정, 아주 작은 진동
    static readonly float[] AB_ROT = { -2f, -1f, 0f, 1f, 2f, 1f, 0f, -1f };

    // Hand_B: rest=105, 달리는 진동으로 ±3만
    static readonly float[] HB_ROT = { -3f, -2f, 0f, 2f, 3f, 2f, 0f, -2f };

    // Weapon: Hand_B 고정이니 거의 안 움직임
    static readonly float[] WP_ROT = { -2f, -1f, 0f, 1f, 2f, 1f, 0f, -1f };

    // ── 건메이햄 다리 (Y 최소화, 땅 안 뚫게) ─────────────────────────
    // LegJoint_F base (-0.25, -0.75): Y offset 양수만 (위로만)
    static readonly float[] LF_X = { 0.18f, 0.08f, -0.05f, -0.20f, -0.18f, -0.05f, 0.10f, 0.18f };
    static readonly float[] LF_Y = { 0.00f, 0.02f, 0.04f, 0.14f, 0.28f, 0.18f, 0.07f, 0.00f };
    static readonly float[] LF_ROT = { 20f, 10f, -5f, -25f, -15f, -5f, 13f, 22f };
    static readonly float[] FF_ROT = { -16f, -8f, 5f, 18f, -20f, -14f, -10f, -18f };

    // LegJoint_B base (0.25, -0.75): 반대 위상
    static readonly float[] LB_X = { -0.18f, -0.05f, 0.10f, 0.18f, 0.18f, 0.08f, -0.05f, -0.20f };
    static readonly float[] LB_Y = { 0.28f, 0.18f, 0.07f, 0.00f, 0.00f, 0.02f, 0.04f, 0.14f };
    static readonly float[] LB_ROT = { -15f, -5f, 13f, 22f, 20f, 10f, -5f, -25f };
    static readonly float[] FB_ROT = { -20f, -14f, -10f, -18f, -16f, -8f, 5f, 18f };

    [MenuItem("Tools/1/EBasicMob/Generate Run SwordShoulder")]
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

        // ShoulderJoint_B: X 고정, Y/rot 최소 진동
        L(SHOULDER_B, "localPosition.x", 0.36f);
        C(SHOULDER_B, "localPosition.y", SB_Y, 0.28f);
        L(SHOULDER_B, "localPosition.z", 0f);
        C(SHOULDER_B, "localEulerAngles.z", SB_ROT, 0f);
        C(ARM_B, "localEulerAngles.z", AB_ROT, 0f);

        // Hand_B: rest=105, 진동 ±3
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