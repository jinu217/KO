using UnityEngine;
using UnityEditor;

public class Smith_SetRestPose
{
    [MenuItem("Tools/NPC1/Smith/Set Rest Pose")]
    public static void SetPose()
    {
        var go = GameObject.Find("Smith");
        if (go == null)
        {
            EditorUtility.DisplayDialog("오류", "Hierarchy에서 Smith를 찾을 수 없습니다", "확인");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(go, "Smith Set Rest Pose");

        void Set(string path, Vector3 pos, float rotZ)
        {
            var t = go.transform.Find(path);
            if (t == null) { Debug.LogWarning($"[Smith] 경로 없음: {path}"); return; }
            t.localPosition = pos;
            t.localEulerAngles = new Vector3(0f, 0f, rotZ);
        }

        // rest 실측값
        Set("Body",                          new Vector3( 0.000f,  0.000f, 0f), 0f);
        Set("Body/Head",                     new Vector3(-0.170f,  0.710f, 0f), 0f);
        Set("Body/Shoulder_F",               new Vector3(-0.610f,  0.536f, 0f), 0f);
        Set("Body/Shoulder_F/Hand_F",        new Vector3( 0.000f, -0.317f, 0f), 0f);
        Set("Body/Shoulder_B",               new Vector3( 0.137f,  0.536f, 0f), 0f);
        Set("Body/Shoulder_B/Hand_B",        new Vector3( 0.079f, -0.300f, 0f), 0f);
        Set("Body/Bottom",                   new Vector3( 0.000f, -0.300f, 0f), 0f);
        Set("Body/Bottom/Foot_F",            new Vector3(-0.480f, -0.190f, 0f), 0f);
        Set("Body/Bottom/Foot_B",            new Vector3( 0.005f, -0.190f, 0f), 0f);

        Debug.Log("[Smith] Rest Pose 설정 완료");
    }
}
