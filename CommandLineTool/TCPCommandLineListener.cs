using System.Collections;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;

public class TCPCommandLineListener 
{
    const int BUFFERS_SIZE = 1024;

    private static Dictionary<TcpListener, List<TcpClient>> s_ListenerInformations = new Dictionary<TcpListener,List<TcpClient>>();

    private static byte[] m_WriteBuffer = new byte[BUFFERS_SIZE];
    private static byte[] m_ReadBuffer = new byte[BUFFERS_SIZE];

    static TCPCommandLineListener()
    {
        StartListeningDefault();
    }

    public static void StartListening(int startingPort, int endingPort)
    {       
        InitializeConnection(startingPort, endingPort);
    }

    public static void StartListeningDefault()
    {
        StartListening(5000, 6000);
    }

    public static void StopListening()
    {
        foreach(KeyValuePair<TcpListener, List<TcpClient>> listenerInfo in s_ListenerInformations)
        {
            for(int i = 0; i < listenerInfo.Value.Count; i++)
            {
                listenerInfo.Value[i].Close();
            }

            listenerInfo.Key.Stop();
        }

        s_ListenerInformations.Clear();
          
        Log("Closed previous command listener instances", "green", UnityEngine.LogType.Log);
    }

    private static void InitializeConnection(int startingPort, int endingPort)
    {
        StopListening();      

        List<IPAddress> validLocalIpAddresses = new List<IPAddress>();

        validLocalIpAddresses.Add(IPAddress.Loopback);

        foreach (NetworkInterface adapter in NetworkInterface.GetAllNetworkInterfaces())
        {
            foreach (UnicastIPAddressInformation x in adapter.GetIPProperties().UnicastAddresses)
            {
                if (x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && x.IsDnsEligible)
                {
                    validLocalIpAddresses.Add(x.Address);
                }
            }
        }

        string logOpenedAddresses = "";

        for(int i = 0; i < validLocalIpAddresses.Count; i++)
        {
            int port = startingPort;
            bool started = false;
            TcpListener listener = null;

            //- Try to find available port in the given range
            while(!started && port <= endingPort)
            {
                try
                {
                    listener = new TcpListener(new IPEndPoint(validLocalIpAddresses[i], port));
                    listener.Start();
                }
                catch
                {
                    port++;
                    listener = null;
                }

                if(listener != null)
                {
                    started = true;
                }
            }

            if(started)
            {                               
                s_ListenerInformations.Add(listener, new List<TcpClient>());
                listener.BeginAcceptTcpClient(new AsyncCallback(WaitForClientMessagesCallback), listener);              
                if(logOpenedAddresses != "")
                    logOpenedAddresses += ",\n";
                logOpenedAddresses += validLocalIpAddresses[i].ToString() + ":" + port;
            }
            else
            {
                Log("Couldn't find available port for " + validLocalIpAddresses[i], "red", UnityEngine.LogType.Log);
            }
        }                
        if(logOpenedAddresses != "")
        {
            Log("Opened command line listener (click to see details)\n\n" + logOpenedAddresses, "green", UnityEngine.LogType.Log);
        }
        else
        {
            Log("Couldn't open command line listener on any address", "red", UnityEngine.LogType.Log);
        }
    }

    private static void WaitForClientMessagesCallback(IAsyncResult result)
    {
        TcpListener listener = (TcpListener)result.AsyncState;

        if(listener != null)
        {
            TcpClient connectedClient = listener.EndAcceptTcpClient(result);                   

            NetworkStream stream = connectedClient.GetStream();
            List<TcpClient> clientInfos = null;
            if(s_ListenerInformations.TryGetValue(listener, out clientInfos))
            {
                clientInfos.Add(connectedClient);
                for (int i = clientInfos.Count - 1; i >= 0; i--)
                {
                    if (!clientInfos[i].Connected)
                    {
                        clientInfos[i].Close();
                        clientInfos.RemoveAt(i);
                    }
                }

                stream.BeginRead(m_ReadBuffer, 0, m_ReadBuffer.Length, new AsyncCallback(ReadMessage), connectedClient);

                listener.BeginAcceptTcpClient(new AsyncCallback(WaitForClientMessagesCallback), listener);
            }
            else
            {
                Log("Message received from unknown listener!", "orange", UnityEngine.LogType.Warning);
            }                        
        }                                                   
    }

    private static void ReadMessage(IAsyncResult result)
    {
        TcpClient client = (TcpClient)result.AsyncState;
        NetworkStream stream = client.GetStream();
        int bytesRead = stream.EndRead(result);        

        string data = null;

        if (bytesRead > 0)
        {
            data += System.Text.Encoding.ASCII.GetString(m_ReadBuffer, 0, bytesRead);
            if(data == "reservedHandshake")
            {
                SendHandshakeReply(client);
            }
            else
            {
                if (!CommandLineHandler.CallCommandLine(data))
                    WriteMessageAsync("Can't find command: " + data, client);
                else
                    WriteMessageAsync("Requested: " + data, client);
            }            
        }
    }

    private static void SendHandshakeReply(TcpClient client)
    {
        string handshakeReply = "reservedHandshakeReply";

#if UNITY_EDITOR
        handshakeReply += " Editor";
#else
        handshakeReply += " Game";
#endif
        handshakeReply += " " + CommandLineHandler.ApplicationName;

        WriteMessageAsync(handshakeReply, client);
    }

    public static void WriteMessageAsync(string message, TcpClient client)
    {
        m_WriteBuffer = System.Text.Encoding.ASCII.GetBytes(message);
        if(client.Connected)
        {
            client.GetStream().BeginWrite(m_WriteBuffer, 0, m_WriteBuffer.Length, new AsyncCallback(WriteMessage), client);
        }
    }

    private static void WriteMessage(IAsyncResult result)
    {
        TcpClient client = (TcpClient)result.AsyncState;
        NetworkStream stream = client.GetStream();
        stream.EndWrite(result);
        m_ReadBuffer = new byte[BUFFERS_SIZE];
        stream.BeginRead(m_ReadBuffer, 0, m_ReadBuffer.Length, new AsyncCallback(ReadMessage), client);
    }

    public static void Log(string logString, string color, LogType type)
    {
        logString = "<color=" + color + ">[TCPCommandLine]</color> " + logString;
        switch (type)
        {
            case LogType.Log:
                Debug.Log(logString);
                break;

            case LogType.Warning:
                Debug.LogWarning(logString);
                break;

            case LogType.Error:
                Debug.LogError(logString);
                break;
        }
    }
}
