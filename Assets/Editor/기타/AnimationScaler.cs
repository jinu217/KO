using UnityEditor;
using UnityEngine;

public class AnimationScaler
{
    [MenuItem("Tools/Scale Selected Animation Positions (×0.5)")]
    static void ScaleSelected()
    {
        int clipCount = 0;
        int keyCount = 0;

        foreach (var obj in Selection.objects)
        {
            if (obj is AnimationClip clip)
            {
                keyCount += ScaleClip(clip, 0.5f);
                clipCount++;
            }
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"완료: {clipCount}개 클립, {keyCount}개 키 변환됨");
    }

    static int ScaleClip(AnimationClip clip, float scale)
    {
        int count = 0;
        var bindings = AnimationUtility.GetCurveBindings(clip);

        foreach (var binding in bindings)
        {
            // Position 채널만 (Rotation은 그대로)
            if (binding.propertyName == "m_LocalPosition.x" ||
                binding.propertyName == "m_LocalPosition.y" ||
                binding.propertyName == "m_LocalPosition.z")
            {

                var curve = AnimationUtility.GetEditorCurve(clip, binding);
                var keys = curve.keys;
                for (int i = 0; i < keys.Length; i++)
                {
                    keys[i].value *= scale;
                    keys[i].inTangent *= scale;
                    keys[i].outTangent *= scale;
                    count++;
                }
                curve.keys = keys;
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }
        }
        return count;
    }
}