#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Assets/Editor/TankMob5_AnimGenerators.cs
// Animator는 시각 루트 TankMob 5에 둡니다. 물리 루트 이동/방향 전환은 AI가 담당합니다.
public static class TankMob5_AnimGenerators
{
    public const string OutputFolder = "Assets/Animations/5/TankMob 5";
    public const float AttackImpactTime = 0.92f;
    private const int SampleRate = 60;
    private const int Body = 0, Head = 1, SF = 2, HF = 3, SB = 4, HB = 5,
        Axe = 6, Bottom = 7, FF = 8, FB = 9;
    private static readonly string[] Paths = {
        "Body",
        "Body/Head",
        "Body/Shoulder_F",
        "Body/Shoulder_F/Hand_F",
        "Body/Shoulder_B",
        "Body/Shoulder_B/Hand_B",
        "Body/Shoulder_B/Hand_B/Club",
        "Body/Bottom",
        "Body/Bottom/Foot_F",
        "Body/Bottom/Foot_B"
    };
    private static readonly Vector3[] RestPositions = {
        new Vector3(0.0f, 0.0f, 0.0f),
        new Vector3(-0.033f, 0.833f, 0.0f),
        new Vector3(-0.612f, 0.628f, 0.0f),
        new Vector3(-0.137f, -0.429f, 0.0f),
        new Vector3(0.296f, 0.702f, 0.0f),
        new Vector3(0.096f, -0.489f, 0.0f),
        new Vector3(-0.0005f, -0.4556f, 0.0f),
        new Vector3(0.0f, -0.434f, 0.0f),
        new Vector3(-0.405f, 0.0f, 0.0f),
        new Vector3(0.079f, 0.0f, 0.0f)
    };
    private static readonly float[] RestAngles = { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, -90.0f, 0.0f, 0.0f, 0.0f };

    private sealed class Pose
    {
        public Vector3[] position = (Vector3[])RestPositions.Clone();
        public float[] angle = (float[])RestAngles.Clone();
    }

