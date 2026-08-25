/// <summary>
/// Базовые параметры события игрового ресурса.
/// </summary>
public class ResourceEventArgs : EventArgsBase
{
    /// <summary>
    /// Тип изменённого ресурса.
    /// </summary>
    public Enumeration ResourceType { get; protected set; }

    /// <summary>
    /// Создаёт параметры события указанного ресурса.
    /// </summary>
    public ResourceEventArgs(Enumeration resourceType)
    {
        ResourceType = resourceType;
    }
}
