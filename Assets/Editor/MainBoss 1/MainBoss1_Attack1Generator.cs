using UnityEngine;
using UnityEditor;
using System.IO;

public class MainBoss1_Attack1Generator
{
    const string SAVE_PATH  = "Assets/Animations/MainBoss 1";
    const string CLIP_NAME  = "MainBoss1_Attack1";
    const float  FRAME_RATE = 12f;
    const int    TOTAL_FRAMES = 14;
    const float  DURATION   = TOTAL_FRAMES / FRAME_RATE;

    // ── 경로 ─────────────────────────────────────────────────────────
    const string BODY        = "Body";
    const string HEAD        = "Body/Head";
    const string BOTTOM      = "Body/Bottom";
    const string SHOULDER_F  = "Body/Shoulder_F";
    const string FOREARM_F   = "Body/Shoulder_F/Forearm_F";
    const string SHOULDER_B  = "Body/Shoulder_B";
    const string FOREARM_B   = "Body/Shoulder_B/Forearm_B";
    const string HAND_B      = "Body/Shoulder_B/Forearm_B/Hand_B";
    const string WEAPON      = "Body/Shoulder_B/Forearm_B/Hand_B/Weapon";
    const string THIGH_F     = "Body/Bottom/Thigh_F";
    const string CALF_F      = "Body/Bottom/Thigh_F/Calf_F";
    const string FOOT_F      = "Body/Bottom/Thigh_F/Calf_F/Foot_F";
    const string THIGH_B     = "Body/Bottom/Thigh_B";
    const string CALF_B      = "Body/Bottom/Thigh_B/Calf_B";
    const string FOOT_B      = "Body/Bottom/Thigh_B/Calf_B/Foot_B";

    // ================================================================
    // 검 잡은 자세 (REST)
    // Body            pos (0,     0)      rot 0
    // Head            pos (0,     0.70)   rot 0
    // Bottom          pos (-0.20,-0.30)   rot 0
    // Shoulder_F      pos (-0.38, 0.34)   rot +20
    // Forearm_F       pos (-0.29,-0.57)   rot +15
    // Hand_F          pos ( 0.07,-0.585)  rot 0
    // Shoulder_B      pos ( 0.26, 0.55)   rot 0
    // Forearm_B       pos ( 0.15,-0.65)   rot 0
    // Hand_B          pos ( 0.05,-0.54)   rot 0
    // Weapon          pos ( 0.13,-0.28)   rot -90
    // Thigh_F         pos (-0.35,-0.15)   rot 0
    // Calf_F          pos (-0.17,-0.88)   rot 0
    // Foot_F          pos (-0.53,-0.845)  rot 0
    // Thigh_B         pos ( 0.07,-0.215)  rot 0
    // Calf_B          pos ( 0.57,-0.75)   rot 0
    // Foot_B          pos (-0.14,-0.91)   rot 0
    //
    // 타이밍:
    // f0        : 대기
    // f1~f2     : 1타 예비 — 검 위로 크게 들기 (SB 위로, Weapon 위 향함)
    // f3~f4     : 1타 가로베기 임팩트 — 검이 수평으로 쓸림, 몸 앞으로
    // f5        : 전환
    // f6~f7     : 2타 예비 — 검 옆으로 들기 (세로베기 준비)
    // f8~f9     : 2타 세로베기 임팩트 — 검이 아래로 강하게
    // f10~f13   : 복귀
    // ================================================================

    // Body: 1타=앞으로 숙임, 2타=더 앞으로
    static readonly float[] BODY_ROT = {
        0f,   5f,   8f,  -10f,  -14f,   -6f,   8f,   12f,
      -16f,  -12f,  -6f,  -3f,  -1f,    0f };
    static readonly float[] BODY_Y = {
        0f,  -0.02f,-0.04f, 0.02f,-0.04f,-0.01f,-0.03f,-0.05f,
       0.02f,-0.02f,-0.01f,  0f,   0f,   0f };

    // Head: body 반대 보정
    static readonly float[] HEAD_ROT = {
        0f,  -3f,  -4f,   6f,   8f,   3f,  -4f,  -6f,
       9f,   7f,   4f,   2f,   1f,   0f };

    // ── 1타: 가로베기 ─────────────────────────────────────────────
    // Shoulder_B: 예비=위로 크게 올림, 임팩트=앞으로 강하게 스윙
    static readonly float[] SB_ROT = {
        0f,  -20f,  -40f,  30f,   55f,   25f,  -15f,  -35f,
       40f,   60f,   35f,  15f,   5f,    0f };

    // Forearm_B: 예비=접힘, 임팩트=펴짐
    static readonly float[] FB_ROT = {
        0f,  -15f,  -30f,  10f,   20f,   10f,  -20f,  -35f,
       15f,   25f,   15f,   6f,   2f,    0f };

