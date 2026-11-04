#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.IO;

// Assets/Editor/PlayerWeaponAttackGenerator.cs
// Separate attack motions share Hand_B/Sword; equipment changes its sprite, not its name.
// The physics root is never animated.
public static class PlayerWeaponAttackGenerator
{
    enum WeaponKind { Sword, Sythe, Club, SSword }
    static readonly string[] WeaponNames = { "Sword", "Sythe", "Club", "SSword" };
    // Seven poses: rest, preparation, loaded, impact, follow-through, recovery, rest.
    sealed class Motion
    {
        public float[] times;
        public float[][] rows;
        public Motion(float[] times, params float[][] rows) { this.times = times; this.rows = rows; }
        public float Duration { get { return times[times.Length - 1]; } }
        public float Sample(int row, float t) { return Track(t, times, rows[row]); }
    }

    [MenuItem("Tools/Player/Weapon Attacks/Generate All")]
    public static void GenerateAll()
    {
        var selected = Selection.activeObject;
        try { foreach (WeaponKind kind in Enum.GetValues(typeof(WeaponKind))) { Selection.activeObject = selected; Generate(kind); } }
        finally { Selection.activeObject = selected; }
    }
    [MenuItem("Tools/Player/Weapon Attacks/Generate Attack_Sword")]
    public static void GenerateSword() { Generate(WeaponKind.Sword); }
    [MenuItem("Tools/Player/Weapon Attacks/Generate Attack_Sythe")]
    public static void GenerateSythe() { Generate(WeaponKind.Sythe); }
    [MenuItem("Tools/Player/Weapon Attacks/Generate Attack_Club")]
    public static void GenerateClub() { Generate(WeaponKind.Club); }
    [MenuItem("Tools/Player/Weapon Attacks/Generate Attack_SSword")]
    public static void GenerateSSword() { Generate(WeaponKind.SSword); }

