//****************************************************************************
//  File:      IRMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.VM.Runtime;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleLanguage.VM
{
    public class RuntimeType
    {
        public RuntimeClass runtimeClass => m_RuntimeClass;
        public List<RuntimeType> runtimeTemplateList => m_RuntimeTemplateList;

        private RuntimeClass m_RuntimeClass = null;
        private List<RuntimeType> m_RuntimeTemplateList = new List<RuntimeType>();
        private SObject[] m_StaticMemObjectList;
        public EVMType eType { get; set; }

        public RuntimeType( RuntimeClass rc, List<RuntimeType> rtList)
        {
            m_RuntimeClass = rc;
            if (rtList != null)
            {
                m_RuntimeTemplateList = rtList;
            }

            m_StaticMemObjectList = new SObject[m_RuntimeClass.staticIRMetaVariableList.Count];
            for (int i = 0; i < m_RuntimeClass.staticIRMetaVariableList.Count; i++)
            {
                RuntimeType rt = GetClassRuntimeType(m_RuntimeClass.staticIRMetaVariableList[i].runtimeDefType, true);
                m_StaticMemObjectList[i] = ObjectManager.CreateObjectByRuntimeType(rt, true);
            }

            if (Enum.TryParse<EVMType>(m_RuntimeClass.name, true, out var eoutType))
            {
                eType = eoutType;
            }
            else
            {
                eType = EVMType.Class;
            }
            //eType = GetVMType(irClass.irName);
        }
        public static EVMType GetVMType(string irName)
        {
            // Minimal mapping by known IR names used by ObjectManager
            if (string.IsNullOrEmpty(irName)) return EVMType.Class;
            if (irName.EndsWith("Int32") || irName.EndsWith("Int16") || irName.EndsWith("Int64") || irName.EndsWith("UInt32") || irName.EndsWith("UInt16") || irName.EndsWith("UInt64") || irName.EndsWith("Byte") || irName.EndsWith("SByte"))
                return EVMType.Num;
            if (irName.EndsWith("Float32") || irName.EndsWith("Float64"))
                return EVMType.Num;
            if (irName.EndsWith("String"))
                return EVMType.String;
            if (irName.EndsWith("Boolean"))
                return EVMType.Boolean;
            return EVMType.Class;
        }

        public RuntimeType GetExtendsTemplateRuntimeType( RuntimeDefType irmt, List<RuntimeType> _runtimeTemplateList)
        {
            if (_runtimeTemplateList?.Count > 0)
            {
                return _runtimeTemplateList[irmt.templateIndex];
            }
            return null;
        }
        public RuntimeType GetClassRuntimeType( RuntimeDefType rdt, bool isAdd = false)
        {
            var irmc = this.m_RuntimeClass;
            if (rdt.templateIndex != -1)
            {
                if (rdt.ownerRuntimeClass == this.m_RuntimeClass)
                {
                    return m_RuntimeTemplateList[rdt.templateIndex];
                }
                else
                {
                    var mt = m_RuntimeClass.GetRuntimeDefTypeByTemplateAndClassRelation(rdt.ownerRuntimeClass, rdt.templateIndex);

                    return GetClassRuntimeType(mt, isAdd);
                }
            }
            else
            {
                List<RuntimeType> rtList = new List<RuntimeType>();
                if (rdt.runtimeDefTypeList.Count > 0)
                {
                    for (int i = 0; i < rdt.runtimeDefTypeList.Count; i++)
                    {
                        var crt = GetClassRuntimeType(rdt.runtimeDefTypeList[i], isAdd);
                        rtList.Add(crt);
                    }
                }
                var rt = RuntimeTypeManager.GetRuntimeTypeByMTAndTemplateMT(rdt.runtimeClass, rtList);
                if (rt == null && isAdd)
                {
                    rt = RuntimeTypeManager.AddRuntimeTypeByClassAndTemplate(rdt.runtimeClass, rtList);
                }
                return rt;
            }
        }
        public void GetMemberVariableSValue(int index, ref SValue svalue)
        {
            if (m_StaticMemObjectList == null)
            {
                // initialize static array if possible
                if (m_RuntimeClass?.staticIRMetaVariableList == null)
                {
                    svalue.SetNull();
                    return;
                }
                m_StaticMemObjectList = new SObject[m_RuntimeClass.staticIRMetaVariableList.Count];
                for (int i = 0; i < m_StaticMemObjectList.Length; i++)
                {
                    var irmv = m_RuntimeClass.staticIRMetaVariableList[i];
                    var rt = GetClassRuntimeType(irmv.runtimeDefType, true);
                    m_StaticMemObjectList[i] = ObjectManager.CreateObjectByRuntimeType(rt, true);
                }
            }
            if (index < 0 || index >= m_StaticMemObjectList.Length)
            {
                svalue.SetNull();
                return;
            }
            var sobj = m_StaticMemObjectList[index];
            if (sobj == null || sobj.isNull)
            {
                svalue.SetNull();
                return;
            }
            svalue.SetSObject(sobj);
        }
        public void SetMemberVariableSValue(int index, SValue svalue)
        {
            if (m_StaticMemObjectList == null)
            {
                if (m_RuntimeClass?.staticIRMetaVariableList == null)
                {
                    return;
                }
                m_StaticMemObjectList = new SObject[m_RuntimeClass.staticIRMetaVariableList.Count];
                for (int i = 0; i < m_StaticMemObjectList.Length; i++)
                {
                    var irmv = m_RuntimeClass.staticIRMetaVariableList[i];
                    var rt = GetClassRuntimeType(irmv.runtimeDefType, true);
                    m_StaticMemObjectList[i] = ObjectManager.CreateObjectByRuntimeType(rt, true);
                }
            }
            if (index < 0 || index >= m_StaticMemObjectList.Length) return;
            var target = m_StaticMemObjectList[index];
            if (svalue.isNull)
            {
                target.SetNull();
                return;
            }
            // attempt to set by type-aware method on SObject
            target.SetValueByType(svalue.eType == EVMType.Class ? EVMType.Class : svalue.eType, svalue.eType == EVMType.Class ? (object)svalue.sobject : svalue.GetValueObject());
        }
        public List<Instruction> CreateStaticMetaMetaVariableIRList()
        {
            return new List<Instruction>();
        }
        public static bool SameRuntimeType(RuntimeType rt1, RuntimeType rt2)
        {
            if (rt1 == null || rt2 == null) return false;
            return rt1.m_RuntimeClass.id == rt2.m_RuntimeClass.id;
        }
        public bool IsExtendsRelation(RuntimeType rt)
        {
            if (rt == null) return false;
            return m_RuntimeClass.IsExtendsRelation(rt.m_RuntimeClass);
        }
        public static bool IsNumericEType(EVMType t)
        {
            return t == EVMType.Num;
        }
        public bool IsExtendsRelationWithPrimitiveSupport(RuntimeType rt)
        {
            return IsExtendsRelation(rt);
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(m_RuntimeClass.name );
            if( m_RuntimeTemplateList.Count > 0 )
            {
                sb.Append("<");
                for( int i = 0; i < m_RuntimeTemplateList.Count; i++ )
                {
                    sb.Append(m_RuntimeTemplateList[i].ToString());
                }
                sb.Append(">");
            }
            return sb.ToString();
        }
    }

    public static class RuntimeTypeManager
    {
        public static List<RuntimeType> runtimeTypeList => s_RuntimeTypeList;
        public static RuntimeType voidRuntimeType { get => m_VoidRuntimeType; }
        public static RuntimeType boolRuntimeType { get => m_BoolRuntimeType; }
        public static RuntimeType byteRuntimeType { get => m_ByteRuntimeType; }
        public static RuntimeType sbyteRuntimeType { get => m_SByteRuntimeType; }
        public static RuntimeType int16RuntimeType { get => m_Int16RuntimeType; }
        public static RuntimeType uint16RuntimeType { get => m_UInt16RuntimeType; }
        public static RuntimeType int32RuntimeType { get => m_Int32RuntimeType; }
        public static RuntimeType uint32RuntimeType { get => m_UInt32RuntimeType; }
        public static RuntimeType int64RuntimeType { get => m_Int64RuntimeType; }
        public static RuntimeType uint64RuntimeType { get => m_UInt64RuntimeType; }
        public static RuntimeType float32RuntimeType { get => m_Float32RuntimeType; }
        public static RuntimeType float64runtimeType { get => m_Float64RuntimeType; }
        public static RuntimeType stringRuntimeType { get => m_StringRuntimeType; }
        public static RuntimeType numRuntimeType { get => m_NumRuntimeType; }
        public static RuntimeType typeRuntimeType { get => m_TypeRuntimeType; }

        private static List<RuntimeType> s_RuntimeTypeList = new List<RuntimeType>();
        private static RuntimeType m_TypeRuntimeType = null;
        private static RuntimeType m_VoidRuntimeType = null;
        private static RuntimeType m_BoolRuntimeType = null;
        private static RuntimeType m_NumRuntimeType = null;
        private static RuntimeType m_ByteRuntimeType = null;
        private static RuntimeType m_SByteRuntimeType = null;
        private static RuntimeType m_Int16RuntimeType = null;
        private static RuntimeType m_UInt16RuntimeType = null;
        private static RuntimeType m_Int32RuntimeType = null;
        private static RuntimeType m_UInt32RuntimeType = null;
        private static RuntimeType m_Int64RuntimeType = null;
        private static RuntimeType m_UInt64RuntimeType = null;
        private static RuntimeType m_Float32RuntimeType = null;
        private static RuntimeType m_Float64RuntimeType = null;
        private static RuntimeType m_StringRuntimeType = null;

        public static ClassObject CreateTypeObject(RuntimeType rt)
        {
            if (rt == null) return null;
            // Use the ObjectClass.GetObjectType to obtain/cached TypeObject
            try
            {
                // create a temporary object instance for this runtime type (no member init)
                SObject obj = ObjectManager.CreateObjectByRuntimeType(rt, false);
                if (obj == null) return null;
                // ObjectClass moved under SimpleLanguage.Lib; use that implementation
                var typeObj = SimpleLanguage.Lib.ObjectClass.GetObjectType(obj);
                return typeObj as ClassObject;
            }
            catch
            {
                return null;
            }
        }

        public static RuntimeType GetRuntimeTypeByMT(RuntimeClass rmc)
        {
            if (rmc == null) return null;
            return s_RuntimeTypeList.Find(r => r.runtimeClass != null && r.runtimeClass.id == rmc.id);
        }
        public static RuntimeType GetRuntimeTypeByClassId( int id )
        {
            return s_RuntimeTypeList.Find(r => r.runtimeClass != null && r.runtimeClass.id == id );
        }
        public static RuntimeType GetRuntimeTypeByMIRMetaType(RuntimeDefType irmt)
        {
            if (irmt == null) return null;
            return GetRuntimeTypeByMT(irmt.runtimeClass);
        }
        public static RuntimeType GetRuntimeTypeByMTAndTemplateMT( RuntimeClass rmc, List<RuntimeType> inputTemplateTypeList)
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
        public static RuntimeType GetRuntimeTypeByMTAndIRMetaClass(RuntimeClass rmc)
        {
            return GetRuntimeTypeByMT(rmc);
        }
        public static RuntimeType AddRuntimeTypeByClassAndTemplate(RuntimeClass rmc, List<RuntimeType> inputTemplateTypeList)
        {
            if (rmc == null) return null;
            RuntimeType rt = new RuntimeType(rmc, inputTemplateTypeList);
            s_RuntimeTypeList.Add(rt);
            return rt;
        }
        public static RuntimeType AddRuntimeTypeByClass(RuntimeClass rmc )
        {
            RuntimeType rt = new RuntimeType(rmc, null);

            string name = rmc.name;
            if (name == "Void")
            {
                m_VoidRuntimeType = rt;
            }
            else if(name == "Type")
            {
                m_TypeRuntimeType = rt;
            }
            else if( name == "Bool")
            {
                m_BoolRuntimeType = rt;
            }
            else if (name == "Num")
            {
                m_NumRuntimeType = rt;
            }
            else if (name == "Byte")
            {
                m_ByteRuntimeType = rt;
            }
            else if (name == "SByte")
            {
                m_SByteRuntimeType = rt;
            }
            else if (name == "Int16")
            {
                m_Int16RuntimeType = rt;
            }
            else if (name == "UInt16")
            {
                m_UInt16RuntimeType = rt;
            }
            else if (name == "Int32")
            {
                m_Int32RuntimeType = rt;
            }
            else if (name == "UInt32")
            {
                m_UInt32RuntimeType = rt;
            }
            else if (name == "Int64")
            {
                m_Int64RuntimeType = rt;
            }
            else if (name == "UInt64")
            {
                m_UInt64RuntimeType = rt;
            }
            else if (name == "String")
            {
                m_StringRuntimeType = rt;
            }
            else if (name == "Float32")
            {
                m_Float32RuntimeType = rt;
            }
            else if (name == "Float64")
            {
                m_Float64RuntimeType = rt;
            }
            s_RuntimeTypeList.Add(rt);

            return rt;
        }
    }
}
