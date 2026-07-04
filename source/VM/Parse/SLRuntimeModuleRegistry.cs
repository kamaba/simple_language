using SimpleLanguage.Logging;
using SimpleLanguage.VM;
using SimpleLanuageVM.Load;
using System.Diagnostics;
using System.Text.Json;

namespace SimpleLanguage.Parse
{
    public static class SLRuntimeModuleRegistry
    {
        private static readonly Dictionary<string, RuntimeMethod> s_MethodById = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> s_MethodDeclaringTypeById = new(StringComparer.Ordinal);
        // Preserve package-level class info so runtime can lookup original exported
        // class metadata on demand (used to register RuntimeClass when missing).
        private static readonly Dictionary<int, SLClassPackage> s_ClassPackageById = new();

        public static void Clear()
        {
            s_MethodById.Clear();
            s_MethodDeclaringTypeById.Clear();
            s_ClassPackageById.Clear();
        }

        private static void ApplyRuntimeClassShellMetadata(RuntimeClass rc, SLClassPackage pkg)
        {
            if (rc == null || pkg == null) return;

            var fullName = string.IsNullOrWhiteSpace(pkg.fullName) ? pkg.name : pkg.fullName;
            rc.id = pkg.id;
            rc.name = fullName;
            rc.metaClassKind = pkg.metaClassKind;
            rc.isDynamicData = pkg.isDynamic;
            rc.fieldsFromPackageApplied = false;

            if (pkg.implementsInterfaceIdList != null)
            {
                for (int i = 0; i < pkg.implementsInterfaceIdList.Count; i++)
                    rc.AddImplementsInterfaceId(pkg.implementsInterfaceIdList[i]);
            }

            s_ClassPackageById[pkg.id] = pkg;
        }

        private static RuntimeDefType ResolveRuntimeDefType(SLRuntimeDefTypePackage? pkg)
        {
            if (pkg == null) return null;

            // 解析 data/class 类型定义时，先确保 RuntimeClass 壳存在，
            // 再继续递归解析模板参数/元素，避免在类型图构建阶段提前递归字段。
            var rc = ResolveOrCreateRuntimeClassShellByIdOrName(pkg.classId, pkg.className);
            if (rc == null) return null;

            var args = new List<RuntimeDefType>();
            if (pkg.runtimeDefTypeList != null)
            {
                for (int i = 0; i < pkg.runtimeDefTypeList.Count; i++)
                {
                    var t = ResolveRuntimeDefType(pkg.runtimeDefTypeList[i]);
                    if (t != null) args.Add(t);
                }
            }

            // Match Front IRMetaType / SLModulePackageWriter wire: owner + template slot for inheritance / template refs.
            bool hasOwnerOrTemplate = pkg.ownerClassId != 0
                || !string.IsNullOrWhiteSpace(pkg.ownerClassName)
                || pkg.templateIndex >= 0
                || pkg.isTemplate;

            if (hasOwnerOrTemplate)
            {
                RuntimeClass? ownerRc = null;
                if (pkg.ownerClassId != 0 || !string.IsNullOrWhiteSpace(pkg.ownerClassName))
                    ownerRc = ResolveOrCreateRuntimeClassByIdOrName(pkg.ownerClassId, pkg.ownerClassName);

                return new RuntimeDefType(rc, args, ownerRc, pkg.templateIndex);
            }

            return new RuntimeDefType(rc, args);
        }

        //public static void LoadFromPackage(SLPackageRootJson pkg)
        //{
        //    if (pkg == null) throw new ArgumentNullException(nameof(pkg));
        //    Clear();
        //    AddFromPackage(pkg);
        //}

        public static void LoadFromPackages(IEnumerable<SLPackageRootJson> packages)
        {
            if (packages == null) throw new ArgumentNullException(nameof(packages));
            Clear();

            foreach (var pkg in packages)
            {
                if (pkg == null) continue;
                AddFromPackage(pkg);
            }
        }

