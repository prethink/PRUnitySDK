using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ассет-действие из нескольких встроенных, выполняемых по порядку.
/// </summary>
/// <remarks>
/// Соединяет сильные стороны обоих способов хранения: ассет переиспользуется и на него
/// ссылаются отовсюду, а содержимое собирается списком - без класса под каждую комбинацию.
/// Типичный случай - «награда за вход»: начислить монеты, показать окно, записать метрику.
/// <para>
/// Если действие всего одно, берите <see cref="InlineActionContainer"/>: список из одного
/// элемента только загромождает инспектор.
/// </para>
/// </remarks>
[CreateAssetMenu(fileName = "Inline Action Pipeline", menuName = "PRUnitySDK/Actions/Inline action pipeline")]
public class InlineActionPipeline : IconActionBase
{
    [SerializeReference, ReferenceSelector]
    [Tooltip("Действия выполняются сверху вниз.")]
    private List<IAction> actions = new();

    [SerializeField]
    [Tooltip("Прерывать выполнение, если очередное действие вернуло false.")]
    private bool stopOnFailure;

    /// <summary>
    /// Действия, входящие в конвейер.
    /// </summary>
    public IReadOnlyList<IAction> Actions => actions;

    /// <summary>
    /// Количество действий, выполненных при последнем вызове <see cref="ActionBase.Execute"/>.
    /// </summary>
    public int LastExecutedCount { get; private set; }

    /// <summary>
    /// Проверяет, что конвейер не пуст и общие условия выполнения соблюдены.
    /// </summary>
    /// <remarks>
    /// Выполнимость каждого действия здесь не проверяется: каждое из них решает само.
    /// Строгую проверку «все готовы» ассет не навязывает, потому что частично применимый
    /// набор - обычная ситуация: часть наград может быть уже выдана.
    /// </remarks>
    public override bool CanExecute()
    {
        return base.CanExecute() && ActionSequence.HasAny(actions);
    }

    /// <summary>
    /// Проверяет, выполнимо ли сейчас хотя бы одно действие конвейера.
    /// </summary>
    public bool CanExecuteAny()
    {
        return ActionSequence.CanExecuteAny(actions);
    }

    /// <inheritdoc />
    protected override void Action()
    {
        LastExecutedCount = ActionSequence.Execute(actions, stopOnFailure);
    }
}
