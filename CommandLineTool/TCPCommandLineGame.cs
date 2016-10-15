using UnityEngine;
using System.Collections;
using System.Reflection;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.Threading;

public class TCPCommandLineGame : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);        
#if !UNITY_EDITOR
        TCPCommandLineListener.StartListeningDefault();
#endif
    }
}
