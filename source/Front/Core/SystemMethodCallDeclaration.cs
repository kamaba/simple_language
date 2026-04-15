using System.Collections.Generic;

namespace SimpleLanguage.Core
{
    public sealed class SystemMethodCallDeclaration
    {
        public ESystemMethodCall method { get; }
        public MetaClass returnMetaClass { get; }
        public List<MetaClass> paramMetaClassList { get; }
        public bool isVariadic { get; }

        public SystemMethodCallDeclaration(ESystemMethodCall method, MetaClass ret, bool variadic, params MetaClass[] paramTypes)
        {
            this.method = method;
            returnMetaClass = ret;
            isVariadic = variadic;
            paramMetaClassList = new List<MetaClass>();
            if (paramTypes != null)
            {
                for (int i = 0; i < paramTypes.Length; i++)
                {
                    paramMetaClassList.Add(paramTypes[i]);
                }
            }
        }
    }

    public static class SystemMethodCallDeclarationRegistry
    {
        private static readonly Dictionary<ESystemMethodCall, SystemMethodCallDeclaration> s_Decl = new Dictionary<ESystemMethodCall, SystemMethodCallDeclaration>
        {
            // bridge calls
            { ESystemMethodCall.SystemCallCLRMethod, new SystemMethodCallDeclaration(ESystemMethodCall.SystemCallCLRMethod, CoreMetaClassManager.objectMetaClass, true, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemCallNativeMethod, new SystemMethodCallDeclaration(ESystemMethodCall.SystemCallNativeMethod, CoreMetaClassManager.objectMetaClass, true, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemCallJVMMethod, new SystemMethodCallDeclaration(ESystemMethodCall.SystemCallJVMMethod, CoreMetaClassManager.objectMetaClass, true, CoreMetaClassManager.objectMetaClass) },

            // console
            { ESystemMethodCall.SystemPrint, new SystemMethodCallDeclaration(ESystemMethodCall.SystemPrint, CoreMetaClassManager.voidMetaClass, true, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemPrintln, new SystemMethodCallDeclaration(ESystemMethodCall.SystemPrintln, CoreMetaClassManager.voidMetaClass, true, CoreMetaClassManager.objectMetaClass) },

            { ESystemMethodCall.SystemReadLine, new SystemMethodCallDeclaration(ESystemMethodCall.SystemReadLine, CoreMetaClassManager.stringMetaClass, false) },
            { ESystemMethodCall.SystemReadKey, new SystemMethodCallDeclaration(ESystemMethodCall.SystemReadKey, CoreMetaClassManager.stringMetaClass, false) },

            // convert
            { ESystemMethodCall.SystemConvertBool, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertBool, CoreMetaClassManager.booleanMetaClass, false, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemConvertInt8, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertInt8, CoreMetaClassManager.byteMetaClass, false, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemConvertSInt8, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertSInt8, CoreMetaClassManager.sbyteMetaClass, false, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemConvertInt16, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertInt16, CoreMetaClassManager.int16MetaClass, false, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemConvertUInt16, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertUInt16, CoreMetaClassManager.uint16MetaClass, false, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemConvertInt32, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertInt32, CoreMetaClassManager.int32MetaClass, false, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemConvertUInt32, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertUInt32, CoreMetaClassManager.uint32MetaClass, false, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemConvertInt64, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertInt64, CoreMetaClassManager.int64MetaClass, false, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemConvertUInt64, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertUInt64, CoreMetaClassManager.uint64MetaClass, false, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemConvertFloat32, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertFloat32, CoreMetaClassManager.float32MetaClass, false, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemConvertFloat64, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertFloat64, CoreMetaClassManager.float64MetaClass, false, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemConvertString, new SystemMethodCallDeclaration(ESystemMethodCall.SystemConvertString, CoreMetaClassManager.stringMetaClass, false, CoreMetaClassManager.objectMetaClass) },

            // object
            { ESystemMethodCall.SystemEqualObject, new SystemMethodCallDeclaration(ESystemMethodCall.SystemEqualObject, CoreMetaClassManager.booleanMetaClass, false, CoreMetaClassManager.objectMetaClass, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemObjectGetType, new SystemMethodCallDeclaration(ESystemMethodCall.SystemObjectGetType, CoreMetaClassManager.typeMetaClass, false, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemObjectGetHashCode, new SystemMethodCallDeclaration(ESystemMethodCall.SystemObjectGetHashCode, CoreMetaClassManager.int32MetaClass, false, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemObjectRef, new SystemMethodCallDeclaration(ESystemMethodCall.SystemObjectRef, CoreMetaClassManager.objectMetaClass, false, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemObjectRefWeak, new SystemMethodCallDeclaration(ESystemMethodCall.SystemObjectRefWeak, CoreMetaClassManager.objectMetaClass, false, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemObjectRefCount, new SystemMethodCallDeclaration(ESystemMethodCall.SystemObjectRefCount, CoreMetaClassManager.int32MetaClass, false, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemObjectFree, new SystemMethodCallDeclaration(ESystemMethodCall.SystemObjectFree, CoreMetaClassManager.voidMetaClass, false, CoreMetaClassManager.objectMetaClass) },
            { ESystemMethodCall.SystemObjectRelease, new SystemMethodCallDeclaration(ESystemMethodCall.SystemObjectRelease, CoreMetaClassManager.voidMetaClass, false, CoreMetaClassManager.objectMetaClass) },

            // array helpers
            { ESystemMethodCall.SystemArrayGetValueThis, new SystemMethodCallDeclaration(ESystemMethodCall.SystemArrayGetValueThis, CoreMetaClassManager.objectMetaClass, false, CoreMetaClassManager.arrayMetaClass, CoreMetaClassManager.int32MetaClass) },
            { ESystemMethodCall.SystemArraySetValueThis, new SystemMethodCallDeclaration(ESystemMethodCall.SystemArraySetValueThis, CoreMetaClassManager.voidMetaClass, false, CoreMetaClassManager.arrayMetaClass, CoreMetaClassManager.int32MetaClass, CoreMetaClassManager.objectMetaClass) },

            // string slice / bytes (instance: this + args)
            { ESystemMethodCall.SystemStringFront, new SystemMethodCallDeclaration(ESystemMethodCall.SystemStringFront, CoreMetaClassManager.stringMetaClass, false, CoreMetaClassManager.stringMetaClass, CoreMetaClassManager.int32MetaClass) },
            { ESystemMethodCall.SystemStringEnd, new SystemMethodCallDeclaration(ESystemMethodCall.SystemStringEnd, CoreMetaClassManager.stringMetaClass, false, CoreMetaClassManager.stringMetaClass, CoreMetaClassManager.int32MetaClass) },
            { ESystemMethodCall.SystemStringRange, new SystemMethodCallDeclaration(ESystemMethodCall.SystemStringRange, CoreMetaClassManager.stringMetaClass, false, CoreMetaClassManager.stringMetaClass, CoreMetaClassManager.int32MetaClass, CoreMetaClassManager.int32MetaClass) },
            { ESystemMethodCall.SystemStringToByteArray, new SystemMethodCallDeclaration(ESystemMethodCall.SystemStringToByteArray, CoreMetaClassManager.arrayMetaClass, false, CoreMetaClassManager.stringMetaClass) },
        };

        public static bool TryGet(ESystemMethodCall call, out SystemMethodCallDeclaration decl)
        {
            return s_Decl.TryGetValue(call, out decl);
        }
    }
}
