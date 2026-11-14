#if !PRSDK_DISABLE_VK
/// <summary>
/// Сохранение игры в VK Storage: весь <see cref="PRSaveData"/> одной строкой.
/// </summary>
/// <remarks>
/// Сохранение приходит вместе с запуском площадки (<see cref="VKInitResult.save"/>), поэтому чтение синхронное.
/// Запись отдаётся мосту (<see cref="VKBridge.StorageSave"/>), а он пишет её сам: сжатой, частями в одну из двух ячеек
/// по очереди и не чаще раза в 30 секунд — у VK Storage значение до 4096 символов и 1000 записей в час на игрока.
/// На скрытой странице несохранённое пишется сразу. Частые сохранения игры поэтому склеиваются: в облако уходит последнее.
/// <para>
/// Площадка не поднялась или сохранение не прочиталось при запуске (сеть, повреждение обеих ячеек) — облако в этой
/// сессии не пишется: пустой прогресс затёр бы настоящий. Мост в этом случае не пишет сам, а хранилище выключает
/// запись у себя (<see cref="GameDataStorageBase.IsWriteBlocked"/>), и игра о такой «записи» не узнаёт.
/// </para>
/// <para>
/// Стратегия сохранения из настроек здесь не важна: данные всегда идут строкой, как при <see cref="SaveStrategy.Serialize"/>.
/// </para>
/// </remarks>
public class VKGameDataStorager : GameDataStorageBase
{
    /// <inheritdoc />
    protected override string ReadRaw()
    {
        if (!VKBridge.IsReady)
        {
            PRLog.WriteWarning(this, "VK Bridge не запущен: сохранение не прочитано, игра начнётся с нуля.");
            MarkUnavailable();
            return string.Empty;
        }

        if (VKBridge.Launch is { saveLoadFailed: true })
            MarkUnavailable();

        return VKBridge.Launch?.save;
    }

    /// <inheritdoc />
    protected override bool WriteRaw(string raw)
    {
        if (!VKBridge.IsReady)
        {
            PRLog.WriteWarning(this, "VK Bridge не запущен: сохранение не отправлено.");
            return false;
        }

        VKBridge.StorageSave(raw);
        return true;
    }
}
#endif
