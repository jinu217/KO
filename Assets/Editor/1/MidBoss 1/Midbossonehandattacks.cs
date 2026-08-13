using UnityEngine;
using UnityEditor;

public class MidBossOneHandAttacks
{

    const string PATH_BODY = "Body";
    const string PATH_HEAD = "Body/Head";
    const string PATH_SHOULDER_F = "Body/ShoulderJoint_F";
    const string PATH_SHOULDER_B = "Body/ShoulderJoint_B";
    const string PATH_ARM_F = "Body/ShoulderJoint_F/Shoulder_F/ArmJoint_F";
    const string PATH_ARM_B = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B";
    const string PATH_HAND_F = "Body/ShoulderJoint_F/Shoulder_F/ArmJoint_F/Arm_F/Hand_F";
    const string PATH_HAND_B = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B/Arm_B/Hand_B";
    const string PATH_KNEE_F = "Body/LowerBody/ThighJoint_F/Thigh_F/KneeJoint_F";
    const string PATH_KNEE_B = "Body/LowerBody/ThighJoint_B/Thigh_B/KneeJoint_B";

    const float SHOULDER_B_REST_Z = 170f;
    const float SHOULDER_F_REST_Z = 175f;

    // ============================================================
    // 약한 공격 (한팔 빠른 베기)
    // ============================================================
    [MenuItem("Tools/Create MidBoss Weak Attack (OneHand)")]
    static void CreateWeakAttack()
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60f;

        // ============ ShoulderJoint_B (메인, 빠른 사선 베기) ============
        AddRotationCurve(clip, PATH_SHOULDER_B, new KeyVec[] {
            new KeyVec(0,  0, 0, SHOULDER_B_REST_Z),       // rest
            new KeyVec(6,  0, 0, 178),                     // 작은 anticipation
            new KeyVec(10, 0, 0, 178),                     // hold
            new KeyVec(15, 0, 0, 130),                     // 베기 시작
            new KeyVec(18, 0, 0, 70),                      // impact (사선 아래)
            new KeyVec(22, 0, 0, 80),                      // 잔동
            new KeyVec(30, 0, 0, SHOULDER_B_REST_Z),       // rest 복귀
        });

        // ============ ShoulderJoint_F (F팔 정지, rest 유지) ============
        AddRotationCurve(clip, PATH_SHOULDER_F, new KeyVec[] {
            new KeyVec(0,  0, 0, SHOULDER_F_REST_Z),
            new KeyVec(30, 0, 0, SHOULDER_F_REST_Z),
        });

