#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Existing locomotion/attack clips and transitions are retained.
public static class MobHitDeathSetup
{
    private const string Menu = "Tools/Mob/Hit Death/";
    private const string Prefix = "MobReaction_";
    private static readonly string[] Kinds = { "BasicMob", "SpeedMob", "TankMob", "EBasicMob", "EArcher" };
    [MenuItem(Menu + "Setup Stage 1 and 2")]
    public static void SetupBoth() { Batch(new[] { 1, 2 }); }
    [MenuItem(Menu + "Setup Stage 1")]
    public static void SetupStage1() { Batch(new[] { 1 }); }
    [MenuItem(Menu + "Setup Stage 2")]
    public static void SetupStage2() { Batch(new[] { 2 }); }
    [MenuItem(Menu + "Connect Existing Stage 3")]
    public static void SetupStage3() { Batch(new[] { 3 }, true); }

    private static void Batch(int[] stages, bool keepClips = false)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        int done = 0; var errors = new List<string>();
        try
        {
            foreach (int stage in stages) foreach (string kind in Kinds)
                {
                    string mob = kind + " " + stage;
                    EditorUtility.DisplayProgressBar("Hit / Death 설정", mob, 0.5f);
                    try { SetupMob(stage, mob, keepClips); done++; }
                    catch (Exception ex) { errors.Add(mob + ": " + ex.Message); }
                }
        }
        finally { EditorUtility.ClearProgressBar(); AssetDatabase.SaveAssets(); }
        Debug.Log("Hit / Death 완료: " + done + "종" + (errors.Count == 0 ? "" : "\n생략/오류:\n" + string.Join("\n", errors)));
    }

    public static void SetupMob(int stage, string mob, bool keepClips = false)
    {
        string[] roots = AssetDatabase.GetSubFolders("Assets").Where(p => string.Equals(Path.GetFileName(p), "Prefab", StringComparison.OrdinalIgnoreCase)).ToArray();
        if (roots.Length != 1) throw new InvalidOperationException("Assets/Prefab 폴더를 확인하세요.");
        string folder = roots[0] + "/" + stage;
        if (!AssetDatabase.IsValidFolder(folder)) throw new InvalidOperationException("폴더 없음: " + folder);
        string[] paths = AssetDatabase.FindAssets("t:Prefab", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => string.Equals(Path.GetFileNameWithoutExtension(p).Replace(" ", ""), mob.Replace(" ", ""), StringComparison.Ordinal)).ToArray();
        if (paths.Length != 1) throw new InvalidOperationException("동일 이름의 프리팹이 정확히 하나 필요합니다: " + mob);
        GameObject root = null, preview = null;
        try
        {
            root = PrefabUtility.LoadPrefabContents(paths[0]);
            var enemy = root.GetComponentInChildren<EnemyController>(true);
            // Named prefab can be the visual child of a separately placed AI root.
            // Target filtering is already handled by the explicit normal-mob names.
            var animator = enemy != null ? enemy.GetComponentInChildren<Animator>(true)
                : root.GetComponentInChildren<Animator>(true);
            if (animator == null) throw new InvalidOperationException("Animator 없음");
            var controller = animator.runtimeAnimatorController as AnimatorController;
            if (controller == null) throw new InvalidOperationException("기존 AnimatorController를 먼저 연결하세요. Override Controller는 자동 변경하지 않습니다.");
            var sm = controller.layers[0].stateMachine;
            var idle = sm.states.Select(x => x.state).FirstOrDefault(x => x.name.EndsWith("Idle", StringComparison.OrdinalIgnoreCase));
            if (idle == null || !(idle.motion is AnimationClip)) throw new InvalidOperationException("Base Layer의 Idle AnimationClip을 찾지 못했습니다.");
            AnimationClip idleClip = (AnimationClip)idle.motion;
            RequireParameter(controller, "Hit", AnimatorControllerParameterType.Trigger);
            RequireParameter(controller, "IsDead", AnimatorControllerParameterType.Bool);
            string clipFolder = Path.GetDirectoryName(AssetDatabase.GetAssetPath(idleClip)).Replace('\\', '/');
            string name = mob.Replace(" ", "");
            string hitName = name + "_Hit", deathName = name + "_Death";
            AnimationClip hit = FindClip(sm, hitName), death = FindClip(sm, deathName);
            if (!keepClips || hit == null || death == null)
            {
                preview = UnityEngine.Object.Instantiate(root);
                preview.hideFlags = HideFlags.HideAndDontSave;
                foreach (var behaviour in preview.GetComponentsInChildren<Behaviour>(true)) behaviour.enabled = false;
                string animatorPath = AnimationUtility.CalculateTransformPath(animator.transform, root.transform);
                Transform previewAnimator = string.IsNullOrEmpty(animatorPath) ? preview.transform : preview.transform.Find(animatorPath);
                idleClip.SampleAnimation(previewAnimator.gameObject, 0f);
                Transform body = previewAnimator.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "Body");
                if (body == null) throw new InvalidOperationException("Body 하이어라키 없음");
                var rig = new Rig(previewAnimator, body);
                if (!keepClips || hit == null) hit = MakeClip(rig, clipFolder, hitName, false);
                if (!keepClips || death == null) death = MakeClip(rig, clipFolder, deathName, true);
            }
            AnimatorState hitState = State(sm, hitName, new Vector3(-280, 220, 0));
            AnimatorState deathState = State(sm, deathName, new Vector3(-280, -30, 0));
            hitState.motion = hit; deathState.motion = death;
            foreach (var state in new[] { hitState, deathState })
            {
                state.speed = 1f; state.speedParameterActive = false; state.writeDefaultValues = false;
                foreach (var tr in state.transitions) state.RemoveTransition(tr);
                var settings = AnimationUtility.GetAnimationClipSettings((AnimationClip)state.motion);
                settings.loopTime = false; AnimationUtility.SetAnimationClipSettings((AnimationClip)state.motion, settings);
                EditorUtility.SetDirty(state.motion);
            }
            foreach (var tr in sm.anyStateTransitions)
                if (tr.destinationState == hitState || tr.destinationState == deathState) sm.RemoveAnyStateTransition(tr);
            // A death flag must also block legacy Any State -> Attack routes.
            GuardMachine(sm, deathState);
            var dt = sm.AddAnyStateTransition(deathState); Configure(dt, false); dt.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            var ht = sm.AddAnyStateTransition(hitState); Configure(ht, false);
            ht.AddCondition(AnimatorConditionMode.If, 0, "Hit"); ht.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            sm.anyStateTransitions = new[] { dt, ht }.Concat(sm.anyStateTransitions.Where(x => x != dt && x != ht)).ToArray();
            var recovery = hitState.AddTransition(idle); Configure(recovery, true);
            recovery.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            if (enemy != null)
            {
                var so = new SerializedObject(enemy);
                Set(so, "hitAnimation", hit); Set(so, "deathAnimation", death);
                so.FindProperty("hitAnimationState").stringValue = hitState.name;
                so.FindProperty("deathAnimationState").stringValue = deathState.name;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            // With a visual-only prefab the updated EnemyController on the parent
            // finds the generated _Hit/_Death clips and matching states at runtime.
            EditorUtility.SetDirty(controller); EditorUtility.SetDirty(sm);
            bool saved; PrefabUtility.SaveAsPrefabAsset(root, paths[0], out saved);
            if (!saved) throw new IOException("프리팹 저장 실패: " + paths[0]);
            AssetDatabase.SaveAssets();
        }
        finally
        {
            if (preview != null) UnityEngine.Object.DestroyImmediate(preview);
            if (root != null) PrefabUtility.UnloadPrefabContents(root);
        }
    }
    private static void Set(SerializedObject so, string key, UnityEngine.Object value)
    {
        var p = so.FindProperty(key);
        if (p == null) throw new InvalidOperationException("새 EnemyController.cs를 먼저 교체하세요: " + key);
        p.objectReferenceValue = value;
    }
    private static AnimationClip FindClip(AnimatorStateMachine sm, string name)
        => sm.states.Select(s => s.state).Where(s => s.name == name).Select(s => s.motion as AnimationClip).FirstOrDefault();
    private static AnimatorState State(AnimatorStateMachine sm, string name, Vector3 position)
        => sm.states.Select(s => s.state).FirstOrDefault(s => s.name == name) ?? sm.AddState(name, position);
    private static void RequireParameter(AnimatorController c, string name, AnimatorControllerParameterType type)
    {
        var p = c.parameters.FirstOrDefault(x => x.name == name);
        if (p == null) c.AddParameter(name, type);
        else if (p.type != type) throw new InvalidOperationException(name + " 파라미터 타입 충돌");
    }
    private static void GuardMachine(AnimatorStateMachine sm, AnimatorState death)
    {
        foreach (var tr in sm.anyStateTransitions) Guard(tr, death);
        foreach (var child in sm.states) foreach (var tr in child.state.transitions) Guard(tr, death);
        foreach (var child in sm.stateMachines) GuardMachine(child.stateMachine, death);
    }
    private static void Guard(AnimatorStateTransition tr, AnimatorState death)
    {
        if (tr.destinationState == death || tr.conditions.Any(c => c.parameter == "IsDead")) return;
        tr.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
    }
    private static void Configure(AnimatorStateTransition tr, bool exit)
    {
        tr.name = Prefix + tr.destinationState.name; tr.hasExitTime = exit; tr.exitTime = 1f;
        tr.hasFixedDuration = true; tr.duration = exit ? .04f : 0f;
        tr.canTransitionToSelf = false; tr.interruptionSource = TransitionInterruptionSource.None;
    }

    private sealed class Rig
    {
        public Transform root, body;
        public Transform[] bones;
        public Vector3[] pos, scale;
        public Quaternion[] rot;
        public float[] z;
        public string[] paths;
        public Dictionary<string, Transform> named = new Dictionary<string, Transform>();
        public Dictionary<Transform, Vector3> feet = new Dictionary<Transform, Vector3>();
        public Dictionary<Transform, Quaternion> footRotations = new Dictionary<Transform, Quaternion>();
        public float size;
        public Rig(Transform root, Transform body)
        {
            this.root = root; this.body = body; bones = body.GetComponentsInChildren<Transform>(true);
            pos = bones.Select(x => x.localPosition).ToArray(); scale = bones.Select(x => x.localScale).ToArray();
            rot = bones.Select(x => x.localRotation).ToArray();
            z = bones.Select(x => Mathf.DeltaAngle(0, x.localEulerAngles.z)).ToArray();
            paths = bones.Select(x => AnimationUtility.CalculateTransformPath(x, root)).ToArray();
            foreach (var t in bones) if (!named.ContainsKey(t.name)) named.Add(t.name, t);
            foreach (string n in new[] { "Foot_F", "Foot_B" })
                if (named.ContainsKey(n)) { feet[named[n]] = root.InverseTransformPoint(named[n].position); footRotations[named[n]] = named[n].rotation; }
            if (feet.Count != 2) throw new InvalidOperationException("Foot_F/Foot_B가 필요합니다.");
            size = named.ContainsKey("Head") ? Mathf.Clamp(Vector3.Distance(body.position, named["Head"].position) / .387f, .7f, 2.5f) : 1f;
        }
        public void Reset() { for (int i = 0; i < bones.Length; i++) { bones[i].localPosition = pos[i]; bones[i].localRotation = rot[i]; bones[i].localScale = scale[i]; } }
        public void Turn(string name, float delta)
        {
            if (!named.TryGetValue(name, out var t)) return;
            int i = Array.IndexOf(bones, t); t.localRotation = Quaternion.Euler(0, 0, z[i] + delta);
        }
        public void Pose(float t, bool death)
        {
            Reset();
            float kick = death ? Smooth((t - .05f) / .43f) : t < .06f ? Smooth(t / .06f) : 1 - Smooth((t - .06f) / .24f);
            float settle = death ? Smooth((t - .48f) / .17f) : 0;
            body.localPosition += new Vector3((death ? -.19f : -.055f) * size * kick, ((death ? -.30f : -.015f) * kick - .025f * settle) * size, 0);
            Turn("Body", (death ? 72f : 10f) * kick - 4f * settle);
            Turn("Head", (death ? 12f : 5f) * kick); Turn("Shoulder_F", (death ? -22f : -12f) * kick);
            Turn("Hand_F", (death ? 18f : 8f) * kick); Turn("Shoulder_B", (death ? -28f : -7f) * kick);
            Turn("Hand_B", (death ? -35f : 10f) * kick); Turn("Bottom", (death ? -22f : -6f) * kick);
            foreach (var pair in feet)
            {
                bool front = pair.Key.name == "Foot_F";
                Vector3 target = pair.Value + new Vector3(death ? (front ? -.06f : .06f) * size * kick : 0, 0, 0);
                pair.Key.position = root.TransformPoint(target);
                // Same simple grounded-foot construction as the stage3 normal mobs.
                pair.Key.rotation = footRotations[pair.Key] * Quaternion.Euler(0, 0, death ? (front ? 18f : -12f) * kick : 0);
            }
        }
    }
    private static float Smooth(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }
    private static AnimationClip MakeClip(Rig rig, string folder, string name, bool death)
    {
        float length = death ? 0.65f : 0.30f;
        var clip = new AnimationClip { name = name, frameRate = 60 };
        var times = new SortedSet<float> { 0, length, .06f };
        if (death) { times.Add(.05f); times.Add(.48f); }
        for (int f = 0; f <= Mathf.CeilToInt(length * 60); f++) times.Add(Mathf.Min(length, f / 60f));
        var curves = new AnimationCurve[rig.bones.Length, 9];
        for (int i = 0; i < rig.bones.Length; i++) for (int c = 0; c < 9; c++) curves[i, c] = new AnimationCurve();
        float[] previous = (float[])rig.z.Clone();
        foreach (float t in times)
        {
            rig.Pose(t, death);
            for (int i = 0; i < rig.bones.Length; i++)
            {
                var bone = rig.bones[i]; float angle = previous[i] + Mathf.DeltaAngle(previous[i], bone.localEulerAngles.z); previous[i] = angle;
                float[] values = { bone.localPosition.x, bone.localPosition.y, bone.localPosition.z, 0, 0, angle, bone.localScale.x, bone.localScale.y, bone.localScale.z };
                for (int c = 0; c < 9; c++) curves[i, c].AddKey(t, values[c]);
            }
        }
        string[] properties = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "localEulerAnglesRaw.x", "localEulerAnglesRaw.y", "localEulerAnglesRaw.z", "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z" };
        for (int i = 0; i < rig.bones.Length; i++) for (int c = 0; c < 9; c++)
            {
                var curve = curves[i, c];
                for (int k = 0; k < curve.length; k++) { AnimationUtility.SetKeyLeftTangentMode(curve, k, AnimationUtility.TangentMode.Linear); AnimationUtility.SetKeyRightTangentMode(curve, k, AnimationUtility.TangentMode.Linear); }
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(rig.paths[i], typeof(Transform), properties[c]), curve);
            }
        rig.Reset();
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = false; AnimationUtility.SetAnimationClipSettings(clip, settings);
        string path = folder + "/" + name + ".anim";
        var old = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (old != null) { EditorUtility.CopySerialized(clip, old); UnityEngine.Object.DestroyImmediate(clip); EditorUtility.SetDirty(old); return old; }
        AssetDatabase.CreateAsset(clip, path); return clip;
    }
}
#endif
