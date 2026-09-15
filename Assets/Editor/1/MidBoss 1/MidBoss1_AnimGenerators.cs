using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

// ================================================================
// MidBoss 1 애니메이션 등록기
// - SAVE_PATH 안의 AnimationClip을 MidBoss 1 AnimatorController에 동기화
// - Walk 클립의 ArmJoint_B 회전값 고정
// - Walk 기반 Run 클립 생성
// ================================================================
public class MidBoss1_AnimGenerators
{
    const string SAVE_PATH = "Assets/Animations/1/MidBoss 1";
    const string AC_NAME = "MidBoss 1_AC";

    const string MOB_PREFIX = "MidBoss 1_";
    const string WALK_NAME = "Walk";
    const string RUN_NAME = "Run";

    const string BODY_PATH = "Body";
    const string HEAD_PATH = "Body/Head";
    const string SHOULDER_B_PATH = "Body/ShoulderJoint_B/Shoulder_B";
    const string LOWER_BODY_PATH = "Body/LowerBody";
    const string ARM_JOINT_B_PATH =
        "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B";

    const float RUN_LOWER_BODY_SPEED = 1.25f;

    [MenuItem("Tools/MidBoss1/Generate AC")]
    public static void GenerateAC()
    {
        string acPath = $"{SAVE_PATH}/{AC_NAME}.controller";
        AnimatorController ac =
            AssetDatabase.LoadAssetAtPath<AnimatorController>(acPath);

        if (ac == null)
        {
            EnsureFolders();
            ac = AnimatorController.CreateAnimatorControllerAtPath(acPath);
            Debug.Log($"[MidBoss1] 새 AnimatorController 생성: {acPath}");
        }

        AnimatorStateMachine rootSM = ac.layers[0].stateMachine;
        var existingNames =
            new System.Collections.Generic.HashSet<string>();

        foreach (ChildAnimatorState state in rootSM.states)
            existingNames.Add(state.state.name);

        string[] guids = AssetDatabase.FindAssets(
            "t:AnimationClip",
            new[] { SAVE_PATH }
        );

        var clipNames = new System.Collections.Generic.List<string>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            clipNames.Add(
                System.IO.Path.GetFileNameWithoutExtension(path)
            );
        }

        clipNames.Sort();

        int added = 0;
        int updated = 0;

