using System;
using UnityEngine;

/// <summary>
/// Как закрывается экран на переходе.
/// </summary>
public enum ScreenFadeMode
{
    /// <summary>
    /// Шторка плавно проявляется на весь экран и так же сходит.
    /// </summary>
    Fade,

    /// <summary>
    /// «Диафрагма» из мультфильмов: круг сжимается в точку и потом раскрывается обратно.
    /// </summary>
    Iris,
}

/// <summary>
/// Вид шторки <see cref="ScreenFade"/>: режим, длительность, цвет или картинка, плывущий узор и звуки.
/// </summary>
[Serializable]
public class ScreenFadeSettings
{
    [field: SerializeField, Tooltip("Fade — плавное затемнение всего экрана. Iris — круг сжимается к точке и раскрывается обратно, как в мультфильмах.")]
    public ScreenFadeMode Mode { get; protected set; } = ScreenFadeMode.Fade;

    [field: SerializeField, Min(0f), Tooltip("Сколько секунд закрывается экран; столько же он открывается.")]
    public float Duration { get; protected set; } = 0.6f;

    [field: SerializeField, Tooltip("Цвет шторки. С картинкой — её оттенок (белый — как есть).")]
    public Color Color { get; protected set; } = Color.black;

    [field: SerializeField, Tooltip("Картинка шторки, растянутая на экран: например, градиент из @Resources/Images/Backgrounds. Пусто — сплошной цвет.")]
    public Sprite Image { get; protected set; }

    [field: SerializeField, Tooltip("Узор поверх шторки, повторяется плиткой и плывёт, как фон магазина (@Resources/!Favorites/Images/Patterns). " +
                                    "У текстуры нужен Wrap Mode = Repeat. Пусто — без узора.")]
    public Texture Pattern { get; protected set; }

    [field: SerializeField, Tooltip("Цвет узора; альфа — его заметность.")]
    public Color PatternColor { get; protected set; } = new(1f, 1f, 1f, 0.2f);

    [field: SerializeField, Min(1f), Tooltip("Сторона одной плитки узора в пикселях разметки.")]
    public float PatternTileSize { get; protected set; } = 256f;

    [field: SerializeField, Tooltip("Сдвиг узора в плитках за секунду; знак — направление.")]
    public Vector2 PatternSpeed { get; protected set; } = new(0.05f, 0.05f);

    [field: SerializeField, Tooltip("Iris: круг сжимается к своему игроку. Выключено или игрока нет — к центру экрана.")]
    public bool IrisOnPlayer { get; protected set; } = true;

    [field: SerializeField, Min(0f), Tooltip("Iris: ширина размытой кромки круга в пикселях экрана.")]
    public float IrisSoftness { get; protected set; } = 3f;

    [field: SerializeField, Tooltip("Звук, когда экран закрывается. Пусто — без звука.")]
    public AudioClip CloseSound { get; protected set; }

    [field: SerializeField, Tooltip("Звук, когда экран открывается. Пусто — без звука.")]
    public AudioClip OpenSound { get; protected set; }
}
