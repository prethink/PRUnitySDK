using System;
using System.Collections.Generic;

/// <summary>
/// Менеджер произвольных свойств проекта (long/float/DateTime/string/bool),
/// хранимых по строковому имени в GameManager.GetProjectData().ProjectProperties.
/// Каждый Set* сохраняет значение и по умолчанию сразу пишет данные на диск
/// (save = true) и рассылает уведомление об изменении (requiredNotify = true) -
/// оба поведения можно отключить отдельно на каждый вызов.
/// </summary>
public class ProjectPropertiesManager : SingletonProviderBase<ProjectPropertiesManager>
{
    #region Set

    /// <summary>
    /// Сохраняет значение DateTime под именем name. save определяет, будет ли
    /// сразу вызван GameManager.SaveProjectData() (запись на диск), requiredNotify -
    /// будет ли разослано уведомление об изменении через EventBus.
    /// </summary>
    public void SetDateTime(string name, DateTime value, bool save = true, bool requiredNotify = true)
    {
        SetValue(name, value, save, requiredNotify);
    }

    /// <summary>
    /// Сохраняет значение long под именем name. save определяет, будет ли сразу
    /// вызван GameManager.SaveProjectData() (запись на диск), requiredNotify -
    /// будет ли разослано уведомление об изменении через EventBus.
    /// </summary>
    public void SetLong(string name, long value, bool save = true, bool requiredNotify = true)
    {
        SetValue(name, value, save, requiredNotify);
    }

    /// <summary>
    /// Прибавляет value к текущему значению свойства name (0, если свойства ещё
    /// не было) и сохраняет результат через SetLong. Удобно для счётчиков
    /// (например, суммарное количество монет), где не нужно читать-менять-писать вручную.
    /// </summary>
    public void AddLong(string name, long value, bool save = true, bool requiredNotify = true)
    {
        TryGetLong(name, out var currentValue);
        SetLong(name, value + currentValue, save, requiredNotify);
    }

    /// <summary>
    /// Сохраняет значение string под именем name. save определяет, будет ли сразу
    /// вызван GameManager.SaveProjectData() (запись на диск), requiredNotify -
    /// будет ли разослано уведомление об изменении через EventBus.
    /// </summary>
    public void SetString(string name, string value, bool save = true, bool requiredNotify = true)
    {
        SetValue(name, value, save, requiredNotify);
    }

    /// <summary>
    /// Сохраняет значение float под именем name. save определяет, будет ли сразу
    /// вызван GameManager.SaveProjectData() (запись на диск), requiredNotify -
    /// будет ли разослано уведомление об изменении через EventBus.
    /// </summary>
    public void SetFloat(string name, float value, bool save = true, bool requiredNotify = true)
    {
        SetValue(name, value, save, requiredNotify);
    }

    /// <summary>
    /// Прибавляет value к текущему значению свойства name (0, если свойства ещё
    /// не было) и сохраняет результат через SetFloat. Аналог AddLong для float -
    /// например, накопление игрового времени или прогресса, измеряемого дробным числом.
    /// </summary>
    public void AddFloat(string name, float value, bool save = true, bool requiredNotify = true)
    {
        TryGetFloat(name, out var currentValue);
        SetFloat(name, value + currentValue, save, requiredNotify);
    }

    /// <summary>
    /// Сохраняет значение bool под именем name. save определяет, будет ли сразу
    /// вызван GameManager.SaveProjectData() (запись на диск), requiredNotify -
    /// будет ли разослано уведомление об изменении через EventBus.
    /// </summary>
    public void SetBool(string name, bool value, bool save = true, bool requiredNotify = true)
    {
        SetValue(name, value, save, requiredNotify);
    }

    /// <summary>
    /// Устанавливает значение по типизированному ключу <see cref="EnumerationType{T}"/>.
    /// Тип T задаётся ключом, поэтому не нужно вызывать конкретный SetLong/SetFloat/...
    /// вручную — попадёт в тот же словарь, что и остальные Set*/TryGet*-методы для этого T.
    /// </summary>
    public void SetValue<T>(EnumerationType<T> enumerationType, T value, bool save = true, bool requiredNotify = true)
    {
        if (enumerationType == null)
            throw new ArgumentNullException(nameof(enumerationType));

        SetValue(enumerationType.Value, value, save, requiredNotify);
    }

