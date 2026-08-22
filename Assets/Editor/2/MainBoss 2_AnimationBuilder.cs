using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Collections.Generic;

public class MainBoss2_AnimGenerators
{
    const string SAVE_PATH = "Assets/Animations/2/MainBoss 2";
    const float FRAME_RATE = 12f;

    const float DUR_16 = 16f / FRAME_RATE;
    const float DUR_24 = 24f / FRAME_RATE;
    const float DUR_32 = 32f / FRAME_RATE;

    // ================================================================
    // Hierarchy
    // ================================================================

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
    // MainBoss 2 Rest 실측값
    // ================================================================

    const float REST_BODY_X = 0f;
    const float REST_BODY_Y = 0f;

    const float REST_HEAD_X = -0.275f;
    const float REST_HEAD_Y = 1.025f;

    const float REST_SF_X = -0.856f;
    const float REST_SF_Y = 0.631f;

    const float REST_FF_X = -0.245f;
    const float REST_FF_Y = -0.562f;

    const float REST_HF_X = -0.093f;
    const float REST_HF_Y = -0.678f;

    const float REST_SB_X = 0.467f;
    const float REST_SB_Y = 0.662f;

    const float REST_FB_X = 0.141f;
    const float REST_FB_Y = -0.506f;

    const float REST_HB_X = 0.026f;
    const float REST_HB_Y = -0.687f;

    const float REST_AXE_X = 0f;
    const float REST_AXE_Y = -0.263f;
    const float REST_AXE_ROT = -90f;

    const float REST_BOTTOM_X = -0.141f;
    const float REST_BOTTOM_Y = 0f;

    const float REST_THIGH_F_X = -0.572f;
    const float REST_THIGH_F_Y = -0.472f;

    const float REST_CALF_F_X = 0.008f;
    const float REST_CALF_F_Y = -0.718f;

    const float REST_FOOT_F_X = -0.265f;
    const float REST_FOOT_F_Y = -0.978f;

    const float REST_THIGH_B_X = 0.394f;
    const float REST_THIGH_B_Y = -0.556f;

    const float REST_CALF_B_X = 0.259f;
    const float REST_CALF_B_Y = -0.737f;

    const float REST_FOOT_B_X = -0.060f;
    const float REST_FOOT_B_Y = -0.875f;


    // ================================================================
    // Foot 바닥 기준
    //
    // Body 기준 Rest 자세에서 Foot Pivot의 Y를 계산한다.
    //
    // Body
    //   └ Bottom
    //      └ Thigh
    //          └ Calf
    //              └ Foot
    //
    // 초기 자세의 Foot Y가 바닥 기준점이다.
    // ================================================================

    const float FOOT_FLOOR_MARGIN = 0.0001f;


    // ================================================================
    // Idle
    // ================================================================

    static readonly float[] IDLE_BODY_Y =
    {
        0f, 0.005f, 0.010f, 0.014f,
        0.017f, 0.019f, 0.020f, 0.019f,
        0.017f, 0.014f, 0.010f, 0.005f,
        0f, -0.005f, -0.010f, -0.014f,
        -0.017f, -0.019f, -0.020f, -0.019f,
        -0.017f, -0.014f, -0.010f, -0.005f
    };


    // ================================================================
    // Walk
    // ================================================================

    static readonly float[] WALK_BODY_Y =
    {
        0f, 0.008f, 0.014f, 0.008f,
        0f, 0.008f, 0.014f, 0.008f,
        0f, 0.008f, 0.014f, 0.008f,
        0f, 0.008f, 0.014f, 0.008f
    };

    static readonly float[] WALK_SF_ROT =
    {
        12f, 8f, 0f, -8f,
        -12f, -8f, 0f, 8f,
        12f, 8f, 0f, -8f,
        -12f, -8f, 0f, 8f
    };

    static readonly float[] WALK_SB_ROT =
    {
        -12f, -8f, 0f, 8f,
        12f, 8f, 0f, -8f,
        -12f, -8f, 0f, 8f,
        12f, 8f, 0f, -8f
    };

    static readonly float[] WALK_THIGH_F_ROT =
    {
        25f, 15f, 0f, -15f,
        -25f, -15f, 0f, 15f,
        25f, 15f, 0f, -15f,
        -25f, -15f, 0f, 15f
    };

