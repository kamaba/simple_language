using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SimpleLanguage.IR;

namespace SimpleLanguage.Export.SLIR
{
    // Reader for SLIR v1 produced by SLIRWriter.
    // Produces a lightweight in-memory representation rather than reconstructing full MetaClass/IRMethod objects.
    public static class SLIRReader
    {
        private const uint Magic = 0x52494C53; // 'SLIR'
        private const ushort VersionV1 = 1;
        private const ushort VersionV2 = 2;

        public sealed class Module
        {
            public List<ClassInfo> Classes { get; } = new();
            public List<MethodInfo> Methods { get; } = new();

            public List<string> StringPool { get; } = new();
            public List<string> TypeTable { get; } = new();
        }

        public sealed class ClassInfo
        {
            public string AllName { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string BaseAllName { get; set; } = string.Empty;
            public bool IsTemplate { get; set; }
            public bool IsInterface { get; set; }
            public bool IsAbstract { get; set; }
            public List<FieldInfo> Fields { get; } = new();
            public List<FunctionInfo> Functions { get; } = new();
        }

        public sealed class FieldInfo
        {
            public string Name { get; set; } = string.Empty;
            public string TypeName { get; set; } = string.Empty;
            public bool IsStatic { get; set; }
            public bool IsConst { get; set; }
        }

        public sealed class FunctionInfo
        {
            public string Name { get; set; } = string.Empty;
            public string AllName { get; set; } = string.Empty;
            public bool IsStatic { get; set; }
            public bool IsOverride { get; set; }
            public bool IsOverrideInterface { get; set; }
            public bool IsAbstract { get; set; }
        }

        public sealed class MethodInfo
        {
            public string Id { get; set; } = string.Empty;
            public string OnlyName { get; set; } = string.Empty;
            public int ArgumentCount { get; set; }
            public int LocalCount { get; set; }
            public int ReturnCount { get; set; }
            public List<InstructionInfo> Instructions { get; } = new();
        }

        public sealed class InstructionInfo
        {
            public EIROpCode OpCode { get; set; }
            public int Index { get; set; }
            public int Offset { get; set; }
            public byte[] Payload { get; set; } = Array.Empty<byte>();
        }

        public static Module ReadModule(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentNullException(nameof(path));
            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs, Encoding.UTF8, leaveOpen: false);

            uint magic = br.ReadUInt32();
            if (magic != Magic) throw new InvalidDataException("Not a SLIR file (magic mismatch)");

            ushort ver = br.ReadUInt16();
            if (ver != VersionV1 && ver != VersionV2) throw new InvalidDataException($"Unsupported SLIR version: {ver}");

            _ = br.ReadUInt16(); // flags

            var module = new Module();

            if (ver == VersionV1)
            {
                ReadClassSectionV1(br, module);
                ReadMethodSectionV1(br, module);
            }
            else
            {
                ReadStringPoolV2(br, module);
                ReadTypeTableV2(br, module);
                ReadClassSectionV2(br, module);
                ReadMethodSectionV2(br, module);
            }

            return module;
        }

        private static void ReadClassSectionV1(BinaryReader br, Module module)
        {
            int classCount = br.ReadInt32();
            for (int i = 0; i < classCount; i++)
            {
                var c = new ClassInfo
                {
                    AllName = ReadString(br),
                    Name = ReadString(br),
                    BaseAllName = ReadString(br),
                    IsTemplate = br.ReadInt32() != 0,
                    IsInterface = br.ReadInt32() != 0,
                    IsAbstract = br.ReadInt32() != 0,
                };

                int fieldCount = br.ReadInt32();
                for (int f = 0; f < fieldCount; f++)
                {
                    c.Fields.Add(new FieldInfo
                    {
                        Name = ReadString(br),
                        TypeName = ReadString(br),
                        IsStatic = br.ReadInt32() != 0,
                        IsConst = br.ReadInt32() != 0,
                    });
                }

                int funCount = br.ReadInt32();
                for (int f = 0; f < funCount; f++)
                {
                    c.Functions.Add(new FunctionInfo
                    {
                        Name = ReadString(br),
                        AllName = ReadString(br),
                        IsStatic = br.ReadInt32() != 0,
                        IsOverride = br.ReadInt32() != 0,
                        IsOverrideInterface = br.ReadInt32() != 0,
                        IsAbstract = br.ReadInt32() != 0,
                    });
                }

                module.Classes.Add(c);
            }
        }

        private static void ReadMethodSectionV1(BinaryReader br, Module module)
        {
            int methodCount = br.ReadInt32();
            for (int i = 0; i < methodCount; i++)
            {
                var m = new MethodInfo
                {
                    Id = ReadString(br),
                    OnlyName = ReadString(br),
                    ArgumentCount = br.ReadInt32(),
                    LocalCount = br.ReadInt32(),
                    ReturnCount = br.ReadInt32(),
                };

                int insCount = br.ReadInt32();
                for (int j = 0; j < insCount; j++)
                {
                    var ins = new InstructionInfo
                    {
                        OpCode = (EIROpCode)br.ReadByte(),
                        Index = br.ReadInt32(),
                        Offset = br.ReadInt32(),
                    };

                    int payloadLen = br.ReadInt32();
                    if (payloadLen < 0) throw new InvalidDataException("Negative payload length");
                    ins.Payload = payloadLen == 0 ? Array.Empty<byte>() : br.ReadBytes(payloadLen);

                    m.Instructions.Add(ins);
                }

                module.Methods.Add(m);
            }
        }

