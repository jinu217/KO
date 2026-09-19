using UnityEngine;
using UnityEditor;

public class Buffer_SwayGenerator
{
    const string SAVE_PATH    = "Assets/Animations/Buffer";
    const string CLIP_NAME    = "Buffer_Sway";
    const float  FRAME_RATE   = 12f;
    const int    TOTAL_FRAMES = 32;
    const float  DUR          = TOTAL_FRAMES / (float)FRAME_RATE;

    const string BODY   = "Body";
    const string HEAD   = "Body/Head";
    const string SF     = "Body/Shoulder_F";
    const string HF     = "Body/Shoulder_F/Hand_F";
    const string SB     = "Body/Shoulder_B";
    const string HB     = "Body/Shoulder_B/Hand_B";
    const string BOTTOM = "Body/Bottom";
    const string FOOT_F = "Body/Bottom/Foot_F";
    const string FOOT_B = "Body/Bottom/Foot_B";

    // ================================================================
    // 좌우 리듬 스윙 (32프레임 루프)
    // Body ±10° 좌우, Head 반대 ±5°, 어깨 살짝 ±3°
    // Bottom 상쇄, Body Y 살짝 위아래
    // ================================================================

    static readonly float[] BODY_ROT = {0.0f,3.83f,7.07f,9.24f,10.0f,9.24f,7.07f,3.83f,0.0f,-3.83f,-7.07f,-9.24f,-10.0f,-9.24f,-7.07f,-3.83f,-0.0f,3.83f,7.07f,9.24f,10.0f,9.24f,7.07f,3.83f,0.0f,-3.83f,-7.07f,-9.24f,-10.0f,-9.24f,-7.07f,-3.83f};
    static readonly float[] BOT_ROT  = {-0.0f,-3.83f,-7.07f,-9.24f,-10.0f,-9.24f,-7.07f,-3.83f,-0.0f,3.83f,7.07f,9.24f,10.0f,9.24f,7.07f,3.83f,0.0f,-3.83f,-7.07f,-9.24f,-10.0f,-9.24f,-7.07f,-3.83f,-0.0f,3.83f,7.07f,9.24f,10.0f,9.24f,7.07f,3.83f};
    static readonly float[] HEAD_ROT = {-0.0f,-1.91f,-3.54f,-4.62f,-5.0f,-4.62f,-3.54f,-1.91f,-0.0f,1.91f,3.54f,4.62f,5.0f,4.62f,3.54f,1.91f,0.0f,-1.91f,-3.54f,-4.62f,-5.0f,-4.62f,-3.54f,-1.91f,-0.0f,1.91f,3.54f,4.62f,5.0f,4.62f,3.54f,1.91f};
    static readonly float[] SF_ROT   = {0.0f,1.15f,2.12f,2.77f,3.0f,2.77f,2.12f,1.15f,0.0f,-1.15f,-2.12f,-2.77f,-3.0f,-2.77f,-2.12f,-1.15f,-0.0f,1.15f,2.12f,2.77f,3.0f,2.77f,2.12f,1.15f,0.0f,-1.15f,-2.12f,-2.77f,-3.0f,-2.77f,-2.12f,-1.15f};
    static readonly float[] SB_ROT   = {-0.0f,-1.15f,-2.12f,-2.77f,-3.0f,-2.77f,-2.12f,-1.15f,-0.0f,1.15f,2.12f,2.77f,3.0f,2.77f,2.12f,1.15f,0.0f,-1.15f,-2.12f,-2.77f,-3.0f,-2.77f,-2.12f,-1.15f,-0.0f,1.15f,2.12f,2.77f,3.0f,2.77f,2.12f,1.15f};
    static readonly float[] BODY_Y   = {0.0f,0.0077f,0.0141f,0.0185f,0.02f,0.0185f,0.0141f,0.0077f,0.0f,0.0077f,0.0141f,0.0185f,0.02f,0.0185f,0.0141f,0.0077f,0.0f,0.0077f,0.0141f,0.0185f,0.02f,0.0185f,0.0141f,0.0077f,0.0f,0.0077f,0.0141f,0.0185f,0.02f,0.0185f,0.0141f,0.0077f};
    static readonly float[] BOTTOM_Y = {-0.41f,-0.4177f,-0.4241f,-0.4285f,-0.43f,-0.4285f,-0.4241f,-0.4177f,-0.41f,-0.4177f,-0.4241f,-0.4285f,-0.43f,-0.4285f,-0.4241f,-0.4177f,-0.41f,-0.4177f,-0.4241f,-0.4285f,-0.43f,-0.4285f,-0.4241f,-0.4177f,-0.41f,-0.4177f,-0.4241f,-0.4285f,-0.43f,-0.4285f,-0.4241f,-0.4177f};

    [MenuItem("Tools/Buffer/Generate Sway")]
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
        C(BODY, "localEulerAngles.z", BODY_ROT);

        // Head
        L(HEAD, "localPosition.x", -0.093f);
        L(HEAD, "localPosition.y",  0.472f);
        L(HEAD, "localPosition.z",  0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT);

        // Shoulder_F / Hand_F
        L(SF, "localPosition.x", -0.254f);
        L(SF, "localPosition.y",  0.324f);
        L(SF, "localPosition.z",  0f);
        C(SF, "localEulerAngles.z", SF_ROT);
        L(HF, "localPosition.x", -0.105f);
        L(HF, "localPosition.y", -0.234f);
        L(HF, "localPosition.z",  0f);
        L(HF, "localEulerAngles.z", 0f);

        // Shoulder_B / Hand_B
        L(SB, "localPosition.x",  0.043f);
        L(SB, "localPosition.y",  0.340f);
        L(SB, "localPosition.z",  0f);
        C(SB, "localEulerAngles.z", SB_ROT);
        L(HB, "localPosition.x",  0.130f);
        L(HB, "localPosition.y", -0.250f);
        L(HB, "localPosition.z",  0f);
        L(HB, "localEulerAngles.z", 0f);

        // Bottom
        L(BOTTOM, "localPosition.x",  0f);
        C(BOTTOM, "localPosition.y", BOTTOM_Y);
        L(BOTTOM, "localPosition.z",  0f);
        C(BOTTOM, "localEulerAngles.z", BOT_ROT);

        // Foot (고정)
        L(FOOT_F, "localPosition.x", -0.255f);
        L(FOOT_F, "localPosition.y",  0f);
        L(FOOT_F, "localPosition.z",  0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x",  0.041f);
        L(FOOT_B, "localPosition.y",  0f);
        L(FOOT_B, "localPosition.z",  0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

        string assetPath = $"{SAVE_PATH}/{CLIP_NAME}.anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null) { EditorUtility.CopySerialized(clip, existing); existing.name = CLIP_NAME; AssetDatabase.SaveAssets(); }
        else { AssetDatabase.CreateAsset(clip, assetPath); AssetDatabase.SaveAssets(); }
        AssetDatabase.Refresh();
        Debug.Log($"[Buffer] {CLIP_NAME} → {assetPath}");
    }
}
