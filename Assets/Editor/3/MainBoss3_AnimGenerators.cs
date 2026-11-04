#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Place in Assets/Editor. Visual root faces +X. Physics movement belongs to boss AI.
// Clip Skill1=spin slash, Skill2=four thrusts + rush, Skill3=rush + spin retreat.
// Runtime skill indices: 0->Skill1, 1->Skill2, 2->Buff, 3->Skill3.
public static class MainBoss3_AnimGenerators
{
    public const string OutputFolder = "Assets/Animations/3/MainBoss 3";
    private const string PrefabFolder = "Assets/Prefab/3", MobName = "MainBoss 3";
    private const int SampleRate = 60;
    public const float AttackImpact = 1.10f;
    public const float Skill2DashStart = 1.38f, Skill2DashEnd = 1.88f;
    public const float Skill3DashStart = .42f, Skill3DashEnd = .92f;
    public const float RetreatStart = 1.12f, RetreatEnd = 1.78f;
    private const int Body = 0, Head = 1, SF = 2, AF = 3, HF = 4, SB = 5, AB = 6, HB = 7,
        Sword = 8, Bottom = 9, TF = 10, CF = 11, FF = 12, TB = 13, CB = 14, FB = 15, Cape = 16;
    private static readonly int[] Parent = { -1, 0, 0, 2, 3, 0, 5, 6, 7, 0, 9, 10, 11, 9, 13, 14, 0 };
    private static readonly string[] Paths = {
        "Body", "Body/Head", "Body/Shoulder_F", "Body/Shoulder_F/Forearm_F",
        "Body/Shoulder_F/Forearm_F/Hand_F", "Body/Shoulder_B", "Body/Shoulder_B/Forearm_B",
        "Body/Shoulder_B/Forearm_B/Hand_B", "Body/Shoulder_B/Forearm_B/Hand_B/Sword",
        "Body/Bottom", "Body/Bottom/Thigh_F", "Body/Bottom/Thigh_F/Calf_F",
        "Body/Bottom/Thigh_F/Calf_F/Foot_F", "Body/Bottom/Thigh_B",
        "Body/Bottom/Thigh_B/Calf_B", "Body/Bottom/Thigh_B/Calf_B/Foot_B", "Body/Cape"
    };
    private static readonly Vector3[] RestPositions = {
        new Vector3(0,0,0),new Vector3(-.022f,.844f,0),
        new Vector3(-.479f,.783f,0),new Vector3(-.136f,-.509f,0),new Vector3(-.134f,-.602f,0),
        new Vector3(.144f,.837f,0),new Vector3(.256f,-.512f,0),new Vector3(.210f,-.625f,0),
        new Vector3(-.001f,-.115f,0),new Vector3(-.027f,.025f,0),
        new Vector3(-.340f,-.444f,0),new Vector3(-.060f,-.836f,0),new Vector3(-.083f,-.921f,0),
        new Vector3(.173f,-.577f,0),new Vector3(.114f,-.703f,0),new Vector3(-.083f,-.921f,0),
        new Vector3(-.100f,.981f,0)
    };
    private const float RestHandBAngle = 30f;
    private static readonly float[] RestAngles = { 0, 0, 0, 0, 0, 0, 0, RestHandBAngle, 90, 0, 0, 0, 0, 0, 0, 0, 0 };
    private sealed class Pose
    {
        public Vector3[] position = (Vector3[])RestPositions.Clone();
        public float[] angle = (float[])RestAngles.Clone();
    }
    private static readonly string[] Names = { "Idle", "Walk", "Run", "Attack", "Skill1", "Skill2", "Skill3", "Hit", "Death", "Buff", "Run3" };
    private static readonly float[] Durations = { 2.8f, 1.1f, .70f, 1.85f, 4.50f, 2.60f, 2.55f, .36f, 1.65f, 2.20f, .70f };
    private static Pose Evaluate(int i, float t)
    {
        switch (i)
        {
            case 0: return Idle(t);
            case 1: return Locomotion(t, 1.1f, false);
            case 2: return Locomotion(t, .70f, true);
            case 3: return Attack(t);
            case 4: return SpinSkill(t);
            case 5: return ThrustRush(t);
            case 6: return RushRetreat(t);
            case 7: return Hit(t);
            case 8: return Death(t);
            case 10: return Locomotion(t, .70f, true, true);
            default: return Buff(t);
        }
    }
    [MenuItem("Tools/3/MainBoss 3/Generate Animations")]
    public static void GenerateAll() { EnsureFolder(OutputFolder); for (int i = 0; i < Names.Length; i++) Make(i); SetupAnimator(); }
    [MenuItem("Tools/3/MainBoss 3/Clips/Idle")]
    public static void GenerateIdle() { EnsureFolder(OutputFolder); Make(0); SetupAnimator(); }
    [MenuItem("Tools/3/MainBoss 3/Clips/Walk")]
    public static void GenerateWalk() { EnsureFolder(OutputFolder); Make(1); SetupAnimator(); }
    [MenuItem("Tools/3/MainBoss 3/Clips/Run")]
    public static void GenerateRun() { EnsureFolder(OutputFolder); Make(2); SetupAnimator(); }
    [MenuItem("Tools/3/MainBoss 3/Clips/Attack")]
    public static void GenerateAttack() { EnsureFolder(OutputFolder); Make(3); SetupAnimator(); }
    [MenuItem("Tools/3/MainBoss 3/Clips/Skill1")]
    public static void GenerateSkill1() { EnsureFolder(OutputFolder); Make(4); SetupAnimator(); }
    [MenuItem("Tools/3/MainBoss 3/Clips/Skill2")]
    public static void GenerateSkill2() { EnsureFolder(OutputFolder); Make(5); SetupAnimator(); }
    [MenuItem("Tools/3/MainBoss 3/Clips/Skill3")]
    public static void GenerateSkill3() { EnsureFolder(OutputFolder); Make(6); SetupAnimator(); }
    [MenuItem("Tools/3/MainBoss 3/Clips/Hit")]
    public static void GenerateHit() { EnsureFolder(OutputFolder); Make(7); SetupAnimator(); }
    [MenuItem("Tools/3/MainBoss 3/Clips/Death")]
    public static void GenerateDeath() { EnsureFolder(OutputFolder); Make(8); SetupAnimator(); }
    [MenuItem("Tools/3/MainBoss 3/Clips/Buff")]
    public static void GenerateBuff() { EnsureFolder(OutputFolder); Make(9); SetupAnimator(); }
    [MenuItem("Tools/3/MainBoss 3/Clips/Run3")]
    public static void GenerateRun3() { EnsureFolder(OutputFolder); Make(10); SetupAnimator(); }
    private static AnimationClip Make(int i)
    {
        return Generate("MainBoss3_" + Names[i], Durations[i], i < 3 || i == 10, t => Evaluate(i, t));
    }
    public static void RebuildThrustAim() { Make(5); Make(6); AssetDatabase.SaveAssets(); }
    [MenuItem("Tools/3/MainBoss 3/Setup Animator")]
    public static void SetupAnimator()
    {
        EnsureFolder(OutputFolder);
        const string name = "MainBoss3_AC";
        AnimatorController controller = null;
        foreach (string guid in AssetDatabase.FindAssets(name + " t:AnimatorController"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) != name) continue;
            if (controller != null) throw new InvalidOperationException("Multiple MainBoss3_AC controllers found.");
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        }
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(OutputFolder + "/" + name + ".controller");
        Parameter(controller, "MoveSpeed", AnimatorControllerParameterType.Float);
        Parameter(controller, "IsChasing", AnimatorControllerParameterType.Bool);
        Parameter(controller, "IsDead", AnimatorControllerParameterType.Bool);
        foreach (string trigger in new[] { "Attack", "Skill1", "Skill2", "Skill3", "Hit", "Buff" }) Parameter(controller, trigger, AnimatorControllerParameterType.Trigger);
        Parameter(controller, "IsAttacking", AnimatorControllerParameterType.Bool);
        Parameter(controller, "AttackSpeed", AnimatorControllerParameterType.Float);
        Parameter(controller, "Phase", AnimatorControllerParameterType.Int);
        // Set a usable default before the first runtime trigger.
        var parameters = controller.parameters;
        for (int i = 0; i < parameters.Length; i++) if (parameters[i].name == "AttackSpeed") parameters[i].defaultFloat = 1;
        controller.parameters = parameters;
        var sm = controller.layers[0].stateMachine;
        var states = new AnimatorState[Names.Length];
        // Boss layout: locomotion on the left, actions on the right, reactions below.
        Vector3[] layout = {
            new Vector3(80, 260, 0), // Idle: locomotion hub
            new Vector3(0, 460, 0), // Walk
            new Vector3(260, 460, 0), // Run
            new Vector3(760, 40, 0), // Attack
            new Vector3(760, 220, 0), // Skill1
            new Vector3(760, 400, 0), // Skill2
            new Vector3(760, 580, 0), // Skill3
            new Vector3(260, 820, 0), // Hit: recovery
            new Vector3(760, 1040, 0), // Death
            new Vector3(760, 760, 0), // Buff
            new Vector3(260, 630, 0), // Run3: phase 3 chase
        };
        for (int i = 0; i < states.Length; i++)
        {
            foreach (var child in sm.states) if (child.state.name == "MainBoss3_" + Names[i]) states[i] = child.state;
            if (states[i] == null) states[i] = sm.AddState("MainBoss3_" + Names[i], layout[i]);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(OutputFolder + "/MainBoss3_" + Names[i] + ".anim");
            if (clip == null) clip = Make(i);
            states[i].motion = clip; states[i].speed = 1; states[i].speedParameter = "AttackSpeed"; states[i].speedParameterActive = (i >= 3 && i <= 6) || i == 9; states[i].writeDefaultValues = false;
            foreach (var tr in states[i].transitions) if (tr.name.StartsWith("MainBoss3_Generated_")) states[i].RemoveTransition(tr);
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = i < 3 || i == 10; AnimationUtility.SetAnimationClipSettings(clip, settings); EditorUtility.SetDirty(clip);
        }
        var children = sm.states;
        for (int i = 0; i < children.Length; i++) for (int j = 0; j < states.Length; j++) if (children[i].state == states[j]) children[i].position = layout[j];
        sm.states = children; sm.defaultState = states[0];
        sm.entryPosition = new Vector3(80, 100, 0);
        sm.anyStatePosition = new Vector3(260, 1040, 0);
        sm.exitPosition = new Vector3(1080, 1040, 0);
        foreach (var tr in sm.anyStateTransitions) if (tr.name.StartsWith("MainBoss3_Generated_")) sm.RemoveAnyStateTransition(tr);
        var death = sm.AddAnyStateTransition(states[8]); Configure(death, false); death.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
        var hit = sm.AddAnyStateTransition(states[7]); Configure(hit, false); hit.AddCondition(AnimatorConditionMode.If, 0, "Hit"); hit.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
        for (int from = 0; from < states.Length; from++)
        {
            if (from == 8) continue;
            foreach (int to in new[] { 0, 1, 2, 10 })
            {
                if (from == to) continue;
                var tr = states[from].AddTransition(states[to]); Configure(tr, from >= 3 && from != 10);
                tr.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                tr.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                tr.AddCondition(to == 0 ? AnimatorConditionMode.Less : AnimatorConditionMode.Greater, .01f, "MoveSpeed");
                if (to != 0) tr.AddCondition((to == 2 || to == 10) ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "IsChasing");
                if (to == 2) tr.AddCondition(AnimatorConditionMode.Less, 3, "Phase");
                if (to == 10) tr.AddCondition(AnimatorConditionMode.Greater, 2, "Phase");
            }
            if (from >= 3 && from != 10) continue;
            foreach (int to in new[] { 3, 4, 5, 6, 9 })
            {
                var tr = states[from].AddTransition(states[to]); Configure(tr, false); tr.duration = 0;
                tr.AddCondition(AnimatorConditionMode.If, 0, Names[to]); tr.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            }
        }
        EditorUtility.SetDirty(sm); EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets(); AssignControllerToPrefab(controller);
        Selection.activeObject = controller;
        Debug.Log("MainBoss3 generated. Skill1=Spin, Skill2=Thrust rush, Skill3=Rush retreat, Buff=Buff. AI movement/damage and facing lock must be connected separately.");
    }
    private static void Configure(AnimatorStateTransition t, bool exit)
    {
        t.name = "MainBoss3_Generated_" + t.destinationState.name; t.hasExitTime = exit; t.exitTime = 1;
        t.hasFixedDuration = true; t.duration = .06f; t.canTransitionToSelf = false; t.interruptionSource = TransitionInterruptionSource.None;
    }
    private static void Parameter(AnimatorController c, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in c.parameters) if (p.name == name) { if (p.type != type) throw new InvalidOperationException(name + " type mismatch"); return; }
        c.AddParameter(name, type);
    }
    private static void AssignControllerToPrefab(AnimatorController controller)
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
        {
            Debug.LogWarning("프리팹 자동 연결 생략: 폴더가 없습니다. " + PrefabFolder);
            return;
        }
        var candidates = new List<string>();
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == MobName && path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                candidates.Add(path);
        }
        if (candidates.Count != 1)
        {
            Debug.LogWarning("프리팹 자동 연결 생략: " + PrefabFolder + " 안의 " + MobName
                + ".prefab 검색 결과가 " + candidates.Count + "개입니다. 정확히 하나가 필요합니다.");
            return;
        }
        string prefabPath = candidates[0];
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            // 클립 경로는 Body/... 이므로 Body를 직계 자식으로 가진 시각 루트에 연결한다.
            Transform visual = null;
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name != MobName || node.Find("Body") == null) continue;
                if (visual != null)
                {
                    Debug.LogWarning("프리팹 자동 연결 생략: 시각 루트가 여러 개입니다. " + prefabPath);
                    return;
                }
                visual = node;
            }
            if (visual == null)
            {
                Debug.LogWarning("프리팹 자동 연결 생략: Body를 자식으로 가진 MainBoss 3 시각 루트를 찾지 못했습니다. " + prefabPath);
                return;
            }
            Animator animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool success);
            if (success) Debug.Log("컨트롤러 자동 연결 완료: " + prefabPath + " → " + MobName + " / " + controller.name);
            else Debug.LogError("프리팹 저장 실패: " + prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }


    private static float Smooth(float x) { x = Mathf.Clamp01(x); return x * x * (3 - 2 * x); }
    private static Vector2 Rotate(Vector2 v, float degrees) { float r = degrees * Mathf.Deg2Rad; return new Vector2(v.x * Mathf.Cos(r) - v.y * Mathf.Sin(r), v.x * Mathf.Sin(r) + v.y * Mathf.Cos(r)); }
    private static float WorldAngle(Pose p, int i) { return p.angle[i] + (Parent[i] < 0 ? 0 : WorldAngle(p, Parent[i])); }
    private static Vector2 World(Pose p, int i) { return Parent[i] < 0 ? (Vector2)p.position[i] : World(p, Parent[i]) + Rotate(p.position[i], WorldAngle(p, Parent[i])); }
    private static float Angle(Vector2 v) { return Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg; }
    // Analytic two-bone solve: preserves segment local positions and bone lengths.
    private static void Solve(Pose p, int upper, int lower, int tip, Vector2 target, float bend, float softExtension = 0f)
    {
        Vector2 delta = target - World(p, upper); float l1 = ((Vector2)RestPositions[lower]).magnitude, l2 = ((Vector2)RestPositions[tip]).magnitude;
        float distance = delta.magnitude;
        // Ease into full extension instead of abruptly hitting the IK reach limit.
        // Only Run opts in; other animations retain their original solve.
        if (softExtension > 0f)
        {
            float start = l1 + l2 - softExtension;
            if (distance > start)
                distance = start + softExtension * (1f - Mathf.Exp(-(distance - start) / softExtension));
        }
        float d = Mathf.Clamp(distance, Mathf.Abs(l1 - l2) + .0001f, l1 + l2 - .0001f);
        float a = Mathf.Acos(Mathf.Clamp((l1 * l1 + d * d - l2 * l2) / (2 * l1 * d), -1, 1)) * Mathf.Rad2Deg;
        float direction = Angle(delta) + bend * a;
        float parent = Parent[upper] < 0 ? 0 : WorldAngle(p, Parent[upper]);
        p.angle[upper] = Mathf.DeltaAngle(0, direction - Angle(RestPositions[lower]) - parent);
        Vector2 elbow = World(p, upper) + Rotate(RestPositions[lower], WorldAngle(p, upper));
        Vector2 clampedTarget = World(p, upper) + (delta.sqrMagnitude > .000001f ? delta.normalized : Vector2.down) * d;
        p.angle[lower] = Mathf.DeltaAngle(0, Angle(clampedTarget - elbow) - Angle(RestPositions[tip]) - WorldAngle(p, upper));
    }
    private static void Foot(Pose p, bool front, float x, float lift, float softExtension = 0f)
    {
        int thigh = front ? TF : TB, calf = front ? CF : CB, foot = front ? FF : FB;
        Vector2 rest = World(new Pose(), foot);
        Solve(p, thigh, calf, foot, rest + new Vector2(x, lift), 1, softExtension);
        p.angle[foot] = -WorldAngle(p, calf);
    }
    // Standing actions use the rest legs. Counter torso motion at the pelvis
    // instead of introducing a knee bend through the foot IK solver.
    private static void Ground(Pose p)
    {
        Vector2 pelvis = Rotate(World(new Pose(), Bottom) - World(p, Body), -WorldAngle(p, Body));
        p.position[Bottom] = new Vector3(pelvis.x, pelvis.y, RestPositions[Bottom].z);
        p.angle[Bottom] = -WorldAngle(p, Body);
        for (int bone = TF; bone <= FB; bone++)
        {
            p.position[bone] = RestPositions[bone];
            p.angle[bone] = RestAngles[bone];
        }
    }
    private static Pose Idle(float t)
    {
        var p = new Pose();
        float b = .5f - .5f * Mathf.Cos(2 * Mathf.PI * t / 2.8f);
        // Shallow torso breathing. Keep legs at rest instead of solving bent knees.
        float breath = -.015f * b;
        p.position[Body].y = RestPositions[Body].y + breath;
        // Counter the parent translation at the pelvis so the feet stay on the floor.
        p.position[Bottom].y = RestPositions[Bottom].y - breath;
        p.angle[Cape] = 0.6f * b;
        return p;
    }
    private static Pose Locomotion(float t, float duration, bool run, bool phase3 = false)
    {
        var p = new Pose(); float u = t / duration, w = Mathf.Sin(u * 2 * Mathf.PI);
        p.angle[Body] = (phase3 ? -24 : run ? -7 : -4) + (phase3 ? 1f : run ? .7f : 1.3f) * w; p.angle[Bottom] = -p.angle[Body] * .8f; p.angle[Head] = -p.angle[Body] * .7f;
        // Keep an upright pelvis; only the stepping leg bends to clear the floor.
        p.position[Body].y = RestPositions[Body].y;
        Ground(p);
        if (run)
        {
            // Two weight transfers per cycle: settle, push off, brief flight.
            float beat = Mathf.Repeat(u * 2f, 1f);
            float bounce;
            if (beat < .16f) bounce = Mathf.Lerp(0f, -.008f, Smooth(beat / .16f));
            else if (beat < .76f) bounce = Mathf.Lerp(-.008f, .035f, Smooth((beat - .16f) / .60f));
            else bounce = Mathf.Lerp(.035f, 0f, Smooth((beat - .76f) / .24f));
            // Apply after pelvis compensation so hips participate in the stride.
            p.position[Body].y += bounce;
        }
        p.angle[SF] = -8 + (run ? 10 : 9) * w; p.angle[AF] = 15 - 5 * w; p.angle[SB] = 12 - 4 * w; p.angle[AB] = 20 + 3 * w; p.angle[HB] = RestHandBAngle - 10f;
        if (phase3)
        {
            p.angle[SF] = -24f + 9f * w;
            p.angle[AF] = 22f - 4f * w;
            p.angle[SB] = 5f - 3f * w;
            p.angle[AB] = 26f + 2f * w;
        }
        p.angle[Cape] = phase3 ? 3f - 6f * w : run ? -6f + 3f * Mathf.Sin(u * 2 * Mathf.PI - .5f) : 0f;
        Step(p, true, u, run); Step(p, false, u + .5f, run);
        if (!run) p.angle[FB] = 0f;
        return p;
    }
    private static void Step(Pose p, bool front, float phase, bool run)
    {
        float u = Mathf.Repeat(phase, 1), stride = run ? .46f : .19f, x, lift;
        if (!run)
        {
            if (u < .5f) { x = Mathf.Lerp(stride, -stride, u * 2); lift = 0; }
            else { float v = (u - .5f) * 2; x = Mathf.Lerp(-stride, stride, Smooth(v)); lift = .08f * Mathf.Pow(Mathf.Sin(v * Mathf.PI), 2); }
            Foot(p, front, x, lift);
            return;
        }

        // Shorter ground contact leaves an airborne gap between alternate steps.
        const float stance = .36f;
        if (u < stance)
        {
            x = Mathf.Lerp(stride, -stride, Smooth(u / stance));
            lift = 0f;
        }
        else
        {
            float v = (u - stance) / (1f - stance);
            x = Mathf.Lerp(-stride, stride, Smooth(v));
            // Recover early, then lower gently into the next light step.
            lift = v < .42f
                ? .25f * Smooth(v / .42f)
                : .25f * (1f - Smooth((v - .42f) / .58f));
        }
        Foot(p, front, x, lift, .08f);
        // Foot articulation in world space; opposite phases share the same roll.
        // +X facing: negative rolls the toe down for push-off.
        float footRoll;
        if (u < stance)
        {
            float contact = u / stance;
            if (contact < .22f)
                footRoll = Mathf.Lerp(5f, 0f, Smooth(contact / .22f));
            else if (contact < .55f)
                footRoll = 0f;
            else
                footRoll = Mathf.Lerp(0f, -17f, Smooth((contact - .55f) / .45f));
        }
        else
        {
            float recovery = (u - stance) / (1f - stance);
            // Keep the toe down longer before lifting it during recovery.
            if (recovery < .20f)
                footRoll = -17f;
            else if (recovery < .68f)
                footRoll = Mathf.Lerp(-17f, 10f, Smooth((recovery - .20f) / .48f));
            else
                footRoll = Mathf.Lerp(10f, 5f, Smooth((recovery - .68f) / .32f));
        }
        int foot = front ? FF : FB, calf = front ? CF : CB;
        p.angle[foot] = footRoll - WorldAngle(p, calf);
    }

    // Key: time, body angle, crouch, rear shoulder/forearm/wrist, sword world angle, front shoulder/forearm.
    private struct Key
    {
        public float t, body, y, sb, ab, hb, sword, sf, af;
        public Key(float t, float body, float y, float sb, float ab, float hb, float sword, float sf, float af) { this.t = t; this.body = body; this.y = y; this.sb = sb; this.ab = ab; this.hb = hb; this.sword = sword; this.sf = sf; this.af = af; }
    }
    private static Key Rest(float t) { return new Key(t, 0, 0, 0, 0, 0, 90 + RestHandBAngle, 0, 0); }
    private static Pose Keys(float t, Key[] keys)
    {
        int i = 1; while (i < keys.Length - 1 && t > keys[i].t) i++;
        Key a = keys[i - 1], b = keys[i]; float u = Smooth((t - a.t) / (b.t - a.t));
        var p = new Pose(); p.angle[Body] = Mathf.Lerp(a.body, b.body, u); p.position[Body].y = RestPositions[Body].y;
        p.angle[Head] = -p.angle[Body] * .7f; p.angle[Bottom] = -p.angle[Body] * .8f;
        p.angle[SB] = Mathf.Lerp(a.sb, b.sb, u); p.angle[AB] = Mathf.Lerp(a.ab, b.ab, u); p.angle[HB] = RestHandBAngle + Mathf.Lerp(a.hb, b.hb, u);
        p.angle[Sword] = Mathf.Lerp(a.sword, b.sword, u) - WorldAngle(p, HB);
        p.angle[SF] = Mathf.Lerp(a.sf, b.sf, u); p.angle[AF] = Mathf.Lerp(a.af, b.af, u);
        p.angle[Cape] = -p.angle[Body] * .45f;
        Ground(p); return p;
    }
    private static Key RestSpin(float t, float turns) { return new Key(t, 0, 0, 0, 0, 0, 90 + RestHandBAngle + turns * 360, 0, 0); }
    private static Pose Attack(float t)
    {
        // Reference pose: hip hand SF=-30/AF=75/HF=30,
        // sword arm SB=45/AB=45/HB=30/Sword=-30.93 (world 89.07).
        Pose p = Keys(t, new[]{Rest(0),
            new Key(.25f,0,0,45,45,0,89.07f,-30,75),
            new Key(.34f,0,0,45,45,18,135,-30,75),
            new Key(.47f,0,0,45,45,5,260,-30,75),
            new Key(.60f,0,0,45,45,0,390,-30,75),
            new Key(.70f,0,0,45,45,0,449.07f,-30,75),
            new Key(.90f,7,0,155,65,0,605,-24,22),
            new Key(.98f,7,0,155,65,0,605,-24,22),
            new Key(AttackImpact,-19,0,15,0,-60,416,-33,26),
            new Key(1.28f,-17,0,15,0,-60,418,-30,24),
            new Key(1.45f,-12,0,15,0,-60,423,-24,22),RestSpin(1.85f,1)});

        // One continuous lift-and-spin instead of easing to rest at each spin key.
        if (t <= .70f)
        {
            float spin = Mathf.Clamp01(t / .70f);
            float lift = Smooth(t / .42f);
            p.angle[SB] = 45f * lift;
            p.angle[AB] = 45f * lift;
            p.angle[SF] = -30f * lift;
            p.angle[AF] = 75f * lift;
            float flick = Mathf.Sin(spin * Mathf.PI);
            p.angle[HB] = RestHandBAngle + 18f * flick * flick;
            float bladeWorld = Mathf.Lerp(90f + RestHandBAngle, 449.07f, Smooth(spin));
            p.angle[Sword] = bladeWorld - WorldAngle(p, HB);
        }
        float ready = Smooth(t / .25f);
        float draw = Smooth((t - .70f) / .20f);
        float strike = Smooth((t - .98f) / (AttackImpact - .98f));
        float recover = Smooth((t - 1.45f) / .40f);
        float active = ready * (1f - recover);
        p.angle[HF] = 30f * Smooth(t / .42f) * (1f - draw) * (1f - recover);

        // The supplied bent rear leg is intentional for this attack only.
        p.angle[TB] = 30f * active;
        p.angle[CB] = -45f * active;
        p.angle[FB] = -45f * active;

        // Move the hips toward +X as the rear leg takes the forward lunge.
        // Resolve a separate landing pose, then blend from the reference stance.
        float transfer = strike * (1f - recover);
        var landing = new Pose();
        landing.angle[Body] = p.angle[Body];
        Ground(landing);
        landing.position[Body].x += .20f;
        landing.position[Body].y -= .18f;
        Foot(landing, true, -.34f, 0f, .06f);
        Foot(landing, false, .44f, 0f, .06f);
        p.position[Body].x += .20f * transfer;
        p.position[Body].y -= .18f * transfer;
        for (int bone = TF; bone <= FB; bone++)
            p.angle[bone] = Mathf.LerpAngle(p.angle[bone], landing.angle[bone], transfer);
        return p;
    }
    private static Pose SpinSkill(float t)
    {
        // Attack-derived windup/strike, then raise the grip while spinning four turns.
        Pose p = Keys(t, new[]{Rest(0),
            new Key(.30f,7,0,155,65,0,245,-24,22),
            new Key(.46f,7,0,155,65,0,245,-24,22),
            new Key(.60f,-19,0,15,0,-60,56,-33,26),
            new Key(.72f,-17,0,15,0,-60,58,-30,24),
            new Key(.96f,0,0,45,45,0,270,-30,75),
            new Key(2.36f,0,0,45,45,0,1710,-30,75),
            new Key(2.64f,0,0,45,45,0,1710,-30,75),
            new Key(3.20f,0,0,45,45,0,1620,-30,75),
            new Key(3.80f,0,0,45,45,0,1620,-30,75),RestSpin(4.50f,4)});

        float stance = Smooth(t / .30f) * (1f - Smooth((t - 3.80f) / .70f));
        p.angle[TB] = 30f * stance;
        p.angle[CB] = -45f * stance;
        p.angle[FB] = -45f * stance;
        float slash = Smooth((t - .46f) / .14f) * (1f - Smooth((t - .72f) / .24f));
        var landing = new Pose();
        landing.angle[Body] = p.angle[Body];
        Ground(landing);
        landing.position[Body].x += .20f;
        landing.position[Body].y -= .18f;
        Foot(landing, true, -.34f, 0f, .06f);
        Foot(landing, false, .44f, 0f, .06f);
        p.position[Body].x += .20f * slash;
        p.position[Body].y -= .18f * slash;
        for (int bone = TF; bone <= FB; bone++)
            p.angle[bone] = Mathf.LerpAngle(p.angle[bone], landing.angle[bone], slash);

        float gripWeight = Smooth((t - .72f) / .24f) * (1f - Smooth((t - 3.80f) / .70f));
        float raise = Smooth((t - .96f) / 1.40f);
        float lower = Smooth((t - 2.64f) / .56f);
        // Recover outward from the strike instead of pulling straight to the chest.
        // The lift starts with an extended arm and folds gradually above the head.
        Vector2 extendedGrip = new Vector2(.94f, .34f);
        Vector2 overheadGrip = new Vector2(.44f, 1.46f);
        Vector2 raisedGrip = Vector2.Lerp(extendedGrip, overheadGrip, raise);
        Vector2 gripLocal = Vector2.Lerp(raisedGrip, new Vector2(.22f, .65f), lower);
        float bladeWorld = WorldAngle(p, Sword);
        // A single continuous curve guarantees exactly four turns in the lift.
        if (t >= .96f && t <= 2.36f)
            bladeWorld = 270f + 1440f * raise;
        var arm = new Pose();
        arm.angle[Body] = p.angle[Body];
        arm.position[Body] = p.position[Body];
        Solve(arm, SB, AB, HB, World(p, Body) + Rotate(gripLocal, p.angle[Body]), -1f);
        p.angle[SB] = Mathf.LerpAngle(p.angle[SB], arm.angle[SB], gripWeight);
        p.angle[AB] = Mathf.LerpAngle(p.angle[AB], arm.angle[AB], gripWeight);
        p.angle[Sword] = bladeWorld - WorldAngle(p, HB);
        p.angle[HF] = 30f * gripWeight;
        // Frame 36 at 60 fps: hold the strike, then lift the extended arm
        // mainly through the shoulder, with a gradual positive forearm fold.
        if (t >= .60f && t <= 2.64f)
        {
            float shoulderLift = Smooth((t - .80f) / 1.56f);
            p.angle[SB] = Mathf.Lerp(15f, 145f, shoulderLift);
            p.angle[AB] = 20f * shoulderLift;
            p.angle[HB] = -30f;
            float blade;
            if (t <= .80f) blade = 56f;
            else if (t <= 2.16f)
                blade = 56f + 1440f * Smooth((t - .80f) / 1.36f);
            else
                // After four full turns, align the point backward horizontally.
                blade = Mathf.Lerp(1496f, 1710f, Smooth((t - 2.16f) / .20f));
            p.angle[Sword] = blade - WorldAngle(p, HB);
        }
        else if (t > 2.64f && t < 3.20f)
        {
            // Use one fixed endpoint: a moving IK target can cross the +/-180
            // shortest-path boundary and flip the interpolation between frames.
            var returnPose = new Pose();
            Solve(returnPose, SB, AB, HB, new Vector2(.22f, .65f), -1f);
            float returnToGrip = Smooth((t - 2.64f) / .56f);
            float shoulderReturn = Mathf.DeltaAngle(145f, returnPose.angle[SB]);
            // Explicitly take the opposite arc to the previous shortest-path return.
            shoulderReturn += shoulderReturn >= 0f ? -360f : 360f;
            p.angle[SB] = 145f + shoulderReturn * returnToGrip;
            p.angle[AB] = 20f + Mathf.DeltaAngle(20f, returnPose.angle[AB]) * returnToGrip;
            p.angle[HB] = Mathf.Lerp(-30f, RestHandBAngle, returnToGrip);
            p.angle[Sword] = bladeWorld - WorldAngle(p, HB);
        }
        return p;
    }
    private static Pose ThrustRush(float t)
    {
        var keys = new List<Key> { Rest(0), new Key(.22f, 3, 0, -12, 105, 0, 90, -24, 35) };
        // Smooth extension and a slightly longer recovery, without frame-count constraints.
        float[] impacts = { .34f, .59f, .84f, 1.09f };
        for (int i = 0; i < impacts.Length; i++)
        {
            float impact = impacts[i];
            keys.Add(new Key(impact - .10f, 3, 0, -12, 105, 0, 90, -24, 35));
            keys.Add(new Key(impact, -13, 0, 90, 0, 0, 90, -34, 25));
            keys.Add(new Key(impact + .015f, -13, 0, 90, 0, 0, 90, -34, 25));
            if (i + 1 < impacts.Length)
                keys.Add(new Key(impact + .135f, 3, 0, -12, 105, 0, 90, -24, 35));
        }
        keys.Add(new Key(1.29f, 3, 0, -12, 105, 0, 90, -24, 35));
        keys.Add(new Key(Skill2DashStart, -28, 0, 90, 0, 0, 90, -45, 30));
        keys.Add(new Key(Skill2DashEnd, -28, 0, 90, 0, 0, 90, -45, 30));
        keys.Add(new Key(2.06f, -17, 0, 65, 5, 0, 95, -20, 30));
        keys.Add(Rest(2.60f));
        Pose p = Keys(t, keys.ToArray());

        float stance = Smooth(t / .22f) * (1f - Smooth((t - Skill2DashEnd) / (2.60f - Skill2DashEnd)));
        float dash = Smooth((t - 1.22f) / (Skill2DashStart - 1.22f))
            * (1f - Smooth((t - Skill2DashEnd) / (2.60f - Skill2DashEnd)));
        // Lower the pelvis enough to reach a wide stance, then solve the ankles
        // at their own rest heights. Horizontal offsets remain fixed during stabs.
        p.position[Body].y -= .18f * stance + .16f * dash;
        // Use the supplied front-leg LOCAL angles, rather than solving them away.
        p.angle[TF] = -20f * stance;
        p.angle[CF] = -15f * stance;
        p.angle[FF] = 0f;
        // Keep the front ankle at its rest height without changing those angles.
        // Then ground the rear leg against its own rest-height target.
        p.position[Body].y += World(new Pose(), FF).y - World(p, FF).y;
        Foot(p, false, .38f * stance, 0f);
        // Leave the planted thrust stance for the airborne rushing silhouette.
        float airborne = Smooth((t - 1.29f) / (Skill2DashStart - 1.29f))
            * (1f - Smooth((t - Skill2DashEnd) / .24f));
        float pelvisAngle = WorldAngle(p, Bottom);
        float straightFront = Mathf.DeltaAngle(Angle(RestPositions[FF]), Angle(RestPositions[CF]));
        p.angle[TF] = Mathf.LerpAngle(p.angle[TF], -48f - pelvisAngle, airborne);
        p.angle[CF] = Mathf.LerpAngle(p.angle[CF], straightFront, airborne);
        p.angle[TB] = Mathf.LerpAngle(p.angle[TB], 78f - pelvisAngle, airborne);
        p.angle[CB] = Mathf.LerpAngle(p.angle[CB], -112f, airborne);
        p.angle[FF] = Mathf.LerpAngle(p.angle[FF], -18f - WorldAngle(p, CF), airborne);
        p.angle[FB] = Mathf.LerpAngle(p.angle[FB], 18f - WorldAngle(p, CB), airborne);
        // Lift the visual pose enough that neither foot touches its rest floor.
        var rest = new Pose();
        float clearance = Mathf.Max(0f, Mathf.Max(
            World(rest, FF).y + .12f - World(p, FF).y,
            World(rest, FB).y + .12f - World(p, FB).y));
        p.position[Body].y += clearance * airborne;
        p.angle[Cape] = Mathf.Lerp(p.angle[Cape], -40f, dash);
        // Aim the complete sword arm down toward the smaller player's torso.
        // Rotating at the shoulder retains the wrist, elbow and weapon attachment.
        p.angle[SB] -= 30f * stance;
        return p;
    }
    private static Pose RushRetreat(float t)
    {
        const float land = 1.08f;
        Pose p = Keys(t, new[]{Rest(0),new Key(.25f,5,0,20,40,0,90,-20,40),
            new Key(Skill3DashStart,-28,0,90,0,0,90,-45,30),
            new Key(Skill3DashEnd,-28,0,90,0,0,90,-45,30),
            new Key(land,-8,0,70,0,0,90,-25,30),
            new Key(RetreatStart,0,0,70,0,0,90,20,35),
            new Key(RetreatEnd,0,0,70,0,0,810,20,35),
            new Key(1.94f,0,0,70,0,0,900,10,30),RestSpin(2.55f,2)});

        // Reuse the exact Skill2 airborne dash silhouette, including both feet.
        Pose flying = ThrustRush((Skill2DashStart + Skill2DashEnd) * .5f);
        float flight = Smooth((t - .30f) / .12f)
            * (1f - Smooth((t - Skill3DashEnd) / (land - Skill3DashEnd)));
        if (t < Skill3DashStart)
        {
            // Brief grounded coil, then the same fast 0.12-second launch.
            float prepare = Smooth(t / .22f);
            p = new Pose();
            p.angle[Body] = -12f * prepare;
            p.angle[Head] = 6f * prepare;
            Ground(p);
            p.position[Body].y -= .20f * prepare;
            Foot(p, true, -.12f * prepare, 0f);
            Foot(p, false, .12f * prepare, 0f);
            p.angle[SB] = -12f * prepare;
            p.angle[AB] = 105f * prepare;
            p.angle[HB] = RestHandBAngle;
            p.angle[SF] = -24f * prepare;
            p.angle[AF] = 35f * prepare;
            p.angle[Sword] = Mathf.Lerp(90f + RestHandBAngle, 90f, prepare) - WorldAngle(p, HB);
            p.angle[Cape] = -8f * prepare;
        }
        for (int bone = 0; bone < Paths.Length; bone++)
        {
            p.position[bone] = Vector3.Lerp(p.position[bone], flying.position[bone], flight);
            p.angle[bone] = Mathf.LerpAngle(p.angle[bone], flying.angle[bone], flight);
        }

        // Land, then push backward almost immediately. Local leg rotations
        // are the supplied reference, not overwritten by grounded leg IK.
        float back = Smooth((t - land) / .12f)
            * (1f - Smooth((t - RetreatEnd) / (2.55f - RetreatEnd)));
        p.position[Bottom] = Vector3.Lerp(p.position[Bottom], RestPositions[Bottom], back);
        p.angle[Bottom] = Mathf.LerpAngle(p.angle[Bottom], 45f, back);
        p.angle[TF] = Mathf.LerpAngle(p.angle[TF], 0f, back);
        p.angle[CF] = Mathf.LerpAngle(p.angle[CF], 0f, back);
        p.angle[FF] = Mathf.LerpAngle(p.angle[FF], 0f, back);
        p.angle[TB] = Mathf.LerpAngle(p.angle[TB], 30f, back);
        p.angle[CB] = Mathf.LerpAngle(p.angle[CB], -90f, back);
        p.angle[FB] = Mathf.LerpAngle(p.angle[FB], 0f, back);
        p.angle[Cape] = Mathf.LerpAngle(p.angle[Cape], 30f, back);
        float bladeBeforeArm = WorldAngle(p, Sword);
        p.angle[SB] = Mathf.LerpAngle(p.angle[SB], 70f, back);
        p.angle[AB] = Mathf.LerpAngle(p.angle[AB], 0f, back);
        p.angle[HB] = Mathf.LerpAngle(p.angle[HB], RestHandBAngle, back);
        p.angle[Sword] = bladeBeforeArm - WorldAngle(p, HB);
        if (t >= RetreatStart && t <= RetreatEnd)
        {
            float spin = Smooth((t - RetreatStart) / (RetreatEnd - RetreatStart));
            p.angle[Sword] = 90f + 720f * spin - WorldAngle(p, HB);
        }
        return p;
    }
    private static Pose Buff(float t)
    {
        // Gather the cape, then flick the free arm outward in a short accent.
        Pose p = Keys(t, new[]{Rest(0),
            new Key(.38f,0,0,45,45,0,180,-24,65),
            new Key(.55f,0,0,45,45,0,180,-18,75),
            new Key(.68f,-2,0,45,45,0,180,-100,-12),
            new Key(.86f,-2,0,45,45,0,180,-107,-15),
            new Key(1.15f,-2,0,45,45,0,180,-100,-12),
            new Key(1.36f,-2,0,45,45,0,180,-100,-12),
            new Key(1.62f,0,0,45,45,0,180,-60,10),Rest(2.20f)});
        float poseWeight = Smooth(t / .38f) * (1f - Smooth((t - 1.62f) / .58f));
        p.angle[TB] = 30f * poseWeight;
        p.angle[CB] = -45f * poseWeight;
        p.angle[FB] = -45f * poseWeight;

        // Bring the grip beside the torso without stretching the arm segments.
        float swordWorld = WorldAngle(p, Sword);
        var gripPose = new Pose();
        gripPose.angle[Body] = p.angle[Body];
        Vector2 grip = World(p, Body) + Rotate(new Vector2(.22f, .65f), p.angle[Body]);
        // Choose the lower elbow solution while keeping the same grip target.
        Solve(gripPose, SB, AB, HB, grip, -1f);
        p.angle[SB] = Mathf.LerpAngle(p.angle[SB], gripPose.angle[SB], poseWeight);
        p.angle[AB] = Mathf.LerpAngle(p.angle[AB], gripPose.angle[AB], poseWeight);
        p.angle[Sword] = swordWorld - WorldAngle(p, HB);

        float flick = Smooth((t - .55f) / .13f);
        p.angle[HF] = Mathf.Lerp(20f * poseWeight, -16f, flick);
        // Snap open at .68, hold briefly, then use one uninterrupted return.
        if (t >= .68f)
        {
            float armReturn = Smooth((t - .92f) / 1.28f);
            p.angle[SF] = Mathf.Lerp(-100f, RestAngles[SF], armReturn);
            p.angle[AF] = Mathf.Lerp(-12f, RestAngles[AF], armReturn);
            p.angle[HF] = Mathf.Lerp(-16f, RestAngles[HF], armReturn);
        }
        // Cape starts after the arm and peaks after the hand has snapped open.
        float cape;
        if (t < .62f) cape = 5f * Smooth(t / .38f);
        else if (t < .90f)
        {
            // One uninterrupted rise: slow only at the single peak.
            float rise = Mathf.Clamp01((t - .62f) / .28f);
            cape = Mathf.Lerp(5f, -63f, Smooth(rise));
        }
        else
        {
            // Immediately fall all the way to rest; no intermediate pose or hold.
            float fall = Mathf.Clamp01((t - .90f) / 1.30f);
            cape = Mathf.Lerp(-63f, 0f, Smooth(fall));
        }
        p.angle[Cape] = cape;
        return p;
    }
    private static Pose Hit(float t)
    {
        float k = t < .07f ? Smooth(t / .07f) : 1 - Smooth((t - .07f) / .29f); var p = new Pose();
        p.angle[Body] = 7 * k; p.position[Body].y = RestPositions[Body].y; p.angle[Cape] = -8 * k; p.angle[Head] = 5 * k; p.angle[SF] = -12 * k; p.angle[AB] = 15 * k; Ground(p); return p;
    }
    private static Pose Death(float t)
    {
        var p = new Pose();
        float kneel = Smooth((t - .08f) / .52f);
        float fall = Smooth((t - .60f) / .62f);
        float settle = Smooth((t - 1.22f) / .25f);
        var rest = new Pose();
        // Kneel first, then carry the pelvis forward and down into a full prone fall.
        Vector2 kneeTarget = World(rest, FF) + new Vector2(.32f, .035f);
        Vector2 hipTarget = kneeTarget - (Vector2)RestPositions[TF] - Rotate(RestPositions[CF], 55f);
        Vector2 hip = Vector2.Lerp(World(rest, Bottom), hipTarget, kneel);
        Vector2 proneHip = new Vector2(hipTarget.x + .85f, World(rest, FF).y + .18f);
        hip = Vector2.Lerp(hip, proneHip, fall);
        hip.y -= .035f * settle;
        p.angle[Body] = -12f * kneel - 75f * fall - 3f * settle;
        // Release the pelvis counter-rotation so the lower body also tips forward.
        p.angle[Bottom] = -p.angle[Body] * (1f - fall);
        Vector2 body = hip - Rotate(RestPositions[Bottom], p.angle[Body]);
        p.position[Body] = new Vector3(body.x, body.y, RestPositions[Body].z);
        float unfold = Smooth((t - .70f) / .67f);
        p.angle[TF] = Mathf.Lerp(55f * kneel, 0f, unfold);
        p.angle[CF] = Mathf.Lerp(-145f * kneel, 0f, unfold);
        p.angle[FF] = Mathf.Lerp(90f * kneel, 12f, unfold);
        p.angle[TB] = Mathf.Lerp(50f * kneel, 3f, unfold);
        p.angle[CB] = Mathf.Lerp(-140f * kneel, -6f, unfold);
        p.angle[FB] = Mathf.Lerp(90f * kneel, 12f, unfold);
        p.angle[Head] = -8f * kneel - 12f * fall + 3f * settle;
        // Front hand braces the fall; weapon hand is pulled to the chest and follows the torso.
        p.angle[SF] = 18f * kneel + 42f * fall;
        p.angle[AF] = -22f * kneel - 28f * fall;
        p.angle[HF] = 12f * fall;
        float clutch = Smooth((t - .32f) / .42f);
        // Body-local target keeps the hand against the chest through the entire fall.
        Vector2 restHand = World(rest, HB) - (Vector2)rest.position[Body];
        Vector2 chestHand = Vector2.Lerp(restHand, new Vector2(.04f, .55f), clutch);
        Vector2 target = (Vector2)p.position[Body] + Rotate(chestHand, p.angle[Body]);
        Solve(p, SB, AB, HB, target, 1f);
        float chestShoulder = p.angle[SB], chestForearm = p.angle[AB];
        // Lead with elbow flexion, keeping the shoulder nearly still initially.
        float elbowFold = Smooth((t - .32f) / .30f);
        float chestSettle = Smooth((t - .60f) / .34f);
        float leadShoulder = 3f * elbowFold;
        float leadForearm = -130f * elbowFold;
        p.angle[SB] = leadShoulder + Mathf.DeltaAngle(leadShoulder, chestShoulder) * chestSettle;
        p.angle[AB] = leadForearm + Mathf.DeltaAngle(leadForearm, chestForearm) * chestSettle;
        // Keep the wrist relaxed relative to the torso as the elbow folds.
        p.angle[HB] = RestAngles[HB] + p.angle[Body] - WorldAngle(p, AB);
        float blade = RestAngles[Sword] + RestAngles[HB];
        blade = Mathf.Lerp(blade, 92f, Smooth((t - .60f) / .85f));
        p.angle[Sword] = blade - WorldAngle(p, HB);
        // Cape follows the torso after a short delay, then rests along the back.
        float capeFall = Smooth((t - .73f) / .68f);
        p.angle[Cape] = 22f * fall - 30f * capeFall;
        return p; // Hold the prone pose; no return to rest.
    }

    private static AnimationClip Generate(string name, float duration, bool loop, Func<float, Pose> evaluate)
    {
        var clip = new AnimationClip { name = name, frameRate = SampleRate };
        // 주요 타이밍도 포함하여 임팩트가 샘플 사이로 밀리지 않게 합니다.
        var timeSet = new SortedSet<float> { 0, duration };
        int count = Mathf.RoundToInt(duration * SampleRate);
        for (int i = 1; i < count; i++) timeSet.Add(duration * i / count);
        var times = new List<float>(timeSet);
        var poses = new Pose[times.Count];
        for (int i = 0; i < times.Count; i++)
            poses[i] = evaluate(loop && i == times.Count - 1 ? 0 : times[i]);
        // Unwrap Euler curves across +/-180 so IK never produces a one-frame full turn.
        for (int part = 0; part < Paths.Length; part++)
            for (int i = 1; i < poses.Length; i++)
                poses[i].angle[part] = poses[i - 1].angle[part] + Mathf.DeltaAngle(poses[i - 1].angle[part], poses[i].angle[part]);
        for (int part = 0; part < Paths.Length; part++)
        {
            for (int axis = 0; axis < 3; axis++)
            {
                var keys = new Keyframe[times.Count];
                for (int i = 0; i < times.Count; i++) keys[i] = new Keyframe(times[i], poses[i].position[part][axis]);
                SetCurve(clip, Paths[part], "m_LocalPosition." + "xyz"[axis], keys);
                for (int i = 0; i < times.Count; i++) keys[i] = new Keyframe(times[i], axis == 2 ? poses[i].angle[part] : 0);
                SetCurve(clip, Paths[part], "localEulerAnglesRaw." + "xyz"[axis], keys);
                SetCurve(clip, Paths[part], "m_LocalScale." + "xyz"[axis], new[] { new Keyframe(0, 1), new Keyframe(duration, 1) });
            }
        }
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        settings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        // 같은 에셋을 갱신하여 Animator 등의 GUID 참조를 유지합니다.
        string path = OutputFolder + "/" + name + ".anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null)
        {
            var events = AnimationUtility.GetAnimationEvents(existing);
            Undo.RecordObject(existing, "Regenerate " + name);
            EditorUtility.CopySerialized(clip, existing);
            AnimationUtility.SetAnimationEvents(existing, events);
            EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(clip);
            return existing;
        }
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    private static void SetCurve(AnimationClip clip, string path, string property, Keyframe[] keys)
    {
        var curve = new AnimationCurve(keys);
        // 촘촘한 샘플 사이 선형 보간: 자동 탄젠트에 의한 회전/발 높이 오버슈트 방지.
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string path = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = path + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(path, parts[i]);
            path = next;
        }
    }
}
#endif