        private static void ReadStringPoolV2(BinaryReader br, Module module)
        {
            int count = br.ReadInt32();
            if (count < 0) throw new InvalidDataException("Negative string pool count");
            for (int i = 0; i < count; i++)
            {
                module.StringPool.Add(ReadString(br));
            }
        }

        private static void ReadTypeTableV2(BinaryReader br, Module module)
        {
            int count = br.ReadInt32();
            if (count < 0) throw new InvalidDataException("Negative type table count");
            for (int i = 0; i < count; i++)
            {
                int sid = br.ReadInt32();
                module.TypeTable.Add(GetString(module, sid));
            }
        }

        private static void ReadClassSectionV2(BinaryReader br, Module module)
        {
            int classCount = br.ReadInt32();
            for (int i = 0; i < classCount; i++)
            {
                var c = new ClassInfo
                {
                    AllName = GetString(module, br.ReadInt32()),
                    Name = GetString(module, br.ReadInt32()),
                    BaseAllName = GetString(module, br.ReadInt32()),
                    IsTemplate = br.ReadInt32() != 0,
                    IsInterface = br.ReadInt32() != 0,
                    IsAbstract = br.ReadInt32() != 0,
                };

                int fieldCount = br.ReadInt32();
                for (int f = 0; f < fieldCount; f++)
                {
                    int nameId = br.ReadInt32();
                    int typeId = br.ReadInt32();
                    c.Fields.Add(new FieldInfo
                    {
                        Name = GetString(module, nameId),
                        TypeName = GetType(module, typeId),
                        IsStatic = br.ReadInt32() != 0,
                        IsConst = br.ReadInt32() != 0,
                    });
                }

                int funCount = br.ReadInt32();
                for (int f = 0; f < funCount; f++)
                {
                    c.Functions.Add(new FunctionInfo
                    {
                        Name = GetString(module, br.ReadInt32()),
                        AllName = GetString(module, br.ReadInt32()),
                        IsStatic = br.ReadInt32() != 0,
                        IsOverride = br.ReadInt32() != 0,
                        IsOverrideInterface = br.ReadInt32() != 0,
                        IsAbstract = br.ReadInt32() != 0,
                    });
                }

                module.Classes.Add(c);
            }
        }

        private static void ReadMethodSectionV2(BinaryReader br, Module module)
        {
            int methodCount = br.ReadInt32();
            for (int i = 0; i < methodCount; i++)
            {
                var m = new MethodInfo
                {
                    Id = GetString(module, br.ReadInt32()),
                    OnlyName = GetString(module, br.ReadInt32()),
                    ArgumentCount = br.ReadInt32(),
                    LocalCount = br.ReadInt32(),
                    ReturnCount = br.ReadInt32(),
                };

                int insCount = br.ReadInt32();
                for (int j = 0; j < insCount; j++)
                {
                    var ins = new InstructionInfo
                    {
                        OpCode = (EIROpCode)br.ReadByte(),
                        Index = br.ReadInt32(),
                        Offset = br.ReadInt32(),
                    };

                    int payloadLen = br.ReadInt32();
                    if (payloadLen < 0) throw new InvalidDataException("Negative payload length");
                    ins.Payload = payloadLen == 0 ? Array.Empty<byte>() : br.ReadBytes(payloadLen);

                    m.Instructions.Add(ins);
                }

                module.Methods.Add(m);
            }
        }

        private static string ReadString(BinaryReader br)
        {
            int len = br.ReadInt32();
            if (len < 0) throw new InvalidDataException("Negative string length");
            if (len == 0) return string.Empty;
            var bytes = br.ReadBytes(len);
            return Encoding.UTF8.GetString(bytes);
        }

        private static string GetString(Module module, int id)
        {
            if (id < 0 || id >= module.StringPool.Count) return string.Empty;
            return module.StringPool[id] ?? string.Empty;
        }

        private static string GetType(Module module, int typeId)
        {
            if (typeId < 0 || typeId >= module.TypeTable.Count) return string.Empty;
            return module.TypeTable[typeId] ?? string.Empty;
        }

        public static Dictionary<string, MethodInfo> BuildMethodMapByAllName(Module module)
        {
            var dict = new Dictionary<string, MethodInfo>(StringComparer.Ordinal);
            foreach (var m in module.Methods)
            {
                if (!string.IsNullOrEmpty(m.Id) && !dict.ContainsKey(m.Id))
                {
                    dict.Add(m.Id, m);
                }
            }
            return dict;
        }
    }
}
