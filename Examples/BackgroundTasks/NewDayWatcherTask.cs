using System;

/// <summary>
/// Пример задачи-наблюдателя: следит за сменой суток по серверному времени.
/// </summary>
/// <remarks>
/// Классический случай, ради которого наблюдатель и нужен: календарный день никто
/// не «присылает» - о его смене можно узнать только периодической проверкой.
/// На этом обычно висят ежедневные награды, сброс лимитов и обновление магазина.
/// <para>
/// Обратите внимание на <see cref="RaiseOnFirstRead"/>: при первом чтении значение
/// появляется впервые, но день при этом не менялся. Без этого флага каждый запуск игры
/// выглядел бы как наступление нового дня и выдавал бы награду заново.
/// </para>
/// <para>
/// Задача выключена: у атрибута стоит <c>Enabled = false</c>. Чтобы попробовать -
/// уберите этот параметр либо зарегистрируйте задачу вручную и подпишитесь на событие:
/// <code>
/// var task = new NewDayWatcherTask();
/// task.Changed += day => PRLog.WriteDebug(this, $"Новый день: {day}");
/// PRUnitySDK.Trackers.BackgroundTasks.Register(task);
/// </code>
/// </para>
/// </remarks>
[AutoBackgroundTask(Enabled = false)]
public class NewDayWatcherTask : WatcherTask<int>
{
    /// <inheritdoc />
    public override Enumeration Key => BackgroundTaskKeyEnumerationProvider.NewDay;

    /// <inheritdoc />
    public override string Name => "Смена суток";

    /// <summary>
    /// Раз в минуту: сутки меняются редко, а точность до минуты для наград достаточна.
    /// Реальное время, а не игровое - день идёт и на паузе.
    /// </summary>
    public override float RepeatSeconds => 60f;

    /// <summary>
    /// Первое чтение сменой дня не считается - иначе награда выдавалась бы
    /// при каждом запуске игры.
    /// </summary>
    public override bool RaiseOnFirstRead => false;

    /// <summary>
    /// Пока серверное время не инициализировано, проверять нечего.
    /// </summary>
    public override bool CanExecute()
    {
        return PRUnitySDK.ServerTime != null;
    }

    /// <summary>
    /// Наблюдаемое значение - номер дня в году.
    /// </summary>
    /// <remarks>
    /// Номер дня, а не сама дата: сравнивать нужно именно календарный день, иначе
    /// событие поднималось бы на каждом опросе, ведь <see cref="DateTime"/> меняется всегда.
    /// </remarks>
    public override int Read()
    {
        return PRUnitySDK.ServerTime.GetNow().DayOfYear;
    }
}
