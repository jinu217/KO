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
    private static readonly string[] Names = { "Idle", "Walk", "Run", "Attack", "Skill1", "Skill2", "Skill3", "Hit", "Death", "Buff" };
    private static readonly float[] Durations = { 2.8f, 1.1f, .70f, 1.85f, 2.65f, 2.60f, 2.55f, .36f, 1.65f, 2.20f };
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
    private static AnimationClip Make(int i)
    {
        return Generate("MainBoss3_" + Names[i], Durations[i], i < 3, t => Evaluate(i, t));
    }
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
        };
        for (int i = 0; i < states.Length; i++)
        {
            foreach (var child in sm.states) if (child.state.name == "MainBoss3_" + Names[i]) states[i] = child.state;
            if (states[i] == null) states[i] = sm.AddState("MainBoss3_" + Names[i], layout[i]);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(OutputFolder + "/MainBoss3_" + Names[i] + ".anim");
            if (clip == null) clip = Make(i);
            states[i].motion = clip; states[i].speed = 1; states[i].speedParameter = "AttackSpeed"; states[i].speedParameterActive = (i >= 3 && i <= 6) || i == 9; states[i].writeDefaultValues = false;
            foreach (var tr in states[i].transitions) if (tr.name.StartsWith("MainBoss3_Generated_")) states[i].RemoveTransition(tr);
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = i < 3; AnimationUtility.SetAnimationClipSettings(clip, settings); EditorUtility.SetDirty(clip);
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
            for (int to = 0; to < 3; to++)
            {
                if (from == to) continue;
                var tr = states[from].AddTransition(states[to]); Configure(tr, from >= 3);
                tr.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                tr.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking");
                tr.AddCondition(to == 0 ? AnimatorConditionMode.Less : AnimatorConditionMode.Greater, .01f, "MoveSpeed");
                if (to != 0) tr.AddCondition(to == 2 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "IsChasing");
            }
            if (from >= 3) continue;
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
    private static void Solve(Pose p, int upper, int lower, int tip, Vector2 target, float bend)
    {
        Vector2 delta = target - World(p, upper); float l1 = ((Vector2)RestPositions[lower]).magnitude, l2 = ((Vector2)RestPositions[tip]).magnitude;
        float d = Mathf.Clamp(delta.magnitude, Mathf.Abs(l1 - l2) + .0001f, l1 + l2 - .0001f);
        float a = Mathf.Acos(Mathf.Clamp((l1 * l1 + d * d - l2 * l2) / (2 * l1 * d), -1, 1)) * Mathf.Rad2Deg;
        float direction = Angle(delta) + bend * a;
        float parent = Parent[upper] < 0 ? 0 : WorldAngle(p, Parent[upper]);
        p.angle[upper] = Mathf.DeltaAngle(0, direction - Angle(RestPositions[lower]) - parent);
        Vector2 elbow = World(p, upper) + Rotate(RestPositions[lower], WorldAngle(p, upper));
        Vector2 clampedTarget = World(p, upper) + (delta.sqrMagnitude > .000001f ? delta.normalized : Vector2.down) * d;
        p.angle[lower] = Mathf.DeltaAngle(0, Angle(clampedTarget - elbow) - Angle(RestPositions[tip]) - WorldAngle(p, upper));
    }
    private static void Foot(Pose p, bool front, float x, float lift)
    {
        int thigh = front ? TF : TB, calf = front ? CF : CB, foot = front ? FF : FB;
        Vector2 rest = World(new Pose(), foot);
        Solve(p, thigh, calf, foot, rest + new Vector2(x, lift), 1);
        p.angle[foot] = -WorldAngle(p, calf);
    }
    private static void Ground(Pose p) { Foot(p, true, 0, 0); Foot(p, false, 0, 0); }
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
    private static Pose Locomotion(float t, float duration, bool run)
    {
        var p = new Pose(); float u = t / duration, w = Mathf.Sin(u * 2 * Mathf.PI);
        p.angle[Body] = (run ? -11 : -4) + 1.3f * w; p.angle[Bottom] = -p.angle[Body] * .8f; p.angle[Head] = -p.angle[Body] * .7f;
        p.position[Body].y = -.05f - (.5f - .5f * Mathf.Cos(u * 4 * Mathf.PI)) * (run ? .055f : .025f);
        p.angle[SF] = -8 + (run ? 15 : 9) * w; p.angle[AF] = 15 - 5 * w; p.angle[SB] = 12 - 4 * w; p.angle[AB] = 20 + 3 * w; p.angle[HB] = RestHandBAngle - 10f;
        p.angle[Cape] = run ? -6f + 3f * Mathf.Sin(u * 2 * Mathf.PI - .5f) : 0f;
        Step(p, true, u, run); Step(p, false, u + .5f, run);
        if (!run) p.angle[FB] = 0f;
        return p;
    }
    private static void Step(Pose p, bool front, float phase, bool run)
    {
        float u = Mathf.Repeat(phase, 1), stride = run ? .28f : .19f, x, lift;
        if (u < .5f) { x = Mathf.Lerp(stride, -stride, u * 2); lift = 0; }
        else { float v = (u - .5f) * 2; x = Mathf.Lerp(-stride, stride, Smooth(v)); lift = (run ? .14f : .08f) * Mathf.Pow(Mathf.Sin(v * Mathf.PI), 2); }
        Foot(p, front, x, lift);
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
        var p = new Pose(); p.angle[Body] = Mathf.Lerp(a.body, b.body, u); p.position[Body].y = Mathf.Lerp(a.y, b.y, u);
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
        // Extend arm, rotate wrist once, then deliver a broad slash.
        return Keys(t, new[]{Rest(0),new Key(.25f,-3,-.04f,65,10,0,90,-15,30),
            new Key(.40f,-3,-.04f,65,10,0,180,-15,30),new Key(.55f,-3,-.04f,65,10,0,300,-15,30),
            new Key(.70f,-3,-.04f,65,10,0,450,-15,30),new Key(.90f,4,-.08f,125,25,0,570,-20,40),
            new Key(AttackImpact,-16,-.16f,65,-20,0,420,20,20),new Key(1.28f,-15,-.15f,60,-20,0,410,20,20),RestSpin(1.85f,1)});
    }
    private static Pose SpinSkill(float t)
    {
        // Downward slash, lift back overhead and rotate multiple times before upright finish.
        return Keys(t, new[]{Rest(0),new Key(.35f,4,-.06f,115,30,0,230,-20,30),
            new Key(.55f,-17,-.17f,60,-25,0,60,20,25),new Key(.72f,-16,-.16f,60,-25,0,60,20,25),
            new Key(1.00f,0,-.06f,130,10,0,230,-10,40),
            new Key(1.16f,0,-.06f,130,10,0,410,-10,40),new Key(1.32f,0,-.06f,130,10,0,590,-10,40),
            new Key(1.48f,0,-.06f,130,10,0,770,-10,40),new Key(1.64f,0,-.06f,130,10,0,950,-10,40),
            new Key(1.88f,0,-.04f,70,25,0,900,-5,30),new Key(2.12f,0,-.03f,70,25,0,900,-5,30),RestSpin(2.65f,2)});
    }
    private static Pose ThrustRush(float t)
    {
        // Each stab has its own retract/extend pair. Blade remains aimed +X.
        return Keys(t, new[]{Rest(0),new Key(.22f,3,-.10f,15,45,0,90,-22,45),
            new Key(.34f,-13,-.14f,70,-10,0,90,-32,45),new Key(.46f,0,-.10f,15,45,0,90,-22,45),
            new Key(.58f,-13,-.14f,70,-10,0,90,-32,45),new Key(.70f,0,-.10f,15,45,0,90,-22,45),
            new Key(.82f,-13,-.14f,70,-10,0,90,-32,45),new Key(.94f,0,-.10f,15,45,0,90,-22,45),
            new Key(1.06f,-13,-.14f,70,-10,0,90,-32,45),new Key(1.22f,-13,-.14f,70,-10,0,90,-32,45),
            new Key(Skill2DashStart,-28,-.23f,85,-8,0,90,-45,30),new Key(Skill2DashEnd,-28,-.23f,85,-8,0,90,-45,30),
            new Key(2.06f,-17,-.15f,65,5,0,95,-20,30),Rest(2.60f)});
    }
    private static Pose RushRetreat(float t)
    {
        Pose p = Keys(t, new[]{Rest(0),new Key(.25f,5,-.13f,20,40,0,90,-20,40),
            new Key(Skill3DashStart,-30,-.24f,85,-8,0,90,-45,30),new Key(Skill3DashEnd,-30,-.24f,85,-8,0,90,-45,30),
            new Key(RetreatStart,4,-.16f,105,20,0,180,20,35),new Key(1.28f,8,-.13f,105,20,0,360,20,35),
            new Key(1.44f,8,-.13f,105,20,0,540,20,35),new Key(1.60f,8,-.13f,105,20,0,720,20,35),
            new Key(RetreatEnd,5,-.12f,80,25,0,900,10,30),RestSpin(2.55f,2)});
        p.angle[Cape] += -12 * Smooth((t - .25f) / .17f) * (1 - Smooth((t - 1.78f) / .4f)); return p;
    }
    private static Pose Buff(float t)
    {
        Pose p = Keys(t, new[]{Rest(0),new Key(.35f,0,-.035f,65,25,0,180,-10,20),
            new Key(.62f,0,-.025f,65,25,0,180,-75,-15),new Key(1.0f,-2,-.02f,65,25,0,180,-90,-10),
            new Key(1.40f,-2,-.02f,65,25,0,180,-90,-10),new Key(1.62f,0,-.025f,65,25,0,180,-60,0),Rest(2.20f)});
        float fan = Smooth((t - .35f) / .65f) * (1 - Smooth((t - 1.40f) / .80f));
        p.angle[Cape] = -52 * fan + 3 * Mathf.Sin(t * 9) * fan; return p;
    }
    private static Pose Hit(float t)
    {
        float k = t < .07f ? Smooth(t / .07f) : 1 - Smooth((t - .07f) / .29f); var p = new Pose();
        p.angle[Body] = 7 * k; p.position[Body].y = -.045f * k; p.angle[Cape] = -8 * k; p.angle[Head] = 5 * k; p.angle[SF] = -12 * k; p.angle[AB] = 15 * k; Ground(p); return p;
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
