using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SimpleLanguage.VM.Runtime;

namespace SimpleLanguage.VM
{
    public static class SLIRModuleLoader
    {
        private const uint Magic = 0x52494C53; // 'SLIR'
        private const ushort VersionV2 = 2;

        private sealed class SlirModule
        {
            public Dictionary<int, string> IRStringDict { get; } = new();
            public List<string> StringPool { get; } = new();
            public List<string> TypeTable { get; } = new();
            public List<TypeSigInfo> TypeSigs { get; } = new();
            public List<ClassInfo> Classes { get; } = new();
            public List<MethodInfo> Methods { get; } = new();
        }

        private static Dictionary<int, string> s_LastIRStringDict = new();

        public static string? TryGetConstString(int stringId)
        {
            if (s_LastIRStringDict != null && s_LastIRStringDict.TryGetValue(stringId, out var s))
                return s;
            return null;
        }

        private sealed class MethodInfo
        {
            public string Id = string.Empty;
            public string OnlyName = string.Empty;
            public int ArgCount;
            public int LocalCount;
            public int RetCount;
            public List<Instruction> Instructions = new();
        }

        public static object ReadModule(string slirPath)
        {
            return ReadSlir(slirPath);
        }

        private sealed class TypeSigInfo
        {
            public int ClassId;
            public int OwnerClassId;
            public int TemplateIndex;
            public bool IsTemplate;
            public List<int> Args = new();
        }

        private sealed class ClassInfo
        {
            public string AllName = string.Empty;
            public string SourcePath = string.Empty;
            public List<FieldInfo> Fields = new();
        }

        private sealed class FieldInfo
        {
            public string Name = string.Empty;
            public int TypeSigId;
            public bool IsStatic;
            public int Index;
        }

        public static void LoadIntoRuntime(string slirPath)
        {
            if (string.IsNullOrWhiteSpace(slirPath)) throw new ArgumentNullException(nameof(slirPath));
            var m = ReadSlir(slirPath);

            s_LastIRStringDict = m.IRStringDict;

            var rcm = RuntimeClassManager.instance;
            rcm.m_IRMetaClassList.Clear();

            // 1) Create runtime classes first.
            for (int i = 0; i < m.Classes.Count; i++)
            {
                var c = m.Classes[i];
                var rc = new RuntimeClass
                {
                    // NOTE: IRMetaClass.id was based on MetaClass.GetHashCode() (not stable across processes).
                    // For SLIR loading we use a stable hash on class name.
                    id = StableId32(c.AllName),
                    name = c.AllName ?? string.Empty,
                };
                rcm.m_IRMetaClassList.Add(rc);
            }

            // 2) Build RuntimeDefType table from TypeSig table.
            var rdtCache = new Dictionary<int, RuntimeDefType>();
            RuntimeDefType ResolveDefType(int typeSigId)
            {
                if (typeSigId < 0 || typeSigId >= m.TypeSigs.Count) return null;
                if (rdtCache.TryGetValue(typeSigId, out var cached)) return cached;

                var ts = m.TypeSigs[typeSigId];
                var rc = rcm.GetRuntimeClassById(ts.ClassId);
                if (rc == null)
                {
                    // Fallback: resolve by stable name-hash if writer exported unstable ids.
                    rc = rcm.GetRuntimeClassById(ts.ClassId);
                }

                var owner = rcm.GetRuntimeClassById(ts.OwnerClassId);
                var rdt = RuntimeDefTypeBuilder.Build(rc, owner, ts.TemplateIndex, ts.IsTemplate, ts.Args, ResolveDefType);
                rdtCache[typeSigId] = rdt;
                return rdt;
            }

            // 3) Attach variables to runtime classes.
            for (int i = 0; i < m.Classes.Count; i++)
            {
                var c = m.Classes[i];
                var rc = rcm.GetRuntimeClassByName(c.AllName);
                if (rc == null) continue;

                for (int f = 0; f < c.Fields.Count; f++)
                {
                    var fi = c.Fields[f];
                    var rv = new RuntimeVariable();
                    RuntimeDefTypeBuilder.SetRuntimeDefType(rv, ResolveDefType(fi.TypeSigId));
                    if (fi.IsStatic)
                        rc.staticIRMetaVariableList.Add(rv);
                    else
                        rc.nonStaticIRMetaVariableList.Add(rv);
                }
            }

            // global init must run before Main/entry logic
            RunGlobalInitializers(m);
        }

