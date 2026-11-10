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
        EditorGUILayout.HelpBox("손잡이 중심의 베기 궤적을 원본 Slash 질감으로 감쌉니다. 세로베기는 큰 원호, 가로베기는 낮은 타원으로 표현합니다.",MessageType.Info);
        {
            foreach(string field in new[]{"verticalSlash","horizontalSlash","verticalAngle","horizontalAngle","basicAttackPoses"})
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
