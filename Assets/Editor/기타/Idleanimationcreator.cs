using UnityEngine;
using UnityEditor;

public class IdleAnimationCreator
{

    const string PATH_BODY = "Body";
    const string PATH_HEAD = "Body/Head";
    const string PATH_LOWERBODY = "Body/LowerBody";

    [MenuItem("Tools/Create Idle Animation (MidBoss)")]
    static void CreateIdleAnimation()
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = 60f;

        // ============ Body Position (호흡, 작게) ============
        // 회전 없이 위아래로만 조금씩
        AddPositionCurve(clip, PATH_BODY, new KeyVec[] {
            new KeyVec(0,   0, 0,       0),
            new KeyVec(50,  0, 0.025f,  0),  // 들이쉬기 peak (작게)
            new KeyVec(60,  0, 0.025f,  0),  // peak 유지
            new KeyVec(120, 0, 0,       0),  // 내쉬기 끝
        });

        // ============ LowerBody Position (Body.y 상쇄, 발 땅에 고정) ============
        // rest: (0.05, -0.6)
        AddPositionCurve(clip, PATH_LOWERBODY, new KeyVec[] {
            new KeyVec(0,   0.05f, -0.6f,   0),
            new KeyVec(50,  0.05f, -0.625f, 0),  // Body.y +0.025 상쇄
            new KeyVec(60,  0.05f, -0.625f, 0),
            new KeyVec(120, 0.05f, -0.6f,   0),
        });

        // ============ Head Rotation (살짝 끄덕임) ============
        AddRotationCurve(clip, PATH_HEAD, new KeyVec[] {
            new KeyVec(0,   0, 0, 0),
            new KeyVec(30,  0, 0, 1),    // 들이쉬기 중간, 머리 살짝 올라감
            new KeyVec(60,  0, 0, 0),
            new KeyVec(90,  0, 0, -1),   // 내쉬기 중간, 살짝 숙임
            new KeyVec(120, 0, 0, 0),
        });

        // ============ Loop 설정 ============
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.startTime = 0;
        settings.stopTime = 120f / 60f;
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

        string assetPath = folderPath + "/대기.anim";
        AnimationClip existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("덮어쓰기?",
                "이미 대기.anim이 존재합니다. 덮어쓸까요?", "예", "아니오"))
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

        Debug.Log($"대기 애니메이션 생성 완료: {assetPath}");
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