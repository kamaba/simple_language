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

            return JsonSerializer.Deserialize<SLModulePackage>(json, options);
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
                    ns.AddType(new SLTypeMeta { name = t.name, fullName = t.fullName });
                }
            }

            var typeMap = module.namespaceList
                .SelectMany(n => n.typeList)
                .GroupBy(t => t.fullName, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

            foreach (var m in pkg.methodList ?? Enumerable.Empty<SLMethodPackage>())
            {
                if (!typeMap.TryGetValue(m.declaringTypeFullName ?? string.Empty, out var tm))
                {
                    var nsName = GetNamespaceFromFullTypeName(m.declaringTypeFullName);
                    var ns = module.GetOrAddNamespace(nsName);
                    tm = new SLTypeMeta
                    {
                        name = GetTypeShortName(m.declaringTypeFullName),
                        fullName = m.declaringTypeFullName ?? string.Empty,
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

        private static List<SimpleLanguage.VM.Instruction> ConvertToVMInstructionList(List<SLIRInstructionPackage> list)
        {
            var result = new List<SimpleLanguage.VM.Instruction>(list?.Count ?? 0);
            if (list == null) return result;

            foreach (var d in list)
            {
                var ins = new SimpleLanguage.VM.Instruction
                {
                    id = d.id,
                    opCode = (SimpleLanguage.VM.EIROpCode)d.opCode,
                    opValue = d.opValue,
                    Payload = d.payload,
                    ByteLength = d.byteLength,
                    index = d.index,
                };
                result.Add(ins);
            }

            return result;
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
