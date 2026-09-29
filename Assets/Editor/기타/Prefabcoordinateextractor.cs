using UnityEditor;
using UnityEngine;
using System.Text;
using System.IO;

public class PrefabCoordinateExtractor : EditorWindow
{
    [MenuItem("Tools/기타/Extract Prefab Coordinates")]
    static void OpenWindow()
    {
        GetWindow<PrefabCoordinateExtractor>("Coordinate Extractor");
    }

    GameObject target;
    bool includePosition = true;
    bool includeRotation = true;
    bool includeScale = false;
    bool includeSpriteInfo = true;
    bool relativePathFromRoot = true;
    int decimalPlaces = 3;
    Vector2 scrollPos;
    string lastResult = "";

    void OnGUI()
    {
        GUILayout.Label("Prefab 좌표 추출기", EditorStyles.boldLabel);
        GUILayout.Label("선택한 GameObject의 모든 자식 좌표를 텍스트로 추출", EditorStyles.miniLabel);
        GUILayout.Space(10);

        target = (GameObject)EditorGUILayout.ObjectField(
          "대상 (Root)",
          target,
          typeof(GameObject),
          true
        );

        GUILayout.Space(5);
        GUILayout.Label("포함 항목", EditorStyles.boldLabel);

        includePosition = EditorGUILayout.Toggle("Position", includePosition);
        includeRotation = EditorGUILayout.Toggle("Rotation (Euler Z)", includeRotation);
        includeScale = EditorGUILayout.Toggle("Scale", includeScale);
        includeSpriteInfo = EditorGUILayout.Toggle("Sprite 정보 (pivot, 크기)", includeSpriteInfo);
        relativePathFromRoot = EditorGUILayout.Toggle("Root 기준 경로 표시", relativePathFromRoot);
        decimalPlaces = EditorGUILayout.IntSlider("소수점 자릿수", decimalPlaces, 2, 6);

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("추출 + 클립보드 복사", GUILayout.Height(30)))
            Extract(true, false);

        if (GUILayout.Button("추출 + 텍스트 파일 저장", GUILayout.Height(30)))
            Extract(false, true);

        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("추출 (미리보기만)"))
            Extract(false, false);

        GUILayout.Space(10);

        if (!string.IsNullOrEmpty(lastResult))
        {
            GUILayout.Label("결과 미리보기:", EditorStyles.boldLabel);

            scrollPos = EditorGUILayout.BeginScrollView(
              scrollPos,
              GUILayout.MinHeight(300)
            );

            EditorGUILayout.TextArea(
              lastResult,
              GUILayout.ExpandHeight(true)
            );

            EditorGUILayout.EndScrollView();
        }
    }

    void Extract(bool copyToClipboard, bool saveToFile)
    {
        if (target == null)
            target = Selection.activeGameObject;

        if (target == null)
        {
            EditorUtility.DisplayDialog(
              "오류",
              "Hierarchy에서 대상 GameObject를 선택하거나 필드에 드래그하세요.",
              "확인"
            );
            return;
        }

        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"=== {target.name} 좌표 추출 ===");
        sb.AppendLine($"추출 시각: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        WriteTransform(
          sb,
          target.transform,
          target.transform,
          0
        );

        lastResult = sb.ToString();

        if (copyToClipboard)
        {
            EditorGUIUtility.systemCopyBuffer = lastResult;

            EditorUtility.DisplayDialog(
              "완료",
              "클립보드에 복사됨",
              "확인"
            );
        }

        if (saveToFile)
        {
            string path = EditorUtility.SaveFilePanel(
              "좌표 저장",
              "",
              $"{target.name}_coordinates.txt",
              "txt"
            );

            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(
                  path,
                  lastResult,
                  new UTF8Encoding(false)
                );

                EditorUtility.DisplayDialog(
                  "완료",
                  $"저장됨:\n{path}",
                  "확인"
                );
            }
        }

        Repaint();
    }

    void WriteTransform(
      StringBuilder sb,
      Transform root,
      Transform t,
      int depth
    )
    {
        string indent = new string(' ', depth * 2);
        string path = relativePathFromRoot
          ? GetRelativePath(root, t)
          : t.name;

        sb.Append($"{indent}[{path}]");

        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();

        if (sr != null && sr.sprite != null)
            sb.Append($"  (sorting_layer: {sr.sortingLayerName})");

        sb.AppendLine();

        string fmt = "F" + decimalPlaces;

        if (includePosition)
        {
            Vector3 p = t.localPosition;

            sb.AppendLine(
              $"{indent}  pos: ({p.x.ToString(fmt)}, {p.y.ToString(fmt)}, {p.z.ToString(fmt)})"
            );
        }

        if (includeRotation)
        {
            Vector3 e = t.localEulerAngles;
            float z = e.z;

            if (z > 180f)
                z -= 360f;

            sb.AppendLine(
              $"{indent}  rot.z: {z.ToString(fmt)}°"
            );

            if (Mathf.Abs(e.x) > 0.01f ||
                Mathf.Abs(e.y) > 0.01f)
            {
                sb.AppendLine(
                  $"{indent}  rot.xy: ({e.x.ToString(fmt)}, {e.y.ToString(fmt)})°"
                );
            }
        }

        if (includeScale)
        {
            Vector3 s = t.localScale;

            if (s != Vector3.one)
            {
                sb.AppendLine(
                  $"{indent}  scale: ({s.x.ToString(fmt)}, {s.y.ToString(fmt)}, {s.z.ToString(fmt)})"
                );
            }
        }

        if (includeSpriteInfo &&
            sr != null &&
            sr.sprite != null)
        {
            Sprite sprite = sr.sprite;

            sb.AppendLine(
              $"{indent}  sprite_size: {sprite.rect.width}x{sprite.rect.height} px"
            );

            sb.AppendLine(
              $"{indent}  pivot_normalized: (" +
              $"{(sprite.pivot.x / sprite.rect.width).ToString(fmt)}, " +
              $"{(sprite.pivot.y / sprite.rect.height).ToString(fmt)})"
            );
        }

        foreach (Transform child in t)
        {
            WriteTransform(
              sb,
              root,
              child,
              depth + 1
            );
        }
    }

    string GetRelativePath(
      Transform root,
      Transform target
    )
    {
        if (root == target)
            return target.name;

        string path = target.name;
        Transform t = target.parent;

        while (t != null && t != root)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }

        if (t == root)
            path = root.name + "/" + path;

        return path;
    }
}
