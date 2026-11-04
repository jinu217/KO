using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

public class PlayerHitAnimationGenerator
{
    private const string SAVE_PATH = "Assets/Animations/1/Player";
    private const string CLIP_NAME = "Player_Hit";

    private const string BODY = "Body";
    private const string HEAD = "Body/Head";

    private const string SHOULDER_JOINT_F =
        "Body/ShoulderJoint_F";

    private const string ARM_JOINT_F =
        "Body/ShoulderJoint_F/Shoulder_F/ArmJoint_F";

    private const string SHOULDER_JOINT_B =
        "Body/ShoulderJoint_B";

    private const string ARM_JOINT_B =
        "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B";

    private const string LEG_JOINT_F =
        "Body/LegJoint_F";

    private const string LEG_JOINT_B =
        "Body/LegJoint_B";

    private const string SWORD =
        "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B/Hand_B/Sword";

    private const float FRAME_RATE = 60f;


    [MenuItem("Tools/1/Player/Generate Hit Animation")]
    public static void GenerateHitAnimation()
    {
        GameObject player = FindPlayer();

        if (player == null)
        {
            Debug.LogError(
                "[Player Hit] 씬에서 Player를 찾을 수 없습니다."
            );

            return;
        }

        Animator animator =
            player.GetComponentInChildren<Animator>();

        if (animator == null)
        {
            Debug.LogError(
                "[Player Hit] Player에서 Animator를 찾을 수 없습니다."
            );

            return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError(
                "[Player Hit] Animator Controller가 연결되어 있지 않습니다."
            );

            return;
        }

        Directory.CreateDirectory(SAVE_PATH);

        string clipPath =
            SAVE_PATH + "/" + CLIP_NAME + ".anim";

        AnimationClip clip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

        if (clip == null)
        {
            clip = new AnimationClip();

            AssetDatabase.CreateAsset(
                clip,
                clipPath
            );
        }
        else
        {
            ClearClipCurves(clip);
        }

        clip.frameRate = FRAME_RATE;

        CreateHitCurves(clip);

        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        AddToAnimator(
            animator,
            clip
        );

        Debug.Log(
            "[Player Hit] 피격 애니메이션 생성 완료\n" +
            "Player: " + player.name + "\n" +
            "Clip: " + clipPath
        );
    }


    private static GameObject FindPlayer()
    {
        GameObject player =
            GameObject.Find("Player");

        if (player != null)
            return player;

        PlayerController controller =
            Object.FindFirstObjectByType<PlayerController>();

        if (controller != null)
            return controller.gameObject;

        return null;
    }


