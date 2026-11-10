#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(MainBoss1SlashComparison))]
public sealed class MainBoss1SlashComparisonEditor : Editor
{
    [MenuItem("Tools/Effects/Bosses/메보1 Slash 비교 설정")]
    [MenuItem("Tools/Effects/Bosses/보스 베기 이펙트 설정")]
    public static void Open()
    {
        Selection.activeObject=Resources.Load<MainBoss1SlashComparison>("MainBoss1SlashComparison");
        EditorGUIUtility.PingObject(Selection.activeObject);
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("원본 비율과 타격 타이밍을 유지하면서 무기 뒤를 따라갑니다. 크기는 원본과 무기 길이의 중간으로 조율합니다.",MessageType.Info);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mode"),new GUIContent("메보1 이펙트 방식"));
        if(serializedObject.FindProperty("mode").intValue==1)
        {
            foreach(string field in new[]{"verticalSlash","horizontalSlash","verticalAngle","horizontalAngle"})
                EditorGUILayout.PropertyField(serializedObject.FindProperty(field),true);
        }
        if(serializedObject.ApplyModifiedProperties())
        {
            foreach(var trail in Resources.FindObjectsOfTypeAll<BossWeaponSlashTrail>())trail.Clear();
            SceneView.RepaintAll();EditorApplication.QueuePlayerLoopUpdate();
        }
    }
}
#endif
