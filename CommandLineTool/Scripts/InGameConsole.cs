using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Text;

public class InGameConsole : MonoBehaviour
{
    private static InGameConsole s_instance = null;
    public static InGameConsole Instance
    {
        get { return s_instance; }
    }

    private bool m_displayed = false;
    public GameObject m_ConsoleObject = null;
    public UnityEngine.UI.InputField m_InputField = null;
    public UnityEngine.UI.Text m_LogText = null;
    public int m_MaxLogBufferSize = 50;
    public int m_MaxHistorySize = 200;

    private List<string> m_LogTextBuffer = new List<string>();
    private List<string> m_SuccessfulCommandsHistory = new List<string>();
    private int m_CurrentHistoryIndex = -1;

    private GameObject m_PreviouslySelectedObject = null;
    private Transform m_InputFieldCaret = null;

    [Header("Line Suggestions")]
    private string m_InitialValue = "";
    public GameObject m_SuggestionRoot = null;
    public GameObject m_SuggestionsLayoutObject = null;
    public Text[] m_SuggestionTexts = null;
    public Color m_CurrentSelectionColor = Color.white;
    public Color m_UnselectedColor = Color.gray;

    List<CommandLineData> m_Suggestions = new List<CommandLineData>();
    private int m_CurrentSuggestionIndex = -1;

    private bool m_controlEnabled = true;

    void OnValidate()
    {
        string[] temp = m_LogTextBuffer.ToArray();
        m_LogTextBuffer.Clear();
        for (int i = 0; i < m_MaxLogBufferSize && i < temp.Length; i++)
        {
            m_LogTextBuffer.Add(temp[i]);
        }
    }

    void Awake()
    {
#if !PUBLIC_BUILD
        s_instance = this;
        m_LogTextBuffer = new List<string>();
#else
        Destroy(gameObject);
#endif
    }

#if !PUBLIC_BUILD
    void Update()
    {
        // - Hack to set the caret as child of the input field so that resizing the layout doesn't mess everything up - cc
        if (m_InputFieldCaret == null)
        {
            m_InputFieldCaret = m_ConsoleObject.transform.FindChild(m_InputField.name + " Input Caret");
            if (m_InputFieldCaret != null)
            {
                m_InputFieldCaret.SetParent(m_InputField.transform);
            }
        }

        if(m_displayed && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.BackQuote))
        {
            ToggleControl();
        }
        else if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            ToggleDisplay();
        }
        else if(m_controlEnabled)
        {
            if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                if (m_CurrentSuggestionIndex >= 0)
                {
                    m_SuggestionTexts[m_CurrentSuggestionIndex].color = m_UnselectedColor;
                    m_CurrentSuggestionIndex--;
                    if (m_CurrentSuggestionIndex >= 0)
                    {
                        m_SuggestionTexts[m_CurrentSuggestionIndex].color = m_CurrentSelectionColor;
                        if (m_Suggestions[m_CurrentSuggestionIndex].IsFieldCommand || (m_Suggestions[m_CurrentSuggestionIndex].IsMethodCommand && m_Suggestions[m_CurrentSuggestionIndex].Method.GetParameters().Length > 0))
                        {
                            SetInputFieldValue(m_Suggestions[m_CurrentSuggestionIndex].CommandLine + " ");
                        }
                        else
                        {
                            SetInputFieldValue(m_Suggestions[m_CurrentSuggestionIndex].CommandLine);
                        }

                    }
                    else
                    {
                        SetInputFieldValue(m_InitialValue);
                        m_InitialValue = "";
                    }
                }
                else if (m_CurrentHistoryIndex < m_SuccessfulCommandsHistory.Count - 1)
                {
                    m_CurrentHistoryIndex++;
                    SetInputFieldValue(m_SuccessfulCommandsHistory[m_SuccessfulCommandsHistory.Count - 1 - m_CurrentHistoryIndex]);
                    ClearSuggestions();
                }
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                if (m_CurrentHistoryIndex > 0)
                {
                    m_CurrentHistoryIndex--;
                    SetInputFieldValue(m_SuccessfulCommandsHistory[m_SuccessfulCommandsHistory.Count - 1 - m_CurrentHistoryIndex]);
                    ClearSuggestions();
                }
                else if (m_CurrentHistoryIndex == 0)
                {
                    m_CurrentHistoryIndex--;
                    SetInputFieldValue("");
                }
                else if (m_CurrentSuggestionIndex < m_SuggestionTexts.Length - 1 && m_CurrentSuggestionIndex < m_Suggestions.Count - 1)
                {
                    if (m_CurrentSuggestionIndex >= 0)
                    {
                        m_SuggestionTexts[m_CurrentSuggestionIndex].color = m_UnselectedColor;
                    }
                    else
                    {
                        m_InitialValue = m_InputField.text;
                    }

                    m_CurrentSuggestionIndex++;

                    m_SuggestionTexts[m_CurrentSuggestionIndex].color = m_CurrentSelectionColor;
                    if (m_Suggestions[m_CurrentSuggestionIndex].IsFieldCommand || (m_Suggestions[m_CurrentSuggestionIndex].IsMethodCommand && m_Suggestions[m_CurrentSuggestionIndex].Method.GetParameters().Length > 0))
                    {
                        SetInputFieldValue(m_Suggestions[m_CurrentSuggestionIndex].CommandLine + " ");
                    }
                    else
                    {
                        SetInputFieldValue(m_Suggestions[m_CurrentSuggestionIndex].CommandLine);
                    }
                }
            }
        }
    }
