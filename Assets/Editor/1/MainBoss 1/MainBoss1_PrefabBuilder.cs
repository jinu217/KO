using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// MainBoss 1 프리팹 생성 + 스프라이트 자동 매칭
/// Tools > MainBoss 1 > Build Prefab & Match Sprites
/// </summary>
public class MainBoss1_PrefabBuilder : EditorWindow
{
    const string PREFAB_SAVE_PATH  = "Assets/Prefabs";
    const string PREFAB_NAME       = "MainBoss 1";
    const string DEFAULT_SPRITE_FOLDER = "Assets/Mob/메인보스";

    string _spriteFolderPath = DEFAULT_SPRITE_FOLDER;

    // ── 히에라키 구조 정의 ───────────────────────────────────────────
    // (오브젝트명, 부모경로)  부모경로 "" = 루트 자식
    static readonly (string name, string parent)[] HIERARCHY = new[]
    {
        // 루트
        ("Body",        ""),
        // Body 직속 자식
        ("Head",        "Body"),
        ("Bottom",      "Body"),
        ("Shoulder_F",  "Body"),
        ("Shoulder_B",  "Body"),
        ("Thigh_F",     "Body"),
        ("Thigh_B",     "Body"),
        // 앞다리 체인
        ("Calf_F",      "Body/Thigh_F"),
        ("Foot_F",      "Body/Thigh_F/Calf_F"),
        // 뒷다리 체인
        ("Calf_B",      "Body/Thigh_B"),
        ("Foot_B",      "Body/Thigh_B/Calf_B"),
        // 앞팔 체인
        ("Forearm_F",   "Body/Shoulder_F"),
        ("Hand_F",      "Body/Shoulder_F/Forearm_F"),
        // 뒷팔 체인
        ("Forearm_B",   "Body/Shoulder_B"),
        ("Hand_B",      "Body/Shoulder_B/Forearm_B"),
        // 무기
        ("Weapon",      "Body/Shoulder_B/Forearm_B/Hand_B"),
    };

    [MenuItem("Tools/1/MainBoss 1/Build Prefab & Match Sprites")]
    static void Open() => GetWindow<MainBoss1_PrefabBuilder>("MainBoss 1 Builder");

    void OnGUI()
    {
        GUILayout.Label("MainBoss 1 Prefab Builder", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        _spriteFolderPath = EditorGUILayout.TextField("Sprite Folder", _spriteFolderPath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string abs = EditorUtility.OpenFolderPanel("Select Sprite Folder",
                Application.dataPath, "");
            if (!string.IsNullOrEmpty(abs))
                _spriteFolderPath = "Assets" + abs.Substring(Application.dataPath.Length);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (GUILayout.Button("Build Prefab & Match Sprites", GUILayout.Height(36)))
            Build();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "1. 스프라이트 폴더를 지정하세요.\n" +
            "2. Build 버튼을 누르면:\n" +
            "   - MainBoss 1 프리팹을 생성합니다.\n" +
            "   - 오브젝트 이름과 같은 스프라이트를 자동 매칭합니다.\n" +
            "3. 이미 프리팹이 존재하면 스프라이트만 다시 매칭합니다.",
            MessageType.Info);
    }

    void Build()
    {
        // ── 1. 스프라이트 수집 ───────────────────────────────────────
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { _spriteFolderPath });
        var spriteMap = new Dictionary<string, Sprite>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sp && !spriteMap.ContainsKey(sp.name))
                    spriteMap[sp.name] = sp;
            }
        }

        if (spriteMap.Count == 0)
        {
            EditorUtility.DisplayDialog("MainBoss 1 Builder",
                $"'{_spriteFolderPath}' 에서 스프라이트를 찾지 못했습니다.", "OK");
            return;
        }

        // ── 2. 프리팹 폴더 확인 ─────────────────────────────────────
        if (!AssetDatabase.IsValidFolder(PREFAB_SAVE_PATH))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        string prefabPath = $"{PREFAB_SAVE_PATH}/{PREFAB_NAME}.prefab";
        GameObject root;
        bool isNew = false;

        if (File.Exists(prefabPath.Replace("Assets/", Application.dataPath + "/")))
        {
            // 기존 프리팹 로드
            root = PrefabUtility.LoadPrefabContents(prefabPath);
        }
        else
        {
            // 새 프리팹 생성
            root = new GameObject(PREFAB_NAME);
            isNew = true;
        }

        // ── 3. 히에라키 생성 ─────────────────────────────────────────
        // 경로 → Transform 캐시
        var transformCache = new Dictionary<string, Transform>();
        transformCache[""] = root.transform;

        foreach (var (name, parent) in HIERARCHY)
        {
            string fullPath = string.IsNullOrEmpty(parent) ? name : $"{parent}/{name}";

            // 이미 존재하는 오브젝트 재사용
            Transform existing = root.transform.Find(fullPath);
            Transform t;
            if (existing != null)
            {
                t = existing;
            }
            else
            {
                GameObject go = new GameObject(name);
                Transform parentT = transformCache[parent];
                go.transform.SetParent(parentT, false);
                t = go.transform;
            }

            transformCache[fullPath] = t;

            // SpriteRenderer 추가/확인
            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = t.gameObject.AddComponent<SpriteRenderer>();

            // 스프라이트 매칭
            if (spriteMap.TryGetValue(name, out Sprite found))
                sr.sprite = found;
        }

        // ── 4. 저장 ──────────────────────────────────────────────────
        int matched   = 0;
        int notFound  = 0;
        var log = new System.Text.StringBuilder();

        foreach (var (name, _) in HIERARCHY)
        {
            if (spriteMap.ContainsKey(name)) { matched++;  log.AppendLine($"  [OK] {name}"); }
            else                             { notFound++; log.AppendLine($"  [--] {name} (스프라이트 없음)"); }
        }

        if (isNew)
        {
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }
        else
        {
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.Refresh();

        string summary = $"완료: {matched}개 매칭, {notFound}개 미매칭\n저장: {prefabPath}\n\n{log}";
        Debug.Log($"[MainBoss 1 Builder]\n{summary}");
        EditorUtility.DisplayDialog("MainBoss 1 Builder 완료", summary, "OK");
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
    }
}
