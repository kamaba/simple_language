//****************************************************************************
//  File:      MLIRExporter.cs
// ------------------------------------------------
//  Description: Export SimpleLanguage IR to MLIR (skeleton)
//****************************************************************************

using System;
using System.IO;
using SimpleLanguage.IR;
using System.Text;

namespace SimpleLanguage.Export.MLIR
{
    public static class MLIRExporter
    {
        public sealed class ExportOptions
        {
            public bool RunToolchain { get; set; } = false;
            public string? NativeOutputPath { get; set; }
        }

        public static void ExportToFile(IRMethod method, string outputPath)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentNullException(nameof(outputPath));

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

            var sb = new StringBuilder();
            sb.AppendLine("module {");
            sb.Append("  func.func @");
            sb.Append(SanitizeSymbol(method.onlyFunctionName));
            sb.AppendLine("() {");
            sb.AppendLine("    ^entry:");

            // Display-only export: list IR instructions as comments so it's inspectable without MLIR libs.
            // Later you can map each EIROpCode to real MLIR ops/dialects.
            if (method.IRDataList != null)
            {
                for (int i = 0; i < method.IRDataList.Count; i++)
                {
                    var ir = method.IRDataList[i];
                    sb.Append("    // ");
                    sb.Append(i);
                    sb.Append(" ");
                    sb.Append(ir?.ToString());
                    sb.AppendLine();
                }
            }

            sb.AppendLine("    return");
            sb.AppendLine("  }");
            sb.AppendLine("}");

            File.WriteAllText(outputPath, sb.ToString());
        }

        public static void ExportAndOptionallyLower(IRMethod method, string mlirOutputPath, ExportOptions? options)
        {
            ExportToFile(method, mlirOutputPath);

            if (options?.RunToolchain == true)
            {
                if (string.IsNullOrWhiteSpace(options.NativeOutputPath))
                {
                    throw new ArgumentException("NativeOutputPath is required when RunToolchain is true", nameof(options));
                }

                MLIRToolchain.LowerToNative(mlirOutputPath, options.NativeOutputPath);
            }
        }

        private static string SanitizeSymbol(string name)
        {
            if (string.IsNullOrEmpty(name)) return "unknown";
            var sb = new StringBuilder(name.Length);
            for (int i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
                else sb.Append('_');
            }
            return sb.ToString();
        }
    }
}
