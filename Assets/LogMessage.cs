using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LogMessage : MonoBehaviour
{
    public Color DarkGrey;
    public Color LightGrey;
    public Color HighlightColor;
    public GameObject MessageTextObject;
    public GameObject Sprite;
    public Sprite ErrorSprite;
    public Sprite WarningSprite;
    public Sprite MessageSprite;
    public GameObject MessageCount;
    public LogType type; 

    public string StackTrace;

    [HideInInspector] public int numMessages = 1; 



    public void SetMessageText(string text, Color color)
    {
        MessageTextObject.GetComponent<Text>().text = text;
        MessageTextObject.GetComponent<Text>().color = color;
    }

    public void SetPanelColor(bool isDarkGrey)
    {
        if (isDarkGrey)
            gameObject.GetComponent<Image>().color = DarkGrey;
        else
            gameObject.GetComponent<Image>().color = LightGrey;
    }

    public void SetSpriteImage(LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            Sprite.GetComponent<Image>().sprite = ErrorSprite;
        if (type == LogType.Warning)
            Sprite.GetComponent<Image>().sprite = WarningSprite;
        if (type == LogType.Log)
            Sprite.GetComponent<Image>().sprite = MessageSprite;
    }

    public void OnClick()
    {
        DebugConsole.LogStacktrace(gameObject.GetComponent<LogMessage>());
    }
}
