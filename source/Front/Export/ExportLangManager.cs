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
using SimpleLanguage.Logging;
using SimpleLanguage.Project;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

            // Unified JSON export (VM symmetric)
            string exportIRPath = Path.Combine(outDir, filePrefix + ".module.json");
            SLModulePackageWriter.Write(IRManager.instance, exportIRPath, moduleName);

            // AOT: 收集 @AOT() 标记的方法，导出模块级 aot.mlir（module.json 照常导出作回退保底）
            ExportAotModule(outDir);

            //// Optional: keep binary writer for debugging/back-compat
            //if (Environment.GetEnvironmentVariable("SIMPLELANG_SLIR_BINARY") == "1")
            //{
            //    SLIRWriter.WriteModule(IRManager.instance, Path.Combine(outDir, "module.slir"));
            //}
        }

        /// <summary>
        /// AOT 导出：收集 @AOT() 标记的方法并导出为模块级 aot.mlir。
        /// 阶段1筛选规则：非模板静态成员函数（isAot && isStatic && !isTemplateFunction）。
        /// 设置 SIMPLELANG_AOT=0 可强制关闭（回退到纯 CVM 执行 module.json）。
        /// </summary>
        private static void ExportAotModule(string outDir)
        {
            if (Environment.GetEnvironmentVariable("SIMPLELANG_AOT") == "0")
                return;

            var candidates = new List<IRMethod>();
            var skipped = new List<string>();
            foreach (var kv in IRManager.instance.IRMethodDict)
            {
                var m = kv.Value;
                if (m == null || !m.isAot) continue;
                if (!m.isStatic || m.isTemplateFunction)
                {
                    skipped.Add(m.id);
                    continue;
                }
                candidates.Add(m);
            }

            if (candidates.Count == 0 && skipped.Count == 0)
                return;

            foreach (var s in skipped)
            {
                Log.AddIRLog(LID.ShowExtendMessage,
                    $"AOT: skip '{s}' (stage1 supports static non-template only)");
            }
            Log.AddIRLog(LID.ShowExtendMessage,
                $"AOT: {candidates.Count} candidate(s) collected from module '{ResolveModuleName()}'");
            foreach (var m in candidates)
            {
                Log.AddIRLog(LID.ShowExtendMessage, $"AOT: candidate '{m.id}'");
            }

            if (candidates.Count == 0)
                return;

            var aotMlirPath = Path.Combine(outDir, "aot.mlir");
            var result = MLIRExporter.ExportModuleToFile(candidates, aotMlirPath);
            Log.AddIRLog(LID.ShowExtendMessage,
                $"AOT: export aot.mlir success: {aotMlirPath} ({result.OkSymbols.Count} ok, {result.FailedIds.Count} failed)");
            foreach (var failedId in result.FailedIds)
            {
                Log.AddIRLog(LID.ShowExtendMessage,
                    $"AOT: method emit failed (see aot_manifest.json): {failedId}");
            }

            if (result.OkSymbols.Count == 0)
                return;

            // Stage 3: lower aot.mlir to aot.dll (fallback to CVM on any failure).
            if (Environment.GetEnvironmentVariable("SIMPLELANG_AOT_DLL") == "0")
            {
                Log.AddIRLog(LID.ShowExtendMessage, "AOT: aot.dll build disabled (SIMPLELANG_AOT_DLL=0)");
                return;
            }

            var aotDllPath = Path.Combine(outDir, "aot.dll");
            // Stage-5 reverse bridge: the module references @sl_aot_bridge_init,
            // so it must be exported from aot.dll (only when actually emitted).
            var exportSymbols = result.NeedsBridgeInit
                ? result.OkSymbols.Concat(new[] { "sl_aot_bridge_init" }).ToArray()
                : (IReadOnlyList<string>)result.OkSymbols;
            if (MLIRToolchain.TryBuildAotDll(aotMlirPath, aotDllPath, exportSymbols, out var dllError))
            {
                MLIRExporter.SetManifestDll(aotMlirPath, Path.GetFileName(aotDllPath));
                Log.AddIRLog(LID.ShowExtendMessage, "AOT: build aot.dll success: " + aotDllPath);
            }
            else
            {
                Log.AddIRLog(LID.ShowExtendMessage,
                    "AOT: build aot.dll failed, fallback to CVM: " + dllError);
            }
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