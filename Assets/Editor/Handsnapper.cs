using UnityEngine;
using UnityEditor;

public class HandSnapper : EditorWindow
{

    [MenuItem("Tools/Hand Snapper (F to B)")]
    static void OpenWindow()
    {
        GetWindow<HandSnapper>("Hand Snapper");
    }

    Transform target;        // 도달해야 할 위치 (예: Hand_B 또는 Weapon)
    Transform sample;        // 측정 기준점 (예: Hand_F)
    Transform mover;         // 이동시킬 GameObject (예: ArmJoint_F)

    void OnGUI()
    {
        GUILayout.Label("F손을 B손/도끼 위치에 매칭", EditorStyles.boldLabel);
        GUILayout.Space(5);

        target = (Transform)EditorGUILayout.ObjectField(
            "Target (도달 위치)", target, typeof(Transform), true);
        EditorGUILayout.HelpBox("F손이 가야 할 곳. 보통 Hand_B 또는 Weapon", MessageType.None);

        GUILayout.Space(5);

        sample = (Transform)EditorGUILayout.ObjectField(
            "Sample (측정 기준점)", sample, typeof(Transform), true);
        EditorGUILayout.HelpBox("F손의 끝점. 보통 Hand_F", MessageType.None);

        GUILayout.Space(5);

        mover = (Transform)EditorGUILayout.ObjectField(
            "Mover (이동시킬 본)", mover, typeof(Transform), true);
        EditorGUILayout.HelpBox("실제로 위치를 옮길 본. 보통 ArmJoint_F", MessageType.None);

        GUILayout.Space(15);

        if (GUILayout.Button("Snap! (F손을 Target 위치로)", GUILayout.Height(40)))
        {
            Snap();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("자동 채우기 (보스 구조 기준)"))
        {
            AutoFill();
        }

        GUILayout.Space(10);
        EditorGUILayout.HelpBox(
            "사용법:\n" +
            "1. 보스 캐릭터 선택 후 자동 채우기 클릭 (또는 수동으로 드래그)\n" +
            "2. Animation 창에서 키프레임 시점으로 이동 (예: F=20)\n" +
            "3. Snap 버튼 클릭 → Mover의 Position이 자동 조정됨\n" +
            "4. Animation 창의 Record 모드면 해당 시점에 자동으로 키 박힘\n" +
            "5. 다른 키프레임으로 이동해서 반복",
            MessageType.Info);

        if (target != null && sample != null && mover != null)
        {
            GUILayout.Space(10);
            GUILayout.Label("현재 상태:", EditorStyles.boldLabel);
            float dist = Vector3.Distance(target.position, sample.position);
            GUILayout.Label($"Target 위치: {target.position}");
            GUILayout.Label($"Sample 위치: {sample.position}");
            GUILayout.Label($"거리: {dist:F4}u ({dist * 64f:F1}px @ PPU 64)");

            Vector3 needed = target.position - sample.position;
            GUILayout.Label($"필요한 이동량: {needed}");
        }
    }

    void Snap()
    {
        if (target == null || sample == null || mover == null)
        {
            EditorUtility.DisplayDialog("오류", "모든 필드를 채워주세요", "확인");
            return;
        }

        // sample을 target 위치로 옮기기 위해 mover에 필요한 offset 계산
        Vector3 offset = target.position - sample.position;

        // mover의 월드 위치를 offset만큼 이동
        Undo.RecordObject(mover, "Snap Hand");
        mover.position += offset;

        Debug.Log($"Snap 완료: {mover.name} 이동량 {offset}, 새 localPosition: {mover.localPosition}");
        EditorUtility.SetDirty(mover);
    }

    void AutoFill()
    {
        // 선택된 GameObject의 자식들에서 자동 검색
        GameObject root = Selection.activeGameObject;
        if (root == null)
        {
            EditorUtility.DisplayDialog("오류", "보스 루트 GameObject를 Hierarchy에서 선택해주세요", "확인");
            return;
        }

        // Find: ArmJoint_F, Hand_F, Hand_B (또는 Weapon)
        Transform armF = FindRecursive(root.transform, "ArmJoint_F");
        Transform handF = FindRecursive(root.transform, "Hand_F");
        Transform handB = FindRecursive(root.transform, "Hand_B");
        Transform weapon = FindRecursive(root.transform, "Weapon");

        if (armF != null) mover = armF;
        if (handF != null) sample = handF;
        // Weapon이 있으면 우선, 없으면 Hand_B
        if (weapon != null) target = weapon;
        else if (handB != null) target = handB;

        if (mover == null || sample == null || target == null)
        {
            EditorUtility.DisplayDialog("경고",
                "일부 본을 못 찾았어요. 수동으로 드래그해주세요.\n\n" +
                $"ArmJoint_F (Mover): {(mover != null ? "✓" : "✗")}\n" +
                $"Hand_F (Sample): {(sample != null ? "✓" : "✗")}\n" +
                $"Weapon/Hand_B (Target): {(target != null ? "✓" : "✗")}",
                "확인");
        }
        else
        {
            Debug.Log($"자동 채우기 완료\nMover: {mover.name}\nSample: {sample.name}\nTarget: {target.name}");
        }
        Repaint();
    }

    Transform FindRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            Transform found = FindRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }
}