using UnityEngine;
using UnityEditor;
using System.IO;

public class EBasicMob_RunAnimationGenerator
{
    const string SAVE_PATH = "Assets/Animations/EBasicMob";
    const float FRAME_RATE = 12f;
    const int TOTAL_FRAMES = 8;
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
    // Body            localPos ( 0.00,  0.00, 0)  rot 0
    // Head            localPos ( 0.05,  0.45, 0)  rot 0
    // ShoulderJoint_F localPos (-0.27,  0.32, 0)  rot 0
    // ShoulderJoint_B localPos ( 0.36,  0.28, 0)  rot 0
    // ArmJoint_F      localPos (-0.25, -0.30, 0)  rot 0
    // ArmJoint_B      localPos ( 0.06, -0.24, 0)  rot 0
    // Weapon          localPos ( 0.06, -0.25, 0)  rot -65
    // LegJoint_F      localPos (-0.25, -0.75, 0)  rot 0
    // LegJoint_B      localPos ( 0.25, -0.75, 0)  rot 0
    // Foot_F/B        localPos ( 0.00,  0.00, 0)  rot 0
    //
    // _F = 카메라 가까운 쪽 = 몹 기준 오른쪽 (빈 손)
    // _B = 카메라 먼 쪽    = 몹 기준 왼쪽  (검 든 손)
    //
    // ArmJoint_B pivot (0.5, 0.7) → 피봇이 위에 있음
    //   rot.z 양수 = 시계방향 = 손이 앞(오른쪽)으로
    //   rot.z 음수 = 반시계   = 손이 뒤(왼쪽)로/위로
    // Weapon base rot -65 → 팔을 위로 접으면 검이 하늘을 향함
    // ================================================================

    // ── VERSION A: light shoulder swing +-10 ────────────────────────

    static readonly float[] A_BODY_Y = { -0.04f, 0.01f, 0.05f, 0.01f, -0.04f, 0.01f, 0.05f, 0.01f };
    static readonly float[] A_HEAD_Y = { -0.01f, 0.00f, 0.01f, 0.00f, -0.01f, 0.00f, 0.01f, 0.00f };
    static readonly float[] A_SF_ROT = { -9f, -5f, 2f, 8f, 10f, 6f, -2f, -7f };
    static readonly float[] A_SB_ROT = { 9f, 5f, -2f, -8f, -10f, -6f, 2f, 7f };
    static readonly float[] A_SF_Y = { 0.01f, 0.00f, -0.01f, -0.01f, 0.00f, 0.01f, 0.01f, 0.00f };
    static readonly float[] A_SB_Y = { -0.01f, 0.00f, 0.01f, 0.01f, 0.00f, -0.01f, -0.01f, 0.00f };
    static readonly float[] A_AF_ROT = { -8f, -4f, 2f, 7f, 9f, 5f, -2f, -6f };
    static readonly float[] A_AB_ROT = { 8f, 4f, -2f, -7f, -9f, -5f, 2f, 6f };
    static readonly float[] A_WEAPON_ROT = { 0f, -1f, -2f, -1f, 0f, 1f, 2f, 1f };

    // ── Shared legs (gunmayham) ──────────────────────────────────────
    static readonly float[] LEG_F_X = { 0.18f, 0.08f, -0.05f, -0.20f, -0.18f, -0.05f, 0.10f, 0.18f };
    static readonly float[] LEG_F_Y = { -0.02f, 0.00f, 0.00f, 0.14f, 0.28f, 0.18f, 0.07f, -0.02f };
    static readonly float[] LEG_F_ROT = { 20f, 10f, -5f, -25f, -15f, -5f, 13f, 22f };
    static readonly float[] FOOT_F_ROT = { -16f, -8f, 5f, 18f, -20f, -14f, -10f, -18f };
    static readonly float[] LEG_B_X = { -0.18f, -0.05f, 0.10f, 0.18f, 0.18f, 0.08f, -0.05f, -0.20f };
    static readonly float[] LEG_B_Y = { 0.28f, 0.18f, 0.07f, -0.02f, -0.02f, 0.00f, 0.00f, 0.14f };
    static readonly float[] LEG_B_ROT = { -15f, -5f, 13f, 22f, 20f, 10f, -5f, -25f };
    static readonly float[] FOOT_B_ROT = { -20f, -14f, -10f, -18f, -16f, -8f, 5f, 18f };

