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
//  Tool location:
//    - SIMPLELANG_MLIR_BIN   env: directory of mlir-opt/mlir-translate/llc
//    - SIMPLELANG_MSVC_LINK  env: full path to MSVC link.exe
//    - auto-probe: monorepo layout <root>\llvm-project\build\Release\bin
//    - auto-probe: vswhere (VS with C++ workload), then PATH
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
            /// <summary>MSVC link.exe (full path). Null/empty = auto-resolve.</summary>
            public string? Link { get; set; }

            public static ToolchainPaths FromEnvironment()
            {
                var t = new ToolchainPaths();

                var bin = Environment.GetEnvironmentVariable("SIMPLELANG_MLIR_BIN");
                if (string.IsNullOrWhiteSpace(bin))
                    bin = ProbeMonorepoMlirBin();
                if (!string.IsNullOrWhiteSpace(bin))
                {
                    t.MlirOpt = Path.Combine(bin, "mlir-opt" + ExeExt);
                    t.MlirTranslate = Path.Combine(bin, "mlir-translate" + ExeExt);
                    t.Llc = Path.Combine(bin, "llc" + ExeExt);
                    t.Clang = Path.Combine(bin, "clang" + ExeExt);
                }

                var link = Environment.GetEnvironmentVariable("SIMPLELANG_MSVC_LINK");
                if (!string.IsNullOrWhiteSpace(link))
                    t.Link = link;

                return t;
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

            tools ??= ToolchainPaths.FromEnvironment();

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
                    error = "MSVC link.exe not found (set SIMPLELANG_MSVC_LINK or install VS C++ tools)";
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
                        .Append(" kernel32.lib msvcrt.lib ucrt.lib");
                    foreach (var s in exportSymbols)
                        args.Append(" /EXPORT:").Append(s);

                    Run(link, args.ToString(), workDir, env);
                    return true;
                }

                args.Append("/nologo /DLL /NOENTRY /OUT:").Append(Quote(dllPath))
                    .Append(' ').Append(Quote(obj));
                foreach (var s in exportSymbols)
                    args.Append(" /EXPORT:").Append(s);

                Run(link, args.ToString(), workDir);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>Legacy single-target lowering (mlir -> exe or obj). Pass chain fixed in stage 3.</summary>
        public static void LowerToNative(string mlirFile, string outputExeOrObj, ToolchainPaths? tools = null)
        {
            if (string.IsNullOrWhiteSpace(mlirFile)) throw new ArgumentNullException(nameof(mlirFile));
            if (!File.Exists(mlirFile)) throw new FileNotFoundException(mlirFile);
            if (string.IsNullOrWhiteSpace(outputExeOrObj)) throw new ArgumentNullException(nameof(outputExeOrObj));

            tools ??= ToolchainPaths.FromEnvironment();

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
        /// Monorepo layout probe: walk up from the exe directory looking for
        /// &lt;root&gt;\llvm-project\build\Release\bin\mlir-opt.exe.
        /// </summary>
        private static string? ProbeMonorepoMlirBin()
        {
            try
            {
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                for (var d = dir; d != null; d = d.Parent)
                {
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
