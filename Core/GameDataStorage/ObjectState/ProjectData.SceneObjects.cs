using System.Collections.Generic;

public partial class ProjectData
{
    /// <summary>
    /// Состояния объектов сцены по их ключам.
    /// </summary>
    /// <remarks>
    /// Словарь, а не список: объект ищет своё состояние по ключу при каждом появлении
    /// на сцене, и таких обращений тем больше, чем крупнее уровень.
    /// </remarks>
    public Dictionary<string, SceneObjectState> SceneObjects { get; set; } = new();

    [MethodHook(MethodHookStage.Cloning)]
    public void CloneSceneObjects(ProjectData clone)
    {
        clone.SceneObjects = new Dictionary<string, SceneObjectState>(SceneObjects.Count);

        // Глубокая копия: иначе правка состояния в клоне меняла бы исходные данные.
        foreach (KeyValuePair<string, SceneObjectState> pair in SceneObjects)
            clone.SceneObjects[pair.Key] = (SceneObjectState)pair.Value.Clone();
    }

    [MethodHook(MethodHookStage.Initializing)]
    public void InitializeSceneObjects()
    {
        SceneObjects = new Dictionary<string, SceneObjectState>();
    }
}
