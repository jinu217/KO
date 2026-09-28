using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class BasicMob1AnimatorControllerTool : EditorWindow
{
    private AnimatorController animatorController;

    private AnimationClip idleClip;
    private AnimationClip runClip;
    private AnimationClip attackClip;

    private const string IDLE = "Mob_Idle";
    private const string RUN = "Run";
    private const string ATTACK = "Attack";

    private const string MOVE_SPEED = "MoveSpeed";
    private const string ATTACK_TRIGGER = "Attack";

    private const float MOVE_THRESHOLD = 0.1f;
    private const float TRANSITION_DURATION = 0.05f;
    private const float ATTACK_TRANSITION_DURATION = 0.02f;

    [MenuItem("Tools/1/BasicMob 1/Animator Controller Tool")]
    public static void OpenWindow()
    {
        GetWindow<BasicMob1AnimatorControllerTool>(
            "BasicMob 1 Animator Tool"
        );
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        EditorGUILayout.LabelField(
            "BasicMob 1 Animator Controller Tool",
            EditorStyles.boldLabel
        );

        GUILayout.Space(5);

        animatorController =
            (AnimatorController)EditorGUILayout.ObjectField(
                "Animator Controller",
                animatorController,
                typeof(AnimatorController),
                false
            );

        GUILayout.Space(5);

        EditorGUILayout.LabelField(
            "Animation Clips",
            EditorStyles.boldLabel
        );

        idleClip =
            (AnimationClip)EditorGUILayout.ObjectField(
                "Mob_idle",
                idleClip,
                typeof(AnimationClip),
                false
            );

        runClip =
            (AnimationClip)EditorGUILayout.ObjectField(
                "Run",
                runClip,
                typeof(AnimationClip),
                false
            );

        attackClip =
            (AnimationClip)EditorGUILayout.ObjectField(
                "Attack",
                attackClip,
                typeof(AnimationClip),
                false
            );

        GUILayout.Space(10);

        if (GUILayout.Button(
            "애니메이션 자동 찾기",
            GUILayout.Height(30)
        ))
        {
            FindAnimationClips();
        }

        GUILayout.Space(5);

        if (GUILayout.Button(
            "BasicMob 1 Animator 전체 설정",
            GUILayout.Height(40)
        ))
        {
            SetupAnimatorController();
        }

        GUILayout.Space(5);

        if (GUILayout.Button(
            "현재 Animator 설정 검사",
            GUILayout.Height(30)
        ))
        {
            ValidateAnimatorController();
        }

        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "생성되는 구성\n\n" +
            "Parameters\n" +
            "• MoveSpeed (Float)\n" +
            "• Attack (Trigger)\n\n" +
            "States\n" +
            "• Mob_idle\n" +
            "• Run\n" +
            "• Attack\n\n" +
            "Transitions\n" +
            "• Mob_idle → Run\n" +
            "• Run → Mob_idle\n" +
            "• Any State → Attack\n" +
            "• Attack → Mob_idle",
            MessageType.Info
        );
    }

    private void FindAnimationClips()
    {
        idleClip = FindClip(IDLE);
        runClip = FindClip(RUN);
        attackClip = FindClip(ATTACK);

        Debug.Log(
            "[BasicMob 1 Animator] Animation 자동 검색 완료\n" +
            $"Mob_idle : {(idleClip != null ? idleClip.name : "없음")}\n" +
            $"Run : {(runClip != null ? runClip.name : "없음")}\n" +
            $"Attack : {(attackClip != null ? attackClip.name : "없음")}"
        );
    }

    private AnimationClip FindClip(string clipName)
    {
        string[] guids = AssetDatabase.FindAssets(
            $"t:AnimationClip {clipName}"
        );

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            AnimationClip clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

            if (clip != null && clip.name == clipName)
                return clip;
        }

        return null;
    }

    private void SetupAnimatorController()
    {
        if (animatorController == null)
        {
            EditorUtility.DisplayDialog(
                "오류",
                "Animator Controller를 넣어주세요.",
                "확인"
            );

            return;
        }

        if (idleClip == null ||
            runClip == null ||
            attackClip == null)
        {
            EditorUtility.DisplayDialog(
                "오류",
                "Mob_idle, Run, Attack Animation Clip을 모두 넣어주세요.\n\n" +
                "'애니메이션 자동 찾기' 버튼을 먼저 눌러도 됩니다.",
                "확인"
            );

            return;
        }

        AnimatorControllerLayer layer =
            animatorController.layers[0];

        AnimatorStateMachine stateMachine =
            layer.stateMachine;

        AddOrUpdateParameter(
            MOVE_SPEED,
            AnimatorControllerParameterType.Float
        );

        AddOrUpdateParameter(
            ATTACK_TRIGGER,
            AnimatorControllerParameterType.Trigger
        );

        AnimatorState idle =
            GetOrCreateState(
                stateMachine,
                IDLE,
                idleClip,
                new Vector3(250, 100, 0)
            );

        AnimatorState run =
            GetOrCreateState(
                stateMachine,
                RUN,
                runClip,
                new Vector3(500, 100, 0)
            );

        AnimatorState attack =
            GetOrCreateState(
                stateMachine,
                ATTACK,
                attackClip,
                new Vector3(500, 250, 0)
            );

        stateMachine.defaultState = idle;

        ConfigureIdleToRun(
            idle,
            run
        );

        ConfigureRunToIdle(
            run,
            idle
        );

        ConfigureAnyToAttack(
            stateMachine,
            attack
        );

        ConfigureAttackToIdle(
            attack,
            idle
        );

        EditorUtility.SetDirty(animatorController);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = animatorController;

        EditorUtility.DisplayDialog(
            "완료",
            "BasicMob 1 Animator 설정이 완료되었습니다.",
            "확인"
        );

        Debug.Log(
            "========== BasicMob 1 Animator 설정 완료 ==========\n" +
            "Parameters : MoveSpeed, Attack\n" +
            "States : Mob_idle, Run, Attack\n" +
            "Transitions : Idle ↔ Run, Any State → Attack, Attack → Idle"
        );
    }

    private void AddOrUpdateParameter(
        string parameterName,
        AnimatorControllerParameterType type
    )
    {
        AnimatorControllerParameter[] parameters =
            animatorController.parameters;

        foreach (AnimatorControllerParameter parameter
            in parameters)
        {
            if (parameter.name == parameterName)
            {
                if (parameter.type != type)
                {
                    Debug.LogWarning(
                        $"[BasicMob 1 Animator] Parameter '{parameterName}'의 타입이 " +
                        $"{parameter.type}입니다. 새로 변경하지 않습니다."
                    );
                }

                return;
            }
        }

        animatorController.AddParameter(
            parameterName,
            type
        );
    }

    private AnimatorState GetOrCreateState(
        AnimatorStateMachine stateMachine,
        string stateName,
        AnimationClip clip,
        Vector3 position
    )
    {
        foreach (ChildAnimatorState child
            in stateMachine.states)
        {
            if (child.state.name == stateName)
            {
                child.state.motion = clip;
                return child.state;
            }
        }

        AnimatorState newState =
            stateMachine.AddState(
                stateName,
                position
            );

        newState.motion = clip;

        return newState;
    }

    private void ConfigureIdleToRun(
        AnimatorState idle,
        AnimatorState run
    )
    {
        AnimatorStateTransition transition =
            GetOrCreateTransition(
                idle,
                run
            );

        ConfigureNormalTransition(
            transition,
            TRANSITION_DURATION
        );

        SetSingleCondition(
            transition,
            AnimatorConditionMode.Greater,
            MOVE_SPEED,
            MOVE_THRESHOLD
        );
    }

    private void ConfigureRunToIdle(
        AnimatorState run,
        AnimatorState idle
    )
    {
        AnimatorStateTransition transition =
            GetOrCreateTransition(
                run,
                idle
            );

        ConfigureNormalTransition(
            transition,
            TRANSITION_DURATION
        );

        SetSingleCondition(
            transition,
            AnimatorConditionMode.Less,
            MOVE_SPEED,
            MOVE_THRESHOLD
        );
    }

    private void ConfigureAnyToAttack(
        AnimatorStateMachine stateMachine,
        AnimatorState attack
    )
    {
        AnimatorStateTransition transition =
            GetOrCreateAnyStateTransition(
                stateMachine,
                attack
            );

        ConfigureNormalTransition(
            transition,
            ATTACK_TRANSITION_DURATION
        );

        SetSingleCondition(
            transition,
            AnimatorConditionMode.If,
            ATTACK_TRIGGER
        );
    }

    private void ConfigureAttackToIdle(
        AnimatorState attack,
        AnimatorState idle
    )
    {
        AnimatorStateTransition transition =
            GetOrCreateTransition(
                attack,
                idle
            );

        ConfigureExitTransition(
            transition
        );
    }

    private void ConfigureNormalTransition(
        AnimatorStateTransition transition,
        float duration
    )
    {
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = duration;
        transition.exitTime = 0f;
        transition.offset = 0f;
        transition.canTransitionToSelf = false;
        transition.interruptionSource =
            TransitionInterruptionSource.None;
    }

    private void ConfigureExitTransition(
        AnimatorStateTransition transition
    )
    {
        transition.hasExitTime = true;
        transition.hasFixedDuration = true;
        transition.exitTime = 1f;
        transition.duration = TRANSITION_DURATION;
        transition.offset = 0f;
        transition.canTransitionToSelf = false;
        transition.interruptionSource =
            TransitionInterruptionSource.None;

        ClearConditions(
            transition
        );
    }

    private AnimatorStateTransition GetOrCreateTransition(
        AnimatorState from,
        AnimatorState to
    )
    {
        foreach (AnimatorStateTransition transition
            in from.transitions)
        {
            if (transition.destinationState == to)
                return transition;
        }

        return from.AddTransition(to);
    }

    private AnimatorStateTransition GetOrCreateAnyStateTransition(
        AnimatorStateMachine stateMachine,
        AnimatorState to
    )
    {
        foreach (AnimatorStateTransition transition
            in stateMachine.anyStateTransitions)
        {
            if (transition.destinationState == to)
                return transition;
        }

        return stateMachine.AddAnyStateTransition(to);
    }

    private void SetSingleCondition(
        AnimatorStateTransition transition,
        AnimatorConditionMode mode,
        string parameter,
        float threshold = 0f
    )
    {
        ClearConditions(
            transition
        );

        transition.AddCondition(
            mode,
            threshold,
            parameter
        );
    }

    private void ClearConditions(
        AnimatorStateTransition transition
    )
    {
        AnimatorCondition[] conditions =
            transition.conditions;

        for (int i = conditions.Length - 1; i >= 0; i--)
        {
            transition.RemoveCondition(
                conditions[i]
            );
        }
    }

    private void ValidateAnimatorController()
    {
        if (animatorController == null)
        {
            Debug.LogError(
                "[BasicMob 1 Animator] Animator Controller가 없습니다."
            );

            return;
        }

        AnimatorControllerParameter moveSpeed =
            FindParameter(MOVE_SPEED);

        AnimatorControllerParameter attack =
            FindParameter(ATTACK_TRIGGER);

        if (moveSpeed == null)
        {
            Debug.LogError(
                "[ERROR] MoveSpeed Parameter 없음"
            );
        }
        else
        {
            Debug.Log(
                $"[OK] MoveSpeed : {moveSpeed.type}"
            );
        }

        if (attack == null)
        {
            Debug.LogError(
                "[ERROR] Attack Parameter 없음"
            );
        }
        else
        {
            Debug.Log(
                $"[OK] Attack : {attack.type}"
            );
        }

        AnimatorStateMachine stateMachine =
            animatorController.layers[0].stateMachine;

        ValidateState(
            stateMachine,
            IDLE
        );

        ValidateState(
            stateMachine,
            RUN
        );

        ValidateState(
            stateMachine,
            ATTACK
        );
    }

    private AnimatorControllerParameter FindParameter(
        string parameterName
    )
    {
        foreach (AnimatorControllerParameter parameter
            in animatorController.parameters)
        {
            if (parameter.name == parameterName)
                return parameter;
        }

        return null;
    }

    private void ValidateState(
        AnimatorStateMachine stateMachine,
        string stateName
    )
    {
        foreach (ChildAnimatorState child
            in stateMachine.states)
        {
            if (child.state.name == stateName)
            {
                Debug.Log(
                    $"[OK] State : {stateName}"
                );

                return;
            }
        }

        Debug.LogError(
            $"[ERROR] State 없음 : {stateName}"
        );
    }
}