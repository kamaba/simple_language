using System.Text;
using SimpleLanguage.Project;

namespace SimpleLanguageCompileStd;

internal static class Program
{
    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        string repoRoot = GetRepoRoot();

        // Default: compile Std.sp with IR export
        string defaultProjectPath = Path.Combine(repoRoot, "source", "Front", "Lib", "Std", "Std");

        // Project path = first non-flag argument. Flags (e.g. -o3, --test) are forwarded to the
        // CLI below and must not be mistaken for the project path.
        string projectPath = defaultProjectPath;
        foreach (var a in args)
        {
            if (!string.IsNullOrEmpty(a) && !a.StartsWith('-'))
            {
                projectPath = a;
                break;
            }
        }

        // Build CLI args: compile -e ir -o3 -p <path> --no-banner.
        // All user args are forwarded after ours, so an explicit -O0..-O2 (etc.) can override.
        var cliArgs = new List<string>
        {
            "compile",
            "-e", "ir",
            "-o3",
            "-p", projectPath,
            "--no-banner"
        };
        cliArgs.AddRange(args);

        Console.WriteLine($"CompileStd: project = {projectPath} (args: {string.Join(' ', args)})");

        // Disable Debug.Assert crashes on compiler errors
        SimpleLanguage.Logging.LogManager.Options.EnableAssertFeature = false;

        try
        {
            var inputArgs = new CommandInputArgs(cliArgs.ToArray());
            _ = CommandExecutor.Execute(inputArgs);
            Console.Out.Flush();
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return 1;
        }
    }

    static string GetRepoRoot()
    {
        string dir = AppContext.BaseDirectory;
        var d = new DirectoryInfo(dir);
        while (d != null && !File.Exists(Path.Combine(d.FullName, "SimpleLanguage.sln")))
        {
            d = d.Parent;
        }
        if (d == null)
            throw new DirectoryNotFoundException("Cannot locate repo root (SimpleLanguage.sln).");
        return d.FullName;
    }
}