    static void Generate(WeaponKind kind)
    {
        string weaponName = kind.ToString(), ClipName = "Attack_" + weaponName;
        Motion motion = GetMotion(kind);
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        GameObject prefab = Selection.activeObject as GameObject;
        if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab))
        {
            string[] candidates = AssetDatabase.FindAssets("t:Prefab").Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => Path.GetFileNameWithoutExtension(p) == "Player").ToArray();
            if (candidates.Length != 1) throw new InvalidOperationException("Project 창에서 최신 Player 프리팹을 선택한 뒤 실행하세요.");
            prefab = AssetDatabase.LoadAssetAtPath<GameObject>(candidates[0]);
        }
        string[] paths = AssetDatabase.FindAssets(ClipName + " t:AnimationClip").Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => Path.GetFileNameWithoutExtension(p) == ClipName && p.EndsWith(".anim", StringComparison.OrdinalIgnoreCase)).Distinct().ToArray();
        if (paths.Length > 1) throw new InvalidOperationException(ClipName + ".anim이 여러 개입니다. 이름이 중복되지 않게 정리하세요.\n" + string.Join("\n", paths));
        string target = paths.Length == 1 ? paths[0] : "Assets/Animations/1/Player/" + ClipName + ".anim";
        EnsureFolder(Path.GetDirectoryName(target).Replace('\\', '/'));
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(target);
        // Preserve events and their absolute times. Extend the resting tail if needed.
        var events = existing != null ? AnimationUtility.GetAnimationEvents(existing) : new AnimationEvent[0];
        events = events.Where(e => e.functionName != "Attack")
            .Concat(new[] { new AnimationEvent { functionName = "Attack", time = motion.times[3] } })
            .OrderBy(e => e.time).ToArray();
        float duration = Mathf.Max(motion.Duration, events.Length == 0 ? 0 : events.Max(e => e.time));
        GameObject preview = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefab));
        AnimationClip generated = null;
        try
        {
            Animator animator = preview.GetComponentInChildren<Animator>(true);
            if (animator == null) throw new InvalidOperationException("프리팹의 Animator를 찾지 못했습니다.");
            Transform root = animator.transform;
            Transform body = root.Find("Body");
            if (body == null) throw new InvalidOperationException("Animator 바로 아래 Body가 필요합니다.");
            Transform[] all = body.GetComponentsInChildren<Transform>(true);
            Transform hand = all.Single(n => n.name == "Hand_B");
            // All equipment swaps sprites on the same Sword transform.
            Transform weapon = hand.Find("Sword");
            if (weapon == null)
                throw new InvalidOperationException("Hand_B 아래 공용 무기 오브젝트 Sword가 필요합니다. 무기를 교체해도 이름은 Sword로 유지하세요.");
            // Equipment visibility and other weapon transforms belong to the equipment system.
            Transform[] nodes = body.GetComponentsInChildren<Transform>(true).Where(n =>
            {
                for (Transform ancestor = n; ancestor != body; ancestor = ancestor.parent)
                    if (ancestor.parent == hand && WeaponNames.Contains(ancestor.name))
                        return ancestor == weapon;
                return true;
            }).ToArray();
            string[] required = { "Head", "ShoulderJoint_F", "ShoulderJoint_B", "ArmJoint_F", "ArmJoint_B", "Hand_B", "Sword", "LegJoint_F", "LegJoint_B", "Foot_F", "Foot_B" };
            foreach (string name in required)
                if (nodes.Count(n => n.name == name) != 1) throw new InvalidOperationException("관절이 없거나 중복됩니다: " + name);
            Vector3[] rest = nodes.Select(n => n.localPosition).ToArray();
            Vector3[] scale = nodes.Select(n => n.localScale).ToArray();
            float[] restAngles = nodes.Select(n => Mathf.DeltaAngle(0, n.localEulerAngles.z)).ToArray();
            Vector3 frontFoot = root.InverseTransformPoint(nodes.First(n => n.name == "Foot_F").position);
            Vector3 backFoot = root.InverseTransformPoint(nodes.First(n => n.name == "Foot_B").position);
            var curves = new AnimationCurve[nodes.Length, 9];
            for (int i = 0; i < nodes.Length; i++) for (int j = 0; j < 9; j++) curves[i, j] = new AnimationCurve();
            int count = Mathf.CeilToInt(duration * 60);
            float daggerImpactTop = 0f;
            for (int frame = kind == WeaponKind.SSword ? -1 : 0; frame <= count; frame++)
            {
                float t = frame < 0 ? motion.times[3] : duration * frame / count;
                float[] angles = (float[])restAngles.Clone();
                for (int i = 0; i < nodes.Length; i++)
                {
                    Vector3 p = rest[i]; float delta = 0;
                    switch (nodes[i].name)
                    {
                        // Player authoring convention: +Z is counterclockwise, -Z is clockwise.
                        // Arm preparation uses negative rotation; the strike increases back toward rest.
                        case "Body": delta = motion.Sample(0, t); p.x += motion.Sample(1, t); p.y += motion.Sample(2, t); break;
                        case "Head": delta = motion.Sample(3, t); break;
                        case "ShoulderJoint_B": delta = motion.Sample(4, t); break;
                        case "ArmJoint_B": delta = motion.Sample(5, t); break;
                        case "Hand_B": delta = motion.Sample(6, t); break;
                        case "ShoulderJoint_F": delta = motion.Sample(7, t); break;
                        case "ArmJoint_F": delta = motion.Sample(8, t); break;
                    }
                    angles[i] += delta; nodes[i].localPosition = p; nodes[i].localRotation = Quaternion.Euler(0, 0, angles[i]); nodes[i].localScale = scale[i];
                }
                int sword = Array.FindIndex(nodes, n => n == weapon);
                // Other weapons retain their prefab-local blade rotation.
                // Shoulder winds up negatively, then increases for the cut.
                // Forearm supports the shoulder windup; the wrist adds only a small follow-through.
                angles[sword] = restAngles[sword];
                if (kind == WeaponKind.SSword)
                {
                    // Aim at the supplied impact orientation by the first preparation pose.
                    // Counter-rotate the weapon against torso + shoulder + forearm + wrist,
                    // so the blade keeps its screen-space inclination during the thrust.
                    // At impact the weapon still reaches the requested local 60 degrees.
                    float impactAim = 60f - restAngles[sword];
                    foreach (int row in new[] { 0, 4, 5, 6 }) impactAim += motion.rows[row][3];
                    float aim = Track(t, motion.times, new float[] { 0f, impactAim, impactAim, impactAim, impactAim, impactAim, 0f });
                    float armRotation = motion.Sample(0, t) + motion.Sample(4, t) + motion.Sample(5, t) + motion.Sample(6, t);
                    angles[sword] = restAngles[sword] + aim - armRotation;
                }
                nodes[sword].localRotation = Quaternion.Euler(0, 0, angles[sword]);
                if (kind == WeaponKind.SSword)
                {
                    // Measure the actual supplied dagger canvas, not whichever equipment
                    // sprite happens to be assigned to the shared Sword in the prefab.
                    float top = DaggerTop(root, weapon);
                    if (frame < 0) { daggerImpactTop = top; continue; }
                    if (t < motion.times[3])
                    {
                        float blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(motion.times[1], motion.times[2], t));
                        float lower = Mathf.Max(0f, top - daggerImpactTop) * blend;
                        // Lower the torso as one unit; keep all arm joints connected.
                        body.position -= root.TransformVector(Vector3.up * lower);
                    }
                }
                // Preserve root-space foot pivot heights despite torso rotation.
                foreach (string footName in new[] { "Foot_F", "Foot_B" })
                {
                    Transform foot = nodes.First(n => n.name == footName);
                    Transform joint = foot.parent;
                    Vector3 destination = root.TransformPoint(footName == "Foot_F" ? frontFoot : backFoot);
                    joint.position += destination - foot.position;
                    float total = 0;
                    for (Transform p = foot.parent; p != root && p != null; p = p.parent)
                    { int i = Array.IndexOf(nodes, p); if (i >= 0) total += angles[i]; }
                    int index = Array.IndexOf(nodes, foot); angles[index] = -total;
                }
                for (int i = 0; i < nodes.Length; i++)
                {
                    Vector3 p = nodes[i].localPosition;
                    float[] v = { p.x, p.y, p.z, 0, 0, angles[i], scale[i].x, scale[i].y, scale[i].z };
                    for (int j = 0; j < 9; j++) curves[i, j].AddKey(new Keyframe(t, v[j]));
                }
            }
            generated = new AnimationClip { name = ClipName, frameRate = 60 };
            string[] props = { "m_LocalPosition.x", "m_LocalPosition.y", "m_LocalPosition.z", "localEulerAnglesRaw.x", "localEulerAnglesRaw.y", "localEulerAnglesRaw.z", "m_LocalScale.x", "m_LocalScale.y", "m_LocalScale.z" };
            for (int i = 0; i < nodes.Length; i++) for (int j = 0; j < 9; j++)
                {
                    for (int k = 0; k < curves[i, j].length; k++)
                    { AnimationUtility.SetKeyLeftTangentMode(curves[i, j], k, AnimationUtility.TangentMode.Linear); AnimationUtility.SetKeyRightTangentMode(curves[i, j], k, AnimationUtility.TangentMode.Linear); }
                    AnimationUtility.SetEditorCurve(generated, EditorCurveBinding.FloatCurve(AnimationUtility.CalculateTransformPath(nodes[i], root), typeof(Transform), props[j]), curves[i, j]);
                }
            var settings = AnimationUtility.GetAnimationClipSettings(generated); settings.loopTime = false; AnimationUtility.SetAnimationClipSettings(generated, settings);
            AnimationUtility.SetAnimationEvents(generated, events);
            if (existing != null)
            { Undo.RecordObject(existing, "Rebuild " + ClipName); EditorUtility.CopySerialized(generated, existing); EditorUtility.SetDirty(existing); UnityEngine.Object.DestroyImmediate(generated); generated = null; }
            else { AssetDatabase.CreateAsset(generated, target); existing = generated; generated = null; }
            AssetDatabase.SaveAssets(); Selection.activeObject = existing; EditorGUIUtility.PingObject(existing);
            Debug.Log(ClipName + " 생성: " + target + " / 길이 " + motion.Duration + "초, 시각적 임팩트 " + motion.times[3] + "초. 기존 이벤트 시간 유지. 무기 선택 및 타격 코드는 별도로 연결하세요.");
        }
        finally { if (generated != null) UnityEngine.Object.DestroyImmediate(generated); PrefabUtility.UnloadPrefabContents(preview); }
    }
    static float Track(float t, float[] times, float[] values)
    {
        for (int i = 1; i < times.Length; i++) if (t <= times[i])
            { float u = Mathf.InverseLerp(times[i - 1], times[i], t); u = i == 3 ? u * u : u * u * (3 - 2 * u); return Mathf.LerpUnclamped(values[i - 1], values[i], u); }
        return values[values.Length - 1];
    }
    // Rows: body rotation, body X, body Y, head, rear shoulder, rear forearm,
    // rear wrist, front shoulder, front forearm. Rotation values are rest-relative.
    // Sword/Sythe/Club: clockwise (-) windup -> counterclockwise (+) strike.
    // SSword: retract -> extend along a nearly horizontal line -> retract.
    // Sythe: compact, quick sweep with little vertical weight; Club keeps its heavy slam.
    static Motion GetMotion(WeaponKind kind)
    {
        switch (kind)
        {
            case WeaponKind.Sword:
                return new Motion(new float[] { 0f, 0.12f, 0.2f, 0.27f, 0.34f, 0.48f, 0.65f },
                new float[] { 0f, -2f, -3f, 6f, 5f, 2f, 0f },
                new float[] { 0f, 0.015f, 0.025f, -0.085f, -0.07f, -0.025f, 0f },
                new float[] { 0f, -0.01f, -0.02f, -0.045f, -0.035f, -0.01f, 0f },
                new float[] { 0f, 1f, 2f, -3f, -2f, -1f, 0f },
                new float[] { 0f, -48f, -105f, -32f, -8f, -4f, 0f },
                new float[] { 0f, -5f, -12f, -8f, -5f, -2f, 0f },
                new float[] { 0f, 0f, -2f, 3f, 4f, 1f, 0f },
                new float[] { 0f, -12f, -20f, -32f, -25f, -10f, 0f },
                new float[] { 0f, 12f, 20f, 8f, 5f, 2f, 0f });
            case WeaponKind.Sythe:
                return new Motion(new float[] { 0f, 0.10f, 0.17f, 0.25f, 0.32f, 0.43f, 0.60f },
                new float[] { 0f, -1f, -2f, 3f, 4f, 1f, 0f },
                new float[] { 0f, 0.015f, 0.025f, -0.055f, -0.07f, -0.02f, 0f },
                new float[] { 0f, -0.003f, -0.008f, -0.012f, -0.01f, -0.003f, 0f },
                new float[] { 0f, 1f, 1f, -2f, -2f, 0f, 0f },
                new float[] { 0f, -35f, -72f, -22f, 6f, 2f, 0f },
                new float[] { 0f, -8f, -20f, -8f, 10f, 4f, 0f },
                new float[] { 0f, 0f, -3f, 3f, 6f, 2f, 0f },
                new float[] { 0f, -8f, -16f, 8f, 14f, 4f, 0f },
                new float[] { 0f, 8f, 15f, 8f, 4f, 2f, 0f });
            case WeaponKind.Club:
                return new Motion(new float[] { 0f, 0.25f, 0.43f, 0.55f, 0.64f, 0.92f, 1.25f },
                new float[] { 0f, -2f, -4f, 10f, 9f, 3f, 0f },
                new float[] { 0f, 0.02f, 0.035f, -0.065f, -0.085f, -0.025f, 0f },
                new float[] { 0f, -0.012f, 0.015f, -0.095f, -0.1f, -0.03f, 0f },
                new float[] { 0f, 1f, 2f, -5f, -4f, -1f, 0f },
                new float[] { 0f, -60f, -125f, -25f, -4f, -2f, 0f },
                new float[] { 0f, -8f, -18f, -12f, -8f, -3f, 0f },
                new float[] { 0f, 0f, -2f, 4f, 5f, 2f, 0f },
                new float[] { 0f, -16f, -28f, -35f, -30f, -12f, 0f },
                new float[] { 0f, 18f, 32f, 12f, 8f, 4f, 0f });
            case WeaponKind.SSword:
                return new Motion(new float[] { 0f, 0.07f, 0.13f, 0.19f, 0.23f, 0.34f, 0.48f },
                new float[] { 0f, -1f, -2f, 3f, 3f, 1f, 0f },
                new float[] { 0f, 0.02f, 0.04f, -0.10f, -0.10f, 0.015f, 0f },
                new float[] { 0f, 0f, -0.005f, -0.008f, -0.008f, -0.003f, 0f },
                new float[] { 0f, 1f, 2f, -2f, -2f, 0f, 0f },
                new float[] { 0f, -12f, -20f, -60f, -60f, -20f, 0f },
                new float[] { 0f, -30f, -50f, 0f, 0f, -50f, 0f },
                new float[] { 0f, 10.75f, 18f, 21.75f, 21.75f, 17.25f, 0f },
                new float[] { 0f, -8f, -14f, -18f, -18f, -8f, 0f },
                new float[] { 0f, 10f, 18f, 22f, 22f, 10f, 0f });
            default: throw new ArgumentOutOfRangeException("kind");
        }
    }
    // User supplied dagger: 64x35 pixels, pivot (0.85,0.28), PPU 64.
    static float DaggerTop(Transform root, Transform weapon)
    {
        float top = float.NegativeInfinity;
        for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++)
            {
                Vector3 corner = new Vector3(x - 0.85f, (y - 0.28f) * 35f / 64f, 0f);
                top = Mathf.Max(top, root.InverseTransformPoint(weapon.TransformPoint(corner)).y);
            }
        return top;
    }
    static void EnsureFolder(string path)
    { if (AssetDatabase.IsValidFolder(path)) return; string parent = Path.GetDirectoryName(path).Replace('\\', '/'); EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path)); }
}
#endif