    private static void CreateHitCurves(
        AnimationClip clip)
    {
        /*
         * 피격 애니메이션
         *
         * 0.00 : 기본
         * 0.08 : 강하게 뒤로 젖힘
         * 0.16 : 반동
         * 0.26 : 복귀
         * 0.38 : 완전 복귀
         */

        float[] times =
        {
            0.00f,
            0.08f,
            0.16f,
            0.26f,
            0.38f
        };


        // =========================
        // Body
        // =========================

        AddPositionCurve(
            clip,
            BODY,
            new Vector3(
                -0.009f,
                0.000f,
                0.000f
            ),
            new Vector3(
                -0.075f,
                0.015f,
                0.000f
            ),
            new Vector3(
                -0.035f,
                0.005f,
                0.000f
            ),
            new Vector3(
                -0.020f,
                0.000f,
                0.000f
            ),
            new Vector3(
                -0.009f,
                0.000f,
                0.000f
            ),
            times
        );

        AddRotationCurve(
            clip,
            BODY,
            2.223f,
            -10f,
            5f,
            0f,
            2.223f,
            times
        );


        // =========================
        // Head
        // =========================

        AddPositionCurve(
            clip,
            HEAD,
            new Vector3(
                0.054f,
                0.298f,
                0.000f
            ),
            new Vector3(
                0.025f,
                0.300f,
                0.000f
            ),
            new Vector3(
                0.045f,
                0.300f,
                0.000f
            ),
            new Vector3(
                0.050f,
                0.298f,
                0.000f
            ),
            new Vector3(
                0.054f,
                0.298f,
                0.000f
            ),
            times
        );

        AddRotationCurve(
            clip,
            HEAD,
            0f,
            -8f,
            4f,
            0f,
            0f,
            times
        );


        // =========================
        // 앞쪽 어깨
        // =========================

        AddPositionCurve(
            clip,
            SHOULDER_JOINT_F,
            new Vector3(
                0.150f,
                0.250f,
                0.000f
            ),
            new Vector3(
                0.125f,
                0.250f,
                0.000f
            ),
            new Vector3(
                0.140f,
                0.252f,
                0.000f
            ),
            new Vector3(
                0.146f,
                0.250f,
                0.000f
            ),
            new Vector3(
                0.150f,
                0.250f,
                0.000f
            ),
            times
        );

        AddRotationCurve(
            clip,
            SHOULDER_JOINT_F,
            0f,
            -12f,
            6f,
            0f,
            0f,
            times
        );


        // =========================
        // 앞쪽 팔꿈치
        // =========================

        AddRotationCurve(
            clip,
            ARM_JOINT_F,
            0f,
            18f,
            -8f,
            3f,
            0f,
            times
        );


        // =========================
        // 뒤쪽 어깨
        // =========================

        AddPositionCurve(
            clip,
            SHOULDER_JOINT_B,
            new Vector3(
                -0.169f,
                0.242f,
                0.000f
            ),
            new Vector3(
                -0.145f,
                0.242f,
                0.000f
            ),
            new Vector3(
                -0.160f,
                0.244f,
                0.000f
            ),
            new Vector3(
                -0.167f,
                0.242f,
                0.000f
            ),
            new Vector3(
                -0.169f,
                0.242f,
                0.000f
            ),
            times
        );

        AddRotationCurve(
            clip,
            SHOULDER_JOINT_B,
            0f,
            10f,
            -5f,
            2f,
            0f,
            times
        );


        // =========================
        // 뒤쪽 팔꿈치
        // =========================

        AddRotationCurve(
            clip,
            ARM_JOINT_B,
            0f,
            -15f,
            7f,
            -3f,
            0f,
            times
        );


        // =========================
        // 앞쪽 다리
        // =========================

        AddPositionCurve(
            clip,
            LEG_JOINT_F,
            new Vector3(
                0.165f,
                -0.500f,
                0.000f
            ),
            new Vector3(
                0.145f,
                -0.500f,
                0.000f
            ),
            new Vector3(
                0.158f,
                -0.502f,
                0.000f
            ),
            new Vector3(
                0.163f,
                -0.500f,
                0.000f
            ),
            new Vector3(
                0.165f,
                -0.500f,
                0.000f
            ),
            times
        );

        AddRotationCurve(
            clip,
            LEG_JOINT_F,
            0f,
            -5f,
            3f,
            0f,
            0f,
            times
        );


        // =========================
        // 뒤쪽 다리
        // =========================

        AddPositionCurve(
            clip,
            LEG_JOINT_B,
            new Vector3(
                -0.085f,
                -0.500f,
                0.000f
            ),
            new Vector3(
                -0.060f,
                -0.500f,
                0.000f
            ),
            new Vector3(
                -0.078f,
                -0.502f,
                0.000f
            ),
            new Vector3(
                -0.083f,
                -0.500f,
                0.000f
            ),
            new Vector3(
                -0.085f,
                -0.500f,
                0.000f
            ),
            times
        );

        AddRotationCurve(
            clip,
            LEG_JOINT_B,
            0f,
            6f,
            -3f,
            0f,
            0f,
            times
        );


        // =========================
        // Sword
        // =========================

        AddPositionCurve(
            clip,
            SWORD,
            new Vector3(
                -0.150f,
                -0.300f,
                0.000f
            ),
            new Vector3(
                -0.145f,
                -0.300f,
                0.000f
            ),
            new Vector3(
                -0.148f,
                -0.300f,
                0.000f
            ),
            new Vector3(
                -0.150f,
                -0.300f,
                0.000f
            ),
            new Vector3(
                -0.150f,
                -0.300f,
                0.000f
            ),
            times
        );

        AddRotationCurve(
            clip,
            SWORD,
            0f,
            -8f,
            4f,
            0f,
            0f,
            times
        );
    }


    private static void AddPositionCurve(
        AnimationClip clip,
        string path,
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3,
        Vector3 p4,
        float[] times)
    {
        AnimationCurve x =
            new AnimationCurve();

        AnimationCurve y =
            new AnimationCurve();

        AnimationCurve z =
            new AnimationCurve();


        // Rebase the authored head keys to the photo-aligned Player rest pose.
        if (path == HEAD)
        {
            var delta = new Vector3(0.019f, 0.042f, 0f);
            p0 += delta; p1 += delta; p2 += delta; p3 += delta; p4 += delta;
        }

        x.AddKey(times[0], p0.x);
        x.AddKey(times[1], p1.x);
        x.AddKey(times[2], p2.x);
        x.AddKey(times[3], p3.x);
        x.AddKey(times[4], p4.x);

        y.AddKey(times[0], p0.y);
        y.AddKey(times[1], p1.y);
        y.AddKey(times[2], p2.y);
        y.AddKey(times[3], p3.y);
        y.AddKey(times[4], p4.y);

        z.AddKey(times[0], p0.z);
        z.AddKey(times[1], p1.z);
        z.AddKey(times[2], p2.z);
        z.AddKey(times[3], p3.z);
        z.AddKey(times[4], p4.z);


        clip.SetCurve(
            path,
            typeof(Transform),
            "localPosition.x",
            x
        );

        clip.SetCurve(
            path,
            typeof(Transform),
            "localPosition.y",
            y
        );

        clip.SetCurve(
            path,
            typeof(Transform),
            "localPosition.z",
            z
        );
    }


