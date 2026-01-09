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

    public bool IsValid => Guid.TryParse(Id, out var _);

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