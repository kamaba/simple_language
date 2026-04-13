using System.Diagnostics;
using System.Text;

namespace SimpleLanguageTest;

internal static class Program
{
    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        string repoRoot = GetRepoRoot();

        // Usage:
        //   dotnet run --project source/Test/SimpleLanguageTest.csproj -- <projectPathWithoutSpOrWithSp> [-test]
        // Example:
        //   ... -- E:\project\lang\simple_language\source\Front\Lib\Core\Core
        string defaultProjectPath = Path.Combine(repoRoot, "source", "Front", "Lib", "Core", "Core");
        string projectPath = args.Length == 0 ? defaultProjectPath : args[0];
        bool runTestEntry = args.Any(a => string.Equals(a, "-test", StringComparison.OrdinalIgnoreCase));
        string frontProject = Path.Combine(repoRoot, "source", "Front", "SimpleLanguageFront.csproj");
        string vmProject = Path.Combine(repoRoot, "source", "VM", "SimpleLanuageVM.csproj");

        if (args.Length == 0)
        {
            Console.WriteLine("No args provided, using defaults:");
            Console.WriteLine($"  projectPath = {projectPath}");
        }

        // 1) Front CLI compile/export IR
        var frontArgs = new List<string>
        {
            "run", "--project", Quote(frontProject), "--",
            "c", "-e", "ir", "-p", projectPath
        };
        int frontExit = RunDotnet(frontArgs, "Front compile");
        if (frontExit != 0)
        {
            Console.WriteLine($"Front compile failed, exit code: {frontExit}");
            return frontExit;
        }

        string packagePath = ResolveModulePackagePath(repoRoot, projectPath);
        if (!File.Exists(packagePath))
        {
            Console.WriteLine("Compile succeeded but module package not found:");
            Console.WriteLine(packagePath);
            return 3;
        }

        Console.WriteLine($"Package: {packagePath}");

        // 2) VM CLI execute package
        var vmArgs = new List<string>
        {
            "run", "--project", Quote(vmProject), "--", Quote(packagePath)
        };
        if (runTestEntry)
        {
            vmArgs.Add("-test");
        }

        int vmExit = RunDotnet(vmArgs, "VM run");
        if (vmExit != 0)
        {
            Console.WriteLine($"VM run failed, exit code: {vmExit}");
            return vmExit;
        }

        Console.WriteLine("Front compile + VM run completed.");
        return 0;
    }

    static int RunDotnet(List<string> args, string stepName)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = string.Join(" ", args),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true
        };

        using var p = new Process { StartInfo = psi };
        var sb = new StringBuilder();

        p.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            sb.AppendLine(e.Data);
            Console.WriteLine(e.Data);
        };
        p.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            sb.AppendLine(e.Data);
            Console.WriteLine(e.Data);
        };

        Console.WriteLine($"=== {stepName} ===");
        Console.WriteLine($"{psi.FileName} {psi.Arguments}");

        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        p.WaitForExit();

        return p.ExitCode;
    }

    static string ResolveModulePackagePath(string repoRoot, string projectPath)
    {
        // Front export default is repoRoot/out/export/module.package.json
        // Keep projectPath parameter for future per-project routing.
        _ = projectPath;
        return Path.Combine(repoRoot, "out", "export", "module.package.json");
    }

    static string GetRepoRoot()
    {
        // source/Test -> source -> repo root
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

    static string Quote(string s)
    {
        if (string.IsNullOrEmpty(s)) return "\"\"";
        if (s.Contains(' ') || s.Contains('\t') || s.Contains('"'))
            return "\"" + s.Replace("\"", "\\\"") + "\"";
        return s;
    }
}
