//****************************************************************************
//  File:      IRMethod.cs
// ------------------------------------------------
//  Copyright (c) kamaba233@gmail.com
//  DateTime: 2022/11/2 12:00:00
//  Description: 
//****************************************************************************

using SimpleLanguage.IR;
using SimpleLanguage.Parse;
using System.Collections.Generic;

namespace SimpleLanguage.VM
{
    public class RuntimeType
    {
        public IRMetaClass irClass;
        public List<RuntimeType> runtimeTemplateList = new List<RuntimeType>();
        public List<RuntimeType> memberVariableRuntimeTypeList = new List<RuntimeType>();
        public RuntimeType(IRMetaClass rc, List<RuntimeType > rtList )
        {
            irClass = rc;
            runtimeTemplateList = rtList;
        }
        private List<SObject> m_StaticMemVariableList = new List<SObject>();
        public void GetMemberVariableSValue(int index, ref SValue svalue)
        {
            if (index < 0)
            {
                Log.AddVM(EError.None, "执行的参数超出范围!! < 0 ");
                return;
            }
            if (index > m_StaticMemVariableList.Count)
            {
                Log.AddVM(EError.None, "执行的参数超出范围!!");
                return;
            }
            var mmv = m_StaticMemVariableList[index];
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
                        svalue.SetStringValue(stringObj.value);
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

        public void SetMemberVariableSValue(int index, SValue svalue)
        {
            if (index > m_StaticMemVariableList.Count)
            {
                Log.AddVM(EError.None, "执行的参数超出范围!!");
                return;
            }
            switch (svalue.eType)
            {
                case EType.Null:
                    {
                        ClassObject classObj = m_StaticMemVariableList[index] as ClassObject;
                        if (classObj == null)
                        {
                            Log.AddVM(EError.None, "Null 该类型不是Int32类型!!");
                            return;
                        }
                        classObj.SetNull();
                    }
                    break;
                case EType.Boolean:
                    {

                    }
                    break;
                case EType.Byte:
                    {
                        ByteObject byteObj = m_StaticMemVariableList[index] as ByteObject;
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
                        SByteObject sbyteObj = m_StaticMemVariableList[index] as SByteObject;
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
                        Int16Object int32Obj = m_StaticMemVariableList[index] as Int16Object;
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
                        UInt16Object uint16Obj = m_StaticMemVariableList[index] as UInt16Object;
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
                        Int32Object int32Obj = m_StaticMemVariableList[index] as Int32Object;
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
                        UInt32Object uint32Obj = m_StaticMemVariableList[index] as UInt32Object;
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
                        Int64Object int64Obj = m_StaticMemVariableList[index] as Int64Object;
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
                        UInt64Object uint64Obj = m_StaticMemVariableList[index] as UInt64Object;
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
                        FloatObject floatObj = m_StaticMemVariableList[index] as FloatObject;
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
                        DoubleObject doubleObj = m_StaticMemVariableList[index] as DoubleObject;
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
                        StringObject stringObj = m_StaticMemVariableList[index] as StringObject;
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
                        var mva = m_StaticMemVariableList[index];
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
                            ClassObject classObj = m_StaticMemVariableList[index] as ClassObject;
                            if (classObj == null)
                            {
                                Log.AddVM(EError.None, "该类型不是classObj类型!!");
                                return;
                            }
                            //classObj.SetValue(svalue.sobject as ClassObject);
                            m_StaticMemVariableList[index] = svalue.sobject as ClassObject;
                        }
                    }
                    break;
            }
        }
        public static bool SameRuntimeType( RuntimeType rt1, RuntimeType rt2 )
        {
            if( rt1.irClass != rt2.irClass )
            {
                return false;
            }
            if( rt1.runtimeTemplateList.Count != rt2.runtimeTemplateList.Count )
            {
                return false;
            }
            for( int i = 0; i < rt1.runtimeTemplateList.Count; i++ )
            {
                if( SameRuntimeType(rt1.runtimeTemplateList[i], rt2.runtimeTemplateList[i] ) == false )
                {
                    return false;
                }
            }
            return true;
        }
    }

    public class RuntimeTypeManager
    {
        public static List<RuntimeType> staticRuntimeList = new List<RuntimeType>();

        public static RuntimeType GetRuntimeTypeByMTAndTemplateMT( IRMetaClass rmc, List<RuntimeType> inputTemplateTypeList )
        {
            foreach( var v in staticRuntimeList )
            {
                if( v.irClass != rmc)
                {
                    continue;
                }

                if( v.runtimeTemplateList.Count == inputTemplateTypeList.Count )
                {
                    for (int i = 0; i < inputTemplateTypeList.Count; i++)
                    {
                        if( RuntimeType.SameRuntimeType(inputTemplateTypeList[i], v.runtimeTemplateList[i] ) )
                        {
                            return v;
                        }
                    }
                }

            }
            return null;
        }
        public static RuntimeType AddRuntimeTypeByClassAndTemplate(IRMetaClass rmc, List<RuntimeType> inputTemplateTypeList)
        {
            RuntimeType rt = new RuntimeType(rmc, inputTemplateTypeList);

            staticRuntimeList.Add(rt);

            return rt;
        }
    }
}
