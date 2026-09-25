using UnityEngine;
using UnityEditor;
using System.IO;

public class Mentor_ThrustGenerator
{
    const string SAVE_PATH  = "Assets/Animations/Mentor";
    const string CLIP_NAME  = "Mentor_Thrust";
    const float  FRAME_RATE = 12f;
    const int    TOTAL_FRAMES = 14;
    const float  DURATION   = TOTAL_FRAMES / FRAME_RATE;

    const string BODY       = "Body";
    const string HEAD       = "Body/Head";
    const string SHOULDER_F = "Body/ShoulderJoint_F";
    const string ARM_F      = "Body/ShoulderJoint_F/Shoulder_F/ArmJoint_F";
    const string SHOULDER_B = "Body/ShoulderJoint_B";
    const string ARM_B      = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B";
    const string WEAPON     = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B/Hand_B/Weapon";
    const string LEG_F      = "Body/LegJoint_F";
    const string FOOT_F     = "Body/LegJoint_F/Foot_F";
    const string LEG_B      = "Body/LegJoint_B";
    const string FOOT_B     = "Body/LegJoint_B/Foot_B";

    // ================================================================
    // REST POSE
    // Body            pos (0.00,  0.00)  rot 0
    // Head            pos (-0.05, 0.62)  rot 0
    // ShoulderJoint_F pos (-0.40, 0.50)  rot 0
    // ArmJoint_F      rot 0
    // ShoulderJoint_B pos ( 0.15, 0.50)  rot 0
    // ArmJoint_B      rot 0
    // Weapon                             rot -90
    // LegJoint_F      pos (-0.35,-0.68)  rot 0
    // LegJoint_B      pos ( 0.10,-0.68)  rot 0
    //
    // 실측 키 자세:
    // [f2 장전]
    //   SF rot=+60,  AF rot=0
    //   SB rot=-45,  AB rot=+80,  Weapon rot=-120 (offset -30 from base -90)
    //
    // [f5 임팩트]
    //   SF rot=-30,  AF rot=+90
    //   SB rot=+45,  AB rot=0,    Weapon rot=-135 (offset -45 from base -90)
    //
    // 타이밍:
    // f0  : 대기 (rest)
    // f1  : 장전 시작
    // f2  : 풀 장전 (실측)
    // f3  : 찌르기 시작
    // f4  : 찌르기 가속
    // f5  : 임팩트 (실측)
    // f6  : 임팩트 유지
    // f7~13: 복귀
    // ================================================================

    // Body: 장전 시 뒤로, 임팩트 시 앞으로
    static readonly float[] BODY_ROT = {
        0f,   4f,   7f,   3f,  -2f,  -8f,  -6f,  -4f,
       -2f,  -1f,   0f,   0f,   0f,   0f };
    static readonly float[] BODY_Y = {
        0f,  -0.01f, -0.02f,  0f,   0.01f, -0.02f, -0.01f,  0f,
        0f,   0f,    0f,     0f,    0f,     0f };

    // Head: body 반대로 보정
    static readonly float[] HEAD_ROT = {
        0f,  -2f,  -4f,  -1f,   1f,   5f,   4f,   2f,
        1f,   0f,   0f,   0f,   0f,   0f };

    // SF: 장전=+60, 임팩트=-30 (실측)
    static readonly float[] SF_ROT = {
        0f,  30f,  60f,  20f,  -8f, -30f, -25f, -18f,
      -10f,  -5f,  -2f,  0f,   0f,   0f };

    // AF: 장전=0, 임팩트=+90 (실측)
    static readonly float[] AF_ROT = {
        0f,   0f,   0f,  30f,  65f,  90f,  80f,  60f,
       40f,  22f,  10f,   4f,   1f,   0f };

    // SB: 장전=-45, 임팩트=+45 (실측)
    static readonly float[] SB_ROT = {
        0f, -22f, -45f, -15f,  18f,  45f,  40f,  28f,
       16f,   8f,   3f,   1f,   0f,   0f };

    // AB: 장전=+80, 임팩트=0 (실측)
    static readonly float[] AB_ROT = {
        0f,  40f,  80f,  45f,  18f,   0f,  -2f,  -1f,
        0f,   0f,   0f,   0f,   0f,   0f };

    // Weapon (base -90): 장전 offset=-30(-120), 임팩트 offset=-45(-135)
    static readonly float[] WP_ROT = {
        0f, -15f, -30f, -35f, -42f, -45f, -40f, -28f,
      -15f,  -6f,  -2f,   0f,   0f,   0f };

