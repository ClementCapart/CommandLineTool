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

        //Assembly EditorAssembly = typeof(CommandLineEditorDispatcher).Assembly;
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
            FieldInfo[] fields = assemblyTypes[i].GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

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

            for (int j = 0; j < fields.Length; j++)
            {
                object[] customAttributes = fields[j].GetCustomAttributes(typeof(CommandLineAttribute), true);
                if (customAttributes.Length > 0)
                {
                    for (int attributeIndex = 0; attributeIndex < customAttributes.Length; attributeIndex++)
                    {
                        CommandLineAttribute line = (CommandLineAttribute)customAttributes[attributeIndex];
                        if (line != null)
                        {
                            object value = null;
                            if (line.m_Arguments != null && line.m_Arguments.Length > 0 && line.m_Arguments[0].GetType().Equals(fields[j].FieldType))
                            {
                                value = line.m_Arguments[0];
                            }

                            CommandLineData newLine = new CommandLineData(line.CommandLine, fields[j], value, line.m_HelpText);
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

        CommandLineData commandData = ParseArguments(commandAndArgs, m_CommandLines[commandAndArgs[0]]);
        return DispatchCommandLineToMainThread(commandData);
    }

    public static bool CallMethod(CommandLineData command)
    {
        try
        {
            if (command.IsMethodCommand)
            {
                command.Method.Invoke(null, command.Arguments);
            }
            else if (command.IsFieldCommand)
            {
                if (command.Arguments[0] != null)
                {
                    command.Field.SetValue(null, command.Arguments[0]);
                }
                else
                {
                    Console.Log("Command '" + command.CommandLine + "' couldn't be called, missing argument of type " + command.Field.FieldType.ToString(), "red");
                }
            }
            return true;
        }
        catch (Exception e)
        {
            string commandExample = command.CommandLine;
            if (command.IsMethodCommand)
            {
                ParameterInfo[] parameters = command.Method.GetParameters();
                for (int i = 0; i < parameters.Length; i++)
                {
                    commandExample += " " + parameters[i].ParameterType.ToString();
                }
            }

            Console.Log("Command '" + command.CommandLine + "' couldn't be called. Command should be: " + commandExample, "red");
            Debug.Log(e);
            return false;
        }
    }

    private static bool DispatchCommandLineToMainThread(CommandLineData command)
    {
        m_MainThreadCommandLines.Enqueue(command);
        return true;
    }

    private static object ParseArgument(string argString, Type paramType)
    {
        if (paramType == typeof(int))
        {
            int arg = 0;
            if (!int.TryParse(argString, out arg))
            {
                return null;
            }
            return arg;
        }
        else if (paramType == typeof(float))
        {
            float arg = 0;
            if (!float.TryParse(argString, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out arg))
            {
                return null;
            }
            return arg;
        }
        else if (paramType == typeof(string))
        {
            string arg = argString;
            if (arg != null && arg.StartsWith("\"") && arg.EndsWith("\""))
            {
                arg = arg.Remove(0, 1);
                arg = arg.Remove(arg.Length - 1, 1);
            }

            return arg;
        }
        else if (paramType == typeof(bool))
        {
            bool arg = false;
            if (!bool.TryParse(argString, out arg))
            {
                int intBool = 0;
                if (!int.TryParse(argString, out intBool))
                {
                    return null;
                }
                else
                {
                    arg = intBool > 0;
                }
            }

            return arg;
        }
        else if (paramType.IsEnum)
        {
            object arg = null;

            try
            {
                arg = Enum.Parse(paramType, argString, true);
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                return null;
            }

            return arg;
        }

        return null;
    }

    private static CommandLineData ParseArguments(string[] commandAndArgs, CommandLineData data)
    {
        CommandLineData lineData = null;

        if (data.IsMethodCommand)
        {
            lineData = new CommandLineData(data.CommandLine, data.Method, (object[])data.Arguments.Clone(), data.HelpText);

            ParameterInfo[] parameters = data.Method.GetParameters();
            object[] newArguments = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                if (commandAndArgs.Length <= i + 1)
                {
                    newArguments[i] = ParseArgument(null, parameters[i].ParameterType);
                }
                else
                {
                    newArguments[i] = ParseArgument(commandAndArgs[i + 1], parameters[i].ParameterType);
                }
            }

            lineData.MergeNewArguments(newArguments);
        }
        else if (data.IsFieldCommand)
        {
            lineData = new CommandLineData(data.CommandLine, data.Field, data.Arguments[0], data.HelpText);

            if (commandAndArgs.Length > 1)
            {
                lineData.SetNewFieldValue(ParseArgument(commandAndArgs[1], data.Field.FieldType));
            }
            else
            {
                lineData.SetNewFieldValue(ParseArgument(null, data.Field.FieldType));
            }
        }

        return lineData;
    }

    [CommandLine("help", "Print the help text of any command line passed as parameter")]
    private static void HelpText(string commandLine)
    {
        if (m_CommandLines.ContainsKey(commandLine))
        {
            if (m_CommandLines[commandLine].HelpText != "")
            {
                Console.Log(commandLine + ": " + m_CommandLines[commandLine].HelpText);
            }
            else
            {
                Console.Log(commandLine + ": Could not find a help message :(");
            }

        }
        else
        {
            Console.Log("Command '" + commandLine + "' could not be found.");
        }
    }

    [CommandLine("attach_unity_output", "Attach or detach Unity's console output to the tool")]
    private static void CatchDebugOutput(bool enable)
    {
        if (enable)
        {
            Application.logMessageReceivedThreaded += RedirectLog;
        }
        else
        {
            Application.logMessageReceivedThreaded -= RedirectLog;
        }
    }

    private static void RedirectLog(string logString, string stackTrace, LogType type)
    {
        //- Meh implementation as if a message is printed with Console.Log to the Debug.Log and the Console, it will be printed twice there : ( - cc
        Console.Log("Unity " + type.ToString() + ": " + logString, "", null, false);
    }
}
