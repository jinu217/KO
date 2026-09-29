using UnityEngine;
using UnityEditor;

public class Smith_IdleGenerator
{
    const string SAVE_PATH = "Assets/Animations/Smith";
    const string CLIP_NAME = "Smith_Idle";
    const float FRAME_RATE = 12f;
    const int TOTAL_FRAMES = 48;
    const float DUR = TOTAL_FRAMES / (float)FRAME_RATE;

    const string BODY = "Body";
    const string HEAD = "Body/Head";
    const string SF = "Body/Shoulder_F";
    const string HF = "Body/Shoulder_F/Hand_F";
    const string SB = "Body/Shoulder_B";
    const string HB = "Body/Shoulder_B/Hand_B";
    const string BOTTOM = "Body/Bottom";
    const string FOOT_F = "Body/Bottom/Foot_F";
    const string FOOT_B = "Body/Bottom/Foot_B";

    // 매 프레임 사인파 값 — Auto 탄젠트 오버슈트 없음
    static readonly float[] BODY_Y = { 0.0f, 0.0065f, 0.0129f, 0.0191f, 0.025f, 0.0304f, 0.0354f, 0.0397f, 0.0433f, 0.0462f, 0.0483f, 0.0496f, 0.05f, 0.0496f, 0.0483f, 0.0462f, 0.0433f, 0.0397f, 0.0354f, 0.0304f, 0.025f, 0.0191f, 0.0129f, 0.0065f, 0.0f, -0.0065f, -0.0129f, -0.0191f, -0.025f, -0.0304f, -0.0354f, -0.0397f, -0.0433f, -0.0462f, -0.0483f, -0.0496f, -0.05f, -0.0496f, -0.0483f, -0.0462f, -0.0433f, -0.0397f, -0.0354f, -0.0304f, -0.025f, -0.0191f, -0.0129f, -0.0065f };
    static readonly float[] HEAD_Y = { 0.71f, 0.7133f, 0.7165f, 0.7196f, 0.7225f, 0.7252f, 0.7277f, 0.7298f, 0.7317f, 0.7331f, 0.7341f, 0.7348f, 0.735f, 0.7348f, 0.7341f, 0.7331f, 0.7317f, 0.7298f, 0.7277f, 0.7252f, 0.7225f, 0.7196f, 0.7165f, 0.7133f, 0.71f, 0.7067f, 0.7035f, 0.7004f, 0.6975f, 0.6948f, 0.6923f, 0.6902f, 0.6883f, 0.6869f, 0.6859f, 0.6852f, 0.685f, 0.6852f, 0.6859f, 0.6869f, 0.6883f, 0.6902f, 0.6923f, 0.6948f, 0.6975f, 0.7004f, 0.7035f, 0.7067f };
    // 어깨: 8프레임 딜레이
    static readonly float[] SF_Y = { 0.51f, 0.5122f, 0.5148f, 0.5177f, 0.521f, 0.5245f, 0.5282f, 0.5321f, 0.536f, 0.5399f, 0.5438f, 0.5475f, 0.551f, 0.5543f, 0.5572f, 0.5598f, 0.562f, 0.5637f, 0.565f, 0.5657f, 0.566f, 0.5657f, 0.565f, 0.5637f, 0.562f, 0.5598f, 0.5572f, 0.5543f, 0.551f, 0.5475f, 0.5438f, 0.5399f, 0.536f, 0.5321f, 0.5282f, 0.5245f, 0.521f, 0.5177f, 0.5148f, 0.5122f, 0.51f, 0.5083f, 0.507f, 0.5063f, 0.506f, 0.5063f, 0.507f, 0.5083f };
    static readonly float[] SB_Y = { 0.51f, 0.5122f, 0.5148f, 0.5177f, 0.521f, 0.5245f, 0.5282f, 0.5321f, 0.536f, 0.5399f, 0.5438f, 0.5475f, 0.551f, 0.5543f, 0.5572f, 0.5598f, 0.562f, 0.5637f, 0.565f, 0.5657f, 0.566f, 0.5657f, 0.565f, 0.5637f, 0.562f, 0.5598f, 0.5572f, 0.5543f, 0.551f, 0.5475f, 0.5438f, 0.5399f, 0.536f, 0.5321f, 0.5282f, 0.5245f, 0.521f, 0.5177f, 0.5148f, 0.5122f, 0.51f, 0.5083f, 0.507f, 0.5063f, 0.506f, 0.5063f, 0.507f, 0.5083f };
    // Bottom: Body Y 반대로 상쇄 → 발이 바닥에 고정
    static readonly float[] BOTTOM_Y = { -0.3f, -0.3065f, -0.3129f, -0.3191f, -0.325f, -0.3304f, -0.3354f, -0.3397f, -0.3433f, -0.3462f, -0.3483f, -0.3496f, -0.35f, -0.3496f, -0.3483f, -0.3462f, -0.3433f, -0.3397f, -0.3354f, -0.3304f, -0.325f, -0.3191f, -0.3129f, -0.3065f, -0.3f, -0.2935f, -0.2871f, -0.2809f, -0.275f, -0.2696f, -0.2646f, -0.2603f, -0.2567f, -0.2538f, -0.2517f, -0.2504f, -0.25f, -0.2504f, -0.2517f, -0.2538f, -0.2567f, -0.2603f, -0.2646f, -0.2696f, -0.275f, -0.2809f, -0.2871f, -0.2935f };

    [MenuItem("Tools/NPC1/Smith/Generate Idle")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "Smith");
        }

        var clip = new AnimationClip { frameRate = FRAME_RATE };
        clip.name = CLIP_NAME;
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        // 매 프레임 값 배열 → Auto 탄젠트 커브
        void C(string path, string prop, float[] vals)
        {
            var keys = new Keyframe[TOTAL_FRAMES];
            for (int i = 0; i < TOTAL_FRAMES; i++)
                keys[i] = new Keyframe(i / FRAME_RATE, vals[i]);
            var curve = new AnimationCurve(keys);
            for (int i = 0; i < curve.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            }
            clip.SetCurve(path, typeof(Transform), prop, curve);
        }
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        // Body
        L(BODY, "localPosition.x", 0f);
        C(BODY, "localPosition.y", BODY_Y);
        L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);

        // Head
        L(HEAD, "localPosition.x", -0.170f);
        C(HEAD, "localPosition.y", HEAD_Y);
        L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);

        // Shoulder_F / Hand_F
        L(SF, "localPosition.x", -0.610f);
        C(SF, "localPosition.y", SF_Y);
        L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", 0f);
        L(HF, "localPosition.y", -0.317f);
        L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        // Shoulder_B / Hand_B
        L(SB, "localPosition.x", 0.137f);
        C(SB, "localPosition.y", SB_Y);
        L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.079f);
        L(HB, "localPosition.y", -0.300f);
        L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);

        // Bottom (Body 상쇄)
        L(BOTTOM, "localPosition.x", 0f);
        C(BOTTOM, "localPosition.y", BOTTOM_Y);
        L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);

        // Foot_F (고정)
        L(FOOT_F, "localPosition.x", -0.480f);
        L(FOOT_F, "localPosition.y", -0.190f);
        L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);

        // Foot_B (고정)
        L(FOOT_B, "localPosition.x", 0.005f);
        L(FOOT_B, "localPosition.y", -0.190f);
        L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

        string assetPath = $"{SAVE_PATH}/{CLIP_NAME}.anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
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
        Debug.Log($"[Smith] {CLIP_NAME} → {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}