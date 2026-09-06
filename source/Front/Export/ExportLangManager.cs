//****************************************************************************
//  File:      ExportLangManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description:  manager Export other lanuage or il etc.
//****************************************************************************

using SimpleLanguage.Export.MLIR;
using SimpleLanguage.Export.SLIR;
using SimpleLanguage.IR;
using SimpleLanguage.Project;
using System;
using System.IO;

namespace SimpleLanguage.ExportLanguage
{
    public enum ExportKind
    {
        LLVM,
        MLIR,
        SLIR,
    }
    public class ExportLangManager
    {

        // Explicit export entrypoint.
        // Use env vars to avoid changing default compile flow:
        // - SIMPLELANG_EXPORT_KIND=llvm|mlir|slir
        // - SIMPLELANG_EXPORT_OUTDIR=... (optional)
        // - SIMPLELANG_MLIR_LOWER=1 (optional; requires external toolchain)
        // - SIMPLELANG_MLIR_NATIVE_OUT=... (optional)
        public static void Export(ExportKind kink)
        {
            var outDir = ResolveOutDir();
            Directory.CreateDirectory(outDir);

            var filePrefix = ResolveProjectNamePrefix();
            var moduleName = ResolveModuleName();

            // AOT 导出必须先于 module.json：stage 1-3 的结果（mlir/dll/methods manifest）
            // 会被 SLModulePackageWriter.Build 合并进 module.json 的 "aot" 字段。
            // 管线细节全部封装在 Export/MLIR 目录（MLIRExportManager）。
            MLIRExportManager.Instance.Run(outDir);

            // Unified JSON export (VM symmetric)
            string exportIRPath = Path.Combine(outDir, filePrefix + ".module.json");
            SLModulePackageWriter.Write(IRManager.instance, exportIRPath, moduleName);

            //// Optional: keep binary writer for debugging/back-compat
            //if (Environment.GetEnvironmentVariable("SIMPLELANG_SLIR_BINARY") == "1")
            //{
            //    SLIRWriter.WriteModule(IRManager.instance, Path.Combine(outDir, "module.slir"));
            //}
        }

        private static string ResolveOutDir()
        {
            // 1) explicit env override
            var outDir = Environment.GetEnvironmentVariable("SIMPLELANG_EXPORT_OUTDIR");
            if (!string.IsNullOrWhiteSpace(outDir))
                return outDir;

            // 2) project config export.outputDir + export.moduleName (when env not preset)
            var fromCfg = ProjectOutputEnvironment.ResolveExportDirectoryFromConfig(
                ProjectManager.config,
                !string.IsNullOrWhiteSpace(ProjectManager.projectPath) ? ProjectManager.projectPath : Environment.CurrentDirectory,
                Path.GetFileNameWithoutExtension(ProjectManager.projectPath) ?? "module");
            if (!string.IsNullOrWhiteSpace(fromCfg))
            {
                outDir = fromCfg;
                Directory.CreateDirectory(outDir);
                Environment.SetEnvironmentVariable("SIMPLELANG_EXPORT_OUTDIR", outDir);
                return outDir;
            }

            // 3) legacy fallback
            outDir = Path.Combine(Environment.CurrentDirectory, "out", "export");
            Environment.SetEnvironmentVariable("SIMPLELANG_EXPORT_OUTDIR", outDir);
            return outDir;
        }

        private static string ResolveProjectNamePrefix()
        {
            var name = ProjectManager.config?.Project?.Name;
            if (string.IsNullOrWhiteSpace(name))
                name = "module";
            return SanitizeFileName(name);
        }

        private static string ResolveModuleName()
        {
            var exportModuleName = ProjectManager.config?.Export?.ModuleName;
            if (!string.IsNullOrWhiteSpace(exportModuleName))
                return exportModuleName;

            var projectName = ProjectManager.config?.Project?.Name;
            if (!string.IsNullOrWhiteSpace(projectName))
                return projectName;

            return "SimpleLanguage";
        }

        private static string SanitizeFileName(string value)
        {
            var invalid = Path.GetInvalidFileNameChars();
            var chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (Array.IndexOf(invalid, chars[i]) >= 0)
                    chars[i] = '_';
            }
            return new string(chars);
        }
    }
}