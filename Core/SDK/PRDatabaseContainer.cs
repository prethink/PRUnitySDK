public partial class PRDatabaseContainer : DataContainer
{
    public PRDatabase Database { get; protected set; }

    public void Initialize()
    {
        Initialize<PRDatabase>(() => Database = ResourcesUtils.GetOrCreateResourceSO<PRDatabase>());
        this.RunMethodHooks(MethodHookStage.SDK);
    }
}