    static readonly float[] WALK_THIGH_B_ROT =
    {
        -25f, -15f, 0f, 15f,
        25f, 15f, 0f, -15f,
        -25f, -15f, 0f, 15f,
        25f, 15f, 0f, -15f
    };

    static readonly float[] WALK_CALF_F_ROT =
    {
        8f, 4f, 0f, -4f,
        -8f, -4f, 0f, 4f,
        8f, 4f, 0f, -4f,
        -8f, -4f, 0f, 4f
    };

    static readonly float[] WALK_CALF_B_ROT =
    {
        -8f, -4f, 0f, 4f,
        8f, 4f, 0f, -4f,
        -8f, -4f, 0f, 4f,
        8f, 4f, 0f, -4f
    };


    // ================================================================
    // Run
    // ================================================================

    static readonly float[] RUN_BODY_Y =
    {
        0f, 0.012f, 0.020f, 0.012f,
        0f, 0.012f, 0.020f, 0.012f,
        0f, 0.012f, 0.020f, 0.012f,
        0f, 0.012f, 0.020f, 0.012f
    };

    static readonly float[] RUN_BODY_ROT =
    {
        -6f, -3f, 0f, 3f,
        6f, 3f, 0f, -3f,
        -6f, -3f, 0f, 3f,
        6f, 3f, 0f, -3f
    };

    static readonly float[] RUN_SF_ROT =
    {
        22f, 12f, 0f, -12f,
        -22f, -12f, 0f, 12f,
        22f, 12f, 0f, -12f,
        -22f, -12f, 0f, 12f
    };

    static readonly float[] RUN_SB_ROT =
    {
        -22f, -12f, 0f, 12f,
        22f, 12f, 0f, -12f,
        -22f, -12f, 0f, 12f,
        22f, 12f, 0f, -12f
    };

    static readonly float[] RUN_THIGH_F_ROT =
    {
        40f, 25f, 0f, -25f,
        -40f, -25f, 0f, 25f,
        40f, 25f, 0f, -25f,
        -40f, -25f, 0f, 25f
    };

    static readonly float[] RUN_THIGH_B_ROT =
    {
        -40f, -25f, 0f, 25f,
        40f, 25f, 0f, -25f,
        -40f, -25f, 0f, 25f,
        40f, 25f, 0f, -25f
    };

    static readonly float[] RUN_CALF_F_ROT =
    {
        15f, 8f, 0f, -8f,
        -15f, -8f, 0f, 8f,
        15f, 8f, 0f, -8f,
        -15f, -8f, 0f, 8f
    };

    static readonly float[] RUN_CALF_B_ROT =
    {
        -15f, -8f, 0f, 8f,
        15f, 8f, 0f, -8f,
        -15f, -8f, 0f, 8f,
        15f, 8f, 0f, -8f
    };


    // ================================================================
    // Generate All
    // ================================================================

    [MenuItem("Tools/MainBoss2/Generate All")]
    public static void GenerateAll()
    {
        GenerateIdle();
        GenerateWalk();
        GenerateRun();
        GenerateAC();

        Debug.Log("[MainBoss 2] 전체 애니메이션 생성 완료");
    }


    // ================================================================
    // Idle
    //
    // 핵심:
    // Body가 위아래로 움직이는 만큼
    // Bottom을 반대로 움직여 하체를 고정한다.
    //
    // 즉,
    //
    // Body Y = +0.020
    // Bottom Y = -0.020
    //
    // Body가 움직여도 Bottom 이하의 하체는 제자리를 유지한다.
    // ================================================================

    [MenuItem("Tools/MainBoss2/Generate Idle")]
    public static void GenerateIdle()
    {
        int total = IDLE_BODY_Y.Length;

        var clip = MakeClip(
            "MainBoss2_Idle",
            total,
            true
        );

        void C(string path, string prop, float[] vals) =>
            SetCurve(clip, path, prop, vals, total);

        void L(string path, string prop, float val) =>
            SetConstantCurve(
                clip,
                path,
                prop,
                val,
                total
            );

        // ------------------------------------------------------------
        // Body
        // ------------------------------------------------------------

        L(BODY, "localPosition.x", REST_BODY_X);

        // Body는 기존 Idle 움직임 사용
        C(BODY, "localPosition.y", IDLE_BODY_Y);

        L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);

