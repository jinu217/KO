using UnityEngine;
using UnityEditor;

public class NPC1_PrefabBuilder
{
    const string PREFAB_PATH = "Assets/Prefab";

    [MenuItem("Tools/NPC1/Build Prefab/대장장이")]
    static void Build_Blacksmith() => BuildNPC("대장장이", isBlacksmith: true);

    [MenuItem("Tools/NPC1/Build Prefab/버프술사")]
    static void Build_BuffMage() => BuildNPC("버프술사");

    [MenuItem("Tools/NPC1/Build Prefab/주민1")]
    static void Build_Citizen1() => BuildNPC("주민1");

    [MenuItem("Tools/NPC1/Build Prefab/주민2")]
    static void Build_Citizen2() => BuildNPC("주민2");

    [MenuItem("Tools/NPC1/Build Prefab/상인")]
    static void Build_Merchant() => BuildNPC("상인");

    [MenuItem("Tools/NPC1/Build Prefab/전체 빌드")]
    static void Build_All()
    {
        Build_Blacksmith();
        Build_BuffMage();
        Build_Citizen1();
        Build_Citizen2();
        Build_Merchant();
        Debug.Log("[NPC1] 전체 빌드 완료");
    }

    // ================================================================
    // Sorting Layer: Front, Body, Back (대문자)
    //
    // 대장장이 레이어 순서 (위=상위):
    //   앞손    Hand_F   Front  3
    //   앞어깨  Shoulder_F  Front  2
    //   머리    Head        Body   1
    //   몸      Body        Body   0
    //   앞발    Foot_F      Front  1
    //   뒷발    Foot_B      Back   1
    //   뒷어깨  Shoulder_B  Back   0
    //   뒷손    Hand_B   Back  -1
    //   (앞손/뒷손 없음)
    //
    // 일반 NPC 레이어 순서 (위=상위):
    //   앞어깨  Shoulder_F  Front  2
    //   앞손    Hand_F   Front  3
    //   머리    Head        Body   1
    //   몸      Body        Body   0
    //   앞발    Foot_F      Front  1
    //   뒷발    Foot_B      Back   1
    //   뒷어깨  Shoulder_B  Back   0
    //   뒷손    Hand_B   Back  -1
    //   앞손    Hand_F      Front  4
    //   뒷손    Hand_B      Back  -2
    // ================================================================

    static void BuildNPC(string charName, bool isBlacksmith = false)
    {
        if (!AssetDatabase.IsValidFolder(PREFAB_PATH))
            AssetDatabase.CreateFolder("Assets", "Prefab");

        var root = new GameObject(charName);

        // Body
        var body = Child(root, "Body", "Body", 0);

        // Head
        Child(body, "Head", "Body", 1);

        // 앞팔
        var sf = Child(body, "Shoulder_F", "Front", isBlacksmith ? 2 : 2);
        var ff = Child(sf, "Hand_F", "Front", isBlacksmith ? 3 : 3);

        // 뒷팔
        var sb = Child(body, "Shoulder_B", "Back", 0);
        var fb = Child(sb, "Hand_B", "Back", -1);

        // 하체
        var bottom = new GameObject("Bottom");
        bottom.transform.SetParent(body.transform, false);
        Child(bottom, "Foot_F", "Front", 1);
        Child(bottom, "Foot_B", "Back", 1);

        // 저장
        string prefabPath = $"{PREFAB_PATH}/{charName}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.Refresh();

        Debug.Log($"[NPC1] {charName} → {prefabPath}");
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(prefabPath);
    }

    static GameObject Child(GameObject parent, string name, string sortingLayer, int orderInLayer)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingLayerName = sortingLayer;
        sr.sortingOrder = orderInLayer;
        return go;
    }
}