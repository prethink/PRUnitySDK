#if PRSDK_TESTS
using UnityEngine;

/// <summary>
/// Слушатель детализации для тестов: считает вызовы и запоминает, чья детализация звала и с каким уровнем.
/// </summary>
/// <remarks>
/// Лежит вне <c>Editor</c>: компонент из папки <c>Editor</c> на объект не вешается.
/// </remarks>
public class EntityLodTestListener : MonoBehaviour, IEntityLodListener
{
    public int Calls;
    public EntityLod LastLod;
    public EntityLodLevel LastLevel;

    public void OnEntityLodChanged(EntityLod lod)
    {
        Calls++;
        LastLod = lod;
        LastLevel = lod.Level;
    }
}
#endif