#endif
    void SetInputFieldValue(string value)
    {
        m_InputField.text = value;
        m_InputField.caretPosition = m_InputField.text.Length;
    }

    void ToggleDisplay()
    {
        if (!m_displayed)
        {
            Display();
        }
        else
        {
            Hide();
        }
    }

    void ToggleControl()
    {
        if(!m_controlEnabled)
        {
            m_controlEnabled = true;
            m_InputField.enabled = true;

            m_PreviouslySelectedObject = EventSystem.current.currentSelectedGameObject;
            m_InputField.ActivateInputField();
        }
        else
        {
            m_controlEnabled = false;
            m_InputField.enabled = false;

            EventSystem.current.SetSelectedGameObject(m_PreviouslySelectedObject);
            m_PreviouslySelectedObject = null;

            m_InputField.textComponent.text = "Control disabled, press Shift + ` to enable";
                
        }
    }

    void Display()
    {
        m_ConsoleObject.SetActive(true);
        m_displayed = true;

        if (m_controlEnabled)
        {
            m_PreviouslySelectedObject = EventSystem.current.currentSelectedGameObject;
            m_InputField.ActivateInputField();
        }           
    }

    void Hide()
    {
        m_ConsoleObject.SetActive(false);
        EventSystem.current.SetSelectedGameObject(m_PreviouslySelectedObject);
        m_PreviouslySelectedObject = null;
        m_displayed = false;
    }

    public void AddToLog(string log)
    {
        if (m_LogTextBuffer.Count >= m_MaxLogBufferSize)
        {
            m_LogTextBuffer.RemoveAt(0);
        }
        m_LogTextBuffer.Add(log);

        UpdateLog();
    }

    void AddToHistory(string command)
    {
        if (m_SuccessfulCommandsHistory.Count == 0 || m_SuccessfulCommandsHistory[m_SuccessfulCommandsHistory.Count - 1] != command)
        {
            if (m_SuccessfulCommandsHistory.Count >= m_MaxHistorySize)
            {
                m_SuccessfulCommandsHistory.RemoveAt(0);
            }
            m_SuccessfulCommandsHistory.Add(command);
        }
    }

    bool ExecuteCommand(string command)
    {
        return CommandLineHandler.CallCommandLine(command);
    }

    void UpdateLog()
    {
        StringBuilder builder = new StringBuilder("");
        for (int i = 0; i < m_LogTextBuffer.Count; i++)
        {
            if (i == m_LogTextBuffer.Count - 1)
            {
                builder.Append(m_LogTextBuffer[i]);
            }
            else
            {
                builder.AppendLine(m_LogTextBuffer[i]);
            }
        }

        m_LogText.text = builder.ToString();
    }

    public void OnEndEdit(string value)
    {
        string command = value != "" ? value : m_InputField.text;
        // - When end from pressing Enter
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!string.IsNullOrEmpty(command))
            {
                if (ExecuteCommand(command))
                {
                    AddToHistory(command);
                    AddToLog(command);
                }
                else
                {
                    AddToLog("Command not found: " + command);
                }
            }
        }
        // - When end from pressing Escape
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
        }

        m_CurrentHistoryIndex = -1;
        m_CurrentSuggestionIndex = -1;

        // - Always
        ClearSuggestions();
        SetInputFieldValue("");
        m_InputField.ActivateInputField();
    }

    public void LookForSuggestions(string value)
    {
        if (m_CurrentSuggestionIndex == -1)
        {
            string text = m_InputField.text;

            m_Suggestions.Clear();
            string[] results = text.Split(' ');

            if (results[0] != "" && results.Length == 1)
            {
                foreach (KeyValuePair<string, CommandLineData> kvp in CommandLineHandler.m_CommandLines)
                {
                    if (kvp.Key.StartsWith(results[0]))
                    {
                        m_Suggestions.Add(kvp.Value);
                    }
                }
            }

            if (m_Suggestions.Count == 0 && results[0].Length >= 3)
            {
                foreach (KeyValuePair<string, CommandLineData> kvp in CommandLineHandler.m_CommandLines)
                {
                    if (kvp.Key.Contains(results[0]))
                    {
                        m_Suggestions.Add(kvp.Value);
                    }
                }

                for (int i = 1; i < results.Length; i++)
                {
                    for (int j = m_Suggestions.Count - 1; j >= 0; j--)
                    {
                        if (!m_Suggestions[j].CommandLine.Contains(results[i]))
                        {
                            m_Suggestions.RemoveAt(j);
                        }
                    }
                }
            }

            UpdateSuggestionsField();
        }
    }

    private void UpdateSuggestionsField()
    {
        if (m_Suggestions.Count > 0)
        {
            m_SuggestionRoot.SetActive(true);
            RectTransform rect = m_SuggestionsLayoutObject.GetComponent<RectTransform>();
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, m_Suggestions.Count * 17);
            m_CurrentSuggestionIndex = -1;

            for (int i = 0; i < m_SuggestionTexts.Length; i++)
            {
                if (i < m_Suggestions.Count)
                {
                    m_SuggestionTexts[i].gameObject.SetActive(true);
                    m_SuggestionTexts[i].text = m_Suggestions[i].CommandLine + " //" + m_Suggestions[i].HelpText;
                    m_SuggestionTexts[i].gameObject.name = m_Suggestions[i].CommandLine;
                    m_SuggestionTexts[i].color = m_UnselectedColor;
                }
                else
                {
                    m_SuggestionTexts[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {
            m_SuggestionRoot.SetActive(false);
        }
    }

    public void ClearSuggestions()
    {
        m_Suggestions.Clear();
        UpdateSuggestionsField();
    }

    [CommandLine("clear", "Clear the console log")]
    static void ClearLog()
    {
        if (Instance != null)
        {
            Instance.m_LogTextBuffer.Clear();
            Instance.UpdateLog();
        }
    }

    [CommandLine("console_size", "Changes the height of the console", new object[] { 400.0f })]
    static void ChangeConsoleHeight(float height)
    {
        height = Mathf.Clamp(height, 17, 720);
        RectTransform rect = Instance.m_ConsoleObject.GetComponent<RectTransform>();
        if(rect != null)
        {
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }
    }

    [CommandLine("console_opacity", "Changes the opacity of the console (between 0.0 and 1.0)", new object[] { 0.5f })]
    static void ChangeConsoleOpacity(float opacity)
    {
        opacity = Mathf.Clamp01(opacity);
        Image background = Instance.m_ConsoleObject.GetComponent<Image>();
        if(background != null)
        {
            background.color = new Color(background.color.r, background.color.g, background.color.b, opacity);
        }

        Image suggestion = Instance.m_SuggestionsLayoutObject.GetComponent<Image>();
        if (suggestion != null)
        {
            suggestion.color = new Color(suggestion.color.r, suggestion.color.g, suggestion.color.b, opacity);
        }
    }
}

public static class Console
{
    public static void Log(string line, string color = "", UnityEngine.Object context = null, bool logToUnity = true)
    {
        if (logToUnity)
        {
            Debug.Log(line, context);
        }

        if (InGameConsole.Instance != null)
        {
            InGameConsole.Instance.AddToLog(color != "" ? ("<color=" + color + ">" + line + "</color>") : line);
        }
    }

    public static void LogWarning(string line, UnityEngine.Object context = null, bool logToUnity = true)
    {
        if (logToUnity)
        {
            Debug.LogWarning(line, context);
        }

        if (InGameConsole.Instance != null)
        {
            InGameConsole.Instance.AddToLog(line);
        }
    }

    public static void LogError(string line, UnityEngine.Object context = null, bool logToUnity = true)
    {
        if (logToUnity)
        {
            Debug.LogError(line, context);
        }

        if (InGameConsole.Instance != null)
        {
            InGameConsole.Instance.AddToLog(line);
        }
    }
}
