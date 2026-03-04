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

        private sealed class IRTypeSigTable
        {
            private readonly Dictionary<IRMetaType, int> _index = new();
            private readonly List<IRMetaType> _items = new();

            public int Add(IRMetaType? t)
            {
                if (t == null) return -1;
                if (_index.TryGetValue(t, out var id)) return id;

                id = _items.Count;
                _items.Add(t);
                _index.Add(t, id);

                var args = t.irMetaTypeList;
                if (args != null)
                {
                    for (int i = 0; i < args.Count; i++)
                    {
                        Add(args[i]);
                    }
                }

                return id;
            }

            public void Write(BinaryWriter bw)
            {
                bw.Write(_items.Count);
                for (int i = 0; i < _items.Count; i++)
                {
                    var t = _items[i];
                    bw.Write(t?.irMetaClass?.id ?? 0);
                    bw.Write(t?.irOwnerMetaClass?.id ?? 0);
                    bw.Write(t?.templateIndex ?? -1);
                    bw.Write(t?.templateIndex != -1 ? 1 : 0); // isTemplate
                    var args = t?.irMetaTypeList;
                    bw.Write(args?.Count ?? 0);
                    if (args != null)
                    {
                        for (int ai = 0; ai < args.Count; ai++)
                        {
                            bw.Write(Add(args[ai]));
                        }
                    }
                }
            }
        }

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
            var ts = new IRTypeSigTable();
            BuildPools(ir, sp, tt);

            // Build IR type signatures table
            BuildIRTypeSigs(ir, ts);

            // String pool section
            sp.Write(bw);

            // Type table section (typeName -> stringId)
            tt.Write(bw, sp);

            // IR TypeSig table section
            ts.Write(bw);

            // Class metadata section
            WriteClassSection(bw, sp, tt, ts);

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

        private static void BuildIRTypeSigs(IRManager ir, IRTypeSigTable ts)
        {
            var classes = ir.GetIRMetaClassList();
            for (int i = 0; i < classes.Count; i++)
            {
                var c = classes[i];
                if (c == null) continue;

                var locals = c.localIRMetaVariableList;
                if (locals != null)
                {
                    for (int vi = 0; vi < locals.Count; vi++)
                    {
                        ts.Add(locals[vi]?.irMetaType);
                    }
                }

                var statics = c.staticIRMetaVariableList;
                if (statics != null)
                {
                    for (int vi = 0; vi < statics.Count; vi++)
                    {
                        ts.Add(statics[vi]?.irMetaType);
                    }
                }
            }

            foreach (var kv in ir.IRMethodDict)
            {
                var m = kv.Value;
                if (m == null) continue;
                var args = m.methodArgumentList;
                if (args != null)
                    for (int i = 0; i < args.Count; i++) ts.Add(args[i]?.irMetaType);
                var locals = m.methodLocalVariableList;
                if (locals != null)
                    for (int i = 0; i < locals.Count; i++) ts.Add(locals[i]?.irMetaType);
                var rets = m.methodReturnVariableList;
                if (rets != null)
                    for (int i = 0; i < rets.Count; i++) ts.Add(rets[i]?.irMetaType);
            }
        }

        private static void BuildPools(IRManager ir, StringPool sp, TypeTable tt)
        {
            var methods = ir.IRMethodDict;
            foreach (var kv in methods)
            {
                var m = kv.Value;
                if (m == null) continue;
                sp.Add(m.id);
                sp.Add(m.onlyFunctionName);
            }

            // IRMetaClass/IRMetaVariable pools (IR is the source of truth for export)
            var classes = ir.GetIRMetaClassList();
            for (int i = 0; i < classes.Count; i++)
            {
                var c = classes[i];
                if (c == null) continue;

                sp.Add(c.irName);
                sp.Add(c.sourcePath);

                var locals = c.localIRMetaVariableList;
                if (locals != null)
                {
                    for (int vi = 0; vi < locals.Count; vi++)
                    {
                        var v = locals[vi];
                        sp.Add(v?.name);
                        var tname = v?.irMetaType?.ToString();
                        tt.Add(tname);
                        sp.Add(tname);
                    }
                }

                var statics = c.staticIRMetaVariableList;
                if (statics != null)
                {
                    for (int vi = 0; vi < statics.Count; vi++)
                    {
                        var v = statics[vi];
                        sp.Add(v?.name);
                        var tname = v?.irMetaType?.ToString();
                        tt.Add(tname);
                        sp.Add(tname);
                    }
                }
            }
        }

        private static void WriteClassSection(BinaryWriter bw, StringPool sp, TypeTable tt, IRTypeSigTable ts)
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
                    bw.Write(sp.Add(string.Empty));
                    bw.Write(sp.Add(string.Empty));
                    bw.Write(sp.Add(string.Empty));
                    bw.Write(0); // class kind
                    bw.Write(0);
                    bw.Write(0);
                    bw.Write(0);
                    continue;
                }

                // names (IR level)
                bw.Write(sp.Add(c.irName ?? string.Empty));
                bw.Write(sp.Add(c.sourcePath ?? string.Empty));
                bw.Write(sp.Add(string.Empty)); // short name not available at IR layer currently
                bw.Write(sp.Add(string.Empty)); // base name not available at IR layer currently

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
                        bw.Write(sp.Add(mv?.name ?? string.Empty));
                        var tn = mv?.irMetaType?.ToString() ?? string.Empty;
                        bw.Write(tt.Add(tn));
                        bw.Write(ts.Add(mv?.irMetaType));
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
                        bw.Write(sp.Add(mv?.name ?? string.Empty));
                        var tn = mv?.irMetaType?.ToString() ?? string.Empty;
                        bw.Write(tt.Add(tn));
                        bw.Write(ts.Add(mv?.irMetaType));
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
