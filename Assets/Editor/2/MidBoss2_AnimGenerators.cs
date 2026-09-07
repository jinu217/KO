using UnityEngine;
using UnityEditor;

// ================================================================
// MidBoss 2 애니메이션 생성기
// 창(spear) 든 중간보스. 콘티 기반 1차 추측 버전 - 실제 좌표 확인 후 다듬을 것.
// PPU 64, 12fps, 오른쪽 바라봄 3/4뷰 (MainBoss 시리즈와 동일 컨벤션)
// ================================================================
public class MidBoss2_AnimGenerators
{
    const string SAVE_PATH = "Assets/Animations/2/MidBoss 2";
    const float FRAME_RATE = 12f;

    const string BODY = "Body";
    const string HEAD = "Body/Head";
    const string SF = "Body/Shoulder_F";
    const string FF = "Body/Shoulder_F/Forearm_F";
    const string HF = "Body/Shoulder_F/Forearm_F/Hand_F";
    const string SB = "Body/Shoulder_B";
    const string FB = "Body/Shoulder_B/Forearm_B";
    const string HB = "Body/Shoulder_B/Forearm_B/Hand_B";
    const string WEAPON = "Body/Shoulder_B/Forearm_B/Hand_B/Spear";
    const string BOTTOM = "Body/Bottom";
    const string THIGH_F = "Body/Bottom/Thigh_F";
    const string CALF_F = "Body/Bottom/Thigh_F/Calf_F";
    const string FOOT_F = "Body/Bottom/Thigh_F/Calf_F/Foot_F";
    const string THIGH_B = "Body/Bottom/Thigh_B";
    const string CALF_B = "Body/Bottom/Thigh_B/Calf_B";
    const string FOOT_B = "Body/Bottom/Thigh_B/Calf_B/Foot_B";

