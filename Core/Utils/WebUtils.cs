using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public static class WebUtils 
{
    private static Dictionary<string, Texture2D> cachedTexture = new();

    /// <summary>
    /// Загружает картинку по ссылке и ставит её в <paramref name="image"/>.
    /// </summary>
    /// <remarks>
    /// Спрайт берётся общий, из того же кеша, что у <see cref="LoadSprite"/>: одна ссылка — один
    /// спрайт. Раньше он создавался заново при каждом вызове, даже для уже скачанной текстуры,
    /// и на каждую перерисовку карточки в памяти оставался ещё один.
    /// <para>
    /// При неудаче картинка не трогается и <paramref name="callback"/> не вызывается.
    /// </para>
    /// </remarks>
    /// <param name="url">Ссылка на картинку.</param>
    /// <param name="image">Куда поставить. Картинку могли уничтожить, пока шла загрузка: тогда ставить некуда.</param>
    /// <param name="callback">
    /// Вызывается, когда картинка загружена и уже стоит на месте: к этому моменту
    /// <c>image.sprite</c> можно читать.
    /// </param>
    public static IEnumerator LoadTexture(string url, Image image, Action callback = null)
    {
        yield return LoadSprite(url, sprite =>
        {
            if (sprite == null)
                return;

            if (image != null)
                image.sprite = sprite;

            callback?.Invoke();
        });
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
}
