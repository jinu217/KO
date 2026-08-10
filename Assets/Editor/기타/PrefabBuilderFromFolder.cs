using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class PrefabBuilderWindow : EditorWindow
{
    string _folderPath = "Assets/Sprites/";
    string _prefabPath = "Assets/Prefab";

    [MenuItem("Tools/Prefab/Build Prefab From Sprite Folder")]
    public static void ShowWindow()
    {
        var win = GetWindow<PrefabBuilderWindow>("Prefab Builder");
        win.minSize = new Vector2(400, 150);
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("스프라이트 폴더 경로", EditorStyles.boldLabel);
        _folderPath = EditorGUILayout.TextField("Sprite Folder", _folderPath);
        _prefabPath = EditorGUILayout.TextField("Prefab Save Path", _prefabPath);

        GUILayout.Space(10);

        // 드래그한 폴더 받기
        var dropArea = GUILayoutUtility.GetRect(0, 40, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "또는 폴더를 여기에 드래그");
        var evt = Event.current;
        if ((evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform) &&
            dropArea.Contains(evt.mousePosition))
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                foreach (var path in DragAndDrop.paths)
                {
                    if (AssetDatabase.IsValidFolder(path))
                    { _folderPath = path; break; }
                }
            }
            evt.Use();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("프리팹 생성", GUILayout.Height(30)))
            Build();
    }

    // 스프라이트 이름 → (부모경로, SortingLayer, OrderInLayer)
    static readonly Dictionary<string, (string parent, string layer, int order)> HIERARCHY = new()
    {
        // 추출값 기준 sortingOrder
        { "Body",       ("root",                    "Body",  0) },
        { "Head",       ("Body",                    "Body",  1) },
        { "Shoulder_F", ("Body",                    "Front", 2) },
        { "Hand_F",     ("Body/Shoulder_F",         "Front", 3) },
        { "Shoulder_B", ("Body",                    "Back",  2) },
        { "Hand_B",     ("Body/Shoulder_B",         "Back",  1) },
        { "Bottom",     ("Body",                    "",      0) },  // SpriteRenderer 없음
        { "Foot_F",     ("Body/Bottom",             "Front", 1) },
        { "Foot_B",     ("Body/Bottom",             "Back",  1) },
        // 무기류
        { "Club",       ("Body/Shoulder_B/Hand_B",  "Back",  0) },
        { "Weapon",     ("Body/Shoulder_B/Hand_B",  "Back",  0) },
        { "Sword",      ("Body/Shoulder_B/Hand_B",  "Back",  0) },
    };

    void Build()
    {
        if (!AssetDatabase.IsValidFolder(_folderPath))
        {
            EditorUtility.DisplayDialog("오류", $"폴더를 찾을 수 없습니다:\n{_folderPath}", "확인");
            return;
        }

        string charName = Path.GetFileName(_folderPath);

        // 스프라이트 수집
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { _folderPath });
        var sprites = new Dictionary<string, Sprite>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) sprites[sprite.name] = sprite;
        }

        if (sprites.Count == 0)
        {
            EditorUtility.DisplayDialog("오류", "해당 폴더에 스프라이트가 없습니다.", "확인");
            return;
        }

        // 프리팹 폴더 확인
        if (!AssetDatabase.IsValidFolder(_prefabPath))
            AssetDatabase.CreateFolder("Assets", Path.GetFileName(_prefabPath));

        var root = new GameObject(charName);
        var goMap = new Dictionary<string, GameObject> { { "root", root } };

        foreach (var kvp in HIERARCHY)
        {
            string spriteName = kvp.Key;
            var (parentPath, layer, order) = kvp.Value;

            if (!goMap.TryGetValue(parentPath, out var parentGo)) continue;

            var go = new GameObject(spriteName);
            go.transform.SetParent(parentGo.transform, false);
            goMap[spriteName] = go;
            string fullPath = parentPath == "root" ? spriteName : parentPath + "/" + spriteName;
            goMap[fullPath] = go;

            if (!string.IsNullOrEmpty(layer))
            {
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingLayerName = layer;
                sr.sortingOrder = order;
                if (sprites.TryGetValue(spriteName, out var sprite))
                    sr.sprite = sprite;
                else
                    Debug.LogWarning($"[PrefabBuilder] 스프라이트 없음: {spriteName}");
            }
        }

        string prefabSavePath = $"{_prefabPath}/{charName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabSavePath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();

        Debug.Log($"[PrefabBuilder] {charName} → {prefabSavePath}");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(prefabSavePath);
        EditorUtility.DisplayDialog("완료", $"{charName}.prefab 생성 완료", "확인");
    }
}