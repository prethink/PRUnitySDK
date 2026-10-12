using System;

/// <summary>
/// Группа раздела в окне настроек SDK, если раздел должен стоять не в группе своей папки.
/// </summary>
/// <remarks>
/// По умолчанию окно складывает разделы по папке, где объявлено поле: <c>PRUnitySDK</c>,
/// <c>PRUnitySDKPrivate</c>. Настройки конкретной игры лежат в коде SDK рядом с игрой, но
/// искать их удобнее отдельно от общих — для этого класс раздела помечают группой
/// <see cref="Games"/>, и каждая игра становится в ней своим разделом.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SettingsGroupAttribute : Attribute
{
    /// <summary>
    /// Группа настроек отдельных игр.
    /// </summary>
    public const string Games = "Games";

    /// <summary>
    /// Имя группы в окне настроек.
    /// </summary>
    public string Name { get; }

    /// <param name="name">Имя группы в окне настроек.</param>
    public SettingsGroupAttribute(string name)
    {
        Name = name;
    }
}
