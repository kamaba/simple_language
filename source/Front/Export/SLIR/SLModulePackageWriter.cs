#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Encodings.Web;
using SimpleLanguage.Core;
using SimpleLanguage.IR;
using SimpleLanguage.Export.MLIR;
using SimpleLanguage.Export.SLIR.Types;
using SimpleLanguage.Logging;
using SimpleLanguage.Project;

namespace SimpleLanguage.Export.SLIR
{
    internal sealed class InstructionPayloadByteArrayJsonConverter : JsonConverter<byte[]>
    {
        public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType == JsonTokenType.String)
            {
                var text = reader.GetString() ?? string.Empty;
                return Encoding.Latin1.GetBytes(text);
            }

            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var buffer = new List<byte>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        return buffer.ToArray();
                    if (reader.TokenType == JsonTokenType.Number && reader.TryGetByte(out var b))
                    {
                        buffer.Add(b);
                        continue;
                    }
                    throw new JsonException("Invalid byte[] payload token in instruction payload.");
                }
            }

            throw new JsonException("Invalid token for instruction payload byte[] field.");
        }

        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStringValue(Encoding.Latin1.GetString(value));
        }
    }

    // Physical package: flat SLModulePackage JSON (moduleName, classList, methodList, ...).
    // Reads still support legacy SLPackageRootJson (entryModule + moduleList) for backward compat.
    public static class SLModulePackageWriter
    {
        /// <summary>Removes the module name prefix from a dot-separated full name.
        /// Since allName is built by walking the MetaNode parent chain to the module root,
        /// the first segment is always the module name (e.g. "Core.Std.IO" -> "Std.IO",
        /// "Core.Array&lt;T&gt;" -> "Array&lt;T&gt;").</summary>
        private static string StripModulePrefix(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;
            var idx = name.IndexOf('.');
            if (idx <= 0)
                return name; // no dot: single segment, nothing to strip
            return name.Substring(idx + 1);
        }

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
            options.Converters.Add(new InstructionPayloadByteArrayJsonConverter());

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
            File.WriteAllText(outputPath, JsonSerializer.Serialize(pkg, options));

            Log.AddIRLog(LID.ShowExtendMessage, "export module success: " + outputPath);
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
            options.Converters.Add(new InstructionPayloadByteArrayJsonConverter());

            var pkg = ReadModulePackageJson(json, options);
            NormalizeFieldFlags(pkg);
            return pkg;
        }

        internal static SLModulePackage ReadWithoutInstructionCode(string inputPath)
        {
            var pkg = Read(inputPath);
            StripInstructionCode(pkg);
            return pkg;
        }

        private static SLModulePackage ReadModulePackageJson(string json, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            });

            // Legacy format: entryModule + moduleList[] wrapper
            if (TryGetJsonArray(doc.RootElement, "moduleList"))
            {
                var root = JsonSerializer.Deserialize<SLPackageRootJson>(json, options) ?? new SLPackageRootJson();
                var pkg = new SLModulePackage
                {
                    moduleName = root.entryModule ?? string.Empty,
                    entryModule = root.entryModule,
                    uuid = root.uuid ?? string.Empty,
                    moduleList = root.moduleList ?? new List<SLAssemblyPackage>(),
                };

                if (string.IsNullOrWhiteSpace(pkg.uuid))
                {
                    var firstUuid = pkg.moduleList.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m?.uuid))?.uuid;
                    pkg.uuid = firstUuid ?? string.Empty;
                }

                var entry = pkg.moduleList.FirstOrDefault(m => string.Equals(m?.moduleName, pkg.entryModule, StringComparison.Ordinal))
                    ?? pkg.moduleList.FirstOrDefault();
                if (entry != null)
                {
                    pkg.entryMethodId = entry.entryMethodId;
                    pkg.moduleReferences = entry.moduleReferences ?? new List<SLModuleReferencePackage>();
                    pkg.irStringDict = entry.irStringDict ?? new List<IRStringItem>();
                    pkg.namespaceList = entry.namespaceList ?? new List<SLNamespacePackage>();
                    pkg.classList = entry.classList ?? new List<SLClassPackage>();
                    pkg.globalStaticVariableList = entry.globalStaticVariableList ?? new List<SLGlobalStaticVariablePackage>();
                    pkg.methodList = entry.methodList ?? new List<SLMethodPackage>();
                    pkg.systemCalls = entry.systemCalls ?? new List<SLSystemCallPackage>();
                    if (string.IsNullOrWhiteSpace(pkg.moduleName))
                    {
                        pkg.moduleName = entry.moduleName ?? string.Empty;
                    }
                }
                return pkg;
            }

            // New format: flat SLModulePackage (no moduleList wrapper)
            return JsonSerializer.Deserialize<SLModulePackage>(json, options) ?? new SLModulePackage();
        }

        private static bool TryGetJsonArray(JsonElement root, string name)
        {
            if (root.ValueKind != JsonValueKind.Object) return false;
            foreach (var p in root.EnumerateObject())
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)
                    && p.Value.ValueKind == JsonValueKind.Array)
                {
                    return true;
                }
            }
            return false;
        }

        private static void StripInstructionCode(SLModulePackage pkg)
        {
            if (pkg == null) return;
            StripInstructionCode(pkg.globalStaticVariableList, pkg.methodList, pkg.classList);
            if (pkg.moduleList != null)
            {
                for (int i = 0; i < pkg.moduleList.Count; i++)
                {
                    var module = pkg.moduleList[i];
                    if (module == null) continue;
                    StripInstructionCode(module.globalStaticVariableList, module.methodList, module.classList);
                }
            }
        }

        private static void StripInstructionCode(
            List<SLGlobalStaticVariablePackage>? globals,
            List<SLMethodPackage>? methods,
            List<SLClassPackage>? classes)
        {
            if (globals != null)
            {
                for (int i = 0; i < globals.Count; i++)
                {
                    globals[i]?.express?.Clear();
                }
            }

            if (methods != null)
            {
                for (int i = 0; i < methods.Count; i++)
                {
                    methods[i]?.instructionList?.Clear();
                }
            }

            if (classes != null)
            {
                for (int c = 0; c < classes.Count; c++)
                {
                    var cls = classes[c];
                    if (cls?.fieldList == null) continue;
                    for (int f = 0; f < cls.fieldList.Count; f++)
                    {
                        cls.fieldList[f]?.express?.Clear();
                    }
                }
            }
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

        /// <summary>根据模块名生成稳定的 UUID（SHA256 前 16 字节的十六进制表示）。</summary>
        private static string GenerateModuleUUID(string moduleName)
        {
            var bytes = System.Security.Cryptography.SHA256.HashData(Encoding.UTF8.GetBytes(moduleName));
            var sb = new StringBuilder(32);
            for (int i = 0; i < 16; i++)
                sb.Append(bytes[i].ToString("x2"));
            return sb.ToString();
        }

        internal static SLModulePackage Build(IRManager ir, string moduleName)
        {
            var pkg = new SLModulePackage();
            var module = new SLAssemblyPackage(moduleName ?? string.Empty);

            // const strings (IRManager.AddStringIRStack) — exported AFTER all IR
            // generation below (instance field initializers, enum members, etc.
            // call AddStringIRStack during export, so the pool must be serialized
            // last to capture every string).
            // (Populated at the end of this method.)

            // types
            var classes = ir.GetIRMetaClassList();
            var nsMap = new Dictionary<string, SLNamespacePackage>(StringComparer.Ordinal);
            foreach (var c in classes)
            {
                if (c == null) continue;
                // 跳过模板实例化类（MetaGenTemplateClass）：它们是编译时按需生成的，
                // 不是源码声明的类型。导出它们会产生空壳（无字段/方法），
                // 导入后主工程按名解析到空壳，方法查找失败。
                // 模板定义类（Array<T>）正常导出，主工程用时再实例化。
                if (c.typeOwner is MetaGenTemplateClass) continue;
                // 跳过引用模块的类：它们已由被引用模块导出，不应在当前模块中重复导出。
                if (c.isRefModulePreBuilt) continue;
                if (c.typeOwner?.refFromType == RefFromType.RefModule) continue;
                // Keep module prefix in fullName so that classes from different modules
                // (e.g. Core.Object vs ProjectTest.Object) have distinct fullNames.
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
                    exportNames = c.exportNames,
                    sourcePath = c.sourcePath ?? string.Empty,
                    metaClassKind = (int)c.metaClassKind,
                    isDynamic = c.OwnerMetaData?.isDynamic ?? false,
                    baseClassId = c.OwnerMetaClass?.extendClass?.classId ?? 0,
                };
                // export class-level attributes
                ExportAttributes(cm.attributeList, c.OwnerMetaClass?.attributeList);
                // export field-level attributes
                var implIds = c.GetImplementsInterfaceClassIds();
                if (implIds != null && implIds.Count > 0)
                {
                    for (int ii = 0; ii < implIds.Count; ii++)
                        cm.implementsInterfaceIdList.Add(implIds[ii]);
                }
                // export template count
                cm.templateCount = c.templateCount;
                cm.templateParameterCount = c.templateParameterCount;
                // export real declared template parameter names (e.g. TKey/TValue of Map<TKey,TValue>).
                // The importer rebuilds MetaTemplate with these names so that allName and its FNV
                // classId match on both sides; a T/T1 fallback would break classId-based lookup.
                var expTplNames = c.OwnerMetaClass?.metaTemplateList;
                if (expTplNames != null)
                {
                    for (int ti = 0; ti < expTplNames.Count; ti++)
                    {
                        var tn = expTplNames[ti]?.name;
                        cm.templateParameterNames.Add(string.IsNullOrWhiteSpace(tn)
                            ? (ti == 0 ? "T" : "T" + ti.ToString())
                            : tn);
                    }
                }
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
                // export per-class method references: static, non-static and operator methods.
                // 继承来的方法也写入子类包（虚表需要），但其归属由 SLMethodPackage.declaringClassId
                // 标记为声明类；导入侧 BuildMetaMemberFunctionFromIR 按 declaringClassId 设置 owner。
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
                            exportNames = v.exportNames,
                            typeDef = CreateRuntimeDefTypePackage(v.irMetaType),
                            flags = BuildFieldFlags(v),
                            index = v.index,
                            order = v.order,
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
                                    if (express is MetaExpressNodeBase enumMemberExpress)
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
                            if (irBufLocal.Count == 0 && v.express != null)
                            {
                                try
                                {
                                    var iexField = IRExpressManager.CreateExpress(null, v.express);
                                    if (iexField?.IRDataList != null)
                                    {
                                        foreach (var d in iexField.IRDataList)
                                        {
                                            if (d == null) continue;
                                            irBufLocal.Add(d);
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                        // Match IRMetaClass.CreateStaticMetaMetaVariableIRList: expr IR then StoreNotStaticField1 for instance fields.
                        if (irBufLocal.Count > 0)
                        {
                            AppendClassInstanceFieldStoreIfNeeded(v, irBufLocal);
                            TryFuseConstInstanceFieldInitializer(v, irBufLocal);
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
                        var fieldPkgStatic = new SLFieldPackage
                        {
                            name = GetShortName(v.name ?? string.Empty),
                            exportNames = v.exportNames,
                            typeDef = CreateRuntimeDefTypePackage(v.irMetaType),
                            flags = BuildFieldFlags(v) | 32,
                            index = v.index,
                            order = v.order,
                        };
                        var irBufStatic = new List<IRData>();
                        if (v.irDataList != null && v.irDataList.Count > 0)
                        {
                            irBufStatic.AddRange(v.irDataList);
                        }
                        // Match IRMetaClass.CreateStaticMetaMetaVariableIRList: expr IR then StoreStaticField for class statics.
                        if (irBufStatic.Count == 0)
                        {
                            TryBuildDataStaticDefaultInitializer(v, irBufStatic, curirmt);
                        }
                        if (irBufStatic.Count > 0)
                        {
                            AppendClassStaticFieldStoreIfNeeded(v, curirmt, irBufStatic);
                            TryRewriteArrayStaticInitializer(v, irBufStatic, curirmt);
                            // 常数融合必须在数组展开之后（Array 类型不能按标量融合）
                            TryFuseConstStaticInitializer(v, irBufStatic);
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
                        TryFuseConstGlobalInitializer(gv, irListForExpr);
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
                // ref module 函数的 IR body 已在编译后的模块中，不重导出
                if (m.bindMetaFunction?.refFromType == RefFromType.RefModule) continue;

                // 声明类：取 bindMetaFunction.ownerMetaBase（方法的真正声明类），
                // 对于继承到子类的方法，这里指向父类（如 Object）而非当前子类（如 Num）。
                // 用 ownerMetaBase（MetaBase）而非 ownerMetaClass，Data/Enum 方法也能拿到声明类。
                var declaringOwner = m.bindMetaFunction?.ownerMetaBase;
                var declaringTypeFullName = declaringOwner != null
                    ? StripModulePrefix(NormalizeTypeName(declaringOwner.allName ?? string.Empty))
                    : (m.irOwnerMetaClass?.irName ?? string.Empty);
                declaringTypeFullName = StripModulePrefix(NormalizeTypeName(declaringTypeFullName));
                var mp = new SLMethodPackage
                {
                    id = m.id ?? string.Empty,
                    declaringTypeFullName = declaringTypeFullName,
                    declaringClassId = declaringOwner?.classId ?? 0,
                    name = m.onlyFunctionName ?? string.Empty,
                    exportNames = m.exportNames,
                    interfaceMethod = m.interfaceMethod,
                    flags = BuildMethodFlags(m),
                    isTemplateFunction = m.isTemplateFunction,
                    templateParameterNames = m.isTemplateFunction ? new List<string>(m.templateParameterNames) : new List<string>(),
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
                            name = NormalizeVariableName(v.name ?? string.Empty),
                            typeDef = CreateRuntimeDefTypePackage(v.irMetaType),
                            debugInfo = CreateVariableDebugInfo(v),
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
                            name = NormalizeVariableName(v.name ?? string.Empty),
                            typeDef = CreateRuntimeDefTypePackage(v.irMetaType),
                            debugInfo = CreateVariableDebugInfo(v),
                            hasExpress = v.isHasExpress,
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
                            name = NormalizeVariableName(v.name ?? string.Empty),
                            typeDef = CreateRuntimeDefTypePackage(v.irMetaType),
                            debugInfo = CreateVariableDebugInfo(v),
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

                // export method-level attributes
                if (m.bindMetaFunction is MetaMemberFunction mmf)
                {
                    ExportAttributes(mp.attributeList, mmf.attributeList);
                }

                module.methodList.Add(mp);

                if (bestEntry == null && string.Equals(m.onlyFunctionName, "_main_", StringComparison.OrdinalIgnoreCase))
                {
                    if (m.IRDataList != null && m.IRDataList.Count > 0)
                        bestEntry = m.id;
                }
            }

            module.entryMethodId = bestEntry;

            // const strings (IRManager.AddStringIRStack) - must be populated AFTER
            // all IR generation (instance field initializers at lines ~380-469,
            // enum member expressions, method bodies, etc. all call
            // AddStringIRStack during this Build). Exporting here ensures every
            // string constant is captured.
            foreach (var kv in ir.IRStringDict)
            {
                module.irStringDict.Add(new IRStringItem { id = kv.Key, value = kv.Value ?? string.Empty });
            }

            // Populate flat fields directly on SLModulePackage (no moduleList wrapper).
            pkg.moduleName = module.moduleName;
            // 生成基于模块名的稳定 UUID，确保 LoadConstString 的 per-module 字符串表能正确隔离
            pkg.uuid = string.IsNullOrEmpty(module.uuid)
                ? GenerateModuleUUID(module.moduleName ?? "module")
                : module.uuid;
            pkg.entryMethodId = module.entryMethodId;
            // Embed the module's own systemCalls verbatim so referencing projects
            // can register them when loading this package as a reference module.
            // Write to both SLModulePackage (new flat format) and SLAssemblyPackage (legacy format)

            string getMetaTypeString(MetaType mt)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(mt.metaClass.name);
                if(mt.GetGenTemplateMetaTypeList().Count > 0 )
                {
                    sb.Append("<");
                    for (int i = 0; i < mt.GetGenTemplateMetaTypeList().Count; i++)
                    {
                        sb.Append("object");
                    }
                    sb.Append(">");
                }
                return sb.ToString();
            }

            foreach ( var v in SystemMethodCallDeclarationRegistry.projectDefine)
            {
                if (v == null) continue;
                var decl = new SLSystemCallPackage
                {
                    name = v.name ?? string.Empty,
                    returnType = getMetaTypeString(v.returnMetaType),
                    isVariadic = v.isVariadic,
                    // Unique int id so the VM can register id -> implementation
                    // from this export and dispatch CallSystemMethod by id.
                    id = v.GetIndex(),
                    // C VM implementation symbol so the VM resolves the builtin
                    // by name instead of a hardcoded mapping table.
                    cvmFunction = v.cvmFunction ?? string.Empty,
                };
                foreach( var v2 in v.paramMetaTypeList )
                {
                    decl.@params.Add(getMetaTypeString(v2));
                }
                pkg.systemCalls.Add(decl);
                module.systemCalls.Add(decl);
            }
            pkg.moduleReferences = module.moduleReferences;
            pkg.irStringDict = module.irStringDict;
            pkg.namespaceList = module.namespaceList;
            pkg.classList = module.classList;
            pkg.globalStaticVariableList = module.globalStaticVariableList;
            pkg.methodList = module.methodList;

            // 从项目配置填充版本号
            var config = Project.ProjectManager.config;
            if (config != null)
            {
                pkg.versionMain = config.Export.VersionMain;
                pkg.versionSub = config.Export.VersionSub;
                pkg.versionPatch = config.Export.VersionPatch;
                pkg.nativeDll = config.Export.NativeDll ?? string.Empty;

                // 从项目配置的 dllImports 填充外部 dll 导入信息（别名/名称/路径），
                // 引用方加载本模块时合并进其配置即可用别名免写长路径
                if (config.DllImports != null)
                {
                    foreach (var d in config.DllImports)
                    {
                        if (d == null || string.IsNullOrWhiteSpace(d.Path)) continue;
                        pkg.dllImports.Add(new SLDllImportPackage
                        {
                            alias = d.Alias ?? string.Empty,
                            name = d.Name ?? string.Empty,
                            path = d.Path,
                        });
                    }
                }

                // 从项目配置的 references 填充引用关系（含 uuid、name、path、版本号）
                if (config.References != null)
                {
                    // 导出文件所在目录（用于计算引用模块的相对路径）
                    var exportDir = Environment.GetEnvironmentVariable(ProjectOutputEnvironment.ExportOutDirEnv) ?? "";
                    var projectDir = ProjectManager.projectPath ?? "";
                    foreach (var refSection in config.References)
                    {
                        if (refSection == null || string.IsNullOrWhiteSpace(refSection.Path)) continue;
                        var refPkg = new SLModuleReferencePackage
                        {
                            name = refSection.Name,
                            uuid = refSection.UUID,
                        };
                        // 尝试从已加载的引用模块包中读取版本号和缺失的 uuid/name
                        TryFillReferenceFromLoadedPackage(refSection, refPkg);
                        // 计算引用模块的相对路径（相对于导出文件所在目录）
                        refPkg.path = ResolveReferencePath(refSection.Path, projectDir, exportDir);
                        pkg.moduleReferences.Add(refPkg);
                    }
                }
            }

            // 合并 AOT manifest（aot.mlir / aot.dll / 方法状态清单）。
            // 数据来自 MLIRExportManager.Run 的最近一次结果（在 ExportLangManager.Export
            // 中先于本方法运行），旧 CVM 仍可读独立的 aot_manifest.json 作为回退。
            pkg.aot = MLIRExportManager.Instance.LastResult?.ToSlAotPackage();

            return pkg;
        }

        /// <summary>
        /// 从已加载的引用模块包中补全引用信息（版本号、缺失的 uuid/name）。
        /// 引用模块在 ProjectReferenceModuleLoader.LoadReferences 时已加载并缓存。
        /// </summary>
        private static void TryFillReferenceFromLoadedPackage(
            ProjectConfig.ReferenceSection refSection,
            SLModuleReferencePackage refPkg)
        {
            /* 先用配置中的 name 尝试查找；如果没有 name，尝试用路径推断。 */
            SLModulePackage loadedPkg = null;
            if (!string.IsNullOrWhiteSpace(refPkg.name))
            {
                loadedPkg = ProjectReferenceModuleLoader.GetLoadedPackage(refPkg.name);
            }
            /* 如果按 name 没找到，遍历所有已加载的包，用 uuid 匹配。 */
            if (loadedPkg == null && !string.IsNullOrWhiteSpace(refPkg.uuid))
            {
                loadedPkg = ProjectReferenceModuleLoader.GetLoadedPackageByUuid(refPkg.uuid);
            }

            if (loadedPkg != null)
            {
                if (string.IsNullOrWhiteSpace(refPkg.name))
                    refPkg.name = loadedPkg.moduleName;
                if (string.IsNullOrWhiteSpace(refPkg.uuid))
                    refPkg.uuid = loadedPkg.uuid;
                refPkg.versionMain = loadedPkg.versionMain;
                refPkg.versionSub = loadedPkg.versionSub;
                refPkg.versionPatch = loadedPkg.versionPatch;
            }
        }

        /// <summary>
        /// 计算引用模块的 .module.json 文件路径（相对于导出文件所在目录）。
        /// refPath 是 .jsonc 中相对于项目目录的路径（如 "../../out/export/Core"）。
        /// 返回从导出目录到引用模块 .module.json 的相对路径。
        /// </summary>
        private static string ResolveReferencePath(string refPath, string projectDir, string exportDir)
        {
            if (string.IsNullOrWhiteSpace(refPath)) return string.Empty;
            try
            {
                /* 将 refPath 解析为绝对路径 */
                var refAbsPath = Path.IsPathRooted(refPath)
                    ? refPath
                    : Path.GetFullPath(Path.Combine(projectDir, refPath));
                /* 如果是目录，尝试找到其中的 .module.json */
                if (Directory.Exists(refAbsPath))
                {
                    var files = Directory.GetFiles(refAbsPath, "*.module.json");
                    if (files.Length > 0) refAbsPath = files[0];
                }
                /* 计算从导出目录到引用模块的相对路径 */
                if (!string.IsNullOrWhiteSpace(exportDir))
                {
                    var rel = Path.GetRelativePath(exportDir, refAbsPath);
                    return rel.Replace('\\', '/');
                }
                return refAbsPath.Replace('\\', '/');
            }
            catch
            {
                return refPath.Replace('\\', '/');
            }
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

        private static string NormalizeVariableName(string rawName)
        {
            var name = GetShortName(rawName);
            var lb = name.LastIndexOf('[');
            if (lb >= 0 && name.EndsWith("]", StringComparison.Ordinal) && lb + 1 < name.Length - 1)
            {
                return name.Substring(lb + 1, name.Length - lb - 2);
            }
            return name;
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
        /// After expression IR for a class static field, append <see cref="StoreStaticField"/>
        /// when missing — same order as <see cref="CreateStaticMetaMetaVariableIRList"/>.
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
            irdata.SetDebugInfoByValue(v.debugInfo);
            irBuf.Add(irdata);
        }

        /// <summary>
        /// After expression IR for an instance field default init, append <see cref="EIROpCode.StoreNotStaticField1"/>.
        /// </summary>
        private static void AppendClassInstanceFieldStoreIfNeeded(IRMetaVariable v, List<IRData> irBuf)
        {
            if (v == null || irBuf == null || irBuf.Count == 0) return;
            //var last = irBuf[irBuf.Count - 1];
            //if (last != null && last.opCode == EIROpCode.StoreNotStaticField1) return;
            if (v.irMetaType == null) return;
            var irdata = new IRData
            {
                id = irBuf.Count,
                opValue = v.irMetaType,
                opCode = EIROpCode.StoreNotStaticField1,
                index = v.index,
            };
            irdata.SetDebugInfoByValue(v.debugInfo);
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
            irdata.SetDebugInfoByValue(gv.debugInfo);
            irBuf.Add(irdata);
        }

        private static int BuildMethodFlags(IRMethod m)
        {
            if (m == null || m.bindMetaFunction == null) return 0;
            int flags = 0;

            if (m.bindMetaFunction is MetaMemberFunction mmf)
            {
                if (mmf.isStatic) flags |= 1;
                if (mmf.isFinal) flags |= 2;
                if (mmf.isAbstract) flags |= 4;
                if (mmf.isOverrideFunction) flags |= 8;
                if (mmf.isOverrideInterface) flags |= 16;
                if (mmf.isCanRewrite) flags |= 32;
                if (mmf.isConstructInitFunction) flags |= 64;
            }
            // 128: 最后一个参数为 params 可变参数（params object[]），导入端需要还原该标记，
            // 否则调用侧按可变参数匹配时识别不了该方法。
            if (m.bindMetaFunction.metaMemberParamCollection?.isExtendParams == true) flags |= 128;
            // 256: @AOT() 标记（AOT 预编译候选），导入端据此还原 IRMethod.isAot。
            if (m.isAot) flags |= 256;
            return flags;
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
            if (v.isStatic) flags |= 32;

            return flags;
        }

        private static void TryBuildDataStaticDefaultInitializer(IRMetaVariable v, List<IRData> irBuf, IRMetaType ownerType)
        {
            if (v?.irMetaType?.irMetaClass == null || irBuf == null || irBuf.Count > 0) return;
            if (v.irMetaType.irMetaClass.metaClassKind != IRMetaClassKind.Data) return;

            // global.data object field without explicit expression:
            // construct data object first, then StoreStaticField by owner class+field index.
            var irNew = new IRData
            {
                id = 0,
                opCode = EIROpCode.NewObject,
                opValue = v.irMetaType,
                index = 0,
            };
            irNew.SetDebugInfoByValue(v.debugInfo);
            irBuf.Add(irNew);
        }

        private static void TryRewriteArrayStaticInitializer(IRMetaVariable v, List<IRData> irBuf, IRMetaType ownerType)
        {
            if (v?.irMetaType?.irMetaClass == null || irBuf == null || irBuf.Count == 0) return;
            var typeName = StripModulePrefix(v.irMetaType.irMetaClass.irName ?? string.Empty);
            if (!typeName.StartsWith("Array", StringComparison.Ordinal)) return;

            bool hasArrayCtor = irBuf.Exists(d => d != null && (d.opCode == EIROpCode.NewArray || d.opCode == EIROpCode.NewTemplateObject));
            if (hasArrayCtor) return;

            // Pattern seen in broken export: [const..., StoreStaticField]
            // Rebuild to: len -> NewArray -> (Dup, value, StoreArrayIndex)* -> StoreStaticField.
            int tailStore = irBuf.Count - 1;
            bool hasTailStore = tailStore >= 0 && irBuf[tailStore] != null && irBuf[tailStore].opCode == EIROpCode.StoreStaticField;
            int valueCount = hasTailStore ? tailStore : irBuf.Count;
            if (valueCount <= 0) return;

            for (int i = 0; i < valueCount; i++)
            {
                var op = irBuf[i]?.opCode ?? EIROpCode.Nop;
                bool isConst =
                    op == EIROpCode.LoadConstNull
                    || op == EIROpCode.LoadConstUInt8
                    || op == EIROpCode.LoadConstInt8
                    || op == EIROpCode.LoadConstInt16
                    || op == EIROpCode.LoadConstUInt16
                    || op == EIROpCode.LoadConstInt32
                    || op == EIROpCode.LoadConstUInt32
                    || op == EIROpCode.LoadConstInt64
                    || op == EIROpCode.LoadConstUInt64
                    || op == EIROpCode.LoadConstFloat32
                    || op == EIROpCode.LoadConstFloat64
                    || op == EIROpCode.LoadConstBoolean
                    || op == EIROpCode.LoadConstString;
                if (!isConst) return;
            }

            List<IRData> rebuilt = new List<IRData>();
            var lenData = new IRData
            {
                id = rebuilt.Count,
                opCode = EIROpCode.LoadConstInt32,
                opValue = valueCount,
                index = 0,
            };
            lenData.SetDebugInfoByValue(v.debugInfo);
            rebuilt.Add(lenData);
            var newArrData = new IRData
            {
                id = rebuilt.Count,
                opCode = EIROpCode.NewArray,
                opValue = v.irMetaType,
                index = 0,
            };
            newArrData.SetDebugInfoByValue(v.debugInfo);
            rebuilt.Add(newArrData);

            for (int i = 0; i < valueCount; i++)
            {
                var dupData = new IRData
                {
                    id = rebuilt.Count,
                    opCode = EIROpCode.Dup,
                    index = 0,
                };
                dupData.SetDebugInfoByValue(v.debugInfo);
                rebuilt.Add(dupData);
                var src = irBuf[i];
                var copied = new IRData
                {
                    id = rebuilt.Count,
                    opCode = src.opCode,
                    index = src.index,
                    opValue = src.opValue,
                    // Preserve original source location of the constant.
                    debugInfo = src.debugInfo,
                };
                if (src.Payload != null && src.Payload.Length > 0)
                {
                    copied.Payload = new byte[src.Payload.Length];
                    Buffer.BlockCopy(src.Payload, 0, copied.Payload, 0, src.Payload.Length);
                    copied.ByteLength = src.ByteLength;
                }
                rebuilt.Add(copied);
                var storeArrData = new IRData
                {
                    id = rebuilt.Count,
                    opCode = EIROpCode.StoreArrayIndex,
                    opValue = true,
                    index = i,
                };
                storeArrData.SetDebugInfoByValue(v.debugInfo);
                rebuilt.Add(storeArrData);
            }

            if (hasTailStore)
            {
                var tailStoreData = new IRData
                {
                    id = rebuilt.Count,
                    opCode = EIROpCode.StoreStaticField,
                    opValue = ownerType,
                    index = v.index,
                };
                tailStoreData.SetDebugInfoByValue(v.debugInfo);
                rebuilt.Add(tailStoreData);
            }

            irBuf.Clear();
            irBuf.AddRange(rebuilt);
        }

        /// <summary>
        /// O3 常数融合（导出路径）：从经典 LoadConst* 提取 [etype:1][value:N]。
        /// 非字符串常量直接复用创建时 SetOpValue 打包出的经典布局字节；
        /// String 的 id 在 index 字段（创建时 opValue=null、Payload=null）；
        /// Null 无 value 字节（C VM 侧 VM_ETYPE_LANG_NULL 不读 value）。
        /// </summary>
        private static bool TryBuildConstValuePayload(IRData loadData, out byte[] payload)
        {
            payload = null;
            if (loadData == null) return false;
            byte etype;
            byte[] valueBytes = null;
            switch (loadData.opCode)
            {
                case EIROpCode.LoadConstNull:
                    etype = (byte)EType.Null;
                    valueBytes = Array.Empty<byte>();
                    break;
                case EIROpCode.LoadConstBoolean: etype = (byte)EType.Boolean; break;
                case EIROpCode.LoadConstUInt8: etype = (byte)EType.UInt8; break;
                case EIROpCode.LoadConstInt8: etype = (byte)EType.Int8; break;
                case EIROpCode.LoadConstInt16: etype = (byte)EType.Int16; break;
                case EIROpCode.LoadConstUInt16: etype = (byte)EType.UInt16; break;
                case EIROpCode.LoadConstInt32: etype = (byte)EType.Int32; break;
                case EIROpCode.LoadConstUInt32: etype = (byte)EType.UInt32; break;
                case EIROpCode.LoadConstInt64: etype = (byte)EType.Int64; break;
                case EIROpCode.LoadConstUInt64: etype = (byte)EType.UInt64; break;
                case EIROpCode.LoadConstFloat8_E4M3: etype = (byte)EType.Float8; break;
                case EIROpCode.LoadConstFloat8_E5M2: etype = (byte)EType.Float8_E5M2; break;
                case EIROpCode.LoadConstFloat16: etype = (byte)EType.Float16; break;
                case EIROpCode.LoadConstFloat16_Brain: etype = (byte)EType.Float16_Brain; break;
                case EIROpCode.LoadConstFloat32: etype = (byte)EType.Float32; break;
                case EIROpCode.LoadConstFloat64: etype = (byte)EType.Float64; break;
                case EIROpCode.LoadConstString:
                    etype = (byte)EType.String;
                    valueBytes = BitConverter.GetBytes(loadData.index);
                    break;
                default:
                    return false;
            }
            if (valueBytes == null)
            {
                valueBytes = loadData.Payload;
                if (valueBytes == null || valueBytes.Length == 0) return false;
            }
            payload = new byte[valueBytes.Length + 1];
            payload[0] = etype;
            Buffer.BlockCopy(valueBytes, 0, payload, 1, valueBytes.Length);
            return true;
        }

        /// <summary>
        /// O3 类静态字段初始化融合：[LoadConst*][StoreStaticField] → StoreStaticFieldConstValue。
        /// payload（EmbedIndexInPayload 后）：[field:4][etype:1][value:N][owner runtimeDefType]，
        /// owner 后缀沿用原 StoreStaticField 的 Payload（"self" 4 字节或 runtimeDefType JSON），
        /// 保证 C VM 静态成员解析路径与经典指令完全一致。
        /// 必须在 <see cref="TryRewriteArrayStaticInitializer"/> 之后调用：
        /// Array 类型字段要先展开成 NewArray 序列（展开后 count>2 天然跳过融合）。
        /// </summary>
        private static void TryFuseConstStaticInitializer(IRMetaVariable v, List<IRData> irBuf)
        {
            // -O3 未开启时必须走原逻辑
            if (ProjectManager.optimizeLevel < 3) return;
            if (v == null || irBuf == null || irBuf.Count != 2) return;
            var loadData = irBuf[0];
            var storeData = irBuf[1];
            if (loadData == null || storeData == null) return;
            if (storeData.opCode != EIROpCode.StoreStaticField) return;
            // Array 类型由 TryRewriteArrayStaticInitializer 处理（先跑），此处双保险跳过
            var typeName = StripModulePrefix(v.irMetaType?.irMetaClass?.irName ?? string.Empty);
            if (typeName.StartsWith("Array", StringComparison.Ordinal)) return;
            var ownerBytes = storeData.Payload; // 创建时 opValue setter 已打包（"self" 或 IRMetaType JSON）
            if (ownerBytes == null || ownerBytes.Length == 0) return;
            if (!TryBuildConstValuePayload(loadData, out var constPayload)) return;
            var fused = new IRData
            {
                id = loadData.id,
                opCode = EIROpCode.StoreStaticFieldConstValue,
                index = storeData.index,
                debugStaticOwnerIrName = storeData.debugStaticOwnerIrName,
            };
            fused.Payload = new byte[constPayload.Length + ownerBytes.Length];
            Buffer.BlockCopy(constPayload, 0, fused.Payload, 0, constPayload.Length);
            Buffer.BlockCopy(ownerBytes, 0, fused.Payload, constPayload.Length, ownerBytes.Length);
            fused.UpdateByteLength();
            fused.SetDebugInfoByValue(v.debugInfo);
            irBuf.Clear();
            irBuf.Add(fused);
        }

        /// <summary>
        /// O3 实例字段默认值融合：[LoadConst*][StoreNotStaticField1] → StoreNotStaticField1ConstValue。
        /// payload（EmbedIndexInPayload 后）：[field:4][etype:1][value:N]；初始化器子 VM 栈底
        /// 已 push 新实例，C VM 侧 peek 栈顶实例写入字段，与经典指令语义一致（实例不入栈出栈）。
        /// </summary>
        private static void TryFuseConstInstanceFieldInitializer(IRMetaVariable v, List<IRData> irBuf)
        {
            // -O3 未开启时必须走原逻辑
            if (ProjectManager.optimizeLevel < 3) return;
            if (v == null || irBuf == null || irBuf.Count != 2) return;
            var loadData = irBuf[0];
            var storeData = irBuf[1];
            if (loadData == null || storeData == null) return;
            if (storeData.opCode != EIROpCode.StoreNotStaticField1) return;
            if (!TryBuildConstValuePayload(loadData, out var constPayload)) return;
            var fused = new IRData
            {
                id = loadData.id,
                opCode = EIROpCode.StoreNotStaticField1ConstValue,
                index = storeData.index,
            };
            fused.Payload = constPayload;
            fused.UpdateByteLength();
            fused.SetDebugInfoByValue(v.debugInfo);
            irBuf.Clear();
            irBuf.Add(fused);
        }

        /// <summary>
        /// O3 全局变量初始化融合：[LoadConst*][StoreGlobal] → StoreGlobalConstValue。
        /// payload（EmbedIndexInPayload 后）：[id:4][etype:1][value:N]，id 为全局变量 id。
        /// </summary>
        private static void TryFuseConstGlobalInitializer(IRMetaVariable gv, List<IRData> irBuf)
        {
            // -O3 未开启时必须走原逻辑
            if (ProjectManager.optimizeLevel < 3) return;
            if (gv == null || irBuf == null || irBuf.Count != 2) return;
            var loadData = irBuf[0];
            var storeData = irBuf[1];
            if (loadData == null || storeData == null) return;
            if (storeData.opCode != EIROpCode.StoreGlobal) return;
            if (!TryBuildConstValuePayload(loadData, out var constPayload)) return;
            var fused = new IRData
            {
                id = loadData.id,
                opCode = EIROpCode.StoreGlobalConstValue,
                index = storeData.index,
            };
            fused.Payload = constPayload;
            fused.UpdateByteLength();
            fused.SetDebugInfoByValue(gv.debugInfo);
            irBuf.Clear();
            irBuf.Add(fused);
        }

        /// <summary>
        /// Copies packed <see cref="IRData"/> to the JSON wire DTO (VM loads the same JSON into <c>Instruction</c>).
        /// </summary>
        private static SLIRInstructionPackage CreateInstructionPackage(IRData d)
        {
            if (d == null) throw new ArgumentNullException(nameof(d));
            try { d.FinalizePack(); } catch { /* best-effort */ }
            try { d.EmbedIndexInPayload(); } catch { /* best-effort */ }

            SLInstructionDebugInfo? dbg = null;
            var src = d.debugInfo;
            if (!string.IsNullOrEmpty(src.path) || !string.IsNullOrEmpty(src.name) || !string.IsNullOrEmpty(src.info)
                || src.beginLine != 0 || src.beginChar != 0 || src.endLine != 0 || src.endChar != 0)
            {
                dbg = new SLInstructionDebugInfo
                {
                    path = src.path ?? string.Empty,
                    name = src.name ?? string.Empty,
                    beginLine = src.beginLine,
                    beginChar = src.beginChar,
                    endLine = src.endLine,
                    endChar = src.endChar,
                    info = src.info ?? string.Empty,
                };
            }

            return new SLIRInstructionPackage
            {
                id = d.id,
                opCode = (byte)d.opCode,
                byteLength = d.ByteLength,
                payload = d.Payload,
                debugInfo = dbg,
            };
        }

        private static SLInstructionDebugInfo? CreateVariableDebugInfo(IRMetaVariable? v)
        {
            if (v == null) return null;

            var src = v.debugInfo;
            var hasData = !string.IsNullOrEmpty(src.path)
                || !string.IsNullOrEmpty(src.name)
                || !string.IsNullOrEmpty(src.info)
                || src.beginLine != 0 || src.beginChar != 0 || src.endLine != 0 || src.endChar != 0;

            if (!hasData) return null;

            return new SLInstructionDebugInfo
            {
                path = src.path ?? string.Empty,
                name = src.name ?? string.Empty,
                beginLine = src.beginLine,
                beginChar = src.beginChar,
                endLine = src.endLine,
                endChar = src.endChar,
                info = src.info ?? string.Empty,
            };
        }

        /// <summary>
        /// Exports MetaAttribute list to SLAttributePackage list for JSON serialization.
        /// Each attribute's name and extracted string arguments are preserved so the VM
        /// loader can reconstruct runtime attributes (Route, Condition, etc.).
        /// </summary>
        private static void ExportAttributes(List<SLAttributePackage> dest, List<Core.MetaAttribute>? src)
        {
            if (dest == null || src == null) return;
            foreach (var attr in src)
            {
                if (attr == null || string.IsNullOrEmpty(attr.name)) continue;
                // Ensure Parse() has been called so stringArgs is populated
                attr.Parse();
                var pkg = new SLAttributePackage { name = attr.name, handleType = attr.handleType };
                if (attr.stringArgs != null)
                {
                    foreach (var arg in attr.stringArgs)
                        pkg.args.Add(arg ?? string.Empty);
                }
                dest.Add(pkg);
            }
        }

        private static SLRuntimeDefTypePackage? CreateRuntimeDefTypePackage(IRMetaType? mt)
        {
            if (mt == null) return null;

            bool isTemplateSlot = mt.templateIndex >= 0;
            var ret = new SLRuntimeDefTypePackage
            {
                classId = isTemplateSlot ? 0 : (mt.irMetaClass?.id ?? 0),
                className = isTemplateSlot ? ("T[" + mt.templateIndex + "]") : StripModulePrefix(NormalizeTypeName(mt.irMetaClass?.irName ?? string.Empty)),
                ownerClassId = mt.irOwnerMetaClass?.id ?? 0,
                ownerClassName = StripModulePrefix(NormalizeTypeName(mt.irOwnerMetaClass?.irName ?? string.Empty)),
                templateIndex = mt.templateIndex,
                isTemplate = isTemplateSlot,
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
