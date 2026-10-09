using SimpleLanguage.Logging;
using SimpleLanguage.Project;
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
        public string name { get; }
        public MetaType returnMetaType { get; }
        public List<MetaType> paramMetaTypeList { get; }
        public bool isVariadic { get; }

        /// <summary>Name of the C VM builtin implementation function (e.g. "vm_sys_ptr_alloc").
        /// Exported with the module's "systemCalls" so the C VM resolves the
        /// implementation by symbol name instead of a hardcoded mapping table.</summary>
        public string cvmFunction { get; }

        /// <summary>Owner class path for class-path style calls (e.g. "SLang.Plugin.CSharpMono").
        /// Empty = global-only systemCall (bare name). When set, the declaration can
        /// also be resolved as <c>OwnerClass.Method(...)</c> even though no stub member
        /// function exists on the class (declared registration, see CSharpMono.jsonc).</summary>
        public string className { get; }

        /// <summary>Unique int id of this system method (MD5-based hash of <see cref="name"/>).
        /// Exported with the module's "systemCalls" and CallSystemMethod payloads so the
        /// VM can dispatch by int id instead of by name string.</summary>
        public int GetIndex()
        {
            return CommonFunction.StringToIntHash(name);
        }

        public int Index()
        {
            return GetIndex();
        }

        public SystemMethodCallDeclaration( string name, MetaType ret, bool variadic, MetaType[] paramTypes, string cvmFunction = null, string className = null)
        {
            this.name = name;
            returnMetaType = ret;
            isVariadic = variadic;
            this.cvmFunction = cvmFunction ?? string.Empty;
            this.className = className ?? string.Empty;
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
    /// </summary>
    public static class SystemMethodCallDeclarationRegistry
    {
        public static List<SystemMethodCallDeclaration> projectDefine => s_ProjectDefine;
        // Type aliases for C-style shorthand names (e.g. "int" -> SystemConvertInt32).
        private static readonly Dictionary<string, string> s_Alias = new Dictionary<string, string>
        {
            { "byte", "SystemConvertUInt8" },
            { "sbyte", "SystemConvertSInt8" },
            { "short", "SystemConvertInt16" },
            { "ushort", "SystemConvertUInt16" },
            { "int", "SystemConvertInt32" },
            { "uint", "SystemConvertUInt32" },
            { "long", "SystemConvertInt64" },
            { "ulong", "SystemConvertUInt64" },
            { "float", "SystemConvertFloat32" },
            { "double", "SystemConvertFloat64" },
            { "Int8", "SystemConvertSInt8" },
            { "UInt8", "SystemConvertUInt8" },
            { "Int16", "SystemConvertInt16" },
            { "UInt16", "SystemConvertUInt16" },
            { "Int32", "SystemConvertInt32" },
            { "UInt32", "SystemConvertUInt32" },
            { "Int64", "SystemConvertInt64" },
            { "UInt64", "SystemConvertUInt64" },
            { "Float32", "SystemConvertFloat32" },
            { "Float64", "SystemConvertFloat64" },
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
        private static readonly MetaType F8 = SystemMethodCallTypes.Of(CoreMetaClassManager.float8MetaClass);
        private static readonly MetaType F16 = SystemMethodCallTypes.Of(CoreMetaClassManager.float16MetaClass);
        private static readonly MetaType F32 = SystemMethodCallTypes.Of(CoreMetaClassManager.float32MetaClass);
        private static readonly MetaType F64 = SystemMethodCallTypes.Of(CoreMetaClassManager.float64MetaClass);
        private static readonly MetaType Fn = SystemMethodCallTypes.Of(CoreMetaClassManager.functionMetaClass);

        // Populated dynamically by LoadFromJsonFile / LoadFromJsonContent.
        private static Dictionary<string, SystemMethodCallDeclaration> s_Decl =
            new Dictionary<string, SystemMethodCallDeclaration>();

        private static List<SystemMethodCallDeclaration> s_ProjectDefine = new List<SystemMethodCallDeclaration>();
        /// <summary>
        /// Resolves a type name string (from JSON config) to a MetaType singleton.
        /// </summary>
        private static MetaType ResolveTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return null;
            switch (typeName.ToLowerInvariant())
            {
                case "void":
                    return Void;
                case "object": return Obj;
                case "string": return Str;
                case "bool":
                case "boolean":
                    return Bool;
                case "num":return Num;
                case "int32": return I32;
                case "uint32": return U32;
                case "int8": return I8;
                case "uint8": return U8;
                case "int16": return I16;
                case "uint16":return U16;
                case "int64":return I64;
                case "uint64": return U64;
                case "float8": return F8;
                case "float16": return F16;
                case "float32": return F32;
                case "float64":return F64;
                case "type":return Typ;
                case "function": return Fn;
                case "array<object>": return ArrayObj;
                case "uint8array":    return UInt8Array;
                default:        return null;
            }
        }
        public static void AddDecl(string name, MetaType retType, List<MetaType> paramTypes, bool variadic, string cvmFunction = null)
        {
            var decl = new SystemMethodCallDeclaration(
                name, retType, variadic, paramTypes.ToArray(), cvmFunction);
            s_Decl[name] = decl;
        }
        public static void LoadConfigSystemCall()
        {
            foreach( var sc in ProjectManager.config.systemCalls )
            {
                AddDeclByMt(sc.name, sc.returnType, new List<string>(sc.@params), sc.isVariadic, true, sc.cvmFunction, sc.className);
            }
        }
        public static void AddDeclByMt(string name, string rt, List<string> mtList, bool variadic, bool isProjectDefine, string cvmFunction = null, string className = null)
        {
            var retType = ResolveTypeName(rt);
            var paramTypes = new List<MetaType>();
            if (paramTypes == null)
            {
                Log.AddProcessLog(LID.ProcessSystemMethodCallNotFoundImportSystem, $"import system method call param type not found! name={name}, paramType={paramTypes}");
                return;
            }
            foreach (var mt in mtList)
            {
                var mtadc = ResolveTypeName(mt);
                if( mtadc == null)
                {
                    Log.AddProcessLog(LID.ProcessSystemMethodCallNotFoundImportSystem2, $"import system method call param type not found! name={name}, paramType={mt}");
                    return;
                }
                paramTypes.Add(mtadc);
            }
            var decl = new SystemMethodCallDeclaration(
                name, retType, variadic, paramTypes.ToArray(), cvmFunction, className);
            if( s_Decl.ContainsKey(name ) )
            {
                /* Same-name re-registration is idempotent when the full signature
                 * matches (e.g. the atsign sentinel AtSignLabelCall declared locally
                 * in the caller project and re-exported by a referenced module such
                 * as Core): keep the first silently, only a genuine signature
                 * conflict is an error. */
                if( !IsSameDecl( s_Decl[name], decl ) )
                {
                    Log.AddProcessLog(LID.ProcessSystemMethodCallImportSystemMethod, $"import system method call name had define! name={name}, class={className}");
                }
                return;
            }
            s_Decl[name] = decl;
            if (isProjectDefine)
            {
                s_ProjectDefine.Add(decl);
            }
        }

        private static bool IsSameDecl( SystemMethodCallDeclaration a, SystemMethodCallDeclaration b )
        {
            if (a.isVariadic != b.isVariadic || a.cvmFunction != b.cvmFunction)
                return false;
            if (a.returnMetaType?.metaClass != b.returnMetaType?.metaClass)
                return false;
            if (a.paramMetaTypeList.Count != b.paramMetaTypeList.Count)
                return false;
            for (int i = 0; i < a.paramMetaTypeList.Count; i++)
            {
                if (a.paramMetaTypeList[i]?.metaClass != b.paramMetaTypeList[i]?.metaClass)
                    return false;
            }
            return true;
        }
        /// <summary>
        /// 通过 string name 查找 SystemMethodCallDeclaration（含返回类型、参数类型等元数据）。
        /// </summary>
        public static bool TryGetDeclaration(string name, out SystemMethodCallDeclaration decl)
        {
            if (string.IsNullOrEmpty(name))
            {
                decl = null;
                return false;
            }
            if(s_Alias.TryGetValue(name, out string aliasName))
            {
                name = aliasName;
            }
            return s_Decl.TryGetValue(name, out decl);
        }

        /// <summary>
        /// 类路径形态查找：按 (ownerClassAllName, 方法名) 匹配带 class 归属标记的声明。
        /// 用于 MetaCallNode.GetFunctionOrVariableByOwnerClass 的成员函数未命中回退——
        /// SLang.Plugin.CSharpMono.CSharpCallInt(...) 这类"声明式注册"调用
        /// （jsonc systemCalls 带 "class" 字段，SL 侧不写桩方法体）。
        /// className 为空或方法名未注册时一律不命中，不影响全局裸调用路径。
        /// </summary>
        public static bool TryGetDeclarationByClass(string className, string name, out SystemMethodCallDeclaration decl)
        {
            decl = null;
            if (string.IsNullOrEmpty(className) || string.IsNullOrEmpty(name))
            {
                return false;
            }
            if (!s_Decl.TryGetValue(name, out decl))
            {
                return false;
            }
            if (decl.className != className)
            {
                decl = null;
                return false;
            }
            return true;
        }
    }
}
