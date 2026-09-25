using UnityEngine;
using UnityEditor;

public class Smith_LaughGenerator
{
    const string SAVE_PATH = "Assets/Animations/Smith";
    const string CLIP_NAME = "Smith_Laugh";
    const float FRAME_RATE = 12f;
    const int TOTAL_FRAMES = 24;
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
    // 양 허리에 손 얹고 호탕하게 웃기
    // 실측: HF pos(-0.116,-0.338) rot=+35, HB pos(0.225,-0.304) rot=-35
    //
    // Body Y: 웃을 때 아래위로 통통 튀는 느낌
    // Head rot: Body와 반대로 (앞으로 숙였다 뒤로 젖히기)
    // Shoulder: 호탕하게 약간 같이 흔들림
    // ================================================================

    // Body Y: 통통 튀기 (4주기)
    static readonly float[] BODY_Y = { 0.0f, 0.0353f, 0.0571f, 0.0571f, 0.0353f, 0.0f, 0.0353f, 0.0571f, 0.0571f, 0.0353f, 0.0f, 0.0353f, 0.0571f, 0.0571f, 0.0353f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

    // Bottom: Body Y 상쇄
    static readonly float[] BOTTOM_Y = { -0.3f, -0.3353f, -0.3571f, -0.3571f, -0.3353f, -0.3f, -0.3353f, -0.3571f, -0.3571f, -0.3353f, -0.3f, -0.3353f, -0.3571f, -0.3571f, -0.3353f, -0.3f, -0.3f, -0.3f, -0.3f, -0.3f, -0.3f, -0.3f, -0.3f, -0.3f };

    // Body rot: 앞으로 숙였다 뒤로 (±8°, 4주기)
    static readonly float[] BODY_ROT = { 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 6.0f, 4.0f, 2.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

    // Bottom rot: Body 상쇄
    static readonly float[] BOT_ROT = { -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -6.0f, -4.0f, -2.0f, -0.0f, -0.0f, -0.0f, -0.0f, -0.0f };

    // Head rot: Body 반대로 (+앞숙임이 Body 뒤젖힐때)
    static readonly float[] HEAD_ROT = { 10.0f, 13.53f, 15.71f, 15.71f, 13.53f, 10.0f, 13.53f, 15.71f, 15.71f, 13.53f, 10.0f, 13.53f, 15.71f, 15.71f, 13.53f, 10.0f, 7.5f, 5.0f, 2.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

    // SF rot: 웃을 때 살짝 같이 들썩 (±5°)
    static readonly float[] SF_ROT = { 0.0f, 2.35f, 3.8f, 3.8f, 2.35f, 0.0f, 2.35f, 3.8f, 3.8f, 2.35f, 0.0f, 2.35f, 3.8f, 3.8f, 2.35f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

    static readonly float[] SB_ROT = { -0.0f, -2.35f, -3.8f, -3.8f, -2.35f, -0.0f, -2.35f, -3.8f, -3.8f, -2.35f, -0.0f, -2.35f, -3.8f, -3.8f, -2.35f, -0.0f, -0.0f, -0.0f, -0.0f, -0.0f, -0.0f, -0.0f, -0.0f, -0.0f };

    [MenuItem("Tools/NPC1/Smith/Generate Laugh")]
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

        // Body
        L(BODY, "localPosition.x", 0f);
        C(BODY, "localPosition.y", BODY_Y);
        L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", BODY_ROT);

        // Head
        L(HEAD, "localPosition.x", -0.170f);
        L(HEAD, "localPosition.y", 0.710f);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT);

        // Shoulder_F / Hand_F (허리 자세 고정, 어깨만 살짝)
        L(SF, "localPosition.x", -0.610f);
        L(SF, "localPosition.y", 0.536f);
        L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", SF_ROT);
        // HF: 허리→rest 복귀
        var hfXKeys = new Keyframe[TOTAL_FRAMES];
        var hfYKeys = new Keyframe[TOTAL_FRAMES];
        var hfRKeys = new Keyframe[TOTAL_FRAMES];
        float[] hfX = { -0.116f, -0.116f, -0.116f, -0.116f, -0.116f, -0.116f, -0.116f, -0.116f, -0.116f, -0.116f, -0.116f, -0.116f, -0.116f, -0.116f, -0.116f, -0.116f, -0.087f, -0.058f, -0.029f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
        float[] hfY = { -0.338f, -0.338f, -0.338f, -0.338f, -0.338f, -0.338f, -0.338f, -0.338f, -0.338f, -0.338f, -0.338f, -0.338f, -0.338f, -0.338f, -0.338f, -0.338f, -0.332f, -0.326f, -0.320f, -0.317f, -0.317f, -0.317f, -0.317f, -0.317f };
        float[] hfR = { 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 26.25f, 17.5f, 8.75f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
        for (int i = 0; i < TOTAL_FRAMES; i++) { hfXKeys[i] = new Keyframe(i / FRAME_RATE, hfX[i]); hfYKeys[i] = new Keyframe(i / FRAME_RATE, hfY[i]); hfRKeys[i] = new Keyframe(i / FRAME_RATE, hfR[i]); }
        clip.SetCurve(HF, typeof(Transform), "localPosition.x", new AnimationCurve(hfXKeys));
        clip.SetCurve(HF, typeof(Transform), "localPosition.y", new AnimationCurve(hfYKeys));
        L(HF, "localPosition.z", 0f);
        clip.SetCurve(HF, typeof(Transform), "localEulerAngles.z", new AnimationCurve(hfRKeys));

        // Shoulder_B / Hand_B
        L(SB, "localPosition.x", 0.137f);
        L(SB, "localPosition.y", 0.536f);
        L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", SB_ROT);
        // HB: 허리→rest 복귀
        var hbXKeys = new Keyframe[TOTAL_FRAMES];
        var hbYKeys = new Keyframe[TOTAL_FRAMES];
        var hbRKeys = new Keyframe[TOTAL_FRAMES];
        float[] hbX = { 0.225f, 0.225f, 0.225f, 0.225f, 0.225f, 0.225f, 0.225f, 0.225f, 0.225f, 0.225f, 0.225f, 0.225f, 0.225f, 0.225f, 0.225f, 0.225f, 0.186f, 0.147f, 0.108f, 0.079f, 0.079f, 0.079f, 0.079f, 0.079f };
        float[] hbY = { -0.304f, -0.304f, -0.304f, -0.304f, -0.304f, -0.304f, -0.304f, -0.304f, -0.304f, -0.304f, -0.304f, -0.304f, -0.304f, -0.304f, -0.304f, -0.304f, -0.303f, -0.302f, -0.301f, -0.300f, -0.300f, -0.300f, -0.300f, -0.300f };
        float[] hbR = { -35.0f, -35.0f, -35.0f, -35.0f, -35.0f, -35.0f, -35.0f, -35.0f, -35.0f, -35.0f, -35.0f, -35.0f, -35.0f, -35.0f, -35.0f, -35.0f, -26.25f, -17.5f, -8.75f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
        for (int i = 0; i < TOTAL_FRAMES; i++) { hbXKeys[i] = new Keyframe(i / FRAME_RATE, hbX[i]); hbYKeys[i] = new Keyframe(i / FRAME_RATE, hbY[i]); hbRKeys[i] = new Keyframe(i / FRAME_RATE, hbR[i]); }
        clip.SetCurve(HB, typeof(Transform), "localPosition.x", new AnimationCurve(hbXKeys));
        clip.SetCurve(HB, typeof(Transform), "localPosition.y", new AnimationCurve(hbYKeys));
        L(HB, "localPosition.z", 0f);
        clip.SetCurve(HB, typeof(Transform), "localEulerAngles.z", new AnimationCurve(hbRKeys));

        // Bottom
        L(BOTTOM, "localPosition.x", 0f);
        C(BOTTOM, "localPosition.y", BOTTOM_Y);
        L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", BOT_ROT);

        // Foot (고정)
        L(FOOT_F, "localPosition.x", -0.480f);
        L(FOOT_F, "localPosition.y", -0.190f);
        L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
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
    }
}