using UnityEngine;
using UnityEditor;

public class MainBoss1_ResetPose
{
    [MenuItem("Tools/1/MainBoss 1/Reset to Rest Pose")]
    public static void ResetPose()
    {
        GameObject root = GameObject.Find("MainBoss 1");
        if (root == null) { EditorUtility.DisplayDialog("Reset Pose", "씬에서 'MainBoss 1' 오브젝트를 찾을 수 없습니다.", "OK"); return; }
        Undo.RegisterFullObjectHierarchyUndo(root, "Reset MainBoss 1 Rest Pose");
        Apply(root, false);
        Debug.Log("[MainBoss 1] Rest pose applied.");
    }

    [MenuItem("Tools/1/MainBoss 1/Reset to Two-Handed Grip Pose")]
    public static void ResetTwoHandedPose()
    {
        GameObject root = GameObject.Find("MainBoss 1");
        if (root == null) { EditorUtility.DisplayDialog("Reset Pose", "씬에서 'MainBoss 1' 오브젝트를 찾을 수 없습니다.", "OK"); return; }
        Undo.RegisterFullObjectHierarchyUndo(root, "Reset MainBoss 1 Two-Handed Grip Pose");
        Apply(root, true);
        Debug.Log("[MainBoss 1] Two-handed grip pose applied.");
    }

    static void Apply(GameObject root, bool twoHanded)
    {
        void SP(string path, Vector3 pos, float rotZ)
        {
            Transform t = root.transform.Find(path);
            if (t == null) { Debug.LogWarning($"[ResetPose] 못 찾음: {path}"); return; }
            t.localPosition = pos;
            t.localEulerAngles = new Vector3(0f, 0f, rotZ);
        }
        void SR(string path, float rotZ)
        {
            Transform t = root.transform.Find(path);
            if (t == null) { Debug.LogWarning($"[ResetPose] 못 찾음: {path}"); return; }
            t.localEulerAngles = new Vector3(0f, 0f, rotZ);
        }

        // 공통
        SP("Body", new Vector3(0.00f, 0.00f, 0f), 0f);
        SP("Body/Head", new Vector3(0.00f, 0.70f, 0f), -2f);
        SP("Body/Bottom", new Vector3(-0.20f, -0.30f, 0f), 0f);
        SP("Body/Bottom/Thigh_F", new Vector3(-0.35f, -0.15f, 0f), 0f);
        SR("Body/Bottom/Thigh_F/Calf_F", 0f);
        SR("Body/Bottom/Thigh_F/Calf_F/Foot_F", 0f);
        SP("Body/Bottom/Thigh_B", new Vector3(0.07f, -0.215f, 0f), 0f);
        SR("Body/Bottom/Thigh_B/Calf_B", 0f);
        SR("Body/Bottom/Thigh_B/Calf_B/Foot_B", 0f);

        // 오른팔(B): 공통
        SP("Body/Shoulder_B", new Vector3(0.26f, 0.55f, 0f), 0f);
        SR("Body/Shoulder_B/Forearm_B", 0f);
        SP("Body/Shoulder_B/Forearm_B/Hand_B", new Vector3(0.05f, -0.54f, 0f), -20f);
        SP("Body/Shoulder_B/Forearm_B/Hand_B/Weapon", new Vector3(0.13f, -0.28f, 0f), -90f);

        if (!twoHanded)
        {
            // ── Rest: 왼손만 잡은 자세 ──────────────────────────────
            SP("Body/Shoulder_F", new Vector3(-0.575f, 0.50f, 0f), 0f);
            SR("Body/Shoulder_F/Forearm_F", 0f);
            SR("Body/Shoulder_F/Forearm_F/Hand_F", 0f);
        }
        else
        {
            // ── 양손 검 자세 (실측) ──────────────────────────────────
            SP("Body/Shoulder_F", new Vector3(-0.38f, 0.34f, 0f), 20f);
            SR("Body/Shoulder_F/Forearm_F", 15f);
            SR("Body/Shoulder_F/Forearm_F/Hand_F", 0f);
            // 오른팔: SB rot=0, FB rot=0, HB rot=0, Weapon rot=-90
            SP("Body/Shoulder_B", new Vector3(0.26f, 0.55f, 0f), 0f);
            SR("Body/Shoulder_B/Forearm_B", 0f);
            SP("Body/Shoulder_B/Forearm_B/Hand_B", new Vector3(0.05f, -0.54f, 0f), 0f);
            SP("Body/Shoulder_B/Forearm_B/Hand_B/Weapon", new Vector3(0.13f, -0.28f, 0f), -90f);
        }
    }
}