    // ── VERSION B: urgent lean, longsword raised ─────────────────────
    // 목표:
    //   1. Body를 앞으로 기울여서 긴박감 (rot.z 음수 = 오른쪽/앞으로 기울기)
    //   2. Head를 반대로 약간 보정해서 고개는 덜 숙임
    //   3. 왼손(_B) ArmJoint를 직각으로 올려서 장검이 하늘을 보게 고정
    //      → ShoulderJoint_B를 뒤로 당기고 ArmJoint_B를 접어올림
    //      → Weapon rot은 base -65 + arm 각도 합산으로 수직에 가깝게
    //   4. 오른손(_F)은 달리기용으로 살짝 앞뒤 스윙
    //   5. 다리는 B버전 (더 강한 건메이햄)

    // Body: 앞으로 기울기 -12도 기준 + 바운스
    // rot.z 음수 = 반시계 = 2D에서 오른쪽(진행방향)으로 기움
    static readonly float[] B_BODY_ROT = { -14f, -12f, -10f, -12f, -14f, -12f, -10f, -12f };
    static readonly float[] B_BODY_Y = { -0.05f, 0.01f, 0.06f, 0.01f, -0.05f, 0.01f, 0.06f, 0.01f };

    // Head: body 기울기를 +8로 보정해서 고개가 너무 숙지 않게
    static readonly float[] B_HEAD_ROT = { 10f, 8f, 6f, 8f, 10f, 8f, 6f, 8f };
    static readonly float[] B_HEAD_Y = { -0.01f, 0.00f, 0.01f, 0.00f, -0.01f, 0.00f, 0.01f, 0.00f };

    // ShoulderJoint_F (오른쪽 빈 손): 달리기 스윙용, 작게
    static readonly float[] B_SF_ROT = { -8f, -4f, 2f, 6f, 8f, 4f, -2f, -6f };
    static readonly float[] B_SF_Y = { 0.01f, 0.00f, -0.01f, 0.00f, 0.01f, 0.00f, -0.01f, 0.00f };

    // ShoulderJoint_B (왼쪽 검 든 손): 뒤로 당겨서 팔 올릴 공간 확보, 고정
    // 양수 = 시계 = 어깨가 뒤로 → 팔이 더 수직으로 서기 좋음
    static readonly float[] B_SB_ROT = { 15f, 14f, 13f, 14f, 15f, 14f, 13f, 14f };
    static readonly float[] B_SB_Y = { 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f, 0.00f };

    // ArmJoint_F (오른쪽 빈 손): 달리기 팔 스윙, 작게
    static readonly float[] B_AF_ROT = { -15f, -8f, 5f, 12f, 15f, 8f, -5f, -12f };

    // ArmJoint_B (왼쪽 검 든 손): 직각으로 접어 올림 = 팔꿈치가 위로
    // ArmJoint pivot (0.5, 0.7) 이므로 팔이 위로 접히는 방향 = 양수
    // +90도면 forearm이 위를 향함 → Weapon -65 + arm +90 = +25 → 검이 거의 수직
    // 달리기 진동 +-3
    static readonly float[] B_AB_ROT = { 93f, 91f, 88f, 90f, 93f, 91f, 88f, 90f };

    // Weapon: 팔이 +90 올라가면 -65 + 90 = +25도 → 칼날이 거의 수직
    // 달리는 진동으로 아주 살짝 흔들림
    static readonly float[] B_WEAPON_ROT = { 2f, 1f, -1f, 0f, 2f, 1f, -1f, 0f };

