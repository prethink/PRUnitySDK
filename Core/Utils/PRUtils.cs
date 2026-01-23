public partial class PRUtils : SingletonProviderBase<PRUtils>
{
    /// <summary>
    /// Сервис предоставляет набор готовых имён.
    /// Путь к файлу с именами: Assets/PRUnitySDK/Resources/Names.txt
    /// </summary>
    public NameService NameService => NameService.Instance;
}
