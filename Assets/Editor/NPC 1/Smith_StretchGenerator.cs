using UnityEngine;
using UnityEditor;

public class Smith_StretchGenerator
{
    const string SAVE_PATH = "Assets/Animations/Smith";
    const string CLIP_NAME = "Smith_Stretch";
    const float FRAME_RATE = 12f;
    const int TOTAL_FRAMES = 90;
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
    // 어깨 스트레칭 (48프레임, loopTime=false)
    // 실측 자세: SF=+30, HF=+60, SB=+155
    //
    // f0~f6  : rest → 자세 진입
    // f7~f28 : 어깨 사인파로 돌리기 (2주기)
    // f29~f35: 자세 유지
    // f36~f47: rest 복귀
    // ================================================================

    static readonly float[] SF_ROT = { 0.0f, 9.0f, 18.0f, 27.0f, 36.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 36.0f, 27.0f, 18.0f, 9.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] HB_ROT = { -0.0f, -5.0f, -10.0f, -15.0f, -20.0f, -25.0f, -30.0f, -35.0f, -40.0f, -45.0f, -45.0f, -40.7f, -36.4f, -32.1f, -27.9f, -23.6f, -19.3f, -15.0f, -15.0f, -19.3f, -23.6f, -27.9f, -32.1f, -36.4f, -40.7f, -45.0f, -45.0f, -40.7f, -36.4f, -32.1f, -27.9f, -23.6f, -19.3f, -15.0f, -15.0f, -19.3f, -23.6f, -27.9f, -32.1f, -36.4f, -40.7f, -45.0f, -45.0f, -42.0f, -39.0f, -36.0f, -33.0f, -30.0f, -27.0f, -24.0f, -21.0f, -18.0f, -15.0f, -12.0f, -9.0f, -6.0f, -3.0f, -0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] SB_ROT = { 0.0f, 22.2f, 44.4f, 66.7f, 88.9f, 111.1f, 133.3f, 155.6f, 177.8f, 200.0f, 215.0f, 195.0f, 183.0f, 171.0f, 160.0f, 151.0f, 145.0f, 140.0f, 140.0f, 145.0f, 153.0f, 164.0f, 176.0f, 188.0f, 200.0f, 220.0f, 220.0f, 200.0f, 188.0f, 176.0f, 165.0f, 156.0f, 150.0f, 145.0f, 145.0f, 150.0f, 158.0f, 169.0f, 181.0f, 193.0f, 205.0f, 225.0f, 246.8f, 268.4f, 289.4f, 309.6f, 328.7f, 346.6f, 362.9f, 377.5f, 390.1f, 400.7f, 409.1f, 415.1f, 418.8f, 420.0f, 420.0f, 420.0f, 420.0f, 412.8f, 399.6f, 382.4f, 362.2f, 339.2f, 313.7f, 286.1f, 256.4f, 224.8f, 191.4f, 156.2f, 119.5f, 81.1f, 41.3f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] HF_ROT = { 0.0f, 11.0f, 22.0f, 33.0f, 44.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 55.0f, 44.0f, 33.0f, 22.0f, 11.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] HEAD_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.85f, 1.62f, 2.27f, 2.73f, 2.97f, 2.97f, 2.73f, 2.27f, 1.62f, 0.85f, 0.0f, -0.85f, -1.62f, -2.27f, -2.73f, -2.97f, -2.97f, -2.73f, -2.27f, -1.62f, -0.85f, -0.0f, -0.72f, -1.39f, -1.99f, -2.47f, -2.81f, -2.98f, -2.98f, -2.81f, -2.47f, -1.99f, -1.39f, -0.72f, -0.0f, 0.72f, 1.39f, 1.99f, 2.47f, 2.81f, 2.98f, 2.98f, 2.81f, 2.47f, 1.99f, 1.39f, 0.72f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

    [MenuItem("Tools/Smith/Generate Stretch")]
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

        // Body (고정)
        L(BODY, "localPosition.x", 0f); L(BODY, "localPosition.y", 0f); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);

        // Head
        L(HEAD, "localPosition.x", -0.170f); L(HEAD, "localPosition.y", 0.710f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT);

        // Shoulder_F / Hand_F
        L(SF, "localPosition.x", -0.610f); L(SF, "localPosition.y", 0.536f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", SF_ROT);
        L(HF, "localPosition.x", 0f); L(HF, "localPosition.y", -0.317f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", HF_ROT);

        // Shoulder_B / Hand_B
        L(SB, "localPosition.x", 0.137f); L(SB, "localPosition.y", 0.536f); L(SB, "localPosition.z", 0f);
        // SB: CLinear 탄젠트로 360도 랩 방지
        {
            var keys = new Keyframe[TOTAL_FRAMES];
            for (int i = 0; i < TOTAL_FRAMES; i++)
                keys[i] = new Keyframe(i / FRAME_RATE, SB_ROT[i]);
            var curve = new AnimationCurve(keys);
            for (int i = 0; i < curve.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
            clip.SetCurve(SB, typeof(Transform), "localEulerAngles.z", curve);
        }
        L(HB, "localPosition.x", 0.079f); L(HB, "localPosition.y", -0.300f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", HB_ROT);

        // Bottom / Foot (고정)
        L(BOTTOM, "localPosition.x", 0f); L(BOTTOM, "localPosition.y", -0.300f); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.480f); L(FOOT_F, "localPosition.y", -0.190f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.005f); L(FOOT_B, "localPosition.y", -0.190f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

        string assetPath = $"{SAVE_PATH}/{CLIP_NAME}.anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null) { EditorUtility.CopySerialized(clip, existing); existing.name = CLIP_NAME; AssetDatabase.SaveAssets(); }
        else { AssetDatabase.CreateAsset(clip, assetPath); AssetDatabase.SaveAssets(); }
        AssetDatabase.Refresh();
        Debug.Log($"[Smith] {CLIP_NAME} → {assetPath}");
    }
}