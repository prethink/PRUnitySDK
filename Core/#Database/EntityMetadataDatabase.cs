using System;

/// <summary>
/// Описания сущностей: имя, иконка, качество и переводы.
/// </summary>
/// <remarks>
/// Прежнее название — <c>EntityInfoDatabase</c>. «Info» не отличало эти данные от прочих
/// сведений о сущности: здесь лежит именно то, чем она представляется игроку, то есть
/// метаданные, а не состояние.
/// </remarks>
[Serializable]
[DatabaseExternalEditor(
    "PRUnitySDK/Windows/Entity metadata",
    WindowName = "Описания сущностей",
    Description = "Имена, иконки и переводы сущностей правятся там.")]
public class EntityMetadataDatabase : Database<EntityMetadataBase>
{
    public static EntityMetadataDatabase Instance => PRUnitySDK.Database.EntityMetadata;
}
