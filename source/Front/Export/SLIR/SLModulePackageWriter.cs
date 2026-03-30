#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using SimpleLanguage;
using SimpleLanguage.Core;
using SimpleLanguage.IR;
using SimpleLanguage.Export.SLIR.Types;

namespace SimpleLanguage.Export.SLIR
{
    // Physical package: root shell (entryModule, moduleList) + per-module SLAssemblyPackage payload.
    // Full IR payload lives only under moduleList[]; VM merges strings and flattens modules at load time.
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

            var root = new SLPackageRootJson
            {
                entryModule = pkg.entryModule ?? pkg.moduleList.FirstOrDefault()?.moduleName ?? moduleName ?? string.Empty,
                moduleList = pkg.moduleList,
            };

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
            File.WriteAllText(outputPath, JsonSerializer.Serialize(root, options));
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
            if (pkg == null) return;
            NormalizeFieldFlagsForClassList(pkg.classList);
            if (pkg.moduleList != null)
            {
                for (int mi = 0; mi < pkg.moduleList.Count; mi++)
                {
                    NormalizeFieldFlagsForClassList(pkg.moduleList[mi]?.classList);
                }
            }
        }

        private static void NormalizeFieldFlagsForClassList(List<SLClassPackage>? classList)
        {
            if (classList == null) return;
            for (int c = 0; c < classList.Count; c++)
            {
                var cls = classList[c];
                if (cls?.fieldList == null) continue;
                for (int f = 0; f < cls.fieldList.Count; f++)
                {
                    var field = cls.fieldList[f];
                    if (field == null) continue;

                    const int allowed = 1 | 2 | 4 | 8 | 16 | 32;
                    field.flags &= allowed;
                }
            }
        }

        internal static SLModulePackage Build(IRManager ir, string moduleName)
        {
            var pkg = new SLModulePackage();
            var module = new SLAssemblyPackage(moduleName ?? string.Empty);

            // const strings (IRManager.AddStringIRStack)
            foreach (var kv in ir.IRStringDict)
            {
                module.irStringDict.Add(new IRStringItem { id = kv.Key, value = kv.Value ?? string.Empty });
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
                    module.namespaceList.Add(nsPkg);
                }
                nsPkg.typeList.Add(new SLTypePackage { fullName = full, name = typeName, templateParameterCount = c.templateParameterCount });

                var cm = new SLClassPackage
                {
                    id = c.id,
                    fullName = full,
                    name = typeName,
                    sourcePath = c.sourcePath ?? string.Empty,
                    metaClassKind = (int)c.metaClassKind,
                };
                // export template count
                cm.templateCount = c.templateCount;
                cm.templateParameterCount = c.templateParameterCount;
                // export template (generated) meta types for the class
                if (c.templateTypeList != null)
                {
                    for (int ti = 0; ti < c.templateTypeList.Count; ti++)
                    {
                        var tt = c.templateTypeList[ti];
                        if (tt == null) continue;
                        // represent template meta type as SLRuntimeDefTypePackage to preserve templateIndex and nested args
                        var rtp = CreateRuntimeDefTypePackage(tt);
                        if (rtp != null) cm.templateTypeList.Add(rtp);
                    }
                }

                IRMetaType curirmt = new IRMetaType(c, c.templateTypeList);

                // export template relations mapping
                if (c.templateRelation != null)
                {
                    foreach (var kv in c.templateRelation)
                    {
                        var relatedClassId = kv.Key;
                        var map = kv.Value;
                        if (map == null) continue;
                        var relPkg = new SLTemplateRelationPackage { relatedClassId = relatedClassId };
                        foreach (var inner in map)
                        {
                            var entry = new SLTemplateRelationEntry { index = inner.Key, type = CreateRuntimeDefTypePackage(inner.Value) };
                            relPkg.mapping.Add(entry);
                        }
                        cm.templateRelationList.Add(relPkg);
                    }
                }
                // export per-class method references: static, non-static and operator methods
                if (c.nonStaticMethodList != null)
                {
                    for (int mi = 0; mi < c.nonStaticMethodList.Count; mi++)
                    {
                        var m = c.nonStaticMethodList[mi];
                        if (m == null) continue;
                        cm.nonStaticMethodList.Add(new SLMethodMeta { id = m.id ?? string.Empty, name = m.onlyFunctionName ?? string.Empty, index = mi });
                    }
                }
                if (c.operatorMethodList != null)
                {
                    for (int mi = 0; mi < c.operatorMethodList.Count; mi++)
                    {
                        var m = c.operatorMethodList[mi];
                        if (m == null) continue;
                        cm.operatorMethodList.Add(new SLMethodMeta { id = m.id ?? string.Empty, name = m.onlyFunctionName ?? string.Empty, index = mi });
                    }
                }
                if (c.staticMethodList != null)
                {
                    for (int mi = 0; mi < c.staticMethodList.Count; mi++)
                    {
                        var m = c.staticMethodList[mi];
                        if (m == null) continue;
                        cm.staticMethodList.Add(new SLMethodMeta { id = m.id ?? string.Empty, name = m.onlyFunctionName ?? string.Empty, index = mi });
                    }
                }
            if (c.localIRMetaVariableList != null)
            {
                foreach (var v in c.localIRMetaVariableList)
                {
                    if (v == null) continue;
                    var fieldPkgLocal = new SLFieldPackage
                    {
                        name = GetShortName(v.name ?? string.Empty),
                        typeDef = CreateRuntimeDefTypePackage(v.irMetaType),
                        flags = BuildFieldFlags(v),
                        index = v.index,
                    };
                    var irBufLocal = new List<IRData>();
                    // fill express from IRMetaVariable.irDataList if present
                    if (v.irDataList != null && v.irDataList.Count > 0)
                    {
                        foreach (var d in v.irDataList)
                        {
                            if (d == null) continue;
                            irBufLocal.Add(d);
                        }
                    }
                    else
                    {
                        // try to synthesize from matching global static variable (for enums/static defined as global)
                        bool enumExpressExported = false;
                        // enum类型特殊处理
                        if (v.irMetaType != null && v.irMetaType.irMetaClass != null && v.irMetaType.irMetaClass.GetType().Name == "MetaEnum")
                        {
                            // 尝试从MetaEnum成员导出express
                            var metaEnum = v.irMetaType.irMetaClass;
                            var member = metaEnum.GetType().GetMethod("GetMemberEnumByName")?.Invoke(metaEnum, new object[] { v.name });
                            if (member != null)
                            {
                                var expressProp = member.GetType().GetProperty("express");
                                var express = expressProp?.GetValue(member);
                                if (express is MetaExpressNode enumMemberExpress)
                                {
                                    try
                                    {
                                        var iex = IRExpressManager.CreateExpress(null, enumMemberExpress);
                                        if (iex?.IRDataList != null)
                                        {
                                            foreach (var d in iex.IRDataList)
                                            {
                                                if (d == null) continue;
                                                irBufLocal.Add(d);
                                            }
                                            enumExpressExported = true;
                                        }
                                    }
                                    catch { }
                                }
                            }
                        }
                        if (!enumExpressExported && ir.globalStaticVariableList != null)
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
                                            irBufLocal.Add(d);
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                    // Match IRMetaClass.CreateStaticMetaMetaVariableIRList: expr IR then StoreNotStaticField1 for instance fields.
                    if (irBufLocal.Count > 0)
                    {
                        AppendClassInstanceFieldStoreIfNeeded(v, irBufLocal);
                    }
                    foreach (var d in irBufLocal)
                    {
                        fieldPkgLocal.express.Add(CreateInstructionPackage(d));
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
                            name = GetShortName(v.name ?? string.Empty),
                            typeDef = CreateRuntimeDefTypePackage(v.irMetaType),
                            flags = BuildFieldFlags(v) | 32,
                            index = v.index,
                        };
                        var irBufStatic = new List<IRData>();
                        if (v.irDataList != null && v.irDataList.Count > 0)
                        {
                            foreach (var d in v.irDataList)
                            {
                                if (d == null) continue;
                                irBufStatic.Add(d);
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
                                                irBufStatic.Add(d);
                                            }
                                        }
                                    }
                                    catch { }
                                }
                            }

                            // Fallback: compile directly from field expression when
                            // globalStaticVariableList matching by id is unavailable.
                            if (irBufStatic.Count == 0 && v.express != null)
                            {
                                try
                                {
                                    var iexField = IRExpressManager.CreateExpress(null, v.express);
                                    if (iexField?.IRDataList != null)
                                    {
                                        foreach (var d in iexField.IRDataList)
                                        {
                                            if (d == null) continue;
                                            irBufStatic.Add(d);
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                        // Match IRMetaClass.CreateStaticMetaMetaVariableIRList: expr IR then StoreStaticField for class statics.
                        if (irBufStatic.Count > 0)
                        {
                            AppendClassStaticFieldStoreIfNeeded(v, curirmt, irBufStatic);
                        }
                        foreach (var d in irBufStatic)
                        {
                            fieldPkgStatic.express.Add(CreateInstructionPackage(d));
                        }
                        cm.fieldList.Add(fieldPkgStatic);
                    }
                }
                module.classList.Add(cm);
            }

            if (ir.globalStaticVariableList != null)
            {
                foreach (var gv in ir.globalStaticVariableList)
                {
                    if (gv == null) continue;
                    var gsp = new SLGlobalStaticVariablePackage
                    {
                        id = gv.id,
                        name = GetShortName(gv.name ?? string.Empty),
                        ownerClassId = gv.irMetaType?.irOwnerMetaClass?.id ?? 0,
                        index = gv.index,
                        typeDef = CreateRuntimeDefTypePackage(gv.irMetaType),
                    };

                    // export initialization expression instructions if available
                    List<IRData> irListForExpr = new List<IRData>();
                    if (gv.irDataList != null && gv.irDataList.Count > 0)
                    {
                        irListForExpr.AddRange(gv.irDataList);
                    }
                    else if (gv.express != null)
                    {
                        try
                        {
                            var iex = IRExpressManager.CreateExpress(null, gv.express);
                            if (iex?.IRDataList != null)
                            {
                                irListForExpr.AddRange(iex.IRDataList);
                            }
                        }
                        catch { }
                    }

                    if (irListForExpr.Count > 0)
                    {
                        // Match IRManager.GlobalVariable: expr IR then StoreGlobal.
                        AppendGlobalStoreIfNeeded(gv, irListForExpr);
                        foreach (var d in irListForExpr)
                        {
                            if (d == null) continue;
                            gsp.express.Add(CreateInstructionPackage(d));
                        }
                    }

                    module.globalStaticVariableList.Add(gsp);
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
                            name = GetShortName(v.name ?? string.Empty),
                            typeDef = CreateRuntimeDefTypePackage(v.irMetaType),
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
                            name = GetShortName(v.name ?? string.Empty),
                            typeDef = CreateRuntimeDefTypePackage(v.irMetaType),
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
                            name = GetShortName(v.name ?? string.Empty),
                            typeDef = CreateRuntimeDefTypePackage(v.irMetaType),
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

                        mp.instructionList.Add(CreateInstructionPackage(d));
                    }
                }

                module.methodList.Add(mp);

                if (bestEntry == null && string.Equals(m.onlyFunctionName, "_main_", StringComparison.OrdinalIgnoreCase))
                {
                    if (m.IRDataList != null && m.IRDataList.Count > 0)
                        bestEntry = m.id;
                }
            }

            module.entryMethodId = bestEntry;

            // In-memory model; Write() serializes SLPackageRootJson (entryModule + moduleList only).
            pkg.entryModule = module.moduleName;
            pkg.moduleList.Add(module);

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

        /// <summary>
        /// After expression IR for a class static field, append <see cref="EIROpCode.StoreStaticField"/>
        /// when missing — same order as <see cref="IRMetaClass.CreateStaticMetaMetaVariableIRList"/>.
        /// </summary>
        private static void AppendClassStaticFieldStoreIfNeeded(IRMetaVariable v, IRMetaType curirmt, List<IRData> irBuf)
        {
            if (v == null || irBuf == null || irBuf.Count == 0) return;
            var last = irBuf[irBuf.Count - 1];
            if (last != null && last.opCode == EIROpCode.StoreStaticField) return;
            if (v.irMetaType == null) return;

            var irdata = new IRData
            {
                id = irBuf.Count,
                opValue = curirmt,
                opCode = EIROpCode.StoreStaticField,
                index = v.index,
            };
            irBuf.Add(irdata);
        }

        /// <summary>
        /// After expression IR for an instance field default init, append <see cref="EIROpCode.StoreNotStaticField1"/>.
        /// </summary>
        private static void AppendClassInstanceFieldStoreIfNeeded(IRMetaVariable v, List<IRData> irBuf)
        {
            if (v == null || irBuf == null || irBuf.Count == 0) return;
            var last = irBuf[irBuf.Count - 1];
            if (last != null && last.opCode == EIROpCode.StoreNotStaticField1) return;
            if (v.irMetaType == null) return;
            var irdata = new IRData
            {
                id = irBuf.Count,
                opValue = v.irMetaType,
                opCode = EIROpCode.StoreNotStaticField1,
                index = v.index,
            };
            irBuf.Add(irdata);
        }

        /// <summary>
        /// After expression IR for a global static variable init, append <see cref="EIROpCode.StoreGlobal"/>.
        /// </summary>
        private static void AppendGlobalStoreIfNeeded(IRMetaVariable gv, List<IRData> irBuf)
        {
            if (gv == null || irBuf == null || irBuf.Count == 0) return;
            var last = irBuf[irBuf.Count - 1];
            if (last != null && last.opCode == EIROpCode.StoreGlobal) return;
            var irdata = new IRData
            {
                id = irBuf.Count,
                opCode = EIROpCode.StoreGlobal,
                index = gv.id,
            };
            irBuf.Add(irdata);
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

        /// <summary>
        /// Copies packed <see cref="IRData"/> to the JSON wire DTO (VM loads the same JSON into <c>Instruction</c>).
        /// </summary>
        private static SLIRInstructionPackage CreateInstructionPackage(IRData d)
        {
            if (d == null) throw new ArgumentNullException(nameof(d));
            try { d.FinalizePack(); } catch { /* best-effort */ }

            return new SLIRInstructionPackage
            {
                id = d.id,
                opCode = (byte)d.opCode,
                index = d.index,
                offset = d.offset,
                byteLength = d.ByteLength,
                payload = d.Payload,
            };
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
}
