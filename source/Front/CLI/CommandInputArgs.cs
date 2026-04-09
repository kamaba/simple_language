
using System;
using System.IO;
using System.Linq;

public class CommandInputArgs
{
    public enum ECommandType
    {
        None,
        Compile,
        NewProject,
        NewClassFile,
    }

    public bool isTest { get; set; } = false;
    public bool isPrintToken { get; set; } = false;

    public ECommandType commandType { get; private set; } = ECommandType.None;
    public bool exportIR { get; private set; } = false;
    public string projectSpPath { get; private set; } = null;
    public string newProjectBasePath { get; private set; } = null;
    public string newProjectName { get; private set; } = null;
    public string newClassFileName { get; private set; } = null;

    public string[] rawArgs { get; private set; } = Array.Empty<string>();
    public string[] normalizedArgs { get; private set; } = Array.Empty<string>();

    public CommandInputArgs(string[] args)
    {
        rawArgs = args ?? Array.Empty<string>();
        normalizedArgs = Normalize(rawArgs);
        ParseCommand(normalizedArgs);
    }

    static string[] Normalize(string[] args)
    {
        if (args == null || args.Length == 0)
        {
            return Array.Empty<string>();
        }
        if (string.Equals(args[0], "sl", StringComparison.OrdinalIgnoreCase))
        {
            return args.Skip(1).ToArray();
        }
        return args;
    }

    void ParseCommand(string[] args)
    {
        if (args == null || args.Length == 0)
        {
            return;
        }

        if (string.Equals(args[0], "new", StringComparison.OrdinalIgnoreCase))
        {
            if (args.Length >= 3 && string.Equals(args[1], "project", StringComparison.OrdinalIgnoreCase))
            {
                commandType = ECommandType.NewProject;
                ParseNewProjectArgs(args);
                return;
            }

            if (args.Length >= 3 && string.Equals(args[1], "classfile", StringComparison.OrdinalIgnoreCase))
            {
                commandType = ECommandType.NewClassFile;
                newClassFileName = args[2]?.Trim();
                return;
            }
        }

        if (string.Equals(args[0], "c", StringComparison.OrdinalIgnoreCase))
        {
            commandType = ECommandType.Compile;
            for (int i = 1; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "-e", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(args[i + 1], "ir", StringComparison.OrdinalIgnoreCase))
                {
                    exportIR = true;
                }
            }
            return;
        }

        if (args[0].EndsWith(".sp", StringComparison.OrdinalIgnoreCase))
        {
            commandType = ECommandType.Compile;
            projectSpPath = args[0];
        }
    }

    void ParseNewProjectArgs(string[] args)
    {
        newProjectBasePath = Directory.GetCurrentDirectory();
        int nameIndex = 2;
        if (args.Length > 3 && string.Equals(args[2], "-p", StringComparison.OrdinalIgnoreCase))
        {
            newProjectBasePath = args[3];
            nameIndex = 4;
        }

        if (args.Length > nameIndex)
        {
            newProjectName = args[nameIndex]?.Trim();
        }
    }
}