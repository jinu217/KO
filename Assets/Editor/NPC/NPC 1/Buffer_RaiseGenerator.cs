using UnityEngine;
using UnityEditor;

public class Buffer_RaiseSwayGenerator
{
    const string SAVE_PATH = "Assets/Animations/Buffer";
    const string CLIP_NAME = "Buffer_RaiseSway";
    const float FRAME_RATE = 12f;
    const int TOTAL_FRAMES = 32;
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

    // 팔 올린 자세 (-120/+120) 에서 흔들거리는 루프
    static readonly float[] BODY_Y = { 0.0f, 0.0073f, 0.0144f, 0.0208f, 0.0265f, 0.0312f, 0.0346f, 0.0368f, 0.0375f, 0.0368f, 0.0346f, 0.0312f, 0.0265f, 0.0208f, 0.0144f, 0.0073f, 0.0f, -0.0073f, -0.0144f, -0.0208f, -0.0265f, -0.0312f, -0.0346f, -0.0368f, -0.0375f, -0.0368f, -0.0346f, -0.0312f, -0.0265f, -0.0208f, -0.0144f, -0.0073f };
    static readonly float[] SF_Y = { 0.3109f, 0.3111f, 0.3119f, 0.3131f, 0.3147f, 0.3167f, 0.319f, 0.3214f, 0.324f, 0.3266f, 0.329f, 0.3313f, 0.3333f, 0.3349f, 0.3361f, 0.3369f, 0.3371f, 0.3369f, 0.3361f, 0.3349f, 0.3333f, 0.3313f, 0.329f, 0.3266f, 0.324f, 0.3214f, 0.319f, 0.3167f, 0.3147f, 0.3131f, 0.3119f, 0.3111f };
    static readonly float[] SB_Y = { 0.3269f, 0.3271f, 0.3279f, 0.3291f, 0.3307f, 0.3327f, 0.335f, 0.3374f, 0.34f, 0.3426f, 0.345f, 0.3473f, 0.3493f, 0.3509f, 0.3521f, 0.3529f, 0.3531f, 0.3529f, 0.3521f, 0.3509f, 0.3493f, 0.3473f, 0.345f, 0.3426f, 0.34f, 0.3374f, 0.335f, 0.3327f, 0.3307f, 0.3291f, 0.3279f, 0.3271f };
    static readonly float[] BOTTOM_Y = { -0.41f, -0.4173f, -0.4244f, -0.4308f, -0.4365f, -0.4412f, -0.4446f, -0.4468f, -0.4475f, -0.4468f, -0.4446f, -0.4412f, -0.4365f, -0.4308f, -0.4244f, -0.4173f, -0.41f, -0.4027f, -0.3956f, -0.3892f, -0.3835f, -0.3788f, -0.3754f, -0.3732f, -0.3725f, -0.3732f, -0.3754f, -0.3788f, -0.3835f, -0.3892f, -0.3956f, -0.4027f };
    static readonly float[] SF_ROT = { -110.0f, -108.05f, -106.17f, -104.44f, -102.93f, -101.69f, -100.76f, -100.19f, -100.0f, -100.19f, -100.76f, -101.69f, -102.93f, -104.44f, -106.17f, -108.05f, -110.0f, -111.95f, -113.83f, -115.56f, -117.07f, -118.31f, -119.24f, -119.81f, -120.0f, -119.81f, -119.24f, -118.31f, -117.07f, -115.56f, -113.83f, -111.95f };
    static readonly float[] HF_ROT = { -26.0f, -25.22f, -24.47f, -23.78f, -23.17f, -22.67f, -22.3f, -22.08f, -22.0f, -22.08f, -22.3f, -22.67f, -23.17f, -23.78f, -24.47f, -25.22f, -26.0f, -26.78f, -27.53f, -28.22f, -28.83f, -29.33f, -29.7f, -29.92f, -30.0f, -29.92f, -29.7f, -29.33f, -28.83f, -28.22f, -27.53f, -26.78f };
    static readonly float[] SB_ROT = { 110.0f, 108.05f, 106.17f, 104.44f, 102.93f, 101.69f, 100.76f, 100.19f, 100.0f, 100.19f, 100.76f, 101.69f, 102.93f, 104.44f, 106.17f, 108.05f, 110.0f, 111.95f, 113.83f, 115.56f, 117.07f, 118.31f, 119.24f, 119.81f, 120.0f, 119.81f, 119.24f, 118.31f, 117.07f, 115.56f, 113.83f, 111.95f };
    static readonly float[] HB_ROT = { 26.0f, 25.22f, 24.47f, 23.78f, 23.17f, 22.67f, 22.3f, 22.08f, 22.0f, 22.08f, 22.3f, 22.67f, 23.17f, 23.78f, 24.47f, 25.22f, 26.0f, 26.78f, 27.53f, 28.22f, 28.83f, 29.33f, 29.7f, 29.92f, 30.0f, 29.92f, 29.7f, 29.33f, 28.83f, 28.22f, 27.53f, 26.78f };

