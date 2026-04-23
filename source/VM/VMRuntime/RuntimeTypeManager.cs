//****************************************************************************
//  File:      RuntimeType.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.VM.Runtime;
using SimpleLanguage.VM.MemoryManagement;
using SimpleLanguage.Parse;

namespace SimpleLanguage.VM
{
    public static class RuntimeTypeManager
    {
        public static List<RuntimeType> runtimeTypeList => s_RuntimeTypeList;
        public static RuntimeType voidRuntimeType { get => m_VoidRuntimeType; }
        public static RuntimeType objectRuntimeType { get => m_ObjectRuntimeType; }
        public static RuntimeType boolRuntimeType { get => m_BoolRuntimeType; }
        public static RuntimeType uint8RuntimeType { get => m_UInt8RuntimeType; }
        public static RuntimeType int8RuntimeType { get => m_Int8RuntimeType; }
        public static RuntimeType int16RuntimeType { get => m_Int16RuntimeType; }
        public static RuntimeType uint16RuntimeType { get => m_UInt16RuntimeType; }
        public static RuntimeType int32RuntimeType { get => m_Int32RuntimeType; }
        public static RuntimeType uint32RuntimeType { get => m_UInt32RuntimeType; }
        public static RuntimeType int64RuntimeType { get => m_Int64RuntimeType; }
        public static RuntimeType uint64RuntimeType { get => m_UInt64RuntimeType; }
        public static RuntimeType float32RuntimeType { get => m_Float32RuntimeType; }
        public static RuntimeType float64RuntimeType { get => m_Float64RuntimeType; }
        public static RuntimeType stringRuntimeType { get => m_StringRuntimeType; }
        public static RuntimeType numRuntimeType { get => m_NumRuntimeType; }
        public static RuntimeType typeRuntimeType { get => m_TypeRuntimeType; }
        public static RuntimeType memberRuntimeType { get => m_MemberRuntimeType; }

        private static List<RuntimeType> s_RuntimeTypeList = new List<RuntimeType>();
        private static List<RuntimeType> s_CoreRuntimeTypeList = new List<RuntimeType>();
        private static RuntimeType m_ObjectRuntimeType = null;
        private static RuntimeType m_TypeRuntimeType = null;
        private static RuntimeType m_VoidRuntimeType = null;
        private static RuntimeType m_BoolRuntimeType = null;
        private static RuntimeType m_NumRuntimeType = null;
        private static RuntimeType m_UInt8RuntimeType = null;
        private static RuntimeType m_Int8RuntimeType = null;
        private static RuntimeType m_Int16RuntimeType = null;
        private static RuntimeType m_UInt16RuntimeType = null;
        private static RuntimeType m_Int32RuntimeType = null;
        private static RuntimeType m_UInt32RuntimeType = null;
        private static RuntimeType m_Int64RuntimeType = null;
        private static RuntimeType m_UInt64RuntimeType = null;
        private static RuntimeType m_Float32RuntimeType = null;
        private static RuntimeType m_Float64RuntimeType = null;
        private static RuntimeType m_StringRuntimeType = null;
        private static RuntimeType m_MemberRuntimeType = null;

