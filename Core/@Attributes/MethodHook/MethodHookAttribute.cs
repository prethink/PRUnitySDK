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

    public MethodHookAttribute(MethodHookStage hookStage, int order = 0) 
        : this(hookStage.ToString(), order) { }

    public MethodHookAttribute(string hookStage, int order = 0)
    {
        this.MethodHookStage = hookStage;
        this.Order = order;
    }
}