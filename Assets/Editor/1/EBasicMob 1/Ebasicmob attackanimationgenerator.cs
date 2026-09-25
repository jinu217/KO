using UnityEngine;
using UnityEditor;
using System.IO;

public class EBasicMob_AttackAnimationGenerator
{
    const string SAVE_PATH = "Assets/Animations/EBasicMob";
    const float FRAME_RATE = 12f;
    const float DURATION_A = 10f / FRAME_RATE; // 10 frames: windup->slash->recovery
    const float DURATION_B = 7f / FRAME_RATE; // 7  frames: windup->slash->hold

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
    // Weapon                                       rot -65
    // LegJoint_F      localPos (-0.25, -0.75, 0)  rot 0
    // LegJoint_B      localPos ( 0.25, -0.75, 0)  rot 0
    // Foot_F/B        localPos ( 0.00,  0.00, 0)  rot 0
    //
    // _F = 카메라 가까운쪽 = 몹 기준 오른손 (빈 손)
    // _B = 카메라 먼 쪽   = 몹 기준 왼손  (검 든 손)
    //   ArmJoint_B: 양수 = 위로,  음수 = 아래로
    //   ArmJoint_F: 음수 = 위로,  양수 = 아래로
    //   Body rot.z: 음수 = 앞으로 기울기
    // ================================================================

    // ── 세로 베기 키프레임 타이밍 ────────────────────────────────────
    //
    // VERSION A (10 frames, loop):
    //  f0        : 대기 (rest pose)
    //  f1        : 예비 — 상체 살짝 뒤로 젖힘, 검 위로 들기 시작
    //  f2        : 풀 예비 — 검 완전히 머리 위, 몸 최대 뒤로
    //  f3        : 내려치기 시작 — 몸 앞으로 빠르게
    //  f4        : 임팩트 — 몸 최대 앞으로 숙임, 검 최대 아래
    //  f5        : 임팩트 후 — 몸 약간 튕김
    //  f6-f9     : 복귀 (rest pose 로 돌아옴)
    //  f10 = f0  : 루프 마감
    //
    // VERSION B (7 frames, no loop):
    //  f0  : 대기
    //  f1  : 예비 시작
    //  f2  : 풀 예비
    //  f3  : 내려치기
    //  f4  : 임팩트
    //  f5  : 임팩트 후 튕김
    //  f6  : 자세 유지 (마지막 프레임)
    // ================================================================

    // ── ATTACK A (10f loop) keyframe values ─────────────────────────
    // 각 배열 길이 = 10, 인덱스 0~9, 마감키 [10]==[0]

    // Body rot.z: 뒤로 젖혔다가 앞으로 숙임
    // f0=0, f1=+5(뒤), f2=+8(풀뒤), f3=-5(앞), f4=-18(임팩트), f5=-10(튕김), f6~9 복귀
    static readonly float[] A_BODY_ROT = { 0f, 5f, 8f, -5f, -18f, -10f, -6f, -3f, -1f, 0f };
    static readonly float[] A_BODY_Y = { 0f, -0.02f, -0.04f, 0.02f, -0.06f, -0.02f, 0f, 0.01f, 0.01f, 0f };

    // Head: body 반대로 보정 (고개가 너무 꺾이지 않게)
    static readonly float[] A_HEAD_ROT = { 0f, -3f, -5f, 3f, 10f, 6f, 4f, 2f, 1f, 0f };
    static readonly float[] A_HEAD_Y = { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };

    // ShoulderJoint_F (오른손): 예비 시 뒤로, 내려칠 때 앞으로 따라옴
    static readonly float[] A_SF_ROT = { 0f, -8f, -14f, 5f, 16f, 10f, 6f, 3f, 1f, 0f };
    static readonly float[] A_SF_Y = { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };

    // ShoulderJoint_B (왼손/검): 예비 시 뒤로 당겨 검 올릴 공간
    static readonly float[] A_SB_ROT = { 0f, 10f, 18f, -5f, -12f, -6f, -3f, -1f, 0f, 0f };
    static readonly float[] A_SB_Y = { 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };

    // ArmJoint_F (오른손): 예비 시 위로 들고, 내려칠 때 앞으로 뻗음
    // 음수 = 위로
    static readonly float[] A_AF_ROT = { 0f, -20f, -35f, 10f, 25f, 15f, 8f, 3f, 1f, 0f };

    // ArmJoint_B (왼손/검): 예비 시 +90(위), 내려치면서 빠르게 -80(아래)로
    // 양수 = 위로
    static readonly float[] A_AB_ROT = { 0f, 50f, 90f, 20f, -80f, -40f, -15f, -5f, 0f, 0f };

    // Weapon (base -65): 팔 움직임에 추가로 약간 더 회전해서 호를 그림
    // 예비: 팔+무기 합산으로 검이 머리 위로
    // 임팩트: 검이 최대 아래로
    static readonly float[] A_WEAPON_ROT = { 0f, 10f, 20f, 5f, -20f, -10f, -5f, -2f, 0f, 0f };

