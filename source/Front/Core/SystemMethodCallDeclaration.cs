using System.Collections.Generic;

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
            return CoreMetaClassManager.arrayMetaClass.AddMetaPreTemplateClass(mt, true, out _);
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
        private static readonly MetaType Obj = SystemMethodCallTypes.Of(CoreMetaClassManager.objectMetaClass);
        private static readonly MetaType Void = SystemMethodCallTypes.Of(CoreMetaClassManager.voidMetaClass);
        private static readonly MetaType Str = SystemMethodCallTypes.Of(CoreMetaClassManager.stringMetaClass);
        private static readonly MetaType Bool = SystemMethodCallTypes.Of(CoreMetaClassManager.booleanMetaClass);
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

            // convert
            { ESystemMethodCall.SystemConvertBool, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertBool, Bool, false, Obj) },
            { ESystemMethodCall.SystemConvertInt8, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertInt8, U8, false, Obj, I32) },
            { ESystemMethodCall.SystemConvertSInt8, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertSInt8, I8, false, Obj, I32) },
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

            // string slice / bytes (instance: this + args)
            { ESystemMethodCall.SystemStringFront, new SystemMethodCallDeclaration(ESystemMethodCall.SystemStringFront, Str, false, Str, I32) },
            { ESystemMethodCall.SystemStringEnd, new SystemMethodCallDeclaration(ESystemMethodCall.SystemStringEnd, Str, false, Str, I32) },
            { ESystemMethodCall.SystemStringRange, new SystemMethodCallDeclaration(ESystemMethodCall.SystemStringRange, Str, false, Str, I32, I32) },
            { ESystemMethodCall.SystemStringToUInt8Array, new SystemMethodCallDeclaration(ESystemMethodCall.SystemStringToUInt8Array, UInt8Array, false, Str) },
        };

        public static bool TryGet(ESystemMethodCall call, out SystemMethodCallDeclaration decl)
        {
            return s_Decl.TryGetValue(call, out decl);
        }
    }
}
