#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MainBoss1SlashComparison))]
public sealed class MainBoss1SlashComparisonEditor : Editor
{
    [MenuItem("Tools/Effects/Bosses/메보1 Slash 비교 설정")]
    public static void Open()
    {
        Selection.activeObject = Resources.Load<MainBoss1SlashComparison>("MainBoss1SlashComparison");
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("메보1 전용입니다. 게임 실행 중 또는 Animation 창 재생 중에도 비교할 수 있습니다. 모션·판정·다른 보스는 그대로 유지됩니다.", MessageType.Info);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("mode"), new GUIContent("이펙트 방식"));
        if (serializedObject.FindProperty("mode").enumValueIndex == (int)MainBoss1SlashComparison.DisplayMode.OriginalSlash)
        {
            EditorGUILayout.HelpBox("세로: slash_big / 가로: slash_medium을 가로로 회전.\n일반: 세로→가로 / 1스킬: 세로→세로→가로\n강화 1스킬: 세로→가로→세로→가로\n욕망 기본: 세로→가로→세로\n욕망 1스킬: 세로→세로→가로→세로→가로\n4스킬: 가로 베기 / 검기와 버프는 유지", MessageType.None);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("verticalSlash"), new GUIContent("세로 베기"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("horizontalSlash"), new GUIContent("가로 베기"), true);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("verticalAngle"), new GUIContent("세로 이펙트 각도"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("horizontalAngle"), new GUIContent("가로 이펙트 각도"));
        }
        if (serializedObject.ApplyModifiedProperties())
        {
            foreach (var trail in Resources.FindObjectsOfTypeAll<BossWeaponSlashTrail>())
                if (trail.source != null && trail.source.mainBoss && trail.source.stage == 1) trail.Clear();
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }
        EditorGUILayout.HelpBox("현재 방식: 여러 Slash 잔상을 좁혀 무기 뒤에 배치\n이전 방식: Slash 원래 비율로 시트 애니메이션 재생\n\n기존에 나온 이펙트가 끝난 뒤 다음 베기부터 비교하세요. 이 설정은 실행을 종료해도 유지됩니다.", MessageType.None);
    }
}
#endif
