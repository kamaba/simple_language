using System.Diagnostics;
using System.Text;
using SimpleLanguage.Project;
using SimpleLanguage.VM;

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
        bool start = TryGetBoolArg(args, "start", defaultValue: true);
        string frontProject = Path.Combine(repoRoot, "source", "Front", "SimpleLanguageFront.csproj");
        string vmProject = Path.Combine(repoRoot, "source", "VM", "SimpleLanuageVM.csproj");

        if (args.Length == 0)
        {
            Console.WriteLine("No args provided, using defaults:");
            Console.WriteLine($"  projectPath = {projectPath}");
        }

        int frontExit = start
            ? RunFrontInProcess(projectPath)
            : RunDotnet(new List<string>
            {
                "run", "--project", Quote(frontProject), "--",
                "c", "-e", "ir", "-p", projectPath
            }, "Front compile");
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

        int vmExit = start
            ? RunVmInProcess(packagePath, runTestEntry)
            : RunVmViaProcess(vmProject, packagePath, runTestEntry);
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

    static int RunVmViaProcess(string vmProject, string packagePath, bool runTestEntry)
    {
        var vmArgs = new List<string>
        {
            "run", "--project", Quote(vmProject), "--", Quote(packagePath)
        };
        if (runTestEntry)
        {
            vmArgs.Add("-test");
        }
        return RunDotnet(vmArgs, "VM run");
    }

    static int RunFrontInProcess(string projectPath)
    {
        try
        {
            Console.WriteLine("=== Front compile (in-process) ===");
            var frontArgs = new[] { "c", "-e", "ir", "-p", projectPath };
            var inputArgs = new CommandInputArgs(frontArgs);
            _ = CommandExecutor.Execute(inputArgs);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return 1;
        }
    }

    static int RunVmInProcess(string packagePath, bool runTestEntry)
    {
        try
        {
            Console.WriteLine("=== VM run (in-process) ===");
            var vmArgs = runTestEntry
                ? new[] { packagePath, "-test" }
                : new[] { packagePath };

            var graph = SLIRJsonModuleLoader.ReadPackagesInExecutionOrder(packagePath);
            var parseResult = SLIRModuleParse.Parse(graph, vmArgs);
            if (parseResult == null)
            {
                return 2;
            }

            SLIRModuleParse.EntryPoint(parseResult);
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return 1;
        }
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

    static bool TryGetBoolArg(string[] args, string key, bool defaultValue)
    {
        if (args == null || args.Length == 0)
        {
            return defaultValue;
        }

        string keyLower = key.ToLowerInvariant();
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (string.IsNullOrWhiteSpace(a))
            {
                continue;
            }

            var low = a.ToLowerInvariant();
            if (low == $"--{keyLower}" || low == $"-{keyLower}")
            {
                if (i + 1 < args.Length && bool.TryParse(args[i + 1], out var parsed))
                {
                    return parsed;
                }
                return true;
            }

            if (low.StartsWith($"--{keyLower}=", StringComparison.Ordinal))
            {
                var raw = a.Substring(key.Length + 3);
                if (bool.TryParse(raw, out var parsed))
                {
                    return parsed;
                }
            }
        }

        return defaultValue;
    }
}
