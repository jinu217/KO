using UnityEngine;
using UnityEditor;
using System.IO;

public class Daughter_WaveGenerator
{
    const string SAVE_PATH = "Assets/Animations/Daughter";
    const string CLIP_NAME = "Daughter_Wave";
    const float FRAME_RATE = 12f;
    const int TOTAL_FRAMES = 28;
    const float DURATION = TOTAL_FRAMES / FRAME_RATE;

    const string BODY = "Body";
    const string HEAD = "Body/Head";
    const string HEAD_B = "Body/Head/Head_B";
    const string SHOULDER_F = "Body/ShoulderJoint_F";
    const string ARM_F = "Body/ShoulderJoint_F/Shoulder_F/ArmJoint_F";
    const string SHOULDER_B = "Body/ShoulderJoint_B";
    const string ARM_B = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B";
    const string LEG_F = "Body/LegJoint_F";
    const string LEG_B = "Body/LegJoint_B";

    // ================================================================
    // REST POSE
    // Body            pos (0.00,   0.00)
    // Head            pos (-0.04,  0.46)
    // Head_B          pos (-0.085, 0.17)
    // ShoulderJoint_F pos (-0.30,  0.32)   rot 0
    // ArmJoint_F                           rot 0
    // ShoulderJoint_B pos ( 0.08,  0.32)   rot 0
    // ArmJoint_B                           rot 0
    // LegJoint_F      pos (-0.26, -0.58)
    // LegJoint_B      pos ( 0.05, -0.58)
    //
    // SF rot 180 = 팔 완전히 번쩍 든 자세
    // 손 흔들기 = SF 180 유지 + AF rot 좌우 진자
    //
    // 16프레임:
    // f0~f4  : SF 0→180 (팔 올라감)
    // f4~f13 : SF 180 유지, AF -20↔+20 흔들기
    // f13~f15: SF 180→0 복귀 시작 (루프 연결)
    // ================================================================

    // Body: 들뜬 느낌 유지
    static readonly float[] BODY_Y = {
        0f,  0.005f, 0.010f, 0.014f, 0.016f, 0.016f, 0.016f, 0.016f,
        0.016f, 0.016f, 0.016f, 0.016f, 0.016f, 0.016f, 0.016f, 0.016f,
        0.016f, 0.016f, 0.016f, 0.016f, 0.016f, 0.016f, 0.016f, 0.016f,
        0.016f, 0.013f, 0.007f, 0.002f };

    static readonly float[] BODY_ROT = {
        0f,  -1f,  -2f,  -3f,  -4f,  -4f,  -4f,  -4f,
        -4f, -4f,  -4f,  -4f,  -4f,  -4f,  -4f,  -4f,
        -4f, -4f,  -4f,  -4f,  -4f,  -4f,  -4f,  -4f,
        -4f, -3f,  -2f,  -1f };

    // Head: 팔보다 느린 주기로 살랑 — 14프레임 주기로 좌우
    // 팔은 4프레임 주기, 머리는 14프레임 주기
    static readonly float[] HEAD_ROT = {
        0f,   1f,   2f,   3f,   4f,   4f,   5f,   5f,
        6f,   6f,   6f,   5f,   4f,   3f,   2f,   2f,
        2f,   3f,   4f,   5f,   6f,   6f,   5f,   4f,
        3f,   3f,   2f,   1f };

    // Head_B: 더 큰 진폭으로 살랑
    static readonly float[] HEAD_B_ROT = {
        0f,  -1f,  -3f,  -6f,  -8f,  -9f,  -11f, -12f,
      -13f, -13f,  -12f, -10f,  -8f,  -6f,  -5f,  -5f,
       -6f,  -8f,  -10f, -12f, -13f,  -12f, -10f,  -8f,
       -6f,  -4f,   -2f,  -1f };
    static readonly float[] HEAD_B_Y = {
        0f, -0.001f,-0.003f,-0.005f,-0.007f,-0.008f,-0.009f,-0.010f,
       -0.010f,-0.009f,-0.008f,-0.006f,-0.005f,-0.004f,-0.004f,-0.004f,
       -0.005f,-0.007f,-0.009f,-0.010f,-0.010f,-0.009f,-0.007f,-0.005f,
       -0.004f,-0.003f,-0.001f, 0f };

    // ShoulderJoint_F: 흔들 폭 ±20 → ±35로 키우고, 복귀 더 천천히
    // f0~f4  : 올라감 (0→180)
    // f0~f5  : SF 올라감 (0→215)
    // f5~f7  : SF 215 잠깐 고정, AF만 뒤로 넘어갔다 돌아옴
    // f7~f24 : SF+AF 같이 흔들기 (흔들기 3사이클)
    // f24~f27: 복귀
    static readonly float[] SF_ROT = {
        0f,   45f,  100f,  155f,  195f,  215f,  215f,  215f,   // f0~f7
      205f,  190f,  172f,  160f,  162f,  172f,  188f,  205f,   // f8~f15  1사이클
      215f,  210f,  196f,  180f,  165f,  160f,  165f,  180f,   // f16~f23 2사이클
      196f,  205f,  175f,  100f };                              // f24~f27 3사이클→복귀

