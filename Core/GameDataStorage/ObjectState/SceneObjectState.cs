using System;

/// <summary>
/// Сохранённое состояние одного объекта сцены.
/// </summary>
/// <remarks>
/// Активность лежит отдельным полем, потому что нужна почти всем. Остальное наследники
/// <c>SaveableObjectState</c> кладут своими ключами <see cref="EnumerationType{T}"/>,
/// которые несут и имя значения, и его тип, поэтому <c>Set</c> и <c>TryGet</c> работают
/// с настоящим типом.
/// </remarks>
[Serializable]
public class SceneObjectState : StateValues, ICloneable
{
    /// <summary>
    /// Был ли объект включён на момент сохранения.
    /// </summary>
    public bool IsActive = true;

    /// <inheritdoc />
    public object Clone()
    {
        var clone = new SceneObjectState { IsActive = IsActive };
        CopyValuesTo(clone);

        return clone;
    }
}
