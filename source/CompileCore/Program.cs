using System.Text;
using SimpleLanguage.Project;

namespace SimpleLanguageCompileCore;

internal static class Program
{
    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        string repoRoot = GetRepoRoot();

        // Usage:
        //   dotnet run --project source/CompileCore/SimpleLanguageCompileCore.csproj -- <projectPathWithoutSp>
        // Example:
        //   ... -- E:\project\lang\simple_language\source\Front\Lib\Core\Core
        string defaultProjectPath = Path.Combine(repoRoot, "source", "Front", "Lib", "Core", "Core");
        string projectPath = args.Length == 0 ? defaultProjectPath : args[0];

        if (args.Length == 0)
        {
            Console.WriteLine("No args provided, using defaults:");
            Console.WriteLine($"  projectPath = {projectPath}");
        }

        // Disable Debug.Assert popups / crashes on compiler errors
        SimpleLanguage.Logging.LogManager.Options.EnableAssertFeature = false;

        Console.WriteLine("=== Front compile (in-process) ===");
        try
        {
            var frontArgs = new[] { "c", "-e", "ir", "-p", projectPath };
            var inputArgs = new CommandInputArgs(frontArgs);
            _ = CommandExecutor.Execute(inputArgs);
            Console.Out.Flush();
            Console.WriteLine("Front compile completed.");
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
