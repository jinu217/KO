using UnityEngine;
using UnityEditor;

public class Smith_StretchGenerator
{
    const string SAVE_PATH = "Assets/Animations/Smith";
    const string CLIP_NAME = "Smith_Stretch";
    const float FRAME_RATE = 12f;
    const int TOTAL_FRAMES = 60;
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

    static readonly float[] SF_ROT = { 0.0f, 5.0f, 10.0f, 15.0f, 20.0f, 25.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f };
    static readonly float[] SB_ROT = { 0.0f, 25.83f, 51.67f, 77.5f, 103.33f, 129.17f, 155.0f, 186.78f, 218.36f, 249.55f, 280.15f, 309.99f, 338.87f, 366.61f, 393.05f, 418.03f, 441.38f, 462.96f, 482.65f, 500.32f, 515.86f, 529.17f, 540.18f, 548.81f, 555.01f, 558.75f, 560.0f, 545.0f, 534.02f, 530.0f, 534.02f, 545.0f, 560.0f, 497.32f, 464.88f, 432.94f, 401.74f, 371.52f, 342.5f, 314.91f, 288.95f, 264.83f, 242.73f, 222.82f, 205.24f, 190.13f, 177.62f, 167.78f, 160.7f, 156.43f, 155.0f, 142.5f, 133.35f, 130.0f, 133.35f, 142.5f, 155.0f, 103.33f, 51.67f, 0.0f };
    static readonly float[] HF_ROT = { 0.0f, 10.0f, 20.0f, 30.0f, 40.0f, 50.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f };
    static readonly float[] HEAD_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.72f, 1.39f, 1.99f, 2.47f, 2.81f, 2.98f, 2.98f, 2.81f, 2.47f, 1.99f, 1.39f, 0.72f, 0.0f, -0.72f, -1.39f, -1.99f, -2.47f, -2.81f, -2.98f, -2.98f, -2.81f, -2.47f, -1.99f, -1.39f, -0.72f, -0.0f, -0.78f, -1.5f, -2.12f, -2.6f, -2.9f, -3.0f, -2.9f, -2.6f, -2.12f, -1.5f, -0.78f, -0.0f, 0.78f, 1.5f, 2.12f, 2.6f, 2.9f, 3.0f, 2.9f, 2.6f, 2.12f, 1.5f, 0.78f, 0.0f, 0.0f, 0.0f, 0.0f };

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
        C(SB, "localEulerAngles.z", SB_ROT);
        L(HB, "localPosition.x", 0.079f); L(HB, "localPosition.y", -0.300f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);

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