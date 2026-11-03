#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Samples isolated prefab contents; never saves or changes the user's open scene.
public static class BossMotionFXAudit
{
    const string Report = "Library/BossMotionFXAudit.txt";
    [MenuItem("Tools/Effects/Bosses/Audit Motion and Foot Contacts")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var report = new StringBuilder();
        int bosses = 0, samples = 0, actions = 0, failures = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || asset.GetComponent<MidBossController>() == null) continue;
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(path);
                var boss = root.GetComponent<MidBossController>();
                var fx = root.GetComponent<BossEffects>();
                var animator = root.GetComponentInChildren<Animator>(true);
                report.AppendLine("\n" + path);
                if (fx == null || animator == null || animator.runtimeAnimatorController == null)
                { report.AppendLine("FAIL missing FX/Animator/controller"); failures++; continue; }
                bosses++;
                fx.motionAnimator = animator;
                var foot = animator.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r => r.name == "Foot_F");
                if (foot == null || foot.sprite == null) { failures++; report.AppendLine("FAIL Foot_F sprite missing"); }
                report.AppendLine("foot=" + (foot == null ? "missing" : AnimationUtility.CalculateTransformPath(foot.transform, animator.transform)));
                foreach (var clip in animator.runtimeAnimatorController.animationClips.Distinct())
                {
                    var times = new System.Collections.Generic.List<float>();
                    for (int ordinal = 0; ordinal < 20; ordinal++)
                    {
                        if (!fx.TryMotionImpact(clip, ordinal, out float time)) break;
                        times.Add(time);
                        if (time < 0 || time > clip.length) { failures++; report.AppendLine("FAIL impact outside clip"); }
                    }
                    string signals = string.Join(",", AnimationUtility.GetAnimationEvents(clip)
                        .Where(e => e.functionName == "OnStage5AnimationCue").Select(e => e.stringParameter + "@" + e.time.ToString("0.###")));
                    report.AppendLine(clip.name + " length=" + clip.length.ToString("0.###") + " impacts="
                        + string.Join(",", times.Select(t => t.ToString("0.###"))) + " cues=" + signals);
                    if (clip.name == "MidBoss3_Skill3")
                    {
                        var skill = boss.data.skills[1];
                        var slam = skill.active.attackSteps.First(s => s.actionType == BossActionType.Slam);
                        foreach (float facing in new[] { -1f, 1f })
                        {
                            clip.SampleAnimation(animator.gameObject, 1.60f);
                            var scale = animator.transform.localScale; scale.x = Mathf.Abs(scale.x) * facing; animator.transform.localScale = scale;
                            if (!fx.TryWeaponImpact(slam, out var tip)) { failures++; report.AppendLine("FAIL missing weapon contact"); continue; }
                            var effect = fx.PreviewStep(skill, 1, slam, 0, root.transform.position, facing, 0);
                            if (effect == null) { failures++; report.AppendLine("FAIL missing slam FX"); continue; }
                            float floor = root.GetComponent<Collider2D>().bounds.min.y;
                            foreach (float age in new[] { 0f, .08f, .16f, .24f })
                            {
                                effect.PreviewSample(age);
                                var sprite = effect.GetComponent<SpriteRenderer>();
                                if (!sprite.enabled) continue;
                                float error = Mathf.Abs(BossEffects.VisibleBounds(sprite).min.y - floor);
                                float xError = Mathf.Abs(effect.transform.position.x - tip.x);
                                report.AppendLine("Slam facing=" + facing + " age=" + age + " floor error=" + error + " weapon X error=" + xError);
                                if (error > .002f || xError > .002f) failures++;
                            }
                            UnityEngine.Object.DestroyImmediate(effect.gameObject);
                        }
                    }
                    if (!clip.name.ToLowerInvariant().Contains("run") && !clip.name.ToLowerInvariant().Contains("dash") && !clip.name.ToLowerInvariant().Contains("chase")) continue;
                    foreach (float direction in new[] { -1f, 1f })
                    foreach (float phase in new[] { 0f, .25f, .5f, .75f })
                    {
                        clip.SampleAnimation(animator.gameObject, clip.length * phase);
                        var scale = animator.transform.localScale; scale.x = Mathf.Abs(scale.x) * direction; animator.transform.localScale = scale;
                        foreach (bool dash in new[] { false, true })
                        foreach (float age in new[] { .01f, .1f, .2f, .3f })
                        {
                            MobEffectInstance instance = null;
                            try
                            {
                                instance = fx.PreviewMovement(dash, direction, age);
                                if (instance == null) { failures++; report.AppendLine("FAIL missing movement FX " + clip.name + " dash=" + dash); continue; }
                                var sr = instance.GetComponent<SpriteRenderer>();
                                var b = BossEffects.VisibleBounds(sr);
                                var edge = new Vector3(direction > 0 ? b.max.x : b.min.x, b.min.y, sr.transform.position.z);
                                float error = Vector2.Distance(edge, fx.MovementContact(direction));
                                samples++;
                                if (error > .002f) { failures++; report.AppendLine("FAIL trail contact " + error); }
                                if (foot != null && Mathf.Abs(b.min.y - BossEffects.VisibleBounds(foot).min.y) > .002f)
                                { failures++; report.AppendLine("FAIL visible foot/effect height " + clip.name); }
                            }
                            finally { if (instance != null) UnityEngine.Object.DestroyImmediate(instance.gameObject); }
                        }
                    }
                }
                var controller = animator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;
                if (controller != null)
                {
                    var graph = PlayableGraph.Create("Boss isolated controller audit");
                    try
                    {
                        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                        var playable = AnimatorControllerPlayable.Create(graph, controller);
                        var output = AnimationPlayableOutput.Create(graph, "Boss audit", animator);
                        output.SetSourcePlayable(playable); graph.Play(); graph.Evaluate(0);
                        foreach (var parameter in controller.parameters)
                        {
                            if (parameter.name == "IsAttacking") playable.SetBool(parameter.name, true);
                            if (parameter.name == "AttackSpeed") playable.SetFloat(parameter.name, 1);
                        }
                        foreach (var child in controller.layers[0].stateMachine.states)
                        {
                            if (child.state.motion == null)
                            { failures++; report.AppendLine("FAIL missing motion " + child.state.name); continue; }
                            string n = child.state.name.ToLowerInvariant();
                            if (!n.Contains("attack") && !n.Contains("skill") && !n.Contains("combo")) continue;
                            int hash = Animator.StringToHash("Base Layer." + child.state.name);
                            playable.Play(hash, 0, 0); graph.Evaluate(0); graph.Evaluate(.05f);
                            var state = playable.GetCurrentAnimatorStateInfo(0);
                            actions++;
                            if (state.fullPathHash != hash || state.normalizedTime <= 0)
                            { failures++; report.AppendLine("FAIL action advancement " + child.state.name); }
                        }
                    }
                    finally { graph.Destroy(); }
                }
            }
            catch (Exception ex) { failures++; report.AppendLine("FAIL " + ex); }
            finally { if (root != null) PrefabUtility.UnloadPrefabContents(root); }
        }
        report.Insert(0, "bosses=" + bosses + " contact samples=" + samples + " action states=" + actions + " failures=" + failures + "\n");
        File.WriteAllText(Report, report.ToString());
        Debug.Log("Boss motion/FX audit: " + bosses + " bosses, " + samples + " contacts, " + failures + " failures. " + Report);
    }
}
#endif
