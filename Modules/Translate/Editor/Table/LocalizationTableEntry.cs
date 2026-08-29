using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Одна строка таблицы переводов: где лежит значение и что в нём сейчас.
/// </summary>
/// <remarks>
/// Адрес собирается из пути к ассету, пути к объекту внутри него и пути к свойству.
/// По нему импорт находит то же самое место, даже если ключ перевода изменился или его
/// вовсе нет: у предметов ключ вычисляется из имени и в сохранение не попадает.
/// </remarks>
public sealed class LocalizationTableEntry
{
    /// <summary>
    /// Ассет, в котором лежит перевод.
    /// </summary>
    public string AssetPath { get; set; } = string.Empty;

    /// <summary>
    /// Путь к объекту внутри префаба и компонент на нём: <c>Panel/Title#0</c>.
    /// Пусто для ScriptableObject.
    /// </summary>
    public string ObjectPath { get; set; } = string.Empty;

    /// <summary>
    /// Путь к сериализованному свойству словаря.
    /// </summary>
    public string PropertyPath { get; set; } = string.Empty;

    /// <summary>
    /// Ключ перевода, если он есть рядом со словарём.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Группа записи, если она у неё есть.
    /// </summary>
    /// <remarks>
    /// Группу хранит <see cref="LocalizationControl"/> — значит она есть и у записей
    /// общего списка, и у подписей на префабах. У предметов словарь лежит без обёртки,
    /// и группа там пустая: такой перевод принадлежит объекту, его видно по владельцу.
    /// </remarks>
    public string Group { get; set; } = string.Empty;

    /// <summary>
    /// Откуда строка: база, ассет или префаб.
    /// </summary>
    public LocalizationTableSource Source { get; set; }

    /// <summary>
    /// Человекочитаемое имя объекта — чтобы переводчик понимал, что переводит.
    /// </summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// Значения по языкам.
    /// </summary>
    public Dictionary<LangType, string> Values { get; } = new();

    /// <summary>
    /// Объект, которому принадлежит перевод.
    /// </summary>
    public Object Target { get; set; }

    /// <summary>
    /// Строковый адрес для таблицы.
    /// </summary>
    public string Address => $"{AssetPath}|{ObjectPath}|{PropertyPath}";
}

/// <summary>
/// Откуда взялась строка таблицы.
/// </summary>
public enum LocalizationTableSource
{
    /// <summary>
    /// Общий список переводов в базе SDK.
    /// </summary>
    Database,

    /// <summary>
    /// Ассет: предмет, награда, определение.
    /// </summary>
    Asset,

    /// <summary>
    /// Компонент на префабе.
    /// </summary>
    Prefab
}
