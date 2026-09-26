using System;
using UnityEngine;

/// <summary>
/// Настройки здоровья проекта.
/// </summary>
[Serializable]
[SettingsDescription("Здоровье сущностей: что считать попаданием, о котором сообщает HealthComponent.OnHit.")]
public class HealthSettings
{
    /// <summary>
    /// Засчитанный удар без урона тоже считается попаданием.
    /// </summary>
    /// <remarks>
    /// Такой удар бывает, когда правило урона умножает урон на 0: удар прошёл, здоровье
    /// цело. Включено — по нему играют эффекты и звук попадания, выключено — он выглядит
    /// как промах.
    /// </remarks>
    [field: SerializeField]
    [field: Tooltip("Удар с нулевым уроном — тоже попадание: эффекты, цифра «0» и звук играют. " +
                    "Выключено — такой удар выглядит как промах.")]
    public bool ZeroDamageIsHit { get; private set; }
}
