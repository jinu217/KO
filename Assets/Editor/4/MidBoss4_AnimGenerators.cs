#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// Assets/Editor/MidBoss4_AnimGenerators.cs
// Animator는 시각 루트 MidBoss 4에 둡니다. 물리 루트 이동/방향 전환은 AI가 담당합니다.
public static class MidBoss4_AnimGenerators
{
    public const string OutputFolder = "Assets/Animations/4/MidBoss 4";
    public const float AttackHit1 = 0.60f, AttackHit2 = 1.30f, AttackHit3 = 2.25f;
    public const float ThrowTime = 1.7f, CatchTime = 4.1f;
    private const int SampleRate = 60;
    private const int Body = 0, Head = 1, SF = 2, AF = 3, HF = 4, SB = 5, AB = 6, HB = 7,
        Weapon = 8, Bottom = 9, TF = 10, CF = 11, FF = 12, TB = 13, CB = 14, FB = 15;
    private static readonly int[] Parent = { -1, 0, 0, 2, 3, 0, 5, 6, 7, 0, 9, 10, 11, 9, 13, 14 };
    private static readonly string[] Paths ={
        "Body","Body/Head","Body/Shoulder_F","Body/Shoulder_F/Forearm_F","Body/Shoulder_F/Forearm_F/Hand_F",
        "Body/Shoulder_B","Body/Shoulder_B/Forearm_B","Body/Shoulder_B/Forearm_B/Hand_B",
        "Body/Shoulder_B/Forearm_B/Hand_B/Scythe","Body/Bottom",
        "Body/Bottom/Thigh_F","Body/Bottom/Thigh_F/Calf_F","Body/Bottom/Thigh_F/Calf_F/Foot_F",
        "Body/Bottom/Thigh_B","Body/Bottom/Thigh_B/Calf_B","Body/Bottom/Thigh_B/Calf_B/Foot_B"
    };
    private static readonly Vector3[] RestPositions ={
        new Vector3(0,0,0),new Vector3(-.081f,1.143f,0),
        new Vector3(-.687f,1.025f,0),new Vector3(-.391f,-.854f,0),new Vector3(.116f,-.558f,0),
        new Vector3(.375f,1.150f,0),new Vector3(.297f,-.933f,0),new Vector3(.131f,-.543f,0),
        new Vector3(.170f,-.144f,0),new Vector3(.173f,-.022f,0),
        new Vector3(-.603f,-.322f,0),new Vector3(-.010f,-.710f,0),new Vector3(-.083f,-.589f,0),
        new Vector3(.101f,-.322f,0),new Vector3(-.010f,-.710f,0),new Vector3(-.083f,-.589f,0)
    };
    private static readonly float[] RestAngles = { 0, 0, 0, 0, 0, 0, 0, 0, -50, 0, 0, 0, 0, 0, 0, 0 };
    private sealed class Pose
    {
        public Vector3[] position = (Vector3[])RestPositions.Clone();
        public float[] angle = (float[])RestAngles.Clone();
        public float frontLift, backLift, frontStep, backStep;
    }
    private static readonly string[] Suffixes = { "Idle", "Walk", "Run", "Attack", "Death", "Skill1", "Skill2", "Buff" };
    private static readonly float[] Lengths = { 2.4f, 1.1f, .72f, 3.1f, 1.9f, 5.1f, 4.5f, 3f };
    private static Pose Evaluate(int index, float t)
    {
        switch (index)
        {
            case 0: return Idle(t);
            case 1: return Locomotion(t, Lengths[1], false);
            case 2: return Locomotion(t, Lengths[2], true);
            case 3: return Attack(t);
            case 4: return Death(t);
            case 5: return Skill1(t);
            case 6: return Skill2(t);
            default: return Buff(t);
        }
    }
    private static AnimationClip Build(int index)
    { return Generate("MidBoss4_" + Suffixes[index], Lengths[index], index < 3, t => Evaluate(index, t)); }

