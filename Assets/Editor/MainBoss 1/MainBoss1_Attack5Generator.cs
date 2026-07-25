using UnityEngine;
using UnityEditor;

public class MainBoss1_Attack5Generator
{
    const string SAVE_PATH    = "Assets/Animations/MainBoss 1";
    const string CLIP_NAME    = "MainBoss1_Attack5";
    const float  FRAME_RATE   = 12f;
    const int    TOTAL_FRAMES = 60;
    const float  DUR          = TOTAL_FRAMES / FRAME_RATE;

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
    // 5연격: 세로-세로-가로-세로-가로
    //
    // Attack2 참고:
    //   세로베기: 실측 임팩트 SF=-4.22, FF=48.30, SB=-10.97, FB=12.12
    //   가로베기: WP rot 140→-90(scale), SB 몸앞(-72)→펼치기(+80)
    //
    // [1타 세로] f0~f8   : 왼쪽위→오른쪽아래 (Body+)
    // [2타 세로] f9~f17  : 오른쪽위→왼쪽아래 (Body-)
    // [3타 가로] f18~f27 : 가로베기 (SB 뒤로→앞으로, WP scale)
    // [4타 세로] f28~f36 : 세로베기 (1타 방향)
    // [5타 가로] f37~f51 : 가로베기 마무리 (더 크게, 넉백)
    // [복귀]     f52~f59 : rest
    // ================================================================

    static float[] F(float b, params (int i, float v)[] o)
    {
        var a = new float[TOTAL_FRAMES];
        for (int i = 0; i < TOTAL_FRAMES; i++) a[i] = b;
        foreach (var (i, v) in o) if (i < TOTAL_FRAMES) a[i] = v;
        return a;
    }

    // ── Body ─────────────────────────────────────────────────────────
    static readonly float[] BODY_ROT = F(0f,
        // 1타: Body+ (왼쪽위→오른쪽아래)
        (0,-8f),(1,-14f),(2,-18f),(3,-20f),(4,-20f),
        (5,-16f),(6,-10f),(7,-4f),(8, 0f),
        // 2타: Body- (반대)
        (9, 8f),(10,14f),(11,18f),(12,20f),(13,20f),
        (14,16f),(15,10f),(16, 4f),(17, 0f),
        // 3타 가로: 오른쪽(+)→왼쪽(-)
        (18,10f),(19,18f),(20,24f),(21,26f),(22,26f),(23,26f),
        (24,-18f),(25,-26f),(26,-20f),(27,-10f),
        // 4타 세로: Body+
        (28,-4f),(29,-10f),(30,-16f),(31,-20f),(32,-20f),
        (33,-16f),(34,-8f),(35,-2f),(36, 0f),
        // 5타 가로: 오른쪽→왼쪽 크게
        (37,12f),(38,22f),(39,28f),(40,30f),(41,30f),(42,30f),(43,30f),
        (44,-22f),(45,-32f),(46,-26f),(47,-18f),(48,-10f),
        // 복귀
        (49,-4f),(50,-1f),(51,0f));

    // ── Head: Body 반대 ───────────────────────────────────────────────
    static readonly float[] HEAD_ROT = F(-2f,
        (0, 6f),(1,12f),(2,16f),(3,18f),(4,18f),
        (5,14f),(6, 8f),(7, 2f),(8,-2f),
        (9,-10f),(10,-16f),(11,-20f),(12,-22f),(13,-22f),
        (14,-18f),(15,-12f),(16,-6f),(17,-2f),
        (18,-12f),(19,-20f),(20,-26f),(21,-28f),(22,-28f),(23,-28f),
        (24,16f),(25,24f),(26,18f),(27, 8f),
        (28, 2f),(29, 8f),(30,14f),(31,18f),(32,18f),
        (33,14f),(34, 6f),(35, 0f),(36,-2f),
        (37,-14f),(38,-24f),(39,-30f),(40,-32f),(41,-32f),(42,-32f),(43,-32f),
        (44,20f),(45,30f),(46,24f),(47,16f),(48, 8f),
        (49, 2f),(50,-1f),(51,-2f));

    // ── Bottom: Body 반대 ─────────────────────────────────────────────
    static readonly float[] BOT_ROT = F(0f,
        (0, 8f),(1,14f),(2,18f),(3,20f),(4,20f),
        (5,16f),(6,10f),(7, 4f),(8, 0f),
        (9,-8f),(10,-14f),(11,-18f),(12,-20f),(13,-20f),
        (14,-16f),(15,-10f),(16,-4f),(17, 0f),
        (18,-10f),(19,-18f),(20,-24f),(21,-26f),(22,-26f),(23,-26f),
        (24,18f),(25,26f),(26,20f),(27,10f),
        (28, 4f),(29,10f),(30,16f),(31,20f),(32,20f),
        (33,16f),(34, 8f),(35, 2f),(36, 0f),
        (37,-12f),(38,-22f),(39,-28f),(40,-30f),(41,-30f),(42,-30f),(43,-30f),
        (44,22f),(45,32f),(46,26f),(47,18f),(48,10f),
        (49, 4f),(50, 1f),(51, 0f));

