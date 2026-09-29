using UnityEngine;
using UnityEditor;
using System.IO;

public class Daughter_SkipGenerator
{
    const string SAVE_PATH    = "Assets/Animations/Daughter";
    const string CLIP_NAME    = "Daughter_Skip";
    const float  FRAME_RATE   = 12f;
    const int    TOTAL_FRAMES = 12; // 1초 사이클
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
    // REST POSE
    // Body            pos (0.00,   0.00)
    // Head            pos (-0.04,  0.46)
    // Head_B          pos (-0.085, 0.17)
    // ShoulderJoint_F pos (-0.29,  0.353)
    // ArmJoint_F      pos (0.00,  -0.25)
    // ShoulderJoint_B pos ( 0.08,  0.353)
    // ArmJoint_B      pos (0.00,  -0.25)
    // LegJoint_F      pos (-0.26, -0.58)
    // LegJoint_B      pos ( 0.05, -0.58)
    //
    // 12프레임 사이클:
    // - Body: 가볍게 위아래로 통...통...
    // - 팔: 앞뒤 스윙 없이 ArmJoint rot으로 팔꿈치가 위아래 간드러지게
    //   F랑 B가 반대 위상으로 번갈아 올라갔다 내려와서 살랑살랑
    // - 다리: 작고 가벼운 스텝
    // - Head_B: 가볍게 찰랑
    // ================================================================

    // Body: 가볍게 통통, 착지 꺼짐 없이
    static readonly float[] BODY_Y = {
        0.00f, 0.02f, 0.04f, 0.05f, 0.04f, 0.02f,
        0.00f, 0.02f, 0.04f, 0.05f, 0.04f, 0.02f };
    static readonly float[] BODY_ROT = {
        -1f, -2f, -3f, -3f, -2f, -1f,
        -1f, -2f, -3f, -3f, -2f, -1f };

    // Head: 가볍게 끄덕
    static readonly float[] HEAD_ROT = {
         1f,  0f, -1f, -1f,  0f,  1f,
         1f,  0f, -1f, -1f,  0f,  1f };

    // Head_B: 살랑
    static readonly float[] HEAD_B_ROT = {
        -2f, -4f, -3f,  0f,  3f,  4f,
         2f,  0f, -3f, -4f, -2f,  0f };
    static readonly float[] HEAD_B_Y = {
         0.002f,  0.001f, -0.001f, -0.003f, -0.001f,  0.001f,
         0.002f,  0.001f, -0.001f, -0.003f, -0.001f,  0.001f };

    // ShoulderJoint_F: 위아래 + 앞뒤 살짝 추가
    static readonly float[] SF_ROT = {
        -6f, -8f, -8f, -6f, -2f,  2f,
         5f,  7f,  8f,  6f,  2f, -2f };
    static readonly float[] SF_Y = {
         0.004f,  0.007f,  0.010f,  0.007f,  0.004f,  0.000f,
        -0.004f, -0.007f, -0.008f, -0.007f, -0.004f,  0.000f };

    // ArmJoint_F: 간드러짐 + 앞뒤 보조
    static readonly float[] AF_ROT = {
        -8f, -14f, -20f, -22f, -18f, -10f,
        -3f,   4f,  10f,  12f,  10f,   4f };

    // ShoulderJoint_B: F 반대 위상
    static readonly float[] SB_ROT = {
         5f,  7f,  8f,  6f,  2f, -2f,
        -6f, -8f, -8f, -6f, -2f,  2f };
    static readonly float[] SB_Y = {
        -0.004f, -0.007f, -0.008f, -0.007f, -0.004f,  0.000f,
         0.004f,  0.007f,  0.010f,  0.007f,  0.004f,  0.000f };

    // ArmJoint_B: F 반대 위상
    static readonly float[] AB_ROT = {
        -3f,   4f,  10f,  12f,  10f,   4f,
        -8f, -14f, -20f, -22f, -18f, -10f };

    // 다리: 작고 가벼운 스텝 (6프레임 교대)
    static readonly float[] LF_X = {
         0.08f,  0.03f, -0.01f, -0.08f, -0.06f, -0.01f,
         0.04f,  0.08f,  0.03f, -0.01f, -0.08f, -0.01f };
    static readonly float[] LF_Y = {
         0.00f,  0.01f,  0.02f,  0.06f,  0.10f,  0.06f,
         0.02f,  0.00f,  0.01f,  0.02f,  0.06f,  0.02f };
    static readonly float[] LF_ROT = {
         9f,   5f,  -1f, -12f,  -8f,  -1f,
         5f,   9f,   5f,  -1f, -12f,  -1f };
    static readonly float[] FF_ROT = {
        -8f,  -4f,   2f,   8f, -10f,  -6f,
        -4f,  -8f,  -4f,   2f,   8f,  -4f };

    static readonly float[] LB_X = {
         0.04f,  0.08f,  0.03f, -0.01f, -0.08f, -0.01f,
         0.08f,  0.03f, -0.01f, -0.08f, -0.06f, -0.01f };
    static readonly float[] LB_Y = {
         0.10f,  0.06f,  0.02f,  0.00f,  0.00f,  0.01f,
         0.02f,  0.06f,  0.10f,  0.06f,  0.02f,  0.00f };
    static readonly float[] LB_ROT = {
        -8f,  -1f,   5f,   9f,   9f,   5f,
        -1f,  -1f,  -8f,  -1f,   5f,  -1f };
    static readonly float[] FB_ROT = {
       -10f,  -6f,  -4f,  -8f,  -8f,  -4f,
        2f,  -4f, -10f,  -4f,  -4f,  -4f };

    // Body Y 보정 — 발 지면 고정
    static readonly float[] LEG_Y_COMP = {
         0.00f, -0.02f, -0.04f, -0.05f, -0.04f, -0.02f,
         0.00f, -0.02f, -0.04f, -0.05f, -0.04f, -0.02f };

    [MenuItem("Tools/NPC1/Daughter/Generate Skip")]
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

        // Legs: Body Y 보정 합산
        float[] lfYFinal = new float[TOTAL_FRAMES];
        float[] lbYFinal = new float[TOTAL_FRAMES];
        for (int i = 0; i < TOTAL_FRAMES; i++)
        {
            lfYFinal[i] = LF_Y[i] + LEG_Y_COMP[i];
            lbYFinal[i] = LB_Y[i] + LEG_Y_COMP[i];
        }

        C(LEG_F, "localPosition.x",    LF_X,     -0.26f);
        C(LEG_F, "localPosition.y",    lfYFinal,  -0.58f);
        L(LEG_F, "localPosition.z",    0f);
        C(LEG_F, "localEulerAngles.z", LF_ROT,    0f);
        C(FOOT_F,"localEulerAngles.z", FF_ROT,    0f);

        C(LEG_B, "localPosition.x",    LB_X,      0.05f);
        C(LEG_B, "localPosition.y",    lbYFinal,  -0.58f);
        L(LEG_B, "localPosition.z",    0f);
        C(LEG_B, "localEulerAngles.z", LB_ROT,    0f);
        C(FOOT_B,"localEulerAngles.z", FB_ROT,    0f);

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
