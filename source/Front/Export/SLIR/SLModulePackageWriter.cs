using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using SimpleLanguage.IR;

namespace SimpleLanguage.Export.SLIR
{
    // Exports a VM-friendly JSON package matching VM's SLModulePackage schema.
    // This keeps Front/VM symmetric and avoids VM->Front dependencies.
    public static class SLModulePackageWriter
    {
        public static void Write(IRManager ir, string outputPath, string moduleName = "SimpleLanguage")
        {
            if (ir == null) throw new ArgumentNullException(nameof(ir));
            if (string.IsNullOrWhiteSpace(outputPath)) throw new ArgumentNullException(nameof(outputPath));

            var pkg = Build(ir, moduleName);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };
            options.Converters.Add(new JsonStringEnumConverter());

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
            File.WriteAllText(outputPath, JsonSerializer.Serialize(pkg, options));
        }

        internal static SLModulePackage Read(string inputPath)
        {
            if (string.IsNullOrWhiteSpace(inputPath)) throw new ArgumentNullException(nameof(inputPath));

            var json = File.ReadAllText(inputPath);
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };
            options.Converters.Add(new JsonStringEnumConverter());

            return JsonSerializer.Deserialize<SLModulePackage>(json, options) ?? new SLModulePackage();
        }

        internal static SLModulePackage Build(IRManager ir, string moduleName)
        {
            var pkg = new SLModulePackage { moduleName = moduleName ?? string.Empty };

            // const strings (IRManager.AddStringIRStack)
            foreach (var kv in ir.IRStringDict)
            {
                pkg.irStringDict.Add(new IRStringItem { id = kv.Key, value = kv.Value ?? string.Empty });
            }

            // types
            var classes = ir.GetIRMetaClassList();
            var nsMap = new Dictionary<string, SLNamespacePackage>(StringComparer.Ordinal);
            foreach (var c in classes)
            {
                if (c == null) continue;
                var full = NormalizeTypeName(c.irName ?? string.Empty);
                var nsName = GetNamespace(full);
                var typeName = GetShortName(full);
                if (!nsMap.TryGetValue(nsName, out var nsPkg))
                {
                    nsPkg = new SLNamespacePackage { fullName = nsName };
                    nsMap[nsName] = nsPkg;
                    pkg.namespaceList.Add(nsPkg);
                }
                nsPkg.typeList.Add(new SLTypePackage { fullName = full, name = typeName });

                var cm = new SLClassPackage
                {
                    id = c.id,
                    fullName = full,
                    name = typeName,
                    sourcePath = c.sourcePath ?? string.Empty,
                };
                if (c.localIRMetaVariableList != null)
                {
                    foreach (var v in c.localIRMetaVariableList)
                    {
                        if (v == null) continue;
                        cm.fieldList.Add(new SLFieldPackage
                        {
                            name = v.name ?? string.Empty,
                            typeName = NormalizeTypeName(v.irMetaType?.ToString() ?? string.Empty),
                            isStatic = false,
                            index = v.index,
                        });
                    }
                }
                if (c.staticIRMetaVariableList != null)
                {
                    foreach (var v in c.staticIRMetaVariableList)
                    {
                        if (v == null) continue;
                        cm.fieldList.Add(new SLFieldPackage
                        {
                            name = v.name ?? string.Empty,
                            typeName = NormalizeTypeName(v.irMetaType?.ToString() ?? string.Empty),
                            isStatic = true,
                            index = v.index,
                        });
                    }
                }
                pkg.classList.Add(cm);
            }

            if (ir.globalStaticVariableList != null)
            {
                foreach (var gv in ir.globalStaticVariableList)
                {
                    if (gv == null) continue;
                    pkg.globalStaticVariableList.Add(new SLGlobalStaticVariablePackage
                    {
                        id = gv.id,
                        name = gv.name ?? string.Empty,
                        ownerClassId = gv.irMetaType?.irOwnerMetaClass?.id ?? 0,
                        index = gv.index,
                        typeName = NormalizeTypeName(gv.irMetaType?.ToString() ?? string.Empty),
                    });
                }
            }

            var globalInitIR = new List<IRData>();
            if (ir.globalStaticVariableList != null)
            {
                for (int i = 0; i < ir.globalStaticVariableList.Count; i++)
                {
                    var gv = ir.globalStaticVariableList[i];
                    if (gv?.express == null) continue;

                    try
                    {
                        var expr = IRExpressManager.CreateExpress(null, gv.express);
                        var store = new IRStoreVariable(gv.irMetaType, null, gv.id, IRMetaVariableFrom.Global);

                        if (expr?.IRDataList != null) globalInitIR.AddRange(expr.IRDataList);
                        if (store?.IRDataList != null) globalInitIR.AddRange(store.IRDataList);
                    }
                    catch
                    {
                    }
                }
            }

            for (int i = 0; i < globalInitIR.Count; i++)
            {
                var d = globalInitIR[i];
                if (d == null) continue;
                d.id = i;
                try { d.FinalizePack(); } catch { }

                pkg.globalInitInstructionList.Add(new SLIRInstructionPackage
                {
                    id = d.id,
                    opCode = (byte)d.opCode,
                    opValue = null,
                    payload = d.Payload,
                    index = d.index,
                    byteLength = d.ByteLength,
                    offset = d.offset,
                });
            }

            // methods
            string? bestEntry = null;
            foreach (var kv in ir.IRMethodDict)
            {
                var m = kv.Value;
                if (m == null) continue;

                var declaringTypeFullName = m.irOwnerMetaClass?.irName ?? string.Empty;
                declaringTypeFullName = NormalizeTypeName(declaringTypeFullName);
                var mp = new SLMethodPackage
                {
                    id = m.id ?? string.Empty,
                    declaringTypeFullName = declaringTypeFullName,
                    name = m.onlyFunctionName ?? string.Empty,
                };

                var code = m.IRDataList;
                if (code != null)
                {
                    for (int i = 0; i < code.Count; i++)
                    {
                        var d = code[i];
                        if (d == null) continue;

                        // Ensure payload is ready.
                        try { d.FinalizePack(); } catch { }

                        mp.instructionList.Add(new SLIRInstructionPackage
                        {
                            id = d.id,
                            opCode = (byte)d.opCode,
                            opValue = null,
                            payload = d.Payload,
                            index = d.index,
                            byteLength = d.ByteLength,
                            offset = d.offset,
                        });
                    }
                }

                pkg.methodList.Add(mp);

                if (bestEntry == null && string.Equals(m.onlyFunctionName, "Main", StringComparison.OrdinalIgnoreCase))
                {
                    if (m.IRDataList != null && m.IRDataList.Count > 0)
                        bestEntry = m.id;
                }
            }

            pkg.entryMethodId = bestEntry;

            return pkg;
        }

        private static string GetNamespace(string fullType)
        {
            if (string.IsNullOrEmpty(fullType)) return string.Empty;
            var idx = fullType.LastIndexOf('.');
            return idx > 0 ? fullType.Substring(0, idx) : string.Empty;
        }

        private static string GetShortName(string fullType)
        {
            if (string.IsNullOrEmpty(fullType)) return string.Empty;
            var idx = fullType.LastIndexOf('.');
            return idx >= 0 && idx + 1 < fullType.Length ? fullType.Substring(idx + 1) : fullType;
        }

        private static string NormalizeTypeName(string name)
        {
            // IR names may occasionally contain duplicated generic segments like "Array<T><T>".
            // Normalize by collapsing consecutive identical generic argument lists.
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
    }

    // Local copies of VM schema ensure Front/VM symmetry without references.
    internal sealed class SLModulePackage
    {
        public string moduleName { get; set; } = string.Empty;
        public string? entryMethodId { get; set; }
        public List<string> moduleReferences { get; set; } = new();
        public List<IRStringItem> irStringDict { get; set; } = new();
        public List<SLNamespacePackage> namespaceList { get; set; } = new();
        public List<SLClassPackage> classList { get; set; } = new();
        public List<SLGlobalStaticVariablePackage> globalStaticVariableList { get; set; } = new();
        public List<SLIRInstructionPackage> globalInitInstructionList { get; set; } = new();
        public List<SLMethodPackage> methodList { get; set; } = new();
    }

    internal sealed class SLGlobalStaticVariablePackage
    {
        public int id { get; set; }
        public string name { get; set; } = string.Empty;
        public int ownerClassId { get; set; }
        public int index { get; set; }
        public string typeName { get; set; } = string.Empty;
    }

    internal sealed class IRStringItem
    {
        public int id { get; set; }
        public string value { get; set; } = string.Empty;
    }

    internal sealed class SLNamespacePackage
    {
        public string fullName { get; set; } = string.Empty;
        public List<SLTypePackage> typeList { get; set; } = new();
    }

    internal sealed class SLTypePackage
    {
        public string fullName { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
    }

    internal sealed class SLMethodPackage
    {
        public string id { get; set; } = string.Empty;
        public string declaringTypeFullName { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public List<SLIRInstructionPackage> instructionList { get; set; } = new();
    }

    internal sealed class SLClassPackage
    {
        public int id { get; set; }
        public string fullName { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string sourcePath { get; set; } = string.Empty;
        public List<SLFieldPackage> fieldList { get; set; } = new();
    }

    internal sealed class SLFieldPackage
    {
        public string name { get; set; } = string.Empty;
        public string typeName { get; set; } = string.Empty;
        public bool isStatic { get; set; }
        public int index { get; set; }
    }

    internal sealed class SLIRInstructionPackage
    {
        public int id { get; set; }
        public byte opCode { get; set; }
        public object? opValue { get; set; }
        public byte[]? payload { get; set; }
        public int index { get; set; }
        public int byteLength { get; set; }
        public int offset { get; set; }
    }
}
