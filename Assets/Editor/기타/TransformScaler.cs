using UnityEditor;
using UnityEngine;

public class TransformScaler
{
    [MenuItem("Tools/기타/Scale Children Positions (×0.5)")]
    static void ScaleChildren()
    {
        if (Selection.gameObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("경고", "GameObject를 선택하세요", "확인");
            return;
        }

        foreach (var obj in Selection.gameObjects)
        {
            ScaleRecursive(obj.transform, 0.5f, includeRoot: false);
        }

        Debug.Log("완료: 자식 파츠들 Position이 1/2로 변환됨");
    }

    [MenuItem("Tools/기타/Scale Selected + Children Positions (×0.5)")]
    static void ScaleSelectedAndChildren()
    {
        if (Selection.gameObjects.Length == 0) return;

        foreach (var obj in Selection.gameObjects)
        {
            ScaleRecursive(obj.transform, 0.5f, includeRoot: true);
        }

        Debug.Log("완료: 선택+자식 Position이 1/2로 변환됨");
    }

    static void ScaleRecursive(Transform t, float scale, bool includeRoot)
    {
        if (includeRoot)
        {
            Undo.RecordObject(t, "Scale Position");
            t.localPosition *= scale;
        }

        foreach (Transform child in t)
        {
            Undo.RecordObject(child, "Scale Position");
            child.localPosition *= scale;
            ScaleRecursive(child, scale, false);
        }
    }
}