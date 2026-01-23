/// <summary>
/// Интерфейс для объектов, которые могут предоставлять информацию об уроне.
/// Используется как источник данных для систем нанесения урона.
/// </summary>
public interface IDamageProvider
{
    DamageData GetDamageData();
}
