using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class BossHierarchyBuilder : EditorWindow
{

    [MenuItem("Tools/Build Mid-Boss Hierarchy")]
    static void OpenWindow()
    {
        GetWindow<BossHierarchyBuilder>("Mid-Boss Builder");
    }

    string spriteFolder = "Assets/Mob/중간보스";
    string rootName = "MidBoss";

    void OnGUI()
    {
        GUILayout.Label("중간보스 Hierarchy 자동 생성", EditorStyles.boldLabel);
        GUILayout.Space(10);

        rootName = EditorGUILayout.TextField("Root 이름", rootName);
        spriteFolder = EditorGUILayout.TextField("Sprite 폴더", spriteFolder);

        GUILayout.Space(10);

        if (GUILayout.Button("Hierarchy 생성"))
        {
            BuildHierarchy();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "지정 폴더 안의 sprite 파일들을 매칭해서 Hierarchy 자동 생성.\n\n" +
            "Sprite 파일명 매칭:\n" +
            "Head, Body, LowerBody (또는 Lower Body)\n" +
            "Shoulder_F/B, Arm_F/B, Hand_F/B\n" +
            "Thigh_F/B, Foot_F/B\n" +
            "Axe (Weapon으로 사용)",
            MessageType.Info);
    }

    void BuildHierarchy()
    {
        // Root 생성
        GameObject root = new GameObject(rootName);
        Undo.RegisterCreatedObjectUndo(root, "Create Boss Hierarchy");

        // 본 구조 정의 (이름, 부모, 매칭 sprite명, sortingOrder)
        // sortingOrder는 큰 숫자가 앞에 그려짐
        var bones = new List<BoneSpec> {
            // 본체
            new BoneSpec("Body",            "",                 "Body",          5),
            new BoneSpec("Head",            "Body",             "Head",          11),
            
            // 등 뒤 (가장 뒤)
            new BoneSpec("Back",            "Body",             "Back",          0),  // 가방 같은 거 있으면
            
            // 뒷팔 (B = 카메라 먼 쪽, 뒤에 그려짐)
            new BoneSpec("ShoulderJoint_B", "Body",             null,            -1),
            new BoneSpec("Shoulder_B",      "ShoulderJoint_B",  "Shoulder_B",    2),
            new BoneSpec("ArmJoint_B",      "Shoulder_B",       null,            -1),
            new BoneSpec("Arm_B",           "ArmJoint_B",       "Arm_B",         3),
            new BoneSpec("Hand_B",          "Arm_B",            "Hand_B",        4),
            new BoneSpec("Weapon",          "Hand_B",           "Axe",           13),  // 무기는 앞쪽
            
            // 앞팔 (F = 카메라 가까운 쪽, 앞에 그려짐)
            new BoneSpec("ShoulderJoint_F", "Body",             null,            -1),
            new BoneSpec("Shoulder_F",      "ShoulderJoint_F",  "Shoulder_F",    9),
            new BoneSpec("ArmJoint_F",      "Shoulder_F",       null,            -1),
            new BoneSpec("Arm_F",           "ArmJoint_F",       "Arm_F",         10),
            new BoneSpec("Hand_F",          "Arm_F",            "Hand_F",        12),
            
            // 하체
            new BoneSpec("LowerBody",       "Body",             "LowerBody",     6),
            
            // 뒷다리 (B)
            new BoneSpec("ThighJoint_B",    "LowerBody",        null,            -1),
            new BoneSpec("Thigh_B",         "ThighJoint_B",     "Thigh_B",       1),
            new BoneSpec("KneeJoint_B",     "Thigh_B",          null,            -1),
            new BoneSpec("Foot_B",          "KneeJoint_B",      "Foot_B",        2),
            
            // 앞다리 (F)
            new BoneSpec("ThighJoint_F",    "LowerBody",        null,            -1),
            new BoneSpec("Thigh_F",         "ThighJoint_F",     "Thigh_F",       7),
            new BoneSpec("KneeJoint_F",     "Thigh_F",          null,            -1),
            new BoneSpec("Foot_F",          "KneeJoint_F",      "Foot_F",        8),
        };

        // sprite 파일 찾기 (다양한 이름 변형 허용)
        Dictionary<string, Sprite> sprites = LoadSprites();

        Dictionary<string, GameObject> created = new Dictionary<string, GameObject>();
        created[""] = root;

        int matched = 0;
        int missing = 0;
        List<string> missingList = new List<string>();

        foreach (var bone in bones)
        {
            GameObject obj = new GameObject(bone.name);

            // 부모 설정
            GameObject parent;
            if (!created.TryGetValue(bone.parent, out parent))
            {
                Debug.LogError($"부모 못 찾음: {bone.parent} (자식: {bone.name})");
                continue;
            }
            obj.transform.SetParent(parent.transform, false);
            obj.transform.localPosition = Vector3.zero;

            // sprite 부착 (이름이 있는 경우)
            if (!string.IsNullOrEmpty(bone.spriteName))
            {
                Sprite sprite = FindSprite(sprites, bone.spriteName);
                if (sprite != null)
                {
                    var sr = obj.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    sr.sortingOrder = bone.sortingOrder;
                    matched++;
                }
                else
                {
                    Debug.LogWarning($"Sprite 못 찾음: {bone.spriteName} (GameObject {bone.name})");
                    missingList.Add(bone.spriteName);
                    missing++;
                }
            }

            created[bone.name] = obj;
        }

        Selection.activeGameObject = root;

        string summary = $"Hierarchy 생성 완료\n" +
                         $"Sprite 매칭: {matched}개\n" +
                         $"매칭 실패: {missing}개";
        if (missing > 0)
        {
            summary += "\n\n실패 목록:\n" + string.Join("\n", missingList);
        }
        EditorUtility.DisplayDialog("완료", summary, "확인");
    }

    Dictionary<string, Sprite> LoadSprites()
    {
        Dictionary<string, Sprite> result = new Dictionary<string, Sprite>();

        if (!AssetDatabase.IsValidFolder(spriteFolder))
        {
            Debug.LogError($"폴더 없음: {spriteFolder}");
            return result;
        }

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { spriteFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                // 파일명 (확장자 제외)
                string name = Path.GetFileNameWithoutExtension(path);
                result[name] = sprite;
            }
        }

        return result;
    }

    Sprite FindSprite(Dictionary<string, Sprite> sprites, string baseName)
    {
        // 다양한 변형 시도
        string[] variations = new string[] {
            baseName,
            baseName.ToLower(),
            baseName.Replace("_", " "),
            baseName.Replace("Lower", "Lower "),  // LowerBody → Lower Body
            // 한글 변형
            baseName == "Head" ? "머리" : null,
            baseName == "Body" ? "몸" : null,
            baseName == "Shoulder_F" ? "앞어깨" : null,
            baseName == "Shoulder_B" ? "뒷어깨" : null,
            baseName == "Arm_F" ? "앞팔" : null,
            baseName == "Arm_B" ? "뒷팔" : null,
            baseName == "Hand_F" ? "앞손" : null,
            baseName == "Hand_B" ? "뒷손" : null,
            baseName == "Thigh_F" ? "앞허벅지" : null,
            baseName == "Thigh_B" ? "뒷허벅지" : null,
            baseName == "Foot_F" ? "앞발" : null,
            baseName == "Foot_B" ? "뒷발" : null,
            baseName == "LowerBody" ? "하체" : null,
            baseName == "Axe" ? "도끼" : null,
            baseName == "Axe" ? "무기" : null,
            baseName == "Back" ? "등" : null,
        };

        foreach (string variation in variations)
        {
            if (string.IsNullOrEmpty(variation)) continue;
            if (sprites.TryGetValue(variation, out Sprite sprite))
            {
                return sprite;
            }
        }

        return null;
    }

    class BoneSpec
    {
        public string name;          // GameObject 이름
        public string parent;        // 부모 이름 ("" = root)
        public string spriteName;    // 매칭할 sprite 파일명 (null = 빈 GameObject)
        public int sortingOrder;     // SortingOrder (-1 = 안 씀)

        public BoneSpec(string name, string parent, string spriteName, int sortingOrder)
        {
            this.name = name;
            this.parent = parent;
            this.spriteName = spriteName;
            this.sortingOrder = sortingOrder;
        }
    }
}