using UnityEngine;
using UnityEditor;

public class Buffer_WaveGenerator
{
    const string SAVE_PATH = "Assets/Animations/Buffer";
    const string CLIP_NAME = "Buffer_Wave";
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

    // ================================================================
    // 뒷손 반갑게 세 번 흔들기 (36프레임, loopTime=false)
    //
    // f0~f4  : SB 위로 들기
    // f5~f8  : 1차 흔들기 (HB 좌우)
    // f9~f12 : 2차 흔들기
    // f13~f16: 3차 흔들기
    // f17~f24: SB 내리기 + rest 복귀
    // ================================================================

    static float[] F(float b, params (int i, float v)[] o)
    {
        var a = new float[TOTAL_FRAMES];
        for (int i = 0; i < TOTAL_FRAMES; i++) a[i] = b;
        foreach (var (i, v) in o) if (i < TOTAL_FRAMES) a[i] = v;
        return a;
    }

    // SB: 위로 크게 올리고 사인파로 흔들기
    static readonly float[] SB_ROT = { 0.0f, 25.0f, 50.0f, 75.0f, 100.0f, 107.07f, 110.0f, 107.07f, 100.0f, 92.93f, 90.0f, 92.93f, 100.0f, 107.07f, 110.0f, 107.07f, 100.0f, 92.93f, 90.0f, 72.0f, 54.0f, 36.0f, 18.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

    // HB: 사인파로 살랑살랑 (10~45)
    static readonly float[] HB_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 27.5f, 39.87f, 45.0f, 39.87f, 27.5f, 15.13f, 10.0f, 15.13f, 27.5f, 39.87f, 45.0f, 39.87f, 27.5f, 15.13f, 10.0f, 6.67f, 3.33f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

    // Body: 고정
    static readonly float[] BODY_ROT = F(0f);
    static readonly float[] BOT_ROT = F(0f);
    static readonly float[] HEAD_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 3.54f, 5.0f, 3.54f, 0.0f, -3.54f, -5.0f, -3.54f, 0.0f, 3.54f, 5.0f, 3.54f, 0.0f, -3.54f, -5.0f, -3.33f, -1.67f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

    [MenuItem("Tools/NPC1/Buffer/Generate Wave")]
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
        s.loopTime = false;
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
        L(BODY, "localPosition.y", 0f);
        L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", BODY_ROT);

        // Head
        L(HEAD, "localPosition.x", -0.093f);
        L(HEAD, "localPosition.y", 0.472f);
        L(HEAD, "localPosition.z", 0f);
        var headKeys = new Keyframe[TOTAL_FRAMES];
        for (int i = 0; i < TOTAL_FRAMES; i++) headKeys[i] = new Keyframe(i / FRAME_RATE, HEAD_ROT[i]);
        clip.SetCurve(HEAD, typeof(Transform), "localEulerAngles.z", new AnimationCurve(headKeys));

        // Shoulder_F / Hand_F (고정)
        L(SF, "localPosition.x", -0.254f);
        L(SF, "localPosition.y", 0.324f);
        L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.105f);
        L(HF, "localPosition.y", -0.234f);
        L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        // Shoulder_B / Hand_B
        L(SB, "localPosition.x", 0.043f);
        L(SB, "localPosition.y", 0.340f);
        L(SB, "localPosition.z", 0f);
        var sbKeys = new Keyframe[TOTAL_FRAMES];
        for (int i = 0; i < TOTAL_FRAMES; i++) sbKeys[i] = new Keyframe(i / FRAME_RATE, SB_ROT[i]);
        clip.SetCurve(SB, typeof(Transform), "localEulerAngles.z", new AnimationCurve(sbKeys));
        L(HB, "localPosition.x", 0.130f);
        L(HB, "localPosition.y", -0.250f);
        L(HB, "localPosition.z", 0f);
        var hbKeys = new Keyframe[TOTAL_FRAMES];
        for (int i = 0; i < TOTAL_FRAMES; i++) hbKeys[i] = new Keyframe(i / FRAME_RATE, HB_ROT[i]);
        clip.SetCurve(HB, typeof(Transform), "localEulerAngles.z", new AnimationCurve(hbKeys));

        // Bottom
        L(BOTTOM, "localPosition.x", 0f);
        L(BOTTOM, "localPosition.y", -0.410f);
        L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", BOT_ROT);

        // Foot (고정)
        L(FOOT_F, "localPosition.x", -0.255f);
        L(FOOT_F, "localPosition.y", 0f);
        L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
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

