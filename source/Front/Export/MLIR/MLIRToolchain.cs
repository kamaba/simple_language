//****************************************************************************
//  File:      MLIRToolchain.cs
// ------------------------------------------------
//  Description: External MLIR/LLVM/MSVC toolchain driver (stage 3).
//  Pipeline (verified end-to-end):
//    aot.mlir
//      --mlir-opt   (pass chain)                    --> aot.opt.mlir
//      --mlir-translate --mlir-to-llvmir            --> aot.ll
//      --llc -filetype=obj                          --> aot.obj
//      --link.exe /DLL /NOENTRY /EXPORT:sym...      --> aot.dll
//  Tool location (fixed probing order, no environment variables — the
//  paths are known at tool-distribution time and never configured):
//    - auto-deploy: on first use copy the needed tool files next to the
//      running exe (EnsureLocalTools), then prefer that local copy
//    - auto-probe: exe directory (auto-deployed tools)
//    - auto-probe: <root>\simple_language\tools\llvm (relocated toolchain)
//    - auto-probe: monorepo layout <root>\llvm-project\build\Release\bin
//    - auto-probe: vswhere (VS with C++ workload), then PATH (link.exe)
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SimpleLanguage.Export.MLIR
{
    public static class MLIRToolchain
    {
        // Verified lowering pass chain (stage 0 + stage 2 validation runs).
        public const string LowerPasses =
            "--canonicalize --convert-arith-to-llvm --convert-cf-to-llvm " +
            "--finalize-memref-to-llvm --convert-func-to-llvm --reconcile-unrealized-casts";

        // GPU lowering pass chain (verified end-to-end on the gpu_build spike:
        // gpu.module -> NVVM -> cubin, host side -> mgpu* launch runtime).
        public const string GpuLowerPasses =
            "--convert-gpu-to-nvvm --gpu-module-to-binary --gpu-to-llvm " +
            "--canonicalize --convert-arith-to-llvm --convert-cf-to-llvm " +
            "--finalize-memref-to-llvm --convert-func-to-llvm --reconcile-unrealized-casts";

        private static string ExeExt => ".exe";

        public sealed class ToolchainPaths
        {
            public string MlirOpt { get; set; } = "mlir-opt";
            public string MlirTranslate { get; set; } = "mlir-translate";
            public string Llc { get; set; } = "llc";
            public string Clang { get; set; } = "clang";
            /// <summary>MSVC link.exe (full path)。程序化覆盖点；null = ResolveLinkExe 走 vswhere/PATH 自动发现。</summary>
            public string? Link { get; set; }

            /// <summary>
            /// 解析外部工具链。路径不通过环境变量配置，按固定顺序探测：
            /// auto-deploy 到 exe 目录（EnsureLocalTools）→ exe 目录副本 →
            /// simple_language\tools\llvm → 旧 monorepo 布局；
            /// link.exe 留空由 ResolveLinkExe 走 vswhere/PATH 自动发现。
            /// </summary>
            public static ToolchainPaths Resolve()
            {
                var t = new ToolchainPaths();

                // Make the toolchain self-contained next to the running exe —
                // copy the tools on first use, then prefer that local copy
                // over the probed source directory.
                EnsureLocalTools();
                var bin = ProbeLocalMlirBin() ?? ProbeMonorepoMlirBin();
                if (bin != null)
                {
                    t.MlirOpt = Path.Combine(bin, "mlir-opt" + ExeExt);
                    t.MlirTranslate = Path.Combine(bin, "mlir-translate" + ExeExt);
                    t.Llc = Path.Combine(bin, "llc" + ExeExt);
                    t.Clang = Path.Combine(bin, "clang" + ExeExt);
                }

                return t;
            }
        }

        /// <summary>
        /// 导出是否保留 AOT 中间产物（aot.mlir / .opt.mlir / .ll / .obj / .exp /
        /// _gpurt.obj）与 module.json 的 "aot.mlir" 溯源字段：Debug 构建保留
        /// （便于事后看 IR 排查）；Release 构建在 aot.dll 构建成功后删除，
        /// 导出目录只留 aot.dll / aot.lib。
        /// </summary>
        internal static bool KeepIntermediateArtifacts
        {
            get
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// Build aot.dll from aot.mlir, exporting the given symbols
        /// (sl_value ABI: (ptr ctx, ptr args, i32 argc, ptr ret) -> i64).
        /// Never throws: returns false with an error message so the caller can
        /// fall back to CVM execution.
        /// </summary>
        /// <param name="gpu">true when the module contains gpu.module kernels:
        /// use the GPU pass chain, strip llvm.global_dtors, compile
        /// sl_gpu_runtime.c with cl.exe and link with /ENTRY:sl_gpu_entry.</param>
        public static bool TryBuildAotDll(
            string mlirFile,
            string dllPath,
            IReadOnlyList<string> exportSymbols,
            out string error,
            ToolchainPaths? tools = null,
            bool gpu = false)
        {
            error = "";
            if (string.IsNullOrWhiteSpace(mlirFile)) { error = "mlir path is empty"; return false; }
            if (!File.Exists(mlirFile)) { error = "mlir file not found: " + mlirFile; return false; }
            if (string.IsNullOrWhiteSpace(dllPath)) { error = "dll path is empty"; return false; }
            if (exportSymbols == null || exportSymbols.Count == 0) { error = "no export symbols"; return false; }

            tools ??= ToolchainPaths.Resolve();

            string workDir = Path.GetDirectoryName(Path.GetFullPath(mlirFile)) ?? Environment.CurrentDirectory;
            Directory.CreateDirectory(workDir);
            string baseName = Path.GetFileNameWithoutExtension(mlirFile);
            string optMlir = Path.Combine(workDir, baseName + ".opt.mlir");
            string llvmIr = Path.Combine(workDir, baseName + ".ll");
            string obj = Path.Combine(workDir, baseName + ".obj");

            try
            {
                string passes = gpu ? GpuLowerPasses : LowerPasses;
                Run(tools.MlirOpt, $"{Quote(mlirFile)} {passes} -o {Quote(optMlir)}", workDir);
                Run(tools.MlirTranslate, $"{Quote(optMlir)} --mlir-to-llvmir -o {Quote(llvmIr)}", workDir);
                if (gpu) StripGlobalDtors(llvmIr);
                Run(tools.Llc, $"{Quote(llvmIr)} -filetype=obj -o {Quote(obj)}", workDir);

                string? link = ResolveLinkExe(tools);
                if (link == null)
                {
                    error = "MSVC link.exe not found (install VS C++ workload or VS Build Tools)";
                    return false;
                }

                var args = new StringBuilder();
                if (gpu)
                {
                    // ---- GPU dll: link the CUDA staging runtime, use the
                    // runtime's custom entry (lazy ctor execution, no CRT
                    // startup: llvm.global_ctors was stripped from the .ll).
                    string? rtC = LocateGpuRuntimeC();
                    if (rtC == null)
                    {
                        error = "sl_gpu_runtime.c not found (set SIMPLELANG_GPU_RUNTIME)";
                        return false;
                    }
                    string? cl = ResolveClExe(link);
                    if (cl == null)
                    {
                        error = "cl.exe not found next to link.exe: " + link;
                        return false;
                    }
                    string rtObj = Path.Combine(workDir, baseName + "_gpurt.obj");
                    var env = BuildMsvcEnv(link);
                    Run(cl, $"/nologo /c /O2 {Quote(rtC)} /Fo:{Quote(rtObj)}", workDir, env);

                    args.Append("/nologo /DLL /INCREMENTAL:NO /ENTRY:sl_gpu_entry /OUT:").Append(Quote(dllPath))
                        .Append(' ').Append(Quote(obj))
                        .Append(' ').Append(Quote(rtObj))
                        .Append(" kernel32.lib msvcrt.lib ucrt.lib libvcruntime.lib");
                    foreach (var s in exportSymbols)
                        args.Append(" /EXPORT:").Append(s);

                    Run(link, args.ToString(), workDir, env);
                    if (!KeepIntermediateArtifacts)
                        DeleteIntermediateArtifacts(mlirFile, dllPath);
                    return true;
                }

                // libvcruntime.lib: static memcpy (LLVM lowers
                // llvm.intr.memcpy to a call); pure static lib, keeps the
                // /NOENTRY no-CRT-startup design intact.
                args.Append("/nologo /DLL /NOENTRY /OUT:").Append(Quote(dllPath))
                    .Append(' ').Append(Quote(obj))
                    .Append(" libvcruntime.lib");
                foreach (var s in exportSymbols)
                    args.Append(" /EXPORT:").Append(s);

                Run(link, args.ToString(), workDir);
                if (!KeepIntermediateArtifacts)
                    DeleteIntermediateArtifacts(mlirFile, dllPath);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Release 导出：aot.dll 构建成功后删除工具链中间产物
        /// （.mlir / .opt.mlir / .ll / .obj / .exp / _gpurt.obj），
        /// 导出目录只保留 aot.dll / aot.lib。单个文件删除失败不影响导出结果。
        /// </summary>
        private static void DeleteIntermediateArtifacts(string mlirFile, string dllPath)
        {
            string workDir = Path.GetDirectoryName(Path.GetFullPath(mlirFile))
                ?? Environment.CurrentDirectory;
            string baseName = Path.GetFileNameWithoutExtension(mlirFile);
            string[] intermediates =
            {
                mlirFile,
                Path.Combine(workDir, baseName + ".opt.mlir"),
                Path.Combine(workDir, baseName + ".ll"),
                Path.Combine(workDir, baseName + ".obj"),
                Path.Combine(workDir, baseName + "_gpurt.obj"),
                Path.ChangeExtension(dllPath, ".exp"),
            };
            foreach (var f in intermediates)
            {
                try
                {
                    if (File.Exists(f)) File.Delete(f);
                }
                catch
                {
                    // 清理失败不影响导出
                }
            }
        }

        /// <summary>Legacy single-target lowering (mlir -> exe or obj). Pass chain fixed in stage 3.</summary>
        public static void LowerToNative(string mlirFile, string outputExeOrObj, ToolchainPaths? tools = null)
        {
            if (string.IsNullOrWhiteSpace(mlirFile)) throw new ArgumentNullException(nameof(mlirFile));
            if (!File.Exists(mlirFile)) throw new FileNotFoundException(mlirFile);
            if (string.IsNullOrWhiteSpace(outputExeOrObj)) throw new ArgumentNullException(nameof(outputExeOrObj));

            tools ??= ToolchainPaths.Resolve();

            string workDir = Path.GetDirectoryName(Path.GetFullPath(mlirFile)) ?? Environment.CurrentDirectory;
            string baseName = Path.GetFileNameWithoutExtension(mlirFile);
            string loweredMlir = Path.Combine(workDir, baseName + ".lowered.mlir");
            string llvmIr = Path.Combine(workDir, baseName + ".ll");
            string obj = Path.Combine(workDir, baseName + ".o");

            Run(tools.MlirOpt, $"{Quote(mlirFile)} {LowerPasses} -o {Quote(loweredMlir)}", workDir);
            Run(tools.MlirTranslate, $"{Quote(loweredMlir)} --mlir-to-llvmir -o {Quote(llvmIr)}", workDir);
            Run(tools.Llc, $"{Quote(llvmIr)} -filetype=obj -o {Quote(obj)}", workDir);

            var ext = Path.GetExtension(outputExeOrObj);
            if (string.Equals(ext, ".o", StringComparison.OrdinalIgnoreCase)
                || string.Equals(ext, ".obj", StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(obj, outputExeOrObj, true);
                return;
            }

            Run(tools.Clang, $"{Quote(obj)} -o {Quote(outputExeOrObj)}", workDir);
        }

        // ------------------------------------------------------------------
        // Tool discovery
        // ------------------------------------------------------------------

        /// <summary>
        /// Tool-directory probe: walk up from the exe directory, at each
        /// ancestor checking (in order)
        ///   1. &lt;root&gt;\simple_language\tools\llvm\mlir-opt.exe
        ///      (relocated toolchain: the llvm-project monorepo is no longer
        ///      part of the checkout; only the 4 exes + the f16 utils dll
        ///      live there)
        ///   2. &lt;root&gt;\llvm-project\build\Release\bin\mlir-opt.exe
        ///      (legacy monorepo layout, kept for older checkouts)
        /// The relative form of (1) also matches when the front-end runs from
        /// a sibling of simple_language (e.g. lang\build\...).
        /// </summary>
        private static string? ProbeMonorepoMlirBin()
        {
            try
            {
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                for (var d = dir; d != null; d = d.Parent)
                {
                    var tools = Path.Combine(d.FullName, "simple_language", "tools", "llvm");
                    if (File.Exists(Path.Combine(tools, "mlir-opt" + ExeExt)))
                        return tools;
                    var candidate = Path.Combine(d.FullName, "llvm-project", "build", "Release", "bin");
                    if (File.Exists(Path.Combine(candidate, "mlir-opt" + ExeExt)))
                        return candidate;
                }
            }
            catch
            {
                // ignore probing errors
            }
            return null;
        }

        /// <summary>
        /// AOT 管线实际用到的 MLIR 工具文件（EnsureLocalTools 拷到 exe 目录的就是
        /// 这几个；mlir-transform-opt.exe 当前未被管线使用，不在此列）。
        /// </summary>
        private static readonly string[] LocalToolFiles =
        {
            "mlir-opt" + ExeExt,
            "mlir-translate" + ExeExt,
            "llc" + ExeExt,
            "mlir_float16_utils.dll",
        };

        /// <summary>
        /// 确保工具随 exe 部署：把用到的 MLIR 工具从探测到的工具目录
        /// （simple_language\tools\llvm 或旧 monorepo 布局）拷到 exe 所在目录
        /// （AppContext.BaseDirectory），让编译器不依赖源码树即可完成 AOT 构建。
        /// 规则：已存在的不覆盖（升级工具需手动删除 exe 目录下的旧文件）；
        /// 源目录里没有的跳过；任何失败静默跳过（工具链回退到探测目录）。
        /// 返回本次实际拷贝的文件数（幂等，已全部就位时为 0）。
        /// </summary>
        public static int EnsureLocalTools()
        {
            try
            {
                string exeDir = AppContext.BaseDirectory;
                string? src = ProbeMonorepoMlirBin();
                if (src == null)
                    return 0; // no source tree to copy from (standalone deploy)
                if (string.Equals(
                        Path.TrimEndingDirectorySeparator(Path.GetFullPath(src)),
                        Path.TrimEndingDirectorySeparator(Path.GetFullPath(exeDir)),
                        StringComparison.OrdinalIgnoreCase))
                    return 0; // already running from the tools directory itself

                int copied = 0;
                foreach (var f in LocalToolFiles)
                {
                    var dst = Path.Combine(exeDir, f);
                    if (File.Exists(dst)) continue; // present -> keep (no overwrite)
                    var from = Path.Combine(src, f);
                    if (!File.Exists(from)) continue;
                    File.Copy(from, dst, overwrite: false);
                    copied++;
                }
                return copied;
            }
            catch
            {
                return 0; // never let the deploy step break the export pipeline
            }
        }

        /// <summary>已随 exe 部署的工具目录（EnsureLocalTools 拷贝产物所在）。</summary>
        private static string? ProbeLocalMlirBin()
        {
            try
            {
                var dir = AppContext.BaseDirectory;
                if (File.Exists(Path.Combine(dir, "mlir-opt" + ExeExt)))
                    return dir;
            }
            catch
            {
                // ignore probing errors
            }
            return null;
        }

        private static string? ResolveLinkExe(ToolchainPaths tools)
        {
            if (!string.IsNullOrWhiteSpace(tools.Link) && File.Exists(tools.Link))
                return tools.Link;

            // 1) vswhere: latest VS with the C++ x64/x86 toolset
            var vswhere = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Microsoft Visual Studio", "Installer", "vswhere.exe");
            if (File.Exists(vswhere))
            {
                var install = TryRunCapture(vswhere,
                    "-latest -products * -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath");
                if (!string.IsNullOrWhiteSpace(install))
                {
                    var msvcRoot = Path.Combine(install.Trim(), "VC", "Tools", "MSVC");
                    if (Directory.Exists(msvcRoot))
                    {
                        var link = Directory.GetDirectories(msvcRoot)
                            .Select(d => Path.Combine(d, "bin", "Hostx64", "x64", "link.exe"))
                            .Where(File.Exists)
                            .OrderByDescending(p => p, StringComparer.OrdinalIgnoreCase)
                            .FirstOrDefault();
                        if (link != null)
                            return link;
                    }
                }
            }

            // 2) PATH (e.g. VS developer prompt)
            var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in pathVar.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    var p = Path.Combine(dir.Trim(), "link" + ExeExt);
                    if (File.Exists(p))
                        return p;
                }
                catch
                {
                    // malformed PATH entry
                }
            }

            return null;
        }

        // ------------------------------------------------------------------
        // GPU build helpers
        // ------------------------------------------------------------------

        /// <summary>
        /// Remove the llvm.global_dtors entries from the translated .ll.
        /// The GPU dll uses /ENTRY:sl_gpu_entry and executes llvm.global_ctors
        /// lazily on the first runtime call; the matching dtor entries (kernel
        /// module unload) would fire through the .CRT walk and crash.
        /// </summary>
        private static void StripGlobalDtors(string llvmIrFile)
        {
            string ll = File.ReadAllText(llvmIrFile);
            string stripped = Regex.Replace(ll,
                @"@llvm\.global_dtors = appending global \[\d+ x \{ i32, ptr, ptr \}\] \[\{[^\n]*\n",
                "");
            if (!ReferenceEquals(stripped, ll) && stripped != ll)
                File.WriteAllText(llvmIrFile, stripped);
        }

        /// <summary>
        /// Locate sl_gpu_runtime.c (mgpu* + slgpu* CUDA staging runtime).
        /// SIMPLELANG_GPU_RUNTIME env override, else walk up from the exe
        /// directory looking for the repo copies.
        /// </summary>
        private static string? LocateGpuRuntimeC()
        {
            var env = Environment.GetEnvironmentVariable("SIMPLELANG_GPU_RUNTIME");
            if (!string.IsNullOrWhiteSpace(env) && File.Exists(env)) return env;

            try
            {
                var rels = new[]
                {
                    Path.Combine("simple_language", "source", "Front", "Export", "MLIR", "sl_gpu_runtime.c"),
                    Path.Combine("simple_language", "test", "SpecialTest", "gpu_build", "sl_gpu_runtime.c"),
                };
                for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                {
                    foreach (var rel in rels)
                    {
                        var p = Path.Combine(d.FullName, rel);
                        if (File.Exists(p)) return p;
                    }
                }
            }
            catch
            {
                // ignore probing errors
            }
            return null;
        }

        /// <summary>cl.exe sits in the same bin\Hostx64\x64 directory as link.exe.</summary>
        private static string? ResolveClExe(string linkExe)
        {
            try
            {
                var cl = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(linkExe)) ?? "", "cl" + ExeExt);
                return File.Exists(cl) ? cl : null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Build INCLUDE/LIB for cl.exe + link.exe (MSVC toolset include/lib +
        /// the latest Windows 10 SDK), so the toolchain works outside a
        /// developer prompt.
        /// </summary>
        private static Dictionary<string, string> BuildMsvcEnv(string linkExe)
        {
            var includes = new List<string>();
            var libs = new List<string>();

            // link.exe: <MSVC>\bin\Hostx64\x64\link.exe
            string linkDir = Path.GetDirectoryName(Path.GetFullPath(linkExe)) ?? "";
            string? msvcRoot = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(linkDir)));
            if (!string.IsNullOrEmpty(msvcRoot) && Directory.Exists(Path.Combine(msvcRoot, "include")))
            {
                includes.Add(Path.Combine(msvcRoot, "include"));
                libs.Add(Path.Combine(msvcRoot, "lib", "x64"));
            }

            foreach (var pf in new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            })
            {
                if (string.IsNullOrWhiteSpace(pf)) continue;
                string sdkInc = Path.Combine(pf, "Windows Kits", "10", "Include");
                string sdkLib = Path.Combine(pf, "Windows Kits", "10", "Lib");
                if (!Directory.Exists(sdkInc) || !Directory.Exists(sdkLib)) continue;

                var ver = Directory.GetDirectories(sdkInc)
                    .OrderByDescending(p => p, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (ver == null) continue;

                foreach (var sub in new[] { "ucrt", "um", "shared", "winrt", "cppwinrt" })
                {
                    var d = Path.Combine(ver, sub);
                    if (Directory.Exists(d)) includes.Add(d);
                }
                var libVer = Path.Combine(sdkLib, Path.GetFileName(ver));
                libs.Add(Path.Combine(libVer, "ucrt", "x64"));
                libs.Add(Path.Combine(libVer, "um", "x64"));
                break;
            }

            var env = new Dictionary<string, string>();
            if (includes.Count > 0) env["INCLUDE"] = string.Join(";", includes);
            if (libs.Count > 0) env["LIB"] = string.Join(";", libs);
            return env;
        }

        private static string? TryRunCapture(string fileName, string args)
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
                if (p == null) return null;
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

        // ------------------------------------------------------------------
        // Process helpers
        // ------------------------------------------------------------------

        private static string Quote(string s) => "\"" + s + "\"";

        private static void Run(string fileName, string args, string workingDirectory,
            Dictionary<string, string>? extraEnv = null)
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
            if (extraEnv != null)
            {
                foreach (var kv in extraEnv)
                    psi.EnvironmentVariables[kv.Key] = kv.Value;
            }
            using var p = Process.Start(psi);
            if (p == null) throw new InvalidOperationException("Failed to start process: " + fileName);

            string stdout = p.StandardOutput.ReadToEnd();
            string stderr = p.StandardError.ReadToEnd();
            p.WaitForExit();

            if (p.ExitCode != 0)
            {
                throw new InvalidOperationException($"Tool failed: {fileName} {args}\n{stdout}\n{stderr}");
            }
        }
    }
}
