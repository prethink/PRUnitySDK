using System;
using UnityEngine;

/// <summary>
/// Настройки ввода.
/// </summary>
[Serializable]
public class InputSettings 
{
    [field: SerializeField, Tooltip("Подписывать клавиши буквами раскладки текущего языка: физическая E станет «У» на русском. Выключено — всегда латиница.")]
    public bool TranslateKeyLabels { get; private set; } = true;
}