    // ── SF: 세로베기 양손, 가로베기 균형 ─────────────────────────────
    static readonly float[] SF_ROT = F(20f,
        // 1타: 위로→임팩트
        (0, 80f),(1,120f),(2,145f),(3,-4.22f),(4,-4.22f),
        (5, 20f),(6, 50f),(7, 10f),(8, 20f),
        // 2타: 반대위로→임팩트
        (9, 80f),(10,118f),(11,140f),(12,-4.22f),(13,-4.22f),
        (14, 20f),(15, 50f),(16, 18f),(17, 20f),
        // 3타 가로
        (18,14f),(19, 8f),(20, 4f),(21, 0f),(22,-2f),(23,-2f),
        (24,28f),(25,36f),(26,26f),(27,22f),
        // 4타 세로
        (28, 80f),(29,118f),(30,140f),(31,-4.22f),(32,-4.22f),
        (33, 20f),(34, 50f),(35, 18f),(36, 20f),
        // 5타 가로
        (37,14f),(38, 8f),(39, 4f),(40, 0f),(41,-2f),(42,-2f),(43,-2f),
        (44,28f),(45,38f),(46,28f),(47,24f),(48,22f),
        // 복귀
        (49,21f),(50,20f),(51,20f));

    static readonly float[] FF_ROT = F(15f,
        (0, 55f),(1, 82f),(2, 98f),(3,48.30f),(4,48.30f),
        (5, 15f),(6, 30f),(7, 12f),(8, 15f),
        (9, 54f),(10, 78f),(11, 92f),(12,48.30f),(13,48.30f),
        (14, 15f),(15, 30f),(16, 12f),(17, 15f),
        (18,10f),(19, 6f),(20, 3f),(21, 0f),(22,-1f),(23,-1f),
        (24,20f),(25,26f),(26,18f),(27,16f),
        (28, 54f),(29, 78f),(30, 92f),(31,48.30f),(32,48.30f),
        (33, 15f),(34, 30f),(35, 12f),(36, 15f),
        (37,10f),(38, 6f),(39, 3f),(40, 0f),(41,-1f),(42,-1f),(43,-1f),
        (44,20f),(45,28f),(46,20f),(47,16f),(48,15f),
        (49,15f),(50,15f),(51,15f));

    // ── SB: 세로베기 실측, 가로베기 Attack2 기준 ─────────────────────
    static readonly float[] SB_ROT = F(0f,
        // 1타
        (0, 65f),(1, 95f),(2,115f),(3,-10.97f),(4,-10.97f),
        (5,-15f),(6, 5f),(7, 0f),(8, 0f),
        // 2타
        (9, 65f),(10, 95f),(11,115f),(12,-10.97f),(13,-10.97f),
        (14,-15f),(15, 5f),(16, 0f),(17, 0f),
        // 3타 가로
        (18,-15f),(19,-35f),(20,-52f),(21,-65f),(22,-72f),(23,-72f),
        (24,52f),(25,88f),(26,65f),(27,40f),
        // 4타 세로
        (28, 65f),(29, 95f),(30,115f),(31,-10.97f),(32,-10.97f),
        (33,-15f),(34, 5f),(35, 0f),(36, 0f),
        // 5타 가로 (더 크게)
        (37,-18f),(38,-40f),(39,-60f),(40,-75f),(41,-80f),(42,-80f),(43,-80f),
        (44,60f),(45,95f),(46,72f),(47,45f),(48,20f),
        (49, 8f),(50, 2f),(51, 0f));

    static readonly float[] FB_ROT = F(0f,
        (0, 40f),(1, 62f),(2, 76f),(3,12.12f),(4,12.12f),
        (5,-8f),(6, 3f),(7, 0f),(8, 0f),
        (9, 40f),(10, 62f),(11, 76f),(12,12.12f),(13,12.12f),
        (14,-8f),(15, 3f),(16, 0f),(17, 0f),
        (18,-8f),(19,-22f),(20,-34f),(21,-42f),(22,-46f),(23,-46f),
        (24,32f),(25,56f),(26,40f),(27,24f),
        (28, 40f),(29, 62f),(30, 76f),(31,12.12f),(32,12.12f),
        (33,-8f),(34, 3f),(35, 0f),(36, 0f),
        (37,-10f),(38,-24f),(39,-38f),(40,-48f),(41,-52f),(42,-52f),(43,-52f),
        (44,38f),(45,62f),(46,46f),(47,28f),(48,12f),
        (49, 4f),(50, 1f),(51, 0f));