        // ============ ArmJoint_B (살짝 굽혀 가속) ============
        AddRotationCurve(clip, PATH_ARM_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(6,  0, 0, 10),
            new KeyVec(10, 0, 0, 15),
            new KeyVec(18, 0, 0, 5),                       // impact 시 살짝 펴짐
            new KeyVec(30, 0, 0, 0),
        });

        // ============ ArmJoint_F (정지) ============
        // 키 박지 않음 (rest 유지)

        // ============ Hand_B (손목 꺾기) ============
        AddRotationCurve(clip, PATH_HAND_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(15, 0, 0, -10),
            new KeyVec(18, 0, 0, -30),                     // impact
            new KeyVec(22, 0, 0, -25),
            new KeyVec(30, 0, 0, 0),
        });

        // ============ Body Rotation (작은 트위스트) ============
        AddRotationCurve(clip, PATH_BODY, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(6,  0, 0, 5),                       // 살짝 뒤로
            new KeyVec(18, 0, 0, -5),                      // impact (앞으로 살짝)
            new KeyVec(30, 0, 0, 0),
        });

        // ============ Body Position (작은 체중 이동) ============
        AddPositionCurve(clip, PATH_BODY, new KeyVec[] {
            new KeyVec(0,  0,      0,     0),
            new KeyVec(6,  -0.02f, 0,     0),
            new KeyVec(18, 0.03f,  -0.02f, 0),             // 살짝 앞 + 가라앉음
            new KeyVec(30, 0,      0,     0),
        });

        // ============ KneeJoint_F (작은 굽힘) ============
        AddRotationCurve(clip, PATH_KNEE_F, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(18, 0, 0, -5),
            new KeyVec(30, 0, 0, 0),
        });

        // ============ KneeJoint_B (양쪽 동기화) ============
        AddRotationCurve(clip, PATH_KNEE_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(18, 0, 0, -5),
            new KeyVec(30, 0, 0, 0),
        });

        // ============ Head ============
        AddRotationCurve(clip, PATH_HEAD, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(18, 0, 0, 5),
            new KeyVec(30, 0, 0, 0),
        });

        SaveClip(clip, "약한공격", 30f);
    }

    // ============================================================
    // 강한 공격 (한팔 큰 휘두름)
    // ============================================================
    [MenuItem("Tools/Create MidBoss Strong Attack (OneHand)")]
    static void CreateStrongAttack()
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60f;

        // ============ ShoulderJoint_B (큰 휘두름) ============
        AddRotationCurve(clip, PATH_SHOULDER_B, new KeyVec[] {
            new KeyVec(0,  0, 0, SHOULDER_B_REST_Z),       // rest (170)
            new KeyVec(10, 0, 0, 205),                     // 큰 anticipation (뒤로 크게)
            new KeyVec(18, 0, 0, 205),                     // hold (긴장)
            new KeyVec(23, 0, 0, 150),                     // 베기 시작
            new KeyVec(28, 0, 0, 100),                     // 가속
            new KeyVec(32, 0, 0, 30),                      // impact (거의 정 아래)
            new KeyVec(36, 0, 0, 35),                      // 잔동
            new KeyVec(45, 0, 0, SHOULDER_B_REST_Z),       // rest 복귀
        });

        // ============ ShoulderJoint_F (살짝 보조) ============
        AddRotationCurve(clip, PATH_SHOULDER_F, new KeyVec[] {
            new KeyVec(0,  0, 0, SHOULDER_F_REST_Z),
            new KeyVec(10, 0, 0, 180),                     // 살짝 보조 (균형 잡기)
            new KeyVec(32, 0, 0, 165),                     // impact 시 살짝 내림
            new KeyVec(45, 0, 0, SHOULDER_F_REST_Z),
        });

        // ============ ArmJoint_B (큰 굽힘으로 가속) ============
        AddRotationCurve(clip, PATH_ARM_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(10, 0, 0, 25),                      // 크게 굽힘
            new KeyVec(18, 0, 0, 30),                      // hold
            new KeyVec(28, 0, 0, 20),
            new KeyVec(32, 0, 0, 5),                       // impact 시 펴짐 (도끼 멀리)
            new KeyVec(36, 0, 0, 10),
            new KeyVec(45, 0, 0, 0),
        });

        // ============ ArmJoint_F (살짝 보조) ============
        AddRotationCurve(clip, PATH_ARM_F, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(10, 0, 0, 10),
            new KeyVec(32, 0, 0, 5),
            new KeyVec(45, 0, 0, 0),
        });

        // ============ Hand_B (손목 크게 꺾기) ============
        AddRotationCurve(clip, PATH_HAND_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(18, 0, 0, 5),
            new KeyVec(23, 0, 0, 0),
            new KeyVec(28, 0, 0, -25),                     // 꺾기 시작
            new KeyVec(32, 0, 0, -60),                     // impact (크게 꺾임)
            new KeyVec(36, 0, 0, -50),
            new KeyVec(45, 0, 0, 0),
        });

        // ============ Body Rotation (큰 트위스트 + 숙임) ============
        AddRotationCurve(clip, PATH_BODY, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(10, 0, 0, 10),                      // 뒤로 활처럼
            new KeyVec(18, 0, 0, 10),
            new KeyVec(32, 0, 0, -15),                     // impact (가슴 숙임)
            new KeyVec(36, 0, 0, -12),
            new KeyVec(45, 0, 0, 0),
        });

        // ============ Body Position (체중 이동) ============
        AddPositionCurve(clip, PATH_BODY, new KeyVec[] {
            new KeyVec(0,  0,      0,      0),
            new KeyVec(10, -0.04f, 0,      0),             // 뒤로 (anticipation)
            new KeyVec(28, 0.04f,  0,      0),
            new KeyVec(32, 0.08f,  -0.04f, 0),             // 앞 + 가라앉음
            new KeyVec(36, 0.06f,  -0.04f, 0),
            new KeyVec(45, 0,      0,      0),
        });

        // ============ KneeJoint_F (squat 살짝) ============
        AddRotationCurve(clip, PATH_KNEE_F, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(10, 0, 0, -5),
            new KeyVec(32, 0, 0, -10),
            new KeyVec(36, 0, 0, -8),
            new KeyVec(45, 0, 0, 0),
        });

        // ============ KneeJoint_B (양쪽 동기화) ============
        AddRotationCurve(clip, PATH_KNEE_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(10, 0, 0, -5),
            new KeyVec(32, 0, 0, -10),
            new KeyVec(36, 0, 0, -8),
            new KeyVec(45, 0, 0, 0),
        });

        // ============ Head ============
        AddRotationCurve(clip, PATH_HEAD, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(10, 0, 0, -5),
            new KeyVec(32, 0, 0, 10),
            new KeyVec(45, 0, 0, 0),
        });

        SaveClip(clip, "강한공격", 45f);
    }

    // ============================================================
    static void SaveClip(AnimationClip clip, string name, float lastFrame)
    {
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        settings.startTime = 0;
        settings.stopTime = lastFrame / 60f;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        string folderPath = "Assets/Animations/MidBoss";
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
        {
            AssetDatabase.CreateFolder("Assets", "Animations");
        }
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Animations", "MidBoss");
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

        Debug.Log($"중간보스 {name} 생성 완료. Stabilizer 적용하세요: {assetPath}");
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