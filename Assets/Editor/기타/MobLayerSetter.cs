using UnityEngine;
using UnityEditor;

public class MobLayerSetter : EditorWindow
{
  const string BODY_LAYER = "Body";
  const string FRONT_LAYER = "Front";
  const string BACK_LAYER = "Back";

  GameObject targetRoot;

  [MenuItem("Tools/Mob/Layer Setter")]
  public static void ShowWindow()
  {
    var window = GetWindow<MobLayerSetter>("Mob Layer Setter");
    window.minSize = new Vector2(420f, 180f);
  }

  void OnGUI()
  {
    GUILayout.Space(10);

    GUILayout.Label("Mob Sorting Layer Setter", EditorStyles.boldLabel);

    GUILayout.Space(5);

    targetRoot = (GameObject)EditorGUILayout.ObjectField(
      "대상 루트",
      targetRoot,
      typeof(GameObject),
      true
    );

    GUILayout.Space(8);

    EditorGUILayout.HelpBox(
      "Body, Head → Body\n" +
      "Shoulder_F, Hand_F, Foot_F → Front\n" +
      "Shoulder_B, Hand_B, Hand_B 아래의 모든 자식 → Back\n\n" +
      "Order in Layer는 변경하지 않습니다.",
      MessageType.Info
    );

    GUILayout.Space(10);

    if (GUILayout.Button("Sorting Layer 적용", GUILayout.Height(32)))
    {
      ApplyLayers();
    }
  }

  void ApplyLayers()
  {
    if (targetRoot == null)
    {
      EditorUtility.DisplayDialog(
        "오류",
        "대상 루트를 지정해주세요.",
        "확인"
      );
      return;
    }

    if (!SortingLayerExists(BODY_LAYER) ||
        !SortingLayerExists(FRONT_LAYER) ||
        !SortingLayerExists(BACK_LAYER))
    {
      EditorUtility.DisplayDialog(
        "오류",
        "Sorting Layer에 Body, Front, Back이 모두 존재해야 합니다.",
        "확인"
      );
      return;
    }

    Transform body = FindChildRecursive(targetRoot.transform, "Body");

    if (body == null)
    {
      EditorUtility.DisplayDialog(
        "오류",
        "대상 안에서 Body 오브젝트를 찾지 못했습니다.",
        "확인"
      );
      return;
    }

    int changed = 0;

    changed += SetSortingLayer(body, BODY_LAYER);

    Transform head = body.Find("Head");
    if (head != null)
      changed += SetSortingLayer(head, BODY_LAYER);

    Transform shoulderF = body.Find("Shoulder_F");
    if (shoulderF != null)
    {
      changed += SetSortingLayer(shoulderF, FRONT_LAYER);

      Transform handF = shoulderF.Find("Hand_F");
      if (handF != null)
        changed += SetSortingLayer(handF, FRONT_LAYER);
    }

    Transform shoulderB = body.Find("Shoulder_B");
    if (shoulderB != null)
    {
      changed += SetSortingLayer(shoulderB, BACK_LAYER);

      Transform handB = shoulderB.Find("Hand_B");
      if (handB != null)
      {
        changed += SetSortingLayer(handB, BACK_LAYER);

        foreach (Transform child in handB)
        {
          changed += SetSortingLayerRecursive(child, BACK_LAYER);
        }
      }
    }

    Transform bottom = body.Find("Bottom");
    if (bottom != null)
    {
      Transform footF = bottom.Find("Foot_F");
      if (footF != null)
        changed += SetSortingLayer(footF, FRONT_LAYER);

      Transform footB = bottom.Find("Foot_B");
      if (footB != null)
        changed += SetSortingLayer(footB, BACK_LAYER);
    }

    EditorUtility.SetDirty(targetRoot);

    Debug.Log(
      $"[MobLayerSetter] 완료: {targetRoot.name} / 변경된 SpriteRenderer {changed}개"
    );
  }

  int SetSortingLayer(Transform target, string sortingLayerName)
  {
    if (target == null)
      return 0;

    SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();

    if (renderer == null)
      return 0;

    Undo.RecordObject(renderer, "Set Sorting Layer");

    renderer.sortingLayerName = sortingLayerName;

    EditorUtility.SetDirty(renderer);

    return 1;
  }

  int SetSortingLayerRecursive(Transform target, string sortingLayerName)
  {
    int changed = SetSortingLayer(target, sortingLayerName);

    foreach (Transform child in target)
    {
      changed += SetSortingLayerRecursive(child, sortingLayerName);
    }

    return changed;
  }

  Transform FindChildRecursive(Transform parent, string childName)
  {
    if (parent.name == childName)
      return parent;

    foreach (Transform child in parent)
    {
      Transform result = FindChildRecursive(child, childName);

      if (result != null)
        return result;
    }

    return null;
  }

  bool SortingLayerExists(string layerName)
  {
    foreach (SortingLayer layer in SortingLayer.layers)
    {
      if (layer.name == layerName)
        return true;
    }

    return false;
  }
}
