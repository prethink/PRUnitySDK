public interface IMonoWindowFactory : IResourcePathProvider
{
    bool UseSharedCanvas { get; }
    bool WorldPositionStays { get; }
}
