using UnityEngine;
using UnityEditor;

public class MidBossSlamCreator
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

    [MenuItem("Tools/Create MidBoss Slam Attack")]
    static void CreateSlamAttack()
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60f;

        // ============ 윗몸 동작 (도끼 깊이 찍기) ============

        // ShoulderJoint_B
        AddRotationCurve(clip, PATH_SHOULDER_B, new KeyVec[] {
            new KeyVec(0,  0, 0, SHOULDER_B_REST_Z),
            new KeyVec(8,  0, 0, 185),
            new KeyVec(15, 0, 0, 185),
            new KeyVec(20, 0, 0, 130),
            new KeyVec(25, 0, 0, 20),
            new KeyVec(30, 0, 0, 25),
            new KeyVec(38, 0, 0, 95),
            new KeyVec(50, 0, 0, SHOULDER_B_REST_Z),
        });

        // ShoulderJoint_F
        AddRotationCurve(clip, PATH_SHOULDER_F, new KeyVec[] {
            new KeyVec(0,  0, 0, SHOULDER_F_REST_Z),
            new KeyVec(8,  0, 0, 190),
            new KeyVec(15, 0, 0, 190),
            new KeyVec(20, 0, 0, 135),
            new KeyVec(25, 0, 0, 25),
            new KeyVec(30, 0, 0, 30),
            new KeyVec(38, 0, 0, 100),
            new KeyVec(50, 0, 0, SHOULDER_F_REST_Z),
        });

        // ArmJoint_B
        AddRotationCurve(clip, PATH_ARM_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(8,  0, 0, 25),
            new KeyVec(15, 0, 0, 30),
            new KeyVec(20, 0, 0, 30),
            new KeyVec(25, 0, 0, 5),
            new KeyVec(30, 0, 0, 10),
            new KeyVec(38, 0, 0, 10),
            new KeyVec(50, 0, 0, 0),
        });

        // ArmJoint_F
        AddRotationCurve(clip, PATH_ARM_F, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(8,  0, 0, 25),
            new KeyVec(15, 0, 0, 30),
            new KeyVec(20, 0, 0, 30),
            new KeyVec(25, 0, 0, 5),
            new KeyVec(30, 0, 0, 10),
            new KeyVec(38, 0, 0, 10),
            new KeyVec(50, 0, 0, 0),
        });

        // Hand_B (손목 꺾기)
        AddRotationCurve(clip, PATH_HAND_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(15, 0, 0, 10),
            new KeyVec(20, 0, 0, 5),
            new KeyVec(22, 0, 0, -35),
            new KeyVec(25, 0, 0, -75),
            new KeyVec(30, 0, 0, -65),
            new KeyVec(38, 0, 0, -25),
            new KeyVec(50, 0, 0, 0),
        });

        // Hand_F
        AddRotationCurve(clip, PATH_HAND_F, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(15, 0, 0, 10),
            new KeyVec(20, 0, 0, 5),
            new KeyVec(22, 0, 0, -35),
            new KeyVec(25, 0, 0, -75),
            new KeyVec(30, 0, 0, -65),
            new KeyVec(38, 0, 0, -25),
            new KeyVec(50, 0, 0, 0),
        });

        // Body Rotation
        AddRotationCurve(clip, PATH_BODY, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(8,  0, 0, 10),
            new KeyVec(15, 0, 0, 10),
            new KeyVec(20, 0, 0, 5),
            new KeyVec(25, 0, 0, -25),
            new KeyVec(30, 0, 0, -20),
            new KeyVec(38, 0, 0, -8),
            new KeyVec(50, 0, 0, 0),
        });

        // Body Position
        AddPositionCurve(clip, PATH_BODY, new KeyVec[] {
            new KeyVec(0,  0,      0,      0),
            new KeyVec(8,  -0.03f, 0,      0),
            new KeyVec(20, 0.04f,  0,      0),
            new KeyVec(25, 0.10f,  -0.08f, 0),
            new KeyVec(30, 0.08f,  -0.08f, 0),
            new KeyVec(38, 0.03f,  -0.03f, 0),
            new KeyVec(50, 0,      0,      0),
        });

        // ============ 다리 (단순화 - 무릎 살짝만 굽힘) ============

        // KneeJoint_F (squat 표현, 작게)
        AddRotationCurve(clip, PATH_KNEE_F, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(8,  0, 0, -5),
            new KeyVec(25, 0, 0, -15),
            new KeyVec(30, 0, 0, -12),
            new KeyVec(50, 0, 0, 0),
        });

        // KneeJoint_B (양쪽 동기화)
        AddRotationCurve(clip, PATH_KNEE_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(8,  0, 0, -5),
            new KeyVec(25, 0, 0, -15),
            new KeyVec(30, 0, 0, -12),
            new KeyVec(50, 0, 0, 0),
        });

        // ThighJoint, LowerBody 회전/위치 키는 박지 않음
        // → Stabilizer가 자동으로 LowerBody.rot.z, LowerBody.position 박음

        // ============ Head ============
        AddRotationCurve(clip, PATH_HEAD, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(8,  0, 0, -5),
            new KeyVec(25, 0, 0, 15),
            new KeyVec(50, 0, 0, 0),
        });

        // ============ 설정 ============
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        settings.startTime = 0;
        settings.stopTime = 50f / 60f;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // ============ 저장 ============
        string folderPath = "Assets/Animations/MidBoss";
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
        {
            AssetDatabase.CreateFolder("Assets", "Animations");
        }
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Animations", "MidBoss");
        }

        string assetPath = folderPath + "/찍기.anim";
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("덮어쓰기?",
                "이미 찍기.anim이 존재합니다. 덮어쓸까요?", "예", "아니오"))
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

        Debug.Log($"중간보스 찍기 (단순화) 생성 완료. 이제 Stabilizer 적용하세요: {assetPath}");
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