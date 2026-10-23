using UnityEngine;

/// <summary>
/// Esc закрывает открытое окно, как крестик.
/// </summary>
/// <remarks>
/// Один на игру и слушает клавишу обычным <c>Update</c>, а не <c>PRUpdate</c>: окна ставят игру на логическую
/// паузу, а на паузе <c>PRUpdate</c> не вызывается. Закрывается только окно с доступным крестиком
/// (<see cref="MonoWindowBase.TryPressExitButton"/>) — обязательные окна без крестика Esc не обойдёт.
/// Esc в управлении не назначается (окно клавиш его не принимает), так что с действиями игрока не спорит.
/// </remarks>
public sealed class MonoWindowEscapeCloser : MonoBehaviour
{
    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape))
            return;

        MonoWindowBase window = PRUnitySDK.Trackers.MonoWindows.CurrentWindow;

        if (window != null)
            window.TryPressExitButton();
    }
}
