//****************************************************************************
//  File:      ArrayObject.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/22 12:00:00
//  Description: 
//****************************************************************************
using SimpleLanguage.IR;
using SimpleLanguage.Logging;
using SimpleLanguage.VM.Runtime;
using System;
using System.Diagnostics;
namespace SimpleLanguage.VM
{
    public class ArrayObject : ClassObject
    {
        public int length => m_Length;
        public Array array => m_Array;

        private Array m_Array = null;
        private RuntimeType eArrayType = null;
        public ArrayObject(RuntimeType rt, int length )
        {
            m_Type = EVMType.Array;
            eArrayType = rt.runtimeTemplateList[0];
            m_Length = length;

            m_RuntimeType = rt;

            int byteCount = irMetaClass.byteCount;
            //m_Data = new byte[byteCount];
            typeId = (short)irMetaClass.id;
            m_IRTemplateList = rt.runtimeTemplateList;

            m_IRMetaVariableList = irMetaClass.localIRMetaVariableList;
            m_MemberObjectArray = new SObject[m_IRMetaVariableList.Count];
            m_MemberRuntimeTypeArray = new RuntimeType[m_IRMetaVariableList.Count];
            CreateDefine();
        }
        public override void CreateObject()
        {
            base.CreateObject();

            //m_MemberRuntimeTypeArray = m_RuntimeType.GetClassRuntimeType(m_RuntimeType, true);

            (this.m_MemberObjectArray[0] as Int32Object).SetValue(m_Length);

            CreateArray();
        }
        //public override void SetSValue(ClassObject val)
        //{
        //    base.SetValue(val);

        //    var ao  = m_Object as ArrayObject;

        //    Debug.Assert( ao != null );

