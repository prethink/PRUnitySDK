using UnityEngine;

/// <summary>
/// Что делает переключатель, когда в него входят.
/// </summary>
public enum ObjectStateTriggerAction
{
    /// <summary>
    /// Показать объект.
    /// </summary>
    Open = 0,

    /// <summary>
    /// Спрятать объект.
    /// </summary>
    Hide = 1,

    /// <summary>
    /// Показать спрятанный и наоборот.
    /// </summary>
    Toggle = 2
}

/// <summary>
/// Площадка: переключает объект, когда в неё заходит игрок.
/// </summary>
/// <remarks>
/// <para>
/// Тот самый тайкун-приём: игрок наступает на площадку — появляется новый объект,
/// и он остаётся на месте после перезапуска. Площадка и появляющийся объект — разные
/// объекты сцены, и это не ограничение, а суть: площадка обязана остаться на месте,
/// когда управляемый объект спрятан.
/// </para>
/// <para>
/// Коллайдер на площадке должен быть отмечен как <c>Is Trigger</c>, иначе игрок в него
/// просто упрётся. Компонент это проверяет при старте и говорит, если галки нет.
/// </para>
/// </remarks>
[RequireComponent(typeof(Collider))]
public class SaveableObjectStateTriggerSwitch : SaveableObjectStateSwitchBase
{
    [Header("Триггер")]
    [Tooltip("Что сделать с объектом при входе в триггер.")]
    [SerializeField] protected ObjectStateTriggerAction action = ObjectStateTriggerAction.Open;

    [Tooltip("Срабатывать только на игрока. Снимите, если площадку должен нажимать кто угодно.")]
    [SerializeField] protected bool playerOnly = true;

    protected override void InitializationComponents()
    {
        base.InitializationComponents();

        WarnIfNotTrigger();
    }

    /// <inheritdoc />
    /// <remarks>
    /// На логической паузе <c>PROnTriggerEnter</c> не вызывается, и это верно:
    /// пока открыто окно, площадки срабатывать не должны.
    /// </remarks>
    protected override void PROnTriggerEnter(Collider other)
    {
        if (!CanActivate(other))
            return;

        switch (action)
        {
            case ObjectStateTriggerAction.Open:
                Open();
                return;

            case ObjectStateTriggerAction.Hide:
                Close();
                return;

            case ObjectStateTriggerAction.Toggle:
                Toggle();
                return;
        }
    }

    /// <summary>
    /// Этому вошедшему разрешено переключать.
    /// </summary>
    /// <remarks>
    /// Переопределите, чтобы сузить правило: например, до одного игрока или владельца
    /// площадки.
    /// </remarks>
    protected virtual bool CanActivate(Collider other)
    {
        if (!playerOnly)
            return true;

        // В родителях, а не на самом коллайдере: коллайдер обычно висит
        // на дочернем объекте персонажа.
        return other.GetComponentInParent<PlayerBase>() != null;
    }

    /// <summary>
    /// Предупреждает, если коллайдер не отмечен триггером.
    /// </summary>
    /// <remarks>
    /// Без галки площадка становится препятствием: ошибок нет, объект не появляется,
    /// а причину приходится искать в коде.
    /// </remarks>
    private void WarnIfNotTrigger()
    {
        foreach (Collider collider in GetComponents<Collider>())
        {
            if (collider.isTrigger)
                return;
        }

        PRLog.WriteWarning(this,
            $"У площадки [{name}] ни один коллайдер не отмечен Is Trigger: вход в неё не сработает.");
    }
}
