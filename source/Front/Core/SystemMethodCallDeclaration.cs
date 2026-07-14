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

    /// <summary>
    /// Registry for system method call declarations.
    /// Declarations are loaded dynamically from the project .jsonc config file
    /// (see the "systemCalls" section in Core.jsonc) via <see cref="LoadFromJsonFile"/>.
    /// </summary>
    public static class SystemMethodCallDeclarationRegistry
    {
        // Type aliases for C-style shorthand names (e.g. "int" -> SystemConvertInt32).
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

        // MetaType singletons used by ResolveTypeName to map JSON type strings.
        private static readonly MetaType Obj = SystemMethodCallTypes.Of(CoreMetaClassManager.objectMetaClass);
        private static readonly MetaType Void = SystemMethodCallTypes.Of(CoreMetaClassManager.voidMetaClass);
        private static readonly MetaType Str = SystemMethodCallTypes.Of(CoreMetaClassManager.stringMetaClass);
        private static readonly MetaType Bool = SystemMethodCallTypes.Of(CoreMetaClassManager.booleanMetaClass);
        private static readonly MetaType Num = SystemMethodCallTypes.Of(CoreMetaClassManager.numMetaClass);
        private static readonly MetaType I32 = SystemMethodCallTypes.Of(CoreMetaClassManager.int32MetaClass);
        private static readonly MetaType U32 = SystemMethodCallTypes.Of(CoreMetaClassManager.uint32MetaClass);
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

        // Populated dynamically by LoadFromJsonFile / LoadFromJsonContent.
        private static Dictionary<ESystemMethodCall, SystemMethodCallDeclaration> s_Decl =
            new Dictionary<ESystemMethodCall, SystemMethodCallDeclaration>();

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
        /// Loads system call declarations from a JSON config file (the project
        /// .jsonc, e.g. Core.jsonc).  Called by ProjectCompile after
        /// CoreMetaClassManager.Init().
        ///
        /// JSON format:
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
                using (JsonDocument doc = JsonDocument.Parse(jsonContent, new JsonDocumentOptions
                {
                    CommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                }))
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
                // JSON parse failure is non-fatal.
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
