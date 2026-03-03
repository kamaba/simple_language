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

        private sealed class StringPool
        {
            private readonly Dictionary<string, int> _index = new(StringComparer.Ordinal);
            private readonly List<string> _items = new();

            public int Add(string? s)
            {
                s ??= string.Empty;
                if (_index.TryGetValue(s, out var id)) return id;
                id = _items.Count;
                _items.Add(s);
                _index.Add(s, id);
                return id;
            }

            public void Write(BinaryWriter bw)
            {
                bw.Write(_items.Count);
                for (int i = 0; i < _items.Count; i++)
                {
                    WriteStringRaw(bw, _items[i]);
                }
            }

            private static void WriteStringRaw(BinaryWriter bw, string s)
            {
                var bytes = Encoding.UTF8.GetBytes(s ?? string.Empty);
                bw.Write(bytes.Length);
                bw.Write(bytes);
            }
        }

        private sealed class TypeTable
        {
            private readonly Dictionary<string, int> _index = new(StringComparer.Ordinal);
            private readonly List<string> _items = new();

            public int Add(string? typeName)
            {
                typeName ??= string.Empty;
                if (_index.TryGetValue(typeName, out var id)) return id;
                id = _items.Count;
                _items.Add(typeName);
                _index.Add(typeName, id);
                return id;
            }

            public void Write(BinaryWriter bw, StringPool sp)
            {
                bw.Write(_items.Count);
                for (int i = 0; i < _items.Count; i++)
                {
                    bw.Write(sp.Add(_items[i]));
                }
            }
        }

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

            // Build pools first for compact/stable references
            var sp = new StringPool();
            var tt = new TypeTable();
            BuildPools(ir, sp, tt);

            // String pool section
            sp.Write(bw);

            // Type table section (typeName -> stringId)
            tt.Write(bw, sp);

            // Class metadata section
            WriteClassSection(bw, sp, tt);

            // Method count
            var methods = ir.IRMethodDict;
            bw.Write(methods.Count);

            // Methods
            foreach (var kv in methods)
            {
                var m = kv.Value;
                if (m == null) continue;
                WriteMethod(bw, m, sp);
            }

        }

        private static void BuildPools(IRManager ir, StringPool sp, TypeTable tt)
        {
            var classes = ClassManager.instance.runtimeClassList;
            for (int i = 0; i < classes.Count; i++)
            {
                var c = classes[i];
                if (c == null) continue;
                sp.Add(c.allClassName);
                sp.Add(c.name);
                sp.Add(c.extendClass?.allClassName);

                var vars = c.allMetaMemberVariableList;
                if (vars != null)
                {
                    for (int vi = 0; vi < vars.Count; vi++)
                    {
                        var mv = vars[vi];
                        sp.Add(mv?.name);
                        var tname = mv?.defineMetaType?.ToString();
                        tt.Add(tname);
                        sp.Add(tname);
                    }
                }

                var staticFuns = c.staticMetaMemberFunctionList;
                var nonStaticFuns = c.nonStaticVirtualMetaMemberFunctionList;
                if (staticFuns != null)
                {
                    for (int fi = 0; fi < staticFuns.Count; fi++)
                    {
                        var mf = staticFuns[fi];
                        sp.Add(mf?.name);
                        sp.Add(mf?.functionAllName);
                    }
                }
                if (nonStaticFuns != null)
                {
                    for (int fi = 0; fi < nonStaticFuns.Count; fi++)
                    {
                        var mf = nonStaticFuns[fi];
                        sp.Add(mf?.name);
                        sp.Add(mf?.functionAllName);
                    }
                }
            }

            var methods = ir.IRMethodDict;
            foreach (var kv in methods)
            {
                var m = kv.Value;
                if (m == null) continue;
                sp.Add(m.id);
                sp.Add(m.onlyFunctionName);
            }
        }

        private static void WriteClassSection(BinaryWriter bw, StringPool sp, TypeTable tt)
        {
            // Runtime class list is the current authoritative class graph.
            // IRMetaClass list inside IRManager is private; exporting runtime classes
            // still allows reconstructing CLR/Dart-like metadata at load time.
            var classes = ClassManager.instance.runtimeClassList;

            bw.Write(classes.Count);

            for (int i = 0; i < classes.Count; i++)
            {
                var c = classes[i];
                if (c == null)
                {
                    bw.Write(sp.Add(string.Empty));
                    bw.Write(sp.Add(string.Empty));
                    bw.Write(sp.Add(string.Empty));
                    bw.Write(0);
                    bw.Write(0);
                    bw.Write(0);
                    continue;
                }

                // Use stable names/relations available on MetaClass.
                bw.Write(sp.Add(c.allClassName ?? string.Empty));
                bw.Write(sp.Add(c.name ?? string.Empty));
                bw.Write(sp.Add(c.extendClass?.allClassName ?? string.Empty));

                // Template / relationship flags
                bw.Write(c.isTemplateClass ? 1 : 0);
                bw.Write(c.isInterfaceClass ? 1 : 0);
                bw.Write(c.isAbstractClass ? 1 : 0);

                // Member variables
                var vars = c.allMetaMemberVariableList;
                bw.Write(vars?.Count ?? 0);
                if (vars != null)
                {
                    for (int vi = 0; vi < vars.Count; vi++)
                    {
                        var mv = vars[vi];
                        bw.Write(sp.Add(mv?.name ?? string.Empty));
                        // store type id (string form table for now)
                        var tn = mv?.defineMetaType?.ToString() ?? string.Empty;
                        bw.Write(tt.Add(tn));
                        bw.Write(mv?.isStatic == true ? 1 : 0);
                        bw.Write(mv?.isConst == true ? 1 : 0);
                    }
                }

                // Member functions
                var staticFuns = c.staticMetaMemberFunctionList;
                var nonStaticFuns = c.nonStaticVirtualMetaMemberFunctionList;
                int funCount = (staticFuns?.Count ?? 0) + (nonStaticFuns?.Count ?? 0);
                bw.Write(funCount);

                if (staticFuns != null)
                {
                    for (int fi = 0; fi < staticFuns.Count; fi++)
                    {
                        var mf = staticFuns[fi];
                        bw.Write(sp.Add(mf?.name ?? string.Empty));
                        bw.Write(sp.Add(mf?.functionAllName ?? string.Empty));
                        bw.Write(mf?.isStatic == true ? 1 : 0);
                        bw.Write(mf?.isOverrideFunction == true ? 1 : 0);
                        bw.Write(mf?.isOverrideInterface == true ? 1 : 0);
                        bw.Write(mf?.isAbstract == true ? 1 : 0);
                    }
                }

                if (nonStaticFuns != null)
                {
                    for (int fi = 0; fi < nonStaticFuns.Count; fi++)
                    {
                        var mf = nonStaticFuns[fi];
                        bw.Write(sp.Add(mf?.name ?? string.Empty));
                        bw.Write(sp.Add(mf?.functionAllName ?? string.Empty));
                        bw.Write(mf?.isStatic == true ? 1 : 0);
                        bw.Write(mf?.isOverrideFunction == true ? 1 : 0);
                        bw.Write(mf?.isOverrideInterface == true ? 1 : 0);
                        bw.Write(mf?.isAbstract == true ? 1 : 0);
                    }
                }
            }
        }

        private static void WriteMethod(BinaryWriter bw, IRMethod m, StringPool sp)
        {
            // id/name
            bw.Write(sp.Add(m.id ?? string.Empty));
            bw.Write(sp.Add(m.onlyFunctionName ?? string.Empty));

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

        // v2 uses StringPool; no raw string writes in payload.
    }
}
