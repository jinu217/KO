#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class Stage1CombatMovementSetup
{
    [MenuItem("Tools/Effects/Fixes/Register DeathScene")]
    public static void RegisterDeathScene()
    {
        var paths = AssetDatabase.FindAssets("DeathScene t:Scene").Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => Path.GetFileNameWithoutExtension(p) == "DeathScene").ToArray();
        if (paths.Length != 1) { Debug.LogError("DeathScene 씬이 없거나 동명 씬이 여러 개입니다. Build Profiles에서 사용할 씬을 직접 등록하세요."); return; }
        var scenes = EditorBuildSettings.scenes.ToList();
        var existing = scenes.Find(s => s.path == paths[0]);
        if (existing != null) existing.enabled = true;
        else scenes.Add(new EditorBuildSettingsScene(paths[0], true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log("DeathScene을 Shared Scene List에 등록했습니다. 활성 Build Profile이 Override Global Scene List를 사용하면 해당 프로필 목록에도 추가하세요: " + paths[0]);
    }

    [MenuItem("Tools/Effects/Fixes/Apply Stage1 Attack And Basic Walk")]
    public static void Apply()
    {
        const string folder = "Assets/Prefabs/Stage1_Enemy";
        if (!AssetDatabase.IsValidFolder(folder)) { Debug.LogError("폴더 없음: " + folder); return; }
        int count = 0;
        var modifiedWalks = new HashSet<AnimationClip>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                bool changed = false;
                foreach (var enemy in root.GetComponentsInChildren<EnemyController>(true))
                {
                    if (!(enemy is MeleeEnemy) && !(enemy is RangedEnemy)) continue;
                    var animator = enemy.GetComponentInChildren<Animator>(true);
                    if (animator == null) animator = enemy.GetComponentInParent<Animator>();
                    if (animator == null) continue;
                    var controller = animator.runtimeAnimatorController as AnimatorController;
                    if (controller == null) { Debug.LogWarning(path + ": 일반 AnimatorController가 아니므로 생략"); continue; }
                    var states = controller.layers.SelectMany(l => States(l.stateMachine)).ToArray();
                    var attacks = states.Where(s => s.motion is AnimationClip &&
                        (s.name.IndexOf("attack", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         s.motion.name.IndexOf("attack", StringComparison.OrdinalIgnoreCase) >= 0)).ToArray();
                    if (attacks.Length == 0) { Debug.LogWarning(path + ": Attack 클립을 찾지 못함"); continue; }
                    Backup(AssetDatabase.GetAssetPath(controller));
                    var so = new SerializedObject(enemy);
                    so.FindProperty("waitForAttackAnimation").boolValue = true;
                    var clips = so.FindProperty("movementLockAttackClips");
                    clips.arraySize = attacks.Length;
                    for (int i = 0; i < attacks.Length; i++)
                    {
                        var state = attacks[i];
                        var clip = (AnimationClip)state.motion;
                        clips.GetArrayElementAtIndex(i).objectReferenceValue = clip;
                        Backup(AssetDatabase.GetAssetPath(clip));
                        var settings = AnimationUtility.GetAnimationClipSettings(clip);
                        settings.loopTime = false;
                        AnimationUtility.SetAnimationClipSettings(clip, settings);
                        EditorUtility.SetDirty(clip);
                        foreach (var transition in state.transitions)
                        {
                            string dest = transition.destinationState != null ? transition.destinationState.name : "";
                            if (dest.IndexOf("hit", StringComparison.OrdinalIgnoreCase) >= 0 || dest.IndexOf("death", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                            transition.hasExitTime = true;
                            transition.exitTime = 1f;
                            transition.hasFixedDuration = true;
                            transition.duration = .06f;
                            EditorUtility.SetDirty(transition);
                        }
                    }
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(controller);
                    // 실제 런타임 프리팹 이름 Stage1_Enemy_1 및 시각 프리팹 BasicMob 1 지원.
                    bool basic = Path.GetFileNameWithoutExtension(path) == "Stage1_Enemy_1" ||
                        root.name.Replace(" ", "").Equals("BasicMob1", StringComparison.OrdinalIgnoreCase);
                    if (basic)
                        foreach (var state in states.Where(s => s.motion is AnimationClip &&
                            s.name.IndexOf("walk", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            var walk = (AnimationClip)state.motion;
                            if (modifiedWalks.Add(walk)) RestArms(animator, walk);
                        }
                    if (basic)
                        foreach (var state in states.Where(s => s.motion is AnimationClip &&
                            (s.name.IndexOf("run", StringComparison.OrdinalIgnoreCase) >= 0 || s.name.IndexOf("chase", StringComparison.OrdinalIgnoreCase) >= 0)))
                        {
                            var run = (AnimationClip)state.motion;
                            if (states.Any(w => w != state && w.motion == run && w.name.IndexOf("walk", StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                string runPath = Path.ChangeExtension(AssetDatabase.GetAssetPath(run), null) + "_SwordRaised.anim";
                                var separate = AssetDatabase.LoadAssetAtPath<AnimationClip>(runPath);
                                if (separate == null) { separate = UnityEngine.Object.Instantiate(run); separate.name = run.name + "_SwordRaised"; AssetDatabase.CreateAsset(separate, runPath); }
                                state.motion = separate;
                                EditorUtility.SetDirty(state);
                                run = separate;
                            }
                            RaiseRunSword(animator, run);
                        }
                    changed = true;
                    count++;
                }
                if (changed) { Backup(path); PrefabUtility.SaveAsPrefabAsset(root, path); }
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Stage1 공격 종료까지 이동 대기 적용: " + count + "개 / 기본몹 Walk 수정: " + modifiedWalks.Count + "개");
    }
    static IEnumerable<AnimatorState> States(AnimatorStateMachine machine)
    {
        foreach (var child in machine.states) yield return child.state;
        foreach (var child in machine.stateMachines)
            foreach (var state in States(child.stateMachine)) yield return state;
    }
    static void RestArms(Animator animator, AnimationClip walk)
    {
        Backup(AssetDatabase.GetAssetPath(walk));
        float duration = Mathf.Max(walk.length, .1f);
        foreach (var binding in AnimationUtility.GetCurveBindings(walk))
            if (binding.type == typeof(Transform) && IsArmPath(binding.path))
                AnimationUtility.SetEditorCurve(walk, binding, null);
        foreach (var part in animator.GetComponentsInChildren<Transform>(true))
        {
            string path = AnimationUtility.CalculateTransformPath(part, animator.transform);
            if (!IsArmPath(path)) continue;
            Vector3 rot = part.localEulerAngles;
            for (int axis = 0; axis < 3; axis++)
            {
                string suffix = "." + "xyz"[axis];
                Constant(walk, path, "m_LocalPosition" + suffix, part.localPosition[axis], duration);
                Constant(walk, path, "m_LocalScale" + suffix, part.localScale[axis], duration);
                Constant(walk, path, "localEulerAnglesRaw" + suffix, Mathf.DeltaAngle(0, rot[axis]), duration);
            }
        }
        EditorUtility.SetDirty(walk);
    }
    static void RaiseRunSword(Animator animator, AnimationClip run)
    {
        var hand = animator.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Hand_B");
        if (hand == null) { Debug.LogWarning("BasicMob1 Run: Hand_B를 찾지 못함"); return; }
        Backup(AssetDatabase.GetAssetPath(run));
        string path = AnimationUtility.CalculateTransformPath(hand, animator.transform);
        foreach (var binding in AnimationUtility.GetCurveBindings(run))
            if (binding.path == path && binding.type == typeof(Transform) &&
                (binding.propertyName.Contains("Rotation") || binding.propertyName.Contains("Euler")))
                AnimationUtility.SetEditorCurve(run, binding, null);
        for (int axis = 0; axis < 3; axis++)
            Constant(run, path, "localEulerAnglesRaw." + "xyz"[axis], axis == 2 ? 75f : Mathf.DeltaAngle(0, hand.localEulerAngles[axis]), Mathf.Max(.1f, run.length));
        EditorUtility.SetDirty(run);
    }
    static bool IsArmPath(string path) => path.Split('/').Any(p => p.StartsWith("Shoulder", StringComparison.OrdinalIgnoreCase));
    static void Constant(AnimationClip clip, string path, string property, float value, float duration)
    {
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property),
            AnimationCurve.Linear(0, value, duration, value));
    }
    static void Backup(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
        string target = "Library/CombatMovementBackups/" + path;
        if (File.Exists(target)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(target));
        File.Copy(path, target);
    }
}
#endif
