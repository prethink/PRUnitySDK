using System;

[AttributeUsage(AttributeTargets.Field)]
public class EnumerationOptionsAttribute : Attribute
{
    /// <summary>
    /// Тип класса, содержащего статический метод для возврата коллекции вариантов.
    /// </summary>
    public Type OptionsType { get; }

    /// <summary>
    /// Имя статического метода, возвращающего IEnumerable<Enumeration>. По умолчанию "GetAllOptions".
    /// </summary>
    public string StaticMethodName { get; }

    public EnumerationOptionsAttribute(Type optionsType, string staticMethodName = "GetAllOptions")
    {
        OptionsType = optionsType;
        StaticMethodName = staticMethodName;
    }
}