    private static void AddRotationCurve(
        AnimationClip clip,
        string path,
        float r0,
        float r1,
        float r2,
        float r3,
        float r4,
        float[] times)
    {
        AnimationCurve curve =
            new AnimationCurve();

        curve.AddKey(times[0], r0);
        curve.AddKey(times[1], r1);
        curve.AddKey(times[2], r2);
        curve.AddKey(times[3], r3);
        curve.AddKey(times[4], r4);


        clip.SetCurve(
            path,
            typeof(Transform),
            "localEulerAnglesRaw.z",
            curve
        );
    }


    private static void ClearClipCurves(
        AnimationClip clip)
    {
        EditorCurveBinding[] bindings =
            AnimationUtility.GetCurveBindings(clip);

        foreach (EditorCurveBinding binding in bindings)
        {
            AnimationUtility.SetEditorCurve(
                clip,
                binding,
                null
            );
        }
    }


    private static void AddToAnimator(
        Animator animator,
        AnimationClip clip)
    {
        AnimatorController controller =
            animator.runtimeAnimatorController
            as AnimatorController;

        if (controller == null)
        {
            Debug.LogError(
                "[Player Hit] Animator Controller를 " +
                "AnimatorController로 가져올 수 없습니다."
            );

            return;
        }


        // =========================
        // Hit Trigger
        // =========================

        bool hasHitParameter = false;

        foreach (AnimatorControllerParameter parameter
                 in controller.parameters)
        {
            if (parameter.name == "Hit" &&
                parameter.type ==
                AnimatorControllerParameterType.Trigger)
            {
                hasHitParameter = true;
                break;
            }
        }

        if (!hasHitParameter)
        {
            controller.AddParameter(
                "Hit",
                AnimatorControllerParameterType.Trigger
            );
        }


        // =========================
        // Layer
        // =========================

        AnimatorStateMachine stateMachine =
            controller.layers[0].stateMachine;


        // =========================
        // Player_Hit State
        // =========================

        AnimatorState hitState = null;

        foreach (ChildAnimatorState child
                 in stateMachine.states)
        {
            if (child.state.name == CLIP_NAME)
            {
                hitState = child.state;
                break;
            }
        }

        if (hitState == null)
        {
            hitState =
                stateMachine.AddState(
                    CLIP_NAME
                );
        }

        hitState.motion = clip;


        // =========================
        // Any State → Hit
        // =========================

        AnimatorStateTransition hitTransition = null;

        foreach (AnimatorStateTransition transition
                 in stateMachine.anyStateTransitions)
        {
            if (transition.destinationState == hitState)
            {
                hitTransition = transition;
                break;
            }
        }

        if (hitTransition == null)
        {
            hitTransition =
                stateMachine.AddAnyStateTransition(
                    hitState
                );
        }

        ClearConditions(hitTransition);

        hitTransition.hasExitTime = false;
        hitTransition.exitTime = 0f;
        hitTransition.duration = 0.02f;
        hitTransition.hasFixedDuration = true;

        hitTransition.AddCondition(
            AnimatorConditionMode.If,
            0f,
            "Hit"
        );


        // =========================
        // Hit → Idle
        // =========================

        AnimatorState idleState = null;

        foreach (ChildAnimatorState child
                 in stateMachine.states)
        {
            if (child.state.name == "Player_Idle")
            {
                idleState = child.state;
                break;
            }
        }

        if (idleState == null)
        {
            Debug.LogWarning(
                "[Player Hit] Player_Idle 상태를 찾지 못했습니다."
            );
        }
        else
        {
            AnimatorStateTransition idleTransition =
                null;

            foreach (AnimatorStateTransition transition
                     in hitState.transitions)
            {
                if (transition.destinationState ==
                    idleState)
                {
                    idleTransition = transition;
                    break;
                }
            }

            if (idleTransition == null)
            {
                idleTransition =
                    hitState.AddTransition(
                        idleState
                    );
            }

            ClearConditions(idleTransition);

            idleTransition.hasExitTime = true;
            idleTransition.exitTime = 1f;
            idleTransition.duration = 0.03f;
            idleTransition.hasFixedDuration = true;
        }


        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }


    private static void ClearConditions(
        AnimatorStateTransition transition)
    {
        AnimatorCondition[] conditions =
            transition.conditions;

        for (int i = conditions.Length - 1;
             i >= 0;
             i--)
        {
            transition.RemoveCondition(
                conditions[i]
            );
        }
    }
}