    static readonly float[] HB_ROT = F(0f,
        // 세로베기: 자연스러운 손목
        (0,-8f),(1,-14f),(2,-18f),(3,10f),(4,10f),
        (5, 8f),(6, 2f),(7, 0f),(8, 0f),
        (9,-8f),(10,-14f),(11,-18f),(12,12f),(13,12f),
        (14, 8f),(15, 2f),(16, 0f),(17, 0f),
        // 3타 가로: 뒤로 접기
        (18, 8f),(19,20f),(20,30f),(21,36f),(22,38f),(23,38f),
        (24,-15f),(25,-25f),(26,-18f),(27,-8f),
        // 4타 세로
        (28,-8f),(29,-14f),(30,-18f),(31,12f),(32,12f),
        (33, 8f),(34, 2f),(35, 0f),(36, 0f),
        // 5타 가로
        (37,10f),(38,24f),(39,34f),(40,40f),(41,42f),(42,42f),(43,42f),
        (44,-18f),(45,-28f),(46,-20f),(47,-10f),(48,-3f),
        (49,-1f),(50, 0f),(51, 0f));

    // ── WP rot/scale ─────────────────────────────────────────────────
    static readonly float[] WP_ROT = F(-90f,
        // 세로베기 구간: -90 유지
        // 3타 가로: 140→-90 순간점프
        (18,50f),(19,95f),(20,120f),(21,135f),(22,140f),(23,140f),
        (24,-90f),
        // 4타 세로: -90 유지
        (28,-90f),
        // 5타 가로: 140→-90
        (37,50f),(38,95f),(39,120f),(40,135f),(41,140f),(42,140f),(43,140f),
        (44,-90f));

    static readonly float[] WP_SCALE_X = F(1f,
        // 1타 세로
        (2, 0.8f),(3, 0.6f),(4, 1.0f),
        // 2타 세로
        (11, 0.8f),(12, 0.6f),(13, 1.0f),
        // 3타 가로
        (21, 0.6f),(22, 0.3f),(23, 0.05f),
        (24, 0.05f),(25, 1.5f),
        (26, 0.3f),(27, 0.05f),
        // 2타 끝 카메라통과
        (28,-0.6f),(29,-1.0f),(30,-0.4f),(31, 1.0f),
        // 4타 세로
        (30, 0.8f),(31, 0.6f),(32, 1.0f),
        // 5타 가로
        (40, 0.6f),(41, 0.3f),(42, 0.2f),(43, 0.05f),
        (44, 0.05f),(45, 1.8f),
        (46, 0.3f),(47, 0.05f),
        (48,-0.6f),(49,-1.0f),
        (50, 1.0f));

    static readonly float[] WP_SCALE_Y = F(1f,
        // 세로베기 임팩트
        (3, 1.4f),(4, 1.6f),(5, 1.2f),(6, 1.0f),
        (12, 1.4f),(13, 1.6f),(14, 1.2f),(15, 1.0f),
        // 3타 가로
        (25, 1.5f),(26, 1.3f),(27, 1.0f),
        // 4타 세로
        (31, 1.4f),(32, 1.6f),(33, 1.2f),(34, 1.0f),
        // 5타 가로
        (45, 1.6f),(46, 1.4f),(47, 1.1f),(48, 1.0f));

    // ── 다리: 체중이동 ────────────────────────────────────────────────
    static readonly float[] TF_ROT = F(0f,
        (0,-8f),(1,-14f),(2,-18f),(3,-20f),(4,-20f),
        (5,-16f),(6,-8f),(7,-2f),(8, 0f),
        (9, 8f),(10,14f),(11,18f),(12,20f),(13,20f),
        (14,16f),(15, 8f),(16, 2f),(17, 0f),
        (18,-8f),(19,-14f),(20,-20f),(21,-22f),(22,-24f),(23,-24f),
        (24, 8f),(25,12f),(26, 8f),(27, 4f),
        (28,-8f),(29,-14f),(30,-18f),(31,-20f),(32,-20f),
        (33,-16f),(34,-8f),(35,-2f),(36, 0f),
        (37,-10f),(38,-18f),(39,-24f),(40,-28f),(41,-28f),(42,-28f),(43,-28f),
        (44,10f),(45,14f),(46,10f),(47, 6f),(48, 2f),
        (49, 0f));

