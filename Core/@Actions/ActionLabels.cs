using System.Collections.Generic;

/// <summary>
/// Причины отказа действий на трёх языках.
/// </summary>
public static class ActionLabels
{
    public static readonly ILocalizationProvider GameNotReady =
        new LocalizationProvider("action_game_not_ready", "The game is not ready yet.", "Игра ещё не готова.", "Oyun henüz hazır değil.");

    public static readonly ILocalizationProvider InvalidUrl =
        new LocalizationProvider("action_invalid_url", "The link is invalid.", "Некорректная ссылка.", "Bağlantı geçersiz.");

    public static readonly ILocalizationProvider MissingPropertyName =
        new LocalizationProvider("action_missing_property", "No value to change has been selected.", "Не выбрано значение для изменения.", "Değiştirilecek değer seçilmedi.");

    public static readonly ILocalizationProvider InvalidDateTime =
        new LocalizationProvider("action_invalid_date", "The date or time is invalid.", "Некорректная дата или время.", "Tarih veya saat geçersiz.");

    public static readonly ILocalizationProvider InvalidAmount =
        new LocalizationProvider("action_invalid_amount", "The amount must be greater than zero.", "Количество должно быть больше нуля.", "Miktar sıfırdan büyük olmalıdır.");

    public static readonly ILocalizationProvider MissingResource =
        new LocalizationProvider("action_missing_resource", "No resource has been selected.", "Не выбран ресурс.", "Kaynak seçilmedi.");

    public static readonly ILocalizationProvider InvalidResource =
        new LocalizationProvider("action_invalid_resource", "The selected resource is invalid.", "Выбранный ресурс недоступен.", "Seçilen kaynak geçersiz.");

    public static readonly ILocalizationProvider MissingAction =
        new LocalizationProvider("action_missing_action", "No action has been selected.", "Не выбрано действие.", "Eylem seçilmedi.");

    public static readonly ILocalizationProvider EmptySequence =
        new LocalizationProvider("action_empty_sequence", "The action list is empty.", "Список действий пуст.", "Eylem listesi boş.");

    public static readonly ILocalizationProvider Unavailable =
        new LocalizationProvider("action_unavailable", "This action is currently unavailable.", "Действие сейчас недоступно.", "Bu eylem şu anda kullanılamıyor.");

    public static readonly ILocalizationProvider InvalidDuration =
        new LocalizationProvider("action_invalid_duration", "The duration must be greater than zero.", "Длительность должна быть больше нуля.", "Süre sıfırdan büyük olmalıdır.");

    public static readonly ILocalizationProvider ServiceNotReady =
        new LocalizationProvider("action_service_not_ready", "The action is not ready yet.", "Действие ещё не готово.", "Eylem henüz hazır değil.");
}
