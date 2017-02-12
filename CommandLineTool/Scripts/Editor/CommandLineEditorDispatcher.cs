using UnityEngine;
using System.Collections;
using UnityEditor;

[InitializeOnLoad]
public static class CommandLineEditorDispatcher
{
    static CommandLineEditorDispatcher()
    {
        CommandLineHandler.Initialize();
        EditorApplication.update += Update;
    }

    public static void Update()
    {
        CommandLineHandler.ApplicationName = Application.productName;

        lock (CommandLineHandler.m_MainThreadCommandLines)
        {
            while (CommandLineHandler.m_MainThreadCommandLines.Count > 0)
            {
                CommandLineData commandLine = CommandLineHandler.m_MainThreadCommandLines.Dequeue();
                CommandLineHandler.CallMethod(commandLine);
            }
        }
    }
}