        private static void RunGlobalInitializers(SlirModule m)
        {
            if (m == null || m.Methods.Count == 0) return;

            bool pushedRoot = false;
            if (CLRVM.clrRuntimeStack.Count == 0)
            {
                var root = new RuntimeVM(new List<Instruction>());
                root.id = "__slir_root__";
                CLRVM.PushCLRRuntime(root);
                pushedRoot = true;
            }

            try
            {
                for (int i = 0; i < m.Methods.Count; i++)
                {
                    var method = m.Methods[i];
                    if (!IsGlobalInitializer(method)) continue;
                    if (method.Instructions == null || method.Instructions.Count == 0) continue;

                    var vm = CLRVM.CreateExeSplite(new List<RuntimeType>(), method.Instructions);
                    vm.id = method.Id;
                    vm.Run(true);
                    CLRVM.PopCLRRuntime();
                }
            }
            finally
            {
                if (pushedRoot && CLRVM.clrRuntimeStack.Count > 0)
                {
                    CLRVM.PopCLRRuntime();
                }
            }
        }

        private static bool IsGlobalInitializer(MethodInfo method)
        {
            if (method == null) return false;

            if (string.Equals(method.OnlyName, "Global", StringComparison.OrdinalIgnoreCase))
                return true;

            var id = method.Id ?? string.Empty;
            return id.EndsWith(".Global", StringComparison.OrdinalIgnoreCase)
                || id.EndsWith(".Global()", StringComparison.OrdinalIgnoreCase);
        }

        private static SlirModule ReadSlir(string path)
        {
            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs, Encoding.UTF8, leaveOpen: false);

            uint magic = br.ReadUInt32();
            if (magic != Magic) throw new InvalidDataException("Not a SLIR file (magic mismatch)");
            ushort ver = br.ReadUInt16();
            if (ver != VersionV2) throw new InvalidDataException($"Unsupported SLIR version: {ver}");
            _ = br.ReadUInt16(); // flags

            var m = new SlirModule();

            // IRStringDict (id->stringId)
            int irStrCount = br.ReadInt32();
            for (int i = 0; i < irStrCount; i++)
            {
                int key = br.ReadInt32();
                int sid = br.ReadInt32();
                // string pool not yet loaded; keep sid encoded as placeholder, remap after pool read
                m.IRStringDict[key] = sid.ToString();
            }

            // string pool
            int spCount = br.ReadInt32();
            for (int i = 0; i < spCount; i++) m.StringPool.Add(ReadString(br));

            // remap IRStringDict sids -> strings
            var keys = new List<int>(m.IRStringDict.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                var k = keys[i];
                if (int.TryParse(m.IRStringDict[k], out var sid))
                    m.IRStringDict[k] = GetString(m, sid);
            }

            // type table
            int ttCount = br.ReadInt32();
            for (int i = 0; i < ttCount; i++)
            {
                int sid = br.ReadInt32();
                m.TypeTable.Add(GetString(m, sid));
            }

            // typesig
            int tsCount = br.ReadInt32();
            for (int i = 0; i < tsCount; i++)
            {
                var ts = new TypeSigInfo();
                ts.ClassId = br.ReadInt32();
                ts.OwnerClassId = br.ReadInt32();
                ts.TemplateIndex = br.ReadInt32();
                ts.IsTemplate = br.ReadInt32() != 0;
                int ac = br.ReadInt32();
                for (int ai = 0; ai < ac; ai++) ts.Args.Add(br.ReadInt32());
                m.TypeSigs.Add(ts);
            }

