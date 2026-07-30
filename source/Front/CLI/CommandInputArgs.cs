using System;
using System.IO;
using System.Linq;

public class CommandInputArgs
{
    public enum ECommandType
    {
        None,
        Compile,        // compile / c / build / b
        Run,            // run / r
        NewProject,     // new project
        NewClassFile,   // new class / new classfile
        Clean,          // clean
        Version,        // version / v
        Help,           // help / h / --help / -h
        Export,         // export / e (IR only, no full compile)
    }

    // --- flags ---
    public bool isTest { get; set; } = false;
    public bool isPrintToken { get; set; } = false;
    public bool verbose { get; private set; } = false;
    public bool noBanner { get; private set; } = false;
    public bool isRelease { get; private set; } = false;

    // --- compile options ---
    public ECommandType commandType { get; private set; } = ECommandType.None;
    public bool exportIR { get; private set; } = false;
    public string projectSpPath { get; private set; } = null;
    public string compileProjectName { get; private set; } = null;
    public string compileProjectDir { get; private set; } = null;
    public string outputPath { get; private set; } = null;

    // --- new project ---
    public string newProjectBasePath { get; private set; } = null;
    public string newProjectName { get; private set; } = null;

    // --- new class ---
    public string newClassFileName { get; private set; } = null;

