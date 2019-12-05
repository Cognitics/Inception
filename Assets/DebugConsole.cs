using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugConsole : MonoBehaviour
{
    public GameObject Panel;
    public Font font;
    public GameObject LogPanel;
    public GameObject StacktraceMessage;
    public GameObject Content;
    public GameObject MessagePrefab;
    public Text NumErrors, NumWarnings, NumMessages;
    public GameObject ScrollView;

    private bool isDarkGrey;
    private bool showErrors, showMessages, showWarnings;
    private int errors, warnings, messages = 0;
    private static Text StacktraceText;
    private List<LogMessage> LogMessages;
    private int i = 0;
    struct Log
    {
        public string message;
        public string stackTrace;
        public LogType type;
        public int count;
    }

    public KeyCode toggleKey = KeyCode.BackQuote;

    List<Log> logs = new List<Log>();
    Vector2 scrollPosition;
    bool show;
    bool collapse;

    static readonly Dictionary<LogType, Color> logTypeColors = new Dictionary<LogType, Color>()
    {
        { LogType.Assert, Color.white },
        { LogType.Error, Color.red },
        { LogType.Exception, Color.red },
        { LogType.Log, Color.white },
        { LogType.Warning, Color.yellow },
    };

    const int margin = 20;

    private void Start()
    {
        showErrors = true;
        showWarnings = true;
        showMessages = true;
        isDarkGrey = true;
        StacktraceText = StacktraceMessage.GetComponent<Text>();
        LogMessages = new List<LogMessage>();
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            Panel.SetActive(!Panel.activeSelf);
    }


    void HandleLog(string message, string stackTrace, LogType type)
    {
        Log l = new Log()
        {
            message = message,
            stackTrace = stackTrace,
            type = type,
            count = 1,
        };
        
        foreach(LogMessage log in LogMessages)
        {
            if(log.MessageTextObject.GetComponent<Text>().text == message)
            {
                ++log.numMessages;
                l.count = log.numMessages;
                log.MessageCount.GetComponent<Text>().text = log.numMessages.ToString();
                return;
            }
        }


        logs.Add(l);
        if(type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
        {
            ++errors;
            NumErrors.text = errors.ToString();
        }
        if (type == LogType.Log)
        {
            ++messages;
            NumMessages.text = messages.ToString();
        }
        if (type == LogType.Warning)
        {
            ++warnings;
            NumWarnings.text = warnings.ToString();
        }


        if (l.type == LogType.Error || l.type == LogType.Exception || l.type == LogType.Assert)
            InstantiateMessagePrefab(l, showErrors);
        if (l.type == LogType.Log)
            InstantiateMessagePrefab(l, showMessages);
        if (l.type == LogType.Warning)
            InstantiateMessagePrefab(l, showWarnings);
    }

    public void ToggleLogs(GameObject button)
    {
        var image = button.transform.parent.GetComponent<Image>();
        if (image.color == new Color(.39216f, .39216f, .39216f, 1f))
            image.color = new Color(.18f, .18f, .18f, 1);
        else
            image.color = new Color(.39216f, .39216f, .39216f, 1);

        if (button.name == "Error Button")
            showErrors = !showErrors;
        if (button.name == "Message Button")
            showMessages = !showMessages;
        if (button.name == "Warning Button")
            showWarnings = !showWarnings;
        ReorganizeLogs();        
    }

    public void ClearLog()
    {
        foreach(Transform t in Content.transform)
            Destroy(t.gameObject);
        logs.Clear();
        LogMessages.Clear();
        NumErrors.text = NumMessages.text = NumWarnings.text = "0";
        errors = messages = warnings = 0;
        StacktraceText.text = "";
    }

    void ReorganizeLogs()
    {
        if((messages + warnings + errors) % 2 == 1)
            isDarkGrey = !isDarkGrey;
        foreach (Transform t in Content.transform)
            t.gameObject.SetActive(false);
        foreach(LogMessage l in LogMessages)
        {
            if (l.type == LogType.Error || l.type == LogType.Exception || l.type == LogType.Assert)
                if (showErrors)
                {
                    l.gameObject.SetActive(true);
                    l.SetPanelColor(isDarkGrey);
                    isDarkGrey = !isDarkGrey;
                }
            if (l.type == LogType.Log)
                if (showMessages)
                {
                    l.gameObject.SetActive(true);
                    l.SetPanelColor(isDarkGrey);
                    isDarkGrey = !isDarkGrey;
                }
            if (l.type == LogType.Warning)
                if (showWarnings)
                {
                    l.gameObject.SetActive(true);
                    l.SetPanelColor(isDarkGrey);
                    isDarkGrey = !isDarkGrey;
                }
        }
    }

    void InstantiateMessagePrefab(Log log, bool active)
    {
        bool moveToBottom = false;
        var go = Instantiate(MessagePrefab);
        var rt = Content.GetComponent<RectTransform>();
        if ((rt.sizeDelta.y - rt.localPosition.y) == 200)
            moveToBottom = true;
        var script = go.GetComponent<LogMessage>();
        LogMessages.Add(script);
        script.SetMessageText(log.message, logTypeColors[log.type]);
        script.SetPanelColor(isDarkGrey);
        script.StackTrace = log.stackTrace;
        script.type = log.type;
        script.MessageCount.GetComponent<Text>().text = log.count.ToString();
        isDarkGrey = !isDarkGrey;
        script.SetSpriteImage(log.type);
        go.transform.SetParent(Content.transform);
        go.SetActive(active);
        if(moveToBottom)
        {
            Canvas.ForceUpdateCanvases();
            ScrollView.GetComponent<ScrollRect>().verticalNormalizedPosition = 0f;
        }
    }

    public static void LogStacktrace(LogMessage logMessage)
    {
        StacktraceText.text = logMessage.MessageTextObject.GetComponent<Text>().text;
        if(!(logMessage.GetComponent<LogMessage>().StackTrace == ""))
            StacktraceText.text += "\n" + logMessage.StackTrace;
    }
}