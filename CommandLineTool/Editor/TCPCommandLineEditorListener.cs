using UnityEngine;
using System.Collections;
using UnityEditor;

[InitializeOnLoad]
public class TCPCommandLineEditorListener 
{   
    [MenuItem("Tools/TCPCommandLine/Stop Listening")]
    public static void StopListening()
    {
        TCPCommandLineListener.StopListening();
    }

    [MenuItem("Tools/TCPCommandLine/Start Listening")]
    public static void StartListeningDefault()
    {
        TCPCommandLineListener.StartListeningDefault();
    }

    static TCPCommandLineEditorListener()
    {
        TCPCommandLineListener.StartListeningDefault();
    }      
}