    // ================================================================
    // 커브 배열 (Idle/Walk/Run + 4개 패턴, 전부 1차 추측)
    // ================================================================
    static readonly float[] IDLE_BODY_Y = { 0.0f, 0.004f, 0.008f, 0.011f, 0.015f, 0.018f, 0.021f, 0.024f, 0.026f, 0.028f, 0.029f, 0.03f, 0.03f, 0.03f, 0.029f, 0.028f, 0.026f, 0.024f, 0.021f, 0.018f, 0.015f, 0.011f, 0.008f, 0.004f, 0.0f, -0.004f, -0.008f, -0.011f, -0.015f, -0.018f, -0.021f, -0.024f, -0.026f, -0.028f, -0.029f, -0.03f, -0.03f, -0.03f, -0.029f, -0.028f, -0.026f, -0.024f, -0.021f, -0.018f, -0.015f, -0.011f, -0.008f, -0.004f };
    static readonly float[] IDLE_SF_Y = { 0.794f, 0.799f, 0.803f, 0.807f, 0.811f, 0.815f, 0.819f, 0.822f, 0.824f, 0.826f, 0.828f, 0.829f, 0.829f, 0.829f, 0.828f, 0.826f, 0.824f, 0.822f, 0.819f, 0.815f, 0.811f, 0.807f, 0.803f, 0.799f, 0.794f, 0.789f, 0.785f, 0.781f, 0.777f, 0.773f, 0.769f, 0.766f, 0.764f, 0.762f, 0.76f, 0.759f, 0.759f, 0.759f, 0.76f, 0.762f, 0.764f, 0.766f, 0.769f, 0.773f, 0.776f, 0.781f, 0.785f, 0.789f };
    static readonly float[] IDLE_THIGH_F_Y = { -0.178f, -0.169f, -0.16f, -0.151f, -0.143f, -0.135f, -0.129f, -0.122f, -0.117f, -0.113f, -0.11f, -0.109f, -0.108f, -0.109f, -0.11f, -0.113f, -0.117f, -0.122f, -0.129f, -0.135f, -0.143f, -0.151f, -0.16f, -0.169f, -0.178f, -0.187f, -0.196f, -0.205f, -0.213f, -0.221f, -0.227f, -0.234f, -0.239f, -0.243f, -0.246f, -0.247f, -0.248f, -0.247f, -0.246f, -0.243f, -0.239f, -0.234f, -0.227f, -0.221f, -0.213f, -0.205f, -0.196f, -0.187f };
    static readonly float[] IDLE_THIGH_B_Y = { -0.309f, -0.3f, -0.291f, -0.282f, -0.274f, -0.266f, -0.26f, -0.253f, -0.248f, -0.244f, -0.241f, -0.24f, -0.239f, -0.24f, -0.241f, -0.244f, -0.248f, -0.253f, -0.26f, -0.266f, -0.274f, -0.282f, -0.291f, -0.3f, -0.309f, -0.318f, -0.327f, -0.336f, -0.344f, -0.352f, -0.358f, -0.365f, -0.37f, -0.374f, -0.377f, -0.378f, -0.379f, -0.378f, -0.377f, -0.374f, -0.37f, -0.365f, -0.358f, -0.352f, -0.344f, -0.336f, -0.327f, -0.318f };
    static readonly float[] IDLE_BODY_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };  // 회전 고정 (MainBoss2 방식)
    static readonly float[] IDLE_HEAD_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] IDLE_SF_ROT = { 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f };
    static readonly float[] IDLE_FF_ROT = { 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f };
    static readonly float[] IDLE_HF_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] IDLE_SB_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };  // rest 그대로 0, 고정
    static readonly float[] IDLE_FB_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };  // rest 그대로 0, 고정
    static readonly float[] IDLE_HB_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] IDLE_BOT_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };  // Body_rot이 0이라 상쇄값도 0
    static readonly float[] IDLE_THIGH_F_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] IDLE_CALF_F_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] IDLE_FOOT_F_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] IDLE_THIGH_B_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] IDLE_CALF_B_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] IDLE_FOOT_B_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] IDLE2_SB_ROT = { 0.0f, 0.261f, 0.518f, 0.765f, 1.0f, 1.218f, 1.414f, 1.587f, 1.732f, 1.848f, 1.932f, 1.983f, 2.0f, 1.983f, 1.932f, 1.848f, 1.732f, 1.587f, 1.414f, 1.218f, 1.0f, 0.765f, 0.518f, 0.261f, 0.0f, -0.261f, -0.518f, -0.765f, -1.0f, -1.218f, -1.414f, -1.587f, -1.732f, -1.848f, -1.932f, -1.983f, -2.0f, -1.983f, -1.932f, -1.848f, -1.732f, -1.587f, -1.414f, -1.218f, -1.0f, -0.765f, -0.518f, -0.261f };  // 뒷팔 rot 0, Forearm_B만 60 -> 창을 수직으로 든 자세
    static readonly float[] IDLE2_FB_ROT = { 60.0f, 60.196f, 60.388f, 60.574f, 60.75f, 60.913f, 61.061f, 61.19f, 61.299f, 61.386f, 61.449f, 61.487f, 61.5f, 61.487f, 61.449f, 61.386f, 61.299f, 61.19f, 61.061f, 60.913f, 60.75f, 60.574f, 60.388f, 60.196f, 60.0f, 59.804f, 59.612f, 59.426f, 59.25f, 59.087f, 58.939f, 58.81f, 58.701f, 58.614f, 58.551f, 58.513f, 58.5f, 58.513f, 58.551f, 58.614f, 58.701f, 58.81f, 58.939f, 59.087f, 59.25f, 59.426f, 59.612f, 59.804f };
    static readonly float[] IDLE2_HB_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] WALK_BODY_Y = { 0.0f, 0.015f, 0.028f, 0.037f, 0.04f, 0.037f, 0.028f, 0.015f, 0.0f, 0.015f, 0.028f, 0.037f, 0.04f, 0.037f, 0.028f, 0.015f, 0.0f, 0.015f, 0.028f, 0.037f, 0.04f, 0.037f, 0.028f, 0.015f, 0.0f, 0.015f, 0.028f, 0.037f, 0.04f, 0.037f, 0.028f, 0.015f };
    static readonly float[] WALK_BODY_ROT = { 0.0f, 0.585f, 1.148f, 1.667f, 2.121f, 2.494f, 2.772f, 2.942f, 3.0f, 2.942f, 2.772f, 2.494f, 2.121f, 1.667f, 1.148f, 0.585f, 0.0f, -0.585f, -1.148f, -1.667f, -2.121f, -2.494f, -2.772f, -2.942f, -3.0f, -2.942f, -2.772f, -2.494f, -2.121f, -1.667f, -1.148f, -0.585f };
    static readonly float[] WALK_BOT_ROT = { -0.0f, -0.585f, -1.148f, -1.667f, -2.121f, -2.494f, -2.772f, -2.942f, -3.0f, -2.942f, -2.772f, -2.494f, -2.121f, -1.667f, -1.148f, -0.585f, -0.0f, 0.585f, 1.148f, 1.667f, 2.121f, 2.494f, 2.772f, 2.942f, 3.0f, 2.942f, 2.772f, 2.494f, 2.121f, 1.667f, 1.148f, 0.585f };
    static readonly float[] WALK_SF_ROT = { 15.0f, 12.074f, 9.26f, 6.666f, 4.393f, 2.528f, 1.142f, 0.288f, 0.0f, 0.288f, 1.142f, 2.528f, 4.393f, 6.666f, 9.26f, 12.074f, 15.0f, 17.926f, 20.74f, 23.334f, 25.607f, 27.472f, 28.858f, 29.712f, 30.0f, 29.712f, 28.858f, 27.472f, 25.607f, 23.334f, 20.74f, 17.926f };
    static readonly float[] WALK_FF_ROT = { 20.0f, 20.975f, 21.913f, 22.778f, 23.536f, 24.157f, 24.619f, 24.904f, 25.0f, 24.904f, 24.619f, 24.157f, 23.536f, 22.778f, 21.913f, 20.975f, 20.0f, 19.025f, 18.087f, 17.222f, 16.464f, 15.843f, 15.381f, 15.096f, 15.0f, 15.096f, 15.381f, 15.843f, 16.464f, 17.222f, 18.087f, 19.025f };
    static readonly float[] WALK_SB_ROT = { 165.0f, 166.561f, 168.061f, 169.445f, 170.657f, 171.652f, 172.391f, 172.846f, 173.0f, 172.846f, 172.391f, 171.652f, 170.657f, 169.445f, 168.061f, 166.561f, 165.0f, 163.439f, 161.939f, 160.555f, 159.343f, 158.348f, 157.609f, 157.154f, 157.0f, 157.154f, 157.609f, 158.348f, 159.343f, 160.555f, 161.939f, 163.439f };
    static readonly float[] WALK_FB_ROT = { 75.0f, 74.025f, 73.087f, 72.222f, 71.464f, 70.843f, 70.381f, 70.096f, 70.0f, 70.096f, 70.381f, 70.843f, 71.464f, 72.222f, 73.087f, 74.025f, 75.0f, 75.975f, 76.913f, 77.778f, 78.536f, 79.157f, 79.619f, 79.904f, 80.0f, 79.904f, 79.619f, 79.157f, 78.536f, 77.778f, 76.913f, 75.975f };
    static readonly float[] WALK_THIGH_F_ROT = { 0.0f, 4.877f, 9.567f, 13.889f, 17.678f, 20.787f, 23.097f, 24.52f, 25.0f, 24.52f, 23.097f, 20.787f, 17.678f, 13.889f, 9.567f, 4.877f, 0.0f, -4.877f, -9.567f, -13.889f, -17.678f, -20.787f, -23.097f, -24.52f, -25.0f, -24.52f, -23.097f, -20.787f, -17.678f, -13.889f, -9.567f, -4.877f };
    static readonly float[] WALK_CALF_F_ROT = { 0f, -6.828f, -13.394f, -19.445f, -24.749f, -29.101f, -32.336f, -34.327f, -35.0f, -34.327f, -32.336f, -29.101f, -24.749f, -19.445f, -13.394f, -6.828f, -0.0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };
    static readonly float[] WALK_FOOT_F_ROT = { 4.794f, 6.414f, 7.788f, 8.862f, 9.595f, 9.96f, 9.942f, 9.543f, 8.776f, 7.672f, 6.273f, 4.633f, 2.815f, 0.889f, -1.071f, -2.99f, -4.794f, -6.414f, -7.788f, -8.862f, -9.595f, -9.96f, -9.942f, -9.543f, -8.776f, -7.672f, -6.273f, -4.633f, -2.815f, -0.889f, 1.071f, 2.99f };
    static readonly float[] WALK_THIGH_B_ROT = { 0.0f, -4.877f, -9.567f, -13.889f, -17.678f, -20.787f, -23.097f, -24.52f, -25.0f, -24.52f, -23.097f, -20.787f, -17.678f, -13.889f, -9.567f, -4.877f, -0.0f, 4.877f, 9.567f, 13.889f, 17.678f, 20.787f, 23.097f, 24.52f, 25.0f, 24.52f, 23.097f, 20.787f, 17.678f, 13.889f, 9.567f, 4.877f };
    static readonly float[] WALK_CALF_B_ROT = { -0.0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, -6.828f, -13.394f, -19.445f, -24.749f, -29.101f, -32.336f, -34.327f, -35.0f, -34.327f, -32.336f, -29.101f, -24.749f, -19.445f, -13.394f, -6.828f };
    static readonly float[] WALK_FOOT_B_ROT = { -4.794f, -6.414f, -7.788f, -8.862f, -9.595f, -9.96f, -9.942f, -9.543f, -8.776f, -7.672f, -6.273f, -4.633f, -2.815f, -0.889f, 1.071f, 2.99f, 4.794f, 6.414f, 7.788f, 8.862f, 9.595f, 9.96f, 9.942f, 9.543f, 8.776f, 7.672f, 6.273f, 4.633f, 2.815f, 0.889f, -1.071f, -2.99f };
    static readonly float[] RUN_BODY_Y = { 0.0f, 0.035f, 0.057f, 0.057f, 0.035f, 0.0f, 0.035f, 0.057f, 0.057f, 0.035f, 0.0f, 0.035f, 0.057f, 0.057f, 0.035f, 0.0f, 0.035f, 0.057f, 0.057f, 0.035f };
    static readonly float[] RUN_BODY_ROT = { -10.0f, -11.545f, -12.939f, -14.045f, -14.755f, -15.0f, -14.755f, -14.045f, -12.939f, -11.545f, -10.0f, -8.455f, -7.061f, -5.955f, -5.245f, -5.0f, -5.245f, -5.955f, -7.061f, -8.455f };
    static readonly float[] RUN_BOT_ROT = { 0.0f, 3.09f, 5.878f, 8.09f, 9.511f, 10.0f, 9.511f, 8.09f, 5.878f, 3.09f, 0.0f, -3.09f, -5.878f, -8.09f, -9.511f, -10.0f, -9.511f, -8.09f, -5.878f, -3.09f };
    static readonly float[] RUN_SF_ROT = { 15.0f, 7.275f, 0.305f, -5.225f, -8.776f, -10.0f, -8.776f, -5.225f, 0.305f, 7.275f, 15.0f, 22.725f, 29.695f, 35.225f, 38.776f, 40.0f, 38.776f, 35.225f, 29.695f, 22.725f };
    static readonly float[] RUN_FF_ROT = { 25.0f, 28.09f, 30.878f, 33.09f, 34.511f, 35.0f, 34.511f, 33.09f, 30.878f, 28.09f, 25.0f, 21.91f, 19.122f, 16.91f, 15.489f, 15.0f, 15.489f, 16.91f, 19.122f, 21.91f };
    static readonly float[] RUN_SB_ROT = { 165.0f, 168.708f, 172.053f, 174.708f, 176.413f, 177.0f, 176.413f, 174.708f, 172.053f, 168.708f, 165.0f, 161.292f, 157.947f, 155.292f, 153.587f, 153.0f, 153.587f, 155.292f, 157.947f, 161.292f };
    static readonly float[] RUN_FB_ROT = { 75.0f, 72.528f, 70.298f, 68.528f, 67.392f, 67.0f, 67.392f, 68.528f, 70.298f, 72.528f, 75.0f, 77.472f, 79.702f, 81.472f, 82.608f, 83.0f, 82.608f, 81.472f, 79.702f, 77.472f };
    static readonly float[] RUN_THIGH_F_ROT = { 0.0f, 13.906f, 26.45f, 36.406f, 42.798f, 45.0f, 42.798f, 36.406f, 26.45f, 13.906f, 0.0f, -13.906f, -26.45f, -36.406f, -42.798f, -45.0f, -42.798f, -36.406f, -26.45f, -13.906f };
    static readonly float[] RUN_CALF_F_ROT = { 0f, -18.541f, -35.267f, -48.541f, -57.063f, -60.0f, -57.063f, -48.541f, -35.267f, -18.541f, -0.0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f };
    static readonly float[] RUN_FOOT_F_ROT = { 7.191f, 10.907f, 13.555f, 14.877f, 14.742f, 13.164f, 10.297f, 6.423f, 1.92f, -2.772f, -7.191f, -10.907f, -13.555f, -14.877f, -14.742f, -13.164f, -10.297f, -6.423f, -1.92f, 2.772f };
    static readonly float[] RUN_THIGH_B_ROT = { 0.0f, -13.906f, -26.45f, -36.406f, -42.798f, -45.0f, -42.798f, -36.406f, -26.45f, -13.906f, -0.0f, 13.906f, 26.45f, 36.406f, 42.798f, 45.0f, 42.798f, 36.406f, 26.45f, 13.906f };
    static readonly float[] RUN_CALF_B_ROT = { -0.0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, -18.541f, -35.267f, -48.541f, -57.063f, -60.0f, -57.063f, -48.541f, -35.267f, -18.541f };
    static readonly float[] RUN_FOOT_B_ROT = { -7.191f, -10.907f, -13.555f, -14.877f, -14.742f, -13.164f, -10.297f, -6.423f, -1.92f, 2.772f, 7.191f, 10.907f, 13.555f, 14.877f, 14.742f, 13.164f, 10.297f, 6.423f, 1.92f, -2.772f };
    static readonly float[] ATK_BODY_Y = { 0.0f, -0.006f, -0.019f, -0.034f, -0.046f, -0.05f, -0.05f, -0.018f, 0.005f, 0.02f, 0.02f, 0.02f, 0.04f, 0.045f, 0.048f, 0.05f, 0.051f, 0.05f, 0.05f, 0.05f, 0.024f, 0.012f, 0.004f, 0.0f };
    static readonly float[] ATK_BODY_ROT = { 0.0f, -0.921f, -3.083f, -5.584f, -7.524f, -8.0f, -8.0f, -1.056f, 3.26f, 5.0f, 5.0f, 5.0f, -0.518f, -3.476f, -6.437f, -9.067f, -11.033f, -12.0f, -12.0f, -12.0f, -6.268f, -3.27f, -0.938f, 0.0f };
    static readonly float[] ATK_BODY_X = { 0.0f, -0.01f, -0.031f, -0.052f, -0.063f, -0.05f, -0.05f, 0.11f, 0.204f, 0.25f, 0.25f, 0.25f, 0.209f, 0.176f, 0.14f, 0.104f, 0.073f, 0.05f, 0.05f, 0.05f, 0.012f, 0.005f, 0.001f, 0.0f };
    static readonly float[] ATK_HEAD_ROT = { 0.0f, -0.348f, -1.163f, -2.104f, -2.83f, -3.0f, -3.0f, -0.347f, 1.307f, 2.0f, 2.0f, 2.0f, 0.104f, -0.94f, -1.993f, -2.933f, -3.642f, -4.0f, -4.0f, -4.0f, -2.107f, -1.101f, -0.316f, 0.0f };
    static readonly float[] ATK_BOT_ROT = { 0.0f, 0.695f, 2.325f, 4.208f, 5.66f, 6.0f, 6.0f, 0.694f, -2.615f, -4.0f, -4.0f, -4.0f, -0.209f, 1.881f, 3.985f, 5.866f, 7.284f, 8.0f, 8.0f, 8.0f, 4.214f, 2.201f, 0.632f, 0.0f };
    static readonly float[] ATK_SF_ROT = { 15.0f, 16.218f, 19.053f, 22.28f, 24.671f, 25.0f, 25.0f, 14.236f, 7.682f, 5.0f, 5.0f, 5.0f, 12.896f, 17.202f, 21.546f, 25.446f, 28.424f, 30.0f, 30.0f, 30.0f, 23.036f, 19.206f, 16.21f, 15.0f };
    static readonly float[] ATK_FF_ROT = { 20.0f, 21.218f, 24.053f, 27.28f, 29.671f, 30.0f, 30.0f, 19.653f, 13.151f, 10.0f, 10.0f, 10.0f, 13.756f, 16.369f, 19.124f, 21.696f, 23.763f, 25.0f, 25.0f, 25.0f, 23.036f, 21.614f, 20.47f, 20.0f };
    static readonly float[] ATK_HF_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] ATK_SB_ROT = { 165.0f, 161.627f, 153.48f, 143.52f, 134.707f, 130.0f, 130.0f, 132.083f, 138.594f, 150.0f, 150.0f, 150.0f, 213.309f, 238.095f, 261.244f, 280.67f, 294.284f, 300.0f, 300.0f, 300.0f, 233.304f, 200.476f, 175.149f, 165.0f };
    static readonly float[] ATK_FB_ROT = { 75.0f, 78.844f, 87.533f, 96.8f, 102.378f, 100.0f, 100.0f, 50.694f, 20.729f, 5.0f, 5.0f, 5.0f, 10.84f, 18.333f, 26.973f, 35.781f, 43.783f, 50.0f, 50.0f, 50.0f, 66.25f, 70.741f, 73.843f, 75.0f };
    static readonly float[] ATK_HB_ROT = { 0.0f, -1.738f, -5.813f, -10.52f, -14.151f, -15.0f, -15.0f, -1.944f, 6.302f, 10.0f, 10.0f, 10.0f, 2.592f, -1.786f, -6.253f, -10.29f, -13.379f, -15.0f, -15.0f, -15.0f, -8.036f, -4.206f, -1.21f, 0.0f };
    static readonly float[] ATK_THIGH_F_ROT = { 0.0f, -1.484f, -4.853f, -8.48f, -10.738f, -10.0f, -10.0f, 7.847f, 18.88f, 25.0f, 25.0f, 25.0f, 25.533f, 23.869f, 21.722f, 19.353f, 17.025f, 15.0f, 15.0f, 15.0f, 6.161f, 3.095f, 0.863f, 0.0f };
    static readonly float[] ATK_CALF_F_ROT = { 0.0f, -0.253f, -0.96f, -2.04f, -3.413f, -5.0f, -5.0f, -10.625f, -13.516f, -15.0f, -15.0f, -15.0f, -14.66f, -13.988f, -13.131f, -12.143f, -11.081f, -10.0f, -10.0f, -10.0f, -4.196f, -2.116f, -0.592f, 0.0f };
    static readonly float[] ATK_FOOT_F_ROT = { 0.0f, -0.142f, -0.427f, -0.64f, -0.569f, 0.0f, 0.0f, 4.444f, 6.917f, 8.0f, 8.0f, 8.0f, 5.871f, 4.571f, 3.201f, 1.893f, 0.781f, 0.0f, 0.0f, 0.0f, -0.429f, -0.254f, -0.079f, 0.0f };
    static readonly float[] ATK_THIGH_B_ROT = { 0.0f, -1.547f, -5.44f, -10.56f, -15.787f, -20.0f, -20.0f, -26.875f, -29.297f, -30.0f, -30.0f, -30.0f, -26.272f, -24.226f, -21.964f, -19.598f, -17.24f, -15.0f, -15.0f, -15.0f, -5.893f, -2.937f, -0.813f, 0.0f };
    static readonly float[] ATK_CALF_B_ROT = { 0.0f, -0.951f, -3.253f, -6.08f, -8.604f, -10.0f, -10.0f, -7.569f, -5.651f, -5.0f, -5.0f, -5.0f, -8.903f, -10.774f, -12.547f, -13.996f, -14.889f, -15.0f, -15.0f, -15.0f, -7.232f, -3.73f, -1.062f, 0.0f };
    static readonly float[] ATK_FOOT_B_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] MS1_BODY_Y = { 0.0f, 0.01f, 0.031f, 0.049f, 0.05f, 0.011f, -0.049f, -0.08f, -0.08f, -0.015f, 0.03f, 0.05f, 0.017f, -0.044f, -0.08f, -0.08f, -0.036f, -0.001f, 0.02f, 0.02f, 0.02f, 0.02f, 0.02f, 0.007f, 0.002f, 0.0f };
    static readonly float[] MS1_BODY_ROT = { 0.0f, 0.603f, 1.857f, 2.933f, 3.0f, 0.608f, -3.085f, -5.0f, -5.0f, -1.0f, 1.75f, 3.0f, 1.021f, -2.735f, -5.0f, -5.0f, -2.942f, -1.117f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.186f, 0.056f, 0.0f };
    static readonly float[] MS1_BODY_X = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] MS1_HEAD_ROT = { 0.0f, -0.393f, -1.214f, -1.929f, -2.0f, -0.513f, 1.799f, 3.0f, 3.0f, 0.5f, -1.219f, -2.0f, -0.767f, 1.577f, 3.0f, 3.0f, 1.779f, 0.676f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -0.111f, -0.033f, 0.0f };
    static readonly float[] MS1_BOT_ROT = { 0.0f, 0.393f, 1.214f, 1.929f, 2.0f, 0.513f, -1.799f, -3.0f, -3.0f, -0.5f, 1.219f, 2.0f, 0.767f, -1.577f, -3.0f, -3.0f, -1.779f, -0.676f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.111f, 0.033f, 0.0f };
    static readonly float[] MS1_SF_ROT = { 15.0f, 14.085f, 12.143f, 10.379f, 10.0f, 12.91f, 17.566f, 20.0f, 20.0f, 15.0f, 11.562f, 10.0f, 12.434f, 17.09f, 20.0f, 20.0f, 18.084f, 16.171f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 14.814f, 14.944f, 15.0f };
    static readonly float[] MS1_FF_ROT = { 20.0f, 19.085f, 17.143f, 15.379f, 15.0f, 17.91f, 22.566f, 25.0f, 25.0f, 20.0f, 16.562f, 15.0f, 17.434f, 22.09f, 25.0f, 25.0f, 23.084f, 21.171f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 19.814f, 19.944f, 20.0f };
    static readonly float[] MS1_HF_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] MS1_SB_ROT = { 165.0f, 170.871f, 183.571f, 195.737f, 200.0f, 186.085f, 162.487f, 150.0f, 150.0f, 175.0f, 192.188f, 200.0f, 187.989f, 164.868f, 150.0f, 150.0f, 157.175f, 165.304f, 170.0f, 170.0f, 170.0f, 170.0f, 170.0f, 166.548f, 165.444f, 165.0f };
    static readonly float[] MS1_FB_ROT = { 75.0f, 72.522f, 67.143f, 61.942f, 60.0f, 65.503f, 74.974f, 80.0f, 80.0f, 70.0f, 63.125f, 60.0f, 64.709f, 73.862f, 80.0f, 80.0f, 78.799f, 76.439f, 75.0f, 75.0f, 75.0f, 75.0f, 75.0f, 74.814f, 74.944f, 75.0f };
    static readonly float[] MS1_HB_ROT = { 0.0f, -1.964f, -6.071f, -9.643f, -10.0f, -2.566f, 8.995f, 15.0f, 15.0f, 2.5f, -6.094f, -10.0f, -3.836f, 7.884f, 15.0f, 15.0f, 8.896f, 3.379f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -0.557f, -0.167f, 0.0f };
    static readonly float[] MS1_THIGH_F_ROT = { 0.0f, -0.134f, -0.357f, -0.402f, 0.0f, 1.614f, 3.862f, 5.0f, 5.0f, 2.5f, 0.781f, 0.0f, 1.296f, 3.704f, 5.0f, 5.0f, 2.727f, 1.037f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -0.186f, -0.056f, 0.0f };
    static readonly float[] MS1_CALF_F_ROT = { 0.0f, 0.214f, 0.571f, 0.643f, 0.0f, -2.582f, -6.18f, -8.0f, -8.0f, -4.0f, -1.25f, 0.0f, -2.074f, -5.926f, -8.0f, -8.0f, -4.364f, -1.659f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.297f, 0.089f, 0.0f };
    static readonly float[] MS1_FOOT_F_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] MS1_THIGH_B_ROT = { 0.0f, 0.134f, 0.357f, 0.402f, 0.0f, -1.614f, -3.862f, -5.0f, -5.0f, -2.5f, -0.781f, 0.0f, -1.296f, -3.704f, -5.0f, -5.0f, -2.727f, -1.037f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.186f, 0.056f, 0.0f };
    static readonly float[] MS1_CALF_B_ROT = { 0.0f, 0.214f, 0.571f, 0.643f, 0.0f, -2.582f, -6.18f, -8.0f, -8.0f, -4.0f, -1.25f, 0.0f, -2.074f, -5.926f, -8.0f, -8.0f, -4.364f, -1.659f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.297f, 0.089f, 0.0f };
    static readonly float[] MS1_FOOT_B_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] MS2_BODY_Y = { 0.0f, -0.007f, -0.021f, -0.035f, -0.04f, -0.025f, 0.002f, 0.02f, 0.018f, 0.007f, 0.0f, 0.005f, 0.015f, 0.02f, 0.015f, 0.005f, 0.0f, 0.003f, 0.01f, 0.017f, 0.02f, 0.019f, 0.016f, 0.012f, 0.008f, 0.004f, 0.001f, 0.0f };
    static readonly float[] MS2_BODY_ROT = { 0.0f, -1.045f, -3.286f, -5.384f, -6.0f, -3.524f, 0.794f, 4.0f, 4.741f, 4.37f, 4.0f, 4.0f, 4.0f, 4.0f, 4.0f, 4.0f, 4.0f, 4.068f, 4.182f, 4.205f, 4.0f, 3.511f, 2.836f, 2.069f, 1.307f, 0.645f, 0.177f, 0.0f };
    static readonly float[] MS2_BODY_X = { 0.0f, -0.008f, -0.024f, -0.035f, -0.03f, 0.011f, 0.074f, 0.12f, 0.131f, 0.126f, 0.12f, 0.12f, 0.12f, 0.12f, 0.12f, 0.12f, 0.12f, 0.122f, 0.125f, 0.126f, 0.12f, 0.105f, 0.085f, 0.062f, 0.039f, 0.019f, 0.005f, 0.0f };
    static readonly float[] MS2_HEAD_ROT = { 0.0f, -0.339f, -1.071f, -1.768f, -2.0f, -1.27f, 0.032f, 1.0f, 1.222f, 1.111f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.017f, 1.045f, 1.051f, 1.0f, 0.878f, 0.709f, 0.517f, 0.327f, 0.161f, 0.044f, 0.0f };
    static readonly float[] MS2_BOT_ROT = { 0.0f, 0.705f, 2.214f, 3.616f, 4.0f, 2.254f, -0.762f, -3.0f, -3.519f, -3.259f, -3.0f, -3.0f, -3.0f, -3.0f, -3.0f, -3.0f, -3.0f, -3.051f, -3.136f, -3.153f, -3.0f, -2.633f, -2.127f, -1.552f, -0.98f, -0.483f, -0.133f, 0.0f };
    static readonly float[] MS2_SF_ROT = { 15.0f, 16.696f, 20.357f, 23.839f, 25.0f, 21.349f, 14.841f, 10.0f, 8.889f, 9.444f, 10.0f, 10.0f, 10.0f, 10.0f, 10.0f, 10.0f, 10.0f, 9.915f, 9.773f, 9.744f, 10.0f, 10.611f, 11.455f, 12.413f, 13.366f, 14.194f, 14.779f, 15.0f };
    static readonly float[] MS2_FF_ROT = { 20.0f, 21.384f, 24.357f, 27.152f, 28.0f, 24.794f, 19.175f, 15.0f, 14.037f, 14.519f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 15.0f, 14.915f, 14.773f, 14.744f, 15.0f, 15.611f, 16.455f, 17.413f, 18.366f, 19.194f, 19.779f, 20.0f };
    static readonly float[] MS2_HF_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] MS2_SB_ROT = { 165.0f, 159.018f, 145.714f, 132.054f, 125.0f, 131.865f, 146.349f, 155.0f, 130.0f, 142.5f, 155.0f, 130.0f, 142.5f, 155.0f, 130.0f, 142.5f, 155.0f, 142.5f, 130.0f, 141.528f, 155.0f, 158.411f, 160.951f, 162.745f, 163.921f, 164.605f, 164.922f, 165.0f };
    static readonly float[] MS2_FB_ROT = { 75.0f, 80.0f, 90.0f, 97.5f, 95.0f, 69.167f, 30.0f, 5.0f, 60.0f, 32.5f, 5.0f, 60.0f, 32.5f, 5.0f, 60.0f, 32.5f, 5.0f, 32.5f, 60.0f, 32.083f, 5.0f, 10.102f, 20.578f, 34.184f, 48.673f, 61.803f, 71.327f, 75.0f };
    static readonly float[] MS2_HB_ROT = { 0.0f, -1.777f, -5.571f, -9.08f, -10.0f, -5.492f, 2.254f, 8.0f, 9.333f, 8.667f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.0f, 8.136f, 8.364f, 8.409f, 8.0f, 7.023f, 5.672f, 4.139f, 2.614f, 1.289f, 0.354f, 0.0f };
    static readonly float[] MS2_THIGH_F_ROT = { 0.0f, -1.732f, -5.286f, -8.196f, -8.0f, -1.079f, 9.905f, 18.0f, 19.926f, 18.963f, 18.0f, 18.0f, 18.0f, 18.0f, 18.0f, 18.0f, 18.0f, 18.307f, 18.818f, 18.92f, 18.0f, 15.801f, 12.762f, 9.312f, 5.882f, 2.901f, 0.797f, 0.0f };
    static readonly float[] MS2_CALF_F_ROT = { 0.0f, -0.513f, -1.786f, -3.415f, -5.0f, -6.746f, -8.651f, -10.0f, -10.37f, -10.185f, -10.0f, -10.0f, -10.0f, -10.0f, -10.0f, -10.0f, -10.0f, -10.17f, -10.455f, -10.511f, -10.0f, -8.778f, -7.09f, -5.174f, -3.268f, -1.611f, -0.443f, 0.0f };
    static readonly float[] MS2_FOOT_F_ROT = { 0.0f, -0.134f, -0.357f, -0.402f, 0.0f, 1.429f, 3.492f, 5.0f, 5.37f, 5.185f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.085f, 5.227f, 5.256f, 5.0f, 4.389f, 3.545f, 2.587f, 1.634f, 0.806f, 0.221f, 0.0f };
    static readonly float[] MS2_THIGH_B_ROT = { 0.0f, -1.754f, -5.929f, -10.888f, -15.0f, -17.952f, -20.365f, -22.0f, -22.519f, -22.259f, -22.0f, -22.0f, -22.0f, -22.0f, -22.0f, -22.0f, -22.0f, -22.375f, -23.0f, -23.125f, -22.0f, -19.312f, -15.598f, -11.382f, -7.19f, -3.545f, -0.974f, 0.0f };
    static readonly float[] MS2_CALF_B_ROT = { 0.0f, -1.116f, -3.643f, -6.348f, -8.0f, -7.651f, -6.159f, -5.0f, -4.778f, -4.889f, -5.0f, -5.0f, -5.0f, -5.0f, -5.0f, -5.0f, -5.0f, -5.085f, -5.227f, -5.256f, -5.0f, -4.389f, -3.545f, -2.587f, -1.634f, -0.806f, -0.221f, 0.0f };
    static readonly float[] MS2_FOOT_B_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] MS3_BODY_Y = { 0.0f, 0.005f, 0.019f, 0.038f, 0.059f, 0.079f, 0.095f, 0.103f, 0.1f, 0.1f, 0.022f, -0.038f, -0.091f, -0.12f, -0.12f, -0.12f, -0.07f, -0.04f, -0.02f, -0.02f, -0.02f, -0.02f, -0.02f, 0.0f };
    static readonly float[] MS3_BODY_ROT = { 0.0f, 0.556f, 1.995f, 3.975f, 6.154f, 8.188f, 9.736f, 10.454f, 10.0f, 10.0f, 1.809f, -4.594f, -10.665f, -15.0f, -15.0f, -15.0f, -20.76f, -20.92f, -20.0f, -20.0f, -20.0f, -20.0f, -20.0f, 0.0f };
    static readonly float[] MS3_BODY_X = { 0.0f, -0.003f, -0.012f, -0.024f, -0.037f, -0.048f, -0.055f, -0.057f, -0.05f, -0.05f, 0.017f, 0.067f, 0.115f, 0.15f, 0.15f, 0.15f, 0.205f, 0.208f, 0.2f, 0.2f, 0.2f, 0.2f, 0.2f, 0.0f };
    static readonly float[] MS3_HEAD_ROT = { 0.0f, 0.214f, 0.769f, 1.536f, 2.385f, 3.185f, 3.808f, 4.123f, 4.0f, 4.0f, 0.891f, -1.513f, -3.678f, -5.0f, -5.0f, -5.0f, -4.4f, -3.64f, -3.0f, -3.0f, -3.0f, -3.0f, -3.0f, 0.0f };
    static readonly float[] MS3_BOT_ROT = { 0.0f, -0.325f, -1.168f, -2.331f, -3.615f, -4.823f, -5.755f, -6.213f, -6.0f, -6.0f, -1.157f, 2.575f, 5.938f, 8.0f, 8.0f, 8.0f, 7.16f, 6.0f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 0.0f };
    static readonly float[] MS3_SF_ROT = { 15.0f, 13.37f, 9.099f, 3.114f, -3.654f, -10.278f, -15.829f, -19.38f, -20.0f, -20.0f, -5.409f, 7.194f, 19.865f, 30.0f, 30.0f, 30.0f, 54.36f, 59.12f, 60.0f, 60.0f, 60.0f, 60.0f, 60.0f, 15.0f };
    static readonly float[] MS3_FF_ROT = { 20.0f, 19.612f, 18.582f, 17.106f, 15.385f, 13.615f, 11.995f, 10.724f, 10.0f, 10.0f, 11.003f, 12.335f, 13.778f, 15.0f, 15.0f, 15.0f, 18.36f, 19.32f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f };
    static readonly float[] MS3_HF_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] MS3_SB_ROT = { 165.0f, 168.247f, 176.623f, 188.081f, 200.577f, 212.063f, 220.493f, 223.821f, 220.0f, 220.0f, 161.225f, 117.483f, 79.828f, 60.0f, 60.0f, 60.0f, 107.4f, 132.8f, 150.0f, 150.0f, 150.0f, 150.0f, 150.0f, 165.0f };
    static readonly float[] MS3_FB_ROT = { 75.0f, 72.595f, 66.262f, 57.327f, 47.115f, 36.952f, 28.161f, 22.069f, 20.0f, 20.0f, 42.117f, 60.145f, 74.782f, 80.0f, 80.0f, 80.0f, 31.04f, 12.88f, 5.0f, 5.0f, 5.0f, 5.0f, 5.0f, 75.0f };
    static readonly float[] MS3_HB_ROT = { 0.0f, -0.729f, -2.632f, -5.287f, -8.269f, -11.155f, -13.522f, -14.944f, -15.0f, -15.0f, -6.846f, -0.231f, 5.923f, 10.0f, 10.0f, 10.0f, 11.92f, 11.04f, 10.0f, 10.0f, 10.0f, 10.0f, 10.0f, 0.0f };
    static readonly float[] MS3_THIGH_F_ROT = { 0.0f, -0.855f, -3.065f, -6.098f, -9.423f, -12.508f, -14.82f, -15.828f, -15.0f, -15.0f, -1.215f, 9.323f, 18.908f, 25.0f, 25.0f, 25.0f, 25.24f, 22.68f, 20.0f, 20.0f, 20.0f, 20.0f, 20.0f, 0.0f };
    static readonly float[] MS3_CALF_F_ROT = { 0.0f, -0.091f, -0.385f, -0.909f, -1.692f, -2.764f, -4.154f, -5.889f, -8.0f, -8.0f, -17.07f, -22.86f, -27.633f, -30.0f, -30.0f, -30.0f, -22.776f, -18.592f, -15.0f, -15.0f, -15.0f, -15.0f, -15.0f, 0.0f };
    static readonly float[] MS3_FOOT_F_ROT = { 0.0f, -0.067f, -0.231f, -0.433f, -0.615f, -0.721f, -0.692f, -0.471f, 0.0f, 0.0f, 3.259f, 5.479f, 7.266f, 8.0f, 8.0f, 8.0f, 3.392f, 1.344f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };
    static readonly float[] MS3_THIGH_B_ROT = { 0.0f, 0.64f, 2.284f, 4.516f, 6.923f, 9.09f, 10.601f, 11.043f, 10.0f, 10.0f, -1.785f, -10.723f, -19.108f, -25.0f, -25.0f, -25.0f, -31.96f, -31.72f, -30.0f, -30.0f, -30.0f, -30.0f, -30.0f, 0.0f };
    static readonly float[] MS3_CALF_B_ROT = { 0.0f, -0.089f, -0.349f, -0.771f, -1.346f, -2.066f, -2.921f, -3.902f, -5.0f, -5.0f, -7.671f, -9.514f, -11.905f, -15.0f, -15.0f, -15.0f, -33.96f, -38.92f, -40.0f, -40.0f, -40.0f, -40.0f, -40.0f, 0.0f };
    static readonly float[] MS3_FOOT_B_ROT = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -0.48f, -0.72f, -0.64f, 0.0f, 0.0f, 0.0f, 6.96f, 9.12f, 10.0f, 10.0f, 10.0f, 10.0f, 10.0f, 0.0f };


    // ================================================================
    // Generate Idle (48프레임, 루프)
    // ================================================================
    [MenuItem("Tools/MidBoss2/Generate Idle")]
    public static void GenerateIdle()
    {
        GenerateIdleVariant("MidBoss2_Idle", IDLE_SB_ROT, IDLE_FB_ROT, IDLE_HB_ROT);
    }

    [MenuItem("Tools/MidBoss2/Generate Idle2")]
    public static void GenerateIdle2()
    {
        GenerateIdleVariant("MidBoss2_Idle2", IDLE2_SB_ROT, IDLE2_FB_ROT, IDLE2_HB_ROT);
    }

    static void GenerateIdleVariant(string clipName, float[] sbRot, float[] fbRot, float[] hbRot)
    {
        var clip = MakeClip(clipName, 48, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 48, FRAME_RATE, loop: true);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, 48 / FRAME_RATE, val));

        C(BODY, "localPosition.y", IDLE_BODY_Y); L(BODY, "localPosition.x", 0f); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", IDLE_BODY_ROT);
        L(HEAD, "localPosition.x", -0.129f); L(HEAD, "localPosition.y", 1.001f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", IDLE_HEAD_ROT);

        L(SF, "localPosition.x", -0.895f); C(SF, "localPosition.y", IDLE_SF_Y); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", IDLE_SF_ROT);
        L(FF, "localPosition.x", -0.344f); L(FF, "localPosition.y", -0.588f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", IDLE_FF_ROT);
        L(HF, "localPosition.x", 0.058f); L(HF, "localPosition.y", -0.426f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", IDLE_HF_ROT);

        L(SB, "localPosition.x", 0.371f); L(SB, "localPosition.y", 0.560f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", sbRot);
        L(FB, "localPosition.x", 0.203f); L(FB, "localPosition.y", -0.306f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", fbRot);
        L(HB, "localPosition.x", 0.152f); L(HB, "localPosition.y", -0.395f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", hbRot);
        L(WEAPON, "localPosition.x", -0.007f); L(WEAPON, "localPosition.y", -0.269f); L(WEAPON, "localPosition.z", 0f);
        L(WEAPON, "localEulerAngles.z", -60f);

        L(BOTTOM, "localPosition.x", -0.098f); L(BOTTOM, "localPosition.y", -0.049f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", IDLE_BOT_ROT);

        L(THIGH_F, "localPosition.x", -0.442f); C(THIGH_F, "localPosition.y", IDLE_THIGH_F_Y); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", IDLE_THIGH_F_ROT);
        L(CALF_F, "localPosition.x", 0.009f); L(CALF_F, "localPosition.y", -0.758f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", IDLE_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.160f); L(FOOT_F, "localPosition.y", -0.504f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", IDLE_FOOT_F_ROT);

        L(THIGH_B, "localPosition.x", 0.120f); C(THIGH_B, "localPosition.y", IDLE_THIGH_B_Y); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", IDLE_THIGH_B_ROT);
        L(CALF_B, "localPosition.x", 0.275f); L(CALF_B, "localPosition.y", -0.627f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", IDLE_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.160f); L(FOOT_B, "localPosition.y", -0.488f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", IDLE_FOOT_B_ROT);

        Save(clip, clipName);
        GenerateAC();
    }

    // ================================================================
    // Generate Walk (32프레임, 루프)
    // ================================================================
    [MenuItem("Tools/MidBoss2/Generate Walk")]
    public static void GenerateWalk()
    {
        var clip = MakeClip("MidBoss2_Walk", 32, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 32, FRAME_RATE, loop: true);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, 32 / FRAME_RATE, val));

        C(BODY, "localPosition.y", WALK_BODY_Y); L(BODY, "localPosition.x", 0f); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", WALK_BODY_ROT);
        L(HEAD, "localPosition.x", -0.129f); L(HEAD, "localPosition.y", 1.001f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);

        L(SF, "localPosition.x", -0.895f); L(SF, "localPosition.y", 0.794f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", WALK_SF_ROT);
        L(FF, "localPosition.x", -0.344f); L(FF, "localPosition.y", -0.588f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", WALK_FF_ROT);
        L(HF, "localPosition.x", 0.058f); L(HF, "localPosition.y", -0.426f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        L(SB, "localPosition.x", 0.371f); L(SB, "localPosition.y", 0.560f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", WALK_SB_ROT);
        L(FB, "localPosition.x", 0.203f); L(FB, "localPosition.y", -0.306f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", WALK_FB_ROT);
        L(HB, "localPosition.x", 0.152f); L(HB, "localPosition.y", -0.395f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(WEAPON, "localPosition.x", -0.007f); L(WEAPON, "localPosition.y", -0.269f); L(WEAPON, "localPosition.z", 0f);
        L(WEAPON, "localEulerAngles.z", -60f);

        L(BOTTOM, "localPosition.x", -0.098f); L(BOTTOM, "localPosition.y", -0.049f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", WALK_BOT_ROT);

        L(THIGH_F, "localPosition.x", -0.442f); L(THIGH_F, "localPosition.y", -0.178f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", WALK_THIGH_F_ROT);
        L(CALF_F, "localPosition.x", 0.009f); L(CALF_F, "localPosition.y", -0.758f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", WALK_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.160f); L(FOOT_F, "localPosition.y", -0.504f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", WALK_FOOT_F_ROT);

        L(THIGH_B, "localPosition.x", 0.120f); L(THIGH_B, "localPosition.y", -0.309f); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", WALK_THIGH_B_ROT);
        L(CALF_B, "localPosition.x", 0.275f); L(CALF_B, "localPosition.y", -0.627f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", WALK_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.160f); L(FOOT_B, "localPosition.y", -0.488f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", WALK_FOOT_B_ROT);

        Save(clip, "MidBoss2_Walk");
        GenerateAC();
    }

    // ================================================================
    // Generate Run (20프레임, 루프)
    // ================================================================
    [MenuItem("Tools/MidBoss2/Generate Run")]
    public static void GenerateRun()
    {
        var clip = MakeClip("MidBoss2_Run", 20, true);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 20, FRAME_RATE, loop: true);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, 20 / FRAME_RATE, val));

        C(BODY, "localPosition.y", RUN_BODY_Y); L(BODY, "localPosition.x", 0f); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", RUN_BODY_ROT);
        L(HEAD, "localPosition.x", -0.129f); L(HEAD, "localPosition.y", 1.001f); L(HEAD, "localPosition.z", 0f);
        L(HEAD, "localEulerAngles.z", 0f);

        L(SF, "localPosition.x", -0.895f); L(SF, "localPosition.y", 0.794f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", RUN_SF_ROT);
        L(FF, "localPosition.x", -0.344f); L(FF, "localPosition.y", -0.588f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", RUN_FF_ROT);
        L(HF, "localPosition.x", 0.058f); L(HF, "localPosition.y", -0.426f); L(HF, "localPosition.z", 0f);
        L(HF, "localEulerAngles.z", 0f);

        L(SB, "localPosition.x", 0.371f); L(SB, "localPosition.y", 0.560f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", RUN_SB_ROT);
        L(FB, "localPosition.x", 0.203f); L(FB, "localPosition.y", -0.306f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", RUN_FB_ROT);
        L(HB, "localPosition.x", 0.152f); L(HB, "localPosition.y", -0.395f); L(HB, "localPosition.z", 0f);
        L(HB, "localEulerAngles.z", 0f);
        L(WEAPON, "localPosition.x", -0.007f); L(WEAPON, "localPosition.y", -0.269f); L(WEAPON, "localPosition.z", 0f);
        L(WEAPON, "localEulerAngles.z", -60f);

        L(BOTTOM, "localPosition.x", -0.098f); L(BOTTOM, "localPosition.y", -0.049f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", RUN_BOT_ROT);

        L(THIGH_F, "localPosition.x", -0.442f); L(THIGH_F, "localPosition.y", -0.178f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", RUN_THIGH_F_ROT);
        L(CALF_F, "localPosition.x", 0.009f); L(CALF_F, "localPosition.y", -0.758f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", RUN_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.160f); L(FOOT_F, "localPosition.y", -0.504f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", RUN_FOOT_F_ROT);

        L(THIGH_B, "localPosition.x", 0.120f); L(THIGH_B, "localPosition.y", -0.309f); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", RUN_THIGH_B_ROT);
        L(CALF_B, "localPosition.x", 0.275f); L(CALF_B, "localPosition.y", -0.627f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", RUN_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.160f); L(FOOT_B, "localPosition.y", -0.488f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", RUN_FOOT_B_ROT);

        Save(clip, "MidBoss2_Run");
        GenerateAC();
    }


    // ================================================================
    // Generate Attack (일반공격: 찌르기 -> 뒤로 젖혀 넉백 스윙), 24프레임 논루프
    // ================================================================
    [MenuItem("Tools/MidBoss2/Generate Attack")]
    public static void GenerateAttack()
    {
        const float DUR = 24 / FRAME_RATE;
        var clip = MakeClip("MidBoss2_Attack", 24, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 24, FRAME_RATE, loop: false);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", ATK_BODY_Y); C(BODY, "localPosition.x", ATK_BODY_X); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", ATK_BODY_ROT);
        L(HEAD, "localPosition.x", -0.129f); L(HEAD, "localPosition.y", 1.001f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", ATK_HEAD_ROT);

        L(SF, "localPosition.x", -0.895f); L(SF, "localPosition.y", 0.794f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", ATK_SF_ROT);
        L(FF, "localPosition.x", -0.344f); L(FF, "localPosition.y", -0.588f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", ATK_FF_ROT);
        L(HF, "localPosition.x", 0.058f); L(HF, "localPosition.y", -0.426f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", ATK_HF_ROT);

        L(SB, "localPosition.x", 0.371f); L(SB, "localPosition.y", 0.560f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", ATK_SB_ROT);
        L(FB, "localPosition.x", 0.203f); L(FB, "localPosition.y", -0.306f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", ATK_FB_ROT);
        L(HB, "localPosition.x", 0.152f); L(HB, "localPosition.y", -0.395f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", ATK_HB_ROT);
        L(WEAPON, "localPosition.x", -0.007f); L(WEAPON, "localPosition.y", -0.269f); L(WEAPON, "localPosition.z", 0f);
        L(WEAPON, "localEulerAngles.z", -60f);

        L(BOTTOM, "localPosition.x", -0.098f); L(BOTTOM, "localPosition.y", -0.049f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", ATK_BOT_ROT);

        L(THIGH_F, "localPosition.x", -0.442f); L(THIGH_F, "localPosition.y", -0.178f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", ATK_THIGH_F_ROT);
        L(CALF_F, "localPosition.x", 0.009f); L(CALF_F, "localPosition.y", -0.758f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", ATK_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.160f); L(FOOT_F, "localPosition.y", -0.504f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", ATK_FOOT_F_ROT);

        L(THIGH_B, "localPosition.x", 0.120f); L(THIGH_B, "localPosition.y", -0.309f); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", ATK_THIGH_B_ROT);
        L(CALF_B, "localPosition.x", 0.275f); L(CALF_B, "localPosition.y", -0.627f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", ATK_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.160f); L(FOOT_B, "localPosition.y", -0.488f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", ATK_FOOT_B_ROT);

        Save(clip, "MidBoss2_Attack");
        GenerateAC();
    }

    // ================================================================
    // Generate Skill1 (버프: 창으로 바닥 두 번 찍기), 26프레임 논루프
    // ================================================================
    [MenuItem("Tools/MidBoss2/Generate Skill1")]
    public static void GenerateSkill1()
    {
        const float DUR = 26 / FRAME_RATE;
        var clip = MakeClip("MidBoss2_Skill1", 26, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 26, FRAME_RATE, loop: false);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", MS1_BODY_Y); C(BODY, "localPosition.x", MS1_BODY_X); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", MS1_BODY_ROT);
        L(HEAD, "localPosition.x", -0.129f); L(HEAD, "localPosition.y", 1.001f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", MS1_HEAD_ROT);

        L(SF, "localPosition.x", -0.895f); L(SF, "localPosition.y", 0.794f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", MS1_SF_ROT);
        L(FF, "localPosition.x", -0.344f); L(FF, "localPosition.y", -0.588f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", MS1_FF_ROT);
        L(HF, "localPosition.x", 0.058f); L(HF, "localPosition.y", -0.426f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", MS1_HF_ROT);

        L(SB, "localPosition.x", 0.371f); L(SB, "localPosition.y", 0.560f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", MS1_SB_ROT);
        L(FB, "localPosition.x", 0.203f); L(FB, "localPosition.y", -0.306f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", MS1_FB_ROT);
        L(HB, "localPosition.x", 0.152f); L(HB, "localPosition.y", -0.395f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", MS1_HB_ROT);
        L(WEAPON, "localPosition.x", -0.007f); L(WEAPON, "localPosition.y", -0.269f); L(WEAPON, "localPosition.z", 0f);
        L(WEAPON, "localEulerAngles.z", -60f);

        L(BOTTOM, "localPosition.x", -0.098f); L(BOTTOM, "localPosition.y", -0.049f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", MS1_BOT_ROT);

        L(THIGH_F, "localPosition.x", -0.442f); L(THIGH_F, "localPosition.y", -0.178f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", MS1_THIGH_F_ROT);
        L(CALF_F, "localPosition.x", 0.009f); L(CALF_F, "localPosition.y", -0.758f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", MS1_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.160f); L(FOOT_F, "localPosition.y", -0.504f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", MS1_FOOT_F_ROT);

        L(THIGH_B, "localPosition.x", 0.120f); L(THIGH_B, "localPosition.y", -0.309f); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", MS1_THIGH_B_ROT);
        L(CALF_B, "localPosition.x", 0.275f); L(CALF_B, "localPosition.y", -0.627f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", MS1_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.160f); L(FOOT_B, "localPosition.y", -0.488f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", MS1_FOOT_B_ROT);

        Save(clip, "MidBoss2_Skill1");
        GenerateAC();
    }

    // ================================================================
    // Generate Skill2 (빠르게 5연속 찌르기), 28프레임 논루프
    // ================================================================
    [MenuItem("Tools/MidBoss2/Generate Skill2")]
    public static void GenerateSkill2()
    {
        const float DUR = 28 / FRAME_RATE;
        var clip = MakeClip("MidBoss2_Skill2", 28, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 28, FRAME_RATE, loop: false);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", MS2_BODY_Y); C(BODY, "localPosition.x", MS2_BODY_X); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", MS2_BODY_ROT);
        L(HEAD, "localPosition.x", -0.129f); L(HEAD, "localPosition.y", 1.001f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", MS2_HEAD_ROT);

        L(SF, "localPosition.x", -0.895f); L(SF, "localPosition.y", 0.794f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", MS2_SF_ROT);
        L(FF, "localPosition.x", -0.344f); L(FF, "localPosition.y", -0.588f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", MS2_FF_ROT);
        L(HF, "localPosition.x", 0.058f); L(HF, "localPosition.y", -0.426f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", MS2_HF_ROT);

        L(SB, "localPosition.x", 0.371f); L(SB, "localPosition.y", 0.560f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", MS2_SB_ROT);
        L(FB, "localPosition.x", 0.203f); L(FB, "localPosition.y", -0.306f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", MS2_FB_ROT);
        L(HB, "localPosition.x", 0.152f); L(HB, "localPosition.y", -0.395f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", MS2_HB_ROT);
        L(WEAPON, "localPosition.x", -0.007f); L(WEAPON, "localPosition.y", -0.269f); L(WEAPON, "localPosition.z", 0f);
        L(WEAPON, "localEulerAngles.z", -60f);

        L(BOTTOM, "localPosition.x", -0.098f); L(BOTTOM, "localPosition.y", -0.049f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", MS2_BOT_ROT);

        L(THIGH_F, "localPosition.x", -0.442f); L(THIGH_F, "localPosition.y", -0.178f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", MS2_THIGH_F_ROT);
        L(CALF_F, "localPosition.x", 0.009f); L(CALF_F, "localPosition.y", -0.758f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", MS2_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.160f); L(FOOT_F, "localPosition.y", -0.504f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", MS2_FOOT_F_ROT);

        L(THIGH_B, "localPosition.x", 0.120f); L(THIGH_B, "localPosition.y", -0.309f); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", MS2_THIGH_B_ROT);
        L(CALF_B, "localPosition.x", 0.275f); L(CALF_B, "localPosition.y", -0.627f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", MS2_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.160f); L(FOOT_B, "localPosition.y", -0.488f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", MS2_FOOT_B_ROT);

        Save(clip, "MidBoss2_Skill2");
        GenerateAC();
    }

    // ================================================================
    // Generate Skill3 (기 끌어와 내리찍기 -> 정면으로 들고 돌진), 24프레임 논루프
    // ================================================================
    [MenuItem("Tools/MidBoss2/Generate Skill3")]
    public static void GenerateSkill3()
    {
        const float DUR = 24 / FRAME_RATE;
        var clip = MakeClip("MidBoss2_Skill3", 24, false);
        void C(string path, string prop, float[] vals) => SetCurve(clip, path, prop, vals, 24, FRAME_RATE, loop: false);
        void L(string path, string prop, float val) =>
            clip.SetCurve(path, typeof(Transform), prop, AnimationCurve.Linear(0f, val, DUR, val));

        C(BODY, "localPosition.y", MS3_BODY_Y); C(BODY, "localPosition.x", MS3_BODY_X); L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", MS3_BODY_ROT);
        L(HEAD, "localPosition.x", -0.129f); L(HEAD, "localPosition.y", 1.001f); L(HEAD, "localPosition.z", 0f);
        C(HEAD, "localEulerAngles.z", MS3_HEAD_ROT);

        L(SF, "localPosition.x", -0.895f); L(SF, "localPosition.y", 0.794f); L(SF, "localPosition.z", 0f);
        C(SF, "localEulerAngles.z", MS3_SF_ROT);
        L(FF, "localPosition.x", -0.344f); L(FF, "localPosition.y", -0.588f); L(FF, "localPosition.z", 0f);
        C(FF, "localEulerAngles.z", MS3_FF_ROT);
        L(HF, "localPosition.x", 0.058f); L(HF, "localPosition.y", -0.426f); L(HF, "localPosition.z", 0f);
        C(HF, "localEulerAngles.z", MS3_HF_ROT);

        L(SB, "localPosition.x", 0.371f); L(SB, "localPosition.y", 0.560f); L(SB, "localPosition.z", 0f);
        C(SB, "localEulerAngles.z", MS3_SB_ROT);
        L(FB, "localPosition.x", 0.203f); L(FB, "localPosition.y", -0.306f); L(FB, "localPosition.z", 0f);
        C(FB, "localEulerAngles.z", MS3_FB_ROT);
        L(HB, "localPosition.x", 0.152f); L(HB, "localPosition.y", -0.395f); L(HB, "localPosition.z", 0f);
        C(HB, "localEulerAngles.z", MS3_HB_ROT);
        L(WEAPON, "localPosition.x", -0.007f); L(WEAPON, "localPosition.y", -0.269f); L(WEAPON, "localPosition.z", 0f);
        L(WEAPON, "localEulerAngles.z", -60f);

        L(BOTTOM, "localPosition.x", -0.098f); L(BOTTOM, "localPosition.y", -0.049f); L(BOTTOM, "localPosition.z", 0f);
        C(BOTTOM, "localEulerAngles.z", MS3_BOT_ROT);

        L(THIGH_F, "localPosition.x", -0.442f); L(THIGH_F, "localPosition.y", -0.178f); L(THIGH_F, "localPosition.z", 0f);
        C(THIGH_F, "localEulerAngles.z", MS3_THIGH_F_ROT);
        L(CALF_F, "localPosition.x", 0.009f); L(CALF_F, "localPosition.y", -0.758f); L(CALF_F, "localPosition.z", 0f);
        C(CALF_F, "localEulerAngles.z", MS3_CALF_F_ROT);
        L(FOOT_F, "localPosition.x", -0.160f); L(FOOT_F, "localPosition.y", -0.504f); L(FOOT_F, "localPosition.z", 0f);
        C(FOOT_F, "localEulerAngles.z", MS3_FOOT_F_ROT);

        L(THIGH_B, "localPosition.x", 0.120f); L(THIGH_B, "localPosition.y", -0.309f); L(THIGH_B, "localPosition.z", 0f);
        C(THIGH_B, "localEulerAngles.z", MS3_THIGH_B_ROT);
        L(CALF_B, "localPosition.x", 0.275f); L(CALF_B, "localPosition.y", -0.627f); L(CALF_B, "localPosition.z", 0f);
        C(CALF_B, "localEulerAngles.z", MS3_CALF_B_ROT);
        L(FOOT_B, "localPosition.x", -0.160f); L(FOOT_B, "localPosition.y", -0.488f); L(FOOT_B, "localPosition.z", 0f);
        C(FOOT_B, "localEulerAngles.z", MS3_FOOT_B_ROT);

        Save(clip, "MidBoss2_Skill3");
        GenerateAC();
    }


    // ================================================================
    // Generate All
    // ================================================================
    [MenuItem("Tools/MidBoss2/Generate All")]
    public static void GenerateAll()
    {
        GenerateIdle();
        GenerateIdle2();
        GenerateWalk();
        GenerateRun();
        GenerateAttack();
        GenerateSkill1();
        GenerateSkill2();
        GenerateSkill3();
        GenerateAC();
        Debug.Log("[MidBoss2] 전체 생성 완료");
    }

    // ================================================================
    // 헬퍼 함수들
    // ================================================================
    static AnimationClip MakeClip(string name, int totalFrames, bool loop)
    {
        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");
            if (!AssetDatabase.IsValidFolder("Assets/Animations/2"))
                AssetDatabase.CreateFolder("Assets/Animations", "2");
            AssetDatabase.CreateFolder("Assets/Animations/2", "MidBoss 2");
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

        // 루프 이음매 보정: 마지막->첫 프레임 전환에서 속도가 어긋나 멈칫하는 걸 방지
        if (loop && total >= 3)
        {
            float dt = 2f / rate;
            float seamTangent = (vals[1] - vals[total - 1]) / dt;

            var k0 = curve.keys[0];
            k0.inTangent = seamTangent; k0.outTangent = seamTangent;
            curve.MoveKey(0, k0);
            AnimationUtility.SetKeyLeftTangentMode(curve, 0, AnimationUtility.TangentMode.Free);
            AnimationUtility.SetKeyRightTangentMode(curve, 0, AnimationUtility.TangentMode.Free);

            int last = total - 1;
            var kLast = curve.keys[last];
            kLast.inTangent = seamTangent; kLast.outTangent = seamTangent;
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
        if (existing != null)
        {
            EditorUtility.CopySerialized(clip, existing);
            existing.name = name;
            AssetDatabase.SaveAssets();
        }
        else
        {
            AssetDatabase.CreateAsset(clip, assetPath);
            AssetDatabase.SaveAssets();
        }
        AssetDatabase.Refresh();
        Debug.Log($"[MidBoss2] {name} -> {assetPath}");
    }

    // ================================================================
    // Generate AC (AnimatorController에 클립 자동 등록 — SAVE_PATH 폴더를 스캔해서
    // 새 클립이 생기면 자동으로 상태(state)로 추가됨)
    // ================================================================
    [MenuItem("Tools/MidBoss2/Generate AC")]
    public static void GenerateAC()
    {
        string acPath = $"{SAVE_PATH}/MidBoss2_AC.controller";
        var ac = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(acPath);
        if (ac == null)
        {
            if (!AssetDatabase.IsValidFolder(SAVE_PATH))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                    AssetDatabase.CreateFolder("Assets", "Animations");
                if (!AssetDatabase.IsValidFolder("Assets/Animations/2"))
                    AssetDatabase.CreateFolder("Assets/Animations", "2");
                AssetDatabase.CreateFolder("Assets/Animations/2", "MidBoss 2");
            }
            ac = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(acPath);
        }

        var rootSM = ac.layers[0].stateMachine;
        var existingNames = new System.Collections.Generic.HashSet<string>();
        foreach (var s in rootSM.states) existingNames.Add(s.state.name);

        var guids = AssetDatabase.FindAssets("t:AnimationClip", new[] { SAVE_PATH });
        var clipNames = new System.Collections.Generic.List<string>();
        foreach (var guid in guids)
            clipNames.Add(System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)));
        clipNames.Sort();

        foreach (string clipName in clipNames)
        {
            string clipPath = $"{SAVE_PATH}/{clipName}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null) { Debug.LogWarning($"[MidBoss2] 클립 없음: {clipPath}"); continue; }
            if (!existingNames.Contains(clipName))
            { var state = rootSM.AddState(clipName); state.motion = clip; }
            else
            { foreach (var cs in rootSM.states) if (cs.state.name == clipName) { cs.state.motion = clip; break; } }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}