    // 다리: 임팩트 시 런지 자세, 발 Y 양수만
    static readonly float[] LF_X = {
        0f,   0f,   0f,  0.04f, 0.08f, 0.12f, 0.10f, 0.07f,
       0.04f, 0.02f, 0f,   0f,   0f,   0f };
    static readonly float[] LF_Y = {
        0f,   0f,   0f,   0f,   0f,   0f,   0f,   0f,
        0f,   0f,   0f,   0f,   0f,   0f };
    static readonly float[] LF_ROT = {
        0f,  -2f,  -4f,  -6f, -10f, -14f, -12f,  -8f,
       -4f,  -2f,   0f,   0f,   0f,   0f };
    static readonly float[] FF_ROT = {
        0f,   1f,   3f,   5f,   8f,  10f,   8f,   6f,
        3f,   1f,   0f,   0f,   0f,   0f };

    static readonly float[] LB_X = {
        0f,   0f,   0f, -0.02f,-0.04f,-0.06f,-0.05f,-0.03f,
      -0.02f,-0.01f, 0f,   0f,   0f,   0f };
    static readonly float[] LB_Y = {
        0f,   0f,   0f,   0f,   0f,   0f,   0f,   0f,
        0f,   0f,   0f,   0f,   0f,   0f };
    static readonly float[] LB_ROT = {
        0f,   2f,   4f,   4f,   6f,   8f,   7f,   5f,
        3f,   1f,   0f,   0f,   0f,   0f };
    static readonly float[] FB_ROT = {
        0f,  -1f,  -2f,  -3f,  -5f,  -6f,  -5f,  -4f,
       -2f,  -1f,   0f,   0f,   0f,   0f };

    // Body rot 보정 (발 지면 고정)
    static readonly float[] LEG_ROT_COMP = {
        0f,  -4f,  -7f,  -3f,   2f,   8f,   6f,   4f,
        2f,   1f,   0f,   0f,   0f,   0f };

    [MenuItem("Tools/NPC1/Mentor/Generate Thrust")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "Mentor");
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
        L(HEAD, "localPosition.x",   -0.05f);
        L(HEAD, "localPosition.y",    0.62f);
        L(HEAD, "localPosition.z",    0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT, 0f);

        // ShoulderJoint_F
        L(SHOULDER_F, "localPosition.x",   -0.40f);
        L(SHOULDER_F, "localPosition.y",    0.50f);
        L(SHOULDER_F, "localPosition.z",   0f);
        C(SHOULDER_F, "localEulerAngles.z", SF_ROT, 0f);
        C(ARM_F,      "localEulerAngles.z", AF_ROT, 0f);

        // ShoulderJoint_B
        L(SHOULDER_B, "localPosition.x",   0.15f);
        L(SHOULDER_B, "localPosition.y",   0.50f);
        L(SHOULDER_B, "localPosition.z",   0f);
        C(SHOULDER_B, "localEulerAngles.z", SB_ROT, 0f);
        C(ARM_B,      "localEulerAngles.z", AB_ROT, 0f);
        C(WEAPON,     "localEulerAngles.z", WP_ROT, -90f);

        // 다리: Body rot 보정
        float[] lfY = new float[TOTAL_FRAMES];
        float[] lbY = new float[TOTAL_FRAMES];
        for (int i = 0; i < TOTAL_FRAMES; i++)
        {
            lfY[i] = LF_Y[i];
            lbY[i] = LB_Y[i];
        }

        C(LEG_F, "localPosition.x",    LF_X,         -0.35f);
        C(LEG_F, "localPosition.y",    lfY,           -0.68f);
        L(LEG_F, "localPosition.z",    0f);
        C(LEG_F, "localEulerAngles.z", LEG_ROT_COMP,  0f);
        C(FOOT_F,"localEulerAngles.z", FF_ROT,        0f);

        C(LEG_B, "localPosition.x",    LB_X,          0.10f);
        C(LEG_B, "localPosition.y",    lbY,           -0.68f);
        L(LEG_B, "localPosition.z",    0f);
        C(LEG_B, "localEulerAngles.z", LEG_ROT_COMP,  0f);
        C(FOOT_B,"localEulerAngles.z", FB_ROT,        0f);

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
        Debug.Log($"[Mentor] {CLIP_NAME} created -> {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}
