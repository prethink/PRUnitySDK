#if !PRSDK_DISABLE_PIKABU
using System;

/// <summary>
/// Сохранение игры в облачном хранилище Пикабу: весь <see cref="PRSaveData"/> одной строкой под ключом <see cref="SaveKey"/>.
/// </summary>
/// <remarks>
/// Хранилище приходит вместе с запуском SDK, чтение и запись синхронные. На сервер изменения SDK отправляет сам,
/// с небольшой задержкой; лимит площадки (100 запросов за 5 минут) считает только явные отправки, поэтому
/// обычное сохранение их не делает. Кому нужно подтверждение сервера — списанию покупок, — тот зовёт <see cref="Flush"/>.
/// <para>
/// Всё хранилище игрока — не больше 200 КБ. Сверх лимита данные в игре остаются, но на сервер уже не уходят.
/// </para>
/// <para>
/// Игра, открытая сразу на двух устройствах или вкладках, получает от площадки <c>REVISION_CONFLICT</c>, и до
/// перезагрузки страницы облако её сохранения не принимает. Игроку об этом говорит приватная часть интеграции.
/// </para>
/// <para>
/// Площадка не поднялась — игра начинается с нуля, а запись до конца запуска выключена
/// (<see cref="GameDataStorageBase.IsWriteBlocked"/>).
/// </para>
/// <para>
/// Стратегия сохранения из настроек здесь не важна: данные всегда идут строкой, как при <see cref="SaveStrategy.Serialize"/>.
/// </para>
/// </remarks>
public class PikabuGameDataStorager : GameDataStorageBase
{
    /// <summary>
    /// Ключ облачного хранилища, под которым лежит сохранение.
    /// </summary>
    public const string SaveKey = "prsave";

    /// <summary>
    /// Отправляет записанное на сервер и сообщает, принял ли он его.
    /// </summary>
    public static void Flush(Action<bool> done)
    {
        PikabuSDK.StorageFlush(result =>
        {
            if (!result.success)
                PRLog.WriteWarning(typeof(PikabuGameDataStorager), $"Облако Пикабу не приняло сохранение: {result.reason}.");
            done?.Invoke(result.success);
        });
    }

    /// <remarks>
    /// Загрузчик собирает SDK после запуска площадки, и хранилище к этому времени уже на месте.
    /// </remarks>
    protected override string ReadRaw()
    {
        if (!PikabuSDK.IsReady)
        {
            PRLog.WriteWarning(this, "SDK Пикабу не запущен: сохранение не прочитано, игра начнётся с нуля.");
            MarkUnavailable();
            return string.Empty;
        }

        return PikabuSDK.StorageGet(SaveKey);
    }

    /// <inheritdoc />
    protected override bool WriteRaw(string raw)
    {
        if (!PikabuSDK.IsReady)
        {
            PRLog.WriteWarning(this, "SDK Пикабу не запущен: сохранение не отправлено.");
            return false;
        }

        PikabuSDK.StorageSet(SaveKey, raw);
        return true;
    }
}
#endif
