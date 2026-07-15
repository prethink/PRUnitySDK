/// <summary>
/// Предоставляет путь к ресурсу, используемый для загрузки или поиска объекта.
/// </summary>
public interface IResourcePathProvider
{
    /// <summary>
    /// Возвращает путь к ресурсу.
    /// </summary>
    string ResourcePath { get; }
}