    // 다리: 공격 시 앞발이 앞으로 나가고 뒷발이 버팀
    static readonly float[] A_LEG_F_X = { 0f, 0f, 0f, 0.05f, 0.10f, 0.06f, 0.03f, 0.01f, 0f, 0f };
    static readonly float[] A_LEG_F_ROT = { 0f, -3f, -5f, -10f, -15f, -10f, -5f, -2f, 0f, 0f };
    static readonly float[] A_FOOT_F_ROT = { 0f, 2f, 4f, 8f, 12f, 8f, 4f, 2f, 0f, 0f };
    static readonly float[] A_LEG_B_X = { 0f, 0f, 0f, -0.03f, -0.06f, -0.04f, -0.02f, -0.01f, 0f, 0f };
    static readonly float[] A_LEG_B_ROT = { 0f, 3f, 5f, 8f, 12f, 8f, 4f, 2f, 0f, 0f };
    static readonly float[] A_FOOT_B_ROT = { 0f, -2f, -3f, -5f, -8f, -5f, -3f, -1f, 0f, 0f };

    // ── ATTACK B (7f no-loop) keyframe values ────────────────────────
    // f0=rest, f1=windup start, f2=full windup, f3=slash, f4=impact, f5=bounce, f6=hold

    static readonly float[] B_BODY_ROT = { 0f, 5f, 8f, -5f, -18f, -12f, -14f };
    static readonly float[] B_BODY_Y = { 0f, -0.02f, -0.04f, 0.02f, -0.06f, -0.03f, -0.04f };

    static readonly float[] B_HEAD_ROT = { 0f, -3f, -5f, 3f, 10f, 7f, 8f };
    static readonly float[] B_HEAD_Y = { 0f, 0f, 0f, 0f, 0f, 0f, 0f };

    static readonly float[] B_SF_ROT = { 0f, -8f, -14f, 5f, 16f, 12f, 13f };
    static readonly float[] B_SF_Y = { 0f, 0f, 0f, 0f, 0f, 0f, 0f };

    static readonly float[] B_SB_ROT = { 0f, 10f, 18f, -5f, -12f, -8f, -10f };
    static readonly float[] B_SB_Y = { 0f, 0f, 0f, 0f, 0f, 0f, 0f };

    static readonly float[] B_AF_ROT = { 0f, -20f, -35f, 10f, 25f, 18f, 20f };
    static readonly float[] B_AB_ROT = { 0f, 50f, 90f, 20f, -80f, -50f, -60f };
    static readonly float[] B_WEAPON_ROT = { 0f, 10f, 20f, 5f, -20f, -12f, -15f };

    static readonly float[] B_LEG_F_X = { 0f, 0f, 0f, 0.05f, 0.10f, 0.08f, 0.09f };
    static readonly float[] B_LEG_F_ROT = { 0f, -3f, -5f, -10f, -15f, -12f, -13f };
    static readonly float[] B_FOOT_F_ROT = { 0f, 2f, 4f, 8f, 12f, 10f, 11f };
    static readonly float[] B_LEG_B_X = { 0f, 0f, 0f, -0.03f, -0.06f, -0.05f, -0.05f };
    static readonly float[] B_LEG_B_ROT = { 0f, 3f, 5f, 8f, 12f, 10f, 11f };
    static readonly float[] B_FOOT_B_ROT = { 0f, -2f, -3f, -5f, -8f, -6f, -7f };

    // ================================================================

    [MenuItem("Tools/1/EBasicMob/Generate Attack A (Slash + Recovery)")]
    public static void GenerateA()
    {
        int frames = 10;
        float dur = frames / FRAME_RATE;
        AnimationClip clip = MakeClip(loop: true);

        void C(string path, string prop, float[] vals, float baseVal = 0f) =>
            AddCurve(clip, path, prop, vals, baseVal, dur, FRAME_RATE, loop: true);
        void L(string path, string prop, float val) =>
            LockAt(clip, path, prop, val, dur);

        L(BODY, "localPosition.x", 0f);
        C(BODY, "localPosition.y", A_BODY_Y, 0f);
        L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", A_BODY_ROT, 0f);

        L(HEAD, "localPosition.x", 0.05f);
        C(HEAD, "localPosition.y", A_HEAD_Y, 0.45f);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", A_HEAD_ROT, 0f);

        L(SHOULDER_F, "localPosition.x", -0.27f);
        C(SHOULDER_F, "localPosition.y", A_SF_Y, 0.32f);
        L(SHOULDER_F, "localPosition.z", 0f);
        C(SHOULDER_F, "localEulerAngles.z", A_SF_ROT, 0f);

        L(SHOULDER_B, "localPosition.x", 0.36f);
        C(SHOULDER_B, "localPosition.y", A_SB_Y, 0.28f);
        L(SHOULDER_B, "localPosition.z", 0f);
        C(SHOULDER_B, "localEulerAngles.z", A_SB_ROT, 0f);

        C(ARM_F, "localEulerAngles.z", A_AF_ROT, 0f);
        C(ARM_B, "localEulerAngles.z", A_AB_ROT, 0f);
        C(WEAPON, "localEulerAngles.z", A_WEAPON_ROT, -65f);

        L(LEG_F, "localPosition.x", -0.25f); // X는 조금만 움직임 — AddCurve로 변경
        C(LEG_F, "localPosition.x", A_LEG_F_X, -0.25f);
        L(LEG_F, "localPosition.y", -0.75f);
        L(LEG_F, "localPosition.z", 0f);
        C(LEG_F, "localEulerAngles.z", A_LEG_F_ROT, 0f);
        C(FOOT_F, "localEulerAngles.z", A_FOOT_F_ROT, 0f);

        C(LEG_B, "localPosition.x", A_LEG_B_X, 0.25f);
        L(LEG_B, "localPosition.y", -0.75f);
        L(LEG_B, "localPosition.z", 0f);
        C(LEG_B, "localEulerAngles.z", A_LEG_B_ROT, 0f);
        C(FOOT_B, "localEulerAngles.z", A_FOOT_B_ROT, 0f);

        SaveClip(clip, "EBasicMob_Attack_A");
    }

