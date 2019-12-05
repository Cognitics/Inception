using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LocationPinImage : MonoBehaviour
{
    private Texture2D texture;

    public void OnClick()
    {
        var image = gameObject.GetComponent<Image>();
        StartCoroutine(GetImage("https://upload.wikimedia.org/wikipedia/commons/2/2b/Ruby-LowCompression-Tiny.jpg"));
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(.5f,.5f));
        image.sprite = sprite;
    }

    IEnumerator GetImage(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();
        if (request.isNetworkError || request.isHttpError)
            Debug.Log(request.error);
        else
        {
            texture = DownloadHandlerTexture.GetContent(request);
        }
            
    }
}

