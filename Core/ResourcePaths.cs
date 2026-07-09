public partial class ResourcePaths 
{
    public const string MonoWindowFolderName        = "MonoWindows";
    public const string WindowFolderName            = "Windows";
    public const string PrefabsFolderName           = "Prefabs";
    public const string NotifierFolderName          = "Notifier";
    public const string PRUnitySDKFolderName        = "PRUnitySDK";

    public readonly string CorePath                 = $"{PRUnitySDKFolderName}";
    public readonly string PrefabsPath              = $"{PRUnitySDKFolderName}/{PrefabsFolderName}";
    public readonly string MonoWindowsPaths         = $"{PRUnitySDKFolderName}/{PrefabsFolderName}/{WindowFolderName}/{MonoWindowFolderName}";
    public readonly string NotifiersPath            = $"{PRUnitySDKFolderName}/{PrefabsFolderName}/{WindowFolderName}/{NotifierFolderName}";
}
