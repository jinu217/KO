#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Place in Assets/Editor/4. All curves are relative to the MainBoss 4 visual root.
public static class MainBoss4_AnimGenerators
{
    const string Folder = "Assets/Animations/4/MainBoss 4";
    const string DataPath = "Assets/Data/Enemys/Stage4/Stage4_MainBoss.asset";
    const string Prefix = "MainBoss4_";
    const int Body = 0, Head = 1, SF = 2, AF = 3, HF = 4, SB = 5, AB = 6, HB = 7, Bottom = 8, TF = 9, CF = 10, FF = 11, TB = 12, CB = 13, FB = 14;
    static readonly int[] Parent = { -1, 0, 0, 2, 3, 0, 5, 6, 0, 8, 9, 10, 8, 12, 13 };
    static readonly string[] Paths ={"Body","Body/Head","Body/Shoulder_F","Body/Shoulder_F/Forearm_F","Body/Shoulder_F/Forearm_F/Hand_F",
        "Body/Shoulder_B","Body/Shoulder_B/Forearm_B","Body/Shoulder_B/Forearm_B/Hand_B","Body/Bottom",
        "Body/Bottom/Thigh_F","Body/Bottom/Thigh_F/Calf_F","Body/Bottom/Thigh_F/Calf_F/Foot_F",
        "Body/Bottom/Thigh_B","Body/Bottom/Thigh_B/Calf_B","Body/Bottom/Thigh_B/Calf_B/Foot_B"};
    static readonly Vector3[] RestPositions ={new Vector3(0,0,0),new Vector3(-.237f,1.291f,0),
        new Vector3(-.995f,.923f,0),new Vector3(-.253f,-.929f,0),new Vector3(.069f,-.618f,0),
        new Vector3(.414f,.924f,0),new Vector3(.365f,-.850f,0),new Vector3(.054f,-.662f,0),
        new Vector3(.154f,-.049f,0),new Vector3(-.884f,-.345f,0),new Vector3(-.018f,-1.084f,0),new Vector3(-.237f,-1.123f,0),
        new Vector3(.039f,-.345f,0),new Vector3(.154f,-1.084f,0),new Vector3(-.237f,-1.123f,0)};
    // Zero local rotations retain the supplied standing rest art, including its slight forearm bend.
    sealed class Pose
    {
        public Vector3[] position = (Vector3[])RestPositions.Clone();
        public float[] angle = new float[15];
        public float frontStep, backStep, frontLift, backLift;
        public bool rearHandGuardFlip;
        public bool rearForearmFlip;
        public bool frontHandFlip;
        public float frontForearmScaleY = 1f;
    }
    sealed class ClipSpec
    {
        public string name; public float length; public bool loop; public Func<float, Pose> pose;
        public ClipSpec(string n, float l, bool repeat, Func<float, Pose> fn) { name = n; length = l; loop = repeat; pose = fn; }
    }
    static List<ClipSpec> specs;
    [MenuItem("Tools/4/MainBoss 4/Rebuild Skill4 + Skill5 (20261012 R39)")]
    public static void RebuildLatestSkill4And5()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Group(3); One("skill5");
        AssetDatabase.SaveAssets();
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/MainBoss4_skill5.anim");
        if (clip == null) throw new InvalidOperationException("Skill5 clip was not generated.");
        float sampleTime = clip.length * .70f;
        var x = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(Paths[HB], typeof(Transform), "m_LocalPosition.x"));
        var y = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(Paths[HB], typeof(Transform), "m_LocalPosition.y"));
        var z = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve(Paths[HB], typeof(Transform), "localEulerAnglesRaw.z"));
        if (x == null || y == null || z == null || Mathf.Abs(x.Evaluate(sampleTime) + .090f) > .001f || Mathf.Abs(y.Evaluate(sampleTime) + .640f) > .001f || Mathf.Abs(z.Evaluate(sampleTime) + 25f) > .01f)
            throw new InvalidOperationException("R39: Skill5 Hand_B curve verification failed.");
        Selection.activeObject = clip;
        EditorGUIUtility.PingObject(clip);
        Debug.Log("[MainBoss4 R39] Skill4/Desire/Skill5 regenerated. Skill5 Hand_B verified: (-0.090, -0.640), -25 degrees. Strong hook preDelay: 0.62s; Desire recovery: 0.10s; final straight preDelay: 0.08s. Clip: " + AssetDatabase.GetAssetPath(clip), clip);
    }

    [MenuItem("Tools/4/MainBoss 4/Generate Animations")]
    public static void GenerateAll() { BuildSpecs(); EnsureFolder(Folder); foreach (var spec in specs) Save(spec); SetupAnimator(); }
    [MenuItem("Tools/4/MainBoss 4/Clips/Idle")] public static void IdleMenu() => One("Idle");
    [MenuItem("Tools/4/MainBoss 4/Clips/Walk")] public static void WalkMenu() => One("Walk");
    [MenuItem("Tools/4/MainBoss 4/Clips/Run")] public static void RunMenu() => One("Run");
    [MenuItem("Tools/4/MainBoss 4/Clips/Attack")] public static void AttackMenu() => Group(-1);
    [MenuItem("Tools/4/MainBoss 4/Clips/Skill1")] public static void Skill1Menu() => Group(0);
    [MenuItem("Tools/4/MainBoss 4/Clips/Skill2")] public static void Skill2Menu() => Group(1);
    [MenuItem("Tools/4/MainBoss 4/Clips/Skill3 Buff")] public static void Skill3Menu() => One("skill3");
    [MenuItem("Tools/4/MainBoss 4/Clips/Skill4")] public static void Skill4Menu() => Group(3);
    [MenuItem("Tools/4/MainBoss 4/Clips/Skill5 Buff")] public static void Skill5Menu() => One("skill5");
    [MenuItem("Tools/4/MainBoss 4/Clips/Desire")] public static void DesireMenu() { One("Dskill"); One("Groggy"); }
    [MenuItem("Tools/4/MainBoss 4/Clips/Death")] public static void DeathMenu() => One("Death");
    static void One(string name) { BuildSpecs(); EnsureFolder(Folder); foreach (var s in specs) if (s.name == name) Save(s); SetupAnimator(); }
    static void Group(int index)
    {
        BuildSpecs(); EnsureFolder(Folder); string key = index < 0 ? "Attack" : "skill" + (index + 1);
        foreach (var s in specs) if (s.name == key || s.name == key + "_Desire" || s.name.StartsWith(key + "_Uppercut")) Save(s);
        SetupAnimator();
    }
    static void BuildSpecs()
    {
        var data = AssetDatabase.LoadAssetAtPath<MainBossData>(DataPath);
        if (data == null) throw new InvalidOperationException("데이터를 찾지 못했습니다: " + DataPath);
        // Keep gameplay hit delays aligned with the longer heavy-punch clips (idempotent).
        bool timingChanged = false;
        for (int skillIndex = 0; skillIndex < data.skills.Count; skillIndex++)
        {
            var skill = data.skills[skillIndex];
            if (skill.skillType == BossSkillType.Buff || skill.active == null) continue;
            for (int n = 0; n < skill.active.attackSteps.Count; n++)
            {
                bool heavy = (skillIndex == 0 && (n == 2 || n == 3)) || (skillIndex == 1 && n == 1) || (skillIndex == 3 && n >= 1 && n <= 4);
                if (skillIndex == 1 && n == 2)
                {
                    var upperStep = skill.active.attackSteps[n];
                    if (upperStep.preDelay < .24f || upperStep.postDelay < .44f) timingChanged = true;
                    upperStep.preDelay = Mathf.Max(upperStep.preDelay, .24f);
                    upperStep.postDelay = Mathf.Max(upperStep.postDelay, .44f);
                }
                if (skillIndex == 0 && n == 4)
                {
                    var straightStep = skill.active.attackSteps[n];
                    if (straightStep.postDelay < .24f) { straightStep.postDelay = .24f; timingChanged = true; }
                }
                if (!heavy) continue;
                var step = skill.active.attackSteps[n];
                float pre = ((skillIndex == 1 && n == 1) || (skillIndex == 0 && n == 3)) ? .18f : skillIndex == 3 && n == 4 ? .62f : Mathf.Max(step.preDelay, .28f), post = Mathf.Max(step.postDelay, ((skillIndex == 1 && n == 1) || (skillIndex == 0 && n == 3)) ? .36f : skillIndex == 3 && n == 4 ? .21f : .14f);
                if (pre != step.preDelay || post != step.postDelay) timingChanged = true;
                step.preDelay = pre; step.postDelay = post;
            }
        }
        if (data.basicAttack != null && data.basicAttack.active != null)
            for (int n = 2; n < data.basicAttack.active.attackSteps.Count; n++)
            {
                var straightStep = data.basicAttack.active.attackSteps[n];
                if (straightStep.postDelay < .24f) { straightStep.postDelay = .24f; timingChanged = true; }
            }
        if (timingChanged) EditorUtility.SetDirty(data);
        specs = new List<ClipSpec>{new ClipSpec("Idle",2.6f,true,Idle),new ClipSpec("Walk",1.1f,true,t=>Walk(t,1.1f,false)),
            new ClipSpec("Run",.36f,true,t=>Walk(t,.36f,true)),new ClipSpec("Death",2f,false,Death),

            new ClipSpec("Dskill",2.2f,false,DesirePose),new ClipSpec("Groggy",3f,false,Groggy)};
        AddPattern(data.basicAttack, -1, false);
        for (int i = 0; i < data.skills.Count; i++) AddPattern(data.skills[i], i, false);
        // Match Stage4_MainBossDesireSkill's appended steps exactly.
        var basic = JsonUtility.FromJson<BossSkillData>(JsonUtility.ToJson(data.basicAttack));
        basic.active.attackSteps.Add(PunchStep("4타 전진 스트레이트", 20, .16f, true)); AddPattern(basic, -1, true);
        var second = JsonUtility.FromJson<BossSkillData>(JsonUtility.ToJson(data.skills[1]));
        second.active.attackSteps.Add(new BossAttackStepData { actionType = BossActionType.Jump, jumpForce = new Vector2(2, 7), duration = .18f });
        var airStrike = PunchStep("공중 스트레이트", 22, .04f); airStrike.postDelay = .40f;
        second.active.attackSteps.Add(airStrike); AddPattern(second, 1, true);
        var fourth = JsonUtility.FromJson<BossSkillData>(JsonUtility.ToJson(data.skills[3]));
        fourth.active.attackSteps[4].postDelay = .10f;
        var finalStraightStep = PunchStep("최종 스트레이트", 25, .08f, true);
        finalStraightStep.postDelay = .28f;
        fourth.active.attackSteps.Add(finalStraightStep); AddPattern(fourth, 3, true);
    }
    static BossAttackStepData PunchStep(string name, float damage, float delay, bool advance = false)
    {
        return new BossAttackStepData
        {
            stepName = name,
            actionType = advance ? BossActionType.DashAttack : BossActionType.MeleeAttack,
            damage = damage,
            preDelay = delay,
            postDelay = .07f,
            duration = advance ? .12f : 0,
            attackCount = 1
        };
    }
    static float StepLength(BossAttackStepData step)
    { return Mathf.Max(.08f, step.preDelay + (step.actionType == BossActionType.MeleeAttack ? 0 : step.duration) + step.postDelay); }
    static void AddPattern(BossSkillData skill, int index, bool enhanced)
    {
        string name = index < 0 ? "Attack" : "skill" + (index + 1);
        if (skill.skillType == BossSkillType.Buff)
        { float duration = Mathf.Max(.5f, skill.preDelay + skill.postDelay); specs.Add(new ClipSpec(name, duration, false, t => index == 2 ? FistClash(t, duration) : Buff(t, duration))); return; }
        int count = skill.active.attackSteps.Count;
        if (index == 1)
        {
            if (!enhanced) AddSegment(skill, index, name, 0, Mathf.Min(2, count), true);
            AddSegment(skill, index, name + "_Uppercut" + (enhanced ? "_Desire" : ""), 2, count, false);
        }
        else AddSegment(skill, index, name + (enhanced ? "_Desire" : ""), 0, count, true);
    }
    static void AddSegment(BossSkillData skill, int index, string name, int start, int end, bool entry)
    {
        float initial = entry ? skill.preDelay : 0, total = initial;
        for (int n = start; n < end; n++) total += StepLength(skill.active.attackSteps[n]);
        total += skill.postDelay + .32f;
        specs.Add(new ClipSpec(name, total, false, t => {
            float time = t - initial;
            if (time < 0) return AttackGuard();
            for (int n = start; n < end; n++)
            {
                var step = skill.active.attackSteps[n]; float length = StepLength(step);
                if (time <= length)
                {
                    var pose = StepPose(index, n, step, time, length);
                    if (entry && n == start && initial <= 0 && time < step.preDelay * .4f)
                        return Mix(AttackGuard(), pose, time / Mathf.Max(.001f, step.preDelay * .4f));
                    return pose;
                }
                time -= length;
            }
            return AttackGuard(); // End in the same base pose as Run, without dipping through Rest.
        }));
    }
    static Pose Mix(Pose a, Pose b, float u)
    {
        u = Smooth(Mathf.Clamp01(u)); var p = new Pose();
        p.frontForearmScaleY = Mathf.Lerp(a.frontForearmScaleY, b.frontForearmScaleY, u);
        p.rearHandGuardFlip = u < .5f ? a.rearHandGuardFlip : b.rearHandGuardFlip;
        p.rearForearmFlip = u < .5f ? a.rearForearmFlip : b.rearForearmFlip;
        p.frontHandFlip = u < .5f ? a.frontHandFlip : b.frontHandFlip;
        for (int i = 0; i < 15; i++) { p.position[i] = Vector3.Lerp(a.position[i], b.position[i], u); p.angle[i] = Mathf.Lerp(a.angle[i], b.angle[i], u); }
        return p;
    }
    private static Vector2 Rotate(Vector2 v, float degrees)
    { float a = degrees * Mathf.Deg2Rad; return new Vector2(v.x * Mathf.Cos(a) - v.y * Mathf.Sin(a), v.x * Mathf.Sin(a) + v.y * Mathf.Cos(a)); }
    private static float Smooth(float t) => t * t * (3 - 2 * t);
    private static float WorldAngle(Pose p, int part)
    { float a = 0; for (int i = part; i >= 0; i = Parent[i]) a += p.angle[i]; return a; }
    private static Vector2 WorldPosition(Pose p, int part)
    {
        int parent = Parent[part];
        return parent < 0 ? (Vector2)p.position[part] :
            WorldPosition(p, parent) + Rotate(p.position[part], WorldAngle(p, parent));
    }
    // Solve the two leg links with their original lengths. No stretching of calf/foot offsets.
    private static void PlantLeg(Pose p, bool front, float step, float lift)
    {
        int thigh = front ? TF : TB, calf = front ? CF : CB, foot = front ? FF : FB;
        var rest = new Pose();
        Vector2 target = WorldPosition(rest, foot) + new Vector2(step, lift);
        Vector2 hip = WorldPosition(p, thigh), d = target - hip;
        Vector2 upper = RestPositions[calf], lower = RestPositions[foot];
        float a = upper.magnitude, b = lower.magnitude;
        float length = Mathf.Clamp(d.magnitude, Mathf.Abs(a - b) + .00001f, a + b);
        float bend = -Mathf.Acos(Mathf.Clamp((length * length - a * a - b * b) / (2 * a * b), -1, 1));
        float heading = Mathf.Atan2(d.y, d.x) - Mathf.Atan2(b * Mathf.Sin(bend), a + b * Mathf.Cos(bend));
        float thighWorld = heading * Mathf.Rad2Deg - Mathf.Atan2(upper.y, upper.x) * Mathf.Rad2Deg;
        float calfWorld = (heading + bend) * Mathf.Rad2Deg - Mathf.Atan2(lower.y, lower.x) * Mathf.Rad2Deg;
        p.angle[thigh] = Mathf.DeltaAngle(0, thighWorld - WorldAngle(p, Bottom));
        p.angle[calf] = Mathf.DeltaAngle(0, calfWorld - thighWorld);
        p.angle[foot] = -WorldAngle(p, calf);
    }
    private static Pose Ground(Pose p)
    { PlantLeg(p, true, p.frontStep, p.frontLift); PlantLeg(p, false, p.backStep, p.backLift); return p; }
    static Pose Idle(float t)
    {
        var p = new Pose(); float u = .5f - .5f * Mathf.Cos(t / 2.6f * Mathf.PI * 2);
        p.position[Body].y = .009f * u; p.position[Bottom].y -= .009f * u; p.angle[Head] = -.3f * u;
        p.angle[SF] = .5f * u; p.angle[SB] = -.4f * u; return p;
    }
    // Author local joint angles: wrist targets previously folded elbows and wrists backwards.
    static void ArmAngles(Pose p, bool front, float shoulder, float forearm, float hand)
    { p.angle[front ? SF : SB] = shoulder; p.angle[front ? AF : AB] = forearm; p.angle[front ? HF : HB] = Mathf.Clamp(hand, front ? 0 : -60, 60); }
    static Pose Guard(float crouch = 0, float lean = -3)
    {
        var p = new Pose(); p.position[Body] = new Vector3(.03f, -crouch, 0);
        p.angle[Body] = lean; p.angle[Head] = -lean * .7f; p.angle[Bottom] = -lean;
        ArmAngles(p, true, 18, 68, 18); ArmAngles(p, false, 22, 64, -16);
        if (crouch > 0) { p.frontStep = -.05f; p.backStep = .10f; Ground(p); }
        else p.position[Bottom] = Quaternion.Euler(0, 0, -lean) * (RestPositions[Bottom] - p.position[Body]);
        return p;
    }
    static Pose AttackGuard()
    {
        var p = Guard(.38f, -42f);
        p.position[Body].x = .20f;
        p.frontStep = -.78f; p.backStep = .30f;
        Ground(p);
        p.angle[Head] = 42f;
        ArmAngles(p, true, 110, 68, 18);
        ArmAngles(p, false, 100, 64, -5);
        ProtectChin(p, true); ProtectChin(p, false);
        return p;
    }
    static Pose Punch(bool front, bool hook, bool upper, bool body, bool air = false)
    {
        var p = Guard(body ? .65f : upper ? .36f : front ? .52f : .41f, upper ? -30 : hook ? -52 : front ? -55 : -44);
        p.position[Body].x = front ? .26f : .20f;
        p.frontStep = front ? -.88f : -.76f; p.backStep = front ? .34f : .28f;
        // Keep the toe planted as the heel rises (right-facing sprite).
        float heelAngle = front && !air ? -12f : 0f;
        if (heelAngle != 0)
        {
            Vector2 toe = new Vector2((1f - .45f) * 83f / 64f, -.37f * 82f / 64f);
            Vector2 pivotShift = toe - Rotate(toe, heelAngle);
            p.frontStep += pivotShift.x;
            p.frontLift = pivotShift.y;
        }
        Ground(p);
        if (heelAngle != 0) p.angle[FF] = heelAngle - WorldAngle(p, CF);
        if (upper) ArmAngles(p, front, front ? 168.8f : 120f, 53f, front ? 18 : -12);
        else if (hook) ArmAngles(p, front, front ? 78 : 65, -10f, front ? 12 : -12);
        else if (body) ArmAngles(p, front, front ? 70 : 52, 12, 0);
        else if (front) ArmAngles(p, true, 110, -20, 3);
        else ArmAngles(p, false, 80, -20, -5); // Preserve jab elbow extension; turn wrist to reduce palm-first silhouette.
        if (!hook && !upper && !body)
        {
            // Shoulder travels inside the armour; tiny link extensions preserve sprite overlap.
            int shoulder = front ? SF : SB, forearm = front ? AF : AB, hand = front ? HF : HB;
            p.position[shoulder] += (Vector3)Rotate(new Vector2(front ? .075f : .055f, 0), -p.angle[Body]);
            p.position[forearm] += RestPositions[forearm].normalized * .025f;
            p.position[hand] += RestPositions[hand].normalized * .020f;
        }
        if (air) { p.angle[Body] = -16; p.angle[Bottom] = 6; p.angle[TF] = -22; p.angle[CF] = -5; p.angle[TB] = 38; p.angle[CB] = -60; p.angle[FF] = 12; p.angle[FB] = 20; }
        // Counter-rotate the head to keep its rest-facing direction despite the deep lean.
        p.angle[Head] = -p.angle[Body];
        if (!upper) MatchPunchHeight(p, front);
        if (air) p.angle[front ? SF : SB] -= 45f; // Aim the airborne straight down-forward.
        if (body) { p.angle[front ? SF : SB] -= 24f; p.angle[front ? AF : AB] = -18f; }
        if (hook) p.angle[front ? SF : SB] += 10f;
        return p;
    }
    static Pose ProtectChin(Pose p, bool front)
    {
        // One fixed elbow bend: no IK branch changes or wrapped-angle solutions.
        int shoulder = front ? SF : SB, forearm = front ? AF : AB, hand = front ? HF : HB;
        ArmAngles(p, front, front ? 75 : 90, front ? 90 : 95, front ? 18 : -5);
        Vector2 reach = (Vector2)p.position[forearm] + Rotate(p.position[hand], p.angle[forearm]);
        float height = WorldPosition(p, Head).y - .22f - WorldPosition(p, shoulder).y;
        float heading = Mathf.Asin(Mathf.Clamp(height / Mathf.Max(.001f, reach.magnitude), -.95f, .95f)) * Mathf.Rad2Deg;
        p.angle[shoulder] = heading - Mathf.Atan2(reach.y, reach.x) * Mathf.Rad2Deg - WorldAngle(p, Body);
        if (!front)
        {
            // Rear-hand guard only: retain the guard's shoulder/elbow pose.
            p.position[HB] = new Vector3(.367f, -.525f, 0f);
            p.angle[HB] = 35.55f;
            p.rearHandGuardFlip = true;
        }
        return p;
    }
    static void MatchPunchHeight(Pose p, bool front, float heightOffset = 0f)
    {
        int shoulder = front ? SF : SB, forearm = front ? AF : AB, hand = front ? HF : HB;
        Vector2 reach = (Vector2)p.position[forearm] + Rotate(p.position[hand], p.angle[forearm]);
        float height = WorldPosition(p, Head).y + heightOffset - WorldPosition(p, shoulder).y;
        // Positive horizontal reach; retain elbow angle and attachment positions.
        float heading = Mathf.Asin(Mathf.Clamp(height / Mathf.Max(.001f, reach.magnitude), -.98f, .98f)) * Mathf.Rad2Deg;
        p.angle[shoulder] = heading - Mathf.Atan2(reach.y, reach.x) * Mathf.Rad2Deg - WorldAngle(p, Body);
    }
    static Pose StepPose(int skill, int n, BossAttackStepData step, float t, float duration)
    {
        var guard = AttackGuard(); bool movement = step.actionType == BossActionType.MoveForward;
        if (movement)
        {
            if (skill == 1)
            {
                // A low step forward, ending in the very same crouched guard used by body blow.
                var lowStep = AttackGuard();
                float stepProgress = Mathf.Clamp01((t - step.preDelay) / Mathf.Max(.01f, step.duration));
                lowStep.backStep = .30f + .12f * Mathf.Sin(stepProgress * Mathf.PI);
                lowStep.backLift = .055f * Mathf.Pow(Mathf.Sin(stepProgress * Mathf.PI), 2);
                lowStep.frontStep = -.78f;
                float leanPulse = Mathf.Sin(stepProgress * Mathf.PI);
                lowStep.angle[Body] -= 8f * leanPulse;
                lowStep.angle[Bottom] += 8f * leanPulse;
                lowStep.angle[Head] += 8f * leanPulse;
                float heel = -22f * leanPulse;
                Vector2 toe = new Vector2((1f - .45f) * 83f / 64f, -.37f * 82f / 64f);
                Vector2 heelShift = toe - Rotate(toe, heel);
                lowStep.frontStep += heelShift.x; lowStep.frontLift += heelShift.y;
                Ground(lowStep);
                lowStep.angle[FF] = heel - WorldAngle(lowStep, CF);
                ProtectChin(lowStep, true); ProtectChin(lowStep, false);
                return lowStep;
            }
            if (skill == 3)
            {
                float rollProgress = Mathf.Clamp01(t / Mathf.Max(.01f, duration));
                var rollPose = Walk(t, .36f, true);
                float envelope = Mathf.Sin(rollProgress * Mathf.PI);
                float roll = Mathf.Sin(rollProgress * Mathf.PI * 6f) * 20f * envelope;
                rollPose.angle[Body] += roll;
                rollPose.angle[Bottom] -= roll * .75f;
                rollPose.position[Body].y -= .045f * envelope;
                rollPose.angle[Head] = -rollPose.angle[Body];
                // Recompute stepping legs after applying the torso roll.
                float phase = t / .36f;
                for (int side = 0; side < 2; side++)
                {
                    float gait = Mathf.Repeat(phase + side * .5f, 1);
                    float stride = gait < .5f ? Mathf.Lerp(.42f, -.42f, Smooth(gait * 2)) : Mathf.Lerp(-.42f, .42f, Smooth((gait - .5f) * 2));
                    float lift = gait < .5f ? 0 : .22f * Mathf.Pow(Mathf.Sin((gait - .5f) * Mathf.PI * 2), 2);
                    PlantLeg(rollPose, side == 0, stride, lift);
                }
                ProtectChin(rollPose, true); ProtectChin(rollPose, false);
                return Mix(rollPose, guard, Mathf.Clamp01((rollProgress - .88f) / .12f));
            }
            var low = Guard(.18f, -16);
            float u = Mathf.Clamp01((t - step.preDelay) / Mathf.Max(.01f, step.duration));
            var moving = Walk(u * .72f, .72f, true);
            var pose = t < step.preDelay ? Mix(guard, low, t / Mathf.Max(.01f, step.preDelay)) : Mix(low, moving, Mathf.Min(u * 5, 1));
            return Mix(pose, guard, Mathf.Clamp01((t - (duration - .05f)) / .05f));
        }
        if (step.actionType == BossActionType.Jump)
        { var p = Guard(.14f, -8); p.angle[TF] = -22; p.angle[CF] = -6; p.angle[TB] = 42; p.angle[CB] = -72; return p; }
        bool upper = skill == 1 && n == 2;
        bool hook = (skill == 0 && n == 2) || (skill == 3 && n >= 1 && n <= 4);
        bool body = (skill == 0 && n == 3) || (skill == 1 && n == 1);
        bool front = skill < 0 ? n >= 2 : skill == 3 ? (n == 5 || n % 2 == 0) : skill == 1 ? n != 1 : n == 2 || n == 4;
        var hit = Punch(front, hook, upper, body, skill == 1 && n >= 4);
        if (skill == 0 && n == 4)
        {
            // Skill1 final straight only: unfold elbow and distribute the added reach.
            hit.angle[AF] = -9f;
            hit.position[SF] += (Vector3)Rotate(new Vector2(.055f, 0), -hit.angle[Body]);
            hit.position[AF] += RestPositions[AF].normalized * .012f;
            hit.position[HF] += RestPositions[HF].normalized * .012f;
            MatchPunchHeight(hit, true);
        }
        if (skill == 3) MatchPunchHeight(hit, front, -.35f);
        var wind = AttackGuard();
        ArmAngles(wind, front, hook ? -12 : upper ? 5 : 8, upper ? 32 : hook ? 78 : 82, front ? 12 : -12);
        if (!hook && !upper && !body)
        {
            // Chamber near chest height, then unfold forward instead of scooping up from below.
            ArmAngles(wind, front, front ? 55 : 45, front ? 95 : 70, front ? 12 : -5);
        }
        // Establish the crouch before launching the arm; stay low between hits.
        if (!upper) MatchPunchHeight(wind, front);
        if (hook || body)
        {
            wind.angle[front ? SF : SB] -= hook ? 28f : 40f;
            wind.angle[front ? AF : AB] += hook ? 12f : 28f;
        }
        if (body)
        {
            // Drop the rear arm beside the torso before driving the body blow upward.
            ArmAngles(wind, false, 5f, 22f, 35.55f);
            wind.angle[Body] -= 7f;
            wind.angle[Bottom] += 7f;
            wind.angle[Head] += 7f;
            wind.frontStep = guard.frontStep; wind.backStep = guard.backStep;
            Ground(wind);
        }
        bool straightPunch = !hook && !upper && !body;
        if (straightPunch)
        {
            // Both jab and straight launch from the same chin guard. No separate shoulder lift.
            wind = AttackGuard();
        }
        if (upper)
        {
            ArmAngles(wind, true, 12f, 24f, 18f);
        }
        if (hook)
        {
            ArmAngles(wind, front, guard.angle[front ? SF : SB] - 48f, 95f, front ? 12 : -12);
        }
        if (!front && hook)
        {
            // Preserve the rear hand's guard reflection through the entire hook.
            wind.rearHandGuardFlip = true;
            hit.rearHandGuardFlip = true;
            hit.angle[HB] = 60f;
            if (skill == 3)
            {
                wind.position[HB] = guard.position[HB];
                hit.position[HB] = guard.position[HB];
            }
        }
        if (!front && (body || upper))
        {
            // Keep the palm turned toward the head throughout loading, impact and recoil.
            wind.rearHandGuardFlip = true; hit.rearHandGuardFlip = true;
            wind.angle[HB] = 35.55f; hit.angle[HB] = 35.55f;
        }
        if (body)
        {
            ArmAngles(hit, false, 90.559f, .600f, 35.550f);
            hit.position[HB] = new Vector3(.410f, -.580f, 0);
        }
        if (hook && front)
        {
            // Start below the ribs; raise shoulder and open elbow together through impact.
            ArmAngles(wind, true, 12f, 38f, 12f);
            hit.angle[SF] -= 16f;
            hit.angle[AF] = 18f;
            hit.frontForearmScaleY = 1.4f;
        }
        if (upper)
        {
            hit.position[Body].x += .16f;
            float heel = -24f;
            Vector2 toe = new Vector2((1f - .45f) * 83f / 64f, -.37f * 82f / 64f);
            Vector2 delta = Rotate(toe, -12f) - Rotate(toe, heel);
            hit.frontStep += delta.x; hit.frontLift += delta.y;
            Ground(hit); hit.angle[FF] = heel - WorldAngle(hit, CF);
        }
        // Skill1: lower jab, hook and straight impacts without changing the body blow.
        if (skill == 0 && !body) hit.angle[front ? SF : SB] -= 8f;
        bool airborneStrike = skill == 1 && n >= 4;
        if (airborneStrike)
        {
            wind = Mix(hit, hit, 0);
            wind.angle[SF] -= 3f;
        }
        if (skill == 3 && n == 5)
        {
            wind.angle[SF] -= 18f; wind.angle[AF] += 18f;
        }
        float impact = Mathf.Max(.04f, step.preDelay);
        bool finalStraight = skill == 3 && n == 5;
        float launchStart = (finalStraight || hook || body) ? Mathf.Max(0f, impact - .055f) : impact * .65f;
        if (t < launchStart)
        {
            float loadTime = skill == 3 && n == 4 ? Mathf.Max(.01f, launchStart - .045f) : (finalStraight || hook || body) ? Mathf.Min(.12f, launchStart) : launchStart;
            return FinishPunch(Mix(guard, wind, t / Mathf.Max(.001f, loadTime)), front, hook, upper);
        }
        if (t < impact)
        {
            float u = (t - launchStart) / Mathf.Max(.01f, impact - launchStart);
            var pose = Mix(wind, hit, u);
            if (upper)
            {
                var forward = Mix(wind, hit, .45f);
                ArmAngles(forward, true, 128f, -18f, 18f);
                pose = u < .45f ? Mix(wind, forward, u / .45f) : Mix(forward, hit, (u - .45f) / .55f);
            }
            if (hook)
            {
                int forearm = front ? AF : AB;
                float whip = front ? Mathf.Clamp01(u) : Mathf.Clamp01((u - .30f) / .70f);
                pose.angle[forearm] = Mathf.Lerp(wind.angle[forearm], hit.angle[forearm], front ? Smooth(whip) : whip * whip);
            }
            return FinishPunch(pose, front, hook, upper);
        }
        // Brief impact, then controlled recoil; next step can interrupt early on a wall.
        float hold = finalStraight ? .18f : airborneStrike ? .18f : body ? .08f : upper ? .12f : straightPunch && front ? .10f : .025f;
        float recovery = Mathf.Clamp01((t - impact - hold) / Mathf.Max(.001f, duration - impact - hold));
        if (body)
        {
            // Fold the elbow while withdrawing, then settle directly into the chin guard.
            var folded = Mix(hit, guard, .55f);
            folded.angle[AB] = guard.angle[AB] + 12f;
            var retract = recovery < .55f
                ? Mix(hit, folded, recovery / .55f)
                : Mix(folded, guard, (recovery - .55f) / .45f);
            return FinishPunch(retract, front, hook, upper);
        }
        var recovered = FinishPunch(Mix(hit, guard, recovery), front, hook, upper);
        if (upper)
        {
            // Ease the dropped rear shoulder back into guard without a state-boundary snap.
            float armReturn = Smooth(recovery * recovery);
            recovered.angle[SB] = Mathf.Lerp(20f, guard.angle[SB], armReturn);
            recovered.angle[AB] = Mathf.Lerp(recovered.angle[AB], guard.angle[AB], armReturn);
        }
        return recovered;
    }
    static Pose FinishPunch(Pose p, bool front, bool hook, bool upper)
    {
        ProtectChin(p, !front);
        if (hook)
        {
            // Tuck the non-striking elbow while retaining a folded chin guard.
            int shoulder = front ? SB : SF, forearm = front ? AB : AF;
            p.angle[shoulder] -= 22f;
            p.angle[forearm] += 22f;
        }
        if (upper)
        {
            p.angle[SB] = 20f;
        }
        return p;
    }
    static Pose Walk(float t, float duration, bool run)
    {
        float phase = t / duration, w = Mathf.Sin(phase * Mathf.PI * 2); var p = run ? AttackGuard() : new Pose();
        if (!run) p.position[Body].y = -.025f;
        else
        {
            float cycle = phase * Mathf.PI * 2f;
            float pelvisSway = Mathf.Sin(cycle);
            float torsoSway = Mathf.Sin(cycle - .45f);
            p.position[Body].y += .025f * (1f - Mathf.Cos(cycle * 2f));
            p.position[Body].x += .018f * torsoSway;
            p.angle[Body] += 1.8f * torsoSway;
            p.angle[Bottom] -= 3.0f * pelvisSway;
            p.position[Bottom].x += .018f * pelvisSway;
            p.angle[Head] -= 2.4f * torsoSway;
        }
        // Replant feet after applying the torso/pelvis motion.
        for (int side = 0; side < 2; side++)
        {
            float u = Mathf.Repeat(phase + side * .5f, 1), stride = run ? .42f : .24f;
            float x = u < .5f ? Mathf.Lerp(stride, -stride, Smooth(u * 2)) : Mathf.Lerp(-stride, stride, Smooth((u - .5f) * 2));
            float lift = u < .5f ? 0 : (run ? .22f : .10f) * Mathf.Pow(Mathf.Sin((u - .5f) * 2 * Mathf.PI), 2);
            PlantLeg(p, side == 0, x, lift);
        }
        if (run) { ProtectChin(p, true); ProtectChin(p, false); }
        else { p.angle[SF] = 5 * w; p.angle[SB] = -5 * w; p.angle[AF] = 2 + 2 * w; p.angle[AB] = 2 - 2 * w; }
        return p;
    }
    static Pose DesirePose(float t)
    {
        var raised = new Pose(); raised.angle[Head] = 4;
        ArmAngles(raised, true, -142.2f, -104.6f, 18);
        ArmAngles(raised, false, 157.765f, 15.836f, -8);
        var flex = new Pose(); flex.angle[Body] = -15; flex.angle[Bottom] = 15; flex.angle[Head] = 4;
        ArmAngles(flex, true, 0, 45, 0); ArmAngles(flex, false, 25, -90, -8);
        foreach (var stance in new[] { raised, flex })
        {
            stance.position[Body].y = -.14f;
            stance.frontStep = -.10f; stance.backStep = .10f;
            Ground(stance);
        }
        // Exact supplied upper poses with a slightly wider, lower stance.
        if (t < .50f) return Mix(new Pose(), raised, t / .50f);
        if (t < .80f) return raised;
        if (t < 1.25f) return Mix(raised, flex, (t - .80f) / .45f);
        if (t < 1.80f) return flex;
        return Mix(flex, new Pose(), (t - 1.80f) / .40f);
    }
    static Pose FistClash(float t, float duration)
    {
        float u = Mathf.Clamp01(t / duration);
        var contact = new Pose();
        ArmAngles(contact, true, 10, 50, 12);
        ArmAngles(contact, false, 20, -60, 46.5f);
        contact.position[HB] = new Vector3(.290f, -.530f, 0);
        contact.position[Body].y = -.10f;
        Ground(contact); contact.angle[Head] = -3;
        contact.rearForearmFlip = true; contact.rearHandGuardFlip = true;
        // Use the newly supplied contact coordinates exactly.
        var open = Mix(contact, contact, 0);
        open.angle[SF] -= 38f; open.angle[SB] += 38f;
        var recoil = Mix(contact, contact, 0);
        recoil.angle[SF] -= 6f; recoil.angle[SB] += 6f;
        // Wide wind-up, fast accelerating close, contact hold and a small shoulder-only rebound.
        if (u < .22f) return Mix(AttackGuard(), open, u / .22f);
        if (u < .27f) return open;
        if (u < .32f) return ClashImpact(open, contact, (u - .27f) / .05f);
        if (u < .39f) return contact;
        if (u < .43f) return Mix(contact, recoil, (u - .39f) / .04f);
        if (u < .58f) return Mix(recoil, open, (u - .43f) / .15f);
        if (u < .63f) return open;
        if (u < .675f) return ClashImpact(open, contact, (u - .63f) / .045f);
        if (u < .77f) return contact;
        if (u < .81f) return Mix(contact, recoil, (u - .77f) / .04f);
        return Mix(recoil, AttackGuard(), (u - .81f) / .19f);
    }
    static Pose ClashImpact(Pose open, Pose contact, float progress)
    {
        var p = Mix(contact, contact, 0);
        float close = Mathf.Clamp01(progress); close *= close;
        p.angle[SF] = Mathf.Lerp(open.angle[SF], contact.angle[SF], close);
        p.angle[SB] = Mathf.Lerp(open.angle[SB], contact.angle[SB], close);
        return p;
    }

    static void AimBuffHand(Pose p, bool front, Vector2 target, float bend)
    {
        int shoulder = front ? SF : SB, forearm = front ? AF : AB, hand = front ? HF : HB;
        Vector2 a = p.position[forearm], b = p.position[hand];
        Vector2 d = target - (Vector2)p.position[shoulder];
        float cosine = Mathf.Clamp((d.sqrMagnitude - a.sqrMagnitude - b.sqrMagnitude) / (2f * a.magnitude * b.magnitude), -1f, 1f);
        float elbow = Mathf.Acos(cosine) * bend;
        float heading = Mathf.Atan2(d.y, d.x) - Mathf.Atan2(b.magnitude * Mathf.Sin(elbow), a.magnitude + b.magnitude * Mathf.Cos(elbow));
        p.angle[shoulder] = Mathf.DeltaAngle(0f, (heading - Mathf.Atan2(a.y, a.x)) * Mathf.Rad2Deg);
        p.angle[forearm] = Mathf.DeltaAngle(0f, (elbow + Mathf.Atan2(a.y, a.x) - Mathf.Atan2(b.y, b.x)) * Mathf.Rad2Deg);
    }
    static Pose Buff(float t, float duration)
    {
        float u = Mathf.Clamp01(t / duration);
        var powered = new Pose();
        // Both arms open outward; slightly straighter elbows remain fixed while shoulders lift/lower.
        ArmAngles(powered, true, 0f, -85f, 12f);
        ArmAngles(powered, false, 0f, 85f, -25f);
        powered.position[HB] = new Vector3(-.090f, -.640f, 0f);
        powered.rearForearmFlip = true;
        powered.frontHandFlip = true;
        powered.angle[Head] = 8f;
        var raised = Mix(powered, powered, 0f);
        raised.angle[SF] = -65f;
        raised.angle[SB] = 65f;
        raised.angle[Head] = 4f;
        // Upright legs throughout. Lift to the outside, then lower without folding the elbows.
        if (u < .36f) return Mix(new Pose(), raised, u / .36f);
        if (u < .48f) return raised;
        if (u < .57f) return Mix(raised, powered, (u - .48f) / .09f);
        if (u < .82f)
        {
            float flex = (u - .57f) / .25f;
            float tension = Mathf.Sin(flex * Mathf.PI);
            // Expand the chest, press elbows down and add a restrained muscular tremor.
            powered.position[SF].y -= .018f * tension;
            powered.position[SB].y -= .018f * tension;
            powered.position[SF].x -= .025f * tension;
            powered.position[SB].x += .025f * tension;
            // Keep the outward forearm angles unchanged through the power pose.
            float tremor = Mathf.Sin(flex * Mathf.PI * 8f) * .65f * tension;
            powered.angle[SF] += tremor; powered.angle[SB] -= tremor;
            return powered;
        }
        return Mix(powered, new Pose(), (u - .82f) / .18f);
    }
    static Pose Groggy(float t)
    {
        var p = new Pose();
        p.position[Body] = new Vector3(-.055f, -.28f, 0);
        p.angle[Body] = -45f; p.angle[Bottom] = 45f;
        p.angle[Head] = -60f;
        p.position[SF] = new Vector3(-.780f, .850f, 0);
        ArmAngles(p, true, 45, -25, 4);
        ArmAngles(p, false, 45, -25, 15);
        Ground(p); // Forward knee bend; feet retain the rest ground height.
        // Power cuts out: a short freeze, then head/arms sag ahead of the collapsing stance.
        var start = AttackGuard();
        if (t <= .12f) return start;
        float collapse = Mathf.Clamp01((t - .12f) / 1.08f);
        var falling = Mix(start, p, collapse);
        float limbProgress = Smooth(Mathf.Clamp01((t - .12f) / .82f));
        foreach (int part in new[] { Head, SF, AF, HF, SB, AB, HB })
        {
            falling.angle[part] = Mathf.Lerp(start.angle[part], p.angle[part], limbProgress);
            falling.position[part] = Vector3.Lerp(start.position[part], p.position[part], limbProgress);
        }
        // Re-solve planted feet at every intermediate pelvis height.
        falling.frontStep = Mathf.Lerp(start.frontStep, p.frontStep, Smooth(collapse));
        falling.backStep = Mathf.Lerp(start.backStep, p.backStep, Smooth(collapse));
        Ground(falling);
        return falling;
    }
    static float SpriteBottom(Pose p, int part)
    {
        Vector2 size = part == Body ? new Vector2(146f / 64, 160f / 64) : new Vector2(83f / 64, 82f / 64);
        Vector2 pivot = part == Body ? new Vector2(.59f, .24f) : new Vector2(.45f, .37f);
        float y = float.PositiveInfinity;
        for (int a = 0; a < 2; a++) for (int b = 0; b < 2; b++) y = Mathf.Min(y, (WorldPosition(p, part) + Rotate(Vector2.Scale(new Vector2(a, b) - pivot, size), WorldAngle(p, part))).y);
        return y;
    }
    static Pose Death(float t)
    {
        // Raise the guard from an upright rest, then tilt only the head and hold.
        var uprightGuard = new Pose();
        ProtectChin(uprightGuard, true); ProtectChin(uprightGuard, false);
        var p = Mix(new Pose(), uprightGuard, Mathf.Clamp01(t / .65f));
        p.angle[Head] += 35f * Smooth(Mathf.Clamp01((t - .75f) / .65f));
        return p;
    }
    static AnimationClip Save(ClipSpec spec)
    {
        var clip = new AnimationClip { name = Prefix + spec.name, frameRate = 60 }; int count = Mathf.CeilToInt(spec.length * 60);
        var poses = new Pose[count + 1]; for (int i = 0; i <= count; i++)
        {
            poses[i] = spec.pose(spec.loop && i == count ? 0 : spec.length * i / count);
            poses[i].angle[HF] = Mathf.Clamp(poses[i].angle[HF], 0, 60); poses[i].angle[HB] = Mathf.Clamp(poses[i].angle[HB], -60, 60);
        }
        for (int bone = 0; bone < 15; bone++) for (int axis = 0; axis < 3; axis++)
            {
                var pos = new Keyframe[count + 1]; var rot = new Keyframe[count + 1];
                for (int i = 0; i <= count; i++) { float t = spec.length * i / count; pos[i] = new Keyframe(t, poses[i].position[bone][axis]); rot[i] = new Keyframe(t, axis == 2 ? poses[i].angle[bone] : 0); }
                SetCurve(clip, Paths[bone], "m_LocalPosition." + "xyz"[axis], pos);
                SetCurve(clip, Paths[bone], "localEulerAnglesRaw." + "xyz"[axis], rot);
                if ((bone == HB || bone == AB || bone == HF) && axis == 0)
                {
                    var scale = new Keyframe[count + 1];
                    for (int i = 0; i <= count; i++) scale[i] = new Keyframe(spec.length * i / count, (bone == HF ? poses[i].frontHandFlip : bone == HB ? poses[i].rearHandGuardFlip : poses[i].rearForearmFlip) ? -1f : 1f);
                    SetCurve(clip, Paths[bone], "m_LocalScale.x", scale, true);
                }
                else if (bone == AF && axis == 1)
                {
                    var scaleY = new Keyframe[count + 1];
                    for (int i = 0; i <= count; i++) scaleY[i] = new Keyframe(spec.length * i / count, poses[i].frontForearmScaleY);
                    SetCurve(clip, Paths[bone], "m_LocalScale.y", scaleY);
                }
                else SetCurve(clip, Paths[bone], "m_LocalScale." + "xyz"[axis], new[] { new Keyframe(0, 1), new Keyframe(spec.length, 1) });
            }
        float front = spec.name == "Dskill" || spec.name == "skill3" || spec.name == "skill5" ? 1f : 0f;
        foreach (int part in new[] { AB, HB })
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(Paths[part], typeof(MainBoss4ArmLayerPreview), "forceFront"),
                AnimationCurve.Constant(0, spec.length, front));
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = spec.loop; AnimationUtility.SetAnimationClipSettings(clip, settings);
        string path = Folder + "/" + clip.name + ".anim"; var old = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (old != null) { var events = AnimationUtility.GetAnimationEvents(old); EditorUtility.CopySerialized(clip, old); AnimationUtility.SetAnimationEvents(old, events); UnityEngine.Object.DestroyImmediate(clip); EditorUtility.SetDirty(old); return old; }
        AssetDatabase.CreateAsset(clip, path); return clip;
    }
    private static void SetCurve(AnimationClip clip, string path, string property, Keyframe[] keys, bool stepped = false)
    {
        var curve = new AnimationCurve(keys);
        // 촘촘한 샘플 사이 선형 보간: 자동 탄젠트에 의한 회전/발 높이 오버슈트 방지.
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, stepped ? AnimationUtility.TangentMode.Constant : AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, stepped ? AnimationUtility.TangentMode.Constant : AnimationUtility.TangentMode.Linear);
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
    [MenuItem("Tools/4/MainBoss 4/Setup Animator")]
    public static void SetupAnimator()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        BuildSpecs(); EnsureFolder(Folder);
        AnimatorController controller = null;
        foreach (string guid in AssetDatabase.FindAssets("MainBoss4_AC t:AnimatorController"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid); if (System.IO.Path.GetFileNameWithoutExtension(path) != "MainBoss4_AC") continue;
            if (controller != null) throw new InvalidOperationException("MainBoss4_AC 이름이 중복됩니다."); controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        }
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(Folder + "/MainBoss4_AC.controller");
        Param(controller, "MoveSpeed", AnimatorControllerParameterType.Float); Param(controller, "AttackSpeed", AnimatorControllerParameterType.Float);
        Param(controller, "IsChasing", AnimatorControllerParameterType.Bool); Param(controller, "IsAttacking", AnimatorControllerParameterType.Bool);
        Param(controller, "IsDead", AnimatorControllerParameterType.Bool);
        var sm = controller.layers[0].stateMachine;
        foreach (var child in sm.states)
            if (Obsolete(child.state.name)) sm.RemoveState(child.state);
        var existingParameters = controller.parameters;
        for (int i = existingParameters.Length - 1; i >= 0; i--)
            if (Obsolete(Prefix + existingParameters[i].name)) controller.RemoveParameter(i);
        var states = new Dictionary<string, AnimatorState>();
        int order = 0;
        foreach (var spec in specs)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/" + Prefix + spec.name + ".anim"); if (clip == null) clip = Save(spec);
            AnimatorState state = null; foreach (var c in sm.states) if (c.state.name == Prefix + spec.name) state = c.state;
            if (state == null) state = sm.AddState(Prefix + spec.name, new Vector3(280 + (order % 4) * 260, (order / 4) * 90)); order++;
            state.motion = clip; state.writeDefaultValues = false; state.speed = 1;
            state.speedParameter = "AttackSpeed"; state.speedParameterActive = !spec.loop && spec.name != "Death" && spec.name != "Groggy" && spec.name != "Dskill";
            foreach (var tr in state.transitions) if (tr.name.StartsWith("MB4_")) state.RemoveTransition(tr);
            states[spec.name] = state;
            if (!spec.loop && spec.name != "Death") Param(controller, spec.name, AnimatorControllerParameterType.Trigger);
        }
        foreach (var tr in sm.anyStateTransitions) if (tr.name.StartsWith("MB4_")) sm.RemoveAnyStateTransition(tr);
        sm.defaultState = states["Idle"];
        string[] locomotion = { "Idle", "Walk", "Run" };
        foreach (var pair in states)
        {
            string name = pair.Key; var state = pair.Value;
            if (name == "Death") continue;
            if (name != "Idle" && name != "Walk" && name != "Run")
            {
                var enter = sm.AddAnyStateTransition(state); enter.name = "MB4_" + name; enter.hasExitTime = false; enter.duration = .025f; enter.hasFixedDuration = true; enter.canTransitionToSelf = true;
                enter.AddCondition(AnimatorConditionMode.If, 0, name); enter.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
            }
            foreach (string dest in locomotion)
            {
                if (dest == name) continue; var tr = state.AddTransition(states[dest]); tr.name = "MB4_Return";
                tr.hasExitTime = false; tr.duration = .08f; tr.hasFixedDuration = true;
                tr.AddCondition(AnimatorConditionMode.IfNot, 0, "IsAttacking"); tr.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
                tr.AddCondition(dest == "Idle" ? AnimatorConditionMode.Less : AnimatorConditionMode.Greater, .01f, "MoveSpeed");
                if (dest != "Idle") tr.AddCondition(dest == "Run" ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "IsChasing");
            }
        }
        var death = sm.AddAnyStateTransition(states["Death"]); death.name = "MB4_Death"; death.hasExitTime = false; death.duration = .04f; death.canTransitionToSelf = false; death.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
        EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets(); Assign(controller);
        foreach (var animator in Resources.FindObjectsOfTypeAll<Animator>())
        {
            if (EditorUtility.IsPersistent(animator) || !animator.gameObject.scene.IsValid()) continue;
            if (animator.runtimeAnimatorController == controller && animator.transform.Find("Body") != null)
                AttachArmPreview(animator.transform, true);
        }
        foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { Folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (Obsolete(System.IO.Path.GetFileNameWithoutExtension(path))) AssetDatabase.DeleteAsset(path);
        }
        AssetDatabase.SaveAssets();
    }
    static void AttachArmPreview(Transform visual, bool scene)
    {
        foreach (int part in new[] { AB, HB })
        {
            Transform bone = visual.Find(Paths[part]);
            if (bone == null || bone.GetComponent<SpriteRenderer>() == null) continue;
            var helper = bone.GetComponent<MainBoss4ArmLayerPreview>();
            if (helper == null) helper = scene ? Undo.AddComponent<MainBoss4ArmLayerPreview>(bone.gameObject) : bone.gameObject.AddComponent<MainBoss4ArmLayerPreview>();
            helper.CaptureDefault();
            EditorUtility.SetDirty(helper);
        }
        if (scene) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(visual.gameObject.scene);
    }
    static bool Obsolete(string name)
    { return name.StartsWith(Prefix + "Step_", StringComparison.Ordinal) || name == Prefix + "Recover" || name == Prefix + "skill2_Desire"; }
    static void Param(AnimatorController c, string name, AnimatorControllerParameterType type)
    { foreach (var p in c.parameters) if (p.name == name) { if (p.type != type) throw new InvalidOperationException(name + " 타입 충돌"); return; } c.AddParameter(name, type); }
    static void Assign(AnimatorController controller)
    {
        const string folder = "Assets/Prefab/4"; if (!AssetDatabase.IsValidFolder(folder)) return;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { folder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid); if (System.IO.Path.GetFileNameWithoutExtension(path) != "MainBoss 4") continue;
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (Transform visual in root.GetComponentsInChildren<Transform>(true))
                {
                    if (visual.name != "MainBoss 4" || visual.Find("Body") == null) continue;
                    var animator = visual.GetComponent<Animator>(); if (animator == null) animator = visual.gameObject.AddComponent<Animator>();
                    animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
                    AttachArmPreview(visual, false);
                    foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (component == null) continue; var so = new SerializedObject(component);
                        foreach (string field in new[] { "bossAnimator" }) { var prop = so.FindProperty(field); if (prop != null) prop.objectReferenceValue = animator; }
                        var vr = so.FindProperty("visualRoot"); if (vr != null) vr.objectReferenceValue = visual;
                        var dc = so.FindProperty("deathAnimation"); if (dc != null) dc.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder + "/MainBoss4_Death.anim");
                        var ds = so.FindProperty("deathAnimationState"); if (ds != null) ds.stringValue = "MainBoss4_Death";
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }

}
#endif
