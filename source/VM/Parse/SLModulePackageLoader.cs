using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SimpleLanguage.VM.LanguageRuntime
{
    public static class SLModulePackageLoader
    {
        public static SLModulePackage LoadFromJson(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException(nameof(path));
            var json = File.ReadAllText(path);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };
            options.Converters.Add(new JsonStringEnumConverter());

            var pkg = JsonSerializer.Deserialize<SLModulePackage>(json, options);
            NormalizeFieldFlags(pkg);
            return pkg;
        }

        private static void NormalizeFieldFlags(SLModulePackage? pkg)
        {
            if (pkg?.classList == null) return;
            for (int c = 0; c < pkg.classList.Count; c++)
            {
                var cls = pkg.classList[c];
                if (cls?.fieldList == null) continue;
                for (int f = 0; f < cls.fieldList.Count; f++)
                {
                    var field = cls.fieldList[f];
                    if (field == null) continue;

                    // bool -> flags
                    if (field.isConst) field.flags |= 16;
                    if (field.isStatic) field.flags |= 32;

                    // flags -> bool (compat for payloads writing flags only)
                    if (!field.isConst && (field.flags & 16) == 16) field.isConst = true;
                    if (!field.isStatic && (field.flags & 32) == 32) field.isStatic = true;
                }
            }
        }

        public static SLAssembly BuildRuntimeModel(SLModulePackage pkg)
        {
            if (pkg == null) throw new ArgumentNullException(nameof(pkg));

            var asm = new SLAssembly("SimpleLanguage");
            var module = new SLModule(pkg.moduleName);
            asm.AddModule(module);

            foreach (var nsPkg in pkg.namespaceList ?? Enumerable.Empty<SLNamespacePackage>())
            {
                var ns = module.GetOrAddNamespace(nsPkg.fullName);
                foreach (var t in nsPkg.typeList ?? Enumerable.Empty<SLTypePackage>())
                {
                    var full = NormalizeTypeName(t.fullName);
                    ns.AddType(new SLTypeMeta { name = GetTypeShortName(full), fullName = full });
                }
            }

            var typeMap = module.namespaceList
                .SelectMany(n => n.typeList)
                .GroupBy(t => t.fullName, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

            foreach (var m in pkg.methodList ?? Enumerable.Empty<SLMethodPackage>())
            {
                var declType = NormalizeTypeName(m.declaringTypeFullName);
                if (!typeMap.TryGetValue(declType ?? string.Empty, out var tm))
                {
                    var nsName = GetNamespaceFromFullTypeName(declType);
                    var ns = module.GetOrAddNamespace(nsName);
                    tm = new SLTypeMeta
                    {
                        name = GetTypeShortName(declType),
                        fullName = declType ?? string.Empty,
                    };
                    ns.AddType(tm);
                    typeMap[tm.fullName] = tm;
                }

                var vmIns = ConvertToVMInstructionList(m.instructionList);
                tm.AddMethod(new SLMethodMeta
                {
                    id = m.id ?? string.Empty,
                    name = m.name ?? string.Empty,
                    irList = Array.Empty<object>(),
                    vmInstructionList = vmIns,
                });
            }

            return asm;
        }

        private static string NormalizeTypeName(string name)
        {
            if (string.IsNullOrEmpty(name)) return string.Empty;
            int i = 0;
            while (true)
            {
                int lt = name.IndexOf('<', i);
                if (lt < 0) break;
                int gt = name.IndexOf('>', lt + 1);
                if (gt < 0) break;
                var seg = name.Substring(lt, gt - lt + 1);

                int nextLt = name.IndexOf('<', gt + 1);
                if (nextLt == gt + 1)
                {
                    int nextGt = name.IndexOf('>', nextLt + 1);
                    if (nextGt > nextLt)
                    {
                        var seg2 = name.Substring(nextLt, nextGt - nextLt + 1);
                        if (string.Equals(seg, seg2, StringComparison.Ordinal))
                        {
                            name = name.Remove(nextLt, seg2.Length);
                            i = lt + seg.Length;
                            continue;
                        }
                    }
                }
                i = gt + 1;
            }
            return name;
        }

        internal static List<SimpleLanguage.VM.Instruction> ConvertToVMInstructionList(List<SLIRInstructionPackage> list)
        {
            var result = new List<SimpleLanguage.VM.Instruction>(list?.Count ?? 0);
            if (list == null) return result;

            foreach (var d in list)
            {
                var opCode = (SimpleLanguage.VM.EIROpCode)d.opCode;
                // Note: resolution of runtime-def-type payloads has been moved to the
                // dynamic runtime layer. We keep the original opValue/payload here and
                // let the runtime resolve/convert them on demand (e.g. before executing
                // NewArray/NewTemplateObject/Ldc/LoadStaticField/StoreStaticField).
                object? opValue = (object?)d.runtimeCall ?? d.opValue;

                var ins = new SimpleLanguage.VM.Instruction
                {
                    id = d.id,
                    opCode = opCode,
                    opValue = opValue,
                    Payload = d.payload,
                    ByteLength = d.byteLength,
                    index = d.index,
                };
                result.Add(ins);
            }

            return result;
        }

        private static bool IsRuntimeDefTypeInstruction(SimpleLanguage.VM.EIROpCode opCode)
        {
            return opCode == SimpleLanguage.VM.EIROpCode.NewArray
                || opCode == SimpleLanguage.VM.EIROpCode.NewTemplateObject
                || opCode == SimpleLanguage.VM.EIROpCode.Ldc
                || opCode == SimpleLanguage.VM.EIROpCode.LoadStaticField
                || opCode == SimpleLanguage.VM.EIROpCode.StoreStaticField;
        }

        private static string GetNamespaceFromFullTypeName(string fullType)
        {
            if (string.IsNullOrEmpty(fullType)) return string.Empty;
            var idx = fullType.LastIndexOf('.');
            return idx > 0 ? fullType.Substring(0, idx) : string.Empty;
        }

        private static string GetTypeShortName(string fullType)
        {
            if (string.IsNullOrEmpty(fullType)) return string.Empty;
            var idx = fullType.LastIndexOf('.');
            return idx >= 0 && idx + 1 < fullType.Length ? fullType.Substring(idx + 1) : fullType;
        }
    }
}