        // Ensure core primitive runtime types are registered (and their static fields populated)
        // before VM global initialization and object creation.
        public static void EnsureCoreRuntimeTypesRegistered()
        {
            EnsureByClassName("Core.Object", ref m_ObjectRuntimeType, true );
            EnsureByClassName("Core.Void", ref m_VoidRuntimeType, true);
            EnsureByClassName("Core.Type", ref m_TypeRuntimeType, true);
            EnsureByClassName("Core.Boolean", ref m_BoolRuntimeType, true);
            EnsureByClassName("Core.Num", ref m_NumRuntimeType, true);
            EnsureByClassName("Core.UInt8", ref m_UInt8RuntimeType, true);
            EnsureByClassName("Core.Int8", ref m_Int8RuntimeType, true);
            EnsureByClassName("Core.Int16", ref m_Int16RuntimeType, true);
            EnsureByClassName("Core.UInt16", ref m_UInt16RuntimeType, true);
            EnsureByClassName("Core.Int32", ref m_Int32RuntimeType, true);
            EnsureByClassName("Core.UInt32", ref m_UInt32RuntimeType, true);
            EnsureByClassName("Core.Int64", ref m_Int64RuntimeType, true);
            EnsureByClassName("Core.UInt64", ref m_UInt64RuntimeType, true);
            EnsureByClassName("Core.String", ref m_StringRuntimeType, true);
            EnsureByClassName("Core.Float32", ref m_Float32RuntimeType, true);
            EnsureByClassName("Core.Float64", ref m_Float64RuntimeType, true);
            //EnsureRuntimeTypeRegisteredByClassName("Core.Array");
        }
        public static bool IsCoreRuntimeType(RuntimeType rt )
        {
            if(s_CoreRuntimeTypeList.Find( a=> a == rt  ) != null)
            {
                return true;
            }
            return false;
        }
        public static bool IsMemberDataDirectType(EVMType evmType)
        {
            return evmType == EVMType.Boolean
                || evmType == EVMType.UInt8
                || evmType == EVMType.Int8
                || evmType == EVMType.Int16
                || evmType == EVMType.UInt16
                || evmType == EVMType.Int32
                || evmType == EVMType.UInt32
                || evmType == EVMType.Int64
                || evmType == EVMType.UInt64
                || evmType == EVMType.Float32
                || evmType == EVMType.Float64;
        }
        public static bool IsNumericTypeLocal(EVMType t)
        {
            return t == EVMType.Num
                || t == EVMType.Int8
                || t == EVMType.UInt8
                || t == EVMType.Int16
                || t == EVMType.UInt16
                || t == EVMType.Int32
                || t == EVMType.UInt32
                || t == EVMType.Int64
                || t == EVMType.UInt64
                || t == EVMType.Float32
                || t == EVMType.Float64;
        }
        public static RuntimeType GetRuntimeTypeByEVMType(EVMType vmtype)
        {
            var rt = vmtype switch
            {
                EVMType.Object => m_ObjectRuntimeType,
                EVMType.Boolean => m_BoolRuntimeType,
                EVMType.UInt8 => m_UInt8RuntimeType,
                EVMType.Int8 => m_Int8RuntimeType,
                EVMType.Int16 => m_Int16RuntimeType,
                EVMType.UInt16 => m_UInt16RuntimeType,
                EVMType.Int32 => m_Int32RuntimeType,
                EVMType.UInt32 => m_UInt32RuntimeType,
                EVMType.Int64 => m_Int64RuntimeType,
                EVMType.UInt64 => m_UInt64RuntimeType,
                EVMType.Float32 => m_Float32RuntimeType,
                EVMType.Float64 => m_Float64RuntimeType,
                EVMType.String => m_StringRuntimeType,
                EVMType.Num => m_NumRuntimeType,
                EVMType.Void => m_VoidRuntimeType,
                EVMType.Type => m_TypeRuntimeType,
                _ => null
            };
            return rt;
        }
        private static void EnsureByClassName(string runtimeClassName, ref RuntimeType targetField, bool isCore = false )
        {
            if (targetField != null) return;

            var rc = RuntimeClassManager.GetRuntimeClassByName(runtimeClassName);
            if (rc == null)
            {
                // Prefer package-driven creation so RuntimeClass has full field/method/template data.
                rc = SLRuntimeModuleRegistry.ResolveOrCreateRuntimeClassByName(runtimeClassName);
                if (rc == null)
                {
                    // Fallback: try by stable id (if exported ids match the stable hash).
                    rc = SLRuntimeModuleRegistry.ResolveOrCreateRuntimeClassById(StableId32(runtimeClassName));
                }
                if (rc == null)
                {
                    // Last resort: create minimal RuntimeClass so ObjectManager can still build primitive objects.
                    rc = new RuntimeClass
                    {
                        id = StableId32(runtimeClassName),
                        name = runtimeClassName,
                    };
                    RuntimeClassManager.AddRuntimeClass(rc);
                }
            }

            // If a runtime type already exists (possibly created via template-based path),
            // reuse it to avoid duplicate RuntimeType instances.
            var existed = GetRuntimeTypeById(rc.id);
            if (existed != null)
            {
                targetField = existed;
                return;
            }

            if (isCore)
            {
                targetField = AddRuntimeTypeByCoreClass(rc);
            }
            else
            {
                targetField = AddRuntimeTypeByClass(rc);
            }
        }

