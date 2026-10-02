using System;

/// <summary>
/// Ссылка на ключ ввода из общего списка <see cref="InputActionEnumerations"/>.
/// </summary>
/// <remarks>
/// Отдельный тип ради конструктора со значением: раскладку по умолчанию удобно описывать
/// кодом, а <see cref="EnumerationReference{T}.Set"/> проверяет значение по списку, то есть
/// перебирает типы — в инициализаторе поля ассета этого делать не стоит.
/// </remarks>
[Serializable]
public class InputKeyReference : EnumerationReference<InputActionEnumerations>
{
    public InputKeyReference()
    {
    }

    public InputKeyReference(Enumeration action)
    {
        value = action?.Value;
    }
}