    [MenuItem("Tools/1/EBasicMob/Generate Attack B (Slash + Hold)")]
    public static void GenerateB()
    {
        int frames = 7;
        float dur = frames / FRAME_RATE;
        AnimationClip clip = MakeClip(loop: false);

        void C(string path, string prop, float[] vals, float baseVal = 0f) =>
            AddCurve(clip, path, prop, vals, baseVal, dur, FRAME_RATE, loop: false);
        void L(string path, string prop, float val) =>
            LockAt(clip, path, prop, val, dur);

        L(BODY, "localPosition.x", 0f);
        C(BODY, "localPosition.y", B_BODY_Y, 0f);
        L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", B_BODY_ROT, 0f);

        L(HEAD, "localPosition.x", 0.05f);
        C(HEAD, "localPosition.y", B_HEAD_Y, 0.45f);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", B_HEAD_ROT, 0f);

        L(SHOULDER_F, "localPosition.x", -0.27f);
        C(SHOULDER_F, "localPosition.y", B_SF_Y, 0.32f);
        L(SHOULDER_F, "localPosition.z", 0f);
        C(SHOULDER_F, "localEulerAngles.z", B_SF_ROT, 0f);

        L(SHOULDER_B, "localPosition.x", 0.36f);
        C(SHOULDER_B, "localPosition.y", B_SB_Y, 0.28f);
        L(SHOULDER_B, "localPosition.z", 0f);
        C(SHOULDER_B, "localEulerAngles.z", B_SB_ROT, 0f);

        C(ARM_F, "localEulerAngles.z", B_AF_ROT, 0f);
        C(ARM_B, "localEulerAngles.z", B_AB_ROT, 0f);
        C(WEAPON, "localEulerAngles.z", B_WEAPON_ROT, -65f);

        C(LEG_F, "localPosition.x", B_LEG_F_X, -0.25f);
        L(LEG_F, "localPosition.y", -0.75f);
        L(LEG_F, "localPosition.z", 0f);
        C(LEG_F, "localEulerAngles.z", B_LEG_F_ROT, 0f);
        C(FOOT_F, "localEulerAngles.z", B_FOOT_F_ROT, 0f);

        C(LEG_B, "localPosition.x", B_LEG_B_X, 0.25f);
        L(LEG_B, "localPosition.y", -0.75f);
        L(LEG_B, "localPosition.z", 0f);
        C(LEG_B, "localEulerAngles.z", B_LEG_B_ROT, 0f);
        C(FOOT_B, "localEulerAngles.z", B_FOOT_B_ROT, 0f);

        SaveClip(clip, "EBasicMob_Attack_B");
    }

    // ── Helpers ─────────────────────────────────────────────────────

    static AnimationClip MakeClip(bool loop)
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = FRAME_RATE;
        AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, s);
        return clip;
    }

    static void AddCurve(AnimationClip clip, string path, string prop,
        float[] vals, float baseVal, float dur, float fps, bool loop)
    {
        int n = vals.Length;
        // loop: 마지막 키 = vals[0] / no-loop: 마지막 키 = vals[n-1] (hold)
        Keyframe[] keys = new Keyframe[n + 1];
        for (int i = 0; i < n; i++)
            keys[i] = new Keyframe(i / fps, baseVal + vals[i]);
        keys[n] = new Keyframe(dur, baseVal + (loop ? vals[0] : vals[n - 1]));

        AnimationCurve c = new AnimationCurve(keys);
        for (int i = 0; i < c.keys.Length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(c, i, AnimationUtility.TangentMode.Auto);
            AnimationUtility.SetKeyRightTangentMode(c, i, AnimationUtility.TangentMode.Auto);
        }
        clip.SetCurve(path, typeof(Transform), prop, c);
    }

    static void LockAt(AnimationClip clip, string path, string prop, float val, float dur) =>
        clip.SetCurve(path, typeof(Transform), prop,
            AnimationCurve.Linear(0f, val, dur, val));

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