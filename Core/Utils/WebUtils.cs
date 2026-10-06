using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public static class WebUtils 
{
    private static Dictionary<string, Texture2D> cachedTexture = new();
    public static IEnumerator LoadTexture(string url, Image image, Action callback = null)
    {
        if (cachedTexture.TryGetValue(url, out var texture))
        {
            callback?.Invoke();
            SetSprite(image, texture);
        }
        else
        {
            using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
            {
                yield return webRequest.SendWebRequest();

                // Любой неуспех, включая ответ сервера с ошибкой (404): текстуры в нём нет,
                // и обращение к ней ниже упало бы.
                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    //if (ProjectBus.IsDebug)
                        //PRLog.WriteWarning(typeof(WebUtils), webRequest.error);
                }
                else
                {
                    DownloadHandlerTexture handlerTexture = webRequest.downloadHandler as DownloadHandlerTexture;

                    if (handlerTexture.isDone)
                    {
                        if (cachedTexture.TryGetValue(url, out texture))
                        {
                            callback?.Invoke();
                            SetSprite(image, texture);
                        }
                        else
                        {
                            callback?.Invoke();
                            SetSprite(image, handlerTexture.texture);
                            cachedTexture[url] = handlerTexture.texture;
                        }
                    }
                }
            }
        }
    }

    private static readonly Dictionary<string, Sprite> cachedSprites = new();

    /// <summary>
    /// Загружает картинку по ссылке и отдаёт её спрайтом.
    /// </summary>
    /// <remarks>
    /// Для того, кому картинку некуда поставить сразу: подписи, которая сама решает, где
    /// и когда её показать. Спрайт создаётся один на ссылку: значок валюты у всех продуктов общий.
    /// При неудаче в ответ уходит <see langword="null"/>.
    /// </remarks>
    public static IEnumerator LoadSprite(string url, Action<Sprite> callback)
    {
        if (string.IsNullOrEmpty(url))
        {
            callback?.Invoke(null);
            yield break;
        }

        if (cachedSprites.TryGetValue(url, out Sprite sprite) && sprite != null)
        {
            callback?.Invoke(sprite);
            yield break;
        }

        if (!cachedTexture.TryGetValue(url, out Texture2D texture))
        {
            using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success
                    && webRequest.downloadHandler is DownloadHandlerTexture handler && handler.isDone)
                {
                    texture = handler.texture;
                    cachedTexture[url] = texture;
                }
            }
        }

        if (texture == null)
        {
            callback?.Invoke(null);
            yield break;
        }

        sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        cachedSprites[url] = sprite;
        callback?.Invoke(sprite);
    }

    /// <summary>
    /// Ставит текстуру в картинку, если та ещё существует.
    /// </summary>
    /// <remarks>
    /// Картинку могли уничтожить, пока шла загрузка: окно закрыли, список перестроили.
    /// </remarks>
    private static void SetSprite(Image image, Texture2D texture)
    {
        if (image == null || texture == null)
            return;

        image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
    }
}
