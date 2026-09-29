#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Assets/Editor/TankMob3_AnimGenerators.cs
// Animator는 시각 루트 TankMob 3에 둡니다. 물리 루트 이동/방향 전환은 AI가 담당합니다.
public static class TankMob3_AnimGenerators
{
    public const string OutputFolder = "Assets/Animations/3/TankMob 3";
    public const float AttackImpactTime = 0.88f;
    private const int SampleRate = 60;
    private const int Body = 0, Head = 1, SF = 2, HF = 3, SB = 4, HB = 5,
        Sword = 6, Bottom = 7, FF = 8, FB = 9;
    private static readonly string[] Paths = {
        "Body", "Body/Head", "Body/Shoulder_F", "Body/Shoulder_F/Hand_F",
        "Body/Shoulder_B", "Body/Shoulder_B/Hand_B", "Body/Shoulder_B/Hand_B/Sword",
        "Body/Bottom", "Body/Bottom/Foot_F", "Body/Bottom/Foot_B"
    };
    private static readonly Vector3[] RestPositions = {
        new Vector3(0, 0, 0), new Vector3(-0.132f, 0.580f, 0),
        new Vector3(-0.662f, 0.495f, 0), new Vector3(-0.184f, -0.435f, 0),
        new Vector3(0.081f, 0.544f, 0), new Vector3(0.198f, -0.468f, 0),
        new Vector3(0.216f, -0.430f, 0), new Vector3(0, -0.679f, 0),
        new Vector3(-0.509f, 0, 0), new Vector3(0.132f, 0, 0)
    };
    private static readonly float[] RestAngles = { 0, 0, 0, 0, -0.1f, 0, -60, 0, 0, 0 };

    private sealed class Pose
    {
        public Vector3[] position = (Vector3[])RestPositions.Clone();
        public float[] angle = (float[])RestAngles.Clone();
    }

    [MenuItem("Tools/3/TankMob 3/Generate Animations")]
    public static void GenerateAll()
    {
        EnsureFolder(OutputFolder);
        Generate("TankMob3_Idle", 2.4f, true, Idle);
        Generate("TankMob3_Walk", 1.3f, true, t => Locomotion(t, 1.3f, false));
        Generate("TankMob3_Run", 0.86f, true, t => Locomotion(t, 0.86f, true));
        AnimationClip attack = Generate("TankMob3_Attack", 1.7f, false, Attack);
        Generate("TankMob3_Hit", 0.30f, false, Hit);
        Generate("TankMob3_Death", 0.65f, false, Death);
        SetupAnimator();
        AssetDatabase.SaveAssets();
        Selection.activeObject = attack;
        EditorGUIUtility.PingObject(attack);
        Debug.Log("TankMob 3: Idle / Walk / Run / Attack / Hit / Death 생성 완료. 공격 임팩트 0.88초. " + OutputFolder);
    }

    [MenuItem("Tools/3/TankMob 3/Clips/Idle")]
    public static void GenerateIdle() => GenerateSingle("TankMob3_Idle", 2.4f, true, Idle);
    [MenuItem("Tools/3/TankMob 3/Clips/Walk")]
    public static void GenerateWalk() => GenerateSingle("TankMob3_Walk", 1.3f, true, t => Locomotion(t, 1.3f, false));
    [MenuItem("Tools/3/TankMob 3/Clips/Run")]
    public static void GenerateRun() => GenerateSingle("TankMob3_Run", 0.86f, true, t => Locomotion(t, 0.86f, true));
    [MenuItem("Tools/3/TankMob 3/Clips/Attack")]
    public static void GenerateAttack() => GenerateSingle("TankMob3_Attack", 1.7f, false, Attack);

    [MenuItem("Tools/3/TankMob 3/Clips/Hit")]
    public static void GenerateHit() => GenerateSingle("TankMob3_Hit", 0.30f, false, Hit);
    [MenuItem("Tools/3/TankMob 3/Clips/Death")]
    public static void GenerateDeath() => GenerateSingle("TankMob3_Death", 0.65f, false, Death);

    private static void GenerateSingle(string name, float duration, bool loop, Func<float, Pose> evaluate)
    {
        EnsureFolder(OutputFolder);
        AnimationClip clip = Generate(name, duration, loop, evaluate);
        SetupAnimator();
        AssetDatabase.SaveAssets();
        Selection.activeObject = clip;
        EditorGUIUtility.PingObject(clip);
    }