    static readonly float[] SF_Y = {
        0f,  0.004f, 0.008f, 0.012f, 0.014f, 0.014f, 0.014f, 0.014f,
        0.014f, 0.014f, 0.014f, 0.014f, 0.014f, 0.014f, 0.014f, 0.014f,
        0.014f, 0.014f, 0.014f, 0.014f, 0.014f, 0.014f, 0.014f, 0.014f,
        0.014f, 0.012f, 0.007f, 0.002f };

    // ArmJoint_F: SF 같은 방향, 폭 더 크게
    static readonly float[] AF_ROT = {
        0f,    0f,    0f,    0f,    0f,    0f,   10f,   18f,   // f0~f7
       10f,    0f,  -10f,  -18f,  -16f,   -8f,    4f,   14f,  // f8~f15  1사이클
       20f,   16f,    8f,   -2f,  -12f,  -18f,  -12f,   -2f,  // f16~f23 2사이클
        8f,   14f,    4f,    0f };                              // f24~f27 3사이클→복귀

    // ShoulderJoint_B: 반대로 균형
    static readonly float[] SB_ROT = {
        0f,   -4f,   -8f,  -12f,  -14f,  -14f,  -14f,  -14f,
       -14f, -14f,  -14f,  -14f,  -14f,  -14f,  -14f,  -14f,
       -14f, -14f,  -14f,  -14f,  -14f,  -14f,  -14f,  -14f,
       -14f, -10f,   -5f,   -1f };
    static readonly float[] SB_Y = {
        0f, -0.002f,-0.005f,-0.008f,-0.010f,-0.010f,-0.010f,-0.010f,
       -0.010f,-0.010f,-0.010f,-0.010f,-0.010f,-0.010f,-0.010f,-0.010f,
       -0.010f,-0.010f,-0.010f,-0.010f,-0.010f,-0.010f,-0.010f,-0.010f,
       -0.010f,-0.007f,-0.004f,-0.001f };
    static readonly float[] AB_ROT = {
        0f,   -2f,   -4f,   -6f,   -7f,   -7f,   -7f,   -7f,
        -7f,  -7f,   -7f,   -7f,   -7f,   -7f,   -7f,   -7f,
        -7f,  -7f,   -7f,   -7f,   -7f,   -7f,   -7f,   -7f,
        -7f,  -5f,   -2f,    0f };

    // 발: Body Y 보정
    static readonly float[] LEG_Y_COMP = {
        0f, -0.005f,-0.010f,-0.014f,-0.016f,-0.016f,-0.016f,-0.016f,
       -0.016f,-0.016f,-0.016f,-0.016f,-0.016f,-0.016f,-0.016f,-0.016f,
       -0.016f,-0.016f,-0.016f,-0.016f,-0.016f,-0.016f,-0.016f,-0.016f,
       -0.016f,-0.013f,-0.007f,-0.002f };

    [MenuItem("Tools/Daughter/Generate Wave")]
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
        L(HEAD, "localPosition.x", -0.04f);
        L(HEAD, "localPosition.y", 0.46f);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT, 0f);

        // Head_B
        L(HEAD_B, "localPosition.x", -0.085f);
        C(HEAD_B, "localPosition.y", HEAD_B_Y, 0.17f);
        L(HEAD_B, "localPosition.z", 0f);
        C(HEAD_B, "localEulerAngles.z", HEAD_B_ROT, 0f);

        // ShoulderJoint_F
        L(SHOULDER_F, "localPosition.x", -0.30f);
        C(SHOULDER_F, "localPosition.y", SF_Y, 0.32f);
        L(SHOULDER_F, "localPosition.z", 0f);
        C(SHOULDER_F, "localEulerAngles.z", SF_ROT, 0f);
        C(ARM_F, "localEulerAngles.z", AF_ROT, 0f);

        // ShoulderJoint_B
        L(SHOULDER_B, "localPosition.x", 0.08f);
        C(SHOULDER_B, "localPosition.y", SB_Y, 0.32f);
        L(SHOULDER_B, "localPosition.z", 0f);
        C(SHOULDER_B, "localEulerAngles.z", SB_ROT, 0f);
        C(ARM_B, "localEulerAngles.z", AB_ROT, 0f);

        // 발: Body Y 보정
        float[] lfY = new float[TOTAL_FRAMES];
        float[] lbY = new float[TOTAL_FRAMES];
        for (int i = 0; i < TOTAL_FRAMES; i++)
        {
            lfY[i] = LEG_Y_COMP[i];
            lbY[i] = LEG_Y_COMP[i];
        }
        L(LEG_F, "localPosition.x", -0.26f);
        C(LEG_F, "localPosition.y", lfY, -0.58f);
        L(LEG_F, "localPosition.z", 0f);

        L(LEG_B, "localPosition.x", 0.05f);
        C(LEG_B, "localPosition.y", lbY, -0.58f);
        L(LEG_B, "localPosition.z", 0f);

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