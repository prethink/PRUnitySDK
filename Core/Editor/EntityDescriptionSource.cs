using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Откуда сущность берёт описание.
/// </summary>
/// <remarks>
/// Поле у каждой сущности своё - <c>Metadata</c>, <c>Definition</c>, <c>kindInfo</c>, -
/// поэтому ищется оно по типу, а не по имени: список имён пришлось бы пополнять при
/// каждом новом виде сущности.
/// </remarks>
public static class EntityDescriptionSource
{
    /// <summary>
    /// Ассет описания, на который ссылается сущность.
    /// </summary>
    /// <param name="entity">Сущность.</param>
    /// <returns>Описание или определение; <c>null</c>, если ссылки нет или поля нет вовсе.</returns>
    public static UnityEngine.Object Resolve(EntityBase entity)
    {
        if (entity == null)
            return null;

        using var serialized = new SerializedObject(entity);
        Type entityType = entity.GetType();

        foreach (SerializedProperty property in PRSDKInspectorUtility.GetRootProperties(serialized))
        {
            if (!IsDescriptionField(entityType, property))
                continue;

            if (property.objectReferenceValue != null)
                return property.objectReferenceValue;
        }

        return null;
    }

    /// <summary>
    /// Есть ли у сущности поле под описание.
    /// </summary>
    /// <remarks>
    /// Отличает «ссылку забыли проставить» от «описание берётся иначе»: сущность, которая
    /// описывает себя сама, поля не имеет, и требовать от неё ссылку не за что.
    /// </remarks>
    public static bool HasField(EntityBase entity)
    {
        if (entity == null)
            return false;

        using var serialized = new SerializedObject(entity);
        Type entityType = entity.GetType();

        foreach (SerializedProperty property in PRSDKInspectorUtility.GetRootProperties(serialized))
        {
            if (IsDescriptionField(entityType, property))
                return true;
        }

        return false;
    }

    private static bool IsDescriptionField(Type entityType, SerializedProperty property)
    {
        if (property.propertyType != SerializedPropertyType.ObjectReference)
            return false;

        Type fieldType = PRSDKInspectorUtility.GetFieldType(entityType, property);

        if (fieldType == null)
            return false;

        return typeof(EntityMetadataBase).IsAssignableFrom(fieldType)
            || typeof(ItemDefinitionBase).IsAssignableFrom(fieldType);
    }
}