    // Legs B: 더 강한 스트라이드
    static readonly float[] B_LEG_F_X = { 0.22f, 0.10f, -0.06f, -0.24f, -0.22f, -0.06f, 0.13f, 0.22f };
    static readonly float[] B_LEG_F_Y = { -0.03f, 0.00f, 0.00f, 0.18f, 0.35f, 0.22f, 0.09f, -0.03f };
    static readonly float[] B_LEG_F_ROT = { 25f, 12f, -7f, -30f, -20f, -7f, 16f, 27f };
    static readonly float[] B_FOOT_F_ROT = { -20f, -10f, 7f, 22f, -25f, -18f, -13f, -22f };
    static readonly float[] B_LEG_B_X = { -0.22f, -0.06f, 0.13f, 0.22f, 0.22f, 0.10f, -0.06f, -0.24f };
    static readonly float[] B_LEG_B_Y = { 0.35f, 0.22f, 0.09f, -0.03f, -0.03f, 0.00f, 0.00f, 0.18f };
    static readonly float[] B_LEG_B_ROT = { -20f, -7f, 16f, 27f, 25f, 12f, -7f, -30f };
    static readonly float[] B_FOOT_B_ROT = { -25f, -18f, -13f, -22f, -20f, -10f, 7f, 22f };

    // ================================================================

    [MenuItem("Tools/1/EBasicMob/Generate Run A (Light Shoulder Swing)")]
    public static void GenerateA()
    {
        AnimationClip clip = MakeClip();

        LockAt(clip, BODY, "localPosition.x", 0f);
        AddCurve(clip, BODY, "localPosition.y", A_BODY_Y, 0f);
        LockAt(clip, BODY, "localPosition.z", 0f);

        LockAt(clip, HEAD, "localPosition.x", 0.05f);
        AddCurve(clip, HEAD, "localPosition.y", A_HEAD_Y, 0.45f);
        LockAt(clip, HEAD, "localPosition.z", 0f);

        LockAt(clip, SHOULDER_F, "localPosition.x", -0.27f);
        AddCurve(clip, SHOULDER_F, "localPosition.y", A_SF_Y, 0.32f);
        LockAt(clip, SHOULDER_F, "localPosition.z", 0f);
        AddCurve(clip, SHOULDER_F, "localEulerAngles.z", A_SF_ROT, 0f);

        LockAt(clip, SHOULDER_B, "localPosition.x", 0.36f);
        AddCurve(clip, SHOULDER_B, "localPosition.y", A_SB_Y, 0.28f);
        LockAt(clip, SHOULDER_B, "localPosition.z", 0f);
        AddCurve(clip, SHOULDER_B, "localEulerAngles.z", A_SB_ROT, 0f);

        AddCurve(clip, ARM_F, "localEulerAngles.z", A_AF_ROT, 0f);
        AddCurve(clip, ARM_B, "localEulerAngles.z", A_AB_ROT, 0f);
        AddCurve(clip, WEAPON, "localEulerAngles.z", A_WEAPON_ROT, -65f);

        ApplyLegs(clip, LEG_F_X, LEG_F_Y, LEG_F_ROT, FOOT_F_ROT,
                        LEG_B_X, LEG_B_Y, LEG_B_ROT, FOOT_B_ROT);
        SaveClip(clip, "EBasicMob_Run_A");
    }

