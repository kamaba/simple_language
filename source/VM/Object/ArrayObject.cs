//****************************************************************************
//  File:      ArrayObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.IR;
using SimpleLanguage.Parse;
using System;
namespace SimpleLanguage.VM
{
    public class ArrayObject : SObject
    {
        private int m_Length = 0;
        private Array m_Array = null;
        private EArrayType eArrayType = EArrayType.Byte;
        public ArrayObject( EArrayType eArrType, int length )
        {
            m_Etype = EType.Array;
            eArrayType = eArrType;
            m_Length = length;
            CreateArray();
        }
        public void SetArray( ArrayObject ao )
        {
            eArrayType = ao.eArrayType;
            m_Length = ao.m_Length;
            m_Array = ao.m_Array;
        }
        void CreateArray()
        {
            int length = m_Length;
            switch (eArrayType)
            {
                case EArrayType.Byte:
                    {
                        m_Array = new Byte[length];
                    }
                    break;
                case EArrayType.SByte:
                    {
                        m_Array = new SByte[length];
                    }
                    break;
                case EArrayType.Int16:
                    {
                        m_Array = new Int16[length];
                    }
                    break;
                case EArrayType.UInt16:
                    {
                        m_Array = new UInt16[length];
                    }
                    break;
                case EArrayType.Int32:
                    {
                        m_Array = new Int32[length];
                    }
                    break;
                case EArrayType.UInt32:
                    {
                        m_Array = new UInt32[length];
                    }
                    break;
                case EArrayType.Int64:
                    {
                        m_Array = new Int64[length];
                    }
                    break;
                case EArrayType.UInt64:
                    {
                        m_Array = new UInt64[length];
                    }
                    break;
                case EArrayType.String:
                    {
                        m_Array = new String[length];
                    }
                    break;
                case EArrayType.Pointer:
                    {
                        m_Array = new IntPtr[length];
                    }
                    break;
            }
        }
        public void LoadValue( int index, ref SValue sval )
        {
            if (index < 0)
            {
                Log.AddVM(EError.None, "执行的参数超出范围!! < 0 ");
                return;
            }
            if (index >= m_Length )
            {
                Log.AddVM(EError.None, "执行的参数超出范围!!");
                return;
            }

            object obj = m_Array.GetValue(index);
            if ( obj != null )
            {
                switch (eArrayType)
                {
                    case EArrayType.Byte:
                        {
                            Byte val = (Byte)obj;
                            sval.SetInt8Value(val);
                        }
                        break;
                    case EArrayType.SByte:
                        {
                            SByte val = (SByte)obj;
                            sval.SetSInt8Value(val);
                        }
                        break;
                    case EArrayType.Int16:
                        {
                            Int16 val = (Int16)obj;
                            sval.SetInt16Value(val);
                        }
                        break;
                    case EArrayType.UInt16:
                        {
                            UInt16 val = (UInt16)obj;
                            sval.SetUInt16Value(val);
                        }
                        break;
                    case EArrayType.Int32:
                        {
                            Int32 val = (Int32)obj;
                            sval.SetInt32Value(val);
                        }
                        break;
                    case EArrayType.UInt32:
                        {
                            UInt32 val = (UInt32)obj;
                            sval.SetUInt32Value(val);
                        }
                        break;
                    case EArrayType.Int64:
                        {
                            Int64 val = (Int64)obj;
                            sval.SetInt64Value(val);
                        }
                        break;
                    case EArrayType.UInt64:
                        {
                            UInt64 val = (UInt64)obj;
                            sval.SetUInt64Value(val);
                        }
                        break;
                    case EArrayType.String:
                        {
                            var str = obj as String;
                            sval.SetStringValue(str);
                        }
                        break;
                    case EArrayType.Pointer:
                        {
                            sval.SetSObject(obj as ClassObject);
                        }
                        break;
                }
            }
        }

        public object GetValue( int index )
        {
            if (index < 0)
            {
                Log.AddVM(EError.None, "执行的参数超出范围!! < 0 ");
                return null;
            }
            if (index > m_Length)
            {
                Log.AddVM(EError.None, "执行的参数超出范围!!");
                return null;
            }

