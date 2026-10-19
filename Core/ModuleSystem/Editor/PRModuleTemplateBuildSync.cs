using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Перед сборкой WebGL обновляет копии шаблонов страниц из исходников включённых модулей.
/// </summary>
/// <remarks>
/// Исходник шаблона правят в модуле (<see cref="PRModuleManifest.WebGLTemplateSource"/>), а «Применить» в окне модулей
/// после этого не нажимают. Без этого шага в сборку ушла бы старая копия из <c>Assets/WebGLTemplates</c>.
/// </remarks>
public sealed class PRModuleTemplateBuildSync : IPreprocessBuildWithReport
{
    public int callbackOrder => -100;

    public void OnPreprocessBuild(BuildReport report)
    {
        if (report.summary.platform != BuildTarget.WebGL)
            return;

        var errors = PRModulePlugins.SyncTemplates(PRModuleCatalog.Load(), PRModuleDefines.ReadDisabledIds());
        AssetDatabase.Refresh();

        if (errors.Count > 0)
            throw new BuildFailedException("Шаблон WebGL не обновлён: " + string.Join("; ", errors));

        Debug.Log("PRUnitySDK: шаблоны WebGL модулей проверены перед сборкой.");
    }
}
