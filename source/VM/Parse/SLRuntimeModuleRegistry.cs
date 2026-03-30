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
        }

        private static RuntimeDefType ResolveRuntimeDefType(SLRuntimeDefTypePackage? pkg)
        {
            if (pkg == null) return null;

            var rc = ResolveOrCreateRuntimeClassByIdOrName(pkg.classId, pkg.className);
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

                bool isTemplate = pkg.isTemplate || pkg.templateIndex >= 0;
                return new RuntimeDefType(rc, args, ownerRc, pkg.templateIndex, isTemplate);
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
                    var rc = RuntimeClassManager.GetRuntimeClassById(c.id);
                    if (rc == null) continue;
                    foreach (var rel in c.templateRelationList)
                    {
                        if (rel?.mapping == null) continue;
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
                            rm.methodReturnVariableList.Add(new RuntimeVariable(rdt, v.id, v.index, v.name));
                        }
                    }
                    if (m.argumentList != null)
                    {
                        foreach (var v in m.argumentList)
                        {
                            if (v == null) continue;
                            var rdt = v.typeDef != null ? ResolveRuntimeDefType(v.typeDef) : null;
                            rm.methodArgumentList.Add(new RuntimeVariable(rdt, v.id, v.index, v.name));
                        }
                    }
                    if (m.localList != null)
                    {
                        foreach (var v in m.localList)
                        {
                            if (v == null) continue;
                            var rdt = v.typeDef != null ? ResolveRuntimeDefType(v.typeDef) : null;
                            rm.methodLocalVariableList.Add(new RuntimeVariable(rdt, v.id, v.index, v.name));
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
                return existed;

            var rc = new RuntimeClass
            {
                id = pkg.id,
                name = string.IsNullOrWhiteSpace(pkg.fullName) ? pkg.name : pkg.fullName,
                metaClassKind = pkg.metaClassKind,
                fieldsFromPackageApplied = false,
            };
            RuntimeClassManager.AddRuntimeClass(rc);

            if (!s_ClassPackageById.ContainsKey(pkg.id))
                s_ClassPackageById[pkg.id] = pkg;

            return rc;
        }

        /// <summary>Fills <paramref name="rc"/> from <paramref name="pkg"/>.<c>fieldList</c> once per class.</summary>
        private static void PopulateRuntimeClassFieldsFromPackage(SLClassPackage pkg, RuntimeClass rc)
        {
            if (pkg == null || rc == null) return;
            if (rc.fieldsFromPackageApplied) return;

            if (pkg.fieldList != null)
            {
                foreach (var f in pkg.fieldList)
                {
                    if (f == null) continue;

                    RuntimeDefType rdt = null;
                    try
                    {
                        if (f.typeDef != null)
                            rdt = ResolveRuntimeDefType(f.typeDef);
                    }
                    catch {
                        Debug.Assert(false, "解析定义类型出错!");
                    }

                    var rv = new RuntimeVariable(rdt, f.GetHashCode(), f.index, f.name ?? string.Empty);
                    if ((f.flags & 32) == 32)
                    {
                        rc.staticIRMetaVariableList.Add(rv);
                        if (f.express != null && f.express.Count > 0)
                        {
                            //Instruction.UnpackPayloadsFromJson(f.express);
                            foreach (var ins in f.express)
                                rc.staticMemberVariableSetValueList.Add(ins);
                        }
                    }
                    else
                    {
                        rc.nonStaticIRMetaVariableList.Add(rv);
                        if (f.express != null && f.express.Count > 0)
                        {
                            //Instruction.UnpackPayloadsFromJson(f.express);
                            foreach (var ins in f.express)
                                rc.nonStaticMemberVariableSetValueList.Add(ins);
                        }
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

        // Note: binding of instruction call payloads to runtime call objects
        // moved to the dynamic runtime layer. Runtime should invoke
        // TryCreateRuntimeCallForInstruction when it needs to resolve an
        // instruction's opValue into a RuntimeCall. Keeping the helper
        // here for on-demand use is still possible via TryCreateRuntimeCallForInstruction.

        public static bool TryBindInstructionCall(Instruction? ins)
        {
            // Binding logic intentionally moved to the runtime layer.
            // Runtime code should call TryCreateRuntimeCallForInstruction when
            // it needs to resolve an instruction's opValue into a RuntimeCall.
            return false;
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

            return null;
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

        private static RuntimeCall? CreateRuntimeCallByMethodId(string methodId, int paramCount)
        {
            if (!s_MethodById.TryGetValue(methodId, out var callee) || callee == null) return null;

            var ownerType = ResolveFallbackOwnerType(methodId);
            if (ownerType == null) return null;

            return new RuntimeCall(ownerType, new List<RuntimeDefType>(), callee, paramCount);
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
                    // try to build from cached package metadata if available
                    if (s_ClassPackageById.TryGetValue(classId, out var pkg) && pkg != null)
                    {
                        rc = CreateRuntimeClassFromPackage(pkg);
                    }
                }
            }

            if (rc == null && !string.IsNullOrWhiteSpace(className))
            {
                rc = ResolveOrCreateRuntimeClass(className);
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

        private static RuntimeDefType? TryBuildRuntimeDefTypeFromTypeName(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return null;
            // reuse existing builder path by using BuildRuntimeDefTypeFromTypeName via string parsing
            return BuildRuntimeDefTypeFromTypeName(typeName);
        }



        private static RuntimeDefType? ResolveRuntimeDefType(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return null;
            return BuildRuntimeDefTypeFromTypeName(typeName);
        }

        private static RuntimeDefType? BuildRuntimeDefTypeFromTypeName(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return null;

            var text = typeName.Trim();
            int lt = text.IndexOf('<');
            if (lt < 0)
            {
                var rc = ResolveOrCreateRuntimeClass(GetGenericRootName(text));
                if (rc == null) return null;

                EnsureRuntimeTypeRegistered(rc, new List<RuntimeType>());
                return new RuntimeDefType(rc);
            }

            int gt = text.LastIndexOf('>');
            if (gt <= lt)
            {
                var rc = ResolveOrCreateRuntimeClass(GetGenericRootName(text));
                if (rc == null) return null;

                EnsureRuntimeTypeRegistered(rc, new List<RuntimeType>());
                return new RuntimeDefType(rc);
            }

            var rootName = text.Substring(0, lt).Trim();
            var rcRoot = ResolveOrCreateRuntimeClass(rootName);
            if (rcRoot == null) return null;

            var argsText = text.Substring(lt + 1, gt - lt - 1);
            var argNames = SplitGenericArguments(argsText);
            var rdtArgs = new List<RuntimeDefType>();
            var rtArgs = new List<RuntimeType>();

            for (int i = 0; i < argNames.Count; i++)
            {
                var childRdt = BuildRuntimeDefTypeFromTypeName(argNames[i]);
                if (childRdt == null) continue;
                rdtArgs.Add(childRdt);

                var childRt = EnsureRuntimeTypeRegistered(childRdt.runtimeClass, GetTemplateRuntimeTypes(childRdt));
                if (childRt != null)
                {
                    rtArgs.Add(childRt);
                }
            }

            EnsureRuntimeTypeRegistered(rcRoot, rtArgs);
            return new RuntimeDefType(rcRoot, rdtArgs);
        }

        private static List<string> SplitGenericArguments(string argsText)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(argsText)) return result;

            int depth = 0;
            int start = 0;
            for (int i = 0; i < argsText.Length; i++)
            {
                var ch = argsText[i];
                if (ch == '<') depth++;
                else if (ch == '>') depth--;
                else if (ch == ',' && depth == 0)
                {
                    var item = argsText.Substring(start, i - start).Trim();
                    if (!string.IsNullOrWhiteSpace(item)) result.Add(item);
                    start = i + 1;
                }
            }

            if (start < argsText.Length)
            {
                var last = argsText.Substring(start).Trim();
                if (!string.IsNullOrWhiteSpace(last)) result.Add(last);
            }

            return result;
        }

        private static List<RuntimeType> GetTemplateRuntimeTypes(RuntimeDefType rdt)
        {
            var list = new List<RuntimeType>();
            if (rdt?.runtimeDefTypeList == null) return list;

            for (int i = 0; i < rdt.runtimeDefTypeList.Count; i++)
            {
                var child = rdt.runtimeDefTypeList[i];
                if (child == null || child.runtimeClass == null) continue;

                var childRt = EnsureRuntimeTypeRegistered(child.runtimeClass, GetTemplateRuntimeTypes(child));
                if (childRt != null) list.Add(childRt);
            }

            return list;
        }

        private static RuntimeType? EnsureRuntimeTypeRegistered(RuntimeClass rc, List<RuntimeType> templateArgs)
        {
            if (rc == null) return null;

            var args = templateArgs ?? new List<RuntimeType>();
            var existed = RuntimeTypeManager.GetRuntimeTypeByMTAndTemplateMT(rc, args);
            if (existed != null) return existed;

            if (args.Count == 0)
            {
                return RuntimeTypeManager.AddRuntimeTypeByClass(rc);
            }

            return RuntimeTypeManager.AddRuntimeTypeByClassAndTemplate(rc, args);
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
                if ((f.flags & 32) != 32) return new List<Instruction>();
                if (f.express == null || f.express.Count == 0) return new List<Instruction>();

                //Instruction.UnpackPayloadsFromJson(f.express);
                return new List<Instruction>(f.express);
            }

            return new List<Instruction>();
        }
    }
}
