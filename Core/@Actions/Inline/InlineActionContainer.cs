using UnityEngine;

/// <summary>
/// Ассет-действие с одним встроенным действием.
/// </summary>
/// <remarks>
/// Простой случай: действие нужно переиспользовать из нескольких мест, но писать под него
/// отдельный класс <see cref="ActionBase"/> незачем - достаточно выбрать готовую реализацию
/// и настроить её параметры.
/// <para>
/// Когда действий несколько и они должны выполняться по порядку, берите
/// <see cref="InlineActionPipeline"/>.
/// </para>
/// <para>
/// Наследует <see cref="IconActionBase"/>, поэтому подходит везде, где нужна иконка -
/// например, в <c>ActionContainer</c> из модуля сущностей.
/// </para>
/// </remarks>
[CreateAssetMenu(fileName = "Inline Action", menuName = "PRUnitySDK/Actions/Inline action")]
public class InlineActionContainer : IconActionBase
{
    [SerializeReference, ReferenceSelector]
    [Tooltip("Действие, которое выполняет этот ассет.")]
    private IAction action;

    /// <summary>
    /// Настроенное действие либо <see langword="null"/>, если тип ещё не выбран.
    /// </summary>
    public IAction InnerAction => action;

    /// <summary>
    /// Проверяет общие условия и готовность вложенного действия.
    /// </summary>
    public override bool CanExecute()
    {
        return base.CanExecute() && action != null && action.CanExecute();
    }

    /// <inheritdoc />
    protected override void Action()
    {
        action.Execute();
    }
}
