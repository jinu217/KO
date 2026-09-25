using UnityEngine;
using UnityEditor;

public class Smith_NodGenerators
{
    const string SAVE_PATH = "Assets/Animations/Smith";
    const string CLIP_NAME = "Smith_Nod";
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

    // ================================================================
    // 두 번 끄덕이기 (24프레임, loopTime=false)
    //
    // f0       : rest
    // f3~f5    : 1차 아래로
    // f7~f8    : 1차 복귀
    // f11~f13  : 2차 아래로 (조금 더 작게)
    // f15~f16  : 2차 복귀
    // f16~f23  : rest 유지
    //
    // Head rot 아래로 = 양수
    // Body는 아주 살짝 같이 움직임 (대장장이 묵직한 느낌)
    // ================================================================

    static float[] F(float b, params (int i, float v)[] o)
    {
        var a = new float[TOTAL_FRAMES];
        for (int i = 0; i < TOTAL_FRAMES; i++) a[i] = b;
        foreach (var (i, v) in o) if (i < TOTAL_FRAMES) a[i] = v;
        return a;
    }

    // Head rot: 아래로 먼저 숙이기
    static readonly float[] HEAD_ROT = F(0f,
        (0, -0f),
        // 1차 끄덕: 빠르게 내려갔다 복귀
        (2, -6f), (3, -12f), (4, -8f),
        (5, -2f), (6, -0f),
        // 2차 끄덕 (살짝 작게)
        (8, -4f), (9, -8f), (10, -5f),
        (11, -1f), (12, -0f), (23, -0f));

    // Head Y: 끄덕일 때 살짝 내려감
    static readonly float[] HEAD_Y = F(0.710f,
        (0, 0.710f),
        (3, 0.704f), (4, 0.707f),
        (5, 0.710f),
        (9, 0.705f), (10, 0.708f),
        (11, 0.710f), (23, 0.710f));

    // Body: 고정 (끄덕임은 Head만)
    // Bottom도 고정 (Body rot=0이라 상쇄 불필요)

