public interface ISingletonInitializer 
{
    public int InitializeOrder { get; }

    public void SingletonInitialize();
}
