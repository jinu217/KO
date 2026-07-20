using UnityEngine;
using UnityEditor;
using System.IO;

public class SpeedMob_AttackGenerator
{
    const string SAVE_PATH = "Assets/Animations/SpeedMob 1";
    const string CLIP_NAME = "SpeedMob1_Attack";
    const float FRAME_RATE = 12f;
    const int TOTAL_FRAMES = 14;
    const float DURATION = TOTAL_FRAMES / FRAME_RATE;

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
    // Body            pos (0.00,   0.00)   rot 0
    // Head            pos (0.00,   0.324)  rot 0
    // ShoulderJoint_F pos (-0.21,  0.22)   rot 0
    // ArmJoint_F      rot 0
    // ShoulderJoint_B pos ( 0.27,  0.22)   rot 0
    // ArmJoint_B      rot 0
    // Weapon          pos ( 0.06, -0.20)   rot -65
    // LegJoint_F      pos (-0.20, -0.475)  rot 0
    // LegJoint_B      pos ( 0.15, -0.475)  rot 0
    //
    // _F = 오른손 (빈 손)
    // _B = 왼손  (단검 든 손)
    //
    // 2타 흐름:
    // f0        : 대기
    // f1~f2     : 1타 예비 — 단검 팔 뒤로, 몸 뒤로
    // f3~f4     : 1타 찌르기 — SB 앞으로 스윙, AB 펴짐
    // f5        : 전환 — 잠깐 수축
    // f6~f7     : 2타 예비 — 단검 위로 들기
    // f8~f9     : 2타 베기 — 단검 아래로 빠르게
    // f10~13    : 복귀
    // ================================================================

    // Body: 2타 예비 더 크게 뒤로
    static readonly float[] BODY_ROT = {
        0f,  5f,  8f, -10f, -12f,  -2f, 10f,  18f, -20f, -16f, -8f, -4f, -1f,  0f };

    // Head: body 반대 보정
    static readonly float[] HEAD_ROT = {
        0f, -3f, -4f,   6f,   7f,   1f, -5f,  -9f,  11f,   9f,  5f,  2f,  1f,  0f };

    // ── 1타: 찌르기 ──────────────────────────────────────────────────
    static readonly float[] SF_ROT = {
        0f,  8f, 12f,  -8f, -12f,  -2f, -18f, -28f,  16f,  12f,  6f,  2f,  0f,  0f };
    static readonly float[] SF_Y = {
        0f,  0f,  0f,   0f,   0f,   0f,   0f,   0f,   0f,   0f,  0f,  0f,  0f,  0f };
    static readonly float[] AF_ROT = {
        0f,  6f, 10f,  -6f, -10f,  -2f, -15f, -22f,  12f,   9f,  4f,  1f,  0f,  0f };

    // SB rot: 2타 예비=+90(팔 확 올라감), 2타 임팩트=-60(크게 내려침)
    static readonly float[] SB_ROT = {
        0f, -12f, -25f,  40f,  45f,  10f,  55f,  90f, -40f, -60f, -30f,-10f, -2f,  0f };

    // AB rot: 자연스러운 방향으로, 꺾임 없게 -30 이하로 안 내려감
    static readonly float[] AB_ROT = {
        0f, -15f, -30f,   5f,   0f,  -5f, -15f, -25f,  30f,  40f,  18f,  5f,  0f,  0f };

    // Weapon (base -65): 2타 예비=위, 베기=더 크게 아래
    static readonly float[] WP_ROT = {
        0f,  -8f, -15f,  25f,  30f,   5f, -10f, -25f, -50f, -65f, -32f,-12f, -2f,  0f };

    // 발: 2타 베기 시 더 크게
    static readonly float[] LF_X = {
        0f,  0f,  0f, 0.06f, 0.08f, 0.04f, 0.06f, 0.05f, 0.14f, 0.16f, 0.09f, 0.04f, 0.01f, 0f };
    static readonly float[] LB_X = {
        0f,  0f,  0f,-0.03f,-0.04f,-0.02f,-0.03f,-0.02f,-0.07f,-0.08f,-0.05f,-0.02f,  0f,   0f };

    // Body rot 보정 (발 지면 고정)
    static readonly float[] LEG_ROT_COMP = {
        0f, -5f, -8f,  10f,  12f,   2f, -10f, -18f,  20f,  16f,  8f,  4f,  1f,  0f };

    [MenuItem("Tools/SpeedMob 1/Generate Attack")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "SpeedMob 1");
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
        L(BODY, "localPosition.y", 0f);
        L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", BODY_ROT, 0f);

        // Head
        L(HEAD, "localPosition.x", 0f);
        L(HEAD, "localPosition.y", 0.324f);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT, 0f);

        // ShoulderJoint_F
        L(SHOULDER_F, "localPosition.x", -0.21f);
        L(SHOULDER_F, "localPosition.y", 0.22f);
        L(SHOULDER_F, "localPosition.z", 0f);
        C(SHOULDER_F, "localEulerAngles.z", SF_ROT, 0f);
        C(ARM_F, "localEulerAngles.z", AF_ROT, 0f);

        // ShoulderJoint_B
        L(SHOULDER_B, "localPosition.x", 0.27f);
        L(SHOULDER_B, "localPosition.y", 0.22f);
        L(SHOULDER_B, "localPosition.z", 0f);
        C(SHOULDER_B, "localEulerAngles.z", SB_ROT, 0f);
        C(ARM_B, "localEulerAngles.z", AB_ROT, 0f);
        C(WEAPON, "localEulerAngles.z", WP_ROT, -65f);

        // 발: X 이동 + Body rot 보정
        C(LEG_F, "localPosition.x", LF_X, -0.20f);
        L(LEG_F, "localPosition.y", -0.475f);
        L(LEG_F, "localPosition.z", 0f);
        C(LEG_F, "localEulerAngles.z", LEG_ROT_COMP, 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);

        C(LEG_B, "localPosition.x", LB_X, 0.15f);
        L(LEG_B, "localPosition.y", -0.475f);
        L(LEG_B, "localPosition.z", 0f);
        C(LEG_B, "localEulerAngles.z", LEG_ROT_COMP, 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

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
        Debug.Log($"[SpeedMob 1] {CLIP_NAME} created -> {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}