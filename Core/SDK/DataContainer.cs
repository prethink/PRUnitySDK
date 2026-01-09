using System;

public abstract class DataContainer 
{
    public void Initialize<T>(Action initializeAction)
    {
        try
        {
            PRUnitySDK.InitializeType<T>(() => { initializeAction.Invoke(); });
        }
        catch (ResourceNotFoundException resourceNotFoundException)
        {
            PRLog.WriteError(typeof(PRUnitySDK), $"Required create {resourceNotFoundException.Type} in 'Resources' folder. Use Assets/Create/PRUnitySDK/...");
            throw;
        }
        catch (MultipleResourcesFoundException multipleResourcesFoundException)
        {
            PRLog.WriteError(typeof(PRUnitySDK), $"More than one {multipleResourcesFoundException.Type} found in 'Resources' folder.");
            throw;
        }
        catch (Exception exception)
        {
            PRLog.WriteError(typeof(PRUnitySDK), $"{exception}");
            throw;
        }
    }
}
