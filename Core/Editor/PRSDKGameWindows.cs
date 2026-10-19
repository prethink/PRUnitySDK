using System;
using System.Linq;
using UnityEditor;

/// <summary>
/// Окно данных игры: правит базу или настройки активного проекта.
/// </summary>
/// <remarks>
/// Такие окна собраны в меню <c>PRUnitySDK/Game</c> и открываются вкладками одной группы
/// (<see cref="PRSDKGameWindows.Open{T}"/>).
/// </remarks>
public interface IPRSDKGameWindow
{
}

/// <summary>
/// Открывает окна данных игры вкладками рядом друг с другом.
/// </summary>
/// <remarks>
/// Окна одной темы — предметы, награды, витрина, покупки — правят данные одного проекта, и с ними
/// работают вместе. Отдельными окнами они расползались по экрану: какое-то висело само по себе
/// с крестиком, какое-то пристыковывалось. Вкладки одной группы ведут себя одинаково.
/// </remarks>
public static class PRSDKGameWindows
{
    /// <summary>
    /// Показывает окно; новое пристыковывается вкладкой к уже открытому окну группы.
    /// </summary>
    public static T Open<T>(string title) where T : EditorWindow, IPRSDKGameWindow
    {
        Type[] group = TypeCache.GetTypesDerivedFrom<IPRSDKGameWindow>()
            .Where(type => type != typeof(T) && !type.IsAbstract && typeof(EditorWindow).IsAssignableFrom(type))
            .ToArray();

        var window = EditorWindow.GetWindow<T>(title, true, group);
        window.Show();
        return window;
    }
}