        private static void AddFromPackage(SLPackageRootJson pkg)
        {
            // cache class packages from each module node
            void CacheClassList(List<SLClassPackage>? classList)
            {
                if (classList == null) return;
                foreach (var c in classList)
                {
                    if (c == null) continue;
                    if (!s_ClassPackageById.ContainsKey(c.id))
                        s_ClassPackageById[c.id] = c;
                }
            }

            if (pkg?.moduleList != null)
            {
                for (int mi = 0; mi < pkg.moduleList.Count; mi++)
                {
                    CacheClassList(pkg.moduleList[mi]?.classList);
                }
            }

            // Support moduleList outer shape: flatten modules to process methods and class lists
            var modulesToProcess = pkg?.moduleList != null
                ? new List<SLModulePackage>(pkg.moduleList)
                : new List<SLModulePackage>();

            // Phase A: register every exported RuntimeClass shell (id/name/metaClassKind only) so
            // ResolveRuntimeDefType during field & method setup can always find RuntimeClass by id.
            foreach (var module in modulesToProcess)
            {
                if (module.classList == null) continue;
                foreach (var c in module.classList)
                {
                    if (c == null) continue;
                    RegisterRuntimeClassShellFromPackage(c);
                }
            }

            // Phase A.5: template class relations (m_IRMetaClassMapTemplateDict) — must run after all shells, before field type resolution uses GetClassRuntimeType.
            foreach (var module in modulesToProcess)
            {
                if (module.classList == null) continue;
                foreach (var c in module.classList)
                {
                    if (c?.templateRelationList == null || c.templateRelationList.Count == 0) continue;
                    var classCandidates = GetRuntimeClassCandidates(c);
                    if (classCandidates.Count == 0) continue;
                    foreach (var rel in c.templateRelationList)
                    {
                        if (rel == null) continue;
                        for (int ci = 0; ci < classCandidates.Count; ci++)
                        {
                            var rc = classCandidates[ci];
                            // Keep direct inheritance/interface relation even when mapping is empty (non-generic relation).
                            rc.EnsureTemplateRelationClass(rel.relatedClassId);
                            if (rel.mapping == null) continue;
                            foreach (var ent in rel.mapping)
                            {
                                if (ent == null) continue;
                                var rdt = ResolveRuntimeDefType(ent.type);
                                if (rdt != null)
                                    rc.SetTemplateRelation(rel.relatedClassId, ent.index, rdt);
                            }
                        }
                    }
                }
            }

            // Phase B: populate fieldList / static init IR for each class (all shells exist).
            foreach (var module in modulesToProcess)
            {
                if (module.classList == null) continue;
                foreach (var c in module.classList)
                {
                    if (c == null) continue;
                    var rc = RuntimeClassManager.GetRuntimeClassById(c.id);
                    if (rc != null)
                        PopulateRuntimeClassFieldsFromPackage(c, rc);
                }
            }

            // Phase C: register all method bodies and variable types (ResolveRuntimeDefType sees full shells + fields).
            foreach (var module in modulesToProcess)
            {
                if (module.methodList == null) continue;
                foreach (var m in module.methodList)
                {
                    if (m == null || string.IsNullOrEmpty(m.id)) continue;

                    var rm = new RuntimeMethod
                    {
                        id = m.id,
                        onlyFunctionName = m.name ?? string.Empty,
                    };
                    rm.SetInterfaceMethodFlag(m.interfaceMethod);

                    // instructions（JSON 仅带 Payload；与 IRData 解包对称）
                    if (m.instructionList != null)
                    {
                        //Instruction.UnpackPayloadsFromJson(m.instructionList);
                        rm.InstructionList.AddRange(m.instructionList);
                    }

                    if (m.returnList != null)
                    {
                        foreach (var v in m.returnList)
                        {
                            if (v == null) continue;
                            var rdt = v.typeDef != null ? ResolveRuntimeDefType(v.typeDef) : null;
                            rm.methodReturnVariableList.Add(new RuntimeVariable(rdt, v.id, v.index, v.name, v.debugInfo));
                        }
                    }
                    if (m.argumentList != null)
                    {
                        foreach (var v in m.argumentList)
                        {
                            if (v == null) continue;
                            var rdt = v.typeDef != null ? ResolveRuntimeDefType(v.typeDef) : null;
                            rm.methodArgumentList.Add(new RuntimeVariable(rdt, v.id, v.index, v.name, v.debugInfo));
                        }
                    }
                    if (m.localList != null)
                    {
                        foreach (var v in m.localList)
                        {
                            if (v == null) continue;
                            var rdt = v.typeDef != null ? ResolveRuntimeDefType(v.typeDef) : null;
                            rm.methodLocalVariableList.Add(new RuntimeVariable(rdt, v.id, v.index, v.name, v.debugInfo));
                        }
                    }

                    s_MethodById[rm.id] = rm;
                    s_MethodDeclaringTypeById[rm.id] = m.declaringTypeFullName ?? string.Empty;
                }
            }

            // Phase D: bind instance / operator method references. Requires s_MethodById from phase C.
            foreach (var module in modulesToProcess)
            {
                if (module.classList == null) continue;
                foreach (var c in module.classList)
                {
                    if (c == null) continue;
                    var rc = RuntimeClassManager.GetRuntimeClassById(c.id);
                    if (rc == null)
                        rc = RegisterRuntimeClassShellFromPackage(c);
                    if (rc != null)
                        BindRuntimeClassMethodsFromClassPackage(c, rc);
                }
            }
        }

