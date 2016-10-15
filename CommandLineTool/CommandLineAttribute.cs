using UnityEngine;
using System.Collections;
using System.Reflection;

[System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple=true)]

public class CommandLineAttribute : System.Attribute
{
    private string m_CommandLine = "";
    public string CommandLine
    {
        get { return m_CommandLine; }
    }

    public object[] m_Arguments;
    public string m_HelpText;

    public CommandLineAttribute(string commandLine)
    {
        m_CommandLine = commandLine;
        m_Arguments = null;
        m_HelpText = "";
    }
}

public class CommandLineData
{
    string m_CommandLine = "";
    public string CommandLine
    {
        get { return m_CommandLine; }
    }

    MethodInfo m_Method;
    public MethodInfo Method
    {
        get {  return m_Method; }
    }

    object[] m_Arguments;
    public object[] Arguments
    {
        get {  return m_Arguments; }
    }

    string m_HelpText;
    public string HelpText
    {
        get {  return m_HelpText; }
    }

    public CommandLineData(string commandLine, MethodInfo method, object[] arguments, string helpText)
    {
        m_CommandLine = commandLine;
        m_Method = method;
        m_Arguments = arguments;
        m_HelpText = helpText;
    }

    public void SetNewArguments(object[] newArguments)
    {
        m_Arguments = newArguments;
    }

    public void MergeNewArguments(object[] newArguments)
    {
        for(int i = 0; i < newArguments.Length; i++)
        {
            if(newArguments[i] != null)
            {
                m_Arguments[i] = newArguments[i];
            }
        }
    }
}