        //    eArrayType = ao.eArrayType;
        //    m_Length = ao.m_Length;
        //    m_Array = ao.m_Array;
        //}
        void CreateArray()
        {
            int length = m_Length;
            if(m_Length < 0 )
            {
                return;
            }
            switch (eArrayType.eType)
            {
                case EVMType.Boolean:
                    {
                        m_Array = new bool?[length];
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
                case EVMType.Byte:
                    {
                        m_Array = new Byte?[length];
                        /*
                        m_Array = new Int8Object[length];
                        for (int i = 0; i < length; i++)
                        {
                            var anyobj = new Int8Object(0);
                            anyobj.SetNull();
                            m_Array.SetValue(anyobj, i);
                        }
                        */
                    }
                    break;
                case EVMType.SByte:
                    {
                        m_Array = new SByte?[length];
                        /*
                        m_Array = new SInt8Object[length];
                        for (int i = 0; i < length; i++)
                        {
                            var anyobj = new SInt8Object(0);
                            anyobj.SetNull();
                            m_Array.SetValue(anyobj, i);
                        }
                        */
                    }
                    break;
                case EVMType.Int16:
                    {
                        m_Array = new Int16?[length];
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
                case EVMType.UInt16:
                    {
                        m_Array = new UInt16?[length];
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
                case EVMType.Int32:
                    {
                        m_Array = new Int32?[length];
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
                case EVMType.UInt32:
                    {
                        m_Array = new UInt32?[length];
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
                case EVMType.Int64:
                    {
                        m_Array = new Int64?[length];
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
                case EVMType.UInt64:
                    {
                        m_Array = new UInt64?[length];
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
                case EVMType.String:
                    {
                        m_Array = new String?[length];
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
                case EVMType.Array:
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
                case EVMType.Object:
                    {
                        m_Array = new SObject[length];
                        for( int i = 0; i < length; i++ )
                        {
                            SObject sobj = new SObject(EVMType.Object);
                            sobj.SetNull();
                            m_Array.SetValue(sobj, i);
                        }
                    }
                    break;
                case EVMType.Type:
                    {
                        m_Array = new TypeObject[length];
                    }
                    break;
                case EVMType.Class:
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
                default:
                    {
                        Debug.Assert(false);
                        Log.AddVM(EError.None, "不支持该类型的数组创建!!");
                    }
                    break;
            }
        }
        public void LoadValue( int index, ref SValue sval )
        {
            if (index < 0)
            {
                Debug.Assert(false);
                Log.AddVM(EError.None, "执行的参数超出范围!! < 0 ");
                return;
            }
            if (index >= m_Length )
            {
                Debug.Assert(false);
                Log.AddVM(EError.None, "执行的参数超出范围!!");
                return;
            }

            object obj = m_Array.GetValue(index);


            SObject anyobj = null;
            if (m_Array.GetValue(index) is SObject sobj)
            {
                if (sobj.eType == EVMType.Object)
                {
                    anyobj = sobj;
                    obj = sobj.value;
                }
            }

            if ( obj != null )
            {
                switch (eArrayType.eType)
                {
                    case EVMType.Boolean:
                        {
                            BoolObject val = obj as BoolObject;
                            if (val != null)
                            {
                                sval.SetBoolValue(val.value);
                            }
                            else
                            {
                                sval.SetBoolValue((bool)obj);
                            }
                        }
                        break;
                    case EVMType.Byte:
                        {
                            Int8Object val = obj as Int8Object;
                            if (val != null)
                            {
                                sval.SetInt8Value( (byte)val.value );
                            }
                            else
                            {
                                sval.SetInt8Value((Byte)obj);
                            }
                        }
                        break;
                    case EVMType.SByte:
                        {
                            SInt8Object val = obj as SInt8Object;
                            if (val != null)
                            {
                                sval.SetSInt8Value(val.value);
                            }
                            else
                            {
                                sval.SetSInt8Value((SByte)obj);
                            }
                        }
                        break;
                    case EVMType.Int16:
                        {
                            Int16Object val = obj as Int16Object;
                            if (val != null)
                            {
                                sval.SetInt32Value(val.value);
                            }
                            else
                            {
                                sval.SetInt32Value((int)obj);
                            }
                            sval.SetInt16Value(val.value);
                        }
                        break;
                    case EVMType.UInt16:
                        {
                            UInt16Object val = obj as UInt16Object;
                            if (val != null)
                            {
                                sval.SetInt32Value(val.value);
                            }
                            else
                            {
                                sval.SetInt32Value((int)obj);
                            }
                            sval.SetUInt16Value(val.value);
                        }
                        break;
                    case EVMType.Int32:
                        {
                            Int32Object val = obj as Int32Object;
                            if (val != null)
                            {
                                sval.SetInt32Value(val.value);
                            }
                            else
                            {
                                sval.SetInt32Value((int)obj);
                            }
                        }
                        break;
                    case EVMType.UInt32:
                        {
                            UInt32Object val = obj as UInt32Object;
                            if (val != null)
                            {
                                sval.SetUInt32Value(val.value);
                            }
                            else
                            {
                                sval.SetUInt32Value((UInt32)obj);
                            }
                        }
                        break;
                    case EVMType.Int64:
                        {
                            Int64Object val = obj as Int64Object;
                            if (val != null)
                            {
                                sval.SetInt64Value(val.value);
                            }
                            else
                            {
                                sval.SetInt64Value((long)obj);
                            }
                        }
                        break;
                    case EVMType.UInt64:
                        {
                            UInt64Object val = obj as UInt64Object;
                            if (val != null)
                            {
                                sval.SetUInt64Value(val.value);
                            }
                            else
                            {
                                sval.SetUInt64Value((UInt64)obj);
                            }
                        }
                        break;
                    case EVMType.Float32:
                        {
                            Float32Object val = obj as Float32Object;
                            if (val != null)
                            {
                                sval.SetFloatValue(val.value);
                            }
                            else
                            {
                                sval.SetFloatValue((float)obj);
                            }
                        }
                        break;
                    case EVMType.Float64:
                        {
                            Float64Object val = obj as Float64Object;
                            if (val != null)
                            {
                                sval.SetDoubleValue(val.value);
                            }
                            else
                            {
                                sval.SetDoubleValue((double)obj);
                            }
                        }
                        break;
                    case EVMType.String:
                        {
                            StringObject val = obj as StringObject;
                            if (val != null)
                            {
                                sval.SetStringValue(val.value);
                            }
                            else
                            {
                                sval.SetStringValue((string)obj);
                            }
                        }
                        break;
                    case EVMType.Array:
                        {
                            var arr = obj as ArrayObject;
                            Debug.Assert(arr != null);
                            sval.SetSObject(arr);
                        }
                        break;
                    case EVMType.Type:
                        {
                            sval.SetSObject(obj as TypeObject);
                        }
                        break;
                    case EVMType.Object:
                        {
                            sval.SetSObject(obj as SObject);
                        }
                        break;
                    case EVMType.Class:
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
            SObject anyobj = null;
            if( m_Array.GetValue(index) is SObject sobj )
            {
                if( sobj.eType == EVMType.Object )
                {
                    anyobj = sobj;
                }
            }
            if( anyobj != null )
            {
                if( svalue.isNull )
                {
                    anyobj.SetNull();
                    return;
                }
                //var valobj = svalue.GetSObject();
            }


            switch (svalue.eType)
            {
                case EVMType.Null:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetNull();
                            return;
                        }
                        m_Array.SetValue(null, index);
                    }
                    break;
                case EVMType.Boolean:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.Boolean, svalue.int8Value == 1);
                            return;
                        }
                        //var int8obj = new BoolObject(svalue.int8Value == 1);
                        //anyobj.SetValue( EVMType.Boolean, int8obj );
                        m_Array.SetValue(svalue.int8Value == 1, index);
                    }
                    break;
                case EVMType.Byte:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.Byte, svalue.int8Value );
                            return;
                        }
                        //var i8obj = new Int8Object(svalue.int8Value);
                        //anyobj.SetValue(EVMType.Byte, i8obj );
                        m_Array.SetValue(svalue.int8Value, index);
                    }
                    break;
                case EVMType.SByte:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.SByte, svalue.sint8Value );
                            return;
                        }
                        //var si8obj = new SInt8Object(svalue.sint8Value);
                        //anyobj.SetValue(EVMType.SByte, si8obj );
                        m_Array.SetValue(svalue.sint8Value, index);
                    }
                    break;
                case EVMType.Int16:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.Int16, svalue.int16Value);
                            return;
                        }
                        //var i16obj = new Int16Object(svalue.int16Value);
                        //anyobj.SetValue(EVMType.Int16, i16obj );
                        m_Array.SetValue(svalue.int16Value, index);
                    }
                    break;
                case EVMType.UInt16:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.UInt16, svalue.uint16Value );
                            return;
                        }
                        //var ui16obj = new UInt16Object(svalue.uint16Value);
                        //anyobj.SetValue(EVMType.UInt16, ui16obj );
                        m_Array.SetValue(svalue.uint16Value, index);
                    }
                    break;
                case EVMType.Int32:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.Int32, svalue.int32Value);
                            return;
                        }
                        //var i32obj = new Int32Object(svalue.int32Value);
                        //anyobj.SetValue(EVMType.Int32, i32obj);
                        //m_Array.SetValue(i32obj, index);
                        m_Array.SetValue(svalue.int32Value, index);
                    }
                    break;
                case EVMType.UInt32:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.UInt32, svalue.int32Value );
                            return;
                        }
                        //var ui32obj = new UInt32Object(svalue.uint32Value);
                        //anyobj.SetValue(EVMType.UInt32, ui32obj );
                        m_Array.SetValue(svalue.uint32Value, index);
                    }
                    break;
                case EVMType.Int64:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.Int64, svalue.int64Value );
                            return;
                        }
                        //var i64obj = new Int64Object(svalue.int64Value);
                        //anyobj.SetValue(EVMType.Int64, i64obj );
                        m_Array.SetValue(svalue.int64Value, index);
                    }
                    break;
                case EVMType.UInt64:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.UInt64, svalue.uint64Value );
                            return;
                        }
                        //var ui64obj = new UInt64Object(svalue.uint64Value);
                        //anyobj.SetValue(EVMType.UInt64, ui64obj );
                        m_Array.SetValue(svalue.uint64Value, index);
                    }
                    break;
                case EVMType.Float32:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.Float32, svalue.floatValue );
                            return;
                        }
                        //var f32obj = new Float32Object(svalue.floatValue);
                        //anyobj.SetValue(EVMType.Float32, f32obj);
                        m_Array.SetValue(svalue.floatValue, index);
                    }
                    break;
                case EVMType.Float64:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.Float64, svalue.doubleValue );
                            return;
                        }
                        //var f64obj = new Float64Object(svalue.doubleValue);
                        //anyobj.SetValue(EVMType.Float64, f64obj );
                        m_Array.SetValue(svalue.doubleValue, index);
                    }
                    break;
                case EVMType.String:
                    {
                        if (anyobj != null)
                        {
                            anyobj.SetValueByType(EVMType.String, svalue.stringValue );
                            return;
                        }
                        //var stringobj = new StringObject(svalue.stringValue);
                        //anyobj.SetValue(EVMType.String, stringobj );
                        m_Array.SetValue(svalue.stringValue, index);
                    }
                    break;
                case EVMType.Array:
                    {
                        if (anyobj != null)
                        {
                            //anyobj.SetValue(EVMType.Array, svalue.sobject);
                            m_Array.SetValue(svalue.sobject, index);
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
                case EVMType.Class:
                    {
                        if (anyobj != null)
                        {
                            //anyobj.SetValue(EVMType.Class, svalue.sobject);
                            m_Array.SetValue(svalue.sobject, index);
                            return;
                        }
                        m_Array.SetValue(svalue.sobject, index);
                        //var mva = m_MemberObjectArray[index];
                        //if (mva.eType == EVMType.Byte)
                        //{

                        //    Int8Object byteObj = mva as Int8Object;
                        //    if (byteObj == null)
                        //    {
                        //        Log.AddVM(EError.None, "Class Int8Object 该类型不是Int32类型!!");
                        //        return;
                        //    }
                        //    byteObj.SetValue(svalue.int8Value);
                        //}
                        //else if (mva.eType == EVMType.SByte)
                        //{

                        //    SInt8Object sbyteObj = mva as SInt8Object;
                        //    if (sbyteObj == null)
                        //    {
                        //        Log.AddVM(EError.None, "Class SInt8Object 该类型不是Int32类型!!");
                        //        return;
                        //    }
                        //    sbyteObj.SetValue(svalue.sint8Value);
                        //}
                        //else if (mva.eType == EVMType.Int16)
                        //{

                        //    Int16Object int16Obj = mva as Int16Object;
                        //    if (int16Obj == null)
                        //    {
                        //        Log.AddVM(EError.None, "Class Int16Object 该类型不是Int16类型!!");
                        //        return;
                        //    }
                        //    int16Obj.SetValue(svalue.int16Value);
                        //}
                        //else if (mva.eType == EVMType.UInt16)
                        //{

                        //    UInt32Object uint32Obj = mva as UInt32Object;
                        //    if (uint32Obj == null)
                        //    {
                        //        Log.AddVM(EError.None, "Class UInt32Object 该类型不是UInt32类型!!");
                        //        return;
                        //    }
                        //    uint32Obj.SetValue(svalue.uint32Value);
                        //}
                        //else if (mva.eType == EVMType.Int32)
                        //{
                        //    Int32Object int32Obj = mva as Int32Object;
                        //    if (int32Obj == null)
                        //    {
                        //        Log.AddVM(EError.None, "Class Int32Object 该类型不是Int32类型!!");
                        //        return;
                        //    }
                        //    int32Obj.SetValue(svalue.int32Value);
                        //}
                        //else if (mva.eType == EVMType.UInt32)
                        //{

                        //    UInt32Object uint32Obj = mva as UInt32Object;
                        //    if (uint32Obj == null)
                        //    {
                        //        Log.AddVM(EError.None, "Class UInt32Object 该类型不是Int32类型!!");
                        //        return;
                        //    }
                        //    uint32Obj.SetValue(svalue.uint32Value);
                        //}
                        //else if (mva.eType == EVMType.Int64)
                        //{

                        //    Int64Object int64Obj = mva as Int64Object;
                        //    if (int64Obj == null)
                        //    {
                        //        Log.AddVM(EError.None, "该类型不是Int64类型!!");
                        //        return;
                        //    }
                        //    int64Obj.SetValue(svalue.int64Value);
                        //}
                        //else if (mva.eType == EVMType.UInt64)
                        //{

                        //    UInt64Object uint64Obj = mva as UInt64Object;
                        //    if (uint64Obj == null)
                        //    {
                        //        Log.AddVM(EError.None, "该类型不是Int64类型!!");
                        //        return;
                        //    }
                        //    uint64Obj.SetValue(svalue.uint64Value);
                        //}
                        //else if (mva.eType == EVMType.String)
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
                        //            anyObj.SetValue(EVMType.Class, svalue.sobject);
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
            switch (eArrayType.eType)
            {
                case  EVMType.Boolean:
                    {
                        m_Array.SetValue( (byte)svalue == 1 ? true : false, index);
                    }
                    break;
                case EVMType.Byte:
                    {
                        m_Array.SetValue((byte)svalue, index);
                    }
                    break;
                case EVMType.SByte:
                    {
                        m_Array.SetValue((sbyte)svalue, index);
                    }
                    break;
                case EVMType.Int16:
                    {
                        m_Array.SetValue((Int16)svalue, index);
                    }
                    break;
                case EVMType.UInt16:
                    {
                        m_Array.SetValue((UInt16)svalue, index);
                    }
                    break;
                case EVMType.Int32:
                    {
                        m_Array.SetValue((int)svalue, index);
                    }
                    break;
                case EVMType.UInt32:
                    {
                        m_Array.SetValue((UInt32)svalue, index);
                    }
                    break;
                case EVMType.Int64:
                    {
                        m_Array.SetValue((Int64)svalue, index);
                    }
                    break;
                case EVMType.UInt64:
                    {
                        m_Array.SetValue((UInt64)svalue, index);
                    }
                    break;
                case EVMType.Float32:
                    {
                        m_Array.SetValue((float)svalue, index);
                    }
                    break;
                case EVMType.Float64:
                    {
                        m_Array.SetValue((double)svalue, index);
                    }
                    break;
                case EVMType.String:
                    {
                        m_Array.SetValue( (string)svalue, index);
                    }
                    break;
                case EVMType.Array:
                    {
                        ArrayObject ao = svalue as ArrayObject;
                        Debug.Assert(ao != null);
                        m_Array.SetValue(ao, index);
                    }
                    break;
                case EVMType.Object:
                    {
                        SObject ao = svalue as SObject;
                        Debug.Assert(ao != null);
                        m_Array.SetValue(ao, index);
                    }
                    break;
                case EVMType.Class:
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