        /// <summary>Creates and registers a minimal <see cref="RuntimeClass"/> if missing. Does not touch fields.</summary>
        private static RuntimeClass? RegisterRuntimeClassShellFromPackage(SLClassPackage pkg)
        {
            if (pkg == null) return null;

            var existed = RuntimeClassManager.GetRuntimeClassById(pkg.id);
            if (existed != null)
            {
                // 可能是前序按 id/名称创建的占位壳，这里必须回填完整 class 元信息。
                ApplyRuntimeClassShellMetadata(existed, pkg);
                return existed;
            }

            var fullName = string.IsNullOrWhiteSpace(pkg.fullName) ? pkg.name : pkg.fullName;
            var existedByName = RuntimeClassManager.GetRuntimeClassByName(fullName);
            if (existedByName != null)
            {
                // Core types may already be pre-created by name before package load.
                // Rebind to exported class id so templateRelationList can attach to the same RuntimeClass instance.
                ApplyRuntimeClassShellMetadata(existedByName, pkg);

                return existedByName;
            }

            var shortName = GetShortName(fullName);
            if (!string.IsNullOrWhiteSpace(shortName))
            {
                var existedByShortName = RuntimeClassManager.GetRuntimeClassByName(shortName);
                if (existedByShortName != null)
                {
                    ApplyRuntimeClassShellMetadata(existedByShortName, pkg);

                    return existedByShortName;
                }
            }

            var rc = new RuntimeClass
            {
                id = pkg.id,
                name = fullName,
                metaClassKind = pkg.metaClassKind,
                isDynamicData = pkg.isDynamic,
                fieldsFromPackageApplied = false,
            };
            if (pkg.implementsInterfaceIdList != null)
            {
                for (int i = 0; i < pkg.implementsInterfaceIdList.Count; i++)
                    rc.AddImplementsInterfaceId(pkg.implementsInterfaceIdList[i]);
            }
            RuntimeClassManager.AddRuntimeClass(rc);
            s_ClassPackageById[pkg.id] = pkg;

            return rc;
        }

        // TypeDef 解析专用：只保证 RuntimeClass 壳存在，不触发字段填充。
        private static RuntimeClass? ResolveOrCreateRuntimeClassShellByIdOrName(int classId, string? className)
        {
            RuntimeClass? rc = null;

            if (classId != 0)
            {
                rc = RuntimeClassManager.GetRuntimeClassById(classId);
                if (rc == null && s_ClassPackageById.TryGetValue(classId, out var pkg) && pkg != null)
                {
                    rc = RegisterRuntimeClassShellFromPackage(pkg);
                }
            }

            if (rc == null && !string.IsNullOrWhiteSpace(className))
            {
                rc = RuntimeClassManager.GetRuntimeClassByName(className)
                    ?? RuntimeClassManager.GetRuntimeClassByName(GetShortName(className));

                if (rc == null)
                {
                    // 名称路径仅建立壳，不做字段展开。
                    rc = ResolveOrCreateRuntimeClass(className);
                }

                if (rc != null && classId != 0 && rc.id != classId)
                {
                    rc.id = classId;
                }
            }

            if (rc == null && classId != 0)
            {
                rc = new RuntimeClass
                {
                    id = classId,
                    name = string.IsNullOrWhiteSpace(className) ? $"Class_{classId}" : className,
                };
                RuntimeClassManager.AddRuntimeClass(rc);
            }

            return rc;
        }