    // --- raw ---
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
            commandType = ECommandType.Help;
            return;
        }

        var cmd = args[0].ToLowerInvariant();

        switch (cmd)
        {
            case "compile":
            case "c":
            case "build":
            case "b":
                commandType = ECommandType.Compile;
                ParseCompileArgs(args);
                return;

            case "run":
            case "r":
                commandType = ECommandType.Run;
                ParseCompileArgs(args);
                return;

            case "export":
            case "e":
                commandType = ECommandType.Export;
                ParseCompileArgs(args);
                exportIR = true;
                return;

            case "new":
            case "n":
                ParseNewCommand(args);
                return;

            case "clean":
                commandType = ECommandType.Clean;
                ParseCompileArgs(args);
                return;

            case "version":
            case "v":
            case "--version":
            case "-v":
                commandType = ECommandType.Version;
                return;

            case "help":
            case "h":
            case "--help":
            case "-h":
            case "/?":
                commandType = ECommandType.Help;
                return;

            default:
                // Direct .sp file
                if (args[0].EndsWith(".sp", StringComparison.OrdinalIgnoreCase))
                {
                    commandType = ECommandType.Compile;
                    projectSpPath = args[0];
                    ParseGlobalFlags(args, 1);
                }
                return;
        }
    }

    void ParseCompileArgs(string[] args)
    {
        for (int i = 1; i < args.Length; i++)
        {
            var a = args[i];

            // -e ir  (export IR)
            if (string.Equals(a, "-e", StringComparison.OrdinalIgnoreCase)
                && i + 1 < args.Length
                && string.Equals(args[i + 1], "ir", StringComparison.OrdinalIgnoreCase))
            {
                exportIR = true;
                i++;
                continue;
            }

            // --export ir
            if (string.Equals(a, "--export", StringComparison.OrdinalIgnoreCase)
                && i + 1 < args.Length)
            {
                exportIR = true;
                i++;
                continue;
            }

            // -p <path>  /  --project <path>
            if ((string.Equals(a, "-p", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(a, "--project", StringComparison.OrdinalIgnoreCase))
                && i + 1 < args.Length)
            {
                ParseCompileProjectPath(args[i + 1]);
                i++;
                continue;
            }

            // -o <dir>  /  --output <dir>
            if ((string.Equals(a, "-o", StringComparison.OrdinalIgnoreCase)
                 || string.Equals(a, "--output", StringComparison.OrdinalIgnoreCase))
                && i + 1 < args.Length)
            {
                outputPath = args[i + 1];
                i++;
                continue;
            }

            // --test
            if (string.Equals(a, "--test", StringComparison.OrdinalIgnoreCase)
                || string.Equals(a, "-t", StringComparison.OrdinalIgnoreCase))
            {
                isTest = true;
                continue;
            }

            // --release
            if (string.Equals(a, "--release", StringComparison.OrdinalIgnoreCase))
            {
                isRelease = true;
                continue;
            }

            // --debug
            if (string.Equals(a, "--debug", StringComparison.OrdinalIgnoreCase))
            {
                isRelease = false;
                continue;
            }

            // --verbose / -v
            if (string.Equals(a, "--verbose", StringComparison.OrdinalIgnoreCase)
                || string.Equals(a, "--verbose", StringComparison.OrdinalIgnoreCase))
            {
                verbose = true;
                continue;
            }

            // --no-banner
            if (string.Equals(a, "--no-banner", StringComparison.OrdinalIgnoreCase))
            {
                noBanner = true;
                continue;
            }

            // --token
            if (string.Equals(a, "--token", StringComparison.OrdinalIgnoreCase))
            {
                isPrintToken = true;
                continue;
            }
        }
    }

    void ParseGlobalFlags(string[] args, int start)
    {
        for (int i = start; i < args.Length; i++)
        {
            if (string.Equals(args[i], "--verbose", StringComparison.OrdinalIgnoreCase))
                verbose = true;
            else if (string.Equals(args[i], "--no-banner", StringComparison.OrdinalIgnoreCase))
                noBanner = true;
            else if (string.Equals(args[i], "--test", StringComparison.OrdinalIgnoreCase))
                isTest = true;
            else if (string.Equals(args[i], "-e", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                exportIR = true;
                i++;
            }
        }
    }

    void ParseNewCommand(string[] args)
    {
        if (args.Length < 2)
        {
            commandType = ECommandType.Help;
            return;
        }

        var subCmd = args[1].ToLowerInvariant();

        if (string.Equals(subCmd, "project", StringComparison.OrdinalIgnoreCase) || string.Equals(subCmd, "p", StringComparison.OrdinalIgnoreCase))
        {
            commandType = ECommandType.NewProject;
            ParseNewProjectArgs(args);
            return;
        }

        if (string.Equals(subCmd, "class", StringComparison.OrdinalIgnoreCase)
            || string.Equals(subCmd, "classfile", StringComparison.OrdinalIgnoreCase)
            || string.Equals(subCmd, "c", StringComparison.OrdinalIgnoreCase))
        {
            commandType = ECommandType.NewClassFile;
            if (args.Length >= 3)
            {
                newClassFileName = args[2]?.Trim();
            }
            return;
        }
    }

    void ParseCompileProjectPath(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return;
        }

        var p = input.Trim();

        // support explicit .sp path directly
        if (p.EndsWith(".sp", StringComparison.OrdinalIgnoreCase))
        {
            projectSpPath = p;
            compileProjectName = Path.GetFileNameWithoutExtension(p);
            compileProjectDir = Path.GetDirectoryName(p);
            return;
        }

        // format expected by user: ...\<ProjectDir>\<ProjectName>
        // last segment is project name, preceding is project directory.
        var full = Path.GetFullPath(p);
        compileProjectName = Path.GetFileName(full);
        compileProjectDir = Path.GetDirectoryName(full);

        if (!string.IsNullOrWhiteSpace(compileProjectName)
            && !string.IsNullOrWhiteSpace(compileProjectDir))
        {
            projectSpPath = Path.Combine(compileProjectDir, compileProjectName + ".sp");
        }
    }

    void ParseNewProjectArgs(string[] args)
    {
        newProjectBasePath = Directory.GetCurrentDirectory();
        int nameIndex = 2;

        // new project -p <path> [name]
        if (args.Length > 3 && string.Equals(args[2], "-p", StringComparison.OrdinalIgnoreCase))
        {
            newProjectBasePath = args[3];
            nameIndex = 4;
        }
        // new project --path <path> [name]
        else if (args.Length > 3 && string.Equals(args[2], "--path", StringComparison.OrdinalIgnoreCase))
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
