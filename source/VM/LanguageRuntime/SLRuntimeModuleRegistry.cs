using System;
using System.Collections.Generic;
using System.Text.Json;

namespace SimpleLanguage.VM.LanguageRuntime
{
    public static class SLRuntimeModuleRegistry
    {
        private static readonly Dictionary<string, RuntimeMethod> s_MethodById = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, string> s_MethodDeclaringTypeById = new(StringComparer.Ordinal);

        public static void Clear()
        {
            s_MethodById.Clear();
            s_MethodDeclaringTypeById.Clear();
        }

        public static void LoadFromPackage(SLModulePackage pkg)
        {
            if (pkg == null) throw new ArgumentNullException(nameof(pkg));
            Clear();
            AddFromPackage(pkg);
            BindStaticCallRuntimeCall();
        }

        public static void LoadFromPackages(IEnumerable<SLModulePackage> packages)
        {
            if (packages == null) throw new ArgumentNullException(nameof(packages));
            Clear();

            foreach (var pkg in packages)
            {
                if (pkg == null) continue;
                AddFromPackage(pkg);
            }

            BindStaticCallRuntimeCall();
        }

        private static void AddFromPackage(SLModulePackage pkg)
        {
            foreach (var m in pkg.methodList)
            {
                if (m == null || string.IsNullOrEmpty(m.id)) continue;

                var rm = new RuntimeMethod
                {
                    id = m.id,
                    onlyFunctionName = m.name ?? string.Empty,
                };

                // instructions
                if (m.instructionList != null)
                {
                    rm.InstructionList.AddRange(SLModulePackageLoader.ConvertToVMInstructionList(m.instructionList));
                }

                if (m.returnList != null)
                {
                    foreach (var v in m.returnList)
                    {
                        if (v == null) continue;
                        rm.methodReturnVariableList.Add(new RuntimeVariable(ResolveRuntimeDefType(v.typeName), v.id, v.index, v.name));
                    }
                }
                if (m.argumentList != null)
                {
                    foreach (var v in m.argumentList)
                    {
                        if (v == null) continue;
                        rm.methodArgumentList.Add(new RuntimeVariable(ResolveRuntimeDefType(v.typeName), v.id, v.index, v.name));
                    }
                }
                if (m.localList != null)
                {
                    foreach (var v in m.localList)
                    {
                        if (v == null) continue;
                        rm.methodLocalVariableList.Add(new RuntimeVariable(ResolveRuntimeDefType(v.typeName), v.id, v.index, v.name));
                    }
                }

                s_MethodById[rm.id] = rm;
                s_MethodDeclaringTypeById[rm.id] = m.declaringTypeFullName ?? string.Empty;
            }
        }

        private static void BindStaticCallRuntimeCall()
        {
            foreach (var kv in s_MethodById)
            {
                var caller = kv.Value;
                if (caller?.InstructionList == null) continue;

                for (int i = 0; i < caller.InstructionList.Count; i++)
                {
                    var ins = caller.InstructionList[i];
                    TryBindInstructionCall(ins);
                }
            }
        }

        public static bool TryBindInstructionCall(Instruction? ins)
        {
            if (ins == null || !IsCallOp(ins.opCode)) return false;

            var runtimeCall = TryCreateRuntimeCallForInstruction(ins.opValue as SLRuntimeCallPackage, ins.opValue, ins.index);
            if (runtimeCall == null) return false;

            ins.opValue = runtimeCall;
            return true;
        }

        public static RuntimeCall? TryCreateRuntimeCallForInstruction(SLRuntimeCallPackage? callPkg, object? legacyOpValue, int fallbackParamCount)
        {
            if (callPkg != null)
            {
                var fromPkg = CreateRuntimeCall(callPkg, fallbackParamCount);
                if (fromPkg != null) return fromPkg;
            }

            if (legacyOpValue is string methodId && !string.IsNullOrWhiteSpace(methodId))
            {
                return CreateRuntimeCallByMethodId(methodId, fallbackParamCount);
            }

            if (legacyOpValue is JsonElement je && je.ValueKind == JsonValueKind.String)
            {
                var methodIdFromJson = je.GetString();
                if (!string.IsNullOrWhiteSpace(methodIdFromJson))
                {
                    return CreateRuntimeCallByMethodId(methodIdFromJson, fallbackParamCount);
                }
            }

            return null;
        }

        private static bool IsCallOp(EIROpCode opCode)
        {
            return opCode == EIROpCode.CallStatic
                || opCode == EIROpCode.CallDynamic
                || opCode == EIROpCode.CallVirt;
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

        private static RuntimeDefType? ResolveRuntimeDefType(SLRuntimeDefTypePackage? pkg)
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

            var rdt = new RuntimeDefType(rc, args);
            return rdt;
        }

        private static RuntimeClass? ResolveOrCreateRuntimeClassByIdOrName(int classId, string? className)
        {
            RuntimeClass? rc = null;

            if (classId != 0)
            {
                rc = RuntimeClassManager.instance.GetRuntimeClassById(classId);
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
                RuntimeClassManager.instance.m_IRMetaClassList.Add(rc);
            }

            return rc;
        }

        private static RuntimeClass? ResolveOrCreateRuntimeClass(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return null;

            var rc = RuntimeClassManager.instance.GetRuntimeClassByName(typeName)
                ?? RuntimeClassManager.instance.GetRuntimeClassByName(GetShortName(typeName));
            if (rc != null) return rc;

            rc = new RuntimeClass
            {
                id = typeName.GetHashCode(),
                name = typeName,
            };
            RuntimeClassManager.instance.m_IRMetaClassList.Add(rc);
            return rc;
        }

        private static RuntimeDefType? ResolveRuntimeDefType(string? typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return null;

            var rc = RuntimeClassManager.instance.GetRuntimeClassByName(typeName)
                ?? RuntimeClassManager.instance.GetRuntimeClassByName(GetShortName(typeName));

            return rc != null ? new RuntimeDefType(rc) : null;
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
    }
}
