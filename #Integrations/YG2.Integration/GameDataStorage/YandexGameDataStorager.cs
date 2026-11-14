#if !PRSDK_DISABLE_YG2
using System;
using YG;

/// <summary>
/// Сохранение игры в Яндекс Играх через плагин YG2.
/// </summary>
/// <remarks>
/// Плагин сам держит локальную копию и облако и выбирает из них более новую; сюда сохранение приходит уже
/// выбранным — в поле <c>YG2.saves</c>. Как оно там лежит, задаёт <see cref="SaveStrategy"/> настроек проекта:
/// строкой (<see cref="SaveStrategy.Serialize"/>) или объектом (<see cref="SaveStrategy.Class"/>).
/// </remarks>
public class YandexGameDataStorager : GameDataStorageBase
{
    private static SaveStrategy Strategy => PRUnitySDK.Settings.GameStorage.SaveStrategy;

    /// <inheritdoc />
    protected override string ReadRaw()
    {
        return YG2.saves?.RawData;
    }

    /// <inheritdoc />
    protected override bool WriteRaw(string raw)
    {
        YG2.saves.RawData = raw;

        return SendToPlatform();
    }

    /// <inheritdoc />
    protected override bool ReadSave(out PRSaveData data, out bool unreadable)
    {
        PRLog.WriteDebug(this, $"Try loading data use strategy {Strategy}");

        switch (Strategy)
        {
            case SaveStrategy.Serialize:
                return base.ReadSave(out data, out unreadable);

            case SaveStrategy.Class:
                // Объект разобрал сам плагин: нечитаемой строки здесь не бывает.
                unreadable = false;
                return SaveDataVersioning.TryRead(YG2.saves?.PRSaveData, out data);

            default:
                throw new NotImplementedException();
        }
    }

    /// <inheritdoc />
    protected override bool WriteSave(PRSaveData data)
    {
        PRLog.WriteDebug(this, $"Try save data use strategy {Strategy}");

        switch (Strategy)
        {
            case SaveStrategy.Serialize:
                return base.WriteSave(data);

            case SaveStrategy.Class:
                SaveDataVersioning.Stamp(data);
                YG2.saves.PRSaveData = (PRSaveData)data.Clone();
                return SendToPlatform();

            default:
                throw new NotImplementedException();
        }
    }

    /// <returns>
    /// <see langword="true"/> — сохранение передано плагину. Дошло ли оно до облака, плагин не сообщает: этот признак
    /// нельзя считать подтверждением сервера.
    /// </returns>
    private static bool SendToPlatform()
    {
        if (!YG2.isSDKEnabled)
            return false;

        YG2.SaveProgress();
        return true;
    }
}
#endif