        // ------------------------------------------------------------
        // 상체
        // ------------------------------------------------------------

        SetRest(L, HEAD);

        SetRest(L, SF);
        SetRest(L, FF);
        SetRest(L, HF);

        SetRest(L, SB);
        SetRest(L, FB);
        SetRest(L, HB);

        // ------------------------------------------------------------
        // 무기
        // ------------------------------------------------------------

        L(AXE, "localPosition.x", REST_AXE_X);
        L(AXE, "localPosition.y", REST_AXE_Y);
        L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", REST_AXE_ROT);

        // ------------------------------------------------------------
        // Bottom
        //
        // Body Y의 반대값을 넣는다.
        // ------------------------------------------------------------

        float[] idleBottomY =
            new float[total];

        for (int i = 0; i < total; i++)
        {
            idleBottomY[i] =
                REST_BOTTOM_Y - IDLE_BODY_Y[i];
        }

        L(BOTTOM, "localPosition.x", REST_BOTTOM_X);
        C(BOTTOM, "localPosition.y", idleBottomY);
        L(BOTTOM, "localPosition.z", 0f);
        L(BOTTOM, "localEulerAngles.z", 0f);

        // ------------------------------------------------------------
        // 하체
        // Bottom이 움직이므로 자식들은 Rest 그대로
        // ------------------------------------------------------------

        SetRest(L, THIGH_F);
        SetRest(L, CALF_F);
        SetRest(L, FOOT_F);

        SetRest(L, THIGH_B);
        SetRest(L, CALF_B);
        SetRest(L, FOOT_B);

