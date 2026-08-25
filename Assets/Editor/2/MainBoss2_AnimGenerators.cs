using UnityEngine;
using UnityEditor;

public class MainBoss2_AnimGenerators
{
    const string SAVE_PATH = "Assets/Animations/2/MainBoss 2";
    const float FRAME_RATE = 12f;

    // ── 경로 상수 ────────────────────────────────────────────────────
    const string BODY = "Body";
    const string HEAD = "Body/Head";
    const string SF = "Body/Shoulder_F";
    const string FF = "Body/Shoulder_F/Forearm_F";
    const string HF = "Body/Shoulder_F/Forearm_F/Hand_F";
    const string SB = "Body/Shoulder_B";
    const string FB = "Body/Shoulder_B/Forearm_B";
    const string HB = "Body/Shoulder_B/Forearm_B/Hand_B";
    const string AXE = "Body/Shoulder_B/Forearm_B/Hand_B/Axe";
    const string BOTTOM = "Body/Bottom";
    const string THIGH_F = "Body/Bottom/Thigh_F";
    const string CALF_F = "Body/Bottom/Thigh_F/Calf_F";
    const string FOOT_F = "Body/Bottom/Thigh_F/Calf_F/Foot_F";
    const string THIGH_B = "Body/Bottom/Thigh_B";
    const string CALF_B = "Body/Bottom/Thigh_B/Calf_B";
    const string FOOT_B = "Body/Bottom/Thigh_B/Calf_B/Foot_B";

    // ================================================================
    // REST 실측값
    // Body(0,0), Head(-0.275, 1.025)
    // SF(-0.856, 0.631), FF(-0.245,-0.562), HF(-0.093,-0.678)
    // SB(0.467, 0.662), FB(0.141,-0.506), HB(0.026,-0.687)
    // Axe(0.000,-0.263) rot=-90
    // Bottom(-0.141, 0), ThighF(-0.572,-0.472), CalfF(0.008,-0.718), FootF(-0.265,-0.978)
    // ThighB(0.394,-0.556), CalfB(0.259,-0.737), FootB(-0.060,-0.875)
    // ================================================================

