using System;

/// <summary>
/// Описание раздела настроек: что он вообще настраивает.
/// </summary>
/// <remarks>
/// Вешается на класс раздела и показывается в окне настроек над его полями. У каждого
/// поля есть <c>Tooltip</c>, но он отвечает на вопрос «что делает эта галка», а не
/// «зачем сюда вообще заходить», — из-за этого раздел, открытый впервые, читается
/// как список несвязанных переключателей.
/// <para>
/// Атрибут обычный, не редакторный: разделы лежат в рантайм-коде, и переносить ради
/// описания их в Editor-сборку незачем.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class SettingsDescriptionAttribute : Attribute
{
    /// <summary>
    /// Текст описания.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// На что обратить внимание: подводные камни, связи с другими разделами.
    /// </summary>
    /// <remarks>
    /// Пусто у большинства разделов. Показывается отдельной пометкой, потому что
    /// предупреждение, слитое с описанием, читают как продолжение текста и пропускают.
    /// </remarks>
    public string Warning { get; set; }

    /// <summary>
    /// Иконка раздела в списке окна настроек.
    /// </summary>
    /// <remarks>
    /// Имя белой пиктограммы без префикса — <c>"Sound"</c> для <c>PictoIcon_Sound.png</c> из
    /// <c>PRUnitySDK/Core/Editor/Icons/SettingsSections</c>. Подойдёт и имя встроенной иконки Unity
    /// (<c>"d_AudioSource Icon"</c>) или путь к картинке от <c>Assets/</c>. Не задана или не нашлась —
    /// раздел получает общую иконку настроек: по одной иконке раздел узнают в списке быстрее,
    /// чем по названию, а пустое место сбивало бы строки.
    /// </remarks>
    public string Icon { get; set; }

    /// <param name="description">Что настраивает раздел.</param>
    public SettingsDescriptionAttribute(string description)
    {
        Description = description;
    }
}
