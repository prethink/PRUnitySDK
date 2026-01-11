using System;
using System.Collections.Generic;

public class ProjectPropertiesManager : SingletonProviderBase<ProjectPropertiesManager>
{
    public enum TypeProperty
    {
        Long,
        Float,
        Datetime,
        String,
        Bool,
        Object
    }

    #region Методы

    public void SetDateTime(string name, DateTime value, bool save = true, bool requiredNotify = true)
    {
        SetValue<DateTime>(name, value, save, requiredNotify, GameManager.Instance.GetProjectData().ProjectProperties.DateTimeProperties);
    }

    public void SetLong(string name, long value, bool save = true, bool requiredNotify = true)
    {
        SetValue<long>(name, value, save, requiredNotify, GameManager.Instance.GetProjectData().ProjectProperties.LongProperties);
    }

    public void AddLong(string name, long value, bool save = true, bool requiredNotify = true)
    {
        long currentValue = 0;
        TryGetLong(name, out currentValue);
        value += currentValue;
        SetLong(name, value, save, requiredNotify);
    }

    public void SetString(string name, string value, bool save = true, bool requiredNotify = true)
    {
        SetValue<string>(name, value, save, requiredNotify, GameManager.Instance.GetProjectData().ProjectProperties.StringProperties);
    }

    public void SetFloat(string name, float value, bool save = true, bool requiredNotify = true)
    {
        SetValue<float>(name, value, save, requiredNotify, GameManager.Instance.GetProjectData().ProjectProperties.FloatProperties);
    }

    public void AddFloat(string name, float value, bool save = true, bool requiredNotify = true)
    {
        float currentValue = 0;
        TryGetFloat(name, out currentValue);
        value += currentValue;
        SetFloat(name, value, save, requiredNotify);
    }

    public void SetBool(string name, bool value, bool save = true, bool requiredNotify = true)
    {
        SetValue<bool>(name, value, save, requiredNotify, GameManager.Instance.GetProjectData().ProjectProperties.BoolProperties);
    }

    private void SetValue<T>(string name, T value, bool save, bool requiredNotify , Dictionary<string, T> propertyDictionary)
    {
        propertyDictionary[name] = value;

        if (save)
            GameManager.Instance.SaveProjectData();

        if (requiredNotify)
        {
            //TODO:EventBus.RaiseEvent<IGlobalEvent>(invoke => invoke.GlobalChange(name, value.ToString(), value.GetType()));
            //TODO:EventBus.RaiseEvent<INotifyEventUI>(ui => ui.ChangeStateUI(name, value.ToString()));
        }
    }

    /// <summary>
    /// Попытка получить значение типа DateTime.
    /// </summary>
    public bool TryGetDateTime(string name, out DateTime value)
    {
        return GameManager.Instance.GetProjectData().ProjectProperties.DateTimeProperties.TryGetValue(name, out value);
    }

    /// <summary>
    /// Попытка получить значение типа long.
    /// </summary>
    public bool TryGetLong(string name, out long value)
    {
        return GameManager.Instance.GetProjectData().ProjectProperties.LongProperties.TryGetValue(name, out value);
    }

    /// <summary>
    /// Попытка получить значение типа string.
    /// </summary>
    public bool TryGetString(string name, out string value)
    {
        return GameManager.Instance.GetProjectData().ProjectProperties.StringProperties.TryGetValue(name, out value);
    }

    /// <summary>
    /// Попытка получить значение типа float.
    /// </summary>
    public bool TryGetFloat(string name, out float value)
    {
        return GameManager.Instance.GetProjectData().ProjectProperties.FloatProperties.TryGetValue(name, out value);
    }

    /// <summary>
    /// Получить значение типа bool.
    /// </summary>
    public bool GetBool(string name)
    {
        return GameManager.Instance.GetProjectData().ProjectProperties.BoolProperties.TryGetValue(name, out bool value) ? value : false;
    }

    public void RemoveProperty(string propertyName, Type type)
    {
        if(type == typeof(long))
            GameManager.Instance.GetProjectData().ProjectProperties.LongProperties.Remove(propertyName);

        if (type == typeof(float))
            GameManager.Instance.GetProjectData().ProjectProperties.FloatProperties.Remove(propertyName);

        if (type == typeof(DateTime))
            GameManager.Instance.GetProjectData().ProjectProperties.DateTimeProperties.Remove(propertyName);

        if (type == typeof(string))
            GameManager.Instance.GetProjectData().ProjectProperties.StringProperties.Remove(propertyName);

        if (type == typeof(bool))
            GameManager.Instance.GetProjectData().ProjectProperties.BoolProperties.Remove(propertyName);

        GameManager.Instance.SaveProjectData();
    }

    #endregion
}
