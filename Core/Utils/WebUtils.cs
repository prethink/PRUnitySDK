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
            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
        }
        else
        {
            using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.ConnectionError ||
                    webRequest.result == UnityWebRequest.Result.DataProcessingError)
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
                            image.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0));
                        }
                        else
                        {
                            callback?.Invoke();
                            image.sprite = Sprite.Create(handlerTexture.texture, new Rect(0, 0, handlerTexture.texture.width, handlerTexture.texture.height), new Vector2(0, 0));
                            cachedTexture[url] = handlerTexture.texture;
                        }
                    }
                }
            }
        }
    }
}
