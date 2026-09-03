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

namespace SimpleLanguage.Export.MLIR
{
    public static class MLIRToolchain
    {
        // Verified lowering pass chain (stage 0 + stage 2 validation runs).
        public const string LowerPasses =
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
        public static bool TryBuildAotDll(
            string mlirFile,
            string dllPath,
            IReadOnlyList<string> exportSymbols,
            out string error,
            ToolchainPaths? tools = null)
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
                Run(tools.MlirOpt, $"{Quote(mlirFile)} {LowerPasses} -o {Quote(optMlir)}", workDir);
                Run(tools.MlirTranslate, $"{Quote(optMlir)} --mlir-to-llvmir -o {Quote(llvmIr)}", workDir);
                Run(tools.Llc, $"{Quote(llvmIr)} -filetype=obj -o {Quote(obj)}", workDir);

                string? link = ResolveLinkExe(tools);
                if (link == null)
                {
                    error = "MSVC link.exe not found (set SIMPLELANG_MSVC_LINK or install VS C++ tools)";
                    return false;
                }

                var args = new StringBuilder();
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

        private static void Run(string fileName, string args, string workingDirectory)
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