            // classes
            int classCount = br.ReadInt32();
            for (int i = 0; i < classCount; i++)
            {
                var ci = new ClassInfo();
                ci.AllName = GetString(m, br.ReadInt32());
                ci.SourcePath = GetString(m, br.ReadInt32());
                _ = br.ReadInt32(); // short name (unused)
                _ = br.ReadInt32(); // base name (unused)

                _ = br.ReadInt32(); // kind
                _ = br.ReadInt32(); // flags1
                _ = br.ReadInt32(); // flags2
                _ = br.ReadInt32(); // flags3

                int fieldCount = br.ReadInt32();
                for (int f = 0; f < fieldCount; f++)
                {
                    var fi = new FieldInfo();
                    fi.Name = GetString(m, br.ReadInt32());
                    _ = br.ReadInt32(); // typeId (string)
                    fi.TypeSigId = br.ReadInt32();
                    fi.IsStatic = br.ReadInt32() != 0;
                    _ = br.ReadInt32(); // isConst
                    _ = br.ReadInt32(); // permission
                    fi.Index = br.ReadInt32();
                    ci.Fields.Add(fi);
                }

                int funCount = br.ReadInt32();
                for (int f = 0; f < funCount; f++)
                {
                    _ = br.ReadInt32();
                    _ = br.ReadInt32();
                    _ = br.ReadInt32();
                    _ = br.ReadInt32();
                    _ = br.ReadInt32();
                    _ = br.ReadInt32();
                    _ = br.ReadInt32();
                }

                m.Classes.Add(ci);
            }

            // methods are currently ignored by this loader (VM still executes from in-memory IRMethod)
            int methodCount = br.ReadInt32();
            for (int i = 0; i < methodCount; i++)
            {
                var mi = new MethodInfo();
                mi.Id = GetString(m, br.ReadInt32());
                mi.OnlyName = GetString(m, br.ReadInt32());
                mi.ArgCount = br.ReadInt32();
                mi.LocalCount = br.ReadInt32();
                mi.RetCount = br.ReadInt32();

                int insCount = br.ReadInt32();
                for (int j = 0; j < insCount; j++)
                {
                    var ins = new Instruction
                    {
                        opCode = (EIROpCode)br.ReadByte(),
                        index = br.ReadInt32(),
                    };
                    _ = br.ReadInt32(); // offset
                    int payloadLen = br.ReadInt32();
                    ins.Payload = payloadLen == 0 ? Array.Empty<byte>() : br.ReadBytes(payloadLen);
                    ins.UpdateByteLength();
                    ins.UnpackOpValueFromPayload();
                    mi.Instructions.Add(ins);
                }

                m.Methods.Add(mi);
            }

            return m;
        }

        private static string ReadString(BinaryReader br)
        {
            int len = br.ReadInt32();
            if (len <= 0) return string.Empty;
            return Encoding.UTF8.GetString(br.ReadBytes(len));
        }

        private static string GetString(SlirModule m, int id)
        {
            if (id < 0 || id >= m.StringPool.Count) return string.Empty;
            return m.StringPool[id] ?? string.Empty;
        }

        private static int StableId32(string s)
        {
            unchecked
            {
                const uint fnvOffset = 2166136261;
                const uint fnvPrime = 16777619;
                uint hash = fnvOffset;
                for (int i = 0; i < s.Length; i++)
                {
                    hash ^= s[i];
                    hash *= fnvPrime;
                }
                return (int)hash;
            }
        }
    }

    internal static class RuntimeDefTypeBuilder
    {
        public static RuntimeDefType Build(RuntimeClass rc, RuntimeClass owner, int templateIndex, bool isTemplate, List<int> argIds, Func<int, RuntimeDefType> resolve)
        {
            var rdt = new RuntimeDefType();
            var t = typeof(RuntimeDefType);
            t.GetField("m_RuntimeClass", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(rdt, rc);
            t.GetField("m_OwnerRuntimeClass", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(rdt, owner);
            t.GetField("m_TemplateIndex", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(rdt, templateIndex);
            t.GetField("m_IsTemplate", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(rdt, isTemplate);

            var list = (List<RuntimeDefType>)t.GetField("m_RuntimeDefTypeList", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(rdt)!;
            list.Clear();
            for (int i = 0; i < argIds.Count; i++)
            {
                var child = resolve(argIds[i]);
                if (child != null) list.Add(child);
            }

            return rdt;
        }

        public static void SetRuntimeDefType(RuntimeVariable rv, RuntimeDefType rdt)
        {
            var t = typeof(RuntimeVariable);
            t.GetField("m_RuntimeDefType", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.SetValue(rv, rdt);
        }
    }
}