        foreach (string clipName in clipNames)
        {
            string clipPath = $"{SAVE_PATH}/{clipName}.anim";
            AnimationClip clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

            if (clip == null)
            {
                Debug.LogWarning($"[MidBoss1] 클립 없음: {clipPath}");
                continue;
            }

            AnimatorState matchedState = null;
            foreach (ChildAnimatorState child in rootSM.states)
            {
                if (child.state.name == clipName)
                {
                    matchedState = child.state;
                    break;
                }
            }

            if (matchedState == null)
            {
                matchedState = rootSM.AddState(clipName);
                added++;
            }
            else
            {
                updated++;
            }

            matchedState.motion = clip;
        }

        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log(
            $"[MidBoss1] AC 동기화 완료 - 클립 {clipNames.Count}개 " +
            $"(신규 {added}, 갱신 {updated}) -> {acPath}"
        );
    }

    [MenuItem("Tools/MidBoss1/Generate Walk and Run")]
    public static void GenerateWalkAndRun()
    {
        EnsureFolders();

        AnimationClip sourceWalk = LoadClip(WALK_NAME);
        if (sourceWalk == null)
        {
            Debug.LogError(
                $"[MidBoss1] {WALK_NAME}.anim을 찾을 수 없습니다: {SAVE_PATH}"
            );
            return;
        }

        AnimationClip normalWalk = CloneClip(sourceWalk);
        normalWalk.name = MOB_PREFIX + WALK_NAME;
        SetFixedRotation(
            normalWalk,
            ARM_JOINT_B_PATH,
            0f
        );
        SaveOrReplaceClip(
            normalWalk,
            FindOrCreateClipPath(WALK_NAME)
        );

        AnimationClip run = CloneClip(sourceWalk);
        run.name = MOB_PREFIX + RUN_NAME;
        // Run은 Walk의 전체 동작을 1.25배 빠르게 재생합니다.
        // 모든 부위의 커브 시간을 함께 줄여 동작 싱크를 유지합니다.
        ScaleAllCurvesTime(
            run,
            1f / RUN_LOWER_BODY_SPEED
        );

        // 추적 자세용 고정 회전
        SetFixedRotation(
            run,
            ARM_JOINT_B_PATH,
            60f
        );
        SetFixedRotation(
            run,
            BODY_PATH,
            -15f
        );
        SetFixedRotation(
            run,
            SHOULDER_B_PATH,
            15f
        );

        // Walk의 기존 회전 커브에 오프셋을 더합니다.
        AddRotationOffset(
            run,
            HEAD_PATH,
            15f
        );
        AddRotationOffset(
            run,
            LOWER_BODY_PATH,
            15f
        );
        SaveOrReplaceClip(
            run,
            FindOrCreateClipPath(RUN_NAME)
        );

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 생성·수정한 Walk/Run 클립을 MidBoss 1_AC에 자동 등록/갱신합니다.
        GenerateAC();

        Debug.Log(
            "[MidBoss1] Walk/Run 클립 생성 및 AC 등록 완료\n" +
            $"- {WALK_NAME}.anim : ArmJoint_B rot.z = 0\n" +
            $"- {RUN_NAME}.anim : Walk 커브 1.25배속, " +
            "ArmJoint_B rot.z = 60, Body rot.z = -15, " +
            "Shoulder_B rot.z = 15, Head/LowerBody 기존 rot 커브 +15"
        );
    }

    // Rest -> 준비 -> 가속 -> 빠른 베기 -> 반동 -> Rest 복귀.
    // 왼쪽(-X)을 공격하는 기준. 루트 이동 없이 로컬 자세만 움직입니다.
    const string HORIZONTAL_ATTACK_NAME = "HorizontalAttack";
    const float HORIZONTAL_SWING_START = 0.24f;
    const float HORIZONTAL_IMPACT = 0.32f;
    const float HORIZONTAL_SWING_END = 0.38f;
    const float HORIZONTAL_DURATION = 0.80f;

    [MenuItem("Tools/MidBoss1/Generate Horizontal Attack")]
    public static void GenerateHorizontalAttack()
    {
        EnsureFolders();
        // 기존 커브/이벤트를 복사하지 않고 새 클립으로 교체합니다.
        AnimationClip clip = new AnimationClip
        {
            name = MOB_PREFIX + HORIZONTAL_ATTACK_NAME,
            frameRate = 60f,
            wrapMode = WrapMode.Once
        };

        const string sb = "Body/ShoulderJoint_B";
        const string ab = sb + "/Shoulder_B/ArmJoint_B";
        const string hb = ab + "/Arm_B/Hand_B";
        const string weapon = hb + "/Weapon";
        const string sf = "Body/ShoulderJoint_F";
        const string af = sf + "/Shoulder_F/ArmJoint_F";
        const string lb = "Body/LowerBody";
        const string tb = lb + "/ThighJoint_B";
        const string tf = lb + "/ThighJoint_F";

        // 추출값은 각 부모 기준 localPosition / localEulerAngles로 적용합니다.
        // 루트의 월드 위치 (-10, 0, 0)는 클립에 기록하지 않습니다.
        SetHorizontalPose(clip, "Body", 0f, 0f, 0f);
        SetHorizontalPose(clip, "Body/Head", 0.1f, 0.45f, 0f);
        SetHorizontalPose(clip, sb, 0.5f, 0.35f, 0f);
        SetHorizontalPose(clip, sb + "/Shoulder_B", 0f, 0f, 0f);
        SetHorizontalPose(clip, ab, 0.2f, -0.75f, 0f);
        SetHorizontalPose(clip, ab + "/Arm_B", 0f, 0f, 0f);
        SetHorizontalPose(clip, hb, 0.25f, -0.4f, 0f);
        SetHorizontalPose(clip, weapon, 0.2f, -0.1f, -60f);
        SetHorizontalPose(clip, sf, -0.6f, 0.35f, 0f);
        SetHorizontalPose(clip, sf + "/Shoulder_F", 0f, 0f, 0f);
        SetHorizontalPose(clip, af, -0.39f, -0.63f, 0f);
        SetHorizontalPose(clip, af + "/Arm_F", 0f, 0f, 0f);
        SetHorizontalPose(clip, af + "/Arm_F/Hand_F", 0f, -0.45f, 0f);
        SetHorizontalPose(clip, lb, 0.05f, -0.6f, 0f);
        SetHorizontalPose(clip, tb, 0.3f, -0.1f, 0f);
        SetHorizontalPose(clip, tb + "/Thigh_B", 0f, 0f, 0f);
        SetHorizontalPose(clip, tb + "/Thigh_B/KneeJoint_B", 0.15f, -0.8f, 0f);
        SetHorizontalPose(clip, tb + "/Thigh_B/KneeJoint_B/Foot_B", 0f, 0f, 0f);
        SetHorizontalPose(clip, tf, -0.35f, -0.1f, 0f);
        SetHorizontalPose(clip, tf + "/Thigh_F", 0f, 0f, 0f);
        SetHorizontalPose(clip, tf + "/Thigh_F/KneeJoint_F", 0.05f, -0.8f, 0f);
        SetHorizontalPose(clip, tf + "/Thigh_F/KneeJoint_F/Foot_F", 0f, 0f, 0f);

        // 몸통이 먼저 힘을 싣고 무거운 팔/무기는 늦게 가속합니다.
        // 마지막 짧은 구간에 베기를 집중하고, 반동 후 복귀는 늦게 따라옵니다.
        // 0.24초/0.38초의 뒷팔 로컬 자세는 제공된 두 추출값과 같습니다.
        SetHorizontalMotion(clip, sb, "localEulerAnglesRaw.z",
            new float[] { 0f, 0.16f, HORIZONTAL_SWING_START, 0.30f, HORIZONTAL_SWING_END, 0.44f, 0.58f, HORIZONTAL_DURATION },
            0f, -40f, -30f, -18f, 30f, 40f, 25f, 0f);
        SetHorizontalMotion(clip, ab, "localEulerAnglesRaw.z",
            new float[] { 0f, 0.18f, HORIZONTAL_SWING_START, 0.32f, HORIZONTAL_SWING_END, 0.46f, 0.62f, HORIZONTAL_DURATION },
            0f, -55f, -45f, -38f, 0f, 10f, 5f, 0f);
        SetHorizontalMotion(clip, hb, "localEulerAnglesRaw.z",
            new float[] { 0f, 0.20f, HORIZONTAL_SWING_START, 0.32f, HORIZONTAL_SWING_END, 0.48f, 0.64f, HORIZONTAL_DURATION },
            0f, 54f, 45f, 40f, -45f, -58f, -34f, 0f);
        SetHorizontalMotion(clip, weapon, "m_LocalPosition.x", HorizontalTimes,
            0.2f, -0.32f, -0.32f, -0.27f, 0.2f, 0.23f, 0.21f, 0.2f);
        SetHorizontalMotion(clip, weapon, "m_LocalPosition.y", HorizontalTimes,
            -0.1f, -0.4f, -0.4f, -0.37f, -0.1f, -0.07f, -0.09f, -0.1f);

        // 무게중심: 뒤로 모았다가 왼쪽으로 싣고 천천히 안정합니다.
        SetHorizontalMotion(clip, BODY_PATH, "m_LocalPosition.x", HorizontalTimes,
            0f, 0.045f, 0.035f, -0.025f, -0.10f, -0.085f, -0.04f, 0f);
        SetHorizontalMotion(clip, BODY_PATH, "m_LocalPosition.y", HorizontalTimes,
            0f, -0.045f, -0.06f, -0.05f, -0.04f, -0.07f, -0.03f, 0f);
        SetHorizontalMotion(clip, BODY_PATH, "localEulerAnglesRaw.z", HorizontalTimes,
            0f, -6f, -4f, 3f, 12f, 10f, 4f, 0f);
        // 머리는 상체 회전을 일부 상쇄하며 약간 늦게 따라옵니다.
        SetHorizontalMotion(clip, HEAD_PATH, "localEulerAnglesRaw.z", HorizontalTimes,
            0f, 4f, 3f, -1f, -8f, -7f, -3f, 0f);
        SetHorizontalMotion(clip, lb, "localEulerAnglesRaw.z", HorizontalTimes,
            0f, 4f, 3f, -2f, -8f, -7f, -3f, 0f);

        // 앞팔은 균형을 잡고 베기 직후에 늦게 반동을 받습니다.
        SetHorizontalMotion(clip, sf, "localEulerAnglesRaw.z", HorizontalTimes,
            0f, 12f, 10f, -3f, -19f, -24f, -12f, 0f);
        SetHorizontalMotion(clip, af, "localEulerAnglesRaw.z", HorizontalTimes,
            0f, -14f, -17f, -10f, 10f, 18f, 9f, 0f);
        SetHorizontalMotion(clip, af + "/Arm_F/Hand_F", "localEulerAnglesRaw.z", HorizontalTimes,
            0f, 4f, 6f, 4f, -6f, -10f, -3f, 0f);

        // 작은 무릎 굽힘과 좌우 다른 타이밍으로 체중 이동을 표현합니다.
        SetHorizontalMotion(clip, tb, "localEulerAnglesRaw.z", HorizontalTimes,
            0f, -5f, -6f, 1f, 6f, 5f, 2f, 0f);
        SetHorizontalMotion(clip, tb + "/Thigh_B/KneeJoint_B", "localEulerAnglesRaw.z", HorizontalTimes,
            0f, 9f, 11f, 5f, -2f, 4f, 2f, 0f);
        SetHorizontalMotion(clip, tf, "localEulerAnglesRaw.z", HorizontalTimes,
            0f, 4f, 5f, -2f, -7f, -6f, -3f, 0f);
        SetHorizontalMotion(clip, tf + "/Thigh_F/KneeJoint_F", "localEulerAnglesRaw.z", HorizontalTimes,
            0f, -5f, -6f, 3f, 11f, 10f, 5f, 0f);
        // Rest 추출에 스케일이 생략되어 기본값 (1, 1, 1)로 복원합니다.
        SetHorizontalMotion(clip, weapon, "m_LocalScale.x", HorizontalTimes,
            1f, 0.5f, 0.5f, 0.5f, 0.5f, 0.5f, 0.8f, 1f);
        SetHorizontalConstant(clip, weapon, "m_LocalScale.z", 1f);

        // 뒤로 빼는 준비 구간은 +1 → -1을 부드럽게 보간합니다.
        // 반속 적용 후 0.12~0.36초에 걸쳐 전환, 타격 시 -1 → +1은 즉시 반전.
        AnimationCurve flip = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.06f, 1f),
            new Keyframe(0.18f, -1f),
            new Keyframe(HORIZONTAL_IMPACT, 1f),
            new Keyframe(HORIZONTAL_DURATION, 1f)
        );
        // 0 접선의 Hermite 보간으로 준비 반전의 시작/끝을 완만하게 만듭니다.
        AnimationUtility.SetKeyRightTangentMode(flip, 2, AnimationUtility.TangentMode.Constant);
        AnimationUtility.SetKeyLeftTangentMode(flip, 3, AnimationUtility.TangentMode.Constant);
        SetHorizontalCurve(clip, weapon, "m_LocalScale.y", flip);

        // 새로 만든 0.8초 커브를 2배로 늘립니다. 재생성해도 누적되지 않습니다.
        StretchHorizontalTime(clip, 2f);
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        settings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        SaveOrReplaceClip(clip, FindOrCreateClipPath(HORIZONTAL_ATTACK_NAME));
        AssetDatabase.SaveAssets();
        GenerateAC();
        Debug.Log("[MidBoss1] HorizontalAttack 생성 및 AC 등록 완료 (1.6초, 임팩트 0.64초, 기존 대비 0.5배속)");
    }

    static void SetHorizontalPose(AnimationClip clip, string path, float x, float y, float zAngle)
    {
        SetHorizontalConstant(clip, path, "m_LocalPosition.x", x);
        SetHorizontalConstant(clip, path, "m_LocalPosition.y", y);
        SetHorizontalConstant(clip, path, "m_LocalPosition.z", 0f);
        SetHorizontalConstant(clip, path, "localEulerAnglesRaw.x", 0f);
        SetHorizontalConstant(clip, path, "localEulerAnglesRaw.y", 0f);
        SetHorizontalConstant(clip, path, "localEulerAnglesRaw.z", zAngle);
    }

    static void SetHorizontalConstant(AnimationClip clip, string path, string property, float value)
    {
        SetHorizontalCurve(clip, path, property,
            AnimationCurve.Constant(0f, HORIZONTAL_DURATION, value));
    }

    static readonly float[] HorizontalTimes =
    {
        0f, 0.16f, HORIZONTAL_SWING_START, HORIZONTAL_IMPACT,
        HORIZONTAL_SWING_END, 0.48f, 0.64f, HORIZONTAL_DURATION
    };

    static void SetHorizontalMotion(AnimationClip clip, string path, string property,
        float[] times, params float[] values)
    {
        if (times.Length != values.Length)
            throw new System.ArgumentException("가로공격 시간/값 개수가 다릅니다: " + path);

        Keyframe[] keys = new Keyframe[times.Length];
        for (int i = 0; i < keys.Length; i++)
            keys[i] = new Keyframe(times[i], values[i]);
        AnimationCurve curve = new AnimationCurve(keys);
        // 중간 키를 통과할 때 속도가 이어지고 극값에서는 부드럽게 감속합니다.
        // ClampedAuto로 의도하지 않은 각도/위치 오버슈트를 방지합니다.
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
        }
        // 클립 시작/끝은 정지 속도로 맞춰 준비와 마무리를 부드럽게 합니다.
        foreach (int i in new int[] { 0, curve.length - 1 })
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Free);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Free);
            Keyframe key = curve[i];
            key.inTangent = 0f;
            key.outTangent = 0f;
            curve.MoveKey(i, key);
        }
        SetHorizontalCurve(clip, path, property, curve);
    }

    static void SetHorizontalCurve(AnimationClip clip, string path, string property, AnimationCurve curve)
    {
        AnimationUtility.SetEditorCurve(clip,
            EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
    }

    // 가로공격 기본 키 시간은 유지하고 결과 커브의 시간/접선을 함께 조정합니다.
    static void StretchHorizontalTime(AnimationClip clip, float factor)
    {
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            var keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i].time *= factor;
                keys[i].inTangent /= factor;
                keys[i].outTangent /= factor;
            }
            curve.keys = keys;
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }
    }

    [MenuItem("Tools/MidBoss1/Generate Combo Attacks")]
    public static void GenerateComboAttacks()
    {
        EnsureFolders();
        AnimationClip vertical = LoadClip("세로공격");
        if (vertical == null) vertical = LoadClip("VerticalAttack");
        if (vertical == null || vertical.length < 0.65f)
        {
            Debug.LogError("[MidBoss1] 저장 폴더에 MidBoss 1_세로공격.anim이 필요합니다 (길이 0.65초 이상).");
            return;
        }
        // 반속 가로공격을 항상 최신 기준으로 생성한 뒤 연격에 사용합니다.
        GenerateHorizontalAttack();
        AnimationClip horizontal = LoadClip(HORIZONTAL_ATTACK_NAME);
        var temporary = new System.Collections.Generic.List<AnimationClip>();
        try
        {
            // Rest에서 첫 세로공격 준비 자세로 연결.
            AnimationClip rest = SliceComboClip(horizontal, 0f, 0.001f, 1f);
            temporary.Add(rest);
            foreach (var binding in AnimationUtility.GetCurveBindings(rest))
            {
                var curve = AnimationUtility.GetEditorCurve(rest, binding);
                AnimationUtility.SetEditorCurve(rest, binding, AnimationCurve.Constant(0f, 0.001f, curve.Evaluate(0f)));
            }
            AnimationUtility.SetAnimationEvents(rest, new AnimationEvent[0]);
            // 원본의 0.6초 타격 직후까지만 사용하여 팔을 다시 드는 복귀를 생략합니다.
            AnimationClip first = SliceComboClip(vertical, 0f, 0.65f, 1.2f);
            temporary.Add(first);
            AnimationClip second = SliceComboClip(vertical, 0.2f, 0.65f, 1.3f);
            temporary.Add(second);
            // 가로공격은 반속 기준 0.48초의 준비 자세부터 마지막 Rest까지.
            AnimationClip finish = SliceComboClip(horizontal, 0.48f, 1.6f, 1f);
            temporary.Add(finish);
            HoldComboStart(finish, 5f / 60f);
            // 세로 → 가로: 기존 추가 5프레임에서 총 4프레임 단축, 도착 후 총 5프레임 유지.
            const float horizontalPreparation = 0.22f + (5f - 4f) / 60f;
            AnimationClip combo2 = BuildCombo("Combo2", new[] { rest, first, finish }, 1f, 0.22f,
                new[] { 0.239f, horizontalPreparation });
            temporary.Add(combo2);
            AnimationClip combo3 = BuildCombo("Combo3", new[] { rest, first, second, finish }, 1f, 0.22f,
                new[] { 0.239f, 0.24f, horizontalPreparation });
            temporary.Add(combo3);
            SaveOrReplaceClip(combo2, $"{SAVE_PATH}/{MOB_PREFIX}Combo2.anim");
            SaveOrReplaceClip(combo3, $"{SAVE_PATH}/{MOB_PREFIX}Combo3.anim");
            AssetDatabase.SaveAssets();
            GenerateAC();
            Debug.Log("[MidBoss1] 2연격 약 2.22초 / 3연격 약 2.81초 생성 완료. 마지막 가로공격 0.5배속, Rest 복귀.");
        }
        finally
        {
            foreach (var clip in temporary)
                if (clip != null && !AssetDatabase.Contains(clip)) Object.DestroyImmediate(clip);
        }
    }

    // 가로공격 시작(장전) 자세를 유지한 뒤 원래 베기 동작을 이어갑니다.
    static void HoldComboStart(AnimationClip clip, float duration)
    {
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            if (curve == null || curve.length == 0) continue;
            float initial = curve.Evaluate(0f);
            var keys = curve.keys;
            for (int i = 0; i < keys.Length; i++) keys[i].time += duration;
            keys[0].inTangent = 0f;
            keys[0].weightedMode = (WeightedMode)((int)keys[0].weightedMode & ~(int)WeightedMode.In);
            curve.keys = keys;
            curve.AddKey(new Keyframe(0f, initial, 0f, 0f));
            AnimationUtility.SetEditorCurve(clip, binding, curve);
        }
        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            if (keys == null || keys.Length == 0) continue;
            var shifted = new System.Collections.Generic.List<ObjectReferenceKeyframe>();
            shifted.Add(new ObjectReferenceKeyframe { time = 0f, value = keys[0].value });
            foreach (var original in keys)
                shifted.Add(new ObjectReferenceKeyframe { time = original.time + duration, value = original.value });
            AnimationUtility.SetObjectReferenceCurve(clip, binding, shifted.ToArray());
        }
        var events = AnimationUtility.GetAnimationEvents(clip);
        foreach (var e in events) e.time += duration;
        AnimationUtility.SetAnimationEvents(clip, events);
    }

    // 원본 커브의 키/접선을 보존하면서 필요한 공격 구간만 잘라냅니다.
    static AnimationClip SliceComboClip(AnimationClip source, float from, float to, float speed)
    {
        AnimationClip result = new AnimationClip { frameRate = 60f };
        float duration = (to - from) / speed;
        foreach (var binding in AnimationUtility.GetCurveBindings(source))
        {
            var curve = AnimationUtility.GetEditorCurve(source, binding);
            if (curve == null || curve.length == 0) continue;
            var keys = new System.Collections.Generic.List<Keyframe>();
            float epsilon = 0.0001f;
            float startSlope = (curve.Evaluate(from + epsilon) - curve.Evaluate(from)) / epsilon * speed;
            float endSlope = (curve.Evaluate(to) - curve.Evaluate(to - epsilon)) / epsilon * speed;
            keys.Add(new Keyframe(0f, curve.Evaluate(from), 0f, startSlope));
            foreach (var original in curve.keys)
            {
                if (original.time <= from || original.time >= to) continue;
                Keyframe key = original;
                key.time = (original.time - from) / speed;
                key.inTangent *= speed;
                key.outTangent *= speed;
                keys.Add(key);
            }
            keys.Add(new Keyframe(duration, curve.Evaluate(to), endSlope, 0f));
            // 경계가 원본 키에 해당하면 내부 방향 접선을 그대로 가져옵니다.
            foreach (var original in curve.keys)
            {
                if (Mathf.Abs(original.time - from) < 0.00001f)
                {
                    Keyframe key = original; key.time = 0f;
                    key.inTangent = 0f; key.outTangent *= speed; keys[0] = key;
                }
                if (Mathf.Abs(original.time - to) < 0.00001f)
                {
                    Keyframe key = original; key.time = duration;
                    key.inTangent *= speed; key.outTangent = 0f; keys[keys.Count - 1] = key;
                }
            }
            AnimationUtility.SetEditorCurve(result, binding, new AnimationCurve(keys.ToArray()));
        }
        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(source))
        {
            var originals = AnimationUtility.GetObjectReferenceCurve(source, binding);
            if (originals == null || originals.Length == 0) continue;
            Object held = originals[0].value;
            foreach (var key in originals) if (key.time <= from) held = key.value;
            var keys = new System.Collections.Generic.List<ObjectReferenceKeyframe>();
            keys.Add(new ObjectReferenceKeyframe { time = 0f, value = held });
            foreach (var original in originals)
                if (original.time > from && original.time <= to)
                    keys.Add(new ObjectReferenceKeyframe { time = (original.time - from) / speed, value = original.value });
            AnimationUtility.SetObjectReferenceCurve(result, binding, keys.ToArray());
        }
        var events = new System.Collections.Generic.List<AnimationEvent>();
        foreach (var e in AnimationUtility.GetAnimationEvents(source))
        {
            if (e.time < from || e.time > to) continue;
            e.time = (e.time - from) / speed;
            events.Add(e);
        }
        AnimationUtility.SetAnimationEvents(result, events.ToArray());
        return result;
    }

    static AnimationClip BuildCombo(string action, AnimationClip[] sources, float speed, float gap, float[] connections = null)
    {
        AnimationClip result = new AnimationClip { name = MOB_PREFIX + action, frameRate = 60f };
        var floats = new System.Collections.Generic.HashSet<EditorCurveBinding>();
        var objects = new System.Collections.Generic.HashSet<EditorCurveBinding>();
        float[] starts = new float[sources.Length];
        for (int i = 0; i < sources.Length; i++)
        {
            if (i > 0) starts[i] = starts[i - 1] + sources[i - 1].length / speed + (connections == null ? gap : connections[i - 1]);
            foreach (var binding in AnimationUtility.GetCurveBindings(sources[i])) floats.Add(binding);
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(sources[i])) objects.Add(binding);
        }
        // 서로 다른 회전 표현을 중복 적용하면 회전이 꼬이므로 명확히 중단합니다.
        var rotationFamilies = new System.Collections.Generic.Dictionary<string, string>();
        foreach (var binding in floats)
        {
            if (binding.type != typeof(Transform)) continue;
            string prop = binding.propertyName;
            if (!prop.StartsWith("m_LocalRotation.") && !prop.StartsWith("localEulerAngles")) continue;
            string family = prop.Substring(0, prop.LastIndexOf('.'));
            if (rotationFamilies.TryGetValue(binding.path, out string previous) && previous != family)
                throw new System.ArgumentException("회전 커브 형식을 통일해주세요: " + binding.path + " (" + previous + ", " + family + ")");
            rotationFamilies[binding.path] = family;
        }
        foreach (var binding in floats)
        {
            var output = new AnimationCurve();
            float held = 0f;
            // 첫 클립에 없는 속성은 해당 속성이 처음 나오는 클립의 시작값으로 초기화합니다.
            foreach (var source in sources)
            {
                var curve = AnimationUtility.GetEditorCurve(source, binding);
                if (curve != null && curve.length > 0) { held = curve.Evaluate(0f); break; }
            }
            for (int i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                var curve = AnimationUtility.GetEditorCurve(source, binding);
                float start = starts[i];
                float end = start + source.length / speed;
                bool present = curve != null && curve.length > 0;
                float first = present ? curve.Evaluate(0f) : held;
                // 무기를 뒤로 빼는 +Y → -Y 반전은 연결 구간 전체에서 부드럽게 보간.
                bool smoothWeaponPreparation = binding.type == typeof(Transform) &&
                    binding.path == "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B/Arm_B/Hand_B/Weapon" &&
                    binding.propertyName == "m_LocalScale.y" && held > 0f && first < 0f;
                if (i > 0 && (binding.isDiscreteCurve ||
                    (binding.propertyName.StartsWith("m_LocalScale.") && held * first < 0f && !smoothWeaponPreparation)))
                {
                    int last = output.length - 1;
                    Keyframe step = output[last];
                    step.outTangent = float.PositiveInfinity;
                    output.MoveKey(last, step);
                }
                output.AddKey(new Keyframe(start, first));
                if (present)
                {
                    foreach (Keyframe original in curve.keys)
                    {
                        if (original.time <= 0f || original.time >= source.length) continue;
                        Keyframe key = original;
                        key.time = start + original.time / speed;
                        key.inTangent *= speed;
                        key.outTangent *= speed;
                        output.AddKey(key);
                    }
                    // 시작/끝 키의 내부 방향 접선과 가중치를 보존합니다.
                    if (Mathf.Approximately(curve[0].time, 0f))
                    {
                        int index = output.length - 1;
                        // 내부 키가 추가되어 있으므로 시작 키를 시간으로 찾습니다.
                        for (int k = 0; k < output.length; k++)
                            if (Mathf.Approximately(output[k].time, start)) { index = k; break; }
                        Keyframe key = curve[0];
                        key.time = start; key.inTangent = 0f; key.outTangent *= speed;
                        output.MoveKey(index, key);
                    }
                    held = curve.Evaluate(source.length);
                }
                Keyframe endKey = new Keyframe(end, held);
                if (present && Mathf.Approximately(curve[curve.length - 1].time, source.length))
                {
                    endKey = curve[curve.length - 1];
                    endKey.time = end; endKey.inTangent *= speed; endKey.outTangent = 0f;
                }
                output.AddKey(endKey);
            }
            AnimationUtility.SetEditorCurve(result, binding, output);
        }
        foreach (var binding in objects)
        {
            var keys = new System.Collections.Generic.List<ObjectReferenceKeyframe>();
            Object held = null;
            foreach (var source in sources)
            {
                var sourceKeys = AnimationUtility.GetObjectReferenceCurve(source, binding);
                if (sourceKeys != null && sourceKeys.Length > 0) { held = sourceKeys[0].value; break; }
            }
            for (int i = 0; i < sources.Length; i++)
            {
                var sourceKeys = AnimationUtility.GetObjectReferenceCurve(sources[i], binding);
                if (sourceKeys != null && sourceKeys.Length > 0 && sourceKeys[0].time <= 0f)
                    held = sourceKeys[0].value;
                keys.Add(new ObjectReferenceKeyframe { time = starts[i], value = held });
                if (sourceKeys == null) continue;
                foreach (var original in sourceKeys)
                {
                    if (original.time <= 0f) continue;
                    keys.Add(new ObjectReferenceKeyframe { time = starts[i] + original.time / speed, value = original.value });
                    held = original.value;
                }
            }
            AnimationUtility.SetObjectReferenceCurve(result, binding, keys.ToArray());
        }
        var events = new System.Collections.Generic.List<AnimationEvent>();
        for (int i = 0; i < sources.Length; i++)
            foreach (var original in AnimationUtility.GetAnimationEvents(sources[i]))
                events.Add(new AnimationEvent
                {
                    time = starts[i] + original.time / speed,
                    functionName = original.functionName,
                    floatParameter = original.floatParameter,
                    intParameter = original.intParameter,
                    stringParameter = original.stringParameter,
                    objectReferenceParameter = original.objectReferenceParameter,
                    messageOptions = original.messageOptions
                });
        AnimationUtility.SetAnimationEvents(result, events.ToArray());
        var settings = AnimationUtility.GetAnimationClipSettings(result);
        settings.loopTime = false; settings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(result, settings);
        return result;
    }

    // 버프/돌진은 사용자가 제공한 Rest에서 시작하고 끝납니다.
    static AnimationClip CreatePatternRest(string name, float duration)
    {
        AnimationClip clip = new AnimationClip { name = MOB_PREFIX + name, frameRate = 60f };
        const string sb = "Body/ShoulderJoint_B";
        const string ab = sb + "/Shoulder_B/ArmJoint_B";
        const string hb = ab + "/Arm_B/Hand_B";
        const string weapon = hb + "/Weapon";
        const string sf = "Body/ShoulderJoint_F";
        const string af = sf + "/Shoulder_F/ArmJoint_F";
        const string lb = "Body/LowerBody";
        const string tb = lb + "/ThighJoint_B";
        const string tf = lb + "/ThighJoint_F";

        // 추출값은 각 부모 기준 localPosition / localEulerAngles로 적용합니다.
        // 루트의 월드 위치 (-10, 0, 0)는 클립에 기록하지 않습니다.
        SetHorizontalPose(clip, "Body", 0f, 0f, 0f);
        SetHorizontalPose(clip, "Body/Head", 0.1f, 0.45f, 0f);
        SetHorizontalPose(clip, sb, 0.5f, 0.35f, 0f);
        SetHorizontalPose(clip, sb + "/Shoulder_B", 0f, 0f, 0f);
        SetHorizontalPose(clip, ab, 0.2f, -0.75f, 0f);
        SetHorizontalPose(clip, ab + "/Arm_B", 0f, 0f, 0f);
        SetHorizontalPose(clip, hb, 0.25f, -0.4f, 0f);
        SetHorizontalPose(clip, weapon, 0.2f, -0.1f, -60f);
        SetHorizontalPose(clip, sf, -0.6f, 0.35f, 0f);
        SetHorizontalPose(clip, sf + "/Shoulder_F", 0f, 0f, 0f);
        SetHorizontalPose(clip, af, -0.39f, -0.63f, 0f);
        SetHorizontalPose(clip, af + "/Arm_F", 0f, 0f, 0f);
        SetHorizontalPose(clip, af + "/Arm_F/Hand_F", 0f, -0.45f, 0f);
        SetHorizontalPose(clip, lb, 0.05f, -0.6f, 0f);
        SetHorizontalPose(clip, tb, 0.3f, -0.1f, 0f);
        SetHorizontalPose(clip, tb + "/Thigh_B", 0f, 0f, 0f);
        SetHorizontalPose(clip, tb + "/Thigh_B/KneeJoint_B", 0.15f, -0.8f, 0f);
        SetHorizontalPose(clip, tb + "/Thigh_B/KneeJoint_B/Foot_B", 0f, 0f, 0f);
        SetHorizontalPose(clip, tf, -0.35f, -0.1f, 0f);
        SetHorizontalPose(clip, tf + "/Thigh_F", 0f, 0f, 0f);
        SetHorizontalPose(clip, tf + "/Thigh_F/KneeJoint_F", 0.05f, -0.8f, 0f);
        SetHorizontalPose(clip, tf + "/Thigh_F/KneeJoint_F/Foot_F", 0f, 0f, 0f);


        SetHorizontalConstant(clip, weapon, "m_LocalScale.x", 1f);
        SetHorizontalConstant(clip, weapon, "m_LocalScale.y", 1f);
        SetHorizontalConstant(clip, weapon, "m_LocalScale.z", 1f);
        StretchHorizontalTime(clip, duration / HORIZONTAL_DURATION);
        return clip;
    }

    [MenuItem("Tools/MidBoss1/Generate Buff and Dash")]
    public static void GenerateBuffAndDash()
    {
        EnsureFolders();
        GenerateBuff();
        GenerateDash();
        AssetDatabase.SaveAssets();
        GenerateAC();
        Debug.Log("[MidBoss1] Buff 1.85초 / Dash 0.8초 생성 및 AC 등록 완료. 실제 돌진 이동은 이동 컨트롤러에서 처리합니다.");
    }

    static void SavePattern(AnimationClip clip, string action)
    {
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        settings.loopBlend = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        SaveOrReplaceClip(clip, $"{SAVE_PATH}/{MOB_PREFIX}{action}.anim");
    }

    static void GenerateBuff()
    {
        AnimationClip clip = CreatePatternRest("Buff", 1.85f);
        const string sb = "Body/ShoulderJoint_B";
        const string ab = sb + "/Shoulder_B/ArmJoint_B";
        const string hb = ab + "/Arm_B/Hand_B";
        const string sf = "Body/ShoulderJoint_F";
        const string af = sf + "/Shoulder_F/ArmJoint_F";
        const string hf = af + "/Arm_F/Hand_F";
        const string tb = "Body/LowerBody/ThighJoint_B";
        const string tf = "Body/LowerBody/ThighJoint_F";
        float[] t = { 0f, 0.18f, 0.36f, 0.44f, 0.62f, 0.72f, 0.90f, 1.00f, 1.18f, 1.28f, 1.56f, 1.85f };
        // 스트레칭 자세를 잡은 뒤 작게 당겼다 천천히 풀기를 세 번 반복.
        // 당김 0.08~0.10초 / 풀기 0.18초: 통… 통… 통… 리듬.
        SetHorizontalMotion(clip, sb, "localEulerAnglesRaw.z", t,
            0f, -10f, -105f, -111f, -105f, -110f, -105f, -109f, -105f, -102f, -35f, 0f);
        SetHorizontalMotion(clip, sf, "localEulerAnglesRaw.z", t,
            0f, 9f, 100f, 105f, 100f, 104.5f, 100f, 104f, 100f, 97f, 32f, 0f);
        SetHorizontalMotion(clip, ab, "localEulerAnglesRaw.z", t,
            0f, -12f, -48f, -54f, -48f, -53.5f, -48f, -53f, -48f, -46f, -18f, 0f);
        SetHorizontalMotion(clip, af, "localEulerAnglesRaw.z", t,
            0f, 12f, 50f, 56f, 50f, 55.5f, 50f, 55f, 50f, 48f, 20f, 0f);
        SetHorizontalMotion(clip, hb, "localEulerAnglesRaw.z", t,
            0f, 4f, 17f, 19.5f, 17f, 19f, 17f, 18.5f, 17f, 16f, 7f, 0f);
        SetHorizontalMotion(clip, hf, "localEulerAnglesRaw.z", t,
            0f, -4f, -17f, -19.5f, -17f, -19f, -17f, -18.5f, -17f, -16f, -7f, 0f);
        SetHorizontalMotion(clip, BODY_PATH, "m_LocalPosition.y", t,
            0f, -0.018f, 0f, -0.01f, 0f, -0.009f, 0f, -0.008f, 0f, 0f, -0.008f, 0f);
        SetHorizontalMotion(clip, BODY_PATH, "localEulerAnglesRaw.z", t,
            0f, 1f, -1f, -2f, -1f, -1.9f, -1f, -1.8f, -1f, -0.8f, 0.5f, 0f);
        SetHorizontalMotion(clip, HEAD_PATH, "localEulerAnglesRaw.z", t,
            0f, 2f, -4f, -5.5f, -4f, -5.3f, -4f, -5f, -4f, -3.5f, 1f, 0f);
        SetHorizontalMotion(clip, LOWER_BODY_PATH, "localEulerAnglesRaw.z", t,
            0f, -0.8f, 0.5f, 1f, 0.5f, 0.9f, 0.5f, 0.8f, 0.5f, 0.4f, -0.3f, 0f);
        SetHorizontalMotion(clip, tb, "localEulerAnglesRaw.z", t,
            0f, -2f, -1f, -1.8f, -1f, -1.7f, -1f, -1.6f, -1f, -1f, -1f, 0f);
        SetHorizontalMotion(clip, tf, "localEulerAnglesRaw.z", t,
            0f, 2f, 1f, 1.8f, 1f, 1.7f, 1f, 1.6f, 1f, 1f, 1f, 0f);
        SetHorizontalMotion(clip, tb + "/Thigh_B/KneeJoint_B", "localEulerAnglesRaw.z", t,
            0f, 3f, 1.5f, 2.7f, 1.5f, 2.5f, 1.5f, 2.3f, 1.5f, 1.5f, 1.5f, 0f);
        SetHorizontalMotion(clip, tf + "/Thigh_F/KneeJoint_F", "localEulerAnglesRaw.z", t,
            0f, -3f, -1.5f, -2.7f, -1.5f, -2.5f, -1.5f, -2.3f, -1.5f, -1.5f, -1.5f, 0f);
        SavePattern(clip, "Buff");
    }

    static void GenerateDash()
    {
        AnimationClip clip = CreatePatternRest("Dash", 0.8f);
        const string sb = "Body/ShoulderJoint_B";
        const string ab = sb + "/Shoulder_B/ArmJoint_B";
        const string hb = ab + "/Arm_B/Hand_B";
        const string sf = "Body/ShoulderJoint_F";
        const string af = sf + "/Shoulder_F/ArmJoint_F";
        const string tb = "Body/LowerBody/ThighJoint_B";
        const string tf = "Body/LowerBody/ThighJoint_F";
        float[] t = { 0f, 0.14f, 0.22f, 0.30f, 0.40f, 0.50f, 0.66f, 0.8f };
        // 화면상 피드백에 맞춰 전신 회전 방향 반전. 0.22~0.50초가 돌진 구간입니다.
        // 루트 이동을 기록하지 않고 시각적 무게중심만 조금 움직입니다.
        SetHorizontalMotion(clip, BODY_PATH, "m_LocalPosition.x", t,
            0f, 0.065f, 0.035f, -0.11f, -0.13f, -0.09f, -0.035f, 0f);
        SetHorizontalMotion(clip, BODY_PATH, "m_LocalPosition.y", t,
            0f, -0.10f, -0.12f, -0.07f, -0.06f, -0.10f, -0.045f, 0f);
        SetHorizontalMotion(clip, BODY_PATH, "localEulerAnglesRaw.z", t,
            0f, 7f, -5f, -24f, -26f, -19f, -7f, 0f);
        SetHorizontalMotion(clip, HEAD_PATH, "localEulerAnglesRaw.z", t,
            0f, -5f, 3f, 17f, 19f, 13f, 4f, 0f);
        SetHorizontalMotion(clip, LOWER_BODY_PATH, "localEulerAnglesRaw.z", t,
            0f, -5f, 3f, 16f, 17f, 12f, 4f, 0f);
        // 무기를 든 뒷팔(B)이 돌진을 이끌고 앞팔(F)은 뒤쪽에서 균형을 잡습니다.
        SetHorizontalMotion(clip, sb, "localEulerAnglesRaw.z", t,
            0f, 12f, 18f, 42f, 46f, 32f, 10f, 0f);
        SetHorizontalMotion(clip, ab, "localEulerAnglesRaw.z", t,
            0f, 15f, 22f, 35f, 38f, 25f, 8f, 0f);
        SetHorizontalMotion(clip, hb, "localEulerAnglesRaw.z", t,
            0f, -8f, -12f, -23f, -25f, -18f, -5f, 0f);
        SetHorizontalMotion(clip, sf, "localEulerAnglesRaw.z", t,
            0f, -12f, -28f, -48f, -50f, -40f, -15f, 0f);
        SetHorizontalMotion(clip, af, "localEulerAnglesRaw.z", t,
            0f, 18f, 32f, 55f, 58f, 42f, 15f, 0f);
        SetHorizontalMotion(clip, af + "/Arm_F/Hand_F", "localEulerAnglesRaw.z", t,
            0f, -5f, -8f, -15f, -16f, -12f, -4f, 0f);
        // 뒷다리로 밀고 앞다리로 버티는 짧은 한 걸음.
        SetHorizontalMotion(clip, tb, "localEulerAnglesRaw.z", t,
            0f, 8f, 12f, 24f, 18f, 5f, -2f, 0f);
        SetHorizontalMotion(clip, tf, "localEulerAnglesRaw.z", t,
            0f, -8f, -12f, -22f, -27f, -18f, -6f, 0f);
        SetHorizontalMotion(clip, tb + "/Thigh_B/KneeJoint_B", "localEulerAnglesRaw.z", t,
            0f, -15f, -19f, -8f, -6f, -10f, -4f, 0f);
        SetHorizontalMotion(clip, tf + "/Thigh_F/KneeJoint_F", "localEulerAnglesRaw.z", t,
            0f, 14f, 20f, 26f, 20f, 10f, 4f, 0f);
        SavePattern(clip, "Dash");
    }

    static string FindOrCreateClipPath(string actionName)
    {
        string[] guids = AssetDatabase.FindAssets(
            "t:AnimationClip",
            new[] { SAVE_PATH }
        );

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

            if (clip != null && IsNamedClip(clip.name, actionName))
                return path;
        }

        return $"{SAVE_PATH}/{MOB_PREFIX}{actionName}.anim";
    }

    static AnimationClip LoadClip(string clipName)
    {
        // 1순위: MidBoss 1_Walk, MidBoss 1|Walk처럼
        // 몹 이름이 앞에 붙은 클립
        string[] guids = AssetDatabase.FindAssets(
            "t:AnimationClip",
            new[] { SAVE_PATH }
        );

        AnimationClip suffixMatch = null;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

            if (clip == null)
                continue;

            if (clip.name == clipName)
                return clip;

            if (IsNamedClip(clip.name, clipName))
            {
                // 같은 폴더 안에서 찾은 첫 번째 접미사 일치 클립을 사용합니다.
                suffixMatch = clip;
            }
        }

        if (suffixMatch != null)
            return suffixMatch;

        // 접두사가 붙은 클립이 없을 때만 Walk.anim 같은
        // 이름이 정확한 클립을 사용합니다.
        string exactPath = $"{SAVE_PATH}/{clipName}.anim";
        return AssetDatabase.LoadAssetAtPath<AnimationClip>(exactPath);
    }

    static bool IsNamedClip(string clipName, string actionName)
    {
        if (string.IsNullOrEmpty(clipName) ||
            string.IsNullOrEmpty(actionName))
            return false;

        return clipName.EndsWith(
                   "_" + actionName,
                   System.StringComparison.OrdinalIgnoreCase
               ) ||
               clipName.EndsWith(
                   "|" + actionName,
                   System.StringComparison.OrdinalIgnoreCase
               ) ||
               clipName.EndsWith(
                   " " + actionName,
                   System.StringComparison.OrdinalIgnoreCase
               );
    }

    static AnimationClip CloneClip(AnimationClip source)
    {
        AnimationClip clone = new AnimationClip
        {
            name = source.name,
            frameRate = source.frameRate,
            wrapMode = source.wrapMode
        };

        EditorCurveBinding[] bindings =
            AnimationUtility.GetCurveBindings(source);

        foreach (EditorCurveBinding binding in bindings)
        {
            AnimationCurve curve =
                AnimationUtility.GetEditorCurve(source, binding);

            AnimationUtility.SetEditorCurve(
                clone,
                binding,
                new AnimationCurve(curve.keys)
            );
        }

        return clone;
    }

    static void SetFixedRotation(
        AnimationClip clip,
        string objectPath,
        float z
    )
    {
        EditorCurveBinding[] bindings =
            AnimationUtility.GetCurveBindings(clip);

        EditorCurveBinding? rotationBinding = null;

        foreach (EditorCurveBinding binding in bindings)
        {
            if (binding.path != objectPath)
                continue;

            if (IsZRotationProperty(binding.propertyName))
            {
                rotationBinding = binding;
                break;
            }
        }

        EditorCurveBinding targetBinding;
        if (rotationBinding.HasValue)
        {
            targetBinding = rotationBinding.Value;
        }
        else
        {
            targetBinding = EditorCurveBinding.FloatCurve(
                objectPath,
                typeof(Transform),
                "localEulerAnglesRaw.z"
            );
        }

        float length = Mathf.Max(0.001f, clip.length);
        AnimationCurve fixedCurve = new AnimationCurve(
            new Keyframe(0f, z),
            new Keyframe(length, z)
        );

        AnimationUtility.SetEditorCurve(
            clip,
            targetBinding,
            fixedCurve
        );

        // 같은 오브젝트의 다른 Z 회전 표현이 남아 있으면 제거합니다.
        foreach (EditorCurveBinding binding in bindings)
        {
            if (binding.path == objectPath &&
                IsZRotationProperty(binding.propertyName) &&
                binding.propertyName != targetBinding.propertyName)
            {
                AnimationUtility.SetEditorCurve(
                    clip,
                    binding,
                    null
                );
            }
        }
    }

    static bool IsZRotationProperty(string propertyName)
    {
        return propertyName == "localEulerAngles.z" ||
               propertyName == "localEulerAnglesRaw.z";
    }

    static void ScaleAllCurvesTime(
        AnimationClip clip,
        float timeScale
    )
    {
        if (clip == null || timeScale <= 0f)
            return;

        EditorCurveBinding[] bindings =
            AnimationUtility.GetCurveBindings(clip);

        foreach (EditorCurveBinding binding in bindings)
        {
            AnimationCurve sourceCurve =
                AnimationUtility.GetEditorCurve(clip, binding);

            if (sourceCurve == null || sourceCurve.length == 0)
                continue;

            Keyframe[] sourceKeys = sourceCurve.keys;
            Keyframe[] scaledKeys = new Keyframe[sourceKeys.Length];

            for (int i = 0; i < sourceKeys.Length; i++)
            {
                Keyframe key = sourceKeys[i];
                key.time *= timeScale;
                scaledKeys[i] = key;
            }

            AnimationUtility.SetEditorCurve(
                clip,
                binding,
                new AnimationCurve(scaledKeys)
            );
        }
    }

    static void AddRotationOffset(
        AnimationClip clip,
        string objectPath,
        float offset
    )
    {
        EditorCurveBinding[] bindings =
            AnimationUtility.GetCurveBindings(clip);

        foreach (EditorCurveBinding binding in bindings)
        {
            if (binding.path != objectPath ||
                !IsZRotationProperty(binding.propertyName))
                continue;

            AnimationCurve sourceCurve =
                AnimationUtility.GetEditorCurve(clip, binding);

            if (sourceCurve == null)
                continue;

            Keyframe[] keys = sourceCurve.keys;
            for (int i = 0; i < keys.Length; i++)
                keys[i].value += offset;

            AnimationUtility.SetEditorCurve(
                clip,
                binding,
                new AnimationCurve(keys)
            );

            return;
        }

        Debug.LogWarning(
            $"[MidBoss1] 회전 커브를 찾지 못해 오프셋을 적용하지 못했습니다: {objectPath}"
        );
    }

    static void SaveOrReplaceClip(
        AnimationClip generated,
        string assetPath
    )
    {
        AnimationClip existing =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);

        if (existing == null)
        {
            AssetDatabase.CreateAsset(generated, assetPath);
            return;
        }

        EditorUtility.CopySerialized(generated, existing);
        EditorUtility.SetDirty(existing);
        Object.DestroyImmediate(generated);
    }

    static void EnsureFolders()
    {
        if (AssetDatabase.IsValidFolder(SAVE_PATH))
            return;

        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");

        if (!AssetDatabase.IsValidFolder("Assets/Animations/1"))
            AssetDatabase.CreateFolder("Assets/Animations", "1");

        if (!AssetDatabase.IsValidFolder(SAVE_PATH))
        {
            AssetDatabase.CreateFolder(
                "Assets/Animations/1",
                "MidBoss 1"
            );
        }
    }
}
