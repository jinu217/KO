#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Place in Assets/Editor. Visual root faces +X. Physics movement belongs to boss AI.
// Skill1 = Buff. Skill2 = planted-sword rush. Skill3 = leap slam.
public static class MidBoss3_AnimGenerators
{
    public const string OutputFolder = "Assets/Animations/3/MidBoss 3";
    private const string PrefabFolder = "Assets/Prefab/3", MobName = "MidBoss 3";
    private const int SampleRate = 60;
    public const float AttackHit1 = 0.70f, AttackHit2 = 1.42f;
    public const float BuffImpact = 0.90f;
    public const float DashStart = 1.08f, DashEnd = 2.08f;
    public const float JumpStart = 0.85f, JumpApex = 1.20f, JumpLand = 1.60f;
    private const int Body = 0, Head = 1, SF = 2, AF = 3, HF = 4, SB = 5, AB = 6, HB = 7,
        Sword = 8, Bottom = 9, TF = 10, CF = 11, FF = 12, TB = 13, CB = 14, FB = 15;
    private static readonly int[] Parent = { -1, 0, 0, 2, 3, 0, 5, 6, 7, 0, 9, 10, 11, 9, 13, 14 };
    private static readonly string[] Paths = {
        "Body", "Body/Head", "Body/Shoulder_F", "Body/Shoulder_F/Forearm_F",
        "Body/Shoulder_F/Forearm_F/Hand_F", "Body/Shoulder_B", "Body/Shoulder_B/Forearm_B",
        "Body/Shoulder_B/Forearm_B/Hand_B", "Body/Shoulder_B/Forearm_B/Hand_B/Sword",
        "Body/Bottom", "Body/Bottom/Thigh_F", "Body/Bottom/Thigh_F/Calf_F",
        "Body/Bottom/Thigh_F/Calf_F/Foot_F", "Body/Bottom/Thigh_B",
        "Body/Bottom/Thigh_B/Calf_B", "Body/Bottom/Thigh_B/Calf_B/Foot_B"
    };
    private static readonly Vector3[] RestPositions = {
        new Vector3(0,0,0), new Vector3(.053f,1.078f,0),
        new Vector3(-.618f,.852f,0),new Vector3(-.452f,-.525f,0),new Vector3(.031f,-.457f,0),
        new Vector3(.366f,.805f,0),new Vector3(.392f,-.431f,0),new Vector3(.015f,-.473f,0),
        new Vector3(.183f,-.241f,0),new Vector3(.054f,.009f,0),
        new Vector3(-.463f,-.178f,0),new Vector3(.013f,-.737f,0),new Vector3(-.121f,-.383f,0),
        new Vector3(.257f,-.194f,0),new Vector3(.090f,-.721f,0),new Vector3(-.074f,-.383f,0)
    };
    private static readonly float[] RestAngles = { 0, 0, 0, 0, 0, 0, 0, 0, 120, 0, 0, 0, 0, 0, 0, 0 };
    private sealed class Pose
    {
        public Vector3[] position = (Vector3[])RestPositions.Clone();
        public float[] angle = (float[])RestAngles.Clone();
    }
    private static readonly string[] Names = { "Idle", "Walk", "Run", "Attack", "Skill1", "Skill2", "Skill3", "Hit", "Death" };
    private static readonly float[] Durations = { 2.8f, 1.2f, .78f, 2.5f, 2.94f, 3.62f, 2.7f, .36f, 1.65f };
    private static Pose Evaluate(int i, float t)
    {
        switch (i)
        {
            case 0: return Idle(t);
            case 1: return Locomotion(t, 1.2f, false);
            case 2: return Locomotion(t, .78f, true);
            case 3: return Attack(t);
            case 4: return Plant(t, false);
            case 5: return Plant(t, true);
            case 6: return Leap(t);
            case 7: return Hit(t);
            default: return Death(t);
        }
    }
    [MenuItem("Tools/3/MidBoss 3/Generate Animations")]
    public static void GenerateAll() { EnsureFolder(OutputFolder); for (int i = 0; i < Names.Length; i++) Make(i); SetupAnimator(); }
    [MenuItem("Tools/3/MidBoss 3/Clips/Idle")]
    public static void GenerateIdle() { EnsureFolder(OutputFolder); Make(0); SetupAnimator(); }
    [MenuItem("Tools/3/MidBoss 3/Clips/Walk")]
    public static void GenerateWalk() { EnsureFolder(OutputFolder); Make(1); SetupAnimator(); }
    [MenuItem("Tools/3/MidBoss 3/Clips/Run")]
    public static void GenerateRun() { EnsureFolder(OutputFolder); Make(2); SetupAnimator(); }
    [MenuItem("Tools/3/MidBoss 3/Clips/Attack")]
    public static void GenerateAttack() { EnsureFolder(OutputFolder); Make(3); SetupAnimator(); }
    [MenuItem("Tools/3/MidBoss 3/Clips/Skill1")]
    public static void GenerateSkill1() { EnsureFolder(OutputFolder); Make(4); Make(5); SetupAnimator(); }
    [MenuItem("Tools/3/MidBoss 3/Clips/Skill2")]
    public static void GenerateSkill2() { EnsureFolder(OutputFolder); Make(5); SetupAnimator(); }
    [MenuItem("Tools/3/MidBoss 3/Clips/Skill3")]
    public static void GenerateSkill3() { EnsureFolder(OutputFolder); Make(6); SetupAnimator(); }
    [MenuItem("Tools/3/MidBoss 3/Clips/Hit")]
    public static void GenerateHit() { EnsureFolder(OutputFolder); Make(7); SetupAnimator(); }
    [MenuItem("Tools/3/MidBoss 3/Clips/Death")]
    public static void GenerateDeath() { EnsureFolder(OutputFolder); Make(8); SetupAnimator(); }
    private static AnimationClip Make(int i)
    {
        return Generate("MidBoss3_" + Names[i], Durations[i], i < 3, t => Evaluate(i, t));
    }
    [MenuItem("Tools/3/MidBoss 3/Setup Animator")]
    public static void SetupAnimator()
    {
        EnsureFolder(OutputFolder);
        const string name = "MidBoss3_AC";
        AnimatorController controller = null;
        foreach (string guid in AssetDatabase.FindAssets(name + " t:AnimatorController"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) != name) continue;
            if (controller != null) throw new InvalidOperationException("Multiple MidBoss3_AC controllers found.");
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        }
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(OutputFolder + "/" + name + ".controller");
        Parameter(controller, "MoveSpeed", AnimatorControllerParameterType.Float);
        Parameter(controller, "IsChasing", AnimatorControllerParameterType.Bool);
        Parameter(controller, "IsDead", AnimatorControllerParameterType.Bool);
        foreach (string trigger in new[] { "Attack", "Skill1", "Skill2", "Skill3", "Hit" }) Parameter(controller, trigger, AnimatorControllerParameterType.Trigger);
        Parameter(controller, "IsAttacking", AnimatorControllerParameterType.Bool);
        Parameter(controller, "AttackSpeed", AnimatorControllerParameterType.Float);
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
            new Vector3(760, 240, 0), // Skill1
            new Vector3(760, 440, 0), // Skill2
            new Vector3(760, 640, 0), // Skill3
            new Vector3(260, 820, 0), // Hit: recovery
            new Vector3(760, 1040, 0), // Death
        };
        for (int i = 0; i < states.Length; i++)
        {
            foreach (var child in sm.states) if (child.state.name == "MidBoss3_" + Names[i]) states[i] = child.state;
            if (states[i] == null) states[i] = sm.AddState("MidBoss3_" + Names[i], layout[i]);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(OutputFolder + "/MidBoss3_" + Names[i] + ".anim");
            if (clip == null) clip = Make(i);
            states[i].motion = clip; states[i].speed = i == 2 ? 2f : 1f; states[i].speedParameter = "AttackSpeed"; states[i].speedParameterActive = i >= 3 && i <= 6; states[i].writeDefaultValues = false;
            foreach (var tr in states[i].transitions) if (tr.name.StartsWith("MidBoss3_Generated_")) states[i].RemoveTransition(tr);
            var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = i < 3; AnimationUtility.SetAnimationClipSettings(clip, settings); EditorUtility.SetDirty(clip);
        }
        var children = sm.states;
        for (int i = 0; i < children.Length; i++) for (int j = 0; j < states.Length; j++) if (children[i].state == states[j]) children[i].position = layout[j];
        sm.states = children; sm.defaultState = states[0];
        sm.entryPosition = new Vector3(80, 100, 0);
        sm.anyStatePosition = new Vector3(260, 1040, 0);
        sm.exitPosition = new Vector3(1080, 1040, 0);
        foreach (var tr in sm.anyStateTransitions) if (tr.name.StartsWith("MidBoss3_Generated_")) sm.RemoveAnyStateTransition(tr);
        var death = sm.AddAnyStateTransition(states[8]); Configure(death, false); death.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
        var hit = sm.AddAnyStateTransition(states[7]); Configure(hit, false); hit.AddCondition(AnimatorConditionMode.If, 0, "Hit"); hit.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
        for (int from = 0; from < 8; from++)
        {
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
            for (int to = 3; to <= 6; to++)
            {
                var tr = states[from].AddTransition(states[to]); Configure(tr, false); tr.duration = 0;
                tr.AddCondition(AnimatorConditionMode.If, 0, Names[to]); tr.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            }
        }
        EditorUtility.SetDirty(sm); EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets(); AssignControllerToPrefab(controller);
        Selection.activeObject = controller;
        Debug.Log("MidBoss3 generated. Skill1=Buff, Skill2=Rush, Skill3=Leap. AI movement/damage and facing lock must be connected separately.");
    }
    private static void Configure(AnimatorStateTransition t, bool exit)
    {
        t.name = "MidBoss3_Generated_" + t.destinationState.name; t.hasExitTime = exit; t.exitTime = 1;
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
                Debug.LogWarning("프리팹 자동 연결 생략: Body를 자식으로 가진 MidBoss 3 시각 루트를 찾지 못했습니다. " + prefabPath);
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
        return p;
    }
    private static Pose Locomotion(float t, float duration, bool run)
    {
        var p = new Pose(); float u = t / duration, w = Mathf.Sin(u * 2 * Mathf.PI);
        p.angle[Body] = (run ? -11 : -4) + 1.3f * w; p.angle[Bottom] = -p.angle[Body] * .8f; p.angle[Head] = -p.angle[Body] * .7f;
        p.position[Body].y = -.05f - (.5f - .5f * Mathf.Cos(u * 4 * Mathf.PI)) * (run ? .055f : .025f);
        p.angle[SF] = -8 + (run ? 15 : 9) * w; p.angle[AF] = 15 - 5 * w; p.angle[SB] = 12 - 4 * w; p.angle[AB] = 20 + 3 * w; p.angle[HB] = -10;
        Step(p, true, u, run); Step(p, false, u + .5f, run); return p;
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
    private static Key Rest(float t) { return new Key(t, 0, 0, 0, 0, 0, 120, 0, 0); }
    private static Pose Keys(float t, Key[] keys)
    {
        int i = 1; while (i < keys.Length - 1 && t > keys[i].t) i++;
        Key a = keys[i - 1], b = keys[i]; float u = Smooth((t - a.t) / (b.t - a.t));
        var p = new Pose(); p.angle[Body] = Mathf.Lerp(a.body, b.body, u); p.position[Body].y = Mathf.Lerp(a.y, b.y, u);
        p.angle[Head] = -p.angle[Body] * .7f; p.angle[Bottom] = -p.angle[Body] * .8f;
        p.angle[SB] = Mathf.Lerp(a.sb, b.sb, u); p.angle[AB] = Mathf.Lerp(a.ab, b.ab, u); p.angle[HB] = Mathf.Lerp(a.hb, b.hb, u);
        p.angle[Sword] = Mathf.Lerp(a.sword, b.sword, u) - WorldAngle(p, HB);
        p.angle[SF] = Mathf.Lerp(a.sf, b.sf, u); p.angle[AF] = Mathf.Lerp(a.af, b.af, u);
        Ground(p); return p;
    }
    private static Pose Attack(float t)
    {
        return Keys(t, new[]{Rest(0),new Key(.32f,5,-.05f,100,30,0,220,-16,25),
            new Key(.52f,7,-.065f,120,25,0,240,-20,35),
            new Key(.62f,-5,-.09f,95,-10,0,165,15,20),
            new Key(AttackHit1,-19,-.16f,58,-40,-5,62,35,15),
            new Key(.86f,-18,-.15f,55,-40,-5,60,32,15),
            new Key(1.15f,-13,-.13f,35,-45,-5,38,15,25),
            new Key(1.30f,-8,-.10f,50,-20,0,70,8,30),
            new Key(AttackHit2,6,-.03f,120,15,0,210,-20,35),
            new Key(1.58f,7,-.025f,130,20,0,232,-23,35),
            new Key(1.82f,4,-.035f,100,25,0,210,-12,20),Rest(2.5f)});
    }
    private static Pose Plant(float t, bool dash)
    {
        // Local -Y is the blade tip: world 180 = straight up, world 0 = down.
        // Both skills share the raise, visible pause, turn and deep planting motion.
        float end = dash ? 3.62f : 2.94f, hold = dash ? DashEnd : 1.35f;
        float returnStart = hold + .30f; // stop, then straighten before extracting
        float liftEnd = returnStart + .25f;
        float restStart = liftEnd + .36f;
        float extraction = Smooth((t - returnStart) / .25f);
        Pose p = Keys(t, new[]{Rest(0),
            new Key(.22f,0,0,100,20,0,180,-12,25),
            new Key(.34f,0,0,100,20,0,180,-12,25),
            new Key(.60f,0,0,100,20,0,0,-12,25),
            new Key(.84f,0,0,100,20,0,0,-12,25),
            new Key(BuffImpact,0,0,65,5,0,0,55,50),
            new Key(hold,0,0,65,5,0,0,55,50),
            new Key(returnStart,0,0,65,5,0,0,55,50),
            new Key(liftEnd,0,0,100,20,0,0,-12,25),
            new Key(restStart,0,0,100,20,0,0,-12,25),Rest(end)});
        float runBlend = dash ? Smooth((t - DashStart) / .12f)
            * (1 - Smooth((t - (DashEnd + .12f)) / .18f)) : 0f;
        float runPhase = (Mathf.Min(t, DashEnd) - DashStart) / .44f;
        if (runBlend > 0f)
        {
            // Torso leads the rush; alternating impacts compress the body slightly.
            p.angle[Body] = -34f * runBlend;
            p.angle[Bottom] = -p.angle[Body] * .8f;
            p.angle[Head] = -p.angle[Body] * .7f;
            p.position[Body].y -= (.025f + .035f * Mathf.Cos(runPhase * 4f * Mathf.PI)) * runBlend;
            Ground(p);
        }
        // Set the hands slowly while keeping the blade above the planted height.
        // The final downward stroke occupies only .06 seconds (about four samples).
        float plant = Smooth((t - .84f) / .06f) * (1 - extraction);
        float stab = Mathf.Clamp01((t - .84f) / .06f);
        stab = stab * stab * (1 - extraction); // fast downstroke, slower extraction
        Vector2 hand = World(p, HB);
        // Lower the grip and move it forward, away from the torso.
        Vector2 planted = new Vector2(1.20f + .28f * runBlend, Mathf.Lerp(1.65f, 1.20f, stab));
        float bladeAngle = WorldAngle(p, Sword);
        float oldShoulder = p.angle[SB], oldElbow = p.angle[AB];
        Solve(p, SB, AB, HB, Vector2.Lerp(hand, planted, plant), -1);
        p.angle[SB] = oldShoulder + Mathf.DeltaAngle(oldShoulder, p.angle[SB]) * plant;
        p.angle[AB] = Mathf.Max(0f, oldElbow + Mathf.DeltaAngle(oldElbow, p.angle[AB]) * plant);
        p.angle[Sword] = Mathf.Lerp(bladeAngle, 0, plant) - WorldAngle(p, HB);
        // Author the front/right arm instead of pinning its hand to the grip.
        // Approach while the sword turns, hold high, then drive down from SF.
        // Keep bone translations at rest so the arm never stretches.
        float rightArmWeight = Smooth((t - .34f) / .26f)
            * (1 - Smooth((t - restStart) / (end - restStart)));
        float rightStrike = Mathf.Clamp01((t - .84f) / .06f);
        rightStrike = rightStrike * rightStrike * (1 - extraction);
        float rightShoulder = Mathf.Lerp(150f, 110f, rightStrike);
        float rightForearm = Mathf.Lerp(-28f, -12f, rightStrike);
        float rightWrist = Mathf.Lerp(-8f, 8f, rightStrike);
        p.angle[SF] = Mathf.Lerp(p.angle[SF], rightShoulder, rightArmWeight);
        p.angle[AF] = Mathf.Lerp(p.angle[AF], rightForearm, rightArmWeight);
        p.angle[HF] = Mathf.Lerp(p.angle[HF], rightWrist, rightArmWeight);
        if (runBlend > 0f)
        {
            DashFoot(p, true, runPhase, runBlend);
            DashFoot(p, false, runPhase + .5f, runBlend);
        }
        if (!dash)
        {
            // Cancel torso translation and rotation at the pelvis. Skill1 keeps
            // the entire lower-body hierarchy at its rest pose in visual-root space.
            Vector2 restPelvis = World(new Pose(), Bottom);
            Vector2 pelvisLocal = Rotate(restPelvis - World(p, Body), -WorldAngle(p, Body));
            p.position[Bottom] = new Vector3(pelvisLocal.x, pelvisLocal.y, RestPositions[Bottom].z);
            p.angle[Bottom] = RestAngles[Bottom] - WorldAngle(p, Body);
            for (int part = TF; part <= FB; part++)
            {
                p.position[part] = RestPositions[part];
                p.angle[part] = RestAngles[part];
            }
        }
        return p;
    }
    private static void DashFoot(Pose p, bool front, float phase, float blend)
    {
        float u = Mathf.Repeat(phase, 1), x, lift, pitch;
        if (u < .42f)
        {
            float v = u / .42f;
            x = Mathf.Lerp(.40f, -.38f, v);
            lift = 0f;
            pitch = -18f * Smooth((v - .65f) / .35f);
        }
        else
        {
            float v = (u - .42f) / .58f;
            x = Mathf.Lerp(-.38f, .40f, Smooth(v));
            lift = .30f * Mathf.Pow(Mathf.Sin(v * Mathf.PI), 2);
            pitch = Mathf.Lerp(-18f, 20f, Smooth(v));
            pitch *= 1f - Smooth((v - .78f) / .22f);
        }
        Foot(p, front, x * blend, lift * blend);
        // Allow the toe to push off and the raised foot to rotate through recovery.
        p.angle[front ? FF : FB] += pitch * blend;
    }
    private static Pose Leap(float t)
    {
        // Carry the blade across the shoulder through most of the jump.
        // Only the rear arm attacks; the front/right arm trails behind for balance.
        Pose p = Keys(t, new[]{Rest(0),
            new Key(.28f,2,-.025f,25,70,-95,220,-15,12),
            new Key(.53f,5,-.065f,35,140,-175,270,-25,15),
            new Key(.67f,5,-.16f,35,140,-175,270,-32,18),
            new Key(JumpStart,4,-.21f,35,140,-175,270,-38,20),
            new Key(1.02f,1,-.035f,35,140,-175,270,-40,20),
            new Key(JumpApex,0,0,35,140,-175,270,-38,18),
            new Key(1.34f,-3,-.015f,35,140,-175,270,-35,18),
            new Key(1.44f,-8,-.04f,138,30,-20,250,-30,15),
            new Key(1.50f,-18,-.14f,105,20,-10,185,-25,12),
            new Key(1.55f,-28,-.32f,70,15,0,120,-20,10),
            new Key(JumpLand,-35,-.54f,45,12,0,80,-15,10),
            new Key(1.78f,-35,-.54f,45,12,0,80,-15,10),
            new Key(1.94f,-28,-.40f,50,18,0,86,-12,8),
            new Key(2.18f,-13,-.16f,35,20,0,105,-6,5),Rest(2.7f)});

        float landing = Smooth((t - 1.46f) / (JumpLand - 1.46f))
            * (1 - Smooth((t - 1.94f) / .35f));
        // Wide landing stance, with the trailing foot behind and leading knee forward.
        Foot(p, true, -.22f * landing, 0);
        Foot(p, false, .30f * landing, 0);
        // No root translation: gameplay still supplies the jump trajectory.
        if (t > JumpStart && t < JumpLand)
        {
            float airborne = Smooth((t - JumpStart) / .14f)
                * (1 - Smooth((t - 1.40f) / (JumpLand - 1.40f)));
            float straightCalf = Mathf.DeltaAngle(Angle(RestPositions[FF]), Angle(RestPositions[CF]));
            p.angle[TF] = Mathf.Lerp(p.angle[TF], -20f - WorldAngle(p, Bottom), airborne);
            p.angle[CF] = Mathf.Lerp(p.angle[CF], straightCalf, airborne);
            p.angle[FF] = Mathf.Lerp(p.angle[FF], 8f - WorldAngle(p, CF), airborne);
            p.angle[TB] = Mathf.Lerp(p.angle[TB], 65f, airborne);
            p.angle[CB] = Mathf.Lerp(p.angle[CB], -105f, airborne);
            p.angle[FB] = Mathf.Lerp(p.angle[FB], 24f, airborne);
        }

        // Rear hand drives the sword forward. At full impact its tip reaches the
        // rest foot's sprite-bottom height (PPU 64, blade pivot .85 of 256 pixels).
        float bladeAngle = WorldAngle(p, Sword);
        float wristWorld = WorldAngle(p, HB);
        float groundY = World(new Pose(), FF).y - .64f;
        float targetHandY = groundY + (256f / 64f * .85f) * Mathf.Cos(80f * Mathf.Deg2Rad) + .241f;
        float shoulderBefore = p.angle[SB], elbowBefore = p.angle[AB];
        Solve(p, SB, AB, HB, new Vector2(.95f, targetHandY), -1);
        p.angle[SB] = shoulderBefore + Mathf.DeltaAngle(shoulderBefore, p.angle[SB]) * landing;
        p.angle[AB] = Mathf.Max(0, elbowBefore + Mathf.DeltaAngle(elbowBefore, p.angle[AB]) * landing);
        p.angle[HB] = Mathf.LerpAngle(wristWorld, 0, landing) - WorldAngle(p, AB);
        p.angle[Sword] = bladeAngle - WorldAngle(p, HB);

        // Keep the authored negative SF pose; no two-handed grip constraint.
        return p;
    }
    private static Pose Hit(float t)
    {
        float k = t < .07f ? Smooth(t / .07f) : 1 - Smooth((t - .07f) / .29f); var p = new Pose();
        p.angle[Body] = 7 * k; p.position[Body].y = -.045f * k; p.angle[Head] = 5 * k; p.angle[SF] = -12 * k; p.angle[AB] = 15 * k; Ground(p); return p;
    }
    private static Pose Death(float t)
    {
        var p = new Pose();
        float kneel = Smooth((t - .08f) / .52f);
        float fall = Smooth((t - .60f) / .62f);
        float settle = Smooth((t - 1.22f) / .25f);
        var rest = new Pose();
        // Fold the shins behind the knees, then keep the pelvis planted during the fall.
        Vector2 kneeTarget = World(rest, FF) + new Vector2(.32f, .035f);
        Vector2 hipTarget = kneeTarget - (Vector2)RestPositions[TF] - Rotate(RestPositions[CF], 55f);
        Vector2 hip = Vector2.Lerp(World(rest, Bottom), hipTarget, kneel);
        hip += new Vector2(.10f * fall, -.035f * settle);
        p.angle[Body] = -12f * kneel - 72f * fall - 3f * settle;
        p.angle[Bottom] = -p.angle[Body];
        Vector2 body = hip - Rotate(RestPositions[Bottom], p.angle[Body]);
        p.position[Body] = new Vector3(body.x, body.y, RestPositions[Body].z);
        p.angle[TF] = 55f * kneel; p.angle[CF] = -145f * kneel; p.angle[FF] = 90f * kneel;
        p.angle[TB] = 50f * kneel; p.angle[CB] = -140f * kneel; p.angle[FB] = 90f * kneel;
        p.angle[Head] = -8f * kneel - 12f * fall + 3f * settle;
        // Hands reach down as the chest falls. Sword settles forward, without a root flip.
        p.angle[SF] = 18f * kneel + 42f * fall;
        p.angle[AF] = -22f * kneel - 28f * fall;
        p.angle[HF] = 12f * fall;
        p.angle[SB] = 12f * kneel + 35f * fall;
        p.angle[AB] = -15f * kneel - 20f * fall;
        p.angle[HB] = RestAngles[HB] - 12f * kneel + 8f * fall;
        float blade = RestAngles[Sword] + RestAngles[HB];
        blade = Mathf.Lerp(blade, 92f, Smooth((t - .60f) / .85f));
        p.angle[Sword] = blade - WorldAngle(p, HB);
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
