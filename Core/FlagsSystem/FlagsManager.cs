using System.Collections.Generic;

public class FlagsManager : MonoBehaviourSingletonBase<FlagsManager>
{
    protected FlagResolver ProjectFlags = new FlagResolver();
    protected HashSet<FlagResolver> SceneFlags = new HashSet<FlagResolver>();

    public bool AddSceneFlags(FlagResolver flagResolver)
    {
        return SceneFlags.Add(flagResolver);
    }

    public bool RemoveSceneFlags(FlagResolver flagResolver)
    {
        return SceneFlags.Remove(flagResolver);
    }

    /// <summary>
    /// Главная агрегация всех флагов проекта + сцен.
    /// Deny имеет абсолютный приоритет.
    /// </summary>
    public bool Get(Enumeration key, bool defaultValue = true)
    {
        bool hasAllow = false;

        // 1. project layer
        if (Evaluate(ProjectFlags, key, ref hasAllow))
            return false;

        // 2. scene layers
        foreach (var scene in SceneFlags)
        {
            if (Evaluate(scene, key, ref hasAllow))
                return false;
        }

        return hasAllow 
            ? true 
            : defaultValue;
    }

    /// <summary>
    /// Возвращает true если найден Deny (ранний выход)
    /// </summary>
    private bool Evaluate(FlagResolver resolver, Enumeration key, ref bool hasAllow)
    {
        if (!resolver.HasAny(key))
            return false;

        bool value = resolver.Get(key);

        if (value == false)
            return true; // Deny найден → стоп всё

        hasAllow = true;
        return false;
    }
}
