using System;

public abstract class SingletonProviderBase<T> 
    where T : class
{
    #region Поля и свойства

    /// <summary>
    /// Lazy инициализация глобального экземпляра настроек.
    /// </summary>
    protected static T instance;

    /// <summary>
    /// Singleton instance.
    /// </summary>
    public static T Instance
    {
        get
        {
            if(instance == null)
                instance = Activator.CreateInstance<T>();

            return instance;
        }
    }

    #endregion

    #region Методы

    /// <summary>
    /// Установить новый экземпляр глобальных настроек.
    /// </summary>
    /// <param name="newInstance"></param>
    public static void Override(T newInstance)
    {
        instance = newInstance;
    }

    #endregion
}
