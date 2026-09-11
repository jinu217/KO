using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class PrefabBuilderWindow : EditorWindow
{
    // 여러 폴더를 담는 리스트 (각 폴더마다 프리팹 하나씩 생성)
    List<string> _folderPaths = new List<string>();
    string _manualFolderInput = "Assets/Sprites/";
    string _prefabPath = "Assets/Prefab";
    Vector2 _scrollPos;

    [MenuItem("Tools/Prefab/Build Prefab From Sprite Folder")]
    public static void ShowWindow()
    {
        var win = GetWindow<PrefabBuilderWindow>("Prefab Builder");
        win.minSize = new Vector2(420, 320);
    }

    void OnGUI()
    {
        GUILayout.Space(10);

        GUILayout.Label("스프라이트 폴더 목록 (여러 개 가능)", EditorStyles.boldLabel);

        // ------------------------------------------------------------
        // 수동 입력 + 추가 버튼
        // ------------------------------------------------------------

        EditorGUILayout.BeginHorizontal();
        _manualFolderInput = EditorGUILayout.TextField(_manualFolderInput);

        if (GUILayout.Button("추가", GUILayout.Width(50)))
        {
            AddFolder(_manualFolderInput);
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        // ------------------------------------------------------------
        // 드래그 앤 드롭 영역 (여러 폴더 동시 드롭 가능)
        // ------------------------------------------------------------

        var dropArea = GUILayoutUtility.GetRect(
            0,
            40,
            GUILayout.ExpandWidth(true)
        );

        GUI.Box(dropArea, "폴더를 여기에 드래그 (여러 개 한번에 가능)");

        var evt = Event.current;

        if ((evt.type == EventType.DragUpdated ||
             evt.type == EventType.DragPerform) &&
            dropArea.Contains(evt.mousePosition))
        {
            DragAndDrop.visualMode =
                DragAndDropVisualMode.Copy;

            if (evt.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                foreach (var path in DragAndDrop.paths)
                {
                    if (AssetDatabase.IsValidFolder(path))
                    {
                        AddFolder(path);
                    }
                }
            }

            evt.Use();
        }

        GUILayout.Space(10);

        // ------------------------------------------------------------
        // 추가된 폴더 목록 표시 (개별 삭제 가능)
        // ------------------------------------------------------------

        GUILayout.Label(
            $"선택된 폴더 ({_folderPaths.Count}개)",
            EditorStyles.boldLabel
        );

        _scrollPos = EditorGUILayout.BeginScrollView(
            _scrollPos,
            GUILayout.Height(100)
        );

        for (int i = 0; i < _folderPaths.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_folderPaths[i]);

            if (GUILayout.Button("X", GUILayout.Width(24)))
            {
                _folderPaths.RemoveAt(i);
                EditorGUILayout.EndHorizontal();
                break;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("전체 비우기"))
        {
            _folderPaths.Clear();
        }

        GUILayout.Space(10);

        _prefabPath = EditorGUILayout.TextField("Prefab Save Path", _prefabPath);

        GUILayout.Space(10);

        using (new EditorGUI.DisabledScope(_folderPaths.Count == 0))
        {
            if (GUILayout.Button(
                _folderPaths.Count <= 1
                    ? "프리팹 생성"
                    : $"프리팹 {_folderPaths.Count}개 한번에 생성",
                GUILayout.Height(30)))
            {
                BuildAll();
            }
        }
    }

    void AddFolder(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        if (!AssetDatabase.IsValidFolder(path))
        {
            EditorUtility.DisplayDialog(
                "오류",
                $"유효한 폴더가 아닙니다:\n{path}",
                "확인"
            );

            return;
        }

        if (!_folderPaths.Contains(path))
        {
            _folderPaths.Add(path);
        }
    }

    // ================================================================
    // 기본 계층 구조
    // ================================================================
    //
    // 여기서는 항상 존재해야 하는 기본 오브젝트만 만든다.
    //
    // Body
    // ├─ Head
    // ├─ Shoulder_F
    // ├─ Shoulder_B
    // └─ Bottom
    //
    // Thigh / Calf / Foot는 BuildLeg()에서
    // 실제 PNG 존재 여부에 따라 생성한다.
    //
    // ================================================================

    static readonly Dictionary<string, (string parent, string layer, int order)> BASIC_HIERARCHY =
        new()
        {
            { "Body",       ("root", "Body",  0) },
            { "Head",       ("Body", "Body",  1) },

            { "Shoulder_F", ("Body", "Front", 2) },
            { "Shoulder_B", ("Body", "Back",  2) },

            { "Bottom",     ("Body", "", 0) }
        };

    // ================================================================
    // 프리팹 생성
    // ================================================================

    // ================================================================
    // 여러 폴더 한번에 처리
    // ================================================================
    //
    // 폴더 하나가 실패해도(스프라이트 없음 등) 나머지는 계속 진행하고,
    // 끝나면 성공/실패 목록을 한번에 요약해서 보여준다.
    //
    // ================================================================

    void BuildAll()
    {
        var succeeded = new List<string>();
        var failed = new List<(string folder, string reason)>();

        foreach (string folderPath in _folderPaths)
        {
            try
            {
                string result = Build(folderPath);

                if (result != null)
                {
                    succeeded.Add(result);
                }
                else
                {
                    failed.Add((folderPath, "스프라이트 없음 또는 폴더 없음"));
                }
            }
            catch (System.Exception e)
            {
                failed.Add((folderPath, e.Message));
                Debug.LogError(
                    $"[PrefabBuilder] {folderPath} 처리 중 오류: {e}"
                );
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string summary =
            $"성공: {succeeded.Count}개\n실패: {failed.Count}개";

        if (failed.Count > 0)
        {
            summary += "\n\n실패 목록:\n";

            foreach (var (folder, reason) in failed)
            {
                summary += $"- {folder} ({reason})\n";
            }
        }

        EditorUtility.DisplayDialog(
            "일괄 생성 완료",
            summary,
            "확인"
        );
    }

    // 반환값: 생성된 캐릭터 이름 (실패 시 null). BuildAll()에서 사용.
    string Build(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            Debug.LogWarning(
                $"[PrefabBuilder] 폴더를 찾을 수 없습니다:\n{folderPath}"
            );

            return null;
        }

        string charName = Path.GetFileName(
            folderPath.TrimEnd('/')
        );

        // ============================================================
        // 스프라이트 수집
        // ============================================================

        string[] guids = AssetDatabase.FindAssets(
            "t:Sprite",
            new[] { folderPath }
        );

        var sprites =
            new Dictionary<string, Sprite>();

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            Sprite sprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite != null)
            {
                sprites[sprite.name] = sprite;
            }
        }

        if (sprites.Count == 0)
        {
            Debug.LogWarning(
                $"[PrefabBuilder] 해당 폴더에 스프라이트가 없습니다: {folderPath}"
            );

            return null;
        }

        // ============================================================
        // 프리팹 저장 폴더
        // ============================================================

        if (!AssetDatabase.IsValidFolder(_prefabPath))
        {
            string parent =
                Path.GetDirectoryName(_prefabPath)
                    .Replace("\\", "/");

            string folderName =
                Path.GetFileName(_prefabPath);

            if (!AssetDatabase.IsValidFolder(parent))
            {
                AssetDatabase.CreateFolder(
                    "Assets",
                    Path.GetFileName(parent)
                );
            }

            AssetDatabase.CreateFolder(
                parent,
                folderName
            );
        }

        // ============================================================
        // Root 생성
        // ============================================================

        var root = new GameObject(charName);

        var goMap =
            new Dictionary<string, GameObject>
            {
                { "root", root }
            };

        // ============================================================
        // 기본 Body / Head / Shoulder / Bottom 생성
        // ============================================================

        foreach (var kvp in BASIC_HIERARCHY)
        {
            string spriteName = kvp.Key;

            var (
                parentPath,
                layer,
                order
            ) = kvp.Value;

            // Body / Shoulder / Bottom은
            // PNG가 없어도 기본 구조를 만든다.
            bool isEssential =
                spriteName == "Body" ||
                spriteName == "Shoulder_F" ||
                spriteName == "Shoulder_B" ||
                spriteName == "Bottom";

            if (!isEssential &&
                !sprites.ContainsKey(spriteName))
            {
                continue;
            }

            if (!goMap.TryGetValue(
                    parentPath,
                    out GameObject parentGo))
            {
                continue;
            }

            GameObject go =
                new GameObject(spriteName);

            go.transform.SetParent(
                parentGo.transform,
                false
            );

            string fullPath =
                parentPath == "root"
                    ? spriteName
                    : parentPath + "/" + spriteName;

            goMap[spriteName] = go;
            goMap[fullPath] = go;

            // --------------------------------------------------------
            // 스프라이트가 실제로 존재할 때만 Renderer 생성
            // --------------------------------------------------------

            if (sprites.TryGetValue(
                    spriteName,
                    out Sprite sprite))
            {
                SpriteRenderer sr =
                    go.AddComponent<SpriteRenderer>();

                if (!string.IsNullOrEmpty(layer))
                {
                    sr.sortingLayerName = layer;
                }

                sr.sortingOrder = order;
                sr.sprite = sprite;
            }
        }

        // ============================================================
        // 앞팔
        // ============================================================

        BuildArm(
            "F",
            "Front",
            sprites,
            goMap
        );

        // ============================================================
        // 뒷팔
        // ============================================================

        BuildArm(
            "B",
            "Back",
            sprites,
            goMap
        );

        // ============================================================
        // 앞다리
        // ============================================================

        BuildLeg(
            "F",
            "Front",
            sprites,
            goMap
        );

        // ============================================================
        // 뒷다리
        // ============================================================

        BuildLeg(
            "B",
            "Back",
            sprites,
            goMap
        );

        // ============================================================
        // 무기
        // ============================================================

        BuildWeapon(
            "Club",
            sprites,
            goMap
        );

        BuildWeapon(
            "Axe",
            sprites,
            goMap
        );

        BuildWeapon(
            "Weapon",
            sprites,
            goMap
        );

        BuildWeapon(
            "Sword",
            sprites,
            goMap
        );

        // ============================================================
        // Prefab 저장
        // ============================================================

        string prefabSavePath =
            $"{_prefabPath}/{charName}.prefab";

        PrefabUtility.SaveAsPrefabAsset(
            root,
            prefabSavePath
        );

        Object.DestroyImmediate(root);

        Debug.Log(
            $"[PrefabBuilder] {charName} → {prefabSavePath}"
        );

        Selection.activeObject =
            AssetDatabase.LoadAssetAtPath<Object>(
                prefabSavePath
            );

        return charName;
    }

    // ================================================================
    // 팔 생성
    // ================================================================
    //
    // Forearm 존재:
    //
    // Shoulder
    //   └─ Forearm
    //       └─ Hand
    //
    // Forearm 없음:
    //
    // Shoulder
    //   └─ Hand
    //
    // ================================================================

    void BuildArm(
        string side,
        string sortingLayer,
        Dictionary<string, Sprite> sprites,
        Dictionary<string, GameObject> goMap
    )
    {
        string shoulderName =
            $"Shoulder_{side}";

        string forearmName =
            $"Forearm_{side}";

        string handName =
            $"Hand_{side}";

        string shoulderPath =
            $"Body/{shoulderName}";

        string forearmPath =
            $"{shoulderPath}/{forearmName}";

        // ------------------------------------------------------------
        // Shoulder가 없으면 팔 전체를 만들지 않음
        // ------------------------------------------------------------

        if (!goMap.TryGetValue(
                shoulderPath,
                out GameObject shoulder))
        {
            return;
        }

        // ------------------------------------------------------------
        // Forearm이 있는 경우
        //
        // Shoulder → Forearm → Hand
        // ------------------------------------------------------------

        if (sprites.ContainsKey(forearmName))
        {
            GameObject forearm =
                CreateSpriteObject(
                    forearmName,
                    shoulderPath,
                    sortingLayer,
                    2,
                    sprites,
                    goMap
                );

            if (forearm != null &&
                sprites.ContainsKey(handName))
            {
                CreateSpriteObject(
                    handName,
                    forearmPath,
                    sortingLayer,
                    3,
                    sprites,
                    goMap
                );
            }

            return;
        }

        // ------------------------------------------------------------
        // Forearm이 없는 경우
        //
        // Shoulder → Hand
        // ------------------------------------------------------------

        if (sprites.ContainsKey(handName))
        {
            CreateSpriteObject(
                handName,
                shoulderPath,
                sortingLayer,
                3,
                sprites,
                goMap
            );
        }
    }

    // ================================================================
    // 다리 생성
    // ================================================================
    //
    // 핵심:
    //
    // Thigh가 있으면
    //
    // Bottom
    //   └─ Thigh
    //       └─ Calf
    //           └─ Foot
    //
    // Thigh가 없으면
    //
    // Bottom
    //   └─ Calf
    //       └─ Foot
    //
    // Calf도 없으면
    //
    // Bottom
    //   └─ Foot
    //
    // 즉, 없는 파츠는 오브젝트를 만들지 않고
    // 존재하는 다음 파츠를 바로 현재 부모 밑에 붙인다.
    //
    // ================================================================

    void BuildLeg(
        string side,
        string sortingLayer,
        Dictionary<string, Sprite> sprites,
        Dictionary<string, GameObject> goMap
    )
    {
        string thighName =
            $"Thigh_{side}";

        string calfName =
            $"Calf_{side}";

        string footName =
            $"Foot_{side}";

        string bottomPath =
            "Body/Bottom";

        // ------------------------------------------------------------
        // Bottom 확인
        // ------------------------------------------------------------

        if (!goMap.TryGetValue(
                bottomPath,
                out GameObject bottom))
        {
            Debug.LogWarning(
                $"[PrefabBuilder] Bottom을 찾을 수 없습니다: {side}"
            );

            return;
        }

        // ------------------------------------------------------------
        // 현재 부모
        //
        // 처음에는 Bottom
        // ------------------------------------------------------------

        string currentParentPath =
            bottomPath;

        // ------------------------------------------------------------
        // Thigh
        //
        // PNG가 있으면 생성
        // 없으면 바로 다음 파츠로 넘어감
        // ------------------------------------------------------------

        if (sprites.ContainsKey(thighName))
        {
            GameObject thigh =
                CreateSpriteObject(
                    thighName,
                    currentParentPath,
                    sortingLayer,
                    1,
                    sprites,
                    goMap
                );

            if (thigh != null)
            {
                currentParentPath =
                    $"{currentParentPath}/{thighName}";
            }
        }

        // ------------------------------------------------------------
        // Calf
        //
        // Thigh가 존재하면
        //     Thigh → Calf
        //
        // Thigh가 없으면
        //     Bottom → Calf
        //
        // ------------------------------------------------------------

        if (sprites.ContainsKey(calfName))
        {
            GameObject calf =
                CreateSpriteObject(
                    calfName,
                    currentParentPath,
                    sortingLayer,
                    2,
                    sprites,
                    goMap
                );

            if (calf != null)
            {
                currentParentPath =
                    $"{currentParentPath}/{calfName}";
            }
        }

        // ------------------------------------------------------------
        // Foot
        //
        // 지금까지 실제로 만들어진 가장 가까운 부모 밑에 생성
        // ------------------------------------------------------------

        if (sprites.ContainsKey(footName))
        {
            CreateSpriteObject(
                footName,
                currentParentPath,
                sortingLayer,
                3,
                sprites,
                goMap
            );
        }
    }

    // ================================================================
    // 무기 생성
    // ================================================================
    //
    // 1순위:
    //
    // Body
    //   └─ Shoulder_B
    //       └─ Forearm_B
    //           └─ Hand_B
    //               └─ Weapon
    //
    // 2순위:
    //
    // Body
    //   └─ Shoulder_B
    //       └─ Hand_B
    //           └─ Weapon
    //
    // ================================================================

    void BuildWeapon(
        string weaponName,
        Dictionary<string, Sprite> sprites,
        Dictionary<string, GameObject> goMap
    )
    {
        // ------------------------------------------------------------
        // 무기 PNG가 없으면 생성하지 않음
        // ------------------------------------------------------------

        if (!sprites.ContainsKey(weaponName))
        {
            return;
        }

        // ------------------------------------------------------------
        // 1순위:
        // Shoulder_B / Forearm_B / Hand_B
        // ------------------------------------------------------------

        string fullHandPath =
            "Body/Shoulder_B/Forearm_B/Hand_B";

        if (goMap.TryGetValue(
                fullHandPath,
                out GameObject fullHand))
        {
            CreateWeaponObject(
                weaponName,
                fullHand,
                sprites
            );

            return;
        }

        // ------------------------------------------------------------
        // 2순위:
        // Shoulder_B / Hand_B
        // ------------------------------------------------------------

        string shortHandPath =
            "Body/Shoulder_B/Hand_B";

        if (goMap.TryGetValue(
                shortHandPath,
                out GameObject shortHand))
        {
            CreateWeaponObject(
                weaponName,
                shortHand,
                sprites
            );

            return;
        }

        Debug.LogWarning(
            $"[PrefabBuilder] {weaponName}의 Hand_B 부모를 찾을 수 없습니다."
        );
    }

    // ================================================================
    // 일반 Sprite 오브젝트 생성
    // ================================================================

    GameObject CreateSpriteObject(
        string objectName,
        string parentPath,
        string sortingLayer,
        int sortingOrder,
        Dictionary<string, Sprite> sprites,
        Dictionary<string, GameObject> goMap
    )
    {
        // ------------------------------------------------------------
        // 부모 찾기
        // ------------------------------------------------------------

        if (!goMap.TryGetValue(
                parentPath,
                out GameObject parent))
        {
            Debug.LogWarning(
                $"[PrefabBuilder] 부모를 찾을 수 없습니다: " +
                $"{parentPath} → {objectName}"
            );

            return null;
        }

        // ------------------------------------------------------------
        // 이미 존재하면 기존 오브젝트 반환
        // ------------------------------------------------------------

        string fullPath =
            $"{parentPath}/{objectName}";

        if (goMap.TryGetValue(
                fullPath,
                out GameObject existing))
        {
            return existing;
        }

        // ------------------------------------------------------------
        // 오브젝트 생성
        // ------------------------------------------------------------

        GameObject go =
            new GameObject(objectName);

        go.transform.SetParent(
            parent.transform,
            false
        );

        // ------------------------------------------------------------
        // SpriteRenderer
        // ------------------------------------------------------------

        SpriteRenderer sr =
            go.AddComponent<SpriteRenderer>();

        sr.sortingLayerName =
            sortingLayer;

        sr.sortingOrder =
            sortingOrder;

        // ------------------------------------------------------------
        // Sprite 연결
        // ------------------------------------------------------------

        if (sprites.TryGetValue(
                objectName,
                out Sprite sprite))
        {
            sr.sprite = sprite;
        }

        // ------------------------------------------------------------
        // Dictionary 등록
        // ------------------------------------------------------------

        goMap[objectName] = go;
        goMap[fullPath] = go;

        return go;
    }

    // ================================================================
    // 무기 오브젝트 생성
    // ================================================================

    void CreateWeaponObject(
        string weaponName,
        GameObject parent,
        Dictionary<string, Sprite> sprites
    )
    {
        // ------------------------------------------------------------
        // 같은 이름의 무기가 이미 존재하는 경우 방지
        // ------------------------------------------------------------

        Transform existing =
            parent.transform.Find(weaponName);

        if (existing != null)
        {
            return;
        }

        // ------------------------------------------------------------
        // 오브젝트 생성
        // ------------------------------------------------------------

        GameObject go =
            new GameObject(weaponName);

        go.transform.SetParent(
            parent.transform,
            false
        );

        // ------------------------------------------------------------
        // SpriteRenderer
        // ------------------------------------------------------------

        SpriteRenderer sr =
            go.AddComponent<SpriteRenderer>();

        sr.sortingLayerName =
            "Back";

        sr.sortingOrder =
            0;

        // ------------------------------------------------------------
        // Sprite 연결
        // ------------------------------------------------------------

        if (sprites.TryGetValue(
                weaponName,
                out Sprite sprite))
        {
            sr.sprite = sprite;
        }
    }
}