using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class InvokePartialAttribute : Attribute
{
    public int Order { get; }

    #region Конструкторы

    public InvokePartialAttribute(int order = 0)
    {
        Order = order;
    }

    #endregion
}
