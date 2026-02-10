public class NameProvider : INameProvider
{
    public NameProvider(string name)
    {
        Name = name;
    }

    public string Name { get; protected set; }
}
