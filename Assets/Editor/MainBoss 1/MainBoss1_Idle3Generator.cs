using UnityEngine;
using UnityEditor;
using System.IO;

public class MainBoss1_Idle3Generator
{
    const string SAVE_PATH  = "Assets/Animations/MainBoss 1";
    const string CLIP_NAME  = "MainBoss1_Idle3";
    const float  FRAME_RATE = 12f;
    const int    N          = 32;
    const float  DUR        = N / FRAME_RATE;

    const string BODY    = "Body";
    const string HEAD    = "Body/Head";
    const string BOTTOM  = "Body/Bottom";
    const string SF      = "Body/Shoulder_F";
    const string FF      = "Body/Shoulder_F/Forearm_F";
    const string SB      = "Body/Shoulder_B";
    const string FB      = "Body/Shoulder_B/Forearm_B";
    const string HB      = "Body/Shoulder_B/Forearm_B/Hand_B";
    const string WP      = "Body/Shoulder_B/Forearm_B/Hand_B/Weapon";
    const string THIGH_F = "Body/Bottom/Thigh_F";
    const string CALF_F  = "Body/Bottom/Thigh_F/Calf_F";
    const string THIGH_B = "Body/Bottom/Thigh_B";
    const string CALF_B  = "Body/Bottom/Thigh_B/Calf_B";

    // ================================================================
    // 자세 컨셉: 몸이 뒤로 젖혀지고 무릎이 앞으로 튀어나온 상태
    //
    // Body    rot = -18  (뒤로 크게 젖혀짐)
    // Head    rot = +12  (뒤로 젖혀진 몸에서 고개는 앞을 보려고 앞으로)
    // SF      rot = -20  (팔이 중력에 의해 앞으로 늘어짐)
    // FF      rot = -15
    // SB      rot = -10
    // FB      rot = +8
    // HB      rot = -20  (검 방향 유지)
    // Weapon  rot = -90  (거의 고정)
    // Thigh_F rot = +22  (무릎 앞으로 크게 튀어나옴)
    // Calf_F  rot = -30  (종아리 아래로 처짐)
    // Thigh_B rot = +18
    // Calf_B  rot = -25
    // Bottom  pos Y: Body 뒤로 젖혀짐 보정
    //
    // 고개 까딱: 불규칙 타이밍
    // ================================================================

    static float[] F(float b, params (int i, float v)[] o)
    {
        var a = new float[N];
        for (int i = 0; i < N; i++) a[i] = b;
        foreach (var (i, v) in o) if (i < N) a[i] = v;
        return a;
    }

    // Body: 뒤로 젖힌 채 고정, 까딱에 미세 반응
    static readonly float[] BODY_ROT = F(-18f,
        (5, -17f),(6, -18f),
        (14,-17f),(15,-18f),
        (23,-17f),(24,-18f));

    // Head: 앞을 보려고 +12, 까딱까딱
    static readonly float[] HEAD_ROT = F(12f,
        (4,  24f),(5,  14f),(6,  12f),    // 까딱1: 더 앞으로
        (13,  2f),(14, 10f),(15, 12f),    // 까딱2: 뒤로 확
        (22, 26f),(23, 14f),(24, 12f),    // 까딱3: 다시 앞으로 크게
        (29,  4f),(30, 10f),(31, 12f));   // 까딱4: 뒤로

    static readonly float[] HEAD_X = F(0f,
        (4,-0.05f),(5,-0.02f),
        (13, 0.03f),(14, 0.01f),
        (22,-0.06f),(23,-0.02f),
        (29, 0.04f),(30, 0.01f));

    // SF+FF: 중력에 앞으로 늘어짐, 고정
    static readonly float[] SF_ROT = F(-20f);
    static readonly float[] FF_ROT = F(-15f);

    // SB+FB: 검 무게로 살짝 처짐, 고정
    static readonly float[] SB_ROT = F(-10f);
    static readonly float[] FB_ROT = F(8f);

    // Hand_B: SB+FB 보정
    static readonly float[] HB_ROT = F(-20f,
        (4,-21f),(5,-20f),
        (13,-19f),(14,-20f));

    static readonly float[] WP_ROT = F(-90f);

    // Thigh: 무릎 앞으로 + Body rot 상쇄(-18 → +18)
    static readonly float[] TF_ROT = F(22f,
        (5, 21f),(6, 22f),
        (14,21f),(15,22f),
        (23,21f),(24,22f));

    static readonly float[] CF_ROT = F(-30f);

    static readonly float[] TB_ROT = F(18f,
        (5, 17f),(6, 18f),
        (14,17f),(15,18f),
        (23,17f),(24,18f));

    static readonly float[] CB_ROT = F(-25f);

    [MenuItem("Tools/MainBoss 1/Generate Idle3 (Leaning Back)")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            AssetDatabase.CreateFolder("Assets/Animations", "MainBoss 1");
        }

        var clip = new AnimationClip { frameRate = FRAME_RATE };
        clip.name = CLIP_NAME;
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        void C(string path, string prop, float[] vals)
        {
            var keys = new Keyframe[N + 1];
            for (int i = 0; i < N; i++) keys[i] = new Keyframe(i / FRAME_RATE, vals[i]);
            keys[N] = new Keyframe(DUR, vals[0]);
            var curve = new AnimationCurve(keys);
            for (int i = 0; i < curve.keys.Length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve,  i, AnimationUtility.TangentMode.Auto);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            }
            clip.SetCurve(path, typeof(Transform), prop, curve);
        }

        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop,
                AnimationCurve.Linear(0f, val, DUR, val));

        L(BODY, "localPosition.x",    0f);
        L(BODY, "localPosition.y",    0f);
        L(BODY, "localPosition.z",    0f);
        C(BODY, "localEulerAngles.z", BODY_ROT);

        C(HEAD, "localPosition.x",    HEAD_X);
        L(HEAD, "localPosition.y",    0.70f);
        L(HEAD, "localPosition.z",    0f);
        C(HEAD, "localEulerAngles.z", HEAD_ROT);

        L(BOTTOM, "localPosition.x", -0.20f);
        L(BOTTOM, "localPosition.y", -0.30f);
        L(BOTTOM, "localPosition.z",  0f);

        L(SF, "localPosition.x",   -0.575f);
        L(SF, "localPosition.y",    0.50f);
        L(SF, "localPosition.z",   0f);
        C(SF, "localEulerAngles.z", SF_ROT);
        C(FF, "localEulerAngles.z", FF_ROT);

        L(SB, "localPosition.x",   0.26f);
        L(SB, "localPosition.y",   0.55f);
        L(SB, "localPosition.z",   0f);
        C(SB, "localEulerAngles.z", SB_ROT);
        C(FB, "localEulerAngles.z", FB_ROT);
        C(HB, "localEulerAngles.z", HB_ROT);
        C(WP, "localEulerAngles.z", WP_ROT);

        C(THIGH_F, "localEulerAngles.z", TF_ROT);
        C(CALF_F,  "localEulerAngles.z", CF_ROT);
        C(THIGH_B, "localEulerAngles.z", TB_ROT);
        C(CALF_B,  "localEulerAngles.z", CB_ROT);

        string assetPath = $"{SAVE_PATH}/{CLIP_NAME}.anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null) { EditorUtility.CopySerialized(clip, existing); existing.name = CLIP_NAME; AssetDatabase.SaveAssets(); }
        else { AssetDatabase.CreateAsset(clip, assetPath); AssetDatabase.SaveAssets(); }
        AssetDatabase.Refresh();
        Debug.Log($"[MainBoss 1] {CLIP_NAME} -> {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}
