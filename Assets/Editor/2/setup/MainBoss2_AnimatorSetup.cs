#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.Collections.Generic;

public static class MainBoss2_AnimatorSetup
{
    const string Auto = "MainBoss2_Auto_";
    static readonly string[] Actions = { "Attack", "Skill1", "Skill2", "Skill3", "Buff" };
    [MenuItem("Tools/2/MainBoss2/Setup Animator")]
    public static void Setup()
    {
        try
        {
            var clips = new Dictionary<string, AnimationClip>();
            foreach (string name in new[] { "Idle", "Walk", "Run", "Run2", "Run3", "Attack", "Skill1", "Skill2", "Skill3", "Buff" })
                clips.Add(name, FindClip("MainBoss2_" + name));
            AnimatorController controller = null;
            foreach (string guid in AssetDatabase.FindAssets("MainBoss2_AC t:AnimatorController"))
            {
                var found = AssetDatabase.LoadAssetAtPath<AnimatorController>(AssetDatabase.GUIDToAssetPath(guid));
                if (found == null || found.name != "MainBoss2_AC") continue;
                if (controller != null && found != controller) throw new InvalidOperationException("MainBoss2_AC가 여러 개 있습니다.");
                controller = found;
            }
            if (controller == null) throw new InvalidOperationException("MainBoss2_AC를 찾을 수 없습니다.");
            var machine = controller.layers[0].stateMachine;
            var states = new Dictionary<string, AnimatorState>();
            int i = 0;
            foreach (var item in clips)
            {
                AnimatorState state = null;
                foreach (var child in machine.states) if (child.state.name == "MainBoss2_" + item.Key) state = child.state;
                if (state == null) state = machine.AddState("MainBoss2_" + item.Key, new Vector3(250+i%3*250,80+i/3*120));
                i++;
                state.motion = item.Value;
                state.writeDefaultValues = false;
                state.speed = 1f;
                bool loop = item.Key == "Idle" || item.Key == "Walk" || item.Key.StartsWith("Run");
                state.speedParameterActive = !loop;
                state.speedParameter = "AttackSpeed";
                var settings = AnimationUtility.GetAnimationClipSettings(item.Value);
                settings.loopTime = loop;
                settings.loopBlend = false;
                AnimationUtility.SetAnimationClipSettings(item.Value, settings);
                EditorUtility.SetDirty(item.Value);
                states.Add(item.Key, state);
            }
            Parameter(controller,"Phase",AnimatorControllerParameterType.Int);
            Parameter(controller,"MoveSpeed",AnimatorControllerParameterType.Float);
            Parameter(controller,"IsChasing",AnimatorControllerParameterType.Bool);
            Parameter(controller,"IsAttacking",AnimatorControllerParameterType.Bool);
            Parameter(controller,"AttackSpeed",AnimatorControllerParameterType.Float);
            foreach (string action in Actions) Parameter(controller,action,AnimatorControllerParameterType.Trigger);
            foreach(var transition in machine.anyStateTransitions) if(transition.name.StartsWith(Auto)) machine.RemoveAnyStateTransition(transition);
            foreach(var state in states.Values)
                foreach(var transition in state.transitions) if(transition.name.StartsWith(Auto)) state.RemoveTransition(transition);
            machine.defaultState = states["Idle"];
            string[] locomotion={"Idle","Walk","Run","Run2","Run3"};
            foreach(string from in locomotion) foreach(string to in locomotion)
            {
                if(from==to) continue;
                var transition=states[from].AddTransition(states[to]);
                Configure(transition,from+"To"+to,.1f);
                Conditions(transition,to);
            }
            foreach(string action in Actions)
            {
                var enter=machine.AddAnyStateTransition(states[action]);
                Configure(enter,"Enter"+action,.04f);
                enter.canTransitionToSelf=true;
                enter.AddCondition(AnimatorConditionMode.If,0,action);
                foreach(string to in locomotion)
                {
                    var exit=states[action].AddTransition(states[to]);
                    Configure(exit,action+"To"+to,.08f);
                    Conditions(exit,to);
                }
            }
            var selected=Selection.activeGameObject;
            if(selected!=null)
            {
                var boss=selected.GetComponentInParent<Stage2_MainBoss>();
                if(boss==null) boss=selected.GetComponentInChildren<Stage2_MainBoss>(true);
                if(boss!=null)
                {
                    var animator=boss.GetComponentInChildren<Animator>(true);
                    if(animator!=null)
                    {
                        Undo.RecordObject(animator,"Assign MainBoss2 Animator");
                        animator.runtimeAnimatorController=controller;
                        PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
                        EditorUtility.SetDirty(animator);
                    }
                }
            }
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(controller);
            Debug.Log("[MainBoss2] 설정 완료: Phase 1 Run / Phase 2 Run2 / Phase 3 Run3, 데이터 4스킬 → Skill3.");
        }
        catch(Exception error) { Debug.LogError("[MainBoss2] "+error.Message); }
    }
    static AnimationClip FindClip(string name)
    {
        AnimationClip found=null;
        foreach(string guid in AssetDatabase.FindAssets(name+" t:AnimationClip"))
            foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
                if(asset is AnimationClip clip && clip.name==name)
                {
                    if(found!=null&&found!=clip) throw new InvalidOperationException("클립 이름 중복: "+name);
                    found=clip;
                }
        if(found==null) throw new InvalidOperationException("클립을 찾을 수 없습니다: "+name);
        return found;
    }
    static void Parameter(AnimatorController controller,string name,AnimatorControllerParameterType type)
    {
        foreach(var p in controller.parameters)
            if(p.name==name) { if(p.type!=type) throw new InvalidOperationException("파라미터 타입 불일치: "+name); return; }
        controller.AddParameter(new AnimatorControllerParameter { name=name,type=type,defaultInt=name=="Phase"?1:0,defaultFloat=name=="AttackSpeed"?1:0 });
    }
    static void Configure(AnimatorStateTransition t,string name,float duration)
    {
        t.name=Auto+name; t.hasExitTime=false; t.hasFixedDuration=true; t.duration=duration;
        t.interruptionSource=TransitionInterruptionSource.None;
    }
    static void Conditions(AnimatorStateTransition t,string destination)
    {
        t.AddCondition(AnimatorConditionMode.IfNot,0,"IsAttacking");
        t.AddCondition(destination=="Idle"?AnimatorConditionMode.Less:AnimatorConditionMode.Greater,.05f,"MoveSpeed");
        if(destination!="Idle")
            t.AddCondition(destination.StartsWith("Run")?AnimatorConditionMode.If:AnimatorConditionMode.IfNot,0,"IsChasing");
        if(destination.StartsWith("Run"))
            t.AddCondition(AnimatorConditionMode.Equals,destination=="Run"?1:destination=="Run2"?2:3,"Phase");
    }
}
#endif
