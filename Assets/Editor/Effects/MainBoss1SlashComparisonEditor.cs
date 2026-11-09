#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MainBoss1SlashComparison))]
public sealed class MainBoss1SlashComparisonEditor : Editor
{
    [MenuItem("Tools/Effects/Bosses/메보1 Slash 비교 설정")]
    [MenuItem("Tools/Effects/Bosses/메인보스 이펙트 3종 비교")]
    public static void Open()
    {
        Selection.activeObject = Resources.Load<MainBoss1SlashComparison>("MainBoss1SlashComparison");
        EditorGUIUtility.PingObject(Selection.activeObject);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("현재/이전 방식에 더해 메인보스 1~5의 모션 기준 방식을 비교합니다. 이전 방식은 메보1에만 적용되며 중간보스는 변경하지 않습니다. 모션·판정 수치는 그대로입니다.", MessageType.Info);
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
                if (trail.source != null && trail.source.mainBoss) trail.Clear();
            SceneView.RepaintAll();
            EditorApplication.QueuePlayerLoopUpdate();
        }
        EditorGUILayout.HelpBox("현재 방식: 여러 Slash 잔상\n이전 방식: 메보1 패턴표의 가로/세로 순서\n새 방식: 실제 모션 우선, 회전은 한 장의 Slash, 찌르기는 Sting, 권투는 주먹 이펙트\n\n세로 베기·회전에 Sting을 사용하지 않습니다. 기존 이펙트가 끝난 뒤 다음 공격부터 비교하세요. 선택은 실행 종료 후에도 유지됩니다.", MessageType.None);
    }
}
#endif
