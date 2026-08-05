using UnityEngine;
using UnityEditor;

public class MidBossAttackCreator
{

    const string PATH_BODY = "Body";
    const string PATH_HEAD = "Body/Head";
    const string PATH_LOWERBODY = "Body/LowerBody";
    const string PATH_SHOULDER_F = "Body/ShoulderJoint_F";
    const string PATH_SHOULDER_B = "Body/ShoulderJoint_B";
    const string PATH_ARM_F = "Body/ShoulderJoint_F/Shoulder_F/ArmJoint_F";
    const string PATH_ARM_B = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B";
    const string PATH_THIGH_F = "Body/LowerBody/ThighJoint_F";
    const string PATH_KNEE_F = "Body/LowerBody/ThighJoint_F/Thigh_F/KneeJoint_F";

    [MenuItem("Tools/Create MidBoss Attack Animation")]
    static void CreateAttackAnimation()
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60f;

        // ============ ShoulderJoint_B (메인 휘두름) ============
        AddRotationCurve(clip, PATH_SHOULDER_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(8,  0, 0, 25),
            new KeyVec(20, 0, 0, 70),
            new KeyVec(25, 0, 0, 65),
            new KeyVec(30, 0, 0, 45),
            new KeyVec(37, 0, 0, -40),
            new KeyVec(42, 0, 0, -32),
            new KeyVec(50, 0, 0, -15),
            new KeyVec(60, 0, 0, 0),
        });

        // ============ ArmJoint_B ============
        AddRotationCurve(clip, PATH_ARM_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(8,  0, 0, 30),
            new KeyVec(20, 0, 0, 80),
            new KeyVec(25, 0, 0, 78),
            new KeyVec(30, 0, 0, 55),
            new KeyVec(37, 0, 0, -20),
            new KeyVec(42, 0, 0, -15),
            new KeyVec(50, 0, 0, -5),
            new KeyVec(60, 0, 0, 0),
        });

        // ============ ShoulderJoint_F (회전) ============
        AddRotationCurve(clip, PATH_SHOULDER_F, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(8,  0, 0, 20),
            new KeyVec(20, 0, 0, 55),
            new KeyVec(25, 0, 0, 50),
            new KeyVec(30, 0, 0, 35),
            new KeyVec(37, 0, 0, -35),
            new KeyVec(42, 0, 0, -28),
            new KeyVec(50, 0, 0, -12),
            new KeyVec(60, 0, 0, 0),
        });

        // ============ ArmJoint_F (회전, 더 크게 굽힘) ============
        AddRotationCurve(clip, PATH_ARM_F, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(8,  0, 0, 45),
            new KeyVec(20, 0, 0, 110),     // peak: 팔꿈치 크게 굽혀서 손 안쪽으로
            new KeyVec(25, 0, 0, 105),
            new KeyVec(30, 0, 0, 75),
            new KeyVec(37, 0, 0, -25),
            new KeyVec(42, 0, 0, -18),
            new KeyVec(50, 0, 0, -8),
            new KeyVec(60, 0, 0, 0),
        });

        // ============ ArmJoint_F Position (도끼 자루 쪽으로 이동) ============
        // rest: (-0.39, -0.63) (Shoulder_F 기준)
        // 도끼 자루 위치는 B쪽이라 F팔 입장에서 +x(앞), 살짝 위로 이동해야 자루에 닿음
        AddPositionCurve(clip, PATH_ARM_F, new KeyVec[] {
            new KeyVec(0,  -0.39f, -0.63f, 0),  // rest
            new KeyVec(8,  -0.25f, -0.55f, 0),  // 도끼 잡으러 가는 중
            new KeyVec(20, -0.15f, -0.45f, 0),  // peak: 도끼 자루 근처
            new KeyVec(25, -0.15f, -0.45f, 0),
            new KeyVec(30, -0.20f, -0.50f, 0),
            new KeyVec(37, -0.30f, -0.55f, 0),  // impact: 살짝 떨어짐 (휘둘러지면서)
            new KeyVec(42, -0.35f, -0.60f, 0),
            new KeyVec(50, -0.38f, -0.62f, 0),
            new KeyVec(60, -0.39f, -0.63f, 0),
        });

        // ============ Body Rotation ============
        AddRotationCurve(clip, PATH_BODY, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(8,  0, 0, 5),
            new KeyVec(20, 0, 0, 15),
            new KeyVec(25, 0, 0, 15),
            new KeyVec(30, 0, 0, 8),
            new KeyVec(37, 0, 0, -20),
            new KeyVec(42, 0, 0, -18),
            new KeyVec(50, 0, 0, -10),
            new KeyVec(60, 0, 0, 0),
        });

        // ============ Body Position ============
        AddPositionCurve(clip, PATH_BODY, new KeyVec[] {
            new KeyVec(0,  0,      0,      0),
            new KeyVec(20, -0.03f, 0,      0),
            new KeyVec(37, 0.08f,  -0.03f, 0),
            new KeyVec(42, 0.06f,  -0.03f, 0),
            new KeyVec(60, 0,      0,      0),
        });

        // ============ LowerBody Position ============
        AddPositionCurve(clip, PATH_LOWERBODY, new KeyVec[] {
            new KeyVec(0,  0.05f, -0.6f,  0),
            new KeyVec(37, 0.05f, -0.57f, 0),
            new KeyVec(42, 0.05f, -0.57f, 0),
            new KeyVec(60, 0.05f, -0.6f,  0),
        });

        // ============ LowerBody Rotation ============
        AddRotationCurve(clip, PATH_LOWERBODY, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(20, 0, 0, -7),
            new KeyVec(37, 0, 0, 10),
            new KeyVec(60, 0, 0, 0),
        });

        // ============ ThighJoint_F ============
        AddRotationCurve(clip, PATH_THIGH_F, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(20, 0, 0, 0),
            new KeyVec(37, 0, 0, 10),
            new KeyVec(42, 0, 0, 10),
            new KeyVec(60, 0, 0, 0),
        });

        // ============ KneeJoint_F ============
        AddRotationCurve(clip, PATH_KNEE_F, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(20, 0, 0, -10),
            new KeyVec(37, 0, 0, -15),
            new KeyVec(42, 0, 0, -15),
            new KeyVec(60, 0, 0, 0),
        });

        // ============ Head ============
        AddRotationCurve(clip, PATH_HEAD, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(20, 0, 0, -8),
            new KeyVec(37, 0, 0, 12),
            new KeyVec(60, 0, 0, 0),
        });

        // ============ 설정 ============
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        settings.startTime = 0;
        settings.stopTime = 60f / 60f;
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

        string assetPath = folderPath + "/공격.anim";
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("덮어쓰기?",
                "이미 공격.anim이 존재합니다. 덮어쓸까요?", "예", "아니오"))
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

        Debug.Log($"중간보스 공격 애니메이션 생성 완료: {assetPath}");
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