using UnityEngine;
using UnityEditor;

public class PlayerAttackCreator
{

    const string PATH_BODY = "Body";
    const string PATH_HEAD = "Body/Head";
    const string PATH_SHOULDER_F = "Body/ShoulderJoint_F";
    const string PATH_SHOULDER_B = "Body/ShoulderJoint_B";
    const string PATH_ARM_F = "Body/ShoulderJoint_F/Shoulder_F/ArmJoint_F";
    const string PATH_ARM_B = "Body/ShoulderJoint_B/Shoulder_B/ArmJoint_B";
    const string PATH_LEG_F = "Body/LegJoint_F";
    const string PATH_LEG_B = "Body/LegJoint_B";

    const float BODY_REST_Y = 7f;
    const float LEG_F_REST_X = 0.225f;
    const float LEG_F_REST_Y = -0.475f;
    const float LEG_B_REST_X = -0.125f;
    const float LEG_B_REST_Y = -0.475f;

    [MenuItem("Tools/Create Player Attack Animation")]
    static void CreateAttack()
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60f;

        // ============ ShoulderJoint_B (왼팔, 크게) ============
        AddRotationCurve(clip, PATH_SHOULDER_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(5,  0, 0, -35),
            new KeyVec(12, 0, 0, -110),
            new KeyVec(15, 0, 0, -105),
            new KeyVec(18, 0, 0, -55),
            new KeyVec(22, 0, 0, 60),
            new KeyVec(26, 0, 0, 52),
            new KeyVec(32, 0, 0, 20),
            new KeyVec(40, 0, 0, 0),
        });

        // ============ ArmJoint_B (왼 팔꿈치, 크게) ============
        AddRotationCurve(clip, PATH_ARM_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(5,  0, 0, -30),
            new KeyVec(12, 0, 0, -90),
            new KeyVec(15, 0, 0, -85),
            new KeyVec(18, 0, 0, -55),
            new KeyVec(22, 0, 0, 25),
            new KeyVec(26, 0, 0, 18),
            new KeyVec(32, 0, 0, 5),
            new KeyVec(40, 0, 0, 0),
        });

        // ============ ShoulderJoint_F (오른팔, 살짝만) ============
        AddRotationCurve(clip, PATH_SHOULDER_F, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(12, 0, 0, -15),     // peak 시 살짝 따라감
            new KeyVec(15, 0, 0, -13),
            new KeyVec(22, 0, 0, 10),      // impact 시 살짝 반대
            new KeyVec(26, 0, 0, 8),
            new KeyVec(40, 0, 0, 0),
        });

        // ============ ArmJoint_F (오른 팔꿈치, 살짝만) ============
        AddRotationCurve(clip, PATH_ARM_F, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(12, 0, 0, -15),
            new KeyVec(15, 0, 0, -13),
            new KeyVec(22, 0, 0, 8),
            new KeyVec(26, 0, 0, 6),
            new KeyVec(40, 0, 0, 0),
        });

        // ============ Body Rotation (작은 가슴 휘둘림) ============
        AddRotationCurve(clip, PATH_BODY, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(12, 0, 0, 8),
            new KeyVec(15, 0, 0, 8),
            new KeyVec(22, 0, 0, -10),
            new KeyVec(26, 0, 0, -8),
            new KeyVec(40, 0, 0, 0),
        });

        // ============ Body Position (제자리, 위아래 작게) ============
        AddPositionCurve(clip, PATH_BODY, new KeyVec[] {
            new KeyVec(0,  0, BODY_REST_Y,        0),
            new KeyVec(22, 0, BODY_REST_Y-0.02f,  0),
            new KeyVec(26, 0, BODY_REST_Y-0.02f,  0),
            new KeyVec(40, 0, BODY_REST_Y,        0),
        });

        // ============ LegJoint_F Position (Body.y 상쇄로 발 절대 y 고정) ============
        AddPositionCurve(clip, PATH_LEG_F, new KeyVec[] {
            new KeyVec(0,  LEG_F_REST_X, LEG_F_REST_Y,        0),
            new KeyVec(22, LEG_F_REST_X, LEG_F_REST_Y+0.02f,  0),
            new KeyVec(26, LEG_F_REST_X, LEG_F_REST_Y+0.02f,  0),
            new KeyVec(40, LEG_F_REST_X, LEG_F_REST_Y,        0),
        });

        // ============ LegJoint_B Position ============
        AddPositionCurve(clip, PATH_LEG_B, new KeyVec[] {
            new KeyVec(0,  LEG_B_REST_X, LEG_B_REST_Y,        0),
            new KeyVec(22, LEG_B_REST_X, LEG_B_REST_Y+0.02f,  0),
            new KeyVec(26, LEG_B_REST_X, LEG_B_REST_Y+0.02f,  0),
            new KeyVec(40, LEG_B_REST_X, LEG_B_REST_Y,        0),
        });

        // ============ LegJoint_F Rotation (-Body.rot.z, 발 회전 상쇄) ============
        AddRotationCurve(clip, PATH_LEG_F, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(12, 0, 0, -8),       // Body +8 상쇄
            new KeyVec(15, 0, 0, -8),
            new KeyVec(22, 0, 0, 10),       // Body -10 상쇄
            new KeyVec(26, 0, 0, 8),
            new KeyVec(40, 0, 0, 0),
        });

        // ============ LegJoint_B Rotation (-Body.rot.z, 발 회전 상쇄) ============
        AddRotationCurve(clip, PATH_LEG_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(12, 0, 0, -8),
            new KeyVec(15, 0, 0, -8),
            new KeyVec(22, 0, 0, 10),
            new KeyVec(26, 0, 0, 8),
            new KeyVec(40, 0, 0, 0),
        });

        // ============ Head (시선 안정화, 작게) ============
        AddRotationCurve(clip, PATH_HEAD, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(12, 0, 0, -4),
            new KeyVec(22, 0, 0, 6),
            new KeyVec(40, 0, 0, 0),
        });

        // ============ 설정 ============
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        settings.startTime = 0;
        settings.stopTime = 40f / 60f;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // ============ 저장 ============
        string folderPath = "Assets/Animations/Player";
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
        {
            AssetDatabase.CreateFolder("Assets", "Animations");
        }
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Animations", "Player");
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

        Debug.Log($"플레이어 공격 애니메이션 생성 완료: {assetPath}");
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