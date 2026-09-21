using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using SimpleLanguage.Logging;
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
        string projectPath = "";
        if (args.Length == 1 )
        {
            string arg = args[0];
            if (arg == "-h" || arg == "--help" || arg == "/?")
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  dotnet run --project source/CSimpleVMTest2/CSimpleVMTest2.csproj -- <projectPathWithoutSpOrWithSp> [-test]");
                Console.WriteLine("Example:");
                Console.WriteLine("  ... -- E:\\project\\lang\\simple_language\\test\\ExpendTest\\ProjectTest");
                return 0;
            }
            else if (arg == "NullFast")
            {
                projectPath = Path.Combine(repoRoot, "test", "Other", "NullFastTest", "ProjectTest");
            }
            else if (arg == "StaticIf")
            {
                projectPath = Path.Combine(repoRoot, "test", "Other", "StaticIfTest", "ProjectTest");
            }
            else if (arg == "JsonTest")
            {
                projectPath = Path.Combine(repoRoot, "test", "Other", "JsonTest", "JsonTest");
            }
            else if (arg == "PlatformAotNegTest")
            {
                projectPath = Path.Combine(repoRoot, "test", "Other", "PlatformAotNegTest", "PlatformAotNegTest");
            }
            else if (arg == "PlatformNegTest")
            {
                projectPath = Path.Combine(repoRoot, "test", "Other", "PlatformNegTest", "PlatformNegTest");
            }
            else if (arg == "PlatformOverrideNegTest")
            {
                projectPath = Path.Combine(repoRoot, "test", "Other", "PlatformOverrideNegTest", "PlatformOverrideNegTest");
            }
            else if (arg == "PlatformVariantNegTest")
            {
                projectPath = Path.Combine(repoRoot, "test", "Other", "PlatformVariantNegTest", "PlatformVariantNegTest");
            }
            else if (arg == "PlatformVariantTest")
            {
                projectPath = Path.Combine(repoRoot, "test", "Other", "PlatformVariantTest", "PlatformVariantTest");
            }
            else if (arg == "InlineLambdaTest")
            {
                projectPath = Path.Combine(repoRoot, "test", "Other", "InlineLambdaTest", "ProjectTest");
            }
            else if (arg == "InlineLambdaForbiddenTest")
            {
                projectPath = Path.Combine(repoRoot, "test", "Other", "InlineLambdaForbiddenTest", "ProjectTest");
            }
            else if (arg == "AutoInlineTest")
            {
                projectPath = Path.Combine(repoRoot, "test", "Other", "AutoInlineTest", "ProjectTest");
            }
            else if (arg == "InlineMethodTest")
            {
                projectPath = Path.Combine(repoRoot, "test", "Other", "InlineMethodTest", "ProjectTest");
            }
            else if (arg == "ARCTest")
            {
                projectPath = Path.Combine(repoRoot, "test", "Other", "ARCTest", "ArcProbe");
            }
            else if (arg == "GameTank")
            {
                projectPath = Path.Combine(repoRoot, "samples", "Game", "TankGame", "TankGame");
            }
            

        }
        else
        {
            projectPath = Path.Combine(repoRoot, "test", "ExpendTest", "ProjectTest");
        }
        bool runTestEntry = args.Any(a => string.Equals(a, "-test", StringComparison.OrdinalIgnoreCase));
        bool start = TryGetBoolArg(args, "start", defaultValue: true);
        bool debug = args.Any(a => string.Equals(a, "-debug", StringComparison.OrdinalIgnoreCase));
        // -O0..-O3 / -o0..-o3 forwarded to the Front compile step (absent = compiler default level)
        string? optLevelArg = GetOptimizeLevelArg(args);

        if (args.Length == 0)
        {
            Console.WriteLine("No args provided, using defaults:");
            Console.WriteLine($"  projectPath = {projectPath}");
        }

        // Step 1: Compile with Front (in-process or via dotnet run)
        var dotnetFrontArgs = new List<string>
        {
            "run", "--project", Quote(Path.Combine(repoRoot, "source", "Front", "SimpleLanguageFront.csproj")), "--",
            "compile", "-e", "ir", "-p", projectPath
        };
        if (optLevelArg != null)
            dotnetFrontArgs.Add(optLevelArg);
        dotnetFrontArgs.Add("--no-banner");

        int frontExit = start
            ? RunFrontInProcess(projectPath, optLevelArg)
            : RunDotnet(dotnetFrontArgs, "Front compile", repoRoot);
        if (frontExit != 0)
        {
            Console.WriteLine($"Front compile failed, exit code: {frontExit}");
            // 负例工程: 前端编译失败是预期结果, 校验预期拦截 LID 全部命中则视为通过 (exit 0)
            var neg = TryGetNegativeFrontCase(args);
            if (neg != null)
                return VerifyNegativeFrontCase(neg, repoRoot, frontExit);
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
        // C VM 测试用例使用相对路径（如 Sqlite3Test 的 Resources/ttest），
        // VS 启动时 CWD 是 bin\Debug\net8.0，需切到测试工程目录，与命令行运行环境一致
        string? projectDir = Path.GetDirectoryName(Path.GetFullPath(projectPath));
        if (Directory.Exists(projectDir))
        {
            Environment.CurrentDirectory = projectDir;
        }
        int cvmExit = RunCVM(packagePath, runTestEntry, repoRoot, debug);
        if (cvmExit != 0)
        {
            Console.WriteLine($"C VM run failed, exit code: {cvmExit}");
            return cvmExit;
        }

        Console.WriteLine("Front compile + C VM run completed.");
        return 0;
    }

    static int RunCVM(string packagePath, bool runTestEntry, string repoRoot, bool debug = false)
    {
        // Resolve csimple_lang exe/dll path
        string cvmDir = Path.GetFullPath(Path.Combine(repoRoot, "..", "csimple_lang", "build", "Debug", "bin"));
        string cvmExe = Path.Combine(cvmDir, "csimple_lang.exe");

        if (!File.Exists(cvmExe))
        {
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

#if DEBUG
        // Debug: P/Invoke into csimple_lang_dll.dll (in-process, can attach C debugger)
        string dllPath = Path.Combine(cvmDir, "csimple_lang_dll.dll");
        if (!File.Exists(dllPath))
        {
            Console.WriteLine($"csimple_lang_dll.dll not found at {dllPath}, falling back to process mode.");
            return RunProcess(cvmExe, cvmArgs, "C VM run (csimple_lang)");
        }

        Console.WriteLine("=== C VM run (P/Invoke) ===");
        Console.WriteLine($"DLL: {dllPath}");

        // Add DLL directory to search path so DllImport can find it
        if (OperatingSystem.IsWindows())
            SetDllDirectory(cvmDir);

        // Build argv: ["csimple_lang", "run", packagePath, ...]
        var argv = new List<string> { "csimple_lang" };
        argv.AddRange(cvmArgs);
        return CallCliMain(argv.ToArray());
#else
        // Release: process invocation
        if (debug)
        {
            Console.WriteLine("=== C VM debug mode (csimple_lang) ===");
            Console.WriteLine($"{cvmExe} {string.Join(" ", cvmArgs)}");
            Console.WriteLine("Press Enter to start C VM...");
            Console.ReadLine();
            return RunProcessDirect(cvmExe, cvmArgs, Environment.CurrentDirectory);
        }
        return RunProcess(cvmExe, cvmArgs, "C VM run (csimple_lang)");
#endif
    }

#if DEBUG
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    static extern bool SetDllDirectory(string lpPathName);

    [DllImport("csimple_lang_dll.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern int cli_main(int argc, IntPtr argv);

    static int CallCliMain(string[] args)
    {
        int argc = args.Length;
        var argvPtrs = new IntPtr[argc];
        try
        {
            for (int i = 0; i < argc; i++)
            {
                byte[] bytes = Encoding.UTF8.GetBytes(args[i] + "\0");
                argvPtrs[i] = Marshal.AllocHGlobal(bytes.Length);
                Marshal.Copy(bytes, 0, argvPtrs[i], bytes.Length);
            }
            IntPtr nativeArgv = Marshal.AllocHGlobal(IntPtr.Size * argc);
            Marshal.Copy(argvPtrs, 0, nativeArgv, argc);
            int ret = cli_main(argc, nativeArgv);
            Marshal.FreeHGlobal(nativeArgv);
            return ret;
        }
        finally
        {
            for (int i = 0; i < argc; i++)
            {
                if (argvPtrs[i] != IntPtr.Zero)
                    Marshal.FreeHGlobal(argvPtrs[i]);
            }
        }
    }
#endif

    static int RunProcessDirect(string fileName, List<string> args, string workingDirectory)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = string.Join(" ", args),
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            CreateNoWindow = false
        };
        using var p = new Process { StartInfo = psi };
        p.Start();
        p.WaitForExit();
        return p.ExitCode;
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

    static int RunFrontInProcess(string projectPath, string? optLevelArg = null)
    {
        try
        {
            Console.WriteLine("=== Front compile (in-process) ===");
            var frontArgs = new List<string> { "compile", "-e", "ir", "-p", projectPath };
            if (optLevelArg != null)
                frontArgs.Add(optLevelArg);
            frontArgs.Add("--no-banner");
            Console.WriteLine("Front: " + string.Join(' ', frontArgs));
            var inputArgs = new CommandInputArgs(frontArgs.ToArray());
            bool ok = CommandExecutor.Execute(inputArgs);
            if (!ok || Log.errorCount > 0)
            {
                Console.WriteLine($"Front compile failed, error count: {Log.errorCount}");
                return 1;
            }
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return 1;
        }
    }

    // ==== 负例工程支持: 前端编译「预期失败 = 成功」====
    // 负例工程的拦截命中会让 Front 以非零码退出, 这里读 Front.txt 校验
    // 预期拦截 LID 是否全部命中: 全命中 = 负例通过 (exit 0, 跳过 C VM 运行),
    // 有缺命中 = 真失败。只看预期 LID 命中数, 不计连锁产生的其他日志。

    private sealed class NegativeFrontCase
    {
        public string ArgName = "";          // 命令行启动项名 (args[0])
        public string ExportModuleName = ""; // 导出模块名: out/export/<名>/Logs/Front.txt
        public (string Lid, int Count)[] ExpectedLids = Array.Empty<(string, int)>();
    }

    private static readonly NegativeFrontCase[] s_negativeFrontCases = new[]
    {
        new NegativeFrontCase
        {
            ArgName = "InlineLambdaForbiddenTest",
            ExportModuleName = "InlineLambdaForbiddenTest",
            // 6 个负例: 协程拦截 3 (spawn/await/isolate) + 自引用/互递归 2 + try? 1
            ExpectedLids = new[]
            {
                ("MetaCoreInlineLambdaBodyForbiddenCoroutine", 3),
                ("MetaCoreInlineLambdaBodyForbiddenSelfReference", 2),
                ("MetaCoreInlineLambdaBodyForbiddenExceptionFrame", 1),
            }
        },
    };

    static NegativeFrontCase? TryGetNegativeFrontCase(string[] args)
    {
        if (args.Length == 0)
            return null;
        return s_negativeFrontCases.FirstOrDefault(c => c.ArgName == args[0]);
    }

    // 校验负例: 按行统计 Front.txt 中 "[LID]" 出现次数, 预期全部达标 = 通过
    static int VerifyNegativeFrontCase(NegativeFrontCase neg, string repoRoot, int frontExit)
    {
        // 优先用 Front 写入的日志路径 (in-process 模式下最准确), 回退到按模块名推导
        string? logPath = Log.FrontLogFilePath;
        if (string.IsNullOrWhiteSpace(logPath) || !File.Exists(logPath))
            logPath = Path.Combine(repoRoot, "out", "export", neg.ExportModuleName, "Logs", "Front.txt");
        if (!File.Exists(logPath))
        {
            Console.WriteLine($"Negative case '{neg.ArgName}': Front log not found: {logPath}");
            return frontExit;
        }

        var lines = File.ReadAllLines(logPath);
        bool allHit = true;
        Console.WriteLine($"=== Negative front case '{neg.ArgName}': verify expected LID hits ===");
        foreach (var (lid, expected) in neg.ExpectedLids)
        {
            int actual = lines.Count(l => l.Contains("[" + lid + "]"));
            bool hit = actual >= expected;
            Console.WriteLine($"  [{(hit ? "OK" : "MISS")}] {lid}: expect >= {expected}, actual {actual}");
            if (!hit)
                allHit = false;
        }
        if (allHit)
        {
            Console.WriteLine($"Negative test passed: all expected LID interceptions hit, skip C VM run.");
            return 0;
        }
        Console.WriteLine("Negative test FAILED: some expected LID interceptions missing.");
        return frontExit;
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

    static string? GetProjectPathArg(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            if (string.IsNullOrEmpty(a) || a.StartsWith('-'))
                continue;
            // "-start true/false" takes the next token as its value, not a project path
            if (i > 0 && string.Equals(args[i - 1], "-start", StringComparison.OrdinalIgnoreCase))
                continue;
            return a;
        }
        return null;
    }

    static string? GetOptimizeLevelArg(string[] args)
    {
        // Same -O0..-O3 / -o0..-o3 pattern as CommandInputArgs
        foreach (var a in args)
        {
            if (a.Length == 3 && a[0] == '-'
                && (a[1] == 'O' || a[1] == 'o')
                && a[2] >= '0' && a[2] <= '3')
                return a;
        }
        return null;
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