    private const string ControllerName = "TankMob3_AC";
    private const string TransitionPrefix = "TankMob3_Generated_";

    [MenuItem("Tools/3/TankMob 3/Setup Animator")]
    public static void SetupAnimator()
    {
        EnsureFolder(OutputFolder);
        AnimatorController controller = null;
        foreach (string guid in AssetDatabase.FindAssets(ControllerName + " t:AnimatorController"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) != ControllerName) continue;
            if (controller != null)
                throw new InvalidOperationException("TankMob3_AC가 여러 개 있습니다. 사용할 컨트롤러만 이 이름으로 남겨주세요.");
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        }
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(OutputFolder + "/" + ControllerName + ".controller");
        EnsureParameter(controller, "MoveSpeed", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "IsChasing", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Hit", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "IsDead", AnimatorControllerParameterType.Bool);
        string[] names = { "TankMob3_Idle", "TankMob3_Walk", "TankMob3_Run", "TankMob3_Attack", "TankMob3_Hit", "TankMob3_Death" };
        var clips = new AnimationClip[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            clips[i] = AssetDatabase.LoadAssetAtPath<AnimationClip>(OutputFolder + "/" + names[i] + ".anim");
            if (clips[i] != null) continue;
            // 셋업은 없는 클립만 생성한다. 수작업으로 수정한 기존 클립은 유지한다.
            if (i == 0) clips[i] = Generate(names[i], 2.4f, true, Idle);
            if (i == 1) clips[i] = Generate(names[i], 1.3f, true, t => Locomotion(t, 1.3f, false));
            if (i == 2) clips[i] = Generate(names[i], 0.86f, true, t => Locomotion(t, 0.86f, true));
            if (i == 3) clips[i] = Generate(names[i], 1.7f, false, Attack);
            if (i == 4) clips[i] = Generate(names[i], 0.30f, false, Hit);
            if (i == 5) clips[i] = Generate(names[i], 0.65f, false, Death);
        }
        var machine = controller.layers[0].stateMachine;
        var states = new AnimatorState[names.Length];
        for (int i = 0; i < states.Length; i++)
        {
            foreach (var child in machine.states)
                if (child.state.name == names[i]) states[i] = child.state;
            if (states[i] == null) states[i] = machine.AddState(names[i], new Vector3(300, i * 90, 0));
            states[i].motion = clips[i];
            states[i].writeDefaultValues = false;
            states[i].speed = 1f;
            states[i].speedParameterActive = false;
            foreach (var transition in states[i].transitions)
                if (transition.name.StartsWith(TransitionPrefix, StringComparison.Ordinal)) states[i].RemoveTransition(transition);
            var settings = AnimationUtility.GetAnimationClipSettings(clips[i]);
            settings.loopTime = i < 3;
            AnimationUtility.SetAnimationClipSettings(clips[i], settings);
            EditorUtility.SetDirty(clips[i]);
        }
        foreach (var transition in machine.anyStateTransitions)
            if (transition.name.StartsWith(TransitionPrefix, StringComparison.Ordinal)) machine.RemoveAnyStateTransition(transition);
        machine.defaultState = states[0];
        ApplyStateLayout(machine, states);
        var death = machine.AddAnyStateTransition(states[5]);
        death.name = TransitionPrefix + "Death";
        death.hasExitTime = false;
        death.duration = 0.04f;
        death.hasFixedDuration = true;
        death.canTransitionToSelf = false;
        death.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
        var hit = machine.AddAnyStateTransition(states[4]);
        hit.name = TransitionPrefix + "Hit";
        hit.hasExitTime = false;
        hit.duration = 0.025f;
        hit.hasFixedDuration = true;
        hit.canTransitionToSelf = true;
        hit.AddCondition(AnimatorConditionMode.If, 0, "Hit");
        hit.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
        for (int to = 0; to < 3; to++)
            AddLocomotionConditions(AddTransition(states[4], states[to], true), to);
        // Locomotion에서만 공격 진입: 공격 도중 트리거로 공격이 재시작되지 않는다.
        for (int from = 0; from < 3; from++)
        {
            var attack = AddTransition(states[from], states[3], false);
            attack.duration = 0f;
            attack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            for (int to = 0; to < 3; to++)
                if (from != to) AddLocomotionConditions(AddTransition(states[from], states[to], false), to);
        }
        for (int to = 0; to < 3; to++)
            AddLocomotionConditions(AddTransition(states[3], states[to], true), to);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssignControllerToPrefab(controller);
        Selection.activeObject = controller;
        EditorGUIUtility.PingObject(controller);
        Debug.Log("TankMob3_AC 설정 완료: MoveSpeed(float), IsChasing(bool), Attack(trigger). 프리팹 자동 연결 결과는 Console에서 확인하세요.");
    }