    static readonly float[] CF_ROT = F(0f,
        (0, 4f),(1, 8f),(2,10f),(3,12f),(4,12f),
        (5, 8f),(6, 4f),(7, 1f),(8, 0f),
        (9,-4f),(10,-8f),(11,-10f),(12,-12f),(13,-12f),
        (14,-8f),(15,-4f),(16,-1f),(17, 0f),
        (18, 4f),(19, 8f),(20,11f),(21,12f),(22,13f),(23,13f),
        (24,-5f),(25,-8f),(26,-5f),(27,-2f),
        (28, 4f),(29, 8f),(30,10f),(31,12f),(32,12f),
        (33, 8f),(34, 4f),(35, 1f),(36, 0f),
        (37, 5f),(38, 9f),(39,12f),(40,14f),(41,14f),(42,14f),(43,14f),
        (44,-6f),(45,-8f),(46,-6f),(47,-3f),(48,-1f),
        (49, 0f));

    static readonly float[] TB_ROT = F(0f,
        (0,-8f),(1,-14f),(2,-18f),(3,-20f),(4,-20f),
        (5,-16f),(6,-8f),(7,-2f),(8, 0f),
        (9, 8f),(10,14f),(11,18f),(12,20f),(13,20f),
        (14,16f),(15, 8f),(16, 2f),(17, 0f),
        (18, 5f),(19, 8f),(20,10f),(21,10f),(22,10f),(23,10f),
        (24,-5f),(25,-8f),(26,-5f),(27,-2f),
        (28,-8f),(29,-14f),(30,-18f),(31,-20f),(32,-20f),
        (33,-16f),(34,-8f),(35,-2f),(36, 0f),
        (37, 6f),(38,10f),(39,14f),(40,16f),(41,16f),(42,16f),(43,16f),
        (44,-8f),(45,-12f),(46,-8f),(47,-4f),(48,-1f),
        (49, 0f));

    static readonly float[] CB_ROT = F(0f,
        (0, 4f),(1, 8f),(2,10f),(3,12f),(4,12f),
        (5, 8f),(6, 4f),(7, 1f),(8, 0f),
        (9,-4f),(10,-8f),(11,-10f),(12,-12f),(13,-12f),
        (14,-8f),(15,-4f),(16,-1f),(17, 0f),
        (18,-2f),(19,-4f),(20,-5f),(21,-5f),(22,-5f),(23,-5f),
        (24, 3f),(25, 5f),(26, 3f),(27, 1f),
        (28, 4f),(29, 8f),(30,10f),(31,12f),(32,12f),
        (33, 8f),(34, 4f),(35, 1f),(36, 0f),
        (37,-3f),(38,-5f),(39,-7f),(40,-8f),(41,-8f),(42,-8f),(43,-8f),
        (44, 4f),(45, 6f),(46, 4f),(47, 2f),(48, 0f),
        (49, 0f));

    [MenuItem("Tools/MainBoss 1/Generate Attack5")]
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
        s.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        void C(string path, string prop, float[] vals)
        {
            var keys = new Keyframe[TOTAL_FRAMES];
            for (int i = 0; i < TOTAL_FRAMES; i++) keys[i] = new Keyframe(i / FRAME_RATE, vals[i]);
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

        L(BODY,"localPosition.x",0f); L(BODY,"localPosition.y",0f); L(BODY,"localPosition.z",0f);
        C(BODY,"localEulerAngles.z",BODY_ROT);
        L(HEAD,"localPosition.x",0f); L(HEAD,"localPosition.y",0.70f); L(HEAD,"localPosition.z",0f);
        C(HEAD,"localEulerAngles.z",HEAD_ROT);
        L(BOTTOM,"localPosition.x",-0.20f); L(BOTTOM,"localPosition.y",-0.30f); L(BOTTOM,"localPosition.z",0f);
        C(BOTTOM,"localEulerAngles.z",BOT_ROT);
        L(SF,"localPosition.x",-0.38f); L(SF,"localPosition.y",0.34f); L(SF,"localPosition.z",0f);
        C(SF,"localEulerAngles.z",SF_ROT); C(FF,"localEulerAngles.z",FF_ROT);
        L(SB,"localPosition.x",0.26f); L(SB,"localPosition.y",0.55f); L(SB,"localPosition.z",0f);
        C(SB,"localEulerAngles.z",SB_ROT); C(FB,"localEulerAngles.z",FB_ROT); C(HB,"localEulerAngles.z",HB_ROT);
        C(WP,"localEulerAngles.z",WP_ROT); C(WP,"localScale.x",WP_SCALE_X); C(WP,"localScale.y",WP_SCALE_Y);
        L(WP,"localScale.z",1f);
        C(THIGH_F,"localEulerAngles.z",TF_ROT); C(CALF_F,"localEulerAngles.z",CF_ROT);
        C(THIGH_B,"localEulerAngles.z",TB_ROT); C(CALF_B,"localEulerAngles.z",CB_ROT);

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
