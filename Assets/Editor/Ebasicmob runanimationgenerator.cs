using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// EBasicMob 건메이햄 스타일 달리기 애니메이션 생성기
/// MenuItem: Tools > EBasicMob > Generate Run Animation
/// </summary>
public class EBasicMob_RunAnimationGenerator
{
    // ── 애니메이션 설정 ──────────────────────────────────────────
    const string SAVE_PATH = "Assets/Animations/EBasicMob";
    const string CLIP_NAME = "EBasicMob_Run";
    const float FRAME_RATE = 12f;
    const int TOTAL_FRAMES = 8;           // 1 사이클
    const float DURATION = TOTAL_FRAMES / FRAME_RATE;

    // ── 히에라키 경로 (추출 데이터 기준) ────────────────────────
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

    // ── 건메이햄 뛰기 모션 파라미터 ─────────────────────────────
    // 몸통 상하 바운스 (Body local Y 오프셋 — 기본 pivot 기준)
    static readonly float[] BODY_Y_BOUNCE = { 0f, 0.04f, 0.06f, 0.04f, 0f, -0.03f, -0.04f, -0.03f };
    // 몸통 앞뒤 기울기 (Body rot.z)
    static readonly float[] BODY_TILT = { -5f, -4f, -3f, -4f, -5f, -6f, -7f, -6f };

    // 머리 고개 흔들림 (Head rot.z) — 달리는 방향 반대로 약하게
    static readonly float[] HEAD_ROT = { 3f, 2f, 1f, 2f, 3f, 4f, 5f, 4f };

    // 앞 어깨 Joint (ShoulderJoint_F rot.z) — 앞뒤로 스윙
    static readonly float[] SHOULDER_F_ROT = { 20f, 10f, -5f, -15f, -20f, -10f, 5f, 15f };
    // 앞 팔꿈치 Joint (ArmJoint_F rot.z) — 꺾임
    static readonly float[] ARM_F_ROT = { -30f, -20f, -10f, -20f, -30f, -40f, -50f, -40f };

    // 뒤 어깨 Joint (ShoulderJoint_B rot.z) — 반대 위상
    static readonly float[] SHOULDER_B_ROT = { -20f, -10f, 5f, 15f, 20f, 10f, -5f, -15f };
    // 뒤 팔꿈치 Joint
    static readonly float[] ARM_B_ROT = { -40f, -30f, -20f, -30f, -40f, -50f, -60f, -50f };

    // 무기 추가 흔들림 (Weapon rot.z, 기본 -65도에서 상대 오프셋)
    static readonly float[] WEAPON_ROT_OFFSET = { 0f, -3f, -5f, -3f, 0f, 3f, 5f, 3f };

    // ── 부츠(하체) 모션 — 건메이햄 핵심 ────────────────────────
    // LegJoint_F/B : Y 오프셋으로 발 높이 제어
    // Foot : rot.z 로 발목 각도
    //
    // 건메이햄 특징: 한쪽 발이 높이 올라갈 때 반대 발은 바닥 푸시
    // 발이 하나씩만 있으므로 두 Joint를 교대로 사용
    static readonly float[] LEG_F_Y = { 0f, 0.1f, 0.22f, 0.28f, 0.22f, 0.1f, 0f, -0.05f };
    static readonly float[] LEG_F_ROT = { 0f, -10f, -20f, -28f, -20f, -10f, 0f, 5f };
    static readonly float[] FOOT_F_ROT = { 0f, 5f, 10f, 15f, 10f, 5f, 0f, -5f };

    static readonly float[] LEG_B_Y = { 0.22f, 0.28f, 0.22f, 0.1f, 0f, -0.05f, 0f, 0.1f };
    static readonly float[] LEG_B_ROT = { -20f, -28f, -20f, -10f, 0f, 5f, 0f, -10f };
    static readonly float[] FOOT_B_ROT = { 10f, 15f, 10f, 5f, 0f, -5f, 0f, 5f };

    // ────────────────────────────────────────────────────────────

    [MenuItem("Tools/EBasicMob/Generate Run Animation")]
    public static void Generate()
    {
        // 저장 폴더 생성
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            string parent = Path.GetDirectoryName(SAVE_PATH);
            string folder = Path.GetFileName(SAVE_PATH);
            AssetDatabase.CreateFolder(parent, folder);
        }

        AnimationClip clip = new AnimationClip();
        clip.frameRate = FRAME_RATE;

        // loop 설정
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // ── 커브 등록 헬퍼 ──────────────────────────────────────
        void AddCurve(string path, string property, float[] values, float baseValue = 0f)
        {
            Keyframe[] keys = new Keyframe[values.Length + 1];
            for (int i = 0; i < values.Length; i++)
            {
                float t = i / FRAME_RATE;
                keys[i] = new Keyframe(t, baseValue + values[i]);
            }
            // 루프 마감 키 (첫 키와 동일)
            keys[values.Length] = new Keyframe(DURATION, baseValue + values[0]);

            AnimationCurve curve = new AnimationCurve(keys);

            // Tangent를 Auto로 설정
            for (int i = 0; i < curve.keys.Length; i++)
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);

            clip.SetCurve(path, typeof(Transform), property, curve);
        }

        // ── Body 바운스 & 기울기 ─────────────────────────────────
        AddCurve(BODY, "localPosition.y", BODY_Y_BOUNCE);
        AddCurve(BODY, "localEulerAngles.z", BODY_TILT);

        // ── Head 흔들림 ──────────────────────────────────────────
        AddCurve(HEAD, "localEulerAngles.z", HEAD_ROT);

        // ── 앞 팔 ────────────────────────────────────────────────
        AddCurve(SHOULDER_F, "localEulerAngles.z", SHOULDER_F_ROT);
        AddCurve(ARM_F, "localEulerAngles.z", ARM_F_ROT);

        // ── 뒤 팔 + 무기 ─────────────────────────────────────────
        AddCurve(SHOULDER_B, "localEulerAngles.z", SHOULDER_B_ROT);
        AddCurve(ARM_B, "localEulerAngles.z", ARM_B_ROT);
        AddCurve(WEAPON, "localEulerAngles.z", WEAPON_ROT_OFFSET, -65f);   // 기본각 -65° 유지

        // ── 부츠 (건메이햄 핵심) ─────────────────────────────────
        AddCurve(LEG_F, "localPosition.y", LEG_F_Y);
        AddCurve(LEG_F, "localEulerAngles.z", LEG_F_ROT);
        AddCurve(FOOT_F, "localEulerAngles.z", FOOT_F_ROT);

        AddCurve(LEG_B, "localPosition.y", LEG_B_Y);
        AddCurve(LEG_B, "localEulerAngles.z", LEG_B_ROT);
        AddCurve(FOOT_B, "localEulerAngles.z", FOOT_B_ROT);

        // ── 저장 ─────────────────────────────────────────────────
        string assetPath = $"{SAVE_PATH}/{CLIP_NAME}.anim";
        AssetDatabase.CreateAsset(clip, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[EBasicMob] Run 애니메이션 생성 완료 → {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = clip;
    }
}