    // 사진 기준 고정 배치. 기존 상태도 재셋업할 때 같은 위치로 정렬합니다.
    private static void ApplyStateLayout(AnimatorStateMachine machine, AnimatorState[] states)
    {
        Vector3[] positions = {
            new Vector3(320, 160, 0), // Idle: 중앙 위
            new Vector3(180, 400, 0), // Walk: 아래 왼쪽
            new Vector3(460, 400, 0), // Run: 아래 오른쪽
            new Vector3(620, 260, 0), // Attack: 오른쪽
            new Vector3(20, 260, 0),  // Hit: 왼쪽
            new Vector3(20, 80, 0)    // Death: 왼쪽 위
        };
        ChildAnimatorState[] children = machine.states;
        for (int i = 0; i < children.Length; i++)
        {
            for (int j = 0; j < states.Length; j++)
            {
                if (children[i].state != states[j]) continue;
                children[i].position = positions[j];
                break;
            }
        }
        machine.states = children;
        machine.anyStatePosition = new Vector3(20, 170, 0);
        machine.entryPosition = new Vector3(320, 10, 0);
        machine.exitPosition = new Vector3(920, 400, 0);
        EditorUtility.SetDirty(machine);
    }

    private const string PrefabFolder = "Assets/Prefab/3";
    private const string MobName = "TankMob 3";

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
                Debug.LogWarning("프리팹 자동 연결 생략: Body를 자식으로 가진 TankMob 3 시각 루트를 찾지 못했습니다. " + prefabPath);
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

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (var parameter in controller.parameters)
        {
            if (parameter.name != name) continue;
            if (parameter.type != type)
                throw new InvalidOperationException(name + " 파라미터 타입이 다릅니다. 필요한 타입: " + type);
            return;
        }
        controller.AddParameter(name, type);
    }

    private static AnimatorStateTransition AddTransition(AnimatorState from, AnimatorState to, bool exitTime)
    {
        var transition = from.AddTransition(to);
        transition.name = TransitionPrefix + from.name + "_" + to.name;
        transition.hasExitTime = exitTime;
        transition.exitTime = 1f;
        transition.hasFixedDuration = true;
        transition.duration = 0.06f;
        transition.offset = 0f;
        transition.canTransitionToSelf = false;
        transition.interruptionSource = TransitionInterruptionSource.None;
        transition.AddCondition(AnimatorConditionMode.IfNot, 0, "IsDead");
        return transition;
    }

    private static void AddLocomotionConditions(AnimatorStateTransition transition, int destination)
    {
        transition.AddCondition(destination == 0 ? AnimatorConditionMode.Less : AnimatorConditionMode.Greater,
            0.01f, "MoveSpeed");
        if (destination != 0)
            transition.AddCondition(destination == 2 ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "IsChasing");
    }

    private static Pose Idle(float t)
    {
        var p = new Pose();
        float breath = 0.5f - 0.5f * Mathf.Cos(t / 2.4f * Mathf.PI * 2f);
        p.position[Body].y -= 0.018f * breath;
        p.position[Head].y -= 0.004f * breath;
        p.angle[Head] = -0.8f * breath;
        p.angle[SF] = 1.8f * breath;
        p.angle[HF] = -1.0f * breath;
        p.angle[SB] = RestAngles[SB] - 1.2f * breath;
        p.angle[HB] += 1.8f * breath;
        GroundFoot(p, FF, RestPositions[FF].x, 0, 0);
        GroundFoot(p, FB, RestPositions[FB].x, 0, 0);
        return p;
    }

    private static Pose Locomotion(float t, float duration, bool run)
    {
        var p = new Pose();
        float phase = t / duration;
        float wave = Mathf.Sin(phase * Mathf.PI * 2f);
        float bob = 0.5f - 0.5f * Mathf.Cos(phase * Mathf.PI * 4f);
        // 오른쪽(+X) 진행. 지지발은 뒤로 밀리고, 들린 발이 앞으로 돌아옵니다.
        p.angle[Body] = (run ? -7f : -2f) + (run ? 1.4f : 0.8f) * wave;
        p.position[Body].y = -(run ? 0.045f : 0.025f) * bob;
        p.position[Body].x = run ? 0.035f : 0;
        p.angle[Head] = -p.angle[Body] * 0.65f;
        p.angle[Bottom] = -p.angle[Body] * 0.8f;
        p.angle[SF] = (run ? -9f : 0) + (run ? 9f : 5f) * wave;
        p.angle[HF] = (run ? 15f : 3f) - (run ? 7f : 3f) * wave;
        // 무기 팔의 진폭은 작게, 손목 반응은 약간 늦게 줍니다.
        p.angle[SB] = RestAngles[SB] + (run ? 12f : 4f) - (run ? 4f : 2f) * wave;
        p.angle[HB] = (run ? 12f : 5f) + 2f * Mathf.Sin(phase * Mathf.PI * 2f - 0.35f);
        p.angle[Sword] = -60f - 2f * Mathf.Sin(phase * Mathf.PI * 2f - 0.6f);
        StepFoot(p, FF, phase, run);
        StepFoot(p, FB, phase + 0.5f, run);
        return p;
    }

    private static void StepFoot(Pose p, int foot, float phase, bool run)
    {
        float u = Mathf.Repeat(phase, 1f);
        float stride = run ? 0.23f : 0.15f;
        float x, lift;
        if (u < 0.5f)
        {
            x = Mathf.Lerp(stride, -stride, u * 2f);
            lift = 0;
        }
        else
        {
            float swing = (u - 0.5f) * 2f;
            x = Mathf.Lerp(-stride, stride, Smooth(swing));
            lift = (run ? 0.065f : 0.045f) * Mathf.Pow(Mathf.Sin(swing * Mathf.PI), 2f);
        }
        GroundFoot(p, foot, RestPositions[foot].x + x, lift, 0);
    }

    // 시각 루트 좌표계에서 발 기준점을 고정. Body 기울기/호흡으로 발이 뜨지 않게 역변환합니다.
    private static void GroundFoot(Pose p, int foot, float rootX, float lift, float angle)
    {
        Vector2 hip = (Vector2)p.position[Body] + Rotate(p.position[Bottom], p.angle[Body]);
        float hipAngle = p.angle[Body] + p.angle[Bottom];
        Vector2 target = new Vector2(rootX, RestPositions[Bottom].y + lift);
        Vector2 local = Rotate(target - hip, -hipAngle);
        p.position[foot] = new Vector3(local.x, local.y, RestPositions[foot].z);
        p.angle[foot] = angle - hipAngle;
    }

    private static Pose Attack(float t)
    {
        // Rest -> 무게를 모아 뒤로 들기 -> 짧은 장전 -> 가속 내려베기 -> 충격 유지 -> 회수.
        float[] times = { 0, 0.38f, 0.64f, 0.72f, AttackImpactTime, 1.00f, 1.16f, 1.42f, 1.7f };
        float[] body = { 0, 4, 8, 8, -20, -20, -14, -5, 0 };
        float[] height = { 0, -0.025f, -0.04f, -0.04f, -0.075f, -0.075f, -0.045f, -0.015f, 0 };
        float[] forward = { 0, -0.025f, -0.045f, -0.045f, 0.13f, 0.13f, 0.09f, 0.035f, 0 };
        float[] shoulder = { -0.1f, 25, 60, 60, 55, 55, 40, 16, -0.1f };
        float[] hand = { 0, 26, 55, 55, -55, -55, -36, -12, 0 };
        float[] front = { 0, -9, -18, -18, 18, 18, 10, 4, 0 };
        var p = new Pose();
        p.angle[Body] = Track(t, times, body);
        p.position[Body].y = Track(t, times, height);
        p.position[Body].x = Track(t, times, forward);
        p.angle[SB] = Track(t, times, shoulder);
        p.angle[HB] = Track(t, times, hand);
        p.angle[SF] = Track(t, times, front);
        p.angle[HF] = -0.55f * p.angle[SF];
        p.angle[Head] = -p.angle[Body] * 0.55f;
        p.angle[Bottom] = -p.angle[Body] * 0.65f;
        // 대검 local 회전/손잡이 위치는 유지하여 손에서 미끄러지는 현상을 피합니다.
        GroundFoot(p, FF, RestPositions[FF].x, 0, 0);
        GroundFoot(p, FB, RestPositions[FB].x, 0, 0);
        return p;
    }

    private static Pose Hit(float t)
    {
        var p = new Pose();
        float kick = t < 0.06f ? Smooth(Mathf.Clamp01(t / 0.06f))
            : 1f - Smooth(Mathf.Clamp01((t - 0.06f) / 0.24f));
        p.position[Body].x -= 0.055f * kick;
        p.position[Body].y -= 0.015f * kick;
        p.angle[Body] = 6f * kick;
        p.angle[Head] = 5f * kick;
        p.angle[SF] = -12f * kick;
        p.angle[HF] = 8f * kick;
        p.angle[SB] = RestAngles[SB] - 5f * kick;
        p.angle[HB] += 10f * kick;
        p.angle[Bottom] = -6f * kick;
        GroundFoot(p, FF, RestPositions[FF].x, 0, 0);
        GroundFoot(p, FB, RestPositions[FB].x, 0, 0);
        return p;
    }

    private static Pose Death(float t)
    {
        var p = new Pose();
        float fall = Smooth(Mathf.Clamp01((t - 0.05f) / 0.43f));
        float settle = Smooth(Mathf.Clamp01((t - 0.48f) / 0.17f));
        p.position[Body] = new Vector3(-0.19f * fall, -0.30f * fall - 0.025f * settle, 0);
        p.angle[Body] = 72f * fall - 4f * settle;
        p.angle[Head] = 12f * fall;
        p.angle[SF] = -22f * fall;
        p.angle[HF] = 18f * fall;
        p.angle[SB] = RestAngles[SB] - 28f * fall;
        p.angle[HB] = -35f * fall;
        p.angle[Bottom] = -22f * fall;
        GroundFoot(p, FF, RestPositions[FF].x - 0.06f * fall, 0, 18f * fall);
        GroundFoot(p, FB, RestPositions[FB].x + 0.06f * fall, 0, -12f * fall);
        return p; // 마지막 쓰러진 자세 유지. 사망 클립은 Rest로 복귀하지 않는다.
    }

    private static float Track(float t, float[] times, float[] values)
    {
        for (int i = 1; i < times.Length; i++)
        {
            if (t > times[i]) continue;
            float u = Mathf.Clamp01((t - times[i - 1]) / (times[i] - times[i - 1]));
            // 공격 스트로크는 초반이 느리고 후반이 빠릅니다. 회수는 부드럽게 감속합니다.
            float blend = i == 4 ? u * u : Smooth(u);
            return Mathf.LerpUnclamped(values[i - 1], values[i], blend);
        }
        return values[values.Length - 1];
    }

    private static float Smooth(float t) => t * t * (3f - 2f * t);
    private static Vector2 Rotate(Vector2 v, float degrees)
    {
        float a = degrees * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
        return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
    }

    private static AnimationClip Generate(string name, float duration, bool loop, Func<float, Pose> evaluate)
    {
        var clip = new AnimationClip { name = name, frameRate = SampleRate };
        // 주요 타이밍도 포함하여 임팩트가 샘플 사이로 밀리지 않게 합니다.
        var timeSet = new SortedSet<float> { 0, duration };
        int count = Mathf.RoundToInt(duration * SampleRate);
        for (int i = 1; i < count; i++) timeSet.Add(duration * i / count);
        if (name == "TankMob3_Attack")
            foreach (float t in new[] { 0.38f, 0.64f, 0.72f, AttackImpactTime, 1.00f, 1.16f, 1.42f }) timeSet.Add(t);
        var times = new List<float>(timeSet);
        var poses = new Pose[times.Count];
        for (int i = 0; i < times.Count; i++)
            poses[i] = evaluate(loop && i == times.Count - 1 ? 0 : times[i]);
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
