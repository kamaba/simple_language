using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SimpleLanguage.Core
{
    /// <summary>Shorthand for <see cref="MetaType"/> used in system method registration (plain class or <c>Array&lt;T&gt;</c>).</summary>
    public static class SystemMethodCallTypes
    {
        public static MetaType Of(MetaClass metaClass) => new MetaType(metaClass);

        /// <summary>Builds <c>Array&lt;T&gt;</c> with element type <paramref name="elementClass"/> (registers template instance on <see cref="CoreMetaClassManager.arrayMetaClass"/>).</summary>
        public static MetaType ArrayOf(MetaClass elementClass)
        {
            var mt = new MetaType();
            mt.SetTemplateMetaClass(CoreMetaClassManager.arrayMetaClass);
            mt.AddDefineTemplateMetaType(new MetaType(elementClass));
            return CoreMetaClassManager.arrayMetaClass.AddMetaPreTemplateClass(mt, false, out _);
        }

        /// <summary>Builds <c>Array&lt;T&gt;</c> where <paramref name="elementType"/> may include templates/nullability (copied into the array signature).</summary>
        public static MetaType ArrayOf(MetaType elementType)
        {
            var mt = new MetaType();
            mt.SetTemplateMetaClass(CoreMetaClassManager.arrayMetaClass);
            mt.AddDefineTemplateMetaType(elementType == null ? new MetaType(CoreMetaClassManager.objectMetaClass) : new MetaType(elementType));
            return CoreMetaClassManager.arrayMetaClass.AddMetaPreTemplateClass(mt, true, out _);
        }
    }

    public sealed class SystemMethodCallDeclaration
    {
        public ESystemMethodCall method { get; }
        public MetaType returnMetaType { get; }
        public List<MetaType> paramMetaTypeList { get; }
        public bool isVariadic { get; }

        public SystemMethodCallDeclaration(ESystemMethodCall method, MetaType ret, bool variadic, params MetaType[] paramTypes)
        {
            this.method = method;
            returnMetaType = ret;
            isVariadic = variadic;
            paramMetaTypeList = new List<MetaType>();
            if (paramTypes != null)
            {
                for (int i = 0; i < paramTypes.Length; i++)
                {
                    paramMetaTypeList.Add(paramTypes[i]);
                }
            }
        }
    }

    public static class SystemMethodCallDeclarationRegistry
    {
        private static readonly Dictionary<string, ESystemMethodCall> s_Alias = new Dictionary<string, ESystemMethodCall>
        {
            { "byte", ESystemMethodCall.SystemConvertUInt8 },
            { "sbyte", ESystemMethodCall.SystemConvertSInt8 },
            { "short", ESystemMethodCall.SystemConvertInt16 },
            { "ushort", ESystemMethodCall.SystemConvertUInt16 },
            { "int", ESystemMethodCall.SystemConvertInt32 },
            { "uint", ESystemMethodCall.SystemConvertUInt32 },
            { "long", ESystemMethodCall.SystemConvertInt64 },
            { "ulong", ESystemMethodCall.SystemConvertUInt64 },
            { "float", ESystemMethodCall.SystemConvertFloat32 },
            { "double", ESystemMethodCall.SystemConvertFloat64 },
            { "Int8", ESystemMethodCall.SystemConvertSInt8 },
            { "UInt8", ESystemMethodCall.SystemConvertUInt8 },
            { "Int16", ESystemMethodCall.SystemConvertInt16 },
            { "UInt16", ESystemMethodCall.SystemConvertUInt16 },
            { "Int32", ESystemMethodCall.SystemConvertInt32 },
            { "UInt32", ESystemMethodCall.SystemConvertUInt32 },
            { "Int64", ESystemMethodCall.SystemConvertInt64 },
            { "UInt64", ESystemMethodCall.SystemConvertUInt64 },
            { "Float32", ESystemMethodCall.SystemConvertFloat32 },
            { "Float64", ESystemMethodCall.SystemConvertFloat64 },
        };

        private static readonly MetaType Obj = SystemMethodCallTypes.Of(CoreMetaClassManager.objectMetaClass);
        private static readonly MetaType Void = SystemMethodCallTypes.Of(CoreMetaClassManager.voidMetaClass);
        private static readonly MetaType Str = SystemMethodCallTypes.Of(CoreMetaClassManager.stringMetaClass);
        private static readonly MetaType Bool = SystemMethodCallTypes.Of(CoreMetaClassManager.booleanMetaClass);
        private static readonly MetaType Num = SystemMethodCallTypes.Of(CoreMetaClassManager.numMetaClass);
        private static readonly MetaType I32 = SystemMethodCallTypes.Of(CoreMetaClassManager.int32MetaClass);
        private static readonly MetaType U32 = SystemMethodCallTypes.Of(CoreMetaClassManager.uint32MetaClass);
        /// <summary><c>Array&lt;object&gt;</c> for generic array <c>this</c> on index get/set builtins.</summary>
        private static readonly MetaType ArrayObj = SystemMethodCallTypes.ArrayOf(CoreMetaClassManager.objectMetaClass);
        private static readonly MetaType UInt8Array = SystemMethodCallTypes.ArrayOf(CoreMetaClassManager.uint8MetaClass);
        private static readonly MetaType Typ = SystemMethodCallTypes.Of(CoreMetaClassManager.typeMetaClass);
        private static readonly MetaType U8 = SystemMethodCallTypes.Of(CoreMetaClassManager.uint8MetaClass);
        private static readonly MetaType I8 = SystemMethodCallTypes.Of(CoreMetaClassManager.int8MetaClass);
        private static readonly MetaType I16 = SystemMethodCallTypes.Of(CoreMetaClassManager.int16MetaClass);
        private static readonly MetaType U16 = SystemMethodCallTypes.Of(CoreMetaClassManager.uint16MetaClass);
        private static readonly MetaType I64 = SystemMethodCallTypes.Of(CoreMetaClassManager.int64MetaClass);
        private static readonly MetaType U64 = SystemMethodCallTypes.Of(CoreMetaClassManager.uint64MetaClass);
        private static readonly MetaType F32 = SystemMethodCallTypes.Of(CoreMetaClassManager.float32MetaClass);
        private static readonly MetaType F64 = SystemMethodCallTypes.Of(CoreMetaClassManager.float64MetaClass);

        private static readonly Dictionary<ESystemMethodCall, SystemMethodCallDeclaration> s_Decl = new Dictionary<ESystemMethodCall, SystemMethodCallDeclaration>
        {
            // bridge calls
            { ESystemMethodCall.SystemCallCLRMethod, new SystemMethodCallDeclaration(ESystemMethodCall.SystemCallCLRMethod, Obj, true, Obj) },
            { ESystemMethodCall.SystemCallNativeMethod, new SystemMethodCallDeclaration(ESystemMethodCall.SystemCallNativeMethod, Obj, true, Obj) },
            { ESystemMethodCall.SystemCallJVMMethod, new SystemMethodCallDeclaration(ESystemMethodCall.SystemCallJVMMethod, Obj, true, Obj) },

            // console
            { ESystemMethodCall.SystemPrint, new SystemMethodCallDeclaration(ESystemMethodCall.SystemPrint, Void, true, Obj) },
            { ESystemMethodCall.SystemPrintln, new SystemMethodCallDeclaration(ESystemMethodCall.SystemPrintln, Void, true, Obj) },

            { ESystemMethodCall.SystemReadLine, new SystemMethodCallDeclaration(ESystemMethodCall.SystemReadLine, Str, false) },
            { ESystemMethodCall.SystemReadKey, new SystemMethodCallDeclaration(ESystemMethodCall.SystemReadKey, Str, false) },

            // parse helpers
            { ESystemMethodCall.SystemInt32Parse, new SystemMethodCallDeclaration(ESystemMethodCall.SystemInt32Parse, I32, false, Str) },

            // convert
            { ESystemMethodCall.SystemConvertBool, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertBool, Bool, false, Obj) },
            { ESystemMethodCall.SystemConvertInt8, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertInt8, I8, false, Obj, I32) },
            { ESystemMethodCall.SystemConvertUInt8, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertUInt8, U8, false, Obj, I32) },
            { ESystemMethodCall.SystemConvertInt16, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertInt16, I16, false, Obj) },
            { ESystemMethodCall.SystemConvertUInt16, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertUInt16, U16, false, Obj) },
            { ESystemMethodCall.SystemConvertInt32, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertInt32, I32, false, Obj) },
            { ESystemMethodCall.SystemConvertUInt32, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertUInt32, U32, false, Obj) },
            { ESystemMethodCall.SystemConvertInt64, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertInt64, I64, false, Obj) },
            { ESystemMethodCall.SystemConvertUInt64, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertUInt64, U64, false, Obj) },
            { ESystemMethodCall.SystemConvertFloat32, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertFloat32, F32, false, Obj) },
            { ESystemMethodCall.SystemConvertFloat64, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertFloat64, F64, false, Obj) },
            { ESystemMethodCall.SystemConvertString, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertString, Str, false, Obj) },

            // object
            { ESystemMethodCall.SystemEqualObject, new SystemMethodCallDeclaration(ESystemMethodCall.SystemEqualObject, Bool, false, Obj, Obj) },
            { ESystemMethodCall.SystemObjectGetType, new SystemMethodCallDeclaration(ESystemMethodCall.SystemObjectGetType, Typ, false, Obj) },
            { ESystemMethodCall.SystemObjectGetHashCode, new SystemMethodCallDeclaration(ESystemMethodCall.SystemObjectGetHashCode, I32, false, Obj) },
            { ESystemMethodCall.SystemObjectRef, new SystemMethodCallDeclaration(ESystemMethodCall.SystemObjectRef, Obj, false, Obj) },
            { ESystemMethodCall.SystemObjectRefWeak, new SystemMethodCallDeclaration(ESystemMethodCall.SystemObjectRefWeak, Obj, false, Obj) },
            { ESystemMethodCall.SystemObjectRefCount, new SystemMethodCallDeclaration(ESystemMethodCall.SystemObjectRefCount, I32, false, Obj) },
            { ESystemMethodCall.SystemObjectFree, new SystemMethodCallDeclaration(ESystemMethodCall.SystemObjectFree, Void, false, Obj) },
            { ESystemMethodCall.SystemObjectRelease, new SystemMethodCallDeclaration(ESystemMethodCall.SystemObjectRelease, Void, false, Obj) },

            // array helpers (this: Array<object> — element type is erased at builtin boundary)
            { ESystemMethodCall.SystemArrayGetValueThis, new SystemMethodCallDeclaration(ESystemMethodCall.SystemArrayGetValueThis, Obj, false, ArrayObj, I32) },
            { ESystemMethodCall.SystemArraySetValueThis, new SystemMethodCallDeclaration(ESystemMethodCall.SystemArraySetValueThis, Void, false, ArrayObj, I32, Obj) },

            // num helpers
            { ESystemMethodCall.SystemNumAbs, new SystemMethodCallDeclaration(ESystemMethodCall.SystemNumAbs, Num, false, Num) },
            { ESystemMethodCall.SystemNumFloor, new SystemMethodCallDeclaration(ESystemMethodCall.SystemNumFloor, Num, false, Num) },

            // string slice / bytes (instance: this + args)
            { ESystemMethodCall.SystemStringFormat, new SystemMethodCallDeclaration(ESystemMethodCall.SystemStringFormat, Str, false, Str, ArrayObj) },
            { ESystemMethodCall.SystemStringFront, new SystemMethodCallDeclaration(ESystemMethodCall.SystemStringFront, Str, false, Str, I32) },
            { ESystemMethodCall.SystemStringEnd, new SystemMethodCallDeclaration(ESystemMethodCall.SystemStringEnd, Str, false, Str, I32) },
            { ESystemMethodCall.SystemStringRange, new SystemMethodCallDeclaration(ESystemMethodCall.SystemStringRange, Str, false, Str, I32, I32) },
            { ESystemMethodCall.SystemStringToUInt8Array, new SystemMethodCallDeclaration(ESystemMethodCall.SystemStringToUInt8Array, UInt8Array, false, Str) },

            // data compare (operands validated at runtime; params are object for anonymous/typed data instances)
            { ESystemMethodCall.DataAllEqual, new SystemMethodCallDeclaration(ESystemMethodCall.DataAllEqual, Bool, false, Obj, Obj) },
            { ESystemMethodCall.DataTypeEqual, new SystemMethodCallDeclaration(ESystemMethodCall.DataTypeEqual, Bool, false, Obj, Obj) },
            { ESystemMethodCall.DataNameAndTypeEqual, new SystemMethodCallDeclaration(ESystemMethodCall.DataNameAndTypeEqual, Bool, false, Obj, Obj) },
            { ESystemMethodCall.DataDataEqual, new SystemMethodCallDeclaration(ESystemMethodCall.DataDataEqual, Bool, false, Obj, Obj) },
            { ESystemMethodCall.SystemBuildDataString, new SystemMethodCallDeclaration(ESystemMethodCall.SystemBuildDataString, Str, false, Obj) },
            { ESystemMethodCall.SystemConvertSInt8, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertSInt8, I8, false, Obj, I32) },

            // memory management (Memory.sl)
            { ESystemMethodCall.SystemMemoryRefCount,         new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemoryRefCount,         I32,  false, Obj) },
            { ESystemMethodCall.SystemMemoryRetain,           new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemoryRetain,           I32,  false, Obj) },
            { ESystemMethodCall.SystemMemoryFree,             new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemoryFree,             I32,  false, Obj) },
            { ESystemMethodCall.SystemMemoryRelease,          new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemoryRelease,          I32,  false, Obj) },
            { ESystemMethodCall.SystemMemoryManual,           new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemoryManual,           I32,  false, Obj) },
            { ESystemMethodCall.SystemMemoryAuto,             new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemoryAuto,             I32,  false, Obj) },
            { ESystemMethodCall.SystemMemoryIsManual,         new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemoryIsManual,         I32,  false, Obj) },
            { ESystemMethodCall.SystemMemoryCollect,          new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemoryCollect,          I32,  false) },
            { ESystemMethodCall.SystemMemoryCollectThreshold, new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemoryCollectThreshold, I32,  false, I32) },
            { ESystemMethodCall.SystemMemoryGetObjectCount,   new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemoryGetObjectCount,   I32,  false) },
            { ESystemMethodCall.SystemMemoryGetGcCycleCount,  new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemoryGetGcCycleCount,  I32,  false) },
            { ESystemMethodCall.SystemMemoryGetGcFreedCount,  new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemoryGetGcFreedCount,  I32,  false) },
            { ESystemMethodCall.SystemMemorySetGcThreshold,   new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemorySetGcThreshold,   I32,  false, I32) },
            { ESystemMethodCall.SystemMemoryGetGcThreshold,   new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemoryGetGcThreshold,   I32,  false) },
            { ESystemMethodCall.SystemMemoryKeepAlive,        new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemoryKeepAlive,        Void, false, Obj) },
            { ESystemMethodCall.SystemMemoryWeakRef,          new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemoryWeakRef,          Obj,  false, Obj) },
            { ESystemMethodCall.SystemMemoryIsWeakRefValid,   new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemoryIsWeakRefValid,   I32,  false, Obj) },
            { ESystemMethodCall.SystemMemoryGetTotalAllocated,new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemoryGetTotalAllocated,I32,  false) },
            { ESystemMethodCall.SystemMemoryGetTotalFreed,    new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemoryGetTotalFreed,    I32,  false) },
            { ESystemMethodCall.SystemMemorySetMode,          new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemorySetMode,          I32,  false, I32) },
            { ESystemMethodCall.SystemMemoryClone,            new SystemMethodCallDeclaration(ESystemMethodCall.SystemMemoryClone,            Obj,  false, Obj) },
        };

        // ---- JSON config loading ----

        /// <summary>
        /// Resolves a type name string (from JSON config) to a MetaType singleton.
        /// </summary>
        private static MetaType ResolveTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return Obj;
            switch (typeName.ToLowerInvariant())
            {
                case "void":     return Void;
                case "object":   return Obj;
                case "string":   return Str;
                case "bool":     return Bool;
                case "num":      return Num;
                case "int32":    return I32;
                case "uint32":   return U32;
                case "int8":     return I8;
                case "uint8":    return U8;
                case "int16":    return I16;
                case "uint16":   return U16;
                case "int64":    return I64;
                case "uint64":   return U64;
                case "float32":  return F32;
                case "float64":  return F64;
                case "type":     return Typ;
                case "array<object>": return ArrayObj;
                case "uint8array":    return UInt8Array;
                default:        return Obj;
            }
        }

        /// <summary>
        /// Loads system call declarations from a JSON config file and merges
        /// them into the registry.  Entries that already exist (by enum value)
        /// are overridden; new entries are added.
        ///
        /// JSON format (see system_calls.jsonc):
        /// { "systemCalls": [
        ///     { "name": "SystemFoo", "returnType": "Int32",
        ///       "params": ["object"], "isVariadic": false }
        ///   ]
        /// }
        /// </summary>
        public static int LoadFromJsonFile(string configPath)
        {
            if (string.IsNullOrEmpty(configPath) || !File.Exists(configPath))
                return 0;

            string json = File.ReadAllText(configPath);
            return LoadFromJsonContent(json);
        }

        /// <summary>
        /// Loads system call declarations from JSON content and merges them
        /// into the registry.  Returns the number of entries registered.
        /// </summary>
        public static int LoadFromJsonContent(string jsonContent)
        {
            if (string.IsNullOrWhiteSpace(jsonContent))
                return 0;

            int count = 0;
            try
            {
                using (JsonDocument doc = JsonDocument.Parse(jsonContent))
                {
                    if (!doc.RootElement.TryGetProperty("systemCalls", out JsonElement callsEl))
                        return 0;

                    foreach (JsonElement entry in callsEl.EnumerateArray())
                    {
                        string name = entry.TryGetProperty("name", out JsonElement nameEl)
                            ? nameEl.GetString() : null;
                        if (string.IsNullOrEmpty(name))
                            continue;

                        // Resolve enum by name (must exist in ESystemMethodCall)
                        if (!Enum.TryParse(name, true, out ESystemMethodCall callKind))
                            continue;

                        // Parse return type
                        MetaType retType = entry.TryGetProperty("returnType", out JsonElement retEl)
                            ? ResolveTypeName(retEl.GetString()) : Void;

                        // Parse variadic flag
                        bool variadic = entry.TryGetProperty("isVariadic", out JsonElement varEl)
                            && varEl.GetBoolean();

                        // Parse parameter types
                        List<MetaType> paramTypes = new List<MetaType>();
                        if (entry.TryGetProperty("params", out JsonElement paramsEl)
                            && paramsEl.ValueKind == JsonValueKind.Array)
                        {
                            foreach (JsonElement p in paramsEl.EnumerateArray())
                            {
                                paramTypes.Add(ResolveTypeName(p.GetString()));
                            }
                        }

                        var decl = new SystemMethodCallDeclaration(
                            callKind, retType, variadic, paramTypes.ToArray());
                        s_Decl[callKind] = decl;
                        count++;
                    }
                }
            }
            catch (Exception)
            {
                // JSON parse failure is non-fatal; hardcoded defaults remain.
            }
            return count;
        }

        public static bool TryGet(ESystemMethodCall call, out SystemMethodCallDeclaration decl)
        {
            return s_Decl.TryGetValue(call, out decl);
        }

        public static bool TryResolveName(string name, out ESystemMethodCall call)
        {
            if (!string.IsNullOrEmpty(name) && s_Alias.TryGetValue(name, out call))
                return true;
            return System.Enum.TryParse(name, true, out call);
        }
    }
}