    // ── Idle 배열 (48프레임) ─────────────────────────────────────────
    static readonly float[] IDLE_BODY_Y = { 0.0f, 0.0091f, 0.0181f, 0.0268f, 0.035f, 0.0426f, 0.0495f, 0.0555f, 0.0606f, 0.0647f, 0.0676f, 0.0694f, 0.07f, 0.0694f, 0.0676f, 0.0647f, 0.0606f, 0.0555f, 0.0495f, 0.0426f, 0.035f, 0.0268f, 0.0181f, 0.0091f, 0.0f, -0.0091f, -0.0181f, -0.0268f, -0.035f, -0.0426f, -0.0495f, -0.0555f, -0.0606f, -0.0647f, -0.0676f, -0.0694f, -0.07f, -0.0694f, -0.0676f, -0.0647f, -0.0606f, -0.0555f, -0.0495f, -0.0426f, -0.035f, -0.0268f, -0.0181f, -0.0091f };
    static readonly float[] IDLE_BOTTOM_Y = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] IDLE_THIGH_F_Y = { -0.472f, -0.4811f, -0.4901f, -0.4988f, -0.507f, -0.5146f, -0.5215f, -0.5275f, -0.5326f, -0.5367f, -0.5396f, -0.5414f, -0.542f, -0.5414f, -0.5396f, -0.5367f, -0.5326f, -0.5275f, -0.5215f, -0.5146f, -0.507f, -0.4988f, -0.4901f, -0.4811f, -0.472f, -0.4629f, -0.4539f, -0.4452f, -0.437f, -0.4294f, -0.4225f, -0.4165f, -0.4114f, -0.4073f, -0.4044f, -0.4026f, -0.402f, -0.4026f, -0.4044f, -0.4073f, -0.4114f, -0.4165f, -0.4225f, -0.4294f, -0.437f, -0.4452f, -0.4539f, -0.4629f };
    static readonly float[] IDLE_THIGH_B_Y = { -0.556f, -0.5651f, -0.5741f, -0.5828f, -0.591f, -0.5986f, -0.6055f, -0.6115f, -0.6166f, -0.6207f, -0.6236f, -0.6254f, -0.626f, -0.6254f, -0.6236f, -0.6207f, -0.6166f, -0.6115f, -0.6055f, -0.5986f, -0.591f, -0.5828f, -0.5741f, -0.5651f, -0.556f, -0.5469f, -0.5379f, -0.5292f, -0.521f, -0.5134f, -0.5065f, -0.5005f, -0.4954f, -0.4913f, -0.4884f, -0.4866f, -0.486f, -0.4866f, -0.4884f, -0.4913f, -0.4954f, -0.5005f, -0.5065f, -0.5134f, -0.521f, -0.5292f, -0.5379f, -0.5469f };
    static readonly float[] IDLE_SF_Y = { 0.5707f, 0.5732f, 0.5763f, 0.5797f, 0.5835f, 0.5876f, 0.5919f, 0.5964f, 0.601f, 0.6056f, 0.6101f, 0.6144f, 0.6185f, 0.6223f, 0.6257f, 0.6288f, 0.6313f, 0.6333f, 0.6348f, 0.6357f, 0.636f, 0.6357f, 0.6348f, 0.6333f, 0.6313f, 0.6288f, 0.6257f, 0.6223f, 0.6185f, 0.6144f, 0.6101f, 0.6056f, 0.601f, 0.5964f, 0.5919f, 0.5876f, 0.5835f, 0.5797f, 0.5763f, 0.5732f, 0.5707f, 0.5687f, 0.5672f, 0.5663f, 0.566f, 0.5663f, 0.5672f, 0.5687f };
    static readonly float[] IDLE_SB_Y = { 0.6017f, 0.6042f, 0.6073f, 0.6107f, 0.6145f, 0.6186f, 0.6229f, 0.6274f, 0.632f, 0.6366f, 0.6411f, 0.6454f, 0.6495f, 0.6533f, 0.6567f, 0.6598f, 0.6623f, 0.6643f, 0.6658f, 0.6667f, 0.667f, 0.6667f, 0.6658f, 0.6643f, 0.6623f, 0.6598f, 0.6567f, 0.6533f, 0.6495f, 0.6454f, 0.6411f, 0.6366f, 0.632f, 0.6274f, 0.6229f, 0.6186f, 0.6145f, 0.6107f, 0.6073f, 0.6042f, 0.6017f, 0.5997f, 0.5982f, 0.5973f, 0.597f, 0.5973f, 0.5982f, 0.5997f };

    // ================================================================
    // Generate All
    // ================================================================
    [MenuItem("Tools/MainBoss2/Generate All")]
    public static void GenerateAll()
    {
        GenerateIdle();
        GenerateIdle2();
        GenerateWalk();
        GenerateWalk2();
        GenerateAttack();
        GenerateSkill1();
        GenerateSkill2();
        GenerateSkill3();
        GenerateBuff();
        GenerateRun();
        GenerateRun2();
        GenerateRun3();
        GenerateAC();
        Debug.Log("[MainBoss2] 전체 생성 완료");
    }

    // ================================================================
    // Generate Idle
    // ================================================================
    [MenuItem("Tools/MainBoss2/Generate Idle")]
    public static void GenerateIdle()
    {
        var clip = MakeClip("MainBoss2_Idle", 48, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 48);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, 4f, val));

        // Body
        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", IDLE_BODY_Y); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);

        // Head (Body 자식, 따라감)
        L(HEAD, "localPosition.x", -0.275f); L(HEAD, "localPosition.y", 1.025f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);

        // Shoulder_F / Forearm_F / Hand_F
        L(SF, "localPosition.x", -0.856f); C(SF, "localPosition.y", IDLE_SF_Y); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", -5.068f);
        L(FF, "localPosition.x", -0.245f); L(FF, "localPosition.y", -0.562f); L(FF, "localPosition.z", 0f);
        L(FF, "localEulerAngles.z", 5.682f);
        L(HF, "localPosition.x", -0.093f); L(HF, "localPosition.y", -0.678f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        // Shoulder_B / Forearm_B / Hand_B / Axe
        L(SB, "localPosition.x", 0.467f); C(SB, "localPosition.y", IDLE_SB_Y); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 0f);
        L(FB, "localPosition.x", 0.141f); L(FB, "localPosition.y", -0.506f); L(FB, "localPosition.z", 0f);
        L(FB, "localEulerAngles.z", 75f);
        L(HB, "localPosition.x", 0.026f); L(HB, "localPosition.y", -0.687f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(AXE, "localPosition.x", 0.000f); L(AXE, "localPosition.y", -0.263f); L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", -90f);

        // Bottom
        L(BOTTOM, "localPosition.x", -0.141f); C(BOTTOM, "localPosition.y", IDLE_BOTTOM_Y); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);

        // Thigh_F / Calf_F / Foot_F
        L(THIGH_F, "localPosition.x", -0.572f); C(THIGH_F, "localPosition.y", IDLE_THIGH_F_Y); L(THIGH_F, "localPosition.z", 0f);
        L(THIGH_F, "localEulerAngles.z", 0f);
        L(CALF_F, "localPosition.x", 0.008f); L(CALF_F, "localPosition.y", -0.718f); L(CALF_F, "localPosition.z", 0f);
        L(CALF_F, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.265f); L(FOOT_F, "localPosition.y", -0.978f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);

        // Thigh_B / Calf_B / Foot_B
        L(THIGH_B, "localPosition.x", 0.394f); C(THIGH_B, "localPosition.y", IDLE_THIGH_B_Y); L(THIGH_B, "localPosition.z", 0f);
        L(THIGH_B, "localEulerAngles.z", 0f);
        L(CALF_B, "localPosition.x", 0.259f); L(CALF_B, "localPosition.y", -0.737f); L(CALF_B, "localPosition.z", 0f);
        L(CALF_B, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", -0.060f); L(FOOT_B, "localPosition.y", -0.875f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

        Save(clip, "MainBoss2_Idle");
        GenerateAC();
    }


    // ── Idle2 배열 (48프레임, 무기 땅에 세운 자세) ──────────────────
    static readonly float[] IDLE2_BODY_Y = { 0.0f, 0.0091f, 0.0181f, 0.0268f, 0.035f, 0.0426f, 0.0495f, 0.0555f, 0.0606f, 0.0647f, 0.0676f, 0.0694f, 0.07f, 0.0694f, 0.0676f, 0.0647f, 0.0606f, 0.0555f, 0.0495f, 0.0426f, 0.035f, 0.0268f, 0.0181f, 0.0091f, 0.0f, -0.0091f, -0.0181f, -0.0268f, -0.035f, -0.0426f, -0.0495f, -0.0555f, -0.0606f, -0.0647f, -0.0676f, -0.0694f, -0.07f, -0.0694f, -0.0676f, -0.0647f, -0.0606f, -0.0555f, -0.0495f, -0.0426f, -0.035f, -0.0268f, -0.0181f, -0.0091f };
    static readonly float[] IDLE2_BOTTOM_Y = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] IDLE2_THIGH_F_Y = { -0.472f, -0.4811f, -0.4901f, -0.4988f, -0.507f, -0.5146f, -0.5215f, -0.5275f, -0.5326f, -0.5367f, -0.5396f, -0.5414f, -0.542f, -0.5414f, -0.5396f, -0.5367f, -0.5326f, -0.5275f, -0.5215f, -0.5146f, -0.507f, -0.4988f, -0.4901f, -0.4811f, -0.472f, -0.4629f, -0.4539f, -0.4452f, -0.437f, -0.4294f, -0.4225f, -0.4165f, -0.4114f, -0.4073f, -0.4044f, -0.4026f, -0.402f, -0.4026f, -0.4044f, -0.4073f, -0.4114f, -0.4165f, -0.4225f, -0.4294f, -0.437f, -0.4452f, -0.4539f, -0.4629f };
    static readonly float[] IDLE2_THIGH_B_Y = { -0.556f, -0.5651f, -0.5741f, -0.5828f, -0.591f, -0.5986f, -0.6055f, -0.6115f, -0.6166f, -0.6207f, -0.6236f, -0.6254f, -0.626f, -0.6254f, -0.6236f, -0.6207f, -0.6166f, -0.6115f, -0.6055f, -0.5986f, -0.591f, -0.5828f, -0.5741f, -0.5651f, -0.556f, -0.5469f, -0.5379f, -0.5292f, -0.521f, -0.5134f, -0.5065f, -0.5005f, -0.4954f, -0.4913f, -0.4884f, -0.4866f, -0.486f, -0.4866f, -0.4884f, -0.4913f, -0.4954f, -0.5005f, -0.5065f, -0.5134f, -0.521f, -0.5292f, -0.5379f, -0.5469f };
    static readonly float[] IDLE2_SF_Y = { 0.6007f, 0.6032f, 0.6063f, 0.6097f, 0.6135f, 0.6176f, 0.6219f, 0.6264f, 0.631f, 0.6356f, 0.6401f, 0.6444f, 0.6485f, 0.6523f, 0.6557f, 0.6588f, 0.6613f, 0.6633f, 0.6648f, 0.6657f, 0.666f, 0.6657f, 0.6648f, 0.6633f, 0.6613f, 0.6588f, 0.6557f, 0.6523f, 0.6485f, 0.6444f, 0.6401f, 0.6356f, 0.631f, 0.6264f, 0.6219f, 0.6176f, 0.6135f, 0.6097f, 0.6063f, 0.6032f, 0.6007f, 0.5987f, 0.5972f, 0.5963f, 0.596f, 0.5963f, 0.5972f, 0.5987f };
    static readonly float[] IDLE2_FB_ROT = { 75.0f, 74.68f, 74.37f, 74.06f, 73.78f, 73.51f, 73.27f, 73.06f, 72.88f, 72.74f, 72.63f, 72.57f, 72.55f, 72.57f, 72.63f, 72.74f, 72.88f, 73.06f, 73.27f, 73.51f, 73.78f, 74.06f, 74.37f, 74.68f, 75.0f, 75.32f, 75.63f, 75.94f, 76.22f, 76.49f, 76.73f, 76.94f, 77.12f, 77.26f, 77.37f, 77.43f, 77.45f, 77.43f, 77.37f, 77.26f, 77.12f, 76.94f, 76.73f, 76.49f, 76.22f, 75.94f, 75.63f, 75.32f };
    static readonly float[] IDLE2_HB_ROT = { 5.0f, 5.32f, 5.63f, 5.94f, 6.22f, 6.49f, 6.73f, 6.94f, 7.12f, 7.26f, 7.37f, 7.43f, 7.45f, 7.43f, 7.37f, 7.26f, 7.12f, 6.94f, 6.73f, 6.49f, 6.22f, 5.94f, 5.63f, 5.32f, 5.0f, 4.68f, 4.37f, 4.06f, 3.77f, 3.51f, 3.27f, 3.06f, 2.88f, 2.74f, 2.63f, 2.57f, 2.55f, 2.57f, 2.63f, 2.74f, 2.88f, 3.06f, 3.27f, 3.51f, 3.77f, 4.06f, 4.37f, 4.68f };
    static readonly float[] IDLE2_SB_Y_FIXED = { 0.632f, 0.6229f, 0.6139f, 0.6052f, 0.597f, 0.5894f, 0.5825f, 0.5765f, 0.5714f, 0.5673f, 0.5644f, 0.5626f, 0.562f, 0.5626f, 0.5644f, 0.5673f, 0.5714f, 0.5765f, 0.5825f, 0.5894f, 0.597f, 0.6052f, 0.6139f, 0.6229f, 0.632f, 0.6411f, 0.6501f, 0.6588f, 0.667f, 0.6746f, 0.6815f, 0.6875f, 0.6926f, 0.6967f, 0.6996f, 0.7014f, 0.702f, 0.7014f, 0.6996f, 0.6967f, 0.6926f, 0.6875f, 0.6815f, 0.6746f, 0.667f, 0.6588f, 0.6501f, 0.6411f };
    static readonly float[] IDLE2_SB_Y = { 0.6017f, 0.6042f, 0.6073f, 0.6107f, 0.6145f, 0.6186f, 0.6229f, 0.6274f, 0.632f, 0.6366f, 0.6411f, 0.6454f, 0.6495f, 0.6533f, 0.6567f, 0.6598f, 0.6623f, 0.6643f, 0.6658f, 0.6667f, 0.667f, 0.6667f, 0.6658f, 0.6643f, 0.6623f, 0.6598f, 0.6567f, 0.6533f, 0.6495f, 0.6454f, 0.6411f, 0.6366f, 0.632f, 0.6274f, 0.6229f, 0.6186f, 0.6145f, 0.6107f, 0.6073f, 0.6042f, 0.6017f, 0.5997f, 0.5982f, 0.5973f, 0.597f, 0.5973f, 0.5982f, 0.5997f };

    // ================================================================
    // Generate Idle2 (한손으로 무기 땅에 세운 자세)
    // SB=10°, FB=75°, HB=5°, Axe pos(0.197,-0.260) rot=-90°
    // ================================================================
    [MenuItem("Tools/MainBoss2/Generate Idle2")]
    public static void GenerateIdle2()
    {
        var clip = MakeClip("MainBoss2_Idle2", 48, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 48);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, 4f, val));

        L(BODY, "localPosition.x", 0f); C(BODY, "localPosition.y", IDLE2_BODY_Y); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);

        L(HEAD, "localPosition.x", -0.275f); L(HEAD, "localPosition.y", 1.025f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);

        L(SF, "localPosition.x", -0.856f); C(SF, "localPosition.y", IDLE2_SF_Y); L(SF, "localPosition.z", 0f);
        L(SF, "localEulerAngles.z", 0f);
        L(FF, "localPosition.x", -0.245f); L(FF, "localPosition.y", -0.562f); L(FF, "localPosition.z", 0f);
        L(FF, "localEulerAngles.z", 5.682f);
        L(HF, "localPosition.x", -0.093f); L(HF, "localPosition.y", -0.678f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        // 뒷팔: 무기 든 자세 (SB=10, FB=75, HB=5)
        L(SB, "localPosition.x", 0.467f); C(SB, "localPosition.y", IDLE2_SB_Y); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 10f);
        L(FB, "localPosition.x", 0.141f); L(FB, "localPosition.y", -0.506f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", IDLE2_FB_ROT);
        L(HB, "localPosition.x", 0.026f); L(HB, "localPosition.y", -0.687f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", IDLE2_HB_ROT);
        L(AXE, "localPosition.x", 0.197f); L(AXE, "localPosition.y", -0.260f); L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", -90f);

        L(BOTTOM, "localPosition.x", -0.141f); L(BOTTOM, "localPosition.y", 0f); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);

        L(THIGH_F, "localPosition.x", -0.572f); C(THIGH_F, "localPosition.y", IDLE2_THIGH_F_Y); L(THIGH_F, "localPosition.z", 0f);
        L(THIGH_F, "localEulerAngles.z", 0f);
        L(CALF_F, "localPosition.x", 0.008f); L(CALF_F, "localPosition.y", -0.718f); L(CALF_F, "localPosition.z", 0f);
        L(CALF_F, "localEulerAngles.z", 0f);
        L(FOOT_F, "localPosition.x", -0.265f); L(FOOT_F, "localPosition.y", -0.978f); L(FOOT_F, "localPosition.z", 0f);
        L(FOOT_F, "localEulerAngles.z", 0f);

        L(THIGH_B, "localPosition.x", 0.394f); C(THIGH_B, "localPosition.y", IDLE_THIGH_B_Y); L(THIGH_B, "localPosition.z", 0f);
        L(THIGH_B, "localEulerAngles.z", 0f);
        L(CALF_B, "localPosition.x", 0.259f); L(CALF_B, "localPosition.y", -0.737f); L(CALF_B, "localPosition.z", 0f);
        L(CALF_B, "localEulerAngles.z", 0f);
        L(FOOT_B, "localPosition.x", -0.060f); L(FOOT_B, "localPosition.y", -0.875f); L(FOOT_B, "localPosition.z", 0f);
        L(FOOT_B, "localEulerAngles.z", 0f);

        Save(clip, "MainBoss2_Idle2");
        GenerateAC();
    }


    // ── Walk 배열 (32프레임) ─────────────────────────────────────────
    static readonly float[] WALK_BODY_ROT = { -0.0f, -0.98f, -1.91f, -2.78f, -3.54f, -4.16f, -4.62f, -4.9f, -5.0f, -4.9f, -4.62f, -4.16f, -3.54f, -2.78f, -1.91f, -0.98f, -0.0f, 0.98f, 1.91f, 2.78f, 3.54f, 4.16f, 4.62f, 4.9f, 5.0f, 4.9f, 4.62f, 4.16f, 3.54f, 2.78f, 1.91f, 0.98f };
    static readonly float[] WALK_BOT_ROT = { 0.0f, 0.98f, 1.91f, 2.78f, 3.54f, 4.16f, 4.62f, 4.9f, 5.0f, 4.9f, 4.62f, 4.16f, 3.54f, 2.78f, 1.91f, 0.98f, 0.0f, -0.98f, -1.91f, -2.78f, -3.54f, -4.16f, -4.62f, -4.9f, -5.0f, -4.9f, -4.62f, -4.16f, -3.54f, -2.78f, -1.91f, -0.98f };
    static readonly float[] WALK_BODY_Y = { -0.03f, -0.02f, -0.01f, -0.01f, 0.0f, 0.01f, 0.02f, 0.02f, 0.03f, 0.02f, 0.01f, 0.0f, -0.0f, -0.01f, -0.02f, -0.03f, -0.03f, -0.02f, -0.01f, -0.0f, 0.0f, 0.01f, 0.02f, 0.03f, 0.02f, 0.02f, 0.01f, 0.0f, -0.01f, -0.01f, -0.02f, -0.03f };
    static readonly float[] WALK_THIGH_B_Y = { -0.526f, -0.536f, -0.546f, -0.546f, -0.556f, -0.566f, -0.576f, -0.576f, -0.586f, -0.576f, -0.566f, -0.556f, -0.556f, -0.546f, -0.536f, -0.526f, -0.526f, -0.536f, -0.546f, -0.556f, -0.556f, -0.566f, -0.576f, -0.586f, -0.576f, -0.576f, -0.566f, -0.556f, -0.546f, -0.546f, -0.536f, -0.526f };
    static readonly float[] WALK_THIGH_F_ROT = { 13.49f, 12.14f, 10.58f, 8.87f, 7.04f, 5.16f, 3.27f, 1.41f, -0.36f, -1.99f, -3.44f, -4.66f, -5.59f, -6.19f, -6.42f, -6.21f, -4.95f, -2.3f, 1.37f, 5.69f, 10.29f, 14.79f, 18.81f, 22.0f, 23.96f, 24.44f, 23.67f, 22.01f, 19.8f, 17.4f, 15.18f, 13.49f };
    static readonly float[] WALK_THIGH_B_ROT = { -6.98f, -2.98f, 1.81f, 6.97f, 12.11f, 16.82f, 20.68f, 23.3f, 24.26f, 24.14f, 23.8f, 23.25f, 22.5f, 21.58f, 20.5f, 19.27f, 17.91f, 16.44f, 14.88f, 13.23f, 11.52f, 9.76f, 7.97f, 6.16f, 4.35f, 2.56f, 0.79f, -0.92f, -2.57f, -4.14f, -5.62f, -6.98f };
    static readonly float[] WALK_CALF_F_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.96f, -4.35f, -7.08f, -10.09f, -13.29f, -16.61f, -19.97f, -23.3f, -26.53f, -29.57f, -32.35f, -34.79f, -36.83f, -38.37f, -39.36f, -39.7f, -38.48f, -35.15f, -30.24f, -24.26f, -17.73f, -11.17f, -5.08f, 0.0f };
    static readonly float[] WALK_CALF_B_ROT = { -20.0f, -22.96f, -26.73f, -30.77f, -34.55f, -37.55f, -39.24f, -39.09f, -37.0f, -33.49f, -28.92f, -23.66f, -18.08f, -12.56f, -7.46f, -3.15f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.64f, -3.79f, -6.33f, -9.11f, -12.01f, -14.88f, -17.59f, -20.0f };
    static readonly float[] WALK_FOOT_F_ROT = { 10.0f, 9.63f, 8.59f, 7.02f, 5.18f, 3.33f, 1.76f, 0.72f, 0.35f, 1.07f, 3.13f, 6.2f, 9.83f, 13.46f, 16.53f, 18.59f, 19.31f, 18.77f, 17.21f, 14.89f, 12.16f, 9.42f, 7.1f, 5.54f, 5.0f, 5.0f, 5.0f, 5.48f, 6.73f, 8.27f, 9.52f, 10.0f };
    static readonly float[] WALK_FOOT_B_ROT = { 5.0f, 5.7f, 7.65f, 10.48f, 13.61f, 16.44f, 18.39f, 19.09f, 17.74f, 14.22f, 9.87f, 6.35f, 5.0f, 5.0f, 5.0f, 5.0f, 4.52f, 3.27f, 1.73f, 0.48f, 0.0f, 0.0f, 0.0f, 0.0f, 0.73f, 2.5f, 4.27f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f };
    static readonly float[] WALK_SF_ROT = { 0.0f, 1.56f, 3.06f, 4.44f, 5.66f, 6.65f, 7.39f, 7.85f, 8.0f, 7.85f, 7.39f, 6.65f, 5.66f, 4.44f, 3.06f, 1.56f, 0.0f, -1.56f, -3.06f, -4.44f, -5.66f, -6.65f, -7.39f, -7.85f, -8.0f, -7.85f, -7.39f, -6.65f, -5.66f, -4.44f, -3.06f, -1.56f };
    static readonly float[] WALK_SB_ROT_FINAL = { 5.0f, 4.22f, 3.47f, 2.78f, 2.17f, 1.67f, 1.31f, 1.08f, 1.0f, 1.08f, 1.31f, 1.67f, 2.17f, 2.78f, 3.47f, 4.22f, 5.0f, 5.78f, 6.53f, 7.22f, 7.83f, 8.32f, 8.7f, 8.93f, 9.0f, 8.93f, 8.7f, 8.32f, 7.83f, 7.22f, 6.53f, 5.78f };
    static readonly float[] WALK_FB_ROT_FINAL = { 86.55f, 86.07f, 85.55f, 85.0f, 84.45f, 83.93f, 83.45f, 83.02f, 82.67f, 82.41f, 82.25f, 82.2f, 82.25f, 82.41f, 82.67f, 83.02f, 83.45f, 83.93f, 84.45f, 85.0f, 85.55f, 86.07f, 86.55f, 86.98f, 87.33f, 87.59f, 87.75f, 87.8f, 87.75f, 87.59f, 87.33f, 86.98f };
    static readonly float[] WALK_HB_ROT_FINAL = { -1.55f, 0.69f, 2.89f, 5.0f, 6.92f, 8.56f, 9.86f, 10.8f, 11.33f, 11.41f, 11.06f, 10.29f, 9.12f, 7.59f, 5.77f, 3.74f, 1.55f, -0.69f, -2.89f, -5.0f, -6.92f, -8.55f, -9.87f, -10.81f, -11.33f, -11.42f, -11.07f, -10.28f, -9.12f, -7.59f, -5.77f, -3.74f };
    static readonly float[] WALK_SB_ROT = { -0.0f, -1.56f, -3.06f, -4.44f, -5.66f, -6.65f, -7.39f, -7.85f, -8.0f, -7.85f, -7.39f, -6.65f, -5.66f, -4.44f, -3.06f, -1.56f, -0.0f, 1.56f, 3.06f, 4.44f, 5.66f, 6.65f, 7.39f, 7.85f, 8.0f, 7.85f, 7.39f, 6.65f, 5.66f, 4.44f, 3.06f, 1.56f };

    static readonly float[] WALK2_HB_ROT_FINAL = { -1.55f, -0.29f, 0.98f, 2.22f, 3.38f, 4.4f, 5.24f, 5.9f, 6.33f, 6.51f, 6.44f, 6.13f, 5.58f, 4.81f, 3.86f, 2.76f, 1.55f, 0.29f, -0.98f, -2.22f, -3.38f, -4.39f, -5.25f, -5.91f, -6.33f, -6.52f, -6.45f, -6.12f, -5.58f, -4.81f, -3.86f, -2.76f };

    static readonly float[] WALK2_THIGH_F_ROT = { 13.49f, 13.12f, 12.49f, 11.65f, 10.58f, 9.32f, 7.89f, 6.31f, 4.64f, 2.91f, 1.18f, -0.5f, -2.05f, -3.41f, -4.51f, -5.23f, -4.95f, -3.28f, -0.54f, 2.91f, 6.75f, 10.63f, 14.19f, 17.1f, 18.96f, 19.54f, 19.05f, 17.85f, 16.26f, 14.62f, 13.27f, 12.51f };
    static readonly float[] WALK2_THIGH_B_ROT = { -6.98f, -2.0f, 3.72f, 9.75f, 15.65f, 20.98f, 25.3f, 28.2f, 29.26f, 29.04f, 28.42f, 27.41f, 26.04f, 24.36f, 22.41f, 20.25f, 17.91f, 15.46f, 12.97f, 10.45f, 7.98f, 5.6f, 3.35f, 1.26f, -0.65f, -2.34f, -3.83f, -5.08f, -6.11f, -6.92f, -7.53f, -7.96f };

    // ================================================================
    // Run 커브 (걷기 대비 진폭 확대 + 사이클 20프레임으로 단축 — 추측 기반 1차 버전)
    // ================================================================
    static readonly float[] RUN_THIGH_F_ROT = { 10.0f, 4.41f, -0.57f, -5.27f, -10.0f, -16.17f, -22.21f, -24.0f, -20.61f, -14.52f, -6.66f, 2.0f, 13.38f, 27.25f, 39.5f, 46.0f, 44.55f, 37.73f, 28.53f, 20.0f };
    static readonly float[] RUN_THIGH_B_ROT = { -6.66f, 2.0f, 13.38f, 27.25f, 39.5f, 46.0f, 44.55f, 37.73f, 28.53f, 20.0f, 10.0f, 4.41f, -0.57f, -5.27f, -10.0f, -16.17f, -22.21f, -24.0f, -20.61f, -14.52f };
    static readonly float[] RUN_CALF_F_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -0.53f, -5.0f, -16.22f, -31.33f, -45.0f, -57.52f, -68.59f, -72.0f, -62.67f, -45.67f, -30.0f, -18.56f, -8.44f, 0.0f };
    static readonly float[] RUN_CALF_B_ROT = { -45.0f, -57.52f, -68.59f, -72.0f, -62.67f, -45.67f, -30.0f, -18.56f, -8.44f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -0.53f, -5.0f, -16.22f, -31.33f };
    static readonly float[] RUN_FOOT_F_ROT = { -6.0f, -8.6f, -10.97f, -13.36f, -16.0f, -20.6f, -25.52f, -26.0f, -18.67f, -6.89f, 4.0f, 13.33f, 21.78f, 26.0f, 23.33f, 16.44f, 10.0f, 5.7f, 1.85f, -2.0f };
    static readonly float[] RUN_FOOT_B_ROT = { 4.0f, 13.33f, 21.78f, 26.0f, 23.33f, 16.44f, 10.0f, 5.7f, 1.85f, -2.0f, -6.0f, -8.6f, -10.97f, -13.36f, -16.0f, -20.6f, -25.52f, -26.0f, -18.67f, -6.89f };
    static readonly float[] RUN_BODY_ROT = { -9.0f, -11.16f, -13.11f, -14.66f, -15.66f, -16.0f, -15.66f, -14.66f, -13.11f, -11.16f, -9.0f, -6.84f, -4.89f, -3.34f, -2.34f, -2.0f, -2.34f, -3.34f, -4.89f, -6.84f };
    static readonly float[] RUN_BODY_Y = { -0.05f, -0.04f, -0.015f, 0.015f, 0.04f, 0.05f, 0.04f, 0.015f, -0.015f, -0.04f, -0.05f, -0.04f, -0.015f, 0.015f, 0.04f, 0.05f, 0.04f, 0.015f, -0.015f, -0.04f };
    static readonly float[] RUN_BOT_ROT = { 0.0f, 3.71f, 7.05f, 9.71f, 11.41f, 12.0f, 11.41f, 9.71f, 7.05f, 3.71f, 0.0f, -3.71f, -7.05f, -9.71f, -11.41f, -12.0f, -11.41f, -9.71f, -7.05f, -3.71f };
    static readonly float[] RUN_THIGH_B_Y = { -0.606f, -0.596f, -0.571f, -0.541f, -0.516f, -0.506f, -0.516f, -0.541f, -0.571f, -0.596f, -0.606f, -0.596f, -0.571f, -0.541f, -0.516f, -0.506f, -0.516f, -0.541f, -0.571f, -0.596f };
    static readonly float[] RUN_SF_ROT = { 0.0f, 6.18f, 11.76f, 16.18f, 19.02f, 20.0f, 19.02f, 16.18f, 11.76f, 6.18f, 0.0f, -6.18f, -11.76f, -16.18f, -19.02f, -20.0f, -19.02f, -16.18f, -11.76f, -6.18f };
    static readonly float[] RUN_SB_ROT = { 8.0f, 3.06f, -1.4f, -4.94f, -7.22f, -8.0f, -7.22f, -4.94f, -1.4f, 3.06f, 8.0f, 12.94f, 17.4f, 20.94f, 23.22f, 24.0f, 23.22f, 20.94f, 17.4f, 12.94f };

    static readonly float[] RUN2_SB_ROT = { 150.0f, 151.55f, 152.94f, 154.05f, 154.76f, 155.0f, 154.76f, 154.05f, 152.94f, 151.55f, 150.0f, 148.45f, 147.06f, 145.95f, 145.24f, 145.0f, 145.24f, 145.95f, 147.06f, 148.45f };
    static readonly float[] RUN2_FB_ROT = { 15.89f, 16.73f, 17.4f, 17.84f, 18.0f, 17.87f, 17.45f, 16.8f, 15.97f, 15.04f, 14.11f, 13.27f, 12.6f, 12.16f, 12.0f, 12.13f, 12.55f, 13.2f, 14.03f, 14.96f };
    static readonly float[] RUN2_HB_ROT = { 1.13f, 1.58f, 1.88f, 2.0f, 1.92f, 1.65f, 1.22f, 0.67f, 0.06f, -0.56f, -1.13f, -1.58f, -1.88f, -2.0f, -1.92f, -1.65f, -1.22f, -0.67f, -0.06f, 0.56f };

    static readonly float[] RUN3_SB_ROT = { 120.0f, 121.55f, 122.94f, 124.05f, 124.76f, 125.0f, 124.76f, 124.05f, 122.94f, 121.55f, 120.0f, 118.45f, 117.06f, 115.95f, 115.24f, 115.0f, 115.24f, 115.95f, 117.06f, 118.45f };
    static readonly float[] RUN_SF_ROT_HALF = { 0.0f, 3.09f, 5.88f, 8.09f, 9.51f, 10.0f, 9.51f, 8.09f, 5.88f, 3.09f, 0.0f, -3.09f, -5.88f, -8.09f, -9.51f, -10.0f, -9.51f, -8.09f, -5.88f, -3.09f };  // Run2/Run3 전용, RUN_SF_ROT 진폭의 절반

    static readonly float[] RUN_FF_ROT = { 30.0f, 39.27f, 47.63f, 54.27f, 58.53f, 60.0f, 58.53f, 54.27f, 47.63f, 39.27f, 30.0f, 20.73f, 12.37f, 5.73f, 1.47f, 0.0f, 1.47f, 5.73f, 12.37f, 20.73f };  // Run/Run2/Run3 공통, Forearm_F 0~60 흔들림

    static readonly float[] RUN_BOT_ROT_CANCEL = { 9.0f, 11.16f, 13.11f, 14.66f, 15.66f, 16.0f, 15.66f, 14.66f, 13.11f, 11.16f, 9.0f, 6.84f, 4.89f, 3.34f, 2.34f, 2.0f, 2.34f, 3.34f, 4.89f, 6.84f };  // Run/Run2/Run3 공통: Bottom이 Body 회전을 상쇄
    static readonly float[] RUN_THIGH_F_ROT_COMP = { 1.0f, -3.04f, -6.63f, -10.22f, -14.25f, -20.17f, -26.46f, -28.95f, -26.67f, -21.97f, -15.66f, -8.55f, 1.44f, 14.2f, 25.75f, 32.0f, 30.8f, 24.68f, 16.59f, 9.45f };  // 상쇄분 보정 (다리 모션 그대로 보이게)
    static readonly float[] RUN_THIGH_B_ROT_COMP = { -15.66f, -5.45f, 7.32f, 22.3f, 35.25f, 42.0f, 40.3f, 32.78f, 22.47f, 12.55f, 1.0f, -6.14f, -12.51f, -18.32f, -23.75f, -30.17f, -35.96f, -37.05f, -32.55f, -25.07f };
    static readonly float[] RUN_FB_ROT_CANCEL_SB = { 100.0f, 104.94f, 109.4f, 112.94f, 115.22f, 116.0f, 115.22f, 112.94f, 109.4f, 104.94f, 100.0f, 92.28f, 85.31f, 79.78f, 76.22f, 75.0f, 76.22f, 79.78f, 85.31f, 92.28f };  // Run1 전용: SB>base 쪽만 1.5625배로 더 상쇄, SB<base 쪽은 기존 1배 유지

    // ================================================================
    // Attack 커브 (일반공격: 기모으기 -> 크게 스윙업 -> 플레이어 발사 -> 복귀, 26프레임 논루프, 추측 기반 1차 버전)
    // ================================================================
    static readonly float[] ATK_SB_ROT = { 0.0f, -6.8f, -22.4f, -39.6f, -51.2f, -50.0f, -36.418f, -16.827f, 7.043f, 33.462f, 60.697f, 87.019f, 110.697f, 130.0f, 144.225f, 154.228f, 160.618f, 164.006f, 165.0f };
    static readonly float[] ATK_FB_ROT = { 75.0f, 77.237f, 82.311f, 87.766f, 91.148f, 90.0f, 84.359f, 76.38f, 66.788f, 56.308f, 45.664f, 35.582f, 26.785f, 20.0f, 16.247f, 15.308f, 16.046f, 17.322f, 18.0f };
    static readonly float[] ATK_HB_ROT = { 0.0f, -1.225f, -4.074f, -7.311f, -9.698f, -10.0f, -8.194f, -5.315f, -1.711f, 2.269f, 6.279f, 9.969f, 12.992f, 15.0f, 15.158f, 13.533f, 11.129f, 8.95f, 8.0f };
    static readonly float[] ATK_SF_ROT = { 0.0f, 2.388f, 7.963f, 14.345f, 19.151f, 20.0f, 16.914f, 11.901f, 5.563f, -1.5f, -8.688f, -15.401f, -21.039f, -25.0f, -26.143f, -24.641f, -21.867f, -19.196f, -18.0f };
    static readonly float[] ATK_FF_ROT = { 25.0f, 26.163f, 28.889f, 32.034f, 34.452f, 35.0f, 33.695f, 31.5f, 28.69f, 25.538f, 22.32f, 19.308f, 16.776f, 15.0f, 14.475f, 15.114f, 16.316f, 17.479f, 18.0f };
    static readonly float[] ATK_BODY_Y = { 0.0f, -0.017f, -0.056f, -0.102f, -0.138f, -0.15f, -0.137f, -0.113f, -0.081f, -0.044f, -0.006f, 0.029f, 0.059f, 0.08f, 0.087f, 0.081f, 0.068f, 0.056f, 0.05f };
    static readonly float[] ATK_BODY_ROT = { 0.0f, -1.806f, -6.018f, -10.828f, -14.425f, -15.0f, -12.579f, -8.695f, -3.8f, 1.654f, 7.213f, 12.425f, 16.839f, 20.0f, 21.11f, 20.309f, 18.553f, 16.798f, 16.0f };
    static readonly float[] ATK_BODY_X = { 0.0f, -0.007f, -0.023f, -0.041f, -0.052f, -0.05f, -0.034f, -0.011f, 0.017f, 0.048f, 0.079f, 0.107f, 0.132f, 0.15f, 0.157f, 0.153f, 0.144f, 0.134f, 0.13f };
    static readonly float[] ATK_BOT_ROT = { 0.0f, 1.084f, 3.611f, 6.497f, 8.655f, 9.0f, 7.542f, 5.2f, 2.247f, -1.038f, -4.382f, -7.507f, -10.139f, -12.0f, -12.574f, -11.941f, -10.721f, -9.534f, -9.0f };
    static readonly float[] ATK_THIGH_F_ROT = { 10.0f, 9.228f, 7.445f, 5.447f, 4.033f, 4.0f, 5.354f, 7.418f, 9.966f, 12.769f, 15.602f, 18.236f, 20.444f, 22.0f, 22.426f, 21.775f, 20.61f, 19.497f, 19.0f };
    static readonly float[] ATK_CALF_F_ROT = { 0.0f, -0.795f, -2.705f, -5.018f, -7.02f, -8.0f, -7.995f, -7.594f, -6.905f, -6.038f, -5.105f, -4.214f, -3.476f, -3.0f, -2.907f, -3.13f, -3.5f, -3.847f, -4.0f };
    static readonly float[] ATK_FOOT_F_ROT = { 0.0f, -0.618f, -2.055f, -3.683f, -4.874f, -5.0f, -4.054f, -2.565f, -0.706f, 1.346f, 3.418f, 5.334f, 6.92f, 8.0f, 8.18f, 7.498f, 6.425f, 5.435f, 5.0f };
    static readonly float[] ATK_THIGH_B_ROT = { -6.0f, -6.769f, -8.708f, -11.262f, -13.877f, -16.0f, -17.761f, -19.606f, -21.456f, -23.231f, -24.852f, -26.24f, -27.316f, -28.0f, -27.671f, -26.22f, -24.334f, -22.698f, -22.0f };
    static readonly float[] ATK_CALF_B_ROT = { 0.0f, -0.732f, -2.597f, -5.095f, -7.729f, -10.0f, -12.041f, -14.248f, -16.513f, -18.731f, -20.795f, -22.599f, -24.036f, -25.0f, -24.819f, -23.386f, -21.444f, -19.735f, -19.0f };
    static readonly float[] ATK_FOOT_B_ROT = { 0.0f, -1.47f, -4.889f, -8.773f, -11.638f, -12.0f, -9.844f, -6.418f, -2.129f, 2.615f, 7.408f, 11.841f, 15.508f, 18.0f, 18.404f, 16.81f, 14.313f, 12.011f, 11.0f };
    static readonly float[] ATK_BODY_SCALE_X = { 1f, 1f, 0.2f, -1f, -1f, -1f, -1f, -1f, -1f, -0.2f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };  // Body 루트 X스케일 반전으로 기모으기 때 잠깐 뒤돌아봄 (기본 스케일 1 가정)

    // ================================================================
    // Skill1 커브 (2연타: 크게 젖혀찍기 -> 점프 내려찍기, 34프레임 논루프, 추측 기반)
    // ================================================================
    static readonly float[] SK1_SB_ROT = { 0.0f, 12.458f, 44.656f, 88.829f, 137.215f, 182.048f, 215.564f, 230.0f, 218.303f, 185.048f, 139.913f, 92.574f, 52.711f, 30.0f, 27.993f, 40.269f, 61.549f, 86.553f, 110.0f, 136.673f, 170.808f, 206.307f, 237.071f, 257.001f, 260.0f, 240.872f, 203.457f, 155.208f, 103.58f, 56.026f, 20.0f, 5.062f, 6.42f, 10.0f };
    static readonly float[] SK1_FB_ROT = { 75.0f, 71.629f, 62.948f, 51.102f, 38.238f, 26.503f, 18.041f, 15.0f, 19.766f, 31.184f, 46.084f, 61.299f, 73.66f, 80.0f, 78.24f, 70.393f, 59.425f, 48.305f, 40.0f, 33.64f, 26.97f, 20.881f, 16.263f, 14.006f, 15.0f, 20.467f, 29.938f, 41.771f, 54.321f, 65.945f, 75.0f, 79.506f, 80.309f, 80.0f };
    static readonly float[] SK1_HB_ROT = { 0.0f, -1.019f, -3.602f, -7.034f, -10.603f, -13.596f, -15.299f, -15.0f, -11.528f, -5.164f, 2.631f, 10.398f, 16.674f, 20.0f, 19.229f, 15.251f, 9.658f, 4.044f, 0.0f, -2.866f, -5.758f, -8.295f, -10.101f, -10.795f, -10.0f, -6.852f, -1.481f, 5.0f, 11.481f, 16.852f, 20.0f, 16.852f, 9.259f, 5.0f };
    static readonly float[] SK1_BODY_Y = { 0.0f, -0.007f, -0.025f, -0.05f, -0.077f, -0.1f, -0.116f, -0.12f, -0.109f, -0.087f, -0.057f, -0.022f, 0.015f, 0.05f, 0.088f, 0.132f, 0.176f, 0.217f, 0.25f, 0.28f, 0.311f, 0.339f, 0.358f, 0.364f, 0.35f, 0.308f, 0.241f, 0.16f, 0.077f, 0.003f, -0.05f, -0.054f, -0.022f, 0.0f };
    static readonly float[] SK1_BODY_ROT = { 0.0f, -1.138f, -4.039f, -7.932f, -12.046f, -15.609f, -17.851f, -18.0f, -15.045f, -9.348f, -2.203f, 5.099f, 11.264f, 15.0f, 15.633f, 13.989f, 11.029f, 7.713f, 5.0f, 2.504f, -0.488f, -3.506f, -6.077f, -7.733f, -8.0f, -6.237f, -2.691f, 1.771f, 6.284f, 9.982f, 12.0f, 9.284f, 3.309f, 0.0f };
    static readonly float[] SK1_BODY_X = { 0.0f, -0.003f, -0.011f, -0.021f, -0.032f, -0.04f, -0.043f, -0.04f, -0.025f, -0.001f, 0.03f, 0.06f, 0.085f, 0.1f, 0.101f, 0.09f, 0.075f, 0.059f, 0.05f, 0.046f, 0.042f, 0.04f, 0.041f, 0.044f, 0.05f, 0.062f, 0.08f, 0.1f, 0.121f, 0.138f, 0.15f, 0.146f, 0.13f, 0.12f };
    static readonly float[] SK1_BOT_ROT = { 0.0f, 0.639f, 2.265f, 4.444f, 6.742f, 8.724f, 9.955f, 10.0f, 8.276f, 4.984f, 0.867f, -3.331f, -6.868f, -9.0f, -9.336f, -8.35f, -6.596f, -4.628f, -3.0f, -1.477f, 0.354f, 2.205f, 3.788f, 4.816f, 5.0f, 3.957f, 1.84f, -0.833f, -3.543f, -5.772f, -7.0f, -5.432f, -1.938f, 0.0f };
    static readonly float[] SK1_SF_ROT = { 0.0f, 1.019f, 3.602f, 7.034f, 10.603f, 13.596f, 15.299f, 15.0f, 11.655f, 5.569f, -1.949f, -9.589f, -16.043f, -20.0f, -20.996f, -19.862f, -17.229f, -13.731f, -10.0f, -5.079f, 1.566f, 8.665f, 14.949f, 19.151f, 20.0f, 16.107f, 8.21f, -1.771f, -11.914f, -20.297f, -25.0f, -19.506f, -6.975f, 0.0f };
    static readonly float[] SK1_FF_ROT = { 25.0f, 25.648f, 27.296f, 29.501f, 31.818f, 33.802f, 35.011f, 35.0f, 33.174f, 29.737f, 25.446f, 21.055f, 17.322f, 15.0f, 14.429f, 15.124f, 16.604f, 18.389f, 20.0f, 21.803f, 24.175f, 26.648f, 28.754f, 30.027f, 30.0f, 28.094f, 24.568f, 20.208f, 15.802f, 12.137f, 10.0f, 12.099f, 17.16f, 20.0f };
    static readonly float[] SK1_THIGH_F_ROT = { 10.0f, 9.463f, 8.1f, 6.286f, 4.395f, 2.801f, 1.878f, 2.0f, 3.451f, 6.017f, 9.327f, 13.009f, 16.69f, 20.0f, 23.189f, 26.567f, 29.851f, 32.756f, 35.0f, 36.864f, 38.653f, 40.114f, 40.993f, 41.04f, 40.0f, 37.454f, 33.519f, 28.75f, 23.704f, 18.935f, 15.0f, 12.222f, 10.556f, 10.0f };
    static readonly float[] SK1_CALF_F_ROT = { 0.0f, -0.314f, -1.127f, -2.248f, -3.488f, -4.654f, -5.555f, -6.0f, -5.318f, -3.524f, -1.456f, 0.046f, 0.144f, -2.0f, -7.97f, -17.376f, -28.097f, -38.012f, -45.0f, -49.492f, -53.172f, -55.801f, -57.141f, -56.954f, -55.0f, -50.49f, -43.457f, -34.896f, -25.802f, -17.172f, -10.0f, -4.691f, -1.235f, 0.0f };
    static readonly float[] SK1_FOOT_F_ROT = { 0.0f, -0.278f, -0.981f, -1.913f, -2.878f, -3.678f, -4.118f, -4.0f, -3.179f, -1.765f, 0.051f, 2.077f, 4.124f, 6.0f, 7.867f, 9.888f, 11.876f, 13.642f, 15.0f, 16.246f, 17.599f, 18.756f, 19.411f, 19.26f, 18.0f, 15.021f, 10.444f, 5.062f, -0.333f, -4.951f, -8.0f, -6.815f, -2.519f, 0.0f };
    static readonly float[] SK1_THIGH_B_ROT = { -6.0f, -6.274f, -7.021f, -8.131f, -9.495f, -11.001f, -12.54f, -14.0f, -15.437f, -16.975f, -18.607f, -20.326f, -22.126f, -24.0f, -26.133f, -28.548f, -30.997f, -33.231f, -35.0f, -36.577f, -38.219f, -39.653f, -40.609f, -40.815f, -40.0f, -37.911f, -34.716f, -30.771f, -26.432f, -22.056f, -18.0f, -13.21f, -8.272f, -6.0f };
    static readonly float[] SK1_CALF_B_ROT = { 0.0f, -0.255f, -0.958f, -2.018f, -3.344f, -4.844f, -6.427f, -8.0f, -9.427f, -10.782f, -12.29f, -14.179f, -16.673f, -20.0f, -25.011f, -31.651f, -38.785f, -45.28f, -50.0f, -53.397f, -56.441f, -58.778f, -60.054f, -59.913f, -58.0f, -53.684f, -47.123f, -39.146f, -30.58f, -22.255f, -15.0f, -8.247f, -2.457f, 0.0f };
    static readonly float[] SK1_FOOT_B_ROT = { 0.0f, -0.695f, -2.453f, -4.784f, -7.194f, -9.195f, -10.294f, -10.0f, -7.675f, -3.543f, 1.593f, 6.93f, 11.667f, 15.0f, 16.573f, 16.923f, 16.586f, 16.1f, 16.0f, 16.638f, 17.724f, 18.83f, 19.529f, 19.394f, 18.0f, 14.699f, 9.704f, 3.875f, -1.926f, -6.838f, -10.0f, -8.296f, -3.037f, 0.0f };

    // ================================================================
    // Skill2 커브 (3연타: 돌진 몸빵 -> 스윙 -> 내려찍기, 36프레임 논루프, 추측 기반)
    // ================================================================
    static readonly float[] SK2_SB_ROT = { 0.0f, 1.36f, 4.48f, 7.92f, 10.24f, 10.0f, 2.255f, -12.036f, -25.455f, -30.582f, -20.0f, 12.643f, 62.357f, 119.545f, 174.613f, 217.963f, 240.0f, 227.411f, 185.615f, 133.113f, 88.407f, 70.0f, 84.659f, 119.697f, 164.432f, 208.182f, 240.265f, 250.0f, 227.2f, 178.2f, 117.1f, 58.0f, 15.0f, -0.259f, 3.093f, 8.0f };
    static readonly float[] SK2_FB_ROT = { 75.0f, 75.28f, 76.04f, 77.16f, 78.52f, 80.0f, 82.873f, 87.218f, 91.127f, 92.691f, 90.0f, 81.406f, 68.215f, 52.955f, 38.148f, 26.322f, 20.0f, 22.487f, 32.335f, 44.938f, 55.695f, 60.0f, 56.162f, 47.323f, 36.136f, 25.253f, 17.323f, 15.0f, 20.853f, 33.322f, 48.865f, 63.938f, 75.0f, 79.278f, 78.972f, 78.0f };
    static readonly float[] SK2_HB_ROT = { 0.0f, 0.36f, 1.28f, 2.52f, 3.84f, 5.0f, 6.378f, 8.135f, 9.702f, 10.513f, 10.0f, 7.508f, 3.401f, -1.364f, -5.825f, -9.024f, -10.0f, -7.109f, -0.873f, 6.418f, 12.473f, 15.0f, 13.148f, 8.519f, 2.5f, -3.519f, -8.148f, -10.0f, -7.66f, -1.98f, 5.03f, 11.36f, 15.0f, 12.611f, 6.472f, 3.0f };
    static readonly float[] SK2_BODY_X = { 0.0f, 0.011f, 0.039f, 0.077f, 0.116f, 0.15f, 0.181f, 0.216f, 0.248f, 0.271f, 0.28f, 0.271f, 0.249f, 0.219f, 0.189f, 0.163f, 0.15f, 0.154f, 0.172f, 0.195f, 0.214f, 0.22f, 0.21f, 0.19f, 0.166f, 0.142f, 0.125f, 0.12f, 0.135f, 0.166f, 0.203f, 0.235f, 0.25f, 0.226f, 0.177f, 0.15f };
    static readonly float[] SK2_BODY_Y = { 0.0f, -0.006f, -0.019f, -0.034f, -0.046f, -0.05f, -0.041f, -0.023f, -0.002f, 0.015f, 0.02f, 0.01f, -0.01f, -0.034f, -0.058f, -0.075f, -0.08f, -0.064f, -0.031f, 0.007f, 0.038f, 0.05f, 0.037f, 0.008f, -0.028f, -0.064f, -0.091f, -0.1f, -0.082f, -0.041f, 0.009f, 0.054f, 0.08f, 0.065f, 0.024f, 0.0f };
    static readonly float[] SK2_BODY_ROT = { 0.0f, -0.544f, -1.952f, -3.888f, -6.016f, -8.0f, -10.483f, -13.689f, -16.653f, -18.412f, -18.0f, -14.553f, -8.727f, -1.841f, 4.788f, 9.841f, 12.0f, 9.323f, 2.561f, -5.561f, -12.323f, -15.0f, -12.652f, -7.182f, -0.136f, 6.939f, 12.5f, 15.0f, 12.991f, 7.427f, 0.368f, -6.127f, -10.0f, -8.241f, -3.009f, 0.0f };
    static readonly float[] SK2_BOT_ROT = { 0.0f, 0.784f, 2.592f, 4.608f, 6.016f, 6.0f, 3.711f, -0.308f, -4.783f, -8.438f, -10.0f, -8.977f, -6.273f, -2.705f, 0.909f, 3.75f, 5.0f, 3.75f, 0.511f, -3.402f, -6.677f, -8.0f, -6.887f, -4.249f, -0.864f, 2.492f, 5.039f, 6.0f, 4.502f, 1.015f, -3.224f, -6.975f, -9.0f, -7.0f, -2.5f, 0.0f };
    static readonly float[] SK2_SF_ROT = { 0.0f, -1.72f, -5.76f, -10.44f, -14.08f, -15.0f, -12.196f, -6.789f, -0.284f, 5.815f, 10.0f, 12.643f, 14.933f, 16.591f, 17.34f, 16.902f, 15.0f, 9.658f, 0.847f, -8.793f, -16.622f, -20.0f, -17.844f, -12.027f, -4.341f, 3.421f, 9.465f, 12.0f, 9.236f, 2.291f, -6.273f, -13.891f, -18.0f, -14.0f, -5.0f, 0.0f };
    static readonly float[] SK2_FF_ROT = { 25.0f, 23.88f, 21.24f, 18.16f, 15.72f, 15.0f, 16.589f, 19.767f, 23.651f, 27.356f, 30.0f, 31.822f, 33.519f, 34.886f, 35.724f, 35.829f, 35.0f, 32.12f, 27.196f, 21.713f, 17.153f, 15.0f, 15.758f, 18.364f, 21.955f, 25.667f, 28.636f, 30.0f, 29.027f, 26.272f, 22.805f, 19.692f, 18.0f, 19.537f, 23.046f, 25.0f };
    static readonly float[] SK2_THIGH_F_ROT = { 10.0f, 7.24f, 0.72f, -6.92f, -13.04f, -15.0f, -11.011f, -2.833f, 6.851f, 15.356f, 20.0f, 20.126f, 17.646f, 13.727f, 9.535f, 6.237f, 5.0f, 6.841f, 10.984f, 15.907f, 20.087f, 22.0f, 21.178f, 18.694f, 15.341f, 11.912f, 9.201f, 8.0f, 9.092f, 11.973f, 15.509f, 18.563f, 20.0f, 17.519f, 12.648f, 10.0f };
    static readonly float[] SK2_CALF_F_ROT = { 0.0f, -3.04f, -10.32f, -19.08f, -26.56f, -30.0f, -28.011f, -22.433f, -15.349f, -8.844f, -5.0f, -4.133f, -4.761f, -6.273f, -8.057f, -9.503f, -10.0f, -9.083f, -7.187f, -5.049f, -3.408f, -3.0f, -4.179f, -6.434f, -9.205f, -11.929f, -14.048f, -15.0f, -14.376f, -12.511f, -9.957f, -7.269f, -5.0f, -2.87f, -0.88f, 0.0f };
    static readonly float[] SK2_FOOT_F_ROT = { 0.0f, -1.72f, -5.76f, -10.44f, -14.08f, -15.0f, -11.978f, -6.135f, 0.698f, 6.687f, 10.0f, 10.231f, 8.7f, 6.159f, 3.36f, 1.056f, 0.0f, 0.788f, 2.903f, 5.424f, 7.43f, 8.0f, 6.696f, 4.145f, 1.023f, -1.993f, -4.226f, -5.0f, -3.424f, 0.111f, 4.357f, 8.069f, 10.0f, 7.685f, 2.731f, 0.0f };
    static readonly float[] SK2_THIGH_B_ROT = { -6.0f, -2.632f, 5.344f, 14.736f, 22.352f, 25.0f, 20.773f, 11.799f, 0.939f, -8.948f, -15.0f, -16.713f, -16.128f, -14.205f, -11.902f, -10.181f, -10.0f, -12.171f, -16.022f, -20.287f, -23.702f, -25.0f, -23.652f, -20.552f, -16.568f, -12.569f, -9.423f, -8.0f, -9.321f, -12.852f, -17.121f, -20.66f, -22.0f, -17.741f, -10.093f, -6.0f };
    static readonly float[] SK2_CALF_B_ROT = { 0.0f, -3.512f, -11.936f, -22.104f, -30.848f, -35.0f, -33.039f, -27.076f, -19.393f, -12.274f, -8.0f, -6.668f, -6.613f, -7.477f, -8.902f, -10.529f, -12.0f, -13.884f, -16.524f, -19.222f, -21.28f, -22.0f, -21.01f, -18.808f, -16.0f, -13.192f, -10.99f, -10.0f, -11.124f, -13.989f, -17.293f, -19.731f, -20.0f, -14.259f, -4.907f, 0.0f };
    static readonly float[] SK2_FOOT_B_ROT = { 0.0f, 2.032f, 6.816f, 12.384f, 16.768f, 18.0f, 14.783f, 8.428f, 0.881f, -5.91f, -10.0f, -11.397f, -11.572f, -10.773f, -9.246f, -7.239f, -5.0f, -1.422f, 3.807f, 9.247f, 13.458f, 15.0f, 13.12f, 8.835f, 3.364f, -2.077f, -6.271f, -8.0f, -5.93f, -0.801f, 5.492f, 11.057f, 14.0f, 10.815f, 3.852f, 0.0f };

    // ================================================================
    // Skill3 커브 (캐릭터 파트: 내리찍기만. 돌기둥/충격파 VFX는 별도 시스템 필요, 22프레임 논루프)
    // ================================================================
    static readonly float[] SK3_SB_ROT = { 0.0f, 10.21f, 37.148f, 75.278f, 119.062f, 162.964f, 201.445f, 228.97f, 240.0f, 233.027f, 212.677f, 182.618f, 146.514f, 108.034f, 70.844f, 38.611f, 15.0f, 2.391f, -1.535f, -0.157f, 3.148f, 5.0f };
    static readonly float[] SK3_FB_ROT = { 75.0f, 72.241f, 64.961f, 54.653f, 42.812f, 30.933f, 20.508f, 13.032f, 10.0f, 11.876f, 17.437f, 25.643f, 35.457f, 45.838f, 55.748f, 64.149f, 70.0f, 72.188f, 71.286f, 68.791f, 66.197f, 65.0f };
    static readonly float[] SK3_HB_ROT = { 0.0f, -0.781f, -2.812f, -5.625f, -8.75f, -11.719f, -14.062f, -15.312f, -15.0f, -12.749f, -8.846f, -3.813f, 1.827f, 7.551f, 12.837f, 17.16f, 20.0f, 20.191f, 17.865f, 14.443f, 11.348f, 10.0f };
    static readonly float[] SK3_BODY_Y = { 0.0f, 0.008f, 0.028f, 0.056f, 0.087f, 0.117f, 0.141f, 0.153f, 0.15f, 0.128f, 0.09f, 0.041f, -0.014f, -0.071f, -0.124f, -0.169f, -0.2f, -0.21f, -0.199f, -0.179f, -0.159f, -0.15f };
    static readonly float[] SK3_BODY_ROT = { 0.0f, -1.03f, -3.711f, -7.427f, -11.562f, -15.503f, -18.633f, -20.337f, -20.0f, -17.207f, -12.365f, -6.094f, 0.986f, 8.255f, 15.093f, 20.882f, 25.0f, 26.449f, 25.455f, 23.237f, 21.012f, 20.0f };
    static readonly float[] SK3_BODY_X = { 0.0f, -0.003f, -0.01f, -0.02f, -0.031f, -0.042f, -0.049f, -0.053f, -0.05f, -0.04f, -0.023f, -0.002f, 0.021f, 0.045f, 0.068f, 0.087f, 0.1f, 0.104f, 0.1f, 0.092f, 0.084f, 0.08f };
    static readonly float[] SK3_BOT_ROT = { 0.0f, 0.625f, 2.25f, 4.5f, 7.0f, 9.375f, 11.25f, 12.25f, 12.0f, 10.242f, 7.221f, 3.321f, -1.077f, -5.59f, -9.837f, -13.434f, -16.0f, -16.919f, -16.329f, -14.979f, -13.62f, -13.0f };
    static readonly float[] SK3_SF_ROT = { 0.0f, 1.03f, 3.711f, 7.427f, 11.562f, 15.503f, 18.633f, 20.337f, 20.0f, 17.207f, 12.365f, 6.094f, -0.986f, -8.255f, -15.093f, -20.882f, -25.0f, -26.449f, -25.455f, -23.237f, -21.012f, -20.0f };
    static readonly float[] SK3_FF_ROT = { 25.0f, 25.498f, 26.797f, 28.604f, 30.625f, 32.568f, 34.141f, 35.049f, 35.0f, 33.805f, 31.662f, 28.859f, 25.683f, 22.421f, 19.362f, 16.792f, 15.0f, 14.475f, 15.114f, 16.316f, 17.479f, 18.0f };
    static readonly float[] SK3_THIGH_F_ROT = { 10.0f, 9.253f, 7.305f, 4.595f, 1.562f, -1.353f, -3.711f, -5.073f, -5.0f, -3.22f, -0.037f, 4.13f, 8.861f, 13.733f, 18.328f, 22.223f, 25.0f, 26.017f, 25.439f, 24.053f, 22.644f, 22.0f };
    static readonly float[] SK3_CALF_F_ROT = { 0.0f, -0.026f, -0.117f, -0.29f, -0.562f, -0.952f, -1.477f, -2.153f, -3.0f, -4.175f, -5.728f, -7.517f, -9.399f, -11.231f, -12.871f, -14.175f, -15.0f, -14.825f, -13.628f, -12.018f, -10.606f, -10.0f };
    static readonly float[] SK3_FOOT_F_ROT = { 0.0f, -0.446f, -1.602f, -3.19f, -4.938f, -6.567f, -7.805f, -8.374f, -8.0f, -6.445f, -3.871f, -0.598f, 3.053f, 6.759f, 10.2f, 13.054f, 15.0f, 15.366f, 14.237f, 12.425f, 10.742f, 10.0f };
    static readonly float[] SK3_THIGH_B_ROT = { -6.0f, -5.664f, -4.812f, -3.68f, -2.5f, -1.508f, -0.938f, -1.023f, -2.0f, -4.15f, -7.37f, -11.319f, -15.654f, -20.032f, -24.111f, -27.547f, -30.0f, -30.766f, -29.921f, -28.294f, -26.711f, -26.0f };
    static readonly float[] SK3_CALF_B_ROT = { 0.0f, -0.01f, -0.078f, -0.264f, -0.625f, -1.221f, -2.109f, -3.35f, -5.0f, -7.35f, -10.468f, -14.08f, -17.913f, -21.695f, -25.153f, -28.012f, -30.0f, -30.311f, -28.94f, -26.814f, -24.858f, -24.0f };
    static readonly float[] SK3_FOOT_B_ROT = { 0.0f, -0.566f, -2.031f, -4.043f, -6.25f, -8.301f, -9.844f, -10.527f, -10.0f, -7.956f, -4.599f, -0.341f, 4.404f, 9.223f, 13.704f, 17.434f, 20.0f, 20.558f, 19.217f, 16.998f, 14.919f, 14.0f };

    // ================================================================
    // Buff 커브 (웅크렸다 가슴 펴며 일어남, 24프레임 논루프)
    // ================================================================
    static readonly float[] BUFF_BODY_Y = { 0.0f, -0.007f, -0.028f, -0.057f, -0.093f, -0.132f, -0.17f, -0.204f, -0.231f, -0.247f, -0.25f, -0.239f, -0.219f, -0.191f, -0.156f, -0.118f, -0.077f, -0.036f, 0.004f, 0.041f, 0.073f, 0.098f, 0.114f, 0.12f };
    static readonly float[] BUFF_BODY_ROT = { 0.0f, -0.759f, -2.809f, -5.811f, -9.426f, -13.315f, -17.139f, -20.559f, -23.235f, -24.828f, -25.0f, -23.771f, -21.517f, -18.435f, -14.719f, -10.565f, -6.169f, -1.724f, 2.572f, 6.525f, 9.94f, 12.621f, 14.373f, 15.0f };
    static readonly float[] BUFF_BODY_X = { 0.0f, -0.001f, -0.005f, -0.011f, -0.018f, -0.025f, -0.032f, -0.039f, -0.045f, -0.049f, -0.05f, -0.049f, -0.047f, -0.043f, -0.039f, -0.034f, -0.028f, -0.022f, -0.016f, -0.011f, -0.007f, -0.003f, -0.001f, 0.0f };
    static readonly float[] BUFF_SB_ROT = { 0.0f, -0.951f, -3.471f, -7.059f, -11.214f, -15.435f, -19.221f, -22.071f, -23.485f, -22.962f, -20.0f, -14.274f, -6.127f, 3.94f, 15.427f, 27.832f, 40.654f, 53.393f, 65.546f, 76.613f, 86.093f, 93.485f, 98.288f, 100.0f };
    static readonly float[] BUFF_FB_ROT = { 75.0f, 75.876f, 78.226f, 81.633f, 85.678f, 89.946f, 94.017f, 97.476f, 99.904f, 100.885f, 100.0f, 97.154f, 92.738f, 87.064f, 80.446f, 73.196f, 65.627f, 58.052f, 50.784f, 44.136f, 38.421f, 33.951f, 31.04f, 30.0f };
    static readonly float[] BUFF_HB_ROT = { 0.0f, -0.319f, -1.179f, -2.434f, -3.937f, -5.543f, -7.106f, -8.479f, -9.517f, -10.072f, -10.0f, -9.293f, -8.103f, -6.525f, -4.651f, -2.577f, -0.395f, 1.8f, 3.915f, 5.855f, 7.528f, 8.839f, 9.694f, 10.0f };
    static readonly float[] BUFF_SF_ROT = { 0.0f, -0.185f, -0.725f, -1.597f, -2.776f, -4.239f, -5.963f, -7.925f, -10.101f, -12.467f, -15.0f, -17.981f, -21.603f, -25.714f, -30.161f, -34.789f, -39.447f, -43.981f, -48.237f, -52.063f, -55.306f, -57.812f, -59.427f, -60.0f };
    static readonly float[] BUFF_FF_ROT = { 25.0f, 25.759f, 27.809f, 30.811f, 34.426f, 38.315f, 42.139f, 45.559f, 48.235f, 49.828f, 50.0f, 48.771f, 46.517f, 43.435f, 39.719f, 35.565f, 31.169f, 26.724f, 22.428f, 18.475f, 15.06f, 12.379f, 10.627f, 10.0f };
    static readonly float[] BUFF_BOT_ROT = { 0.0f, 0.459f, 1.699f, 3.514f, 5.697f, 8.043f, 10.346f, 12.399f, 13.997f, 14.932f, 15.0f, 14.209f, 12.784f, 10.849f, 8.522f, 5.927f, 3.183f, 0.412f, -2.265f, -4.726f, -6.852f, -8.52f, -9.61f, -10.0f };
    static readonly float[] BUFF_THIGH_F_ROT = { 10.0f, 10.568f, 12.108f, 14.375f, 17.123f, 20.109f, 23.085f, 25.808f, 28.031f, 29.51f, 30.0f, 29.555f, 28.474f, 26.872f, 24.867f, 22.575f, 20.115f, 17.604f, 15.157f, 12.893f, 10.928f, 9.379f, 8.364f, 8.0f };
    static readonly float[] BUFF_CALF_F_ROT = { 0.0f, -1.26f, -4.68f, -9.72f, -15.84f, -22.5f, -29.16f, -35.28f, -40.32f, -43.74f, -45.0f, -44.242f, -42.132f, -38.917f, -34.841f, -30.15f, -25.091f, -19.909f, -14.85f, -10.159f, -6.083f, -2.868f, -0.758f, 0.0f };
    static readonly float[] BUFF_FOOT_F_ROT = { 0.0f, 0.336f, 1.248f, 2.592f, 4.224f, 6.0f, 7.776f, 9.408f, 10.752f, 11.664f, 12.0f, 11.798f, 11.235f, 10.378f, 9.291f, 8.04f, 6.691f, 5.309f, 3.96f, 2.709f, 1.622f, 0.765f, 0.202f, 0.0f };
    static readonly float[] BUFF_THIGH_B_ROT = { -6.0f, -6.812f, -9.016f, -12.264f, -16.208f, -20.5f, -24.792f, -28.736f, -31.984f, -34.188f, -35.0f, -34.512f, -33.152f, -31.08f, -28.453f, -25.43f, -22.17f, -18.83f, -15.57f, -12.547f, -9.92f, -7.848f, -6.488f, -6.0f };
    static readonly float[] BUFF_CALF_B_ROT = { 0.0f, -1.4f, -5.2f, -10.8f, -17.6f, -25.0f, -32.4f, -39.2f, -44.8f, -48.6f, -50.0f, -49.158f, -46.814f, -43.241f, -38.712f, -33.5f, -27.879f, -22.121f, -16.5f, -11.288f, -6.759f, -3.186f, -0.842f, 0.0f };
    static readonly float[] BUFF_FOOT_B_ROT = { 0.0f, 0.42f, 1.56f, 3.24f, 5.28f, 7.5f, 9.72f, 11.76f, 13.44f, 14.58f, 15.0f, 14.747f, 14.044f, 12.972f, 11.614f, 10.05f, 8.364f, 6.636f, 4.95f, 3.386f, 2.028f, 0.956f, 0.253f, 0.0f };

    // ================================================================
    // Generate Walk
    // ================================================================
    [MenuItem("Tools/MainBoss2/Generate Walk")]
    public static void GenerateWalk()
    {
        const float DUR = 32 / 12f;
        var clip = MakeClip("MainBoss2_Walk", 32, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 32);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", WALK_BODY_Y); L(BODY, "localPosition.x", 0f); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", WALK_BODY_ROT);
        L(HEAD, "localPosition.x", -0.275f); L(HEAD, "localPosition.y", 1.025f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);

        // 앞팔: SF 살짝 스윙, FF/HF 고정
        L(SF, "localPosition.x", -0.856f); L(SF, "localPosition.y", 0.601f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", WALK_SF_ROT);
        L(FF, "localPosition.x", -0.245f); L(FF, "localPosition.y", -0.562f); L(FF, "localPosition.z", 0f);
        L(FF, "localEulerAngles.z", 5.682f);
        L(HF, "localPosition.x", -0.093f); L(HF, "localPosition.y", -0.678f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        // 뒷팔: SB 살짝 반대 스윙, 도끼 고정
        L(SB, "localPosition.x", 0.467f); L(SB, "localPosition.y", 0.632f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", WALK_SB_ROT_FINAL);
        L(FB, "localPosition.x", 0.141f); L(FB, "localPosition.y", -0.506f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", WALK_FB_ROT_FINAL);
        L(HB, "localPosition.x", 0.026f); L(HB, "localPosition.y", -0.687f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", WALK_HB_ROT_FINAL);
        L(AXE, "localPosition.x", 0.191f); L(AXE, "localPosition.y", -0.263f); L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", -90f);

        // Bottom
        L(BOTTOM, "localPosition.x", -0.141f); L(BOTTOM, "localPosition.y", 0f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", WALK_BOT_ROT);

        // 앞다리: Thigh rot 스윙, Calf 무릎 굽힘
        L(THIGH_F, "localPosition.x", -0.572f); L(THIGH_F, "localPosition.y", -0.472f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", WALK_THIGH_F_ROT);
        L(CALF_F, "localPosition.x", 0.008f); L(CALF_F, "localPosition.y", -0.718f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", WALK_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.265f); L(FOOT_F, "localPosition.y", -0.978f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", WALK_FOOT_F_ROT);

        // 뒷다리
        L(THIGH_B, "localPosition.x", 0.394f); C(THIGH_B, "localPosition.y", WALK_THIGH_B_Y); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", WALK_THIGH_B_ROT);
        L(CALF_B, "localPosition.x", 0.259f); L(CALF_B, "localPosition.y", -0.737f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", WALK_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.060f); L(FOOT_B, "localPosition.y", -0.875f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", WALK_FOOT_B_ROT);

        Save(clip, "MainBoss2_Walk");
        GenerateAC();
    }


    // ================================================================
    // Generate Walk2 (Body 회전 없는 버전 — 나머지는 Walk와 동일)
    // ================================================================
    [MenuItem("Tools/MainBoss2/Generate Walk2")]
    public static void GenerateWalk2()
    {
        const float DUR = 32 / 12f;
        var clip = MakeClip("MainBoss2_Walk2", 32, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 32);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", WALK_BODY_Y); L(BODY, "localPosition.x", 0f); L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);
        L(HEAD, "localPosition.x", -0.275f); L(HEAD, "localPosition.y", 1.025f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);

        // 앞팔: SF 살짝 스윙, FF/HF 고정
        L(SF, "localPosition.x", -0.856f); L(SF, "localPosition.y", 0.601f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", WALK_SF_ROT);
        L(FF, "localPosition.x", -0.245f); L(FF, "localPosition.y", -0.562f); L(FF, "localPosition.z", 0f);
        L(FF, "localEulerAngles.z", 5.682f);
        L(HF, "localPosition.x", -0.093f); L(HF, "localPosition.y", -0.678f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        // 뒷팔: SB/FB 스윙 동일, HB는 Body 회전 없는 버전에 맞게 재보정 (SB+FB 변화량만 상쇄)
        L(SB, "localPosition.x", 0.467f); L(SB, "localPosition.y", 0.632f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", WALK_SB_ROT_FINAL);
        L(FB, "localPosition.x", 0.141f); L(FB, "localPosition.y", -0.506f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", WALK_FB_ROT_FINAL);
        L(HB, "localPosition.x", 0.026f); L(HB, "localPosition.y", -0.687f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", WALK2_HB_ROT_FINAL);
        L(AXE, "localPosition.x", 0.191f); L(AXE, "localPosition.y", -0.263f); L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", -90f);

        // Bottom (회전 고정 — 대신 다리 쪽에서 보정해서 스윙은 그대로 보이게 함)
        L(BOTTOM, "localPosition.x", -0.141f); L(BOTTOM, "localPosition.y", 0f); L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);

        // 앞다리: Thigh rot 스윙, Calf 무릎 굽힘
        L(THIGH_F, "localPosition.x", -0.572f); L(THIGH_F, "localPosition.y", -0.472f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", WALK2_THIGH_F_ROT);
        L(CALF_F, "localPosition.x", 0.008f); L(CALF_F, "localPosition.y", -0.718f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", WALK_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.265f); L(FOOT_F, "localPosition.y", -0.978f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", WALK_FOOT_F_ROT);

        // 뒷다리
        L(THIGH_B, "localPosition.x", 0.394f); C(THIGH_B, "localPosition.y", WALK_THIGH_B_Y); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", WALK2_THIGH_B_ROT);
        L(CALF_B, "localPosition.x", 0.259f); L(CALF_B, "localPosition.y", -0.737f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", WALK_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.060f); L(FOOT_B, "localPosition.y", -0.875f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", WALK_FOOT_B_ROT);

        Save(clip, "MainBoss2_Walk2");
        GenerateAC();
    }


    // ================================================================
    // Generate Attack (일반공격 — 기모으기 → 크게 스윙업 → 든 자세로 마무리)
    // 콘티 기반 1차 추측 버전: 도끼를 낮게 감았다가 위로 크게 쓸어올려 맞은 대상을 띄워보냄.
    // 내려찍는 동작은 없음 — 스윙업 후 든 자세에서 클립이 끝남 (Idle 복귀는 Animator 크로스페이드로 처리).
    // 논루프.
    // ================================================================
    [MenuItem("Tools/MainBoss2/Generate Attack")]
    public static void GenerateAttack()
    {
        const float DUR = 19 / FRAME_RATE;
        var clip = MakeClip("MainBoss2_Attack", 19, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 19, FRAME_RATE, loop: false);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", ATK_BODY_Y); C(BODY, "localPosition.x", ATK_BODY_X); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", ATK_BODY_ROT);
        C(BODY, "localScale.x", ATK_BODY_SCALE_X); L(BODY, "localScale.y", 1f); L(BODY, "localScale.z", 1f);
        L(HEAD, "localPosition.x", -0.275f); L(HEAD, "localPosition.y", 1.025f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);

        // 앞팔: 무기 스윙 반대 방향으로 작게 카운터스윙 (균형 잡는 느낌)
        L(SF, "localPosition.x", -0.856f); L(SF, "localPosition.y", 0.601f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", ATK_SF_ROT);
        L(FF, "localPosition.x", -0.245f); L(FF, "localPosition.y", -0.562f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", ATK_FF_ROT);
        L(HF, "localPosition.x", -0.093f); L(HF, "localPosition.y", -0.678f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        // 뒷팔(무기팔): 낮게 감았다가(기모으기) 크게 위로 쓸어올려 스윙 (SB가 -50°→170° 큰 호를 그림)
        L(SB, "localPosition.x", 0.467f); L(SB, "localPosition.y", 0.632f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", ATK_SB_ROT);
        L(FB, "localPosition.x", 0.141f); L(FB, "localPosition.y", -0.506f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", ATK_FB_ROT);
        L(HB, "localPosition.x", 0.026f); L(HB, "localPosition.y", -0.687f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", ATK_HB_ROT);
        L(AXE, "localPosition.x", 0.191f); L(AXE, "localPosition.y", -0.263f); L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", -90f);

        // Bottom: 몸통 반대로 카운터 트위스트
        L(BOTTOM, "localPosition.x", -0.141f); L(BOTTOM, "localPosition.y", 0f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", ATK_BOT_ROT);

        // 앞다리: 스윙에 맞춰 앞으로 굳게 딛으며 체중 실음
        L(THIGH_F, "localPosition.x", -0.572f); L(THIGH_F, "localPosition.y", -0.472f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", ATK_THIGH_F_ROT);
        L(CALF_F, "localPosition.x", 0.008f); L(CALF_F, "localPosition.y", -0.718f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", ATK_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.265f); L(FOOT_F, "localPosition.y", -0.978f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", ATK_FOOT_F_ROT);

        // 뒷다리: 스윙 파워를 위해 뒤로 밀어내며 지지
        L(THIGH_B, "localPosition.x", 0.394f); L(THIGH_B, "localPosition.y", -0.556f); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", ATK_THIGH_B_ROT);
        L(CALF_B, "localPosition.x", 0.259f); L(CALF_B, "localPosition.y", -0.737f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", ATK_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.060f); L(FOOT_B, "localPosition.y", -0.875f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", ATK_FOOT_B_ROT);

        Save(clip, "MainBoss2_Attack");
        GenerateAC();
    }

    // ================================================================
    // Generate Skill1 (2연타 — 크게 뒤로 젖혀찍기 → 점프 내려찍기)
    // 콘티 기반 1차 추측: 도끼를 크게 뒤로 젖혔다가 아래로 내려찍고(HIT1),
    // 그 반동으로 점프해서 공중에서 한 번 더 내려찍음(HIT2). 논루프, 착지 자세로 끝.
    // ================================================================
    [MenuItem("Tools/MainBoss2/Generate Skill1")]
    public static void GenerateSkill1()
    {
        const float DUR = 34 / FRAME_RATE;
        var clip = MakeClip("MainBoss2_Skill1", 34, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 34, FRAME_RATE, loop: false);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", SK1_BODY_Y); C(BODY, "localPosition.x", SK1_BODY_X); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", SK1_BODY_ROT);
        L(HEAD, "localPosition.x", -0.275f); L(HEAD, "localPosition.y", 1.025f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);

        L(SF, "localPosition.x", -0.856f); L(SF, "localPosition.y", 0.601f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", SK1_SF_ROT);
        L(FF, "localPosition.x", -0.245f); L(FF, "localPosition.y", -0.562f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", SK1_FF_ROT);
        L(HF, "localPosition.x", -0.093f); L(HF, "localPosition.y", -0.678f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        L(SB, "localPosition.x", 0.467f); L(SB, "localPosition.y", 0.632f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", SK1_SB_ROT);
        L(FB, "localPosition.x", 0.141f); L(FB, "localPosition.y", -0.506f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", SK1_FB_ROT);
        L(HB, "localPosition.x", 0.026f); L(HB, "localPosition.y", -0.687f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", SK1_HB_ROT);
        L(AXE, "localPosition.x", 0.191f); L(AXE, "localPosition.y", -0.263f); L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", -90f);

        L(BOTTOM, "localPosition.x", -0.141f); L(BOTTOM, "localPosition.y", 0f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", SK1_BOT_ROT);

        L(THIGH_F, "localPosition.x", -0.572f); L(THIGH_F, "localPosition.y", -0.472f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", SK1_THIGH_F_ROT);
        L(CALF_F, "localPosition.x", 0.008f); L(CALF_F, "localPosition.y", -0.718f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", SK1_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.265f); L(FOOT_F, "localPosition.y", -0.978f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", SK1_FOOT_F_ROT);

        L(THIGH_B, "localPosition.x", 0.394f); L(THIGH_B, "localPosition.y", -0.556f); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", SK1_THIGH_B_ROT);
        L(CALF_B, "localPosition.x", 0.259f); L(CALF_B, "localPosition.y", -0.737f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", SK1_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.060f); L(FOOT_B, "localPosition.y", -0.875f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", SK1_FOOT_B_ROT);

        Save(clip, "MainBoss2_Skill1");
        GenerateAC();
    }


    // ================================================================
    // Generate Skill2 (3연타 — 돌진 몸빵 → 스윙 → 내려찍기)
    // 콘티 기반 1차 추측: 빠르게 돌진해 몸으로 들이받고(HIT1), 도끼를 들어 크게 휘두르고(HIT2),
    // 마지막으로 내려찍음(HIT3). 논루프, 마지막 내려찍은 자세로 끝.
    // ================================================================
    [MenuItem("Tools/MainBoss2/Generate Skill2")]
    public static void GenerateSkill2()
    {
        const float DUR = 36 / FRAME_RATE;
        var clip = MakeClip("MainBoss2_Skill2", 36, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 36, FRAME_RATE, loop: false);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", SK2_BODY_Y); C(BODY, "localPosition.x", SK2_BODY_X); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", SK2_BODY_ROT);
        L(HEAD, "localPosition.x", -0.275f); L(HEAD, "localPosition.y", 1.025f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);

        L(SF, "localPosition.x", -0.856f); L(SF, "localPosition.y", 0.601f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", SK2_SF_ROT);
        L(FF, "localPosition.x", -0.245f); L(FF, "localPosition.y", -0.562f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", SK2_FF_ROT);
        L(HF, "localPosition.x", -0.093f); L(HF, "localPosition.y", -0.678f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        L(SB, "localPosition.x", 0.467f); L(SB, "localPosition.y", 0.632f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", SK2_SB_ROT);
        L(FB, "localPosition.x", 0.141f); L(FB, "localPosition.y", -0.506f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", SK2_FB_ROT);
        L(HB, "localPosition.x", 0.026f); L(HB, "localPosition.y", -0.687f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", SK2_HB_ROT);
        L(AXE, "localPosition.x", 0.191f); L(AXE, "localPosition.y", -0.263f); L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", -90f);

        L(BOTTOM, "localPosition.x", -0.141f); L(BOTTOM, "localPosition.y", 0f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", SK2_BOT_ROT);

        L(THIGH_F, "localPosition.x", -0.572f); L(THIGH_F, "localPosition.y", -0.472f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", SK2_THIGH_F_ROT);
        L(CALF_F, "localPosition.x", 0.008f); L(CALF_F, "localPosition.y", -0.718f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", SK2_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.265f); L(FOOT_F, "localPosition.y", -0.978f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", SK2_FOOT_F_ROT);

        L(THIGH_B, "localPosition.x", 0.394f); L(THIGH_B, "localPosition.y", -0.556f); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", SK2_THIGH_B_ROT);
        L(CALF_B, "localPosition.x", 0.259f); L(CALF_B, "localPosition.y", -0.737f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", SK2_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.060f); L(FOOT_B, "localPosition.y", -0.875f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", SK2_FOOT_B_ROT);

        Save(clip, "MainBoss2_Skill2");
        GenerateAC();
    }


    // ================================================================
    // Generate Skill3 (돌기둥/충격파 — 캐릭터 파트: 내리찍기만)
    // 콘티 기반 1차 추측: 크고 느린 windup 후 바닥에 강하게 내리찍음.
    // ⚠ 바닥에서 순서대로 솟아오르는 돌기둥/충격파는 캐릭터 본 애니메이션이 아니라
    //   별도의 이펙트/프리팹 스폰 시스템(스크립트+파티클 또는 개별 오브젝트 애니메이션)이 필요함.
    //   여기서는 내리찍는 순간까지만 만들고, 그 프레임에 애니메이션 이벤트를 걸어서
    //   충격파 스폰 스크립트를 트리거하는 방식을 추천.
    // 논루프, 내리찍은 자세로 끝.
    // ================================================================
    [MenuItem("Tools/MainBoss2/Generate Skill3")]
    public static void GenerateSkill3()
    {
        const float DUR = 22 / FRAME_RATE;
        var clip = MakeClip("MainBoss2_Skill3", 22, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 22, FRAME_RATE, loop: false);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", SK3_BODY_Y); C(BODY, "localPosition.x", SK3_BODY_X); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", SK3_BODY_ROT);
        L(HEAD, "localPosition.x", -0.275f); L(HEAD, "localPosition.y", 1.025f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);

        L(SF, "localPosition.x", -0.856f); L(SF, "localPosition.y", 0.601f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", SK3_SF_ROT);
        L(FF, "localPosition.x", -0.245f); L(FF, "localPosition.y", -0.562f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", SK3_FF_ROT);
        L(HF, "localPosition.x", -0.093f); L(HF, "localPosition.y", -0.678f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        L(SB, "localPosition.x", 0.467f); L(SB, "localPosition.y", 0.632f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", SK3_SB_ROT);
        L(FB, "localPosition.x", 0.141f); L(FB, "localPosition.y", -0.506f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", SK3_FB_ROT);
        L(HB, "localPosition.x", 0.026f); L(HB, "localPosition.y", -0.687f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", SK3_HB_ROT);
        L(AXE, "localPosition.x", 0.191f); L(AXE, "localPosition.y", -0.263f); L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", -90f);

        L(BOTTOM, "localPosition.x", -0.141f); L(BOTTOM, "localPosition.y", 0f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", SK3_BOT_ROT);

        L(THIGH_F, "localPosition.x", -0.572f); L(THIGH_F, "localPosition.y", -0.472f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", SK3_THIGH_F_ROT);
        L(CALF_F, "localPosition.x", 0.008f); L(CALF_F, "localPosition.y", -0.718f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", SK3_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.265f); L(FOOT_F, "localPosition.y", -0.978f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", SK3_FOOT_F_ROT);

        L(THIGH_B, "localPosition.x", 0.394f); L(THIGH_B, "localPosition.y", -0.556f); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", SK3_THIGH_B_ROT);
        L(CALF_B, "localPosition.x", 0.259f); L(CALF_B, "localPosition.y", -0.737f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", SK3_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.060f); L(FOOT_B, "localPosition.y", -0.875f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", SK3_FOOT_B_ROT);

        Save(clip, "MainBoss2_Skill3");
        GenerateAC();
    }


    // ================================================================
    // Generate Buff (웅크렸다 가슴 펴며 일어남)
    // 콘티 기반 1차 추측: 깊게 웅크려 힘을 모았다가, 가슴을 펴며 힘차게 일어남.
    // 논루프, 일어난 파워 자세로 끝 (Idle로는 Animator 크로스페이드로 복귀).
    // ================================================================
    [MenuItem("Tools/MainBoss2/Generate Buff")]
    public static void GenerateBuff()
    {
        const float DUR = 24 / FRAME_RATE;
        var clip = MakeClip("MainBoss2_Buff", 24, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 24, FRAME_RATE, loop: false);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", BUFF_BODY_Y); C(BODY, "localPosition.x", BUFF_BODY_X); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", BUFF_BODY_ROT);
        L(HEAD, "localPosition.x", -0.275f); L(HEAD, "localPosition.y", 1.025f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);

        L(SF, "localPosition.x", -0.856f); L(SF, "localPosition.y", 0.601f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", BUFF_SF_ROT);
        L(FF, "localPosition.x", -0.245f); L(FF, "localPosition.y", -0.562f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", BUFF_FF_ROT);
        L(HF, "localPosition.x", -0.093f); L(HF, "localPosition.y", -0.678f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        L(SB, "localPosition.x", 0.467f); L(SB, "localPosition.y", 0.632f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", BUFF_SB_ROT);
        L(FB, "localPosition.x", 0.141f); L(FB, "localPosition.y", -0.506f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", BUFF_FB_ROT);
        L(HB, "localPosition.x", 0.026f); L(HB, "localPosition.y", -0.687f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", BUFF_HB_ROT);
        L(AXE, "localPosition.x", 0.191f); L(AXE, "localPosition.y", -0.263f); L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", -90f);

        L(BOTTOM, "localPosition.x", -0.141f); L(BOTTOM, "localPosition.y", 0f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", BUFF_BOT_ROT);

        L(THIGH_F, "localPosition.x", -0.572f); L(THIGH_F, "localPosition.y", -0.472f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", BUFF_THIGH_F_ROT);
        L(CALF_F, "localPosition.x", 0.008f); L(CALF_F, "localPosition.y", -0.718f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", BUFF_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.265f); L(FOOT_F, "localPosition.y", -0.978f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", BUFF_FOOT_F_ROT);

        L(THIGH_B, "localPosition.x", 0.394f); L(THIGH_B, "localPosition.y", -0.556f); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", BUFF_THIGH_B_ROT);
        L(CALF_B, "localPosition.x", 0.259f); L(CALF_B, "localPosition.y", -0.737f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", BUFF_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.060f); L(FOOT_B, "localPosition.y", -0.875f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", BUFF_FOOT_B_ROT);

        Save(clip, "MainBoss2_Buff");
        GenerateAC();
    }



    // ================================================================
    // Generate Run (추측 기반 1차 버전 — Walk보다 보폭/무릎 굴곡/팔 스윙 확대, 20프레임 사이클)
    // ================================================================
    [MenuItem("Tools/MainBoss2/Generate Run")]
    public static void GenerateRun()
    {
        const float SPEED_MULT = 3f;                 // 모션 속도 3배
        const float RATE = 12f * SPEED_MULT;          // 키프레임 타이밍 계산용 실효 프레임레이트
        const float DUR = 20 / RATE;
        var clip = MakeClip("MainBoss2_Run", 20, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 20, RATE);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", RUN_BODY_Y); L(BODY, "localPosition.x", 0f); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", RUN_BODY_ROT);
        L(HEAD, "localPosition.x", -0.275f); L(HEAD, "localPosition.y", 1.025f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);

        // 앞팔: 걷기보다 크고 빠른 스윙, FF/HF 고정
        L(SF, "localPosition.x", -0.856f); L(SF, "localPosition.y", 0.601f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", RUN_SF_ROT);
        L(FF, "localPosition.x", -0.245f); L(FF, "localPosition.y", -0.562f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", RUN_FF_ROT);
        L(HF, "localPosition.x", -0.093f); L(HF, "localPosition.y", -0.678f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        // 뒷팔: 스윙 확대 (도끼 안정화 보정은 1차 버전에서 생략 — 필요하면 Walk처럼 HB 보정 추가 가능)
        L(SB, "localPosition.x", 0.467f); L(SB, "localPosition.y", 0.632f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", RUN_SB_ROT);
        L(FB, "localPosition.x", 0.141f); L(FB, "localPosition.y", -0.506f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", RUN_FB_ROT_CANCEL_SB);
        L(HB, "localPosition.x", 0.026f); L(HB, "localPosition.y", -0.687f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(AXE, "localPosition.x", 0.191f); L(AXE, "localPosition.y", -0.263f); L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", -90f);

        // Bottom: 걷기보다 큰 반대 회전 (골반 트위스트 확대)
        L(BOTTOM, "localPosition.x", -0.141f); L(BOTTOM, "localPosition.y", 0f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", RUN_BOT_ROT_CANCEL);

        // 앞다리: 큰 보폭, 깊은 무릎 굴곡 (스윙 중 뒤꿈치가 높이 올라감)
        L(THIGH_F, "localPosition.x", -0.572f); L(THIGH_F, "localPosition.y", -0.472f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", RUN_THIGH_F_ROT_COMP);
        L(CALF_F, "localPosition.x", 0.008f); L(CALF_F, "localPosition.y", -0.718f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", RUN_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.265f); L(FOOT_F, "localPosition.y", -0.978f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", RUN_FOOT_F_ROT);

        // 뒷다리
        L(THIGH_B, "localPosition.x", 0.394f); C(THIGH_B, "localPosition.y", RUN_THIGH_B_Y); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", RUN_THIGH_B_ROT_COMP);
        L(CALF_B, "localPosition.x", 0.259f); L(CALF_B, "localPosition.y", -0.737f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", RUN_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.060f); L(FOOT_B, "localPosition.y", -0.875f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", RUN_FOOT_B_ROT);

        Save(clip, "MainBoss2_Run");
        GenerateAC();
    }


    // ================================================================
    // Generate Run2 (Run과 동일한 하체/몸통 리듬 + 뒷팔은 화면 기준 오른쪽 위로
    // 무기를 치켜든 채 고정한 상태로 질주)
    // ================================================================
    [MenuItem("Tools/MainBoss2/Generate Run2")]
    public static void GenerateRun2()
    {
        const float SPEED_MULT = 3f;                 // 모션 속도 3배
        const float RATE = 12f * SPEED_MULT;          // 키프레임 타이밍 계산용 실효 프레임레이트
        const float DUR = 20 / RATE;
        var clip = MakeClip("MainBoss2_Run2", 20, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 20, RATE);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", RUN_BODY_Y); L(BODY, "localPosition.x", 0f); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", RUN_BODY_ROT);
        L(HEAD, "localPosition.x", -0.275f); L(HEAD, "localPosition.y", 1.025f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);

        // 앞팔: Run과 동일 (반대쪽 팔은 그대로 펌핑, SF 변화량은 절반으로 축소)
        L(SF, "localPosition.x", -0.856f); L(SF, "localPosition.y", 0.601f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", RUN_SF_ROT_HALF);
        L(FF, "localPosition.x", -0.245f); L(FF, "localPosition.y", -0.562f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", RUN_FF_ROT);
        L(HF, "localPosition.x", -0.093f); L(HF, "localPosition.y", -0.678f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        // 뒷팔: 화면 오른쪽 위로 무기 치켜든 자세 고정 (SB≈150°로 팔을 위-오른쪽 대각선으로 들어올림,
        // FB는 살짝만 굽혀서 팔이 말리지 않고 쭉 뻗어 보이게 함). 완전 고정이면 죽어 보이니
        // 러닝 박자에 맞춰 아주 작은 폭으로만 흔들어줌.
        L(SB, "localPosition.x", 0.467f); L(SB, "localPosition.y", 0.632f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", RUN2_SB_ROT);
        L(FB, "localPosition.x", 0.141f); L(FB, "localPosition.y", -0.506f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", RUN2_FB_ROT);
        L(HB, "localPosition.x", 0.026f); L(HB, "localPosition.y", -0.687f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", RUN2_HB_ROT);
        L(AXE, "localPosition.x", 0.191f); L(AXE, "localPosition.y", -0.263f); L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", -90f);

        // Bottom: Run과 동일
        L(BOTTOM, "localPosition.x", -0.141f); L(BOTTOM, "localPosition.y", 0f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", RUN_BOT_ROT_CANCEL);

        // 다리: Run과 동일
        L(THIGH_F, "localPosition.x", -0.572f); L(THIGH_F, "localPosition.y", -0.472f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", RUN_THIGH_F_ROT_COMP);
        L(CALF_F, "localPosition.x", 0.008f); L(CALF_F, "localPosition.y", -0.718f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", RUN_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.265f); L(FOOT_F, "localPosition.y", -0.978f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", RUN_FOOT_F_ROT);

        L(THIGH_B, "localPosition.x", 0.394f); C(THIGH_B, "localPosition.y", RUN_THIGH_B_Y); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", RUN_THIGH_B_ROT_COMP);
        L(CALF_B, "localPosition.x", 0.259f); L(CALF_B, "localPosition.y", -0.737f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", RUN_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.060f); L(FOOT_B, "localPosition.y", -0.875f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", RUN_FOOT_B_ROT);

        Save(clip, "MainBoss2_Run2");
        GenerateAC();
    }


    // ================================================================
    // Generate Run3 (Run2와 동일한 느낌, 뒷팔 SB만 120° 기준으로 낮춤)
    // ================================================================
    [MenuItem("Tools/MainBoss2/Generate Run3")]
    public static void GenerateRun3()
    {
        const float SPEED_MULT = 3f;                 // 모션 속도 3배
        const float RATE = 12f * SPEED_MULT;          // 키프레임 타이밍 계산용 실효 프레임레이트
        const float DUR = 20 / RATE;
        var clip = MakeClip("MainBoss2_Run3", 20, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 20, RATE);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", RUN_BODY_Y); L(BODY, "localPosition.x", 0f); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", RUN_BODY_ROT);
        L(HEAD, "localPosition.x", -0.275f); L(HEAD, "localPosition.y", 1.025f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);

        // 앞팔: Run2와 동일 (SF 변화량 절반)
        L(SF, "localPosition.x", -0.856f); L(SF, "localPosition.y", 0.601f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", RUN_SF_ROT_HALF);
        L(FF, "localPosition.x", -0.245f); L(FF, "localPosition.y", -0.562f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", RUN_FF_ROT);
        L(HF, "localPosition.x", -0.093f); L(HF, "localPosition.y", -0.678f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        // 뒷팔: Run2와 같은 자세/느낌, SB 기준각만 150°→120°로 낮춤 (FB/HB는 그대로)
        L(SB, "localPosition.x", 0.467f); L(SB, "localPosition.y", 0.632f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", RUN3_SB_ROT);
        L(FB, "localPosition.x", 0.141f); L(FB, "localPosition.y", -0.506f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", RUN2_FB_ROT);
        L(HB, "localPosition.x", 0.026f); L(HB, "localPosition.y", -0.687f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", RUN2_HB_ROT);
        L(AXE, "localPosition.x", 0.191f); L(AXE, "localPosition.y", -0.263f); L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", -90f);

        // Bottom: Run과 동일
        L(BOTTOM, "localPosition.x", -0.141f); L(BOTTOM, "localPosition.y", 0f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", RUN_BOT_ROT_CANCEL);

        // 다리: Run과 동일
        L(THIGH_F, "localPosition.x", -0.572f); L(THIGH_F, "localPosition.y", -0.472f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", RUN_THIGH_F_ROT_COMP);
        L(CALF_F, "localPosition.x", 0.008f); L(CALF_F, "localPosition.y", -0.718f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", RUN_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.265f); L(FOOT_F, "localPosition.y", -0.978f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", RUN_FOOT_F_ROT);

        L(THIGH_B, "localPosition.x", 0.394f); C(THIGH_B, "localPosition.y", RUN_THIGH_B_Y); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", RUN_THIGH_B_ROT_COMP);
        L(CALF_B, "localPosition.x", 0.259f); L(CALF_B, "localPosition.y", -0.737f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", RUN_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.060f); L(FOOT_B, "localPosition.y", -0.875f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", RUN_FOOT_B_ROT);

        Save(clip, "MainBoss2_Run3");
        GenerateAC();
    }

    // ================================================================
    // Generate AC
    // ================================================================
    [MenuItem("Tools/MainBoss2/Generate AC")]
    public static void GenerateAC()
    {
        string acPath = $"{SAVE_PATH}/MainBoss2_AC.controller";
        var ac = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(acPath);
        if (ac == null)
            ac = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(acPath);

        var rootSM = ac.layers[0].stateMachine;
        var existingNames = new System.Collections.Generic.HashSet<string>();
        foreach (var s in rootSM.states) existingNames.Add(s.state.name);

        // SAVE_PATH 폴더에 있는 모든 .anim 클립을 자동으로 스캔 → 새 클립 만들 때마다
        // 여기 리스트를 손으로 안 고쳐도 AC에 자동 반영됨
        var guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { SAVE_PATH });
        var clipNames = new System.Collections.Generic.List<string>();
        foreach (var guid in guids)
            clipNames.Add(System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)));
        clipNames.Sort(); // 순서 고정 (알파벳순)

        foreach (string clipName in clipNames)
        {
            string clipPath = $"{SAVE_PATH}/{clipName}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null) { Debug.LogWarning($"[MainBoss2] 클립 없음: {clipPath}"); continue; }
            if (!existingNames.Contains(clipName))
            { var state = rootSM.AddState(clipName); state.motion = clip; }
            else
            { foreach (var cs in rootSM.states) if (cs.state.name == clipName) { cs.state.motion = clip; break; } }
        }
        if (rootSM.states.Length > 0) rootSM.defaultState = rootSM.states[0].state;
        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[MainBoss2] AC 저장 완료: {acPath}");
    }

    // ── 공통 헬퍼 ────────────────────────────────────────────────────
    static AnimationClip MakeClip(string name, int totalFrames, bool loop)
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            if (!AssetDatabase.IsValidFolder("Assets/Animations/2"))
                AssetDatabase.CreateFolder("Assets/Animations", "2");
            AssetDatabase.CreateFolder("Assets/Animations/2", "MainBoss 2");
        }
        var clip = new AnimationClip { frameRate = FRAME_RATE };
        clip.name = name;
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, s);
        return clip;
    }

    static void SetCurve(AnimationClip clip, string path, string prop, float[] vals, int total, float rate = FRAME_RATE, bool loop = true)
    {
        var keys = new Keyframe[total];
        for (int i = 0; i < total; i++) keys[i] = new Keyframe(i / rate, vals[i]);
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < curve.keys.Length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
        }

        // 루프 이음매 보정: Unity의 Auto 탄젠트는 첫/끝 키를 "끊어진 끝점"으로 보고
        // 한쪽 이웃만으로 기울기를 계산해서, loopTime이 켜져 있어도 마지막→첫 프레임
        // 전환에서 속도가 어긋나 멈칫하는 느낌이 생김. 이음매를 가로지르는 기울기를
        // 직접 계산해서 첫 키의 outTangent와 마지막 키의 inTangent에 동일하게 넣어줌.
        // 논루프 클립(공격 모션 등)은 시작/끝이 정지 상태여야 하므로 이 보정을 건너뜀.
        if (loop && total >= 3)
        {
            float dt = 2f / rate;
            float seamTangent = (vals[1] - vals[total - 2]) / dt;

            var k0 = curve.keys[0];
            k0.inTangent = seamTangent;
            k0.outTangent = seamTangent;
            curve.MoveKey(0, k0);
            AnimationUtility.SetKeyLeftTangentMode(curve, 0, AnimationUtility.TangentMode.Free);
            AnimationUtility.SetKeyRightTangentMode(curve, 0, AnimationUtility.TangentMode.Free);

            int last = total - 1;
            var kLast = curve.keys[last];
            kLast.inTangent = seamTangent;
            kLast.outTangent = seamTangent;
            curve.MoveKey(last, kLast);
            AnimationUtility.SetKeyLeftTangentMode(curve, last, AnimationUtility.TangentMode.Free);
            AnimationUtility.SetKeyRightTangentMode(curve, last, AnimationUtility.TangentMode.Free);
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
        Debug.Log($"[MainBoss2] {name} → {assetPath}");
    }
}