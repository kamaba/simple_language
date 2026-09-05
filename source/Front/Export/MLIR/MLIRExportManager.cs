//****************************************************************************
//  File:      MLIRExportManager.cs
// ------------------------------------------------
//  Description: Unified entrypoint for the MLIR AOT export pipeline.
//  Everything MLIR/AOT related (env-var switches, stage-1 candidate
//  selection, stage-2 emission, stage-3 dll build, manifest bookkeeping)
//  is encapsulated in this directory:
//
//    MLIRExportConfig   - environment/config switches (SIMPLELANG_*)
//    MLIRExportManager  - orchestration + public AOT query interfaces
//    MLIRExporter       - stage-2 MLIR emission (per-method)
//    MLIRToolchain      - stage-3 external toolchain (mlir-opt/llc/link)
//
//  Pipeline (per module):
//    stage 1: collect @AOT() candidates (isAot && isStatic && !isTemplateFunction)
//    stage 2: MLIRExporter.ExportModuleToFile -> aot.mlir (+ method manifest)
//    stage 3: MLIRToolchain.TryBuildAotDll    -> aot.dll (fallback to CVM on failure)
//    stage 3.5: manifest is merged into module.json ("aot" field) and,
//              for backward compatibility, optionally written standalone
//              as <name>_manifest.json (SIMPLELANG_AOT_MANIFEST=0 disables).
//****************************************************************************

