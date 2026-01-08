using UnityEngine;

/// <summary>
/// Базовый объект действия.
/// Может быть клик по ссылке, загрузка сцены, или что-то другое.
/// </summary>
public abstract class ActionBase : ScriptableObject, IIconItem
{
    /// <summary>
    /// Иконка действия.
    /// </summary>
    [field: SerializeField] public Sprite Icon { get; protected set; }

    /// <summary>
    /// Выполнить действие.
    /// </summary>
    public virtual void Execute()
    {
        if(CanExecute())
        {
            Action();
        }
    }

    /// <summary>
    /// Можно ли выполнить действие.
    /// </summary>
    /// <returns></returns>
    public virtual bool CanExecute()
    {
        if(!PRUnitySDK.IsInitialized)
        {
            PRLog.WriteWarning(GetType(), $"Can't execute action, SDK not initialized.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Само действие.
    /// </summary>
    protected abstract void Action();
}