    [MenuItem("Tools/4/MidBoss 4/Generate Animations")]
    public static void GenerateAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EnsureFolder(OutputFolder);
        for (int i = 0; i < Suffixes.Length; i++) Build(i);
        SetupAnimator();
        Debug.Log("MidBoss 4: 10종 클립 생성 완료. Stage4_MidBoss 스크립트와 수정된 데이터 에셋을 함께 적용하세요.");
    }
    [MenuItem("Tools/4/MidBoss 4/Clips/Idle")] public static void GenerateIdle() => GenerateSingle(0);
    [MenuItem("Tools/4/MidBoss 4/Clips/Walk")] public static void GenerateWalk() => GenerateSingle(1);
    [MenuItem("Tools/4/MidBoss 4/Clips/Run")] public static void GenerateRun() => GenerateSingle(2);
    [MenuItem("Tools/4/MidBoss 4/Clips/Attack")] public static void GenerateAttack() => GenerateSingle(3);
    [MenuItem("Tools/4/MidBoss 4/Clips/Death")] public static void GenerateDeath() => GenerateSingle(4);
    [MenuItem("Tools/4/MidBoss 4/Clips/Skill1")] public static void GenerateSkill1() => GenerateSingle(5);
    [MenuItem("Tools/4/MidBoss 4/Clips/Skill2")] public static void GenerateSkill2() => GenerateSingle(6);
    [MenuItem("Tools/4/MidBoss 4/Clips/Buff")] public static void GenerateBuff() => GenerateSingle(7);
    private static void GenerateSingle(int index)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        EnsureFolder(OutputFolder); AnimationClip clip = Build(index); SetupAnimator();
        Selection.activeObject = clip; EditorGUIUtility.PingObject(clip);
    }

    private const string ControllerName = "MidBoss4_AC";
    private const string TransitionPrefix = "MidBoss4_Generated_";

    [MenuItem("Tools/4/MidBoss 4/Setup Animator")]
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
                throw new InvalidOperationException("MidBoss4_AC가 여러 개 있습니다. 사용할 컨트롤러만 이 이름으로 남겨주세요.");
            controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
        }
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPath(OutputFolder + "/" + ControllerName + ".controller");
        EnsureParameter(controller, "Phase", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "MoveSpeed", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "IsChasing", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Attack", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "IsDead", AnimatorControllerParameterType.Bool);
        foreach (string trigger in new[] { "Skill1", "Skill2", "Buff" })
            EnsureParameter(controller, trigger, AnimatorControllerParameterType.Trigger);
        string[] names = Array.ConvertAll(Suffixes, n => "MidBoss4_" + n);
        var clips = new AnimationClip[names.Length];
        for (int i = 0; i < names.Length; i++)
        {
            clips[i] = AssetDatabase.LoadAssetAtPath<AnimationClip>(OutputFolder + "/" + names[i] + ".anim");
            if (clips[i] == null) clips[i] = Build(i);
        }
        var machine = controller.layers[0].stateMachine;
        // Remove the Hit state generated by the previous version.
        foreach (var child in machine.states)
        {
            if (child.state.name != "MidBoss4_Hit") continue;
            foreach (var transition in machine.anyStateTransitions)
                if (transition.destinationState == child.state) machine.RemoveAnyStateTransition(transition);
            foreach (var source in machine.states)
                foreach (var transition in source.state.transitions)
                    if (transition.destinationState == child.state) source.state.RemoveTransition(transition);
            machine.RemoveState(child.state);
        }

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
        var run2 = Generate("MidBoss4_Run2", .66f, true, t => PhaseRun(t, 2));
        var run3 = Generate("MidBoss4_Run3", .60f, true, t => PhaseRun(t, 3));
        var tree = states[2].motion as BlendTree;
        // Reuse the generated subasset on subsequent setup runs.
        foreach (var item in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(controller)))
            if (item is BlendTree existingTree && existingTree.name == "MidBoss4_PhaseRun") tree = existingTree;
        if (tree == null) { tree = new BlendTree { name = "MidBoss4_PhaseRun" }; AssetDatabase.AddObjectToAsset(tree, controller); }
        tree.blendType = BlendTreeType.Simple1D; tree.blendParameter = "Phase"; tree.useAutomaticThresholds = false;
        tree.children = new[]{new ChildMotion{motion=clips[2],threshold=1,timeScale=1},
            new ChildMotion{motion=run2,threshold=2,timeScale=1},new ChildMotion{motion=run3,threshold=3,timeScale=1}};
        states[2].motion = tree; EditorUtility.SetDirty(tree);
        foreach (var transition in machine.anyStateTransitions)
            if (transition.name.StartsWith(TransitionPrefix, StringComparison.Ordinal)) machine.RemoveAnyStateTransition(transition);
        machine.defaultState = states[0];
        ApplyStateLayout(machine, states);
        var death = machine.AddAnyStateTransition(states[4]);
        death.name = TransitionPrefix + "Death";
        death.hasExitTime = false;
        death.duration = 0.04f;
        death.hasFixedDuration = true;
        death.canTransitionToSelf = false;
        death.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
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
        for (int skill = 5; skill < 8; skill++)
        {
            for (int from = 0; from < 3; from++)
            {
                var transition = AddTransition(states[from], states[skill], false);
                transition.duration = 0f;
                transition.AddCondition(AnimatorConditionMode.If, 0, Suffixes[skill]);
            }
            for (int to = 0; to < 3; to++)
                AddLocomotionConditions(AddTransition(states[skill], states[to], true), to);
        }
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssignControllerToPrefab(controller);
        ApplyPatternData();

        Selection.activeObject = controller;
        EditorGUIUtility.PingObject(controller);
        Debug.Log("MidBoss4_AC 설정 완료: MoveSpeed(float), IsChasing/IsDead(bool), Attack/Skill1/Skill2/Buff(trigger). Stage4_MidBoss가 상태 재생과 실제 판정을 담당합니다.");
    }

    // Locomotion, combat actions, and terminal state are grouped for readability.
    private static void ApplyStateLayout(AnimatorStateMachine machine, AnimatorState[] states)
    {
        Vector3[] positions = {
            new Vector3(320, 160, 0), // Idle: 중앙 위
            new Vector3(180, 400, 0), // Walk: 아래 왼쪽
            new Vector3(460, 400, 0), // Run: 아래 오른쪽
            new Vector3(620, 260, 0), // Attack: 오른쪽
            new Vector3(20, 80, 0),    // Death
            new Vector3(650, 420, 0), // Skill1
            new Vector3(650, 570, 0), // Skill2
            new Vector3(650, 720, 0)  // Buff
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

    private const string PrefabFolder = "Assets/Prefab/4";
    private const string MobName = "MidBoss 4";

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
                Debug.LogWarning("프리팹 자동 연결 생략: Body를 자식으로 가진 MidBoss 4 시각 루트를 찾지 못했습니다. " + prefabPath);
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
                SetReference(serialized, "deathAnimation", AssetDatabase.LoadAssetAtPath<AnimationClip>(OutputFolder + "/MidBoss4_Death.anim"));
                SetString(serialized, "deathAnimationState", "MidBoss4_Death");
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
    [MenuItem("Tools/4/MidBoss 4/Apply Pattern Data")]
    public static void ApplyPatternData()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        const string path = "Assets/Data/Enemys/Stage4/Stage4_MidBoss.asset";
        var asset = AssetDatabase.LoadMainAssetAtPath(path);
        if (asset == null) { Debug.LogWarning("MidBoss 4 데이터 동기화 생략: " + path); return; }
        var obj = new SerializedObject(asset);
        var basic = obj.FindProperty("basicAttack");
        var skills = obj.FindProperty("skills");
        if (basic == null || skills == null || !skills.isArray || skills.arraySize < 3)
        { Debug.LogError("MidBoss 4 데이터에는 기본공격과 스킬 3개가 필요합니다."); return; }
        var steps = basic.FindPropertyRelative("active.attackSteps");
        var first = skills.GetArrayElementAtIndex(0);
        var second = skills.GetArrayElementAtIndex(1);
        var buff = skills.GetArrayElementAtIndex(2);
        var throwing = first.FindPropertyRelative("active.attackSteps");
        var pulling = second.FindPropertyRelative("active.attackSteps");
        if (steps == null || throwing == null || pulling == null || steps.arraySize < 3 || throwing.arraySize < 1 || pulling.arraySize < 2)
        { Debug.LogError("MidBoss 4 공격 단계 구조를 확인하세요. 수정된 Stage4_MidBoss.asset을 먼저 적용하세요."); return; }
        Undo.RecordObject(asset, "Sync MidBoss 4 pattern timing");
        PatternString(basic, "skillName", "기본 공격: 내려찍기 2회 + 전방 밀어베기");
        PatternInt(basic, "skillType", 0); PatternFloat(basic, "preDelay", 0); PatternFloat(basic, "postDelay", 0);
        steps.arraySize = 3;
        float[] delays = { AttackHit1, AttackHit2 - AttackHit1, AttackHit3 - AttackHit2 };
        for (int i = 0; i < 3; i++)
        {
            var step = steps.GetArrayElementAtIndex(i);
            PatternInt(step, "actionType", 2); PatternInt(step, "attackCount", 1);
            PatternFloat(step, "preDelay", delays[i]);
            PatternFloat(step, "postDelay", i == 2 ? 3.1f - AttackHit3 : 0);
            PatternFloat(step, "duration", 0); PatternFloat(step, "moveSpeed", 0); PatternFloat(step, "maxMoveDistance", 0);
        }
        PatternString(first, "skillName", "낫 투척: 전진 회전 유지 회수");
        PatternInt(first, "skillType", 0); PatternFloat(first, "preDelay", 0); PatternFloat(first, "postDelay", 1);
        throwing.arraySize = 1; // remove obsolete Jump / Slam steps
        var flight = throwing.GetArrayElementAtIndex(0);
        PatternString(flight, "stepName", "낫 왕복 회전 판정 (Stage4_MidBoss 전용)");
        PatternInt(flight, "actionType", 8); PatternInt(flight, "attackCount", 6);
        PatternFloat(flight, "preDelay", ThrowTime); PatternFloat(flight, "postDelay", 0);
        PatternFloat(flight, "duration", CatchTime - ThrowTime); PatternFloat(flight, "interval", .4f);
        PatternVector(flight, "hitboxSize", new Vector2(1.2f, 1.8f));
        PatternVector(flight, "hitboxOffset", Vector2.zero);
        PatternVector(flight, "knockbackForce", new Vector2(1.5f, .3f));
        PatternBool(flight, "knockbackAwayFromCenter", false); PatternBool(flight, "persistentFixedCadence", true);
        PatternString(second, "skillName", "낫으로 끌어오기 후 전방 회전");
        PatternInt(second, "skillType", 0); PatternFloat(second, "preDelay", 0); PatternFloat(second, "postDelay", 0);
        pulling.arraySize = 2;
        var pull = pulling.GetArrayElementAtIndex(0); var spin = pulling.GetArrayElementAtIndex(1);
        PatternInt(pull, "actionType", 9);
        PatternFloat(pull, "preDelay", .55f); PatternFloat(pull, "duration", .35f); PatternFloat(pull, "postDelay", .45f);
        PatternInt(spin, "actionType", 8); PatternInt(spin, "attackCount", 4);
        PatternFloat(spin, "preDelay", 0); PatternFloat(spin, "duration", 2.4f); PatternFloat(spin, "postDelay", .75f);
        PatternFloat(spin, "interval", .6f); PatternBool(spin, "persistentFixedCadence", true);
        // Preserve the merged Hunger buff effects, HP, cooldowns and all damage values.
        PatternFloat(buff, "preDelay", 1.7f); PatternFloat(buff, "postDelay", 1.3f);
        obj.ApplyModifiedProperties(); EditorUtility.SetDirty(asset); AssetDatabase.SaveAssets();
    }
    private static void PatternFloat(SerializedProperty root, string field, float value)
    { var p = root.FindPropertyRelative(field); if (p != null && p.propertyType == SerializedPropertyType.Float) p.floatValue = value; }
    private static void PatternInt(SerializedProperty root, string field, int value)
    { var p = root.FindPropertyRelative(field); if (p != null && (p.propertyType == SerializedPropertyType.Integer || p.propertyType == SerializedPropertyType.Enum)) p.intValue = value; }
    private static void PatternString(SerializedProperty root, string field, string value)
    { var p = root.FindPropertyRelative(field); if (p != null && p.propertyType == SerializedPropertyType.String) p.stringValue = value; }
    private static void PatternBool(SerializedProperty root, string field, bool value)
    { var p = root.FindPropertyRelative(field); if (p != null && p.propertyType == SerializedPropertyType.Boolean) p.boolValue = value; }
    private static void PatternVector(SerializedProperty root, string field, Vector2 value)
    { var p = root.FindPropertyRelative(field); if (p != null && p.propertyType == SerializedPropertyType.Vector2) p.vector2Value = value; }

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

    // +X is forward for this boss. Weapon root-space -90 points forward,
    // exactly as supplied by the author. Hands keep their original local positions.
    private static Pose P(float x, float y, float body, float sb, float ab, float hb,
        float sf, float af, float weaponAngle, float lift = 0)
    {
        var p = new Pose();
        p.position[Body] = new Vector3(x, y, 0); p.angle[Body] = body;
        p.angle[Head] = -body * .55f; p.angle[Bottom] = -body;
        p.angle[SB] = sb; p.angle[AB] = ab; p.angle[HB] = hb;
        p.angle[SF] = sf; p.angle[AF] = af; p.angle[HF] = -af * .2f;
        p.angle[Weapon] = weaponAngle - body - sb - ab - hb;
        p.frontLift = lift; return p;
    }
    private static Pose Mix(Pose a, Pose b, float u)
    {
        var p = new Pose(); u = Smooth(Mathf.Clamp01(u));
        for (int i = 0; i < Paths.Length; i++)
        { p.position[i] = Vector3.LerpUnclamped(a.position[i], b.position[i], u); p.angle[i] = Mathf.LerpUnclamped(a.angle[i], b.angle[i], u); }
        p.frontLift = Mathf.Lerp(a.frontLift, b.frontLift, u); p.backLift = Mathf.Lerp(a.backLift, b.backLift, u);
        p.frontStep = Mathf.Lerp(a.frontStep, b.frontStep, u); p.backStep = Mathf.Lerp(a.backStep, b.backStep, u);
        return p;
    }
    private static Pose Sequence(float t, float[] times, params Pose[] poses)
    {
        for (int i = 1; i < times.Length; i++) if (t <= times[i])
                return Mix(poses[i - 1], poses[i], Mathf.InverseLerp(times[i - 1], times[i], t));
        return Mix(poses[poses.Length - 1], poses[poses.Length - 1], 0);
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
    private static Pose Idle(float t)
    {
        // Restrained mechanical idle: a slow, small adjustment, not a frozen pose.
        // Counter the chest translation at the pelvis so feet and knees stay still.
        var p = new Pose();
        float phase = t / 2.4f * Mathf.PI * 2f;
        float rise = .5f - .5f * Mathf.Cos(phase);
        float lag = .5f - .5f * Mathf.Cos(phase - .45f);
        p.position[Body].y += .009f * rise;
        p.position[Bottom].y -= .009f * rise;
        p.angle[Head] = -.3f * rise;
        p.angle[SF] = .65f * rise;
        p.angle[AF] = -.25f * lag;
        p.angle[SB] = .35f * lag;
        p.angle[AB] = -.2f * rise;
        return p;
    }
    private static Pose Locomotion(float t, float duration, bool run)
    {
        float phase = t / duration, w = Mathf.Sin(phase * 2 * Mathf.PI);
        var p = P(0, -.045f - (run ? .06f : .025f) * (1 - Mathf.Cos(phase * 4 * Mathf.PI)) * .5f,
            run ? -12 : -3, 10 - 5 * w, 15 + 3 * w, 0, 8 + (run ? 18 : 9) * w, 10, -50);
        float stride = run ? .34f : .20f;
        Step(p, true, phase, stride, run ? .19f : .10f); Step(p, false, phase + .5f, stride, run ? .19f : .10f);
        return Ground(p);
    }
    [MenuItem("Tools/4/MidBoss 4/Clips/Run2")] public static void GenerateRun2() => GenerateSingle(2);
    [MenuItem("Tools/4/MidBoss 4/Clips/Run3")] public static void GenerateRun3() => GenerateSingle(2);
    private static Pose PhaseRun(float t, int phase)
    {
        float duration = phase == 2 ? .66f : .60f;
        var p = Locomotion(t, duration, true);
        p.angle[Body] = phase == 2 ? -20 : -29;
        // Pelvis leans forward too, instead of fully cancelling the chest lean.
        p.angle[Bottom] = phase == 2 ? 11 : 14;
        Step(p, true, t / duration, phase == 2 ? .40f : .46f, phase == 2 ? .23f : .28f);
        Step(p, false, t / duration + .5f, phase == 2 ? .40f : .46f, phase == 2 ? .23f : .28f);
        return Ground(p);
    }
    private static void Step(Pose p, bool front, float phase, float stride, float lift)
    {
        float u = Mathf.Repeat(phase, 1), x, h = 0;
        if (u < .5f) x = Mathf.Lerp(stride, -stride, u * 2);
        else { float v = (u - .5f) * 2; x = Mathf.Lerp(-stride, stride, Smooth(v)); h = lift * Mathf.Pow(Mathf.Sin(v * Mathf.PI), 2); }
        if (front) { p.frontStep = x; p.frontLift = h; } else { p.backStep = x; p.backLift = h; }
    }
    private static Pose Attack(float t)
    {
        var ready = P(-.035f, -.025f, 4, 112, 20, 0, 35, 10, 5);
        ready.backLift = .34f; ready.backStep = .12f;
        var impact = P(.23f, -.46f, -35, 104, 7, -8, 30, 8, -115);
        impact.angle[Bottom] = 17; impact.backStep = .40f; impact.frontStep = -.35f;
        var retract = P(-.02f, -.10f, 2, 40, 55, -5, 25, 8, -110);
        var extend = P(.12f, -.14f, -10, 80, 6, 0, 20, 8, -82);
        var p = Sequence(t, new[] { 0f, .38f, .48f, AttackHit1, .74f, 1.06f, 1.18f, AttackHit2, 1.48f, 1.88f, 2.07f, AttackHit3, 2.52f, 3.1f },
            new Pose(), ready, ready, impact, impact, ready, ready, impact, impact, retract, retract, extend, extend, new Pose());
        PlantLeg(p, false, p.backStep, p.backLift);
        // Front leg stays nearly straight, reaching behind the descending pelvis.
        float load = Mathf.Clamp01(-p.position[Body].y / .46f);
        p.angle[TF] = -WorldAngle(p, Bottom) - 36 * load;
        p.angle[CF] = -3 * load; p.angle[FF] = -WorldAngle(p, CF);
        return KeepAboveFloor(p, false);
    }
    private static Pose ThrowCharge()
    {
        var p = P(-.025f, 0, 4, 142, 38, 0, 30, 12, 30);
        p.angle[Weapon] = RestAngles[Weapon]; return p;
    }
    private static Pose ThrowHold()
    {
        var p = P(.12f, -.20f, -12, 82, 8, 0, 22, 12, -90);
        p.backStep = .32f; p.frontStep = -.16f; return p;
    }
    private static Pose Skill1(float t)
    {
        var charge = ThrowCharge(); var hold = ThrowHold();
        var catchPose = P(.01f, -.08f, -2, 58, 28, -5, 26, 18, -75);
        var p = Sequence(t, new[] { 0f, .55f, 1.48f, ThrowTime, CatchTime, 4.27f, 4.46f, 5.1f },
            new Pose(), charge, charge, hold, hold, catchPose, catchPose, new Pose());
        if (t > .55f && t < 1.48f)
        {
            float envelope = Mathf.Sin((t - .55f) / .93f * Mathf.PI);
            p.position[Body].x += .026f * envelope * Mathf.Sin(t * 72);
            p.angle[Head] += 2.4f * envelope * Mathf.Sin(t * 68);
            p.angle[SF] += 3f * envelope * Mathf.Sin(t * 64);
        }
        if (t <= 1.48f)
        {
            p.angle[Weapon] = RestAngles[Weapon];
            p.position[Bottom] = Quaternion.Euler(0, 0, -p.angle[Body]) * (RestPositions[Bottom] - p.position[Body]);
        }
        else
        {
            var straight = Mix(p, p, 0);
            straight.position[Bottom] = Quaternion.Euler(0, 0, -p.angle[Body]) * (RestPositions[Bottom] - p.position[Body]);
            Ground(p);
            p = Mix(straight, p, Mathf.Clamp01((t - 1.48f) / (ThrowTime - 1.48f)));
        }
        if (t >= ThrowTime && t <= CatchTime)
        {
            Ground(hold);
            Vector2 start = WorldPosition(hold, Weapon);
            float elapsed = t - ThrowTime;
            float distance = elapsed < .7f ? 4f * Smooth(elapsed / .7f) :
                elapsed < 1.5f ? 4f : 4f * (1 - Smooth((elapsed - 1.5f) / .9f));
            float u = elapsed / (CatchTime - ThrowTime);
            Vector2 target = start + new Vector2(distance, .10f * Mathf.Sin(u * Mathf.PI));
            Vector2 local = Rotate(target - WorldPosition(p, HB), -WorldAngle(p, HB));
            p.position[Weapon] = new Vector3(local.x, local.y, 0);
            p.angle[Weapon] = -90 - 1800 * u - WorldAngle(p, HB);
        }
        return p;
    }
    private static Pose Skill2(float t)
    {
        var reach = P(.07f, -.12f, -9, 85, 6, 0, 22, 10, -80);
        var pull = P(-.12f, -.05f, 6, 8, 92, -22, 30, 15, -165);
        var spin = P(.01f, -.09f, -4, 60, 20, 0, 25, 12, -90);
        var p = Sequence(t, new[] { 0f, .30f, .55f, .76f, 1.12f, 1.35f, 3.75f, 3.95f, 4.5f },
            new Pose(), reach, reach, pull, pull, spin, spin, spin, new Pose());
        if (t >= 1.35f && t <= 3.75f)
            p.angle[Weapon] -= 1440 * (t - 1.35f) / 2.4f;
        p.position[Bottom] = Quaternion.Euler(0, 0, -p.angle[Body]) * (RestPositions[Bottom] - p.position[Body]);
        return p;
    }
    private static Pose Buff(float t)
    {
        var raised = P(0, -.03f, 0, 128, 18, 0, 20, 15, 0);
        // Authored upper-body pose: weapon LOCAL rotation is -85 degrees.
        var point = P(.01f, -.04f, -3, 90, 0, -90, 0, 15, -88);
        point.angle[Weapon] = -85;
        point.angle[HF] = -3;
        var p = Sequence(t, new[] { 0f, 1.10f, 1.38f, 1.70f, 2.18f, 3f },
            new Pose(), raised, raised, point, point, new Pose());
        if (t <= 1.1f) p.angle[Weapon] -= 720 * Smooth(Mathf.Clamp01(t / 1.1f));
        // Keep pelvis and straight legs at their rest-space positions while
        // the upper body translates/leans. No leg IK or knee flex in this buff.
        Vector3 delta = RestPositions[Bottom] - p.position[Body];
        p.position[Bottom] = Quaternion.Euler(0, 0, -p.angle[Body]) * delta;
        p.angle[Bottom] = -p.angle[Body];
        return p;
    }
    // Sprite rectangle bounds use the supplied dimensions, pivots and PPU 64.
    // Conservative bounds keep even transparent sprite corners above the floor.
    private static float SpriteBottom(Pose p, int part)
    {
        Vector2 size = part == Body ? new Vector2(2, 2) : Vector2.one;
        Vector2 pivot = part == Body ? new Vector2(.57f, .30f) : new Vector2(.43f, .66f);
        float minimum = float.PositiveInfinity;
        for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++)
            {
                Vector2 local = Vector2.Scale(new Vector2(x, y) - pivot, size);
                minimum = Mathf.Min(minimum, (WorldPosition(p, part) + Rotate(local, WorldAngle(p, part))).y);
            }
        return minimum;
    }
    private static Pose KeepAboveFloor(Pose p, bool settle)
    {
        var rest = new Pose(); float floor = Mathf.Min(SpriteBottom(rest, FF), SpriteBottom(rest, FB));
        float lowest = Mathf.Min(SpriteBottom(p, Body), Mathf.Min(SpriteBottom(p, FF), SpriteBottom(p, FB)));
        p.position[Body].y += settle ? floor - lowest : Mathf.Max(0, floor - lowest);
        return p;
    }
    private static Pose Death(float t)
    {
        float kneel = Smooth(Mathf.Clamp01(t / .6f)), fall = Smooth(Mathf.Clamp01((t - .65f) / .7f));
        float straighten = Smooth(Mathf.Clamp01((t - .95f) / .55f));
        var p = P(.65f * fall, -.60f * kneel - 1.1f * fall, -88 * fall, 12 * fall, 65 * fall, -10 * fall, 20 * fall, 45 * fall, -50 - 25 * fall);
        p.angle[Bottom] = 12 * fall;
        p.angle[TF] = Mathf.Lerp(65 * kneel, -12, straighten);
        p.angle[CF] = -105 * kneel * (1 - straighten); p.angle[FF] = 40 * kneel * (1 - straighten);
        p.angle[TB] = Mathf.Lerp(60 * kneel, -12, straighten);
        p.angle[CB] = -110 * kneel * (1 - straighten); p.angle[FB] = 50 * kneel * (1 - straighten);
        return KeepAboveFloor(p, true);
    }

    private static AnimationClip Generate(string name, float duration, bool loop, Func<float, Pose> evaluate)
    {
        var clip = new AnimationClip { name = name, frameRate = SampleRate };
        int count = Mathf.CeilToInt(duration * SampleRate);
        var poses = new Pose[count + 1]; var times = new float[count + 1];
        for (int i = 0; i <= count; i++)
        { times[i] = duration * i / count; poses[i] = evaluate(loop && i == count ? 0 : times[i]); }
        for (int part = 0; part < Paths.Length; part++)
        {
            for (int axis = 0; axis < 3; axis++)
            {
                var keys = new Keyframe[count + 1];
                for (int i = 0; i <= count; i++) keys[i] = new Keyframe(times[i], poses[i].position[part][axis]);
                SetCurve(clip, Paths[part], "m_LocalPosition." + "xyz"[axis], keys);
                if (part != Weapon)
                {
                    for (int i = 0; i <= count; i++) keys[i] = new Keyframe(times[i], axis == 2 ? poses[i].angle[part] : 0);
                    SetCurve(clip, Paths[part], "localEulerAnglesRaw." + "xyz"[axis], keys);
                }
                // The scythe sprite's authored default is mirrored locally.
                // Preserve this in every clip, including Idle and Death.
                float restScale = part == Weapon && axis == 0 ? -1f : 1f;
                SetCurve(clip, Paths[part], "m_LocalScale." + "xyz"[axis],
                    new[] { new Keyframe(0, restScale), new Keyframe(duration, restScale) });
            }
        }
        // Quaternion sampling keeps continuous revolutions, then joins the canonical rest
        // without unwinding several turns during the next Animator transition.
        var rotations = new Quaternion[count + 1];
        for (int i = 0; i <= count; i++)
        {
            Quaternion q = Quaternion.Euler(0, 0, poses[i].angle[Weapon]);
            if (i > 0 && Quaternion.Dot(rotations[i - 1], q) < 0) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
            rotations[i] = q;
        }
        for (int axis = 0; axis < 4; axis++)
        {
            var keys = new Keyframe[count + 1];
            for (int i = 0; i <= count; i++) keys[i] = new Keyframe(times[i], rotations[i][axis]);
            SetCurve(clip, Paths[Weapon], "m_LocalRotation." + "xyzw"[axis], keys);
        }
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop; settings.loopBlend = false; AnimationUtility.SetAnimationClipSettings(clip, settings);
        string path = OutputFolder + "/" + name + ".anim";
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (existing != null)
        {
            var events = AnimationUtility.GetAnimationEvents(existing);
            Undo.RecordObject(existing, "Regenerate " + name); EditorUtility.CopySerialized(clip, existing);
            AnimationUtility.SetAnimationEvents(existing, events); EditorUtility.SetDirty(existing);
            UnityEngine.Object.DestroyImmediate(clip); return existing;
        }
        AssetDatabase.CreateAsset(clip, path); return clip;
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
