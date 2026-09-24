using UnityEngine;
using UnityEditor;
using System.IO;

public class EArcher_AttackAnimationGenerator
{
    const string SAVE_PATH = "Assets/Animations/EArcher 1";
    const string CLIP_NAME = "EArcher1_Attack";
    const float FRAME_RATE = 12f;
    const int TOTAL_FRAMES = 14;
    const float DURATION = TOTAL_FRAMES / FRAME_RATE;

    const string BODY = "Body";
    const string HEAD = "Body/Head";
    const string SHOULDER_F = "Body/ShoulderJoint_F";
    const string SHOULDER_B = "Body/ShoulderJoint_B";
    const string ARM_F = "Body/ShoulderJoint_F/Shoulder_F/ArmJoint_F";
    const string ARM_B = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B";
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
    // Weapon(Bow)     pos (0.06, -0.28)  rot -65
    // LegJoint_F      pos (-0.25,-0.75)  rot 0
    // LegJoint_B      pos ( 0.25,-0.75)  rot 0
    //
    // 실측 조준 자세 (f4~f6 유지):
    //   SF pos X=-0.20 (offset +0.07), rot=+60
    //   AF rot=+30
    //   SB rot=+45
    //   AB rot=+25
    //   Weapon rot=-65 (유지)
    //   발 고정
    // ================================================================

    // ── 타이밍 ───────────────────────────────────────────────────────
    // f0  : 대기 (rest)
    // f1  : 조준 시작 — 팔 올라가기 시작
    // f2  : 조준 중간
    // f3  : 시위 당기기 시작
    // f4  : 풀 조준 (실측 자세)
    // f5  : 조준 유지
    // f6  : 발사 직전 (긴장 최고)
    // f7  : 발사! — SB 팔 튕겨나감, SF 약간 반동
    // f8  : 반동 피크
    // f9~13: 복귀
    // f14 = f0 루프

    // Body: 조준 시 살짝 뒤로 무게, 발사 반동
    static readonly float[] BODY_ROT = {
        0f, -2f, -4f, -5f, -5f, -5f, -6f,  3f,  5f,  3f,  1f,  0f,  0f,  0f };

    // Head: 조준 방향으로 살짝 앞으로
    static readonly float[] HEAD_ROT = {
        0f, -1f, -2f, -3f, -3f, -3f, -4f,  2f,  3f,  2f,  1f,  0f,  0f,  0f };

    // ShoulderJoint_F X: rest=-0.27, 조준=−0.20 (offset +0.07)
    static readonly float[] SF_X = {
        0f, 0.02f, 0.05f, 0.07f, 0.07f, 0.07f, 0.07f, 0.03f, 0.01f, 0f, 0f, 0f, 0f, 0f };

    // ShoulderJoint_F rot: rest=0, 조준=+60
    static readonly float[] SF_ROT = {
        0f, 20f, 42f, 55f, 60f, 60f, 62f, 30f, 15f,  8f,  4f,  1f,  0f,  0f };

    // ArmJoint_F rot: rest=0, 조준=+30
    static readonly float[] AF_ROT = {
        0f, 10f, 22f, 28f, 30f, 30f, 32f, 15f,  8f,  4f,  2f,  0f,  0f,  0f };

    // ShoulderJoint_B rot: rest=0, 조준=+45, 발사 시 튕김(음수)
    static readonly float[] SB_ROT = {
        0f, 15f, 30f, 40f, 45f, 45f, 48f, -20f, -30f, -15f, -6f, -2f,  0f,  0f };

    // ArmJoint_B rot: rest=0, 조준=+25, 발사 시 튕김
    static readonly float[] AB_ROT = {
        0f,  8f, 16f, 22f, 25f, 25f, 26f, -15f, -22f, -10f, -4f, -1f,  0f,  0f };

    // Weapon(Bow): 조준 시 약간 기울기 보정, 발사 후 흔들림 (base -65)
    static readonly float[] WP_ROT = {
        0f,  2f,  5f,  7f,  8f,  8f,  8f,  -5f, -10f,  -6f, -3f, -1f,  0f,  0f };

    // 발 보정: Body rot 반대값으로 발이 땅에 고정된 것처럼
    static readonly float[] LEG_ROT_COMP = {
        0f,  2f,  4f,  5f,  5f,  5f,  6f, -3f, -5f, -3f, -1f,  0f,  0f,  0f };

    [MenuItem("Tools/EArcher 1/Generate Attack")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "EArcher 1");
        }

        AnimationClip clip = new AnimationClip();
        clip.frameRate = FRAME_RATE;
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
        L(HEAD, "localPosition.x", 0.05f);
        L(HEAD, "localPosition.y", 0.45f);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT, 0f);

        // ShoulderJoint_F: X 앞으로 + rot
        C(SHOULDER_F, "localPosition.x", SF_X, -0.27f);
        L(SHOULDER_F, "localPosition.y", 0.32f);
        L(SHOULDER_F, "localPosition.z", 0f);
        C(SHOULDER_F, "localEulerAngles.z", SF_ROT, 0f);
        C(ARM_F, "localEulerAngles.z", AF_ROT, 0f);

        // ShoulderJoint_B: X 고정, rot으로 시위 당기기
        L(SHOULDER_B, "localPosition.x", 0.36f);
        L(SHOULDER_B, "localPosition.y", 0.28f);
        L(SHOULDER_B, "localPosition.z", 0f);
        C(SHOULDER_B, "localEulerAngles.z", SB_ROT, 0f);
        C(ARM_B, "localEulerAngles.z", AB_ROT, 0f);
        C(WEAPON, "localEulerAngles.z", WP_ROT, -65f);

        // 발: Body rot 반대값으로 지면 고정
        L(LEG_F, "localPosition.x", -0.25f);
        L(LEG_F, "localPosition.y", -0.75f);
        L(LEG_F, "localPosition.z", 0f);
        C(LEG_F, "localEulerAngles.z", LEG_ROT_COMP, 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(LEG_B, "localPosition.x", 0.25f);
        L(LEG_B, "localPosition.y", -0.75f);
        L(LEG_B, "localPosition.z", 0f);
        C(LEG_B, "localEulerAngles.z", LEG_ROT_COMP, 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

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
        Debug.Log($"[EArcher 1] {CLIP_NAME} created -> {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}