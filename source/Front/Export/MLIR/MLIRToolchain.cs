using System;
using System.Diagnostics;
using System.IO;

namespace SimpleLanguage.Export.MLIR
{
    // Uses external MLIR/LLVM tools (mlir-opt/mlir-translate/llc/clang) to lower .mlir to native.
    // This avoids embedding MLIR libraries in the C# process.
    public static class MLIRToolchain
    {
        public sealed class ToolchainPaths
        {
            public string MlirOpt { get; set; } = "mlir-opt";
            public string MlirTranslate { get; set; } = "mlir-translate";
            public string Llc { get; set; } = "llc";
            public string Clang { get; set; } = "clang";

            public static ToolchainPaths FromEnvironment()
            {
                var bin = Environment.GetEnvironmentVariable("SIMPLELANG_MLIR_BIN");
                if (string.IsNullOrWhiteSpace(bin)) return new ToolchainPaths();
                return new ToolchainPaths
                {
                    MlirOpt = Path.Combine(bin, "mlir-opt"),
                    MlirTranslate = Path.Combine(bin, "mlir-translate"),
                    Llc = Path.Combine(bin, "llc"),
                    Clang = Path.Combine(bin, "clang"),
                };
            }
        }

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

            Run(tools.MlirOpt, $"\"{mlirFile}\" -o \"{loweredMlir}\"", workDir);
            Run(tools.MlirTranslate, $"\"{loweredMlir}\" --mlir-to-llvmir -o \"{llvmIr}\"", workDir);
            Run(tools.Llc, $"\"{llvmIr}\" -filetype=obj -o \"{obj}\"", workDir);

            // If output path ends with .o/.obj, just copy object.
            var ext = Path.GetExtension(outputExeOrObj);
            if (string.Equals(ext, ".o", StringComparison.OrdinalIgnoreCase) || string.Equals(ext, ".obj", StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(obj, outputExeOrObj, true);
                return;
            }

            Run(tools.Clang, $"\"{obj}\" -o \"{outputExeOrObj}\"", workDir);
        }

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
