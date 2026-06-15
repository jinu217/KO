using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class AttackAnimationCreator
{

    [MenuItem("Tools/Create Attack Animation")]
    static void CreateAttackAnimation()
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60f;

        // ============ Body Position ============
        AddPositionCurve(clip, "Body", new KeyVec[] {
            new KeyVec(0,  0,     0,    0),
            new KeyVec(6,  -0.03f, 0.03f, 0),
            new KeyVec(12, 0.08f, -0.04f, 0),
            new KeyVec(19, 0,     0,    0),
        });

        // ============ Body Rotation ============
        AddRotationCurve(clip, "Body", new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(6,  0, 0, -3),
            new KeyVec(12, 0, 0, 12),
            new KeyVec(19, 0, 0, 0),
        });

        // ============ Head Rotation ============
        AddRotationCurve(clip, "Body/Head", new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(6,  0, 0, 5),
            new KeyVec(12, 0, 0, -8),
            new KeyVec(19, 0, 0, 0),
        });

        // ============ ShoulderJoint_F Rotation (앞 어깨 반동) ============
        AddRotationCurve(clip, "Body/ShoulderJoint_F", new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(6,  0, 0, -8),
            new KeyVec(12, 0, 0, 8),
            new KeyVec(19, 0, 0, 0),
        });

        // ============ ArmJoint_F Rotation (앞팔 균형) ============
        AddRotationCurve(clip, "Body/ShoulderJoint_F/ArmJoint_F", new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(6,  0, 0, -15),
            new KeyVec(12, 0, 0, 15),
            new KeyVec(19, 0, 0, 0),
        });

        // ============ ShoulderJoint_B Rotation (견갑, ±15°) ============
        AddRotationCurve(clip, "Body/ShoulderJoint_B", new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(6,  0, 0, 12),
            new KeyVec(12, 0, 0, -12),
            new KeyVec(19, 0, 0, 0),
        });

        // ============ ArmJoint_B Rotation (검 휘두름 핵심) ============
        AddRotationCurve(clip, "Body/ShoulderJoint_B/ArmJoint_B", new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(3,  0, 0, 60),
            new KeyVec(6,  0, 0, 130),
            new KeyVec(7,  0, 0, 125),
            new KeyVec(9,  0, 0, 50),
            new KeyVec(11, 0, 0, -50),
            new KeyVec(12, 0, 0, -75),
            new KeyVec(15, 0, 0, -40),
            new KeyVec(19, 0, 0, 0),
        });

        // ============ LegJoint_F Position (Body.y 상쇄, 발 고정) ============
        // rest: (-0.2, -0.475)
        AddPositionCurve(clip, "Body/LegJoint_F", new KeyVec[] {
            new KeyVec(0,  -0.2f, -0.475f, 0),
            new KeyVec(6,  -0.2f, -0.505f, 0),
            new KeyVec(12, -0.2f, -0.435f, 0),
            new KeyVec(19, -0.2f, -0.475f, 0),
        });

        // ============ LegJoint_B Position ============
        // rest: (0.15, -0.475)
        AddPositionCurve(clip, "Body/LegJoint_B", new KeyVec[] {
            new KeyVec(0,  0.15f, -0.475f, 0),
            new KeyVec(6,  0.15f, -0.505f, 0),
            new KeyVec(12, 0.15f, -0.435f, 0),
            new KeyVec(19, 0.15f, -0.475f, 0),
        });

        // 저장
        string folderPath = "Assets/Animations";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Animations");
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

        // 생성된 Clip 선택
        Selection.activeObject = clip;
        EditorGUIUtility.PingObject(clip);

        Debug.Log($"공격 애니메이션 생성 완료: {assetPath}");
    }

    // 키프레임 데이터 구조
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

        // Euler 각도로 키 저장 (Animation 창에 Rotation으로 표시됨)
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