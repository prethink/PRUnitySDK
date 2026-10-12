using UnityEngine;

/// <summary>
/// Приложение закрывается: в редакторе это и выход из Play Mode.
/// </summary>
/// <remarks>
/// После <see cref="Application.quitting"/> объекты сцены уничтожаются, и их обработчики ещё могут
/// обратиться к синглтону. Создавать его заново в этот момент нельзя: новый объект переживает
/// закрытие сцены, и редактор пишет «Some objects were not cleaned up».
/// </remarks>
public static class ApplicationLifetime
{
    /// <summary>
    /// Идёт закрытие приложения.
    /// </summary>
    public static bool IsQuitting { get; private set; }

    // Сброс на каждом входе в игру: без перезагрузки домена статика пережила бы прошлый запуск.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        IsQuitting = false;
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;
    }

    private static void OnQuitting() => IsQuitting = true;
}
