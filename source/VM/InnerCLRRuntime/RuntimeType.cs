//****************************************************************************
//  File:      IRMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.IR;
using SimpleLanguage.Logging;
using SimpleLanguage.VM.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace SimpleLanguage.VM
{
    public class RuntimeType {
        public IRMetaClass irClass;
        public List<RuntimeType> runtimeTemplateList = new List<RuntimeType>();
        private SObject[] m_StaticMemObjectList;
        public EVMType eType { get; set; }

        public RuntimeType(IRMetaClass rc, List<RuntimeType> rtList)
        {
            irClass = rc;
            if (rtList != null)
            {
                runtimeTemplateList = rtList;
            }

            m_StaticMemObjectList = new SObject[irClass.staticIRMetaVariableList.Count];
            for (int i = 0; i < irClass.staticIRMetaVariableList.Count; i++)
            {
                RuntimeType rt = GetClassRuntimeType(irClass.staticIRMetaVariableList[i].irMetaType, true);
                m_StaticMemObjectList[i] = ObjectManager.CreateObjectByRuntimeType(rt, true);
            }

            if (Enum.TryParse<EVMType>(irClass.irName, true, out var eoutType))
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

        public RuntimeType GetExtendsTemplateRuntimeType(IRMetaType irmt, List<RuntimeType> _runtimeTemplateList)
        {
            if (_runtimeTemplateList?.Count > 0)
            {
                return _runtimeTemplateList[irmt.templateIndex];
            }
            return null;
        }
        public RuntimeType GetClassRuntimeType(IRMetaType irmt, bool isAdd = false)
        {
            var irmc = this.irClass;
            if (irmt.templateIndex != -1)
            {
                if (irmt.irOwnerMetaClass == this.irClass)
                {
                    return runtimeTemplateList[irmt.templateIndex];
                }
                else
                {
                    var mt = irClass.GetIRMetaTypeByTemplateAndClassRelation(irmt.irOwnerMetaClass, irmt.templateIndex);

                    return GetClassRuntimeType(mt, isAdd);
                }
            }
            else
            {
                List<RuntimeType> rtList = new List<RuntimeType>();
                if (irmt.irMetaTypeList.Count > 0)
                {
                    for (int i = 0; i < irmt.irMetaTypeList.Count; i++)
                    {
                        var crt = GetClassRuntimeType(irmt.irMetaTypeList[i], isAdd);
                        rtList.Add(crt);
                    }
                }
                var rt = RuntimeTypeManager.GetRuntimeTypeByMTAndTemplateMT(irmt.irMetaClass, rtList);
                if (rt == null && isAdd)
                {
                    rt = RuntimeTypeManager.AddRuntimeTypeByClassAndTemplate(irmt.irMetaClass, rtList);
                }
                return rt;
            }
        }
        public void GetMemberVariableSValue(int index, ref SValue svalue)
        {
            if (m_StaticMemObjectList == null)
            {
                // initialize static array if possible
                if (irClass?.staticIRMetaVariableList == null)
                {
                    svalue.SetNull();
                    return;
                }
                m_StaticMemObjectList = new SObject[irClass.staticIRMetaVariableList.Count];
                for (int i = 0; i < m_StaticMemObjectList.Length; i++)
                {
                    var irmv = irClass.staticIRMetaVariableList[i];
                    var rt = GetClassRuntimeType(irmv.irMetaType, true);
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
                if (irClass?.staticIRMetaVariableList == null)
                {
                    return;
                }
                m_StaticMemObjectList = new SObject[irClass.staticIRMetaVariableList.Count];
                for (int i = 0; i < m_StaticMemObjectList.Length; i++)
                {
                    var irmv = irClass.staticIRMetaVariableList[i];
                    var rt = GetClassRuntimeType(irmv.irMetaType, true);
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
        public List<IRData> CreateStaticMetaMetaVariableIRList()
        {
            return new List<IRData>();
        }
        public static bool SameRuntimeType(RuntimeType rt1, RuntimeType rt2)
        {
            if (rt1 == null || rt2 == null) return false;
            return rt1.irClass.id == rt2.irClass.id;
        }
        public bool IsExtendsRelation(RuntimeType rt)
        {
            if (rt == null) return false;
            return irClass.IsExtendsRelation(rt.irClass);
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
            return irClass?.irName ?? base.ToString();
        }
    }

    public static class RuntimeTypeManager {
        private static List<RuntimeType> s_RuntimeList = new List<RuntimeType>();
        private static RuntimeType m_TypeRuntimeType = null;
        private static RuntimeType m_VoidRuntimeType = null;
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

        public static List<RuntimeType> runtimeList => s_RuntimeList;
        public static RuntimeType typeRuntimeType { get => m_TypeRuntimeType; }
        public static RuntimeType voidRuntimeType { get => m_VoidRuntimeType; }
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

        public static ClassObject CreateTypeObject(RuntimeType rt)
        {
            if (rt == null) return null;
            // Use the ObjectClass.GetObjectType to obtain/cached TypeObject
            try
            {
                // create a temporary object instance for this runtime type (no member init)
                SObject obj = ObjectManager.CreateObjectByRuntimeType(rt, false);
                if (obj == null) return null;
                var typeObj = SimpleLanguage.Lib.ObjectClass.GetObjectType(obj);
                return typeObj as ClassObject;
            }
            catch
            {
                return null;
            }
        }

        public static RuntimeType GetRuntimeTypeByMT(IRMetaClass rmc)
        {
            if (rmc == null) return null;
            return s_RuntimeList.Find(r => r.irClass != null && r.irClass.id == rmc.id);
        }
        public static RuntimeType GetRuntimeTypeByMIRMetaType(IRMetaType irmt)
        {
            if (irmt == null) return null;
            return GetRuntimeTypeByMT(irmt.m_IRMetaClass);
        }
        public static RuntimeType GetRuntimeTypeByMTAndTemplateMT(IRMetaClass rmc, List<RuntimeType> inputTemplateTypeList)
        {
            foreach (var v in s_RuntimeList)
            {
                if (v.irClass != rmc)
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
        public static RuntimeType GetRuntimeTypeByMTAndIRMetaClass(IRMetaClass rmc)
        {
            return GetRuntimeTypeByMT(rmc);
        }
        public static RuntimeType AddRuntimeTypeByClassAndTemplate(IRMetaClass rmc, List<RuntimeType> inputTemplateTypeList)
        {
            if (rmc == null) return null;
            var exist = GetRuntimeTypeByMT(rmc);
            if (exist != null) return exist;

            RuntimeType rt = new RuntimeType(rmc, inputTemplateTypeList);
            rt.irClass = rmc;
            s_RuntimeList.Add(rt);

            // init well-known static mappings based on irName
            string name = rmc.irName ?? "";
            if (name == "Type") m_TypeRuntimeType = rt;
            if (name == "Void") m_VoidRuntimeType = rt;
            if (name == "Num") m_NumRuntimeType = rt;
            if (name == "Byte") m_ByteRuntimeType = rt;
            if (name == "SByte") m_SByteRuntimeType = rt;
            if (name == "Int16") m_Int16RuntimeType = rt;
            if (name == "UInt16") m_UInt16RuntimeType = rt;
            if (name == "Int32") m_Int32RuntimeType = rt;
            if (name == "UInt32") m_UInt32RuntimeType = rt;
            if (name == "Int64") m_Int64RuntimeType = rt;
            if (name == "UInt64") m_UInt64RuntimeType = rt;
            if (name == "Float32") m_Float32RuntimeType = rt;
            if (name == "Float64") m_Float64RuntimeType = rt;
            if (name == "String") m_StringRuntimeType = rt;

            return rt;
        }
        public static RuntimeType AddRuntimeTypeByClass(IRMetaClass rmc )
        {
            return AddRuntimeTypeByClassAndTemplate(rmc, null);
        }
    }
}
