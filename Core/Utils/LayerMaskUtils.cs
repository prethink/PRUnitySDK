using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Работа с масками слоёв.
/// </summary>
/// <remarks>
/// Маска — это набор битов, и в коде она выглядит как обычное число: по вызову
/// <c>Physics.OverlapSphere(point, radius, ~0)</c> не понять, намеренно там «все слои»
/// или забытая заглушка. Здесь у таких значений есть имена, а у сборки маски —
/// проверка: <see cref="LayerMask.GetMask"/> на незнакомое имя молча возвращает ноль,
/// и выстрел перестаёт попадать вообще во что-либо без единой ошибки в консоли.
/// </remarks>
public static class LayerMaskUtils
{
    /// <summary>
    /// Маска, включающая все слои.
    /// </summary>
    public static int GetAnyLayer() => ~0;

    /// <summary>
    /// Пустая маска: не попадает ни один слой.
    /// </summary>
    public static int GetNoneLayer() => 0;

    /// <summary>
    /// Маска пуста — проверка по ней ничего не найдёт.
    /// </summary>
    public static bool IsEmpty(int layerMask) => layerMask == 0;

    /// <summary>
    /// Слой входит в маску.
    /// </summary>
    public static bool ContainsLayer(int layerMask, int layer)
    {
        return (layerMask & (1 << layer)) != 0;
    }

    /// <summary>
    /// Объект лежит на слое из маски.
    /// </summary>
    /// <remarks>
    /// Самая частая проверка на практике: коллайдер уже на руках, а его слой каждый раз
    /// доставали вручную.
    /// </remarks>
    public static bool Contains(int layerMask, GameObject target)
    {
        return target != null && ContainsLayer(layerMask, target.layer);
    }

    /// <summary>
    /// Компонент принадлежит объекту на слое из маски.
    /// </summary>
    public static bool Contains(int layerMask, Component target)
    {
        return target != null && ContainsLayer(layerMask, target.gameObject.layer);
    }

    /// <summary>
    /// Собирает маску из имён слоёв.
    /// </summary>
    /// <remarks>
    /// В отличие от <see cref="LayerMask.GetMask"/> сообщает о слое, которого нет
    /// в проекте: без этого опечатка в имени превращает маску в пустую, и физика
    /// молча перестаёт что-либо находить.
    /// </remarks>
    public static int Create(params string[] layerNames)
    {
        if (layerNames == null || layerNames.Length == 0)
            return GetNoneLayer();

        var mask = 0;

        foreach (string name in layerNames)
        {
            int layer = LayerMask.NameToLayer(name);

            if (layer < 0)
            {
                PRLog.WriteWarning(typeof(LayerMaskUtils), $"Слоя «{name}» нет в проекте — он пропущен в маске.");
                continue;
            }

            mask |= 1 << layer;
        }

        return mask;
    }

    /// <summary>
    /// Добавляет слой в маску.
    /// </summary>
    public static int Add(int layerMask, int layer)
    {
        return layerMask | (1 << layer);
    }

    /// <summary>
    /// Убирает слой из маски.
    /// </summary>
    /// <remarks>
    /// Пригодится, чтобы выстрел не задевал самого стрелка: маска берётся общая,
    /// а слой владельца из неё убирается.
    /// </remarks>
    public static int Remove(int layerMask, int layer)
    {
        return layerMask & ~(1 << layer);
    }

    /// <summary>
    /// Объединяет маски.
    /// </summary>
    public static int Combine(params int[] layerMasks)
    {
        var mask = 0;

        if (layerMasks == null)
            return mask;

        foreach (int layerMask in layerMasks)
            mask |= layerMask;

        return mask;
    }

    /// <summary>
    /// Убирает из маски всё, что входит во вторую.
    /// </summary>
    public static int Exclude(int layerMask, int excludedMask)
    {
        return layerMask & ~excludedMask;
    }

    /// <summary>
    /// Слои, входящие в маску.
    /// </summary>
    /// <remarks>
    /// Для отладки и подписей: увидеть маску числом бесполезно, а списком слоёв — сразу
    /// понятно, во что она попадает.
    /// </remarks>
    public static IEnumerable<int> GetLayers(int layerMask)
    {
        for (var layer = 0; layer < 32; layer++)
        {
            if (ContainsLayer(layerMask, layer))
                yield return layer;
        }
    }

    /// <summary>
    /// Имена слоёв маски через запятую.
    /// </summary>
    public static string Describe(int layerMask)
    {
        if (IsEmpty(layerMask))
            return "нет слоёв";

        var names = new List<string>();

        foreach (int layer in GetLayers(layerMask))
        {
            string name = LayerMask.LayerToName(layer);
            names.Add(string.IsNullOrEmpty(name) ? layer.ToString() : name);
        }

        // Все 32 слоя перечислять незачем: маска «всё» встречается чаще любой другой,
        // и списком из безымянных номеров она только сбивает.
        return names.Count == 32 ? "все слои" : string.Join(", ", names);
    }
}
