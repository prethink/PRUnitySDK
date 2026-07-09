using System;

public interface IMonoWindowFactory : IResourcePathProvider
{
    bool UseSharedCanvas { get; }
}
