using System;

/// <summary>
/// Базовые параметры, передаваемые при открытии MonoWindow.
/// </summary>
public abstract class MonoWindowArgs
{
    /// <summary>
    /// Идентификатор игрока или другого исполнителя, открывшего окно.
    /// </summary>
    public long Executor { get; set; }

    /// <summary>
    /// Устаревшее имя свойства <see cref="Executor"/>.
    /// </summary>
    [Obsolete("Use Executor instead.")]
    public long Executer
    {
        get => Executor;
        set => Executor = value;
    }

    /// <summary>
    /// Возвращает данные без приведения к конкретному типу.
    /// </summary>
    public abstract object GetRawData();

    /// <summary>
    /// Возвращает данные окна, приведённые к указанному типу.
    /// </summary>
    /// <typeparam name="T">Ожидаемый тип данных.</typeparam>
    /// <exception cref="InvalidCastException">
    /// Фактический тип данных не совместим с <typeparamref name="T"/>.
    /// </exception>
    public T GetData<T>()
    {
        object rawData = GetRawData();
        if (rawData is T data)
            return data;

        throw new InvalidCastException(
            $"Невозможно привести {rawData?.GetType().ToString() ?? "null"} к {typeof(T)}.");
    }

    /// <summary>
    /// Пытается получить данные окна указанного типа без исключения.
    /// </summary>
    public bool TryGetData<T>(out T data)
    {
        if (GetRawData() is T typedData)
        {
            data = typedData;
            return true;
        }

        data = default;
        return false;
    }
}

/// <summary>
/// Типизированные параметры MonoWindow.
/// </summary>
/// <typeparam name="T">Тип передаваемых данных.</typeparam>
public class MonoWindowArgs<T> : MonoWindowArgs
{
    /// <summary>
    /// Данные, переданные окну.
    /// </summary>
    public T Data { get; }

    /// <summary>
    /// Создаёт параметры с указанными данными.
    /// </summary>
    public MonoWindowArgs(T data)
    {
        Data = data;
    }

    public override object GetRawData() => Data;
}

/// <summary>
/// Пустые параметры для окна, которому не нужны дополнительные данные.
/// </summary>
public class MonoWindowArgsEmpty : MonoWindowArgs
{
    /// <inheritdoc />
    public override object GetRawData() => null;
}

/// <summary>
/// Устаревшее имя <see cref="MonoWindowArgsEmpty"/>.
/// </summary>
[Obsolete("Use MonoWindowArgsEmpty instead.")]
public class MonoWindowsArgsEmpty : MonoWindowArgsEmpty
{
}
