using UnityEditor;
using UnityEngine;

/// <summary>
/// Инспектор компонента состояния объекта.
/// </summary>
/// <remarks>
/// <para>
/// Ключ править руками нельзя: он связывает объект с записью в сохранении, и опечатка
/// ничего не ломает заметно — состояние перестаёт находиться, а объект встаёт в значения
/// по умолчанию. Поэтому поле показывается только для чтения, а новый ключ выдаётся
/// кнопкой с подтверждением.
/// </para>
/// <para>
/// Написан вручную, хотя атрибуты плагина сделали бы то же короче: плагин лежит
/// в проектном слое, а компонент — в SDK, и такая ссылка развернула бы зависимость
/// не в ту сторону.
/// </para>
/// </remarks>
[CustomEditor(typeof(SaveableObjectState), editorForChildClasses: true)]
[CanEditMultipleObjects]
public class SaveableObjectStateEditor : Editor
{
    private const string ScriptProperty = "m_Script";
    private const string OwnIdProperty = "ownId";
    private const string IdSourceProperty = "idSource";
    private const string SaveActiveProperty = "saveActiveState";
    private const string DefaultActiveProperty = "defaultIsActive";
    private const string GroupProperty = "group";
    private const string OverrideGroupProperty = "overrideEntityGroup";

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty idSource = serializedObject.FindProperty(IdSourceProperty);
        SerializedProperty saveActiveState = serializedObject.FindProperty(SaveActiveProperty);
        SerializedProperty overrideGroup = serializedObject.FindProperty(OverrideGroupProperty);

        // Тип сущности есть не у всех: у обычного объекта выбирать не из чего,
        // и галку показывать незачем.
        bool hasEntityType = HasEntityType();
        bool usesEntityType = hasEntityType && overrideGroup != null && !overrideGroup.boolValue;

        SerializedProperty iterator = serializedObject.GetIterator();
        bool enterChildren = true;

        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;

            switch (iterator.propertyPath)
            {
                case ScriptProperty:
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.PropertyField(iterator);
                    continue;

                case OwnIdProperty:
                    // Ключ нужен только своему источнику: у чужого он всё равно не читается.
                    if (!IsOwnId(idSource))
                        continue;

                    DrawOwnId(iterator);
                    continue;

                case OverrideGroupProperty:
                    if (!hasEntityType)
                        continue;

                    EditorGUILayout.PropertyField(iterator, true);
                    continue;

                case GroupProperty:
                    if (usesEntityType)
                    {
                        EditorGUILayout.HelpBox(
                            "Группа берётся из типа сущности. Чтобы задать её вручную, включите галку выше.",
                            MessageType.None);

                        continue;
                    }

                    EditorGUILayout.PropertyField(iterator, true);
                    continue;

                case DefaultActiveProperty:
                    // Значение по умолчанию без хранения активности ни на что не влияет.
                    if (saveActiveState != null && !saveActiveState.hasMultipleDifferentValues && !saveActiveState.boolValue)
                        continue;

                    EditorGUILayout.PropertyField(iterator, true);
                    continue;

                default:
                    EditorGUILayout.PropertyField(iterator, true);
                    continue;
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// Объект, чьё состояние хранится, является сущностью.
    /// </summary>
    /// <remarks>
    /// Спрашиваем у самого компонента, а не ищем связь здесь: правило, что считать
    /// сущностью, одно и живёт в нём. При нескольких выделенных объектах достаточно,
    /// чтобы сущностью был хотя бы один, — иначе галка пропала бы у всей группы
    /// из-за одного постороннего объекта.
    /// </remarks>
    private bool HasEntityType()
    {
        foreach (UnityEngine.Object candidate in targets)
        {
            if (candidate is SaveableObjectState state && state.HasEntityType)
                return true;
        }

        return false;
    }

    private void DrawOwnId(SerializedProperty ownId)
    {
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(ownId);

        if (!ownId.hasMultipleDifferentValues && string.IsNullOrEmpty(ownId.stringValue))
        {
            EditorGUILayout.HelpBox(
                "Ключ не задан: состояние этого объекта сохраняться не будет.",
                MessageType.Warning);
        }

        if (!GUILayout.Button("Сгенерировать новый идентификатор"))
            return;

        if (!Confirm(ownId))
            return;

        // Правим через SerializedProperty, а не через сам компонент: так работает
        // отмена изменений, а сцена помечается изменённой.
        ownId.stringValue = SaveableObjectState.CreateId();
    }

    /// <summary>
    /// Спрашивает подтверждение на смену идентификатора.
    /// </summary>
    /// <remarks>
    /// Ключ меняют редко и почти всегда осознанно — например разводя два объекта,
    /// которым он достался общим при копировании. Но прежнее сохранённое состояние
    /// после этого не найдётся, и объект встанет в значения по умолчанию, поэтому
    /// спрашиваем прямо.
    /// </remarks>
    private bool Confirm(SerializedProperty ownId)
    {
        string current = ownId.hasMultipleDifferentValues ? "разные у выбранных объектов" : ownId.stringValue;

        return EditorUtility.DisplayDialog(
            "Сгенерировать новый идентификатор?",
            $"Текущий ключ: {current}.\n\n" +
            "Прежнее сохранённое состояние этого объекта после смены ключа не найдётся, " +
            "и объект появится со значениями по умолчанию.",
            "Сгенерировать",
            "Отмена");
    }

    private static bool IsOwnId(SerializedProperty idSource)
    {
        if (idSource == null)
            return true;

        if (idSource.hasMultipleDifferentValues)
            return true;

        return idSource.enumValueIndex == (int)SaveableIdSource.OwnId;
    }
}
