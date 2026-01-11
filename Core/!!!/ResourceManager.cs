using System.Collections;
using UnityEngine;

public class ResourceManager
{
    #region Поля и свойства

    /// <summary>
    /// Игровой менеджер.
    /// </summary>
    private GameManager gameManager;

    #endregion

    public void NotifyOfChange(ResourceType[] resources)
    {
        foreach (var resourceType in resources)
        {
            var resourceName = resourceType.ToString();
            var resourceValue = GetOrCreateResource(resourceType);

            //TODO:EventBus.RaiseEvent<IGlobalEvent>(invoke => invoke.GlobalChange(resourceName, resourceValue.ToString(), resourceValue.GetType()));
        }
    }

    /// <summary>
    /// Получить значение ресурса.
    /// </summary>
    /// <param name="resourceType">Тип ресурса.</param>
    /// <returns>Значение ресурса.</returns>
    public int GetOrCreateResource(ResourceType resourceType)
    {
        var resourceName = resourceType.ToString();
        //if (gameManager.GetProjectData().InventoryData.Resources.TryGetValue(resourceName, out var value))
        //    return value;

        //gameManager.GetProjectData().InventoryData.Resources[resourceName] = 0;
        return 0;
    }

    /// <summary>
    /// Установить значение ресурсу.
    /// </summary>
    /// <param name="resourceType">Тип ресурса.</param>
    /// <param name="value">Значение.</param>
    /// <param name="requiredNotify">Признак того, что требуется оповестить об изменение ресурса.</param>
    /// <param name="requiredSaveNow">Признак того, что требуется сохранить данные после изменения ресурса.</param>
    public void SetOrUpdateResource(ResourceType resourceType, int value, bool requiredNotify = false, bool requiredSaveNow = false)
    {
        var resourceName = resourceType.ToString();
        //gameManager.GetProjectData().InventoryData.Resources[resourceName] = value;

        //if (requiredNotify)
            //TODO:EventBus.RaiseEvent<IGlobalEvent>(invoke => invoke.GlobalChange(resourceName, value.ToString(), value.GetType()));

        if (requiredSaveNow)
            gameManager.SaveProjectData();
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="resourceType"></param>
    /// <param name="addValue"></param>
    /// <param name="requiredNotify"></param>
    /// <param name="requiredSaveNow"></param>
    public void AddResourceValue(ResourceType resourceType, int addValue, bool requiredNotify = false, bool requiredSaveNow = false)
    {
        int startValue = GetOrCreateResource(resourceType);
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
    public void UpdateResourceValueSmooth(ResourceType resourceType, int targetValue, float duration, bool requiredNotify = false, bool requiredSaveNow = false)
    {
        gameManager.StartCoroutine(SmoothUpdateCoroutine(resourceType, targetValue, duration, requiredNotify, requiredSaveNow));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="resourceType"></param>
    /// <param name="addValue"></param>
    /// <param name="duration"></param>
    /// <param name="requiredNotify"></param>
    /// <param name="requiredSaveNow"></param>
    public void AddResourceValueSmooth(ResourceType resourceType, int addValue, float duration, bool requiredNotify = false, bool requiredSaveNow = false)
    {
        int startValue = GetOrCreateResource(resourceType);
        var targetValue = startValue + addValue;
        gameManager.StartCoroutine(SmoothUpdateCoroutine(resourceType, targetValue, duration, requiredNotify, requiredSaveNow));
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
    private IEnumerator SmoothUpdateCoroutine(ResourceType resourceType, int targetValue, float duration, bool requiredNotify, bool requiredSaveNow)
    {
        var resourceName = resourceType.ToString();

        int startValue = GetOrCreateResource(resourceType);

        if (startValue == targetValue)
        {
            SetOrUpdateResource(resourceType, targetValue, requiredNotify, requiredSaveNow);
            yield break;
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            int newValue = Mathf.RoundToInt(Mathf.Lerp(startValue, targetValue, elapsedTime / duration));

            SetOrUpdateResource(resourceType, newValue, requiredNotify, false);

            yield return null;
        }
        SetOrUpdateResource(resourceType, targetValue, requiredNotify, requiredSaveNow);
    }


    #region Конструкторы

    public ResourceManager(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    #endregion
}
