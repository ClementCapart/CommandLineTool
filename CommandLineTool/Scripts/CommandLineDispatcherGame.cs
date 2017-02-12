using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CommandLineDispatcherGame : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
#if !UNITY_EDITOR
		CommandLineHandler.Initialize();
#endif
    }

#if !UNITY_EDITOR
private void Update()
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
#endif
}
