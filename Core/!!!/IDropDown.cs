/// <summary>
/// Интерфейс для работы с DropDown списками.
/// </summary>
public interface IDropDown 
{
    /// <summary>
    /// Массив строй для дальнейшней генерации в DropDown.
    /// </summary>
    /// <returns>Массив строк.</returns>
    string[] GetKeys();
}