    [MenuItem("Tools/1/EBasicMob/Generate Run B (Sword Up Urgency)")]
    public static void GenerateB()
    {
        AnimationClip clip = MakeClip();

        // Body: 앞으로 기울기 + 바운스
        LockAt(clip, BODY, "localPosition.x", 0f);
        AddCurve(clip, BODY, "localPosition.y", B_BODY_Y, 0f);
        LockAt(clip, BODY, "localPosition.z", 0f);
        AddCurve(clip, BODY, "localEulerAngles.z", B_BODY_ROT, 0f);

        // Head: 기울기 보정
        LockAt(clip, HEAD, "localPosition.x", 0.05f);
        AddCurve(clip, HEAD, "localPosition.y", B_HEAD_Y, 0.45f);
        LockAt(clip, HEAD, "localPosition.z", 0f);
        AddCurve(clip, HEAD, "localEulerAngles.z", B_HEAD_ROT, 0f);

        // ShoulderJoint_F: 오른쪽 빈 손, 달리기 스윙
        LockAt(clip, SHOULDER_F, "localPosition.x", -0.27f);
        AddCurve(clip, SHOULDER_F, "localPosition.y", B_SF_Y, 0.32f);
        LockAt(clip, SHOULDER_F, "localPosition.z", 0f);
        AddCurve(clip, SHOULDER_F, "localEulerAngles.z", B_SF_ROT, 0f);

        // ShoulderJoint_B: 왼쪽 검 든 손, 뒤로 당겨 고정
        LockAt(clip, SHOULDER_B, "localPosition.x", 0.36f);
        AddCurve(clip, SHOULDER_B, "localPosition.y", B_SB_Y, 0.28f);
        LockAt(clip, SHOULDER_B, "localPosition.z", 0f);
        AddCurve(clip, SHOULDER_B, "localEulerAngles.z", B_SB_ROT, 0f);

        // ArmJoint_F: 오른쪽 빈 손 스윙
        AddCurve(clip, ARM_F, "localEulerAngles.z", B_AF_ROT, 0f);

        // ArmJoint_B: 왼쪽 검 든 손 직각으로 올림 → 장검 하늘 향함
        AddCurve(clip, ARM_B, "localEulerAngles.z", B_AB_ROT, 0f);
        AddCurve(clip, WEAPON, "localEulerAngles.z", B_WEAPON_ROT, -65f);

        ApplyLegs(clip, B_LEG_F_X, B_LEG_F_Y, B_LEG_F_ROT, B_FOOT_F_ROT,
                        B_LEG_B_X, B_LEG_B_Y, B_LEG_B_ROT, B_FOOT_B_ROT);
        SaveClip(clip, "EBasicMob_Run_B");
    }

    // ── Helpers ─────────────────────────────────────────────────────

    static AnimationClip MakeClip()
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = FRAME_RATE;
        AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, s);
        return clip;
    }

    static void ApplyLegs(AnimationClip clip,
        float[] lfX, float[] lfY, float[] lfRot, float[] ffRot,
        float[] lbX, float[] lbY, float[] lbRot, float[] fbRot)
    {
        AddCurve(clip, LEG_F, "localPosition.x", lfX, -0.25f);
        AddCurve(clip, LEG_F, "localPosition.y", lfY, -0.75f);
        LockAt(clip, LEG_F, "localPosition.z", 0f);
        AddCurve(clip, LEG_F, "localEulerAngles.z", lfRot, 0f);
        AddCurve(clip, FOOT_F, "localEulerAngles.z", ffRot, 0f);

        AddCurve(clip, LEG_B, "localPosition.x", lbX, 0.25f);
        AddCurve(clip, LEG_B, "localPosition.y", lbY, -0.75f);
        LockAt(clip, LEG_B, "localPosition.z", 0f);
        AddCurve(clip, LEG_B, "localEulerAngles.z", lbRot, 0f);
        AddCurve(clip, FOOT_B, "localEulerAngles.z", fbRot, 0f);
    }

    static void AddCurve(AnimationClip clip, string path, string prop, float[] offsets, float baseVal = 0f)
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

    static void LockAt(AnimationClip clip, string path, string prop, float val) =>
        clip.SetCurve(path, typeof(Transform), prop,
            AnimationCurve.Linear(0f, val, DURATION, val));

    static void SaveClip(AnimationClip clip, string clipName)
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
            AssetDatabase.CreateFolder(Path.GetDirectoryName(SAVE_PATH), Path.GetFileName(SAVE_PATH));

        string assetPath = $"{SAVE_PATH}/{clipName}.anim";
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
        Debug.Log($"[EBasicMob] {clipName} created -> {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}