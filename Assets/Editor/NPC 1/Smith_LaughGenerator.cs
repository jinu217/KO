using UnityEngine;
using UnityEditor;

public class Smith_LaughGenerator
{
    const string SAVE_PATH    = "Assets/Animations/Smith";
    const string CLIP_NAME    = "Smith_Laugh";
    const float  FRAME_RATE   = 12f;
    const int    TOTAL_FRAMES = 48;
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
    // 양 허리에 손 얹고 호탕하게 웃기
    // 실측: HF pos(-0.116,-0.338) rot=+35, HB pos(0.225,-0.304) rot=-35
    //
    // Body Y: 웃을 때 아래위로 통통 튀는 느낌
    // Head rot: Body와 반대로 (앞으로 숙였다 뒤로 젖히기)
    // Shoulder: 호탕하게 약간 같이 흔들림
    // ================================================================

    // Body Y: 통통 튀기 (4주기)
    static readonly float[] BODY_Y = {
        0.0f,0.0196f,0.0378f,0.0535f,0.0655f,0.0724f,0.0731f,0.0671f,
        0.0549f,0.0379f,0.0178f,-0.0031f,-0.0224f,-0.0378f,-0.0474f,-0.0497f,
        -0.0443f,-0.0316f,-0.0132f,0.0071f,0.0262f,0.0416f,0.0512f,0.0536f,
        0.0482f,0.0357f,0.0175f,-0.0026f,-0.0214f,-0.0369f,-0.0464f,-0.0486f,
        -0.0430f,-0.0302f,-0.0118f,0.0085f,0.0275f,0.0427f,0.0520f,0.0540f,
        0.0482f,0.0353f,0.0168f,-0.0034f,-0.0220f,-0.0372f,-0.0462f,-0.0480f};

    // Bottom: Body Y 상쇄
    static readonly float[] BOTTOM_Y = {
        -0.3f,-0.3196f,-0.3378f,-0.3535f,-0.3655f,-0.3724f,-0.3731f,-0.3671f,
        -0.3549f,-0.3379f,-0.3178f,-0.2969f,-0.2776f,-0.2622f,-0.2526f,-0.2503f,
        -0.2557f,-0.2684f,-0.2868f,-0.3071f,-0.3262f,-0.3416f,-0.3512f,-0.3536f,
        -0.3482f,-0.3357f,-0.3175f,-0.2974f,-0.2786f,-0.2631f,-0.2536f,-0.2514f,
        -0.257f,-0.2698f,-0.2882f,-0.3085f,-0.3275f,-0.3427f,-0.352f,-0.354f,
        -0.3482f,-0.3353f,-0.3168f,-0.2966f,-0.278f,-0.2628f,-0.2538f,-0.252f};

    // Body rot: 앞으로 숙였다 뒤로 (±8°, 4주기)
    static readonly float[] BODY_ROT = {
        0.0f,-2.83f,-5.66f,-7.07f,-8.0f,-7.07f,-5.66f,-2.83f,
        0.0f,2.83f,5.66f,7.07f,8.0f,7.07f,5.66f,2.83f,
        0.0f,-2.83f,-5.66f,-7.07f,-8.0f,-7.07f,-5.66f,-2.83f,
        0.0f,2.83f,5.66f,7.07f,8.0f,7.07f,5.66f,2.83f,
        0.0f,-2.83f,-5.66f,-7.07f,-8.0f,-7.07f,-5.66f,-2.83f,
        0.0f,2.83f,5.66f,7.07f,8.0f,7.07f,5.66f,2.83f};

    // Bottom rot: Body 상쇄
    static readonly float[] BOT_ROT = {
        0.0f,2.83f,5.66f,7.07f,8.0f,7.07f,5.66f,2.83f,
        0.0f,-2.83f,-5.66f,-7.07f,-8.0f,-7.07f,-5.66f,-2.83f,
        0.0f,2.83f,5.66f,7.07f,8.0f,7.07f,5.66f,2.83f,
        0.0f,-2.83f,-5.66f,-7.07f,-8.0f,-7.07f,-5.66f,-2.83f,
        0.0f,2.83f,5.66f,7.07f,8.0f,7.07f,5.66f,2.83f,
        0.0f,-2.83f,-5.66f,-7.07f,-8.0f,-7.07f,-5.66f,-2.83f};

