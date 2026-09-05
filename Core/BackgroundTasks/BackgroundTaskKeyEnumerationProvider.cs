/// <summary>
/// Ключи фоновых задач.
/// Проектные задачи добавляют свои ключи `partial`-частью рядом со своим модулем,
/// править этот файл для них не нужно.
/// </summary>
public partial class BackgroundTaskKeyEnumerations : EnumerationProviderBase
{
    /// <inheritdoc />
    public override Enumeration Default => FirstOption;

    public override bool IncludeInherited => true;
}
