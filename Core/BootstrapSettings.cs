using System;
using UnityEngine;

/// <summary>
/// Настройки запуска игры: какая сцена игровая, как собирается SDK, показывать ли диагностику загрузки.
/// </summary>
/// <remarks>
/// Лежат в настройках проекта, а не на объектах загрузочной сцены: сцена у SDK общая, а запуск у каждой
/// игры свой, и менять его приходится, не открывая сцену и не трогая код.
/// <para>
/// Вид экрана загрузки настраивается отдельно — <see cref="LoadingScreenSettings"/>.
/// </para>
/// </remarks>
[Serializable]
[SettingsDescription("Запуск игры: какая сцена открывается после загрузчика, собирать ли SDK порциями, показывать ли отладочные цифры загрузки. Вид экрана загрузки — в Loading Screen.")]
public sealed class BootstrapSettings
{
    [Header("Сцена")]
    [SerializeField, Min(0)]
    [Tooltip("Индекс игровой сцены в Build Settings: на неё загрузчик переходит, когда SDK готов.")]
    private int gameSceneIndex = 1;

    [Header("Сборка SDK")]
    [SerializeField]
    [Tooltip("Собирать SDK порциями, пропуская между ними кадры: экран загрузки при этом виден с самого начала " +
             "и не замирает на всё время сборки. Выключено — SDK собирается одним вызовом. " +
             "Работает, только пока включён экран загрузки.")]
    private bool spreadInitialization = true;

    [SerializeField, Range(0.02f, 1f)]
    [Tooltip("Сколько секунд сборки идёт подряд, прежде чем отдать кадр. Меньше — полоса прогресса плавнее, " +
             "но запуск дольше: каждый отданный кадр стоит своего времени. На слабом телефоне при 0,04 кадры " +
             "добавляли к 3,3 с сборки ещё 2,4 с. Модуль дольше этого всё равно займёт кадр целиком.")]
    private float frameBudgetSeconds = 0.2f;

    [Header("Диагностика загрузки")]
    [SerializeField]
    [Tooltip("Замерять стадии запуска (BootstrapProbe). Работает только в отладочном режиме проекта " +
             "(Project → Release Type = Debug); в релизном диагностики нет вовсе.")]
    private bool probeEnabled = true;

    [SerializeField]
    [Tooltip("Показывать цифры на экране: стадию во время загрузки и итог в углу после неё. " +
             "Выключено — цифры только пишутся в лог.")]
    private bool probeOnScreen = true;

    [SerializeField]
    [Tooltip("Цвет фона загрузочной сцены при включённой диагностике. По нему её отличают от чёрного экрана " +
             "до запуска движка. Под экраном загрузки его не видно.")]
    private Color probeBackgroundColor = new(0.09f, 0.19f, 0.48f, 1f);

    [SerializeField, Min(0f)]
    [Tooltip("Сколько секунд после загрузки игровой сцены держать в углу итог по стадиям.")]
    private float probeSummarySeconds = 20f;

    /// <summary>
    /// Индекс игровой сцены в Build Settings.
    /// </summary>
    public int GameSceneIndex => Mathf.Max(0, gameSceneIndex);

    /// <summary>
    /// SDK собирается порциями с кадрами между ними.
    /// </summary>
    public bool SpreadInitialization => spreadInitialization;

    /// <summary>
    /// Сколько секунд сборки SDK идёт подряд, прежде чем отдать кадр.
    /// </summary>
    public float FrameBudgetSeconds => Mathf.Clamp(frameBudgetSeconds, 0.02f, 1f);

    /// <summary>
    /// Диагностика загрузки включена. В релизном режиме проекта она не работает независимо от этого.
    /// </summary>
    public bool ProbeEnabled => probeEnabled;

    /// <summary>
    /// Диагностика пишет цифры на экран, а не только в лог.
    /// </summary>
    public bool ProbeOnScreen => probeOnScreen;

    /// <summary>
    /// Цвет фона загрузочной сцены при включённой диагностике.
    /// </summary>
    public Color ProbeBackgroundColor => probeBackgroundColor;

    /// <summary>
    /// Сколько секунд после загрузки держать итог диагностики на экране.
    /// </summary>
    public float ProbeSummarySeconds => Mathf.Max(0f, probeSummarySeconds);
}
