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

            var pkg = JsonSerializer.Deserialize<SLModulePackage>(json, options) ?? new SLModulePackage();
            NormalizeFieldFlags(pkg);
            return pkg;
        }

        private static void NormalizeFieldFlags(SLModulePackage pkg)
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

                    if (field.isConst) field.flags |= 16;
                    if (field.isStatic) field.flags |= 32;
                    if (!field.isConst && (field.flags & 16) == 16) field.isConst = true;
                    if (!field.isStatic && (field.flags & 32) == 32) field.isStatic = true;
                }
            }
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
                        var fieldPkgLocal = new SLFieldPackage
                        {
                            name = v.name ?? string.Empty,
                            typeName = NormalizeTypeName(v.irMetaType?.ToString() ?? string.Empty),
                            isStatic = false,
                            isConst = v.isConst,
                            flags = BuildFieldFlags(v),
                            index = v.index,
                        };
                        // fill express from IRMetaVariable.irDataList if present
                        if (v.irDataList != null && v.irDataList.Count > 0)
                        {
                            foreach (var d in v.irDataList)
                            {
                                if (d == null) continue;
                                var rawOpValue = d.opValue;
                                try { d.FinalizePack(); } catch { }
                                fieldPkgLocal.express.Add(CreateInstructionPackage(d, rawOpValue));
                            }
                        }
                        else
                        {
                            // try to synthesize from matching global static variable (for enums/static defined as global)
                            if (ir.globalStaticVariableList != null)
                            {
                                var match = ir.globalStaticVariableList.Find(g => g != null && g.id == v.id);
                                if (match != null && match.express != null)
                                {
                                    try
                                    {
                                        var iex = IRExpressManager.CreateExpress(null, match.express);
                                        if (iex?.IRDataList != null)
                                        {
                                            foreach (var d in iex.IRDataList)
                                            {
                                                if (d == null) continue;
                                                var rawOpValue = d.opValue;
                                                try { d.FinalizePack(); } catch { }
                                                fieldPkgLocal.express.Add(CreateInstructionPackage(d, rawOpValue));
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                        cm.fieldList.Add(fieldPkgLocal);
                    }
                }
                if (c.staticIRMetaVariableList != null)
                {
                    foreach (var v in c.staticIRMetaVariableList)
                    {
                        if (v == null) continue;
                        var fieldPkgStatic = new SLFieldPackage
                        {
                            name = v.name ?? string.Empty,
                            typeName = NormalizeTypeName(v.irMetaType?.ToString() ?? string.Empty),
                            isStatic = true,
                            isConst = v.isConst,
                            flags = BuildFieldFlags(v),
                            index = v.index,
                        };
                        if (v.irDataList != null && v.irDataList.Count > 0)
                        {
                            foreach (var d in v.irDataList)
                            {
                                if (d == null) continue;
                                var rawOpValue = d.opValue;
                                try { d.FinalizePack(); } catch { }
                                fieldPkgStatic.express.Add(CreateInstructionPackage(d, rawOpValue));
                            }
                        }
                        else
                        {
                            // try to synthesize from globalStaticVariableList entry
                            if (ir.globalStaticVariableList != null)
                            {
                                var match = ir.globalStaticVariableList.Find(g => g != null && g.id == v.id);
                                if (match != null && match.express != null)
                                {
                                    try
                                    {
                                        var iex = IRExpressManager.CreateExpress(null, match.express);
                                        if (iex?.IRDataList != null)
                                        {
                                            foreach (var d in iex.IRDataList)
                                            {
                                                if (d == null) continue;
                                                var rawOpValue = d.opValue;
                                                try { d.FinalizePack(); } catch { }
                                                fieldPkgStatic.express.Add(CreateInstructionPackage(d, rawOpValue));
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                        cm.fieldList.Add(fieldPkgStatic);
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

                if (m.methodReturnVariableList != null)
                {
                    for (int i = 0; i < m.methodReturnVariableList.Count; i++)
                    {
                        var v = m.methodReturnVariableList[i];
                        if (v == null) continue;
                        mp.returnList.Add(new SLVariablePackage
                        {
                            id = v.id,
                            index = v.index,
                            name = v.name ?? string.Empty,
                            typeName = NormalizeTypeName(v.irMetaType?.ToString() ?? string.Empty),
                        });
                    }
                }

                if (m.methodArgumentList != null)
                {
                    for (int i = 0; i < m.methodArgumentList.Count; i++)
                    {
                        var v = m.methodArgumentList[i];
                        if (v == null) continue;
                        mp.argumentList.Add(new SLVariablePackage
                        {
                            id = v.id,
                            index = v.index,
                            name = v.name ?? string.Empty,
                            typeName = NormalizeTypeName(v.irMetaType?.ToString() ?? string.Empty),
                        });
                    }
                }

                if (m.methodLocalVariableList != null)
                {
                    for (int i = 0; i < m.methodLocalVariableList.Count; i++)
                    {
                        var v = m.methodLocalVariableList[i];
                        if (v == null) continue;
                        mp.localList.Add(new SLVariablePackage
                        {
                            id = v.id,
                            index = v.index,
                            name = v.name ?? string.Empty,
                            typeName = NormalizeTypeName(v.irMetaType?.ToString() ?? string.Empty),
                        });
                    }
                }

                var code = m.IRDataList;
                if (code != null)
                {
                    for (int i = 0; i < code.Count; i++)
                    {
                        var d = code[i];
                        if (d == null) continue;

                        // Ensure payload is ready.
                        var rawOpValue = d.opValue;
                        try { d.FinalizePack(); } catch { }

                        mp.instructionList.Add(CreateInstructionPackage(d, rawOpValue));
                    }
                }

                pkg.methodList.Add(mp);

                if (bestEntry == null && string.Equals(m.onlyFunctionName, "_main_", StringComparison.OrdinalIgnoreCase))
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

        private static int BuildFieldFlags(IRMetaVariable v)
        {
            if (v == null) return 0;
            int flags = 0;

            // permission bits
            // 1: private, 2: public, 4: export, 8: protected
            switch (v.permission)
            {
                case EPermission.Private: flags |= 1; break;
                case EPermission.Public: flags |= 2; break;
                case EPermission.Export: flags |= 4; break;
                case EPermission.Protected: flags |= 8; break;
            }

            // 16: const, 32: static
            if (v.isConst) flags |= 16;
            if (v.isStatic) flags |= 32;

            return flags;
        }

        private static SLIRInstructionPackage CreateInstructionPackage(IRData d, object? rawOpValue = null)
        {
            var sourceOpValue = rawOpValue ?? d.opValue;
            var pkg = new SLIRInstructionPackage
            {
                id = d.id,
                opCode = (byte)d.opCode,
                opValue = sourceOpValue is string s ? s : null,
                payload = d.Payload,
                index = d.index,
                byteLength = d.ByteLength,
                offset = d.offset,
            };

            if (sourceOpValue is IRMethodCall mc)
            {
                pkg.runtimeCall = CreateRuntimeCallPackage(mc);

                // backward compatible: VM registry can still bind by methodId string
                if (!string.IsNullOrWhiteSpace(mc.irMethod?.id))
                {
                    pkg.opValue = mc.irMethod.id;
                }
            }
            else if (IsCallInstruction((EIROpCode)d.opCode) && sourceOpValue is string methodId && !string.IsNullOrWhiteSpace(methodId))
            {
                // legacy/fallback: keep method id for VM-side RuntimeCall binding
                pkg.opValue = methodId;
            }

            if (pkg.runtimeCall == null && IsCallInstruction((EIROpCode)d.opCode) && TryReadRuntimeCallFromPayload(d.Payload, out var callFromPayload))
            {
                pkg.runtimeCall = callFromPayload;
                if (string.IsNullOrWhiteSpace(pkg.opValue as string) && !string.IsNullOrWhiteSpace(callFromPayload?.methodId))
                {
                    pkg.opValue = callFromPayload.methodId;
                }
            }

            return pkg;
        }

        private static bool TryReadRuntimeCallFromPayload(byte[]? payload, out SLRuntimeCallPackage? call)
        {
            call = null;
            if (payload == null || payload.Length == 0) return false;

            try
            {
                var text = Encoding.UTF8.GetString(payload);
                if (string.IsNullOrWhiteSpace(text) || text[0] != '{') return false;
                call = JsonSerializer.Deserialize<SLRuntimeCallPackage>(text);
                return call != null && !string.IsNullOrWhiteSpace(call.methodId);
            }
            catch
            {
                return false;
            }
        }

        private static bool IsCallInstruction(EIROpCode opCode)
        {
            return opCode == EIROpCode.CallStatic
                || opCode == EIROpCode.CallDynamic
                || opCode == EIROpCode.CallVirt;
        }

        private static SLRuntimeCallPackage CreateRuntimeCallPackage(IRMethodCall mc)
        {
            var ret = new SLRuntimeCallPackage
            {
                methodId = mc.irMethod?.id ?? string.Empty,
                methodName = mc.methodName ?? string.Empty,
                paramCount = mc.paramCount,
                runtimeDefType = CreateRuntimeDefTypePackage(mc.metaType),
            };

            if (mc.irTemplateMetaType != null)
            {
                for (int i = 0; i < mc.irTemplateMetaType.Count; i++)
                {
                    var t = CreateRuntimeDefTypePackage(mc.irTemplateMetaType[i]);
                    if (t != null) ret.templateRuntimeDefTypeList.Add(t);
                }
            }

            return ret;
        }

        private static SLRuntimeDefTypePackage? CreateRuntimeDefTypePackage(IRMetaType? mt)
        {
            if (mt == null) return null;

            var ret = new SLRuntimeDefTypePackage
            {
                classId = mt.irMetaClass?.id ?? 0,
                className = NormalizeTypeName(mt.irMetaClass?.irName ?? string.Empty),
                ownerClassId = mt.irOwnerMetaClass?.id ?? 0,
                ownerClassName = NormalizeTypeName(mt.irOwnerMetaClass?.irName ?? string.Empty),
                templateIndex = mt.templateIndex,
                isTemplate = mt.templateIndex >= 0,
            };

            if (mt.irMetaTypeList != null)
            {
                for (int i = 0; i < mt.irMetaTypeList.Count; i++)
                {
                    var child = CreateRuntimeDefTypePackage(mt.irMetaTypeList[i]);
                    if (child != null) ret.runtimeDefTypeList.Add(child);
                }
            }

            return ret;
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
        public List<SLVariablePackage> returnList { get; set; } = new();
        public List<SLVariablePackage> argumentList { get; set; } = new();
        public List<SLVariablePackage> localList { get; set; } = new();
        public List<SLIRInstructionPackage> instructionList { get; set; } = new();
    }

    internal sealed class SLVariablePackage
    {
        public int id { get; set; }
        public int index { get; set; }
        public string name { get; set; } = string.Empty;
        public string typeName { get; set; } = string.Empty;
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
        public bool isConst { get; set; }
        public int flags { get; set; }
        public int index { get; set; }
        public List<SLIRInstructionPackage> express { get; set; } = new();
    }

    internal sealed class SLIRInstructionPackage
    {
        public int id { get; set; }
        public byte opCode { get; set; }
        public object? opValue { get; set; }
        public SLRuntimeCallPackage? runtimeCall { get; set; }
        public byte[]? payload { get; set; }
        public int index { get; set; }
        public int byteLength { get; set; }
        public int offset { get; set; }
    }

    internal sealed class SLRuntimeCallPackage
    {
        public SLRuntimeDefTypePackage? runtimeDefType { get; set; }
        public List<SLRuntimeDefTypePackage> templateRuntimeDefTypeList { get; set; } = new();
        public string methodId { get; set; } = string.Empty;
        public string methodName { get; set; } = string.Empty;
        public int paramCount { get; set; }
    }

    internal sealed class SLRuntimeDefTypePackage
    {
        public int classId { get; set; }
        public string className { get; set; } = string.Empty;
        public int ownerClassId { get; set; }
        public string ownerClassName { get; set; } = string.Empty;
        public int templateIndex { get; set; } = -1;
        public bool isTemplate { get; set; }
        public List<SLRuntimeDefTypePackage> runtimeDefTypeList { get; set; } = new();
    }
}
