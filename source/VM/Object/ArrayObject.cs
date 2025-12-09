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
using System.Diagnostics;
namespace SimpleLanguage.VM
{
    public class ArrayObject : ClassObject
    {
        private Array m_Array = null;
        private EArrayType eArrayType = EArrayType.Byte;
        public ArrayObject( EArrayType eArrType, int length ) : base( RuntimeTypeManager.arrayRuntimeType, false )
        {
            m_Etype = EType.Array;
            eArrayType = eArrType;
            m_Length = length;
            CreateObject();
            CreateArray();
        }
        public override void SetValue(ClassObject val)
        {
            base.SetValue(val);
            var ao  = val as ArrayObject;

            Debug.Assert( ao != null );

            eArrayType = ao.eArrayType;
            m_Length = ao.m_Length;
            m_Array = ao.m_Array;
        }
        void CreateArray()
        {
            int length = m_Length;
            if(m_Length < 0 )
            {
                return;
            }
            switch (eArrayType)
            {
                case EArrayType.Boolean:
                    {
                        m_Array = new bool[length];
                        /*
                        m_Array = new Boolean[length];
                        for (int i = 0; i < length; i++)
                        {
                            var anyobj = new BoolObject(false);
                            anyobj.SetNull();
                            m_Array.SetValue(anyobj, i);
                        }
                        */
                    }
                    break;
                case EArrayType.Byte:
                    {
                        m_Array = new Byte[length];
                        /*
                        m_Array = new ByteObject[length];
                        for (int i = 0; i < length; i++)
                        {
                            var anyobj = new ByteObject(0);
                            anyobj.SetNull();
                            m_Array.SetValue(anyobj, i);
                        }
                        */
                    }
                    break;
                case EArrayType.SByte:
                    {
                        m_Array = new SByte[length];
                        /*
                        m_Array = new SByteObject[length];
                        for (int i = 0; i < length; i++)
                        {
                            var anyobj = new SByteObject(0);
                            anyobj.SetNull();
                            m_Array.SetValue(anyobj, i);
                        }
                        */
                    }
                    break;
                case EArrayType.Int16:
                    {
                        m_Array = new Int16[length];
                        /*
                        m_Array = new Int16Object[length];
                        for (int i = 0; i < length; i++)
                        {
                            var anyobj = new Int16Object(0);
                            anyobj.SetNull();
                            m_Array.SetValue(anyobj, i);
                        }
                        */
                    }
                    break;
                case EArrayType.UInt16:
                    {
                        m_Array = new UInt16[length];
                        /*
                        m_Array = new UInt16Object[length];
                        for (int i = 0; i < length; i++)
                        {
                            var anyobj = new UInt16Object(0);
                            anyobj.SetNull();
                            m_Array.SetValue(anyobj, i);
                        }
                        */
                    }
                    break;
                case EArrayType.Int32:
                    {
                        m_Array = new Int32[length];
                        /*
                        m_Array = new Int32Object[length];
                        for (int i = 0; i < length; i++)
                        {
                            var anyobj = new Int32Object(0);
                            anyobj.SetNull();
                            m_Array.SetValue(anyobj, i);
                        }
                        */
                    }
                    break;
                case EArrayType.UInt32:
                    {
                        m_Array = new UInt32[length];
                        /*
                        m_Array = new UInt32Object[length];

                        for (int i = 0; i < length; i++)
                        {
                            var anyobj = new UInt32Object(0);
                            anyobj.SetNull();
                            m_Array.SetValue(anyobj, i);
                        }
                        */
                    }
                    break;
                case EArrayType.Int64:
                    {
                        m_Array = new Int64[length];
                        /*
                        m_Array = new Int64Object[length];

                        for (int i = 0; i < length; i++)
                        {
                            var anyobj = new Int64Object(0);
                            anyobj.SetNull();
                            m_Array.SetValue(anyobj, i);
                        }
                        */
                    }
                    break;
                case EArrayType.UInt64:
                    {
                        m_Array = new UInt64[length];
                        /*
                        m_Array = new UInt64Object[length];

                        for (int i = 0; i < length; i++)
                        {
                            var anyobj = new UInt64Object(0);
                            anyobj.SetNull();
                            m_Array.SetValue(anyobj, i);
                        }
                        */
                    }
                    break;
                case EArrayType.String:
                    {
                        m_Array = new String[length];
                        /*
                        m_Array = new StringObject[length];

                        for (int i = 0; i < length; i++)
                        {
                            var anyobj = new StringObject("");
                            anyobj.SetNull();
                            m_Array.SetValue(anyobj, i);
                        }
                        */
                    }
                    break;
                case EArrayType.Array:
                    {
                        m_Array = new ArrayObject[length];
                        //for (int i = 0; i < length; i++)
                        //{
                        //    var anyobj = new ArrayObject("");
                        //    anyobj.SetNull();
                        //    m_Array.SetValue(anyobj, i);
                        //}
                        //for (int i = 0; i < m_IRMetaType.irMetaTypeList.Count; i++)
                        //{
                        //    var irlc = m_IRMetaType.irMetaTypeList[i];
                        //    RuntimeType rt = null;
                        //    if (irlc.isArray)
                        //    {
                        //        rt = RuntimeTypeManager.arrayRuntimeType;
                        //    }
                        //    else
                        //    {
                        //        rt = new RuntimeType(irlc.irMetaClass, new System.Collections.Generic.List<RuntimeType>());
                        //    }
                        //    SObject sobj = ObjectManager.CreateObjectByRuntimeType(rt, false);
                        //    m_Array.SetValue(sobj, i);
                        //}
                    }
                    break;
                case EArrayType.Any:
                    {
                        m_Array = new AnyObject[length];
                        /*
                        for (int i = 0; i < length; i++)
                        {
                            var anyobj = new AnyObject();
                            anyobj.SetNull();
                            m_Array.SetValue(anyobj, i);
                        }
                        */
                    }
                    break;
                case EArrayType.Class:
                    {
                        m_Array = new ClassObject[length];
                        /*
                        for (int i = 0; i < m_IRMetaType.irMetaTypeList.Count; i++)
                        {
                            var irlc = m_IRMetaType.irMetaTypeList[i];
                            RuntimeType rt = null;
                            if (irlc.isArray)
                            {
                                rt = RuntimeTypeManager.arrayRuntimeType;
                            }
                            else
                            {
                                rt = new RuntimeType(irlc.irMetaClass, new System.Collections.Generic.List<RuntimeType>());
                            }
                            SObject sobj = ObjectManager.CreateObjectByRuntimeType(rt, false);
                            m_Array.SetValue(sobj, i);
                        }
                        */
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
                    case EArrayType.Boolean:
                        {
                            BoolObject val = obj as BoolObject;
                            sval.SetBoolValue(val.value);
                        }
                        break;
                    case EArrayType.Byte:
                        {
                            ByteObject val = obj as ByteObject;
                            sval.SetInt8Value(val.value);
                        }
                        break;
                    case EArrayType.SByte:
                        {
                            SByteObject val = obj as SByteObject;
                            sval.SetSInt8Value(val.value);
                        }
                        break;
                    case EArrayType.Int16:
                        {
                            Int16Object val = obj as Int16Object;
                            sval.SetInt16Value(val.value);
                        }
                        break;
                    case EArrayType.UInt16:
                        {
                            UInt16Object val = obj as UInt16Object;
                            sval.SetUInt16Value(val.value);
                        }
                        break;
                    case EArrayType.Int32:
                        {
                            Int32Object val = obj as Int32Object;
                            sval.SetInt32Value(val.value);
                        }
                        break;
                    case EArrayType.UInt32:
                        {
                            UInt32Object val = obj as UInt32Object;
                            sval.SetUInt32Value(val.value);
                        }
                        break;
                    case EArrayType.Int64:
                        {
                            Int64Object val = obj as Int64Object;
                            sval.SetInt64Value(val.value);
                        }
                        break;
                    case EArrayType.UInt64:
                        {
                            UInt64Object val = obj as UInt64Object;
                            sval.SetUInt64Value(val.value);
                        }
                        break;
                    case EArrayType.Single:
                        {
                            FloatObject val = obj as FloatObject;
                            sval.SetFloatValue(val.value);
                        }
                        break;
                    case EArrayType.Double:
                        {
                            DoubleObject val = obj as DoubleObject;
                            sval.SetDoubleValue(val.value);
                        }
                        break;
                    case EArrayType.String:
                        {
                            StringObject val = obj as StringObject;
                            sval.SetStringValue(val.value);
                        }
                        break;
                    case EArrayType.Array:
                        {
                            var arr = obj as ArrayObject;
                            Debug.Assert(arr != null);
                            sval.SetSObject(arr);
                        }
                        break;
                    case EArrayType.Any:
                        {
                            sval.SetSObject(obj as AnyObject);
                        }
                        break;
                    case EArrayType.Class:
                        {
                            sval.SetSObject(obj as ClassObject);
                        }
                        break;
                    default:  
                        {
                            Debug.Assert(false);
                            Log.AddVM(EError.None, "不支持该类型的数组读取!!");
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
                        if (anyobj != null)
                        {
                            anyobj.SetNull();
                            return;
                        }
                        m_Array.SetValue(null, index);
                    }
                    break;
                case EType.Boolean:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValue( EType.Boolean, svalue.int8Value == 1 );
                            return;
                        }

                        //BoolObject boolobj = m_Array.GetValue(index) as BoolObject;
                        //if (boolobj == null)
                        //{
                        //    boolobj = new BoolObject(svalue.int8Value == 1 );
                        //    m_Array.SetValue(boolobj, index);
                        //    return;
                        //}
                        //else
                        //{
                            m_Array.SetValue(svalue.int8Value == 1 ? true : false, index);
                        //}
                    }
                    break;
                case EType.Byte:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValue(EType.Byte, svalue.int8Value);
                            return;
                        }
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
                        //UInt16Object uint16obj = m_Array.GetValue(index) as UInt16Object;
                        //if (uint16obj == null)
                        //{
                        //    uint16obj = new UInt16Object(svalue.uint16Value);
                        //    m_Array.SetValue(uint16obj, index);
                        //    return;
                        //}
                        //else
                        //{
                            m_Array.SetValue(svalue.uint16Value, index);
                        //}
                    }
                    break;
                case EType.Int32:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValue(EType.Int32, svalue.int32Value);
                            return;
                        }
                        //Int32Object int32obj = m_Array.GetValue(index) as Int32Object;
                        //if(int32obj == null )
                        //{
                        //    int32obj = new Int32Object(svalue.int32Value);
                        //    return;
                        //}
                        //else
                        //{
                                m_Array.SetValue(svalue.int32Value, index);
                        //}
                    }
                    break;
                case EType.UInt32:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValue(EType.UInt32, svalue.uint32Value);
                            return;
                        }
                        //UInt32Object int32obj = m_Array.GetValue(index) as UInt32Object;
                        //if (int32obj != null)
                        //{
                        //    int32obj.SetValue(svalue.uint32Value);
                        //    return;
                        //}
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
                        //Int64Object int64obj = m_Array.GetValue(index) as Int64Object;
                        //if (int64obj != null)
                        //{
                        //    int64obj.SetValue(svalue.int64Value);
                        //    return;
                        //}
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
                        //UInt64Object uint64obj = m_Array.GetValue(index) as UInt64Object;
                        //if (uint64obj != null)
                        //{
                        //    uint64obj.SetValue(svalue.uint64Value);
                        //    return;
                        //}
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
                        //FloatObject float32obj = m_Array.GetValue(index) as FloatObject;
                        //if (float32obj != null)
                        //{
                        //    float32obj.SetValue(svalue.floatValue);
                        //    return;
                        //}
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
                        //DoubleObject doubleobj = m_Array.GetValue(index) as DoubleObject;
                        //if (doubleobj != null)
                        //{
                        //    doubleobj.SetValue(svalue.doubleValue);
                        //    return;
                        //}
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
                        //StringObject stringobj = m_Array.GetValue(index) as StringObject;
                        //if (stringobj != null)
                        //{
                        //    stringobj.SetValue(svalue.stringValue);
                        //    return;
                        //}
                        m_Array.SetValue(svalue.stringValue, index);
                    }
                    break;
                case EType.Array:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValue(EType.Array, svalue.sobject);
                            return;
                        }
                        //ArrayObject arrayobj = m_Array.GetValue(index) as ArrayObject;
                        //if (arrayobj != null)
                        //{
                        //    arrayobj.SetValue(svalue.sobject as ClassObject);
                        //    return;
                        //}
                        m_Array.SetValue(svalue.sobject, index);
                    }
                    break;
                case EType.Class:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValue(EType.Class, svalue.sobject);
                            return;
                        }
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
                case  EArrayType.Boolean:
                    {
                        m_Array.SetValue( (byte)svalue == 1 ? true : false, index);
                    }
                    break;
                case EArrayType.Byte:
                    {
                        m_Array.SetValue((byte)svalue, index);
                    }
                    break;
                case EArrayType.SByte:
                    {
                        m_Array.SetValue((sbyte)svalue, index);
                    }
                    break;
                case EArrayType.Int16:
                    {
                        m_Array.SetValue((Int16)svalue, index);
                    }
                    break;
                case EArrayType.UInt16:
                    {
                        m_Array.SetValue((UInt16)svalue, index);
                    }
                    break;
                case EArrayType.Int32:
                    {
                        m_Array.SetValue((int)svalue, index);
                    }
                    break;
                case EArrayType.UInt32:
                    {
                        m_Array.SetValue((UInt32)svalue, index);
                    }
                    break;
                case EArrayType.Int64:
                    {
                        m_Array.SetValue((Int64)svalue, index);
                    }
                    break;
                case EArrayType.UInt64:
                    {
                        m_Array.SetValue((UInt64)svalue, index);
                    }
                    break;
                case EArrayType.Single:
                    {
                        m_Array.SetValue((float)svalue, index);
                    }
                    break;
                case EArrayType.Double:
                    {
                        m_Array.SetValue((double)svalue, index);
                    }
                    break;
                case EArrayType.String:
                    {
                        m_Array.SetValue( (string)svalue, index);
                    }
                    break;
                case EArrayType.Array:
                    {
                        ArrayObject ao = svalue as ArrayObject;
                        Debug.Assert(ao != null);
                        m_Array.SetValue(ao, index);
                    }
                    break;
                case EArrayType.Any:
                    {
                        AnyObject ao = svalue as AnyObject;
                        Debug.Assert(ao != null);
                        m_Array.SetValue(ao, index);
                    }
                    break;
                case EArrayType.Class:
                    {
                        ClassObject ao = svalue as ClassObject;
                        Debug.Assert(ao != null);
                        m_Array.SetValue(ao, index);
                    }
                    break;
                default:    
                    {
                        Debug.Assert(false);
                        Log.AddVM(EError.None, "不支持该类型的数组存储!!");
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
