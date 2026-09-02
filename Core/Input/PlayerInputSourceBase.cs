/// <summary>
/// Источник ввода: каждый кадр переносит состояние устройства в состояние игрока.
/// </summary>
/// <remarks>
/// База не знает ни устройства, ни игровых действий — она отвечает за плумбинг:
/// находит владельца, ждёт, пока он появится, и зовёт <see cref="InputHandle"/>
/// в фазе Update, до кадровой синхронизации <see cref="InputTranslator"/>.
/// Наследник читает своё устройство и пишет ключи в <see cref="InputState"/>.
/// </remarks>
public abstract class PlayerInputSourceBase : PRMonoBehaviour
{
    private PlayerBase owner;

    /// <summary>
    /// Игрок, которому принадлежит источник ввода, либо <c>null</c>, пока он не найден.
    /// </summary>
    protected PlayerBase Owner => owner;

    /// <summary>
    /// Состояние ввода владельца, либо <c>null</c>, пока владелец не найден.
    /// </summary>
    /// <remarks>
    /// Названо не <c>Input</c>, чтобы не перекрывать <see cref="UnityEngine.Input"/>
    /// в наследниках: они читают устройство именно через него.
    /// </remarks>
    protected PlayerInputState InputState => owner != null ? owner.GetInput() : null;

    protected override void PRUpdate()
    {
        if (!CanInput())
            return;

        InputHandle();
    }

    /// <summary>
    /// Можно ли читать ввод в этом кадре.
    /// </summary>
    /// <remarks>
    /// Наследники добавляют свои условия поверх базового — например, тип устройства.
    /// </remarks>
    protected virtual bool CanInput()
    {
        return HasOwner();
    }

    /// <summary>
    /// Ищет владельца, пока не найдёт, и запоминает результат.
    /// </summary>
    /// <remarks>
    /// Игрок появляется не сразу, поэтому поиск повторяется каждый кадр до успеха.
    /// </remarks>
    protected bool HasOwner()
    {
        if (owner != null)
            return true;

        owner = ResolveOwner();
        return owner != null;
    }

    /// <summary>
    /// Возвращает владельца источника или <c>null</c>, если он ещё не готов.
    /// </summary>
    protected abstract PlayerBase ResolveOwner();

    /// <summary>
    /// Читает устройство и пишет ключи в состояние ввода владельца.
    /// </summary>
    protected abstract void InputHandle();
}
