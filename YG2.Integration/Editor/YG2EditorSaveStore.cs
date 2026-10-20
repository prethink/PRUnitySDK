#if !PRSDK_DISABLE_YG2
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Сохранение Яндекса (YG2) в редакторе: <c>Assets/PluginYourGames/Editor/SavesEditorYG2.json</c>.
/// </summary>
/// <remarks>
/// Его переключает <see cref="PRSDKProjectSaves"/> вместе с проектом SDK: у каждой игры свой прогресс.
/// </remarks>
public sealed class YG2EditorSaveStore : IPRSDKEditorSaveStore
{
    /// <inheritdoc />
    public string Platform => "Yandex";

    /// <inheritdoc />
    public IEnumerable<string> Files
    {
        get { yield return Path.Combine(YG.InfoYG.PATCH_PC_EDITOR, "SavesEditorYG2.json"); }
    }
}
#endif
