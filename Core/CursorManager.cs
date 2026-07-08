using UnityEngine;

/// <summary>
/// Менеджер управления курсором.
/// </summary>
public static class CursorManager 
{
    /// <summary>
    /// Спрятать курсор.
    /// </summary>
    public static void HideCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Активировать курсор.
    /// </summary>
    public static void ActiveCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void SetCursor(Sprite cursorSprite)
    {
        Cursor.SetCursor(cursorSprite.texture, Vector2.zero, CursorMode.Auto);
    }
}
