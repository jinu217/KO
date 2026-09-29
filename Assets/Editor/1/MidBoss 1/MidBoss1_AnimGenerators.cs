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

    [MenuItem("Tools/1/MidBoss1/Generate AC")]
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

        // 참고용 클립은 파일로 보존하고 게임플레이 상태/파라미터만 제거합니다.
        foreach (var layer in ac.layers) RemoveReferenceAttackStates(layer.stateMachine);
        for (int i = ac.parameters.Length - 1; i >= 0; i--)
            if (IsReferenceAttackParameter(ac.parameters[i].name)) ac.RemoveParameter(i);
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

            if (IsReferenceAttackName(clipName) || IsReferenceAttackName(clip.name)) continue;
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

            EnableLocomotionLoop(clip);
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

    [MenuItem("Tools/1/MidBoss1/Generate Walk and Run")]
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

    // 콤보 제작용 내부 참고 클립 생성. 단독 공격 메뉴/상태는 만들지 않습니다.
    static void GenerateHorizontalAttack()
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
        Debug.Log("[MidBoss1] 콤보 참고용 HorizontalAttack 갱신 완료 (1.6초, 임팩트 0.64초, 기존 대비 0.5배속)");
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

    [MenuItem("Tools/1/MidBoss1/Generate Combo Attacks")]
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
            // 준비 구간(0~0.25초)과 HB 누적 회전을 포함해 무기를 돌려 올립니다.
            // 사용자가 확인한 실제 양손찍기 2 에셋을 우선 사용합니다.
            AnimationClip twoHandSlam2 = LoadClip("양손찍기 2");
            if (twoHandSlam2 == null)
            {
                twoHandSlam2 = DecodeTwoHandSlam(TwoHandSlam2Data, "양손찍기 2");
                temporary.Add(twoHandSlam2);
            }
            AnimationClip second = SliceComboClip(twoHandSlam2, 0f, 0.87f, 1f);
            temporary.Add(second);
            // 가로공격은 반속 기준 0.48초의 준비 자세부터 마지막 Rest까지.
            // 가로베기 반동까지 재생한 뒤 0.15초 유지, 복귀 구간은 0.20초 연장.
            AnimationClip swing = SliceComboClip(horizontal, 0.48f, 0.96f, 1f);
            temporary.Add(swing);
            AnimationClip recovery = SliceComboClip(horizontal, 0.96f, 1.6f, 0.64f / 0.84f);
            temporary.Add(recovery);
            HoldComboStart(recovery, 0.15f);
            AnimationClip finish = BuildCombo("HorizontalFinish", new[] { swing, recovery }, 1f, 0f);
            temporary.Add(finish);
            HoldComboStart(finish, 5f / 60f);
            // 세로 → 가로: 기존 추가 5프레임에서 총 4프레임 단축, 도착 후 총 5프레임 유지.
            const float horizontalPreparation = 0.22f + (5f - 4f) / 60f;
            AnimationClip combo2 = BuildCombo("Combo2", new[] { rest, first, finish }, 1f, 0.22f,
                new[] { 0.239f, horizontalPreparation });
            temporary.Add(combo2);
            // 돌려 올린 HB가 가로 장전으로 넘어갈 때 반대로 한 바퀴 풀리지 않게
            // 3연격 전용 가로 구간만 동등한 회전값으로 맞춥니다.
            AnimationClip finish3 = Object.Instantiate(finish);
            temporary.Add(finish3);
            foreach (var binding in AnimationUtility.GetCurveBindings(finish3))
            {
                if (binding.type != typeof(Transform) || !binding.propertyName.StartsWith("localEulerAngles")) continue;
                var next = AnimationUtility.GetEditorCurve(finish3, binding);
                var previous = AnimationUtility.GetEditorCurve(second, binding);
                if (next == null || next.length == 0 || previous == null || previous.length == 0) continue;
                float offset = 360f * Mathf.Round((previous.Evaluate(second.length) - next.Evaluate(0f)) / 360f);
                var keys = next.keys;
                for (int k = 0; k < keys.Length; k++) keys[k].value += offset;
                next.keys = keys;
                AnimationUtility.SetEditorCurve(finish3, binding, next);
            }
            // 첫 타격 후 0.24초 연결 + 두 번째 공격 자체의 준비 0.25초.
            AnimationClip combo3 = BuildCombo("Combo3", new[] { rest, first, second, finish3 }, 1f, 0.22f,
                new[] { 0.239f, 0.24f, horizontalPreparation });
            temporary.Add(combo3);
            float secondStart = rest.length + 0.239f + first.length + 0.24f;
            CopyComboHandRotation(combo3, twoHandSlam2, secondStart, 0.87f);
            SaveOrReplaceClip(combo2, $"{SAVE_PATH}/{MOB_PREFIX}Combo2.anim");
            SaveOrReplaceClip(combo3, $"{SAVE_PATH}/{MOB_PREFIX}Combo3.anim");
            AssetDatabase.SaveAssets();
            GenerateAC();
            Debug.Log("[MidBoss1] 2연격: 세로 → 가로 / 3연격: 세로 → 양손찍기 2 → 가로 생성 완료. 기본공격은 기존 양손찍기를 유지합니다.");
        }
        finally
        {
            foreach (var clip in temporary)
                if (clip != null && !AssetDatabase.Contains(clip)) Object.DestroyImmediate(clip);
        }
    }

    // 원본 HB 회전의 키와 접선을 시간 이동만 하여 복사합니다.
    // 360도 이상 누적되는 Euler 회전을 정규화하거나 최단 경로로 바꾸지 않습니다.
    static void CopyComboHandRotation(AnimationClip combo, AnimationClip source, float start, float duration)
    {
        const string handPath = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B/Arm_B/Hand_B";
        foreach (var binding in AnimationUtility.GetCurveBindings(source))
        {
            if (binding.path != handPath || !binding.propertyName.StartsWith("localEulerAngles")) continue;
            var original = AnimationUtility.GetEditorCurve(source, binding);
            var combined = AnimationUtility.GetEditorCurve(combo, binding);
            if (original == null || original.length == 0 || combined == null) continue;
            var keys = new System.Collections.Generic.List<Keyframe>();
            float end = start + duration;
            foreach (var key in combined.keys)
                if (key.time < start - 0.00001f || key.time > end + 0.00001f) keys.Add(key);
            foreach (var originalKey in original.keys)
            {
                if (originalKey.time < 0f || originalKey.time >= duration - 0.00001f) continue;
                var key = originalKey;
                key.time += start;
                if (originalKey.time == 0f) key.inTangent = 0f;
                keys.Add(key);
            }
            keys.Add(new Keyframe(end, original.Evaluate(duration), 0f, 0f));
            keys.Sort((a, b) => a.time.CompareTo(b.time));
            AnimationUtility.SetEditorCurve(combo, binding, new AnimationCurve(keys.ToArray()));
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

    [MenuItem("Tools/1/MidBoss1/Generate Buff and Dash")]
    public static void GenerateBuffAndDash()
    {
        EnsureFolders();
        GenerateBuff();
        GenerateDash();
        AssetDatabase.SaveAssets();
        GenerateAC();
        Debug.Log("[MidBoss1] Buff 약 2.33초 / Dash 0.8초 생성 및 AC 등록 완료. 실제 돌진 이동은 이동 컨트롤러에서 처리합니다.");
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
        const float stretchSpeed = 0.7f;
        const float recoveryExtra = 5f / 60f;
        const float stretchEnd = 0.36f + (1.28f - 0.36f) / stretchSpeed;
        const float recoveryDuration = (1.85f - 1.28f) + recoveryExtra;
        AnimationClip clip = CreatePatternRest("Buff", stretchEnd + recoveryDuration);
        const string sb = "Body/ShoulderJoint_B";
        const string ab = sb + "/Shoulder_B/ArmJoint_B";
        const string hb = ab + "/Arm_B/Hand_B";
        const string sf = "Body/ShoulderJoint_F";
        const string af = sf + "/Shoulder_F/ArmJoint_F";
        const string hf = af + "/Arm_F/Hand_F";
        const string tb = "Body/LowerBody/ThighJoint_B";
        const string tf = "Body/LowerBody/ThighJoint_F";
        float[] t = { 0f, 0.18f, 0.36f, 0.44f, 0.62f, 0.72f, 0.90f, 1.00f, 1.18f, 1.28f, 1.56f, 1.85f };
        // 자세를 잡는 구간은 유지하고, 세 번 당기고 푸는 리듬만 0.7배속.
        // 마지막 자세 복귀는 기존보다 60 FPS 기준 5프레임 길게 보간합니다.
        for (int i = 0; i < t.Length; i++)
        {
            if (t[i] > 1.28f)
                t[i] = stretchEnd + (t[i] - 1.28f) / (1.85f - 1.28f) * recoveryDuration;
            else if (t[i] > 0.36f)
                t[i] = 0.36f + (t[i] - 0.36f) / stretchSpeed;
        }
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

    [MenuItem("Tools/1/MidBoss1/Setup Gameplay Animator")]
    public static void SetupGameplayAnimator()
    {
        EnsureFolders();
        AnimationClip basicAttackClip = LoadClip("양손찍기");
        if (basicAttackClip == null)
        {
            Debug.LogError("[MidBoss1] " + SAVE_PATH + "/MidBoss 1_양손찍기.anim을 먼저 넣어주세요.");
            return;
        }
        // Rest 좌표는 제작 기준일 뿐, 실제 대기는 사용자가 지정한 Idle 클립입니다.
        AnimationClip idleClip = LoadExactGameplayClip("MidBoss 1_Idle");
        if (idleClip == null)
        {
            Debug.LogError("[MidBoss1] 프로젝트에서 MidBoss 1_Idle 클립을 찾을 수 없습니다.");
            return;
        }
        GenerateDashPhases();
        GenerateAC();
        AnimatorController ac = AssetDatabase.LoadAssetAtPath<AnimatorController>($"{SAVE_PATH}/{AC_NAME}.controller");
        string[] triggers = { "BasicAttack", "Buff", "Combo2", "Combo3", "Dash" };
        // 기존 동명 파라미터의 타입이 다르면 임의 변경하지 않고 알려줍니다.
        foreach (string trigger in triggers) EnsureGameplayParameter(ac, trigger, AnimatorControllerParameterType.Trigger);
        EnsureGameplayParameter(ac, "MoveSpeed", AnimatorControllerParameterType.Float);
        EnsureGameplayParameter(ac, "IsChasing", AnimatorControllerParameterType.Bool);
        EnsureGameplayParameter(ac, "IsAttacking", AnimatorControllerParameterType.Bool);
        EnsureGameplayParameter(ac, "DashMoving", AnimatorControllerParameterType.Bool);
        EnsureGameplayParameter(ac, "DashFinished", AnimatorControllerParameterType.Bool);
        EnsureGameplayParameter(ac, "AttackSpeed", AnimatorControllerParameterType.Float);
        var parameters = ac.parameters;
        for (int i = 0; i < parameters.Length; i++)
            if (parameters[i].name == "AttackSpeed") parameters[i].defaultFloat = 1f;
        ac.parameters = parameters;

        AnimatorStateMachine sm = ac.layers[0].stateMachine;
        AnimatorState idle = FindGameplayState(sm, idleClip);
        if (idle == null) idle = sm.AddState("MidBoss 1_Idle");
        EnableLocomotionLoop(idleClip);
        idle.motion = idleClip;
        idle.speed = 1f;
        idle.speedParameterActive = false;
        sm.defaultState = idle;
        // 이 생성기가 만든 전환만 갱신하여 반복 실행 시 중복을 방지합니다.
        foreach (var transition in sm.anyStateTransitions)
            if (transition.name.StartsWith("MidBoss1_Auto_")) sm.RemoveAnyStateTransition(transition);
        foreach (var child in sm.states)
            foreach (var transition in child.state.transitions)
                if (transition.name.StartsWith("MidBoss1_Auto_")) child.state.RemoveTransition(transition);

        AnimatorState walk = FindGameplayState(sm, LoadClip("Walk"));
        AnimatorState run = FindGameplayState(sm, LoadClip("Run"));
        if (walk != null && run != null && walk != run)
        {
            AddLocomotionTransition(idle, walk, "Idle_Walk", false);
            AddLocomotionTransition(idle, run, "Idle_Run", true);
            AddLocomotionTransition(walk, run, "Walk_Run", true);
            AddLocomotionTransition(run, walk, "Run_Walk", false);
            AddStopTransition(walk, idle, "Walk_Idle");
            AddStopTransition(run, idle, "Run_Idle");
        }
        else
        {
            AnimatorState moving = run != null ? run : walk;
            if (moving != null)
            {
                var move = GameplayTransition(idle, moving, "MidBoss1_Auto_Move");
                move.AddCondition(AnimatorConditionMode.Greater, 0.05f, "MoveSpeed");
                move.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsAttacking");
                AddStopTransition(moving, idle, "Move_Idle");
            }
        }
        foreach (string trigger in triggers)
        {
            AnimationClip clip = trigger == "Dash" ? LoadClip("DashPrepare") :
                trigger == "BasicAttack" ? basicAttackClip : LoadClip(trigger);
            AnimatorState state = FindGameplayState(sm, clip);
            if (state == null) { Debug.LogWarning("[MidBoss1] 클립이 없어 전환을 생략했습니다: " + trigger); continue; }
            state.speedParameter = "AttackSpeed";
            state.speedParameterActive = true;
            string transitionName = "MidBoss1_Auto_" + trigger;
            AnimatorStateTransition enter = null;
            foreach (var existing in sm.anyStateTransitions)
                if (existing.name == transitionName) { enter = existing; break; }
            if (enter == null) enter = sm.AddAnyStateTransition(state);
            enter.name = transitionName; enter.destinationState = state;
            ConfigureGameplayTransition(enter);
            enter.canTransitionToSelf = true;
            enter.AddCondition(AnimatorConditionMode.If, 0f, trigger);
            if (trigger == "Dash")
            {
                ConfigureDashPhases(sm, state, idle);
                continue;
            }
            var leave = GameplayTransition(state, idle, transitionName + "_End");
            leave.hasExitTime = true;
            leave.exitTime = 1f;
        }
        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        Debug.Log("[MidBoss1] BasicAttack(양손찍기), 패턴 Trigger, MoveSpeed, IsChasing, IsAttacking, AttackSpeed 및 전환 연결 완료. 보스 Animator에 MidBoss 1_AC를 연결하세요.");
    }

    // 대기/걷기/달리기는 반복 재생, 공격/버프/돌진의 루프 설정은 건드리지 않습니다.
    static void EnableLocomotionLoop(AnimationClip clip)
    {
        if (clip == null) return;
        bool locomotion = false;
        foreach (string action in new[] { "Idle", "Walk", "Run" })
            if (string.Equals(clip.name, action, System.StringComparison.OrdinalIgnoreCase) || IsNamedClip(clip.name, action))
                locomotion = true;
        if (!locomotion) return;
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
    }

    static AnimationClip LoadExactGameplayClip(string clipName)
    {
        AnimationClip match = null;
        foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                AnimationClip clip = asset as AnimationClip;
                if (clip == null || clip.name != clipName) continue;
                if (match != null && match != clip)
                    throw new System.InvalidOperationException("동일한 이름의 클립이 여러 개 있습니다: " + clipName);
                match = clip;
            }
        }
        return match;
    }

    static void GenerateDashPhases()
    {
        // 기존 대쉬 자세를 준비·이동 루프·제동으로 나눕니다.
        GenerateDash();
        AnimationClip source = LoadClip("Dash");
        AnimationClip prepare = SliceComboClip(source, 0f, 0.22f, 1f);
        prepare.name = MOB_PREFIX + "DashPrepare";
        SavePattern(prepare, "DashPrepare");
        AnimationClip move = SliceComboClip(source, 0.30f, 0.50f, 1f);
        move.name = MOB_PREFIX + "DashMove";
        // 전경 자세로 작게 움직이는 루프. 끝 자세와 시작 자세를 일치시킵니다.
        foreach (var binding in AnimationUtility.GetCurveBindings(move))
        {
            var sourceCurve = AnimationUtility.GetEditorCurve(source, binding);
            float first = sourceCurve.Evaluate(0.30f);
            float middle = sourceCurve.Evaluate(0.40f);
            SetHorizontalMotion(move, binding.path, binding.propertyName,
                new[] { 0f, 0.10f, 0.20f }, first, middle, first);
        }
        var settings = AnimationUtility.GetAnimationClipSettings(move);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(move, settings);
        SaveOrReplaceClip(move, $"{SAVE_PATH}/{MOB_PREFIX}DashMove.anim");
        AnimationClip end = SliceComboClip(source, 0.50f, 0.80f, 1f);
        end.name = MOB_PREFIX + "DashEnd";
        // 이동 자세에 가까운 전경 자세에서 제동을 시작합니다.
        foreach (var binding in AnimationUtility.GetCurveBindings(end))
        {
            var curve = AnimationUtility.GetEditorCurve(end, binding);
            var key = curve[0];
            key.value = AnimationUtility.GetEditorCurve(source, binding).Evaluate(0.30f);
            key.inTangent = 0f; key.outTangent = 0f;
            curve.MoveKey(0, key);
            AnimationUtility.SetEditorCurve(end, binding, curve);
        }
        SavePattern(end, "DashEnd");
    }

    static void ConfigureDashPhases(AnimatorStateMachine sm, AnimatorState prepare, AnimatorState idle)
    {
        AnimatorState move = FindGameplayState(sm, LoadClip("DashMove"));
        AnimatorState end = FindGameplayState(sm, LoadClip("DashEnd"));
        move.speedParameterActive = false;
        move.speed = 1f;
        end.speedParameter = "AttackSpeed";
        end.speedParameterActive = true;
        var begin = GameplayTransition(prepare, move, "MidBoss1_Auto_Dash_Move");
        begin.duration = 0f;
        begin.AddCondition(AnimatorConditionMode.If, 0f, "DashMoving");
        var stop = GameplayTransition(move, end, "MidBoss1_Auto_Dash_Stop");
        stop.duration = 0f;
        stop.AddCondition(AnimatorConditionMode.IfNot, 0f, "DashMoving");
        var cancelled = GameplayTransition(prepare, end, "MidBoss1_Auto_Dash_Cancel");
        cancelled.AddCondition(AnimatorConditionMode.If, 0f, "DashFinished");
        var finish = GameplayTransition(end, idle, "MidBoss1_Auto_Dash_Idle");
        finish.hasExitTime = true;
        finish.exitTime = 1f;
    }

    static void AddLocomotionTransition(AnimatorState from, AnimatorState to, string name, bool chasing)
    {
        var transition = GameplayTransition(from, to, "MidBoss1_Auto_" + name);
        transition.AddCondition(AnimatorConditionMode.Greater, 0.05f, "MoveSpeed");
        transition.AddCondition(chasing ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, "IsChasing");
        transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsAttacking");
    }

    static void AddStopTransition(AnimatorState from, AnimatorState rest, string name)
    {
        var transition = GameplayTransition(from, rest, "MidBoss1_Auto_" + name);
        transition.AddCondition(AnimatorConditionMode.Less, 0.05f, "MoveSpeed");
        transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsAttacking");
    }

    static void EnsureGameplayParameter(AnimatorController ac, string name, AnimatorControllerParameterType type)
    {
        foreach (var parameter in ac.parameters)
            if (parameter.name == name)
            {
                if (parameter.type != type) throw new System.InvalidOperationException("Animator 파라미터 타입을 확인하세요: " + name + " → " + type);
                return;
            }
        ac.AddParameter(name, type);
    }

    static AnimatorState FindGameplayState(AnimatorStateMachine sm, AnimationClip clip)
    {
        if (clip == null) return null;
        foreach (var child in sm.states) if (child.state.motion == clip) return child.state;
        return null;
    }

    static AnimatorStateTransition GameplayTransition(AnimatorState from, AnimatorState to, string name)
    {
        AnimatorStateTransition transition = null;
        foreach (var existing in from.transitions) if (existing.name == name) { transition = existing; break; }
        if (transition == null) transition = from.AddTransition(to);
        transition.name = name;
        transition.destinationState = to;
        ConfigureGameplayTransition(transition);
        return transition;
    }

    static void ConfigureGameplayTransition(AnimatorStateTransition transition)
    {
        transition.conditions = new AnimatorCondition[0];
        transition.hasExitTime = false;
        transition.hasFixedDuration = true;
        transition.duration = 0.04f;
        transition.offset = 0f;
        transition.interruptionSource = TransitionInterruptionSource.None;
    }

    static bool IsReferenceAttackParameter(string name)
        => name == "VerticalAttack" || name == "HorizontalAttack";

    static bool IsReferenceAttackName(string name)
    {
        foreach (string action in new[] { "VerticalAttack", "HorizontalAttack", "세로공격", "가로공격" })
            if (string.Equals(name, action, System.StringComparison.OrdinalIgnoreCase) || IsNamedClip(name, action))
                return true;
        return false;
    }

    static bool UsesReferenceAttackParameter(AnimatorStateTransition transition)
    {
        foreach (var condition in transition.conditions)
            if (IsReferenceAttackParameter(condition.parameter)) return true;
        return false;
    }

    static void RemoveReferenceAttackStates(AnimatorStateMachine sm)
    {
        foreach (var transition in sm.anyStateTransitions)
            if (UsesReferenceAttackParameter(transition)) sm.RemoveAnyStateTransition(transition);
        foreach (var transition in sm.entryTransitions)
            foreach (var condition in transition.conditions)
                if (IsReferenceAttackParameter(condition.parameter))
                { sm.RemoveEntryTransition(transition); break; }
        foreach (var child in sm.states)
        {
            foreach (var transition in child.state.transitions)
                if (UsesReferenceAttackParameter(transition)) child.state.RemoveTransition(transition);
            AnimationClip clip = child.state.motion as AnimationClip;
            if (IsReferenceAttackName(child.state.name) || (clip != null && IsReferenceAttackName(clip.name)))
                sm.RemoveState(child.state);
        }
        foreach (var child in sm.stateMachines) RemoveReferenceAttackStates(child.stateMachine);
    }

    [MenuItem("Tools/1/MidBoss1/Generate Two Hand Slam Versions")]
    public static void GenerateTwoHandSlamVersions()
    {
        EnsureFolders();
        // 검토용 두 결과의 커브를 내장하여 원본/외부 파일 없이 동일하게 재생성합니다.
        AnimationClip original = DecodeTwoHandSlam(TwoHandSlamData, "양손찍기");
        AnimationClip alternative = DecodeTwoHandSlam(TwoHandSlam2Data, "양손찍기 2");
        SaveOrReplaceClip(original, FindOrCreateClipPath("양손찍기"));
        SaveOrReplaceClip(alternative, FindOrCreateClipPath("양손찍기 2"));
        AssetDatabase.SaveAssets();
        GenerateAC();
        // BasicAttack은 기존 양손찍기에만 연결됩니다. 2번에는 전환을 만들지 않습니다.
        Debug.Log("[MidBoss1] 양손찍기 / 양손찍기 2 생성 완료. 각 1.35초, 60 FPS 기준 임팩트 40프레임. 2번은 비교용입니다.");
    }

    static AnimationClip DecodeTwoHandSlam(string data, string action)
    {
        using (var memory = new System.IO.MemoryStream(System.Convert.FromBase64String(data)))
        using (var gzip = new System.IO.Compression.GZipStream(memory, System.IO.Compression.CompressionMode.Decompress))
        using (var reader = new System.IO.BinaryReader(gzip))
        {
            if (reader.ReadInt32() != 1) throw new System.InvalidOperationException("양손찍기 커브 데이터 버전 오류");
            float frameRate = reader.ReadSingle();
            float duration = reader.ReadSingle();
            AnimationClip clip = new AnimationClip { name = MOB_PREFIX + action, frameRate = frameRate };
            int bindingCount = reader.ReadInt32();
            for (int b = 0; b < bindingCount; b++)
            {
                string path = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
                string property = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
                int count = reader.ReadInt32();
                var keys = new System.Collections.Generic.List<Keyframe>(count);
                for (int k = 0; k < count; k++)
                {
                    var key = new Keyframe(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                    // Float precision can collapse separately sampled impact times onto one key.
                    if (keys.Count > 0 && key.time - keys[keys.Count - 1].time <= 0.000001f)
                        keys[keys.Count - 1] = key;
                    else
                        keys.Add(key);
                }
                var curve = new AnimationCurve(keys.ToArray());
                // Dense baked samples: linear segments prevent tangent overshoot.
                for (int k = 0; k < curve.length; k++)
                {
                    AnimationUtility.SetKeyBroken(curve, k, true);
                    AnimationUtility.SetKeyLeftTangentMode(curve, k, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(curve, k, AnimationUtility.TangentMode.Linear);
                }
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
            }
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.startTime = 0f;
            settings.stopTime = duration;
            settings.loopTime = false;
            settings.loopBlend = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }
    }

    // 양손찍기 두 버전의 경로/속성/시간/값/접선 데이터 (gzip + base64).
    const string TwoHandSlamData =
        "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" +
        "pJK1CqYRrEglaxVMI1iRStYqmEawIpWsVTCNYEUqWatgJoEVqWStgpkEVqSStQpmEliRStYqmElgRSoZq2AmgRWpZKyCmQRWpJKxCuZBsCKVjFUwD4IVqWSs" +
        "gnkQrEglYxXMA2BFKv2sgrkfrEilXxbB9PszwRyjC6YumP9NgqmuN0Wz3lTNek9r1ntes95LmvVe1az3pma9dzXrfaBZ7xPNen/XrPe1Zr3vNeu142zPn52z" +
        "Xb8DZ3s9OTnN65+zvb48nO39ycfZXm8BTvP7wtlefxHO9v45c7brceE0v1+c7fpKcLb3tyRnu15PzXrLaNZbTrPeCpr1VtSst7JmvVU1662mWW8NzXp9NOut" +
        "rVlvXc1662vW66tZL6NZL69Zr6BZr79mvQGa9TbWrLepZr3NNettqVlvG81622nWG6hZb5BmvZ006+2iWW83mxiCabv+npr199asv69m/cGa9Q/QrH+QZv1D" +
        "NOsfpll/mGb9IzTrH6VZf7hm/RGa9Y/VrH+8Zr2RmvNP0qw/SnM9kzX3Y4rm+qI19ydGc71TNfdrmub6p2vu3wzNemZq7ucszfpma+7vHM1652nWO1+z3gWa" +
        "9S7UrHeRZr2LNetdolnvUs16l2nWG6tZ73LNeldo1rtSs95VmvX+oFlvvGa9qzXrTdCsd41mvWs1612nWe96zXoTNevdoFnvRs16N2nWu1mz3i02saNVML0b" +
        "9+vV93/Nk2e3fnsWU6NHscZfdpSIz0fJ8nlKj2KNv+woY7L/L8arXVHRz0dpNyBk1JC+/YY3DRk4bGQP4Z8uLsvH8OcDcv/KAf9YZxbvoQe0xn/5gF+0ZI9/" +
        "cMA/4j9f/dc+dsQ3PPYX3ZPKf35sbzJ86Je8OLI4vvWV35z9eqfJcqd40XY1NP5Kp/mim1b9L50G+DdeWV/rZBH/zpN90W2s+y+czLtxr2F9//RuRrK254xk" +
        "v9E5I7K87Zs1b/vmb3TOL7q37L9+Tu+gfr1CQ4b9y7/r3+bUWe+0SXOnTd/21H/n8zTgn95G5Z+FNu9l1vgvH/DvfJ4GfO3P04DMux3wtT9P/+zYEd/w2H/n" +
        "8zTrsTNfgf/89hTdetTmLcUaf6XT/HGntjYqbfMatMZf6TR/5/P0H5yG/toGfO3P0798soh/58n+zufpPz2Z+u73r9/Nr3zOrP8AMmv+AWT+Ruf8ontb4PM5" +
        "m4eE9xv+p9XSWfIBNNdgjf/INWSNkWvIGiPXkDVGrsH2+5vbxMg1aI5vEyPXYHu8TTYxcg2a67GJkWuwPd8Tmxifu7bnz20TI9dgez2uNjFyDZr12sTINdhe" +
        "r69NjFyD5v7YxMg12K6nm02MXIPmftrEyDXYrjfyj7jx+8Iicg3nfeoz4B9PFBORawguXp4BR5tdReQaYpcVYsCDT5QQkWtwXffaDxz1zl1ErmFPygk/8OqG" +
        "JUXkGtiLa/zAvtvcReQaLnmM9APfrFNCRK7haVBpP/CeK8VF5Bo6J8T5gpfGO4vINTxlI2uDn0Y5icg1NKq2IgncdnI+EU5wdmpxI/ju6pwicg1xgT2NYDdT" +
        "hhm5hnz+C4zgBiPSzcg19G+30wie1O+6GbmGje6njOAHMWfNyDWcaP3ICJ5a54QZuYZdF+xNYOfth83INSxwcDKBD/9uNCPX0HlVSRN4YLZdZuQa+syqZAJP" +
        "vpRoRq7BwVTLBF40doUZuYYlbpwJfOjebDP2QkLmNTaB030nmJFr+M2xjQnsxHQ3I9cQNLyjCVzrjK8ZuYaie7qbwCveFzMj1xBu6mMCtx1+3YRcQ9r3/U3g" +
        "wGKzIJPczNSBlO+/+s2EXEPM1qGU81dvb0auoYY0kvLhGuFm5BoWfxpnAteP7Gam1dLVo0zgePdcZuQaLoZOMYEd13J+yDWYmCkm8MfkLixyDUUWRZrALTxf" +
        "sMg1JN0INYEdujan1dINEwJN4OYPltBq6RN9vExg34/XabX0mja3jOA+XVx45Bp+NNkbwU0ja/HINTxsN94PHDmkEY9cw4vtlRhwgGdrHrmG6R1nMeCEFS15" +
        "5BpehiYz4AXXBB65htwDXjPgIver8sg15JALs+B7B/PxyDWsiS7PgjdsPMQh12CfUJMFP+rQi0Ou4bdyDVjwjKbHWOQaLBMZFty5s5sRuYYqgxnK3KZEWi2d" +
        "GteABV9dVVBErqHhhdoseE1ybRG5hnpHvVlw4YAAEbmGhlUqsWBDBSIi11AoqQwL9uxTQUSuoYF3SRZs/+4hrZbux7uyKqu5BsRZcw1ZY+QassbINWSNkWvI" +
        "GiPXkDVGriFrjFxD1hi5hqwxcg1ZY+QassbINWSNkWvIGiPXkDVGruFzvLD0WVot/XtdVxY84b6ZVkvnyeHKgjNK+dBq6ah1xVnwvcuTaLV0hltxFtxo5QZa" +
        "LV2+vwsLnnfuBK2WXjazGAs+GfWMVku/n+7MgrecdTQj1/B7n6Is2Lm0mxm5hnIlirDg1Rsr0WrpmjucWHCbofVotbTlu0IsuOKQAFotPSKyAAs2rmtHq6Wf" +
        "GvOxYL8SPWm1dNq1PCx44k+DabX0qXu5WfCtvd/Taunxl3Ky4L3DImm19OQ9Dix4jkc0rZaOHp+dBb8aOp1WS3/nY8eCn3WaTaul3be/Z8ATb82j1dIHf3nF" +
        "gFflWUSrpcMePWPALU8uodXS0+48YsD2NWJptXTj4/cYcHCNFbRa+s2qmwz4559X0mrpX/tcZcApjj/QaukmHhcZcLubP9Bq6TFnzzDg0I5xtFr64OQTDLj0" +
        "kDhaLd3OJ4UB9yoZR6ulZ92RGXDtIfTpHO7HVSYGvLLjKlot/bH7fga88OYKWi19uOouBlwyz3JaLd0iz1YG3ODkUlotffXdBgZ8r8ZiWi1tzLaWAX9XcwGt" +
        "lq5ZJo4Bjzw5h1ZLT+uxnAF3yzuTVkvnMC1mwO1ux9Bq6efMfAZ8tXMUrZZefGcWA358dQytlq63bxoDDiChtFq6zN4pDPjw9b60Wnr5nUgG3PdAR1otfW7+" +
        "GAZc6edmtFp6x5ERDLhoKZZWS3cNHcqAPTd602rpPov7M+C2IZ60WrqjX28GvDG0kBm5hp8Gd2PA/p0zaLV008qdGPCkJldotXTwhEAG3GXrPlotvXVYWwZ8" +
        "ZsZ8Wi29K1trBnzyQDtaLZ2DbcmAZzbNZkKuwadyCwbczsPbaN0/YcBfrpp/1E1n3UOAambdQ4BqZo2hmlljqGbWGKpp+/3NbWKopub4NjFU0/Z4m2xiqKbm" +
        "emxiqKbt+Z7YxFBN2/PntomhmrbX42oTQzU167WJoZq21+trE0M1NffHJoZq2q6nm00M1dTcT5sYqmm73sg/4u1zC5igmiu3uIrgOu1zmeiDeYpWguseymaC" +
        "ar446SqCt5nfG6Gapl9dRfDQZq+NUM36z1xF8MBqr4xQzTXZ3ERwhwFvjFDNmoXdRHDMm49GqGbpsm4ieGmIPX4V2MRabiLYcY4jXtrspwA3Efw+yMkE1ezR" +
        "2U0E5z7hZoJq2oe4ieD2j8uboJplo91E8J79NemDea9Xuongdd6EPph3YK+bCN7Upo0Jqplwxk0EPx8SZaIP5r10E8FDpiSaoJrFypQQwf43kk1QzbYdSojg" +
        "pZPTTFDN43NLiOAqg2+aoJpVUkuI4PXLbpugmp6O7iK4f95rJqhmkL+7CI5L/tkE1Rwz1l0EV5T2mqCadba4i+BJDktMUM2659xFcGu3TiaoZqVn7iK44tD7" +
        "RqjmAbuSIviG6/z6UM2FSgzOHUpLyrheT9xF8PgFP/lBNU/+6i6Cp9+pboRq7rnrLoIXJ2+kddM5n7uL4AUTVydBNfe9cBfB+a44M1BNo/L3weVWn2Ogmrk3" +
        "uIvgbZFNqGrO93MXwdsmr6aq2XheCRHcdPhDFqpZbrGbCC4WUJqqpgvjKoJ/XNaHqubsIBcRvOnaYvpgntvFoiLYNWY7fTDv6V0nEXxKkuiDecyWAiK48IJj" +
        "9MG82q3ziuDbr47SB/Nyvs8lgv3TjfTBvFvnHESw94QN9MG8Dzmyi+CB8TPog3kOQzPM4H7bAqhqNiv5wQz+fepb+mDeq9zvzOBnT+ZT1Vxz+o0ZPKDhbQaq" +
        "eSfojRm8IiY3rZtuMe6NGVzlaXH6YN4wj7dm8JWGw+mDeb+Ufkd5YeGl9MG88n3fU+7aaD19MO/6kQ+UR1nW0wfzVlb+RNlzwjKqmo9DMqysqibirKqZNYZq" +
        "Zo2hmlljqGbWGKqZNYZqZo2hmlljqGbWGKqZNYZqZo2hmlljqGbWGKqZNYZqfo79q12gqllnZIYZPHSzRFUz95wMM7j9ntpUNcebM8zgQkmTqWr2yG4ngsW8" +
        "m6lqfvC2E8EbTpyiqrmpv50IvpDxgqqm3UY7EdzqdF6qmvN/txPB2bq4U9V8GZBNBH9wr0pV05KQTQQ3dPGlqjktZ3YRfKVJE6qat8Kyi+Cg3R2oava7nV0E" +
        "3w/qQ1VzXTd7EdzXJ4SqZr2r9iK4YqMxVDV39XYQwauaRFHV/PGxgwg+kXcqVc0D43OI4NKDZlLVDCmcUwSv6juXqmbvzTlFMPd+AVXNyo1zieCc1ZZQ1ezy" +
        "Wy4R/PuHZVQ1Z87MLVLuv4KqZqsajiKYhKyiqslcchTBqwvHUdV8GJVHBFdsG09V87FXXhF81ms1Vc3Ll/KK4HXrVlPVDIjJJ4LjDqymqplSM78INg5cTVXz" +
        "xbX8Ijjb3niqmr1mFBDBQ3+Io6q5plZBEfyhzA9UNdtcLSiCdxlWUtW0m1JIBM+3W05Vs2FFJxG8sNNSqpp7jjmJYFPLRVQ17/UvLIKjr8+jqhmavYgInpR/" +
        "NlXN28uKiOCSadOoav5SuagI/tVvClXN+/uKiuDZ4ydQ1VzDOYvgDQdGUNWMl5xFcOFqA6hqjmSKieBDl7tQ1dyzs5gIPiy2pKr51tNFBBe5ylPVvDDVRQRv" +
        "qlmTquavd11E8FxLGaqaPzLFRbAUU5iq5pmZxUUwE50ND75zd08VF8EFRl+nqjkhn6sInhV4kKpmNdZVBH9cu5Cq5ux+riJ4d0oHqpo1olxF8A8V7alqbl3o" +
        "KoKvr69h/JxjAn+5auoV1HoFtV5BrVdQ6xXUegW1XkGtV1DrFdR6BfVfrqAu8T9U07v9gIH9B3xJZWkWd7CWScWyf+O4X1RK9i8c94sKCcr+8+Oqwd8oF/0b" +
        "x4/4xsf/ovvj/WXH9242rF+/L3vtuGpeO5mZsa90riyvp+aipipS/Mrn+qJ7WOevn8s7ICRk5N942X3dU0b8+0/5RTf2n78p/PMqKuUfKGZNaab5bxz3K7yJ" +
        "BXyjN7EA660P+EZvYv/0+BHf+Phf4U3sj+NneWkG/Fm5teahosxypK90rq/wJvbF5/oKb2L/67nUX++Ab/Qm9ldPGfHvP+W/6dGAApmnbqf86Wfz6IXmAbFI" +
        "7hueN+I/dN4xX3Lez00pnJWvQ/B3G44a0m84GdZ/SL8RbXuF66119W1VfVtV31bVt1X1bVV9W1XfVtW3VfVt1b++rfpnkqm319UlU5dMXTJ1ydQlU5dMXTJ1" +
        "ydQl86tLpl4g+l8mmbn7LRcgmZ+2lePAt7MnCJDMDHEDB3a8nihAMjPKZuPBed5uFiCZGRnVeHDeNlsFSGbG2G48+MbdrQIkM6NgDA/Ok/SjAMnMqL+KB+c9" +
        "sUmAZGZk7OLBeUquFyCZGeWP8+Dbm+IESGZGyHUenCd8qQDJzFj9Ow92nDRboJLJ2xNw7mOTBCqZ9/IS8OVVAwQqmRuKEHD+Ck0FKpn93Ag4b+WKgiqZngS8" +
        "uZmjAMlcurAcARewu0EgmUvbVSRgE7uDQDIvNKpCeXW7yQSSWXm0F+WA8dUIJHPAo2qUC9jt5iGZA7Z4Ux6XXpijkmnxJuBSD6daIJk5vL0JeOXQ/BIks2W2" +
        "agQ8pX47CZK5k61KwCFbZkiQzBxPKxHw/TsbJUimh9t3BJy9yVEJknnhaFkC5i/clFTJ9CTg+NgdEiSTzVOSgHsH15EgmR7RJQh4bOxbCyTz5hg3As6m/H+Q" +
        "zOSXxQl4mfJ3IJnZ7IoR8KlUTxmSeXFNIQI+kxolQzJjHuYi4HWxyTIkc4H8kgcPC3ZMhmQmlTrNg1mfesmQTHu7eB7suysyGZLZcX8dHlzm7oZkSOa4i2UZ" +
        "8OXmR5IhmeaXrS3g8LsXkyGZLytesYANu35NViWzsgQWdl1NhmQeKNVPAn9/92QyJHNbozkS+FLzfcmQzLb710vgyncXJkMy14YelMAjgpslQzKz2aVI4OF3" +
        "r8mQTOdhJyVwfGxFGZIZxpyRwIE+4QSSmVz7tASOCB4lQDIPlDohgRNdRYFKpuWIBN7s+kiAZDa+YZbAwZfsDJBMtuleCRx+N0OAZM6Vf5TA/rvuCpDMR07r" +
        "JfA6172CKpnxksqfJTNespXMzFiVzMxYlczMWJXMzFiVzMxYlczMWJXMzFiVzMxYlczMWJXMzFiVzMxYlczMWJXMzFiVTDUeGIUP0J6c/DxOAv+0YTEPyXyW" +
        "ECeBZwzKRyCZlfPHSeC+S5sRKpm1fpDAjNcUAsnM4bZKAq/uvJpAMk9IKySwRzsTgWT+5rVcAr8vdpFAMh/2XCaBa61/RiCZwd2WSOA+djkESObj8oskcOeF" +
        "hQRIZmHTfAlsN6uEAMkcUH6uBHZ4UF6AZEb2miWB2271FiCZT0ZMl8DZL9YTIJmGHjESuFt/IkAyi1SYLIH7d20iQDKvL54ogT33tRYgmefjR0vgKeFBAiTT" +
        "e3iYBF66qhve3zmngoMkcOdKfQRIZvy43hL4qNsAgUqmpbMEvh02RIBkBl1rK4FPeoXi84A7dqWpBP6uzQgBkjnWIkjgFpdGCZDMhXMaSOCyR8MFSGat5jUl" +
        "cIJbBD4/uOhPlSTwmbMRAiRz46bSEnjH8wgBknkk0FUCNwiPEKhk5nCSwCN6jcbnDTfelFsCd93xvQDJ7DXFTgI/CR4pQDJXm9It4OpRYQIk89aaXy3gMvbD" +
        "8PnEnT58ygI23x8oQDJ3e0kWcJ46/QRIZtqdHRawz70eAiQz6k2CBbwpo5NAJTN0kQWcMa6dAMlc0SHGArbv2VyAZNYZGmYB79zgL0Ayl9UKsoCdgvzw+cc9" +
        "cmEt4FKDfQRIZkqVChZw2u1KAiTTPKWgBcwe8hAgmcnDXojgQEdnfF5ys7animCX3Y4Clcw3iSK40PT3BJLpPyxGBLcedZNAMjNGtxbB96qmEEjmbpcSIvjK" +
        "ti0Ekvmb82UzOPHjXALJLN50thm8w7svgWR63/A0gy1vyxNIpv3+eSZwwT3HeUjm62ELjOBCjR35z5IJxlebtrZ6zlw3TX07U9/O1Lcz9e1MfTtT387UtzP1" +
        "7cyvtp35RaapJ85109RNUzdN3TR109RNUzdN3TR10/w2pqlnz//LTHPrpjIyTPN01I9mcMm4ejJMc3S9+iL4ZFQTGaa5u+MCEdwkMFCGafr6/SSCdxfrKsM0" +
        "R7/KbgFfOtJNhmmOnuZpAXcM7izDNL0SGQt4yYs2Mkxz3bpOFrAU5i/DNJ9dGGEB775aQ4ZpFjwabQE/rucmwzRLFZxnAZ/L8V6CaXoNXWYB7wrfJ8E0D834" +
        "wQLePaedBNMc6JRgAS/cUNAC01y/KcECTvvkQVTTjKf8cdFGAtO823G5BZyt8DOaPT/UaoEF/DyfmwDTXJtnmgXcv0N9Aabp2iPcAq7cr4MA0yxwrLMFfL5+" +
        "mADTrBfXwAK+f3+KANNM9HKxgBNd5wkwzYuRN0SwfdXlAkyzWqO5InhQ6TUCTDPV9YYZPPjtJgGmeX3tTBbsvXuHANPcsW0SB57RZp8A07w48hYHfnc6CRlV" +
        "ruGpsjw4e21RgGm2P9GBB6eOkQSYZnLNaB7cfo0swDT7V4jjwf5bZQGmObPEAR68daUkwDQrPTrDg8cMFQWYZr3hj3iwY2mjANO8mJydgLvs2yfANA32BQl4" +
        "cPWdAkyzy203AmZnbRZgmm7tKxDw2eNrBJjmeK4GAV97uFyAaU5c6EvATPo8AabpzjQk4GJp0QJMs3WllgTclYwQYJouDTsQcKHlHQWY5pDx3Qi4yX5fAabZ" +
        "XOxDwHYr3QWY5s7XAwg4x4xXBKa5vMhQAn7dfxtRTTOUcvf04QSm2TDncMoXDlUnMM2eE0dQHtz0Bg/T3B4ykvLDhk15mOb8X0YS8Lh+LrRpfK+jIwl46u0Y" +
        "C0xzQt2RBFzi0CsLTDOg+ggC7htQQ4JpuuwNI+CVpfpJMM0WJ0IIeOSomRJMs9GYoURl1TQRZzXNrDFMM2sM08wawzSzxjDNrDFMM2sM08wawzSzxjDNrDFM" +
        "M2sM08wawzSzxjDNrDFM83O8MXGUCNOMDx5KwDVLd7fANCumDyFgj4GnLDDNy05DCHh/MRcJpjn15CACHlLNX4JpZvcaSMDSuBAJpulWrT8Bt4ycJcE0V1/o" +
        "S8A1usVLMM2BlXsTcO7AXRJM0+DVg4BP9T8swTQzbnYh4NmLL0gwzdBWHQmYv3ZXgmn2/749AQ8jrySY5p4urQmY6ZldhmlWtm9OwPZejjJMMza0EQGTqQVl" +
        "mObZRIGA3w1zlmGaG7cwBFzttpsM03wUVY+AL931kGGaw6r5ELBdeDm8v3MZO70IePWiijJMc3jRigR8gKsqwzSXdChDwC1Ge8swTW68OwHvZmri84Dzm1WM" +
        "gG/MryXDNHtOLUTAN4bXkWGaY4fnIeBuN+rKMM3ubRwIuPXVevj84Pa3+sCDzcH1ZZjmjN+f8uDE8fVlmOb5xbd4cK4y9WWYph+bxoNvtqsn006eL47z4HpF" +
        "6sowzbuimQfn7FlbhmmGbNjJg1s28JFhmj/sWc+DCydUx+cT5/x0OQ9uvtRLhmmGd53Lg7O7VZZhmv1zTOHBSytVkGGa+W6F8eDLR0vj84x71rorD/743F2G" +
        "aYp9GvHgFZtdZJimqbIPD97+zEmGaXrv8eDB/OG8+PzjuhTOz4ObVcghwzTzrnnFgVdU+iDBNO/Zn+fAi0s/lmCa5/vt4cDRta5KMM0e95Zy4H69UyWYZqmW" +
        "ERy4weaDEkzzWkwjDuxQcIME0yyy140Di7MXSTDNNTXusuCQ7yIlmGaXi6tZcLd9gRJM88nuNiy4TVAlCaY5KeUJAz7e8ZUFpvnmpw4MWBq2xALTHBPUyQ9c" +
        "UP5J/GyaYHz9R8NS9US6Lp369qa+valvb+rbm/r2pr69qW9v6tubX217869Kp55T16VTl05dOnXp1KVTl05dOnXp1KXzm0vn/0ivt3o3RYB0pqbl58DFhhAD" +
        "pHPaD3l48Pu1KwyQzkdtlvHgvnnPGCCdn6o4EPCqF9n8IZ2GsU0IuGlccX9I59rQ2QS8I4eXP6Sz6aIkAj75HecP6bw4+wkB78jRyh/S2fhGYQHcNK6rP6Tz" +
        "crPqAnj1i/7+kM6G9VsJ4B05wvwhnZW6hgjg8WfC/SGdi3ymCeDcXcb5QzpH5IwVwE3jJvir0rlJAPsqfyCds98eFMAuyvdAOoek/iSAQ5RjQDoftbkigEXl" +
        "HJDO7n0eCeBFyjVAOotcfi+Ag5RrhHRubZTDAF6nrAHS2bBaPgP4urJGSGf47MIG8HrlHkA6H7UpbgC3U+4RpLP5gJIGcKxyDyGdvKm0ATxUuceQzjCmvAH8" +
        "TPkZQDpjzn9HuZzyM4J0VhxeiXJj5WeoSmdlyrOX3qMPp+d+XdkA7uDwO304nX9XxQCe8/N7+nD6xZxeBvDRd3YGKp1u1QzgFGN2A6Tzt+relCt52RsgnZbG" +
        "1Sk3ap7dAOnc370G5RRXOwOVztCalOdWeUsfTk+J8KHcIfIpfTidj6hlAM+teYs+nL5scG0DeDY5pz6c3rKOATzrR5k+nL6hZF0DuM/3W+nD6Za0ugbwjJVL" +
        "6cPpoyPqGcBO3Djrw+n1DeCiR2fRh9MrL6lvAHtVOihAOo1dGxjA01Iu0PT6BeJrADdad5Om15f6+BnANcSrNL3u4ckYwPeKpdD0+uyXjAF8YvNqml63S2AN" +
        "4LIf2tP0+tIinAF8rM4MAukcUY8zgB8vuqg+nP6JNYB/Xh4nQzpHtGcN4In38yVDOi98xxjA+4LbJkM63wf7GsA/l5mXrEpnfQPYMXJPMqRz+dvaBnC+yAvJ" +
        "kM7g4JoGcN7I18k0vR5QzQA+zOU7BOlcHlvJAHa0uB+i0tmznAGcm/c6BOlcHlvKAM5jYQ5BOvsHFDeA8/MtD0E6+wc7GcD5Ld0PQTqXv3U0gAvyIYdU6cxu" +
        "AK/pNPMQpDNm8DMBnD583SFIZ+WuvwjgEXf3HYJ05k2TBbCwSz4E6Wx8Y4MATrx0+BCVzsg5Ajixk3QI0rk2dKAAJj57DkE695eqL4DDglcfgnSOKVpAAH98" +
        "Hn0I0tl6/zkC5nc1PATp/JQRR8DnX2Q7BOmM2z6GgP1u/ZAM6bzXpjUBn6taMBnSebUBQ8AR/SfQ9PosmSHgHZ1e04fTP3ItCfjXR0EGSKdvnsEEvKTrLPpw" +
        "ekW/aQS8zjHOAOlcE7qKgC9/WGOAdBZtuYuAw+qsMKjSmUJU/iydKcRWOlOIrXSmEFvpTCG20plCbKUzhdhKZwqxlc4UYiudKcRWOlOIrXSmEFvpTCG20plC" +
        "bKUzhdhKpxrb3/qZptdHPD1KwB8LJ9H0+o8zjxKwV7S3BOkceeoIATPjJ9D0up10mIBn1VtH0+tTex4i4CY7jtH0Or9TJuB7iY9oer33AQsBdyuQS4Z0lo8w" +
        "E/CNXC4ypNPyIomAx/IVZEjnAO8DBFx9fy0Z0hlUdS8B24UIMqRTerKTgKP7tpIhnb9GbCPge7FdaHr9/qnNBJy9QH+aXi/6OpGAa0lhNL2+7OkaAo7cNZam" +
        "1zeb4gh45PAoml6P7ruCgHduiqHp9e73lhDwxKgZNL0+uMkCAr74djZNr9+ePZuATYXm0/S63f5pBOx9dCFNr786PpmAE9yX0PT6+4XjCXh/4WU0vb48dAQB" +
        "n0qMpen1ejUHEvD3V5bT9Dq53p2A129fQdPrFSa0J+A+5VbS9Ho756YEvK3uSpper7uZI+ApD1bQ9DofUJuAf/NbQdPrVx5VJuBL1ZbT9HrEOk8CDrQso+n1" +
        "6O+LEXD3Z0toen1YcD4Cfiktoun1NWOzE7BLzQU0vV464hkPPkLm0vR68x7XeHBk+kyaXk/Y+xMPPs5No+l1u1kHeLBHtSk0vU4eJfJgh9ETaXpduL2EB18/" +
        "M4qm169HxvDg80MG0/T6vVVDefDdhj1pev3g3lY8OF/39jKk83E/Hx7cck8jGdLZ4HBxHrypWQOUo3HbrmRw4JKlq8pUOl+d5sCbapeUIZ3nHTdw4LbT88uQ" +
        "zvhmURy4dL13NL2++irhwGd/SaPp9R0tcnPgQh930fR63MrtLPjkqjk0vT7WtQkLrj66lQTpdLi7mwG37f2Rptf35NnnBy66vIrls3SC8dXD7k97yP/DTPuX" +
        "9OT/G8eP+MbH/6KZApXt/kqPfb0oQd8f1veH9f1hfX9Y3x/W94f1/WF9f1jfH/5q+8NfR0X1UgVdRXUV1VVUV1FdRXUV1VVUV1FdRf9DKqr3B/gvU9GrRs4A" +
        "FS0bncqB8zo3MtDu+iGteXBiUnMDVHToqR948HfbW9MChsvfX+DBSc/b0gKGY11yEbDX5Ha0gGHo3LIE7Ny7LS1gmLiSI+Bf57SiBQzlmnQh4IOOzQxQ0Yld" +
        "vifgiDP+BqjoL82nEfCG+34GqOir24sIeH9LHwNUtNyleAK+mq2iASr6fb4tBJyevYSBqmjvvQTcpW0+A1T0jFEk4MHXX1sLGFIIuFTAeVrA8KzESQLe772H" +
        "FjAUqnCegH+us4wWMLysd5mAS+0bRwsYaodcI+Bk31a0gGHArRsEXPrDd7SAodz22wS8/nVOtYAh/S4Bf+h6kUBFF5p+I2Afso0WMIz0eUB50MoFBCpaf8hD" +
        "yqWHRRGoaETUI8r5ho4nUNGf5j2mPHrUVAIVPbLjCeXAzSuJqqLplCf88opARVcXeE65ZpOatD9Am1wvKfs0aUr7AwQHvyHgQUMNtD+A74b3BPxgb0naH6B8" +
        "k08EXLfJTgIVHX84g4CdFpSzQEWXx2ZQbrUgH+2uf458JOBRvyyRoaJTGrwh4DG/vJKhotmU6wNzuwTaXd8jxy0C/jB8Ku2un/zyFAGHrV9Du+tfLJtEwOT3" +
        "PclqAUMCASd2Mqvd9SOnE3DiJSPtrp+2picBkxE7aHf9h21qEHBYhZW0u36RrbkIONvdiGS1gCGVB/vt+i7ZWsDAg+v7rKfd9R3TpvPgJbGzJajo/lLRPHhU" +
        "cD4BKnpjzBwevDZ2Ce2u33VuAg++nHpC7a5vZ+bBV1Mf0u76a0Ov8uB6sekCVNQj+iMP5n2u0O76a0PzE/C62K20u/7Yoq4EvC62lbW7vidR+bOKemoKGDw1" +
        "BQyemgIGT00Bg6emgMFTU8DgqSlg8NQUMHhqChg8NQUMnpoCBk9NAYOnpoDBU1PAoMYDo8qLand9DwL+aYOzRe2u70HAMwbNsqjd9T0IuO/Snyxqd/1SBMx4" +
        "OUhqd/2SBLy6c2lJ7a5fgoA92jGS2l3fjYDfF+soqd31ixNwrfVhktpdvxgB97GLltTu+kUJuPPCeZLaXd+JgO1mrZDU7voFCdjhwTpJ7a6fj4Dbbt0mqd31" +
        "HQk4+8X9ktpdPycBd+svSWp3fXsC7t/1mKR218/gwZ77Tklqd/03PHhKeJqkdtd/xoOXrroqqd31H/DgzpVuSWp3/Zs8+Kjbb5LaXf8yD74d9khSu+uf4cEn" +
        "vdIltbv+cR78XZvfJbW7vsyDW1x6Kand9Q/y4LJHX0tqd/2dPDjB7a2kdtffyIPPnH0rqd31V/PgHc/fSmp3/Vge3CD8raR215/Pg0f0eiOp3fWn8eCuO15J" +
        "anf9SB78JPiFpHbXD+XB1aOeSWp3/Z48uIz9E0ntrt+aB5vv35fU7vqEB+epc0dSu+vX4ME+965Lanf9Mjx4U8YlSe2uX5QHZ4w7K6nd9XPyYPueqZLaXf8Z" +
        "B9654ZCkdtdP48BOQUZJ7a5v5sClBu+S1O766zlw2u2Nktpdfy4HZg/FSWp3/ZEcONBxsaR212/OgV12T5fU7vrfceBC08dKanf9nBy49ajektpd/xQLvle1" +
        "saR211/Bgq9sqyqp3fU7s+DEjwUltbt+fha8w/u2Re2uH8+ALW/XWdTu+oUYcME9TS1qd/3CfuBCjaeLmd31p9P+ANX/kooC/2ZFw9c8YcS/+4RfVPNQ9184" +
        "oXfjXsP66gUQuurru876rrO+66zvOuu7zvqus77rrO86f8Vd52/gpXo1hO6lupfqXqp7qe6lupfqXqp7qe6l/xe8VC+N+C/z0tNhFWhvh6EzdrDgjWE1aW+H" +
        "8+l1OfB4d4b2dggOnsOBz4YFYBA0O6NNMgc+H9YSg53Z093fceDm7oEY1MzGxrry4HNhnTF4mb1woA4PvhDWHYOU2RW92/Pgie69aG+H4ODhPDgurDft7XC5" +
        "2TQefEH5Ay+d1HcpD8b3wEuVv0O//5xyDHjpsQZ7eHCCcg546b3jyTx4gnINqpee4cFnlWuEl7ZJvs6D1ytrgJe6fXrEg9X1pirX/5YHn1HuAbw0RsxOwGeU" +
        "ewQvvdwsNwEHubvS0ojg4PwEfCosDy2N+Mm+MAHH3XpB6OiE4sUI+Fm9M7Q0Qrk/lMNubaGlEcJYd8rLbk2hpRH8zlKUz4XVoqURPj6elJvO2cXDSw8+96R8" +
        "u8RRM7x05B1PAg53T7OoXupJwBHBn2hpRBhTjoAFHxMtjRh5uTrl0cEfaGmE81ZCwKdqfqSlEd3mtiXgLbEiLY3oENiDgBv75KKjE/Kl9SHgpbFDZXjp1Idd" +
        "CPhcat1keOnZgQYCbrBrYzK89G3FYgR8dpY97e3gaBfPg/PU60d7O9zOqGgBmwwJtLdDi8AgCZzv3I+0t8OHHikSOH/zxbS3Q0ZGXhl8eccY2tvBcaiXDJ58" +
        "ZSTt7fCqcEsZfKlGX9rbwS5yiAw+v6Mx7e1QffVkGVy9axna26HCyDkyeFWfm8mqly6RwQmdopLhpW07LpfBl1Lvy/DS4s1WyuCNwUvo6IQ8dVdSbuhzipZG" +
        "3BizXAZfbl7bAC/tMGqpDP61+RDa28EjeqEMXjorhvZ2eJV7rgxec2km7e2wsMcMGZyWL4b2dth6L1oG/9J8kLW3Q5Ss8mcvjZJtvTQzVr00M1a9NDNWvTQz" +
        "Vr00M1a9NDNWvTQzVr00M1a9NDNWvTQzVr00M1a9NDNWvTQzVr1UjV9GxPDwUvblJBncpOYIAi/tu3uSDPb0uk3gpTsbTJLB2QaXF+Cl/IxIGTz5WAcBXtqk" +
        "6UQZnOA6WYCXPnYbL4NnnI0V4KWxM8bI4C0Ltgnw0vGrwmVwnqBDArz0SNBIGRzveVGAl642hcrg4U8fCPBS5uwQGRyZ/EGAl6asHCCDy/6c0wAvDS3VVwav" +
        "MRUwwEt7d+0hg6O8ixngpSc6d5bB78uWNMBLr5cMlMFFVpQ1wEsPb2wlg48vqmSAl/6YvakMLuHkbYCXHq3qL4PzFqqF0jeOrcrK4Nj59QzwUiFHPRksL/Yz" +
        "UC811pDB0915A7x0ffsqMrheeQNK5bg+Z8vJ4PQdDQ3w0r5+pWTwHEsjA7w0baGLDE5t08QAL31wtZAMXtuvKUrruNRSeWRwiQ9NDbQ0orO9DK7u3MwAL+1V" +
        "5a0Evr69qQFe6rnhoQT2OtkEpXjcQ59rEtgttLEBXloy7ZQEXr0wwAAvLbI8WQIfre9vgJdWit4rgWf1Jijd4w7Gb5TAD/KzBnhpmZcrJfBvvg0M8FLzjHkS" +
        "+NLD2gZ46YOBUyTwQY8aBuqldcIk8NzzVQzw0qTmnSWwfYEKBnipsaK/BHY65mGAlyZdriaBTTldURrIhY5wk8C5jzoZ4KUL7HNJ4CeOeQzw0t1171vAI05m" +
        "M8BLP8UesYArez/H5yU3vNV6C3jdoesCvDQyYpoF3K1HqgAv9R7R2QJuk7FPgJe27l3JAh63NgGfr1x0zg8i+FbLWQK8NLXGDhHs/WGgAC8dsKyfCLYE1xfg" +
        "pa/3ZhfBjy7nxOcxl6PKYDPYfddaAi9t+2KoCezmfJ3/7KXgzzL1L3qpd1C/XqEhw/5WocS3O33Ef/b0WYooQmXb04f+8Wb5D7q5BegVErrx6zvR+k60vhOt" +
        "70TrO9H6TrS+E63vRH+1nei/Kp16+YMunbp06tKpS6cunbp06tKpS6cund9cOv9HbcOcW9Po3Irj9oU5cJg7bQvgR3YW4MG368XTuRXKjebB9+pdpHMrqhTM" +
        "TcBuR3PQuRXNB7Qi4OHuJenciuWx8wl4uHtNOrcisbhEwMWP+tO5FQ4LXhLwcPdAOrfCx8dFAA9370PnVghjawtgj6PD6NyKmW0CBfBw99F0bkVw8PcCeLj7" +
        "RDq3wrX2LAE899YkOrdiTcwqAVy/wGTr3IptAriG8gfS2S3CIoCXKt8D6bx3/LQA7qMcA9J5IvWmAJ6knAPS6Rv0uwB2Uq4B0um9OpsBPFG5Rkinj4+jARys" +
        "rAHSaWlRyACufNSfzq14eKWYATxAuQeQzhOp7gbwWOUeQTrHOpYxgIsp9xDSmX9iBQM4XbnHkM7lrpUN4MvKzwDSWfdwVcoTlZ8RpPPD0GqUY5SfoSqd3pQz" +
        "51Z4a+ZWVNfMraipmVvho5lbUUszt6K2Zm5FHc3cirqauRX1NHMr6mvmVjTQzK3w1cyt8NPMrfDTzK1gNHMrWM3cClYzt4LTzK3gNXMriGZuhaCZWyFo5lYY" +
        "NHMr/DVzK/w1cysMmrkVBs3cCkEzt4LXzK1gNXMrGmjmVtTVzK3w0cytqKaZW1FJM7eirGZuRUnN3IpimrkVBTRzK3Jp5la818ytuKOZW5GqmVuxUzO3Yplm" +
        "bsV4zdyKFpq5FWU0cys+aOZWHLWZW9G64zqbuRUvKs6zmVsRsirGZm5FwI0Ym7kVGR5zbeZWlIpepZlbsV0zt+KwZm7FL5q5FemauRXpmrYP6Zq2D+matg/p" +
        "mrYP6Zq2D+matg/pmrYP6Zq2D+matg/pmrYP6Zq2D+matg/pmrYP6Zq2D2r8g+ENbftQ7tFTAq4+/lfa9mHgyKcEfDKuLZ1bIS58QsAzdyxT51a0fEzAvxiO" +
        "0LYPuRMfEvCzXM9p24ddCfcJuOOn/DKk84r/PQL2cypD51b0nH+bgBP9atO5Fa6zbhDwoshGdG7F7XrXCDjX1Y50bsWKFZcJ2K7tQDq3osT+CwSc8/poOreC" +
        "X3CGgM3yZDq34o7XzwScLWEGnVtxZ84xAo7JP5/OrXA/cIiAW+RYSudW9N8uEnDH6Svp3Ir40QcJeE3sajq3Yq3zHgKuUWc9nVvRYuY2AnbosYnOrRh9dSMB" +
        "Oxfdqs6tKLSWgAcH7qBzK6QKPxBwlzK76dyKpeWXEfDVcXvp3IquhRYQ8Lve++nciicPZhJw5/MH6NyK7w5EE/C7swfp3IqjHycQ8NXuSXRuxceJwwk4x/dJ" +
        "dG7FRrtgAh7imkTnVkRO7UTAxZocpHMrfN1bEHCOHAfo3Ir5R3gCrtliH51bUX9OLQJeW3oPnVvxYExFAu4ctZPOrei2yJ2AW4Zso3MrGt0sRMBTf9tM51b0" +
        "GpKDgIMfJ9K5FckjX/Bgy8Q16tyKDdd4sLjqBzq34rdJx3hw9saxdG7Ffac9PDhmwkI6t2J4/9U8uDmZTedWPJs/hwcHzYuhcyu2kAgeLKRNoHMr3BsF8eBu" +
        "60Po3IrC7ery4IzZPejcioFTXHnKc1qqcytevOfAqev96NyK4HXHOPCHtEp0boWl80oO/GPpYjKkc86aARz4zuRsMqRz2LyyHHjMhzTa9qH807Ms+HDZzXRu" +
        "xdE2YSw4/tVoOrciR9FLDPinXMUlSOeI9b/4gYs/avHH3Aowvv6DuQ8Bmdn9gG8xt+JLjh/xjY//d+ZWZD1+ZvWDXpSg7w/r+8P6/rC+P6zvD+v7w/r+sL4/" +
        "rO8Pf7394a+jonqpgq6iuorqKqqrqK6iuorqKqqrqK6i/yEV1Zsz2OlzK/S5FfrcCn1uhT63wk6fW6HPrdDnVuhzK77O3Ip/oKK0PUHAt5hb8S+dMOLffcK/" +
        "M7fin55Q7figF0Doqq/vOuu7zvqus77rrO8667vO+q6zvuv89Xadv4GX6tUQupfqXqp7qe6lupfqXqp7qe6lupf+X/BSvTTCTp9boc+t0OdW/P8yt6JNgQ20" +
        "NKJCLRcZ7P9zFfRa4S43s5PBE40r/eGlnzK2SOAJ4zb5w0t/K19OAp9qMcEfXvpz+dsieMitQAO81G2/x15wWIWFdG7Fzy/dLeBE19a0t0PNt9EWsO+ubLS3" +
        "Q++5GyzgQcFDZHhpepv9FnDSiSE8vLRT4D4L+HHqIFoa4bI10QKekHqWlkasD51pAV9q7kR7O2T0bmIBj7pb3aCWRlwQwfyuxobPXqqy7qW6l+peqnvp/x0v" +
        "LfDZS5uHhPcbDtLT87pu6tug+jaovg2qb4Pquqnrpq6bum7+B3RTz7rruqnrpq6bum7quqnrpq6bum7quvkNdVNPpv+X6WbufsvRSJv9tK2cCL6dPUGmfQbE" +
        "DSLY8XoiGl2zGWWzWcB53m5G42o2I6OaBZy3zVY0omYzxnazgG/c3YrG0mxGwRgLOE/Sj2gUzWbUX2UB5z2xCY2flb+/ywLOU3I9GjmzGeWPW8C3N8XJ0M2M" +
        "kOsWcJ7wpTJ0M2P17xaw46TZMtVN3l4C5z42Saa6eS+vBL68aoBMdXNDEQmcv0JTmepmPzcJnLdyRVnVTU8JvLmZowzdXLqwnAQuYHdDgm4ubVdRApvYHRId" +
        "lNCoCuXV7SZL0M3Ko70oB4yvJtE+A4+qUS5gt9sC3RywxZvyuPTCItVNi7cELvVwKg/dzOHtLYFXDs1Pk+kts1WTwFPqt6PJ9J1sVQkcsmUGTabneFpJAt+/" +
        "s5H2GfBw+04CZ29ylPYZuHC0rATmL9y09hnwlMDxsTtoMp3NU1IC9w6uQ6CbHtElJPDY2Lc8dPPmGDcJnE35/6CbyS+LS+Blyt+BbmazKyaBT6V60mT6xTWF" +
        "JPCZ1CgBuhnzMJcEXhebLEA3F8gvLeBhwY4G6GZSqdMWMOuDIfTOnL1dvAXsuyvSAN3suL+OBVzm7gYDdHPcxbIm8OXmRwzQTfPL1jw4/O5FA3TzZcUrPNiw" +
        "61eDqpuVCVjYddUA3TxQqh8Bf3/3pAG6ua3RHAK+1HyfAbrZdv96Aq58d6EBurk29CABjwhuRpPp2exSCHj43WsCdNN52EkCjo+tKEA3w5gzBBzoEy5BN5Nr" +
        "nybgiOBRNJl+oNQJAk50FWWqm5YjBLzZ9ZEM3Wx8w0zAwZfskqGbbNO9BBx+N0OGbs6VfyRg/113ZejmI6f1BLzOda+s6mY8UfmzbsZr+gzEa/oMxGv6DMRr" +
        "+gzEa/oMxGv6DMRr+gzEa/oMxGv6DMRr+gzEa/oMxGv6DMRr+gzEa/oMxFv7DKyz9hmII+CfNiy29hmII+AZg/JJap+BOALuu7SZOiih1g8EzHhNsfYZWEXA" +
        "qzuvtvYZWEHAHu1M1j4Dywn4fbGL1j4Dywi41vpn1j4DSwi4j10OWe0zsIiAOy8sJKt9BuYTsN2sErLaZ2AuATs8KC+rfQZmEXDbrd6y2mdgOgFnv1hPVvsM" +
        "xBBwt/5EVvsMTCbg/l2byGqfgYkE7Lmvtaz2GRhNwFPCg2S1z0AYAS9d1U1W+wwMIuDOlfrIap+B3gR81G2AOijB0pmAb4cNkdU+A20J+KRXqKz2GWhKwN+1" +
        "GSGrfQYEAm5xaZSs9hloQMBlj4bLap+BmgSc4BYhq30GKhHwmbMRstpnoDQB73geIat9BlwJuEF4hKz2GXAi4BG9Rstqn4HcBNx1x/ey2mfAjoCfBI+U1T4D" +
        "6Ty4elSYrPYZ+JUHl7EfJqt9Bk7xYPP9gbLaZ0DiwXnq9JPVPgM7eLDPvR6y2mcggQdvyuikDkoIXcSDM8a1k9U+AzE82L5nc1ntMxDGg3du8JfVPgNBPNgp" +
        "yE9W+wywPLjUYB9Z7TNQgQen3a4kq30GCvJg9pCHrPYZeMGBAx2dZbXPQCoHdtntqA5KeJPIgQtNf2/tMxDDgVuPumntM9CaA9+rmmLtM1CCA1/ZtsXaZ+Ay" +
        "C078ONfaZ2A2C97h3VdS+wx4smDL2/KS2mdgHgMuuOe4tc/AAj9wocaOlsw+A44o5LIr8T9007v9gIH9B6hVncLfainwLx474hse+4saBZT958dWg795b/7m" +
        "OSL+Def4onvl/WXn8G42rF+/P3tN6f+e0bfP9e1zfftc3z7Xt8/17XN9+1zfPte3z//q9vlX9FG9nEP3Ud1HdR/VfVT3Ud1HdR/VfVT30f+kj+r1Hv9lPlpb" +
        "KEbrPRYHxJvBKbuL0XqP5MmeIrhCfhda79G0SJgIftDUhdZ73LgRL4LrDnOh9R5TDcdE8J3vXWi9R0ziMxHcp58LrfcYaJ/fAg6o70LrPby8SlvAW5/Twd3s" +
        "1Ba1LeBx84vReo/0nk0sYNdixWi9x40+3Szg4pHOtN7jTNswCzjuZFFa71FqcZQFPNuhKK33aDZilgXc0KMIrffwEpZYwH08CtN6j4yMOAu4YH9nWu9x8uVG" +
        "C/jpbHcZPrq5324LuOjHMjJ8tINBtIATdleU4aOLoo5bwIN3e8nw0fIuFyzgxx9qyPDRn1/csIC3z64tw0dbF3xiAV8IrifDR0M6v7OA609rIMNHX/W1l8Bd" +
        "n/vK8NEjGxwl8L61fjJ8dOrzghJ4bJyfDB919ysmgcfc9JXho/0mukvgOsMaoAaAa7i3jASuJtST4aMrLleUwHO715bho03vVpPA7ZNryPBRn7O1JLB3qJcM" +
        "H627ooEELhZcUYaP1q3HS+CCa8vI8FHntQ0lcFhld9o8YefFphLY921ROlci/WQrCTywUH46V2Lb5PYSOCPEgc6VOPW4owR+7/1Kgo/Wzd1NAncsdFOCj8qp" +
        "PSl3jELf9hJc+9p9KZun7JHgoyd9+1N+4rJKgo8WuTyQ8hPfyRJ8NFueIZQjbrSS4KPjDg+lvHVIMQk+2idnCOWtNXdb4KPLj4RQHsuliaqPhkrgDrlu8/DR" +
        "w/lCJPDK17EEPnpx7BAJ7Hz6PoGPNlgyQAKXbFSUzpV4vLaPBJ7mUoXOlXiW3k0Cp3aqT5snNE4OksCz8nO0ecKLvm0k8EUfPzpX4lWpZhJ47U/V6VyJVg0a" +
        "SuCG592tcyV4SeXPPspLtj6aGas+mhmrPpoZqz6aGas+mhmrPpoZqz6aGas+mhmrPpoZqz6aGas+mhmrPpoZqz6aGas+qsYTVmItPTn3dE4Cd17qw8NH28/j" +
        "JPCliRt4+GjsFVai3OoFT330IiOBt60sReCj8dF+EvhFHCHw0bm/NZDAs6r3IfDR2/b1JXCfxEkEPrrrch0J3O7jPAIfLRpaSwIXWh5H4KPuR2tI4LjxPxL4" +
        "6K2b1SSw3YaDBD467kgVCXy6+FFCfXRURQnsffoMgY9OeVZOAmc/dZXAR0typSVwgvNvBD56u2dJCeyc8IzARx91cpXAnUe9I/DRFl7OEnhvo2wCfNT9WiEJ" +
        "XPGNA5rjcGEh+SSwffPcAny05+1cEtjfkFegPsrbS+CnV/ML8NGTeT5awONKFEIzHS4o5HcLeMJHJwE+enTXfQu4V1QRAT7a9Np1C3jrpqICfDTfy/MWcK/v" +
        "ndF8h6v/6ScLeMIdZwE++ilDsoAd3jgL8NGpdvst4PQfnQXqozm3WsANczmjWQ+3pNhaC9jBoagAHx1Zb7kFXHFtYQE+ejRsngW8714hAT5qTImxgLf+XADN" +
        "fbj3MWMs4Hyd8gnw0eGn+lvAZ2McBfho7TGBFnB0UE4BPrpynsECjk3NLlAf9axpAdc89pHARw/UKm0BO/AvCXy09C+FLOA1Hx8S+Ogcj2wWcNHsNwl8dI/7" +
        "bREc1CKNwEdN/Y+K4AHXfyLw0dv1tojgZvtFAh/9ZcMCEZyRtpPARzfEjxLBUew6Qn10XhMRfOHhEgIf9WhTWgR/vD6VwEdTK78zg2/WDiXw0eHuB83gYfVb" +
        "Efjos0tRZvD7Y1UJfLTkrEpm8KD7uQl8tH4ByQTeXeUoDx9dtdHLRJkbwcNHu+73NoJ/Na7lPvsoGF/r/HUf9Q4ICdHT9rqW6tuk+japvk2qb5Pq26T6Nqm+" +
        "Tapvk37NbdKvr6V69l7XUl1LdS3VtVTXUl1LdS3VtVTX0v8DWqon8f/LtDTJx5lOQChSIY4Fjz1dhE5AqDOwFAeuIxWmExBKbB3CgadlL0wnICQGrOLACxc7" +
        "0QkINV0PcWCfMU50AsKY2w848KntTnQCQrnruXmwQ/3CdAJCre7uPPhp4SJ0AkK7d948OJQtSicg9DMbeHB6kjOdgNBmTxAPdpjpQicgvL8/mAef3uxKJyCs" +
        "9YnkwT6l3OkEhEIrZ/DgRTdL0QkIu3wW8+AZb0oL1iQ+D055V55OQDhQaDMPzhZQmU5AODBoLw/2euxFJyAUspd58Mj06nQCwtmfT/DgxFY+dAJCkeRLPLhJ" +
        "3tp0AsLtc3d48JUydQRoaae3z3hw9JI6ArR0cvlPPLhUcB0BWur7XQ4C/m5BbQFamlArLwG7u9USoKWTajsRcJVPNQRoac9yLgTsxXoL0NKU9+4EvORyFcGa" +
        "xCfg7clVBWhpzOiKBFzjbHU6AWFot2oE7BFdk05ASG1di4C9LtWgExCGsw0IuO2panQCQvXCPAEvGFiJNm1I29eQgA3rStOmDXVLNCPg4MkutGmDZ8XWBPza" +
        "Pq8ALe17vD0Bv2jxhkBLzzzuSMBXmt8n0FK/010pl+t1j0BLv9/Sk/L0gGsEWnqhZ1/Kye9PEGjpzpP9KTeYtJdAS3tfHkT5xYWlBFrq23so5V/f9iHQ0kWN" +
        "Qyh3uOtIoKWHhoVSzkjNwUNLPfeGUh5q3mSxJvEJuMuyYAlaWqjoMAImx5MkaOm554MIuPf417Rpw/UzwQR8uGYR2rQh5UEvAu5FytGmDWXDuhLw5AfVaNOG" +
        "6JUdCPicl48MLZ2ypjUBB+arQZs27DE2JWDH6Iq0acOWUg0JmF/mam3awBOV/0jia5o28JqmDbymaQOvadrAa5o28JqmDbymaQOvadrAa5o28JqmDbymaQOv" +
        "adrAa5o28JqmDWo8YWWAaE3iE3DnpT4WaxKfgC9N3GCxJvEJ5VYvLNYkPgFvW1lKsibxCfhFHJGsSXwk9JG4l6xJfCT0kbiXrEl8JPSRuJesSXwk9JG4l6xJ" +
        "fCT0kbiXrEl8JPSRuJesSXwk9JG4l6xJfCT0kbiXrEl8JPSRuJesSXwk9JG4l6xJfCT0kbiXrEl8JPSRuJesSXwk9JG4l61JfCT05YpvHGRrEp+A7Zvnlq1J" +
        "fAL2N+SVrUl8An56Nb9sTeLz4HElCsnWJD4PnvDRSbYm8Xlwr6gisjWJz4O3bioqW5P4PLjX986yNYnPgyfccZatSXwe7PDGWbYm8Xlw+o/OsjWJz4Mb5nKW" +
        "rUl8HuzgUFS2JvF5cMW1hWVrEp8H77tXSLYm8Xnw1p8LyNYkPg/O1ymfbE3i8+CzMY6yNYnPg6ODcsrWJD4Pjk3NLluT+Dy45rGPkjWJz4Md+JeSNYnPg9d8" +
        "fChZk/g8uGj2m5I1ic+Bg1qkSdYkPgcecP0nyZrE58DN9ouSNYnPgTPSdkrWJD4HjmLXSdYkPge+8HCJZE3ic+CP16dK1iQ+C75ZO1SyJvFZ8LD6rSRrEp8F" +
        "vz9WVbIm8VnwoPu5JWsSnwHvrnLUYk3iM5S5ERZrEt8P/KtxrZgliS/i6z9vfhDwDZs2BHzDpg0B37BpQ4BV1QO+YdOGPz1HxL/hHF+hacMf58jyz5oAvfpD" +
        "//eMvs2ub7Pr2+z6Nru+za5vs+vb7Po2+1fbZv+KPqqXfeg+qvuo7qO6j+o+qvuo7qO6j+o++p/0Ub3ew05v2qA3bdCbNuhNG/SmDXZ60wa9aYPetEFv2vBV" +
        "mzb8rz6qliHraXtdS/VtUn2bVN8m1bdJ9W1SfZtU3ybVt0m/3jbp19dSPXuva6mupbqW6lqqa6mupbqW6lqqa+n/AS3Vk/h2etMGvWmD3rRBb9qgN22w05s2" +
        "6E0b9KYNetOGv9i04f8BQAhiH378AQA=";

    const string TwoHandSlam2Data =
        "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" +
        "AA==";

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
