using System;
using UnityEngine;

[Serializable]
public partial class GlobalGameSettings
{
    /// <summary>
    /// Базовые настройки игры
    /// </summary>
    [field: Header("Базовые игровые настройки")]
    [field: SerializeField]
    public BaseGameSettings BaseGameSettings { get; private set; }

    /// <summary>
    /// Базовые настройки управления.
    /// </summary>
    [field: Header("Базовые игровые настройки")]
    [field: SerializeField]
    public DefaultControlSettings DefaultControlSettings { get; private set; }
}


[Serializable]
public partial class BaseGameSettings
{
    [Header("Глобальные настройки проекта")]
    [Tooltip("Скорость игрового мира")]
    [SerializeField] private float baseGameSpeed = 1f;

    [Tooltip("Базовый урон")]
    [SerializeField] private int baseDamage = 2;

    #region public

    public float BaseGameSpeed => baseGameSpeed;

    public int BaseDamage => baseDamage;

    #endregion
}