        private static List<RuntimeClass> GetRuntimeClassCandidates(SLClassPackage c)
        {
            var list = new List<RuntimeClass>();
            if (c == null) return list;

            void AddUnique(RuntimeClass? rc)
            {
                if (rc == null) return;
                for (int i = 0; i < list.Count; i++)
                {
                    if (ReferenceEquals(list[i], rc))
                        return;
                }
                list.Add(rc);
            }

            AddUnique(RuntimeClassManager.GetRuntimeClassById(c.id));

            var fullName = string.IsNullOrWhiteSpace(c.fullName) ? c.name : c.fullName;
            AddUnique(RuntimeClassManager.GetRuntimeClassByName(fullName));

            var shortName = GetShortName(fullName);
            if (!string.IsNullOrWhiteSpace(shortName))
                AddUnique(RuntimeClassManager.GetRuntimeClassByName(shortName));

            return list;
        }

        /// <summary>Fills <paramref name="rc"/> from <paramref name="pkg"/>.<c>fieldList</c> once per class.</summary>
        private static void PopulateRuntimeClassFieldsFromPackage(SLClassPackage pkg, RuntimeClass rc)
        {
            if (pkg == null || rc == null) return;
            if (rc.fieldsFromPackageApplied) return;

            rc.ClearFieldRuntimeState();

            if (pkg.implementsInterfaceIdList != null)
            {
                for (int i = 0; i < pkg.implementsInterfaceIdList.Count; i++)
                    rc.AddImplementsInterfaceId(pkg.implementsInterfaceIdList[i]);
            }

            // class-level template metadata
            rc.templateCount = pkg.templateCount;
            //rc.templateParameterCount = pkg.templateParameterCount;
            rc.templateDefTypeList.Clear();
            if (pkg.templateTypeList != null)
            {
                for (int i = 0; i < pkg.templateTypeList.Count; i++)
                {
                    var t = ResolveRuntimeDefType(pkg.templateTypeList[i]);
                    if (t != null)
                        rc.templateDefTypeList.Add(t);
                }
            }

            if (pkg.fieldList != null)
            {
                // Phase 1: 注册 RuntimeVariable —— 必须严格按声明顺序，
                // 保证 RuntimeClass 内部按 index 维护的槽位与 SLIR 包一致。
                foreach (var f in pkg.fieldList)
                {
                    if (f == null) continue;

                    RuntimeDefType rdt = null;
                    
                    if (f.typeDef != null)
                        rdt = ResolveRuntimeDefType(f.typeDef);

                    if( rdt == null )
                    {
                        Log.AddParseIRLog(LID.ShowMessageAssert, "");
                    }

                    var rv = new RuntimeVariable(rdt, f.GetHashCode(), f.index, f.name ?? string.Empty);
                    if ((f.flags & 32) == 32)
                    {
                        rc.AddStaticIRMetaVariableList(rv);
                    }
                    else
                    {
                        rc.AddNonStaticIRMetaVariableList(rv);
                    }
                }

                // Phase 2: 按 order 升序注入初始化表达式指令。
                // order 来自 MetaMemberVariable.parseOrder（首次进入 ParseMetaExpress 时分配），
                // 反映成员之间的实际依赖解析次序：被依赖者先获得较小 order，需要先执行其初始化。
                // 缺省 order(-1) 排在后面（视为"无显式依赖"），相同 order 内按原始声明顺序稳定排列。
                var orderedFields = new List<(SLFieldPackage field, int declIndex)>(pkg.fieldList.Count);
                for (int fi = 0; fi < pkg.fieldList.Count; fi++)
                {
                    var f = pkg.fieldList[fi];
                    if (f == null) continue;
                    orderedFields.Add((f, fi));
                }
                orderedFields.Sort((a, b) =>
                {
                    int ao = a.field.order;
                    int bo = b.field.order;
                    // -1 视为最大值，排到末尾
                    int akey = ao < 0 ? int.MaxValue : ao;
                    int bkey = bo < 0 ? int.MaxValue : bo;
                    int cmp = akey.CompareTo(bkey);
                    if (cmp != 0) return cmp;
                    return a.declIndex.CompareTo(b.declIndex);
                });

                foreach (var (f, _) in orderedFields)
                {
                    if (f.express == null || f.express.Count == 0) continue;
                    if ((f.flags & 32) == 32)
                    {
                        foreach (var ins in f.express)
                            rc.staticMemberVariableSetValueList.Add(ins);
                    }
                    else
                    {
                        foreach (var ins in f.express)
                            rc.AddNonStaticMemberVariableSetValueList(ins);
                    }
                }
            }

            rc.fieldsFromPackageApplied = true;
        }

