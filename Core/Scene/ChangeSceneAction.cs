using UnityEngine;

/// <summary>
/// Действие «перейти на сцену» по имени: кнопка «Играть» в меню, «Выйти в меню» на уровне.
/// </summary>
/// <remarks>
/// Переход — <see cref="SceneChanger.SceneChange(string, System.Action)"/>, с затемнением по настройкам. Сцена должна
/// быть включена в сборке: сцену игры и второстепенные сцены проекта (<c>PRSDKProject.ExtraSceneGuids</c>) туда ставит
/// смена проекта. Сцены нет в сборке — действие недоступно, а не падает при нажатии.
/// </remarks>
[CreateAssetMenu(fileName = "ChangeSceneAction", menuName = "PRUnitySDK/Actions/Change scene")]
public class ChangeSceneAction : ActionBase
{
    [Tooltip("Имя сцены без пути и расширения, как в списке сборки.")]
    [SerializeField] private string sceneName;

    /// <summary>
    /// Сцена, на которую ведёт действие.
    /// </summary>
    public string SceneName => sceneName;

    public override ActionResult CanExecute()
    {
        ActionResult availability = base.CanExecute();
        if (availability.IsFailed)
            return availability;

        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            return ActionResult.Fail(ActionLabels.Unavailable);

        // Переход уже идёт (затемнение не сошло): второй запрос загрузил бы сцену дважды.
        return changing ? ActionResult.Fail(ActionLabels.Unavailable) : ActionResult.Success;
    }

    /// <summary>
    /// Переход по любому такому действию идёт: от нажатия до показа новой сцены.
    /// </summary>
    private static bool changing;

    protected override ActionResult Action()
    {
        changing = true;
        SceneChanger.Instance.SceneChange(sceneName, () => changing = false);
        return ActionResult.Success;
    }

    /// <summary>
    /// Статика переживает выход из игры в редакторе без перезагрузки домена — флаг сбрасывается на входе.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnEnterPlay() => changing = false;
}
