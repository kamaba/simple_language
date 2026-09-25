//****************************************************************************
//  File:      AtSignLabelBuildManager.cs
//  ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/9/25 12:00:00
//  Description:  @<tag>(){} 内联块的导出期构建管理器（语言无关分发，
//                PLUGIN_SYSTEM_DESIGN.md §A20 通用化）。
//                把 AtSignLabelBlockCollector 登记的插件临时代码按块 Label
//                （= 插件 id）分组，分发给对应构建 handler 生成目标库：
//                首期仅 csharp_mono（.NET Framework csc.exe 把全部 C# 条目
//                合并编译为 SLAtSign.dll 并部署到插件 lib 目录，运行期靠
//                assemblies_path 兜底加载）；Source 为空的块不进构建器
//                （插件自管）；无 handler 的标签记告警不中断导出。
//                后续 C/CUDA/Vulkan 插件 handler 按各自工具链在此追加。
//                任何失败仅记日志不中断导出（运行期再告警）。
//****************************************************************************

using SimpleLanguage.Logging;
using SimpleLanguage.Project;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace SimpleLanguage.Export
{
    /// <summary>
    /// @<tag>(){} 块构建管理器：导出 module.json 前，按块 Label 分发
    /// AtSignLabelBlockCollector 登记的临时代码到对应构建 handler。
    /// csharp_mono handler：块内中段代码引用的外部程序集（如 MathUtil）
    /// 需用户自行放到插件 lib 目录或 mono 4.5 GAC。
    /// </summary>
    public static class AtSignLabelBuildManager
    {
        /// <summary>csharp_mono handler（.NET Framework csc 编译 SLAtSign.dll）。</summary>
        private const string CSharpMonoLabel = "csharp_mono";
        private const string CSharpMonoDllName = "SLAtSign.dll";
        /// <summary>csc.exe 路径覆盖环境变量（优先于自动探测）。</summary>
        private const string CscEnv = "SIMPLELANG_CSC";

        public static void Run( string outDir )
        {
            var blocks = SimpleLanguage.Compile.AtSignLabelBlockCollector.Blocks;
            if (blocks.Count == 0 || string.IsNullOrWhiteSpace(outDir))
            {
                return;
            }

            // 按标签分组（Source 空 = 插件自管，不进构建器）
            var groups = new Dictionary<string, List<SimpleLanguage.Compile.AtSignLabelBlock>>(StringComparer.Ordinal);
            foreach (var block in blocks)
            {
                if (string.IsNullOrEmpty(block.Source))
                {
                    continue;
                }
                if (!groups.TryGetValue(block.Label, out var list))
                {
                    list = new List<SimpleLanguage.Compile.AtSignLabelBlock>();
                    groups[block.Label] = list;
                }
                list.Add(block);
            }

            foreach (var pair in groups)
            {
                if (string.Equals(pair.Key, CSharpMonoLabel, StringComparison.OrdinalIgnoreCase))
                {
                    BuildCSharpMono(pair.Value, outDir);
                }
                else
                {
                    // 无 handler 的标签：记告警不中断（该标签条目运行期由
                    // 插件 labelExec capability 自行定位产物或报错）
                    Log.AddIRLog(LID.ExportCscAtSignBuildFailed,
                        "AtSignLabel: no build handler for label '" + pair.Key
                            + "', " + pair.Value.Count + " block(s) skipped (plugin manages its own artifacts)");
                }
            }
        }

        // ------------------------------------------------------------------
        // csharp_mono handler：csc 合并编译全部 C# 条目为 SLAtSign.dll 并部署
        // ------------------------------------------------------------------

        private static void BuildCSharpMono( List<SimpleLanguage.Compile.AtSignLabelBlock> blocks, string outDir )
        {
            var csc = ResolveCsc();
            if (csc == null)
            {
                Log.AddIRLog(LID.ExportCscAtSignCompilerNotFound,
                    "AtSignLabel: csc.exe not found (set " + CscEnv + " to override), skip @csharp_mono build");
                return;
            }

            var tempDir = Path.Combine(Path.GetTempPath(),
                "sl_csharp_mono_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            try
            {
                Directory.CreateDirectory(tempDir);

                // 1) 逐块写 .cs（UTF-8 带 BOM：csc 对无 BOM 文件按 ANSI 读，中文乱码）
                var srcFiles = new List<string>();
                foreach (var block in blocks)
                {
                    var srcPath = Path.Combine(tempDir, block.EntryClassName + ".cs");
                    File.WriteAllText(srcPath, block.Source, new UTF8Encoding(true));
                    srcFiles.Add(srcPath);
                }

                // 2) csc 编译为 SLAtSign.dll（C#5 兼容，v4.0.30319）
                var dllPath = Path.Combine(tempDir, CSharpMonoDllName);
                var args = new StringBuilder();
                args.Append("/target:library /optimize+ /nologo /warn:0 /out:").Append(Quote(dllPath));
                // 引用部署目录中已有的用户程序集（如 SLCSharpTestLib.dll），
                // 块内 import 的命名空间类型才可解析（跳过自身旧版与 native dll）
                var deployDir = ResolvePluginLibDir(outDir);
                if (deployDir != null)
                {
                    foreach (var refDll in Directory.GetFiles(deployDir, "*.dll"))
                    {
                        if (string.Equals(Path.GetFileName(refDll), CSharpMonoDllName, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        try
                        {
                            _ = AssemblyName.GetAssemblyName(refDll);
                        }
                        catch
                        {
                            continue; // native（cvm_csharp_mono / mono-2.0-sgen）等非程序集
                        }
                        args.Append(" /r:").Append(Quote(refDll));
                    }
                }
                foreach (var f in srcFiles)
                {
                    args.Append(' ').Append(Quote(f));
                }
                var output = RunCapture(csc, args.ToString(), tempDir);
                if (output == null || !File.Exists(dllPath))
                {
                    Log.AddIRLog(LID.ExportCscAtSignBuildFailed,
                        "AtSignLabel: csc build failed: " + (output ?? "cannot start process"));
                    return;
                }

                // 3) 部署到 csharp_mono 插件 lib 目录（运行期 assemblies_path 兜底加载）
                if (deployDir == null)
                {
                    Log.AddIRLog(LID.ExportCscAtSignDeployFailed,
                        "AtSignLabel: csharp_mono lib dir not found, skip deploy " + CSharpMonoDllName);
                    return;
                }
                var dstPath = Path.Combine(deployDir, CSharpMonoDllName);
                File.Copy(dllPath, dstPath, overwrite: true);
                Log.AddIRLog(LID.ExportCscAtSignBuildSuccess,
                    "AtSignLabel: build success (" + blocks.Count + " block(s)): " + dstPath);
            }
            finally
            {
                try
                {
                    Directory.Delete(tempDir, true);
                }
                catch
                {
                    // temp 清理失败无害
                }
            }
        }

        // ------------------------------------------------------------------
        // csc 探测：env 覆盖 -> Framework64 v4.0.30319 -> Framework v4.0.30319
        // ------------------------------------------------------------------

        private static string ResolveCsc()
        {
            // 1) 显式覆盖
            var env = Environment.GetEnvironmentVariable(CscEnv);
            if (!string.IsNullOrWhiteSpace(env) && File.Exists(env))
            {
                return env;
            }

            // 2) .NET Framework 自带 csc（64 位优先）
            var windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            foreach (var root in new[]
                     {
                         Path.Combine(windir, "Microsoft.NET", "Framework64"),
                         Path.Combine(windir, "Microsoft.NET", "Framework"),
                     })
            {
                if (string.IsNullOrWhiteSpace(root))
                {
                    continue;
                }
                foreach (var ver in Directory.Exists(root)
                             ? Directory.GetDirectories(root, "v*", SearchOption.TopDirectoryOnly)
                             : Array.Empty<string>())
                {
                    var exe = Path.Combine(ver, "csc.exe");
                    if (File.Exists(exe))
                    {
                        return exe;
                    }
                }
            }
            return null;
        }

        // ------------------------------------------------------------------
        // 部署目录解析：与运行期 CVM 同语义（lib 直给值按包目录 outDir 解析），
        // 兜底从 outDir 逐级上溯找 SLPlugin\csharp_mono\lib\windows-x64
        // ------------------------------------------------------------------

        private static string ResolvePluginLibDir( string outDir )
        {
            // 1) jsonc plugins 段 csharp_mono 的 lib 直给值（如
            //    "../../../../SLPlugin/csharp_mono/lib/windows-x64/cvm_csharp_mono.dll"）：
            //    运行期 CVM 按 module.json 所在包目录解析，这里取同语义解析出 dll 目录
            var plugins = ProjectManager.config?.Plugins;
            if (plugins != null)
            {
                foreach (var p in plugins)
                {
                    if (p == null || !string.Equals(p.Id, CSharpMonoLabel, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    var dir = ResolveLibDirFromValue(p.Lib, outDir);
                    if (dir != null)
                    {
                        return dir;
                    }
                    // 插件目录（Path）下的 lib\windows-x64
                    if (!string.IsNullOrWhiteSpace(p.Path))
                    {
                        var pluginDir = Path.IsPathRooted(p.Path)
                            ? Path.GetFullPath(p.Path)
                            : Path.GetFullPath(Path.Combine(outDir, p.Path));
                        var d = Path.Combine(pluginDir, "lib", "windows-x64");
                        if (Directory.Exists(d))
                        {
                            return d;
                        }
                    }
                }
            }

            // 2) 兜底：从 outDir 逐级上溯找 SLPlugin\csharp_mono\lib\windows-x64
            var cur = outDir;
            while (!string.IsNullOrEmpty(cur))
            {
                var d = Path.Combine(cur, "SLPlugin", "csharp_mono", "lib", "windows-x64");
                if (Directory.Exists(d))
                {
                    return d;
                }
                var parent = Path.GetDirectoryName(cur);
                if (parent == cur)
                {
                    break;
                }
                cur = parent;
            }
            return null;
        }

        private static string ResolveLibDirFromValue( string lib, string outDir )
        {
            if (string.IsNullOrWhiteSpace(lib))
            {
                return null;
            }
            try
            {
                var libPath = Path.IsPathRooted(lib)
                    ? Path.GetFullPath(lib)
                    : Path.GetFullPath(Path.Combine(outDir, lib));
                if (File.Exists(libPath))
                {
                    return Path.GetDirectoryName(libPath);
                }
                if (Directory.Exists(libPath))
                {
                    return libPath;
                }
            }
            catch
            {
                // malformed path
            }
            return null;
        }

        // ------------------------------------------------------------------
        // Process helpers
        // ------------------------------------------------------------------

        private static string Quote( string s )
        {
            return "\"" + s + "\"";
        }

        /// <summary>运行并合并 stdout/stderr；无法启动返回 null（非零退出码仍返回输出，供诊断）。</summary>
        private static string RunCapture( string fileName, string args, string workingDirectory )
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
                return p.ExitCode == 0 ? stdout + "\n" + stderr : "exit=" + p.ExitCode + "\n" + stdout + "\n" + stderr;
            }
            catch
            {
                return null;
            }
        }
    }
}
