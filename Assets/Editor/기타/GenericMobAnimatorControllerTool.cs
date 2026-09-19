using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class GenericMobAnimatorControllerTool : EditorWindow
{
    private UnityEngine.Object sourceAsset;
    private AnimatorController animatorController;

    private AnimationClip idleClip;
    private AnimationClip runClip;
    private AnimationClip walkClip;
    private AnimationClip attackClip;

    private const string IDLE = "Idle";
    private const string RUN = "Run";
    private const string WALK = "Walk";
    private const string ATTACK = "Attack";

    private const string MOVE_SPEED = "MoveSpeed";
    private const string ATTACK_TRIGGER = "Attack";
    private const string IS_CHASING = "IsChasing";

    private const float MOVE_THRESHOLD = 0.1f;
    private const float TRANSITION_DURATION = 0.05f;
    private const float ATTACK_TRANSITION_DURATION = 0.02f;

    [MenuItem("Tools/Animation/Animator Controller Tool")]
    public static void OpenWindow()
    {
        GetWindow<GenericMobAnimatorControllerTool>(
            "Animator Tool"
        );
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        EditorGUILayout.LabelField(
            "Generic Animator Controller Tool",
            EditorStyles.boldLabel
        );

        GUILayout.Space(5);

        UnityEngine.Object previousSourceAsset = sourceAsset;

        sourceAsset = EditorGUILayout.ObjectField(
            "애니메이션 폴더 드래그앤드롭",
            sourceAsset,
            typeof(DefaultAsset),
            false
        );

        if (sourceAsset != previousSourceAsset)
        {
            ScanDroppedAsset();
        }

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
                "Idle",
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

        walkClip =
            (AnimationClip)EditorGUILayout.ObjectField(
                "Walk",
                walkClip,
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
            "Animation Clips 칸 비우기",
            GUILayout.Height(30)
        ))
        {
            ClearAnimationClips();
        }

        GUILayout.Space(5);

        if (GUILayout.Button(
            "Generic Animator 전체 설정",
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
            "• IsChasing (Bool)\n" +
            "• Attack (Trigger)\n\n" +
            "States\n" +
            "• Idle\n" +
            "• Walk\n" +
            "• Run\n" +
            "• Attack\n\n" +
            "Transitions\n" +
            "• Idle ↔ Walk\n" +
            "• Walk ↔ Run (IsChasing)\n" +
            "• Any State → Attack\n" +
            "• Attack → Idle",
            MessageType.Info
        );
    }

    private void ClearAnimationClips()
    {
        // 프로젝트의 AnimationClip 에셋은 삭제하지 않고,
        // 이 툴의 Animation Clips 입력 칸 참조만 초기화합니다.
        idleClip = null;
        runClip = null;
        walkClip = null;
        attackClip = null;

        Repaint();

        Debug.Log(
            "[Generic Animator] Animation Clips 입력 칸을 비웠습니다. " +
            "프로젝트의 클립 에셋은 삭제되지 않았습니다."
        );
    }

    private void ScanDroppedAsset()
    {
        if (sourceAsset == null)
        {
            animatorController = null;
            ClearAnimationClips();
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(sourceAsset);
        if (string.IsNullOrEmpty(assetPath) ||
            !AssetDatabase.IsValidFolder(assetPath))
        {
            Debug.LogWarning(
                "[Generic Animator] Animation Controller와 Clip이 들어 있는 폴더를 드래그해 주세요."
            );
            sourceAsset = null;
            Repaint();
            return;
        }

        animatorController = null;
        idleClip = null;
        runClip = null;
        walkClip = null;
        attackClip = null;

        UnityEngine.Object[] assets =
            LoadDroppedAssetContents(assetPath);

        foreach (UnityEngine.Object asset in assets)
        {
            AnimatorController controller =
                asset as AnimatorController;

            if (controller != null &&
                controller.name.EndsWith("_AC", StringComparison.Ordinal))
            {
                animatorController = controller;
                continue;
            }

            AnimationClip clip = asset as AnimationClip;
            if (clip == null) continue;

            string clipName = GetClipNameWithoutPrefix(clip.name);

            switch (clipName)
            {
                case IDLE:
                    idleClip = clip;
                    break;
                case RUN:
                    runClip = clip;
                    break;
                case WALK:
                    walkClip = clip;
                    break;
                case ATTACK:
                    attackClip = clip;
                    break;
            }
        }

        AnimatorController droppedController =
            sourceAsset as AnimatorController;

        if (droppedController != null &&
            droppedController.name.EndsWith("_AC", StringComparison.Ordinal))
        {
            animatorController = droppedController;
        }

        // FBX/모델 파일 안에 애니메이션 클립만 들어 있고
        // Animator Controller가 별도 .controller 에셋인 경우를 지원합니다.
        if (animatorController == null)
        {
            animatorController = FindMatchingAnimatorController(assetPath);
        }

        Repaint();

        Debug.Log(
            "[Generic Animator] 드래그한 폴더 자동 인식 완료\n" +
            $"Controller : {(animatorController != null ? animatorController.name : "없음")}\n" +
            $"Idle : {(idleClip != null ? idleClip.name : "없음")}\n" +
            $"Run : {(runClip != null ? runClip.name : "없음")}\n" +
            $"Walk : {(walkClip != null ? walkClip.name : "없음")}\n" +
            $"Attack : {(attackClip != null ? attackClip.name : "없음")}"
        );
    }

    private UnityEngine.Object[] LoadDroppedAssetContents(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
        {
            string[] guids = AssetDatabase.FindAssets(
                string.Empty,
                new[] { assetPath }
            );

            System.Collections.Generic.List<UnityEngine.Object> contents =
                new System.Collections.Generic.List<UnityEngine.Object>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                contents.AddRange(
                    AssetDatabase.LoadAllAssetsAtPath(path)
                );
            }

            return contents.ToArray();
        }

        return AssetDatabase.LoadAllAssetsAtPath(assetPath);
    }

    private AnimatorController FindMatchingAnimatorController(string assetPath)
    {
        string sourceFolder =
            AssetDatabase.IsValidFolder(assetPath)
                ? assetPath
                : System.IO.Path.GetDirectoryName(assetPath).Replace("\\", "/");

        string sourceName = sourceAsset != null
            ? sourceAsset.name
            : System.IO.Path.GetFileNameWithoutExtension(assetPath);

        string normalizedSourceName = NormalizeAssetName(sourceName);
        AnimatorController fallback = null;

        string[] guids = AssetDatabase.FindAssets(
            "t:AnimatorController"
        );

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(path);

            if (controller == null ||
                !controller.name.EndsWith("_AC", StringComparison.OrdinalIgnoreCase))
                continue;

            string controllerBaseName = controller.name.Substring(
                0,
                controller.name.Length - 3
            );

            string normalizedControllerBase =
                NormalizeAssetName(controllerBaseName);

            bool sameFolder = path.StartsWith(
                sourceFolder + "/",
                StringComparison.OrdinalIgnoreCase
            ) || string.Equals(
                path,
                sourceFolder,
                StringComparison.OrdinalIgnoreCase
            );

            bool nameMatches =
                normalizedControllerBase == normalizedSourceName ||
                normalizedControllerBase.Contains(normalizedSourceName) ||
                normalizedSourceName.Contains(normalizedControllerBase);

            if (sameFolder && nameMatches)
                return controller;

            if (fallback == null && nameMatches)
                fallback = controller;
        }

        return fallback;
    }

    private string NormalizeAssetName(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace(" ", string.Empty)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .ToLowerInvariant();
    }

    private string GetClipNameWithoutPrefix(string clipName)
    {
        if (string.IsNullOrEmpty(clipName))
            return string.Empty;

        // Unity FBX/모델 클립 이름 예:
        // EBasicMob 1|Idle, EBasicMob 1_Idle, EBasicMob 1-Idle
        int separatorIndex = clipName.LastIndexOfAny(
            new[] { '|', '_', '-' }
        );

        if (separatorIndex >= 0 && separatorIndex < clipName.Length - 1)
            return clipName.Substring(separatorIndex + 1).Trim();

        string[] standardNames =
        {
            IDLE,
            RUN,
            WALK,
            ATTACK
        };

        foreach (string standardName in standardNames)
        {
            if (clipName.EndsWith(
                " " + standardName,
                StringComparison.OrdinalIgnoreCase
            ))
            {
                return standardName;
            }
        }

        return clipName.Trim();
    }

    private string NormalizeWhitespace(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        System.Text.StringBuilder builder =
            new System.Text.StringBuilder(value.Length);

        foreach (char character in value)
        {
            if (!char.IsWhiteSpace(character))
                builder.Append(character);
        }

        return builder.ToString();
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
            walkClip == null ||
            attackClip == null)
        {
            EditorUtility.DisplayDialog(
                "오류",
                "Animation Controller와 Idle, Run, Walk, Attack Clip이 들어 있는 폴더를\n" +
                "위의 폴더 칸에 드래그해 주세요.",
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

        AddOrUpdateParameter(
            IS_CHASING,
            AnimatorControllerParameterType.Bool
        );

        AnimatorState idle =
            GetOrCreateState(
                stateMachine,
                idleClip.name,
                idleClip,
                new Vector3(250, 100, 0)
            );

        AnimatorState run =
            GetOrCreateState(
                stateMachine,
                runClip.name,
                runClip,
                new Vector3(500, 100, 0)
            );

        AnimatorState walk =
            GetOrCreateState(
                stateMachine,
                walkClip.name,
                walkClip,
                new Vector3(375, 100, 0)
            );

        AnimatorState attack =
            GetOrCreateState(
                stateMachine,
                attackClip.name,
                attackClip,
                new Vector3(500, 250, 0)
            );

        stateMachine.defaultState = idle;

        ConfigureIdleToWalk(
            idle,
            walk
        );

        ConfigureIdleToRun(
            idle,
            run
        );

        ConfigureRunToWalk(
            run,
            walk
        );

        ConfigureRunToIdle(
            run,
            idle
        );

        ConfigureWalkToRun(
            walk,
            run
        );

        ConfigureWalkToIdle(
            walk,
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
            "Generic Animator 설정이 완료되었습니다.",
            "확인"
        );

        Debug.Log(
            "========== Generic Animator 설정 완료 ==========\n" +
            "Parameters : MoveSpeed, IsChasing, Attack\n" +
            "States : Idle, Walk, Run, Attack\n" +
            "Transitions : Idle ↔ Walk, Idle → Run, Walk ↔ Run, Run → Idle, Any State → Attack, Attack → Idle"
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
                        $"[Generic Animator] Parameter '{parameterName}'의 타입이 " +
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
            if (child.state.name == stateName ||
                NormalizeWhitespace(child.state.name) == NormalizeWhitespace(stateName))
            {
                // 이름이 완전히 같거나 공백만 다른 기존 State를 재사용합니다.
                // 기존 State 이름은 변경하지 않습니다.
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

    private void ConfigureIdleToWalk(
        AnimatorState idle,
        AnimatorState walk
    )
    {
        AnimatorStateTransition transition =
            GetOrCreateTransition(
                idle,
                walk
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

        ConfigureNormalTransition(transition, TRANSITION_DURATION);
        ClearConditions(transition);
        transition.AddCondition(
            AnimatorConditionMode.Greater,
            MOVE_THRESHOLD,
            MOVE_SPEED
        );
        transition.AddCondition(
            AnimatorConditionMode.If,
            0f,
            IS_CHASING
        );
    }

    private void ConfigureRunToWalk(
        AnimatorState run,
        AnimatorState walk
    )
    {
        AnimatorStateTransition transition =
            GetOrCreateTransition(
                run,
                walk
            );

        ConfigureNormalTransition(
            transition,
            TRANSITION_DURATION
        );

        SetSingleCondition(
            transition,
            AnimatorConditionMode.IfNot,
            IS_CHASING
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

        ClearConditions(
            transition
        );

        transition.AddCondition(
            AnimatorConditionMode.IfNot,
            0f,
            IS_CHASING
        );

        transition.AddCondition(
            AnimatorConditionMode.Less,
            MOVE_THRESHOLD,
            MOVE_SPEED
        );
    }

    private void ConfigureWalkToRun(
        AnimatorState walk,
        AnimatorState run
    )
    {
        AnimatorStateTransition transition =
            GetOrCreateTransition(
                walk,
                run
            );

        ConfigureNormalTransition(transition, TRANSITION_DURATION);
        SetSingleCondition(transition, AnimatorConditionMode.If, IS_CHASING);
    }

    private void ConfigureWalkToIdle(
        AnimatorState walk,
        AnimatorState idle
    )
    {
        AnimatorStateTransition transition =
            GetOrCreateTransition(
                walk,
                idle
            );

        ConfigureNormalTransition(transition, TRANSITION_DURATION);
        SetSingleCondition(transition, AnimatorConditionMode.Less, MOVE_SPEED, MOVE_THRESHOLD);
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
                "[Generic Animator] Animator Controller가 없습니다."
            );

            return;
        }

        AnimatorControllerParameter moveSpeed =
            FindParameter(MOVE_SPEED);

        AnimatorControllerParameter isChasing =
            FindParameter(IS_CHASING);

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

        if (isChasing == null)
        {
            Debug.LogError(
                "[ERROR] IsChasing Parameter 없음"
            );
        }
        else
        {
            Debug.Log(
                $"[OK] IsChasing : {isChasing.type}"
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
            idleClip != null ? idleClip.name : IDLE
        );

        ValidateState(
            stateMachine,
            runClip != null ? runClip.name : RUN
        );

        ValidateState(
            stateMachine,
            walkClip != null ? walkClip.name : WALK
        );

        ValidateState(
            stateMachine,
            attackClip != null ? attackClip.name : ATTACK
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
            if (child.state.name == stateName ||
                NormalizeWhitespace(child.state.name) == NormalizeWhitespace(stateName))
            {
                Debug.Log(
                    $"[OK] State : {child.state.name} (요청 이름: {stateName})"
                );

                return;
            }
        }

        Debug.LogError(
            $"[ERROR] State 없음 : {stateName}"
        );
    }
}