        /// <summary>
        /// Attaches non-static and operator <see cref="RuntimeMethod"/> entries from the package to <paramref name="rc"/>.
        /// Call only after the global method registry pass has populated <see cref="s_MethodById"/>.
        /// </summary>
        private static void BindRuntimeClassMethodsFromClassPackage(SLClassPackage c, RuntimeClass rc)
        {
            if (c == null || rc == null) return;

            rc.ClearBoundMethods();

            if (c.nonStaticMethodList != null)
            {
                foreach (var mm in c.nonStaticMethodList)
                {
                    if (mm == null || string.IsNullOrWhiteSpace(mm.id)) continue;
                    if (!s_MethodById.TryGetValue(mm.id, out var runtimeMethod) || runtimeMethod == null) continue;
                    runtimeMethod.SetOwner(rc);
                    int idx = mm.index;
                    rc.AddNonStaticMethod(runtimeMethod);
                }
            }

            if (c.operatorMethodList != null)
            {
                foreach (var mm in c.operatorMethodList)
                {
                    if (mm == null || string.IsNullOrWhiteSpace(mm.id)) continue;
                    if (!s_MethodById.TryGetValue(mm.id, out var runtimeMethod) || runtimeMethod == null) continue;
                    runtimeMethod.SetOwner(rc);
                    int idx = mm.index;
                    rc.AddOperatorMethod(runtimeMethod);
                }
            }

            // static methods are not added to instance lists here; they remain in registry
        }
        public static RuntimeDefType? TryResolveRuntimeDefTypeFromInstruction(object? opValue, byte[]? payload = null)
        {
            if (opValue is Instruction ins && ins.TryGetRuntimeDefTypePackage(out var pkgFromInstruction))
            {
                return ResolveRuntimeDefType(pkgFromInstruction);
            }

            if (TryReadRuntimeDefTypePackage(opValue, out var pkg))
            {
                return ResolveRuntimeDefType(pkg);
            }

            if (payload != null && payload.Length > 0)
            {
                try
                {
                    var text = System.Text.Encoding.UTF8.GetString(payload);
                    if (!string.IsNullOrWhiteSpace(text) && text[0] == '{')
                    {
                        var parsed = JsonSerializer.Deserialize<SLRuntimeDefTypePackage>(text);
                        if (parsed != null)
                        {
                            return ResolveRuntimeDefType(parsed);
                        }
                    }
                }
                catch
                {
                }
            }

            return opValue as RuntimeDefType;
        }