using SimpleLanguage.Export.SLIR.Types;
using SimpleLanguage.IR;
using SimpleLanguage.Logging;
using SimpleLanguage.Project;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SimpleLanguage.Export.MLIR
{
    /// <summary>
    /// MLIR/AOT 导出配置：统一收口所有 SIMPLELANG_* 环境变量开关。
    /// </summary>
    public sealed class MLIRExportConfig
    {
        /// <summary>SIMPLELANG_AOT=0 关闭整个 AOT 导出（默认开启）。</summary>
        public bool AotEnabled { get; init; } = true;
        /// <summary>SIMPLELANG_AOT_DLL=0 跳过 stage-3 dll 构建（只导出 mlir，默认开启）。</summary>
        public bool BuildDll { get; init; } = true;
        /// <summary>
        /// SIMPLELANG_AOT_MANIFEST=0 不再单独写 &lt;name&gt;_manifest.json
        /// （默认仍写，兼容旧 CVM；新 CVM 优先读 module.json 内嵌的 "aot" 字段）。
        /// </summary>
        public bool WriteStandaloneManifest { get; init; } = true;
        /// <summary>SIMPLELANG_MLIR_BIN：mlir-opt/mlir-translate/llc 所在目录。</summary>
        public string? MlirBin { get; init; }
        /// <summary>SIMPLELANG_MSVC_LINK：MSVC link.exe 完整路径。</summary>
        public string? MsvcLink { get; init; }
        /// <summary>SIMPLELANG_EXPORT_OUTDIR：导出输出目录。</summary>
        public string? OutDir { get; init; }
        /// <summary>SIMPLELANG_PROJECT_NAME：项目名（模块文件名前缀）。</summary>
        public string? ProjectName { get; init; }

        /// <summary>模块级 AOT 产物文件名（aot.mlir）。</summary>
        public string MlirFileName { get; init; } = "aot.mlir";
        /// <summary>模块级 AOT dll 文件名（aot.dll）。</summary>
        public string DllFileName { get; init; } = "aot.dll";

        public static MLIRExportConfig FromEnvironment()
        {
            return new MLIRExportConfig
            {
                AotEnabled = Environment.GetEnvironmentVariable("SIMPLELANG_AOT") != "0",
                BuildDll = Environment.GetEnvironmentVariable("SIMPLELANG_AOT_DLL") != "0",
                WriteStandaloneManifest = Environment.GetEnvironmentVariable("SIMPLELANG_AOT_MANIFEST") != "0",
                MlirBin = NullIfWhiteSpace(Environment.GetEnvironmentVariable("SIMPLELANG_MLIR_BIN")),
                MsvcLink = NullIfWhiteSpace(Environment.GetEnvironmentVariable("SIMPLELANG_MSVC_LINK")),
                OutDir = NullIfWhiteSpace(Environment.GetEnvironmentVariable("SIMPLELANG_EXPORT_OUTDIR")),
                ProjectName = NullIfWhiteSpace(Environment.GetEnvironmentVariable("SIMPLELANG_PROJECT_NAME")),
            };
        }

        private static string? NullIfWhiteSpace(string? s)
            => string.IsNullOrWhiteSpace(s) ? null : s;
    }

    /// <summary>
    /// MLIR AOT 导出编排器（stage 1 -> 3.5）。
    /// 用法：
    ///   var result = MLIRExportManager.Instance.Run(outDir);
    ///   // result.ToSlAotPackage() 并入 module.json 的 "aot" 字段
    /// </summary>
    public sealed class MLIRExportManager
    {
        private static readonly MLIRExportManager s_Instance = new MLIRExportManager();
        public static MLIRExportManager Instance => s_Instance;

        /// <summary>最近一次 Run 的结果（null 表示尚未运行）。</summary>
        public AotModuleResult? LastResult { get; private set; }

        // ------------------------------------------------------------------
        // Public AOT query interfaces (任务2：供 AOT/宿主调用)
        // ------------------------------------------------------------------

        /// <summary>stage-1 候选判定：@AOT() 标记的非模板静态成员函数。</summary>
        public static bool IsAotCandidate(IRMethod? m)
            => m != null && m.isAot && m.isStatic && !m.isTemplateFunction;

        /// <summary>方法是否在最近一次导出中成功降级（可原生分发）。</summary>
        public bool IsMethodAotReady(string methodId)
            => LastResult?.Methods.FirstOrDefault(x => x.Id == methodId)?.Status == "ok";

        /// <summary>收集当前模块的全部 AOT 候选与被跳过项。</summary>
        public static (List<IRMethod> candidates, List<string> skipped) CollectCandidates(
            IEnumerable<KeyValuePair<string, IRMethod>> methods)
        {
            var candidates = new List<IRMethod>();
            var skipped = new List<string>();
            foreach (var kv in methods)
            {
                var m = kv.Value;
                if (m == null || !m.isAot) continue;
                if (!IsAotCandidate(m))
                {
                    skipped.Add(m.id);
                    continue;
                }
                candidates.Add(m);
            }
            return (candidates, skipped);
        }

        /// <summary>
        /// 单方法/方法集导出接口：发射 MLIR 并按需构建 dll（不经过 stage-1 收集）。
        /// 返回 null 表示发射失败。
        /// </summary>
        public static MLIRExporter.AotExportResult? TryExportMethods(
            IReadOnlyList<IRMethod> methods, string mlirPath, bool buildDll = false,
            string? dllPath = null, MLIRExportConfig? config = null)
        {
            config ??= MLIRExportConfig.FromEnvironment();
            var result = MLIRExporter.ExportModuleToFile(methods, mlirPath,
                writeManifest: config.WriteStandaloneManifest);
            if (result.OkSymbols.Count == 0) return result;

            if (!buildDll) return result;

            var symbols = (IReadOnlyList<string>)result.OkSymbols;
            if (result.NeedsBridgeInit)
                symbols = result.OkSymbols.Concat(new[] { "sl_aot_bridge_init" }).ToArray();
            if (MLIRToolchain.TryBuildAotDll(mlirPath, dllPath ?? "", symbols,
                    out var error, MLIRToolchain.ToolchainPaths.FromEnvironment(),
                    gpu: result.HasGpuMethods))
            {
                result.DllFileName = Path.GetFileName(dllPath ?? "aot.dll");
                if (config.WriteStandaloneManifest)
                    MLIRExporter.SetManifestDll(mlirPath, result.DllFileName);
            }
            else
            {
                Log.AddIRLog(LID.ShowExtendMessage,
                    "AOT: build dll failed, fallback to CVM: " + error);
            }
            return result;
        }

        /// <summary>构建 AOT dll（stage-3）。返回 null 表示构建失败。</summary>
        public static string? TryBuildDll(string mlirPath, IReadOnlyList<string> exportSymbols,
            string dllPath, MLIRExportConfig? config = null)
        {
            if (!MLIRToolchain.TryBuildAotDll(mlirPath, dllPath, exportSymbols,
                    out var error, MLIRToolchain.ToolchainPaths.FromEnvironment()))
            {
                Log.AddIRLog(LID.ShowExtendMessage,
                    "AOT: build dll failed, fallback to CVM: " + error);
                return null;
            }
            return dllPath;
        }

        // ------------------------------------------------------------------
        // Orchestration (stage 1 -> 3.5)
        // ------------------------------------------------------------------

        /// <summary>
        /// 执行整个模块的 AOT 导出管线。开关关闭或无候选时返回未运行的空结果。
        /// </summary>
        public AotModuleResult Run(string outDir, MLIRExportConfig? config = null)
        {
            config ??= MLIRExportConfig.FromEnvironment();

            var result = new AotModuleResult { Ran = false };
            if (!config.AotEnabled)
            {
                LastResult = result;
                return result;
            }

            var (candidates, skipped) = CollectCandidates(IRManager.instance.IRMethodDict);
            if (candidates.Count == 0 && skipped.Count == 0)
            {
                LastResult = result;
                return result;
            }

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
            {
                LastResult = result;
                return result;
            }

            // ---- stage 2: emit aot.mlir -----------------------------------
            result.Ran = true;
            var mlirPath = Path.Combine(outDir, config.MlirFileName);
            var export = MLIRExporter.ExportModuleToFile(candidates, mlirPath,
                writeManifest: config.WriteStandaloneManifest);
            result.MlirPath = mlirPath;
            result.NeedsBridgeInit = export.NeedsBridgeInit;
            result.Methods.AddRange(export.Methods);

            Log.AddIRLog(LID.ShowExtendMessage,
                $"AOT: export {config.MlirFileName} success: {mlirPath} " +
                $"({export.OkSymbols.Count} ok, {export.FailedIds.Count} failed)");
            foreach (var failedId in export.FailedIds)
            {
                Log.AddIRLog(LID.ShowExtendMessage,
                    $"AOT: method emit failed (see manifest): {failedId}");
            }

            if (export.OkSymbols.Count == 0)
            {
                LastResult = result;
                return result;
            }

            // ---- stage 3: lower to aot.dll (fallback to CVM on failure) ---
            if (config.BuildDll)
            {
                var dllPath = Path.Combine(outDir, config.DllFileName);
                // Stage-5 reverse bridge: the module references @sl_aot_bridge_init,
                // so it must be exported from the dll (only when actually emitted).
                var exportSymbols = export.NeedsBridgeInit
                    ? export.OkSymbols.Concat(new[] { "sl_aot_bridge_init" }).ToArray()
                    : (IReadOnlyList<string>)export.OkSymbols;
                if (MLIRToolchain.TryBuildAotDll(mlirPath, dllPath, exportSymbols,
                        out var dllError, MLIRToolchain.ToolchainPaths.FromEnvironment(),
                        gpu: export.HasGpuMethods))
                {
                    result.DllFileName = config.DllFileName;
                    if (config.WriteStandaloneManifest)
                        MLIRExporter.SetManifestDll(mlirPath, config.DllFileName);
                    Log.AddIRLog(LID.ShowExtendMessage, "AOT: build dll success: " + dllPath);
                }
                else
                {
                    Log.AddIRLog(LID.ShowExtendMessage,
                        "AOT: build dll failed, fallback to CVM: " + dllError);
                }
            }
            else
            {
                Log.AddIRLog(LID.ShowExtendMessage,
                    "AOT: dll build disabled (SIMPLELANG_AOT_DLL=0)");
            }

            LastResult = result;
            return result;
        }

        // ------------------------------------------------------------------
        // Result model
        // ------------------------------------------------------------------

        /// <summary>一次模块级 AOT 导出的汇总结果。</summary>
        public sealed class AotModuleResult
        {
            /// <summary>管线是否真正执行了发射（开关开启且有候选）。</summary>
            public bool Ran { get; set; }
            public string? MlirPath { get; set; }
            /// <summary>构建成功的 dll 文件名（相对导出目录；null = 未构建/失败）。</summary>
            public string? DllFileName { get; set; }
            /// <summary>模块是否包含 stage-5 反向桥（需要导出 sl_aot_bridge_init）。</summary>
            public bool NeedsBridgeInit { get; set; }
            public List<MLIRExporter.AotMethodManifest> Methods { get; } = new();

            /// <summary>是否有可原生分发的方法。</summary>
            public bool AnyOk
                => Methods.Any(m => m.Status == "ok" && !string.IsNullOrEmpty(DllFileName));

            /// <summary>转换为 module.json 的 "aot" 字段数据（任务3）。</summary>
            public SLAotPackage? ToSlAotPackage()
            {
                if (!Ran || Methods.Count == 0) return null;
                var pkg = new SLAotPackage
                {
                    mlir = Path.GetFileName(MlirPath ?? "aot.mlir"),
                    dll = DllFileName ?? string.Empty,
                };
                foreach (var m in Methods)
                {
                    pkg.methods.Add(new SLAotMethodPackage
                    {
                        id = m.Id,
                        symbol = m.Symbol,
                        status = m.Status,
                        reason = string.IsNullOrEmpty(m.Reason) ? null : m.Reason,
                    });
                }
                return pkg;
            }
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
    }
}
