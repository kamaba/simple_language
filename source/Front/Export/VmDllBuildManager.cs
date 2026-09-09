//****************************************************************************
//  File:      VmDllBuildManager.cs
// ------------------------------------------------
//  Description:  build "vmDlls" projects (VS .vcxproj / .sln) via MSBuild and
//                copy the output DLLs next to the exported module.json.
//****************************************************************************

using SimpleLanguage.Logging;
using SimpleLanguage.Project;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace SimpleLanguage.Export
{
    /// <summary>
    /// vmDlls 构建管理器（project.jsonc "vmDlls" 段）：
    /// 导出 module.json 前，用 MSBuild 编译配置里的 VS 工程（.vcxproj/.sln，
    /// project 字段相对 jsonc 所在目录），并把产出 DLL 拷到 module.json 相同
    /// 目录（outDir）。module.json 的 vmDllImports 只带 name，cvm 加载时按
    /// package 目录预加载这些 DLL，供 systemCalls 的 "DllName!symbol" 解析。
    /// 任何失败仅记日志不中断导出（module.json 仍带 vmDllImports，运行期再告警）。
    /// </summary>
    public static class VmDllBuildManager
    {
        /// <summary>MSBuild 路径覆盖环境变量（优先于自动探测）。</summary>
        private const string MsBuildEnv = "SIMPLELANG_MSBUILD";

        /// <summary>构建产物行（/v:m 时 MSBuild 输出 "ProjectName -> path"）。</summary>
        private static readonly Regex s_outputLineRegex =
            new Regex(@"^\s*(?<proj>.+?)\s+->\s+(?<path>.+?)\s*$", RegexOptions.Compiled);

        public static void Run(string outDir)
        {
            var vmDlls = ProjectManager.config?.VmDlls;
            if (vmDlls == null || vmDlls.Count == 0 || string.IsNullOrWhiteSpace(outDir))
            {
                return;
            }

            var msbuild = ResolveMsBuild();
            if (msbuild == null)
            {
                Log.AddIRLog(LID.ExportVmDllMsBuildNotFound,
                    "vmDll: MSBuild.exe not found (set " + MsBuildEnv + " to override), skip vmDlls build");
                return;
            }

            var projectDir = !string.IsNullOrWhiteSpace(ProjectManager.projectPath)
                ? ProjectManager.projectPath
                : Environment.CurrentDirectory;

            foreach (var v in vmDlls)
            {
                if (v == null || string.IsNullOrWhiteSpace(v.Project) || string.IsNullOrWhiteSpace(v.Name))
                {
                    continue;
                }
                try
                {
                    BuildOne(msbuild, v, projectDir, outDir);
                }
                catch (Exception e)
                {
                    Log.AddIRLog(LID.ExportVmDllBuildFailed,
                        "vmDll: build failed: " + v.Project + " " + e.Message);
                }
            }
        }

        private static void BuildOne(string msbuild, ProjectConfig.VmDllSection v,
            string projectDir, string outDir)
        {
            // 1) 解析工程文件路径（相对 jsonc 所在目录）
            var projPath = Path.IsPathRooted(v.Project)
                ? Path.GetFullPath(v.Project)
                : Path.GetFullPath(Path.Combine(projectDir, v.Project));
            if (!File.Exists(projPath))
            {
                Log.AddIRLog(LID.ExportVmDllBuildFailed,
                    "vmDll: project not found: " + projPath);
                return;
            }

            Log.AddIRLog(LID.ExportVmDllBuildProject,
                "vmDll: build project: " + projPath + " (" + v.Configuration + "|" + v.Platform + ")");

            // 2) MSBuild 编译（自身带增量：未变更时为 no-op）
            var workingDir = Path.GetDirectoryName(projPath) ?? projectDir;
            var props = " /p:Configuration=" + Quote(v.Configuration)
                      + " /p:Platform=" + Quote(v.Platform);
            var buildOutput = RunCapture(msbuild,
                Quote(projPath) + " /t:Build" + props + " /m /v:m /nologo", workingDir);
            if (buildOutput == null)
            {
                Log.AddIRLog(LID.ExportVmDllBuildFailed,
                    "vmDll: msbuild failed: " + projPath);
                return;
            }

            // 3) 定位构建产物（TargetPath）：优先解析 /v:m 输出的 "-> path" 行
            var targetPath = FindOutputInText(buildOutput, v.Name, workingDir);
            if (targetPath == null)
            {
                // 备选：-getProperty:TargetPath（MSBuild 17.8+），直接回显绝对路径
                var propOutput = RunCapture(msbuild,
                    Quote(projPath) + " -getProperty:TargetPath" + props + " /nologo", workingDir);
                targetPath = FindOutputInText(propOutput ?? string.Empty, v.Name, workingDir);
            }
            if (targetPath == null || !File.Exists(targetPath))
            {
                Log.AddIRLog(LID.ExportVmDllBuildFailed,
                    "vmDll: target dll not found after build: " + v.Name + " (" + projPath + ")");
                return;
            }

            // 4) 拷到 module.json 相同目录
            Directory.CreateDirectory(outDir);
            var dstPath = Path.Combine(outDir, v.Name);
            File.Copy(targetPath, dstPath, overwrite: true);
            Log.AddIRLog(LID.ExportVmDllBuildSuccess,
                "vmDll: build success: " + targetPath + " -> " + dstPath);
        }

        /// <summary>
        /// 从 MSBuild 输出中解析产物路径：
        /// a) "ProjectName -> path" 行（/v:m；.sln 多工程时按文件名匹配）；
        /// b) 纯路径行（-getProperty:TargetPath 的回显）。
        /// 相对路径按 workingDir 解析。
        /// </summary>
        private static string FindOutputInText(string text, string wantName, string workingDir)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }
            string last = null;
            foreach (var line in text.Split('\n'))
            {
                var m = s_outputLineRegex.Match(line);
                if (!m.Success)
                {
                    continue;
                }
                var p = ResolvePath(m.Groups["path"].Value, workingDir);
                if (p == null)
                {
                    continue;
                }
                last = p;
                if (string.Equals(Path.GetFileName(p), wantName, StringComparison.OrdinalIgnoreCase))
                {
                    return p;
                }
            }
            if (last != null)
            {
                return last;
            }
            // -getProperty:TargetPath 回显：整行即绝对路径
            foreach (var line in text.Split('\n'))
            {
                var t = line.Trim();
                if (t.Length > 3 && Path.IsPathRooted(t) && t.IndexOf(Path.DirectorySeparatorChar) > 0
                    && string.Equals(Path.GetFileName(t), wantName, StringComparison.OrdinalIgnoreCase))
                {
                    return File.Exists(t) ? t : null;
                }
            }
            return null;
        }

        private static string ResolvePath(string p, string workingDir)
        {
            try
            {
                p = p.Trim().Trim('"');
                if (string.IsNullOrWhiteSpace(p))
                {
                    return null;
                }
                return Path.IsPathRooted(p) ? Path.GetFullPath(p) : Path.GetFullPath(Path.Combine(workingDir, p));
            }
            catch
            {
                return null;
            }
        }

        // ------------------------------------------------------------------
        // MSBuild 探测：env 覆盖 -> vswhere -> 常见安装路径 -> PATH
        // ------------------------------------------------------------------

        private static string ResolveMsBuild()
        {
            // 1) 显式覆盖
            var env = Environment.GetEnvironmentVariable(MsBuildEnv);
            if (!string.IsNullOrWhiteSpace(env) && File.Exists(env))
            {
                return env;
            }

            // 2) vswhere：最新 VS 的 MSBuild（-find 返回多行时取第一个）
            var vswhere = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Microsoft Visual Studio", "Installer", "vswhere.exe");
            if (File.Exists(vswhere))
            {
                var found = TryRunCapture(vswhere,
                    "-latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\\**\\Bin\\MSBuild.exe");
                if (!string.IsNullOrWhiteSpace(found))
                {
                    var first = found.Split('\n').Select(l => l.Trim())
                        .FirstOrDefault(l => l.Length > 0 && File.Exists(l));
                    if (first != null)
                    {
                        return first;
                    }
                }
            }

            // 3) 常见安装路径（版本目录倒序取最新）
            foreach (var root in new[]
                     {
                         Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                         Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                     })
            {
                if (string.IsNullOrWhiteSpace(root))
                {
                    continue;
                }
                var vsRoot = Path.Combine(root, "Microsoft Visual Studio");
                if (!Directory.Exists(vsRoot))
                {
                    continue;
                }
                var exe = Directory.EnumerateDirectories(vsRoot, "*", SearchOption.TopDirectoryOnly)
                    .OrderByDescending(d => d, StringComparer.OrdinalIgnoreCase)
                    .SelectMany(ver => Directory.EnumerateDirectories(ver, "*", SearchOption.TopDirectoryOnly)
                        .OrderByDescending(e => e, StringComparer.OrdinalIgnoreCase),
                        (ver, edition) => Path.Combine(edition, "MSBuild", "Current", "Bin", "MSBuild.exe"))
                    .FirstOrDefault(File.Exists);
                if (exe != null)
                {
                    return exe;
                }
            }

            // 4) PATH（如 VS 开发者命令行）
            var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    var p = Path.Combine(dir.Trim(), "MSBuild.exe");
                    if (File.Exists(p))
                    {
                        return p;
                    }
                }
                catch
                {
                    // malformed PATH entry
                }
            }
            return null;
        }

        // ------------------------------------------------------------------
        // Process helpers
        // ------------------------------------------------------------------

        private static string Quote(string s) => "\"" + s + "\"";

        /// <summary>运行并合并 stdout/stderr；失败（非零退出/无法启动）返回 null。</summary>
        private static string RunCapture(string fileName, string args, string workingDirectory)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                using var p = Process.Start(psi);
                if (p == null)
                {
                    return null;
                }
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();
                p.WaitForExit();
                return p.ExitCode == 0 ? stdout + "\n" + stderr : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>探测用：忽略退出码与 stderr，只取 stdout。</summary>
        private static string TryRunCapture(string fileName, string args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                using var p = Process.Start(psi);
                if (p == null)
                {
                    return null;
                }
                string stdout = p.StandardOutput.ReadToEnd();
                _ = p.StandardError.ReadToEnd();
                p.WaitForExit();
                return p.ExitCode == 0 ? stdout : null;
            }
            catch
            {
                return null;
            }
        }
    }
}