    // ── IdleWave 전용 배열 ───────────────────────────────────────
    static readonly float[] IW_BODY_Y = { 0.0f, 0.0049f, 0.0097f, 0.0144f, 0.0187f, 0.0228f, 0.0265f, 0.0298f, 0.0325f, 0.0346f, 0.0362f, 0.0372f, 0.0375f, 0.0372f, 0.0362f, 0.0346f, 0.0325f, 0.0298f, 0.0265f, 0.0228f, 0.0187f, 0.0144f, 0.0097f, 0.0049f, 0.0f, -0.0049f, -0.0097f, -0.0144f, -0.0187f, -0.0228f, -0.0265f, -0.0298f, -0.0325f, -0.0346f, -0.0362f, -0.0372f, -0.0375f, -0.0372f, -0.0362f, -0.0346f, -0.0325f, -0.0298f, -0.0265f, -0.0228f, -0.0188f, -0.0144f, -0.0097f, -0.0049f };
    static readonly float[] IW_HEAD_Y = { 0.472f, 0.4744f, 0.4769f, 0.4792f, 0.4814f, 0.4834f, 0.4853f, 0.4869f, 0.4882f, 0.4893f, 0.4901f, 0.4906f, 0.4907f, 0.4906f, 0.4901f, 0.4893f, 0.4882f, 0.4869f, 0.4853f, 0.4834f, 0.4814f, 0.4792f, 0.4769f, 0.4744f, 0.472f, 0.4696f, 0.4671f, 0.4648f, 0.4626f, 0.4606f, 0.4587f, 0.4571f, 0.4558f, 0.4547f, 0.4539f, 0.4534f, 0.4532f, 0.4534f, 0.4539f, 0.4547f, 0.4558f, 0.4571f, 0.4587f, 0.4606f, 0.4626f, 0.4648f, 0.4671f, 0.4696f };
    static readonly float[] IW_SF_Y = { 0.3126f, 0.3136f, 0.3147f, 0.316f, 0.3174f, 0.319f, 0.3206f, 0.3223f, 0.324f, 0.3257f, 0.3274f, 0.329f, 0.3306f, 0.332f, 0.3333f, 0.3344f, 0.3354f, 0.3361f, 0.3367f, 0.337f, 0.3371f, 0.337f, 0.3367f, 0.3361f, 0.3354f, 0.3344f, 0.3333f, 0.332f, 0.3306f, 0.329f, 0.3274f, 0.3257f, 0.324f, 0.3223f, 0.3206f, 0.319f, 0.3174f, 0.316f, 0.3147f, 0.3136f, 0.3126f, 0.3119f, 0.3113f, 0.311f, 0.3109f, 0.311f, 0.3113f, 0.3119f };
    static readonly float[] IW_SB_Y = { 0.3286f, 0.3296f, 0.3307f, 0.332f, 0.3334f, 0.335f, 0.3366f, 0.3383f, 0.34f, 0.3417f, 0.3434f, 0.345f, 0.3466f, 0.348f, 0.3493f, 0.3504f, 0.3514f, 0.3521f, 0.3527f, 0.353f, 0.3531f, 0.353f, 0.3527f, 0.3521f, 0.3514f, 0.3504f, 0.3493f, 0.348f, 0.3466f, 0.345f, 0.3434f, 0.3417f, 0.34f, 0.3383f, 0.3366f, 0.335f, 0.3334f, 0.332f, 0.3307f, 0.3296f, 0.3286f, 0.3279f, 0.3273f, 0.327f, 0.3269f, 0.327f, 0.3273f, 0.3279f };
    static readonly float[] IW_BOTTOM_Y = { -0.41f, -0.4149f, -0.4197f, -0.4244f, -0.4287f, -0.4328f, -0.4365f, -0.4398f, -0.4425f, -0.4446f, -0.4462f, -0.4472f, -0.4475f, -0.4472f, -0.4462f, -0.4446f, -0.4425f, -0.4398f, -0.4365f, -0.4328f, -0.4287f, -0.4244f, -0.4197f, -0.4149f, -0.41f, -0.4051f, -0.4003f, -0.3956f, -0.3913f, -0.3872f, -0.3835f, -0.3802f, -0.3775f, -0.3754f, -0.3738f, -0.3728f, -0.3725f, -0.3728f, -0.3738f, -0.3754f, -0.3775f, -0.3802f, -0.3835f, -0.3872f, -0.3912f, -0.3956f, -0.4003f, -0.4051f };

    [MenuItem("Tools/NPC1/Buffer/Generate IdleWave")]
    public static void GenerateIdleWave()
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "Buffer");
        }
        var clip = new AnimationClip { frameRate = FRAME_RATE };
        clip.name = "Buffer_IdleWave";
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        void Carr(string path, string prop, float[] vals)
        {
            var keys = new Keyframe[TOTAL_FRAMES];
            for (int i = 0; i < TOTAL_FRAMES; i++) keys[i] = new Keyframe(i / FRAME_RATE, vals[i]);
            clip.SetCurve(path, typeof(Transform), prop, new AnimationCurve(keys));
        }
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        L(BODY, "localPosition.x", 0f);
        Carr(BODY, "localPosition.y", IW_BODY_Y);
        L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);

        L(HEAD, "localPosition.x", -0.093f);
        Carr(HEAD, "localPosition.y", IW_HEAD_Y);
        L(HEAD, "localPosition.z", 0f);
        Carr(HEAD, "localEulerAngles.z", HEAD_ROT);

        L(SF, "localPosition.x", -0.254f);
        Carr(SF, "localPosition.y", IW_SF_Y);
        L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.105f);
        L(HF, "localPosition.y", -0.234f);
        L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        L(SB, "localPosition.x", 0.043f);
        Carr(SB, "localPosition.y", IW_SB_Y);
        L(SB, "localPosition.z", 0f);
        Carr(SB, "localEulerAngles.z", SB_ROT);
        L(HB, "localPosition.x", 0.130f);
        L(HB, "localPosition.y", -0.250f);
        L(HB, "localPosition.z", 0f);
        Carr(HB, "localEulerAngles.z", HB_ROT);

        L(BOTTOM, "localPosition.x", 0f);
        Carr(BOTTOM, "localPosition.y", IW_BOTTOM_Y);
        L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);

        L(FOOT_F, "localPosition.x", -0.255f);
        L(FOOT_F, "localPosition.y", 0f);
        L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.041f);
        L(FOOT_B, "localPosition.y", 0f);
        L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

        string assetPath = "Assets/Animations/Buffer/Buffer_IdleWave.anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null) { EditorUtility.CopySerialized(clip, existing); existing.name = "Buffer_IdleWave"; AssetDatabase.SaveAssets(); }
        else { AssetDatabase.CreateAsset(clip, assetPath); AssetDatabase.SaveAssets(); }
        AssetDatabase.Refresh();
        Debug.Log("[Buffer] Buffer_IdleWave -> " + assetPath);
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}