            object obj = m_Array.GetValue(index);

            return obj;
        }
        public void StoreValue(int index, SValue svalue)
        {
            AnyObject anyobj = m_Array.GetValue(index) as AnyObject;
            switch (svalue.eType)
            {
                case EType.Null:
                    {
                        m_Array.SetValue(null, index);
                    }
                    break;
                case EType.Boolean:
                    {
                        m_Array.SetValue(svalue.int8Value == 1 ? true : false, index);
                    }
                    break;
                case EType.Byte:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValue(EType.Byte, svalue.int8Value);
                            return;
                        }
                        //ByteObject bo = svalue.int8Value

                        //ByteObject byteObj = m_MemberObjectArray[index] as ByteObject;
                        //if (byteObj == null)
                        //{
                        //    Log.AddVM(EError.None, "Byte 该类型不是Int32类型!!");
                        //    return;
                        //}
                        //byteObj.SetValue(svalue.int8Value);
                        m_Array.SetValue(svalue.int8Value, index);
                    }
                    break;
                case EType.SByte:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValue(EType.SByte, svalue.sint8Value);
                            return;
                        }
                        m_Array.SetValue(svalue.sint8Value, index);
                    }
                    break;
                case EType.Int16:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValue(EType.Int16, svalue.int16Value);
                            return;
                        }

                        m_Array.SetValue(svalue.int16Value, index);
                    }
                    break;
                case EType.UInt16:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValue(EType.UInt16, svalue.uint16Value);
                            return;
                        }
                        m_Array.SetValue(svalue.uint16Value, index);
                    }
                    break;
                case EType.Int32:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValue(EType.Int32, svalue.int32Value);
                            return;
                        }
                        m_Array.SetValue(svalue.int32Value, index);
                    }
                    break;
                case EType.UInt32:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValue(EType.UInt32, svalue.uint32Value);
                            return;
                        }
                        m_Array.SetValue(svalue.uint32Value, index);
                    }
                    break;
                case EType.Int64:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValue(EType.Int64, svalue.int64Value);
                            return;
                        }
                        m_Array.SetValue(svalue.int64Value, index);
                    }
                    break;
                case EType.UInt64:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValue(EType.UInt64, svalue.uint64Value);
                            return;
                        }
                        m_Array.SetValue(svalue.uint64Value, index);
                    }
                    break;
                case EType.Float32:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValue(EType.Float32, svalue.floatValue);
                            return;
                        }
                        m_Array.SetValue(svalue.floatValue, index);
                    }
                    break;
                case EType.Float64:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValue(EType.Float64, svalue.doubleValue);
                            return;
                        }
                        m_Array.SetValue(svalue.doubleValue, index);
                    }
                    break;
                case EType.String:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValue(EType.String, svalue.stringValue);
                            return;
                        }
                        m_Array.SetValue(svalue.stringValue, index);
                    }
                    break;
                case EType.Class:
                    {
                        m_Array.SetValue(svalue.sobject, index);
                        //var mva = m_MemberObjectArray[index];
                        //if (mva.eType == EType.Byte)
                        //{

                        //    ByteObject byteObj = mva as ByteObject;
                        //    if (byteObj == null)
                        //    {
                        //        Log.AddVM(EError.None, "Class ByteObject 该类型不是Int32类型!!");
                        //        return;
                        //    }
                        //    byteObj.SetValue(svalue.int8Value);
                        //}
                        //else if (mva.eType == EType.SByte)
                        //{

                        //    SByteObject sbyteObj = mva as SByteObject;
                        //    if (sbyteObj == null)
                        //    {
                        //        Log.AddVM(EError.None, "Class SByteObject 该类型不是Int32类型!!");
                        //        return;
                        //    }
                        //    sbyteObj.SetValue(svalue.sint8Value);
                        //}
                        //else if (mva.eType == EType.Int16)
                        //{

                        //    Int16Object int16Obj = mva as Int16Object;
                        //    if (int16Obj == null)
                        //    {
                        //        Log.AddVM(EError.None, "Class Int16Object 该类型不是Int16类型!!");
                        //        return;
                        //    }
                        //    int16Obj.SetValue(svalue.int16Value);
                        //}
                        //else if (mva.eType == EType.UInt16)
                        //{

                        //    UInt32Object uint32Obj = mva as UInt32Object;
                        //    if (uint32Obj == null)
                        //    {
                        //        Log.AddVM(EError.None, "Class UInt32Object 该类型不是UInt32类型!!");
                        //        return;
                        //    }
                        //    uint32Obj.SetValue(svalue.uint32Value);
                        //}
                        //else if (mva.eType == EType.Int32)
                        //{
                        //    Int32Object int32Obj = mva as Int32Object;
                        //    if (int32Obj == null)
                        //    {
                        //        Log.AddVM(EError.None, "Class Int32Object 该类型不是Int32类型!!");
                        //        return;
                        //    }
                        //    int32Obj.SetValue(svalue.int32Value);
                        //}
                        //else if (mva.eType == EType.UInt32)
                        //{

                        //    UInt32Object uint32Obj = mva as UInt32Object;
                        //    if (uint32Obj == null)
                        //    {
                        //        Log.AddVM(EError.None, "Class UInt32Object 该类型不是Int32类型!!");
                        //        return;
                        //    }
                        //    uint32Obj.SetValue(svalue.uint32Value);
                        //}
                        //else if (mva.eType == EType.Int64)
                        //{

                        //    Int64Object int64Obj = mva as Int64Object;
                        //    if (int64Obj == null)
                        //    {
                        //        Log.AddVM(EError.None, "该类型不是Int64类型!!");
                        //        return;
                        //    }
                        //    int64Obj.SetValue(svalue.int64Value);
                        //}
                        //else if (mva.eType == EType.UInt64)
                        //{

                        //    UInt64Object uint64Obj = mva as UInt64Object;
                        //    if (uint64Obj == null)
                        //    {
                        //        Log.AddVM(EError.None, "该类型不是Int64类型!!");
                        //        return;
                        //    }
                        //    uint64Obj.SetValue(svalue.uint64Value);
                        //}
                        //else if (mva.eType == EType.String)
                        //{

                        //    StringObject stringObj = mva as StringObject;
                        //    if (stringObj == null)
                        //    {
                        //        Log.AddVM(EError.None, "该类型不是stringObj类型!!");
                        //        return;
                        //    }
                        //    stringObj.SetValue(svalue.stringValue);
                        //}
                        //else
                        //{
                        //    ClassObject classObj = m_MemberObjectArray[index] as ClassObject;
                        //    if (classObj == null)
                        //    {
                        //        AnyObject anyObj = m_MemberObjectArray[index] as AnyObject;
                        //        if (anyObj != null)
                        //        {
                        //            anyObj.SetValue(EType.Class, svalue.sobject);
                        //            return;
                        //        }
                        //        Log.AddVM(EError.None, "该类型不是classObj类型!!");
                        //        return;
                        //    }
                        //    //classObj.SetValue(svalue.sobject as ClassObject);
                        //    m_MemberObjectArray[index] = svalue.sobject as ClassObject;
                        //}
                    }
                    break;
            }
        }
        public void StoreObject(int index, object svalue)
        {
            switch (eArrayType)
            {
                case  EArrayType.Byte:
                    {
                        m_Array.SetValue( (byte)svalue == 1 ? true : false, index);
                    }
                    break;
                case EArrayType.Int32:
                    {
                        m_Array.SetValue( (int)svalue, index);
                    }
                    break;
                case EArrayType.String:
                    {
                        m_Array.SetValue( svalue.ToString(), index);
                    }
                    break;
                case EArrayType.Pointer:
                    {
                        m_Array.SetValue(svalue, index);
                    }
                    break;
            }
        }
        public override string ToFormatString()
        {
            return "";
        }
    }
}
