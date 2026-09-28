using UnityEngine;
using UnityEditor;

public class Buffer_NodGenerators
{
    const string SAVE_PATH = "Assets/Animations/Buffer";
    const float FRAME_RATE = 12f;
    const int TOTAL_FRAMES = 24;
    const float DUR = 2f;

    const string BODY = "Body";
    const string HEAD = "Body/Head";
    const string SF = "Body/Shoulder_F";
    const string HF = "Body/Shoulder_F/Hand_F";
    const string SB = "Body/Shoulder_B";
    const string HB = "Body/Shoulder_B/Hand_B";
    const string BOTTOM = "Body/Bottom";
    const string FOOT_F = "Body/Bottom/Foot_F";
    const string FOOT_B = "Body/Bottom/Foot_B";

    // Buffer rest:
    //   Body pos(0,0), Head pos(-0.093, 0.472)
    //   SF pos(-0.254, 0.324), HF pos(-0.105, -0.234)
    //   SB pos(0.043, 0.340), HB pos(0.130, -0.250)
    //   Bottom pos(0, -0.410), Foot_F pos(-0.255, 0), Foot_B pos(0.041, 0)

    static float[] F(float b, params (int i, float v)[] o)
    {
        var a = new float[TOTAL_FRAMES];
        for (int i = 0; i < TOTAL_FRAMES; i++) a[i] = b;
        foreach (var (i, v) in o) if (i < TOTAL_FRAMES) a[i] = v;
        return a;
    }

    // Head rot: 아래로 먼저 (Nod용 24프레임)
    static readonly float[] HEAD_ROT = F(0f,
        (2, -6f), (3, -12f), (4, -8f), (5, -2f), (6, 0f),
        (8, -4f), (9, -8f), (10, -5f), (11, -1f), (12, 0f), (23, 0f));

    // Head rot: IdleNod용 48프레임
    static readonly float[] IDLE_NOD_HEAD_ROT = { 0.0f, 0.0f, -6.0f, -12.0f, -8.0f, -2.0f, 0.0f, 0.0f, -4.0f, -8.0f, -5.0f, -1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] IDLE_NOD_HEAD_Y = { 0.472f, 0.472f, 0.472f, 0.466f, 0.469f, 0.472f, 0.472f, 0.472f, 0.472f, 0.467f, 0.470f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f, 0.472f };

    static readonly float[] HEAD_Y = F(0.472f,
        (3, 0.466f), (4, 0.469f), (5, 0.472f),
        (9, 0.467f), (10, 0.470f), (11, 0.472f), (23, 0.472f));

    // IdleNod용 숨쉬기 배열 (48프레임)
    static readonly float[] IW_BODY_Y = { 0.0f, 0.0049f, 0.0097f, 0.0144f, 0.0187f, 0.0228f, 0.0265f, 0.0298f, 0.0325f, 0.0346f, 0.0362f, 0.0372f, 0.0375f, 0.0372f, 0.0362f, 0.0346f, 0.0325f, 0.0298f, 0.0265f, 0.0228f, 0.0187f, 0.0144f, 0.0097f, 0.0049f, 0.0f, -0.0049f, -0.0097f, -0.0144f, -0.0187f, -0.0228f, -0.0265f, -0.0298f, -0.0325f, -0.0346f, -0.0362f, -0.0372f, -0.0375f, -0.0372f, -0.0362f, -0.0346f, -0.0325f, -0.0298f, -0.0265f, -0.0228f, -0.0188f, -0.0144f, -0.0097f, -0.0049f };
    static readonly float[] IW_HEAD_Y = { 0.472f, 0.4744f, 0.4768f, 0.4791f, 0.4812f, 0.4832f, 0.4851f, 0.4867f, 0.488f, 0.4891f, 0.4899f, 0.4904f, 0.4905f, 0.4904f, 0.4899f, 0.4891f, 0.488f, 0.4867f, 0.4851f, 0.4832f, 0.4812f, 0.4791f, 0.4768f, 0.4744f, 0.472f, 0.4696f, 0.4672f, 0.4649f, 0.4628f, 0.4608f, 0.4589f, 0.4573f, 0.456f, 0.4549f, 0.4541f, 0.4536f, 0.4535f, 0.4536f, 0.4541f, 0.4549f, 0.456f, 0.4573f, 0.4589f, 0.4608f, 0.4628f, 0.4649f, 0.4672f, 0.4696f };
    static readonly float[] IW_SF_Y = { 0.3088f, 0.3101f, 0.3116f, 0.3133f, 0.3153f, 0.3173f, 0.3195f, 0.3217f, 0.324f, 0.3263f, 0.3285f, 0.3307f, 0.3327f, 0.3347f, 0.3364f, 0.3379f, 0.3392f, 0.3402f, 0.3409f, 0.3414f, 0.3415f, 0.3414f, 0.3409f, 0.3402f, 0.3392f, 0.3379f, 0.3364f, 0.3347f, 0.3327f, 0.3307f, 0.3285f, 0.3263f, 0.324f, 0.3217f, 0.3195f, 0.3173f, 0.3153f, 0.3133f, 0.3116f, 0.3101f, 0.3088f, 0.3078f, 0.3071f, 0.3066f, 0.3065f, 0.3066f, 0.3071f, 0.3078f };
    static readonly float[] IW_SB_Y = { 0.3248f, 0.3261f, 0.3276f, 0.3293f, 0.3313f, 0.3333f, 0.3355f, 0.3377f, 0.34f, 0.3423f, 0.3445f, 0.3467f, 0.3488f, 0.3507f, 0.3524f, 0.3539f, 0.3552f, 0.3562f, 0.3569f, 0.3574f, 0.3575f, 0.3574f, 0.3569f, 0.3562f, 0.3552f, 0.3539f, 0.3524f, 0.3507f, 0.3488f, 0.3467f, 0.3445f, 0.3423f, 0.34f, 0.3377f, 0.3355f, 0.3333f, 0.3313f, 0.3293f, 0.3276f, 0.3261f, 0.3248f, 0.3238f, 0.3231f, 0.3226f, 0.3225f, 0.3226f, 0.3231f, 0.3238f };
    static readonly float[] IW_BOTTOM_Y = { -0.41f, -0.4149f, -0.4197f, -0.4244f, -0.4287f, -0.4328f, -0.4365f, -0.4398f, -0.4425f, -0.4446f, -0.4462f, -0.4472f, -0.4475f, -0.4472f, -0.4462f, -0.4446f, -0.4425f, -0.4398f, -0.4365f, -0.4328f, -0.4287f, -0.4244f, -0.4197f, -0.4149f, -0.41f, -0.4051f, -0.4003f, -0.3956f, -0.3913f, -0.3872f, -0.3835f, -0.3802f, -0.3775f, -0.3754f, -0.3738f, -0.3728f, -0.3725f, -0.3728f, -0.3738f, -0.3754f, -0.3775f, -0.3802f, -0.3835f, -0.3872f, -0.3912f, -0.3956f, -0.4003f, -0.4051f };