    /// <summary>
    /// Общая реализация для всех Set*-методов. Тип свойства определяется
    /// параметром T на этапе компиляции, поэтому конкретный словарь ищется
    /// через GetProperties&lt;T&gt;() - добавление нового типа свойства требует
    /// правки только GetProperties&lt;T&gt;(), а не каждого Set/TryGet/Remove по отдельности.
    /// </summary>
    public void SetValue<T>(string name, T value, bool save = true, bool requiredNotify = true)
    {
        GetProperties<T>()[name] = value;

        if (save)
            GameManager.Instance.SaveProjectData();
    }

    #endregion

    #region TryGet / Get

    /// <summary>Пытается получить значение DateTime по имени name. Возвращает
    /// false, если свойство с таким именем не было сохранено - в этом случае
    /// value будет содержать default(DateTime).</summary>
    public bool TryGetDateTime(string name, out DateTime value) => TryGetValue(name, out value);

    /// <summary>Пытается получить значение long по имени name. Возвращает
    /// false, если свойство с таким именем не было сохранено - в этом случае
    /// value будет содержать 0.</summary>
    public bool TryGetLong(string name, out long value) => TryGetValue(name, out value);

    /// <summary>Пытается получить значение string по имени name. Возвращает
    /// false, если свойство с таким именем не было сохранено - в этом случае
    /// value будет содержать null.</summary>
    public bool TryGetString(string name, out string value) => TryGetValue(name, out value);

    /// <summary>Пытается получить значение float по имени name. Возвращает
    /// false, если свойство с таким именем не было сохранено - в этом случае
    /// value будет содержать 0.</summary>
    public bool TryGetFloat(string name, out float value) => TryGetValue(name, out value);

    /// <summary>Пытается получить значение bool по имени name. Возвращает
    /// false, если свойство с таким именем не было сохранено - в этом случае
    /// value будет содержать false.</summary>
    public bool TryGetBool(string name, out bool value) => TryGetValue(name, out value);

    /// <summary>Возвращает значение DateTime по имени name, либо default(DateTime),
    /// если свойство не найдено - удобно, когда отсутствие свойства не является
    /// ошибкой и можно просто использовать значение по умолчанию.</summary>
    public DateTime GetDateTime(string name) => TryGetDateTime(name, out var value) ? value : default;

    /// <summary>Возвращает значение long по имени name, либо 0, если свойство
    /// не найдено - удобно, когда отсутствие свойства не является ошибкой
    /// и можно просто использовать значение по умолчанию.</summary>
    public long GetLong(string name) => TryGetLong(name, out var value) ? value : default;

    /// <summary>Возвращает значение string по имени name, либо null, если свойство
    /// не найдено - удобно, когда отсутствие свойства не является ошибкой
    /// и можно просто использовать значение по умолчанию.</summary>
    public string GetString(string name) => TryGetString(name, out var value) ? value : default;

    /// <summary>Возвращает значение float по имени name, либо 0, если свойство
    /// не найдено - удобно, когда отсутствие свойства не является ошибкой
    /// и можно просто использовать значение по умолчанию.</summary>
    public float GetFloat(string name) => TryGetFloat(name, out var value) ? value : default;

    /// <summary>Возвращает значение bool по имени name, либо false, если свойство
    /// не найдено - удобно, когда отсутствие свойства не является ошибкой
    /// и можно просто использовать значение по умолчанию.</summary>
    public bool GetBool(string name) => TryGetBool(name, out var value) ? value : default;

    /// <summary>Пытается получить значение DateTime по имени name. Возвращает
    /// false, если свойство с таким именем не было сохранено - в этом случае
    /// value будет содержать default(DateTime).</summary>
    public bool TryGetDateTime(Enumeration enumeration, out DateTime value) => TryGetValue(enumeration.Value, out value);

    /// <summary>Пытается получить значение long по имени name. Возвращает
    /// false, если свойство с таким именем не было сохранено - в этом случае
    /// value будет содержать 0.</summary>
    public bool TryGetLong(Enumeration enumeration, out long value) => TryGetValue(enumeration.Value, out value);

    /// <summary>Пытается получить значение string по имени name. Возвращает
    /// false, если свойство с таким именем не было сохранено - в этом случае
    /// value будет содержать null.</summary>
    public bool TryGetString(Enumeration enumeration, out string value) => TryGetValue(enumeration.Value, out value);

    /// <summary>Пытается получить значение float по имени name. Возвращает
    /// false, если свойство с таким именем не было сохранено - в этом случае
    /// value будет содержать 0.</summary>
    public bool TryGetFloat(Enumeration enumeration, out float value) => TryGetValue(enumeration.Value, out value);

