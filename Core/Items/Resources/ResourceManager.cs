using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceManager : SingletonProviderBase<ResourceManager>
{
    private readonly ProjectDataMap<string, long> resources;

    public ResourceManager()
    {
        resources = new ProjectDataMap<string, long>(
            GetProjectData,
            projectData => projectData.Resources ??= new Dictionary<string, long>());
    }

    /// <summary>
    /// Пытается получить сохранённое значение ресурса, не создавая новый ключ.
    /// </summary>
    public bool TryGetResource(Enumeration resourceType, out long value)
    {
        value = 0;
        return TryGetResourceName(resourceType, out var resourceName)
            && resources.TryGetValue(resourceName, out value);
    }

    /// <summary>
    /// Пытается получить сохранённое значение ресурса по definition.
    /// </summary>
    public bool TryGetResource(ResourceItemDefinition resource, out long value)
    {
        value = 0;
        return TryGetResourceType(resource, out var resourceType)
            && TryGetResource(resourceType, out value);
    }

    /// <summary>
    /// Возвращает значение ресурса или fallback, не создавая новый ключ.
    /// </summary>
    public long GetResource(Enumeration resourceType, long fallback = 0)
    {
        return TryGetResourceName(resourceType, out var resourceName)
            ? resources.GetValue(resourceName, fallback)
            : fallback;
    }

    /// <summary>
    /// Возвращает значение ресурса по definition или fallback.
    /// </summary>
    public long GetResource(ResourceItemDefinition resource, long fallback = 0)
    {
        return TryGetResourceType(resource, out var resourceType)
            ? GetResource(resourceType, fallback)
            : fallback;
    }

    /// <summary>
    /// Возвращает существующее значение ресурса или создаёт его со значением 0.
    /// Метод сохранён для обратной совместимости; для обычного чтения используйте
    /// <see cref="GetResource(Enumeration, long)"/>.
    /// </summary>
    /// <param name="resourceType">Тип ресурса.</param>
    /// <returns>Значение ресурса.</returns>
    public long GetOrCreateResource(Enumeration resourceType)
    {
        if (!TryGetResourceName(resourceType, out var resourceName))
            return 0;

        return resources.GetOrCreateValue(resourceName, 0).CurrentValue;
    }

    /// <summary>
    /// Возвращает значение ресурса по definition или создаёт его со значением 0.
    /// </summary>
    public long GetOrCreateResource(ResourceItemDefinition resource)
    {
        return TryGetResourceType(resource, out var resourceType)
            ? GetOrCreateResource(resourceType)
            : 0;
    }

    /// <summary>
    /// Устанавливает значение ресурса. Сохранение и уведомление выполняются только
    /// при фактическом изменении значения.
    /// </summary>
    /// <param name="resourceType">Тип ресурса.</param>
    /// <param name="value">Значение.</param>
    /// <param name="requiredNotify">Признак того, что требуется оповестить об изменение ресурса.</param>
    /// <param name="requiredSaveNow">Признак того, что требуется сохранить данные после изменения ресурса.</param>
    public void SetOrUpdateResource(Enumeration resourceType, long value, bool requiredNotify = false, bool requiredSaveNow = false)
    {
        if (!TryGetResourceName(resourceType, out var resourceName))
            return;

        var change = resources.SetValue(resourceName, value);
        if (!change.Changed)
            return;

        if (requiredSaveNow)
            GameManager.Instance.SaveProjectData();

        if (requiredNotify)
        {
            ResourceEvents.RaiseResourceValueChange(
                new ResourceValueChangeEventArgs(
                    resourceType,
                    change.HadPreviousValue ? change.PreviousValue : 0,
                    change.CurrentValue));
        }
    }

    /// <summary>
    /// Устанавливает значение ресурса по definition.
    /// </summary>
    public void SetOrUpdateResource(
        ResourceItemDefinition resource,
        long value,
        bool requiredNotify = false,
        bool requiredSaveNow = false)
    {
        if (!TryGetResourceType(resource, out var resourceType))
            return;

        SetOrUpdateResource(resourceType, value, requiredNotify, requiredSaveNow);
    }

    /// <summary>
    /// Прибавляет addValue к текущему значению ресурса.
    /// </summary>
    /// <param name="resourceType">Тип ресурса.</param>
    /// <param name="addValue">Добавляемое значение.</param>
    /// <param name="requiredNotify">Нужно ли публиковать событие изменения.</param>
    /// <param name="requiredSaveNow">Нужно ли сразу сохранять ProjectData.</param>
    public void AddResourceValue(Enumeration resourceType, long addValue, bool requiredNotify = false, bool requiredSaveNow = false)
    {
        long startValue = GetResource(resourceType);
        var targetValue = startValue + addValue;
        SetOrUpdateResource(resourceType, targetValue, requiredNotify, requiredSaveNow);
    }

    /// <summary>
    /// Прибавляет значение к ресурсу, заданному через definition.
    /// </summary>
    public void AddResourceValue(
        ResourceItemDefinition resource,
        long addValue,
        bool requiredNotify = false,
        bool requiredSaveNow = false)
    {
        if (!TryGetResourceType(resource, out var resourceType))
            return;

        AddResourceValue(resourceType, addValue, requiredNotify, requiredSaveNow);
    }

    /// <summary>
    /// Атомарно проверяет баланс и списывает amount, если ресурса достаточно.
    /// </summary>
    public bool TrySpendResource(
        Enumeration resourceType,
        long amount,
        bool requiredNotify = false,
        bool requiredSaveNow = false)
    {
        if (!TryGetResourceName(resourceType, out _))
            return false;

        if (amount < 0)
        {
            PRLog.WriteWarning(this, "Cannot spend a negative resource amount.");
            return false;
        }

        var currentValue = GetResource(resourceType);
        if (currentValue < amount)
            return false;

        SetOrUpdateResource(
            resourceType,
            currentValue - amount,
            requiredNotify,
            requiredSaveNow);

        return true;
    }

    /// <summary>
    /// Проверяет баланс и списывает ресурс, заданный через definition.
    /// </summary>
    public bool TrySpendResource(
        ResourceItemDefinition resource,
        long amount,
        bool requiredNotify = false,
        bool requiredSaveNow = false)
    {
        return TryGetResourceType(resource, out var resourceType)
            && TrySpendResource(
                resourceType,
                amount,
                requiredNotify,
                requiredSaveNow);
    }

    /// <summary>
    /// Постепенно изменяет фактическое значение ресурса корутиной.
    /// Для визуальной интерполяции UI предпочтительнее сразу записать итоговое
    /// значение и анимировать представление по ResourceValueChangeEventArgs.
    /// </summary>
    public void UpdateResourceValueSmooth(Enumeration resourceType, long targetValue, float duration, bool requiredNotify = false, bool requiredSaveNow = false)
    {
        GameManager.Instance.StartCoroutine(SmoothUpdateCoroutine(resourceType, targetValue, duration, requiredNotify, requiredSaveNow));
    }

    /// <summary>
    /// Постепенно прибавляет значение к фактическому ресурсу корутиной.
    /// Для визуальной интерполяции UI предпочтительнее анимировать представление.
    /// </summary>
    public void AddResourceValueSmooth(Enumeration resourceType, long addValue, float duration, bool requiredNotify = false, bool requiredSaveNow = false)
    {
        long startValue = GetOrCreateResource(resourceType);
        var targetValue = startValue + addValue;
        GameManager.Instance.StartCoroutine(SmoothUpdateCoroutine(resourceType, targetValue, duration, requiredNotify, requiredSaveNow));
    }

    private IEnumerator SmoothUpdateCoroutine(Enumeration resourceType, long targetValue, float duration, bool requiredNotify, bool requiredSaveNow)
    {
        long startValue = GetOrCreateResource(resourceType);

        if (startValue == targetValue)
        {
            SetOrUpdateResource(resourceType, targetValue, requiredNotify, requiredSaveNow);
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += PRTime.Instance.GameDeltaTime;

            // Интерполяция идёт в double, а результат остаётся long: Mathf.Lerp работает
            // через float и теряет точность уже с 16.7 млн (2^24), а RoundToInt переполняется
            // выше 2.1 млрд - для игровых счётчиков это достижимые значения.
            var progress = Mathf.Clamp01(elapsedTime / duration);
            long newValue = InterpolateValue(startValue, targetValue, progress);

            SetOrUpdateResource(resourceType, newValue, requiredNotify, false);

            yield return null;
        }
        SetOrUpdateResource(resourceType, targetValue, requiredNotify, requiredSaveNow);
    }

    /// <summary>
    /// Промежуточное значение между start и target для доли progress.
    /// Считается в double и округляется к ближайшему long, поэтому большие
    /// значения ресурсов не теряют точность и не переполняются.
    /// </summary>
    private static long InterpolateValue(long start, long target, float progress)
    {
        var value = start + (target - start) * (double)progress;

        if (value >= long.MaxValue)
            return long.MaxValue;

        if (value <= long.MinValue)
            return long.MinValue;

        return (long)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
    }

    private static ProjectData GetProjectData()
    {
        return GameManager.Instance.GetProjectData();
    }

    private bool TryGetResourceName(Enumeration resourceType, out string resourceName)
    {
        resourceName = resourceType?.ToString();

        if (!string.IsNullOrEmpty(resourceName))
            return true;

        PRLog.WriteWarning(this, "Cannot use a resource with a null or empty type.");
        return false;
    }

    private bool TryGetResourceType(
        ResourceItemDefinition resource,
        out Enumeration resourceType)
    {
        if (resource != null && resource.TryGetResourceType(out resourceType))
            return true;

        resourceType = null;
        PRLog.WriteWarning(
            this,
            "Cannot use ResourceItemDefinition without a configured CurrencyType.");
        return false;
    }
}
