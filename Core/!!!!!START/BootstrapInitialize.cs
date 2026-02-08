using UnityEngine;
using UnityEngine.SceneManagement;

public static class BootstrapInitializer
{
    // Флаг, чтобы инициализация сработала только один раз
    private static bool _initialized = false;

    // Этот метод сработает **до всех Awake**
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        // Если уже инициализировано — выходим
        if (_initialized)
            return;

        _initialized = true; // ставим флаг, чтобы больше не срабатывало
        Debug.Log("Bootstrap: Инициализация до всех Awake");

        // Всегда вызываем загрузку первой сцены, чтобы гарантировать, что она будет загружена.
        // Это bootstrap сцена, которая может быть пустой или содержать только необходимые объекты для инициализации.
        SceneManager.LoadScene(0);
    }
}
