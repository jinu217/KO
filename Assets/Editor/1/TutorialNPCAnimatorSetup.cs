#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class TutorialNPCAnimatorSetup
{
    [MenuItem("Tools/Tutorial NPC/Setup Mentor Animator")]
    public static void Mentor() { Setup("Mentor", new[] { "Thrust" }); }
    [MenuItem("Tools/Tutorial NPC/Setup Daughter Animator")]
    public static void Daughter() { Setup("Daughter", new[] { "Skip", "Smile", "Wave" }); }

    static T Find<T>(string name) where T : UnityEngine.Object
    {
        var paths = AssetDatabase.FindAssets(name + " t:" + typeof(T).Name)
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => Path.GetFileNameWithoutExtension(p) == name).ToArray();
        if (paths.Length != 1) throw new InvalidOperationException(name + ": 정확히 하나의 에셋이 필요합니다. 검색 결과 " + paths.Length);
        return AssetDatabase.LoadAssetAtPath<T>(paths[0]);
    }
    static void Setup(string prefix, string[] actions)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        try
        {
            var controller = Find<AnimatorController>(prefix + "_AC");
            var names = new[] { "Idle", "Walk" }.Concat(actions).ToArray();
            var clips = names.Select(n => Find<AnimationClip>(prefix + "_" + n)).ToArray();
            var sm = controller.layers[0].stateMachine;
            if (sm.stateMachines.Length != 0) throw new InvalidOperationException("Base Layer에 하위 상태 머신이 있습니다. 자동 교체를 중단했습니다.");
            Undo.RegisterCompleteObjectUndo(controller, "Setup NPC Animator");
            foreach (var t in sm.anyStateTransitions) sm.RemoveAnyStateTransition(t);
            foreach (var t in sm.entryTransitions) sm.RemoveEntryTransition(t);
            foreach (var s in sm.states)
                foreach (var t in s.state.transitions) s.state.RemoveTransition(t);
            Parameter(controller, "MoveSpeed", AnimatorControllerParameterType.Float);
            var states = new AnimatorState[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                string stateName = prefix + "_" + names[i];
                states[i] = sm.states.Select(s => s.state).FirstOrDefault(s => s.name == stateName)
                    ?? sm.AddState(stateName);
                states[i].motion = clips[i];
                states[i].speed = 1f;
                states[i].speedParameterActive = false;
                states[i].timeParameterActive = false;
                states[i].cycleOffsetParameterActive = false;
                states[i].writeDefaultValues = true;
                // Old randomizer can repeatedly seek the same state. NPC idle uses a normal loop.
                foreach (var b in states[i].behaviours)
                    if (b != null && b.GetType().Name == "RandomizeIdleStart") UnityEngine.Object.DestroyImmediate(b, true);
                var settings = AnimationUtility.GetAnimationClipSettings(clips[i]);
                settings.loopTime = i < 2;
                AnimationUtility.SetAnimationClipSettings(clips[i], settings);
                EditorUtility.SetDirty(clips[i]);
            }
            sm.defaultState = states[0];
            Transition(states[0], states[1], false, AnimatorConditionMode.Greater, .01f, "MoveSpeed");
            Transition(states[1], states[0], false, AnimatorConditionMode.Less, .01f, "MoveSpeed");
            for (int i = 2; i < states.Length; i++)
            {
                string trigger = names[i] == "Thrust" ? "Attack" : names[i];
                Parameter(controller, trigger, AnimatorControllerParameterType.Trigger);
                // Only locomotion can start an action: no self re-entry or action interruption.
                Transition(states[0], states[i], false, AnimatorConditionMode.If, 0, trigger);
                Transition(states[1], states[i], false, AnimatorConditionMode.If, 0, trigger);
                Transition(states[i], states[1], true, AnimatorConditionMode.Greater, .01f, "MoveSpeed");
                var exit = states[i].AddTransition(states[0]);
                Configure(exit, true); // unconditional fallback after the conditional Walk exit
            }
            var children = sm.states;
            for (int i = 0; i < children.Length; i++)
            {
                int index = Array.IndexOf(states, children[i].state);
                if (index >= 0) children[i].position = index == 0 ? new Vector3(280, 100) :
                    index == 1 ? new Vector3(50, 300) : new Vector3(530, 220 + (index - 2) * 130);
            }
            sm.states = children;
            sm.entryPosition = new Vector3(280, -30);
            sm.anyStatePosition = new Vector3(30, -30);
            EditorUtility.SetDirty(sm);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Selection.activeObject = controller;
            Debug.Log(prefix + " Animator 설정 완료. NPC의 Body 바로 위 오브젝트 Animator에 " + controller.name + "를 연결하세요.");
        }
        catch (Exception ex) { Debug.LogError(ex.Message); }
    }
    static void Parameter(AnimatorController c, string name, AnimatorControllerParameterType type)
    {
        var existing = c.parameters.FirstOrDefault(p => p.name == name);
        if (existing != null && existing.type != type) c.RemoveParameter(Array.FindIndex(c.parameters, p => p.name == name));
        if (!c.parameters.Any(p => p.name == name)) c.AddParameter(name, type);
    }
    static void Configure(AnimatorStateTransition t, bool exit)
    {
        t.hasExitTime = exit; t.exitTime = 1f;
        t.hasFixedDuration = true; t.duration = .06f;
        t.canTransitionToSelf = false;
        t.interruptionSource = TransitionInterruptionSource.None;
    }
    static void Transition(AnimatorState a, AnimatorState b, bool exit, AnimatorConditionMode mode, float threshold, string param)
    {
        var t = a.AddTransition(b); Configure(t, exit); t.AddCondition(mode, threshold, param);
    }
}
#endif
