using System;

public class SelectableItem : ISelectableItem
{
    public string Id { get; set; }

    public static SelectableItem GetEmptySelectedItem()
    {
        return new SelectableItem() { Id = Guid.Empty.ToString() };
    }

    public void GenerateId() { }

    public void GenerateIdIfNull() { }

    /// <summary>
    /// У предмета есть идентификатор.
    /// </summary>
    /// <remarks>
    /// Проверяется наличие строки, а не формат GUID: идентификатором бывает и ключ
    /// перечисления — например, у валют. Прежняя проверка через <c>Guid.TryParse</c>
    /// считала такие предметы невалидными.
    /// </remarks>
    public bool IsValid => !string.IsNullOrWhiteSpace(Id) && Id != Guid.Empty.ToString();


    public static ISelectableItem Create(string id)
    {
        return new SelectableItem() { Id = id };
    }

    public static ISelectableItem Create(Guid id)
    {
        return new SelectableItem() { Id = id.ToString() };
    }

    public static ISelectableItem CreateEmpty()
    {
        return new SelectableItem() { Id = Guid.Empty.ToString() };
    }
}