    [MenuItem("Tools/NPC1/Smith/Generate Nod")]
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
        L(BODY, "localPosition.y", 0f);
        L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);

        // Head
        L(HEAD, "localPosition.x", -0.170f);
        C(HEAD, "localPosition.y", HEAD_Y);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT);

        // Shoulder_F / Hand_F (고정)
        L(SF, "localPosition.x", -0.610f);
        L(SF, "localPosition.y", 0.536f);
        L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", 0f);
        L(HF, "localPosition.y", -0.317f);
        L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        // Shoulder_B / Hand_B (고정)
        L(SB, "localPosition.x", 0.137f);
        L(SB, "localPosition.y", 0.536f);
        L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.079f);
        L(HB, "localPosition.y", -0.300f);
        L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);

        // Bottom / Foot (고정)
        L(BOTTOM, "localPosition.x", 0f);
        L(BOTTOM, "localPosition.y", -0.300f);
        L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);
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
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }

    // Body: Head와 같이 살짝 따라감
    static readonly float[] BODY_ROT = F(0f,
        (0, 0f),
        (3, 2f), (4, 3f),
        (5, 1f), (6, 0f),
        (9, 1f), (10, 2f),
        (11, 0f), (23, 0f));

    // Bottom: Body rot 상쇄
    static readonly float[] BOT_ROT = F(0f,
        (0, 0f),
        (3, -2f), (4, -3f),
        (5, -1f), (6, 0f),
        (9, -1f), (10, -2f),
        (11, 0f), (23, 0f));

    [MenuItem("Tools/NPC1/Smith/Generate NodBody")]
    public static void GenerateNodBody()
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "Smith");
        }

        var clip = new AnimationClip { frameRate = FRAME_RATE };
        clip.name = "Smith_NodBody";
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
        L(HEAD, "localPosition.x", -0.170f);
        C(HEAD, "localPosition.y", HEAD_Y);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT);

        // Shoulder_F / Hand_F (고정)
        L(SF, "localPosition.x", -0.610f);
        L(SF, "localPosition.y", 0.536f);
        L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(HF, "localPosition.x", 0f);
        L(HF, "localPosition.y", -0.317f);
        L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        // Shoulder_B / Hand_B (고정)
        L(SB, "localPosition.x", 0.137f);
        L(SB, "localPosition.y", 0.536f);
        L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(HB, "localPosition.x", 0.079f);
        L(HB, "localPosition.y", -0.300f);
        L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);

        // Bottom / Foot (고정)
        L(BOTTOM, "localPosition.x", 0f);
        L(BOTTOM, "localPosition.y", -0.300f);
        L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", BOT_ROT);
        L(FOOT_F, "localPosition.x", -0.480f);
        L(FOOT_F, "localPosition.y", -0.190f);
        L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", 0.005f);
        L(FOOT_B, "localPosition.y", -0.190f);
        L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

        string assetPath = "Assets/Animations/Smith/Smith_NodBody.anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(clip, existing);
            existing.name = "Smith_NodBody";
            AssetDatabase.SaveAssets();
        }
        else
        {
            AssetDatabase.CreateAsset(clip, assetPath);
            AssetDatabase.SaveAssets();
        }
        AssetDatabase.Refresh();
        Debug.Log("[Smith] Smith_NodBody → Assets/Animations/Smith/Smith_NodBody.anim");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }

    // 숨쉬기 Body Y (IdleGenerator와 동일)
    static readonly float[] BODY_Y = { 0.0f, 0.0065f, 0.0129f, 0.0191f, 0.025f, 0.0304f, 0.0354f, 0.0397f, 0.0433f, 0.0462f, 0.0483f, 0.0496f, 0.05f, 0.0496f, 0.0483f, 0.0462f, 0.0433f, 0.0397f, 0.0354f, 0.0304f, 0.025f, 0.0191f, 0.0129f, 0.0065f, 0.0f, -0.0065f, -0.0129f, -0.0191f, -0.025f, -0.0304f, -0.0354f, -0.0397f, -0.0433f, -0.0462f, -0.0483f, -0.0496f, -0.05f, -0.0496f, -0.0483f, -0.0462f, -0.0433f, -0.0397f, -0.0354f, -0.0304f, -0.025f, -0.0191f, -0.0129f, -0.0065f };
    static readonly float[] BOTTOM_Y = { -0.3f, -0.3065f, -0.3129f, -0.3191f, -0.325f, -0.3304f, -0.3354f, -0.3397f, -0.3433f, -0.3462f, -0.3483f, -0.3496f, -0.35f, -0.3496f, -0.3483f, -0.3462f, -0.3433f, -0.3397f, -0.3354f, -0.3304f, -0.325f, -0.3191f, -0.3129f, -0.3065f, -0.3f, -0.2935f, -0.2871f, -0.2809f, -0.275f, -0.2696f, -0.2646f, -0.2603f, -0.2567f, -0.2538f, -0.2517f, -0.2504f, -0.25f, -0.2504f, -0.2517f, -0.2538f, -0.2567f, -0.2603f, -0.2646f, -0.2696f, -0.275f, -0.2809f, -0.2871f, -0.2935f };

    // 어깨 Y (8프레임 딜레이)
    static readonly float[] SF_Y = { 0.51f, 0.5122f, 0.5148f, 0.5177f, 0.521f, 0.5245f, 0.5282f, 0.5321f, 0.536f, 0.5399f, 0.5438f, 0.5475f, 0.551f, 0.5543f, 0.5572f, 0.5598f, 0.562f, 0.5637f, 0.565f, 0.5657f, 0.566f, 0.5657f, 0.565f, 0.5637f, 0.562f, 0.5598f, 0.5572f, 0.5543f, 0.551f, 0.5475f, 0.5438f, 0.5399f, 0.536f, 0.5321f, 0.5282f, 0.5245f, 0.521f, 0.5177f, 0.5148f, 0.5122f, 0.51f, 0.5083f, 0.507f, 0.5063f, 0.506f, 0.5063f, 0.507f, 0.5083f };
    static readonly float[] SB_Y = { 0.51f, 0.5122f, 0.5148f, 0.5177f, 0.521f, 0.5245f, 0.5282f, 0.5321f, 0.536f, 0.5399f, 0.5438f, 0.5475f, 0.551f, 0.5543f, 0.5572f, 0.5598f, 0.562f, 0.5637f, 0.565f, 0.5657f, 0.566f, 0.5657f, 0.565f, 0.5637f, 0.562f, 0.5598f, 0.5572f, 0.5543f, 0.551f, 0.5475f, 0.5438f, 0.5399f, 0.536f, 0.5321f, 0.5282f, 0.5245f, 0.521f, 0.5177f, 0.5148f, 0.5122f, 0.51f, 0.5083f, 0.507f, 0.5063f, 0.506f, 0.5063f, 0.507f, 0.5083f };

    // Head Y: 숨쉬기 + 끄덕임 합산
    // 숨쉬기 HEAD_Y에 끄덕임 오프셋 추가 (f4~f16)

    // Head rot (IdleNod용) - 아래로 먼저
    static readonly float[] IDLE_NOD_HEAD_ROT = { 0.0f, 0.0f, -0.0f, -4.59f, -8.49f, -11.09f, -12.0f, -11.09f, -8.49f, -4.59f, -0.0f, -3.83f, -7.07f, -9.24f, -10.0f, -9.24f, -7.07f, -3.83f, -0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

    // Head Y: 숨쉬기 + 끄덕임 합산
    // 숨쉬기 HEAD_Y에 끄덕임 오프셋 추가 (f4~f16)
    static readonly float[] IDLE_NOD_HEAD_Y = {
        0.71f,0.7133f,0.7165f,0.7196f,
        // f4~f6: 1차 끄덕 (아래로)
        0.7118f,0.7096f,0.7118f,
        // f7~f8: 복귀
        0.7252f,0.7277f,
        // f9~f11: 2차 끄덕
        0.7226f,0.7208f,0.7226f,
        // f12~: 숨쉬기로 복귀
        0.7220f,0.7219f,0.7216f,0.7211f,0.7204f,0.7195f,0.7185f,0.7173f,
        0.716f,0.7146f,0.7131f,0.7116f,0.71f,0.7084f,0.7069f,0.7054f,
        0.704f,0.7027f,0.7015f,0.7005f,0.6996f,0.6989f,0.6984f,0.6981f,
        0.698f,0.6981f,0.6984f,0.6989f,0.6996f,0.7005f,0.7015f,0.7027f,
        0.704f,0.7054f,0.7069f,0.7084f};



    [MenuItem("Tools/NPC1/Smith/Generate IdleNod")]
    public static void GenerateIdleNod()
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "Smith");
        }

        var clip = new AnimationClip { frameRate = FRAME_RATE };
        clip.name = "Smith_IdleNod";
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        void C(string path, string prop, float[] vals)
        {
            var keys = new Keyframe[48];
            for (int i = 0; i < 48; i++)
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
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, 4f, val));

        // Body
        L(BODY, "localPosition.x", 0f);
        C(BODY, "localPosition.y", BODY_Y);
        L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);

        // Head
        L(HEAD, "localPosition.x", -0.170f);
        C(HEAD, "localPosition.y", IDLE_NOD_HEAD_Y);
        L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", IDLE_NOD_HEAD_ROT);

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

        // Bottom
        L(BOTTOM, "localPosition.x", 0f);
        C(BOTTOM, "localPosition.y", BOTTOM_Y);
        L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);

        // Foot_F
        L(FOOT_F, "localPosition.x", -0.480f);
        L(FOOT_F, "localPosition.y", -0.190f);
        L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);

        // Foot_B
        L(FOOT_B, "localPosition.x", 0.005f);
        L(FOOT_B, "localPosition.y", -0.190f);
        L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

        string assetPath = "Assets/Animations/Smith/Smith_IdleNod.anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null)
        {
            EditorUtility.CopySerialized(clip, existing);
            existing.name = "Smith_IdleNod";
            AssetDatabase.SaveAssets();
        }
        else
        {
            AssetDatabase.CreateAsset(clip, assetPath);
            AssetDatabase.SaveAssets();
        }
        AssetDatabase.Refresh();
        Debug.Log("[Smith] Smith_IdleNod → Assets/Animations/Smith/Smith_IdleNod.anim");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}