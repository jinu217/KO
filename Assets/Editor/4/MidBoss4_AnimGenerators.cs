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
        public float weaponScale = 1;
        public float weaponWidth = 1;
        public float bodyYaw;
        public bool weaponInFront = false;
    }
    private static readonly string[] Suffixes = { "Idle", "Walk", "Run", "Attack", "Death", "Skill1", "Skill2", "Buff" };
    private static readonly float[] Lengths = { 2.4f, 1.1f, .72f, 3.1f, 1.9f, 3.3f, 3.6f, 3f };
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
        // Generate future phase clips without connecting them to the Animator.
        Generate("MidBoss4_Run2", .66f, true, t => PhaseRun(t, 2));
        Generate("MidBoss4_Run3", .60f, true, t => PhaseRun(t, 3));
        states[2].motion = clips[2];
        // Remove only our obsolete generated blend tree.
        foreach (var item in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(controller)))
            if (item is BlendTree oldTree && oldTree.name == "MidBoss4_PhaseRun")
                UnityEngine.Object.DestroyImmediate(oldTree, true);
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
        foreach (var animator in Resources.FindObjectsOfTypeAll<Animator>())
        {
            if (!animator.gameObject.scene.IsValid() || animator.runtimeAnimatorController != controller) continue;
            var weapon = animator.transform.Find(Paths[Weapon]);
            if (weapon != null && weapon.GetComponent<MidBoss4WeaponLayerPreview>() == null)
                Undo.AddComponent<MidBoss4WeaponLayerPreview>(weapon.gameObject);
        }
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
            var weapon = visual.Find(Paths[Weapon]);
            if (weapon != null && weapon.GetComponent<MidBoss4WeaponLayerPreview>() == null)
                weapon.gameObject.AddComponent<MidBoss4WeaponLayerPreview>();
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
        PatternString(first, "skillName", "낫 회전 도약 내려찍기");
        PatternInt(first, "skillType", 0); PatternFloat(first, "preDelay", 0); PatternFloat(first, "postDelay", .8f);
        throwing.arraySize = 3;
        var sweep = throwing.GetArrayElementAtIndex(0);
        PatternInt(sweep, "actionType", 2); PatternInt(sweep, "attackCount", 1);
        PatternFloat(sweep, "preDelay", .85f); PatternFloat(sweep, "postDelay", .55f); PatternFloat(sweep, "duration", 0);
        PatternFloat(sweep, "damage", 14); PatternVector(sweep, "hitboxSize", new Vector2(5.5f, 3));
        PatternVector(sweep, "hitboxOffset", Vector2.zero); PatternVector(sweep, "knockbackForce", new Vector2(5, 2));
        PatternBool(sweep, "knockbackAwayFromCenter", true);
        var jump = throwing.GetArrayElementAtIndex(1);
        PatternInt(jump, "actionType", 5); PatternFloat(jump, "preDelay", 0); PatternFloat(jump, "postDelay", 0);
        PatternFloat(jump, "duration", .6f); PatternFloat(jump, "damage", 0); PatternVector(jump, "jumpForce", new Vector2(4, 10));
        var slam = throwing.GetArrayElementAtIndex(2);
        PatternInt(slam, "actionType", 6); PatternInt(slam, "attackCount", 1); PatternFloat(slam, "preDelay", 0); PatternFloat(slam, "postDelay", 0);
        PatternFloat(slam, "duration", .5f); PatternFloat(slam, "damage", 17);
        PatternVector(slam, "hitboxSize", new Vector2(3.5f, 2)); PatternVector(slam, "hitboxOffset", new Vector2(1, 0));
        PatternVector(slam, "knockbackForce", new Vector2(3, 2));
        var third = steps.GetArrayElementAtIndex(2);
        PatternFloat(third, "moveSpeed", 2.4f); PatternFloat(third, "maxMoveDistance", .45f);
        PatternString(second, "skillName", "낫으로 끌어오기 후 전방 회전");
        PatternInt(second, "skillType", 0); PatternFloat(second, "preDelay", 0); PatternFloat(second, "postDelay", 0);
        pulling.arraySize = 2;
        var pull = pulling.GetArrayElementAtIndex(0); var spin = pulling.GetArrayElementAtIndex(1);
        PatternInt(pull, "actionType", 9);
        PatternFloat(pull, "preDelay", .55f); PatternFloat(pull, "duration", .35f); PatternFloat(pull, "postDelay", .45f);
        PatternInt(spin, "actionType", 8); PatternInt(spin, "attackCount", 5);
        PatternFloat(spin, "preDelay", 0); PatternFloat(spin, "duration", 1.5f); PatternFloat(spin, "postDelay", .75f);
        PatternFloat(spin, "interval", .3f); PatternBool(spin, "persistentFixedCadence", true);
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

    // Keep the scythe at its authored local rest angle except explicit spin sections.
    // Attack gestures are carried by the arm and torso; hand positions stay attached.
    private static Pose P(float x, float y, float body, float sb, float ab, float hb,
        float sf, float af, float weaponAngle, float lift = 0)
    {
        var p = new Pose();
        p.position[Body] = new Vector3(x, y, 0); p.angle[Body] = body;
        p.angle[Head] = -body * .55f; p.angle[Bottom] = -body;
        p.angle[SB] = sb; p.angle[AB] = ab; p.angle[HB] = hb;
        p.angle[SF] = sf; p.angle[AF] = af; p.angle[HF] = -af * .2f;
        p.angle[Weapon] = RestAngles[Weapon];
        p.frontLift = lift; return p;
    }
    private static Pose Mix(Pose a, Pose b, float u)
    {
        var p = new Pose(); u = Smooth(Mathf.Clamp01(u));
        for (int i = 0; i < Paths.Length; i++)
        { p.position[i] = Vector3.LerpUnclamped(a.position[i], b.position[i], u); p.angle[i] = Mathf.LerpUnclamped(a.angle[i], b.angle[i], u); }
        p.frontLift = Mathf.Lerp(a.frontLift, b.frontLift, u); p.backLift = Mathf.Lerp(a.backLift, b.backLift, u);
        p.frontStep = Mathf.Lerp(a.frontStep, b.frontStep, u); p.backStep = Mathf.Lerp(a.backStep, b.backStep, u);
        p.bodyYaw = Mathf.Lerp(a.bodyYaw, b.bodyYaw, u);
        p.weaponWidth = Mathf.Lerp(a.weaponWidth, b.weaponWidth, u);
        p.weaponScale = Mathf.Lerp(a.weaponScale, b.weaponScale, u); p.weaponInFront = u < .5f ? a.weaponInFront : b.weaponInFront;
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
    private static void FollowLiftWithPelvis(Pose p)
    {
        // Small sagittal pelvic tilt follows the lifted thigh's travel direction.
        float front = Mathf.Clamp01(p.frontLift / .30f) * Mathf.Clamp(p.frontStep / .18f, -1, 1);
        float back = Mathf.Clamp01(p.backLift / .30f) * Mathf.Clamp(p.backStep / .18f, -1, 1);
        p.angle[Bottom] += 7f * Mathf.Clamp(front + back, -1, 1);
    }
    private static Pose Ground(Pose p)
    {
        FollowLiftWithPelvis(p);
        PlantLeg(p, true, p.frontStep, p.frontLift); PlantLeg(p, false, p.backStep, p.backLift);
        return p;
    }
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
        // A broad angry wind-up: free arm counter-swings behind the torso.
        var ready = P(-.06f, -.03f, 10, 122, 18, 0, -48, 12, -50);
        ready.backLift = .23f; ready.backStep = .27f;
        var impact = P(.25f, -.48f, -38, 45, 0, -45, -66, 16, -50);
        impact.angle[Bottom] = 20; impact.backStep = .42f; impact.frontStep = -.35f;
        // Recover through a lower knee lift before the second larger wind-up.
        var reload = P(-.035f, -.10f, 3, 92, 22, -8, -38, 15, -50);
        reload.backLift = .12f; reload.backStep = .17f;
        // Draw the elbow and scythe inward, then load the low forward push.
        var retract = P(-.07f, -.39f, -8, 12, 78, -122, -35, 18, -50);
        retract.angle[Bottom] = 8; retract.backStep = .25f;
        var loaded = P(-.12f, -.48f, -12, 5, 90, -123, -43, 20, -50);
        loaded.angle[Bottom] = 10; loaded.backStep = .28f;
        var extend = P(.20f, -.62f, -24, 78, 8, -102, -52, 18, -50);
        extend.angle[Bottom] = 12; extend.backStep = .48f;
        // -24 + 78 + 8 - 102 - 50 = -90: scythe faces forward via the wrist.
        var p = Sequence(t, new[] { 0f, .36f, .48f, AttackHit1, .76f, .93f, 1.08f, 1.18f, AttackHit2, 1.49f, 1.78f, 1.98f, 2.07f, AttackHit3, 2.52f, 3.1f },
            new Pose(), ready, ready, impact, impact, reload, ready, ready, impact, impact, retract, loaded, loaded, extend, extend, new Pose());
        FollowLiftWithPelvis(p);
        PlantLeg(p, false, p.backStep, p.backLift);
        // Let the airborne rear foot trail the shin; flatten it gradually on planting.
        float airborne = Smooth(Mathf.Clamp01(p.backLift / .23f));
        p.angle[FB] += Mathf.Clamp(WorldAngle(p, CB) * .35f, -16, 12) * airborne;
        float load = Mathf.Clamp01(-p.position[Body].y / .62f);
        p.angle[TF] = -WorldAngle(p, Bottom) - 42 * load;
        p.angle[CF] = -4 * load; p.angle[FF] = -WorldAngle(p, CF);
        KeepAboveFloor(p, false);
        SeatRearFootWithCalf(p);
        return p;
    }
    private static void SeatRearFootWithCalf(Pose p)
    {
        // Work after body clearance: front-leg clearance can lift the rear sole.
        // Keep intentional lift height, and extend the rear shin only in +rotation.
        float target = SpriteBottom(new Pose(), FB) + Mathf.Max(0, p.backLift);
        float gap = SpriteBottom(p, FB) - target;
        if (gap <= .0001f) return;
        float calf = p.angle[CB], foot = p.angle[FB], best = 0, bestGap = gap;
        for (int n = 1; n <= 180; n++)
        {
            float delta = n * .25f;
            p.angle[CB] = calf + delta; p.angle[FB] = foot - delta;
            float error = SpriteBottom(p, FB) - target;
            if (error < 0)
            {
                // First contact, refined without allowing the sole below target.
                float lo = delta - .25f, hi = delta;
                for (int j = 0; j < 12; j++)
                {
                    float mid = (lo + hi) * .5f; p.angle[CB] = calf + mid; p.angle[FB] = foot - mid;
                    if (SpriteBottom(p, FB) >= target) lo = mid; else hi = mid;
                }
                best = lo; break;
            }
            if (error < bestGap) { bestGap = error; best = delta; }
        }
        p.angle[CB] = calf + best; p.angle[FB] = foot - best;
    }
    private static Pose Skill1(float t)
    {
        var wind = Ground(P(-.04f, 0, 5, 12.08f, 81.29f, -30, 15, 25, -50));
        wind.angle[Weapon] = -90 - WorldAngle(wind, HB);
        var spin = Ground(P(.03f, 0, -6, 12.08f, 81.29f, -30, 15, 25, -50));
        spin.angle[Weapon] = -90 - WorldAngle(spin, HB);
        var crouch = Ground(P(-.03f, -.32f, 8, 128, 35, 0, 32, 18, 10));
        var air = P(.08f, -.10f, -8, 130, 20, 0, 20, 15, 10);
        air.backLift = .42f; air.frontLift = .12f; Ground(air);
        var slam = Attack(AttackHit1);
        // Solve each key pose before blending. At frame 165 (2.75 seconds),
        // recovery now begins from this same solved slam rather than switching IK.
        var p = Sequence(t, new[] { 0f, .45f, .60f, 1.05f, 1.20f, 1.4f, 2f, 2.5f, 2.75f, 3.3f },
            new Pose(), wind, wind, spin, spin, crouch, air, slam, slam, new Pose());
        if (t >= .60f && t <= 1.05f)
        {
            float u = Mathf.Clamp01((t - .60f) / .45f), turn = Smooth(u);
            float weight = Smooth(Mathf.Clamp01(u / .16f)) * Smooth(Mathf.Clamp01((1 - u) / .16f));
            // One full turn around the body's vertical axis, with the weapon
            // extended radially. All child parts turn together.
            p.bodyYaw = 360 * turn;
            p.angle[SB] = Mathf.Lerp(p.angle[SB], 12.08f, weight);
            p.angle[AB] = Mathf.Lerp(p.angle[AB], 81.29f, weight);
            p.angle[HB] = Mathf.Lerp(p.angle[HB], -30, weight);
            p.angle[Weapon] = -90 - WorldAngle(p, HB);
            // Weapon pivot sits midway up the torso; front hand supports the
            // inner shaft instead of stretching to the rear hand's grip.
            Vector2 grip = WorldPosition(p, Weapon) + new Vector2(-1.15f, 0);
            MatchFrontHand(p, grip, weight);
            float depth = Mathf.Sin(turn * Mathf.PI * 2);
            p.weaponScale = 1 + .22f * depth * weight;
            p.weaponWidth = Mathf.Lerp(1, .60f, weight);
            p.weaponInFront = depth >= 0;
        }
        return KeepAboveFloor(p, false);
    }
    private static void MatchFrontHand(Pose p, Vector2 target, float weight)
    {
        Vector2 a = RestPositions[AF], b = RestPositions[HF], d = target - WorldPosition(p, SF);
        float length = Mathf.Clamp(d.magnitude, Mathf.Abs(a.magnitude - b.magnitude) + .001f, a.magnitude + b.magnitude - .001f);
        float bend = Mathf.Acos(Mathf.Clamp((length * length - a.sqrMagnitude - b.sqrMagnitude) / (2 * a.magnitude * b.magnitude), -1, 1));
        float heading = Mathf.Atan2(d.y, d.x) - Mathf.Atan2(b.magnitude * Mathf.Sin(bend), a.magnitude + b.magnitude * Mathf.Cos(bend));
        float shoulder = heading * Mathf.Rad2Deg - Mathf.Atan2(a.y, a.x) * Mathf.Rad2Deg - p.angle[Body];
        float forearm = (heading + bend) * Mathf.Rad2Deg - Mathf.Atan2(b.y, b.x) * Mathf.Rad2Deg - heading * Mathf.Rad2Deg + Mathf.Atan2(a.y, a.x) * Mathf.Rad2Deg;
        p.angle[SF] = Mathf.LerpAngle(p.angle[SF], shoulder, weight);
        p.angle[AF] = Mathf.LerpAngle(p.angle[AF], forearm, weight);
        p.angle[HF] = Mathf.LerpAngle(p.angle[HF], -WorldAngle(p, AF), weight);
    }
    private static Pose StationaryUpperBody(Pose p)
    {
        // Pelvis and straight legs keep their rest-space placement throughout.
        p.position[Bottom] = Quaternion.Euler(0, 0, -p.angle[Body]) * (RestPositions[Bottom] - p.position[Body]);
        p.angle[Bottom] = -p.angle[Body];
        p.angle[TF] = p.angle[CF] = p.angle[FF] = p.angle[TB] = p.angle[CB] = p.angle[FB] = 0;
        return p;
    }
    private static Pose Skill2(float t)
    {
        // Extend the arm with the blade aligned to the supplied inward-facing grip.
        var raised = P(-.025f, -.015f, 3, 48, 45, -20, -18, 16, -50);
        raised.angle[Weapon] = -76; // +Y head vertical above the hand
        var reach = P(.10f, -.035f, -10, 40, 25, -20, -20, 12, -50);
        reach.angle[Weapon] = -125; // head forward before the pull
        // Retract the elbow/forearm, bringing the forward scythe head inward.
        // Keep the head direction stable while the entire weapon travels back.
        var pull = P(-.06f, -.025f, 4, -15, 35, -30, -30, 18, -50);
        pull.angle[Weapon] = -236; // compensate +8 combined arm rotation to retain inward blade direction
        // Shoulder 30 + forearm 33 preserves the previous combined 45 + 18 angle.
        var spin = P(.02f, -.02f, -3, 30, 33, -30, -12, 16, -50);
        spin.angle[Weapon] = -120;
        var p = Sequence(t, new[] { 0f, .28f, .40f, .55f, .70f, 1.12f, 1.35f, 2.85f, 3.05f, 3.6f },
            new Pose(), raised, raised, reach, pull, pull, spin, spin, spin, new Pose());
        if (t >= 1.35f && t <= 2.85f)
            p.angle[Weapon] -= 3600 * (t - 1.35f) / 1.5f;
        p.weaponInFront = true;
        StationaryUpperBody(p);
        // Add weight through the pelvis/legs only; retain the approved upper body.
        float recover = 1 - Smooth(Mathf.Clamp01((t - 1.12f) / .23f));
        float step = Smooth(Mathf.Clamp01((t - .40f) / .30f)) * recover;
        float load = Smooth(Mathf.Clamp01((t - .55f) / .15f)) * recover;
        float lift = 0;
        if (t > .40f && t < .70f) lift = .055f * Mathf.Pow(Mathf.Sin((t - .40f) / .30f * Mathf.PI), 2);
        p.position[Bottom] += (Vector3)Rotate(new Vector2(.035f * load, -.065f * load), -p.angle[Body]);
        p.angle[Bottom] += 4f * step;
        PlantLeg(p, true, 0, 0);
        PlantLeg(p, false, .16f * step, lift);
        return p;
    }
    private static Pose Buff(float t)
    {
        var raised = P(0, -.03f, 0, 128, 18, 0, 20, 15, 0);
        // Preserve the pointing direction through the wrist, with scythe local rotation at rest.
        var point = P(.01f, -.04f, -3, 90, 0, -90, 0, 15, -88);
        point.angle[HB] -= 35;
        point.angle[Weapon] = RestAngles[Weapon];
        point.angle[HF] = -3;
        var p = Sequence(t, new[] { 0f, 1.10f, 1.38f, 1.70f, 2.18f, 3f },
            new Pose(), raised, raised, point, point, new Pose());
        if (t <= .825f) p.angle[Weapon] -= 1080 * Smooth(Mathf.Clamp01(t / .825f));
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
                if (part != Weapon && part != Body)
                {
                    for (int i = 0; i <= count; i++) keys[i] = new Keyframe(times[i], axis == 2 ? poses[i].angle[part] : 0);
                    SetCurve(clip, Paths[part], "localEulerAnglesRaw." + "xyz"[axis], keys);
                }
                // The scythe sprite's authored default is mirrored locally.
                // Preserve this in every clip, including Idle and Death.
                float restScale = part == Weapon && axis == 0 ? -1f : 1f;
                var scaleKeys = new Keyframe[count + 1];
                for (int i = 0; i <= count; i++) scaleKeys[i] = new Keyframe(times[i], restScale * (part == Weapon ? poses[i].weaponScale * (axis == 0 ? poses[i].weaponWidth : 1) : 1));
                SetCurve(clip, Paths[part], "m_LocalScale." + "xyz"[axis], scaleKeys);
            }
        }
        // Serialized renderer curve also works in the Animation window preview.
        // Reset to the authored Back layer outside the near half of the sweep.
        // Animate a small flag; large sorting-layer IDs lose precision in float curves.
        var layerKeys = new Keyframe[count + 1];
        for (int i = 0; i <= count; i++) layerKeys[i] = new Keyframe(times[i], poses[i].weaponInFront ? 1 : 0);
        var layerCurve = new AnimationCurve(layerKeys);
        for (int i = 0; i < layerCurve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(layerCurve, i, AnimationUtility.TangentMode.Constant);
            AnimationUtility.SetKeyRightTangentMode(layerCurve, i, AnimationUtility.TangentMode.Constant);
        }
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(Paths[Weapon], typeof(MidBoss4WeaponLayerPreview), "front"), layerCurve);
        // Quaternion sampling keeps continuous revolutions, then joins the canonical rest
        // without unwinding several turns during the next Animator transition.
        foreach (int part in new[] { Body, Weapon })
        {
            var rotations = new Quaternion[count + 1];
            for (int i = 0; i <= count; i++)
            {
                Quaternion q = Quaternion.Euler(0, part == Body ? poses[i].bodyYaw : 0, poses[i].angle[part]);
                if (i > 0 && Quaternion.Dot(rotations[i - 1], q) < 0) q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                rotations[i] = q;
            }
            for (int axis = 0; axis < 4; axis++)
            {
                var keys = new Keyframe[count + 1];
                for (int i = 0; i <= count; i++) keys[i] = new Keyframe(times[i], rotations[i][axis]);
                SetCurve(clip, Paths[part], "m_LocalRotation." + "xyzw"[axis], keys);
            }
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
