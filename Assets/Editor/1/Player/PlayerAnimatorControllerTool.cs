using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class PlayerAnimatorControllerTool : EditorWindow
{
    private AnimatorController animatorController;

    private const string IDLE = "Player_Idle";
    private const string RUN = "Run";
    private const string JUMP = "Jump";
    private const string FALL = "Fall";
    private const string DOWN_JUMP = "DownJump";
    private const string ATTACK = "Attack_Sword";
    private const string DASH = "Dash";

    private const string MOVE_SPEED = "MoveSpeed";
    private const string VERTICAL_VELOCITY = "VerticalVelocity";
    private const string SPEED = "Speed";
    private const string IS_GROUNDED = "IsGrounded";
    private const string IS_DOWN_JUMPING = "IsDownJumping";
    private const string ATTACK_TRIGGER = "Attack";
    private const string DASH_TRIGGER = "Dash";

    private const float MOVE_THRESHOLD = 0.1f;
    private const float TRANSITION_DURATION = 0.05f;

    [MenuItem("Tools/Player/Player Animator Controller Tool")]
    public static void OpenWindow()
    {
        GetWindow<PlayerAnimatorControllerTool>("Player Animator Tool");
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        EditorGUILayout.LabelField(
            "Player Animator Controller Tool",
            EditorStyles.boldLabel
        );

        GUILayout.Space(5);

        animatorController = (AnimatorController)EditorGUILayout.ObjectField(
            "Animator Controller",
            animatorController,
            typeof(AnimatorController),
            false
        );

        GUILayout.Space(10);

        if (animatorController == null)
        {
            EditorGUILayout.HelpBox(
                "Player의 Animator Controller를 넣어주세요.",
                MessageType.Info
            );

            return;
        }

        EditorGUILayout.LabelField(
            "Controller : " + animatorController.name
        );

        GUILayout.Space(10);

        if (GUILayout.Button(
            "전체 Animator 자동 설정",
            GUILayout.Height(35)
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
            "기존 Animator Controller를 직접 수정합니다.\n" +
            "이미 존재하는 State와 Transition은 최대한 재사용하며\n" +
            "필요한 Parameter / Transition / Condition만 추가하거나 수정합니다.",
            MessageType.Info
        );
    }

    private void SetupAnimatorController()
    {
        if (animatorController == null)
            return;

        Undo.RegisterCompleteObjectUndo(
            animatorController,
            "Setup Player Animator Controller"
        );

        AddParameters();
        AddStates();
        SetupTransitions();

        EditorUtility.SetDirty(animatorController);
        AssetDatabase.SaveAssets();

        Debug.Log(
            "[Player Animator Tool] Animator Controller 자동 설정 완료."
        );

        ValidateAnimatorController();
    }

    private void AddParameters()
    {
        AddParameterIfMissing(
            MOVE_SPEED,
            AnimatorControllerParameterType.Float
        );

        AddParameterIfMissing(
            VERTICAL_VELOCITY,
            AnimatorControllerParameterType.Float
        );

        AddParameterIfMissing(
            SPEED,
            AnimatorControllerParameterType.Float
        );

        AddParameterIfMissing(
            IS_GROUNDED,
            AnimatorControllerParameterType.Bool
        );

        AddParameterIfMissing(
            IS_DOWN_JUMPING,
            AnimatorControllerParameterType.Bool
        );

        AddParameterIfMissing(
            ATTACK_TRIGGER,
            AnimatorControllerParameterType.Trigger
        );

        AddParameterIfMissing(
            DASH_TRIGGER,
            AnimatorControllerParameterType.Trigger
        );
    }

    private void AddParameterIfMissing(
        string parameterName,
        AnimatorControllerParameterType type
    )
    {
        foreach (AnimatorControllerParameter parameter
            in animatorController.parameters)
        {
            if (parameter.name == parameterName)
                return;
        }

        animatorController.AddParameter(
            parameterName,
            type
        );

        Debug.Log(
            "[Player Animator Tool] Parameter 추가: " +
            parameterName
        );
    }

    private void AddStates()
    {
        AnimatorControllerLayer layer =
            animatorController.layers[0];

        AnimatorStateMachine stateMachine =
            layer.stateMachine;

        EnsureState(stateMachine, IDLE);
        EnsureState(stateMachine, RUN);
        EnsureState(stateMachine, JUMP);
        EnsureState(stateMachine, FALL);
        EnsureState(stateMachine, DOWN_JUMP);
        EnsureState(stateMachine, ATTACK);
        EnsureState(stateMachine, DASH);

        AnimatorState idleState =
            FindState(stateMachine, IDLE);

        if (idleState != null &&
            stateMachine.defaultState == null)
        {
            stateMachine.defaultState = idleState;
        }
    }

    private AnimatorState EnsureState(
        AnimatorStateMachine stateMachine,
        string stateName
    )
    {
        AnimatorState existingState =
            FindState(stateMachine, stateName);

        if (existingState != null)
            return existingState;

        AnimatorState newState =
            stateMachine.AddState(stateName);

        Debug.Log(
            "[Player Animator Tool] State 추가: " +
            stateName
        );

        return newState;
    }

    private AnimatorState FindState(
        AnimatorStateMachine stateMachine,
        string stateName
    )
    {
        foreach (ChildAnimatorState childState
            in stateMachine.states)
        {
            if (childState.state.name == stateName)
                return childState.state;
        }

        return null;
    }

    private AnimatorStateTransition GetOrCreateTransition(
        AnimatorState fromState,
        AnimatorState toState
    )
    {
        foreach (AnimatorStateTransition transition
            in fromState.transitions)
        {
            if (transition.destinationState == toState)
                return transition;
        }

        return fromState.AddTransition(toState);
    }

    private AnimatorStateTransition GetOrCreateAnyStateTransition(
        AnimatorStateMachine stateMachine,
        AnimatorState toState
    )
    {
        foreach (AnimatorStateTransition transition
            in stateMachine.anyStateTransitions)
        {
            if (transition.destinationState == toState)
                return transition;
        }

        return stateMachine.AddAnyStateTransition(toState);
    }

    private void SetupTransitions()
    {
        AnimatorControllerLayer layer =
            animatorController.layers[0];

        AnimatorStateMachine stateMachine =
            layer.stateMachine;

        AnimatorState idle =
            FindState(stateMachine, IDLE);

        AnimatorState run =
            FindState(stateMachine, RUN);

        AnimatorState jump =
            FindState(stateMachine, JUMP);

        AnimatorState fall =
            FindState(stateMachine, FALL);

        AnimatorState downJump =
            FindState(stateMachine, DOWN_JUMP);

        AnimatorState attack =
            FindState(stateMachine, ATTACK);

        AnimatorState dash =
            FindState(stateMachine, DASH);

        if (idle == null ||
            run == null ||
            jump == null ||
            fall == null ||
            downJump == null ||
            attack == null ||
            dash == null)
        {
            Debug.LogError(
                "[Player Animator Tool] 필요한 State가 없습니다."
            );

            return;
        }

        SetupIdleRun(idle, run);

        SetupJumpTransitions(
            idle,
            run,
            jump
        );

        SetupFallTransitions(
            idle,
            run,
            jump,
            fall
        );

        SetupDownJumpTransitions(
            idle,
            run,
            downJump
        );

        SetupLandingTransitions(
            fall,
            downJump,
            idle
        );

        SetupAttackTransition(
            stateMachine,
            attack,
            idle
        );

        SetupDashTransition(
            stateMachine,
            dash,
            idle
        );
    }

    private void SetupIdleRun(
        AnimatorState idle,
        AnimatorState run
    )
    {
        AnimatorStateTransition idleToRun =
            GetOrCreateTransition(idle, run);

        ConfigureNormalTransition(
            idleToRun
        );

        SetSingleCondition(
            idleToRun,
            AnimatorConditionMode.Greater,
            MOVE_SPEED,
            MOVE_THRESHOLD
        );

        AnimatorStateTransition runToIdle =
            GetOrCreateTransition(run, idle);

        ConfigureNormalTransition(
            runToIdle
        );

        SetSingleCondition(
            runToIdle,
            AnimatorConditionMode.Less,
            MOVE_SPEED,
            MOVE_THRESHOLD
        );
    }

    private void SetupJumpTransitions(
        AnimatorState idle,
        AnimatorState run,
        AnimatorState jump
    )
    {
        AnimatorStateTransition idleToJump =
            GetOrCreateTransition(idle, jump);

        ConfigureNormalTransition(
            idleToJump
        );

        SetConditions(
            idleToJump,
            new AnimatorCondition
            {
                mode = AnimatorConditionMode.IfNot,
                parameter = IS_GROUNDED
            },
            new AnimatorCondition
            {
                mode = AnimatorConditionMode.Greater,
                parameter = VERTICAL_VELOCITY,
                threshold = 0f
            }
        );

        AnimatorStateTransition runToJump =
            GetOrCreateTransition(run, jump);

        ConfigureNormalTransition(
            runToJump
        );

        SetConditions(
            runToJump,
            new AnimatorCondition
            {
                mode = AnimatorConditionMode.IfNot,
                parameter = IS_GROUNDED
            },
            new AnimatorCondition
            {
                mode = AnimatorConditionMode.Greater,
                parameter = VERTICAL_VELOCITY,
                threshold = 0f
            }
        );
    }

    private void SetupFallTransitions(
        AnimatorState idle,
        AnimatorState run,
        AnimatorState jump,
        AnimatorState fall
    )
    {
        AnimatorStateTransition jumpToFall =
            GetOrCreateTransition(jump, fall);

        ConfigureNormalTransition(
            jumpToFall
        );

        SetSingleCondition(
            jumpToFall,
            AnimatorConditionMode.Less,
            VERTICAL_VELOCITY,
            0f
        );

        AnimatorStateTransition idleToFall =
            GetOrCreateTransition(idle, fall);

        ConfigureNormalTransition(
            idleToFall
        );

        SetConditions(
            idleToFall,
            new AnimatorCondition
            {
                mode = AnimatorConditionMode.IfNot,
                parameter = IS_GROUNDED
            },
            new AnimatorCondition
            {
                mode = AnimatorConditionMode.Less,
                parameter = VERTICAL_VELOCITY,
                threshold = 0f
            }
        );

        AnimatorStateTransition runToFall =
            GetOrCreateTransition(run, fall);

        ConfigureNormalTransition(
            runToFall
        );

        SetConditions(
            runToFall,
            new AnimatorCondition
            {
                mode = AnimatorConditionMode.IfNot,
                parameter = IS_GROUNDED
            },
            new AnimatorCondition
            {
                mode = AnimatorConditionMode.Less,
                parameter = VERTICAL_VELOCITY,
                threshold = 0f
            }
        );
    }

    private void SetupDownJumpTransitions(
        AnimatorState idle,
        AnimatorState run,
        AnimatorState downJump
    )
    {
        AnimatorStateTransition idleToDownJump =
            GetOrCreateTransition(idle, downJump);

        ConfigureNormalTransition(
            idleToDownJump
        );

        SetSingleCondition(
            idleToDownJump,
            AnimatorConditionMode.If,
            IS_DOWN_JUMPING
        );

        AnimatorStateTransition runToDownJump =
            GetOrCreateTransition(run, downJump);

        ConfigureNormalTransition(
            runToDownJump
        );

        SetSingleCondition(
            runToDownJump,
            AnimatorConditionMode.If,
            IS_DOWN_JUMPING
        );
    }

    private void SetupLandingTransitions(
        AnimatorState fall,
        AnimatorState downJump,
        AnimatorState idle
    )
    {
        AnimatorStateTransition fallToIdle =
            GetOrCreateTransition(fall, idle);

        ConfigureNormalTransition(
            fallToIdle
        );

        SetSingleCondition(
            fallToIdle,
            AnimatorConditionMode.If,
            IS_GROUNDED
        );

        AnimatorStateTransition downJumpToIdle =
            GetOrCreateTransition(downJump, idle);

        ConfigureNormalTransition(
            downJumpToIdle
        );

        SetSingleCondition(
            downJumpToIdle,
            AnimatorConditionMode.If,
            IS_GROUNDED
        );
    }

    private void SetupAttackTransition(
        AnimatorStateMachine stateMachine,
        AnimatorState attack,
        AnimatorState idle
    )
    {
        AnimatorStateTransition anyToAttack =
            GetOrCreateAnyStateTransition(
                stateMachine,
                attack
            );

        ConfigureNormalTransition(
            anyToAttack
        );

        SetSingleCondition(
            anyToAttack,
            AnimatorConditionMode.If,
            ATTACK_TRIGGER
        );

        AnimatorStateTransition attackToIdle =
            GetOrCreateTransition(
                attack,
                idle
            );

        ConfigureExitTransition(
            attackToIdle
        );
    }

    private void SetupDashTransition(
        AnimatorStateMachine stateMachine,
        AnimatorState dash,
        AnimatorState idle
    )
    {
        AnimatorStateTransition anyToDash =
            GetOrCreateAnyStateTransition(
                stateMachine,
                dash
            );

        ConfigureNormalTransition(
            anyToDash
        );

        SetSingleCondition(
            anyToDash,
            AnimatorConditionMode.If,
            DASH_TRIGGER
        );

        AnimatorStateTransition dashToIdle =
            GetOrCreateTransition(
                dash,
                idle
            );

        ConfigureExitTransition(
            dashToIdle
        );
    }

    private void ConfigureNormalTransition(
        AnimatorStateTransition transition
    )
    {
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = TRANSITION_DURATION;
        transition.exitTime = 0f;
        transition.offset = 0f;
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
        transition.interruptionSource =
            TransitionInterruptionSource.None;

        ClearConditions(transition);
    }

    private void SetSingleCondition(
        AnimatorStateTransition transition,
        AnimatorConditionMode mode,
        string parameter,
        float threshold = 0f
    )
    {
        ClearConditions(transition);

        transition.AddCondition(
            mode,
            threshold,
            parameter
        );
    }

    private void SetConditions(
        AnimatorStateTransition transition,
        params AnimatorCondition[] conditions
    )
    {
        ClearConditions(transition);

        foreach (AnimatorCondition condition
            in conditions)
        {
            transition.AddCondition(
                condition.mode,
                condition.threshold,
                condition.parameter
            );
        }
    }

    private void ClearConditions(
        AnimatorStateTransition transition
    )
    {
        AnimatorCondition[] conditions =
            transition.conditions;

        for (int i = conditions.Length - 1; i >= 0; i--)
        {
            transition.RemoveCondition(conditions[i]);
        }
    }

    private void ValidateAnimatorController()
    {
        if (animatorController == null)
            return;

        AnimatorControllerLayer layer =
            animatorController.layers[0];

        AnimatorStateMachine stateMachine =
            layer.stateMachine;

        int errorCount = 0;
        int warningCount = 0;

        Debug.Log(
            "========== Player Animator 검사 시작 =========="
        );

        string[] requiredParameters =
        {
            MOVE_SPEED,
            VERTICAL_VELOCITY,
            SPEED,
            IS_GROUNDED,
            IS_DOWN_JUMPING,
            ATTACK_TRIGGER,
            DASH_TRIGGER
        };

        foreach (string parameterName
            in requiredParameters)
        {
            if (HasParameter(parameterName))
            {
                Debug.Log(
                    "[OK] Parameter : " +
                    parameterName
                );
            }
            else
            {
                Debug.LogError(
                    "[ERROR] Parameter 없음 : " +
                    parameterName
                );

                errorCount++;
            }
        }

        string[] requiredStates =
        {
            IDLE,
            RUN,
            JUMP,
            FALL,
            DOWN_JUMP,
            ATTACK,
            DASH
        };

        foreach (string stateName
            in requiredStates)
        {
            if (FindState(stateMachine, stateName) != null)
            {
                Debug.Log(
                    "[OK] State : " +
                    stateName
                );
            }
            else
            {
                Debug.LogError(
                    "[ERROR] State 없음 : " +
                    stateName
                );

                errorCount++;
            }
        }

        AnimatorState idle =
            FindState(stateMachine, IDLE);

        AnimatorState run =
            FindState(stateMachine, RUN);

        AnimatorState jump =
            FindState(stateMachine, JUMP);

        AnimatorState fall =
            FindState(stateMachine, FALL);

        AnimatorState downJump =
            FindState(stateMachine, DOWN_JUMP);

        AnimatorState attack =
            FindState(stateMachine, ATTACK);

        AnimatorState dash =
            FindState(stateMachine, DASH);

        if (idle != null && run != null)
        {
            CheckTransition(
                idle,
                run,
                "Idle → Run",
                ref errorCount
            );

            CheckTransition(
                run,
                idle,
                "Run → Idle",
                ref errorCount
            );
        }

        if (idle != null && jump != null)
        {
            CheckTransition(
                idle,
                jump,
                "Idle → Jump",
                ref errorCount
            );
        }

        if (run != null && jump != null)
        {
            CheckTransition(
                run,
                jump,
                "Run → Jump",
                ref errorCount
            );
        }

        if (jump != null && fall != null)
        {
            CheckTransition(
                jump,
                fall,
                "Jump → Fall",
                ref errorCount
            );
        }

        if (idle != null && fall != null)
        {
            CheckTransition(
                idle,
                fall,
                "Idle → Fall",
                ref errorCount
            );
        }

        if (run != null && fall != null)
        {
            CheckTransition(
                run,
                fall,
                "Run → Fall",
                ref errorCount
            );
        }

        if (idle != null && downJump != null)
        {
            CheckTransition(
                idle,
                downJump,
                "Idle → DownJump",
                ref errorCount
            );
        }

        if (run != null && downJump != null)
        {
            CheckTransition(
                run,
                downJump,
                "Run → DownJump",
                ref errorCount
            );
        }

        if (fall != null && idle != null)
        {
            CheckTransition(
                fall,
                idle,
                "Fall → Idle",
                ref errorCount
            );
        }

        if (downJump != null && idle != null)
        {
            CheckTransition(
                downJump,
                idle,
                "DownJump → Idle",
                ref errorCount
            );
        }

        if (attack != null)
        {
            CheckAnyStateTransition(
                stateMachine,
                attack,
                "Any State → Attack_Sword",
                ATTACK_TRIGGER,
                ref errorCount
            );

            CheckExitTransition(
                attack,
                idle,
                "Attack_Sword → Idle",
                ref errorCount
            );
        }

        if (dash != null)
        {
            CheckAnyStateTransition(
                stateMachine,
                dash,
                "Any State → Dash",
                DASH_TRIGGER,
                ref errorCount
            );

            CheckExitTransition(
                dash,
                idle,
                "Dash → Idle",
                ref errorCount
            );
        }

        if (stateMachine.defaultState == null)
        {
            Debug.LogWarning(
                "[WARNING] Default State가 설정되어 있지 않습니다."
            );

            warningCount++;
        }
        else if (stateMachine.defaultState.name != IDLE)
        {
            Debug.LogWarning(
                "[WARNING] Default State가 " +
                IDLE +
                "가 아닙니다."
            );

            warningCount++;
        }

        Debug.Log(
            "========== Player Animator 검사 종료 ==========\n" +
            "Errors : " +
            errorCount +
            "\nWarnings : " +
            warningCount
        );
    }

    private bool HasParameter(
        string parameterName
    )
    {
        foreach (AnimatorControllerParameter parameter
            in animatorController.parameters)
        {
            if (parameter.name == parameterName)
                return true;
        }

        return false;
    }

    private void CheckTransition(
        AnimatorState from,
        AnimatorState to,
        string label,
        ref int errorCount
    )
    {
        AnimatorStateTransition transition =
            FindTransition(from, to);

        if (transition == null)
        {
            Debug.LogError(
                "[ERROR] Transition 없음 : " +
                label
            );

            errorCount++;
            return;
        }

        Debug.Log(
            "[OK] Transition : " +
            label
        );
    }

    private void CheckAnyStateTransition(
        AnimatorStateMachine stateMachine,
        AnimatorState destination,
        string label,
        string parameter,
        ref int errorCount
    )
    {
        AnimatorStateTransition transition =
            null;

        foreach (AnimatorStateTransition candidate
            in stateMachine.anyStateTransitions)
        {
            if (candidate.destinationState == destination)
            {
                transition = candidate;
                break;
            }
        }

        if (transition == null)
        {
            Debug.LogError(
                "[ERROR] Transition 없음 : " +
                label
            );

            errorCount++;
            return;
        }

        bool hasParameter =
            false;

        foreach (AnimatorCondition condition
            in transition.conditions)
        {
            if (condition.parameter == parameter)
            {
                hasParameter = true;
                break;
            }
        }

        if (!hasParameter)
        {
            Debug.LogError(
                "[ERROR] 조건 없음 : " +
                label +
                " / " +
                parameter
            );

            errorCount++;
            return;
        }

        Debug.Log(
            "[OK] Transition : " +
            label
        );
    }

    private void CheckExitTransition(
        AnimatorState from,
        AnimatorState to,
        string label,
        ref int errorCount
    )
    {
        AnimatorStateTransition transition =
            FindTransition(from, to);

        if (transition == null)
        {
            Debug.LogError(
                "[ERROR] Transition 없음 : " +
                label
            );

            errorCount++;
            return;
        }

        if (!transition.hasExitTime)
        {
            Debug.LogError(
                "[ERROR] Exit Time 없음 : " +
                label
            );

            errorCount++;
            return;
        }

        Debug.Log(
            "[OK] Transition : " +
            label
        );
    }

    private AnimatorStateTransition FindTransition(
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

        return null;
    }
}