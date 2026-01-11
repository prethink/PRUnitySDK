public partial class PRDatabaseContainer : DataContainer
{
    public PRDatabase Core { get; protected set; }

    public void Initialize()
    {
        Initialize<PRDatabase>(() => Core = ResourcesUtils.GetOrCreateResourceSO<PRDatabase>());

        this.RunMethodHooks(MethodHookStage.SDK);
    }
}
