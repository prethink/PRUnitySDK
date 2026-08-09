using System.Collections.Generic;

/// <summary>
/// Aggregates project-wide and registered scene flag resolvers.
/// </summary>
public class FlagsManager : MonoBehaviourSingletonBase<FlagsManager>
{
    protected readonly FlagResolver ProjectFlags = new();
    protected readonly HashSet<FlagResolver> SceneFlags = new();

    /// <summary>
    /// Project-wide resolver. Its influences participate in every global query.
    /// </summary>
    public FlagResolver Global => ProjectFlags;

    public IReadOnlyCollection<FlagResolver> Scenes => SceneFlags;

    public bool AddSceneFlags(FlagResolver flagResolver)
    {
        return flagResolver != null && SceneFlags.Add(flagResolver);
    }

    public bool RemoveSceneFlags(FlagResolver flagResolver)
    {
        return flagResolver != null && SceneFlags.Remove(flagResolver);
    }

    public void Allow(Enumeration key, object source) => ProjectFlags.Allow(key, source);

    public void Deny(Enumeration key, object source) => ProjectFlags.Deny(key, source);

    public void Remove(Enumeration key, object source) => ProjectFlags.Remove(key, source);

    public void ClearSource(object source) => ProjectFlags.ClearSource(source);

    /// <summary>
    /// Aggregates project and scene decisions. Deny has absolute priority.
    /// </summary>
    public FlagDecision Resolve(Enumeration key)
    {
        bool hasAllow = false;

        if (Evaluate(ProjectFlags, key, ref hasAllow))
            return FlagDecision.Deny;

        foreach (var scene in SceneFlags)
        {
            if (Evaluate(scene, key, ref hasAllow))
                return FlagDecision.Deny;
        }

        return hasAllow ? FlagDecision.Allow : FlagDecision.Unspecified;
    }

    public bool Get(Enumeration key, bool defaultValue = true)
    {
        return Resolve(key) switch
        {
            FlagDecision.Allow => true,
            FlagDecision.Deny => false,
            _ => defaultValue
        };
    }

    private static bool Evaluate(FlagResolver resolver, Enumeration key, ref bool hasAllow)
    {
        FlagDecision decision = resolver.Resolve(key);
        if (decision == FlagDecision.Deny)
            return true;

        if (decision == FlagDecision.Allow)
            hasAllow = true;

        return false;
    }
}
