using UnityEngine;
using UnityEditor;

public class EBasicMobAnimations
{

    const string PATH_BODY = "Body";
    const string PATH_HEAD = "Body/Head";
    const string PATH_SHOULDER_F = "Body/ShoulderJoint_F";
    const string PATH_SHOULDER_B = "Body/ShoulderJoint_B";
    const string PATH_ARM_F = "Body/ShoulderJoint_F/Shoulder_F/ArmJoint_F";
    const string PATH_ARM_B = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B";
    const string PATH_HAND_B = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B/Hand_B";
    const string PATH_LEG_F = "Body/LegJoint_F";
    const string PATH_LEG_B = "Body/LegJoint_B";
    const string PATH_FOOT_F = "Body/LegJoint_F/Foot_F";
    const string PATH_FOOT_B = "Body/LegJoint_B/Foot_B";

    // ============================================================
    // 대기 (호흡)
    // ============================================================
    [MenuItem("Tools/Create EBasicMob Idle Animation")]
    static void CreateIdle()
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60f;

        // Body Y (호흡)
        AddPositionCurve(clip, PATH_BODY, new KeyVec[] {
            new KeyVec(0,  0, 0,      0),
            new KeyVec(20, 0, -0.02f, 0),
            new KeyVec(40, 0, 0,      0),
            new KeyVec(60, 0, -0.02f, 0),
        });

