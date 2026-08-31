using System;

[AttributeUsage(AttributeTargets.Method)]
public class MethodHookAttribute : Attribute
{
    /// <summary>
    /// Приоритет.
    /// Чем значение ниже, тем выше приоритет.
    /// </summary>
    public int Order { get; }
    public string MethodHookStage { get; }

    public bool IsEnabled { get; } = true;

    public MethodHookAttribute(MethodHookStage hookStage, int order = 0, bool isEnabled = true) 
        : this(hookStage.ToString(), order, isEnabled) { }

    public MethodHookAttribute(string hookStage, int order = 0, bool isEnabled = true)
    {
        this.MethodHookStage = hookStage;
        this.Order = order;
        this.IsEnabled = isEnabled;
    }
}