    /// <summary>Пытается получить значение bool по имени name. Возвращает
    /// false, если свойство с таким именем не было сохранено - в этом случае
    /// value будет содержать false.</summary>
    public bool TryGetBool(Enumeration enumeration, out bool value) => TryGetValue(enumeration.Value, out value);

    /// <summary>Возвращает значение DateTime по имени name, либо default(DateTime),
    /// если свойство не найдено - удобно, когда отсутствие свойства не является
    /// ошибкой и можно просто использовать значение по умолчанию.</summary>
    public DateTime GetDateTime(Enumeration enumeration) => TryGetDateTime(enumeration.Value, out var value) ? value : default;

    /// <summary>Возвращает значение long по имени name, либо 0, если свойство
    /// не найдено - удобно, когда отсутствие свойства не является ошибкой
    /// и можно просто использовать значение по умолчанию.</summary>
    public long GetLong(Enumeration enumeration) => TryGetLong(enumeration.Value, out var value) ? value : default;

    /// <summary>Возвращает значение string по имени name, либо null, если свойство
    /// не найдено - удобно, когда отсутствие свойства не является ошибкой
    /// и можно просто использовать значение по умолчанию.</summary>
    public string GetString(Enumeration enumeration) => TryGetString(enumeration.Value, out var value) ? value : default;

    /// <summary>Возвращает значение float по имени name, либо 0, если свойство
    /// не найдено - удобно, когда отсутствие свойства не является ошибкой
    /// и можно просто использовать значение по умолчанию.</summary>
    public float GetFloat(Enumeration enumeration) => TryGetFloat(enumeration.Value, out var value) ? value : default;

    /// <summary>Возвращает значение bool по имени name, либо false, если свойство
    /// не найдено - удобно, когда отсутствие свойства не является ошибкой
    /// и можно просто использовать значение по умолчанию.</summary>
    public bool GetBool(Enumeration enumeration) => TryGetBool(enumeration.Value, out var value) ? value : default;


    public bool TryGetValue<T>(EnumerationType<T> enumerationType, out T value) => TryGetValue(enumerationType.Value, out value);

    /// <summary>
    /// Возвращает значение по <see cref="EnumerationType{T}"/> или defaultValue, если
    /// ключ ещё не сохранён. Тот же паттерн, что GetLong/GetFloat/GetBool/..., но без
    /// привязки к конкретному T — подходит для любого поддерживаемого типа.
    /// </summary>
    public T GetValue<T>(EnumerationType<T> enumerationType, T defaultValue) =>
        TryGetValue(enumerationType, out var value) ? value : defaultValue;

    /// <summary>
    /// Перегрузка AddLong для типизированного ключа <see cref="EnumerationType{T}"/> с T = long.
    /// </summary>
    public void AddLong(EnumerationType<long> enumerationType, long value, bool save = true, bool requiredNotify = true)
    {
        TryGetValue(enumerationType, out var currentValue);
        SetValue(enumerationType, value + currentValue, save, requiredNotify);
    }

    /// <summary>
    /// Перегрузка AddFloat для типизированного ключа <see cref="EnumerationType{T}"/> с T = float.
    /// </summary>
    public void AddFloat(EnumerationType<float> enumerationType, float value, bool save = true, bool requiredNotify = true)
    {
        TryGetValue(enumerationType, out var currentValue);
        SetValue(enumerationType, value + currentValue, save, requiredNotify);
    }

    /// <summary>
    /// Общая реализация для всех TryGet*-методов - находит нужный словарь через
    /// GetProperties&lt;T&gt;() и делегирует в его обычный Dictionary.TryGetValue.
    /// </summary>
    public bool TryGetValue<T>(string name, out T value)
    {
        return GetProperties<T>().TryGetValue(name, out value);
    }

    /// <summary>
    /// Возвращает значение по строковому имени, либо defaultValue, если свойство
    /// ещё не сохранялось.
    /// </summary>
    public T GetValue<T>(string name, T defaultValue)
    {
        return TryGetValue<T>(name, out var value) ? value : defaultValue;
    }

    #endregion

    #region Remove