        // Head
        AddRotationCurve(clip, PATH_HEAD, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(20, 0, 0, 1),
            new KeyVec(40, 0, 0, -1),
            new KeyVec(60, 0, 0, 0),
        });

        // ShoulderJoint_F (호흡으로 들썩)
        AddRotationCurve(clip, PATH_SHOULDER_F, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(20, 0, 0, -2),
            new KeyVec(40, 0, 0, 1),
            new KeyVec(60, 0, 0, 0),
        });

        // ShoulderJoint_B (무기 든 팔)
        AddRotationCurve(clip, PATH_SHOULDER_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(20, 0, 0, 2),
            new KeyVec(40, 0, 0, -1),
            new KeyVec(60, 0, 0, 0),
        });

        // Hand_B (무기 흔들림)
        AddRotationCurve(clip, PATH_HAND_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(20, 0, 0, 2),
            new KeyVec(40, 0, 0, -2),
            new KeyVec(60, 0, 0, 0),
        });

        SaveClip(clip, "대기", 60f, true);
    }

    // ============================================================
    // 걷기 (유동적 발 동작, 진행 방향 +x)
    // ============================================================
    [MenuItem("Tools/Create EBasicMob Walk Animation")]
    static void CreateWalk()
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60f;

        // ============ LegJoint_F (정면 다리 swing, 양수=앞으로) ============
        AddRotationCurve(clip, PATH_LEG_F, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(15, 0, 0, 25),                      // 앞 정점
            new KeyVec(30, 0, 0, 0),                       // 디딤 (중립)
            new KeyVec(45, 0, 0, -25),                     // 뒤 정점
            new KeyVec(60, 0, 0, 0),
        });

        // ============ LegJoint_B (반대 phase) ============
        AddRotationCurve(clip, PATH_LEG_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(15, 0, 0, -25),                     // 뒤 정점
            new KeyVec(30, 0, 0, 0),
            new KeyVec(45, 0, 0, 25),                      // 앞 정점
            new KeyVec(60, 0, 0, 0),
        });

        // ============ Foot_F (유동적 발 동작) ============
        // 앞 정점(F=15): 발끝 살짝 들림 (heel strike 준비)
        // 디딤(F=22): 평평
        // 뒤 정점(F=45): 뒤꿈치 들림, 발끝 아래 (toe-off)
        AddRotationCurve(clip, PATH_FOOT_F, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(8,  0, 0, -10),                     // swing 중 발끝 들기 (걸림 방지)
            new KeyVec(15, 0, 0, -15),                     // 앞 정점, 발끝 위 (절대 +10°)
            new KeyVec(22, 0, 0, -5),                      // heel strike → 평평으로 가는 중
            new KeyVec(30, 0, 0, 0),                       // 평평 디딤
            new KeyVec(38, 0, 0, 10),                      // 뒤꿈치 떼기 시작
            new KeyVec(45, 0, 0, 20),                      // 뒤 정점, 발끝으로 미는 (절대 -5°)
            new KeyVec(52, 0, 0, 10),                      // 발이 들림
            new KeyVec(60, 0, 0, 0),                       // 평평으로 복귀
        });

        // ============ Foot_B (반대 phase 동일 패턴) ============
        AddRotationCurve(clip, PATH_FOOT_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(8,  0, 0, 10),
            new KeyVec(15, 0, 0, 20),                      // 뒤 정점, toe-off (절대 -5°)
            new KeyVec(22, 0, 0, 10),
            new KeyVec(30, 0, 0, 0),                       // 평평 디딤
            new KeyVec(38, 0, 0, -10),
            new KeyVec(45, 0, 0, -15),                     // 앞 정점, 발끝 위 (절대 +10°)
            new KeyVec(52, 0, 0, -5),
            new KeyVec(60, 0, 0, 0),
        });

        // ============ ShoulderJoint_F (다리 반대 phase) ============
        AddRotationCurve(clip, PATH_SHOULDER_F, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(15, 0, 0, -20),                     // F다리 앞일 때 F팔 뒤
            new KeyVec(30, 0, 0, 0),
            new KeyVec(45, 0, 0, 20),
            new KeyVec(60, 0, 0, 0),
        });

        // ============ ShoulderJoint_B (F다리와 같은 phase) ============
        AddRotationCurve(clip, PATH_SHOULDER_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(15, 0, 0, 20),
            new KeyVec(30, 0, 0, 0),
            new KeyVec(45, 0, 0, -20),
            new KeyVec(60, 0, 0, 0),
        });

        // ============ ArmJoint_F (살짝 굽힘 유지) ============
        AddRotationCurve(clip, PATH_ARM_F, new KeyVec[] {
            new KeyVec(0,  0, 0, 5),
            new KeyVec(15, 0, 0, 10),
            new KeyVec(30, 0, 0, 5),
            new KeyVec(45, 0, 0, 10),
            new KeyVec(60, 0, 0, 5),
        });

        // ============ ArmJoint_B (무기 든 팔) ============
        AddRotationCurve(clip, PATH_ARM_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 10),
            new KeyVec(15, 0, 0, 5),
            new KeyVec(30, 0, 0, 10),
            new KeyVec(45, 0, 0, 5),
            new KeyVec(60, 0, 0, 10),
        });

        // ============ Body Y (걸음마다 위아래, 자연스러운 발걸음 무게) ============
        // 디딤 직전 위로, 디딤 순간 아래로
        AddPositionCurve(clip, PATH_BODY, new KeyVec[] {
            new KeyVec(0,  0, 0,      0),                  // 디딤 중간
            new KeyVec(8,  0, 0.01f,  0),                  // 다리 swing 중 살짝 위로
            new KeyVec(15, 0, 0,      0),                  // 발 디딜 직전
            new KeyVec(22, 0, -0.02f, 0),                  // 디딤 무게 받음 (가라앉음)
            new KeyVec(30, 0, 0,      0),                  // 다음 디딤
            new KeyVec(38, 0, 0.01f,  0),                  // swing 중
            new KeyVec(45, 0, 0,      0),
            new KeyVec(52, 0, -0.02f, 0),
            new KeyVec(60, 0, 0,      0),
        });

        // ============ Body Rotation (살짝 좌우 흔들림) ============
        AddRotationCurve(clip, PATH_BODY, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(15, 0, 0, -2),                      // F다리 앞일 때 살짝 비틂
            new KeyVec(30, 0, 0, 0),
            new KeyVec(45, 0, 0, 2),
            new KeyVec(60, 0, 0, 0),
        });

        // ============ Head (시선 안정화) ============
        AddRotationCurve(clip, PATH_HEAD, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(15, 0, 0, 2),                       // Body 회전 상쇄
            new KeyVec(30, 0, 0, 0),
            new KeyVec(45, 0, 0, -2),
            new KeyVec(60, 0, 0, 0),
        });

        // ============ Hand_B (걸을 때 무기 흔들림) ============
        AddRotationCurve(clip, PATH_HAND_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(15, 0, 0, 5),
            new KeyVec(30, 0, 0, 0),
            new KeyVec(45, 0, 0, -5),
            new KeyVec(60, 0, 0, 0),
        });

        SaveClip(clip, "걷기", 60f, true);
    }

    // ============================================================
    static void SaveClip(AnimationClip clip, string name, float lastFrame, bool loop)
    {
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        settings.startTime = 0;
        settings.stopTime = lastFrame / 60f;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string folderPath = "Assets/Animations/EBasicMob";
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
        {
            AssetDatabase.CreateFolder("Assets", "Animations");
        }
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Animations", "EBasicMob");
        }

        string assetPath = folderPath + "/" + name + ".anim";
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("덮어쓰기?",
                $"이미 {name}.anim이 존재합니다. 덮어쓸까요?", "예", "아니오"))
            {
                return;
            }
            AssetDatabase.DeleteAsset(assetPath);
        }

        AssetDatabase.CreateAsset(clip, assetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = clip;
        EditorGUIUtility.PingObject(clip);

        Debug.Log($"EBasicMob {name} 생성 완료: {assetPath}");
    }

    struct KeyVec
    {
        public float frame;
        public float x, y, z;
        public KeyVec(float f, float x, float y, float z)
        {
            this.frame = f; this.x = x; this.y = y; this.z = z;
        }
        public float Time(float frameRate) { return frame / frameRate; }
    }

    static void AddPositionCurve(AnimationClip clip, string path, KeyVec[] keys)
    {
        AnimationCurve curveX = new AnimationCurve();
        AnimationCurve curveY = new AnimationCurve();
        AnimationCurve curveZ = new AnimationCurve();

        foreach (var k in keys)
        {
            float t = k.Time(60f);
            curveX.AddKey(t, k.x);
            curveY.AddKey(t, k.y);
            curveZ.AddKey(t, k.z);
        }

        SetAutoTangents(curveX);
        SetAutoTangents(curveY);
        SetAutoTangents(curveZ);

        clip.SetCurve(path, typeof(Transform), "m_LocalPosition.x", curveX);
        clip.SetCurve(path, typeof(Transform), "m_LocalPosition.y", curveY);
        clip.SetCurve(path, typeof(Transform), "m_LocalPosition.z", curveZ);
    }

    static void AddRotationCurve(AnimationClip clip, string path, KeyVec[] keys)
    {
        AnimationCurve curveX = new AnimationCurve();
        AnimationCurve curveY = new AnimationCurve();
        AnimationCurve curveZ = new AnimationCurve();

        foreach (var k in keys)
        {
            float t = k.Time(60f);
            curveX.AddKey(t, k.x);
            curveY.AddKey(t, k.y);
            curveZ.AddKey(t, k.z);
        }

        SetAutoTangents(curveX);
        SetAutoTangents(curveY);
        SetAutoTangents(curveZ);

        clip.SetCurve(path, typeof(Transform), "localEulerAnglesRaw.x", curveX);
        clip.SetCurve(path, typeof(Transform), "localEulerAnglesRaw.y", curveY);
        clip.SetCurve(path, typeof(Transform), "localEulerAnglesRaw.z", curveZ);
    }

    static void SetAutoTangents(AnimationCurve curve)
    {
        for (int i = 0; i < curve.keys.Length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Auto);
        }
    }
}