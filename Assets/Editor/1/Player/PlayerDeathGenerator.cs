#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class PlayerDeathGenerator
{
    [MenuItem("Tools/Player/Death/Generate And Setup")]
    public static void Generate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        GameObject prefab = Selection.activeObject as GameObject;
        if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab))
        {
            var matches = AssetDatabase.FindAssets("Player t:Prefab").Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p) == "Player").ToArray();
            if (matches.Length != 1) { Debug.LogError("Project 창에서 사용할 Player 프리팹을 선택하세요."); return; }
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(matches[0]);
        }
        var root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefab));
        var clip = new AnimationClip { name = "PlayerDeath", frameRate = 12 };
        try
        {
            var animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.transform.Find("Body") == null) throw new InvalidOperationException("Animator 아래 Body가 필요합니다.");
            Transform body = animator.transform.Find("Body");
            var nodes = body.GetComponentsInChildren<Transform>(true);
            var pos = nodes.Select(t => t.localPosition).ToArray();
            var rot = nodes.Select(t => Mathf.DeltaAngle(0, t.localEulerAngles.z)).ToArray();
            var scale = nodes.Select(t => t.localScale).ToArray();
            var curves = new AnimationCurve[nodes.Length, 7];
            for (int i = 0; i < nodes.Length; i++) for (int c = 0; c < 7; c++) curves[i, c] = new AnimationCurve();
            float floor = Lowest(animator.transform, body);
            const float duration = 1.2f;
            for (int f = 0; f <= 72; f++)
            {
                float time = f / 60f;
                float collapse = Smooth(time, .08f, .38f), fall = Smooth(time, .28f, .85f), settle = Smooth(time, .85f, 1.08f);
                for (int i = 0; i < nodes.Length; i++)
                {
                    nodes[i].localPosition = pos[i]; nodes[i].localScale = scale[i];
                    float angle = 0;
                    switch (nodes[i].name)
                    {
                        case "Body": angle = -12 * collapse - 80 * fall + 2 * settle; break;
                        case "Head": angle = -10 * collapse + 18 * fall; break;
                        case "LegJoint_F": angle = 22 * collapse * (1 - fall); break;
                        case "LegJoint_B": angle = -15 * collapse * (1 - fall); break;
                        case "Foot_F": angle = -15 * collapse * (1 - fall); break;
                        case "Foot_B": angle = 12 * collapse * (1 - fall); break;
                        case "ShoulderJoint_F": angle = 25 * collapse - 40 * fall; break;
                        case "ArmJoint_F": angle = -30 * collapse + 15 * fall; break;
                        case "ShoulderJoint_B": angle = -15 * collapse + 20 * fall; break;
                        case "ArmJoint_B": angle = 20 * collapse - 25 * fall; break;
                    }
                    nodes[i].localRotation = Quaternion.Euler(0, 0, rot[i] + angle);
                }
                // Sprite vertices include the configured pivots and PPU. Keep the lowest body part on the floor.
                body.position += animator.transform.TransformVector(Vector3.up * (floor - Lowest(animator.transform, body)));
                for (int i = 0; i < nodes.Length; i++)
                {
                    Vector3 p = nodes[i].localPosition;
                    for (int axis = 0; axis < 3; axis++) { curves[i, axis].AddKey(time, p[axis]); curves[i, axis + 4].AddKey(time, scale[i][axis]); }
                    curves[i, 3].AddKey(time, Mathf.DeltaAngle(0, nodes[i].localEulerAngles.z));
                }
            }
            for (int i = 0; i < nodes.Length; i++)
            {
                string path = AnimationUtility.CalculateTransformPath(nodes[i], animator.transform);
                for (int c = 0; c < 7; c++)
                {
                    string property = c < 3 ? "m_LocalPosition." + "xyz"[c] : c == 3 ? "localEulerAnglesRaw.z" : "m_LocalScale." + "xyz"[c - 4];
                    for (int k = 0; k < curves[i, c].length; k++)
                    { AnimationUtility.SetKeyLeftTangentMode(curves[i, c], k, AnimationUtility.TangentMode.Linear); AnimationUtility.SetKeyRightTangentMode(curves[i, c], k, AnimationUtility.TangentMode.Linear); }
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curves[i, c]);
                }
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = false; AnimationUtility.SetAnimationClipSettings(clip, settings);
            const string target = "Assets/Animations/1/Player/Resources/PlayerDeath.anim";
            Directory.CreateDirectory(Path.GetDirectoryName(target)); AssetDatabase.Refresh();
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(target);
            if (existing != null) { EditorUtility.CopySerialized(clip, existing); EditorUtility.SetDirty(existing); }
            else { AssetDatabase.CreateAsset(clip, target); clip = null; }
            AssetDatabase.SaveAssets();
            Debug.Log("PlayerDeath 뒤로 쓰러지기 생성 완료: " + target + " / 사망 시 재생 후 데스신으로 이동합니다.");
        }
        finally { if (clip != null) UnityEngine.Object.DestroyImmediate(clip); PrefabUtility.UnloadPrefabContents(root); }
    }
    static float Smooth(float t, float a, float b) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(a, b, t));
    static float Lowest(Transform root, Transform body)
    {
        float min = float.PositiveInfinity;
        foreach (var sr in body.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (sr.sprite == null || !sr.enabled || !sr.gameObject.activeInHierarchy || sr.name == "Sword" || sr.name == "Weapon") continue;
            foreach (var v in sr.sprite.vertices)
            {
                Vector3 p = new Vector3(sr.flipX ? -v.x : v.x, sr.flipY ? -v.y : v.y, 0);
                min = Mathf.Min(min, root.InverseTransformPoint(sr.transform.TransformPoint(p)).y);
            }
        }
        if (float.IsPositiveInfinity(min)) throw new InvalidOperationException("Body 아래 스프라이트가 없습니다.");
        return min;
    }
}
#endif
