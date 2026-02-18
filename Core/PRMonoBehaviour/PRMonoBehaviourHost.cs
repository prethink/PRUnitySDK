using System.Collections.Generic;

/// <summary>
/// Центральный хост, управляющий пользовательским циклом обновления.
/// </summary>
public class PRMonoBehaviourHost : PRMonoBehaviourSingletonBase<PRMonoBehaviourHost>
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
    private Cooldown tickCooldown = new Cooldown();

    #endregion

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

    #region Базовый класс

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    protected override void PRUpdate()
    {
        base.PRUpdate();

        for (int i = 0; i < updates.Count; i++)
            updates[i]?.PRUpdate();

        tickCooldown.TryExecute(GetHostTick(), () =>
        {
            PRTick();
        });
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    override protected void PRFixedUpdate()
    {
        base.PRFixedUpdate();

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

    #endregion
}
