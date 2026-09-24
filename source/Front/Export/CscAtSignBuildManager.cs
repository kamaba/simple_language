//****************************************************************************
//  File:      CscAtSignBuildManager.cs
//  ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2026/9/24 12:00:00
//  Description:  @csharp_mono(){} 内联块的导出期 csc 编译管理器：
//                把 CSharpMonoBlockCollector 登记的全部 C# 入口源码用
//                .NET Framework csc.exe 编译为 SLAtSign.dll 并部署到
//                csharp_mono 插件 lib 目录（运行期靠 assemblies_path 兜底
//                加载，CSharpCallXxx 按 "SLAtSign.dll" 裸名命中）。
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
    /// cscAtSign 构建管理器：
    /// 导出 module.json 前，把改写器登记的 @csharp_mono(){} 块体 C# 源码
    /// （namespace SLAtSign.Entry_N）合并编译为 SLAtSign.dll（/target:library），
    /// 拷到 csharp_mono 插件 lib 目录。块内中段代码引用的外部程序集
    /// （如 MathUtil）需用户自行放到同目录或 mono 4.5 GAC。
    /// </summary>
    public static class CscAtSignBuildManager
    {
        private const string PluginId = "csharp_mono";
        private const string DllName = "SLAtSign.dll";
        /// <summary>csc.exe 路径覆盖环境变量（优先于自动探测）。</summary>
        private const string CscEnv = "SIMPLELANG_CSC";

        public static void Run( string outDir )
        {
            var blocks = SimpleLanguage.Compile.CSharpMonoBlockCollector.Blocks;
            if (blocks.Count == 0 || string.IsNullOrWhiteSpace(outDir))
            {
                return;
            }

            var csc = ResolveCsc();
            if (csc == null)
            {
                Log.AddIRLog(LID.ExportCscAtSignCompilerNotFound,
                    "cscAtSign: csc.exe not found (set " + CscEnv + " to override), skip @csharp_mono build");
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
                    File.WriteAllText(srcPath, block.CSharpSource, new UTF8Encoding(true));
                    srcFiles.Add(srcPath);
                }

                // 2) csc 编译为 SLAtSign.dll（C#5 兼容，v4.0.30319）
                var dllPath = Path.Combine(tempDir, DllName);
                var args = new StringBuilder();
                args.Append("/target:library /optimize+ /nologo /warn:0 /out:").Append(Quote(dllPath));
                // 引用部署目录中已有的用户程序集（如 SLCSharpTestLib.dll），
                // 块内 import 的命名空间类型才可解析（跳过自身旧版与 native dll）
                var deployDir = ResolvePluginLibDir(outDir);
                if (deployDir != null)
                {
                    foreach (var refDll in Directory.GetFiles(deployDir, "*.dll"))
                    {
                        if (string.Equals(Path.GetFileName(refDll), DllName, StringComparison.OrdinalIgnoreCase))
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
                        "cscAtSign: csc build failed: " + (output ?? "cannot start process"));
                    return;
                }

                // 3) 部署到 csharp_mono 插件 lib 目录（运行期 assemblies_path 兜底加载）
                if (deployDir == null)
                {
                    Log.AddIRLog(LID.ExportCscAtSignDeployFailed,
                        "cscAtSign: csharp_mono lib dir not found, skip deploy " + DllName);
                    return;
                }
                var dstPath = Path.Combine(deployDir, DllName);
                File.Copy(dllPath, dstPath, overwrite: true);
                Log.AddIRLog(LID.ExportCscAtSignBuildSuccess,
                    "cscAtSign: build success (" + blocks.Count + " block(s)): " + dstPath);
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
                    if (p == null || !string.Equals(p.Id, PluginId, StringComparison.OrdinalIgnoreCase))
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
