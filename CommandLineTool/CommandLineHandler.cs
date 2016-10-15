using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Globalization;

public static class CommandLineHandler 
{
    public static Dictionary<string, CommandLineData> m_CommandLines = null;
    public static Queue<CommandLineData> m_MainThreadCommandLines = new Queue<CommandLineData>();
    
    private static string s_applicationName = "";
    
    public static string ApplicationName
    {
        get { return s_applicationName; }
        set
        {
            if (s_applicationName != value)
            {
                lock (s_applicationName)
                {
                    s_applicationName = value;
                }
            }
        }
    }

    public static void Initialize()
    {
        GatherCommandLineMethods();
    }

    private static void GatherCommandLineMethods()
    {
        m_CommandLines = new Dictionary<string, CommandLineData>();

        //Assembly EditorAssembly = typeof(TCPCommandLineStarter).Assembly;
        Assembly GameAssembly = typeof(CommandLineHandler).Assembly;

        //GatherMethodsFromAssembly(EditorAssembly);
        GatherMethodsFromAssembly(GameAssembly);
    }

    private static void GatherMethodsFromAssembly(Assembly assembly)
    {
        Type[] assemblyTypes = assembly.GetTypes();

        for (int i = 0; i < assemblyTypes.Length; i++)
        {
            MethodInfo[] methods = assemblyTypes[i].GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            for (int j = 0; j < methods.Length; j++)
            {
                object[] customAttributes = methods[j].GetCustomAttributes(typeof(CommandLineAttribute), true);
                if (customAttributes.Length > 0)
                {
                    for (int attributeIndex = 0; attributeIndex < customAttributes.Length; attributeIndex++)
                    {
                        CommandLineAttribute line = (CommandLineAttribute)customAttributes[attributeIndex];
                        if (line != null)
                        {
                            ParameterInfo[] parameters = methods[j].GetParameters();
                            object[] args = new object[parameters.Length];
                            for (int parameterIndex = 0; parameterIndex < parameters.Length; parameterIndex++)
                            {
                                if (line.m_Arguments != null && parameterIndex < line.m_Arguments.Length)
                                {
                                    if (line.m_Arguments[parameterIndex] == null)
                                    {
                                        args[parameterIndex] = parameters[parameterIndex].DefaultValue;
                                    }
                                    else
                                    {
                                        args[parameterIndex] = line.m_Arguments[parameterIndex];
                                    }
                                }
                                else
                                {
                                    args[parameterIndex] = parameters[parameterIndex].DefaultValue;
                                }
                            }

                            CommandLineData newLine = new CommandLineData(line.CommandLine, methods[j], args, line.m_HelpText);
                            m_CommandLines.Add(line.CommandLine, newLine);
                        }
                    }
                }
            }
        }
    }

    public static bool CallCommandLine(string command)
    {
        string[] commandAndArgs = command.Split(' ');

        if (!m_CommandLines.ContainsKey(commandAndArgs[0]))
            return false;

        if (commandAndArgs.Length > 1)
        {
            CommandLineData commandData = ParseArguments(commandAndArgs, m_CommandLines[commandAndArgs[0]]);
            return DispatchCommandLineToMainThread(commandData);
        }

        return DispatchCommandLineToMainThread(m_CommandLines[command]);
    }

    public static bool CallMethod(CommandLineData command)
    {
        try
        {
            command.Method.Invoke(null, command.Arguments);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool DispatchCommandLineToMainThread(CommandLineData command)
    {
        m_MainThreadCommandLines.Enqueue(command);
        return true;
    }

    private static CommandLineData ParseArguments(string[] commandAndArgs, CommandLineData data)
    {
        CommandLineData lineData = new CommandLineData(data.CommandLine, data.Method, data.Arguments, data.HelpText);

        ParameterInfo[] parameters = data.Method.GetParameters();
        object[] newArguments = new object[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            Type paramType = parameters[i].ParameterType;

            if (commandAndArgs.Length <= i + 1)
                break;

            if (paramType == typeof(int))
            {
                int arg = 0;
                if (!int.TryParse(commandAndArgs[i + 1], out arg))
                {
                    //Error message ?
                }
                newArguments[i] = arg;
            }
            else if (paramType == typeof(float))
            {
                float arg = 0;
                if (!float.TryParse(commandAndArgs[i + 1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out arg))
                {
                    //Error message ?
                }
                newArguments[i] = arg;
            }
            else if (paramType == typeof(string))
            {
                string arg = commandAndArgs[i + 1];
                if (arg.StartsWith("\"") && arg.EndsWith("\""))
                {
                    arg = arg.Remove(0, 1);
                    arg = arg.Remove(arg.Length - 1, 1);
                }

                newArguments[i] = arg;
            }
            else if (paramType == typeof(bool))
            {
                bool arg = false;
                if (!bool.TryParse(commandAndArgs[i + 1], out arg))
                {
                    int intBool = 0;
                    if (!int.TryParse(commandAndArgs[i + 1], out intBool))
                    {
                        //Error Message ?
                    }
                    else
                    {
                        arg = intBool > 0;
                    }
                }

                newArguments[i] = arg;
            }
            else if (paramType.IsEnum)
            {
                object arg = null;

                try
                {
                    arg = Enum.Parse(paramType, commandAndArgs[i + 1], true);
                }
                catch (Exception e)
                {
                    Debug.Log(e.Message);
                }

                newArguments[i] = arg;
            }
        }

        lineData.MergeNewArguments(newArguments);

        return lineData;
    }

    [CommandLineAttribute("help", m_HelpText = "Print the help text of any command line passed as parameter")]
    private static void HelpText(string commandLine)
    {
        if (m_CommandLines.ContainsKey(commandLine))
        {
            Debug.Log(m_CommandLines[commandLine].HelpText);
        }
    }

    // - Not entirely implemented yet
    // ------------------------------
    [CommandLineAttribute("attach_unity_output", m_HelpText = "Attach or detach Unity's console output to the tool")]
    private static void CatchDebugOutput(bool enable = true)
    {
        if (enable)
        {
            Application.logMessageReceived += RedirectLog;
        }
        else
        {
            Application.logMessageReceived -= RedirectLog;
        }
    }

    private static void RedirectLog(string logString, string stackTrace, LogType type)
    {
        //TCPCommandLineListener.WriteMessageAsync(logString);
    }
}