    [MenuItem("Tools/NPC1/Buffer/Generate Nod")]
    public static void GenerateNod()
    {
        var clip = MakeClip("Buffer_Nod", TOTAL_FRAMES, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, TOTAL_FRAMES);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        L(BODY, "localPosition.x", 0f); L(BODY, "localPosition.y", 0f); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);
        L(HEAD, "localPosition.x", -0.093f); C(HEAD, "localPosition.y", HEAD_Y); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT);
        L(SF, "localPosition.x", -0.254f); L(SF, "localPosition.y", 0.324f); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.105f); L(HF, "localPosition.y", -0.234f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.043f); L(SB, "localPosition.y", 0.340f); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.130f); L(HB, "localPosition.y", -0.250f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(BOTTOM, "localPosition.x", 0f); L(BOTTOM, "localPosition.y", -0.410f); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.255f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.041f); L(FOOT_B, "localPosition.y", 0f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

        Save(clip, "Buffer_Nod");
    }

    [MenuItem("Tools/NPC1/Buffer/Generate IdleNod")]
    public static void GenerateIdleNod()
    {
        var clip = MakeClip("Buffer_IdleNod", 48, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 48);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, 48 / 12f, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", IW_BODY_Y); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);
        L(HEAD, "localPosition.x", -0.093f); C(HEAD, "localPosition.y", IDLE_NOD_HEAD_Y); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", IDLE_NOD_HEAD_ROT);
        L(SF, "localPosition.x", -0.254f); C(SF, "localPosition.y", IW_SF_Y); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", -0.105f); L(HF, "localPosition.y", -0.234f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);
        L(SB, "localPosition.x", 0.043f); C(SB, "localPosition.y", IW_SB_Y); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.130f); L(HB, "localPosition.y", -0.250f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(BOTTOM, "localPosition.x", 0f); C(BOTTOM, "localPosition.y", IW_BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.255f); L(FOOT_F, "localPosition.y", 0f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.041f); L(FOOT_B, "localPosition.y", 0f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

        Save(clip, "Buffer_IdleNod");
    }

    static AnimationClip MakeClip(string name, int totalFrames, bool loop)
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "Buffer");
        }
        var clip = new AnimationClip { frameRate = FRAME_RATE };
        clip.name = name;
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, s);
        return clip;
    }

    static void SetCurve(AnimationClip clip, string path, string prop, float[] vals, int total)
    {
        var keys = new Keyframe[total];
        for (int i = 0; i < total; i++) keys[i] = new Keyframe(i / FRAME_RATE, vals[i]);
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < curve.keys.Length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
        }
        clip.SetCurve(path, typeof(Transform), prop, curve);
    }

    static void Save(AnimationClip clip, string name)
    {
        string assetPath = $"{SAVE_PATH}/{name}.anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null) { EditorUtility.CopySerialized(clip, existing); existing.name = name; AssetDatabase.SaveAssets(); }
        else { AssetDatabase.CreateAsset(clip, assetPath); AssetDatabase.SaveAssets(); }
        AssetDatabase.Refresh();
        Debug.Log($"[Buffer] {name} → {assetPath}");
    }
}