using UnityEngine;
using UnityEditor;
using System.IO;

public class Daughter_WalkGenerator
{
    const string SAVE_PATH    = "Assets/Animations/Daughter";
    const string CLIP_NAME    = "Daughter_Walk";
    const float  FRAME_RATE   = 12f;
    const int    TOTAL_FRAMES = 8;
    const float  DURATION     = TOTAL_FRAMES / FRAME_RATE;

    const string BODY       = "Body";
    const string HEAD       = "Body/Head";
    const string HEAD_B     = "Body/Head/Head_B";
    const string SHOULDER_F = "Body/ShoulderJoint_F";
    const string ARM_F      = "Body/ShoulderJoint_F/Shoulder_F/ArmJoint_F";
    const string SHOULDER_B = "Body/ShoulderJoint_B";
    const string ARM_B      = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B";
    const string LEG_F      = "Body/LegJoint_F";
    const string FOOT_F     = "Body/LegJoint_F/Foot_F";
    const string LEG_B      = "Body/LegJoint_B";
    const string FOOT_B     = "Body/LegJoint_B/Foot_B";

    // ================================================================
    // REST POSE (2026-07-09 14:21 추출 기준)
    // Body            pos (0.00,   0.00)
    // Head            pos (-0.04,  0.46)
    // Head_B          pos (-0.085, 0.17)
    // ShoulderJoint_F pos (-0.29,  0.353)
    // ArmJoint_F      pos (0.00,  -0.25)
    // ShoulderJoint_B pos ( 0.08,  0.353)
    // ArmJoint_B      pos (0.00,  -0.25)
    // LegJoint_F      pos (-0.26, -0.58)
    // LegJoint_B      pos ( 0.05, -0.58)
    // ================================================================

    // Body: 우아한 걸음 — 바운스 작게
    static readonly float[] BODY_Y   = { -0.012f, 0.004f, 0.014f, 0.004f, -0.012f, 0.004f, 0.014f, 0.004f };
    static readonly float[] BODY_ROT = { -1.5f, -1f, -0.5f, -1f, -1.5f, -1f, -0.5f, -1f };

    // Head
    static readonly float[] HEAD_ROT = { 0.5f, 0f, -0.5f, 0f, 0.5f, 0f, -0.5f, 0f };

    // Head_B: 찰랑 — 반 박자 늦은 위상
    static readonly float[] HEAD_B_ROT = { -3f, -5f, -3f, 0f, 3f, 5f, 3f, 0f };
    static readonly float[] HEAD_B_Y   = { 0.004f, 0.002f, -0.002f, -0.004f, -0.002f, 0.002f, 0.004f, 0.002f };

    // ShoulderJoint_F: ±6
    static readonly float[] SF_ROT = { -5f, -3f, 1f, 4f, 6f, 4f, 0f, -3f };
    static readonly float[] SF_Y   = { 0.006f, 0f, -0.006f, -0.006f, 0f, 0.006f, 0.006f, 0f };
    static readonly float[] AF_ROT = { -4f, -2f, 1f, 3f, 5f, 3f, 0f, -2f };

    // ShoulderJoint_B: 반대 위상
    static readonly float[] SB_ROT = { 5f, 3f, -1f, -4f, -6f, -4f, 0f, 3f };
    static readonly float[] SB_Y   = { -0.006f, 0f, 0.006f, 0.006f, 0f, -0.006f, -0.006f, 0f };
    static readonly float[] AB_ROT = { 4f, 2f, -1f, -3f, -5f, -3f, 0f, 2f };

    // 다리: 우아하게, Y 양수만
    static readonly float[] LF_X   = {  0.09f,  0.04f, -0.02f, -0.09f, -0.07f, -0.02f,  0.04f,  0.09f };
    static readonly float[] LF_Y   = {  0.00f,  0.008f, 0.014f, 0.050f, 0.110f, 0.065f, 0.022f,  0.00f };
    static readonly float[] LF_ROT = {  9f,     5f,    -2f,   -11f,    -7f,    -2f,     5f,     10f   };
    static readonly float[] FF_ROT = { -7f,    -4f,     2f,    7f,    -9f,    -6f,    -4f,    -8f   };