    [MenuItem("Tools/5/TankMob 5/Generate Animations")]
    public static void GenerateAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EnsureFolder(OutputFolder);
        Generate("TankMob5_Idle", 2.4f, true, Idle);
        Generate("TankMob5_Walk", 1.30f, true, t => Locomotion(t, 1.30f, false));
        Generate("TankMob5_Run", 0.85f, true, t => Locomotion(t, 0.85f, true));
        AnimationClip attack = Generate("TankMob5_Attack", 1.9f, false, Attack);
        Generate("TankMob5_Hit", 0.30f, false, Hit);
        Generate("TankMob5_Death", 0.85f, false, Death);
        SetupAnimator();
        AssetDatabase.SaveAssets();
        Selection.activeObject = attack;
        EditorGUIUtility.PingObject(attack);
        Debug.Log("TankMob 5: Idle / Walk / Run / Attack / Hit / Death 생성 완료. 공격 임팩트 0.92초. " + OutputFolder);
    }

    [MenuItem("Tools/5/TankMob 5/Clips/Idle")]
    public static void GenerateIdle() => GenerateSingle("TankMob5_Idle", 2.4f, true, Idle);
    [MenuItem("Tools/5/TankMob 5/Clips/Walk")]
    public static void GenerateWalk() => GenerateSingle("TankMob5_Walk", 1.30f, true, t => Locomotion(t, 1.30f, false));
    [MenuItem("Tools/5/TankMob 5/Clips/Run")]
    public static void GenerateRun() => GenerateSingle("TankMob5_Run", 0.85f, true, t => Locomotion(t, 0.85f, true));
    [MenuItem("Tools/5/TankMob 5/Clips/Attack")]
    public static void GenerateAttack() => GenerateSingle("TankMob5_Attack", 1.9f, false, Attack);

    [MenuItem("Tools/5/TankMob 5/Clips/Hit")]
    public static void GenerateHit() => GenerateSingle("TankMob5_Hit", 0.30f, false, Hit);
    [MenuItem("Tools/5/TankMob 5/Clips/Death")]
    public static void GenerateDeath() => GenerateSingle("TankMob5_Death", 0.85f, false, Death);

    private static void GenerateSingle(string name, float duration, bool loop, Func<float, Pose> evaluate)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EnsureFolder(OutputFolder);
        AnimationClip clip = Generate(name, duration, loop, evaluate);
        SetupAnimator();
        AssetDatabase.SaveAssets();
        Selection.activeObject = clip;
        EditorGUIUtility.PingObject(clip);
    }

    private const string ControllerName = "TankMob5_AC";
    private const string TransitionPrefix = "TankMob5_Generated_";

    [MenuItem("Tools/5/TankMob 5/Setup Animator")]
    public static void SetupAnimator()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EnsureFolder(OutputFolder);
        AnimatorController controller = null;
        foreach (string guid in AssetDatabase.FindAssets(ControllerName + " t:AnimatorController"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) != ControllerName) continue;
            if (controller != null)
                throw new InvalidOperationException("TankMob5_AC가 여러 개 있습니다. 사용할 컨트롤러만 이 이름으로 남겨주세요.");
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        }
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(OutputFolder + "/" + ControllerName + ".controller");
        EnsureParameter(controller, "MoveSpeed", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "IsChasing", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Hit", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "IsDead", AnimatorControllerParameterType.Bool);
        string[] names = { "TankMob5_Idle", "TankMob5_Walk", "TankMob5_Run", "TankMob5_Attack", "TankMob5_Hit", "TankMob5_Death" };
        var clips = new AnimationClip[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            clips[i] = AssetDatabase.LoadAssetAtPath<AnimationClip>(OutputFolder + "/" + names[i] + ".anim");
            if (clips[i] != null) continue;
            // 셋업은 없는 클립만 생성한다. 수작업으로 수정한 기존 클립은 유지한다.
            if (i == 0) clips[i] = Generate(names[i], 2.4f, true, Idle);
            if (i == 1) clips[i] = Generate(names[i], 1.30f, true, t => Locomotion(t, 1.30f, false));
            if (i == 2) clips[i] = Generate(names[i], 0.85f, true, t => Locomotion(t, 0.85f, true));
            if (i == 3) clips[i] = Generate(names[i], 1.9f, false, Attack);
            if (i == 4) clips[i] = Generate(names[i], 0.30f, false, Hit);
            if (i == 5) clips[i] = Generate(names[i], 0.85f, false, Death);
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
        UpdateEnemyData();
        Selection.activeObject = controller;
        EditorGUIUtility.PingObject(controller);
        Debug.Log("TankMob5_AC 설정 완료: MoveSpeed(float), IsChasing(bool), Attack(trigger). 프리팹 자동 연결 결과는 Console에서 확인하세요.");
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

    private const string PrefabFolder = "Assets/Prefab/5";
    private const string MobName = "TankMob 5";

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
                Debug.LogWarning("프리팹 자동 연결 생략: Body를 자식으로 가진 TankMob 5 시각 루트를 찾지 못했습니다. " + prefabPath);
                return;
            }
            Animator animator = visual.GetComponent<Animator>();
            if (animator == null) animator = visual.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component == null) continue;
                var serialized = new SerializedObject(component);
                SetReference(serialized, "hitAnimation", AssetDatabase.LoadAssetAtPath<AnimationClip>(OutputFolder + "/TankMob5_Hit.anim"));
                SetReference(serialized, "deathAnimation", AssetDatabase.LoadAssetAtPath<AnimationClip>(OutputFolder + "/TankMob5_Death.anim"));
                SetString(serialized, "hitAnimationState", "TankMob5_Hit");
                SetString(serialized, "deathAnimationState", "TankMob5_Death");
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out bool success);
            if (success) Debug.Log("컨트롤러 자동 연결 완료: " + prefabPath + " → " + MobName + " / " + controller.name);
            else Debug.LogError("프리팹 저장 실패: " + prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void SetReference(SerializedObject obj, string name, UnityEngine.Object value)
    {
        var property = obj.FindProperty(name);
        if (property != null && property.propertyType == SerializedPropertyType.ObjectReference)
            property.objectReferenceValue = value;
    }
    private static void SetString(SerializedObject obj, string name, string value)
    {
        var property = obj.FindProperty(name);
        if (property != null && property.propertyType == SerializedPropertyType.String)
            property.stringValue = value;
    }
    private static void UpdateEnemyData()
    {
        const string path = "Assets/Data/Enemys/Stage5/Stage5_Enemy_2.asset";
        var asset = AssetDatabase.LoadMainAssetAtPath(path);
        if (asset == null) { Debug.LogWarning("데이터 자동 반영 생략: " + path); return; }
        var serialized = new SerializedObject(asset);
        var pre = serialized.FindProperty("preAttackDelay");
        var post = serialized.FindProperty("postAttackDelay");
        if (pre == null || post == null || pre.propertyType != SerializedPropertyType.Float || post.propertyType != SerializedPropertyType.Float)
        { Debug.LogWarning("데이터의 preAttackDelay/postAttackDelay 필드를 찾지 못했습니다: " + path); return; }
        Undo.RecordObject(asset, "TankMob5 attack timing");
        pre.floatValue = AttackImpactTime;
        post.floatValue = 1.9f - AttackImpactTime;
        serialized.ApplyModifiedProperties();
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
        Debug.Log("Stage5 attack timing: " + pre.floatValue + " / " + post.floatValue);
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
        p.position[Body].y -= 0.008f * breath;
        p.position[Head].y -= 0.002f * breath;
        p.angle[Head] = -0.8f * breath;
        p.angle[SF] = 0.6f * breath;
        p.angle[HF] = -1.0f * breath;
        p.angle[SB] = -1.2f * breath;
        p.angle[HB] += 0.7f * breath;
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
        p.angle[Body] = (run ? -11f : -3f) + (run ? 0.9f : 0.55f) * wave;
        p.position[Body].y = -(run ? 0.035f : 0.018f) * bob;
        p.position[Body].x = run ? 0.035f : 0;
        p.angle[Head] = -p.angle[Body] * 0.65f;
        p.angle[Bottom] = -p.angle[Body] * 0.8f;
        p.angle[SF] = (run ? -9f : 0) + (run ? 11f : 6f) * wave;
        p.angle[HF] = (run ? 15f : 3f) - (run ? 7f : 3f) * wave;
        // 무기 팔의 진폭은 작게, 손목 반응은 약간 늦게 줍니다.
        p.angle[SB] = (run ? 9f : 2f) - (run ? 7f : 4f) * wave;
        p.angle[HB] = RestAngles[HB] + (run ? 8f : 3f) + 3f * Mathf.Sin(phase * Mathf.PI * 2f - 0.35f);
        p.angle[Axe] = RestAngles[Axe] - 2f * Mathf.Sin(phase * Mathf.PI * 2f - 0.6f);
        StepFoot(p, FF, phase, run);
        StepFoot(p, FB, phase + 0.5f, run);
        return p;
    }

    private static void StepFoot(Pose p, int foot, float phase, bool run)
    {
        float u = Mathf.Repeat(phase, 1f);
        float stride = run ? 0.26f : 0.16f;
        float x, lift;
        if (u < 0.5f)
        {
            x = Mathf.Lerp(stride, -stride, Smooth(u * 2f));
            lift = 0;
        }
        else
        {
            float swing = (u - 0.5f) * 2f;
            x = Mathf.Lerp(-stride, stride, Smooth(swing));
            lift = (run ? 0.075f : 0.045f) * Mathf.Pow(Mathf.Sin(swing * Mathf.PI), 2f);
        }
        GroundFoot(p, foot, RestPositions[foot].x + x, lift, 0);
    }

    // 시각 루트 좌표계에서 발 기준점을 고정. Body 기울기/호흡으로 발이 뜨지 않게 역변환합니다.
    private static void GroundFoot(Pose p, int foot, float rootX, float lift, float angle)
    {
        Vector2 hip = (Vector2)p.position[Body] + Rotate(p.position[Bottom], p.angle[Body]);
        float hipAngle = p.angle[Body] + p.angle[Bottom];
        Vector2 target = new Vector2(RestPositions[Body].x + RestPositions[Bottom].x + rootX,
            RestPositions[Body].y + RestPositions[Bottom].y + RestPositions[foot].y + lift);
        Vector2 local = Rotate(target - hip, -hipAngle);
        p.position[foot] = new Vector3(local.x, local.y, RestPositions[foot].z);
        p.angle[foot] = angle - hipAngle;
    }

    private static Pose Attack(float t)
    {
        // Rest -> 무게를 모아 뒤로 들기 -> 짧은 장전 -> 가속 내려찍기 -> 충격 유지 -> 회수.
        float[] times = { 0, 0.42f, 0.68f, 0.78f, AttackImpactTime, 1.06f, 1.25f, 1.60f, 1.9f };
        float[] body = { 0, 6, 11, 11, -22, -22, -15, -5, 0 };
        float[] height = { 0, -0.025f, -0.045f, -0.045f, -0.105f, -0.105f, -0.065f, -0.020f, 0 };
        float[] forward = { 0, -0.02f, -0.035f, -0.035f, 0.10f, 0.10f, 0.075f, 0.025f, 0 };
        float[] shoulder = { 0, 60, 135, 135, 60, 60, 45, 16, 0 };
        float[] hand = { 0, 25, 55, 55, -32, -32, -20, 6, 0 };
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
        // 무기 local 회전/손잡이 위치는 유지하여 손에서 미끄러지는 현상을 피합니다.
        GroundFoot(p, FF, RestPositions[FF].x, 0, 0);
        GroundFoot(p, FB, RestPositions[FB].x, 0, 0);
        return p;
    }

    private static Pose Hit(float t)
    {
        var p = new Pose();
        float kick = t < 0.06f ? Smooth(Mathf.Clamp01(t / 0.06f))
            : 1f - Smooth(Mathf.Clamp01((t - 0.06f) / 0.24f));
        p.position[Body].x -= 0.035f * kick;
        p.position[Body].y -= 0.015f * kick;
        p.angle[Body] = 6f * kick;
        p.angle[Head] = 5f * kick;
        p.angle[SF] = -12f * kick;
        p.angle[HF] = 8f * kick;
        p.angle[SB] = -7f * kick;
        p.angle[HB] += 10f * kick;
        p.angle[Bottom] = -6f * kick;
        GroundFoot(p, FF, RestPositions[FF].x, 0, 0);
        GroundFoot(p, FB, RestPositions[FB].x, 0, 0);
        return p;
    }

    private static Pose Death(float t)
    {
        var p = new Pose();
        float u = Mathf.Clamp01(t / 0.85f);
        float fall = Smooth(Mathf.Clamp01((u - 0.08f) / 0.76f));
        float settle = Smooth(Mathf.Clamp01((u - 0.84f) / 0.16f));
        p.position[Body].x -= 0.20f * fall;
        p.angle[Body] = 94f * fall - 4f * settle;
        p.angle[Head] = 8f * fall;
        p.angle[SF] = -24f * fall;
        p.angle[HF] = 15f * fall;
        p.angle[SB] = -20f * fall;
        p.angle[HB] = RestAngles[HB] - 25f * fall;
        p.angle[Bottom] = 0f;
        p.angle[FF] = -10f * fall;
        p.angle[FB] = 8f * fall;
        return p; // Generation grounds the sampled sprite geometry; hold the final pose.
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
        var geometry = ReadPrefabGeometry();
        var clip = new AnimationClip { name = name, frameRate = 12f };
        // 주요 타이밍도 포함하여 임팩트가 샘플 사이로 밀리지 않게 합니다.
        var timeSet = new SortedSet<float> { 0, duration };
        int count = Mathf.RoundToInt(duration * SampleRate);
        for (int i = 1; i < count; i++) timeSet.Add(duration * i / count);
        if (name == "TankMob5_Attack")
            foreach (float t in new[] { 0.42f, 0.68f, 0.78f, AttackImpactTime, 1.06f, 1.25f, 1.60f }) timeSet.Add(t);
        var times = new List<float>(timeSet);
        var poses = new Pose[times.Count];
        float restFloor = LowestVertex(new Pose(), geometry);
        for (int i = 0; i < times.Count; i++)
        {
            poses[i] = evaluate(loop && i == times.Count - 1 ? 0 : times[i]);
            if (name.EndsWith("_Death", StringComparison.Ordinal))
                poses[i].position[Body].y += restFloor - LowestVertex(poses[i], geometry);
        }
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

    private static Vector2[][] ReadPrefabGeometry()
    {
        var found = new List<string>();
        if (AssetDatabase.IsValidFolder(PrefabFolder))
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == MobName) found.Add(path);
            }
        if (found.Count != 1)
            throw new InvalidOperationException(PrefabFolder + "/" + MobName + ".prefab이 정확히 하나 있어야 합니다.");
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(found[0]);
        Transform visual = null;
        foreach (Transform t in asset.GetComponentsInChildren<Transform>(true))
            if (t.name == MobName && t.Find("Body") != null)
            {
                if (visual != null) throw new InvalidOperationException("시각 루트 중복: " + MobName);
                visual = t;
            }
        if (visual == null) throw new InvalidOperationException("Body를 가진 시각 루트가 없습니다: " + MobName);
        var geometry = new Vector2[Paths.Length][];
        for (int i = 0; i < Paths.Length; i++)
        {
            Transform t = visual.Find(Paths[i]);
            if (t == null) throw new InvalidOperationException("파츠 경로가 없습니다: " + Paths[i]);
            if ((t.localScale - Vector3.one).sqrMagnitude > 0.0001f)
                throw new InvalidOperationException("제공된 프리팹과 Scale이 다릅니다: " + Paths[i]);
            SpriteRenderer renderer = t.GetComponent<SpriteRenderer>();
            if (renderer != null && (renderer.flipX || renderer.flipY))
                throw new InvalidOperationException("파츠 Flip은 해제하고 루트에서 방향을 반전하세요: " + Paths[i]);
            if (renderer != null && renderer.sprite != null) geometry[i] = renderer.sprite.vertices;
        }
        if (geometry[Body] == null || geometry[FF] == null || geometry[FB] == null)
            throw new InvalidOperationException("Body / Foot 스프라이트 참조를 확인하세요: " + MobName);
        return geometry;
    }

    private static float LowestVertex(Pose pose, Vector2[][] geometry)
    {
        int[] parents = { -1, 0, 0, 2, 0, 4, 5, 0, 7, 7 };
        var matrices = new Matrix4x4[Paths.Length];
        float lowest = float.PositiveInfinity;
        for (int i = 0; i < Paths.Length; i++)
        {
            Matrix4x4 local = Matrix4x4.TRS(pose.position[i], Quaternion.Euler(0, 0, pose.angle[i]), Vector3.one);
            matrices[i] = parents[i] < 0 ? local : matrices[parents[i]] * local;
            // Weapon tips may touch below the body; ground using the character's geometry.
            if (i == 6 || geometry[i] == null) continue;
            foreach (Vector2 point in geometry[i])
                lowest = Mathf.Min(lowest, matrices[i].MultiplyPoint3x4(new Vector3(point.x, point.y, 0)).y);
        }
        return lowest;
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
