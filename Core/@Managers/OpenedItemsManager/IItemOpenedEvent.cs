/// <summary>
/// Получает уведомление, когда игрок впервые открыл предмет.
/// </summary>
/// <remarks>
/// Источник не важен: покупка, награда и находка открывают предмет через один и тот же
/// <see cref="OpenedItemsManager"/>, и событие приходит от него. Откуда предмет взялся,
/// написано в <see cref="ItemStack.Created"/>.
/// <para>
/// Повторная выдача уже открытого (ещё три ключа) сюда не приходит: меняется количество,
/// а не факт открытия. Предмет, закрытый через <see cref="OpenedItemsManager.Close"/>
/// и открытый снова, событие поднимает заново.
/// </para>
/// </remarks>
public interface IItemOpenedEvent : IGlobalSubscriber
{
    /// <summary>
    /// Вызывается после того, как предмет отмечен открытым, до записи сохранения.
    /// </summary>
    /// <param name="item">Запись об открытом предмете.</param>
    void OnItemOpened(ItemStack item);
}