        public static RuntimeCall? TryCreateRuntimeCallForInstruction(SLRuntimeCallPackage? callPkg, int fallbackParamCount)
        {
            if (callPkg != null)
            {
                var fromPkg = CreateRuntimeCall(callPkg, fallbackParamCount);
                if (fromPkg != null) return fromPkg;
            }

            //if (legacyOpValue is string methodId && !string.IsNullOrWhiteSpace(methodId))
            //{
            //    if (methodId.Length > 0 && methodId[0] == '{')
            //    {
            //        try
            //        {
            //            var embeddedFromString = JsonSerializer.Deserialize<SLRuntimeCallPackage>(methodId);
            //            if (embeddedFromString != null)
            //            {
            //                var fromEmbeddedString = CreateRuntimeCall(embeddedFromString, fallbackParamCount);
            //                if (fromEmbeddedString != null) return fromEmbeddedString;
            //            }
            //        }
            //        catch
            //        {
            //        }
            //    }
            //    return CreateRuntimeCallByMethodId(methodId, fallbackParamCount);
            //}

            //if (legacyOpValue is JsonElement je && je.ValueKind == JsonValueKind.String)
            //{
            //    var methodIdFromJson = je.GetString();
            //    if (!string.IsNullOrWhiteSpace(methodIdFromJson))
            //    {
            //        return CreateRuntimeCallByMethodId(methodIdFromJson, fallbackParamCount);
            //    }
            //}

            //if (legacyOpValue is JsonElement jo && jo.ValueKind == JsonValueKind.Object)
            //{
            //    try
            //    {
            //        var embedded = jo.Deserialize<SLRuntimeCallPackage>();
            //        if (embedded != null)
            //        {
            //            var fromEmbedded = CreateRuntimeCall(embedded, fallbackParamCount);
            //            if (fromEmbedded != null) return fromEmbedded;
            //        }

            //        if (jo.TryGetProperty("methodId", out var methodIdProp)
            //            && methodIdProp.ValueKind == JsonValueKind.String)
            //        {
            //            var methodIdFromObj = methodIdProp.GetString();
            //            if (!string.IsNullOrWhiteSpace(methodIdFromObj))
            //            {
            //                return CreateRuntimeCallByMethodId(methodIdFromObj, fallbackParamCount);
            //            }
            //        }
            //    }
            //    catch
            //    {
            //    }
            //}

            return null;
        }
        private static bool TryReadRuntimeDefTypePackage(object? opValue, out SLRuntimeDefTypePackage? pkg)
        {
            pkg = null;
            if (opValue == null) return false;

            if (opValue is SLRuntimeDefTypePackage direct)
            {
                pkg = direct;
                return true;
            }

            if (opValue is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.Object)
                {
                    try
                    {
                        pkg = je.Deserialize<SLRuntimeDefTypePackage>();
                        return pkg != null;
                    }
                    catch
                    {
                        return false;
                    }
                }

                if (je.ValueKind == JsonValueKind.String)
                {
                    opValue = je.GetString();
                }
            }

            if (opValue is string s && !string.IsNullOrWhiteSpace(s) && s[0] == '{')
            {
                try
                {
                    pkg = JsonSerializer.Deserialize<SLRuntimeDefTypePackage>(s);
                    return pkg != null;
                }
                catch
                {
                    return false;
                }
            }

            return false;
        }
        private static RuntimeCall? CreateRuntimeCall(SLRuntimeCallPackage callPkg, int fallbackParamCount)
        {
            if (callPkg == null) return null;

            RuntimeMethod? callee = null;
            if (!string.IsNullOrWhiteSpace(callPkg.methodId))
            {
                s_MethodById.TryGetValue(callPkg.methodId, out callee);
            }

            if (callee == null && !string.IsNullOrWhiteSpace(callPkg.methodName))
            {
                foreach (var m in s_MethodById.Values)
                {
                    if (string.Equals(m?.onlyFunctionName, callPkg.methodName, StringComparison.Ordinal))
                    {
                        callee = m;
                        break;
                    }
                }
            }

            if (callee == null) return null;

            var ownerType = ResolveRuntimeDefType(callPkg.runtimeDefType);
            if (ownerType == null)
            {
                ownerType = ResolveFallbackOwnerType(callPkg.methodId);
            }
            if (ownerType == null) return null;

            var templateList = new List<RuntimeDefType>();
            if (callPkg.templateRuntimeDefTypeList != null)
            {
                for (int i = 0; i < callPkg.templateRuntimeDefTypeList.Count; i++)
                {
                    var t = ResolveRuntimeDefType(callPkg.templateRuntimeDefTypeList[i]);
                    if (t != null) templateList.Add(t);
                }
            }

            var paramCount = callPkg.paramCount > 0 ? callPkg.paramCount : fallbackParamCount;
            return new RuntimeCall(ownerType, templateList, callee, paramCount);
        }
        private static RuntimeDefType? ResolveFallbackOwnerType(string? methodId)
        {
            RuntimeDefType? ownerType = null;
            if (!string.IsNullOrWhiteSpace(methodId) && s_MethodDeclaringTypeById.TryGetValue(methodId, out var ownerTypeName))
            {
                var rc = ResolveOrCreateRuntimeClass(ownerTypeName);
                if (rc != null) ownerType = new RuntimeDefType(rc, new List<RuntimeDefType>());
            }

            if (ownerType == null)
            {
                var fallbackRc = ResolveOrCreateRuntimeClass("Core.Object");
                if (fallbackRc != null) ownerType = new RuntimeDefType(fallbackRc, new List<RuntimeDefType>());
            }

            return ownerType;
        }

