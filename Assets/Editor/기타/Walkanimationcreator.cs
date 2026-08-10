using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class WalkAnimationCreator
{

    const string PATH_BODY = "Body";
    const string PATH_HEAD = "Body/Head";
    const string PATH_LOWERBODY = "Body/LowerBody";
    const string PATH_SHOULDER_F = "Body/ShoulderJoint_F";
    const string PATH_SHOULDER_B = "Body/ShoulderJoint_B";
    const string PATH_THIGH_F = "Body/LowerBody/ThighJoint_F";
    const string PATH_THIGH_B = "Body/LowerBody/ThighJoint_B";
    const string PATH_KNEE_F = "Body/LowerBody/ThighJoint_F/Thigh_F/KneeJoint_F";
    const string PATH_KNEE_B = "Body/LowerBody/ThighJoint_B/Thigh_B/KneeJoint_B";

    [MenuItem("Tools/Create Walk Animation (MidBoss)")]
    static void CreateWalkAnimation()
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60f;

        // ThighJoint_F (앞다리, 부호 반전)
        AddRotationCurve(clip, PATH_THIGH_F, new KeyVec[] {
            new KeyVec(0,  0, 0, 15),
            new KeyVec(7,  0, 0, 8),
            new KeyVec(15, 0, 0, 0),
            new KeyVec(22, 0, 0, -13),
            new KeyVec(30, 0, 0, -25),
            new KeyVec(37, 0, 0, 10),
            new KeyVec(45, 0, 0, 30),
            new KeyVec(52, 0, 0, 25),
            new KeyVec(60, 0, 0, 15),
        });

        // ThighJoint_B
        AddRotationCurve(clip, PATH_THIGH_B, new KeyVec[] {
            new KeyVec(0,  0, 0, -25),
            new KeyVec(7,  0, 0, 10),
            new KeyVec(15, 0, 0, 30),
            new KeyVec(22, 0, 0, 25),
            new KeyVec(30, 0, 0, 15),
            new KeyVec(37, 0, 0, 8),
            new KeyVec(45, 0, 0, 0),
            new KeyVec(52, 0, 0, -13),
            new KeyVec(60, 0, 0, -25),
        });

        // KneeJoint_F (부호 반전)
        AddRotationCurve(clip, PATH_KNEE_F, new KeyVec[] {
            new KeyVec(0,  0, 0, 0),
            new KeyVec(7,  0, 0, 0),
            new KeyVec(15, 0, 0, 0),
            new KeyVec(22, 0, 0, -10),
            new KeyVec(30, 0, 0, -20),
            new KeyVec(37, 0, 0, -35),
            new KeyVec(45, 0, 0, -40),
            new KeyVec(52, 0, 0, -15),
            new KeyVec(60, 0, 0, 0),
        });

        // KneeJoint_B
        AddRotationCurve(clip, PATH_KNEE_B, new KeyVec[] {
            new KeyVec(0,  0, 0, -20),
            new KeyVec(7,  0, 0, -35),
            new KeyVec(15, 0, 0, -40),
            new KeyVec(22, 0, 0, -15),
            new KeyVec(30, 0, 0, 0),
            new KeyVec(37, 0, 0, 0),
            new KeyVec(45, 0, 0, 0),
            new KeyVec(52, 0, 0, -10),
            new KeyVec(60, 0, 0, -20),
        });

        // Body Position (바운스, 그대로)
        AddPositionCurve(clip, PATH_BODY, new KeyVec[] {
            new KeyVec(0,  0, -0.02f, 0),
            new KeyVec(15, 0, 0.02f,  0),
            new KeyVec(30, 0, -0.02f, 0),
            new KeyVec(45, 0, 0.02f,  0),
            new KeyVec(60, 0, -0.02f, 0),
        });

        // LowerBody (골반 흔들림, 부호 반전)
        AddRotationCurve(clip, PATH_LOWERBODY, new KeyVec[] {
            new KeyVec(0,  0, 0, 3),
            new KeyVec(15, 0, 0, 0),
            new KeyVec(30, 0, 0, -3),
            new KeyVec(45, 0, 0, 0),
            new KeyVec(60, 0, 0, 3),
        });

        // ShoulderJoint_F (부호 반전)
        AddRotationCurve(clip, PATH_SHOULDER_F, new KeyVec[] {
            new KeyVec(0,  0, 0, -5),
            new KeyVec(15, 0, 0, 0),
            new KeyVec(30, 0, 0, 5),
            new KeyVec(45, 0, 0, 0),
            new KeyVec(60, 0, 0, -5),
        });

        // ShoulderJoint_B
        AddRotationCurve(clip, PATH_SHOULDER_B, new KeyVec[] {
            new KeyVec(0,  0, 0, 5),
            new KeyVec(15, 0, 0, 0),
            new KeyVec(30, 0, 0, -5),
            new KeyVec(45, 0, 0, 0),
            new KeyVec(60, 0, 0, 5),
        });

        // Head (그대로)
        AddRotationCurve(clip, PATH_HEAD, new KeyVec[] {
            new KeyVec(0,  0, 0, 2),
            new KeyVec(15, 0, 0, -1),
            new KeyVec(30, 0, 0, 2),
            new KeyVec(45, 0, 0, -1),
            new KeyVec(60, 0, 0, 2),
        });

        // Loop 설정
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.startTime = 0;
        settings.stopTime = 60f / 60f;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // 저장
        string folderPath = "Assets/Animations/MidBoss";
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
        {
            AssetDatabase.CreateFolder("Assets", "Animations");
        }
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Animations", "MidBoss");
        }

        string assetPath = folderPath + "/Walk.anim";
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("덮어쓰기?",
                "이미 걷기.anim이 존재합니다. 덮어쓸까요?", "예", "아니오"))
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

        Debug.Log($"걷기 애니메이션 생성 완료: {assetPath}");
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