using Newtonsoft.Json.Linq;

/// <summary>
/// Переводит сохранение с версии <see cref="FromVersion"/> на следующую.
/// </summary>
/// <remarks>
/// Реализации находятся рефлексией и попадают в <c>link.xml</c> сами. Нужен публичный
/// конструктор без параметров. Версию без шага сохранение проходит без изменений.
/// </remarks>
/// <example>
/// <code>
/// public class RenameCoinMigration : ISaveMigration
/// {
///     public int FromVersion => 1;
///
///     public void Migrate(JObject save)
///     {
///         var resources = (JObject)save["ProjectData"]?["Resources"];
///         if (resources?["Coin"] is JToken coins)
///         {
///             resources["Gold"] = coins;
///             resources.Remove("Coin");
///         }
///     }
/// }
/// </code>
/// </example>
public interface ISaveMigration
{
    /// <summary>
    /// Версия, из которой переводит шаг.
    /// </summary>
    int FromVersion { get; }

    /// <summary>
    /// Меняет JSON сохранения на месте. Поле <c>Version</c> трогать не нужно.
    /// </summary>
    /// <remarks>
    /// Исключение означает неудачу: сохранение отбрасывается и начинается новое.
    /// </remarks>
    /// <param name="save">Корень сохранения, объект <see cref="PRSaveData"/> в JSON.</param>
    void Migrate(JObject save);
}
