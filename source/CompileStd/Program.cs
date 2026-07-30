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
        string projectPath = args.Length == 0 ? defaultProjectPath : args[0];

        // Build CLI args: compile -e ir -p <path> --no-banner
        var cliArgs = new List<string>
        {
            "compile",
            "-e", "ir",
            "-p", projectPath,
            "--no-banner"
        };

        // Pass through extra args (e.g. --test, --release, --verbose)
        for (int i = 1; i < args.Length; i++)
        {
            cliArgs.Add(args[i]);
        }

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