    // Hand_B: 검 방향 보정
    static readonly float[] HB_ROT = {
        0f,   10f,   20f, -10f,  -20f,  -10f,   15f,   25f,
      -15f,  -25f,  -12f,  -5f,  -2f,   0f };

    // Weapon: 1타=가로(수평에 가깝게), 2타=세로(아래로)
    // base -90 기준 offset
    static readonly float[] WP_ROT = {
        0f,  -30f,  -50f,  60f,   80f,   40f,  -20f,  -60f,
       30f,   50f,   25f,  10f,   3f,    0f };

    // Shoulder_F: 1타 때 반대로 균형, 2타 때 위로
    static readonly float[] SF_ROT = {
        0f,   8f,   14f, -10f,  -18f,  -8f,   12f,   20f,
      -12f,  -8f,   -4f, -2f,  -1f,   0f };
    static readonly float[] SF_ROT_BASE = new float[14]; // base=20

    static readonly float[] FF_ROT = {
        0f,   5f,   8f,  -6f,  -10f,  -4f,   6f,   10f,
       -8f,  -6f,   -3f, -1f,   0f,   0f };
    static readonly float[] FF_ROT_BASE = new float[14]; // base=15

    // Body rot 보정 (발 지면 고정)
    static readonly float[] LEG_ROT_COMP = {
        0f,  -5f,  -8f,  10f,   14f,   6f,  -8f,  -12f,
       16f,  12f,   6f,   3f,   1f,   0f };

    // 다리: 공격 시 앞발 앞으로 런지
    static readonly float[] THIGH_F_ROT = {
        0f,  -3f,  -5f,  -8f,  -12f,  -6f,  -5f,  -8f,
      -14f,  -10f,  -5f,  -2f,  -1f,   0f };
    static readonly float[] CALF_F_ROT = {
        0f,   2f,   3f,   5f,    8f,   4f,   3f,   5f,
        9f,    7f,   3f,   1f,   0f,   0f };
    static readonly float[] THIGH_B_ROT = {
        0f,   3f,   5f,   6f,    8f,   4f,   4f,   6f,
       10f,    7f,   4f,   2f,   1f,   0f };
    static readonly float[] CALF_B_ROT = {
        0f,  -2f,  -3f,  -4f,   -5f,  -3f,  -2f,  -4f,
       -6f,   -4f,  -2f,  -1f,   0f,   0f };

    [MenuItem("Tools/MainBoss 1/Generate Attack1 (Slash + Vertical)")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "MainBoss 1");
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
        L(HEAD, "localPosition.x",    0f);
        L(HEAD, "localPosition.y",    0.70f);
        L(HEAD, "localPosition.z",    0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT, 0f);

        // Bottom 고정
        L(BOTTOM, "localPosition.x",  -0.20f);
        L(BOTTOM, "localPosition.y",  -0.30f);
        L(BOTTOM, "localPosition.z",   0f);

        // Shoulder_F (base rot=20)
        L(SHOULDER_F, "localPosition.x",   -0.38f);
        L(SHOULDER_F, "localPosition.y",    0.34f);
        L(SHOULDER_F, "localPosition.z",   0f);
        C(SHOULDER_F, "localEulerAngles.z", SF_ROT, 20f);

        // Forearm_F (base rot=15)
        L(FOREARM_F, "localPosition.x",   -0.29f);
        L(FOREARM_F, "localPosition.y",   -0.57f);
        L(FOREARM_F, "localPosition.z",   0f);
        C(FOREARM_F, "localEulerAngles.z", FF_ROT, 15f);

        // Shoulder_B
        L(SHOULDER_B, "localPosition.x",   0.26f);
        L(SHOULDER_B, "localPosition.y",   0.55f);
        L(SHOULDER_B, "localPosition.z",   0f);
        C(SHOULDER_B, "localEulerAngles.z", SB_ROT, 0f);

        // Forearm_B
        L(FOREARM_B, "localPosition.x",   0.15f);
        L(FOREARM_B, "localPosition.y",  -0.65f);
        L(FOREARM_B, "localPosition.z",   0f);
        C(FOREARM_B, "localEulerAngles.z", FB_ROT, 0f);

        // Hand_B
        L(HAND_B, "localPosition.x",   0.05f);
        L(HAND_B, "localPosition.y",  -0.54f);
        L(HAND_B, "localPosition.z",   0f);
        C(HAND_B, "localEulerAngles.z", HB_ROT, 0f);

        // Weapon (base -90)
        C(WEAPON, "localEulerAngles.z", WP_ROT, -90f);

        // 다리: Body rot 보정
        C(THIGH_F, "localEulerAngles.z", LEG_ROT_COMP, 0f);
        C(CALF_F,  "localEulerAngles.z", CALF_F_ROT,   0f);
        C(THIGH_B, "localEulerAngles.z", LEG_ROT_COMP, 0f);
        C(CALF_B,  "localEulerAngles.z", CALF_B_ROT,   0f);

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
        Debug.Log($"[MainBoss 1] {CLIP_NAME} created -> {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}
