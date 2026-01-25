using System.Collections.Generic;

/// <summary>
/// Класс для передачи данных между сценами.
/// </summary>
public class SceneDataChanger : SingletonProviderBase<SceneDataChanger>
{
    public const string NEXT_SCENE_KEY = nameof(NEXT_SCENE_KEY);

    /// <summary>
    /// Передаваемые данные.
    /// </summary>
    private Dictionary<string, object> data = new Dictionary<string, object>();

    /// <summary>
    /// Установить данные.
    /// </summary>
    /// <typeparam name="T">Тип.</typeparam>
    /// <param name="key">Ключ.</param>
    /// <param name="value">Значение.</param>
    public void SetData<T>(string key, T value)
    {
        data[key] = value;
    }

    /// <summary>
    /// Попытаться получить данные нужного типа.
    /// </summary>
    /// <typeparam name="T">Тип.</typeparam>
    /// <param name="key">Ключ.</param>
    /// <param name="clearDataAfterGetting">Очищать данные после получения.</param>
    /// <param name="data">Данные.</param>
    /// <returns>True - удалось получить, False - нет.</returns>
    public bool TryGetData<T>(string key, bool clearDataAfterGetting, out T data)
    {
        data = default(T);
        if (this.data.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
            {
                if (clearDataAfterGetting)
                    this.data.Remove(key);

                data = typedValue;
                return true;
            }
            else
            {
                PRLog.WriteWarning(this, $"Ошибка приведения типа: ключ '{key}' содержит {value.GetType()}, а запрошен {typeof(T)}.");
            }
        }
        return false;
    }

    /// <summary>
    /// Есть ли данные по данному ключу.
    /// </summary>
    /// <param name="key">Ключ.</param>
    /// <returns>True - есть, False - нет.</returns>
    public bool HasData(string key) => data.ContainsKey(key);

    /// <summary>
    /// Очистка всех данных.
    /// </summary>
    public void Clear() => data.Clear();
}
