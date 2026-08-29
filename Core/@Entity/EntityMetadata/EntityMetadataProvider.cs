using UnityEngine;

/// <summary>
/// Переопределяет описание сущности: имя, иконку, локализацию и качество.
/// </summary>
/// <remarks>
/// Компонент вешается на объект сущности рядом с ней. <see cref="EntityUtils.GetEntityMetadata"/>
/// находит его через <c>GetComponent</c> и подставляет как <c>Override</c>-описание поверх
/// базового, поэтому подменить можно часть полей, не трогая остальные - например, дать
/// конкретному экземпляру собственную иконку.
/// </remarks>
public class EntityMetadataProvider : PRMonoBehaviour, IEntityMetadataProvider
{
    /// <summary>
    /// Ассет с переопределяющим описанием.
    /// </summary>
    [field: SerializeField] public EntityMetadataBase EntityMetadataData { get; private set; }

    /// <inheritdoc />
    public IEntityMetadata EntityMetadata => EntityMetadataData;

    /// <summary>
    /// Заменяет описание в рантайме.
    /// </summary>
    /// <remarks>
    /// Контейнер <see cref="EntityMetadataContainer"/> собирается один раз при инициализации
    /// сущности, поэтому вызывать метод нужно до её регистрации - позже сущность
    /// придётся переинициализировать.
    /// </remarks>
    /// <param name="entityInfo">Новое описание.</param>
    public void SetEntityMetadata(EntityMetadataBase entityInfo)
    {
        EntityMetadataData = entityInfo;
    }
}
