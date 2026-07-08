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
            var classes = IRManager.instance.GetIRMetaClassList();
            bw.Write(classes.Count);
            for (int i = 0; i < classes.Count; i++)
            {
                var c = classes[i];
                WriteIRMetaClass( bw, c);
            }
        }
        public static void WriteIRMetaClass(BinaryWriter bw, IRMetaClass c )
        {
            if (c == null)
            {
                bw.Write(string.Empty);
                bw.Write(string.Empty);
                bw.Write(string.Empty);
                bw.Write(0); // class kind
                bw.Write(0);
                bw.Write(0);
                bw.Write(0);
                return;
            }

            // names (IR level)
            bw.Write(c.irName ?? string.Empty);
            bw.Write(c.sourcePath ?? string.Empty);
            bw.Write(string.Empty); // short name not available at IR layer currently
            bw.Write(string.Empty); // base name not available at IR layer currently

            bw.Write((int)c.metaClassKind);
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
                    WriteDefIRMetaVariable( bw, mv);
                }
            }

            if (svars != null)
            {
                for (int vi = 0; vi < svars.Count; vi++)
                {
                    var mv = svars[vi];
                    WriteDefIRMetaVariable(bw, mv);
                }
            }
            // Member functions are exported via IRMethod section; class->method binding is not yet in IRMetaClass.
            bw.Write(0);
        }
        public static void WriteDefIRMetaVariable(BinaryWriter bw, IRMetaVariable v )
        {
            if (v == null)
            {
                bw.Write(string.Empty);
                bw.Write(string.Empty);
                //bw.Write(0); // isStatic
                //bw.Write(0); // isConst
                //bw.Write(0); // permission
                bw.Write(0); // index
                return;
            }
            bw.Write(v.id);
            bw.Write(v.name ?? string.Empty);
            bw.Write(v.index);
            WriteIRMetaType(bw, v.irMetaType);
            //bw.Write(tt.Add(tn));
            //bw.Write(ts.Add(v.irMetaType));
            //bw.Write(v.isStatic ? 1 : 0);
            //bw.Write(v.isConst ? 1 : 0);
            //bw.Write(0); // permission (not in IRMetaVariable currently)
        }  
        static void WriteIRMetaType( BinaryWriter bw, IRMetaType t)
        {
            if (t == null)
            {
                bw.Write(string.Empty);
                return;
            }
            bw.Write(t.templateIndex);
            bw.Write(t.irOwnerMetaClass.id);
            bw.Write(t.irMetaClass.id);

            bw.Write(t.irMetaTypeList.Count);
            for( int i = 0; i < t.irMetaTypeList.Count; i++ )
            {
                WriteIRMetaType(bw, t.irMetaTypeList[i]);
            }
        }

        private static void WriteMethod(BinaryWriter bw, IRMethod m )
        {
            // id/name
            bw.Write(m.id ?? string.Empty);
            bw.Write(m.onlyFunctionName ?? string.Empty);
            bw.Write(m.virtualFunctionName ?? string.Empty);

            // signature (minimal)
            bw.Write(m.methodArgumentList?.Count ?? 0);
            for( int i = 0; i < m.methodArgumentList.Count; i++ )
            {
                WriteIRMetaVariable(bw, m.methodArgumentList[i]);
            }

            bw.Write(m.methodLocalVariableList?.Count ?? 0);
            for (int i = 0; i < m.methodLocalVariableList.Count; i++)
            {
                WriteIRMetaVariable(bw, m.methodLocalVariableList[i]);
            }

            bw.Write(m.methodReturnVariableList?.Count ?? 0);
            for (int i = 0; i < m.methodReturnVariableList.Count; i++)
            {
                WriteIRMetaVariable(bw, m.methodReturnVariableList[i]);
            }

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
        static void WriteIRMetaVariable(BinaryWriter bw, IRMetaVariable v )
        {

            if (v == null)
            {
                bw.Write(string.Empty);
                bw.Write(string.Empty);
                //bw.Write(0); // isStatic
                //bw.Write(0); // isConst
                //bw.Write(0); // permission
                bw.Write(0); // index
                return;
            }
            bw.Write(v.id);
            bw.Write(v.name ?? string.Empty);
            bw.Write(v.index);
            WriteIRMetaType(bw, v.irMetaType);
            //bw.Write(tt.Add(tn));
            //bw.Write(ts.Add(v.irMetaType));
            //bw.Write(v.isStatic ? 1 : 0);
            //bw.Write(v.isConst ? 1 : 0);
            //bw.Write(0); // permission (not in IRMetaVariable currently)
        }
    }
}
