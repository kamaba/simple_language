//****************************************************************************
//  File:      ClassObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/28 12:00:00
//  Description: 
//****************************************************************************

using System.Collections.Generic;
using System.Text;
using SimpleLanguage.Core;
using SimpleLanguage.IR;
using SimpleLanguage.Parse;

namespace SimpleLanguage.VM
{
    public class MemberVariableData
    {
        public int index { get; set; } = 0;
        public int start { get; set; } = 0;
        public int length { get; set; } = 0;
    }
    public class ClassObject : SObject
    {
        public ClassObject value => m_Object;
        public IRMetaClass irMetaClass=> m_RuntimeType?.irClass;

        private ClassObject m_Object = null;
        private byte[] m_Data = null;   /*  m_Data  结构  bit形，只有运算时要用 1-> byte 2->sbyte   3-> int16  4-> uint16    */
        private short[] m_Type = null;
        private SObject[] m_MemberObjectArray = null;
        protected List<IRMetaVariable> m_IRMetaVariableList = null;
        protected List<RuntimeType> m_IRTemplateList = new List<RuntimeType>();


        public ClassObject( RuntimeType irmt, bool isStatic = false )
        {
            m_RuntimeType = irmt;

            int byteCount = m_RuntimeType.irClass.byteCount;
            m_Data = new byte[byteCount];
            typeId = (short)m_RuntimeType.irClass.id;
            m_IRTemplateList = irmt.runtimeTemplateList;

            m_IRMetaVariableList = isStatic ? m_RuntimeType.irClass.staticIRMetaVariableList : m_RuntimeType.irClass.localIRMetaVariableList;
            m_MemberObjectArray = new SObject[m_IRMetaVariableList.Count];
            m_Type = new short[m_IRMetaVariableList.Count];
            m_Object = this;
        }
        public RuntimeType GetClassRuntimeType(IRMetaType irmt, bool isAdd = false)
        {
            if (irmt.templateIndex != -1)
            {
                return m_IRTemplateList[irmt.templateIndex];
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
        public void CreateObject()
        {
            for (int i = 0; i < m_IRMetaVariableList.Count; i++)
            {
                var irmv = m_IRMetaVariableList[i].irMetaType;
                var rt = GetClassRuntimeType(irmv, true );
                SObject sobj = ObjectManager.CreateObjectByRuntimeType(rt );
                if(sobj == null )
                {
                    continue;
                }
                m_Type[i] = sobj.typeId;
                m_MemberObjectArray[i] = sobj;
            }
        }
        public SObject GetMemberVariable(int index)
        {
            if (index > m_MemberObjectArray.Length)
            {
                Log.AddVM(EError.None, "执行的参数超出范围!!");
                return null;
            }
            return m_MemberObjectArray[index];
        }
        public void SetValue(ClassObject val )
        {
            m_Object = val.m_Object;
            val.refCount++;
        }
        public void GetMemberVariableSValue( int index, ref SValue svalue )
        {
            if (index < 0 )
            {
                Log.AddVM(EError.None, "执行的参数超出范围!! < 0 ");
                return;
            }
            if (index > m_MemberObjectArray.Length)
            {
                Log.AddVM(EError.None, "执行的参数超出范围!!");
                return;
            }
            var mmv = m_MemberObjectArray[index];
            switch (mmv)
            {
                case ByteObject byteob:
                    {
                        svalue.SetInt8Value(byteob.value);
                    }
                    break;
                case SByteObject sbyteobj:
                    {
                        svalue.SetSInt8Value(sbyteobj.value);
                    }
                    break;
                case Int16Object int16Obj:
                    {
                        svalue.SetInt16Value(int16Obj.value);
                    }
                    break;
                case UInt16Object uint16Obj:
                    {
                        svalue.SetUInt16Value(uint16Obj.value);
                    }
                    break;
                case Int32Object int32Obj:
                    {
                        svalue.SetInt32Value(int32Obj.value);
                    }
                    break;
                case UInt32Object uint32Obj:
                    {
                        svalue.SetUInt32Value(uint32Obj.value);
                    }
                    break;
                case Int64Object int64Obj:
                    {
                        svalue.SetInt64Value(int64Obj.value);
                    }
                    break;
                case UInt64Object uint64Obj:
                    {
                        svalue.SetUInt64Value(uint64Obj.value);
                    }
                    break;
                case FloatObject floatobj:
                    {
                        svalue.SetFloatValue(floatobj.value);
                    }
                    break;
                case DoubleObject doubleobj:
                    {
                        svalue.SetDoubleValue(doubleobj.value);
                    }
                    break;
                case StringObject stringObj:
                    {
                        svalue.SetStringValue( stringObj.value );
                    }
                    break;
                case ClassObject classObj:
                    {
                        svalue.SetSObject(classObj);
                    }
                    break;
                case TemplateObject templateObj:
                    {

                    }
                    break;
            }
        }
        public void SetMemberVariableSValue( int index, SValue svalue)
        {
            if (index > m_MemberObjectArray.Length)
            {
                Log.AddVM(EError.None, "执行的参数超出范围!!");
                return;
            }
            switch (svalue.eType)
            {
                case EType.Null:
                    {
                        m_MemberObjectArray[index].SetNull();
                        /*
                        ClassObject classObj = m_MemberObjectArray[index] as ClassObject;
                        if (classObj == null)
                        {
                            Log.AddVM(EError.None, "Null 该类型不是Int32类型!!");
                            return;
                        }
                        classObj.SetNull();
                        */
                    }
                    break;
                case EType.Boolean:
                    {

                    }
                    break;
                case EType.Byte:
                    {
                        ByteObject byteObj = m_MemberObjectArray[index] as ByteObject;
                        if (byteObj == null)
                        {
                            Log.AddVM(EError.None, "Byte 该类型不是Int32类型!!");
                            return;
                        }
                        byteObj.SetValue(svalue.int8Value);
                    }
                    break;
                case EType.SByte:
                    {
                        SByteObject sbyteObj = m_MemberObjectArray[index] as SByteObject;
                        if (sbyteObj == null)
                        {
                            Log.AddVM(EError.None, "Sbyte 该类型不是Int32类型!!");
                            return;
                        }
                        sbyteObj.SetValue(svalue.sint8Value);
                    }
                    break;
                case EType.Int16:
                    {
                        Int16Object int32Obj = m_MemberObjectArray[index] as Int16Object;
                        if (int32Obj == null)
                        {
                            Log.AddVM(EError.None, "Int16 该类型不是Int32类型!!");
                            return;
                        }
                        int32Obj.SetValue(svalue.int16Value);
                    }
                    break;
                case EType.UInt16:
                    {
                        UInt16Object uint16Obj = m_MemberObjectArray[index] as UInt16Object;
                        if (uint16Obj == null)
                        {
                            Log.AddVM(EError.None, "UInt16 该类型不是Int16类型!!");
                            return;
                        }
                        uint16Obj.SetValue(svalue.uint16Value);
                    }
                    break;
                case EType.Int32:
                    {
                        Int32Object int32Obj = null;
                        if (m_MemberObjectArray[index] == null )
                        {
                            int32Obj = new Int32Object(0);
                            m_MemberObjectArray[index] = int32Obj;
                        }
                        int32Obj = m_MemberObjectArray[index] as Int32Object;
                        if (int32Obj == null)
                        {
                            Log.AddVM(EError.None, "Int32 该类型不是Int32类型!!");
                            return;
                        }
                        int32Obj.SetValue(svalue.int32Value);
                    }
                    break;
                case EType.UInt32:
                    {
                        UInt32Object uint32Obj = m_MemberObjectArray[index] as UInt32Object;
                        if (uint32Obj == null)
                        {
                            Log.AddVM(EError.None, "UInt32 该类型不是UInt32类型!!");
                            return;
                        }
                        uint32Obj.SetValue(svalue.uint32Value);
                    }
                    break;
                case EType.Int64:
                    {
                        Int64Object int64Obj = m_MemberObjectArray[index] as Int64Object;
                        if (int64Obj == null)
                        {
                            Log.AddVM(EError.None, "Int64 该类型不是Int32类型!!");
                            return;
                        }
                        int64Obj.SetValue(svalue.int64Value);
                    }
                    break;
                case EType.UInt64:
                    {
                        UInt64Object uint64Obj = m_MemberObjectArray[index] as UInt64Object;
                        if (uint64Obj == null)
                        {
                            Log.AddVM(EError.None, "UInt64 该类型不是Int64类型!!");
                            return;
                        }
                        uint64Obj.SetValue(svalue.uint64Value);
                    }
                    break;
                case EType.Float32:
                    {
                        FloatObject floatObj = m_MemberObjectArray[index] as FloatObject;
                        if (floatObj == null)
                        {
                            Log.AddVM(EError.None, "Float 该类型不是float类型!!");
                            return;
                        }
                        floatObj.SetValue(svalue.floatValue);
                    }
                    break;
                case EType.Float64:
                    {
                        DoubleObject doubleObj = m_MemberObjectArray[index] as DoubleObject;
                        if (doubleObj == null)
                        {
                            Log.AddVM(EError.None, "Double 该类型不是Double类型!!");
                            return;
                        }
                        doubleObj.SetValue(svalue.doubleValue);
                    }
                    break;
                case EType.String:
                    {
                        StringObject stringObj = m_MemberObjectArray[index] as StringObject;
                        if (stringObj == null)
                        {
                            Log.AddVM(EError.None, "String 该类型不是Int32类型!!");
                            return;
                        }
                        stringObj.SetValue(svalue.stringValue);
                    }
                    break;
                case EType.Class:
                    {
                        var mva = m_MemberObjectArray[index];
                        if (mva.eType == EType.Byte)
                        {

                            ByteObject byteObj = mva as ByteObject;
                            if (byteObj == null)
                            {
                                Log.AddVM(EError.None, "Class ByteObject 该类型不是Int32类型!!");
                                return;
                            }
                            byteObj.SetValue(svalue.int8Value);
                        }
                        else if (mva.eType == EType.SByte)
                        {

                            SByteObject sbyteObj = mva as SByteObject;
                            if (sbyteObj == null)
                            {
                                Log.AddVM(EError.None, "Class SByteObject 该类型不是Int32类型!!");
                                return;
                            }
                            sbyteObj.SetValue(svalue.sint8Value);
                        }
                        else if (mva.eType == EType.Int16)
                        {

                            Int16Object int16Obj = mva as Int16Object;
                            if (int16Obj == null)
                            {
                                Log.AddVM(EError.None, "Class Int16Object 该类型不是Int16类型!!");
                                return;
                            }
                            int16Obj.SetValue(svalue.int16Value);
                        }
                        else if (mva.eType == EType.UInt16)
                        {

                            UInt32Object uint32Obj = mva as UInt32Object;
                            if (uint32Obj == null)
                            {
                                Log.AddVM(EError.None, "Class UInt32Object 该类型不是UInt32类型!!");
                                return;
                            }
                            uint32Obj.SetValue(svalue.uint32Value);
                        }
                        else if (mva.eType == EType.Int32)
                        {
                            Int32Object int32Obj = mva as Int32Object;
                            if (int32Obj == null)
                            {
                                Log.AddVM(EError.None, "Class Int32Object 该类型不是Int32类型!!");
                                return;
                            }
                            int32Obj.SetValue(svalue.int32Value);
                        }
                        else if (mva.eType == EType.UInt32)
                        {

                            UInt32Object uint32Obj = mva as UInt32Object;
                            if (uint32Obj == null)
                            {
                                Log.AddVM(EError.None, "Class UInt32Object 该类型不是Int32类型!!");
                                return;
                            }
                            uint32Obj.SetValue(svalue.uint32Value);
                        }
                        else if (mva.eType == EType.Int64)
                        {

                            Int64Object int64Obj = mva as Int64Object;
                            if (int64Obj == null)
                            {
                                Log.AddVM(EError.None, "该类型不是Int64类型!!");
                                return;
                            }
                            int64Obj.SetValue(svalue.int64Value);
                        }
                        else if (mva.eType == EType.UInt64)
                        {

                            UInt64Object uint64Obj = mva as UInt64Object;
                            if (uint64Obj == null)
                            {
                                Log.AddVM(EError.None, "该类型不是Int64类型!!");
                                return;
                            }
                            uint64Obj.SetValue(svalue.uint64Value);
                        }
                        else if (mva.eType == EType.String)
                        {

                            StringObject stringObj = mva as StringObject;
                            if (stringObj == null)
                            {
                                Log.AddVM(EError.None, "该类型不是stringObj类型!!");
                                return;
                            }
                            stringObj.SetValue(svalue.stringValue);
                        }
                        else
                        {
                            ClassObject classObj = m_MemberObjectArray[index] as ClassObject;
                            if (classObj == null)
                            {
                                Log.AddVM(EError.None, "该类型不是classObj类型!!");
                                return;
                            }
                            //classObj.SetValue(svalue.sobject as ClassObject);
                            m_MemberObjectArray[index] = svalue.sobject as ClassObject;
                        }
                    }
                    break;
            }
        }
        public override string ToFormatString()
        {
            StringBuilder sb = new StringBuilder();

            if (m_Object != null )
            {
                sb.Append(m_Object.ToFormatString());
            }
            sb.Append(m_RuntimeType.irClass.ToString());
            //for( int i = 0; i < m_MemberVariableArray)

            return sb.ToString();
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(m_RuntimeType.ToString());

            return sb.ToString();
         }
    }
}
