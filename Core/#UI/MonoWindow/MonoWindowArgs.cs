using System;

public abstract class MonoWindowArgs
{
    public int Executer { get; set; }

    public abstract object GetRawData(); // Базовый метод возвращает данные как object

    public T GetData<T>()
    {
        if (GetRawData() is T data)
            return data;

        throw new InvalidCastException($"Невозможно привести {GetRawData()?.GetType()} к {typeof(T)}");
    }
}

public class MonoWindowArgs<T> : MonoWindowArgs
{
    public T Data { get; }

    public MonoWindowArgs(T data)
    {
        Data = data;
    }

    public override object GetRawData() => Data;
}

public class MonoWindowsArgsEmpty : MonoWindowArgs
{
    public override object GetRawData() => "empty";
}
