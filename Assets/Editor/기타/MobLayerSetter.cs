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
        window.minSize = new Vector2(460f, 430f);
    }

    void OnGUI()
    {
        GUILayout.Space(10);

        GUILayout.Label(
          "Mob Sorting Layer Setter",
          EditorStyles.boldLabel
        );

        GUILayout.Space(5);

        targetRoot = (GameObject)EditorGUILayout.ObjectField(
          "대상 루트",
          targetRoot,
          typeof(GameObject),
          true
        );

        GUILayout.Space(12);

        GUILayout.Label(
          "일반 몹",
          EditorStyles.boldLabel
        );

        if (GUILayout.Button(
          "일반 몹 Sorting Layer 적용",
          GUILayout.Height(32)
        ))
        {
            ApplyLayers(false);
        }

        GUILayout.Space(18);

        GUILayout.Label(
          "보스",
          EditorStyles.boldLabel
        );

        if (GUILayout.Button(
          "보스 Sorting Layer 적용",
          GUILayout.Height(32)
        ))
        {
            ApplyLayers(true);
        }
    }

    void ApplyLayers(bool bossMode)
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

        Transform body = FindChildRecursive(
          targetRoot.transform,
          "Body"
        );

        if (body == null)
        {
            EditorUtility.DisplayDialog(
              "오류",
              "대상 안에서 Body 오브젝트를 찾지 못했습니다.",
              "확인"
            );
            return;
        }

        int changed = bossMode
          ? ApplyBossLayers(body)
          : ApplyMobLayers(body);

        EditorUtility.SetDirty(targetRoot);

        Debug.Log(
          $"[MobLayerSetter] {(bossMode ? "보스" : "일반 몹")} 완료: " +
          $"{targetRoot.name} / 변경된 SpriteRenderer {changed}개"
        );
    }

    int ApplyMobLayers(Transform body)
    {
        int changed = 0;

        changed += SetSortingLayer(
          body,
          BODY_LAYER
        );

        Transform head = body.Find("Head");

        if (head != null)
        {
            changed += SetSortingLayer(
              head,
              BODY_LAYER
            );
        }

        Transform shoulderF = body.Find("Shoulder_F");

        if (shoulderF != null)
        {
            changed += SetSortingLayer(
              shoulderF,
              FRONT_LAYER
            );

            Transform handF = FindChildRecursive(
              shoulderF,
              "Hand_F"
            );

            if (handF != null)
            {
                changed += SetSortingLayer(
                  handF,
                  FRONT_LAYER
                );
            }
        }

        Transform shoulderB = body.Find("Shoulder_B");

        if (shoulderB != null)
        {
            changed += SetSortingLayer(
              shoulderB,
              BACK_LAYER
            );

            Transform handB = FindChildRecursive(
              shoulderB,
              "Hand_B"
            );

            if (handB != null)
            {
                changed += SetSortingLayer(
                  handB,
                  BACK_LAYER
                );

                foreach (Transform child in handB)
                {
                    changed += SetSortingLayerRecursive(
                      child,
                      BACK_LAYER
                    );
                }
            }
        }

        Transform bottom = body.Find("Bottom");

        if (bottom != null)
        {
            Transform footF = FindChildRecursive(
              bottom,
              "Foot_F"
            );

            if (footF != null)
            {
                changed += SetSortingLayer(
                  footF,
                  FRONT_LAYER
                );
            }

            Transform footB = FindChildRecursive(
              bottom,
              "Foot_B"
            );

            if (footB != null)
            {
                changed += SetSortingLayer(
                  footB,
                  BACK_LAYER
                );
            }
        }

        return changed;
    }

    int ApplyBossLayers(Transform body)
    {
        int changed = 0;

        changed += SetSortingLayer(
          body,
          BODY_LAYER
        );

        Transform head = body.Find("Head");

        if (head != null)
        {
            changed += SetSortingLayer(
              head,
              BODY_LAYER
            );
        }

        Transform shoulderF = body.Find("Shoulder_F");

        if (shoulderF != null)
        {
            changed += SetSortingLayerRecursiveIgnoringGripPoint(
              shoulderF,
              FRONT_LAYER
            );
        }

        Transform shoulderB = body.Find("Shoulder_B");

        if (shoulderB != null)
        {
            changed += SetSortingLayer(
              shoulderB,
              BACK_LAYER
            );

            Transform forearmB = shoulderB.Find("Forearm_B");

            if (forearmB != null)
            {
                changed += SetSortingLayer(
                  forearmB,
                  BACK_LAYER
                );

                Transform handB = forearmB.Find("Hand_B");

                if (handB != null)
                {
                    changed += SetSortingLayerRecursiveIgnoringGripPoint(
                      handB,
                      FRONT_LAYER
                    );
                }
            }
        }

        Transform bottom = body.Find("Bottom");

        if (bottom != null)
        {
            changed += SetSortingLayer(
              bottom,
              BODY_LAYER
            );

            foreach (Transform child in bottom)
            {
                changed += SetSortingLayerRecursiveIgnoringGripPoint(
                  child,
                  BACK_LAYER
                );
            }
        }

        return changed;
    }

    int SetSortingLayer(
      Transform target,
      string sortingLayerName
    )
    {
        if (target == null)
            return 0;

        SpriteRenderer renderer =
          target.GetComponent<SpriteRenderer>();

        if (renderer == null)
            return 0;

        Undo.RecordObject(
          renderer,
          "Set Sorting Layer"
        );

        renderer.sortingLayerName =
          sortingLayerName;

        EditorUtility.SetDirty(renderer);

        return 1;
    }

    int SetSortingLayerRecursive(
      Transform target,
      string sortingLayerName
    )
    {
        int changed = SetSortingLayer(
          target,
          sortingLayerName
        );

        foreach (Transform child in target)
        {
            changed += SetSortingLayerRecursive(
              child,
              sortingLayerName
            );
        }

        return changed;
    }

    int SetSortingLayerRecursiveIgnoringGripPoint(
      Transform target,
      string sortingLayerName
    )
    {
        if (IsGripPoint(target.name))
            return 0;

        int changed = SetSortingLayer(
          target,
          sortingLayerName
        );

        foreach (Transform child in target)
        {
            changed += SetSortingLayerRecursiveIgnoringGripPoint(
              child,
              sortingLayerName
            );
        }

        return changed;
    }

    bool IsGripPoint(string objectName)
    {
        return objectName
          .ToLowerInvariant()
          .Contains("grippoint");
    }

    Transform FindChildRecursive(
      Transform parent,
      string childName
    )
    {
        if (parent.name == childName)
            return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(
              child,
              childName
            );

            if (result != null)
                return result;
        }

        return null;
    }

    bool SortingLayerExists(
      string layerName
    )
    {
        foreach (SortingLayer layer in SortingLayer.layers)
        {
            if (layer.name == layerName)
                return true;
        }

        return false;
    }
}
