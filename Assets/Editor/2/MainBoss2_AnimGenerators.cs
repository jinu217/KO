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
    [MenuItem("Tools/2/MainBoss2/Generate All")]
    public static void GenerateAll()
    {
        GenerateIdle();
        GenerateIdle2();
        GenerateWalk();
        GenerateWalk2();
        GenerateAttack();
        GenerateAttack2();
        GenerateSkill1();
        GenerateSkill1_1();
        GenerateSkill1AG();
        GenerateSkill2();
        GenerateSkill2_1();
        GenerateSkill3();
        GenerateSkill3_1();
        GenerateBuff();
        GenerateBuff1();
        GenerateRun();
        GenerateRun2();
        GenerateRun3();
        GenerateAC();
        Debug.Log("[MainBoss2] 전체 생성 완료");
    }

    // ================================================================
    // Generate Idle
    // ================================================================
    [MenuItem("Tools/2/MainBoss2/Generate Idle")]
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
        L(SF, "localEulerAngles.z", 0f);
        L(FF, "localPosition.x", -0.245f); L(FF, "localPosition.y", -0.562f); L(FF, "localPosition.z", 0f);
        L(FF, "localEulerAngles.z", 5.682f);
        L(HF, "localPosition.x", -0.093f); L(HF, "localPosition.y", -0.678f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        // Shoulder_B / Forearm_B / Hand_B / Axe
        L(SB, "localPosition.x", 0.467f); C(SB, "localPosition.y", IDLE_SB_Y); L(SB, "localPosition.z", 0f);
        L(SB, "localEulerAngles.z", 10f);
        L(FB, "localPosition.x", 0.141f); L(FB, "localPosition.y", -0.506f); L(FB, "localPosition.z", 0f);
        L(FB, "localEulerAngles.z", 75f);
        L(HB, "localPosition.x", 0.026f); L(HB, "localPosition.y", -0.687f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 5f);
        L(AXE, "localPosition.x", 0.190f); L(AXE, "localPosition.y", -0.260f); L(AXE, "localPosition.z", 0f);
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
    [MenuItem("Tools/2/MainBoss2/Generate Idle2")]
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
    // Attack 커브 (일반공격: 기모으기 -> 크게 스윙업 -> 플레이어 발사 -> 복귀, 29프레임 논루프, 추측 기반 1차 버전)
    // ================================================================
    static readonly float[] ATK_SB_ROT = { 10.0f, -0.831f, -2.974f, -5.904f, -9.096f, -12.026f, -14.169f, -15.0f, -13.0f, -13.0f, -13.0f, -13.0f, -13.0f, -13.0f, -13.0f, -13.0f, -13.0f, -13.0f, -13.0f, -17.0f, 60.697f, 87.019f, 110.697f, 130.0f, 144.225f, 154.228f, 160.618f, 164.006f, 165.0f };
    static readonly float[] ATK_FB_ROT = { 75.0f, 71.676f, 63.105f, 51.385f, 38.615f, 26.895f, 18.324f, 15.0f, 16.5f, 16.5f, 16.5f, 16.5f, 16.5f, 16.5f, 16.5f, 16.5f, 16.5f, 16.5f, 16.5f, 13.5f, 45.664f, 35.582f, 26.785f, 20.0f, 16.247f, 15.308f, 16.046f, 17.322f, 18.0f };
    static readonly float[] ATK_HB_ROT = { 5.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 6.279f, 9.969f, 12.992f, 15.0f, 15.158f, 13.533f, 11.129f, 8.95f, 8.0f };
    static readonly float[] ATK_AXE_POS_X = { 0.19f, 0.191f, 0.191f, 0.191f, 0.191f, 0.191f, 0.191f, 0.191f, 0.191f, 0.191f, 0.191f, 0.191f, 0.191f, 0.191f, 0.191f, 0.191f, 0.191f, 0.191f, 0.191f, 0.191f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f };
    static readonly float[] ATK_SF_ROT = { 0.0f, -0.352f, -1.26f, -2.501f, -3.853f, -5.094f, -6.002f, -6.354f, -6.354f, -6.354f, -6.354f, -6.354f, -6.354f, -6.354f, -6.354f, -6.354f, -6.354f, -6.354f, -6.354f, -6.354f, 105.802f, 117.047f, 127.238f, 135.614f, 142.02f, 146.85f, 150.18f, 152.097f, 152.71f };
    static readonly float[] ATK_FF_ROT = { 5.682f, 23.279f, 18.842f, 12.775f, 6.164f, 0.097f, -4.34f, -6.061f, -6.061f, -6.061f, -6.061f, -6.061f, -6.061f, -6.061f, -6.061f, -6.061f, -6.061f, -6.061f, -6.061f, -6.061f, -11.238f, -11.238f, -11.238f, -11.238f, -11.238f, -11.238f, -11.238f, -11.238f, -11.238f };
    static readonly float[] ATK_BODY_Y = { 0.0f, -0.037f, -0.132f, -0.262f, -0.404f, -0.534f, -0.629f, -0.666f, -0.666f, -0.666f, -0.666f, -0.666f, -0.666f, -0.666f, -0.666f, -0.666f, -0.666f, -0.666f, -0.666f, -0.666f, -0.006f, 0.029f, 0.059f, 0.08f, 0.087f, 0.081f, 0.068f, 0.056f, 0.05f };
    static readonly float[] ATK_BODY_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 1.2f, 1.2f, 1.2f, 1.2f, 1.2f, 1.2f, 1.2f, 1.2f, 1.2f, 1.2f, 1.2f, -1.2f, 7.213f, 12.425f, 16.839f, 20.0f, 21.11f, 20.309f, 18.553f, 16.798f, 16.0f };
    static readonly float[] ATK_BODY_X = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.079f, 0.107f, 0.132f, 0.15f, 0.157f, 0.153f, 0.144f, 0.134f, 0.13f };
    static readonly float[] ATK_BOT_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -4.382f, -7.507f, -10.139f, -12.0f, -12.574f, -11.941f, -10.721f, -9.534f, -9.0f };
    static readonly float[] ATK_THIGH_F_ROT = { 0.0f, 10.434f, 11.554f, 13.085f, 14.754f, 16.285f, 17.405f, 17.839f, 17.839f, 17.839f, 17.839f, 17.839f, 17.839f, 17.839f, 17.839f, 17.839f, 17.839f, 17.839f, 17.839f, 17.839f, 15.602f, 18.236f, 20.444f, 22.0f, 22.426f, 21.775f, 20.61f, 19.497f, 19.0f };
    static readonly float[] ATK_CALF_F_ROT = { 0.0f, -4.282f, -15.324f, -30.423f, -46.873f, -61.972f, -73.014f, -77.296f, -77.296f, -77.296f, -77.296f, -77.296f, -77.296f, -77.296f, -77.296f, -77.296f, -77.296f, -77.296f, -77.296f, -77.296f, -5.105f, -4.214f, -3.476f, -3.0f, -2.907f, -3.13f, -3.5f, -3.847f, -4.0f };
    static readonly float[] ATK_FOOT_F_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 3.418f, 5.334f, 6.92f, 8.0f, 8.18f, 7.498f, 6.425f, 5.435f, 5.0f };
    static readonly float[] ATK_THIGH_B_ROT = { 0.0f, -2.811f, 5.413f, 16.657f, 28.909f, 40.153f, 48.377f, 51.566f, 51.566f, 51.566f, 51.566f, 51.566f, 51.566f, 51.566f, 51.566f, 51.566f, 51.566f, 51.566f, 51.566f, 51.566f, -24.852f, -26.24f, -27.316f, -28.0f, -27.671f, -26.22f, -24.334f, -22.698f, -22.0f };
    static readonly float[] ATK_CALF_B_ROT = { 0.0f, -2.752f, -9.848f, -19.551f, -30.123f, -39.826f, -46.922f, -49.674f, -49.674f, -49.674f, -49.674f, -49.674f, -49.674f, -49.674f, -49.674f, -49.674f, -49.674f, -49.674f, -49.674f, -49.674f, -20.795f, -22.599f, -24.036f, -25.0f, -24.819f, -23.386f, -21.444f, -19.735f, -19.0f };
    static readonly float[] ATK_FOOT_B_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 7.408f, 11.841f, 15.508f, 18.0f, 18.404f, 16.81f, 14.313f, 12.011f, 11.0f };
    static readonly float[] ATK_HEAD_ROT = { 0.0f, 0.111f, 0.397f, 0.787f, 1.213f, 1.603f, 1.889f, 2.0f, 3.0f, 3.0f, 3.0f, 3.0f, 3.0f, 3.0f, 3.0f, 3.0f, 3.0f, 3.0f, 3.0f, 1.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] ATK_BODY_SCALE_X = { 1.0f, 1.0f, 0.2f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -0.2f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };  // Body 루트 X스케일 반전 (기본 스케일 1 가정) — 3~18 홀드 구간 -1 유지, 19에서 복귀

    // ================================================================
    // Skill3 커브 (rest -> 즉시 점프 -> 임팩트, 21프레임 논루프)
    // ================================================================
    static readonly float[] SK3N_BODY_Y = { 0.0f, 0.028f, 0.096f, 0.184f, 0.27f, 0.332f, 0.35f, 0.35f, 0.35f, 0.16f, 0.063f, -0.03f, -0.105f, -0.15f, -0.15f, -0.15f, -0.15f, -0.077f, -0.04f, -0.011f, 0.0f };
    static readonly float[] SK3N_BODY_ROT = { 0.0f, 1.325f, 4.573f, 8.654f, 12.479f, 14.957f, 15.0f, 13.338f, 9.052f, 3.192f, -3.192f, -9.052f, -13.338f, -15.0f, -15.0f, -15.0f, -15.0f, -12.656f, -7.5f, -2.344f, 0.0f };
    static readonly float[] SK3N_BODY_X = { 0.0f, 0.002f, 0.008f, 0.016f, 0.027f, 0.038f, 0.05f, 0.05f, 0.05f, 0.101f, 0.119f, 0.135f, 0.146f, 0.15f, 0.15f, 0.15f, 0.15f, 0.089f, 0.07f, 0.056f, 0.05f };
    static readonly float[] SK3N_BOT_ROT = { 0.0f, 0.721f, 2.484f, 4.692f, 6.746f, 8.048f, 8.0f, 8.388f, 9.388f, 10.755f, 12.245f, 13.612f, 14.612f, 15.0f, 15.0f, 15.0f, 15.0f, 12.656f, 7.5f, 2.344f, 0.0f };
    static readonly float[] SK3N_HEAD_ROT = { 0.0f, 0.265f, 0.915f, 1.731f, 2.496f, 2.991f, 3.0f, 3.0f, 3.0f, 0.101f, -1.261f, -2.519f, -3.492f, -4.0f, -4.0f, -4.0f, -4.0f, -1.732f, -0.88f, -0.248f, 0.0f };
    static readonly float[] SK3N_SB_ROT = { 10.0f, 23.967f, 58.917f, 104.423f, 150.057f, 185.392f, 200.0f, 188.921f, 160.35f, 121.283f, 78.717f, 39.65f, 11.079f, 0.0f, 0.0f, 0.0f, 0.0f, 1.562f, 5.0f, 8.438f, 10.0f };
    static readonly float[] SK3N_FB_ROT = { 75.0f, 70.979f, 60.912f, 47.788f, 34.601f, 24.341f, 20.0f, 18.892f, 16.035f, 12.128f, 7.872f, 3.965f, 1.108f, 0.0f, 0.0f, 0.0f, 0.0f, 11.719f, 37.5f, 63.281f, 75.0f };
    static readonly float[] SK3N_HB_ROT = { 5.0f, 3.465f, -0.356f, -5.288f, -10.157f, -13.786f, -15.0f, -14.169f, -12.026f, -9.096f, -5.904f, -2.974f, -0.831f, 0.0f, 0.0f, 0.0f, 0.0f, 0.781f, 2.5f, 4.219f, 5.0f };
    static readonly float[] SK3N_SF_ROT = { 0.0f, 6.151f, 34.278f, 71.02f, 108.083f, 137.174f, 150.0f, 142.522f, 123.236f, 96.866f, 68.134f, 41.764f, 22.478f, 15.0f, 15.0f, 15.0f, 15.0f, 11.864f, 4.966f, -1.932f, -5.068f };
    static readonly float[] SK3N_FF_ROT = { 5.682f, 6.59f, 8.905f, 12.015f, 15.309f, 18.175f, 20.0f, 19.723f, 19.009f, 18.032f, 16.968f, 15.991f, 15.277f, 15.0f, 15.0f, 15.0f, 15.0f, 13.544f, 10.341f, 7.138f, 5.682f };
    static readonly float[] SK3N_HF_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.385f, -4.956f, -9.84f, -15.16f, -20.044f, -23.615f, -25.0f, -25.0f, -25.0f, -25.0f, -21.094f, -12.5f, -3.906f, 0.0f };
    static readonly float[] SK3N_THIGH_F_ROT = { 0.0f, 1.902f, 6.752f, 13.269f, 20.171f, 26.175f, 30.0f, 26.676f, 18.105f, 6.385f, -6.385f, -18.105f, -26.676f, -30.0f, -30.0f, -30.0f, -30.0f, -25.312f, -15.0f, -4.688f, 0.0f };
    static readonly float[] SK3N_CALF_F_ROT = { 0.0f, -4.124f, -14.53f, -28.269f, -42.393f, -53.953f, -60.0f, -58.338f, -54.052f, -48.192f, -41.808f, -35.948f, -31.662f, -30.0f, -30.0f, -30.0f, -30.0f, -25.312f, -15.0f, -4.688f, 0.0f };
    static readonly float[] SK3N_FOOT_F_ROT = { 0.0f, 0.655f, 2.319f, 4.538f, 6.86f, 8.832f, 10.0f, 9.446f, 8.017f, 6.064f, 3.936f, 1.983f, 0.554f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] SK3N_THIGH_B_ROT = { 0.0f, -1.638f, -5.798f, -11.346f, -17.151f, -22.08f, -25.0f, -21.122f, -11.122f, 2.551f, 17.449f, 31.122f, 41.122f, 45.0f, 45.0f, 45.0f, 45.0f, 37.969f, 22.5f, 7.031f, 0.0f };
    static readonly float[] SK3N_CALF_B_ROT = { 0.0f, -4.284f, -15.043f, -29.135f, -43.419f, -54.754f, -60.0f, -58.338f, -54.052f, -48.192f, -41.808f, -35.948f, -31.662f, -30.0f, -30.0f, -30.0f, -30.0f, -25.312f, -15.0f, -4.688f, 0.0f };
    static readonly float[] SK3N_FOOT_B_ROT = { 0.0f, 0.741f, 2.593f, 5.0f, 7.407f, 9.259f, 10.0f, 8.615f, 5.044f, 0.16f, -5.16f, -10.044f, -13.615f, -15.0f, -15.0f, -15.0f, -15.0f, -12.656f, -7.5f, -2.344f, 0.0f };
    static readonly float[] SK3N_AXE_POS_X = { 0.19f, 0.177f, 0.15f, 0.122f, 0.11f, 0.128f, 0.19f, 0.379f, 0.688f, 1.0f, 1.08f, 1.255f, 1.43f, 1.51f, 1.51f, 1.51f, 1.51f, 1.0f, 0.668f, 0.339f, 0.19f };

    // ================================================================
    // Skill3-1 커브 (rest -> 무릎 굽히는 예비동작 -> 점프 -> 임팩트, 27프레임 논루프)
    // ================================================================
    static readonly float[] SK31N_BODY_Y = { 0.0f, -0.024f, -0.075f, -0.118f, -0.12f, -0.12f, -0.12f, 0.064f, 0.146f, 0.224f, 0.29f, 0.335f, 0.35f, 0.35f, 0.35f, 0.17f, 0.07f, -0.026f, -0.104f, -0.15f, -0.15f, -0.15f, -0.15f, -0.077f, -0.04f, -0.011f, 0.0f };
    static readonly float[] SK31N_BODY_ROT = { 0.0f, -1.484f, -4.625f, -7.453f, -8.0f, -8.0f, -8.0f, 1.305f, 5.55f, 9.539f, 12.775f, 14.761f, 15.0f, 13.338f, 9.052f, 3.192f, -3.192f, -9.052f, -13.338f, -15.0f, -15.0f, -15.0f, -15.0f, -12.656f, -7.5f, -2.344f, 0.0f };
    static readonly float[] SK31N_BODY_X = { 0.0f, -0.005f, -0.017f, -0.028f, -0.03f, -0.03f, -0.03f, -0.008f, 0.002f, 0.014f, 0.026f, 0.038f, 0.05f, 0.05f, 0.05f, 0.101f, 0.119f, 0.135f, 0.146f, 0.15f, 0.15f, 0.15f, 0.15f, 0.089f, 0.07f, 0.056f, 0.05f };
    static readonly float[] SK31N_BOT_ROT = { 0.0f, 0.656f, 2.167f, 3.844f, 5.0f, 5.0f, 5.0f, 7.527f, 8.3f, 8.848f, 9.056f, 8.812f, 8.0f, 8.388f, 9.388f, 10.755f, 12.245f, 13.612f, 14.612f, 15.0f, 15.0f, 15.0f, 15.0f, 12.656f, 7.5f, 2.344f, 0.0f };
    static readonly float[] SK31N_HEAD_ROT = { 0.0f, 0.266f, 0.875f, 1.547f, 2.0f, 2.0f, 2.0f, 2.891f, 3.15f, 3.328f, 3.388f, 3.291f, 3.0f, 3.0f, 3.0f, 0.01f, -1.329f, -2.557f, -3.504f, -4.0f, -4.0f, -4.0f, -4.0f, -1.732f, -0.88f, -0.248f, 0.0f };
    static readonly float[] SK31N_SB_ROT = { 10.0f, 5.469f, -2.917f, -7.344f, 0.0f, 0.0f, 0.0f, 80.898f, 114.5f, 146.289f, 173.188f, 192.117f, 200.0f, 188.921f, 160.35f, 121.283f, 78.717f, 39.65f, 11.079f, 0.0f, 0.0f, 0.0f, 0.0f, 1.562f, 5.0f, 8.438f, 10.0f };
    static readonly float[] SK31N_FB_ROT = { 75.0f, 76.641f, 79.792f, 81.797f, 80.0f, 80.0f, 80.0f, 56.113f, 46.083f, 36.543f, 28.406f, 22.587f, 20.0f, 18.892f, 16.035f, 12.128f, 7.872f, 3.965f, 1.108f, 0.0f, 0.0f, 0.0f, 0.0f, 11.719f, 37.5f, 63.281f, 75.0f };
    static readonly float[] SK31N_HB_ROT = { 5.0f, 5.781f, 7.333f, 8.469f, 8.0f, 8.0f, 8.0f, -1.324f, -5.3f, -9.051f, -12.181f, -14.296f, -15.0f, -14.169f, -12.026f, -9.096f, -5.904f, -2.974f, -0.831f, 0.0f, 0.0f, 0.0f, 0.0f, 0.781f, 2.5f, 4.219f, 5.0f };
    static readonly float[] SK31N_SF_ROT = { 0.0f, -8.262f, -13.995f, -16.498f, -10.0f, -10.0f, -10.0f, 54.362f, 80.922f, 106.117f, 127.596f, 143.007f, 150.0f, 142.522f, 123.236f, 96.866f, 68.134f, 41.764f, 22.478f, 15.0f, 15.0f, 15.0f, 15.0f, 11.864f, 4.966f, -1.932f, -5.068f };
    static readonly float[] SK31N_FF_ROT = { 5.682f, 6.133f, 7.244f, 8.654f, 10.0f, 10.0f, 10.0f, 14.094f, 15.527f, 16.894f, 18.135f, 19.19f, 20.0f, 19.723f, 19.009f, 18.032f, 16.968f, 15.991f, 15.277f, 15.0f, 15.0f, 15.0f, 15.0f, 13.544f, 10.341f, 7.138f, 5.682f };
    static readonly float[] SK31N_HF_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -1.385f, -4.956f, -9.84f, -15.16f, -20.044f, -23.615f, -25.0f, -25.0f, -25.0f, -25.0f, -21.094f, -12.5f, -3.906f, 0.0f };
    static readonly float[] SK31N_THIGH_F_ROT = { 0.0f, 2.656f, 8.75f, 15.469f, 20.0f, 20.0f, 20.0f, 25.625f, 26.833f, 27.812f, 28.625f, 29.333f, 30.0f, 26.676f, 18.105f, 6.385f, -6.385f, -18.105f, -26.676f, -30.0f, -30.0f, -30.0f, -30.0f, -25.312f, -15.0f, -4.688f, 0.0f };
    static readonly float[] SK31N_CALF_F_ROT = { 0.0f, -4.531f, -15.0f, -26.719f, -35.0f, -35.0f, -35.0f, -49.004f, -52.833f, -55.996f, -58.344f, -59.728f, -60.0f, -58.338f, -54.052f, -48.192f, -41.808f, -35.948f, -31.662f, -30.0f, -30.0f, -30.0f, -30.0f, -25.312f, -15.0f, -4.688f, 0.0f };
    static readonly float[] SK31N_FOOT_F_ROT = { 0.0f, 1.094f, 3.583f, 6.281f, 8.0f, 8.0f, 8.0f, 9.609f, 9.833f, 9.953f, 10.0f, 10.005f, 10.0f, 9.446f, 8.017f, 6.064f, 3.936f, 1.983f, 0.554f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] SK31N_THIGH_B_ROT = { 0.0f, -1.953f, -6.458f, -11.484f, -15.0f, -15.0f, -15.0f, -20.371f, -21.75f, -22.91f, -23.844f, -24.543f, -25.0f, -21.122f, -11.122f, 2.551f, 17.449f, 31.122f, 41.122f, 45.0f, 45.0f, 45.0f, 45.0f, 37.969f, 22.5f, 7.031f, 0.0f };
    static readonly float[] SK31N_CALF_B_ROT = { 0.0f, -3.75f, -12.5f, -22.5f, -30.0f, -30.0f, -30.0f, -46.055f, -51.0f, -55.195f, -58.312f, -60.023f, -60.0f, -58.338f, -54.052f, -48.192f, -41.808f, -35.948f, -31.662f, -30.0f, -30.0f, -30.0f, -30.0f, -25.312f, -15.0f, -4.688f, 0.0f };
    static readonly float[] SK31N_FOOT_B_ROT = { 0.0f, 1.094f, 3.583f, 6.281f, 8.0f, 8.0f, 8.0f, 9.984f, 10.367f, 10.578f, 10.6f, 10.414f, 10.0f, 8.615f, 5.044f, 0.16f, -5.16f, -10.044f, -13.615f, -15.0f, -15.0f, -15.0f, -15.0f, -12.656f, -7.5f, -2.344f, 0.0f };
    static readonly float[] SK31N_AXE_POS_X = { 0.19f, 0.19f, 0.19f, 0.19f, 0.19f, 0.19f, 0.19f, 0.138f, 0.116f, 0.104f, 0.107f, 0.134f, 0.19f, 0.372f, 0.684f, 1.0f, 1.08f, 1.255f, 1.43f, 1.51f, 1.51f, 1.51f, 1.51f, 1.0f, 0.668f, 0.339f, 0.19f };

    // ================================================================
    // Buff 커브 (rest -> 웅크림(대기) -> 확 펼침(3프레임 유지) -> rest), 19프레임 논루프
    // ================================================================
    static readonly float[] BUFF2_BODY_ROT = { 0.0f, -3.623f, -12.593f, -24.062f, -35.185f, -43.113f, -45.0f, -45.0f, -45.0f, -45.0f, -45.0f, -15.0f, 15.0f, 15.0f, 15.0f, 12.656f, 7.5f, 2.344f, 0.0f };
    static readonly float[] BUFF2_HEAD_ROT = { 0.0f, -1.227f, -4.259f, -8.125f, -11.852f, -14.468f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -10.0f, -5.0f, -5.0f, -5.0f, -4.219f, -2.5f, -0.781f, 0.0f };
    static readonly float[] BUFF2_SF_ROT = { -5.068f, 4.485f, 28.28f, 59.024f, 89.422f, 112.179f, 120.0f, 120.0f, 120.0f, 120.0f, 120.0f, 52.5f, -15.0f, -15.0f, -15.0f, -13.448f, -10.034f, -6.62f, -5.068f };
    static readonly float[] BUFF2_FF_ROT = { 5.682f, 11.878f, 27.382f, 47.571f, 67.82f, 83.504f, 90.0f, 90.0f, 90.0f, 90.0f, 90.0f, 90.0f, 90.0f, 90.0f, 90.0f, 76.825f, 47.841f, 18.857f, 5.682f };
    static readonly float[] BUFF2_HF_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] BUFF2_SB_ROT = { 10.0f, 19.398f, 43.519f, 76.25f, 111.481f, 143.102f, 165.0f, 165.0f, 165.0f, 165.0f, 165.0f, 77.5f, -10.0f, -10.0f, -10.0f, -6.875f, 0.0f, 6.875f, 10.0f };
    static readonly float[] BUFF2_FB_ROT = { 75.0f, 73.53f, 69.63f, 64.062f, 57.593f, 50.984f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 67.5f, 90.0f, 90.0f, 90.0f, 87.656f, 82.5f, 77.344f, 75.0f };
    static readonly float[] BUFF2_HB_ROT = { 5.0f, 4.688f, 3.889f, 2.812f, 1.667f, 0.66f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.781f, 2.5f, 4.219f, 5.0f };
    static readonly float[] BUFF2_BOT_ROT = { 0.0f, 2.396f, 8.333f, 15.938f, 23.333f, 28.646f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 12.5f, -5.0f, -5.0f, -5.0f, -4.219f, -2.5f, -0.781f, 0.0f };
    static readonly float[] BUFF2_THIGH_F_ROT = { 0.0f, 3.218f, 11.296f, 21.875f, 32.593f, 41.088f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 15.0f, -15.0f, -15.0f, -15.0f, -12.656f, -7.5f, -2.344f, 0.0f };
    static readonly float[] BUFF2_CALF_F_ROT = { 0.0f, -3.333f, -11.667f, -22.5f, -33.333f, -41.667f, -45.0f, -45.0f, -45.0f, -45.0f, -45.0f, -22.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] BUFF2_FOOT_F_ROT = { 0.0f, 1.111f, 3.889f, 7.5f, 11.111f, 13.889f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 10.0f, 5.0f, 5.0f, 5.0f, 4.219f, 2.5f, 0.781f, 0.0f };
    static readonly float[] BUFF2_THIGH_B_ROT = { 0.0f, 4.514f, 15.778f, 30.375f, 44.889f, 55.903f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 30.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] BUFF2_CALF_B_ROT = { 0.0f, -4.444f, -15.556f, -30.0f, -44.444f, -55.556f, -60.0f, -60.0f, -60.0f, -60.0f, -60.0f, -35.0f, -10.0f, -10.0f, -10.0f, -8.438f, -5.0f, -1.562f, 0.0f };
    static readonly float[] BUFF2_FOOT_B_ROT = { 0.0f, 1.111f, 3.889f, 7.5f, 11.111f, 13.889f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 7.5f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] BUFF2_AXE_POS_X = { 0.19f, 0.271f, 0.482f, 0.77f, 1.085f, 1.375f, 1.59f, 1.59f, 1.59f, 1.59f, 1.59f, 1.225f, 0.86f, 0.86f, 0.86f, 0.755f, 0.525f, 0.295f, 0.19f };
    static readonly float[] BUFF1_AXE_POS_X = { 0.19f, 0.271f, 0.482f, 0.77f, 1.085f, 1.375f, 1.59f, 1.59f, 1.59f, 1.59f, 1.59f, 0.885f, 0.18f, 0.18f, 0.18f, 0.182f, 0.185f, 0.188f, 0.19f };  // Buff1 전용: 가슴편 자세 때 Axe.x=0.180

    // ================================================================
    // Attack2 커브 (Attack 베이스, scale=-1 10프레임 동안 새 좌표로 도달 후 기존 스윙 이어붙임)
    // ================================================================
    static readonly float[] ATK2_BODY_SCALE_X = { 1.0f, 1.0f, 0.2f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -0.2f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };
    static readonly float[] ATK2_BODY_Y = { 0.0f, -0.037f, -0.132f, -0.262f, -0.404f, -0.534f, -0.629f, -0.666f, -0.666f, -0.666f, -0.666f, -0.666f, -0.666f, -0.666f, -0.006f, 0.029f, 0.059f, 0.08f, 0.087f, 0.081f, 0.068f, 0.056f, 0.05f };
    static readonly float[] ATK2_BODY_ROT = { 0.0f, 0.024f, 0.089f, 0.188f, 0.311f, 0.451f, 0.6f, 0.749f, 0.889f, 1.012f, 1.111f, 1.176f, 1.2f, 1.2f, 7.213f, 12.425f, 16.839f, 20.0f, 21.11f, 20.309f, 18.553f, 16.798f, 16.0f };
    static readonly float[] ATK2_BODY_X = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.079f, 0.107f, 0.132f, 0.15f, 0.157f, 0.153f, 0.144f, 0.134f, 0.13f };
    static readonly float[] ATK2_HEAD_ROT = { 0.0f, 0.059f, 0.222f, 0.469f, 0.778f, 1.128f, 1.5f, 1.872f, 2.222f, 2.531f, 2.778f, 2.941f, 3.0f, 3.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] ATK2_SF_ROT = { 0.0f, 0.735f, 2.767f, 5.836f, 9.684f, 14.05f, 18.676f, 23.302f, 27.668f, 31.516f, 34.585f, 36.617f, 37.352f, 37.352f, 105.802f, 117.047f, 127.238f, 135.614f, 142.02f, 146.85f, 150.18f, 152.097f, 152.71f };
    static readonly float[] ATK2_FF_ROT = { 25.0f, 24.281f, 22.292f, 19.289f, 15.524f, 11.251f, 6.724f, 2.197f, -2.076f, -5.841f, -8.844f, -10.833f, -11.552f, -11.552f, -11.238f, -11.238f, -11.238f, -11.238f, -11.238f, -11.238f, -11.238f, -11.238f, -11.238f };
    static readonly float[] ATK2_SB_ROT = { 0.0f, -0.256f, -0.963f, -2.031f, -3.37f, -4.89f, -6.5f, -8.11f, -9.63f, -10.969f, -12.037f, -12.744f, -13.0f, -13.0f, 60.697f, 87.019f, 110.697f, 130.0f, 144.225f, 154.228f, 160.618f, 164.006f, 165.0f };
    static readonly float[] ATK2_FB_ROT = { 75.0f, 73.849f, 70.667f, 65.859f, 59.833f, 52.995f, 45.75f, 38.505f, 31.667f, 25.641f, 20.833f, 17.651f, 16.5f, 16.5f, 45.664f, 35.582f, 26.785f, 20.0f, 16.247f, 15.308f, 16.046f, 17.322f, 18.0f };
    static readonly float[] ATK2_HB_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 6.279f, 9.969f, 12.992f, 15.0f, 15.158f, 13.533f, 11.129f, 8.95f, 8.0f };
    static readonly float[] ATK2_AXE_POS_X = { 0.191f, 0.218f, 0.294f, 0.409f, 0.553f, 0.716f, 0.89f, 1.063f, 1.226f, 1.37f, 1.485f, 1.561f, 1.588f, 1.588f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f };
    static readonly float[] ATK2_BOT_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -4.382f, -7.507f, -10.139f, -12.0f, -12.574f, -11.941f, -10.721f, -9.534f, -9.0f };
    static readonly float[] ATK2_THIGH_F_ROT = { 10.0f, 10.434f, 11.554f, 13.085f, 14.754f, 16.285f, 17.405f, 17.839f, 17.839f, 17.839f, 17.839f, 17.839f, 17.839f, 17.839f, 15.602f, 18.236f, 20.444f, 22.0f, 22.426f, 21.775f, 20.61f, 19.497f, 19.0f };
    static readonly float[] ATK2_CALF_F_ROT = { 0.0f, -4.282f, -15.324f, -30.423f, -46.873f, -61.972f, -73.014f, -77.296f, -77.296f, -77.296f, -77.296f, -77.296f, -77.296f, -77.296f, -5.105f, -4.214f, -3.476f, -3.0f, -2.907f, -3.13f, -3.5f, -3.847f, -4.0f };
    static readonly float[] ATK2_FOOT_F_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 3.418f, 5.334f, 6.92f, 8.0f, 8.18f, 7.498f, 6.425f, 5.435f, 5.0f };
    static readonly float[] ATK2_THIGH_B_ROT = { -6.0f, -2.811f, 5.413f, 16.657f, 28.909f, 40.153f, 48.377f, 51.566f, 51.566f, 51.566f, 51.566f, 51.566f, 51.566f, 51.566f, -24.852f, -26.24f, -27.316f, -28.0f, -27.671f, -26.22f, -24.334f, -22.698f, -22.0f };
    static readonly float[] ATK2_CALF_B_ROT = { 0.0f, -2.752f, -9.848f, -19.551f, -30.123f, -39.826f, -46.922f, -49.674f, -49.674f, -49.674f, -49.674f, -49.674f, -49.674f, -49.674f, -20.795f, -22.599f, -24.036f, -25.0f, -24.819f, -23.386f, -21.444f, -19.735f, -19.0f };
    static readonly float[] ATK2_FOOT_B_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 7.408f, 11.841f, 15.508f, 18.0f, 18.404f, 16.81f, 14.313f, 12.011f, 11.0f };

    // ================================================================
    // Skill1 / Skill1-1 커브 (2연타: 크게 위로 당겨 windup -> 크로스오버 크라우치 임팩트
    // -> 점프 재windup(공중) -> 착지 임팩트). 29프레임 논루프.
    // 다리 히에라키(Front/Back)가 포즈마다 교차되고, 공중 포즈(3번)에서 어느 쪽이 화면 오른쪽에
    // 오는지에 따라 S1_*(Front가 오른쪽=Skill1), S1B_*(Back이 오른쪽=Skill1-1) 두 변형으로 나눔.
    // 팔/몸통/도끼그립은 두 변형이 공유.
    // ================================================================
    // MainBoss2_Skill1(3).anim의 현재 Body Y 기준, 9~29프레임에 -0.53 적용
    static readonly float[] S1_BODY_Y = { 0.0f, 0.010222f, 0.034778f, 0.068f, 0.103222f, 0.132778f, 0.15f, 0.126222f, 0.081778f, -0.45f, -0.45f, -0.45f, -0.45f, -0.486f, -0.51f, -0.465f, -0.299f, -0.271f, -0.249f, -0.235f, -0.23f, -0.28f, -0.38f, -0.45f, -0.45f, -0.45f, -0.45f, -0.486f, -0.51f, -0.465f, 0.009f, 0.004f, 0.001f, 0.0f };
    static readonly float[] S1_BODY_ROT = { 0.0f, 1.565f, 5.407f, 10.25f, 14.815f, 17.824f, 18.0f, 8.764f, -6.694f, -15.0f, -15.0f, -15.0f, -15.0f, -12.0f, -10.0f, -13.75f, -0.273f, 5.316f, 10.21f, 13.681f, 15.0f, 7.556f, -6.556f, -15.0f, -15.0f, -15.0f, -15.0f, -12.0f, -10.0f, -13.75f, -7.006f, -3.586f, -1.015f, 0.0f };
    static readonly float[] S1_BODY_X = { 0.0f, -0.006f, -0.019f, -0.035f, -0.049f, -0.056f, -0.05f, -0.002f, 0.075f, 0.12f, 0.12f, 0.12f, 0.12f, 0.12f, 0.12f, 0.12f, 0.091f, 0.075f, 0.061f, 0.052f, 0.05f, 0.077f, 0.125f, 0.15f, 0.15f, 0.15f, 0.15f, 0.15f, 0.15f, 0.15f, 0.089f, 0.07f, 0.056f, 0.05f };
    static readonly float[] S1_BOT_ROT = { 0.0f, 0.895f, 3.086f, 5.833f, 8.395f, 10.031f, 10.0f, 4.402f, -4.9f, -10.0f, -10.0f, -10.0f, -10.0f, -10.0f, -10.0f, -10.0f, -2.455f, 0.575f, 3.261f, 5.203f, 6.0f, 2.285f, -4.824f, -9.0f, -9.0f, -9.0f, -9.0f, -9.0f, -9.0f, -9.0f, -3.983f, -2.029f, -0.572f, 0.0f };
    static readonly float[] S1_HEAD_ROT = { 0.0f, 0.269f, 0.926f, 1.75f, 2.519f, 3.009f, 3.0f, 1.316f, -1.478f, -3.0f, -3.0f, -3.0f, -3.0f, -3.0f, -3.0f, -3.0f, -0.682f, 0.248f, 1.082f, 1.706f, 2.0f, 1.048f, -0.854f, -2.0f, -2.0f, -2.0f, -2.0f, -2.0f, -2.0f, -2.0f, -0.934f, -0.478f, -0.135f, 0.0f };
    static readonly float[] S1_SB_ROT = { 10.0f, 16.065f, 56.296f, 108.75f, 161.481f, 202.546f, 220.0f, 167.997f, 69.327f, 15.0f, 15.0f, 15.0f, 15.0f, 12.0f, 10.0f, 13.75f, 105.682f, 140.186f, 170.412f, 191.852f, 200.0f, 156.259f, 71.407f, 15.0f, 15.0f, 15.0f, 15.0f, 12.0f, 10.0f, 13.75f, -1.991f, 3.236f, 7.95f, 10.0f };
    static readonly float[] S1_FB_ROT = { 75.0f, 71.713f, 63.148f, 51.25f, 37.963f, 25.231f, 15.0f, 7.306f, 1.835f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 10.455f, 13.991f, 17.045f, 19.19f, 20.0f, 13.593f, 2.741f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 49.522f, 62.376f, 71.519f, 75.0f };
    static readonly float[] S1_HB_ROT = { 5.0f, -1.111f, -3.889f, -7.5f, -11.111f, -13.889f, -15.0f, -11.212f, -4.091f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -4.545f, -6.516f, -8.267f, -9.521f, -10.0f, -7.741f, -3.259f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 4.134f, 4.621f, 4.907f, 5.0f };
    static readonly float[] S1_SF_ROT = { 0.0f, 16.065f, 56.296f, 108.75f, 161.481f, 202.546f, 220.0f, 167.997f, 69.327f, 15.0f, 15.0f, 15.0f, 15.0f, 12.0f, 10.0f, 13.75f, 105.682f, 140.186f, 170.412f, 191.852f, 200.0f, 156.259f, 71.407f, 15.0f, 15.0f, 15.0f, 15.0f, 12.0f, 10.0f, 13.75f, -1.991f, 3.236f, 7.95f, 10.0f };
    static readonly float[] S1_FF_ROT = { 5.682f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f };
    static readonly float[] S1_AXE_POS_X = { 0.19f, 0.168f, 0.118f, 0.068f, 0.046f, 0.077f, 0.191f, 0.726f, 1.5f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 1.256f, 1.0f, 0.661f, 0.303f, 0.191f, 0.711f, 1.5f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 1.204f, 1.0f, 0.96f, 1.0f };
    static readonly float[] S1_THIGH_F_ROT = { 0.0f, 7.796f, 2.539f, -3.742f, -9.012f, -11.242f, -8.4f, 12.608f, 37.076f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.682f, 37.334f, 29.383f, 23.374f, 20.5f, 29.143f, 37.428f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 17.636f, 13.732f, 11.017f, 10.0f };
    static readonly float[] S1_CALF_F_ROT = { 0.0f, -0.031f, -0.432f, -1.667f, -4.198f, -8.488f, -15.0f, -31.807f, -55.651f, -70.0f, -70.0f, -70.0f, -70.0f, -70.0f, -70.0f, -70.0f, -72.273f, -69.769f, -66.676f, -63.313f, -60.0f, -53.438f, -43.441f, -35.0f, -35.0f, -35.0f, -35.0f, -35.0f, -35.0f, -35.0f, -9.367f, -4.49f, -1.204f, 0.0f };
    static readonly float[] S1_FOOT_F_ROT = { 0.0f, -0.231f, -0.741f, -1.25f, -1.481f, -1.157f, 0.0f, 4.63f, 11.481f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 8.136f, 5.492f, 3.06f, 1.132f, 0.0f, 1.791f, 5.785f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 3.149f, 1.586f, 0.443f, 0.0f };
    static readonly float[] S1_THIGH_B_ROT = { 0.0f, -2.855f, 5.198f, 16.083f, 27.728f, 38.059f, 45.0f, 45.644f, 41.029f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 35.0f, 3.636f, -4.08f, -10.256f, -14.144f, -15.0f, 0.76f, 29.246f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 14.734f, 4.478f, -3.065f, -6.0f };
    static readonly float[] S1_CALF_B_ROT = { 0.0f, -4.645f, -16.42f, -32.083f, -48.395f, -62.114f, -70.0f, -62.654f, -44.938f, -35.0f, -35.0f, -35.0f, -35.0f, -35.0f, -35.0f, -35.0f, -49.318f, -55.197f, -60.952f, -66.06f, -70.0f, -72.97f, -73.818f, -70.0f, -70.0f, -70.0f, -70.0f, -70.0f, -70.0f, -70.0f, -22.408f, -11.02f, -3.02f, 0.0f };
    static readonly float[] S1_FOOT_B_ROT = { 0.0f, 0.988f, 3.494f, 6.833f, 10.321f, 13.272f, 15.0f, 13.883f, 10.618f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 2.0f, 0.827f, 0.023f, -0.293f, 0.0f, 4.172f, 11.253f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 5.904f, 2.974f, 0.831f, 0.0f };
    static readonly float[] S1B_THIGH_F_ROT = { 0.0f, 8.025f, 2.901f, -4.167f, -11.975f, -19.321f, -25.0f, -28.272f, -29.691f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, 47.22f, 52.86f, 60.39f, 68.28f, 75.0f, 81.056f, 86.694f, 90.0f, 90.0f, 90.0f, 90.0f, 90.0f, 90.0f, 90.0f, 90.0f, 65.704f, 28.963f, 10.0f };
    static readonly float[] S1B_CALF_F_ROT = { 0.0f, -0.648f, -2.407f, -5.0f, -8.148f, -11.574f, -15.0f, -19.461f, -25.034f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -69.46f, -67.98f, -65.77f, -63.04f, -60.0f, -55.056f, -48.861f, -45.0f, -45.0f, -45.0f, -45.0f, -45.0f, -45.0f, -45.0f, -45.0f, -31.333f, -10.667f, 0.0f };  // f0~15는 Skill1AG 하체와 동일, f16부터 기존 커스텀
    static readonly float[] S1B_FOOT_F_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 14.14f, 11.82f, 8.43f, 4.36f, 0.0f, -6.685f, -14.898f, -20.0f, -20.0f, -20.0f, -20.0f, -20.0f, -20.0f, -20.0f, -20.0f, -13.926f, -4.741f, 0.0f };  // f0~15는 Skill1AG 하체와 동일, f16부터 기존 커스텀
    static readonly float[] S1B_THIGH_B_ROT = { 0.0f, -3.009f, 4.704f, 15.25f, 26.741f, 37.287f, 45.0f, 48.731f, 48.684f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 26.72f, 6.36f, -19.36f, -43.72f, -60.0f, -60.889f, -51.778f, -45.0f, -45.0f, -45.0f, -45.0f, -45.0f, -45.0f, -45.0f, -45.0f, -33.156f, -15.244f, -6.0f };
    static readonly float[] S1B_CALF_B_ROT = { 0.0f, -4.722f, -16.667f, -32.5f, -48.889f, -62.5f, -70.0f, -61.111f, -41.111f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -31.76f, -23.88f, -14.12f, -5.24f, 0.0f, -2.444f, -9.889f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -10.444f, -3.556f, 0.0f };  // f0~15는 Skill1AG 하체와 동일, f16부터 기존 커스텀
    static readonly float[] S1B_FOOT_B_ROT = { 0.0f, 1.343f, 4.63f, 8.75f, 12.593f, 15.046f, 15.0f, 6.785f, -6.987f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, 7.328f, 5.664f, 3.536f, 1.472f, 0.0f, -0.444f, -0.222f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };  // f0~15는 Skill1AG 하체와 동일, f16부터 기존 커스텀
    static readonly float[] S1B_BOT_ROT = { 0.0f, 0.509f, 1.852f, 3.75f, 5.926f, 8.102f, 10.0f, 12.118f, 14.236f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 14.144f, 11.85f, 8.531f, 4.6f, 0.469f, -3.45f, -6.744f, -9.0f, -9.0f, -9.0f, -9.0f, -9.0f, -9.0f, -9.0f, -9.0f, -6.267f, -2.133f, 0.0f };  // S1AG 기반, 임팩트2(f23)만 신규 좌표로 교체

    // ================================================================
    // Skill1AG 커브 (Skill1 구조 재사용 + 임팩트1을 실측 좌표로 교체)
    // ================================================================
    static readonly float[] S1AG_BODY_Y = { 0.0f, -0.012f, -0.043f, -0.082f, -0.119f, -0.145f, -0.15f, -0.096f, 0.004f, 0.08f, 0.08f, 0.08f, 0.08f, 0.044f, 0.02f, 0.065f, 0.231f, 0.259f, 0.281f, 0.295f, 0.3f, 0.25f, 0.15f, 0.08f, 0.08f, 0.08f, 0.08f, 0.044f, 0.02f, 0.065f, 0.009f, 0.004f, 0.001f, 0.0f };
    static readonly float[] S1AG_BODY_ROT = { 0.0f, 1.565f, 5.407f, 10.25f, 14.815f, 17.824f, 18.0f, 8.764f, -6.694f, -15.0f, -15.0f, -15.0f, -15.0f, -12.0f, -10.0f, -13.75f, -0.273f, 5.316f, 10.21f, 13.681f, 15.0f, 7.556f, -6.556f, -15.0f, -15.0f, -15.0f, -15.0f, -12.0f, -10.0f, -13.75f, -7.006f, -3.586f, -1.015f, 0.0f };
    static readonly float[] S1AG_BODY_X = { 0.0f, -0.006f, -0.019f, -0.035f, -0.049f, -0.056f, -0.05f, -0.002f, 0.075f, 0.12f, 0.12f, 0.12f, 0.12f, 0.12f, 0.12f, 0.12f, 0.094f, 0.079f, 0.064f, 0.054f, 0.05f, 0.068f, 0.102f, 0.12f, 0.12f, 0.12f, 0.12f, 0.12f, 0.12f, 0.12f, 0.078f, 0.064f, 0.054f, 0.05f };
    static readonly float[] S1AG_BOT_ROT = { 0.0f, 0.509f, 1.852f, 3.75f, 5.926f, 8.102f, 10.0f, 12.118f, 14.236f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 10.136f, 8.592f, 7.27f, 6.347f, 6.0f, 8.467f, 12.933f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 5.463f, 2.729f, 0.757f, 0.0f };
    static readonly float[] S1AG_HEAD_ROT = { 0.0f, 0.269f, 0.926f, 1.75f, 2.519f, 3.009f, 3.0f, 1.316f, -1.478f, -3.0f, -3.0f, -3.0f, -3.0f, -3.0f, -3.0f, -3.0f, -0.591f, 0.354f, 1.185f, 1.775f, 2.0f, 0.748f, -1.615f, -3.0f, -3.0f, -3.0f, -3.0f, -3.0f, -3.0f, -3.0f, -1.328f, -0.676f, -0.191f, 0.0f };
    static readonly float[] S1AG_SB_ROT = { 10.0f, 16.296f, 57.037f, 110.0f, 162.963f, 203.704f, 220.0f, 163.367f, 57.845f, 0.0f, 0.0f, 0.0f, 0.0f, 2.4f, 4.0f, 1.0f, 98.182f, 135.44f, 168.068f, 191.207f, 200.0f, 152.37f, 60.296f, 0.0f, 0.0f, 0.0f, 0.0f, 2.4f, 4.0f, 1.0f, -7.895f, 0.262f, 7.12f, 10.0f };
    static readonly float[] S1AG_FB_ROT = { 75.0f, 71.713f, 63.148f, 51.25f, 37.963f, 25.231f, 15.0f, 7.306f, 1.835f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 10.455f, 13.991f, 17.045f, 19.19f, 20.0f, 13.593f, 2.741f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 49.522f, 62.376f, 71.519f, 75.0f };
    static readonly float[] S1AG_HB_ROT = { 5.0f, -1.111f, -3.889f, -7.5f, -11.111f, -13.889f, -15.0f, -11.212f, -4.091f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -4.545f, -6.516f, -8.267f, -9.521f, -10.0f, -7.741f, -3.259f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 4.134f, 4.621f, 4.907f, 5.0f };
    static readonly float[] S1AG_SF_ROT = { 0.0f, 10.88f, 38.148f, 73.75f, 109.63f, 137.731f, 150.0f, 116.751f, 52.391f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 52.955f, 69.909f, 85.014f, 95.85f, 100.0f, 80.185f, 41.481f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, -1.443f, -1.108f, -0.394f, 0.0f };
    static readonly float[] S1AG_FF_ROT = { 5.682f, 25.895f, 28.086f, 30.833f, 33.395f, 35.031f, 35.0f, 29.422f, 20.14f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 22.045f, 24.934f, 27.486f, 29.306f, 30.0f, 26.222f, 19.111f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 20.697f, 22.813f, 24.385f, 25.0f };
    static readonly float[] S1AG_HF_ROT = { 0.0f, 0.386f, 1.235f, 2.083f, 2.469f, 1.929f, 0.0f, -7.716f, -19.136f, -25.0f, -25.0f, -25.0f, -25.0f, -25.0f, -25.0f, -25.0f, -12.5f, -7.91f, -3.906f, -1.074f, 0.0f, -6.481f, -18.519f, -25.0f, -25.0f, -25.0f, -25.0f, -25.0f, -25.0f, -25.0f, -9.84f, -4.956f, -1.385f, 0.0f };
    static readonly float[] S1AG_THIGH_F_ROT = { 0.0f, 8.954f, 6.243f, 2.508f, -1.605f, -5.455f, -8.4f, -10.54f, -20.331f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, 7.273f, 12.539f, 16.642f, 19.456f, 20.5f, 12.696f, -10.519f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -7.948f, 0.845f, 7.417f, 10.0f };
    static readonly float[] S1AG_CALF_F_ROT = { 0.0f, -0.648f, -2.407f, -5.0f, -8.148f, -11.574f, -15.0f, -19.461f, -25.034f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -49.091f, -53.384f, -56.847f, -59.158f, -60.0f, -53.556f, -40.444f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -7.399f, -3.499f, -0.927f, 0.0f };
    static readonly float[] S1AG_FOOT_F_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] S1AG_THIGH_B_ROT = { 0.0f, -3.009f, 4.704f, 15.25f, 26.741f, 37.287f, 45.0f, 48.731f, 48.684f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 9.545f, 0.149f, -7.67f, -13.018f, -15.0f, 0.356f, 29.044f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 45.0f, 14.734f, 4.478f, -3.065f, -6.0f };
    static readonly float[] S1AG_CALF_B_ROT = { 0.0f, -4.722f, -16.667f, -32.5f, -48.889f, -62.5f, -70.0f, -61.111f, -41.111f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -50.0f, -57.344f, -63.75f, -68.281f, -70.0f, -61.185f, -43.481f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, -6.665f, -3.09f, -0.805f, 0.0f };
    static readonly float[] S1AG_FOOT_B_ROT = { 0.0f, 1.343f, 4.63f, 8.75f, 12.593f, 15.046f, 15.0f, 6.785f, -6.987f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -8.864f, -5.705f, -2.855f, -0.794f, 0.0f, -3.889f, -11.111f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -5.904f, -2.974f, -0.831f, 0.0f };
    static readonly float[] S1AG_AXE_POS_X = { 0.19f, 0.172f, 0.129f, 0.087f, 0.068f, 0.095f, 0.191f, 0.67f, 1.3f, 1.51f, 1.51f, 1.51f, 1.51f, 1.51f, 1.51f, 1.51f, 1.151f, 1.0f, 0.695f, 0.32f, 0.191f, 0.761f, 1.5f, 1.51f, 1.51f, 1.51f, 1.51f, 1.51f, 1.51f, 1.51f, 1.086f, 1.0f, 0.982f, 1.0f };

    static readonly float[] S1_HF_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };  // Skill1은 기존처럼 HF 고정 0

    // ================================================================
    // Skill2 커브 (3연타: 돌진 몸빵 -> 도끼 크게 스윙업 -> 내려찍기, 28프레임 논루프)
    // ================================================================
    static readonly float[] SK2N_BODY_Y = { 0.0f, -0.008f, -0.028f, -0.052f, -0.072f, -0.08f, -0.08f, -0.08f, -0.08f, -0.08f, -0.08f, -0.08f, -0.08f, -0.08f, -0.06f, -0.015f, 0.03f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.037f, 0.013f, 0.0f };
    static readonly float[] SK2N_BODY_ROT = { 0.0f, -4.68f, -15.84f, -29.16f, -40.32f, -45.0f, -45.0f, -45.0f, -45.0f, -45.0f, -45.0f, -45.0f, -45.0f, -45.0f, -35.156f, -13.5f, 8.156f, 18.0f, 18.0f, 18.0f, 18.0f, 18.0f, 9.444f, -6.444f, -15.0f, -15.0f, -15.0f, -15.0f, -11.111f, -3.889f, 0.0f };
    static readonly float[] SK2N_BODY_X = { 0.0f, 0.016f, 0.053f, 0.097f, 0.134f, 0.15f, 0.15f, 0.15f, 0.15f, 0.15f, 0.15f, 0.15f, 0.15f, 0.15f, 0.134f, 0.1f, 0.066f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f, 0.05f };
    static readonly float[] SK2N_BOT_ROT = { 0.0f, 2.6f, 8.8f, 16.2f, 22.4f, 25.0f, 25.0f, 25.0f, 25.0f, 25.0f, 25.0f, 25.0f, 25.0f, 25.0f, 19.844f, 8.5f, -2.844f, -8.0f, -8.0f, -8.0f, -8.0f, -8.0f, -2.037f, 9.037f, 15.0f, 15.0f, 15.0f, 15.0f, 11.111f, 3.889f, 0.0f };
    static readonly float[] SK2N_HEAD_ROT = { 0.0f, -0.52f, -1.76f, -3.24f, -4.48f, -5.0f, -5.0f, -5.0f, -5.0f, -5.0f, -5.0f, -5.0f, -5.0f, -5.0f, -3.75f, -1.0f, 1.75f, 3.0f, 3.0f, 3.0f, 3.0f, 3.0f, 1.444f, -1.444f, -3.0f, -3.0f, -3.0f, -3.0f, -2.222f, -0.778f, 0.0f };
    static readonly float[] SK2N_SB_ROT = { 10.0f, 4.8f, -7.6f, -22.4f, -34.8f, -40.0f, -40.0f, -40.0f, -40.0f, -40.0f, -40.0f, -40.0f, -40.0f, -40.0f, -5.625f, 70.0f, 145.625f, 180.0f, 180.0f, 180.0f, 180.0f, 180.0f, 133.333f, 46.667f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] SK2N_FB_ROT = { 75.0f, 73.44f, 69.72f, 65.28f, 61.56f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 52.969f, 37.5f, 22.031f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 11.111f, 3.889f, 0.0f, 0.0f, 0.0f, 0.0f, 19.444f, 55.556f, 75.0f };
    static readonly float[] SK2N_HB_ROT = { 5.0f, 2.92f, -2.04f, -7.96f, -12.92f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -11.111f, -3.889f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] SK2N_SF_ROT = { 0.0f, -6.101f, -8.564f, -11.504f, -13.967f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, 15.469f, 82.5f, 149.531f, 180.0f, 180.0f, 180.0f, 180.0f, 180.0f, 137.222f, 57.778f, 15.0f, 15.0f, 15.0f, 15.0f, 9.797f, 0.135f, -5.068f };
    static readonly float[] SK2N_FF_ROT = { 5.682f, 6.131f, 7.202f, 8.48f, 9.551f, 10.0f, 10.0f, 10.0f, 10.0f, 10.0f, 10.0f, 10.0f, 10.0f, 10.0f, 8.438f, 5.0f, 1.562f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 3.889f, 11.111f, 15.0f, 15.0f, 15.0f, 15.0f, 12.584f, 8.098f, 5.682f };
    static readonly float[] SK2N_HF_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -6.481f, -18.519f, -25.0f, -25.0f, -25.0f, -25.0f, -18.519f, -6.481f, 0.0f };
    static readonly float[] SK2N_AXE_POS_X = { 0.19f, 0.19f, 0.19f, 0.19f, 0.19f, -1.88f, -1.88f, -1.88f, -1.88f, -1.88f, -1.88f, -1.88f, -1.88f, -1.88f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 2.11f, 1.612f, 0.688f, 0.19f };
    static readonly float[] SK2N_THIGH_F_ROT = { 0.0f, -1.04f, -3.52f, -6.48f, -8.96f, -10.0f, -10.0f, -10.0f, -10.0f, -10.0f, -10.0f, -10.0f, -10.0f, -10.0f, -8.438f, -5.0f, -1.562f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -7.778f, -22.222f, -30.0f, -30.0f, -30.0f, -30.0f, -22.222f, -7.778f, 0.0f };
    static readonly float[] SK2N_CALF_F_ROT = { 0.0f, -1.56f, -5.28f, -9.72f, -13.44f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, -14.219f, -12.5f, -10.781f, -10.0f, -10.0f, -10.0f, -10.0f, -10.0f, -15.185f, -24.815f, -30.0f, -30.0f, -30.0f, -30.0f, -22.222f, -7.778f, 0.0f };
    static readonly float[] SK2N_FOOT_F_ROT = { 0.0f, -2.6f, -8.8f, -16.2f, -22.4f, -25.0f, -25.0f, -25.0f, -25.0f, -25.0f, -25.0f, -25.0f, -25.0f, -25.0f, -21.094f, -12.5f, -3.906f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] SK2N_THIGH_B_ROT = { 0.0f, 7.8f, 26.4f, 48.6f, 67.2f, 75.0f, 75.0f, 75.0f, 75.0f, 75.0f, 75.0f, 75.0f, 75.0f, 75.0f, 67.188f, 50.0f, 32.812f, 25.0f, 25.0f, 25.0f, 25.0f, 25.0f, 30.185f, 39.815f, 45.0f, 45.0f, 45.0f, 45.0f, 33.333f, 11.667f, 0.0f };
    static readonly float[] SK2N_CALF_B_ROT = { 0.0f, -9.36f, -31.68f, -58.32f, -80.64f, -90.0f, -90.0f, -90.0f, -90.0f, -90.0f, -90.0f, -90.0f, -90.0f, -90.0f, -79.062f, -55.0f, -30.938f, -20.0f, -20.0f, -20.0f, -20.0f, -20.0f, -22.593f, -27.407f, -30.0f, -30.0f, -30.0f, -30.0f, -22.222f, -7.778f, 0.0f };
    static readonly float[] SK2N_FOOT_B_ROT = { 0.0f, 1.56f, 5.28f, 9.72f, 13.44f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 12.656f, 7.5f, 2.344f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -3.889f, -11.111f, -15.0f, -15.0f, -15.0f, -15.0f, -11.111f, -3.889f, 0.0f };
    static readonly float[] SK2N_AXE_SCALE_Y = { 0.0f, -0.104f, -0.352f, -0.648f, -0.896f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, -1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f };  // 돌진 끝(f13)까지 -1 유지, f14부터 1로 복귀
    static readonly float[] SK2N_HEAD_ROT_V1 = { 0.0f, 3.12f, 10.56f, 19.44f, 26.88f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 30.0f, 25.781f, 16.5f, 7.219f, 3.0f, 3.0f, 3.0f, 3.0f, 3.0f, 1.444f, -1.444f, -3.0f, -3.0f, -3.0f, -3.0f, -2.222f, -0.778f, 0.0f };

    // ================================================================
    // Generate Walk
    // ================================================================
    [MenuItem("Tools/2/MainBoss2/Generate Walk")]
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
    [MenuItem("Tools/2/MainBoss2/Generate Walk2")]
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
    [MenuItem("Tools/2/MainBoss2/Generate Attack")]
    public static void GenerateAttack()
    {
        const float DUR = 29 / FRAME_RATE;
        var clip = MakeClip("MainBoss2_Attack", 29, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 29, FRAME_RATE, loop: false);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", ATK_BODY_Y); C(BODY, "localPosition.x", ATK_BODY_X); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", ATK_BODY_ROT);
        C(BODY, "localScale.x", ATK_BODY_SCALE_X); L(BODY, "localScale.y", 1f); L(BODY, "localScale.z", 1f);
        L(HEAD, "localPosition.x", -0.275f); L(HEAD, "localPosition.y", 1.025f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", ATK_HEAD_ROT);

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
        C(AXE, "localPosition.x", ATK_AXE_POS_X); L(AXE, "localPosition.y", -0.263f); L(AXE, "localPosition.z", 0f);
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
    // Generate Attack2 (Attack 베이스 — scale=-1인 10프레임 동안 새 좌표로 도달, 이후 Attack의 스윙을 이어붙임)
    // ================================================================
    [MenuItem("Tools/2/MainBoss2/Generate Attack2")]
    public static void GenerateAttack2()
    {
        const float DUR = 23 / FRAME_RATE;
        var clip = MakeClip("MainBoss2_Attack2", 23, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 23, FRAME_RATE, loop: false);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", ATK2_BODY_Y); C(BODY, "localPosition.x", ATK2_BODY_X); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", ATK2_BODY_ROT);
        C(BODY, "localScale.x", ATK2_BODY_SCALE_X); L(BODY, "localScale.y", 1f); L(BODY, "localScale.z", 1f);
        L(HEAD, "localPosition.x", -0.275f); L(HEAD, "localPosition.y", 1.025f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", ATK2_HEAD_ROT);

        L(SF, "localPosition.x", -0.856f); L(SF, "localPosition.y", 0.601f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", ATK2_SF_ROT);
        L(FF, "localPosition.x", -0.245f); L(FF, "localPosition.y", -0.562f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", ATK2_FF_ROT);
        L(HF, "localPosition.x", -0.093f); L(HF, "localPosition.y", -0.678f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        L(SB, "localPosition.x", 0.467f); L(SB, "localPosition.y", 0.632f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", ATK2_SB_ROT);
        L(FB, "localPosition.x", 0.141f); L(FB, "localPosition.y", -0.506f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", ATK2_FB_ROT);
        L(HB, "localPosition.x", 0.026f); L(HB, "localPosition.y", -0.687f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", ATK2_HB_ROT);
        C(AXE, "localPosition.x", ATK2_AXE_POS_X); L(AXE, "localPosition.y", -0.263f); L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", -90f);

        L(BOTTOM, "localPosition.x", -0.141f); L(BOTTOM, "localPosition.y", 0f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", ATK2_BOT_ROT);

        L(THIGH_F, "localPosition.x", -0.572f); L(THIGH_F, "localPosition.y", -0.472f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", ATK2_THIGH_F_ROT);
        L(CALF_F, "localPosition.x", 0.008f); L(CALF_F, "localPosition.y", -0.718f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", ATK2_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.265f); L(FOOT_F, "localPosition.y", -0.978f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", ATK2_FOOT_F_ROT);

        L(THIGH_B, "localPosition.x", 0.394f); L(THIGH_B, "localPosition.y", -0.556f); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", ATK2_THIGH_B_ROT);
        L(CALF_B, "localPosition.x", 0.259f); L(CALF_B, "localPosition.y", -0.737f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", ATK2_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.060f); L(FOOT_B, "localPosition.y", -0.875f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", ATK2_FOOT_B_ROT);

        Save(clip, "MainBoss2_Attack2");
        GenerateAC();
    }


    // ================================================================
    // Generate Skill1 (front 버전 — 공중 포즈에서 Front 다리가 화면 오른쪽/두드러짐)
    // ================================================================
    [MenuItem("Tools/2/MainBoss2/Generate Skill1")]
    public static void GenerateSkill1()
    {
        GenerateSkill1Variant("MainBoss2_Skill1",
            S1_BODY_Y, S1_BODY_ROT, S1_BODY_X, S1_HEAD_ROT,
            S1_SF_ROT, S1_FF_ROT, S1_HF_ROT,
            S1_SB_ROT, S1_FB_ROT, S1_HB_ROT, S1_AXE_POS_X, S1_BOT_ROT,
            S1_THIGH_F_ROT, S1_CALF_F_ROT, S1_FOOT_F_ROT,
            S1_THIGH_B_ROT, S1_CALF_B_ROT, S1_FOOT_B_ROT);
    }

    // ================================================================
    // Generate Skill1-1 (back 버전 — 공중 포즈에서 Back 다리가 화면 오른쪽/두드러짐)
    // ================================================================
    [MenuItem("Tools/2/MainBoss2/Generate Skill1-1")]
    public static void GenerateSkill1_1()
    {
        GenerateSkill1Variant("MainBoss2_Skill1-1",
            S1AG_BODY_Y, S1AG_BODY_ROT, S1AG_BODY_X, S1AG_HEAD_ROT,
            S1AG_SF_ROT, S1AG_FF_ROT, S1AG_HF_ROT,
            S1AG_SB_ROT, S1AG_FB_ROT, S1AG_HB_ROT, S1AG_AXE_POS_X, S1B_BOT_ROT,
            S1B_THIGH_F_ROT, S1B_CALF_F_ROT, S1B_FOOT_F_ROT,
            S1B_THIGH_B_ROT, S1B_CALF_B_ROT, S1B_FOOT_B_ROT);
    }

    static void GenerateSkill1Variant(string clipName,
        float[] bodyY, float[] bodyRot, float[] bodyX, float[] headRot,
        float[] sfRot, float[] ffRot, float[] hfRot,
        float[] sbRot, float[] fbRot, float[] hbRot, float[] axeX, float[] botRot,
        float[] thighF, float[] calfF, float[] footF,
        float[] thighB, float[] calfB, float[] footB)
    {
        const float DUR = 34 / FRAME_RATE;
        var clip = MakeClip(clipName, 34, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 34, FRAME_RATE, loop: false);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", bodyY); C(BODY, "localPosition.x", bodyX); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", bodyRot);
        L(HEAD, "localPosition.x", -0.275f); L(HEAD, "localPosition.y", 1.025f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", headRot);

        L(SF, "localPosition.x", -0.856f); L(SF, "localPosition.y", 0.601f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", sfRot);
        L(FF, "localPosition.x", -0.245f); L(FF, "localPosition.y", -0.562f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", ffRot);
        L(HF, "localPosition.x", -0.093f); L(HF, "localPosition.y", -0.678f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", hfRot);

        L(SB, "localPosition.x", 0.467f); L(SB, "localPosition.y", 0.632f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", sbRot);
        L(FB, "localPosition.x", 0.141f); L(FB, "localPosition.y", -0.506f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", fbRot);
        L(HB, "localPosition.x", 0.026f); L(HB, "localPosition.y", -0.687f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", hbRot);
        C(AXE, "localPosition.x", axeX); L(AXE, "localPosition.y", -0.263f); L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", -90f);

        L(BOTTOM, "localPosition.x", -0.141f); L(BOTTOM, "localPosition.y", 0f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", botRot);

        L(THIGH_F, "localPosition.x", -0.572f); L(THIGH_F, "localPosition.y", -0.472f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", thighF);
        L(CALF_F, "localPosition.x", 0.008f); L(CALF_F, "localPosition.y", -0.718f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", calfF);
        L(FOOT_F, "localPosition.x", -0.265f); L(FOOT_F, "localPosition.y", -0.978f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", footF);

        L(THIGH_B, "localPosition.x", 0.394f); L(THIGH_B, "localPosition.y", -0.556f); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", thighB);
        L(CALF_B, "localPosition.x", 0.259f); L(CALF_B, "localPosition.y", -0.737f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", calfB);
        L(FOOT_B, "localPosition.x", -0.060f); L(FOOT_B, "localPosition.y", -0.875f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", footB);

        Save(clip, clipName);
        GenerateAC();
    }

    // ================================================================
    // Generate Skill1AG (Skill1 구조 재사용, 임팩트1만 실측 좌표로 교체 — Front 프로필 기반)
    // ================================================================
    [MenuItem("Tools/2/MainBoss2/Generate Skill1AG")]
    public static void GenerateSkill1AG()
    {
        const float DUR = 34 / FRAME_RATE;
        var clip = MakeClip("MainBoss2_Skill1AG", 34, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 34, FRAME_RATE, loop: false);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", S1AG_BODY_Y); C(BODY, "localPosition.x", S1AG_BODY_X); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", S1AG_BODY_ROT);
        L(HEAD, "localPosition.x", -0.275f); L(HEAD, "localPosition.y", 1.025f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", S1AG_HEAD_ROT);

        L(SF, "localPosition.x", -0.856f); L(SF, "localPosition.y", 0.601f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", S1AG_SF_ROT);
        L(FF, "localPosition.x", -0.245f); L(FF, "localPosition.y", -0.562f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", S1AG_FF_ROT);
        L(HF, "localPosition.x", -0.093f); L(HF, "localPosition.y", -0.678f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", S1AG_HF_ROT);

        L(SB, "localPosition.x", 0.467f); L(SB, "localPosition.y", 0.632f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", S1AG_SB_ROT);
        L(FB, "localPosition.x", 0.141f); L(FB, "localPosition.y", -0.506f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", S1AG_FB_ROT);
        L(HB, "localPosition.x", 0.026f); L(HB, "localPosition.y", -0.687f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", S1AG_HB_ROT);
        C(AXE, "localPosition.x", S1AG_AXE_POS_X); L(AXE, "localPosition.y", -0.263f); L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", -90f);

        L(BOTTOM, "localPosition.x", -0.141f); L(BOTTOM, "localPosition.y", 0f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", S1AG_BOT_ROT);

        L(THIGH_F, "localPosition.x", -0.572f); L(THIGH_F, "localPosition.y", -0.472f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", S1AG_THIGH_F_ROT);
        L(CALF_F, "localPosition.x", 0.008f); L(CALF_F, "localPosition.y", -0.718f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", S1AG_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.265f); L(FOOT_F, "localPosition.y", -0.978f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", S1AG_FOOT_F_ROT);

        L(THIGH_B, "localPosition.x", 0.394f); L(THIGH_B, "localPosition.y", -0.556f); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", S1AG_THIGH_B_ROT);
        L(CALF_B, "localPosition.x", 0.259f); L(CALF_B, "localPosition.y", -0.737f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", S1AG_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.060f); L(FOOT_B, "localPosition.y", -0.875f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", S1AG_FOOT_B_ROT);

        Save(clip, "MainBoss2_Skill1AG");
        GenerateAC();
    }



    // ================================================================
    // Generate Skill2 (3연타 — 돌진 몸빵 → 스윙 → 내려찍기)
    // 콘티 기반 1차 추측: 빠르게 돌진해 몸으로 들이받고(HIT1), 도끼를 들어 크게 휘두르고(HIT2),
    // 마지막으로 내려찍음(HIT3). 논루프, 마지막 내려찍은 자세로 끝.
    // ================================================================
    [MenuItem("Tools/2/MainBoss2/Generate Skill2")]
    public static void GenerateSkill2()
    {
        GenerateSkill2Variant("MainBoss2_Skill2", SK2N_HEAD_ROT);
    }

    [MenuItem("Tools/2/MainBoss2/Generate Skill2-1")]
    public static void GenerateSkill2_1()
    {
        GenerateSkill2Variant("MainBoss2_Skill2-1", SK2N_HEAD_ROT_V1);
    }

    static void GenerateSkill2Variant(string clipName, float[] headRot)
    {
        const float DUR = 31 / FRAME_RATE;
        var clip = MakeClip(clipName, 31, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 31, FRAME_RATE, loop: false);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", SK2N_BODY_Y); C(BODY, "localPosition.x", SK2N_BODY_X); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", SK2N_BODY_ROT);
        L(HEAD, "localPosition.x", -0.275f); L(HEAD, "localPosition.y", 1.025f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", headRot);

        L(SF, "localPosition.x", -0.856f); L(SF, "localPosition.y", 0.601f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", SK2N_SF_ROT);
        L(FF, "localPosition.x", -0.245f); L(FF, "localPosition.y", -0.562f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", SK2N_FF_ROT);
        L(HF, "localPosition.x", -0.093f); L(HF, "localPosition.y", -0.678f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", SK2N_HF_ROT);

        L(SB, "localPosition.x", 0.467f); L(SB, "localPosition.y", 0.632f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", SK2N_SB_ROT);
        L(FB, "localPosition.x", 0.141f); L(FB, "localPosition.y", -0.506f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", SK2N_FB_ROT);
        L(HB, "localPosition.x", 0.026f); L(HB, "localPosition.y", -0.687f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", SK2N_HB_ROT);
        C(AXE, "localPosition.x", SK2N_AXE_POS_X); L(AXE, "localPosition.y", -0.263f); L(AXE, "localPosition.z", 0f);
        L(AXE, "localScale.x", 1f); C(AXE, "localScale.y", SK2N_AXE_SCALE_Y); L(AXE, "localScale.z", 1f);
        L(AXE, "localEulerAngles.z", -90f);

        L(BOTTOM, "localPosition.x", -0.141f); L(BOTTOM, "localPosition.y", 0f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", SK2N_BOT_ROT);

        L(THIGH_F, "localPosition.x", -0.572f); L(THIGH_F, "localPosition.y", -0.472f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", SK2N_THIGH_F_ROT);
        L(CALF_F, "localPosition.x", 0.008f); L(CALF_F, "localPosition.y", -0.718f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", SK2N_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.265f); L(FOOT_F, "localPosition.y", -0.978f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", SK2N_FOOT_F_ROT);

        L(THIGH_B, "localPosition.x", 0.394f); L(THIGH_B, "localPosition.y", -0.556f); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", SK2N_THIGH_B_ROT);
        L(CALF_B, "localPosition.x", 0.259f); L(CALF_B, "localPosition.y", -0.737f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", SK2N_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.060f); L(FOOT_B, "localPosition.y", -0.875f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", SK2N_FOOT_B_ROT);

        Save(clip, clipName);
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
    [MenuItem("Tools/2/MainBoss2/Generate Skill3")]
    public static void GenerateSkill3()
    {
        const float DUR = 21 / FRAME_RATE;
        var clip = MakeClip("MainBoss2_Skill3", 21, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 21, FRAME_RATE, loop: false);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", SK3N_BODY_Y); C(BODY, "localPosition.x", SK3N_BODY_X); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", SK3N_BODY_ROT);
        L(HEAD, "localPosition.x", -0.275f); L(HEAD, "localPosition.y", 1.025f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", SK3N_HEAD_ROT);

        L(SF, "localPosition.x", -0.856f); L(SF, "localPosition.y", 0.601f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", SK3N_SF_ROT);
        L(FF, "localPosition.x", -0.245f); L(FF, "localPosition.y", -0.562f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", SK3N_FF_ROT);
        L(HF, "localPosition.x", -0.093f); L(HF, "localPosition.y", -0.678f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", SK3N_HF_ROT);

        L(SB, "localPosition.x", 0.467f); L(SB, "localPosition.y", 0.632f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", SK3N_SB_ROT);
        L(FB, "localPosition.x", 0.141f); L(FB, "localPosition.y", -0.506f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", SK3N_FB_ROT);
        L(HB, "localPosition.x", 0.026f); L(HB, "localPosition.y", -0.687f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", SK3N_HB_ROT);
        C(AXE, "localPosition.x", SK3N_AXE_POS_X); L(AXE, "localPosition.y", -0.263f); L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", -90f);

        L(BOTTOM, "localPosition.x", -0.141f); L(BOTTOM, "localPosition.y", 0f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", SK3N_BOT_ROT);

        L(THIGH_F, "localPosition.x", -0.572f); L(THIGH_F, "localPosition.y", -0.472f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", SK3N_THIGH_F_ROT);
        L(CALF_F, "localPosition.x", 0.008f); L(CALF_F, "localPosition.y", -0.718f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", SK3N_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.265f); L(FOOT_F, "localPosition.y", -0.978f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", SK3N_FOOT_F_ROT);

        L(THIGH_B, "localPosition.x", 0.394f); L(THIGH_B, "localPosition.y", -0.556f); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", SK3N_THIGH_B_ROT);
        L(CALF_B, "localPosition.x", 0.259f); L(CALF_B, "localPosition.y", -0.737f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", SK3N_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.060f); L(FOOT_B, "localPosition.y", -0.875f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", SK3N_FOOT_B_ROT);

        Save(clip, "MainBoss2_Skill3");
        GenerateAC();
    }


    // ================================================================
    // Generate Skill3-1 (rest -> 무릎 굽히는 예비동작 -> 점프 -> 임팩트)
    // ================================================================
    [MenuItem("Tools/2/MainBoss2/Generate Skill3-1")]
    public static void GenerateSkill3_1()
    {
        const float DUR = 27 / FRAME_RATE;
        var clip = MakeClip("MainBoss2_Skill3-1", 27, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 27, FRAME_RATE, loop: false);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", SK31N_BODY_Y); C(BODY, "localPosition.x", SK31N_BODY_X); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", SK31N_BODY_ROT);
        L(HEAD, "localPosition.x", -0.275f); L(HEAD, "localPosition.y", 1.025f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", SK31N_HEAD_ROT);

        L(SF, "localPosition.x", -0.856f); L(SF, "localPosition.y", 0.601f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", SK31N_SF_ROT);
        L(FF, "localPosition.x", -0.245f); L(FF, "localPosition.y", -0.562f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", SK31N_FF_ROT);
        L(HF, "localPosition.x", -0.093f); L(HF, "localPosition.y", -0.678f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", SK31N_HF_ROT);

        L(SB, "localPosition.x", 0.467f); L(SB, "localPosition.y", 0.632f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", SK31N_SB_ROT);
        L(FB, "localPosition.x", 0.141f); L(FB, "localPosition.y", -0.506f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", SK31N_FB_ROT);
        L(HB, "localPosition.x", 0.026f); L(HB, "localPosition.y", -0.687f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", SK31N_HB_ROT);
        C(AXE, "localPosition.x", SK31N_AXE_POS_X); L(AXE, "localPosition.y", -0.263f); L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", -90f);

        L(BOTTOM, "localPosition.x", -0.141f); L(BOTTOM, "localPosition.y", 0f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", SK31N_BOT_ROT);

        L(THIGH_F, "localPosition.x", -0.572f); L(THIGH_F, "localPosition.y", -0.472f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", SK31N_THIGH_F_ROT);
        L(CALF_F, "localPosition.x", 0.008f); L(CALF_F, "localPosition.y", -0.718f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", SK31N_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.265f); L(FOOT_F, "localPosition.y", -0.978f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", SK31N_FOOT_F_ROT);

        L(THIGH_B, "localPosition.x", 0.394f); L(THIGH_B, "localPosition.y", -0.556f); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", SK31N_THIGH_B_ROT);
        L(CALF_B, "localPosition.x", 0.259f); L(CALF_B, "localPosition.y", -0.737f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", SK31N_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.060f); L(FOOT_B, "localPosition.y", -0.875f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", SK31N_FOOT_B_ROT);

        Save(clip, "MainBoss2_Skill3-1");
        GenerateAC();
    }


    // ================================================================
    // Generate Buff (웅크렸다 가슴 펴며 일어남)
    // 콘티 기반 1차 추측: 깊게 웅크려 힘을 모았다가, 가슴을 펴며 힘차게 일어남.
    // 논루프, 일어난 파워 자세로 끝 (Idle로는 Animator 크로스페이드로 복귀).
    // ================================================================
    [MenuItem("Tools/2/MainBoss2/Generate Buff")]
    public static void GenerateBuff()
    {
        GenerateBuffVariant("MainBoss2_Buff", BUFF2_AXE_POS_X);
    }

    [MenuItem("Tools/2/MainBoss2/Generate Buff1")]
    public static void GenerateBuff1()
    {
        GenerateBuffVariant("MainBoss2_Buff1", BUFF1_AXE_POS_X);
    }

    static void GenerateBuffVariant(string clipName, float[] axeX)
    {
        const float DUR = 19 / FRAME_RATE;
        var clip = MakeClip(clipName, 19, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 19, FRAME_RATE, loop: false);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        L(BODY, "localPosition.y", 0f); L(BODY, "localPosition.x", 0f); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", BUFF2_BODY_ROT);
        L(HEAD, "localPosition.x", -0.275f); L(HEAD, "localPosition.y", 1.025f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", BUFF2_HEAD_ROT);

        L(SF, "localPosition.x", -0.856f); L(SF, "localPosition.y", 0.601f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", BUFF2_SF_ROT);
        L(FF, "localPosition.x", -0.245f); L(FF, "localPosition.y", -0.562f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", BUFF2_FF_ROT);
        L(HF, "localPosition.x", -0.093f); L(HF, "localPosition.y", -0.678f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", BUFF2_HF_ROT);

        L(SB, "localPosition.x", 0.467f); L(SB, "localPosition.y", 0.632f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", BUFF2_SB_ROT);
        L(FB, "localPosition.x", 0.141f); L(FB, "localPosition.y", -0.506f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", BUFF2_FB_ROT);
        L(HB, "localPosition.x", 0.026f); L(HB, "localPosition.y", -0.687f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", BUFF2_HB_ROT);
        C(AXE, "localPosition.x", axeX); L(AXE, "localPosition.y", -0.263f); L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", -90f);

        L(BOTTOM, "localPosition.x", -0.141f); L(BOTTOM, "localPosition.y", 0f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", BUFF2_BOT_ROT);

        L(THIGH_F, "localPosition.x", -0.572f); L(THIGH_F, "localPosition.y", -0.472f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", BUFF2_THIGH_F_ROT);
        L(CALF_F, "localPosition.x", 0.008f); L(CALF_F, "localPosition.y", -0.718f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", BUFF2_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.265f); L(FOOT_F, "localPosition.y", -0.978f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", BUFF2_FOOT_F_ROT);

        L(THIGH_B, "localPosition.x", 0.394f); L(THIGH_B, "localPosition.y", -0.556f); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", BUFF2_THIGH_B_ROT);
        L(CALF_B, "localPosition.x", 0.259f); L(CALF_B, "localPosition.y", -0.737f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", BUFF2_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.060f); L(FOOT_B, "localPosition.y", -0.875f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", BUFF2_FOOT_B_ROT);

        Save(clip, clipName);
        GenerateAC();
    }


    // ================================================================
    // Generate Run (추측 기반 1차 버전 — Walk보다 보폭/무릎 굴곡/팔 스윙 확대, 20프레임 사이클)
    // ================================================================
    [MenuItem("Tools/2/MainBoss2/Generate Run")]
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
    [MenuItem("Tools/2/MainBoss2/Generate Run2")]
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
    [MenuItem("Tools/2/MainBoss2/Generate Run3")]
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
    [MenuItem("Tools/2/MainBoss2/Generate AC")]
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
        if (vals == null || vals.Length == 0)
        {
            Debug.LogError($"[MainBoss2] 빈 배열: {path} / {prop}");
            return;
        }

        int count = Mathf.Min(total, vals.Length);

        if (vals.Length != total)
        {
            Debug.LogWarning($"[MainBoss2] 프레임 수 불일치: {path} / {prop} / total={total} / array={vals.Length}");
        }

        var keys = new Keyframe[count];
        for (int i = 0; i < count; i++) keys[i] = new Keyframe(i / rate, vals[i]);
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
        if (loop && count >= 3)
        {
            float dt = 2f / rate;
            float seamTangent = (vals[1] - vals[count - 2]) / dt;

            var k0 = curve.keys[0];
            k0.inTangent = seamTangent;
            k0.outTangent = seamTangent;
            curve.MoveKey(0, k0);
            AnimationUtility.SetKeyLeftTangentMode(curve, 0, AnimationUtility.TangentMode.Free);
            AnimationUtility.SetKeyRightTangentMode(curve, 0, AnimationUtility.TangentMode.Free);

            int last = count - 1;
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