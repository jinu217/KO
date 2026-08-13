using UnityEngine;
using UnityEditor;
using System.IO;

public class MainBoss1_Idle2Generator
{
    const string SAVE_PATH  = "Assets/Animations/MainBoss 1";
    const string CLIP_NAME  = "MainBoss1_Idle2";
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
    // 컨셉: 몸 전체가 축 늘어진 상태로 고정
    //       고개만 불규칙하게 까딱까딱
    //       검은 거의 완전 고정
    //
    // 늘어진 자세 (base값):
    //   Body   rot = +8    (앞으로 크게 기울어짐)
    //   SF     rot = -15   (팔이 축 처짐)
    //   FF     rot = -25   (팔꿈치 더 처짐)
    //   SB     rot = +5    (검 든 팔도 살짝 처짐)
    //   FB     rot = +10
    //   Thigh_F/B rot = -8 (Body rot 상쇄 + 살짝 더)
    //   Calf_F rot = +6 / Calf_B rot = -5
    //
    // 고개 까딱:
    //   불규칙한 타이밍으로 Head rot이 확 꺾였다 돌아옴
    //   방향도 불규칙 (앞/뒤/옆)
    //   까딱 사이 간격: 5프레임, 3프레임, 7프레임, 4프레임... 들쭉날쭉
    //
    // Bottom Y: Body Y 변화 없으므로 고정
    // Hand_B: SB+FB 보정으로 Weapon 방향 유지
    // ================================================================

    static float[] F(float b, params (int i, float v)[] o)
    {
        var a = new float[N];
        for (int i = 0; i < N; i++) a[i] = b;
        foreach (var (i, v) in o) if (i < N) a[i] = v;
        return a;
    }

    // ── Body: 늘어진 채 고정, 고개 까딱에 맞춰 아주 미세하게만 반응 ──
    static readonly float[] BODY_ROT = F(8f,
        (5,  9f),(6,  8f),        // 까딱1 반동
        (12, 7f),(13, 8f),        // 까딱2 반동
        (20, 9f),(21, 8f),        // 까딱3 반동
        (27, 7f),(28, 8f));       // 까딱4 반동

    // ── Head: 고개 까딱까딱 (불규칙 타이밍, 불규칙 방향) ─────────────
    // 까딱1 f4~f6   : 앞으로 확 (f4 peak)
    // 까딱2 f11~f13 : 반대(뒤)로 확 (f11 peak)
    // 까딱3 f19~f21 : 앞으로 다시, 더 크게 (f19 peak)
    // 까딱4 f26~f28 : 옆으로 (f26 peak)
    // 나머지는 base=-5 유지
    static readonly float[] HEAD_ROT = F(-5f,
        // 까딱1: 앞으로
        (3, -8f),(4,-22f),(5, -8f),(6, -5f),
        // 까딱2: 뒤로
        (10, -3f),(11, 12f),(12, 2f),(13,-5f),
        // 까딱3: 앞으로 크게
        (18,-10f),(19,-28f),(20,-12f),(21,-5f),
        // 까딱4: 반대
        (25,-3f),(26, 16f),(27,  4f),(28,-5f));

    static readonly float[] HEAD_X = F(0f,
        (3,-0.02f),(4,-0.06f),(5,-0.02f),
        (10, 0.01f),(11, 0.04f),(12, 0.01f),
        (18,-0.03f),(19,-0.08f),(20,-0.03f),
        (25, 0.02f),(26, 0.05f),(27, 0.02f));

    // ── SF+FF: 축 처진 채 고정 ────────────────────────────────────────
    static readonly float[] SF_ROT = F(-15f);
    static readonly float[] FF_ROT = F(-25f);

    // ── SB+FB: 살짝 처진 채 고정, 까딱에 아주 미세하게 반응 ──────────
    static readonly float[] SB_ROT = F(5f,
        (4, 6f),(5, 5f),
        (11,4f),(12,5f),
        (19,6f),(20,5f),
        (26,4f),(27,5f));

    static readonly float[] FB_ROT = F(10f,
        (4,11f),(5,10f),
        (11,9f),(12,10f),
        (19,11f),(20,10f),
        (26,9f),(27,10f));

    // Hand_B: SB+FB base 보정 (SB+5, FB+10 → Hand_B로 역보정)
    // 검끝이 바닥 유지: Hand_B = -20 - (SB_offset) - (FB_offset * 0.5)
    static readonly float[] HB_ROT = F(-20f,
        (4,-21f),(5,-20f),
        (11,-19f),(12,-20f),
        (19,-21f),(20,-20f),
        (26,-19f),(27,-20f));

    // Weapon: 거의 완전 고정
    static readonly float[] WP_ROT = F(-90f,
        (4,-91f),(5,-90f),
        (11,-89f),(12,-90f));

    // ── 다리: Body rot +8 상쇄 ────────────────────────────────────────
    static readonly float[] TF_ROT = F(-8f,
        (5,-9f),(6,-8f),
        (12,-7f),(13,-8f),
        (20,-9f),(21,-8f),
        (27,-7f),(28,-8f));

    static readonly float[] TB_ROT = F(-8f,
        (5,-9f),(6,-8f),
        (12,-7f),(13,-8f),
        (20,-9f),(21,-8f),
        (27,-7f),(28,-8f));

    static readonly float[] CF_ROT = F(6f);
    static readonly float[] CB_ROT = F(-5f);

    [MenuItem("Tools/MainBoss 1/Generate Idle2")]
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
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

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
        Debug.Log($"[MainBoss 1] {CLIP_NAME} -> {assetPath}");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
    }
}
