using UnityEngine;

/// <summary>
/// Переопределяет описание сущности: имя, иконку, локализацию и качество.
/// </summary>
/// <remarks>
/// Компонент вешается на объект сущности рядом с ней. <see cref="EntityUtils.GetEntityInfo"/>
/// находит его через <c>GetComponent</c> и подставляет как <c>Override</c>-описание поверх
/// базового, поэтому подменить можно часть полей, не трогая остальные - например, дать
/// конкретному экземпляру собственную иконку.
/// </remarks>
public class EntityInfoProvider : PRMonoBehaviour, IEntityInfoProvider
{
    /// <summary>
    /// Ассет с переопределяющим описанием.
    /// </summary>
    [field: SerializeField] public EntityInfoBase EntityInfoData { get; private set; }

    /// <inheritdoc />
    public IEntityInfo EntityInfo => EntityInfoData;

    /// <summary>
    /// Заменяет описание в рантайме.
    /// </summary>
    /// <remarks>
    /// Контейнер <see cref="EntityInfoContainer"/> собирается один раз при инициализации
    /// сущности, поэтому вызывать метод нужно до её регистрации - позже сущность
    /// придётся переинициализировать.
    /// </remarks>
    /// <param name="entityInfo">Новое описание.</param>
    public void SetEntityInfo(EntityInfoBase entityInfo)
    {
        EntityInfoData = entityInfo;
    }
}
