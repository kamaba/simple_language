using System.Diagnostics;
using System.Linq;
using System.Text;
using SimpleLanguage.Project;

namespace CSimpleVMTest;

internal static class Program
{
    static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        string repoRoot = GetRepoRoot();

        // Usage:
        //   dotnet run --project source/CSimpleVMTest2/CSimpleVMTest2.csproj -- <projectPathWithoutSpOrWithSp> [-test]
        // Example:
        //   ... -- E:\project\lang\simple_language\test\ExpendTest\ProjectTest
        string defaultProjectPath = Path.Combine(repoRoot, "test", "BaseTest", "ProjectTest");
        string projectPath = args.Length == 0 ? defaultProjectPath : args[0];
        bool runTestEntry = args.Any(a => string.Equals(a, "-test", StringComparison.OrdinalIgnoreCase));
        bool start = TryGetBoolArg(args, "start", defaultValue: true);

        if (args.Length == 0)
        {
            Console.WriteLine("No args provided, using defaults:");
            Console.WriteLine($"  projectPath = {projectPath}");
        }

        // Step 1: Compile with Front (in-process or via dotnet run)
        int frontExit = start
            ? RunFrontInProcess(projectPath)
            : RunDotnet(new List<string>
            {
                "run", "--project", Quote(Path.Combine(repoRoot, "source", "Front", "SimpleLanguageFront.csproj")), "--",
                "compile", "-e", "ir", "-p", projectPath, "--no-banner"
            }, "Front compile", repoRoot);
        if (frontExit != 0)
        {
            Console.WriteLine($"Front compile failed, exit code: {frontExit}");
            return frontExit;
        }

        // Step 2: Resolve compiled module package path
        string packagePath = ResolveModulePackagePath(repoRoot, projectPath);
        if (!File.Exists(packagePath))
        {
            Console.WriteLine("Compile succeeded but module package not found:");
            Console.WriteLine(packagePath);
            return 3;
        }

        Console.WriteLine($"Package: {packagePath}");

        // Step 3: Run C VM (csimple_lang.exe) as external process
        int cvmExit = RunCVM(packagePath, runTestEntry, repoRoot);
        if (cvmExit != 0)
        {
            Console.WriteLine($"C VM run failed, exit code: {cvmExit}");
            return cvmExit;
        }

        Console.WriteLine("Front compile + C VM run completed.");
        return 0;
    }

    static int RunCVM(string packagePath, bool runTestEntry, string repoRoot)
    {
        // Resolve csimple_lang.exe path
        string cvmDir = Path.GetFullPath(Path.Combine(repoRoot, "..", "csimple_lang", "build", "Debug", "bin"));
        string cvmExe = Path.Combine(cvmDir, "csimple_lang.exe");

        if (!File.Exists(cvmExe))
        {
            // Try Release build
            cvmDir = Path.GetFullPath(Path.Combine(repoRoot, "..", "csimple_lang", "build", "Release", "bin"));
            cvmExe = Path.Combine(cvmDir, "csimple_lang.exe");
        }

        if (!File.Exists(cvmExe))
        {
            Console.WriteLine("csimple_lang.exe not found. Build the C VM first:");
            Console.WriteLine("  cd ../csimple_lang && cmake -B build && cmake --build build --config Debug");
            return 4;
        }

        var cvmArgs = new List<string> { "run", Quote(packagePath) };
        if (runTestEntry)
        {
            cvmArgs.Add("-test");
        }

        return RunProcess(cvmExe, cvmArgs, "C VM run (csimple_lang)", cvmDir);
    }

    static int RunProcess(string fileName, List<string> args, string stepName, string? workingDirectory = null)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = string.Join(" ", args),
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? Environment.CurrentDirectory : workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true
        };

        using var p = new Process { StartInfo = psi };

        p.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            Console.WriteLine(e.Data);
        };
        p.ErrorDataReceived += (_, e) =>
        {
            if (e.Data == null) return;
            Console.Error.WriteLine(e.Data);
        };

        Console.WriteLine($"=== {stepName} ===");
        Console.WriteLine($"{psi.FileName} {psi.Arguments}");

        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        p.WaitForExit();

        return p.ExitCode;
    }

    static int RunDotnet(List<string> args, string stepName, string? workingDirectory = null)
    {
        return RunProcess("dotnet", args, stepName, workingDirectory);
    }

    static int RunFrontInProcess(string projectPath)
    {
        try
        {
            Console.WriteLine("=== Front compile (in-process) ===");
            var frontArgs = new[] { "compile", "-e", "ir", "-p", projectPath, "--no-banner" };
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

    static string ResolveModulePackagePath(string repoRoot, string projectPath)
    {
        var exportDir = Environment.GetEnvironmentVariable(ProjectOutputEnvironment.ExportOutDirEnv);
        if (!string.IsNullOrWhiteSpace(exportDir))
        {
            var d = Path.GetFullPath(exportDir.Trim());
            if (Directory.Exists(d))
            {
                var found = Directory.GetFiles(d, "*.module.json");
                if (found.Length == 1)
                    return found[0];
                if (found.Length > 1)
                    return found.OrderByDescending(File.GetLastWriteTimeUtc).First();
            }
        }

        // Derive module name from project path
        var moduleName = !string.IsNullOrWhiteSpace(projectPath)
            ? Path.GetFileName(projectPath.TrimEnd('\\', '/'))
            : "Core";
        var fallback = Path.Combine(repoRoot, "out", "export", moduleName, moduleName + ".module.json");
        if (File.Exists(fallback))
            return fallback;

        // Final fallback: search export dir for any module.json
        var exportRoot = Path.Combine(repoRoot, "out", "export");
        if (Directory.Exists(exportRoot))
        {
            var found = Directory.GetFiles(exportRoot, "*.module.json", SearchOption.AllDirectories);
            if (found.Length >= 1)
                return found.OrderByDescending(File.GetLastWriteTimeUtc).First();
        }

        return Path.Combine(repoRoot, "out", "export", "Core", "Core.module.json");
    }

    static string GetRepoRoot()
    {
        // source/CSimpleVMTest2 -> source -> repo root
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
