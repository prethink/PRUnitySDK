using UnityEngine;

/// <summary>
/// Указатель на текущий проект SDK.
/// </summary>
/// <remarks>
/// Единственный ассет проектов, лежащий в <c>Resources</c>: по нему игра узнаёт, чьи
/// база, настройки и префабы брать. Сами проекты держатся вне ресурсов, поэтому в сборку
/// попадает только активный — вместе со всем, на что он ссылается.
/// </remarks>
public class PRSDKActiveProject : ScriptableObjectSingleton<PRSDKActiveProject>
{
    [SerializeField]
    [Tooltip("Проект, с которым собирается игра. Пусто — данные берутся из Resources, как раньше.")]
    private PRSDKProject project;

    /// <summary>
    /// Текущий проект либо <see langword="null"/>.
    /// </summary>
    public PRSDKProject Project => project;

    /// <summary>
    /// Ассет текущего проекта нужного вида.
    /// </summary>
    /// <remarks>
    /// Возвращает <see langword="null"/>, когда проект не выбран или в нём этой части
    /// нет: тогда синглтон грузится прежним путём. Так переход на проекты не ломает
    /// проект, который на них ещё не перешёл.
    /// <para>
    /// Сам указатель через себя не разрешается — иначе получилась бы бесконечная
    /// рекурсия при первом же обращении.
    /// </para>
    /// </remarks>
    public static T ResolveAsset<T>() where T : ScriptableObject
    {
        if (typeof(T) == typeof(PRSDKActiveProject))
            return null;

        PRSDKActiveProject pointer = Instance;

        return pointer != null && pointer.project != null ? pointer.project.Resolve<T>() : null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Делает проект текущим.
    /// </summary>
    public void SetProject(PRSDKProject value)
    {
        project = value;
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
    }
#endif
}
