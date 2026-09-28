using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// 프리팹 안의 SpriteRenderer 오브젝트 이름과
/// 동일한 이름의 스프라이트를 찾아서 자동으로 교체합니다.
///
/// 사용법:
///   1. 메뉴 Tools > Sprite Matcher > Match Sprites to Prefab
///   2. 팝업에서 대상 프리팹과 스프라이트 폴더를 지정
///   3. Match 버튼 클릭
/// </summary>
public class SpriteMatcher : EditorWindow
{
    GameObject  _targetPrefab;
    string      _spriteFolderPath = "Assets/Sprites";

    [MenuItem("Tools/기타/Sprite Matcher/Match Sprites to Prefab")]
    static void Open() => GetWindow<SpriteMatcher>("Sprite Matcher");

    void OnGUI()
    {
        GUILayout.Label("Sprite Matcher", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 프리팹 선택
        _targetPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Target Prefab", _targetPrefab, typeof(GameObject), false);

        // 스프라이트 폴더 경로
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

        GUI.enabled = _targetPrefab != null;
        if (GUILayout.Button("Match Sprites", GUILayout.Height(32)))
            DoMatch();
        GUI.enabled = true;
    }

    void DoMatch()
    {
        // 1. 스프라이트 폴더에서 스프라이트 전부 수집 (이름 → Sprite)
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { _spriteFolderPath });
        var spriteMap = new Dictionary<string, Sprite>();
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // 스프라이트 시트인 경우 서브에셋 포함
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
            {
                if (asset is Sprite sp && !spriteMap.ContainsKey(sp.name))
                    spriteMap[sp.name] = sp;
            }
        }

        if (spriteMap.Count == 0)
        {
            EditorUtility.DisplayDialog("Sprite Matcher",
                $"'{_spriteFolderPath}' 에서 스프라이트를 찾지 못했습니다.", "OK");
            return;
        }

        // 2. 프리팹 열어서 SpriteRenderer 순회
        string prefabPath = AssetDatabase.GetAssetPath(_targetPrefab);
        GameObject root   = PrefabUtility.LoadPrefabContents(prefabPath);

        int matched = 0, notFound = 0;
        var log = new System.Text.StringBuilder();

        SpriteRenderer[] renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in renderers)
        {
            // 오브젝트 이름으로 스프라이트 검색
            string objName = sr.gameObject.name;
            if (spriteMap.TryGetValue(objName, out Sprite found))
            {
                sr.sprite = found;
                matched++;
                log.AppendLine($"  [OK]  {objName}  →  {found.name}");
            }
            else
            {
                notFound++;
                log.AppendLine($"  [--]  {objName}  (스프라이트 없음)");
            }
        }

        // 3. 저장
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.Refresh();

        // 4. 결과 로그
        string summary = $"완료: {matched}개 교체, {notFound}개 미매칭\n\n{log}";
        Debug.Log($"[SpriteMatcher]\n{summary}");
        EditorUtility.DisplayDialog("Sprite Matcher 완료", summary, "OK");
    }
}
