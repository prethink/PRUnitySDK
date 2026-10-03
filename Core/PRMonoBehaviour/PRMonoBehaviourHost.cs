using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Центральный хост, управляющий пользовательским циклом обновления.
/// </summary>
/// <remarks>
/// Единственный <see cref="PRMonoBehaviour"/>, у которого Unity вызывает методы кадра: через
/// них он крутит <see cref="PRUpdateRunner"/> и свои списки обычных классов.
/// <para>
/// На логической паузе не вызывается ничего, в том числе <see cref="Physics.Simulate(float)"/>:
/// физика и анимации стоят так же, как стояли, когда каждый объект проверял паузу сам.
/// </para>
/// </remarks>
public class PRMonoBehaviourHost : PRMonoBehaviourSingletonBase<PRMonoBehaviourHost>, ISingletonInitializer
{
    #region Поля и свойства

    /// <summary>
    /// Объекты, участвующие в кастомном FixedUpdate цикле.
    /// </summary>
    private List<IPRFixedUpdate> fixedUpdates = new();

    /// <summary>
    /// Объекты, участвующие в кастомном Update цикле.
    /// </summary>
    private List<IPRUpdate> updates = new();

    /// <summary>
    /// Объекты, участвующие в тиковом (интервальном) обновлении.
    /// </summary>
    private List<IPRTickable> tickables = new();

    /// <summary>
    /// Кулдаун, управляющий частотой вызова PRTick().
    /// </summary>
    private CooldownBase tickCooldown = new CooldownGameTime();

    /// <summary>
    /// Делегат тика. Создаётся один раз: лямбда прямо в <c>Update</c> была новым объектом на каждый кадр.
    /// </summary>
    private System.Action tickAction;

    /// <summary>
    /// Сколько <see cref="PRMonoBehaviour"/> получает PRUpdate. Для отладки.
    /// </summary>
    public int RunnerUpdateCount => PRUpdateRunner.UpdateCount;

    /// <summary>
    /// Сколько <see cref="PRMonoBehaviour"/> получает PRLateUpdate. Для отладки.
    /// </summary>
    public int RunnerLateUpdateCount => PRUpdateRunner.LateUpdateCount;

    /// <summary>
    /// Сколько <see cref="PRMonoBehaviour"/> получает PRFixedUpdate. Для отладки.
    /// </summary>
    public int RunnerFixedUpdateCount => PRUpdateRunner.FixedUpdateCount;

    #endregion

    /// <summary>
    /// Пытается получить уже существующий хост, не создавая новый GameObject.
    /// </summary>
    /// <remarks>
    /// Для кода, который может выполняться при закрытии сцены: <see cref="PRMonoBehaviourSingletonBase{T}.Instance"/>
    /// в этот момент создал бы хост заново.
    /// </remarks>
    public static bool TryGetExisting(out PRMonoBehaviourHost host)
    {
        host = instance;
        return host != null;
    }

    #region Registration

    /// <summary>
    /// Регистрирует объект в Update цикле.
    /// Возвращает false, если объект уже зарегистрирован.
    /// </summary>
    public bool Register(IPRUpdate update)
    {
        if(updates.Contains(update))
            return false;

        updates.Add(update);
        return true;
    }

    /// <summary>
    /// Удаляет объект из Update цикла.
    /// Возвращает true, если удаление прошло успешно.
    /// </summary>
    public bool Unregister(IPRUpdate update)
    {
        return updates.Remove(update);
    }

    /// <summary>
    /// Регистрирует объект в Tick цикле (интервальное обновление).
    /// </summary>
    public bool Register(IPRTickable tickable)
    {
        if (tickables.Contains(tickable))
            return false;

        tickables.Add(tickable);
        return true;
    }

    /// <summary>
    /// Удаляет объект из Tick цикла.
    /// </summary>
    public bool Unregister(IPRTickable tickable)
    {
        return tickables.Remove(tickable);
    }

    /// <summary>
    /// Регистрирует объект в FixedUpdate цикле.
    /// </summary>
    public bool Register(IPRFixedUpdate fixedUpdate)
    {
        if (fixedUpdates.Contains(fixedUpdate))
            return false;

        fixedUpdates.Add(fixedUpdate);
        return true;
    }

    /// <summary>
    /// Удаляет объект из FixedUpdate цикла.
    /// </summary>
    public bool Unregister(IPRFixedUpdate fixedUpdate)
    {
        return fixedUpdates.Remove(fixedUpdate);
    }

    #endregion

    #region MonoBehaviour

    private void Update()
    {
        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        PRUpdateRunner.RunUpdate();

        // Свои списки и тик читают настройки SDK, а хост теперь может появиться раньше
        // инициализации - его создаёт первый же объект сцены, которому нужен PRUpdate.
        if (!PRUnitySDK.IsInitialized || PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        for (int i = 0; i < updates.Count; i++)
            updates[i]?.PRUpdate();

        tickCooldown.TryExecute(GetHostTick(), tickAction ??= PRTick);
    }

    private void LateUpdate()
    {
        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        PRUpdateRunner.RunLateUpdate();
    }

    /// <summary>
    /// Сначала все PRFixedUpdate, затем шаг физики - как устроено у самой Unity: силы,
    /// приложенные в этом шаге, физика обработает в этом же шаге.
    /// </summary>
    private void FixedUpdate()
    {
        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        PRUpdateRunner.RunFixedUpdate();

        if (PRUnitySDK.PauseManager.IsLogicPaused)
            return;

        // Режим проверяется первым: до инициализации SDK физику шагает сама Unity, и
        // обращение к PRTime.Instance создало бы его раньше времени.
        if (Physics.simulationMode == SimulationMode.Script)
        {
            var gameFixedDeltaTime = PRTime.Instance.GameFixedDeltaTime;
            if (gameFixedDeltaTime > 0)
                Physics.Simulate(gameFixedDeltaTime);
        }

        if (!PRUnitySDK.IsInitialized)
            return;

        for (int i = 0; i < fixedUpdates.Count; i++)
            fixedUpdates[i]?.PRFixedUpdate();
    }

    #endregion

    #region Методы

    /// <summary>
    /// Вызов тикового обновления для всех зарегистрированных объектов.
    /// </summary>
    protected void PRTick()
    {
        for (int i = 0; i < tickables.Count; i++)
            tickables[i]?.PRTick();
    }

    /// <summary>
    /// Получить текущий интервал тика из настроек проекта.
    /// </summary>
    /// <returns></returns>
    public float GetHostTick()
    {
        return PRUnitySDK.Settings.Project.PRMonobehaviourHost.Tick;
    }

    public void SingletonInitialize()
    {
        Physics.simulationMode = SimulationMode.Script;
        PRLog.WriteDebug(this, $"Physics.simulationMode = {Physics.simulationMode}");
    }

    #endregion
}
