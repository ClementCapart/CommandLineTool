using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Net.Sockets;
using System.Reflection;
using System.Collections.Generic;


public class PrintLineTest : MonoBehaviour 
{
    [CommandLine("debug_log_no_params")]
    static void PrintLine()
    {
        Debug.Log("Test test test");
    }   

    [CommandLine("debug_log_default_params")]
    static void PrintLine(string defaultArgs = "default")
    {
        Debug.Log("Test Default Args");
    }   

    [CommandLine("debug_log", m_HelpText="Display text in Unity's console")]
    [CommandLine("debug_log_embedded_params", m_Arguments= new object[] { "Test Embedded Parameters" })]
    [CommandLine("debug_log_second_string", m_Arguments = new object[] { null, "default second" })]
    static void PrintLineParams(string line, string secondString = "test second")
    {
        Debug.Log(line + " : " + secondString);
    }

    [CommandLine("test_camera")]
    static void MoveCamera()
    {
        Camera.main.transform.position += new Vector3(1.0f, 0.0f, 0.0f);
    }


}