        // ResolveRuntimeDefType(SLRuntimeDefTypePackage) is implemented above to prefer
        // resolution by class id/name via cached package metadata. The overload that
        // accepts string type names is handled separately.

        private static RuntimeClass? ResolveOrCreateRuntimeClassByIdOrName(int classId, string? className)
        {
            RuntimeClass? rc = null;

            if (classId != 0)
            {
                rc = RuntimeClassManager.GetRuntimeClassById(classId);
                if (rc == null)
                {
                    // 这里仅允许建壳，禁止触发字段填充，避免 TypeDef 解析阶段递归读包内容。
                    if (s_ClassPackageById.TryGetValue(classId, out var pkg) && pkg != null)
                    {
                        rc = RegisterRuntimeClassShellFromPackage(pkg);
                    }
                }
            }

            if (rc == null && !string.IsNullOrWhiteSpace(className))
            {
                rc = ResolveOrCreateRuntimeClass(className);
                if (rc != null && classId != 0 && rc.id != classId)
                {
                    // Keep one RuntimeClass instance per logical class; align id to exported package id.
                    rc.id = classId;
                }
            }

            if (rc == null && classId != 0)
            {
                rc = new RuntimeClass
                {
                    id = classId,
                    name = string.IsNullOrWhiteSpace(className) ? $"Class_{classId}" : className,
                };
                RuntimeClassManager.AddRuntimeClass(rc);
            }

            return rc;
        }

        private static RuntimeClass? ResolveOrCreateRuntimeClass(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return null;

            typeName = GetGenericRootName(typeName);

            var rc = RuntimeClassManager.GetRuntimeClassByName(typeName)
                ?? RuntimeClassManager.GetRuntimeClassByName(GetShortName(typeName));
            if (rc != null) return rc;

            rc = new RuntimeClass
            {
                id = typeName.GetHashCode(),
                name = typeName,
            };
            RuntimeClassManager.AddRuntimeClass(rc);
            return rc;
        }

        /// <summary>
        /// On-demand: ensure shell + field list from package. Used when resolving types outside the main multi-phase load.
        /// </summary>
        private static RuntimeClass? CreateRuntimeClassFromPackage(SLClassPackage pkg)
        {
            if (pkg == null) return null;

            var existed = RuntimeClassManager.GetRuntimeClassById(pkg.id);
            if (existed != null)
            {
                if (!existed.fieldsFromPackageApplied)
                    PopulateRuntimeClassFieldsFromPackage(pkg, existed);
                if (!s_ClassPackageById.ContainsKey(pkg.id))
                    s_ClassPackageById[pkg.id] = pkg;
                return existed;
            }

            var rc = RegisterRuntimeClassShellFromPackage(pkg);
            if (rc == null) return null;
            PopulateRuntimeClassFieldsFromPackage(pkg, rc);
            return rc;
        }

        private static string GetGenericRootName(string fullTypeName)
        {
            if (string.IsNullOrWhiteSpace(fullTypeName)) return string.Empty;
            int lt = fullTypeName.IndexOf('<');
            return lt < 0 ? fullTypeName.Trim() : fullTypeName.Substring(0, lt).Trim();
        }

        private static string GetShortName(string full)
        {
            if (string.IsNullOrEmpty(full)) return string.Empty;
            var idx = full.LastIndexOf('.');
            return idx >= 0 && idx + 1 < full.Length ? full[(idx + 1)..] : full;
        }

        public static RuntimeMethod? GetMethod(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return s_MethodById.TryGetValue(id, out var m) ? m : null;
        }

