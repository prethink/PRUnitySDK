using DG.Tweening;

/// <summary>
/// Управляемый DOTween-эффект, реагирующий на логическую паузу SDK.
/// </summary>
public interface IDoTweenEffect : IPauseStateListener
{
    /// <summary>
    /// Функция сглаживания эффекта.
    /// </summary>
    Ease Ease { get; }

    /// <summary>
    /// Тип повторения эффекта.
    /// </summary>
    LoopType LoopType { get; }

    /// <summary>
    /// Количество циклов. Значение -1 означает бесконечное повторение.
    /// </summary>
    int LoopCount { get; }

    /// <summary>
    /// Длительность одного цикла в секундах.
    /// </summary>
    float Duration { get; }

    /// <summary>
    /// Нужно ли создать эффект автоматически в Start().
    /// </summary>
    bool PlayAnimationOnStart { get; }

    /// <summary>
    /// Пересоздаёт DOTween-анимацию и возвращает её экземпляр.
    /// </summary>
    Tween CreateAnimation();

    /// <summary>
    /// Возобновляет уже созданную анимацию.
    /// </summary>
    void StartAnimation();

    /// <summary>
    /// Приостанавливает анимацию с сохранением прогресса.
    /// </summary>
    void StopAnimation();

    /// <summary>
    /// Уничтожает анимацию.
    /// </summary>
    void DestroyAnimation();
}
