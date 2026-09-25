using UnityEngine;
using UnityEditor;

public class Buffer_IdleGenerator
{
    const string SAVE_PATH = "Assets/Animations/Buffer";
    const string CLIP_NAME = "Buffer_Idle";
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

    // rest 실측값:
    //   Body pos(0,0), Head pos(-0.093, 0.472)
    //   SF pos(-0.254, 0.324), HF pos(-0.105, -0.234)
    //   SB pos(0.043, 0.340), HB pos(0.130, -0.250)
    //   Bottom pos(0, -0.410), Foot_F pos(-0.255, 0), Foot_B pos(0.041, 0)

    static readonly float[] BODY_Y = { 0.0f, 0.0049f, 0.0097f, 0.0144f, 0.0187f, 0.0228f, 0.0265f, 0.0298f, 0.0325f, 0.0346f, 0.0362f, 0.0372f, 0.0375f, 0.0372f, 0.0362f, 0.0346f, 0.0325f, 0.0298f, 0.0265f, 0.0228f, 0.0187f, 0.0144f, 0.0097f, 0.0049f, 0.0f, -0.0049f, -0.0097f, -0.0144f, -0.0187f, -0.0228f, -0.0265f, -0.0298f, -0.0325f, -0.0346f, -0.0362f, -0.0372f, -0.0375f, -0.0372f, -0.0362f, -0.0346f, -0.0325f, -0.0298f, -0.0265f, -0.0228f, -0.0188f, -0.0144f, -0.0097f, -0.0049f };
    static readonly float[] HEAD_Y = { 0.472f, 0.4744f, 0.4769f, 0.4792f, 0.4814f, 0.4834f, 0.4853f, 0.4869f, 0.4882f, 0.4893f, 0.4901f, 0.4906f, 0.4907f, 0.4906f, 0.4901f, 0.4893f, 0.4882f, 0.4869f, 0.4853f, 0.4834f, 0.4814f, 0.4792f, 0.4769f, 0.4744f, 0.472f, 0.4696f, 0.4671f, 0.4648f, 0.4626f, 0.4606f, 0.4587f, 0.4571f, 0.4558f, 0.4547f, 0.4539f, 0.4534f, 0.4532f, 0.4534f, 0.4539f, 0.4547f, 0.4558f, 0.4571f, 0.4587f, 0.4606f, 0.4626f, 0.4648f, 0.4671f, 0.4696f };
    static readonly float[] SF_Y = { 0.3126f, 0.3136f, 0.3147f, 0.316f, 0.3174f, 0.319f, 0.3206f, 0.3223f, 0.324f, 0.3257f, 0.3274f, 0.329f, 0.3306f, 0.332f, 0.3333f, 0.3344f, 0.3354f, 0.3361f, 0.3367f, 0.337f, 0.3371f, 0.337f, 0.3367f, 0.3361f, 0.3354f, 0.3344f, 0.3333f, 0.332f, 0.3306f, 0.329f, 0.3274f, 0.3257f, 0.324f, 0.3223f, 0.3206f, 0.319f, 0.3174f, 0.316f, 0.3147f, 0.3136f, 0.3126f, 0.3119f, 0.3113f, 0.311f, 0.3109f, 0.311f, 0.3113f, 0.3119f };
    static readonly float[] SB_Y = { 0.3286f, 0.3296f, 0.3307f, 0.332f, 0.3334f, 0.335f, 0.3366f, 0.3383f, 0.34f, 0.3417f, 0.3434f, 0.345f, 0.3466f, 0.348f, 0.3493f, 0.3504f, 0.3514f, 0.3521f, 0.3527f, 0.353f, 0.3531f, 0.353f, 0.3527f, 0.3521f, 0.3514f, 0.3504f, 0.3493f, 0.348f, 0.3466f, 0.345f, 0.3434f, 0.3417f, 0.34f, 0.3383f, 0.3366f, 0.335f, 0.3334f, 0.332f, 0.3307f, 0.3296f, 0.3286f, 0.3279f, 0.3273f, 0.327f, 0.3269f, 0.327f, 0.3273f, 0.3279f };
    static readonly float[] BOTTOM_Y = { -0.41f, -0.4149f, -0.4197f, -0.4244f, -0.4287f, -0.4328f, -0.4365f, -0.4398f, -0.4425f, -0.4446f, -0.4462f, -0.4472f, -0.4475f, -0.4472f, -0.4462f, -0.4446f, -0.4425f, -0.4398f, -0.4365f, -0.4328f, -0.4287f, -0.4244f, -0.4197f, -0.4149f, -0.41f, -0.4051f, -0.4003f, -0.3956f, -0.3913f, -0.3872f, -0.3835f, -0.3802f, -0.3775f, -0.3754f, -0.3738f, -0.3728f, -0.3725f, -0.3728f, -0.3738f, -0.3754f, -0.3775f, -0.3802f, -0.3835f, -0.3872f, -0.3912f, -0.3956f, -0.4003f, -0.4051f };

    [MenuItem("Tools/NPC1/Buffer/Generate Idle")]
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

        // Body
        L(BODY, "localPosition.x", 0f);
        C(BODY, "localPosition.y", BODY_Y);
        L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);

        // Head
        L(HEAD, "localPosition.x", -0.093f);
        C(HEAD, "localPosition.y", HEAD_Y);
        L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);

        // Shoulder_F / Hand_F
        L(SF, "localPosition.x", -0.254f);
        C(SF, "localPosition.y", SF_Y);
        L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.105f);
        L(HF, "localPosition.y", -0.234f);
        L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        // Shoulder_B / Hand_B
        L(SB, "localPosition.x", 0.043f);
        C(SB, "localPosition.y", SB_Y);
        L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.130f);
        L(HB, "localPosition.y", -0.250f);
        L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);

        // Bottom
        L(BOTTOM, "localPosition.x", 0f);
        C(BOTTOM, "localPosition.y", BOTTOM_Y);
        L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);

        // Foot_F
        L(FOOT_F, "localPosition.x", -0.255f);
        L(FOOT_F, "localPosition.y", 0f);
        L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);

        // Foot_B
        L(FOOT_B, "localPosition.x", 0.041f);
        L(FOOT_B, "localPosition.y", 0f);
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
        Debug.Log($"[Buffer] {CLIP_NAME} → {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}