        // Resolve runtime class by type name by scanning cached SLClassPackage metadata.
        // Prefer package-driven creation so RuntimeClass gets fully populated (fields/methods/templates).
        public static RuntimeClass? ResolveOrCreateRuntimeClassByName(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return null;

            typeName = GetGenericRootName(typeName.Trim());

            // Fast path: runtime class already registered by some prior resolution.
            var rcExisting = RuntimeClassManager.GetRuntimeClassByName(typeName)
                ?? RuntimeClassManager.GetRuntimeClassByName(GetShortName(typeName));
            if (rcExisting != null) return rcExisting;

            // Slow path: try to find matching SLClassPackage by name/fullName.
            foreach (var pkg in s_ClassPackageById.Values)
            {
                if (pkg == null) continue;
                if (string.IsNullOrWhiteSpace(pkg.name) && string.IsNullOrWhiteSpace(pkg.fullName)) continue;

                var pkgRootName = GetGenericRootName(pkg.name ?? string.Empty);
                if (string.Equals(pkgRootName, typeName, StringComparison.Ordinal)) return CreateRuntimeClassFromPackage(pkg);

                var pkgFull = pkg.fullName ?? string.Empty;
                var pkgFullRoot = GetGenericRootName(pkgFull);
                if (string.Equals(pkgFullRoot, typeName, StringComparison.Ordinal)) return CreateRuntimeClassFromPackage(pkg);

                var pkgShortFull = GetShortName(pkgFullRoot);
                if (!string.IsNullOrWhiteSpace(pkgShortFull) && string.Equals(pkgShortFull, typeName, StringComparison.Ordinal))
                    return CreateRuntimeClassFromPackage(pkg);
            }

            return null;
        }

        // Public helper used by runtime to ensure a RuntimeClass is registered
        // from cached package metadata when only a class id is available.
        public static RuntimeClass? ResolveOrCreateRuntimeClassById(int classId)
        {
            if (classId == 0) return null;

            var rc = RuntimeClassManager.GetRuntimeClassById(classId);
            if (rc != null) return rc;

            if (s_ClassPackageById.TryGetValue(classId, out var pkg) && pkg != null)
            {
                // CreateRuntimeClassFromPackage will register the new RuntimeClass
                return CreateRuntimeClassFromPackage(pkg);
            }

            return null;
        }

        // Returns a copy of static field initializer instructions for one class field.
        // Caller can execute it in a dedicated RuntimeVM and assign the top stack value.
        public static List<Instruction> GetStaticFieldInitializerExpressions(int classId, int fieldIndex)
        {
            if (classId == 0 || fieldIndex < 0) return new List<Instruction>();
            if (!s_ClassPackageById.TryGetValue(classId, out var pkg) || pkg == null) return new List<Instruction>();
            if (pkg.fieldList == null || pkg.fieldList.Count == 0) return new List<Instruction>();

            for (int i = 0; i < pkg.fieldList.Count; i++)
            {
                var f = pkg.fieldList[i];
                if (f == null) continue;
                if (f.index != fieldIndex) continue;
                if ((f.flags & 32) == 32)
                {
                    //Instruction.UnpackPayloadsFromJson(f.express);
                    return new List<Instruction>(f.express);
                }
            }

            return new List<Instruction>();
        }

        // 按 order 升序返回某个类的全部静态字段初始化指令（合并好的单一序列）。
        // order 来自 MetaMemberVariable.parseOrder，反映成员之间的依赖解析次序：
        // 被依赖者先获得较小 order，必须先执行其初始化。缺省 order(-1) 视为最大值排到末尾，
        // 相同 order 内按字段在包内的声明顺序（fieldList 索引）稳定排列。
        // 这样静态初始化（如 x1 = x2 * 1 + -2, x2 = x3 + 4, x3 = 13）会按 x3 -> x2 -> x1 执行。
        public static List<Instruction> GetStaticFieldInitializerExpressionsInOrder(int classId)
        {
            var result = new List<Instruction>();
            if (classId == 0) return result;
            if (!s_ClassPackageById.TryGetValue(classId, out var pkg) || pkg == null) return result;
            if (pkg.fieldList == null || pkg.fieldList.Count == 0) return result;

            var orderedFields = new List<(SLFieldPackage field, int declIndex)>(pkg.fieldList.Count);
            for (int i = 0; i < pkg.fieldList.Count; i++)
            {
                var f = pkg.fieldList[i];
                if (f == null) continue;
                if ((f.flags & 32) != 32) continue;
                if (f.express == null || f.express.Count == 0) continue;
                orderedFields.Add((f, i));
            }

            orderedFields.Sort((a, b) =>
            {
                int akey = a.field.order < 0 ? int.MaxValue : a.field.order;
                int bkey = b.field.order < 0 ? int.MaxValue : b.field.order;
                int cmp = akey.CompareTo(bkey);
                if (cmp != 0) return cmp;
                return a.declIndex.CompareTo(b.declIndex);
            });

            foreach (var (f, _) in orderedFields)
            {
                result.AddRange(f.express);
            }

            return result;
        }
    }
}
