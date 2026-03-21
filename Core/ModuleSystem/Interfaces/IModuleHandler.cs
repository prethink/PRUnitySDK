public interface IModuleHandler<TContext> 
{
    void Handle(TContext context);
}
