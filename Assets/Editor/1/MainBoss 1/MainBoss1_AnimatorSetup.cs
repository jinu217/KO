#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class MainBoss1_AnimatorSetup
{
    private const string Prefix = "MainBoss1_";
    private const string Auto = "MainBoss1_Auto_";
    private static readonly string[] Actions = { "Attack", "skill1", "skill2", "skill3", "skill4", "skill5", "Dskill", "P2E", "P3E" };

    [MenuItem("Tools/1/MainBoss 1/Setup Animator")]
    public static void Setup()
    {
        try
        {
            var clips = new Dictionary<string, AnimationClip>();
            foreach (string action in new[] { "Idle", "Idle2", "Walk", "Chase", "Attack", "skill1", "skill2", "skill3", "skill4", "skill5", "Dskill", "P2E", "P3E" })
                clips.Add(action, FindClip(Prefix + action));
            const string controllerName = "MainBoss 1_AC";
            AnimatorController controller = null;
            foreach (string guid in AssetDatabase.FindAssets(controllerName + " t:AnimatorController"))
            {
                var candidate = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GUIDToAssetPath(guid));
                if (candidate == null || candidate.name != controllerName) continue;
                if (controller != null && controller != candidate)
                    throw new InvalidOperationException("동일 이름의 컨트롤러가 여러 개 있습니다: " + controllerName);
                controller = candidate;
            }
            if (controller == null)
                throw new InvalidOperationException("기존 Animator Controller를 찾을 수 없습니다: " + controllerName);
            string path = AssetDatabase.GetAssetPath(controller);
            clips = MainBoss1_ClipPreparation.Prepare(clips, path);
            Undo.RegisterCompleteObjectUndo(controller, "Setup MainBoss1 Animator");
            var machine = controller.layers[0].stateMachine;
            var states = new Dictionary<string, AnimatorState>();
            int index = 0;
            foreach (var pair in clips)
            {
                AnimatorState state = null;
                foreach (var child in machine.states)
                    if (child.state.name == Prefix + pair.Key) state = child.state;
                if (state == null) state = machine.AddState(Prefix + pair.Key, new Vector3(280 + index % 3 * 250, 80 + index / 3 * 120));
                index++;
                state.motion = pair.Value;
                state.writeDefaultValues = false;
                state.speed = 1f;
                state.speedParameterActive = pair.Key != "Idle" && pair.Key != "Idle2" && pair.Key != "P2E" && pair.Key != "P3E";
                if (pair.Key == "Walk" || pair.Key == "Chase")
                {
                    state.speedParameterActive = true;
                    state.speedParameter = "LocomotionSpeed";
                }
                else state.speedParameter = "AttackSpeed";
                states.Add(pair.Key, state);
                var settings = AnimationUtility.GetAnimationClipSettings(pair.Value);
                settings.loopTime = pair.Key == "Idle" || pair.Key == "Idle2" || pair.Key == "Walk" || pair.Key == "Chase";
                settings.loopBlend = false;
                AnimationUtility.SetAnimationClipSettings(pair.Value, settings);
                EditorUtility.SetDirty(pair.Value);
            }
            AddParameter(controller, "Phase", AnimatorControllerParameterType.Int);
            AddParameter(controller, "IsAttacking", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "AttackSpeed", AnimatorControllerParameterType.Float);
            AddParameter(controller, "MoveSpeed", AnimatorControllerParameterType.Float);
            AddParameter(controller, "IsChasing", AnimatorControllerParameterType.Bool);
            AddParameter(controller, "LocomotionSpeed", AnimatorControllerParameterType.Float);
            foreach (string action in Actions) AddParameter(controller, action, AnimatorControllerParameterType.Trigger);
            foreach (var transition in machine.anyStateTransitions)
                if (transition.name.StartsWith(Auto, StringComparison.Ordinal)) machine.RemoveAnyStateTransition(transition);
            foreach (var state in states.Values)
                foreach (var transition in state.transitions)
                    if (transition.name.StartsWith(Auto, StringComparison.Ordinal)) state.RemoveTransition(transition);
            machine.defaultState = states["Idle"];
            string[] locomotion = { "Idle", "Idle2", "Walk", "Chase" };
            foreach (string from in locomotion)
                foreach (string to in locomotion)
                {
                    if (from == to) continue;
                    var transition = states[from].AddTransition(states[to]);
                    Configure(transition, from + "To" + to, false);
                    transition.duration = 0.12f;
                    AddLocomotionConditions(transition, to);
                }
            foreach (string action in Actions)
            {
                bool phaseEntry = action == "P2E" || action == "P3E";
                foreach (string stateKey in phaseEntry ? new[] { action } : new[] { action, action + "_Phase3" })
                {
                    var enter = machine.AddAnyStateTransition(states[stateKey]);
                    Configure(enter, "Enter_" + stateKey, false);
                    enter.canTransitionToSelf = true;
                    enter.AddCondition(AnimatorConditionMode.If, 0, action);
                    if (!phaseEntry)
                        enter.AddCondition(stateKey.EndsWith("_Phase3") ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less,
                            stateKey.EndsWith("_Phase3") ? 2 : 3, "Phase");
                    foreach (string idle in locomotion)
                    {
                        var exit = states[stateKey].AddTransition(states[idle]);
                        // 컨트롤러가 모션 종료까지 기다린 뒤 IsAttacking을 해제합니다.
                        Configure(exit, stateKey + "To" + idle, false);
                        AddLocomotionConditions(exit, idle);
                    }
                }
            }
            // 선택한 보스의 기존 Animator 위치를 유지합니다. 임의로 루트에 추가하지 않습니다.
            GameObject selected = Selection.activeGameObject;
            if (selected != null)
            {
                var boss = selected.GetComponentInParent<MainBossController>();
                if (boss == null) boss = selected.GetComponentInChildren<MainBossController>(true);
                if (boss != null)
                {
                    var animator = boss.GetComponentInChildren<Animator>(true);
                    if (animator != null)
                    {
                        Undo.RecordObject(animator, "Assign MainBoss1 Animator");
                        animator.runtimeAnimatorController = controller;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                        EditorUtility.SetDirty(animator);
                    }
                }
            }
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(controller);
            Debug.Log("[MainBoss1] Animator 설정 완료: " + path + ". 3페이즈 진입 완료 후 Idle2 사용.");
        }
        catch (Exception error) { Debug.LogError("[MainBoss1] " + error.Message); }
    }

    private static AnimationClip FindClip(string name)
    {
        AnimationClip found = null;
        foreach (string guid in AssetDatabase.FindAssets(name + " t:AnimationClip"))
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                if (asset is AnimationClip clip && clip.name == name)
                {
                    if (found != null && found != clip) throw new InvalidOperationException("동일 이름의 클립이 여러 개 있습니다: " + name);
                    found = clip;
                }
        if (found == null) throw new InvalidOperationException("클립을 찾을 수 없습니다: " + name);
        return found;
    }

    private static void AddParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (var parameter in controller.parameters)
        {
            if (parameter.name != name) continue;
            if (parameter.type != type) throw new InvalidOperationException("파라미터 타입 불일치: " + name);
            return;
        }
        controller.AddParameter(new AnimatorControllerParameter
        {
            name = name,
            type = type,
            defaultInt = name == "Phase" ? 1 : 0,
            defaultFloat = name == "AttackSpeed" || name == "LocomotionSpeed" ? 1f : 0f
        });
    }

    private static void AddLocomotionConditions(AnimatorStateTransition transition, string destination)
    {
        transition.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
        bool moving = destination == "Walk" || destination == "Chase";
        transition.AddCondition(moving ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less, 0.05f, "MoveSpeed");
        if (moving)
            transition.AddCondition(destination == "Chase" ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "IsChasing");
        else
            transition.AddCondition(destination == "Idle2" ? AnimatorConditionMode.Greater : AnimatorConditionMode.Less,
                destination == "Idle2" ? 2 : 3, "Phase");
    }

    private static void Configure(AnimatorStateTransition transition, string name, bool exitTime)
    {
        transition.name = Auto + name;
        transition.hasExitTime = exitTime;
        transition.exitTime = 1f;
        transition.hasFixedDuration = true;
        transition.duration = 0.04f;
        transition.interruptionSource = TransitionInterruptionSource.None;
    }
}
#endif