    static readonly float[] LB_X   = { -0.07f, -0.02f,  0.04f,  0.09f,  0.09f,  0.04f, -0.02f, -0.09f };
    static readonly float[] LB_Y   = {  0.110f, 0.065f, 0.022f,  0.00f,  0.00f,  0.008f, 0.014f, 0.050f };
    static readonly float[] LB_ROT = { -7f,    -2f,     5f,    10f,     9f,     5f,    -2f,   -11f   };
    static readonly float[] FB_ROT = { -9f,    -6f,    -4f,   -8f,    -7f,    -4f,     2f,    7f    };

    [MenuItem("Tools/NPC1/Daughter/Generate Walk")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "Daughter");
        }

        AnimationClip clip = new AnimationClip();
        clip.frameRate = FRAME_RATE;
        clip.name = CLIP_NAME;
        AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        void C(string path, string prop, float[] offsets, float baseVal = 0f)
        {
            int n = offsets.Length;
            Keyframe[] keys = new Keyframe[n + 1];
            for (int i = 0; i < n; i++)
                keys[i] = new Keyframe(i / FRAME_RATE, baseVal + offsets[i]);
            keys[n] = new Keyframe(DURATION, baseVal + offsets[0]);
            AnimationCurve c = new AnimationCurve(keys);
            for (int i = 0; i < c.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(c,  i, AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(c, i, AnimationUtility.TangentMode.Auto);
            }
            clip.SetCurve(path, typeof(Transform), prop, c);
        }

        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop,
                AnimationCurve.Linear(0f, val, DURATION, val));

        // Body
        L(BODY, "localPosition.x",    0f);
        C(BODY, "localPosition.y",    BODY_Y,   0f);
        L(BODY, "localPosition.z",    0f);
        C(BODY, "localEulerAngles.z", BODY_ROT, 0f);

        // Head
        L(HEAD, "localPosition.x",   -0.04f);
        L(HEAD, "localPosition.y",    0.46f);
        L(HEAD, "localPosition.z",    0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT, 0f);

        // Head_B
        L(HEAD_B, "localPosition.x",   -0.085f);
        C(HEAD_B, "localPosition.y",    HEAD_B_Y,   0.17f);
        L(HEAD_B, "localPosition.z",    0f);
        C(HEAD_B, "localEulerAngles.z", HEAD_B_ROT, 0f);

        // ShoulderJoint_F
        L(SHOULDER_F, "localPosition.x",   -0.30f);
        C(SHOULDER_F, "localPosition.y",    SF_Y,   0.32f);
        L(SHOULDER_F, "localPosition.z",   0f);
        C(SHOULDER_F, "localEulerAngles.z", SF_ROT, 0f);
        C(ARM_F,      "localEulerAngles.z", AF_ROT, 0f);

        // ShoulderJoint_B
        L(SHOULDER_B, "localPosition.x",   0.08f);
        C(SHOULDER_B, "localPosition.y",    SB_Y,   0.32f);
        L(SHOULDER_B, "localPosition.z",   0f);
        C(SHOULDER_B, "localEulerAngles.z", SB_ROT, 0f);
        C(ARM_B,      "localEulerAngles.z", AB_ROT, 0f);

        // Legs: Y 양수만, base -0.58
        C(LEG_F, "localPosition.x",    LF_X,  -0.26f);
        C(LEG_F, "localPosition.y",    LF_Y,  -0.58f);
        L(LEG_F, "localPosition.z",    0f);
        C(LEG_F, "localEulerAngles.z", LF_ROT, 0f);
        C(FOOT_F,"localEulerAngles.z", FF_ROT, 0f);

        C(LEG_B, "localPosition.x",    LB_X,   0.05f);
        C(LEG_B, "localPosition.y",    LB_Y,  -0.58f);
        L(LEG_B, "localPosition.z",    0f);
        C(LEG_B, "localEulerAngles.z", LB_ROT, 0f);
        C(FOOT_B,"localEulerAngles.z", FB_ROT, 0f);

        string assetPath = $"{SAVE_PATH}/{CLIP_NAME}.anim";
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(clip, existing);
            existing.name = CLIP_NAME;
            AssetDatabase.SaveAssets();
        }
        else
        {
            AssetDatabase.CreateAsset(clip, assetPath);
            AssetDatabase.SaveAssets();
        }
        AssetDatabase.Refresh();
        Debug.Log($"[Daughter] {CLIP_NAME} created -> {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}
