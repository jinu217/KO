using UnityEngine;
using UnityEditor;

/// <summary>
/// MainBoss 1 rest 포즈를 씬의 오브젝트에 즉시 적용합니다.
/// Tools > MainBoss 1 > Reset to Rest Pose
/// </summary>
public class MainBoss1_ResetPose
{
    [MenuItem("Tools/MainBoss 1/Reset to Rest Pose")]
    public static void ResetPose()
    {
        GameObject root = GameObject.Find("MainBoss 1");
        if (root == null)
        {
            EditorUtility.DisplayDialog("Reset Pose", "씬에서 'MainBoss 1' 오브젝트를 찾을 수 없습니다.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(root, "Reset MainBoss 1 Rest Pose");

        void SetLocal(string path, Vector3 pos, float rotZ)
        {
            Transform t = root.transform.Find(path);
            if (t == null) { Debug.LogWarning($"[ResetPose] 못 찾음: {path}"); return; }
            t.localPosition    = pos;
            t.localEulerAngles = new Vector3(0f, 0f, rotZ);
        }

        void SetRot(string path, float rotZ)
        {
            Transform t = root.transform.Find(path);
            if (t == null) { Debug.LogWarning($"[ResetPose] 못 찾음: {path}"); return; }
            t.localEulerAngles = new Vector3(0f, 0f, rotZ);
        }

        // ── REST POSE (실측) ──────────────────────────────────────────
        SetLocal("Body",                                              new Vector3( 0.00f,  0.00f, 0f),  0f);
        SetLocal("Body/Head",                                         new Vector3( 0.00f,  0.70f, 0f), -2f);
        SetLocal("Body/Bottom",                                       new Vector3(-0.20f, -0.30f, 0f),  0f);

        SetLocal("Body/Shoulder_F",                                   new Vector3(-0.575f, 0.50f, 0f),  0f);
        SetRot  ("Body/Shoulder_F/Forearm_F",                          0f);
        SetRot  ("Body/Shoulder_F/Forearm_F/Hand_F",                   0f);

        SetLocal("Body/Shoulder_B",                                   new Vector3( 0.26f,  0.55f, 0f),  0f);
        SetRot  ("Body/Shoulder_B/Forearm_B",                          0f);
        SetLocal("Body/Shoulder_B/Forearm_B/Hand_B",                  new Vector3( 0.05f, -0.54f, 0f), -20f);
        SetLocal("Body/Shoulder_B/Forearm_B/Hand_B/Weapon",           new Vector3( 0.13f, -0.28f, 0f), -90f);

        SetLocal("Body/Bottom/Thigh_F",                               new Vector3(-0.35f, -0.15f,  0f), 0f);
        SetRot  ("Body/Bottom/Thigh_F/Calf_F",                         0f);
        SetRot  ("Body/Bottom/Thigh_F/Calf_F/Foot_F",                  0f);

        SetLocal("Body/Bottom/Thigh_B",                               new Vector3( 0.07f, -0.215f, 0f), 0f);
        SetRot  ("Body/Bottom/Thigh_B/Calf_B",                         0f);
        SetRot  ("Body/Bottom/Thigh_B/Calf_B/Foot_B",                  0f);

        Debug.Log("[MainBoss 1] Rest pose applied.");
    }
}
