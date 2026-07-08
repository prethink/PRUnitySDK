public class ItemStack 
{
    public string Created { get; set; }
    public IIdentifiable Item { get; set; }

    public int Count { get; set; }

    public bool HasItem(string id)
    {
        return Item.Id == id && Count > 0;
    }

    public void Add(int count)
    {
        Count += count;
    }

    public static ItemStack Create(string created, IIdentifiable item, int count = 1)
    {
        return new ItemStack()
        {
            Created = created,
            Item = item,
            Count = count
        };
    }

    public static ItemStack CreateEmpty(string type, IIdentifiable item, int count = 1)
    {
        return Create(type, item, 0);
    }
}