    [MenuItem("Tools/NPC1/Buffer/Generate RaiseSway")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "Buffer");
        }

        var clip = new AnimationClip { frameRate = FRAME_RATE };
        clip.name = CLIP_NAME;
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, s);

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

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", BODY_Y); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);
        L(HEAD, "localPosition.x", -0.093f); L(HEAD, "localPosition.y", 0.472f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);
        L(SF, "localPosition.x", -0.254f); C(SF, "localPosition.y", SF_Y); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", SF_ROT);
        L(HF, "localPosition.x", -0.105f); L(HF, "localPosition.y", -0.234f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", HF_ROT);
        L(SB, "localPosition.x", 0.043f); C(SB, "localPosition.y", SB_Y); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", SB_ROT);
        L(HB, "localPosition.x", 0.130f); L(HB, "localPosition.y", -0.250f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", HB_ROT);
        L(BOTTOM, "localPosition.x", 0f); C(BOTTOM, "localPosition.y", BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.255f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.041f); L(FOOT_B, "localPosition.y", 0f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

        string assetPath = $"{SAVE_PATH}/{CLIP_NAME}.anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null) { EditorUtility.CopySerialized(clip, existing); existing.name = CLIP_NAME; AssetDatabase.SaveAssets(); }
        else { AssetDatabase.CreateAsset(clip, assetPath); AssetDatabase.SaveAssets(); }
        AssetDatabase.Refresh();
        Debug.Log($"[Buffer] {CLIP_NAME} → {assetPath}");
    }
    // ── RaiseBodySway 전용 배열 ──────────────────────────────────────
    static readonly float[] RBS_BODY_Y = { 0.0f, 0.0073f, 0.0144f, 0.0208f, 0.0265f, 0.0312f, 0.0346f, 0.0368f, 0.0375f, 0.0368f, 0.0346f, 0.0312f, 0.0265f, 0.0208f, 0.0144f, 0.0073f, 0.0f, -0.0073f, -0.0144f, -0.0208f, -0.0265f, -0.0312f, -0.0346f, -0.0368f, -0.0375f, -0.0368f, -0.0346f, -0.0312f, -0.0265f, -0.0208f, -0.0144f, -0.0073f };
    static readonly float[] RBS_SF_Y = { 0.3109f, 0.3111f, 0.3119f, 0.3131f, 0.3147f, 0.3167f, 0.319f, 0.3214f, 0.324f, 0.3266f, 0.329f, 0.3313f, 0.3333f, 0.3349f, 0.3361f, 0.3369f, 0.3371f, 0.3369f, 0.3361f, 0.3349f, 0.3333f, 0.3313f, 0.329f, 0.3266f, 0.324f, 0.3214f, 0.319f, 0.3167f, 0.3147f, 0.3131f, 0.3119f, 0.3111f };
    static readonly float[] RBS_SB_Y = { 0.3269f, 0.3271f, 0.3279f, 0.3291f, 0.3307f, 0.3327f, 0.335f, 0.3374f, 0.34f, 0.3426f, 0.345f, 0.3473f, 0.3493f, 0.3509f, 0.3521f, 0.3529f, 0.3531f, 0.3529f, 0.3521f, 0.3509f, 0.3493f, 0.3473f, 0.345f, 0.3426f, 0.34f, 0.3374f, 0.335f, 0.3327f, 0.3307f, 0.3291f, 0.3279f, 0.3271f };
    static readonly float[] RBS_BOTTOM_Y = { -0.41f, -0.4173f, -0.4244f, -0.4308f, -0.4365f, -0.4412f, -0.4446f, -0.4468f, -0.4475f, -0.4468f, -0.4446f, -0.4412f, -0.4365f, -0.4308f, -0.4244f, -0.4173f, -0.41f, -0.4027f, -0.3956f, -0.3892f, -0.3835f, -0.3788f, -0.3754f, -0.3732f, -0.3725f, -0.3732f, -0.3754f, -0.3788f, -0.3835f, -0.3892f, -0.3956f, -0.4027f };
    static readonly float[] RBS_BODY_ROT = { 0.0f, 1.56f, 3.06f, 4.44f, 5.66f, 6.65f, 7.39f, 7.85f, 8.0f, 7.85f, 7.39f, 6.65f, 5.66f, 4.44f, 3.06f, 1.56f, 0.0f, -1.56f, -3.06f, -4.44f, -5.66f, -6.65f, -7.39f, -7.85f, -8.0f, -7.85f, -7.39f, -6.65f, -5.66f, -4.44f, -3.06f, -1.56f };
    static readonly float[] RBS_BOT_ROT = { -0.0f, -1.56f, -3.06f, -4.44f, -5.66f, -6.65f, -7.39f, -7.85f, -8.0f, -7.85f, -7.39f, -6.65f, -5.66f, -4.44f, -3.06f, -1.56f, -0.0f, 1.56f, 3.06f, 4.44f, 5.66f, 6.65f, 7.39f, 7.85f, 8.0f, 7.85f, 7.39f, 6.65f, 5.66f, 4.44f, 3.06f, 1.56f };
    static readonly float[] RBS_SF_ROT = { -110.0f, -107.07f, -104.26f, -101.67f, -99.39f, -97.53f, -96.14f, -95.29f, -95.0f, -95.29f, -96.14f, -97.53f, -99.39f, -101.67f, -104.26f, -107.07f, -110.0f, -112.93f, -115.74f, -118.33f, -120.61f, -122.47f, -123.86f, -124.71f, -125.0f, -124.71f, -123.86f, -122.47f, -120.61f, -118.33f, -115.74f, -112.93f };
    static readonly float[] RBS_HF_ROT = { -26.0f, -25.02f, -24.09f, -23.22f, -22.46f, -21.84f, -21.38f, -21.1f, -21.0f, -21.1f, -21.38f, -21.84f, -22.46f, -23.22f, -24.09f, -25.02f, -26.0f, -26.98f, -27.91f, -28.78f, -29.54f, -30.16f, -30.62f, -30.9f, -31.0f, -30.9f, -30.62f, -30.16f, -29.54f, -28.78f, -27.91f, -26.98f };
    static readonly float[] RBS_SB_ROT = { 110.0f, 112.93f, 115.74f, 118.33f, 120.61f, 122.47f, 123.86f, 124.71f, 125.0f, 124.71f, 123.86f, 122.47f, 120.61f, 118.33f, 115.74f, 112.93f, 110.0f, 107.07f, 104.26f, 101.67f, 99.39f, 97.53f, 96.14f, 95.29f, 95.0f, 95.29f, 96.14f, 97.53f, 99.39f, 101.67f, 104.26f, 107.07f };
    static readonly float[] RBS_HB_ROT = { 26.0f, 26.98f, 27.91f, 28.78f, 29.54f, 30.16f, 30.62f, 30.9f, 31.0f, 30.9f, 30.62f, 30.16f, 29.54f, 28.78f, 27.91f, 26.98f, 26.0f, 25.02f, 24.09f, 23.22f, 22.46f, 21.84f, 21.38f, 21.1f, 21.0f, 21.1f, 21.38f, 21.84f, 22.46f, 23.22f, 24.09f, 25.02f };

    [MenuItem("Tools/NPC1/Buffer/Generate RaiseBodySway")]
    public static void GenerateRaiseBodySway()
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "Buffer");
        }

        var clip = new AnimationClip { frameRate = FRAME_RATE };
        clip.name = "Buffer_RaiseBodySway";
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, s);

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

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", RBS_BODY_Y); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", RBS_BODY_ROT);
        L(HEAD, "localPosition.x", -0.093f); L(HEAD, "localPosition.y", 0.472f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);
        L(SF, "localPosition.x", -0.254f); C(SF, "localPosition.y", RBS_SF_Y); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", RBS_SF_ROT);
        L(HF, "localPosition.x", -0.105f); L(HF, "localPosition.y", -0.234f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", RBS_HF_ROT);
        L(SB, "localPosition.x", 0.043f); C(SB, "localPosition.y", RBS_SB_Y); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", RBS_SB_ROT);
        L(HB, "localPosition.x", 0.130f); L(HB, "localPosition.y", -0.250f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", RBS_HB_ROT);
        L(BOTTOM, "localPosition.x", 0f); C(BOTTOM, "localPosition.y", RBS_BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", RBS_BOT_ROT);
        L(FOOT_F, "localPosition.x", -0.255f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.041f); L(FOOT_B, "localPosition.y", 0f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

        string assetPath = $"{SAVE_PATH}/Buffer_RaiseBodySway.anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null) { EditorUtility.CopySerialized(clip, existing); existing.name = "Buffer_RaiseBodySway"; AssetDatabase.SaveAssets(); }
        else { AssetDatabase.CreateAsset(clip, assetPath); AssetDatabase.SaveAssets(); }
        AssetDatabase.Refresh();
        Debug.Log($"[Buffer] Buffer_RaiseBodySway → {assetPath}");
    }
}