    // Head rot: Body 반대로 (+앞숙임이 Body 뒤젖힐때)
    static readonly float[] HEAD_ROT = {
        0.0f,3.54f,7.07f,8.84f,10.0f,8.84f,7.07f,3.54f,
        0.0f,-3.54f,-7.07f,-8.84f,-10.0f,-8.84f,-7.07f,-3.54f,
        0.0f,3.54f,7.07f,8.84f,10.0f,8.84f,7.07f,3.54f,
        0.0f,-3.54f,-7.07f,-8.84f,-10.0f,-8.84f,-7.07f,-3.54f,
        0.0f,3.54f,7.07f,8.84f,10.0f,8.84f,7.07f,3.54f,
        0.0f,-3.54f,-7.07f,-8.84f,-10.0f,-8.84f,-7.07f,-3.54f};

    // SF rot: 웃을 때 살짝 같이 들썩 (±5°)
    static readonly float[] SF_ROT = {
        0.0f,1.77f,3.54f,4.42f,5.0f,4.42f,3.54f,1.77f,
        0.0f,-1.77f,-3.54f,-4.42f,-5.0f,-4.42f,-3.54f,-1.77f,
        0.0f,1.77f,3.54f,4.42f,5.0f,4.42f,3.54f,1.77f,
        0.0f,-1.77f,-3.54f,-4.42f,-5.0f,-4.42f,-3.54f,-1.77f,
        0.0f,1.77f,3.54f,4.42f,5.0f,4.42f,3.54f,1.77f,
        0.0f,-1.77f,-3.54f,-4.42f,-5.0f,-4.42f,-3.54f,-1.77f};

    static readonly float[] SB_ROT = {
        0.0f,-1.77f,-3.54f,-4.42f,-5.0f,-4.42f,-3.54f,-1.77f,
        0.0f,1.77f,3.54f,4.42f,5.0f,4.42f,3.54f,1.77f,
        0.0f,-1.77f,-3.54f,-4.42f,-5.0f,-4.42f,-3.54f,-1.77f,
        0.0f,1.77f,3.54f,4.42f,5.0f,4.42f,3.54f,1.77f,
        0.0f,-1.77f,-3.54f,-4.42f,-5.0f,-4.42f,-3.54f,-1.77f,
        0.0f,1.77f,3.54f,4.42f,5.0f,4.42f,3.54f,1.77f};

    [MenuItem("Tools/Smith/Generate Laugh")]
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
        L(HEAD, "localPosition.y",  0.710f);
        L(HEAD, "localPosition.z",  0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT);

        // Shoulder_F / Hand_F (허리 자세 고정, 어깨만 살짝)
        L(SF, "localPosition.x", -0.610f);
        L(SF, "localPosition.y",  0.536f);
        L(SF, "localPosition.z",  0f);
        C(SF, "localEulerAngles.z", SF_ROT);
        L(HF, "localPosition.x", -0.116f);
        L(HF, "localPosition.y", -0.338f);
        L(HF, "localPosition.z",  0f);
        L(HF, "localEulerAngles.z", 35f);

        // Shoulder_B / Hand_B
        L(SB, "localPosition.x",  0.137f);
        L(SB, "localPosition.y",  0.536f);
        L(SB, "localPosition.z",  0f);
        C(SB, "localEulerAngles.z", SB_ROT);
        L(HB, "localPosition.x",  0.225f);
        L(HB, "localPosition.y", -0.304f);
        L(HB, "localPosition.z",  0f);
        L(HB, "localEulerAngles.z", -35f);

        // Bottom
        L(BOTTOM, "localPosition.x",  0f);
        C(BOTTOM, "localPosition.y", BOTTOM_Y);
        L(BOTTOM, "localPosition.z",  0f);
        C(BOTTOM, "localEulerAngles.z", BOT_ROT);

        // Foot (고정)
        L(FOOT_F, "localPosition.x", -0.480f);
        L(FOOT_F, "localPosition.y", -0.190f);
        L(FOOT_F, "localPosition.z",  0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x",  0.005f);
        L(FOOT_B, "localPosition.y", -0.190f);
        L(FOOT_B, "localPosition.z",  0f);
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
