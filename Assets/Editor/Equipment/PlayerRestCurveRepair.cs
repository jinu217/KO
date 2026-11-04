using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class PlayerRestCurveRepair
{
    [MenuItem("Tools/Player/Rest 기준 애니메이션 정리")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play before repairing clips.");
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Player.prefab");
        var body = prefab.transform.Find("Body");
        var log = new StringBuilder();
        foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Animations/1/Player" }))
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid));
            Undo.RecordObject(clip, "Repair Player Rest curves");
            float end = Mathf.Max(clip.length, 1f / clip.frameRate);
            int added = 0, adjusted = 0;
            foreach (var t in body.GetComponentsInChildren<Transform>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(t, prefab.transform);
                var bindings = AnimationUtility.GetCurveBindings(clip);
                bool quaternion = bindings.Any(b => b.path == path && b.type == typeof(Transform) && b.propertyName.StartsWith("m_LocalRotation."));
                string euler = bindings.Where(b => b.path == path && b.type == typeof(Transform) && b.propertyName.StartsWith("localEulerAngles"))
                    .Select(b => b.propertyName.Split('.')[0]).FirstOrDefault() ?? "localEulerAnglesRaw";
                foreach (string group in new[] { "m_LocalPosition", "m_LocalScale", quaternion ? "m_LocalRotation" : euler })
                {
                    for (int axis = 0; axis < (group == "m_LocalRotation" ? 4 : 3); axis++)
                    {
                        float rest = group == "m_LocalPosition" ? t.localPosition[axis] : group == "m_LocalScale" ? t.localScale[axis]
                            : group == "m_LocalRotation" ? t.localRotation[axis] : Mathf.DeltaAngle(0, t.localEulerAngles[axis]);
                        var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), group + "." + "xyzw"[axis]);
                        var curve = AnimationUtility.GetEditorCurve(clip, binding);
                        if (curve == null || curve.length == 0)
                        {
                            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Constant(0, end, rest)); added++; continue;
                        }
                        // Preserve authored rotation/scale and their motion. Position baselines
                        // alone are rebased; all tangents, key times and event timing survive.
                        if (group != "m_LocalPosition") continue;
                        var keys = curve.keys;
                        float lo = keys.Min(k => k.value), hi = keys.Max(k => k.value);
                        float baseline;
                        if (hi - lo < .00001f) baseline = keys[0].value;
                        else if (t.name == "Head") baseline = keys[0].value;
                        else if (clip.name == "Player_Idle" || clip.name == "Dash") baseline = keys[0].value;
                        else if ((clip.name == "Run" || clip.name == "Run 1") && t != body)
                            baseline = axis == 1 && t.name.StartsWith("LegJoint") ? lo : (lo + hi) * .5f;
                        else continue;
                        float delta = rest - baseline;
                        if (Mathf.Abs(delta) < .000001f) continue;
                        for (int k = 0; k < keys.Length; k++) keys[k].value += delta;
                        curve.keys = keys;
                        AnimationUtility.SetEditorCurve(clip, binding, curve); adjusted++;
                        log.AppendLine(clip.name + " " + path + " " + binding.propertyName + " shift=" + delta);
                    }
                }
            }
            EditorUtility.SetDirty(clip);
            AssetDatabase.SaveAssetIfDirty(clip);
            log.AppendLine(clip.name + ": added=" + added + ", rebased=" + adjusted);
            // Check the actual Unity bindings after saving, not only the edited text.
            foreach (var t in body.GetComponentsInChildren<Transform>(true))
            {
                string path = AnimationUtility.CalculateTransformPath(t, prefab.transform);
                var bs = AnimationUtility.GetCurveBindings(clip).Where(b => b.path == path && b.type == typeof(Transform)).ToArray();
                foreach (var prefix in new[] { "m_LocalPosition.", "m_LocalScale." })
                    foreach (char axis in "xyz")
                        if (!bs.Any(b => b.propertyName == prefix + axis)) throw new Exception("Missing binding: " + clip.name + " " + path + " " + prefix + axis);
                if (bs.Count(b => b.propertyName.StartsWith("localEulerAngles") || b.propertyName.StartsWith("m_LocalRotation.")) < 3)
                    throw new Exception("Missing rotation: " + clip.name + " " + path);
            }
        }
        ValidateSampling();
        Debug.Log("[PlayerRestCurveRepair] Completed\n" + log);
    }
    public static void ValidateSampling()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Player.prefab");
        var root = new GameObject("Rest curve validation") { hideFlags = HideFlags.HideAndDontSave };
        var nodes = new List<Transform>();
        Action<Transform, Transform> copy = null;
        copy = (src, dst) => {
            foreach (Transform child in src)
            {
                var t = new GameObject(child.name) { hideFlags = HideFlags.HideAndDontSave }.transform;
                t.SetParent(dst, false); t.localPosition = child.localPosition; t.localRotation = child.localRotation; t.localScale = child.localScale;
                if (child.name != "GroundCheck") nodes.Add(t);
                copy(child, t);
            }
        };
        copy(prefab.transform, root.transform);
        try
        {
            var clips = AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/Animations/1/Player" })
                .Select(g => AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            int checks = 0;
            foreach (var target in clips)
                foreach (float phase in new[] { 0f, .25f, .5f, .75f, 1f })
                {
                    target.SampleAnimation(root, target.length * phase);
                    var positions = nodes.Select(t => t.localPosition).ToArray();
                    var rotations = nodes.Select(t => t.localRotation).ToArray();
                    var scales = nodes.Select(t => t.localScale).ToArray();
                    foreach (var previous in clips)
                    {
                        previous.SampleAnimation(root, previous.length * .63f);
                        target.SampleAnimation(root, target.length * phase);
                        for (int i = 0; i < nodes.Count; i++)
                            if (Vector3.Distance(positions[i], nodes[i].localPosition) > .00001f || Quaternion.Angle(rotations[i], nodes[i].localRotation) > .05f || Vector3.Distance(scales[i], nodes[i].localScale) > .00001f)
                                throw new Exception("Pose depends on previous clip: " + previous.name + " -> " + target.name + " " + nodes[i].name);
                        checks++;
                    }
                }
            Debug.Log("[PlayerRestCurveRepair] Sampling verified: " + checks + " clip/phase transitions; no inherited transform state.");
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
