#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Put in Assets/Editor/기타. No runtime component or dependency is required.
public static class Boss12_DeathGenerator
{
    const string Menu = "Tools/Boss/Death/";
    const int FPS = 60;
    [MenuItem(Menu + "Generate All Stage 1 and 2")]
    public static void All()
    {
        var errors = new List<string>();
        foreach (string name in new[] { "MidBoss 1", "MainBoss 1", "MidBoss 2", "MainBoss 2" })
            try { Generate(name); } catch (Exception e) { errors.Add(name + ": " + e.Message); }
        AssetDatabase.SaveAssets();
        if (errors.Count > 0) Debug.LogError(string.Join("\n", errors));
    }
    [MenuItem(Menu + "MidBoss 1")]
    public static void Mid1() { Generate("MidBoss 1"); }
    [MenuItem(Menu + "MainBoss 1")]
    public static void Main1() { Generate("MainBoss 1"); }
    [MenuItem(Menu + "MidBoss 2")]
    public static void Mid2() { Generate("MidBoss 2"); }
    [MenuItem(Menu + "MainBoss 2")]
    public static void Main2() { Generate("MainBoss 2"); }

    static void Generate(string name)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("플레이를 종료한 뒤 실행하세요.");
        string stage = name.EndsWith("1") ? "1" : "2";
        string[] folders = AssetDatabase.GetSubFolders("Assets")
            .Where(x => string.Equals(Path.GetFileName(x), "Prefab", StringComparison.OrdinalIgnoreCase))
            .Select(x => x + "/" + stage).Where(AssetDatabase.IsValidFolder).ToArray();
        if (folders.Length == 0) throw new InvalidOperationException("Assets/Prefab/" + stage + " 폴더 없음");
        string[] paths = AssetDatabase.FindAssets("t:Prefab", folders).Select(AssetDatabase.GUIDToAssetPath)
            .Where(x => Path.GetFileNameWithoutExtension(x).Replace(" ", "") == name.Replace(" ", "")).ToArray();
        if (paths.Length != 1) throw new InvalidOperationException(name + " 프리팹이 정확히 하나 필요합니다.");
        GameObject root = PrefabUtility.LoadPrefabContents(paths[0]);
        try
        {
            Animator animator = root.GetComponentInChildren<Animator>(true);
            if (animator == null) throw new InvalidOperationException("Animator 없음");
            var controller = animator.runtimeAnimatorController as AnimatorController;
            if (controller == null) throw new InvalidOperationException("기존 AnimatorController 연결 필요");
            Transform body = animator.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == "Body");
            if (body == null) throw new InvalidOperationException("Body 없음");
            var rig = new Rig(animator.transform, body, name);
            float duration = name == "MainBoss 1" ? 3.17f : name == "MainBoss 2" ? 2.20f : 1.85f;
            string clipName = name.Replace(" ", "") + "_Death";
            var clip = new AnimationClip { name = clipName, frameRate = FPS };
            var curves = new AnimationCurve[rig.nodes.Length, 9];
            for (int i = 0; i < rig.nodes.Length; i++) for (int j = 0; j < 9; j++) curves[i, j] = new AnimationCurve();
            int count = Mathf.RoundToInt(duration * FPS);
            for (int frame = 0; frame <= count; frame++)
            {
                float time = duration * frame / count;
                rig.Pose(time);
                for (int i = 0; i < rig.nodes.Length; i++)
                {
                    Vector3 p = rig.nodes[i].localPosition, s = rig.nodes[i].localScale;
                    float[] v = { p.x, p.y, p.z, 0, 0, rig.angles[i], s.x, s.y, s.z };
                    for (int j = 0; j < 9; j++) curves[i, j].AddKey(new Keyframe(time, v[j]));
                }
            }
            string[] props = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z",
                "localEulerAnglesRaw.x", "localEulerAnglesRaw.y", "localEulerAnglesRaw.z",
                "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z" };
            for (int i = 0; i < rig.nodes.Length; i++) for (int j = 0; j < 9; j++)
                {
                    // Dense linear samples preserve the authored tiny joint twitches without overshoot.
                    for (int k = 0; k < curves[i, j].length; k++)
                    {
                        AnimationUtility.SetKeyLeftTangentMode(curves[i, j], k, AnimationUtility.TangentMode.Linear);
                        AnimationUtility.SetKeyRightTangentMode(curves[i, j], k, AnimationUtility.TangentMode.Linear);
                    }
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(
                        AnimationUtility.CalculateTransformPath(rig.nodes[i], animator.transform), typeof(Transform), props[j]), curves[i, j]);
                }
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false; settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            string folder = "Assets/Animations/" + stage + "/" + name;
            EnsureFolder(folder);
            string target = folder + "/" + clipName + ".anim";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(target);
            if (existing != null)
            {
                EditorUtility.CopySerialized(clip, existing);
                UnityEngine.Object.DestroyImmediate(clip); clip = existing;
                EditorUtility.SetDirty(clip);
            }
            else AssetDatabase.CreateAsset(clip, target);
            Setup(controller, clip);
            AssetDatabase.SaveAssets();
            Debug.Log(name + " Death 생성 완료: " + target + " / IsDead = true로 진입");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static void EnsureFolder(string path)
    {
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        if (AssetDatabase.IsValidFolder(path)) return;
        EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
    static IEnumerable<AnimatorStateMachine> Machines(AnimatorStateMachine sm)
    {
        yield return sm;
        foreach (var sub in sm.stateMachines) foreach (var nested in Machines(sub.stateMachine)) yield return nested;
    }
    static void Setup(AnimatorController ac, AnimationClip clip)
    {
        var parameter = ac.parameters.FirstOrDefault(x => x.name == "IsDead");
        if (parameter != null && parameter.type != AnimatorControllerParameterType.Bool)
            throw new InvalidOperationException("IsDead가 Bool이 아닙니다. 기존 타입을 확인하세요.");
        if (parameter == null) ac.AddParameter("IsDead", AnimatorControllerParameterType.Bool);
        var sm = ac.layers[0].stateMachine;
        var death = sm.states.Select(x => x.state).FirstOrDefault(x => x.name == clip.name)
            ?? sm.AddState(clip.name, new Vector3(40, -150, 0));
        death.motion = clip; death.speed = 1; death.speedParameterActive = false; death.writeDefaultValues = false;
        foreach (var tr in death.transitions.ToArray()) death.RemoveTransition(tr);
        foreach (var tr in sm.anyStateTransitions.Where(x => x.destinationState == death).ToArray()) sm.RemoveAnyStateTransition(tr);
        var enter = sm.AddAnyStateTransition(death);
        enter.name = "BossDeath_Enter"; enter.hasExitTime = false; enter.duration = .08f;
        enter.hasFixedDuration = true; enter.canTransitionToSelf = false;
        enter.interruptionSource = TransitionInterruptionSource.None;
        enter.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
        // Death wins over an attack/phase trigger fired in the same update.
        sm.anyStateTransitions = new[] { enter }.Concat(sm.anyStateTransitions.Where(x => x != enter)).ToArray();
        foreach (var layer in ac.layers) foreach (var machine in Machines(layer.stateMachine))
            {
                foreach (var tr in machine.anyStateTransitions.Concat(machine.states.SelectMany(x => x.state.transitions)))
                    if (tr != enter && tr.destinationState != death && !tr.conditions.Any(x => x.parameter == "IsDead"))
                        tr.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            }
        EditorUtility.SetDirty(ac);
    }

    sealed class Rig
    {
        public readonly Transform[] nodes;
        public readonly float[] angles;
        readonly Vector3[] positions, scales;
        readonly float[] restAngles;
        readonly Dictionary<string, Transform> byName;
        readonly Transform root, body, bottom, tf, tb, cf, cb, ff, fb;
        readonly string kind;
        readonly float floor, size, sole;
        readonly Vector3 restHip;
        readonly Dictionary<Sprite, Vector2[]> outlines = new Dictionary<Sprite, Vector2[]>();
        readonly Dictionary<Texture2D, Color32[]> pixels = new Dictionary<Texture2D, Color32[]>();
        public Rig(Transform root, Transform body, string kind)
        {
            this.root = root; this.body = body; this.kind = kind;
            nodes = body.GetComponentsInChildren<Transform>(true);
            byName = nodes.GroupBy(x => x.name).ToDictionary(x => x.Key, x => x.First());
            positions = nodes.Select(x => x.localPosition).ToArray(); scales = nodes.Select(x => x.localScale).ToArray();
            restAngles = nodes.Select(x => Mathf.DeltaAngle(0, x.localEulerAngles.z)).ToArray();
            angles = new float[nodes.Length];
            bottom = Get("Bottom", "LowerBody"); tf = Get("ThighJoint_F", "Thigh_F"); tb = Get("ThighJoint_B", "Thigh_B");
            cf = Get("KneeJoint_F", "Calf_F"); cb = Get("KneeJoint_B", "Calf_B"); ff = Get("Foot_F"); fb = Get("Foot_B");
            if (bottom == null || tf == null || tb == null || cf == null || cb == null || ff == null || fb == null)
                throw new InvalidOperationException("하체 관절 구조를 찾지 못했습니다.");
            restHip = Local(bottom);
            floor = Mathf.Min(Local(ff).y, Local(fb).y);
            size = Mathf.Max(.8f, restHip.y - floor);
            sole = Mathf.Min(SpriteLow(ff), SpriteLow(fb));
        }
        Transform Get(params string[] names) { foreach (string n in names) if (byName.TryGetValue(n, out var t)) return t; return null; }
        Vector3 Local(Transform t) { return root.InverseTransformPoint(t.position); }
        void Set(Transform t, float z)
        {
            if (t == null) return;
            int i = Array.IndexOf(nodes, t); angles[i] = z; t.localRotation = Quaternion.Euler(0, 0, z);
        }
        void Add(string name, float z) { Transform t = Get(name); if (t != null) Set(t, restAngles[Array.IndexOf(nodes, t)] + z); }
        void Arm(string side, float shoulder, float elbow, float hand)
        {
            Transform s = Get("ShoulderJoint_" + side, "Shoulder_" + side);
            Transform e = Get("ArmJoint_" + side, "Forearm_" + side);
            Set(s, restAngles[Array.IndexOf(nodes, s)] + shoulder);
            Set(e, restAngles[Array.IndexOf(nodes, e)] + elbow);
            Add("Hand_" + side, hand);
        }
        static float Ease(float time, float from, float to) { float x = Mathf.InverseLerp(from, to, time); return x * x * (3 - 2 * x); }
        static float Pulse(float t, float start, float peak, float end)
        { return t < peak ? Ease(t, start, peak) : 1 - Ease(t, peak, end); }
        void MoveHip(Vector3 target) { body.localPosition += root.InverseTransformVector(root.TransformVector(target - Local(bottom))); }
        public void Pose(float t)
        {
            for (int i = 0; i < nodes.Length; i++) { nodes[i].localPosition = positions[i]; nodes[i].localScale = scales[i]; Set(nodes[i], restAngles[i]); }
            if (kind == "MainBoss 2") { BackFall(t); return; }
            bool zombie = kind == "MainBoss 1";
            float offset = zombie ? 1.47f : 0;
            float k = Ease(t, .06f + offset, .65f + offset);
            float f = Ease(t, .78f + offset, 1.43f + offset);
            float settle = Pulse(t, 1.43f + offset, 1.49f + offset, 1.64f + offset);
            Set(body, -10 * k - 78 * f + 2 * settle);
            Set(bottom, 10 * k * (1 - f));
            Set(tf, Mathf.Lerp(58 * k, 2, f)); Set(tb, Mathf.Lerp(52 * k, -3, f));
            Set(cf, Mathf.Lerp(-132 * k, -8, f)); Set(cb, Mathf.Lerp(-125 * k, -5, f));
            Set(ff, 65 * k * (1 - f)); Set(fb, 60 * k * (1 - f));
            Vector3 hip = restHip;
            hip.x += size * (.12f * k + .57f * f);
            hip.y = Mathf.Lerp(restHip.y, floor + size * .40f, k);
            hip.y = Mathf.Lerp(hip.y, floor + size * .17f, f) + size * .025f * settle;
            MoveHip(hip);
            Arm("F", 12 * k + 50 * f, -25 * k - 18 * f, 8 * f);
            Arm("B", 4 * k + 14 * f, -78 * k + 12 * f, 12 * k);
            if (kind == "MidBoss 1")
            {
                // Kneeling ends at 0.65 s; move the rear arm only after that pose.
                float rearPose = Ease(t, .70f, 1.43f);
                Transform shoulder = Get("ShoulderJoint_B");
                Transform elbow = Get("ArmJoint_B");
                Transform hand = Get("Hand_B");
                Set(shoulder, Mathf.Lerp(angles[Array.IndexOf(nodes, shoulder)], 170.290f, rearPose));
                Set(elbow, Mathf.Lerp(angles[Array.IndexOf(nodes, elbow)], -66f, rearPose));
                Set(hand, Mathf.Lerp(angles[Array.IndexOf(nodes, hand)], -53.100f, rearPose));
                Set(Get("Shoulder_B"), 0f);
                Set(Get("Arm_B"), 0f);
                Set(Get("Weapon"), -60f);
            }
            if (kind == "MidBoss 2")
            {
                // Keep the spear arm relaxed during kneeling, then fold alongside the torso.
                float relax = Ease(t, .65f, 1.43f);
                Set(Get("Shoulder_B"), Mathf.Lerp(8f * k, 110f, relax));
                Set(Get("Forearm_B"), Mathf.Lerp(60f - 15f * k, -100f, relax));
                Set(Get("Hand_B"), Mathf.Lerp(0f, -12f, relax));
                // Compensate the parent rotations: the spear settles horizontally, tip forward.
                Transform spear = Get("Spear");
                float parentAngle = 0f;
                for (Transform parent = spear.parent; parent != root && parent != null; parent = parent.parent)
                {
                    int index = Array.IndexOf(nodes, parent);
                    parentAngle += index >= 0 ? angles[index] : Mathf.DeltaAngle(0, parent.localEulerAngles.z);
                }
                float currentAngle = parentAngle + restAngles[Array.IndexOf(nodes, spear)];
                float worldAngle = Mathf.Lerp(currentAngle, -90f, relax);
                Set(spear, worldAngle - parentAngle);
            }
            Add("Head", -8 * k - 7 * f + 4 * settle);
            // Short lower-leg rig: KneeJoint is the only lower-leg pivot.
            // Keep knees above initial foot height during the kneeling phase.
            float kneeLow = Mathf.Min(Local(cf).y, Local(cb).y);
            if (kneeLow < floor) body.localPosition += Vector3.up * (floor - kneeLow) * (1 - f);
            if (zombie)
            {
                Zombie(t / 1.4f, 1 - Ease(t, 1.47f, 1.87f));
                // Start forming the requested front leg as the kneeling body falls forward.
                float legSettle = f;
                Set(tf, Mathf.Lerp(angles[Array.IndexOf(nodes, tf)], 65f, legSettle));
                Set(cf, Mathf.Lerp(angles[Array.IndexOf(nodes, cf)], -25f, legSettle));
                Set(ff, Mathf.Lerp(angles[Array.IndexOf(nodes, ff)], -90f, legSettle));
            }
            GroundCorpse(f);
        }
        void Zombie(float t, float fade)
        {
            float a = Pulse(t, .04f, .11f, .24f), b = Pulse(t, .28f, .33f, .52f);
            float c = Pulse(t, .56f, .63f, .82f), d = Pulse(t, .85f, .90f, 1.12f);
            // Separate joints snap at different times, then slowly lose tension.
            if (fade <= 0) return;
            Transform head = Get("Head");
            Set(head, angles[Array.IndexOf(nodes, head)] + (29 * a - 38 * b + 25 * c - 33 * d) * fade);
            // Alternate independent arm jerks: front, back, front, back.
            // Each arm drops before the other rises; forearms lag their own shoulder.
            float liftF = (82f * a + 94f * c) * fade;
            float liftB = (78f * b + 88f * d) * fade;
            float elbowF = (Pulse(t, .08f, .15f, .24f) * 82f
                - Pulse(t, .59f, .69f, .82f) * 76f) * fade;
            float elbowB = (-Pulse(t, .30f, .39f, .52f) * 80f
                + Pulse(t, .88f, .97f, 1.12f) * 86f) * fade;
            Twitch("Shoulder_F", liftF);
            Twitch("Forearm_F", -.22f * liftF + elbowF);
            Twitch("Hand_F", (-29 * a + 36 * c) * fade);
            Twitch("Shoulder_B", liftB);
            Twitch("Forearm_B", -.17f * liftB + elbowB);
            Twitch("Hand_B", (28 * b - 34 * d) * fade);
            Set(tf, angles[Array.IndexOf(nodes, tf)] + (6 * b - 4 * d) * fade);
            Set(tb, angles[Array.IndexOf(nodes, tb)] + (-5 * a + 8 * c) * fade);
        }
        float SpriteLow(Transform t)
        {
            var r = t.GetComponent<SpriteRenderer>();
            if (r == null || r.sprite == null) return Local(t).y;
            float low = float.PositiveInfinity;
            foreach (Vector2 point in Outline(r.sprite))
            {
                Vector3 p = point;
                if (r.flipX) p.x = -p.x;
                if (r.flipY) p.y = -p.y;
                low = Mathf.Min(low, root.InverseTransformPoint(t.TransformPoint(p)).y);
            }
            return low;
        }
        Vector2[] Outline(Sprite sprite)
        {
            if (outlines.TryGetValue(sprite, out var cached)) return cached;
            // Scan visible pixels, not the rectangular sprite bounds (which include padding).
            // A column's top/bottom pixel corners suffice for the minimum at any 2D rotation.
            if (sprite.packed)
                throw new InvalidOperationException("접지 계산은 패킹 전 원본 Sprite가 필요합니다: " + sprite.name);
            Texture2D texture = sprite.texture;
            if (!pixels.TryGetValue(texture, out var rgba))
            {
                RenderTexture previous = RenderTexture.active;
                RenderTexture rt = RenderTexture.GetTemporary(texture.width, texture.height, 0,
                    RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                Texture2D readable = null;
                try
                {
                    Graphics.Blit(texture, rt);
                    RenderTexture.active = rt;
                    readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, true);
                    readable.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
                    readable.Apply();
                    rgba = readable.GetPixels32();
                    pixels.Add(texture, rgba);
                }
                finally
                {
                    RenderTexture.active = previous;
                    RenderTexture.ReleaseTemporary(rt);
                    if (readable != null) UnityEngine.Object.DestroyImmediate(readable);
                }
            }
            Rect rect = sprite.rect;
            int ox = Mathf.RoundToInt(rect.x), oy = Mathf.RoundToInt(rect.y);
            int w = Mathf.RoundToInt(rect.width), h = Mathf.RoundToInt(rect.height);
            var points = new List<Vector2>();
            for (int x = 0; x < w; x++)
            {
                int lo = h, hi = -1;
                for (int y = 0; y < h; y++)
                {
                    if (rgba[(oy + y) * texture.width + ox + x].a < 32) continue;
                    lo = Mathf.Min(lo, y); hi = Mathf.Max(hi, y);
                }
                if (hi < 0) continue;
                points.Add((new Vector2(x, lo) - sprite.pivot) / sprite.pixelsPerUnit);
                points.Add((new Vector2(x + 1, lo) - sprite.pivot) / sprite.pixelsPerUnit);
                points.Add((new Vector2(x, hi + 1) - sprite.pivot) / sprite.pixelsPerUnit);
                points.Add((new Vector2(x + 1, hi + 1) - sprite.pivot) / sprite.pixelsPerUnit);
            }
            if (points.Count == 0) throw new InvalidOperationException("불투명 픽셀 없음: " + sprite.name);
            cached = points.ToArray(); outlines.Add(sprite, cached);
            return cached;
        }
        void GroundCorpse(float weight)
        {
            if (weight <= 0) return;
            float low = float.PositiveInfinity;
            foreach (Transform t in nodes)
            {
                if (t.name == "Weapon" || t.name == "Spear" || t.name == "Axe") continue;
                if (t.GetComponent<SpriteRenderer>() != null) low = Mathf.Min(low, SpriteLow(t));
            }
            if (kind == "MidBoss 2" || kind == "MainBoss 2")
            {
                // A hanging hand/foot must not prop the entire corpse above the floor.
                // Transfer support from limbs to the visible torso during the final fall.
                float torsoLow = SpriteLow(body);
                float transfer = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.35f, 1f, weight));
                low = Mathf.Lerp(low, torsoLow, transfer);
            }
            if (!float.IsInfinity(low)) body.localPosition += Vector3.up * ((sole - low) * weight);
            if (kind == "MidBoss 2")
                body.localPosition += Vector3.down * (.05f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.65f, 1f, weight)));
        }
        void Twitch(string name, float delta)
        {
            Transform joint = Get(name);
            if (joint != null) Set(joint, angles[Array.IndexOf(nodes, joint)] + delta);
        }
        void BackFall(float t)
        {
            float lean = Ease(t, .08f, 1.22f);
            float x = Mathf.Clamp01((t - 1.22f) / .38f);
            float slam = x * x; // gravity-like acceleration, quick impact at 1.60 s
            float bounce = Pulse(t, 1.60f, 1.67f, 1.90f);
            Set(body, 24 * lean + 66 * slam - 3 * bounce);
            Set(bottom, -8 * lean * (1 - slam));
            Set(tf, -10 * lean + 9 * slam); Set(tb, -7 * lean + 5 * slam);
            Set(cf, 18 * lean * (1 - slam)); Set(cb, 14 * lean * (1 - slam));
            Set(ff, -10 * lean + 20 * slam); Set(fb, -8 * lean + 16 * slam);
            Vector3 hip = restHip;
            hip.x -= size * (.12f * lean + .70f * slam);
            hip.y -= size * .05f * lean;
            hip.y = Mathf.Lerp(hip.y, floor + size * .19f, slam) + size * .035f * bounce;
            MoveHip(hip);
            // Keep the front shoulder still; only settle the forearm/hand slightly.
            Set(Get("Shoulder_F"), 0f);
            Set(Get("Forearm_F"), 10f * lean);
            Set(Get("Hand_F"), -12f * lean);
            Arm("B", 8 * lean - 25 * slam, -25 * lean - 10 * slam, 8 * slam);
            Add("Head", -9 * lean + 18 * slam - 7 * bounce);
            GroundCorpse(slam);
        }
    }
}
#endif
