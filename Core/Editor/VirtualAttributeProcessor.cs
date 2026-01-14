using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

// Абстрактная “виртуальная система атрибутов”
public abstract class VirtualAttributeProcessor<T> : Editor where T : UnityEngine.Object
{
    /// <summary>
    /// Аналог Odin ProcessChildMemberAttributes
    /// Здесь передаются уже существующие атрибуты поля/свойства
    /// Можно добавить новые виртуальные атрибуты
    /// </summary>
    /// <param name="prop">SerializedProperty текущего поля</param>
    /// <param name="member">MemberInfo поля/свойства</param>
    /// <param name="attributes">Список всех текущих атрибутов, сюда можно добавить новые</param>
    protected abstract void ProcessMemberAttributes(SerializedProperty prop, MemberInfo member, List<Attribute> attributes);

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var targetType = typeof(T);

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            // Получаем MemberInfo поля
            var member = targetType.GetMember(prop.name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault();

            // Берем все реальные атрибуты поля/свойства
            var currentAttributes = new List<Attribute>();
            if (member != null)
            {
                currentAttributes.AddRange(member.GetCustomAttributes(true).Cast<Attribute>());

                // Вызываем метод, где пользователь может добавить виртуальные атрибуты
                ProcessMemberAttributes(prop, member, currentAttributes);
            }

            // После этого Inspector отрисовывает поле как обычно
            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
