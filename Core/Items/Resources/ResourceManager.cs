using System.Collections;
using UnityEngine;

public class ResourceManager : SingletonProviderBase<ResourceManager>
{
    /// <summary>
    /// Получить значение ресурса.
    /// </summary>
    /// <param name="resourceType">Тип ресурса.</param>
    /// <returns>Значение ресурса.</returns>
    public long GetOrCreateResource(Enumeration resourceType)
    {
        var resourceName = resourceType.ToString();
        if (GameManager.Instance.GetProjectData().Resources.TryGetValue(resourceName, out var value))
            return value;

        GameManager.Instance.GetProjectData().Resources[resourceName] = 0;
        return 0;
    }

    /// <summary>
    /// Установить значение ресурсу.
    /// </summary>
    /// <param name="resourceType">Тип ресурса.</param>
    /// <param name="value">Значение.</param>
    /// <param name="requiredNotify">Признак того, что требуется оповестить об изменение ресурса.</param>
    /// <param name="requiredSaveNow">Признак того, что требуется сохранить данные после изменения ресурса.</param>
    public void SetOrUpdateResource(Enumeration resourceType, long value, bool requiredNotify = false, bool requiredSaveNow = false)
    {
        var resourceName = resourceType.ToString();
        GameManager.Instance.GetProjectData().Resources[resourceName] = value;

        if (requiredNotify)
            ResourceEvents.RaiseResourceValueChange(new ResourceValueChangeEventArgs(resourceType, value));

        if (requiredSaveNow)
            GameManager.Instance.SaveProjectData();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="resourceType"></param>
    /// <param name="addValue"></param>
    /// <param name="requiredNotify"></param>
    /// <param name="requiredSaveNow"></param>
    public void AddResourceValue(Enumeration resourceType, long addValue, bool requiredNotify = false, bool requiredSaveNow = false)
    {
        long startValue = GetOrCreateResource(resourceType);
        var targetValue = startValue + addValue;
        SetOrUpdateResource(resourceType, targetValue, requiredNotify, requiredSaveNow);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="resourceType"></param>
    /// <param name="targetValue"></param>
    /// <param name="duration"></param>
    /// <param name="requiredNotify"></param>
    /// <param name="requiredSaveNow"></param>
    public void UpdateResourceValueSmooth(Enumeration resourceType, long targetValue, float duration, bool requiredNotify = false, bool requiredSaveNow = false)
    {
        GameManager.Instance.StartCoroutine(SmoothUpdateCoroutine(resourceType, targetValue, duration, requiredNotify, requiredSaveNow));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="resourceType"></param>
    /// <param name="addValue"></param>
    /// <param name="duration"></param>
    /// <param name="requiredNotify"></param>
    /// <param name="requiredSaveNow"></param>
    public void AddResourceValueSmooth(Enumeration resourceType, long addValue, float duration, bool requiredNotify = false, bool requiredSaveNow = false)
    {
        long startValue = GetOrCreateResource(resourceType);
        var targetValue = startValue + addValue;
        GameManager.Instance.StartCoroutine(SmoothUpdateCoroutine(resourceType, targetValue, duration, requiredNotify, requiredSaveNow));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="resourceType"></param>
    /// <param name="targetValue"></param>
    /// <param name="duration"></param>
    /// <param name="requiredNotify"></param>
    /// <param name="requiredSaveNow"></param>
    /// <returns></returns>
    private IEnumerator SmoothUpdateCoroutine(Enumeration resourceType, long targetValue, float duration, bool requiredNotify, bool requiredSaveNow)
    {
        var resourceName = resourceType.ToString();

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
            int newValue = Mathf.RoundToInt(Mathf.Lerp(startValue, targetValue, elapsedTime / duration));

            SetOrUpdateResource(resourceType, newValue, requiredNotify, false);

            yield return null;
        }
        SetOrUpdateResource(resourceType, targetValue, requiredNotify, requiredSaveNow);
    }
}
