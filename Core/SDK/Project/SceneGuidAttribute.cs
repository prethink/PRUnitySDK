using UnityEngine;

/// <summary>
/// Строковое поле хранит GUID сцены: в инспекторе оно рисуется полем выбора сцены.
/// </summary>
/// <remarks>
/// Ссылка на <c>SceneAsset</c> есть только в редакторе, а GUID — обычная строка: ассет с таким полем одинаково
/// читается в редакторе и в сборке, и перенос сцены в другую папку его не ломает.
/// </remarks>
public sealed class SceneGuidAttribute : PropertyAttribute
{
}