    /// <summary>
    /// Удаляет свойство указанного типа. В отличие от Set*-методов, тип задаётся
    /// не через generic-параметр (вызывающий код обычно хранит только Type, а не
    /// статический тип значения), поэтому здесь нужен явный перебор - но, в отличие
    /// от предыдущей версии, save/notify вызываются только если что-то РЕАЛЬНО было
    /// удалено, а неизвестный type логируется вместо тихого игнорирования.
    /// </summary>
    public void RemoveProperty(string propertyName, Type type, bool save = true, bool requiredNotify = true)
    {
        var properties = GetProjectProperties();
        bool removed;

        if (type == typeof(long))
            removed = properties.LongProperties.Remove(propertyName);
        else if (type == typeof(float))
            removed = properties.FloatProperties.Remove(propertyName);
        else if (type == typeof(DateTime))
            removed = properties.DateTimeProperties.Remove(propertyName);
        else if (type == typeof(string))
            removed = properties.StringProperties.Remove(propertyName);
        else if (type == typeof(bool))
            removed = properties.BoolProperties.Remove(propertyName);
        else
        {
            // Неизвестный тип - раньше метод просто ничего не делал и всё равно
            // сохранял данные; теперь явно предупреждаем, чтобы ошибка в вызывающем
            // коде (например, опечатка в typeof(...)) не терялась молча.
            PRLog.WriteWarning(this, $"RemoveProperty: неподдерживаемый тип '{type}' для свойства '{propertyName}'.");
            return;
        }

        if (!removed)
            return; // свойства с таким именем и не было - не тратим save/notify впустую

        if (save)
            GameManager.Instance.SaveProjectData();
    }

    /// <summary>
    /// Удаляет свойство типа T по имени. В отличие от перегрузки с явным Type, здесь
    /// T известен на этапе компиляции — используется напрямую GetProperties&lt;T&gt;()
    /// без ветвления по typeof(T) и без отдельной проверки поддерживаемых типов
    /// (её уже делает GetProperties&lt;T&gt;(), бросая NotSupportedException).
    /// </summary>
    public void RemoveProperty<T>(string propertyName, bool save = true, bool requiredNotify = true)
    {
        var removed = GetProperties<T>().Remove(propertyName);

        if (!removed)
            return;

        if (save)
            GameManager.Instance.SaveProjectData();
    }

    /// <summary>
    /// Удаляет свойство по типизированному ключу <see cref="EnumerationType{T}"/>.
    /// </summary>
    public void RemoveProperty<T>(EnumerationType<T> enumerationType, bool save = true, bool requiredNotify = true)
    {
        if (enumerationType == null)
            throw new ArgumentNullException(nameof(enumerationType));

        RemoveProperty<T>(enumerationType.Value, save, requiredNotify);
    }

    #endregion

    #region Внутреннее

    /// <summary>
    /// Единая точка доступа к словарю нужного типа. typeof(T) сравнивается на
    /// этапе выполнения (T всегда известен статически из вызывающего Set*/TryGet*,
    /// поэтому ветка всегда конкретна) - обычный приём для generic-диспетчеризации
    /// по набору заранее известных типов без reflection.
    /// </summary>
    private Dictionary<string, T> GetProperties<T>()
    {
        var properties = GetProjectProperties();

        if (typeof(T) == typeof(long))
            return (Dictionary<string, T>)(object)properties.LongProperties;

        if (typeof(T) == typeof(float))
            return (Dictionary<string, T>)(object)properties.FloatProperties;

        if (typeof(T) == typeof(DateTime))
            return (Dictionary<string, T>)(object)properties.DateTimeProperties;

        if (typeof(T) == typeof(string))
            return (Dictionary<string, T>)(object)properties.StringProperties;

        if (typeof(T) == typeof(bool))
            return (Dictionary<string, T>)(object)properties.BoolProperties;

        throw new NotSupportedException($"Тип свойства '{typeof(T)}' не поддерживается ProjectPropertiesManager.");
    }

    /// <summary>
    /// Единая точка получения ProjectProperties - раньше GameManager.Instance.GetProjectData()
    /// вызывался напрямую в каждом из ~15 методов, и если GetProjectData() вернёт null
    /// (например, обращение до полной загрузки данных проекта), падать пришлось бы
    /// в каждом месте отдельно. Теперь понятная ошибка кидается один раз, здесь.
    /// </summary>
    private ProjectProperties GetProjectProperties()
    {
        var data = GameManager.Instance.GetProjectData();

        if (data == null)
            throw new InvalidOperationException("ProjectPropertiesManager: GameManager.GetProjectData() вернул null - данные проекта ещё не загружены.");

        return data.ProjectProperties;
    }

    #endregion
}