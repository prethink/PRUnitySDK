using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Основа переключателей: открывает и закрывает объект, чьё состояние хранится
/// между запусками.
/// </summary>
/// <remarks>
/// <see cref="SaveableObjectState"/> хранит состояние, переключатель решает, когда его
/// менять. Здесь общая часть: ссылка на состояние, <c>Open</c>, <c>Close</c>,
/// <c>Toggle</c>, события и защита от повторного срабатывания. Повод задаёт наследник,
/// готовый есть у <see cref="SaveableObjectStateTriggerSwitch"/>.
/// <para>
/// Обычно висит не на том объекте, которым управляет: на кнопке, площадке покупки или
/// триггере. Управлять своим же объектом можно, но, спрятав его, компонент выключится
/// вместе с ним, и показать объект обратно сможет только кто-то снаружи.
/// </para>
/// </remarks>
public abstract class SaveableObjectStateSwitchBase : PRMonoBehaviour
{
    [Tooltip("Чьё состояние переключаем. Обычно объект, которым управляем, — другой.")]
    [SerializeField] protected SaveableObjectState state;

    [Tooltip("Записывать сохранение сразу после переключения.")]
    [SerializeField] protected bool saveImmediately = true;

    [Header("События")]
    [SerializeField] protected UnityEvent onOpened;
    [SerializeField] protected UnityEvent onClosed;

    /// <summary>
    /// Состояние, которым управляем.
    /// </summary>
    public SaveableObjectState State => state;

    /// <summary>
    /// Объект сейчас открыт.
    /// </summary>
    public bool IsOpened => state != null && state.IsOpened;

    protected override void InitializationComponents()
    {
        base.InitializationComponents();

        if (state == null)
        {
            PRLog.WriteWarning(this, "Состояние не указано: переключать нечего.");
            return;
        }

        // Проверяем не совпадение объектов, а вложенность: выключение цели гасит
        // и её потомков, поэтому переключатель на дочернем объекте попадает
        // в ту же ловушку, что и на самой цели.
        if (state.Target != null && transform.IsChildOf(state.Target.transform))
        {
            PRLog.WriteWarning(this,
                $"Переключатель находится внутри объекта [{state.Target.name}], которым управляет: " +
                "спрятав его, он выключится вместе с ним, и показать обратно сможет только кто-то снаружи.");
        }
    }

    /// <summary>
    /// Открывает объект.
    /// </summary>
    public void Open()
    {
        SetOpened(true);
    }

    /// <summary>
    /// Закрывает объект.
    /// </summary>
    public void Close()
    {
        SetOpened(false);
    }

    /// <summary>
    /// Открывает закрытый объект и наоборот.
    /// </summary>
    public void Toggle()
    {
        SetOpened(!IsOpened);
    }

    /// <summary>
    /// Приводит объект к нужному виду и сохраняет это.
    /// </summary>
    /// <remarks>
    /// Повторное открытие уже открытого ничего не делает и события не поднимает:
    /// иначе триггер, в котором игрок стоит, слал бы их каждый кадр.
    /// </remarks>
    public void SetOpened(bool isOpened)
    {
        if (state == null)
        {
            PRLog.WriteWarning(this, "Состояние не указано: переключать нечего.");
            return;
        }

        if (IsOpened == isOpened)
            return;

        state.SetActiveState(isOpened, saveImmediately);

        if (isOpened)
            onOpened?.Invoke();
        else
            onClosed?.Invoke();
    }
}
