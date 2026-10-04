using System;
using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// Окно, которое правит отдельные поля чужого раздела настроек вместо общего окна.
/// </summary>
/// <remarks>
/// Раздел целиком уводит в своё окно <c>DatabaseExternalEditor</c>. Здесь случай мельче:
/// раздел остаётся в общем окне, а часть его полей по смыслу принадлежит другому окну —
/// чувствительность мыши лежит среди стартовых значений игрока, но правят её вместе
/// с клавишами. Данные при этом не переезжают: их читает прежний код.
/// <para>
/// Общее окно находит реализации через <c>TypeCache</c>, как вкладки окна отладки,
/// и создаёт их конструктором без параметров. Поля владельца оно не рисует, а оставляет
/// строку со ссылкой на его окно. Нет реализации — поля рисуются на своём месте, поэтому
/// часть SDK без такого окна ничего не теряет.
/// </para>
/// </remarks>
public interface IPRSettingsFieldOwner
{
    /// <summary>
    /// Название окна для строки-ссылки.
    /// </summary>
    string WindowName { get; }

    /// <summary>
    /// Пункт меню, которым окно открывается.
    /// </summary>
    string MenuPath { get; }

    /// <summary>
    /// Правит ли окно это поле.
    /// </summary>
    /// <param name="sectionType">Тип раздела настроек, которому принадлежит поле.</param>
    /// <param name="fieldName">Имя поля или автосвойства.</param>
    bool Owns(Type sectionType, string fieldName);
}

/// <summary>
/// Находит окно, которому принадлежит поле настроек.
/// </summary>
internal static class PRSettingsFieldOwners
{
    private const string BackingFieldSuffix = ">k__BackingField";

    private static List<IPRSettingsFieldOwner> owners;

    /// <summary>
    /// Владелец поля или <c>null</c>, если поле рисует общее окно.
    /// </summary>
    /// <param name="sectionType">Тип раздела.</param>
    /// <param name="property">Сериализованное свойство поля.</param>
    public static IPRSettingsFieldOwner Find(Type sectionType, SerializedProperty property)
    {
        if (sectionType == null || property == null)
            return null;

        owners ??= Discover();

        string fieldName = GetMemberName(property.name);

        foreach (IPRSettingsFieldOwner owner in owners)
        {
            if (owner.Owns(sectionType, fieldName))
                return owner;
        }

        return null;
    }

    private static List<IPRSettingsFieldOwner> Discover()
    {
        var result = new List<IPRSettingsFieldOwner>();

        foreach (Type type in TypeCache.GetTypesDerivedFrom<IPRSettingsFieldOwner>())
        {
            if (type.IsAbstract || type.ContainsGenericParameters)
                continue;

            try
            {
                if (Activator.CreateInstance(type) is IPRSettingsFieldOwner owner)
                    result.Add(owner);
            }
            catch (Exception exception)
            {
                // Сломанный владелец не должен ронять окно настроек: его поля просто
                // останутся на своём месте.
                UnityEngine.Debug.LogException(exception);
            }
        }

        return result;
    }

    private static string GetMemberName(string serializedName)
    {
        return serializedName.StartsWith("<", StringComparison.Ordinal) &&
               serializedName.EndsWith(BackingFieldSuffix, StringComparison.Ordinal)
            ? serializedName.Substring(1, serializedName.Length - BackingFieldSuffix.Length - 1)
            : serializedName;
    }
}
