/// <summary>
/// Хитбокс предмета, передающий входящий урон без изменений.
/// </summary>
public class ItemHitBox : EntityHitBoxBase
{
    /// <summary>
    /// Возвращает исходный провайдер урона.
    /// </summary>
    /// <param name="damage">Исходный провайдер урона.</param>
    /// <returns>Тот же экземпляр провайдера.</returns>
    public override IDamageProvider GetHandledDamage(IDamageProvider damage)
    {
        return damage;
    }
}
