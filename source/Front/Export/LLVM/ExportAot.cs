using System;
using SimpleLanguage.IR;

namespace SimpleLanguage.Export.AOT
{
    public static class ExportAot
    {
        // Entry: export provided methods to LLVM IR files under outDir
        public static void Export(IRMethod[] methods, string outDir)
        {
            if (!System.IO.Directory.Exists(outDir)) System.IO.Directory.CreateDirectory(outDir);
            var emitter = new LLVMEmitter();
            for (int i = 0; i < methods.Length; i++)
            {
                var m = methods[i];
                string fn = System.IO.Path.Combine(outDir, m.id + ".ll");
                emitter.EmitMethod(m, fn);
            }
        }
    }
}
