/// <summary>
/// Флаги проекта: значения по имени, которые можно поменять без пересборки игры.
/// </summary>
/// <remarks>
/// На площадке флаги приходят с её сервера (у Яндекса — «флаги» в консоли разработчика),
/// без площадки — из настроек проекта (<see cref="RemoteFlagsSettings"/>). Не путать с
/// <c>FlagsSystem</c>: там решения «можно/нельзя» от компонентов, здесь — настройки
/// «имя → значение».
/// <para>
/// Реализация отвечает только строкой; числа и логические значения разбирают расширения
/// <see cref="RemoteFlagsExtensions"/>.
/// </para>
/// </remarks>
public interface IRemoteFlags
{
    /// <summary>
    /// Значение флага строкой.
    /// </summary>
    /// <returns><c>false</c>, если флага нет.</returns>
    bool TryGetString(string name, out string value);
}