        private static int StableId32(string s)
        {
            unchecked
            {
                const uint fnvOffset = 2166136261;
                const uint fnvPrime = 16777619;
                uint hash = fnvOffset;
                for (int i = 0; i < s.Length; i++)
                {
                    hash ^= s[i];
                    hash *= fnvPrime;
                }
                return (int)hash;
            }
        }

        /// <summary>
        /// Builds a <see cref="TypeObject"/> that <i>describes</i> <paramref name="rt"/> (for <c>GetType()</c> / <c>typeof</c>).
        /// Previous implementation always returned null, so <c>type.toString()</c> was empty and type handles were broken.
        /// </summary>
        public static ClassObject? CreateTypeObject(RuntimeType? rt)
        {
            if (rt == null) return null;
            try
            {
                EnsureCoreRuntimeTypesRegistered();
                var tobj = new TypeObject(rt);
                tobj.CreateObject();
                if (tobj.refCount == 0)
                    tobj.refCount = 1;
                SlMemoryManager.Instance.RegisterAllocation(tobj);
                return tobj;
            }
            catch
            {
                return null;
            }
        }

        public static RuntimeType GetRuntimeTypeByRuntimeClass(RuntimeClass rmc)
        {
            if (rmc == null) return null;
            return s_RuntimeTypeList.Find(r => r.runtimeClass != null && r.runtimeClass.id == rmc.id);
        }
        public static RuntimeType GetRuntimeTypeById( int id )
        {
            return s_RuntimeTypeList.Find(r => r.runtimeClass != null && r.runtimeClass.id == id );
        }
        public static RuntimeType GetRuntimeTypeByDefTypeAndAdd(RuntimeDefType irmt )
        {
            var rt = GetRuntimeTypeByDefType(irmt);
            if( rt == null )
            {
                List<RuntimeType> rtList = new List<RuntimeType>();
                if (irmt.runtimeDefTypeList.Count > 0)
                {
                    for (int i = 0; i < irmt.runtimeDefTypeList.Count; i++)
                    {
                        var crt = GetRuntimeTypeByDefTypeAndAdd(irmt.runtimeDefTypeList[i]);
                        rtList.Add(crt);
                    }
                }
                rt = RuntimeTypeManager.AddRuntimeTypeByRuntimeClassAndRuntimeTypeList(irmt.runtimeClass, rtList);
            }
            return rt;
        }
        public static RuntimeType GetRuntimeTypeByDefType(RuntimeDefType irmt )
        {
            if (irmt == null) return null;
            foreach (var v in s_RuntimeTypeList)
            {
                if (v.runtimeClass != irmt.runtimeClass)
                {
                    continue;
                }

                if (v.runtimeTemplateList.Count == irmt.runtimeDefTypeList.Count )
                {
                    if (v.runtimeTemplateList.Count == 0)
                    {
                        return v;
                    }
                    bool flag = true;
                    for (int i = 0; i < irmt.runtimeDefTypeList.Count; i++)
                    {
                        var ft = GetRuntimeTypeByDefType(irmt.runtimeDefTypeList[i]);
                        if (!RuntimeType.SameRuntimeType(ft, v.runtimeTemplateList[i]))
                        {
                            flag = false;
                            break;
                        }
                    }
                    if (flag)
                        return v;
                }
            }
            return null;
        }
        public static RuntimeType GetRuntimeTypeByRuntimeClassAndRuntimeTypeList( RuntimeClass rmc, List<RuntimeType> inputTemplateTypeList)
        {
            foreach (var v in s_RuntimeTypeList)
            {
                if (v.runtimeClass != rmc)
                {
                    continue;
                }

                if (v.runtimeTemplateList.Count == inputTemplateTypeList.Count)
                {
                    if (v.runtimeTemplateList.Count == 0)
                    {
                        return v;
                    }
                    bool flag = true;
                    for (int i = 0; i < inputTemplateTypeList.Count; i++)
                    {
                        if (!RuntimeType.SameRuntimeType(inputTemplateTypeList[i], v.runtimeTemplateList[i]))
                        {
                            flag = false;
                            break;
                        }
                    }
                    if (flag)
                        return v;
                }
            }
            return null;
        }
        public static RuntimeType AddRuntimeTypeByRuntimeClassAndRuntimeTypeList(RuntimeClass rmc, List<RuntimeType> inputTemplateTypeList)
        {
            if (rmc == null) return null;
            RuntimeType rt = new RuntimeType(rmc, inputTemplateTypeList);
            s_RuntimeTypeList.Add(rt);
            rt.EnsureStaticMemberObjectsInitialized();
            return rt;
        }
        public static RuntimeType AddRuntimeTypeByCoreClass(RuntimeClass rmc)
        {
            RuntimeType rt = new RuntimeType(rmc, null);

            string name = rmc.name;
            if( name == "Object" || name == "Core.Object" )
            {
                m_ObjectRuntimeType = rt;
                m_ObjectRuntimeType.SetEVMType(EVMType.Object);
            }
            if (name == "Void" || name == "Core.Void")
            {
                m_VoidRuntimeType = rt;
                m_VoidRuntimeType.SetEVMType(EVMType.Void);
            }
            else if (name == "Type" || name == "Core.Type")
            {
                m_TypeRuntimeType = rt;
                m_TypeRuntimeType.SetEVMType(EVMType.Type);
            }
            else if (name == "Boolean" || name == "Core.Boolean")
            {
                m_BoolRuntimeType = rt;
                m_BoolRuntimeType.SetEVMType(EVMType.Boolean);
            }
            else if (name == "Num" || name == "Core.Num")
            {
                m_NumRuntimeType = rt;
                m_NumRuntimeType.SetEVMType(EVMType.Num);
            }
            else if (name == "UInt8" || name == "Core.UInt8")
            {
                m_UInt8RuntimeType = rt;
                m_UInt8RuntimeType.SetEVMType(EVMType.UInt8);
            }
            else if (name == "Int8" || name == "Core.Int8")
            {
                m_Int8RuntimeType = rt;
                m_Int8RuntimeType.SetEVMType(EVMType.Int8);
            }
            else if (name == "Int16" || name == "Core.Int16")
            {
                m_Int16RuntimeType = rt;
                m_Int16RuntimeType.SetEVMType(EVMType.Int16);
            }
            else if (name == "UInt16" || name == "Core.UInt16")
            {
                m_UInt16RuntimeType = rt;
                m_UInt16RuntimeType.SetEVMType(EVMType.UInt16);
            }
            else if (name == "Int32" || name == "Core.Int32")
            {
                m_Int32RuntimeType = rt;
                m_Int32RuntimeType.SetEVMType(EVMType.Int32);
            }
            else if (name == "UInt32" || name == "Core.UInt32")
            {
                m_UInt32RuntimeType = rt;
                m_UInt32RuntimeType.SetEVMType(EVMType.UInt32);
            }
            else if (name == "Int64" || name == "Core.Int64")
            {
                m_Int64RuntimeType = rt;
                m_Int64RuntimeType.SetEVMType(EVMType.Int64);
            }
            else if (name == "UInt64" || name == "Core.UInt64")
            {
                m_UInt64RuntimeType = rt;
                m_UInt64RuntimeType.SetEVMType(EVMType.UInt64);
            }
            else if (name == "Float32" || name == "Core.Float32")
            {
                m_Float32RuntimeType = rt;
                m_Float32RuntimeType.SetEVMType(EVMType.Float32);
            }
            else if (name == "Float64" || name == "Core.Float64")
            {
                m_Float64RuntimeType = rt;
                m_Float64RuntimeType.SetEVMType(EVMType.Float64);
            }
            else if (name == "String" || name == "Core.String")
            {
                m_StringRuntimeType = rt;
                m_StringRuntimeType.SetEVMType(EVMType.String);
            }
            s_RuntimeTypeList.Add(rt);
            s_CoreRuntimeTypeList.Add(rt);
            rt.EnsureStaticMemberObjectsInitialized();

            return rt;
        }
        public static RuntimeType AddRuntimeTypeByClass(RuntimeClass rmc )
        {
            RuntimeType rt = new RuntimeType(rmc, null);
            rt.EnsureStaticMemberObjectsInitialized();

            s_RuntimeTypeList.Add(rt);

            return rt;
        }
    }
}
