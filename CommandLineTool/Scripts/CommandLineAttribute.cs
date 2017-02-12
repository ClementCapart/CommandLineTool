using UnityEngine;
using System.Collections;
using System.Reflection;

[System.AttributeUsage(System.AttributeTargets.Method | System.AttributeTargets.Field, AllowMultiple = true)]

public class CommandLineAttribute : System.Attribute
{
    private string m_CommandLine = "";
    public string CommandLine
    {
        get { return m_CommandLine; }
    }

    public string m_HelpText;
    public object[] m_Arguments;

    public CommandLineAttribute(string commandLine, string helpText = "", object[] arguments = null)
    {
        m_CommandLine = commandLine;
        m_HelpText = helpText;
        m_Arguments = arguments;
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
        get { return m_Method; }
    }

    FieldInfo m_Field;
    public FieldInfo Field
    {
        get { return m_Field; }
    }

    public bool IsMethodCommand
    {
        get { return m_Method != null; }
    }

    public bool IsFieldCommand
    {
        get { return m_Field != null; }
    }

    object[] m_Arguments;
    public object[] Arguments
    {
        get { return m_Arguments; }
    }

    string m_HelpText;
    public string HelpText
    {
        get { return m_HelpText; }
    }

    public CommandLineData(string commandLine, MethodInfo method, object[] arguments, string helpText)
    {
        m_CommandLine = commandLine;
        m_Method = method;
        m_Arguments = arguments;
        m_HelpText = helpText;
    }

    public CommandLineData(string commandLine, FieldInfo field, object value, string helpText)
    {
        m_CommandLine = commandLine;
        m_Field = field;
        m_Arguments = new object[1] { value };
        m_HelpText = helpText;
    }

    public void SetNewFieldValue(object value)
    {
        m_Arguments = new object[1] { value };
    }

    public void SetNewArguments(object[] newArguments)
    {
        m_Arguments = newArguments;
    }

    public void MergeNewArguments(object[] newArguments)
    {
        for (int i = 0; i < newArguments.Length; i++)
        {
            if (newArguments[i] != null)
            {
                m_Arguments[i] = newArguments[i];
            }
        }
    }

    public void MergeFieldValue(object value)
    {
        if (m_Arguments != null && m_Arguments.Length > 0)
        {
            if (value != null)
            {
                m_Arguments[0] = value;
            }
        }
    }
}