        Save(clip, "MainBoss2_Idle");
    }


    // ================================================================
    // Walk
    // ================================================================

    [MenuItem("Tools/MainBoss2/Generate Walk")]
    public static void GenerateWalk()
    {
        int total = WALK_BODY_Y.Length;

        var clip = MakeClip(
            "MainBoss2_Walk",
            total,
            true
        );

        void C(string path, string prop, float[] vals) =>
            SetCurve(clip, path, prop, vals, total);

        void L(string path, string prop, float val) =>
            SetConstantCurve(
                clip,
                path,
                prop,
                val,
                total
            );

        // ------------------------------------------------------------
        // 상체
        // ------------------------------------------------------------

        L(BODY, "localPosition.x", REST_BODY_X);

        // Foot 바닥 보정이 들어간 Body Y를 계산한다.
        float[] walkBodyY =
            BuildGroundedBodyY(
                WALK_BODY_Y,
                WALK_THIGH_F_ROT,
                WALK_CALF_F_ROT,
                WALK_THIGH_B_ROT,
                WALK_CALF_B_ROT
            );

        C(BODY, "localPosition.y", walkBodyY);

        L(BODY, "localPosition.z", 0f);
        L(BODY, "localEulerAngles.z", 0f);

        SetRest(L, HEAD);

        SetRest(L, SF);
        SetRest(L, FF);
        SetRest(L, HF);

        SetRest(L, SB);
        SetRest(L, FB);
        SetRest(L, HB);

        // ------------------------------------------------------------
        // Axe
        // ------------------------------------------------------------

        L(AXE, "localPosition.x", REST_AXE_X);
        L(AXE, "localPosition.y", REST_AXE_Y);
        L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", REST_AXE_ROT);

        // ------------------------------------------------------------
        // Bottom
        //
        // Walk에서는 Body와 Bottom을 반대로 움직이지 않는다.
        // 하체 전체가 Body와 함께 이동해야 한다.
        // ------------------------------------------------------------

        SetRest(L, BOTTOM);

        // ------------------------------------------------------------
        // Front Leg
        // ------------------------------------------------------------

        L(THIGH_F, "localPosition.x", REST_THIGH_F_X);
        L(THIGH_F, "localPosition.y", REST_THIGH_F_Y);
        L(THIGH_F, "localPosition.z", 0f);
        C(
            THIGH_F,
            "localEulerAngles.z",
            WALK_THIGH_F_ROT
        );

        L(CALF_F, "localPosition.x", REST_CALF_F_X);
        L(CALF_F, "localPosition.y", REST_CALF_F_Y);
        L(CALF_F, "localPosition.z", 0f);
        C(
            CALF_F,
            "localEulerAngles.z",
            WALK_CALF_F_ROT
        );

        SetRest(L, FOOT_F);

        // ------------------------------------------------------------
        // Back Leg
        // ------------------------------------------------------------

        L(THIGH_B, "localPosition.x", REST_THIGH_B_X);
        L(THIGH_B, "localPosition.y", REST_THIGH_B_Y);
        L(THIGH_B, "localPosition.z", 0f);
        C(
            THIGH_B,
            "localEulerAngles.z",
            WALK_THIGH_B_ROT
        );

        L(CALF_B, "localPosition.x", REST_CALF_B_X);
        L(CALF_B, "localPosition.y", REST_CALF_B_Y);
        L(CALF_B, "localPosition.z", 0f);
        C(
            CALF_B,
            "localEulerAngles.z",
            WALK_CALF_B_ROT
        );

        SetRest(L, FOOT_B);

        // ------------------------------------------------------------
        // Arms
        // ------------------------------------------------------------

        C(
            SF,
            "localEulerAngles.z",
            WALK_SF_ROT
        );

        C(
            SB,
            "localEulerAngles.z",
            WALK_SB_ROT
        );

        Save(clip, "MainBoss2_Walk");
    }


    // ================================================================
    // Run
    // ================================================================

    [MenuItem("Tools/MainBoss2/Generate Run")]
    public static void GenerateRun()
    {
        int total = RUN_BODY_Y.Length;

        var clip = MakeClip(
            "MainBoss2_Run",
            total,
            true
        );

        void C(string path, string prop, float[] vals) =>
            SetCurve(clip, path, prop, vals, total);

        void L(string path, string prop, float val) =>
            SetConstantCurve(
                clip,
                path,
                prop,
                val,
                total
            );

        // ------------------------------------------------------------
        // Body
        // ------------------------------------------------------------

        L(BODY, "localPosition.x", REST_BODY_X);

        float[] runBodyY =
            BuildGroundedBodyY(
                RUN_BODY_Y,
                RUN_THIGH_F_ROT,
                RUN_CALF_F_ROT,
                RUN_THIGH_B_ROT,
                RUN_CALF_B_ROT
            );

        C(BODY, "localPosition.y", runBodyY);

        L(BODY, "localPosition.z", 0f);
        C(BODY, "localEulerAngles.z", RUN_BODY_ROT);

        // ------------------------------------------------------------
        // Head / Arms
        // ------------------------------------------------------------

        SetRest(L, HEAD);

        SetRest(L, SF);
        SetRest(L, FF);
        SetRest(L, HF);

        SetRest(L, SB);
        SetRest(L, FB);
        SetRest(L, HB);

        // ------------------------------------------------------------
        // Axe
        // ------------------------------------------------------------

        L(AXE, "localPosition.x", REST_AXE_X);
        L(AXE, "localPosition.y", REST_AXE_Y);
        L(AXE, "localPosition.z", 0f);
        L(AXE, "localEulerAngles.z", REST_AXE_ROT);

        // ------------------------------------------------------------
        // Bottom
        // ------------------------------------------------------------

        SetRest(L, BOTTOM);

        // ------------------------------------------------------------
        // Front Leg
        // ------------------------------------------------------------

        L(THIGH_F, "localPosition.x", REST_THIGH_F_X);
        L(THIGH_F, "localPosition.y", REST_THIGH_F_Y);
        L(THIGH_F, "localPosition.z", 0f);

        C(
            THIGH_F,
            "localEulerAngles.z",
            RUN_THIGH_F_ROT
        );

        L(CALF_F, "localPosition.x", REST_CALF_F_X);
        L(CALF_F, "localPosition.y", REST_CALF_F_Y);
        L(CALF_F, "localPosition.z", 0f);

        C(
            CALF_F,
            "localEulerAngles.z",
            RUN_CALF_F_ROT
        );

        SetRest(L, FOOT_F);

        // ------------------------------------------------------------
        // Back Leg
        // ------------------------------------------------------------

        L(THIGH_B, "localPosition.x", REST_THIGH_B_X);
        L(THIGH_B, "localPosition.y", REST_THIGH_B_Y);
        L(THIGH_B, "localPosition.z", 0f);

        C(
            THIGH_B,
            "localEulerAngles.z",
            RUN_THIGH_B_ROT
        );

        L(CALF_B, "localPosition.x", REST_CALF_B_X);
        L(CALF_B, "localPosition.y", REST_CALF_B_Y);
        L(CALF_B, "localPosition.z", 0f);

        C(
            CALF_B,
            "localEulerAngles.z",
            RUN_CALF_B_ROT
        );

        SetRest(L, FOOT_B);

        // ------------------------------------------------------------
        // Arms
        // ------------------------------------------------------------

        C(
            SF,
            "localEulerAngles.z",
            RUN_SF_ROT
        );

        C(
            SB,
            "localEulerAngles.z",
            RUN_SB_ROT
        );

        Save(clip, "MainBoss2_Run");
    }


    // ================================================================
    // Ground 계산
    //
    // Foot의 "초기 Rest 자세"를 바닥으로 본다.
    //
    // 각 프레임에서
    //
    // 1. Thigh 회전
    // 2. Calf 회전
    // 3. Foot 위치
    //
    // 를 계산해서 Foot이 기준 바닥보다 아래로 내려가면
    // Body를 위로 올린다.
    //
    // Body의 기본 Y 움직임은 유지하면서 필요한 만큼만 보정한다.
    // ================================================================

    static float[] BuildGroundedBodyY(
        float[] intendedBodyY,
        float[] thighFRot,
        float[] calfFRot,
        float[] thighBRot,
        float[] calfBRot
    )
    {
        int count = intendedBodyY.Length;

        float[] result =
            new float[count];

        float floorF =
            GetRestFootWorldY_Front();

        float floorB =
            GetRestFootWorldY_Back();

        float floor =
            Mathf.Min(
                floorF,
                floorB
            );

        for (int i = 0; i < count; i++)
        {
            float footF =
                GetFootWorldY(
                    true,
                    thighFRot[i],
                    calfFRot[i]
                );

            float footB =
                GetFootWorldY(
                    false,
                    thighBRot[i],
                    calfBRot[i]
                );

            float lowestFoot =
                Mathf.Min(
                    footF,
                    footB
                );

            // 현재 Body bob을 적용한 상태에서
            // 바닥보다 얼마나 내려가는지 계산
            float predicted =
                lowestFoot +
                intendedBodyY[i];

            float correction = 0f;

            if (predicted <
                floor - FOOT_FLOOR_MARGIN)
            {
                correction =
                    floor - predicted;
            }

            result[i] =
                Round3(
                    intendedBodyY[i] +
                    correction
                );
        }

        return result;
    }


    // ================================================================
    // Front Foot World Y
    //
    // Body 기준 좌표.
    // ================================================================

    static float GetFootWorldY(
        bool front,
        float thighRotation,
        float calfRotation
    )
    {
        float thighX;
        float thighY;
        float calfX;
        float calfY;
        float footX;
        float footY;

        if (front)
        {
            thighX = REST_THIGH_F_X;
            thighY = REST_THIGH_F_Y;

            calfX = REST_CALF_F_X;
            calfY = REST_CALF_F_Y;

            footX = REST_FOOT_F_X;
            footY = REST_FOOT_F_Y;
        }
        else
        {
            thighX = REST_THIGH_B_X;
            thighY = REST_THIGH_B_Y;

            calfX = REST_CALF_B_X;
            calfY = REST_CALF_B_Y;

            footX = REST_FOOT_B_X;
            footY = REST_FOOT_B_Y;
        }

        // ------------------------------------------------------------
        // Thigh 위치
        // ------------------------------------------------------------

        Vector2 thigh =
            new Vector2(
                thighX,
                thighY
            );

        // ------------------------------------------------------------
        // Calf 위치
        //
        // Thigh 회전에 의해 부모 기준 위치도 회전한다.
        // ------------------------------------------------------------

        Vector2 calf =
            Rotate(
                new Vector2(
                    calfX,
                    calfY
                ),
                thighRotation
            );

        Vector2 calfWorld =
            thigh +
            calf;

        // ------------------------------------------------------------
        // Foot 위치
        //
        // Calf의 누적 회전으로 회전한다.
        // ------------------------------------------------------------

        Vector2 foot =
            Rotate(
                new Vector2(
                    footX,
                    footY
                ),
                thighRotation +
                calfRotation
            );

        Vector2 footWorld =
            calfWorld +
            foot;

        return footWorld.y;
    }


    // ================================================================
    // Rest Foot Y
    // ================================================================

    static float GetRestFootWorldY_Front()
    {
        return GetFootWorldY(
            true,
            0f,
            0f
        );
    }

    static float GetRestFootWorldY_Back()
    {
        return GetFootWorldY(
            false,
            0f,
            0f
        );
    }


    // ================================================================
    // 2D Rotation
    // ================================================================

    static Vector2 Rotate(
        Vector2 point,
        float degrees
    )
    {
        float rad =
            degrees *
            Mathf.Deg2Rad;

        float cos =
            Mathf.Cos(rad);

        float sin =
            Mathf.Sin(rad);

        return new Vector2(
            point.x * cos -
            point.y * sin,

            point.x * sin +
            point.y * cos
        );
    }


    // ================================================================
    // Rest
    // ================================================================

    static void SetRest(
        System.Action<string, string, float> L,
        string path
    )
    {
        switch (path)
        {
            case HEAD:
                L(path, "localPosition.x", REST_HEAD_X);
                L(path, "localPosition.y", REST_HEAD_Y);
                break;

            case SF:
                L(path, "localPosition.x", REST_SF_X);
                L(path, "localPosition.y", REST_SF_Y);
                break;

            case FF:
                L(path, "localPosition.x", REST_FF_X);
                L(path, "localPosition.y", REST_FF_Y);
                break;

            case HF:
                L(path, "localPosition.x", REST_HF_X);
                L(path, "localPosition.y", REST_HF_Y);
                break;

            case SB:
                L(path, "localPosition.x", REST_SB_X);
                L(path, "localPosition.y", REST_SB_Y);
                break;

            case FB:
                L(path, "localPosition.x", REST_FB_X);
                L(path, "localPosition.y", REST_FB_Y);
                break;

            case HB:
                L(path, "localPosition.x", REST_HB_X);
                L(path, "localPosition.y", REST_HB_Y);
                break;

            case BOTTOM:
                L(path, "localPosition.x", REST_BOTTOM_X);
                L(path, "localPosition.y", REST_BOTTOM_Y);
                break;

            case THIGH_F:
                L(path, "localPosition.x", REST_THIGH_F_X);
                L(path, "localPosition.y", REST_THIGH_F_Y);
                break;

            case CALF_F:
                L(path, "localPosition.x", REST_CALF_F_X);
                L(path, "localPosition.y", REST_CALF_F_Y);
                break;

            case FOOT_F:
                L(path, "localPosition.x", REST_FOOT_F_X);
                L(path, "localPosition.y", REST_FOOT_F_Y);
                break;

            case THIGH_B:
                L(path, "localPosition.x", REST_THIGH_B_X);
                L(path, "localPosition.y", REST_THIGH_B_Y);
                break;

            case CALF_B:
                L(path, "localPosition.x", REST_CALF_B_X);
                L(path, "localPosition.y", REST_CALF_B_Y);
                break;

            case FOOT_B:
                L(path, "localPosition.x", REST_FOOT_B_X);
                L(path, "localPosition.y", REST_FOOT_B_Y);
                break;
        }

        L(path, "localPosition.z", 0f);
        L(path, "localEulerAngles.z", 0f);
    }


    // ================================================================
    // Constant Curve
    // ================================================================

    static void SetConstantCurve(
        AnimationClip clip,
        string path,
        string property,
        float value,
        int totalFrames
    )
    {
        var keys =
            new Keyframe[totalFrames];

        for (int i = 0; i < totalFrames; i++)
        {
            keys[i] =
                new Keyframe(
                    i / FRAME_RATE,
                    value
                );
        }

        var curve =
            new AnimationCurve(keys);

        for (int i = 0;
             i < curve.keys.Length;
             i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(
                curve,
                i,
                AnimationUtility.TangentMode.Constant
            );

            AnimationUtility.SetKeyRightTangentMode(
                curve,
                i,
                AnimationUtility.TangentMode.Constant
            );
        }

        clip.SetCurve(
            path,
            typeof(Transform),
            property,
            curve
        );
    }


    // ================================================================
    // Make Clip
    // ================================================================

    static AnimationClip MakeClip(
        string name,
        int totalFrames,
        bool loop
    )
    {
        EnsureFolder();

        var clip =
            new AnimationClip
            {
                frameRate = FRAME_RATE,
                name = name
            };

        var settings =
            AnimationUtility.GetAnimationClipSettings(
                clip
            );

        settings.loopTime = loop;

        AnimationUtility.SetAnimationClipSettings(
            clip,
            settings
        );

        return clip;
    }


    // ================================================================
    // Set Curve
    // ================================================================

    static void SetCurve(
        AnimationClip clip,
        string path,
        string prop,
        float[] vals,
        int total
    )
    {
        var keys =
            new Keyframe[total];

        for (int i = 0; i < total; i++)
        {
            keys[i] =
                new Keyframe(
                    i / FRAME_RATE,
                    Round3(vals[i])
                );
        }

        var curve =
            new AnimationCurve(keys);

        for (int i = 0;
             i < curve.keys.Length;
             i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(
                curve,
                i,
                AnimationUtility.TangentMode.Auto
            );

            AnimationUtility.SetKeyRightTangentMode(
                curve,
                i,
                AnimationUtility.TangentMode.Auto
            );
        }

        clip.SetCurve(
            path,
            typeof(Transform),
            prop,
            curve
        );
    }


    // ================================================================
    // Save
    // ================================================================

    static void Save(
        AnimationClip clip,
        string name
    )
    {
        string path =
            $"{SAVE_PATH}/{name}.anim";

        var existing =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(
                path
            );

        if (existing != null)
        {
            EditorUtility.CopySerialized(
                clip,
                existing
            );

            existing.name = name;

            EditorUtility.SetDirty(existing);
        }
        else
        {
            AssetDatabase.CreateAsset(
                clip,
                path
            );
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[MainBoss 2] {name} → {path}"
        );
    }


    // ================================================================
    // Generate Animator Controller
    // ================================================================

    [MenuItem("Tools/MainBoss2/Generate AC")]
    public static void GenerateAC()
    {
        EnsureFolder();

        string acPath =
            $"{SAVE_PATH}/MainBoss2_AC.controller";

        var ac =
            AssetDatabase.LoadAssetAtPath<
                AnimatorController
            >(acPath);

        if (ac == null)
        {
            ac =
                AnimatorController
                    .CreateAnimatorControllerAtPath(
                        acPath
                    );
        }

        var rootSM =
            ac.layers[0].stateMachine;

        var existingNames =
            new HashSet<string>();

        foreach (var state in rootSM.states)
            existingNames.Add(
                state.state.name
            );

        string[] clipNames =
        {
            "MainBoss2_Idle",
            "MainBoss2_Walk",
            "MainBoss2_Run"
        };

        foreach (string clipName in clipNames)
        {
            string clipPath =
                $"{SAVE_PATH}/{clipName}.anim";

            var clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    clipPath
                );

            if (clip == null)
            {
                Debug.LogWarning(
                    $"[MainBoss 2] 클립 없음: {clipPath}"
                );

                continue;
            }

            if (!existingNames.Contains(clipName))
            {
                var state =
                    rootSM.AddState(clipName);

                state.motion = clip;
            }
            else
            {
                foreach (var state in rootSM.states)
                {
                    if (state.state.name == clipName)
                    {
                        state.state.motion = clip;
                        break;
                    }
                }
            }
        }

        if (rootSM.states.Length > 0)
        {
            foreach (var state in rootSM.states)
            {
                if (state.state.name ==
                    "MainBoss2_Idle")
                {
                    rootSM.defaultState =
                        state.state;
                    break;
                }
            }
        }

        EditorUtility.SetDirty(ac);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[MainBoss 2] AC 저장 완료: {acPath}"
        );
    }


    // ================================================================
    // Folder
    // ================================================================

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder(
                "Assets/Animations"))
        {
            AssetDatabase.CreateFolder(
                "Assets",
                "Animations"
            );
        }

        if (!AssetDatabase.IsValidFolder(
                "Assets/Animations/2"))
        {
            AssetDatabase.CreateFolder(
                "Assets/Animations",
                "2"
            );
        }

        if (!AssetDatabase.IsValidFolder(
                SAVE_PATH))
        {
            AssetDatabase.CreateFolder(
                "Assets/Animations/2",
                "MainBoss 2"
            );
        }
    }


    // ================================================================
    // Round 3
    // ================================================================

    static float Round3(float value)
    {
        return Mathf.Round(
            value * 1000f
        ) / 1000f;
    }
}