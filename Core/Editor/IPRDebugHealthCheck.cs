using System.Collections.Generic;

/// <summary>
/// Расширение вкладки Problems окна PRUnitySDK Debug.
/// Конкретная реализация должна иметь конструктор без параметров.
/// </summary>
public interface IPRDebugHealthCheck
{
    /// <summary>
    /// Возвращает актуальные проблемы текущего runtime-состояния.
    /// </summary>
    IEnumerable<PRDebugProblem> Check();
}
