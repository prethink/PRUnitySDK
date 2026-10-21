using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

/// <summary>
/// Перед сборкой сверяет номер сцены игры: стартовая сцена должна загрузить сцену игры активного проекта.
/// </summary>
/// <remarks>
/// <see cref="PRSDKProjectScenes.Apply"/> ставит сцену игры номером 1, а стартовая грузит номер из
/// <c>Bootstrap → Game Scene Index</c>. Разойдутся — в сборке после загрузки откроется не та сцена или никакой;
/// в редакторе этого не видно (Play идёт со сцены редактора), поэтому сборка останавливается.
/// <para>
/// Сверяется список <c>EditorBuildSettings</c>: на этом шаге Unity не сообщает, какие сцены уйдут в сборку. Сборка
/// скриптом со своим <c>BuildPlayerOptions.scenes</c> должна проверять свой список сама —
/// <see cref="PRSDKProjectScenes.CheckBuild"/>.
/// </para>
/// </remarks>
public sealed class PRSDKProjectScenesBuildCheck : IPreprocessBuildWithReport
{
    public int callbackOrder => -100;

    public void OnPreprocessBuild(BuildReport report)
    {
        PRSDKProject project = PRSDKActiveProject.Instance != null ? PRSDKActiveProject.Instance.Project : null;
        string[] enabled = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();

        string error = PRSDKProjectScenes.CheckBuild(project, enabled);
        if (error != null)
            throw new BuildFailedException("[PRUnitySDK] Сборка остановлена: " + error + ".");
    }
}
