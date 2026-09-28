using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;

// 좌표 추출 덤프(사람이 "=== ... 좌표 추출 ===" 형태로 복붙해주는 텍스트)를 그대로 붙여넣으면
// MainBoss 2 리그에 그 포즈를 적용해주는 에디터 창.
// 검증용: 코드로 계산한 포즈가 맞는지, 반대로 이 창에 붙여넣어서 씬에서 직접 눈으로 확인할 때 씀.
public class MB2_PoseApplier : EditorWindow
{
    string _rootName = "MainBoss 2";
    string _input = "";
    bool _applyPosition = true;
    bool _applyRotation = true;
    Vector2 _scroll;
    string _status = "";

    [MenuItem("Tools/2/MainBoss2/Pose Applier (붙여넣기)")]
    static void Open()
    {
        var win = GetWindow<MB2_PoseApplier>("MB2 Pose Applier");
        win.minSize = new Vector2(420, 500);
    }

    void OnGUI()
    {
        GUILayout.Label("좌표 추출 텍스트 붙여넣기", EditorStyles.boldLabel);
        _rootName = EditorField("루트 오브젝트 이름", _rootName);

        EditorGUILayout.BeginHorizontal();
        _applyPosition = EditorGUILayout.ToggleLeft("Position 적용", _applyPosition, GUILayout.Width(150));
        _applyRotation = EditorGUILayout.ToggleLeft("Rotation.z 적용", _applyRotation, GUILayout.Width(150));
        EditorGUILayout.EndHorizontal();

        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(320));
        _input = EditorGUILayout.TextArea(_input, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("포즈 적용", GUILayout.Height(32)))
            ApplyPose();
        if (GUILayout.Button("지우기", GUILayout.Height(32), GUILayout.Width(80)))
        {
            _input = "";
            _status = "";
        }
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(_status))
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(_status, MessageType.Info);
        }
    }

    static string EditorField(string label, string value)
    {
        return EditorGUILayout.TextField(label, value);
    }

    class Entry
    {
        public string path;
        public Vector3? pos;
        public float? rotZ;
    }

    void ApplyPose()
    {
        var root = GameObject.Find(_rootName);
        if (root == null)
        {
            _status = $"'{_rootName}' 오브젝트를 씬에서 못 찾았어요.";
            return;
        }

        var entries = Parse(_input);
        if (entries.Count == 0)
        {
            _status = "파싱된 항목이 없어요. 형식을 확인해주세요 ([Path] 다음에 pos: / rot.z: 줄이 있어야 함).";
            return;
        }

        // 루트 아래 모든 Transform의 "전체 경로"(루트 제외, 슬래시 구분)를 미리 인덱싱해서
        // 붙여넣은 경로가 "Bottom/Thigh_B"든 "Body/Bottom/Thigh_B"든 "MainBoss 2/Body/..."든
        // 끝부분이 일치하는 실제 Transform을 찾아냄.
        var allPaths = new List<(string fullPath, Transform t)>();
        BuildPathIndex(root.transform, "", allPaths);

        int applied = 0, missed = 0;
        var missedNames = new List<string>();

        foreach (var e in entries)
        {
            string search = e.path;
            if (search.StartsWith(_rootName + "/")) search = search.Substring(_rootName.Length + 1);
            if (search == _rootName) search = "";

            Transform target = null;
            if (search == "")
            {
                target = root.transform;
            }
            else
            {
                // 끝부분이 일치하는 가장 긴(=가장 구체적인) 경로를 우선 채택
                (string fullPath, Transform t) best = default;
                int bestLen = -1;
                foreach (var (fullPath, t) in allPaths)
                {
                    if (fullPath == search || fullPath.EndsWith("/" + search))
                    {
                        if (fullPath.Length > bestLen) { bestLen = fullPath.Length; best = (fullPath, t); }
                    }
                }
                target = best.t;
            }

            if (target == null)
            {
                missed++;
                missedNames.Add(e.path);
                continue;
            }

            Undo.RecordObject(target, "MB2 Pose Applier");
            if (_applyPosition && e.pos.HasValue)
                target.localPosition = e.pos.Value;
            if (_applyRotation && e.rotZ.HasValue)
            {
                var euler = target.localEulerAngles;
                euler.z = e.rotZ.Value;
                target.localEulerAngles = euler;
            }
            applied++;
        }

        SceneView.RepaintAll();
        _status = $"적용됨: {applied}개, 못 찾음: {missed}개";
        if (missed > 0) _status += $"\n못 찾은 경로: {string.Join(", ", missedNames)}";
        Debug.Log($"[MB2 PoseApplier] {_status}");
    }

    static void BuildPathIndex(Transform t, string prefix, List<(string, Transform)> outList)
    {
        foreach (Transform child in t)
        {
            string p = prefix == "" ? child.name : prefix + "/" + child.name;
            outList.Add((p, child));
            BuildPathIndex(child, p, outList);
        }
    }

    // 텍스트 파싱: [Path] (뒤에 (layer: N) 등 부가정보는 무시) 다음에 나오는
    // pos: (x, y, z) / rot.z: value° 줄을 그 [Path]의 값으로 묶음.
    static List<Entry> Parse(string text)
    {
        var result = new List<Entry>();
        Entry current = null;

        var pathRe = new Regex(@"\[([^\]]+)\]");
        var posRe = new Regex(@"pos:\s*\(\s*(-?[\d.]+)\s*,\s*(-?[\d.]+)\s*,\s*(-?[\d.]+)\s*\)");
        var rotRe = new Regex(@"rot\.z:\s*(-?[\d.]+)");

        foreach (var rawLine in text.Split('\n'))
        {
            string line = rawLine.TrimEnd('\r');

            var pathMatch = pathRe.Match(line);
            if (pathMatch.Success)
            {
                if (current != null) result.Add(current);
                current = new Entry { path = pathMatch.Groups[1].Value.Trim() };
                continue;
            }

            if (current == null) continue;

            var posMatch = posRe.Match(line);
            if (posMatch.Success)
            {
                current.pos = new Vector3(
                    float.Parse(posMatch.Groups[1].Value),
                    float.Parse(posMatch.Groups[2].Value),
                    float.Parse(posMatch.Groups[3].Value));
                continue;
            }

            var rotMatch = rotRe.Match(line);
            if (rotMatch.Success)
            {
                current.rotZ = float.Parse(rotMatch.Groups[1].Value);
                continue;
            }
        }
        if (current != null) result.Add(current);

        // pos/rot 둘 다 없는(=헤더만 있고 값 못 찾은) 항목은 제외
        result.RemoveAll(e => !e.pos.HasValue && !e.rotZ.HasValue);
        return result;
    }
}
