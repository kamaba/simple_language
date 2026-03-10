//****************************************************************************
//  File:      ExportLangManager.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/30 12:00:00
//  Description:  manager Export other lanuage or il etc.
//****************************************************************************

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
            var outDir = Environment.GetEnvironmentVariable("SIMPLELANG_EXPORT_OUTDIR");
            if (string.IsNullOrWhiteSpace(outDir))
            {
                outDir = Path.Combine(Environment.CurrentDirectory, "out", "export");
            }
            Directory.CreateDirectory(outDir);

            SLIRWriter.WriteModule(IRManager.instance, Path.Combine(outDir, "module.slir"));

            /*
            foreach (var kv in irManager.IRMethodDict)
            {
                var m = kv.Value;
                if (m == null) continue;

                if (string.Equals(kind, "llvm", StringComparison.OrdinalIgnoreCase))
                {
                    var llvm = new LLVMEmitter();
                    llvm.EmitMethod(m, Path.Combine(outDir, m.onlyFunctionName + ".ll"));
                }
                else if (string.Equals(kind, "mlir", StringComparison.OrdinalIgnoreCase))
                {
                    var mlirPath = Path.Combine(outDir, m.onlyFunctionName + ".mlir");
                    var lower = Environment.GetEnvironmentVariable("SIMPLELANG_MLIR_LOWER") == "1";
                    if (!lower)
                    {
                        MLIRExporter.ExportToFile(m, mlirPath);
                    }
                    else
                    {
                        var nativeOut = Environment.GetEnvironmentVariable("SIMPLELANG_MLIR_NATIVE_OUT");
                        if (string.IsNullOrWhiteSpace(nativeOut))
                        {
                            nativeOut = Path.Combine(outDir, m.onlyFunctionName + ".exe");
                        }

                        MLIRExporter.ExportAndOptionallyLower(m, mlirPath, new MLIRExporter.ExportOptions
                        {
                            RunToolchain = true,
                            NativeOutputPath = nativeOut,
                        });
                    }
                }
                else if (string.Equals(kind, "slir", StringComparison.OrdinalIgnoreCase))
                {
                    var slirPath = Path.Combine(outDir, "module.slir");
                    SLIRWriter.WriteModule(irManager, slirPath);

                    if (Environment.GetEnvironmentVariable("SIMPLELANG_SLIR_DUMP") == "1")
                    {
                        SLIRDump.DumpToText(slirPath, Path.Combine(outDir, "module.slir.txt"));
                    }
                    // one module file is enough; stop after first iteration
                    break;
                }
            }
            */
        }
    }
}