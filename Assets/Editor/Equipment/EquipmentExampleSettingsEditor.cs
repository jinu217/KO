#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(EquipmentExampleSettings))]
public sealed class EquipmentExampleSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox("부품 Position과 Order in Layer를 조정한 후 저장하세요. 모션 자세를 제외한 위치 보정만 기록합니다. 회전·스케일은 저장하지 않습니다.", MessageType.Info);
        if (GUILayout.Button("위치·정렬을 장비 설정에 저장")) Save((EquipmentExampleSettings)target);
        DrawDefaultInspector();
    }
    public static void Save(EquipmentExampleSettings example)
    {
        if (example == null || Application.isPlaying) return;
        var valid = example.parts.Where(p => p != null && p.item != null && p.renderer != null).ToArray();
        var items = valid.Select(p => p.item).Distinct().ToArray();
        Undo.RecordObjects(items, "장비 위치·정렬 설정 저장");
        foreach (var part in valid)
        {
            if (part.item.visualAdjustments == null) part.item.visualAdjustments = new System.Collections.Generic.List<EquipmentVisualAdjustment>();
            var a = part.item.visualAdjustments.Find(x => x.part == part.part);
            if (a == null) { a = new EquipmentVisualAdjustment { part = part.part }; part.item.visualAdjustments.Add(a); }
            a.positionOffset = example.transform.InverseTransformPoint(part.renderer.transform.position) - part.baseRootPosition;
            a.overrideSortingOrder = true; a.sortingOrder = part.renderer.sortingOrder;
            EditorUtility.SetDirty(part.item);
        }
        foreach (var item in items) AssetDatabase.SaveAssetIfDirty(item);
        Debug.Log("장비 위치·정렬 저장 완료: " + items.Length + "개 ItemData", example);
    }
}
#endif
