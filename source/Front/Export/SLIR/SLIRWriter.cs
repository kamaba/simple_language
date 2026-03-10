using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SimpleLanguage.IR;
using SimpleLanguage.Core;

namespace SimpleLanguage.Export.SLIR
{
    // SimpleLanguage IR binary format (SLIR v1)
    // Goals: stable, compact, easy to parse (CLR/Dart-like bytecode container).
    // NOTE: keeps opcodes as EIROpCode byte for now.
    public static class SLIRWriter
    {
        private const uint Magic = 0x52494C53; // 'SLIR'
        private const ushort Version = 2;

        public static void WriteModule(IRManager ir, string outputPath)
        {
            if (ir == null) throw new ArgumentNullException(nameof(ir));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentNullException(nameof(outputPath));

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");

            using var fs = File.Create(outputPath);
            using var bw = new BinaryWriter(fs, Encoding.UTF8, leaveOpen: false);

            // Header
            bw.Write(Magic);
            bw.Write(Version);
            bw.Write((ushort)0); // flags            

            // 1) IR string dict section (id->stringId)
            WriteIRStringDict(bw, ir);

            // 5) IR meta classes section
            WriteClassSection(bw);

            // 6) IR methods section
            var methods = ir.IRMethodDict;
            bw.Write(methods.Count);
            foreach (var kv in methods)
            {
                var m = kv.Value;
                if (m == null) continue;
                WriteMethod(bw, m);
            }
        }
        private static void WriteIRStringDict(BinaryWriter bw, IRManager ir )
        {
            var dict = ir.IRStringDict;
            bw.Write(dict?.Count ?? 0);
            if (dict == null) return;

            foreach (var kv in dict)
            {
                bw.Write(kv.Key);
                bw.Write(kv.Value ?? string.Empty);
            }
        }

        private static void WriteClassSection(BinaryWriter bw )
        {
            // Export from IR layer (IRMetaClass/IRMetaVariable). If IR lacks data,
            // it must be populated into IR before export.
            var classes = IRManager.instance.GetIRMetaClassList();

            bw.Write(classes.Count);

            for (int i = 0; i < classes.Count; i++)
            {
                var c = classes[i];
                if (c == null)
                {
                    bw.Write(string.Empty);
                    bw.Write(string.Empty);
                    bw.Write(string.Empty);
                    bw.Write(0); // class kind
                    bw.Write(0);
                    bw.Write(0);
                    bw.Write(0);
                    continue;
                }

                // names (IR level)
                bw.Write(c.irName ?? string.Empty);
                bw.Write(c.sourcePath ?? string.Empty);
                bw.Write(string.Empty); // short name not available at IR layer currently
                bw.Write(string.Empty); // base name not available at IR layer currently

                // class kind/flags are not represented in IRMetaClass today
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);

                // Member variables (IR)
                var vars = c.localIRMetaVariableList;
                var svars = c.staticIRMetaVariableList;
                int fieldCount = (vars?.Count ?? 0) + (svars?.Count ?? 0);
                bw.Write(fieldCount);

                if (vars != null)
                {
                    for (int vi = 0; vi < vars.Count; vi++)
                    {
                        var mv = vars[vi];
                        bw.Write(mv?.name ?? string.Empty);
                        var tn = mv?.irMetaType?.ToString() ?? string.Empty;
                        //bw.Write(tt.Add(tn));
                        //bw.Write(ts.Add(mv?.irMetaType));
                        bw.Write(0); // isStatic
                        bw.Write(0); // isConst
                        bw.Write(0); // permission
                        bw.Write(mv?.index ?? -1);
                    }
                }

                if (svars != null)
                {
                    for (int vi = 0; vi < svars.Count; vi++)
                    {
                        var mv = svars[vi];
                        bw.Write(mv?.name ?? string.Empty);
                        var tn = mv?.irMetaType?.ToString() ?? string.Empty;
                        //bw.Write(tt.Add(tn));
                        //bw.Write(ts.Add(mv?.irMetaType));
                        bw.Write(1); // isStatic
                        bw.Write(0); // isConst
                        bw.Write(0); // permission
                        bw.Write(mv?.index ?? -1);
                    }
                }

                // Member functions are exported via IRMethod section; class->method binding is not yet in IRMetaClass.
                bw.Write(0);
            }
        }

        private static void WriteMethod(BinaryWriter bw, IRMethod m )
        {
            // id/name
            bw.Write(m.id ?? string.Empty);
            bw.Write(m.onlyFunctionName ?? string.Empty);

            // signature (minimal)
            bw.Write(m.methodArgumentList?.Count ?? 0);
            bw.Write(m.methodLocalVariableList?.Count ?? 0);
            bw.Write(m.methodReturnVariableList?.Count ?? 0);

            // code
            var code = m.IRDataList ?? new List<IRData>();
            bw.Write(code.Count);

            // Ensure payloads are finalized (self-contained)
            for (int i = 0; i < code.Count; i++)
            {
                code[i]?.FinalizePack();
            }

            for (int i = 0; i < code.Count; i++)
            {
                var ins = code[i];
                if (ins == null)
                {
                    bw.Write((byte)EIROpCode.Nop);
                    bw.Write(0); // payload len
                    continue;
                }

                bw.Write((byte)ins.opCode);

                // index field: used by branches/locals/args
                bw.Write(ins.index);

                // debug offset (serialized stream offset) if present
                bw.Write(ins.offset);

                var payload = ins.Payload;
                bw.Write(payload?.Length ?? 0);
                if (payload != null && payload.Length > 0)
                {
                    bw.Write(payload);
                }
            }
        }
    }
}
