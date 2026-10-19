/// <summary>
/// Приоритеты интеграций нативных платформ: Android и PC.
/// </summary>
/// <remarks>
/// Меньше приоритета веб-площадок (5): переопределение с меньшим числом выигрывает. Нативные интеграции собираются
/// только под свою платформу (<c>UNITY_ANDROID</c>, <c>UNITY_STANDALONE</c>), поэтому в WebGL-сборку не попадают,
/// а в сборке под Android или PC перекрывают веб-площадку, даже если её забыли выключить.
/// <para>
/// Константы в ядре, а не в модулях: модулей рекламы под Android два, и выключенный не должен уносить общую константу.
/// </para>
/// </remarks>
public partial class PrioritySDK
{
    /// <summary>
    /// Реклама на Android: Yandex Mobile Ads или AdMob, включается одна.
    /// </summary>
    public const int OVERRIDE_PROPERTY_ANDROID_ADS_PRIORITY = 4;

    /// <summary>
    /// Магазин приложений на Android: покупки RuStore.
    /// </summary>
    public const int OVERRIDE_PROPERTY_ANDROID_STORE_PRIORITY = 4;

    /// <summary>
    /// Steam на PC.
    /// </summary>
    public const int OVERRIDE_PROPERTY_STEAM_PRIORITY = 4;
}
