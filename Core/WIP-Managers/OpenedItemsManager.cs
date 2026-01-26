using System;
using System.Linq;

public class OpenedItemsManager : SingletonProviderBase<OpenedItemsManager>
{
    public bool IsOpenedItem(ISelectableItem selectableItem)
    {
        return IsOpenedItem(selectableItem.Id);
    }

    public bool IsOpenedItem(string id)
    {
        return GameManager.Instance.GetProjectData().OpenedItems.Any(x => x.HasItem(id));
    }

    public bool IsOpenedItem(Type type, ISelectableItem selectableItem)
    {
        return IsOpenedItem(type.ToString(), selectableItem.Id);
    }

    public bool IsOpenedItem(Type type, string id)
    {
        return IsOpenedItem(type.ToString(), id);
    }

    public bool IsOpenedItem(string type, string id)
    {
        return GameManager.Instance.GetProjectData().OpenedItems.Any(x => x.HasItem(id) && x.Created.Equals(type));
    }

    public bool AddOpenItem(Type type, ISelectableItem selectableItem, bool requiredSave = true)
    {
        return AddOpenItem(type.ToString(), selectableItem, requiredSave);
    }

    public bool AddOpenItem(string type, ISelectableItem selectableItem, bool requiredSave = true)
    {
        return AddOpenItem(type, selectableItem, 1, requiredSave);
    }

    public bool AddOpenItem(string type, ISelectableItem selectableItem, int count, bool requiredSave = true)
    {
        var item = GameManager.Instance.GetProjectData().OpenedItems.FirstOrDefault(x => x.HasItem(selectableItem.Id));
        if(item == null)
        {
            item = ItemStack.CreateEmpty(type, selectableItem);
            GameManager.Instance.GetProjectData().OpenedItems.Add(item);
        }

        item.Add(count);
        if (requiredSave)
            GameManager.Instance.SaveProjectData();

        return true;
    }
}
