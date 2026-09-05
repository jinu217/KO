#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(BossSkillData))]
public class BossSkillDataDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        Rect line = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, GetLabel(property, label), true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            DrawProperty(ref line, property.FindPropertyRelative("skillName"));
            DrawProperty(ref line, property.FindPropertyRelative("skillType"));
            DrawProperty(ref line, property.FindPropertyRelative("executionOrder"));
            DrawProperty(ref line, property.FindPropertyRelative("cooldown"));
            DrawProperty(ref line, property.FindPropertyRelative("minimumPhase"));
            DrawProperty(ref line, property.FindPropertyRelative("castRange"));
            DrawProperty(ref line, property.FindPropertyRelative("forceCastMinDistance"));
            DrawProperty(ref line, property.FindPropertyRelative("forceCastRemainingCooldown"));
            DrawProperty(ref line, property.FindPropertyRelative("preDelay"));
            DrawProperty(ref line, property.FindPropertyRelative("postDelay"));

            BossSkillType type = (BossSkillType)property.FindPropertyRelative("skillType").enumValueIndex;
            if (type == BossSkillType.Active || type == BossSkillType.ActiveAndBuff)
                DrawProperty(ref line, property.FindPropertyRelative("active"), true);

            if (type == BossSkillType.Buff || type == BossSkillType.ActiveAndBuff)
                DrawProperty(ref line, property.FindPropertyRelative("buff"), true);

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = EditorGUIUtility.singleLineHeight;
        if (!property.isExpanded)
            return height;

        height += Height(property.FindPropertyRelative("skillName"));
        height += Height(property.FindPropertyRelative("skillType"));
        height += Height(property.FindPropertyRelative("executionOrder"));
        height += Height(property.FindPropertyRelative("cooldown"));
        height += Height(property.FindPropertyRelative("minimumPhase"));
        height += Height(property.FindPropertyRelative("castRange"));
        height += Height(property.FindPropertyRelative("forceCastMinDistance"));
        height += Height(property.FindPropertyRelative("forceCastRemainingCooldown"));
        height += Height(property.FindPropertyRelative("preDelay"));
        height += Height(property.FindPropertyRelative("postDelay"));

        BossSkillType type = (BossSkillType)property.FindPropertyRelative("skillType").enumValueIndex;
        if (type == BossSkillType.Active || type == BossSkillType.ActiveAndBuff)
            height += Height(property.FindPropertyRelative("active"), true);

        if (type == BossSkillType.Buff || type == BossSkillType.ActiveAndBuff)
            height += Height(property.FindPropertyRelative("buff"), true);

        return height;
    }

    private static void DrawProperty(ref Rect line, SerializedProperty property, bool includeChildren = false)
    {
        line.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
        line.height = EditorGUI.GetPropertyHeight(property, includeChildren);
        EditorGUI.PropertyField(line, property, includeChildren);
        line.y += line.height - EditorGUIUtility.singleLineHeight;
    }

    private static float Height(SerializedProperty property, bool includeChildren = false)
    {
        return EditorGUI.GetPropertyHeight(property, includeChildren)
            + EditorGUIUtility.standardVerticalSpacing;
    }

    private static GUIContent GetLabel(SerializedProperty property, GUIContent fallback)
    {
        string skillName = property.FindPropertyRelative("skillName").stringValue;
        return string.IsNullOrWhiteSpace(skillName) ? fallback : new GUIContent(skillName);
    }
}
#endif
