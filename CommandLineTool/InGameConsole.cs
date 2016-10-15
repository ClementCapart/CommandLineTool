using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System.Text;

public class InGameConsole : MonoBehaviour 
{
    bool m_displayed = false;
    public GameObject m_ConsoleObject = null;
    public UnityEngine.UI.InputField m_InputField = null;
    public UnityEngine.UI.Text m_LogText = null;
    public int m_MaxLogBufferSize = 50;
    public int m_MaxHistorySize = 200;

    private List<string> m_LogTextBuffer = new List<string>();
    private List<string> m_SuccessfulCommandsHistory = new List<string>();
    private int m_CurrentHistoryIndex = -1;

    void OnValidate()
    {
        string[] temp = m_LogTextBuffer.ToArray();
        m_LogTextBuffer.Clear();
        for(int i = 0; i < m_MaxLogBufferSize && i < temp.Length; i++)
        {
            m_LogTextBuffer.Add(temp[i]);
        }
    }

    void Awake()
    {
        m_LogTextBuffer = new List<string>();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.BackQuote))
        {
            ToggleDisplay();
        }
        else if(Input.GetKeyDown(KeyCode.UpArrow))
        {
            if(m_CurrentHistoryIndex < m_SuccessfulCommandsHistory.Count - 1)
            {
                m_CurrentHistoryIndex++;
                m_InputField.text = m_SuccessfulCommandsHistory[m_SuccessfulCommandsHistory.Count - 1 - m_CurrentHistoryIndex];
            }
        }
        else if(Input.GetKeyDown(KeyCode.DownArrow))
        {
            if(m_CurrentHistoryIndex > 0)
            {
                m_CurrentHistoryIndex--;
                m_InputField.text = m_SuccessfulCommandsHistory[m_SuccessfulCommandsHistory.Count - 1 - m_CurrentHistoryIndex];                
            }
            else if(m_CurrentHistoryIndex == 0)
            {
                m_CurrentHistoryIndex--;
                m_InputField.text = "";
            }
        }
    }

    void ToggleDisplay()
    {
        if(!m_displayed)
        {
            Display();
        }
        else
        {
            Hide();
        }
    }

    void Display()
    {
        m_ConsoleObject.SetActive(true);
        m_displayed = true;
        m_InputField.ActivateInputField();
    }

    void Hide()
    {
        m_ConsoleObject.SetActive(false);
        m_displayed = false;
    }

    void AddToLog(string log)
    {
        if(m_LogTextBuffer.Count >= m_MaxLogBufferSize)
        {
            m_LogTextBuffer.RemoveAt(0);
        }
        m_LogTextBuffer.Add(log);
        
        UpdateLog();
    }

    void AddToHistory(string command)
    {
        if(m_SuccessfulCommandsHistory.Count >= m_MaxHistorySize)
        {
            m_SuccessfulCommandsHistory.RemoveAt(0);
        }
        m_SuccessfulCommandsHistory.Add(command);
    }

    bool ExecuteCommand(string command)
    {
        return CommandLineHandler.CallCommandLine(command);
    }

    void UpdateLog()
    {
        StringBuilder builder = new StringBuilder("");
        for(int i = 0; i < m_LogTextBuffer.Count; i++)
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
        // - When end from pressing Enter
        if(Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!string.IsNullOrEmpty(m_InputField.text))
            {
                if (ExecuteCommand(m_InputField.text))
                {
                    AddToHistory(m_InputField.text);
                    AddToLog(m_InputField.text);
                }
                else
                {
                    AddToLog("Command not found: " + m_InputField.text);
                }
                m_CurrentHistoryIndex = -1;
            }
        }
        // - When end from pressing Escape
        else if(Input.GetKeyDown(KeyCode.Escape))
        {

        }
        
        // - Always
        m_InputField.text = "";
        m_InputField.ActivateInputField();
    }
}
