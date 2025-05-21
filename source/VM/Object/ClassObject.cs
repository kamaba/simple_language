//****************************************************************************
//  File:      ClassObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/28 12:00:00
//  Description: 
//****************************************************************************

using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using SimpleLanguage.IR;

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

        private ClassObject m_Object = null;
        private byte[] m_Data = null;   /*  m_Data  结构  bit形，只有运算时要用 1-> byte 2->sbyte   3-> int16  4-> uint16    */
        private short[] m_Type = null;
        private SObject[] m_MemberVariableObjectArray = null;

        private IRMetaClass m_IRMetaClass;


        public ClassObject( IRMetaClass irmc )
        {
            m_IRMetaClass = irmc;

            int byteCount = irmc.byteCount;
            m_Data = new byte[byteCount];
            typeId = irmc.id;

            var mvdict = irmc.localIRMetaVariableList;
            m_MemberVariableObjectArray = new SObject[mvdict.Count];
            m_Type = new short[mvdict.Count];
            for ( int i = 0; i < mvdict.Count; i++ )
            {
                var obj = ObjectManager.CreateObjectByDefineType(mvdict[i].irMetaClass);
                m_Type[i] = obj.typeId;
                m_MemberVariableObjectArray[i] = obj;
            }
        }
        public SObject GetMemberVariable(int index)
        {
            if (index > m_MemberVariableObjectArray.Length)
            {
                Debug.Write("执行的参数超出范围!!");
                return null;
            }
            return m_MemberVariableObjectArray[index];
        }
        public void SetValue(ClassObject val )
        {
            m_Object = val;
            val.refCount++;
        }
        public void GetMemberVariableSValue( int index, ref SValue svalue )
        {
            if (index > m_MemberVariableObjectArray.Length)
            {
                Debug.Write("执行的参数超出范围!!");
                return;
            }
            var mmv = m_MemberVariableObjectArray[index];
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
            }
        }
        public void SetMemberVariableSValue( int index, SValue svalue)
        {
            if (index > m_MemberVariableObjectArray.Length)
            {
                Debug.Write("执行的参数超出范围!!");
                return;
            }
            switch (svalue.eType)
            {
                case EType.Null:
                    {
                        ClassObject classObj = m_MemberVariableObjectArray[index] as ClassObject;
                        if (classObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        classObj.SetNull();
                    }
                    break;
                case EType.Byte:
                    {
                        ByteObject byteObj = m_MemberVariableObjectArray[index] as ByteObject;
                        if (byteObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        byteObj.SetValue(svalue.int8Value);
                    }
                    break;
                case EType.SByte:
                    {
                        SByteObject sbyteObj = m_MemberVariableObjectArray[index] as SByteObject;
                        if (sbyteObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        sbyteObj.SetValue(svalue.sint8Value);
                    }
                    break;
                case EType.Int16:
                    {
                        Int16Object int32Obj = m_MemberVariableObjectArray[index] as Int16Object;
                        if (int32Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        int32Obj.SetValue(svalue.int16Value);
                    }
                    break;
                case EType.UInt16:
                    {
                        UInt16Object uint16Obj = m_MemberVariableObjectArray[index] as UInt16Object;
                        if (uint16Obj == null)
                        {
                            Debug.Write("该类型不是Int16类型!!");
                            return;
                        }
                        uint16Obj.SetValue(svalue.uint16Value);
                    }
                    break;
                case EType.Int32:
                    {
                        Int32Object int32Obj = m_MemberVariableObjectArray[index] as Int32Object;
                        if (int32Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        int32Obj.SetValue(svalue.int32Value);
                    }
                    break;
                case EType.UInt32:
                    {
                        UInt32Object uint32Obj = m_MemberVariableObjectArray[index] as UInt32Object;
                        if (uint32Obj == null)
                        {
                            Debug.Write("该类型不是UInt32类型!!");
                            return;
                        }
                        uint32Obj.SetValue(svalue.uint32Value);
                    }
                    break;
                case EType.Int64:
                    {
                        Int64Object int64Obj = m_MemberVariableObjectArray[index] as Int64Object;
                        if (int64Obj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        int64Obj.SetValue(svalue.int64Value);
                    }
                    break;
                case EType.UInt64:
                    {
                        UInt64Object uint64Obj = m_MemberVariableObjectArray[index] as UInt64Object;
                        if (uint64Obj == null)
                        {
                            Debug.Write("该类型不是Int64类型!!");
                            return;
                        }
                        uint64Obj.SetValue(svalue.uint64Value);
                    }
                    break;
                case EType.Float:
                    {
                        FloatObject floatObj = m_MemberVariableObjectArray[index] as FloatObject;
                        if (floatObj == null)
                        {
                            Debug.Write("该类型不是float类型!!");
                            return;
                        }
                        floatObj.SetValue(svalue.floatValue);
                    }
                    break;
                case EType.Double:
                    {
                        DoubleObject doubleObj = m_MemberVariableObjectArray[index] as DoubleObject;
                        if (doubleObj == null)
                        {
                            Debug.Write("该类型不是Double类型!!");
                            return;
                        }
                        doubleObj.SetValue(svalue.doubleValue);
                    }
                    break;
                case EType.String:
                    {
                        StringObject stringObj = m_MemberVariableObjectArray[index] as StringObject;
                        if (stringObj == null)
                        {
                            Debug.Write("该类型不是Int32类型!!");
                            return;
                        }
                        stringObj.SetValue(svalue.stringValue);
                    }
                    break;
                case EType.Class:
                    {
                        var mva = m_MemberVariableObjectArray[index];
                        if (mva.eType == EType.Byte)
                        {

                            ByteObject byteObj = mva as ByteObject;
                            if (byteObj == null)
                            {
                                Debug.Write("该类型不是Int32类型!!");
                                return;
                            }
                            byteObj.SetValue(svalue.int8Value);
                        }
                        else if (mva.eType == EType.SByte)
                        {

                            SByteObject sbyteObj = mva as SByteObject;
                            if (sbyteObj == null)
                            {
                                Debug.Write("该类型不是Int32类型!!");
                                return;
                            }
                            sbyteObj.SetValue(svalue.sint8Value);
                        }
                        else if (mva.eType == EType.Int16)
                        {

                            Int16Object int16Obj = mva as Int16Object;
                            if (int16Obj == null)
                            {
                                Debug.Write("该类型不是Int16类型!!");
                                return;
                            }
                            int16Obj.SetValue(svalue.int16Value);
                        }
                        else if (mva.eType == EType.UInt16)
                        {

                            UInt32Object uint32Obj = mva as UInt32Object;
                            if (uint32Obj == null)
                            {
                                Debug.Write("该类型不是UInt32类型!!");
                                return;
                            }
                            uint32Obj.SetValue(svalue.uint32Value);
                        }
                        else if (mva.eType == EType.Int32)
                        {
                            Int32Object int32Obj = mva as Int32Object;
                            if (int32Obj == null)
                            {
                                Debug.Write("该类型不是Int32类型!!");
                                return;
                            }
                            int32Obj.SetValue(svalue.int32Value);
                        }
                        else if (mva.eType == EType.UInt32)
                        {

                            UInt32Object uint32Obj = mva as UInt32Object;
                            if (uint32Obj == null)
                            {
                                Debug.Write("该类型不是Int32类型!!");
                                return;
                            }
                            uint32Obj.SetValue(svalue.uint32Value);
                        }
                        else if (mva.eType == EType.Int64)
                        {

                            Int64Object int64Obj = mva as Int64Object;
                            if (int64Obj == null)
                            {
                                Debug.Write("该类型不是Int64类型!!");
                                return;
                            }
                            int64Obj.SetValue(svalue.int64Value);
                        }
                        else if (mva.eType == EType.UInt64)
                        {

                            UInt64Object uint64Obj = mva as UInt64Object;
                            if (uint64Obj == null)
                            {
                                Debug.Write("该类型不是Int64类型!!");
                                return;
                            }
                            uint64Obj.SetValue(svalue.uint64Value);
                        }
                        else if (mva.eType == EType.String)
                        {

                            StringObject stringObj = mva as StringObject;
                            if (stringObj == null)
                            {
                                Debug.Write("该类型不是stringObj类型!!");
                                return;
                            }
                            stringObj.SetValue(svalue.stringValue);
                        }
                        else
                        {
                            ClassObject classObj = m_MemberVariableObjectArray[index] as ClassObject;
                            if (classObj == null)
                            {
                                Debug.Write("该类型不是Int32类型!!");
                                return;
                            }
                            classObj.SetValue(svalue.sobject as ClassObject);
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
            sb.Append(m_IRMetaClass.ToString());
            //for( int i = 0; i < m_MemberVariableArray)

            return sb.ToString();
        }
        public override string ToString()
        {
            return m_IRMetaClass.allName + "  " ;
         }
    }
}
