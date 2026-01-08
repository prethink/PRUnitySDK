using System.Linq;
using UnityEngine;

public partial class Bootstrap : MonoBehaviour
{
    #region  Поля и свойства

    /// <summary>
    /// Признак того, что инициализация SDK была переопределена.
    /// </summary>
    private bool isOverriden;

    #endregion

    #region MonoBehaviour

    /// <inheritdoc />
    private void Awake()
    {
        TryOverrideBootstrap();

        if (!isOverriden)
            InitializeSDK();
    }

    #endregion

    #region Методы

    /// <summary>
    /// Перехват метода инициализации SDK для возможности кастомной инициализации.
    /// </summary>
    private void TryOverrideBootstrap()
    {
        var overrideMethod = this.GetMethods<OverrideBootstrapAttribute>().FirstOrDefault();
        overrideMethod?.Invoke(this, null);
    }

    /// <summary>
    /// Инициализация SDK.
    /// </summary>
    private void InitializeSDK()
    {
        PRUnitySDK.InitializeSDK();
    }

    #endregion
}
