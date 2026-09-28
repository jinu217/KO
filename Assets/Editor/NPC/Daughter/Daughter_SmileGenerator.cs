using UnityEngine;
using UnityEditor;
using System.IO;

public class Daughter_SmileGenerator
{
    const string SAVE_PATH    = "Assets/Animations/Daughter";
    const string CLIP_NAME    = "Daughter_Smile";
    const float  FRAME_RATE   = 12f;
    const int    TOTAL_FRAMES = 16;
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
    // ArmJoint_F      pos (0.00,  -0.25)   rot 0 → 140 = 손이 입가
    // ShoulderJoint_B pos ( 0.08,  0.353)
    // ArmJoint_B      pos (0.00,  -0.25)
    // LegJoint_F      pos (-0.26, -0.58)
    // LegJoint_B      pos ( 0.05, -0.58)
    //
    // 웃는 모션 구조 (16프레임):
    //  f0~f3  : 손이 입가로 올라옴 (AF 0→140)
    //  f4~f11 : 입가에 손 올린 채 살랑살랑 (숨쉬기+머리카락)
    //  f12~f15: 살짝 내려갔다 다시 올라오며 루프
    // ================================================================

    // Body: 들뜬 느낌 — 살짝 위로 뜨고 앞으로 기울기
    static readonly float[] BODY_Y = {
        0f,  0.005f, 0.010f, 0.014f, 0.016f, 0.018f, 0.018f, 0.017f,
        0.016f, 0.018f, 0.018f, 0.017f, 0.016f, 0.014f, 0.008f, 0.003f };

    static readonly float[] BODY_ROT = {
        0f,  -0.5f, -1.0f, -1.5f, -1.8f, -2.0f, -2.0f, -1.9f,
       -1.8f, -2.0f, -2.0f, -1.9f, -1.8f, -1.5f, -0.8f, -0.3f };

    // Head: 웃으면서 살짝 기울어짐
    static readonly float[] HEAD_ROT = {
        0f,   0.5f,  1.0f,  1.5f,  2.0f,  2.2f,  2.0f,  1.8f,
        2.0f, 2.2f,  2.0f,  1.8f,  2.0f,  1.8f,  1.2f,  0.5f };

    // Head_B: 찰랑 — 손 올라갈 때 뒤로 처졌다가 살랑거림
    static readonly float[] HEAD_B_ROT = {
         0f,   1.0f,  2.5f,  4.0f,  5.0f,  5.5f,  5.0f,  4.0f,
         5.0f, 5.5f,  5.0f,  4.0f,  3.5f,  2.5f,  1.5f,  0.5f };

    static readonly float[] HEAD_B_Y = {
         0f,  -0.002f,-0.004f,-0.006f,-0.007f,-0.008f,-0.007f,-0.006f,
        -0.007f,-0.008f,-0.007f,-0.006f,-0.005f,-0.004f,-0.002f,-0.001f };

    // ShoulderJoint_F: 어깨는 살짝만 올라옴
    static readonly float[] SF_ROT = {
         0f,   2f,    5f,    8f,   10f,   11f,   10f,    9f,
        10f,  11f,   10f,    9f,   10f,    8f,    5f,    2f };

    static readonly float[] SF_Y = {
         0f,  0.003f, 0.006f, 0.009f, 0.011f, 0.012f, 0.011f, 0.010f,
         0.011f, 0.012f, 0.011f, 0.010f, 0.010f, 0.008f, 0.004f, 0.001f };

    // ArmJoint_F: f9 기준 125, 살랑 유지
    static readonly float[] AF_ROT = {
         0f,   30f,   70f,  110f,  124f,  126f,  125f,  124f,
        125f,  127f,  125f,  123f,  124f,  110f,   65f,   28f };

    // ShoulderJoint_B: F 반대로 약간, 균형감
    static readonly float[] SB_ROT = {
         0f,  -2f,   -4f,   -6f,   -7f,   -8f,   -7f,   -6f,
        -7f,  -8f,   -7f,   -6f,   -6f,   -5f,   -3f,   -1f };

    static readonly float[] SB_Y = {
         0f,  0.002f, 0.004f, 0.006f, 0.007f, 0.008f, 0.007f, 0.006f,
         0.007f, 0.008f, 0.007f, 0.006f, 0.006f, 0.005f, 0.003f, 0.001f };

    // ArmJoint_B: 자연스럽게 살짝 뒤로
    static readonly float[] AB_ROT = {
         0f,  -2f,   -4f,   -6f,   -7f,   -8f,   -7f,   -6f,
        -7f,  -8f,   -7f,   -6f,   -6f,   -5f,   -3f,   -1f };

    // 발: Body Y 보정 (지면 고정)
    static readonly float[] LEG_Y_COMP = {
         0f,  -0.005f,-0.010f,-0.014f,-0.016f,-0.018f,-0.018f,-0.017f,
        -0.016f,-0.018f,-0.018f,-0.017f,-0.016f,-0.014f,-0.008f,-0.003f };

    [MenuItem("Tools/NPC1/Daughter/Generate Smile")]
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

        // 발: Body Y 보정
        L(LEG_F, "localPosition.x",   -0.26f);
        C(LEG_F, "localPosition.y",    LEG_Y_COMP, -0.58f);
        L(LEG_F, "localPosition.z",    0f);
        L(LEG_F, "localEulerAngles.z", 0f);
        L(FOOT_F,"localEulerAngles.z", 0f);

        L(LEG_B, "localPosition.x",    0.05f);
        C(LEG_B, "localPosition.y",    LEG_Y_COMP, -0.58f);
        L(LEG_B, "localPosition.z",    0f);
        L(LEG_B, "localEulerAngles.z", 0f);
        L(FOOT_B,"localEulerAngles.z", 0f);

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
